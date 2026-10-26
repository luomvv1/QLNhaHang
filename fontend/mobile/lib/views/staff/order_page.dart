import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import 'payment_page.dart';
import 'shared_widgets.dart';

class OrderPage extends StatefulWidget {
  const OrderPage({super.key, required this.api, required this.table});
  final ApiClient api;
  final Map<String, dynamic> table;

  @override
  State<OrderPage> createState() => _OrderPageState();
}

class _OrderPageState extends State<OrderPage>
    with SingleTickerProviderStateMixin {
  Map<String, dynamic>? detail;
  List<Map<String, dynamic>> categories = [];
  List<Map<String, dynamic>> foods = [];
  TabController? tabs;
  String? selectedCategory;
  bool isPendingExpanded = true;
  bool loading = true;
  String? error;
  Timer? _realtimeTimer;
  bool _polling = false;
  bool _acting = false;

  int get tableId => integer(widget.table['tableId']);
  Map<String, dynamic>? get order =>
      detail?['order'] is Map ? jsonMap(detail!['order']) : null;
  List<Map<String, dynamic>> get items =>
      order == null ? [] : jsonMaps(order!['items']);
  int get orderId => integer(order?['orderId']);

  @override
  void initState() {
    super.initState();
    tabs = TabController(length: 2, vsync: this);
    load();
    _startRealtimePolling();
  }

  @override
  void dispose() {
    _realtimeTimer?.cancel();
    tabs?.dispose();
    super.dispose();
  }

  // Polling tự động ngầm 2 giây/lần để nhận món xong từ Bếp realtime
  void _startRealtimePolling() {
    _realtimeTimer?.cancel();
    _realtimeTimer = Timer.periodic(const Duration(seconds: 2), (_) {
      if (mounted) _pollOrderSilently();
    });
  }

  Future<void> _pollOrderSilently() async {
    if (_polling || _acting) return;
    _polling = true;
    try {
      final results = await Future.wait<dynamic>([
        widget.api.get('/staff/orders/table/$tableId'),
        widget.api.get('/staff/orders/menu'),
      ]);
      final currentDetail = jsonMap(results[0]);
      if (currentDetail['order'] == null) {
        if (mounted) {
          _realtimeTimer?.cancel();
          Navigator.of(context).pop(true);
        }
        return;
      }
      final menu = jsonMap(results[1]);
      if (mounted && !_acting) {
        setState(() {
          detail = currentDetail;
          foods = jsonMaps(menu['foods']);
          categories = jsonMaps(menu['categories']);
          error = currentDetail['order'] == null ? 'Order đã đóng. Vui lòng quay lại sơ đồ bàn.' : null;
        });
      }
    } catch (_) {
      // Chạy ngầm, không hiện lỗi
    } finally { _polling = false; }
  }

  Future<void> load({bool silent = false}) async {
    if (!silent && detail == null) {
      setState(() {
        loading = true;
        error = null;
      });
    }
    try {
      final results = await Future.wait<dynamic>([
        widget.api.get('/staff/orders/table/$tableId'),
        widget.api.get('/staff/orders/menu'),
      ]);
      final currentDetail = jsonMap(results[0]);
      if (currentDetail['order'] == null) {
        if (mounted) {
          _realtimeTimer?.cancel();
          Navigator.of(context).pop(true);
        }
        return;
      }
      final menu = jsonMap(results[1]);
      if (mounted) {
        setState(() {
          detail = currentDetail;
          categories = jsonMaps(menu['categories']);
          foods = jsonMaps(menu['foods']);
          error = null;
        });
      }
    } on ApiException catch (exception) {
      if (mounted && detail == null) {
        setState(() => error = exception.message);
      } else if (mounted) {
        showMessage(context, exception.message, isError: true);
      }
    } finally {
      if (mounted && loading) setState(() => loading = false);
    }
  }

  Future<void> action(Future<dynamic> Function() request, String message) async {
    if (_acting) return;
    _acting = true;
    try {
      await request();
      if (mounted) showMessage(context, message);
      await load(silent: true);
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    } finally { _acting = false; }
  }

  // Thêm món vào order
  Future<void> addFood(Map<String, dynamic> food) async {
    if (orderId <= 0) return;
    if (food['canOrder'] == false) return;
    final variants = jsonMaps(food['variants']);
    if (variants.isEmpty) {
      showMessage(context, 'Món này chưa có biến thể đang mở bán.', isError: true);
      return;
    }
    final request = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => AddFoodSheet(food: food, variants: variants),
    );
    if (request != null) {
      await action(
        () => widget.api.post('/staff/orders/$orderId/items', data: request),
        'Đã thêm món vào order.',
      );
    }
  }

  // Chỉnh sửa món khi chưa gửi bếp
  Future<void> editItem(Map<String, dynamic> item) async {
    final foodId = integer(item['foodId']);
    final food = foods.firstWhere(
      (f) => integer(f['foodId']) == foodId,
      orElse: () => <String, dynamic>{},
    );
    final itemVariants = jsonMaps(food['variants']);

    final result = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => EditOrderItemSheet(
        item: item,
        variants: itemVariants,
      ),
    );
    if (result != null) {
      final itemId = integer(item['orderItemId']);
      await action(
        () => widget.api.patch(
          '/staff/orders/$orderId/items/$itemId',
          data: {
            'variantId': result['variantId'],
            'quantity': result['quantity'],
            'note': result['note'],
          },
        ),
        'Đã cập nhật món thành công.',
      );
    }
  }

  // Tăng số lượng món pending
  Future<void> increaseQty(Map<String, dynamic> item) async {
    final qty = integer(item['quantity']);
    final itemId = integer(item['orderItemId']);
    await action(
      () => widget.api.patch(
        '/staff/orders/$orderId/items/$itemId',
        data: {'quantity': qty + 1, 'note': text(item['note'])},
      ),
      'Đã tăng số lượng.',
    );
  }

  // Giảm số lượng món pending
  Future<void> decreaseQty(Map<String, dynamic> item) async {
    final qty = integer(item['quantity']);
    final itemId = integer(item['orderItemId']);
    if (qty > 1) {
      await action(
        () => widget.api.patch(
          '/staff/orders/$orderId/items/$itemId',
          data: {'quantity': qty - 1, 'note': text(item['note'])},
        ),
        'Đã giảm số lượng.',
      );
    } else {
      deleteItem(item);
    }
  }

  // Xóa món
  Future<void> deleteItem(Map<String, dynamic> item) async {
    final itemId = integer(item['orderItemId']);
    await action(
      () => widget.api.delete('/staff/orders/$orderId/items/$itemId'),
      'Đã xóa món ${text(item['foodNameSnapshot'])}.',
    );
  }

  Future<String?> cancellationReason(String title) async {
    return askCancellationReason(context, title);
  }

  Future<void> cancelSentItem(Map<String, dynamic> item) async {
    final reason = await cancellationReason('Hủy ${text(item['foodNameSnapshot'])}');
    if (reason == null || !mounted) return;
    await action(() => widget.api.post('/staff/orders/$orderId/items/${integer(item['orderItemId'])}/cancel',
      data: {'reason': reason}), 'Đã hủy món và cập nhật tiền đơn.');
  }

  // Menu tác vụ bàn
  Future<void> tableActions() async {
    final choice = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      builder: (sheet) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ActionRow(
                icon: Icons.swap_horiz,
                title: 'Chuyển bàn phục vụ',
                subtitle: 'Chuyển toàn bộ order sang một bàn trống khác',
                onTap: () => Navigator.pop(sheet, 'switch'),
              ),
              ActionRow(
                icon: Icons.call_merge,
                title: 'Ghép thêm bàn trống',
                subtitle: 'Ghép thêm bàn vào order phục vụ đoàn đông',
                onTap: () => Navigator.pop(sheet, 'link'),
              ),
              ActionRow(
                icon: Icons.merge_type,
                title: 'Gộp hai order bàn ăn',
                subtitle: 'Gộp món từ bàn khác sang bàn này',
                onTap: () => Navigator.pop(sheet, 'merge'),
              ),
              ActionRow(
                icon: Icons.cancel_outlined,
                title: 'Hủy order này',
                subtitle: 'Chỉ hủy được khi chưa có món nào bắt đầu nấu',
                danger: true,
                onTap: () => Navigator.pop(sheet, 'cancel'),
              ),
            ],
          ),
        ),
      ),
    );
    if (!mounted || choice == null) return;

    if (choice == 'cancel') {
      if (items.any((item) => text(item['itemStatus']) != 'cancelled' &&
          (item['canCancel'] == false || ['cooking', 'ready', 'served'].contains(text(item['itemStatus']))))) {
        showMessage(context, 'Đã có món bắt đầu nấu, không được hủy order.', isError: true);
        return;
      }
      final confirm = await showDialog<bool>(
        context: context,
        builder: (dialog) => AlertDialog(
          title: const Text('Xác nhận hủy order'),
          content: const Text(
            'Các món chưa chế biến sẽ bị hủy và bàn sẽ được giải phóng về trạng thái trống?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialog, false),
              child: const Text('Không'),
            ),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: const Color(0xFFC62828)),
              onPressed: () => Navigator.pop(dialog, true),
              child: const Text('Hủy order'),
            ),
          ],
        ),
      );
      if (confirm == true) {
        final reason = await cancellationReason('Lý do hủy order');
        if (reason == null || !mounted) return;
        try {
          await widget.api.post('/staff/orders/$orderId/cancel', data: {'reason': reason});
          if (mounted) Navigator.pop(context);
        } on ApiException catch (exception) {
          if (mounted) showMessage(context, exception.message, isError: true);
        }
      }
      return;
    }

    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      final currentArea = text(widget.table['areaName']).trim();
      final allowed = choice == 'merge'
          ? map
                .where((t) =>
                    text(t['tableStatus']) == 'serving' &&
                    integer(t['tableId']) != tableId &&
                    text(t['areaName']).trim().toLowerCase() == currentArea.toLowerCase())
                .toList()
          : choice == 'link'
          ? map
                .where((t) =>
                    text(t['tableStatus']) == 'empty' &&
                    integer(t['tableId']) != tableId &&
                    text(t['areaName']).trim().toLowerCase() == currentArea.toLowerCase())
                .toList()
          : map.where((t) => text(t['tableStatus']) == 'empty' && integer(t['tableId']) != tableId).toList();

      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        showDragHandle: false,
        backgroundColor: Colors.transparent,
        builder: (_) => TablePicker(
          title: choice == 'switch'
              ? 'Chọn bàn trống để chuyển đến'
              : choice == 'link'
              ? 'Ghép thêm bàn trống'
              : 'Gộp hai order bàn',
          subtitle: 'Bàn gốc: ${text(widget.table['tableName'])} · $currentArea',
          currentTable: widget.table,
          areaName: currentArea,
          actionType: choice,
          tables: allowed,
        ),
      );
      if (target == null) return;
      final targetId = integer(target['tableId']);

      if (choice == 'switch') {
        await action(
          () => widget.api.post(
            '/staff/orders/switch-table',
            data: {'fromTableId': tableId, 'toTableId': targetId},
          ),
          'Đã chuyển bàn thành công.',
        );
        if (mounted) Navigator.pop(context);
      } else {
        await action(
          () => widget.api.post(
            '/staff/orders/merge-tables',
            data: {
              'currentTableId': tableId,
              'targetTableId': targetId,
              'actionType': choice == 'link' ? 'link_empty' : 'merge_orders',
              'keepSourceTableLinked': true,
            },
          ),
          choice == 'link' ? 'Đã ghép thêm bàn.' : 'Đã gộp hai order.',
        );
      }
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  // Danh sách các món trong Order (Phân tách món mới pending và món đã gửi bếp)
  Widget _buildOrderList(List<Map<String, dynamic>> currentItems) {
    final ord = order;
    if (ord == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.check_circle_outline, color: Color(0xFF10B981), size: 54),
              const SizedBox(height: 14),
              const Text(
                'Bàn đã hoàn tất thanh toán.',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 16),
              FilledButton.tonal(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Quay về sơ đồ bàn'),
              ),
            ],
          ),
        ),
      );
    }

    final isDark = isDarkMode(context);
    final pendingItems = currentItems.where((i) => text(i['itemStatus']) == 'pending').toList();
    final confirmedItems = currentItems.where((i) => text(i['itemStatus']) != 'pending').toList();
    final readyCount = currentItems.where((i) => text(i['itemStatus']) == 'ready').length;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Thẻ tóm tắt Order
        OrderSummary(table: widget.table, order: ord),
        const SizedBox(height: 12),

        // CẢNH BÁO BẾP BÁO XONG MÓN CẦN BƯNG RA BÀN
        if (readyCount > 0) ...[
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: const Color(0xFFDC2626),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                const Text('🛎️', style: TextStyle(fontSize: 18)),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'CÓ $readyCount MÓN ĐÃ NẤU XONG! Hãy mang ra bàn phục vụ.',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
        ],

        // KHU VỰC 1: MÓN CHƯA GỬI BẾP (CÓ NÚT THU GỌN VÀ CHỈNH SỬA)
        if (pendingItems.isNotEmpty) ...[
          Container(
            decoration: BoxDecoration(
              color: isDark ? const Color(0xFF1E293B) : const Color(0xFFFFF9E6),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: isDark ? gold.withValues(alpha: 0.4) : const Color(0xFFFFE8A3),
              ),
            ),
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Dòng tiêu đề có nút Thu gọn / Mở rộng
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(6),
                      decoration: const BoxDecoration(
                        color: Color(0xFFB85108),
                        shape: BoxShape.circle,
                      ),
                      child: Text(
                        '${pendingItems.length}',
                        style: const TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.w900,
                          fontSize: 12,
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      'Món mới chờ gửi bếp',
                      style: TextStyle(
                        fontWeight: FontWeight.w900,
                        fontSize: 14,
                        color: isDark ? gold : const Color(0xFF78350F),
                      ),
                    ),
                    const Spacer(),
                    // NÚT THU GỌN / MỞ RỘNG THEO YÊU CẦU NGƯỜI DÙNG
                    TextButton.icon(
                      style: TextButton.styleFrom(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        minimumSize: Size.zero,
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                        foregroundColor: isDark ? gold : const Color(0xFFB85108),
                      ),
                      onPressed: () => setState(() => isPendingExpanded = !isPendingExpanded),
                      icon: Icon(
                        isPendingExpanded ? Icons.keyboard_arrow_up : Icons.keyboard_arrow_down,
                        size: 18,
                      ),
                      label: Text(
                        isPendingExpanded ? 'Thu gọn' : 'Mở rộng (${pendingItems.length})',
                        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800),
                      ),
                    ),
                  ],
                ),

                // Danh sách món pending khi mở rộng
                if (isPendingExpanded) ...[
                  const SizedBox(height: 10),
                  ...pendingItems.map(
                    (item) => Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: OrderItem(
                        item: item,
                        onEdit: () => editItem(item),
                        onDelete: () => deleteItem(item),
                      ),
                    ),
                  ),
                  const SizedBox(height: 6),
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF106854),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 10),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    onPressed: () => action(
                      () => widget.api.post('/staff/orders/$orderId/confirm'),
                      'Đã xác nhận gửi toàn bộ món mới xuống bếp.',
                    ),
                    icon: const Icon(Icons.send_rounded, size: 16),
                    label: Text(
                      'Xác nhận gửi ${pendingItems.length} món xuống bếp ✓',
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13),
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 14),
        ],

        // KHU VỰC 2: MÓN ĐÃ GỬI BẾP (THEO DÕI TRẠNG THÁI)
        if (confirmedItems.isNotEmpty) ...[
          Row(
            children: [
              Icon(Icons.receipt_long, size: 18, color: isDark ? gold : brand),
              const SizedBox(width: 6),
              Text(
                'Món đang phục vụ (${confirmedItems.length})',
                style: TextStyle(
                  fontWeight: FontWeight.w900,
                  fontSize: 14,
                  color: isDark ? darkTextPrimary : brand,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ...confirmedItems.map(
            (item) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: OrderItem(
                item: item,
                onDelete: text(item['itemStatus']) == 'confirmed' && item['canCancel'] != false ? () => cancelSentItem(item) : null,
                onServe: text(item['itemStatus']) == 'ready'
                    ? () => action(
                        () => widget.api.patch(
                          '/staff/orders/$orderId/items/${integer(item['orderItemId'])}/served',
                        ),
                        'Đã xác nhận phục vụ món ra bàn.',
                      )
                    : null,
              ),
            ),
          ),
        ],

        if (currentItems.isEmpty)
          const EmptyState(message: 'Order chưa có món ăn. Hãy chọn món từ thực đơn bên cạnh.'),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final isTablet = MediaQuery.sizeOf(context).width >= 750;
    final currentItems = items;
    final hasPendingItems = currentItems.any((item) => text(item['itemStatus']) == 'pending');

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              text(widget.table['tableName']),
              style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 17),
            ),
            Text(
              '${text(widget.table['areaName'], 'Khu chung')} · Sức chứa ${integer(widget.table['capacity'])} người (Realtime)',
              style: const TextStyle(fontSize: 11, color: Color(0xFF6F7E78), fontWeight: FontWeight.normal),
            ),
          ],
        ),
        // ĐÃ BỎ NÚT LOAD LẠI THEO YÊU CẦU NGƯỜI DÙNG
        actions: [
          IconButton(
            onPressed: loading ? null : tableActions,
            tooltip: 'Tác vụ bàn',
            icon: const Icon(Icons.more_vert),
          ),
        ],
        bottom: loading || error != null || isTablet
            ? null
            : TabBar(
                controller: tabs,
                indicatorColor: gold,
                labelColor: brand,
                unselectedLabelColor: const Color(0xFF75857F),
                tabs: [
                  Tab(
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Text('Order của bàn'),
                        if (currentItems.isNotEmpty) ...[
                          const SizedBox(width: 6),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(color: brand, borderRadius: BorderRadius.circular(10)),
                            child: Text(
                              '${currentItems.length}',
                              style: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                  const Tab(text: 'Thực đơn món'),
                ],
              ),
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? RetryState(message: error!, onRetry: load)
          : isTablet
          // BỐ CỤC TABLET: 2 CỘT SONG SONG
          ? Row(
              children: [
                Expanded(
                  flex: 6,
                  child: MenuTab(
                    categories: categories,
                    foods: foods,
                    selectedCategory: selectedCategory,
                    onCategory: (v) => setState(() => selectedCategory = v),
                    onFood: addFood,
                  ),
                ),
                const VerticalDivider(width: 1, thickness: 1, color: cardBorder),
                Expanded(
                  flex: 5,
                  child: _buildOrderList(currentItems),
                ),
              ],
            )
          // BỐ CỤC ĐIỆN THOẠI: 2 TABS
          : TabBarView(
              controller: tabs,
              children: [
                _buildOrderList(currentItems),
                MenuTab(
                  categories: categories,
                  foods: foods,
                  selectedCategory: selectedCategory,
                  onCategory: (v) => setState(() => selectedCategory = v),
                  onFood: addFood,
                ),
              ],
            ),
      bottomNavigationBar: loading || error != null
          ? null
          : SafeArea(
              top: false,
              child: Container(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 12),
                decoration: BoxDecoration(
                  color: themedSurface(context),
                  border: Border(top: BorderSide(color: themedBorder(context))),
                ),
                child: Row(
                  children: [
                    if (!isTablet) ...[
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: () => tabs?.animateTo(1),
                          icon: const Icon(Icons.add_circle_outline, size: 18),
                          label: const Text('Thêm món'),
                        ),
                      ),
                      const SizedBox(width: 8),
                    ],
                    Expanded(
                      child: FilledButton.icon(
                        style: FilledButton.styleFrom(
                          backgroundColor: hasPendingItems ? const Color(0xFF106854) : const Color(0xFFB0BEC5),
                        ),
                        onPressed: hasPendingItems
                            ? () => action(
                                () => widget.api.post('/staff/orders/$orderId/confirm'),
                                'Đã gửi toàn bộ món mới xuống bếp.',
                              )
                            : null,
                        icon: const Icon(Icons.send_rounded, size: 18),
                        label: Text(hasPendingItems ? 'Gửi Bếp' : 'Đã gửi bếp'),
                      ),
                    ),
                    const SizedBox(width: 8),
                    FilledButton.icon(
                      style: FilledButton.styleFrom(backgroundColor: gold, foregroundColor: brand),
                      onPressed: () async {
                        final navigator = Navigator.of(context);
                        await navigator.push<bool>(
                          MaterialPageRoute(
                            builder: (_) => PaymentPage(api: widget.api, orderId: orderId),
                          ),
                        );
                        if (mounted) {
                          _realtimeTimer?.cancel();
                          navigator.pop(true);
                        }
                      },
                      icon: const Icon(Icons.payments, size: 18),
                      label: const Text('Thanh Toán', style: TextStyle(fontWeight: FontWeight.w900)),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}

class AddFoodSheet extends StatefulWidget {
  const AddFoodSheet({super.key, required this.food, required this.variants});
  final Map<String, dynamic> food;
  final List<Map<String, dynamic>> variants;

  @override
  State<AddFoodSheet> createState() => _AddFoodSheetState();
}

class _AddFoodSheetState extends State<AddFoodSheet> {
  late Map<String, dynamic> variant = widget.variants.firstWhere((v) => v['canOrder'] != false);
  final note = TextEditingController();
  int quantity = 1;

  @override
  void dispose() {
    note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(20, 6, 20, 24 + MediaQuery.viewInsetsOf(context).bottom),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: text(widget.food['imageUrl']).isNotEmpty
                        ? Image.network(
                            text(widget.food['imageUrl']),
                            width: 44,
                            height: 44,
                            fit: BoxFit.cover,
                            errorBuilder: (_, _, _) => Container(
                              padding: const EdgeInsets.all(8),
                              decoration: BoxDecoration(
                                color: isDark ? const Color(0xFF0F172A) : brand.withValues(alpha: 0.1),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Icon(Icons.restaurant, color: isDark ? gold : brand, size: 22),
                            ),
                          )
                        : Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: isDark ? const Color(0xFF0F172A) : brand.withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Icon(Icons.restaurant, color: isDark ? gold : brand, size: 22),
                          ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          text(widget.food['foodName']),
                          style: TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 17,
                            color: isDark ? darkTextPrimary : brand,
                          ),
                        ),
                        if (text(widget.food['description']).isNotEmpty)
                          Text(
                            text(widget.food['description']),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontSize: 12,
                              color: isDark ? darkTextSecondary : const Color(0xFF6F7E78),
                            ),
                          ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<Map<String, dynamic>>(
                initialValue: variant,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Kích cỡ / Biến thể món'),
                items: widget.variants
                    .map(
                      (item) => DropdownMenuItem(
                        value: item,
                        enabled: item['canOrder'] != false,
                        child: Text(
                          '${text(item['variantName'])} · ${money(number(item['price']))}${item['canOrder'] == false ? ' · ${text(item['availabilityText'], 'Hết món')}' : ''}',
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                      ),
                    )
                    .toList(),
                onChanged: (value) {
                  if (value != null) setState(() => variant = value);
                },
              ),
              const SizedBox(height: 14),
              Row(
                children: [
                  const Text('Số lượng phần:', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 14)),
                  const Spacer(),
                  IconButton(
                    onPressed: quantity > 1 ? () => setState(() => quantity--) : null,
                    icon: const Icon(Icons.remove_circle, color: gold, size: 28),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                    child: Text(
                      '$quantity',
                      style: TextStyle(
                        fontWeight: FontWeight.w900,
                        fontSize: 18,
                        color: isDark ? gold : brand,
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: () => setState(() => quantity++),
                    icon: Icon(Icons.add_circle, color: isDark ? gold : brand, size: 28),
                  ),
                ],
              ),
              const SizedBox(height: 10),
            TextField(
              controller: note,
              maxLines: 2,
              decoration: const InputDecoration(
                labelText: 'Ghi chú chế biến gửi bếp',
                hintText: 'Ví dụ: ít cay, không hành, làm chín kĩ...',
                prefixIcon: Icon(Icons.edit_note),
              ),
            ),
            const SizedBox(height: 20),
            FilledButton.icon(
              style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
              onPressed: () => Navigator.pop(context, {
                'foodId': integer(widget.food['foodId']),
                'variantId': integer(variant['variantId']),
                'quantity': quantity,
                'note': note.text.trim(),
              }),
              icon: const Icon(Icons.check),
              label: Text(
                'Thêm Vào Order · ${money(number(variant['price']) * quantity)}',
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 15),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
}

import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import 'order_page.dart';
import 'payment_page.dart';
import 'shared_widgets.dart';

class TablesPage extends StatefulWidget {
  const TablesPage({
    super.key,
    required this.api,
    this.initialFilter,
    this.actionPrompt,
    this.onClearPrompt,
  });

  final ApiClient api;
  final String? initialFilter;
  final String? actionPrompt;
  final VoidCallback? onClearPrompt;

  @override
  State<TablesPage> createState() => _TablesPageState();
}

class _TablesPageState extends State<TablesPage> {
  List<Map<String, dynamic>> tables = [];
  String filter = 'all';
  String? areaFilter;
  String? actionPrompt;
  final TextEditingController searchCtrl = TextEditingController();
  String searchQuery = '';
  bool loading = true;
  String? error;
  Timer? _realtimeTimer;

  @override
  void initState() {
    super.initState();
    if (widget.initialFilter != null) {
      filter = widget.initialFilter!;
    }
    actionPrompt = widget.actionPrompt;
    loadTables();
    _startRealtimePolling();
  }

  @override
  void didUpdateWidget(covariant TablesPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.initialFilter != null &&
        (widget.initialFilter != oldWidget.initialFilter ||
            widget.actionPrompt != oldWidget.actionPrompt)) {
      setState(() {
        filter = widget.initialFilter!;
        actionPrompt = widget.actionPrompt;
      });
    }
  }

  void clearFilterAndPrompt() {
    setState(() {
      filter = 'all';
      areaFilter = null;
      actionPrompt = null;
    });
    widget.onClearPrompt?.call();
  }

  @override
  void dispose() {
    _realtimeTimer?.cancel();
    searchCtrl.dispose();
    super.dispose();
  }

  // Polling tự động ngầm 3 giây 1 lần để cập nhật realtime
  void _startRealtimePolling() {
    _realtimeTimer?.cancel();
    _realtimeTimer = Timer.periodic(const Duration(seconds: 3), (_) {
      if (mounted) _pollSilently();
    });
  }

  Future<void> _pollSilently() async {
    try {
      final data = jsonMaps(await widget.api.get('/staff/orders/tables'));
      if (mounted) setState(() => tables = data);
    } catch (_) {
      // Chạy ngầm, không hiện lỗi làm phiền nhân viên
    }
  }

  Future<void> loadTables({bool silent = false}) async {
    if (!silent && tables.isEmpty) {
      setState(() {
        loading = true;
        error = null;
      });
    }
    try {
      final data = jsonMaps(await widget.api.get('/staff/orders/tables'));
      if (mounted) setState(() => tables = data);
    } on ApiException catch (exception) {
      if (mounted && tables.isEmpty) setState(() => error = exception.message);
    } finally {
      if (mounted && loading) setState(() => loading = false);
    }
  }

  Future<void> openTable(Map<String, dynamic> table) async {
    final status = text(table['tableStatus']);

    if (status == 'empty') {
      final open = await showModalBottomSheet<bool>(
        context: context,
        isScrollControlled: true,
        showDragHandle: true,
        builder: (_) => OpenOrderSheet(api: widget.api, table: table),
      );
      if (open == true) await loadTables(silent: true);
      return;
    }

    if (status == 'cleaning') {
      final confirm = await showDialog<bool>(
        context: context,
        builder: (dialog) => AlertDialog(
          title: const Text('Bàn đang dọn dẹp'),
          content: Text(
            'Bàn "${text(table['tableName'])}" đã được dọn dẹp sạch sẽ và sẵn sàng đón khách?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialog, false),
              child: const Text('Chưa xong'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialog, true),
              child: const Text('Đã dọn xong (Mở bàn trống)'),
            ),
          ],
        ),
      );
      if (confirm == true && mounted) {
        showMessage(context, 'Bàn đã sẵn sàng đón khách mới.');
        await loadTables(silent: true);
      }
      return;
    }

    if (status == 'reserved' && mounted) {
      await showReservationDetail(table);
      return;
    }

    if (status != 'serving') {
      showMessage(
        context,
        'Bàn này đang ${tableStatusText(status).toLowerCase()}.',
      );
      return;
    }

    // Nếu đang ở chế độ Chuyển / Gộp bàn thì mở ngay menu thao tác
    if (actionPrompt == 'transfer_merge') {
      await showTableActions(table);
      return;
    }

    // Nếu đang ở chế độ Thanh toán POS thì mở ngay trang thanh toán
    if (actionPrompt == 'payment') {
      final order = table['activeOrder'] is Map ? jsonMap(table['activeOrder']) : null;
      final orderId = order != null ? integer(order['orderId']) : 0;
      if (orderId > 0) {
        await Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => PaymentPage(api: widget.api, orderId: orderId),
          ),
        );
        if (mounted) await loadTables(silent: true);
        return;
      }
    }

    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => OrderPage(api: widget.api, table: table),
      ),
    );
    if (mounted) await loadTables(silent: true);
  }

  // Check-in đón khách trực tiếp từ sơ đồ bàn
  Future<void> checkInReservation(Map<String, dynamic> table) async {
    final reservation = table['reservation'] is Map ? jsonMap(table['reservation']) : null;
    int resId = integer(reservation?['reservationId']);

    if (resId <= 0) {
      // Nếu chưa có id trong model, thử truy vấn danh sách đặt bàn
      try {
        final res = jsonMap(await widget.api.get('/admin/reservations'));
        final list = jsonMaps(res['data']);
        final match = list.firstWhere(
          (r) => integer(r['table']?['tableId']) == integer(table['tableId']) && text(r['reservationStatus']) == 'confirmed',
          orElse: () => <String, dynamic>{},
        );
        resId = integer(match['reservationId']);
      } catch (_) {}
    }

    if (resId <= 0) {
      if (mounted) showMessage(context, 'Không tìm thấy thông tin đặt bàn cho bàn này.', isError: true);
      return;
    }

    try {
      await widget.api.post('/admin/reservations/$resId/check-in');
      if (mounted) showMessage(context, 'Đón khách vào bàn thành công!');
      await loadTables(silent: true);
      if (mounted) {
        // Tự động chuyển tới trang order
        await Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => OrderPage(api: widget.api, table: table),
          ),
        );
        if (mounted) await loadTables(silent: true);
      }
    } on ApiException catch (e) {
      if (mounted) showMessage(context, e.message, isError: true);
    }
  }

  // Xem chi tiết đặt bàn
  Future<void> showReservationDetail(Map<String, dynamic> table) async {
    final tableId = integer(table['tableId']);
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheet) => ReservedDetailSheet(
        table: table,
        onCheckIn: () {
          Navigator.pop(sheet);
          checkInReservation(table);
        },
        onRelease: () async {
          Navigator.pop(sheet);
          final confirm = await showDialog<bool>(
            context: context,
            builder: (dialog) => AlertDialog(
              title: const Text('Xác nhận giải phóng bàn'),
              content: Text(
                'Giải phóng bàn "${text(table['tableName'])}" về trạng thái bàn trống?',
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(dialog, false),
                  child: const Text('Hủy'),
                ),
                FilledButton(
                  style: FilledButton.styleFrom(backgroundColor: const Color(0xFFDC2626)),
                  onPressed: () => Navigator.pop(dialog, true),
                  child: const Text('Giải phóng bàn'),
                ),
              ],
            ),
          );
          if (confirm == true) {
            try {
              await widget.api.post('/staff/orders/tables/$tableId/release-reserved');
              if (mounted) showMessage(context, 'Đã giải phóng bàn về trạng thái trống.');
              await loadTables(silent: true);
            } on ApiException catch (e) {
              if (mounted) showMessage(context, e.message, isError: true);
            }
          }
        },
      ),
    );
  }

  // Tác vụ nhanh trên bàn phục vụ (Chuyển bàn, Ghép bàn, Gộp order, Thanh toán)
  Future<void> showTableActions(Map<String, dynamic> table) async {
    final status = text(table['tableStatus']);
    if (status != 'serving') {
      openTable(table);
      return;
    }

    final order = table['activeOrder'] is Map ? jsonMap(table['activeOrder']) : null;
    final orderId = order != null ? integer(order['orderId']) : 0;

    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheet) => TableActionSheet(
        table: table,
        onSwitch: () async {
          Navigator.pop(sheet);
          _handleSwitchTable(table);
        },
        onLink: () async {
          Navigator.pop(sheet);
          _handleLinkTable(table);
        },
        onMerge: () async {
          Navigator.pop(sheet);
          _handleMergeTable(table);
        },
        onPay: () {
          Navigator.pop(sheet);
          if (orderId > 0) {
            Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => PaymentPage(api: widget.api, orderId: orderId),
              ),
            ).then((_) => loadTables(silent: true));
          } else {
            showMessage(context, 'Bàn chưa có order.', isError: true);
          }
        },
        onCancel: () async {
          Navigator.pop(sheet);
          if (orderId <= 0) return;
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
            if (!mounted) return;
            final reason = await askCancellationReason(context, 'Lý do hủy order');
            if (reason == null || !mounted) return;
            try {
              await widget.api.post('/staff/orders/$orderId/cancel', data: {'reason': reason});
              if (mounted) showMessage(context, 'Đã hủy order thành công.');
              await loadTables(silent: true);
            } on ApiException catch (e) {
              if (mounted) showMessage(context, e.message, isError: true);
            }
          }
        },
      ),
    );
  }

  Future<void> _handleSwitchTable(Map<String, dynamic> currentTable) async {
    final tableId = integer(currentTable['tableId']);
    final currentArea = text(currentTable['areaName']).trim();
    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      final emptyTables = map.where((t) => text(t['tableStatus']) == 'empty' && integer(t['tableId']) != tableId).toList();
      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        showDragHandle: false,
        backgroundColor: Colors.transparent,
        builder: (_) => TablePicker(
          title: 'Đổi sang bàn trống khác',
          subtitle: 'Bàn hiện tại: ${text(currentTable['tableName'])}',
          currentTable: currentTable,
          areaName: currentArea,
          actionType: 'switch',
          tables: emptyTables,
        ),
      );
      if (target == null) return;
      final targetId = integer(target['tableId']);
      await widget.api.post(
        '/staff/orders/switch-table',
        data: {'fromTableId': tableId, 'toTableId': targetId},
      );
      if (mounted) showMessage(context, 'Đã chuyển bàn thành công sang "${text(target['tableName'])}".');
      await loadTables(silent: true);
    } on ApiException catch (e) {
      if (mounted) showMessage(context, e.message, isError: true);
    }
  }

  Future<void> _handleLinkTable(Map<String, dynamic> currentTable) async {
    final tableId = integer(currentTable['tableId']);
    final currentArea = text(currentTable['areaName']).trim();
    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      // Chỉ cho phép ghép bàn trống trong CÙNG KHU VỰC
      final emptyTables = map.where((t) {
        final isEmpty = text(t['tableStatus']) == 'empty';
        final isSameArea = text(t['areaName']).trim().toLowerCase() == currentArea.toLowerCase();
        final isDiff = integer(t['tableId']) != tableId;
        return isEmpty && isSameArea && isDiff;
      }).toList();

      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        showDragHandle: false,
        backgroundColor: Colors.transparent,
        builder: (_) => TablePicker(
          title: 'Ghép thêm bàn trống',
          subtitle: 'Bàn gốc: ${text(currentTable['tableName'])} · $currentArea',
          currentTable: currentTable,
          areaName: currentArea,
          actionType: 'link',
          tables: emptyTables,
        ),
      );
      if (target == null) return;
      final targetId = integer(target['tableId']);
      await widget.api.post(
        '/staff/orders/merge-tables',
        data: {
          'currentTableId': tableId,
          'targetTableId': targetId,
          'actionType': 'link_empty',
          'keepSourceTableLinked': true,
        },
      );
      if (mounted) showMessage(context, 'Đã ghép thêm bàn "${text(target['tableName'])}" thành công.');
      await loadTables(silent: true);
    } on ApiException catch (e) {
      if (mounted) showMessage(context, e.message, isError: true);
    }
  }

  Future<void> _handleMergeTable(Map<String, dynamic> currentTable) async {
    final tableId = integer(currentTable['tableId']);
    final currentArea = text(currentTable['areaName']).trim();
    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      // Chỉ cho phép gộp hai order bàn trong CÙNG KHU VỰC
      final servingTables = map.where((t) {
        final isServing = text(t['tableStatus']) == 'serving';
        final isSameArea = text(t['areaName']).trim().toLowerCase() == currentArea.toLowerCase();
        final isDiff = integer(t['tableId']) != tableId;
        return isServing && isSameArea && isDiff;
      }).toList();

      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        showDragHandle: false,
        backgroundColor: Colors.transparent,
        builder: (_) => TablePicker(
          title: 'Gộp hai order bàn',
          subtitle: 'Bàn gốc: ${text(currentTable['tableName'])} · $currentArea',
          currentTable: currentTable,
          areaName: currentArea,
          actionType: 'merge',
          tables: servingTables,
        ),
      );
      if (target == null) return;
      final targetId = integer(target['tableId']);
      await widget.api.post(
        '/staff/orders/merge-tables',
        data: {
          'currentTableId': tableId,
          'targetTableId': targetId,
          'actionType': 'merge_orders',
          'keepSourceTableLinked': true,
        },
      );
      if (mounted) showMessage(context, 'Đã gộp hai order bàn thành công.');
      await loadTables(silent: true);
    } on ApiException catch (e) {
      if (mounted) showMessage(context, e.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final areas = tables
        .map((t) => text(t['areaName']))
        .where((a) => a.isNotEmpty)
        .toSet()
        .toList();

    final serving = tables.where((t) => text(t['tableStatus']) == 'serving').length;
    final empty = tables.where((t) => text(t['tableStatus']) == 'empty').length;
    final reserved = tables.where((t) => text(t['tableStatus']) == 'reserved').length;
    final cleaning = tables.where((t) => text(t['tableStatus']) == 'cleaning').length;

    final query = searchQuery.trim().toLowerCase();
    final visible = tables.where((table) {
      final status = text(table['tableStatus']);
      if (filter != 'all') {
        if (filter == 'ready') {
          final order = table['activeOrder'] is Map ? jsonMap(table['activeOrder']) : null;
          final readyCount = order != null ? integer(order['readyCount']) : 0;
          if (readyCount <= 0) return false;
        } else if (status != filter) {
          return false;
        }
      }
      final area = text(table['areaName']);
      if (areaFilter != null && area != areaFilter) return false;
      if (query.isNotEmpty) {
        final name = text(table['tableName']).toLowerCase();
        final code = text(table['tableCode']).toLowerCase();
        final areaStr = text(table['areaName']).toLowerCase();
        final order = table['activeOrder'] is Map ? jsonMap(table['activeOrder']) : null;
        final orderId = order != null ? '${order['orderId']}' : '';
        final res = table['reservation'] is Map ? jsonMap(table['reservation']) : null;
        final custName = res != null ? text(res['customerName']).toLowerCase() : '';
        if (!name.contains(query) &&
            !code.contains(query) &&
            !areaStr.contains(query) &&
            !orderId.contains(query) &&
            !custName.contains(query)) {
          return false;
        }
      }
      return true;
    }).toList();

    return Scaffold(
      appBar: AppBar(
        toolbarHeight: 64,
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Sơ đồ bàn phục vụ', style: TextStyle(fontWeight: FontWeight.w900)),
            Text(
              'Tự động đồng bộ thời gian thực (Realtime)',
              style: TextStyle(fontSize: 12, color: Color(0xFF6F7E78), fontWeight: FontWeight.normal),
            ),
          ],
        ),
        // ĐÃ BỎ NÚT LOAD LẠI THEO YÊU CẦU NGƯỜI DÙNG
        actions: const [],
      ),
      body: RefreshIndicator(
        onRefresh: loadTables,
        child: loading
            ? const Center(child: CircularProgressIndicator())
            : error != null
            ? RetryState(message: error!, onRetry: loadTables)
            : ListView(
                padding: const EdgeInsets.fromLTRB(14, 8, 14, 24),
                children: [
                  _buildActionPromptBanner(isDark),

                  // 1. Thẻ Thống kê nhanh gọn gàng (Realtime Auto)
                  Container(
                    margin: const EdgeInsets.only(bottom: 8),
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    decoration: BoxDecoration(
                      color: isDark ? darkSurface : Colors.white,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: isDark ? 0.2 : 0.03),
                          blurRadius: 4,
                          offset: const Offset(0, 1),
                        ),
                      ],
                    ),
                    child: Row(
                      children: [
                        Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(
                              'Tổng số ${tables.length} bàn',
                              style: TextStyle(
                                fontSize: 12.5,
                                fontWeight: FontWeight.w900,
                                color: isDark ? gold : brand,
                              ),
                            ),
                            const SizedBox(width: 6),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1.5),
                              decoration: BoxDecoration(
                                color: isDark
                                    ? const Color(0xFF064E3B).withValues(alpha: 0.4)
                                    : const Color(0xFFE8F5E9),
                                borderRadius: BorderRadius.circular(4),
                              ),
                              child: const Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Text('● ', style: TextStyle(fontSize: 7.5, color: Color(0xFF2E7D32))),
                                  Text(
                                    'REALTIME AUTO',
                                    style: TextStyle(fontSize: 8, fontWeight: FontWeight.w900, color: Color(0xFF2E7D32)),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                        const Spacer(),
                        Wrap(
                          spacing: 8,
                          crossAxisAlignment: WrapCrossAlignment.center,
                          children: [
                            _StatItem(label: 'Trống', count: empty, color: const Color(0xFF16A34A)),
                            _StatItem(label: 'Phục vụ', count: serving, color: const Color(0xFFD97706)),
                            _StatItem(label: 'Đặt trước', count: reserved, color: const Color(0xFF2563EB)),
                            if (cleaning > 0)
                              _StatItem(label: 'Dọn', count: cleaning, color: const Color(0xFF9333EA)),
                          ],
                        ),
                      ],
                    ),
                  ),

                  // 2. Hàng tìm kiếm và Nút lọc gộp (Trạng thái + Khu vực)
                  Row(
                    children: [
                      Expanded(
                        child: Container(
                          height: 38,
                          decoration: BoxDecoration(
                            color: isDark ? darkSurface : Colors.white,
                            borderRadius: BorderRadius.circular(9),
                            border: Border.all(
                              color: isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
                            ),
                          ),
                          child: TextField(
                            controller: searchCtrl,
                            onChanged: (val) => setState(() => searchQuery = val),
                            style: TextStyle(
                              fontSize: 12.5,
                              color: isDark ? Colors.white : const Color(0xFF0F172A),
                            ),
                            decoration: InputDecoration(
                              hintText: 'Tìm theo tên bàn, số bàn, khu vực...',
                              hintStyle: TextStyle(
                                fontSize: 12,
                                color: isDark ? darkTextSecondary : const Color(0xFF94A3B8),
                              ),
                              prefixIcon: Icon(
                                Icons.search_rounded,
                                size: 18,
                                color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                              ),
                              prefixIconConstraints: const BoxConstraints(minWidth: 36, minHeight: 36),
                              suffixIcon: searchQuery.isNotEmpty
                                  ? IconButton(
                                      padding: EdgeInsets.zero,
                                      icon: const Icon(Icons.cancel_rounded, size: 16),
                                      onPressed: () {
                                        searchCtrl.clear();
                                        setState(() => searchQuery = '');
                                      },
                                    )
                                  : null,
                              border: InputBorder.none,
                              isDense: true,
                              contentPadding: const EdgeInsets.symmetric(vertical: 9, horizontal: 4),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),

                      // Nút Lọc gộp duy nhất (Trạng thái + Khu vực)
                      _buildFilterTriggerButton(
                        isDark: isDark,
                        areas: areas,
                        emptyCount: empty,
                        servingCount: serving,
                        reservedCount: reserved,
                        cleaningCount: cleaning,
                      ),
                    ],
                  ),

                  // Chỉ hiện thanh trạng thái lọc khi người dùng đang áp dụng bộ lọc
                  if (filter != 'all' || areaFilter != null) ...[
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        Text(
                          'Đang lọc: ',
                          style: TextStyle(
                            fontSize: 11,
                            color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        if (filter != 'all') ...[
                          _buildActiveBadge(
                            label: _statusFilterLabel(filter),
                            onClear: () => setState(() => filter = 'all'),
                            isDark: isDark,
                          ),
                          const SizedBox(width: 5),
                        ],
                        if (areaFilter != null) ...[
                          _buildActiveBadge(
                            label: '📍 $areaFilter',
                            onClear: () => setState(() => areaFilter = null),
                            isDark: isDark,
                          ),
                        ],
                        const Spacer(),
                        InkWell(
                          onTap: () => setState(() {
                            filter = 'all';
                            areaFilter = null;
                          }),
                          child: const Text(
                            'Xóa lọc',
                            style: TextStyle(
                              fontSize: 11,
                              color: Color(0xFFDC2626),
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],

                  const SizedBox(height: 10),

                  // Lưới bàn Responsive (Điện thoại: 2 cột gọn chuẩn POS, Tablet: 3-4 cột)
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final columns = constraints.maxWidth >= 1000
                          ? 4
                          : constraints.maxWidth >= 700
                          ? 3
                          : 2;

                      final aspectRatio = constraints.maxWidth >= 700 ? 1.08 : 0.96;

                      if (visible.isEmpty) {
                        return Container(
                          padding: const EdgeInsets.symmetric(vertical: 40),
                          alignment: Alignment.center,
                          child: Column(
                            children: [
                              Icon(
                                Icons.table_restaurant_outlined,
                                size: 44,
                                color: isDark ? const Color(0xFF475569) : const Color(0xFF94A3B8),
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'Không tìm thấy bàn nào phù hợp.',
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w600,
                                  color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                                ),
                              ),
                              if (searchQuery.isNotEmpty || filter != 'all' || areaFilter != null) ...[
                                const SizedBox(height: 8),
                                TextButton.icon(
                                  onPressed: () {
                                    searchCtrl.clear();
                                    setState(() {
                                      searchQuery = '';
                                      filter = 'all';
                                      areaFilter = null;
                                    });
                                  },
                                  icon: const Icon(Icons.refresh_rounded, size: 16),
                                  label: const Text('Xóa tất cả bộ lọc', style: TextStyle(fontSize: 12)),
                                ),
                              ],
                            ],
                          ),
                        );
                      }

                      return GridView.builder(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: visible.length,
                        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: columns,
                          mainAxisSpacing: 10,
                          crossAxisSpacing: 10,
                          childAspectRatio: aspectRatio,
                        ),
                        itemBuilder: (_, itemIndex) => TableCard(
                          table: visible[itemIndex],
                          onTap: () => openTable(visible[itemIndex]),
                          onAction: () => showTableActions(visible[itemIndex]),
                          onTransferMerge: () => showTableActions(visible[itemIndex]),
                          onCheckIn: () => checkInReservation(visible[itemIndex]),
                          onViewReservation: () => showReservationDetail(visible[itemIndex]),
                        ),
                      );
                    },
                  ),
                ],
              ),
      ),
    );
  }

  Widget _buildFilterTriggerButton({
    required bool isDark,
    required List<String> areas,
    required int emptyCount,
    required int servingCount,
    required int reservedCount,
    required int cleaningCount,
  }) {
    final isFiltered = filter != 'all' || areaFilter != null;
    final activeCount = (filter != 'all' ? 1 : 0) + (areaFilter != null ? 1 : 0);

    return InkWell(
      onTap: () => _openFilterSheet(
        isDark: isDark,
        areas: areas,
        emptyCount: emptyCount,
        servingCount: servingCount,
        reservedCount: reservedCount,
        cleaningCount: cleaningCount,
      ),
      borderRadius: BorderRadius.circular(9),
      child: Container(
        height: 38,
        padding: const EdgeInsets.symmetric(horizontal: 10),
        decoration: BoxDecoration(
          color: isFiltered
              ? (isDark ? gold.withValues(alpha: 0.2) : const Color(0xFFFEF3C7))
              : (isDark ? darkSurface : Colors.white),
          borderRadius: BorderRadius.circular(9),
          border: Border.all(
            color: isFiltered
                ? (isDark ? gold : const Color(0xFFD97706))
                : (isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1)),
            width: isFiltered ? 1.5 : 1,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.tune_rounded,
              size: 16,
              color: isFiltered
                  ? (isDark ? gold : const Color(0xFFB45309))
                  : (isDark ? darkTextSecondary : const Color(0xFF64748B)),
            ),
            const SizedBox(width: 5),
            Text(
              isFiltered ? 'Lọc ($activeCount)' : 'Bộ lọc',
              style: TextStyle(
                fontSize: 12,
                fontWeight: isFiltered ? FontWeight.w800 : FontWeight.w600,
                color: isFiltered
                    ? (isDark ? gold : const Color(0xFFB45309))
                    : (isDark ? darkTextPrimary : const Color(0xFF334155)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openFilterSheet({
    required bool isDark,
    required List<String> areas,
    required int emptyCount,
    required int servingCount,
    required int reservedCount,
    required int cleaningCount,
  }) async {
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            final isFiltered = filter != 'all' || areaFilter != null;

            return SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 4, 16, 20),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Header
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.tune_rounded, size: 20, color: Color(0xFF2563EB)),
                            const SizedBox(width: 8),
                            Text(
                              'Bộ lọc trạng thái & Khu vực',
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w800,
                                color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                              ),
                            ),
                          ],
                        ),
                        if (isFiltered)
                          TextButton(
                            onPressed: () {
                              setSheetState(() {
                                filter = 'all';
                                areaFilter = null;
                              });
                              setState(() {});
                            },
                            child: const Text(
                              'Đặt lại',
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w700,
                                color: Color(0xFFDC2626),
                              ),
                            ),
                          ),
                      ],
                    ),

                    const SizedBox(height: 14),

                    // Section 1: Trạng thái bàn
                    Text(
                      'TRẠNG THÁI BÀN',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w800,
                        color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                        letterSpacing: 0.5,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        _buildSheetChip(
                          label: 'Tất cả (${tables.length})',
                          isSelected: filter == 'all',
                          activeColor: isDark ? gold : brand,
                          isDark: isDark,
                          onTap: () {
                            setSheetState(() => filter = 'all');
                            setState(() {});
                          },
                        ),
                        _buildSheetChip(
                          label: '🟢 Trống ($emptyCount)',
                          isSelected: filter == 'empty',
                          activeColor: const Color(0xFF16A34A),
                          isDark: isDark,
                          onTap: () {
                            setSheetState(() => filter = 'empty');
                            setState(() {});
                          },
                        ),
                        _buildSheetChip(
                          label: '🟠 Phục vụ ($servingCount)',
                          isSelected: filter == 'serving',
                          activeColor: const Color(0xFFD97706),
                          isDark: isDark,
                          onTap: () {
                            setSheetState(() => filter = 'serving');
                            setState(() {});
                          },
                        ),
                        _buildSheetChip(
                          label: '🔵 Đặt trước ($reservedCount)',
                          isSelected: filter == 'reserved',
                          activeColor: const Color(0xFF2563EB),
                          isDark: isDark,
                          onTap: () {
                            setSheetState(() => filter = 'reserved');
                            setState(() {});
                          },
                        ),
                        if (cleaningCount > 0)
                          _buildSheetChip(
                            label: '🟣 Đang dọn ($cleaningCount)',
                            isSelected: filter == 'cleaning',
                            activeColor: const Color(0xFF9333EA),
                            isDark: isDark,
                            onTap: () {
                              setSheetState(() => filter = 'cleaning');
                              setState(() {});
                            },
                          ),
                      ],
                    ),

                    if (areas.isNotEmpty) ...[
                      const SizedBox(height: 18),
                      // Section 2: Khu vực bàn
                      Text(
                        'KHU VỰC BÀN',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w800,
                          color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                          letterSpacing: 0.5,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          _buildSheetChip(
                            label: 'Tất cả khu vực',
                            isSelected: areaFilter == null,
                            activeColor: isDark ? gold : brand,
                            isDark: isDark,
                            onTap: () {
                              setSheetState(() => areaFilter = null);
                              setState(() {});
                            },
                          ),
                          ...areas.map((area) {
                            final count = tables.where((t) => text(t['areaName']) == area).length;
                            return _buildSheetChip(
                              label: '📍 $area ($count)',
                              isSelected: areaFilter == area,
                              activeColor: const Color(0xFF2563EB),
                              isDark: isDark,
                              onTap: () {
                                setSheetState(() => areaFilter = area);
                                setState(() {});
                              },
                            );
                          }),
                        ],
                      ),
                    ],

                    const SizedBox(height: 20),

                    // Nút Áp Dụng
                    FilledButton(
                      style: FilledButton.styleFrom(
                        backgroundColor: brand,
                        minimumSize: const Size.fromHeight(44),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      onPressed: () => Navigator.pop(ctx),
                      child: const Text(
                        'Áp Dụng Bộ Lọc',
                        style: TextStyle(fontSize: 14, fontWeight: FontWeight.w800, color: Colors.white),
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildActiveBadge({
    required String label,
    required VoidCallback onClear,
    required bool isDark,
  }) {
    return Container(
      padding: const EdgeInsets.fromLTRB(8, 2, 4, 2),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: 10.5,
              fontWeight: FontWeight.w600,
              color: isDark ? darkTextPrimary : const Color(0xFF334155),
            ),
          ),
          const SizedBox(width: 2),
          InkWell(
            onTap: onClear,
            child: const Icon(Icons.close_rounded, size: 14, color: Color(0xFF94A3B8)),
          ),
        ],
      ),
    );
  }

  Widget _buildSheetChip({
    required String label,
    required bool isSelected,
    required Color activeColor,
    required bool isDark,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          color: isSelected
              ? activeColor
              : (isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: isSelected
                ? activeColor
                : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
            width: isSelected ? 1.5 : 1,
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
            color: isSelected
                ? Colors.white
                : (isDark ? darkTextPrimary : const Color(0xFF334155)),
          ),
        ),
      ),
    );
  }

  Widget _buildActionPromptBanner(bool isDark) {
    if (actionPrompt == null) return const SizedBox.shrink();

    final Color bg;
    final Color border;
    final Color textColor;
    final IconData icon;
    final String title;
    final String subtitle;

    switch (actionPrompt) {
      case 'open_table':
        bg = isDark ? const Color(0xFF064E3B) : const Color(0xFFDCFCE7);
        border = const Color(0xFF86EFAC);
        textColor = const Color(0xFF15803D);
        icon = Icons.add_circle_outline_rounded;
        title = 'Chế độ tạo order mới';
        subtitle = 'Chỉ hiển thị bàn trống. Chạm vào bàn để mở order ngay.';
        break;
      case 'add_order':
        bg = isDark ? const Color(0xFF78350F) : const Color(0xFFFEF3C7);
        border = const Color(0xFFFDE68A);
        textColor = const Color(0xFFB45309);
        icon = Icons.restaurant_menu_rounded;
        title = 'Chế độ gọi món / thêm món';
        subtitle = 'Chỉ hiển thị bàn đang phục vụ. Chạm vào bàn để gọi thêm món.';
        break;
      case 'transfer_merge':
        bg = isDark ? const Color(0xFF581C87) : const Color(0xFFF3E8FF);
        border = const Color(0xFFD8B4FE);
        textColor = const Color(0xFF7E22CE);
        icon = Icons.swap_horiz_rounded;
        title = 'Chế độ chuyển / gộp bàn';
        subtitle = 'Chạm vào bàn đang ăn để thực hiện đổi bàn hoặc ghép bàn.';
        break;
      case 'payment':
        bg = isDark ? const Color(0xFF14532D) : const Color(0xFFECFDF5);
        border = const Color(0xFFA7F3D0);
        textColor = const Color(0xFF047857);
        icon = Icons.payments_rounded;
        title = 'Chế độ thanh toán POS';
        subtitle = 'Chạm vào bàn đang ăn để vào màn hình thanh toán hóa đơn.';
        break;
      case 'kitchen_ready':
        bg = isDark ? const Color(0xFF1E3A8A) : const Color(0xFFEFF6FF);
        border = const Color(0xFFBFDBFE);
        textColor = const Color(0xFF1D4ED8);
        icon = Icons.room_service_rounded;
        title = 'Món bếp đã nấu xong';
        subtitle = 'Chỉ hiển thị các bàn có món sẵn sàng để bưng ra phục vụ.';
        break;
      default:
        return const SizedBox.shrink();
    }

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: border),
      ),
      child: Row(
        children: [
          Icon(icon, color: textColor, size: 22),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w900,
                    color: textColor,
                  ),
                ),
                Text(
                  subtitle,
                  style: TextStyle(
                    fontSize: 11,
                    color: isDark ? Colors.white70 : const Color(0xFF475569),
                  ),
                ),
              ],
            ),
          ),
          TextButton(
            style: TextButton.styleFrom(
              visualDensity: VisualDensity.compact,
              padding: const EdgeInsets.symmetric(horizontal: 8),
            ),
            onPressed: clearFilterAndPrompt,
            child: Text(
              'Xem tất cả ✕',
              style: TextStyle(
                fontSize: 11.5,
                fontWeight: FontWeight.w800,
                color: textColor,
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _statusFilterLabel(String id) {
    switch (id) {
      case 'ready':
        return '🔔 Bếp xong';
      case 'empty':
        return '🟢 Trống';
      case 'serving':
        return '🟠 Phục vụ';
      case 'reserved':
        return '🔵 Đặt trước';
      case 'cleaning':
        return '🟣 Đang dọn';
      default:
        return id;
    }
  }
}

class _StatItem extends StatelessWidget {
  const _StatItem({required this.label, required this.count, required this.color});
  final String label;
  final int count;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(width: 8, height: 8, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
        const SizedBox(width: 4),
        Text('$label: ', style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78))),
        Text('$count', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: color)),
      ],
    );
  }
}

class OpenOrderSheet extends StatefulWidget {
  const OpenOrderSheet({super.key, required this.api, required this.table});
  final ApiClient api;
  final Map<String, dynamic> table;

  @override
  State<OpenOrderSheet> createState() => _OpenOrderSheetState();
}

class _OpenOrderSheetState extends State<OpenOrderSheet> {
  final note = TextEditingController();
  bool saving = false;
  String? error;

  @override
  void dispose() {
    note.dispose();
    super.dispose();
  }

  Future<void> create() async {
    setState(() {
      saving = true;
      error = null;
    });
    try {
      await widget.api.post(
        '/staff/orders/create',
        data: {
          'tableId': integer(widget.table['tableId']),
          'note': note.text.trim(),
          'initialItems': [],
        },
      );
      if (mounted) Navigator.of(context).pop(true);
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(20, 6, 20, 24 + MediaQuery.viewInsetsOf(context).bottom),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(color: brand.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
                  child: const Icon(Icons.add_shopping_cart, color: brand, size: 22),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Mở Order · ${text(widget.table['tableName'])}',
                        style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w900, color: brand),
                      ),
                      Text(
                        '${text(widget.table['areaName'], 'Khu chung')} · Sức chứa ${integer(widget.table['capacity'])} người',
                        style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78)),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 18),
            TextField(
              controller: note,
              maxLines: 2,
              decoration: const InputDecoration(
                labelText: 'Ghi chú order (không bắt buộc)',
                hintText: 'Ví dụ: Khách VIP, bàn sinh nhật...',
                prefixIcon: Icon(Icons.note_alt_outlined),
              ),
            ),
            if (error != null)
              Padding(
                padding: const EdgeInsets.only(top: 10),
                child: Text(
                  error!,
                  style: const TextStyle(color: Color(0xFFC62828), fontSize: 13, fontWeight: FontWeight.w600),
                ),
              ),
            const SizedBox(height: 20),
            FilledButton.icon(
              style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
              onPressed: saving ? null : create,
              icon: saving
                  ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.check),
              label: Text(
                saving ? 'Đang mở order...' : 'Xác Nhận Mở Bàn Phục Vụ',
                style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 15),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

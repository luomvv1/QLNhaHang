import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import 'order_page.dart';
import 'payment_page.dart';
import 'shared_widgets.dart';

class TablesPage extends StatefulWidget {
  const TablesPage({super.key, required this.api});
  final ApiClient api;

  @override
  State<TablesPage> createState() => _TablesPageState();
}

class _TablesPageState extends State<TablesPage> {
  List<Map<String, dynamic>> tables = [];
  String filter = 'all';
  String? areaFilter;
  bool loading = true;
  String? error;
  Timer? _realtimeTimer;

  @override
  void initState() {
    super.initState();
    loadTables();
    _startRealtimePolling();
  }

  @override
  void dispose() {
    _realtimeTimer?.cancel();
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

    final tableId = integer(table['tableId']);
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
          _handleSwitchTable(tableId);
        },
        onLink: () async {
          Navigator.pop(sheet);
          _handleLinkTable(tableId);
        },
        onMerge: () async {
          Navigator.pop(sheet);
          _handleMergeTable(tableId);
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
            try {
              await widget.api.post('/staff/orders/$orderId/cancel');
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

  Future<void> _handleSwitchTable(int tableId) async {
    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      final emptyTables = map.where((t) => text(t['tableStatus']) == 'empty').toList();
      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        showDragHandle: true,
        builder: (_) => TablePicker(
          title: 'Chọn bàn trống để chuyển đến',
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

  Future<void> _handleLinkTable(int tableId) async {
    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      final emptyTables = map.where((t) => text(t['tableStatus']) == 'empty').toList();
      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        showDragHandle: true,
        builder: (_) => TablePicker(
          title: 'Chọn bàn trống để ghép thêm',
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

  Future<void> _handleMergeTable(int tableId) async {
    try {
      final map = jsonMaps(await widget.api.get('/staff/orders/tables'));
      final servingTables = map.where(
        (t) => text(t['tableStatus']) == 'serving' && integer(t['tableId']) != tableId,
      ).toList();
      if (!mounted) return;
      final target = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        showDragHandle: true,
        builder: (_) => TablePicker(
          title: 'Chọn order bàn cần gộp chung',
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

    var visible = tables;
    if (filter != 'all') {
      visible = visible
          .where((table) => text(table['tableStatus']) == filter)
          .toList();
    }
    if (areaFilter != null) {
      visible = visible
          .where((table) => text(table['areaName']) == areaFilter)
          .toList();
    }

    final serving = tables
        .where((table) => text(table['tableStatus']) == 'serving')
        .length;
    final empty = tables
        .where((table) => text(table['tableStatus']) == 'empty')
        .length;
    final reserved = tables
        .where((table) => text(table['tableStatus']) == 'reserved')
        .length;
    final cleaning = tables
        .where((table) => text(table['tableStatus']) == 'cleaning')
        .length;

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
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                children: [
                  // Thẻ Thống kê nhanh (Đã tinh chỉnh gọn gàng, hợp lý, không quá bự)
                  Container(
                    margin: const EdgeInsets.only(bottom: 10),
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 9),
                    decoration: BoxDecoration(
                      color: isDark ? darkSurface : Colors.white,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                        width: 1,
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
                        Text(
                          'Tổng số ${tables.length} bàn',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w900,
                            color: isDark ? gold : brand,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                          decoration: BoxDecoration(
                            color: isDark
                                ? const Color(0xFF064E3B).withValues(alpha: 0.4)
                                : const Color(0xFFE8F5E9),
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: const Text(
                            'REALTIME AUTO',
                            style: TextStyle(fontSize: 8.5, fontWeight: FontWeight.w900, color: Color(0xFF2E7D32)),
                          ),
                        ),
                        const Spacer(),
                        Wrap(
                          spacing: 12,
                          crossAxisAlignment: WrapCrossAlignment.center,
                          children: [
                            _StatItem(label: 'Trống', count: empty, color: const Color(0xFF2E7D32)),
                            _StatItem(label: 'Phục vụ', count: serving, color: const Color(0xFFE65100)),
                            _StatItem(label: 'Đặt trước', count: reserved, color: const Color(0xFF1565C0)),
                            if (cleaning > 0)
                              _StatItem(label: 'Đang dọn', count: cleaning, color: const Color(0xFF6A1B9A)),
                          ],
                        ),
                      ],
                    ),
                  ),

                  // Bộ lọc dạng Dropdown sổ xuống (Trạng thái bàn & Khu vực) chuẩn như yêu cầu
                  Row(
                    children: [
                      // Dropdown 1: Trạng thái bàn
                      Expanded(
                        child: Container(
                          height: 40,
                          padding: const EdgeInsets.symmetric(horizontal: 10),
                          decoration: BoxDecoration(
                            color: isDark ? darkSurface : Colors.white,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: filter != 'all'
                                  ? (isDark ? gold : brand)
                                  : (isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1)),
                              width: filter != 'all' ? 1.5 : 1,
                            ),
                          ),
                          child: DropdownButtonHideUnderline(
                            child: DropdownButton<String>(
                              value: filter,
                              isExpanded: true,
                              icon: const Icon(Icons.keyboard_arrow_down, size: 20),
                              dropdownColor: isDark ? darkSurface : Colors.white,
                              borderRadius: BorderRadius.circular(8),
                              elevation: 4,
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: isDark ? Colors.white : const Color(0xFF1E293B),
                              ),
                              onChanged: (val) {
                                if (val != null) setState(() => filter = val);
                              },
                              selectedItemBuilder: (BuildContext context) {
                                return [
                                  Text(
                                    'Tất cả trạng thái (${tables.length})',
                                    style: TextStyle(
                                      fontWeight: FontWeight.w700,
                                      fontSize: 12.5,
                                      color: isDark ? Colors.white : const Color(0xFF1E293B),
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  Text(
                                    '🟢 Trống ($empty)',
                                    style: const TextStyle(fontWeight: FontWeight.w700, color: Color(0xFF16A34A), fontSize: 12.5),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  Text(
                                    '🟠 Phục vụ ($serving)',
                                    style: const TextStyle(fontWeight: FontWeight.w700, color: Color(0xFFD97706), fontSize: 12.5),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  Text(
                                    '🔵 Đặt trước ($reserved)',
                                    style: const TextStyle(fontWeight: FontWeight.w700, color: Color(0xFF2563EB), fontSize: 12.5),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  if (cleaning > 0)
                                    Text(
                                      '🟣 Đang dọn ($cleaning)',
                                      style: const TextStyle(fontWeight: FontWeight.w700, color: Color(0xFF7C3AED), fontSize: 12.5),
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                ];
                              },
                              items: [
                                DropdownMenuItem(
                                  value: 'all',
                                  child: Container(
                                    decoration: filter == 'all'
                                        ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                        : null,
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'Tất cả trạng thái',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            color: filter == 'all' ? Colors.white : const Color(0xFF1E293B),
                                          ),
                                        ),
                                        Text(
                                          '${tables.length}',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            color: filter == 'all' ? Colors.white70 : const Color(0xFF64748B),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                                DropdownMenuItem(
                                  value: 'empty',
                                  child: Container(
                                    decoration: filter == 'empty'
                                        ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                        : null,
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'Bàn trống',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w600,
                                            color: filter == 'empty' ? Colors.white : const Color(0xFF16A34A),
                                          ),
                                        ),
                                        Text(
                                          '$empty',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            color: filter == 'empty' ? Colors.white70 : const Color(0xFF16A34A),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                                DropdownMenuItem(
                                  value: 'serving',
                                  child: Container(
                                    decoration: filter == 'serving'
                                        ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                        : null,
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'Đang phục vụ',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w600,
                                            color: filter == 'serving' ? Colors.white : const Color(0xFFD97706),
                                          ),
                                        ),
                                        Text(
                                          '$serving',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            color: filter == 'serving' ? Colors.white70 : const Color(0xFFD97706),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                                DropdownMenuItem(
                                  value: 'reserved',
                                  child: Container(
                                    decoration: filter == 'reserved'
                                        ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                        : null,
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    child: Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Text(
                                          'Đã đặt trước',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w600,
                                            color: filter == 'reserved' ? Colors.white : const Color(0xFF2563EB),
                                          ),
                                        ),
                                        Text(
                                          '$reserved',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            color: filter == 'reserved' ? Colors.white70 : const Color(0xFF2563EB),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                                if (cleaning > 0)
                                  DropdownMenuItem(
                                    value: 'cleaning',
                                    child: Container(
                                      decoration: filter == 'cleaning'
                                          ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                          : null,
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                      child: Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Text(
                                            'Đang dọn dẹp',
                                            style: TextStyle(
                                              fontWeight: FontWeight.w600,
                                              color: filter == 'cleaning' ? Colors.white : const Color(0xFF7C3AED),
                                            ),
                                          ),
                                          Text(
                                            '$cleaning',
                                            style: TextStyle(
                                              fontWeight: FontWeight.w700,
                                              color: filter == 'cleaning' ? Colors.white70 : const Color(0xFF7C3AED),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                              ],
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),

                      // Dropdown 2: Khu vực
                      Expanded(
                        child: Container(
                          height: 40,
                          padding: const EdgeInsets.symmetric(horizontal: 10),
                          decoration: BoxDecoration(
                            color: isDark ? darkSurface : Colors.white,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(
                              color: areaFilter != null
                                  ? (isDark ? gold : brand)
                                  : (isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1)),
                              width: areaFilter != null ? 1.5 : 1,
                            ),
                          ),
                          child: DropdownButtonHideUnderline(
                            child: DropdownButton<String?>(
                              value: areaFilter,
                              isExpanded: true,
                              icon: const Icon(Icons.keyboard_arrow_down, size: 20),
                              dropdownColor: isDark ? darkSurface : Colors.white,
                              borderRadius: BorderRadius.circular(8),
                              elevation: 4,
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: isDark ? Colors.white : const Color(0xFF1E293B),
                              ),
                              onChanged: (val) {
                                setState(() => areaFilter = val);
                              },
                              selectedItemBuilder: (BuildContext context) {
                                return [
                                  Text(
                                    'Tất cả khu vực',
                                    style: TextStyle(
                                      fontWeight: FontWeight.w700,
                                      fontSize: 12.5,
                                      color: isDark ? Colors.white : const Color(0xFF1E293B),
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  ...areas.map((area) => Text(
                                    '📍 $area',
                                    style: TextStyle(
                                      fontWeight: FontWeight.w700,
                                      fontSize: 12.5,
                                      color: isDark ? Colors.white : const Color(0xFF1E293B),
                                    ),
                                    overflow: TextOverflow.ellipsis,
                                  )),
                                ];
                              },
                              items: [
                                DropdownMenuItem<String?>(
                                  value: null,
                                  child: Container(
                                    decoration: areaFilter == null
                                        ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                        : null,
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    child: Row(
                                      children: [
                                        Text(
                                          'Tất cả khu vực',
                                          style: TextStyle(
                                            fontWeight: FontWeight.w700,
                                            color: areaFilter == null ? Colors.white : const Color(0xFF1E293B),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                                ...areas.map((area) => DropdownMenuItem<String?>(
                                  value: area,
                                  child: Container(
                                    decoration: areaFilter == area
                                        ? BoxDecoration(color: const Color(0xFF475569), borderRadius: BorderRadius.circular(4))
                                        : null,
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                                    child: Row(
                                      children: [
                                        Text(
                                          area,
                                          style: TextStyle(
                                            fontWeight: FontWeight.w600,
                                            color: areaFilter == area ? Colors.white : const Color(0xFF1E293B),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                )),
                              ],
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 14),

                  // Lưới bàn Responsive (Điện thoại: 2 cột, Tablet: 3-4 cột)
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final columns = constraints.maxWidth >= 1000
                          ? 4
                          : constraints.maxWidth >= 700
                          ? 3
                          : constraints.maxWidth >= 500
                          ? 2
                          : 2;

                      if (visible.isEmpty) {
                        return const EmptyState(message: 'Không có bàn nào trong bộ lọc này.');
                      }

                      return GridView.builder(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: visible.length,
                        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: columns,
                          mainAxisSpacing: 12,
                          crossAxisSpacing: 12,
                          childAspectRatio: 0.68,
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

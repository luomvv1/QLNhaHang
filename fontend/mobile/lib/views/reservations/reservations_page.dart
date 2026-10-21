import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import '../staff/order_page.dart';
import '../staff/shared_widgets.dart';

class ReservationsPage extends StatefulWidget {
  const ReservationsPage({super.key, required this.api});

  final ApiClient api;

  @override
  State<ReservationsPage> createState() => _ReservationsPageState();
}

class _ReservationsPageState extends State<ReservationsPage> {
  List<Map<String, dynamic>> reservations = [];
  Map<String, dynamic>? stats;
  String status = 'all';
  String dateScope = 'all'; // 'all', 'today', 'custom'
  DateTime? selectedDate;
  final searchController = TextEditingController();
  bool loading = true;
  String? error;
  Timer? _realtimeTimer;

  @override
  void initState() {
    super.initState();
    load();
    _startRealtimePolling();
  }

  @override
  void dispose() {
    _realtimeTimer?.cancel();
    searchController.dispose();
    super.dispose();
  }

  void _startRealtimePolling() {
    _realtimeTimer?.cancel();
    _realtimeTimer = Timer.periodic(const Duration(seconds: 4), (_) {
      if (mounted) _pollSilently();
    });
  }

  Map<String, String> _buildQueryParams() {
    final params = <String, String>{};
    if (status != 'all') {
      params['status'] = status;
    }
    if (dateScope == 'today') {
      params['date'] = DateTime.now().toIso8601String().substring(0, 10);
    } else if (dateScope == 'custom' && selectedDate != null) {
      params['date'] = selectedDate!.toIso8601String().substring(0, 10);
    }
    final kw = searchController.text.trim();
    if (kw.isNotEmpty) {
      params['search'] = kw;
    }
    return params;
  }

  Future<void> _pollSilently() async {
    try {
      final result = jsonMap(
        await widget.api.get('/admin/reservations', query: _buildQueryParams()),
      );
      if (mounted) {
        setState(() {
          reservations = jsonMaps(result['data']);
          if (result['stats'] is Map) {
            stats = jsonMap(result['stats']);
          }
        });
      }
    } catch (_) {}
  }

  Future<void> load({bool silent = false}) async {
    if (!silent && reservations.isEmpty) {
      setState(() {
        loading = true;
        error = null;
      });
    }
    try {
      final result = jsonMap(
        await widget.api.get('/admin/reservations', query: _buildQueryParams()),
      );
      if (mounted) {
        setState(() {
          reservations = jsonMaps(result['data']);
          if (result['stats'] is Map) {
            stats = jsonMap(result['stats']);
          }
        });
      }
    } on ApiException catch (exception) {
      if (mounted && reservations.isEmpty) {
        setState(() => error = exception.message);
      }
    } finally {
      if (mounted && loading) setState(() => loading = false);
    }
  }

  Future<void> action(int id, String path, String success) async {
    try {
      await widget.api.post('/admin/reservations/$id/$path');
      if (mounted) showMessage(context, success);
      await load(silent: true);
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  Future<void> assignTable(Map<String, dynamic> reservation) async {
    try {
      final result = jsonMap(
        await widget.api.get('/admin/reservations/tables'),
      );
      final tables = jsonMaps(result['data'])
          .where(
            (table) =>
                text(table['tableStatus']) == 'empty' &&
                integer(table['capacity']) >= integer(reservation['guestCount']),
          )
          .toList();
      if (!mounted) return;
      final selected = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        showDragHandle: true,
        builder: (_) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Chọn bàn cho khách "${text(reservation['customerName'])}"',
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                    color: brand,
                  ),
                ),
                Text(
                  'Yêu cầu: Số lượng ${integer(reservation['guestCount'])} khách',
                  style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78)),
                ),
                const SizedBox(height: 12),
                if (tables.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 20),
                    child: Center(
                      child: Text(
                        'Không có bàn trống nào đủ chỗ cho đoàn khách này.',
                        style: TextStyle(color: Color(0xFFC62828)),
                      ),
                    ),
                  )
                else
                  ConstrainedBox(
                    constraints: const BoxConstraints(maxHeight: 320),
                    child: ListView.builder(
                      shrinkWrap: true,
                      itemCount: tables.length,
                      itemBuilder: (_, idx) {
                        final table = tables[idx];
                        return ListTile(
                          leading: const Icon(Icons.table_restaurant_outlined, color: brand),
                          title: Text(text(table['tableName']), style: const TextStyle(fontWeight: FontWeight.w700)),
                          subtitle: Text('${text(table['areaName'], 'Khu chung')} · ${integer(table['capacity'])} chỗ'),
                          onTap: () => Navigator.pop(context, table),
                        );
                      },
                    ),
                  ),
              ],
            ),
          ),
        ),
      );
      if (!mounted || selected == null) return;
      await widget.api.post(
        '/admin/reservations/${integer(reservation['reservationId'])}/assign-table',
        data: {'tableId': integer(selected['tableId'])},
      );
      if (mounted) showMessage(context, 'Đã xếp bàn thành công.');
      await load(silent: true);
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  Future<void> handleCheckIn(Map<String, dynamic> reservation) async {
    final id = integer(reservation['reservationId']);
    try {
      await widget.api.post('/admin/reservations/$id/check-in');
      if (mounted) {
        showMessage(context, 'Check-in thành công! Bàn đã mở sẵn sàng phục vụ.');
      }
      await load(silent: true);

      // Nếu có bàn, điều hướng thẳng vào trang gọi món
      final table = reservation['table'] is Map ? jsonMap(reservation['table']) : null;
      if (table != null && mounted) {
        final confirmGoToOrder = await showDialog<bool>(
          context: context,
          builder: (dialog) => AlertDialog(
            title: const Text('Đón khách thành công!'),
            content: Text(
              'Đã mở bàn "${text(table['tableName'])}" cho khách ${text(reservation['customerName'])}.\nBạn có muốn vào màn hình gọi món ngay?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(dialog, false),
                child: const Text('Để sau'),
              ),
              FilledButton(
                style: FilledButton.styleFrom(backgroundColor: const Color(0xFF2E7D32)),
                onPressed: () => Navigator.pop(dialog, true),
                child: const Text('Vào Gọi Món 🍽'),
              ),
            ],
          ),
        );

        if (confirmGoToOrder == true && mounted) {
          // Lấy thông tin bàn mới nhất
          final tables = jsonMaps(await widget.api.get('/staff/orders/tables'));
          final currentTable = tables.firstWhere(
            (t) => integer(t['tableId']) == integer(table['tableId']),
            orElse: () => table,
          );
          if (mounted) {
            Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => OrderPage(api: widget.api, table: currentTable),
              ),
            ).then((_) => load(silent: true));
          }
        }
      }
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  Future<void> cancelReservation(Map<String, dynamic> reservation) async {
    final id = integer(reservation['reservationId']);
    final reasonController = TextEditingController();

    final confirm = await showDialog<bool>(
      context: context,
      builder: (dialog) => AlertDialog(
        title: const Text('Hủy phiếu đặt bàn'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hủy phiếu đặt bàn của khách "${text(reservation['customerName'])}"?'),
            const SizedBox(height: 12),
            TextField(
              controller: reasonController,
              decoration: const InputDecoration(
                labelText: 'Lý do hủy (tùy chọn)',
                border: OutlineInputBorder(),
                isDense: true,
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialog, false),
            child: const Text('Quay lại'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFFDC2626)),
            onPressed: () => Navigator.pop(dialog, true),
            child: const Text('Xác nhận hủy'),
          ),
        ],
      ),
    );

    if (confirm == true) {
      try {
        await widget.api.post(
          '/admin/reservations/$id/cancel',
          data: {'reason': reasonController.text.trim()},
        );
        if (mounted) showMessage(context, 'Đã hủy phiếu đặt bàn.');
        await load(silent: true);
      } on ApiException catch (e) {
        if (mounted) showMessage(context, e.message, isError: true);
      }
    }
  }

  Future<void> _pickCustomDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: selectedDate ?? DateTime.now(),
      firstDate: DateTime(2024),
      lastDate: DateTime(2030),
    );
    if (picked != null) {
      setState(() {
        selectedDate = picked;
        dateScope = 'custom';
      });
      load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    const filters = {
      'all': 'Tất cả',
      'pending': 'Chờ duyệt',
      'confirmed': 'Đã xác nhận',
      'checked_in': 'Đang dùng bữa',
      'cancelled': 'Đã hủy',
    };

    final isTablet = MediaQuery.sizeOf(context).width >= 750;

    return Scaffold(
      appBar: AppBar(
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Quản lý đặt bàn', style: TextStyle(fontWeight: FontWeight.w900)),
            Text(
              'Danh sách đặt bàn & đón khách vào bàn',
              style: TextStyle(fontSize: 12, color: Color(0xFF6F7E78), fontWeight: FontWeight.normal),
            ),
          ],
        ),
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? RetryState(message: error!, onRetry: load)
          : RefreshIndicator(
              onRefresh: load,
              child: Center(
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxWidth: isTablet ? 900 : double.infinity),
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                    children: [
                      // 1. THANH TÌM KIẾM
                      TextField(
                        controller: searchController,
                        onChanged: (_) => load(silent: true),
                        decoration: InputDecoration(
                          hintText: 'Tìm theo tên khách, số điện thoại...',
                          prefixIcon: const Icon(Icons.search, size: 20),
                          suffixIcon: searchController.text.isNotEmpty
                              ? IconButton(
                                  icon: const Icon(Icons.clear, size: 18),
                                  onPressed: () {
                                    searchController.clear();
                                    load();
                                  },
                                )
                              : null,
                          contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                          filled: true,
                          fillColor: isDark ? darkSurface : Colors.white,
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: BorderSide(color: themedBorder(context)),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(12),
                            borderSide: BorderSide(color: themedBorder(context)),
                          ),
                        ),
                      ),
                      const SizedBox(height: 10),

                      // 2. BỘ LỌC NGÀY (TẤT CẢ / HÔM NAY / CHỌN NGÀY)
                      SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: Row(
                          children: [
                            const Text('Thời gian: ', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFF6F7E78))),
                            ChoiceChip(
                              label: const Text('Tất cả ngày'),
                              selected: dateScope == 'all',
                              selectedColor: gold.withValues(alpha: 0.25),
                              onSelected: (_) {
                                setState(() => dateScope = 'all');
                                load();
                              },
                            ),
                            const SizedBox(width: 8),
                            ChoiceChip(
                              label: const Text('Hôm nay'),
                              selected: dateScope == 'today',
                              selectedColor: const Color(0xFFDCFCE7),
                              onSelected: (_) {
                                setState(() => dateScope = 'today');
                                load();
                              },
                            ),
                            const SizedBox(width: 8),
                            ActionChip(
                              avatar: const Icon(Icons.calendar_month, size: 16),
                              label: Text(
                                dateScope == 'custom' && selectedDate != null
                                    ? '${selectedDate!.day}/${selectedDate!.month}/${selectedDate!.year}'
                                    : 'Chọn ngày...',
                                style: TextStyle(
                                  fontWeight: dateScope == 'custom' ? FontWeight.w800 : FontWeight.normal,
                                  color: dateScope == 'custom' ? const Color(0xFF1E40AF) : null,
                                ),
                              ),
                              backgroundColor: dateScope == 'custom' ? const Color(0xFFDBEAFE) : null,
                              onPressed: _pickCustomDate,
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 10),

                      // 3. BỘ LỌC TRẠNG THÁI
                      SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: Row(
                          children: [
                            for (final entry in filters.entries)
                              Padding(
                                padding: const EdgeInsets.only(right: 8),
                                child: ChoiceChip(
                                  label: Text(entry.value),
                                  selected: status == entry.key,
                                  selectedColor: gold.withValues(alpha: 0.25),
                                  onSelected: (_) {
                                    setState(() => status = entry.key);
                                    load();
                                  },
                                ),
                              ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 14),

                      // 4. DANH SÁCH PHIẾU ĐẶT BÀN
                      if (reservations.isEmpty)
                        const EmptyState(message: 'Không tìm thấy phiếu đặt bàn nào phù hợp với bộ lọc.')
                      else if (isTablet)
                        // BỐ CỤC TABLET: 2 CỘT
                        GridView.builder(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          itemCount: reservations.length,
                          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 2,
                            crossAxisSpacing: 12,
                            mainAxisSpacing: 12,
                            childAspectRatio: 1.6,
                          ),
                          itemBuilder: (_, idx) => _ReservationCard(
                            reservation: reservations[idx],
                            onConfirm: () => action(integer(reservations[idx]['reservationId']), 'confirm', 'Đã duyệt phiếu đặt.'),
                            onAssignTable: () => assignTable(reservations[idx]),
                            onCheckIn: () => handleCheckIn(reservations[idx]),
                            onCancel: () => cancelReservation(reservations[idx]),
                            api: widget.api,
                          ),
                        )
                      else
                        // BỐ CỤC ĐIỆN THOẠI: LIST DỌC
                        ...reservations.map(
                          (reservation) => Padding(
                            padding: const EdgeInsets.only(bottom: 12),
                            child: _ReservationCard(
                              reservation: reservation,
                              onConfirm: () => action(integer(reservation['reservationId']), 'confirm', 'Đã duyệt phiếu đặt.'),
                              onAssignTable: () => assignTable(reservation),
                              onCheckIn: () => handleCheckIn(reservation),
                              onCancel: () => cancelReservation(reservation),
                              api: widget.api,
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
    );
  }
}

class _ReservationCard extends StatelessWidget {
  const _ReservationCard({
    required this.reservation,
    required this.onConfirm,
    required this.onAssignTable,
    required this.onCheckIn,
    required this.onCancel,
    required this.api,
  });

  final Map<String, dynamic> reservation;
  final VoidCallback onConfirm;
  final VoidCallback onAssignTable;
  final VoidCallback onCheckIn;
  final VoidCallback onCancel;
  final ApiClient api;

  @override
  Widget build(BuildContext context) {
    final current = text(reservation['reservationStatus']);
    final table = reservation['table'] is Map ? jsonMap(reservation['table']) : null;
    final isDark = isDarkMode(context);

    final color = current == 'pending'
        ? const Color(0xFFB78103)
        : current == 'checked_in'
        ? const Color(0xFF2E7D32)
        : current == 'cancelled'
        ? const Color(0xFFDC2626)
        : const Color(0xFF1565C0);

    return Card(
      elevation: 0,
      color: isDark ? darkSurface : Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Row 1: Khách hàng + Badge trạng thái
            Row(
              children: [
                Expanded(
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(6),
                        decoration: BoxDecoration(
                          color: const Color(0xFF2563EB).withValues(alpha: 0.1),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(Icons.person, size: 16, color: Color(0xFF2563EB)),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          text(reservation['customerName']),
                          style: TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 15,
                            color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: .12),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    reservationStatusText(current),
                    style: TextStyle(
                      color: color,
                      fontWeight: FontWeight.w800,
                      fontSize: 11,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),

            // Row 2: SĐT & Số lượng khách
            Row(
              children: [
                const Icon(Icons.phone_outlined, size: 14, color: Color(0xFF6F7E78)),
                const SizedBox(width: 4),
                Text(
                  text(reservation['customerPhone']),
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
                ),
                const SizedBox(width: 14),
                const Icon(Icons.groups_outlined, size: 15, color: Color(0xFF6F7E78)),
                const SizedBox(width: 4),
                Text(
                  '${integer(reservation['guestCount'])} người',
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
                ),
              ],
            ),
            const SizedBox(height: 6),

            // Row 3: Thời gian đặt & Bàn được phân
            Row(
              children: [
                const Icon(Icons.access_time_rounded, size: 14, color: Color(0xFF6F7E78)),
                const SizedBox(width: 4),
                Text(
                  shortDate(reservation['reservationDate']),
                  style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78)),
                ),
                const SizedBox(width: 14),
                const Icon(Icons.table_restaurant_outlined, size: 15, color: Color(0xFF6F7E78)),
                const SizedBox(width: 4),
                Text(
                  table == null ? 'Chưa xếp bàn' : text(table['tableName']),
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: table == null ? const Color(0xFFDC2626) : const Color(0xFF2563EB),
                  ),
                ),
              ],
            ),

            if (text(reservation['note']).isNotEmpty) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: isDark ? const Color(0xFF1E293B) : const Color(0xFFFEF3C7),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  '📝 ${text(reservation['note'])}',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 11, color: Color(0xFFD97706), fontWeight: FontWeight.w600),
                ),
              ),
            ],

            const SizedBox(height: 12),
            const Divider(height: 1),
            const SizedBox(height: 10),

            // HÀNG NÚT THAO TÁC TRỰC TIẾP
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                if (current != 'cancelled' && current != 'completed' && current != 'checked_in') ...[
                  OutlinedButton(
                    style: OutlinedButton.styleFrom(
                      foregroundColor: const Color(0xFFDC2626),
                      side: const BorderSide(color: Color(0xFFFCA5A5)),
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                      visualDensity: VisualDensity.compact,
                    ),
                    onPressed: onCancel,
                    child: const Text('Hủy phiếu', style: TextStyle(fontSize: 11)),
                  ),
                  const SizedBox(width: 8),
                ],

                if (current == 'pending') ...[
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFFB78103),
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      visualDensity: VisualDensity.compact,
                    ),
                    onPressed: onConfirm,
                    icon: const Icon(Icons.check, size: 14),
                    label: const Text('Duyệt phiếu', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800)),
                  ),
                ] else if (current == 'confirmed' && table == null) ...[
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF1565C0),
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      visualDensity: VisualDensity.compact,
                    ),
                    onPressed: onAssignTable,
                    icon: const Icon(Icons.table_restaurant, size: 14),
                    label: const Text('Xếp bàn ngay', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800)),
                  ),
                ] else if (current == 'confirmed' && table != null) ...[
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF2E7D32),
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                      visualDensity: VisualDensity.compact,
                    ),
                    onPressed: onCheckIn,
                    icon: const Icon(Icons.login, size: 15),
                    label: const Text('Đón khách (Check-in)', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800)),
                  ),
                ] else if (current == 'checked_in' && table != null) ...[
                  OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(
                      foregroundColor: const Color(0xFF2E7D32),
                      side: const BorderSide(color: Color(0xFF86EFAC)),
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                      visualDensity: VisualDensity.compact,
                    ),
                    onPressed: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => OrderPage(api: api, table: table),
                        ),
                      );
                    },
                    icon: const Icon(Icons.restaurant_menu, size: 14),
                    label: const Text('Vào gọi món', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800)),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}

String reservationStatusText(String status) =>
    const {
      'pending': 'Chờ duyệt',
      'confirmed': 'Đã xác nhận',
      'checked_in': 'Đang dùng bữa',
      'completed': 'Hoàn tất',
      'cancelled': 'Đã hủy',
      'no_show': 'Không đến',
    }[status] ??
    status;

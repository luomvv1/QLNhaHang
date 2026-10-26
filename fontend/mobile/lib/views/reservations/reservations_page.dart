import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

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
  String dateScope = 'today'; // Mặc định là 'today' chuẩn nghiệp vụ POS
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
    } else if (dateScope == 'tomorrow') {
      params['date'] = DateTime.now().add(const Duration(days: 1)).toIso8601String().substring(0, 10);
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

  // Xếp bàn trực quan theo khu vực & sức chứa
  Future<void> assignTable(Map<String, dynamic> reservation) async {
    final isDark = isDarkMode(context);
    final guestCount = integer(reservation['guestCount']);
    final resId = integer(reservation['reservationId']);
    final dateStr = dateScope == 'today'
        ? DateTime.now().toIso8601String().substring(0, 10)
        : dateScope == 'tomorrow'
        ? DateTime.now().add(const Duration(days: 1)).toIso8601String().substring(0, 10)
        : selectedDate?.toIso8601String().substring(0, 10);

    try {
      final query = <String, String>{};
      if (dateStr != null) query['date'] = dateStr;
      final result = jsonMap(
        await widget.api.get('/admin/reservations/tables', query: query),
      );
      final allTables = jsonMaps(result['data']);

      // Bàn hợp lệ là bàn có sức chứa >= số khách và chưa có khách đặt vào ngày đó
      final validTables = allTables.where((t) {
        final cap = integer(t['capacity']);
        final isBooked = t['isBookedOnDate'] == true;
        return cap >= guestCount && !isBooked;
      }).toList();

      if (!mounted) return;

      final selected = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        showDragHandle: true,
        backgroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        builder: (bottomSheetContext) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: const Color(0xFF2563EB).withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: const Icon(Icons.table_restaurant_rounded, color: Color(0xFF2563EB), size: 22),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Xếp bàn cho: ${text(reservation['customerName'])}',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                            ),
                          ),
                          Text(
                            'Yêu cầu: $guestCount khách · ${shortDate(reservation['reservationDate'])}',
                            style: TextStyle(fontSize: 12, color: isDark ? darkTextSecondary : const Color(0xFF64748B)),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),

                if (validTables.isEmpty)
                  Container(
                    padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
                    decoration: BoxDecoration(
                      color: isDark ? const Color(0xFF1E293B) : const Color(0xFFFEF2F2),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFFCA5A5)),
                    ),
                    child: Column(
                      children: [
                        const Icon(Icons.info_outline_rounded, color: Color(0xFFDC2626), size: 30),
                        const SizedBox(height: 8),
                        const Text(
                          'Không có bàn trống nào đủ sức chứa vào khung giờ này.',
                          textAlign: TextAlign.center,
                          style: TextStyle(fontWeight: FontWeight.w700, color: Color(0xFFDC2626), fontSize: 13),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Hãy kiểm tra lại sơ đồ bàn hoặc chuyển sang bàn khác.',
                          textAlign: TextAlign.center,
                          style: TextStyle(fontSize: 11.5, color: isDark ? darkTextSecondary : const Color(0xFF64748B)),
                        ),
                      ],
                    ),
                  )
                else
                  ConstrainedBox(
                    constraints: const BoxConstraints(maxHeight: 360),
                    child: ListView.separated(
                      shrinkWrap: true,
                      itemCount: validTables.length,
                      separatorBuilder: (ctx, index) => const SizedBox(height: 8),
                      itemBuilder: (ctx, idx) {
                        final t = validTables[idx];
                        final tName = text(t['tableName']);
                        final tArea = text(t['areaName'], 'Khu chung');
                        final tCap = integer(t['capacity']);
                        final isPerfectFit = tCap >= guestCount && tCap <= guestCount + 2;

                        return InkWell(
                          onTap: () => Navigator.pop(bottomSheetContext, t),
                          borderRadius: BorderRadius.circular(10),
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                            decoration: BoxDecoration(
                              color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: isPerfectFit
                                    ? const Color(0xFF22C55E)
                                    : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
                                width: isPerfectFit ? 1.5 : 1,
                              ),
                            ),
                            child: Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(8),
                                  decoration: BoxDecoration(
                                    color: const Color(0xFF2563EB).withValues(alpha: 0.1),
                                    borderRadius: BorderRadius.circular(8),
                                  ),
                                  child: const Icon(Icons.table_bar_rounded, size: 20, color: Color(0xFF2563EB)),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          Text(
                                            tName,
                                            style: TextStyle(
                                              fontWeight: FontWeight.w800,
                                              fontSize: 14.5,
                                              color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                                            ),
                                          ),
                                          if (isPerfectFit) ...[
                                            const SizedBox(width: 6),
                                            Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1.5),
                                              decoration: BoxDecoration(
                                                color: const Color(0xFFDCFCE7),
                                                borderRadius: BorderRadius.circular(4),
                                              ),
                                              child: const Text(
                                                'Phù hợp nhất',
                                                style: TextStyle(fontSize: 9.5, fontWeight: FontWeight.w800, color: Color(0xFF15803D)),
                                              ),
                                            ),
                                          ],
                                        ],
                                      ),
                                      const SizedBox(height: 2),
                                      Text(
                                        'Khu: $tArea · Sức chứa: $tCap người',
                                        style: TextStyle(fontSize: 11.5, color: isDark ? darkTextSecondary : const Color(0xFF64748B)),
                                      ),
                                    ],
                                  ),
                                ),
                                const Icon(Icons.chevron_right_rounded, color: Color(0xFF94A3B8)),
                              ],
                            ),
                          ),
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
        '/admin/reservations/$resId/assign-table',
        data: {'tableId': integer(selected['tableId'])},
      );
      if (mounted) showMessage(context, 'Đã xếp bàn "${text(selected['tableName'])}" thành công!');
      await load(silent: true);
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  // Check-in đón khách và tự động chuyển màn hình gọi món
  Future<void> handleCheckIn(Map<String, dynamic> reservation) async {
    final id = integer(reservation['reservationId']);
    try {
      await widget.api.post('/admin/reservations/$id/check-in');
      if (mounted) {
        showMessage(context, 'Đón khách thành công! Bàn đã mở sẵn sàng phục vụ.');
      }
      await load(silent: true);

      // Nếu có bàn, điều hướng thẳng vào trang gọi món
      final table = reservation['table'] is Map ? jsonMap(reservation['table']) : null;
      if (table != null && mounted) {
        final confirmGoToOrder = await showDialog<bool>(
          context: context,
          builder: (dialog) => AlertDialog(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            title: const Row(
              children: [
                Icon(Icons.check_circle_rounded, color: Color(0xFF16A34A), size: 24),
                SizedBox(width: 8),
                Text('Đón khách thành công!'),
              ],
            ),
            content: Text(
              'Đã check-in bàn "${text(table['tableName'])}" cho khách "${text(reservation['customerName'])}".\n\nBạn có muốn vào màn hình gọi món ngay?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(dialog, false),
                child: const Text('Để sau'),
              ),
              FilledButton.icon(
                style: FilledButton.styleFrom(backgroundColor: const Color(0xFF16A34A)),
                onPressed: () => Navigator.pop(dialog, true),
                icon: const Icon(Icons.restaurant_menu_rounded, size: 16),
                label: const Text('Vào Gọi Món 🍽'),
              ),
            ],
          ),
        );

        if (confirmGoToOrder == true && mounted) {
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
            ).then((_) {
              if (mounted) load(silent: true);
            });
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
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
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

  // Mở BottomSheet Tạo đặt bàn mới nhanh qua Hotline cho nhân viên
  Future<void> _openCreateReservationSheet() async {
    final isDark = isDarkMode(context);
    final nameCtrl = TextEditingController();
    final phoneCtrl = TextEditingController();
    final guestCtrl = TextEditingController(text: '4');
    final noteCtrl = TextEditingController();
    DateTime chosenDate = DateTime.now();
    TimeOfDay chosenTime = TimeOfDay.fromDateTime(DateTime.now().add(const Duration(minutes: 30)));
    bool isSubmitting = false;

    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (sheetCtx) {
        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            return SafeArea(
              child: Padding(
                padding: EdgeInsets.fromLTRB(16, 4, 16, 20 + MediaQuery.viewInsetsOf(ctx).bottom),
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: const Color(0xFF16A34A).withValues(alpha: 0.12),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: const Icon(Icons.add_call, color: Color(0xFF16A34A), size: 22),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'Nhận Đặt Bàn Qua Hotline',
                                  style: TextStyle(
                                    fontSize: 16.5,
                                    fontWeight: FontWeight.w800,
                                    color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                                  ),
                                ),
                                Text(
                                  'Ghi nhận thông tin khách gọi điện đặt chỗ',
                                  style: TextStyle(fontSize: 11.5, color: isDark ? darkTextSecondary : const Color(0xFF64748B)),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),

                      // Tên khách hàng & Số điện thoại
                      TextField(
                        controller: nameCtrl,
                        decoration: const InputDecoration(
                          labelText: 'Họ tên khách hàng *',
                          hintText: 'Ví dụ: Anh Tuấn, Chị Hoa...',
                          prefixIcon: Icon(Icons.person_rounded, size: 20),
                          isDense: true,
                        ),
                      ),
                      const SizedBox(height: 10),

                      Row(
                        children: [
                          Expanded(
                            flex: 3,
                            child: TextField(
                              controller: phoneCtrl,
                              keyboardType: TextInputType.phone,
                              decoration: const InputDecoration(
                                labelText: 'Số điện thoại *',
                                hintText: '090xxxxxxx',
                                prefixIcon: Icon(Icons.phone_rounded, size: 20),
                                isDense: true,
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            flex: 2,
                            child: TextField(
                              controller: guestCtrl,
                              keyboardType: TextInputType.number,
                              decoration: const InputDecoration(
                                labelText: 'Số khách *',
                                prefixIcon: Icon(Icons.groups_rounded, size: 20),
                                isDense: true,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),

                      // Chọn ngày và giờ
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              style: OutlinedButton.styleFrom(
                                padding: const EdgeInsets.symmetric(vertical: 12),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                              ),
                              onPressed: () async {
                                final picked = await showDatePicker(
                                  context: ctx,
                                  initialDate: chosenDate,
                                  firstDate: DateTime.now(),
                                  lastDate: DateTime.now().add(const Duration(days: 60)),
                                );
                                if (picked != null) {
                                  setSheetState(() => chosenDate = picked);
                                }
                              },
                              icon: const Icon(Icons.calendar_today_rounded, size: 16),
                              label: Text(
                                '${chosenDate.day}/${chosenDate.month}/${chosenDate.year}',
                                style: const TextStyle(fontWeight: FontWeight.w700),
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: OutlinedButton.icon(
                              style: OutlinedButton.styleFrom(
                                padding: const EdgeInsets.symmetric(vertical: 12),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                              ),
                              onPressed: () async {
                                final picked = await showTimePicker(
                                  context: ctx,
                                  initialTime: chosenTime,
                                );
                                if (picked != null) {
                                  setSheetState(() => chosenTime = picked);
                                }
                              },
                              icon: const Icon(Icons.schedule_rounded, size: 16),
                              label: Text(
                                '${chosenTime.hour.toString().padLeft(2, '0')}:${chosenTime.minute.toString().padLeft(2, '0')}',
                                style: const TextStyle(fontWeight: FontWeight.w700),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),

                      TextField(
                        controller: noteCtrl,
                        maxLines: 2,
                        decoration: const InputDecoration(
                          labelText: 'Ghi chú yêu cầu (tùy chọn)',
                          hintText: 'Ví dụ: Bàn gần cửa sổ, sinh nhật...',
                          prefixIcon: Icon(Icons.note_alt_outlined, size: 20),
                          isDense: true,
                        ),
                      ),
                      const SizedBox(height: 18),

                      FilledButton.icon(
                        style: FilledButton.styleFrom(
                          backgroundColor: brand,
                          padding: const EdgeInsets.symmetric(vertical: 13),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        ),
                        onPressed: isSubmitting
                            ? null
                            : () async {
                                final name = nameCtrl.text.trim();
                                final phone = phoneCtrl.text.trim();
                                final guests = int.tryParse(guestCtrl.text.trim()) ?? 0;

                                if (name.isEmpty) {
                                  showMessage(sheetCtx, 'Vui lòng nhập tên khách hàng.', isError: true);
                                  return;
                                }
                                if (phone.isEmpty) {
                                  showMessage(sheetCtx, 'Vui lòng nhập số điện thoại.', isError: true);
                                  return;
                                }
                                if (guests <= 0) {
                                  showMessage(sheetCtx, 'Số lượng khách phải lớn hơn 0.', isError: true);
                                  return;
                                }

                                final fullDateTime = DateTime(
                                  chosenDate.year,
                                  chosenDate.month,
                                  chosenDate.day,
                                  chosenTime.hour,
                                  chosenTime.minute,
                                );

                                setSheetState(() => isSubmitting = true);
                                try {
                                  await widget.api.post(
                                    '/admin/reservations/create',
                                    data: {
                                      'customerName': name,
                                      'customerPhone': phone,
                                      'guestCount': guests,
                                      'reservationDate': fullDateTime.toIso8601String(),
                                      'note': noteCtrl.text.trim(),
                                    },
                                  );
                                  if (!sheetCtx.mounted) return;
                                  Navigator.pop(sheetCtx);
                                  if (!mounted) return;
                                  showMessage(context, 'Đã tạo lịch đặt bàn thành công!');
                                  await load(silent: true);
                                } on ApiException catch (e) {
                                  setSheetState(() => isSubmitting = false);
                                  if (sheetCtx.mounted) showMessage(sheetCtx, e.message, isError: true);
                                }
                              },
                        icon: isSubmitting
                            ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                            : const Icon(Icons.check_rounded, size: 20),
                        label: Text(
                          isSubmitting ? 'Đang tạo lịch...' : 'Xác Nhận Tạo Lịch Đặt Bàn',
                          style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        );
      },
    );
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
    final isTablet = MediaQuery.sizeOf(context).width >= 750;

    final totalTodayCount = stats != null ? integer(stats!['totalToday']) : reservations.length;
    final pendingCount = stats != null ? integer(stats!['pendingCount']) : 0;
    final confirmedCount = stats != null ? integer(stats!['confirmedCount']) : 0;
    final checkedInCount = stats != null ? integer(stats!['checkedInCount']) : 0;

    return Scaffold(
      appBar: AppBar(
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Quản lý đặt bàn', style: TextStyle(fontWeight: FontWeight.w900)),
            Text(
              'Đón khách & Điều phối chỗ ngồi',
              style: TextStyle(fontSize: 12, color: Color(0xFF6F7E78), fontWeight: FontWeight.normal),
            ),
          ],
        ),
        actions: [
          // Nút tạo lịch đặt bàn nhanh qua Hotline
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: FilledButton.icon(
              style: FilledButton.styleFrom(
                backgroundColor: isDark ? gold : brand,
                foregroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                visualDensity: VisualDensity.compact,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
              ),
              onPressed: _openCreateReservationSheet,
              icon: const Icon(Icons.add_rounded, size: 16),
              label: const Text(
                'Nhận Đặt',
                style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w800),
              ),
            ),
          ),
        ],
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
                    padding: const EdgeInsets.fromLTRB(14, 8, 14, 24),
                    children: [
                      // 1. THANH KPI TƯƠNG TÁC LỌC 1 CHẠM (UI/UX PRO METRIC BAR)
                      Container(
                        margin: const EdgeInsets.only(bottom: 10),
                        child: Row(
                          children: [
                            Expanded(
                              child: _KpiChip(
                                label: 'Tất cả',
                                count: totalTodayCount,
                                isSelected: status == 'all',
                                color: isDark ? gold : brand,
                                isDark: isDark,
                                onTap: () {
                                  setState(() => status = 'all');
                                  load();
                                },
                              ),
                            ),
                            const SizedBox(width: 6),
                            Expanded(
                              child: _KpiChip(
                                label: 'Chờ duyệt',
                                count: pendingCount,
                                isSelected: status == 'pending',
                                color: const Color(0xFFD97706),
                                isDark: isDark,
                                onTap: () {
                                  setState(() => status = 'pending');
                                  load();
                                },
                              ),
                            ),
                            const SizedBox(width: 6),
                            Expanded(
                              child: _KpiChip(
                                label: 'Đã xếp bàn',
                                count: confirmedCount,
                                isSelected: status == 'confirmed',
                                color: const Color(0xFF2563EB),
                                isDark: isDark,
                                onTap: () {
                                  setState(() => status = 'confirmed');
                                  load();
                                },
                              ),
                            ),
                            const SizedBox(width: 6),
                            Expanded(
                              child: _KpiChip(
                                label: 'Đang ăn',
                                count: checkedInCount,
                                isSelected: status == 'checked_in',
                                color: const Color(0xFF16A34A),
                                isDark: isDark,
                                onTap: () {
                                  setState(() => status = 'checked_in');
                                  load();
                                },
                              ),
                            ),
                          ],
                        ),
                      ),

                      // 2. Ô TÌM KIẾM
                      Container(
                        height: 38,
                        margin: const EdgeInsets.only(bottom: 8),
                        decoration: BoxDecoration(
                          color: isDark ? darkSurface : Colors.white,
                          borderRadius: BorderRadius.circular(9),
                          border: Border.all(
                            color: isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
                          ),
                        ),
                        child: TextField(
                          controller: searchController,
                          onChanged: (_) => load(silent: true),
                          style: TextStyle(
                            fontSize: 12.5,
                            color: isDark ? Colors.white : const Color(0xFF0F172A),
                          ),
                          decoration: InputDecoration(
                            hintText: 'Tìm theo tên khách, số điện thoại...',
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
                            suffixIcon: searchController.text.isNotEmpty
                                ? IconButton(
                                    padding: EdgeInsets.zero,
                                    icon: const Icon(Icons.cancel_rounded, size: 16),
                                    onPressed: () {
                                      searchController.clear();
                                      load();
                                    },
                                  )
                                : null,
                            border: InputBorder.none,
                            isDense: true,
                            contentPadding: const EdgeInsets.symmetric(vertical: 9, horizontal: 4),
                          ),
                        ),
                      ),

                      // 3. THANH LỌC NGÀY CHUẨN NGHIỆP VỤ NHÀ HÀNG
                      SizedBox(
                        height: 30,
                        child: ListView(
                          scrollDirection: Axis.horizontal,
                          children: [
                            _buildDateChip(
                              label: '⚡ Hôm nay',
                              isSelected: dateScope == 'today',
                              isDark: isDark,
                              onTap: () {
                                setState(() => dateScope = 'today');
                                load();
                              },
                            ),
                            const SizedBox(width: 6),
                            _buildDateChip(
                              label: 'Ngày mai',
                              isSelected: dateScope == 'tomorrow',
                              isDark: isDark,
                              onTap: () {
                                setState(() => dateScope = 'tomorrow');
                                load();
                              },
                            ),
                            const SizedBox(width: 6),
                            _buildDateChip(
                              label: 'Tất cả ngày',
                              isSelected: dateScope == 'all',
                              isDark: isDark,
                              onTap: () {
                                setState(() => dateScope = 'all');
                                load();
                              },
                            ),
                            const SizedBox(width: 6),
                            InkWell(
                              onTap: _pickCustomDate,
                              borderRadius: BorderRadius.circular(6),
                              child: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
                                decoration: BoxDecoration(
                                  color: dateScope == 'custom'
                                      ? (isDark ? gold.withValues(alpha: 0.25) : const Color(0xFFFEF3C7))
                                      : (isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
                                  borderRadius: BorderRadius.circular(6),
                                  border: Border.all(
                                    color: dateScope == 'custom'
                                        ? (isDark ? gold : const Color(0xFFD97706))
                                        : Colors.transparent,
                                  ),
                                ),
                                child: Row(
                                  children: [
                                    Icon(
                                      Icons.calendar_month_rounded,
                                      size: 13,
                                      color: dateScope == 'custom'
                                          ? (isDark ? gold : const Color(0xFFB45309))
                                          : (isDark ? darkTextSecondary : const Color(0xFF64748B)),
                                    ),
                                    const SizedBox(width: 4),
                                    Text(
                                      dateScope == 'custom' && selectedDate != null
                                          ? '${selectedDate!.day}/${selectedDate!.month}/${selectedDate!.year}'
                                          : 'Chọn ngày...',
                                      style: TextStyle(
                                        fontSize: 11,
                                        fontWeight: dateScope == 'custom' ? FontWeight.w800 : FontWeight.w600,
                                        color: dateScope == 'custom'
                                            ? (isDark ? gold : const Color(0xFFB45309))
                                            : (isDark ? darkTextSecondary : const Color(0xFF64748B)),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 12),

                      // 4. DANH SÁCH PHIẾU ĐẶT BÀN
                      if (reservations.isEmpty)
                        Container(
                          padding: const EdgeInsets.symmetric(vertical: 48, horizontal: 16),
                          alignment: Alignment.center,
                          child: Column(
                            children: [
                              Icon(
                                Icons.event_available_rounded,
                                size: 48,
                                color: isDark ? const Color(0xFF475569) : const Color(0xFF94A3B8),
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'Không có lịch đặt bàn nào phù hợp.',
                                style: TextStyle(
                                  fontSize: 13.5,
                                  fontWeight: FontWeight.w700,
                                  color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                                ),
                              ),
                              const SizedBox(height: 12),
                              OutlinedButton.icon(
                                style: OutlinedButton.styleFrom(
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                ),
                                onPressed: _openCreateReservationSheet,
                                icon: const Icon(Icons.add_rounded, size: 16),
                                label: const Text('Nhận Đặt Bàn Ngay', style: TextStyle(fontSize: 12)),
                              ),
                            ],
                          ),
                        )
                      else if (isTablet)
                        // BỐ CỤC TABLET: GRID 2 CỘT
                        GridView.builder(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          itemCount: reservations.length,
                          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 2,
                            crossAxisSpacing: 10,
                            mainAxisSpacing: 10,
                            childAspectRatio: 1.55,
                          ),
                          itemBuilder: (ctx, idx) => _ReservationCard(
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
                        ListView.separated(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          itemCount: reservations.length,
                          separatorBuilder: (ctx, index) => const SizedBox(height: 10),
                          itemBuilder: (ctx, idx) => _ReservationCard(
                            reservation: reservations[idx],
                            onConfirm: () => action(integer(reservations[idx]['reservationId']), 'confirm', 'Đã duyệt phiếu đặt.'),
                            onAssignTable: () => assignTable(reservations[idx]),
                            onCheckIn: () => handleCheckIn(reservations[idx]),
                            onCancel: () => cancelReservation(reservations[idx]),
                            api: widget.api,
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
    );
  }

  Widget _buildDateChip({
    required String label,
    required bool isSelected,
    required bool isDark,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(6),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        decoration: BoxDecoration(
          color: isSelected
              ? (isDark ? gold.withValues(alpha: 0.25) : const Color(0xFFFEF3C7))
              : (isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9)),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(
            color: isSelected ? (isDark ? gold : const Color(0xFFD97706)) : Colors.transparent,
          ),
        ),
        alignment: Alignment.center,
        child: Text(
          label,
          style: TextStyle(
            fontSize: 11,
            fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
            color: isSelected
                ? (isDark ? gold : const Color(0xFFB45309))
                : (isDark ? darkTextSecondary : const Color(0xFF64748B)),
          ),
        ),
      ),
    );
  }
}

class _KpiChip extends StatelessWidget {
  const _KpiChip({
    required this.label,
    required this.count,
    required this.isSelected,
    required this.color,
    required this.isDark,
    required this.onTap,
  });

  final String label;
  final int count;
  final bool isSelected;
  final Color color;
  final bool isDark;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(9),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 4),
        decoration: BoxDecoration(
          color: isSelected
              ? color.withValues(alpha: isDark ? 0.22 : 0.12)
              : (isDark ? darkSurface : Colors.white),
          borderRadius: BorderRadius.circular(9),
          border: Border.all(
            color: isSelected ? color : (isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
            width: isSelected ? 1.6 : 1,
          ),
        ),
        child: Column(
          children: [
            Text(
              '$count',
              style: TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w900,
                color: isSelected ? color : (isDark ? Colors.white : const Color(0xFF0F172A)),
                height: 1.1,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontSize: 10,
                fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                color: isSelected ? color : (isDark ? darkTextSecondary : const Color(0xFF64748B)),
              ),
            ),
          ],
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
    final custName = text(reservation['customerName']);
    final custPhone = text(reservation['customerPhone']);
    final guestCount = integer(reservation['guestCount']);
    final note = text(reservation['note']);
    final resDate = reservation['reservationDate'];

    // Tính toán thời gian đón khách
    String timeStr = '—';
    String dateLabel = '';
    Color timeColor = const Color(0xFF2563EB);

    if (resDate != null) {
      final dt = DateTime.tryParse(resDate.toString())?.toLocal();
      if (dt != null) {
        String two(int v) => v.toString().padLeft(2, '0');
        timeStr = '${two(dt.hour)}:${two(dt.minute)}';

        final now = DateTime.now();
        final diff = dt.difference(now);
        final isToday = dt.year == now.year && dt.month == now.month && dt.day == now.day;

        if (isToday) {
          if (diff.inMinutes > 0 && diff.inMinutes <= 45) {
            dateLabel = 'Còn ${diff.inMinutes}p';
            timeColor = const Color(0xFFD97706);
          } else if (diff.inMinutes <= 0 && diff.inMinutes >= -60) {
            dateLabel = 'Tới giờ hẹn';
            timeColor = const Color(0xFFDC2626);
          } else {
            dateLabel = 'Hôm nay';
          }
        } else {
          dateLabel = '${two(dt.day)}/${two(dt.month)}';
        }
      }
    }

    final Color statusColor;
    final Color statusBg;
    if (current == 'pending') {
      statusColor = const Color(0xFFD97706);
      statusBg = const Color(0xFFFEF3C7);
    } else if (current == 'checked_in') {
      statusColor = const Color(0xFF16A34A);
      statusBg = const Color(0xFFDCFCE7);
    } else if (current == 'cancelled') {
      statusColor = const Color(0xFFDC2626);
      statusBg = const Color(0xFFFEE2E2);
    } else {
      statusColor = const Color(0xFF2563EB);
      statusBg = const Color(0xFFDBEAFE);
    }

    return Card(
      elevation: 0,
      margin: EdgeInsets.zero,
      color: isDark ? darkSurface : Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(13),
        side: BorderSide(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
          width: 1.1,
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Row 1: Khối Giờ Hẹn Nổi Bật + Tên Khách & SĐT + Trạng Thái
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Khối Giờ Hẹn chuẩn POS (Nhận diện thời gian đến tức thì)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
                  decoration: BoxDecoration(
                    color: timeColor.withValues(alpha: isDark ? 0.2 : 0.1),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: timeColor.withValues(alpha: 0.3)),
                  ),
                  child: Column(
                    children: [
                      Text(
                        timeStr,
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w900,
                          color: timeColor,
                          letterSpacing: -0.3,
                        ),
                      ),
                      Text(
                        dateLabel,
                        style: TextStyle(
                          fontSize: 9.5,
                          fontWeight: FontWeight.w700,
                          color: timeColor,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 10),

                // Thông tin khách & SĐT
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Flexible(
                            child: Text(
                              custName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                fontWeight: FontWeight.w900,
                                fontSize: 14.5,
                                color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Row(
                        children: [
                          InkWell(
                            onTap: () {
                              if (custPhone.isNotEmpty) {
                                Clipboard.setData(ClipboardData(text: custPhone));
                                showMessage(context, 'Đã sao chép SĐT: $custPhone');
                              }
                            },
                            child: Row(
                              children: [
                                const Icon(Icons.phone_outlined, size: 12.5, color: Color(0xFF64748B)),
                                const SizedBox(width: 3),
                                Text(
                                  custPhone,
                                  style: TextStyle(
                                    fontSize: 11.5,
                                    fontWeight: FontWeight.w600,
                                    color: isDark ? darkTextSecondary : const Color(0xFF475569),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                            decoration: BoxDecoration(
                              color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Row(
                              children: [
                                const Icon(Icons.groups_outlined, size: 12, color: Color(0xFF64748B)),
                                const SizedBox(width: 2.5),
                                Text(
                                  '$guestCount khách',
                                  style: TextStyle(
                                    fontSize: 10.5,
                                    fontWeight: FontWeight.w700,
                                    color: isDark ? darkTextSecondary : const Color(0xFF334155),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                // Badge Trạng thái
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                  decoration: BoxDecoration(
                    color: isDark ? statusColor.withValues(alpha: 0.2) : statusBg,
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    reservationStatusText(current),
                    style: TextStyle(
                      color: statusColor,
                      fontWeight: FontWeight.w800,
                      fontSize: 10.5,
                    ),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 8),

            // Row 2: Bàn được phân hoặc cảnh báo chưa xếp bàn
            if (table != null)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
                decoration: BoxDecoration(
                  color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF0FDF4),
                  borderRadius: BorderRadius.circular(7),
                  border: Border.all(
                    color: isDark ? const Color(0xFF334155) : const Color(0xFFBBF7D0),
                  ),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.table_restaurant_rounded, size: 15, color: Color(0xFF16A34A)),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        '${text(table['tableName'])} · ${text(table['areaName'], 'Khu chung')} (${integer(table['capacity'])} chỗ)',
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: FontWeight.w700,
                          color: isDark ? const Color(0xFF4ADE80) : const Color(0xFF15803D),
                        ),
                      ),
                    ),
                    if (current == 'confirmed')
                      InkWell(
                        onTap: onAssignTable,
                        child: const Text(
                          'Đổi bàn',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: Color(0xFF2563EB),
                          ),
                        ),
                      ),
                  ],
                ),
              )
            else if (current != 'cancelled')
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
                decoration: BoxDecoration(
                  color: isDark ? const Color(0xFF3B1E1E) : const Color(0xFFFEF2F2),
                  borderRadius: BorderRadius.circular(7),
                  border: Border.all(color: const Color(0xFFFECACA)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.warning_amber_rounded, size: 15, color: Color(0xFFDC2626)),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        'Chưa xếp bàn ăn cho khách',
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: FontWeight.w700,
                          color: isDark ? const Color(0xFFF87171) : const Color(0xFFB91C1C),
                        ),
                      ),
                    ),
                    InkWell(
                      onTap: onAssignTable,
                      child: const Text(
                        'Xếp bàn ngay →',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w800,
                          color: Color(0xFF2563EB),
                        ),
                      ),
                    ),
                  ],
                ),
              ),

            // Note (nếu có)
            if (note.isNotEmpty) ...[
              const SizedBox(height: 6),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: isDark ? const Color(0xFF1E293B) : const Color(0xFFFEF3C7),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  '💬 “$note”',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 11,
                    fontStyle: FontStyle.italic,
                    color: isDark ? const Color(0xFFFCD34D) : const Color(0xFFB45309),
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],

            const SizedBox(height: 9),

            // HÀNG NÚT THAO TÁC NGAY DÀNH CHO NHÂN VIÊN
            Row(
              children: [
                if (current != 'cancelled' && current != 'completed' && current != 'checked_in') ...[
                  SizedBox(
                    height: 32,
                    child: OutlinedButton(
                      style: OutlinedButton.styleFrom(
                        foregroundColor: const Color(0xFFDC2626),
                        side: const BorderSide(color: Color(0xFFFCA5A5)),
                        padding: const EdgeInsets.symmetric(horizontal: 10),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(7)),
                      ),
                      onPressed: onCancel,
                      child: const Text('Hủy phiếu', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
                    ),
                  ),
                  const SizedBox(width: 6),
                ],

                Expanded(
                  child: _buildMainActionButton(
                    context: context,
                    status: current,
                    hasTable: table != null,
                    table: table,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMainActionButton({
    required BuildContext context,
    required String status,
    required bool hasTable,
    required Map<String, dynamic>? table,
  }) {
    if (status == 'pending') {
      return SizedBox(
        height: 32,
        child: FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: const Color(0xFFD97706),
            foregroundColor: Colors.white,
            padding: EdgeInsets.zero,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(7)),
          ),
          onPressed: onConfirm,
          icon: const Icon(Icons.check_rounded, size: 15),
          label: const Text(
            'Duyệt Phiếu Đặt Bàn',
            style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w800),
          ),
        ),
      );
    }

    if (status == 'confirmed' && !hasTable) {
      return SizedBox(
        height: 32,
        child: FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: const Color(0xFF2563EB),
            foregroundColor: Colors.white,
            padding: EdgeInsets.zero,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(7)),
          ),
          onPressed: onAssignTable,
          icon: const Icon(Icons.table_restaurant_rounded, size: 15),
          label: const Text(
            'Chọn Bàn Cho Khách',
            style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w800),
          ),
        ),
      );
    }

    if (status == 'confirmed' && hasTable) {
      return SizedBox(
        height: 32,
        child: FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: const Color(0xFF16A34A),
            foregroundColor: Colors.white,
            padding: EdgeInsets.zero,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(7)),
          ),
          onPressed: onCheckIn,
          icon: const Icon(Icons.login_rounded, size: 15),
          label: const Text(
            'Đón Khách (Check-in)',
            style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.w800),
          ),
        ),
      );
    }

    if (status == 'checked_in' && table != null) {
      return SizedBox(
        height: 32,
        child: OutlinedButton.icon(
          style: OutlinedButton.styleFrom(
            foregroundColor: const Color(0xFF16A34A),
            side: const BorderSide(color: Color(0xFF86EFAC)),
            padding: EdgeInsets.zero,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(7)),
          ),
          onPressed: () {
            Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => OrderPage(api: api, table: table),
              ),
            );
          },
          icon: const Icon(Icons.restaurant_menu_rounded, size: 14),
          label: const Text('Vào Gọi Món', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800)),
        ),
      );
    }

    return const SizedBox.shrink();
  }
}

String reservationStatusText(String status) =>
    const {
      'pending': 'Chờ duyệt',
      'confirmed': 'Đã xếp bàn',
      'checked_in': 'Đang dùng bữa',
      'completed': 'Hoàn tất',
      'cancelled': 'Đã hủy',
      'no_show': 'Không đến',
    }[status] ??
    status;

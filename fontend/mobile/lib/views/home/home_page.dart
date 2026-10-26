import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/staff_core.dart';

class HomePage extends StatefulWidget {
  const HomePage({
    super.key,
    required this.session,
    required this.onTables,
    required this.onReservations,
    required this.onInvoices,
    this.onOpenNewOrder,
    this.onServingOrder,
    this.onTransferMerge,
    this.onPayment,
    this.onKitchenReady,
  });

  final StaffSession session;
  final VoidCallback onTables;
  final VoidCallback onReservations;
  final VoidCallback onInvoices;
  final VoidCallback? onOpenNewOrder;
  final VoidCallback? onServingOrder;
  final VoidCallback? onTransferMerge;
  final VoidCallback? onPayment;
  final VoidCallback? onKitchenReady;

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int totalTables = 0;
  int emptyCount = 0;
  int servingCount = 0;
  int readyTablesCount = 0;
  int reservedCount = 0;
  bool loadingStats = false;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    loadStats();
    _pollingTimer = Timer.periodic(const Duration(seconds: 4), (_) {
      if (mounted) loadStats(silent: true);
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  Future<void> loadStats({bool silent = false}) async {
    if (!silent && totalTables == 0) {
      setState(() => loadingStats = true);
    }
    try {
      final data = jsonMaps(await widget.session.api.get('/staff/orders/tables'));
      int empty = 0;
      int serving = 0;
      int ready = 0;
      int reserved = 0;

      for (final t in data) {
        final status = text(t['tableStatus']);
        if (status == 'empty') empty++;
        if (status == 'serving') serving++;
        if (status == 'reserved') reserved++;

        final order = t['activeOrder'];
        if (order is Map && integer(order['readyCount']) > 0) {
          ready++;
        }
      }

      if (mounted) {
        setState(() {
          totalTables = data.length;
          emptyCount = empty;
          servingCount = serving;
          readyTablesCount = ready;
          reservedCount = reserved;
        });
      }
    } catch (_) {
      // Bỏ qua lỗi ngầm
    } finally {
      if (mounted && loadingStats) {
        setState(() => loadingStats = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return Scaffold(
      backgroundColor: isDark ? darkCanvas : canvas,
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: isDark ? const Color(0xFF0F172A) : brand,
                borderRadius: BorderRadius.circular(8),
                border: isDark ? Border.all(color: gold.withValues(alpha: 0.3)) : null,
              ),
              child: const Icon(Icons.restaurant, color: gold, size: 20),
            ),
            const SizedBox(width: 10),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Dola Restaurant',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w900,
                    color: isDark ? darkTextPrimary : brand,
                  ),
                ),
                Text(
                  'Ứng dụng phục vụ (Staff POS)',
                  style: TextStyle(
                    fontSize: 11,
                    color: isDark ? darkTextSecondary : const Color(0xFF6F7E78),
                    fontWeight: FontWeight.normal,
                  ),
                ),
              ],
            ),
          ],
        ),
        actions: [
          IconButton(
            onPressed: widget.onTables,
            tooltip: 'Sơ đồ bàn',
            icon: Icon(
              Icons.table_restaurant_outlined,
              color: isDark ? gold : brand,
            ),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: loadStats,
        color: brand,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
          children: [
            // Banner Nhân viên
            Card(
              color: isDark ? darkSurface : brand,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
                side: BorderSide(
                  color: isDark ? gold.withValues(alpha: 0.4) : cardBorder,
                ),
              ),
              child: Padding(
                padding: const EdgeInsets.all(18),
                child: Row(
                  children: [
                    Container(
                      width: 50,
                      height: 50,
                      decoration: BoxDecoration(
                        color: isDark
                            ? const Color(0xFF0F172A)
                            : Colors.white.withValues(alpha: 0.12),
                        shape: BoxShape.circle,
                        border: Border.all(color: gold, width: 2),
                      ),
                      child: Center(
                        child: Text(
                          widget.session.fullName.isNotEmpty
                              ? widget.session.fullName[0].toUpperCase()
                              : 'N',
                          style: const TextStyle(
                            color: gold,
                            fontSize: 22,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Xin chào, ${widget.session.fullName}',
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 16.5,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                          const SizedBox(height: 3),
                          Row(
                            children: [
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 7,
                                  vertical: 1.5,
                                ),
                                decoration: BoxDecoration(
                                  color: gold.withValues(alpha: 0.25),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  widget.session.role.toUpperCase(),
                                  style: const TextStyle(
                                    color: gold,
                                    fontSize: 10.5,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ),
                              const SizedBox(width: 8),
                              const Text(
                                'Đang trong ca làm việc',
                                style: TextStyle(
                                  color: Color(0xFFBACEC6),
                                  fontSize: 11.5,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 18),

            // Tiêu đề Thao tác
            Row(
              children: [
                const Icon(Icons.bolt_rounded, color: gold, size: 20),
                const SizedBox(width: 6),
                Text(
                  'Thao tác phục vụ nhanh',
                  style: TextStyle(
                    fontSize: 15.5,
                    fontWeight: FontWeight.w900,
                    color: isDark ? gold : brand,
                  ),
                ),
                const Spacer(),
                Text(
                  '$totalTables bàn',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),

            // Lưới Action Responsive với tính năng Auto-Filter
            LayoutBuilder(
              builder: (context, constraints) {
                final columns = constraints.maxWidth >= 900
                    ? 4
                    : constraints.maxWidth >= 600
                    ? 3
                    : 2;

                final actions = [
                  // 1. Tạo order mới (Chỉ hiện bàn trống)
                  _HomeAction(
                    icon: Icons.add_shopping_cart_rounded,
                    title: 'Tạo order mới',
                    subtitle: 'Lọc các bàn trống để mở đơn',
                    badge: emptyCount > 0 ? '$emptyCount bàn trống' : 'Hết bàn',
                    badgeColor: emptyCount > 0 ? const Color(0xFF16A34A) : const Color(0xFFDC2626),
                    color: const Color(0xFF16A34A),
                    onTap: widget.onOpenNewOrder ?? widget.onTables,
                  ),

                  // 2. Gọi món / Thêm món (Chỉ hiện bàn đang phục vụ)
                  _HomeAction(
                    icon: Icons.restaurant_menu_rounded,
                    title: 'Gọi món',
                    subtitle: 'Thêm món cho bàn đang ăn',
                    badge: '$servingCount bàn đang ăn',
                    badgeColor: const Color(0xFFD97706),
                    color: const Color(0xFFD97706),
                    onTap: widget.onServingOrder ?? widget.onTables,
                  ),

                  // 3. Chuyển / gộp bàn (Đi thẳng vào bàn có khách)
                  _HomeAction(
                    icon: Icons.swap_horiz_rounded,
                    title: 'Chuyển / gộp bàn',
                    subtitle: 'Đổi hoặc ghép bàn có khách',
                    badge: '$servingCount bàn',
                    badgeColor: const Color(0xFF7C3AED),
                    color: const Color(0xFF7C3AED),
                    onTap: widget.onTransferMerge ?? widget.onTables,
                  ),

                  // 4. Thanh toán POS (Đi thẳng vào bàn có khách)
                  _HomeAction(
                    icon: Icons.payments_rounded,
                    title: 'Thanh toán POS',
                    subtitle: 'Tính tiền bàn đang ăn',
                    badge: '$servingCount bàn',
                    badgeColor: const Color(0xFFEA580C),
                    color: const Color(0xFFEA580C),
                    onTap: widget.onPayment ?? widget.onTables,
                  ),

                  // 5. Món chờ bưng (Bếp nấu xong, chờ ra bàn)
                  _HomeAction(
                    icon: Icons.room_service_rounded,
                    title: 'Món chờ bưng',
                    subtitle: 'Bếp đã nấu xong, chờ ra bàn',
                    badge: readyTablesCount > 0 ? '🔥 $readyTablesCount bàn có món' : '0 bàn',
                    badgeColor: readyTablesCount > 0 ? const Color(0xFFDC2626) : const Color(0xFF64748B),
                    color: const Color(0xFF0284C7),
                    onTap: widget.onKitchenReady ?? widget.onTables,
                  ),

                  // 6. Sơ đồ bàn (Xem tất cả)
                  _HomeAction(
                    icon: Icons.table_restaurant_rounded,
                    title: 'Sơ đồ bàn',
                    subtitle: 'Xem toàn bộ khu vực bàn',
                    badge: '$totalTables bàn',
                    badgeColor: const Color(0xFF2E7D32),
                    color: const Color(0xFF2E7D32),
                    onTap: widget.onTables,
                  ),

                  // 7. Khách đặt bàn
                  _HomeAction(
                    icon: Icons.event_seat_rounded,
                    title: 'Khách đặt bàn',
                    subtitle: 'Tiếp nhận & check-in',
                    badge: reservedCount > 0 ? '$reservedCount đặt trước' : 'Lịch đặt',
                    badgeColor: const Color(0xFF2563EB),
                    color: const Color(0xFF2563EB),
                    onTap: widget.onReservations,
                  ),

                  // 8. Hóa đơn gần đây
                  _HomeAction(
                    icon: Icons.receipt_long_rounded,
                    title: 'Hóa đơn gần đây',
                    subtitle: 'Xem lại bill & in lại',
                    badge: 'In lại bill',
                    badgeColor: const Color(0xFF0D9488),
                    color: const Color(0xFF0D9488),
                    onTap: widget.onInvoices,
                  ),
                ];

                return GridView.builder(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: actions.length,
                  gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: columns,
                    crossAxisSpacing: 10,
                    mainAxisSpacing: 10,
                    childAspectRatio: constraints.maxWidth >= 600 ? 1.55 : 1.25,
                  ),
                  itemBuilder: (_, idx) => actions[idx],
                );
              },
            ),
            const SizedBox(height: 20),

            // Lời nhắc nghiệp vụ phục vụ
            Card(
              color: isDark ? darkSurface : const Color(0xFFF9FBF9),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: isDark ? darkCardBorder : cardBorder),
              ),
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: gold.withValues(alpha: 0.15),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.tips_and_updates_outlined,
                        color: gold,
                        size: 22,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Quy trình phục vụ chuẩn POS',
                            style: TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 13,
                              color: isDark ? gold : brand,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '1. Tạo order bàn trống -> 2. Thêm món & Gửi bếp -> 3. Kiểm tra "Món chờ bưng" mang ra bàn -> 4. Thanh toán bill.',
                            style: TextStyle(
                              fontSize: 11,
                              color: isDark ? darkTextSecondary : const Color(0xFF6F7E78),
                              height: 1.35,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HomeAction extends StatelessWidget {
  const _HomeAction({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.color,
    required this.onTap,
    this.badge,
    this.badgeColor,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color color;
  final VoidCallback onTap;
  final String? badge;
  final Color? badgeColor;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return Card(
      elevation: 0,
      margin: EdgeInsets.zero,
      color: isDark ? darkSurface : Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: isDark ? darkCardBorder : cardBorder),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: color.withValues(alpha: isDark ? 0.2 : 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Icon(icon, color: color, size: 22),
                  ),
                  const Spacer(),
                  if (badge != null && badge!.isNotEmpty)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: (badgeColor ?? color).withValues(alpha: isDark ? 0.25 : 0.1),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: (badgeColor ?? color).withValues(alpha: isDark ? 0.4 : 0.3),
                        ),
                      ),
                      child: Text(
                        badge!,
                        style: TextStyle(
                          fontSize: 9.5,
                          fontWeight: FontWeight.w800,
                          color: badgeColor ?? color,
                        ),
                      ),
                    ),
                ],
              ),
              const Spacer(),
              Text(
                title,
                style: TextStyle(
                  fontWeight: FontWeight.w900,
                  fontSize: 13.5,
                  color: isDark ? darkTextPrimary : brand,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                subtitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 10.5,
                  color: isDark ? darkTextSecondary : const Color(0xFF6F7E78),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

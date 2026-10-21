import 'package:flutter/material.dart';

import '../../core/staff_core.dart';

class HomePage extends StatelessWidget {
  const HomePage({
    super.key,
    required this.session,
    required this.onTables,
    required this.onReservations,
    required this.onInvoices,
  });

  final StaffSession session;
  final VoidCallback onTables;
  final VoidCallback onReservations;
  final VoidCallback onInvoices;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return Scaffold(
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
            onPressed: onTables,
            tooltip: 'Sơ đồ bàn',
            icon: Icon(
              Icons.table_restaurant_outlined,
              color: isDark ? gold : brand,
            ),
          ),
        ],
      ),
      body: ListView(
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
              padding: const EdgeInsets.all(20),
              child: Row(
                children: [
                  Container(
                    width: 54,
                    height: 54,
                    decoration: BoxDecoration(
                      color: isDark
                          ? const Color(0xFF0F172A)
                          : Colors.white.withValues(alpha: 0.12),
                      shape: BoxShape.circle,
                      border: Border.all(color: gold, width: 2),
                    ),
                    child: Center(
                      child: Text(
                        session.fullName.isNotEmpty
                            ? session.fullName[0].toUpperCase()
                            : 'N',
                        style: const TextStyle(
                          color: gold,
                          fontSize: 24,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Xin chào, ${session.fullName}',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 8,
                                vertical: 2,
                              ),
                              decoration: BoxDecoration(
                                color: gold.withValues(alpha: 0.25),
                                borderRadius: BorderRadius.circular(4),
                              ),
                              child: Text(
                                session.role.toUpperCase(),
                                style: const TextStyle(
                                  color: gold,
                                  fontSize: 11,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),
                            const Text(
                              'Đang trong ca làm việc',
                              style: TextStyle(
                                color: Color(0xFFBACEC6),
                                fontSize: 12,
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
          const SizedBox(height: 24),

          // Tiêu đề Thao tác
          Row(
            children: [
              const Icon(Icons.bolt, color: gold, size: 20),
              const SizedBox(width: 6),
              Text(
                'Thao tác phục vụ nhanh',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w900,
                  color: isDark ? gold : brand,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),

          // Lưới Action Responsive
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = constraints.maxWidth >= 900
                  ? 4
                  : constraints.maxWidth >= 600
                  ? 3
                  : 2;
              final actions = [
                _HomeAction(
                  icon: Icons.table_restaurant,
                  title: 'Sơ đồ bàn',
                  subtitle: 'Xem & mở bàn phục vụ',
                  color: const Color(0xFF2E7D32),
                  onTap: onTables,
                ),
                _HomeAction(
                  icon: Icons.event_seat,
                  title: 'Khách đặt bàn',
                  subtitle: 'Tiếp nhận & check-in',
                  color: const Color(0xFF1565C0),
                  onTap: onReservations,
                ),
                _HomeAction(
                  icon: Icons.add_shopping_cart,
                  title: 'Gọi món',
                  subtitle: 'Thêm món & gửi bếp',
                  color: const Color(0xFFB78103),
                  onTap: onTables,
                ),
                _HomeAction(
                  icon: Icons.swap_horiz,
                  title: 'Chuyển / gộp bàn',
                  subtitle: 'Đổi hoặc ghép bàn',
                  color: const Color(0xFF7B1FA2),
                  onTap: onTables,
                ),
                _HomeAction(
                  icon: Icons.payments,
                  title: 'Thanh toán POS',
                  subtitle: 'Tiền mặt, VietQR, điểm',
                  color: const Color(0xFFE65100),
                  onTap: onTables,
                ),
                _HomeAction(
                  icon: Icons.receipt_long,
                  title: 'Hóa đơn gần đây',
                  subtitle: 'Xem lại bill & in lại',
                  color: const Color(0xFF00695C),
                  onTap: onInvoices,
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
                  childAspectRatio: constraints.maxWidth >= 600 ? 1.6 : 1.3,
                ),
                itemBuilder: (_, idx) => actions[idx],
              );
            },
          ),
          const SizedBox(height: 24),

          // Lời nhắc nghiệp vụ chuẩn
          Card(
            color: isDark ? darkSurface : const Color(0xFFF9FBF9),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: gold.withValues(alpha: 0.15),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.tips_and_updates_outlined,
                      color: gold,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Quy trình phục vụ chuẩn',
                          style: TextStyle(
                            fontWeight: FontWeight.w800,
                            color: isDark ? gold : brand,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          'Nhận bàn -> Gọi món -> Bấm "Gửi bếp" -> Nhận thông báo "Bếp báo xong" -> Mang ra bàn bấm "Đã phục vụ" -> Thanh toán.',
                          style: TextStyle(
                            fontSize: 12,
                            color: isDark ? darkTextSecondary : const Color(0xFF6F7E78),
                            height: 1.4,
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
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: color, size: 24),
              ),
              const Spacer(),
              Text(
                title,
                style: TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 14,
                  color: isDark ? darkTextPrimary : brand,
                ),
              ),
              const SizedBox(height: 3),
              Text(
                subtitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 11,
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

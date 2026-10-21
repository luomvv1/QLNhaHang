import 'package:flutter/material.dart';

import '../../core/staff_core.dart';

class TableCard extends StatelessWidget {
  const TableCard({
    super.key,
    required this.table,
    required this.onTap,
    this.onAction,
    this.onTransferMerge,
    this.onCheckIn,
    this.onViewReservation,
  });

  final Map<String, dynamic> table;
  final VoidCallback onTap;
  final VoidCallback? onAction;
  final VoidCallback? onTransferMerge;
  final VoidCallback? onCheckIn;
  final VoidCallback? onViewReservation;

  @override
  Widget build(BuildContext context) {
    final status = text(table['tableStatus']);
    final order = table['activeOrder'] is Map ? jsonMap(table['activeOrder']) : null;
    final reservation = table['reservation'] is Map ? jsonMap(table['reservation']) : null;
    final isServing = status == 'serving' && order != null;
    final capacity = integer(table['capacity']);
    final tableName = text(table['tableName']);
    final areaName = text(table['areaName'], 'Khu chung');

    // Thống kê đơn
    final orderId = order != null ? integer(order['orderId']) : 0;
    final isPrimary = order != null ? (order['isPrimary'] != false) : true;
    final itemCount = order != null ? integer(order['itemCount']) : 0;
    final servedCount = order != null ? integer(order['servedCount']) : 0;
    final readyCount = order != null ? integer(order['readyCount']) : 0;
    final totalAmount = order != null ? number(order['totalAmount'] ?? order['subtotal']) : 0;

    final isDark = isDarkMode(context);

    return Card(
      elevation: 0,
      color: isDark ? darkSurface : Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(
          color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
          width: 1,
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // 1. HEADER: Badge trạng thái + Sức chứa người
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  _buildStatusBadge(status),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.groups_outlined,
                          size: 13,
                          color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                        ),
                        const SizedBox(width: 3),
                        Text(
                          '$capacity',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: isDark ? darkTextSecondary : const Color(0xFF475569),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),

              // 2. TÊN BÀN VÀ KHU VỰC
              Text(
                tableName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                  letterSpacing: -0.2,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                areaName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w500,
                  color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                ),
              ),
              const SizedBox(height: 8),

              // 3. KHUNG THÔNG TIN CHÍNH
              Expanded(
                child: _buildBody(
                  status: status,
                  order: order,
                  reservation: reservation,
                  isServing: isServing,
                  orderId: orderId,
                  isPrimary: isPrimary,
                  readyCount: readyCount,
                  itemCount: itemCount,
                  servedCount: servedCount,
                  totalAmount: totalAmount,
                  isDark: isDark,
                ),
              ),
              const SizedBox(height: 8),

              // 4. HÀNG NÚT CHỨC NĂNG DÙNG NGAY
              _buildActionButtons(
                status: status,
                isDark: isDark,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBody({
    required String status,
    required Map<String, dynamic>? order,
    required Map<String, dynamic>? reservation,
    required bool isServing,
    required int orderId,
    required bool isPrimary,
    required int readyCount,
    required int itemCount,
    required int servedCount,
    required num totalAmount,
    required bool isDark,
  }) {
    if (isServing) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF1E293B) : const Color(0xFFFFFBEB),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: isDark
                ? const Color(0xFFD97706).withValues(alpha: 0.35)
                : const Color(0xFFFDE68A),
          ),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (readyCount > 0)
              Container(
                margin: const EdgeInsets.only(bottom: 3),
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: const Color(0xFFEF4444),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  '🛎️ CÓ $readyCount MÓN ĐÃ XONG!',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 9,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              )
            else
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Đơn #$orderId',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: isDark ? darkTextSecondary : const Color(0xFF78350F),
                    ),
                  ),
                  if (!isPrimary)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                      decoration: BoxDecoration(
                        color: const Color(0xFFDC2626),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: const Text(
                        '🔗 Ghép',
                        style: TextStyle(color: Colors.white, fontSize: 9, fontWeight: FontWeight.bold),
                      ),
                    ),
                ],
              ),
            Text(
              money(totalAmount),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w900,
                color: isDark ? const Color(0xFFFBBF24) : const Color(0xFFD97706),
              ),
            ),
            Text(
              '$itemCount món · Đã bưng $servedCount',
              style: TextStyle(
                fontSize: 10,
                color: isDark ? darkTextSecondary : const Color(0xFF92400E),
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      );
    }

    if (status == 'reserved') {
      final customerName = text(reservation?['customerName'], 'Khách đặt trước');
      final guestCount = integer(reservation?['guestCount']);
      final timeStr = reservation != null ? shortDate(reservation['reservationDate']) : '';
      final note = text(reservation?['note']);

      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF1E293B) : const Color(0xFFEFF6FF),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: isDark
                ? const Color(0xFF3B82F6).withValues(alpha: 0.35)
                : const Color(0xFFBFDBFE),
          ),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.person, size: 13, color: Color(0xFF2563EB)),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    customerName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w800,
                      color: isDark ? const Color(0xFF93C5FD) : const Color(0xFF1E40AF),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 2),
            Row(
              children: [
                const Icon(Icons.access_time, size: 11, color: Color(0xFF64748B)),
                const SizedBox(width: 3),
                Expanded(
                  child: Text(
                    timeStr.isNotEmpty
                        ? '$timeStr · $guestCount khách'
                        : 'Giữ chỗ cho hôm nay',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                      color: isDark ? darkTextSecondary : const Color(0xFF3B82F6),
                    ),
                  ),
                ),
              ],
            ),
            if (note.isNotEmpty) ...[
              const SizedBox(height: 1),
              Text(
                '📝 $note',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontSize: 9,
                  fontStyle: FontStyle.italic,
                  color: Color(0xFFD97706),
                ),
              ),
            ],
          ],
        ),
      );
    }

    if (status == 'empty') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF064E3B).withValues(alpha: 0.25) : const Color(0xFFF0FDF4),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: isDark ? const Color(0xFF059669).withValues(alpha: 0.3) : const Color(0xFFBBF7D0),
          ),
        ),
        child: Center(
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.check_circle_outline, size: 16, color: Color(0xFF16A34A)),
              const SizedBox(width: 6),
              Text(
                'Sẵn sàng đón khách',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: isDark ? const Color(0xFF34D399) : const Color(0xFF15803D),
                ),
              ),
            ],
          ),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF581C87).withValues(alpha: 0.25) : const Color(0xFFFAF5FF),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: isDark ? const Color(0xFF9333EA).withValues(alpha: 0.3) : const Color(0xFFE9D5FF),
        ),
      ),
      child: Center(
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.cleaning_services_outlined, size: 16, color: Color(0xFF9333EA)),
            const SizedBox(width: 6),
            Text(
              'Đang dọn vệ sinh',
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w700,
                color: isDark ? const Color(0xFFC084FC) : const Color(0xFF7E22CE),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActionButtons({
    required String status,
    required bool isDark,
  }) {
    if (status == 'serving') {
      return Row(
        children: [
          // Nút Vào Gọi Món
          Expanded(
            flex: 3,
            child: SizedBox(
              height: 34,
              child: FilledButton.icon(
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFFFAB005),
                  foregroundColor: const Color(0xFF1E293B),
                  padding: EdgeInsets.zero,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                onPressed: onTap,
                icon: const Icon(Icons.restaurant_menu, size: 14),
                label: const Text(
                  'Gọi Món',
                  style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800),
                ),
              ),
            ),
          ),
          const SizedBox(width: 6),
          // Nút Chuyển / Gộp bàn trực tiếp!
          Expanded(
            flex: 3,
            child: SizedBox(
              height: 34,
              child: OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  foregroundColor: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                  side: BorderSide(
                    color: isDark ? const Color(0xFF475569) : const Color(0xFFCBD5E1),
                  ),
                  padding: EdgeInsets.zero,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                onPressed: onTransferMerge ?? onAction ?? onTap,
                icon: const Icon(Icons.swap_horiz_rounded, size: 15, color: Color(0xFF2563EB)),
                label: const Text(
                  'Chuyển/Gộp',
                  style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ),
        ],
      );
    }

    if (status == 'reserved') {
      return Row(
        children: [
          // Nút Đón khách (Check-in)
          Expanded(
            flex: 3,
            child: SizedBox(
              height: 34,
              child: FilledButton.icon(
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFF2563EB),
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.zero,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                onPressed: onCheckIn ?? onTap,
                icon: const Icon(Icons.login_rounded, size: 14),
                label: const Text(
                  'Đón Khách',
                  style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800),
                ),
              ),
            ),
          ),
          const SizedBox(width: 6),
          // Nút Chi tiết đặt bàn
          Expanded(
            flex: 2,
            child: SizedBox(
              height: 34,
              child: OutlinedButton(
                style: OutlinedButton.styleFrom(
                  foregroundColor: const Color(0xFF2563EB),
                  side: const BorderSide(color: Color(0xFF93C5FD)),
                  padding: EdgeInsets.zero,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
                onPressed: onViewReservation ?? onAction ?? onTap,
                child: const Text(
                  'Chi Tiết',
                  style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ),
        ],
      );
    }

    if (status == 'empty') {
      return SizedBox(
        height: 34,
        child: FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: const Color(0xFF16A34A),
            foregroundColor: Colors.white,
            padding: EdgeInsets.zero,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          ),
          onPressed: onTap,
          icon: const Icon(Icons.add_rounded, size: 16),
          label: const Text(
            'Mở Bàn Phục Vụ',
            style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800),
          ),
        ),
      );
    }

    return SizedBox(
      height: 34,
      child: FilledButton.icon(
        style: FilledButton.styleFrom(
          backgroundColor: const Color(0xFF9333EA),
          foregroundColor: Colors.white,
          padding: EdgeInsets.zero,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        onPressed: onTap,
        icon: const Icon(Icons.check_rounded, size: 16),
        label: const Text(
          'Đã Dọn Xong ✓',
          style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800),
        ),
      ),
    );
  }

  Widget _buildStatusBadge(String status) {
    if (status == 'serving') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
        decoration: BoxDecoration(
          color: const Color(0xFFFEF3C7),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFFDE68A)),
        ),
        child: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('🟡', style: TextStyle(fontSize: 8)),
            SizedBox(width: 4),
            Text(
              'Đang phục vụ',
              style: TextStyle(fontSize: 10, fontWeight: FontWeight.w800, color: Color(0xFF92400E)),
            ),
          ],
        ),
      );
    }
    if (status == 'empty') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
        decoration: BoxDecoration(
          color: const Color(0xFFDCFCE7),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFBBF7D0)),
        ),
        child: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('🟢', style: TextStyle(fontSize: 8)),
            SizedBox(width: 4),
            Text(
              'Bàn trống',
              style: TextStyle(fontSize: 10, fontWeight: FontWeight.w800, color: Color(0xFF166534)),
            ),
          ],
        ),
      );
    }
    if (status == 'reserved') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
        decoration: BoxDecoration(
          color: const Color(0xFFDBEAFE),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFFBFDBFE)),
        ),
        child: const Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('🔵', style: TextStyle(fontSize: 8)),
            SizedBox(width: 4),
            Text(
              'Đã đặt trước',
              style: TextStyle(fontSize: 10, fontWeight: FontWeight.w800, color: Color(0xFF1E40AF)),
            ),
          ],
        ),
      );
    }
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
      decoration: BoxDecoration(
        color: const Color(0xFFF3E8FF),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFE9D5FF)),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text('🟣', style: TextStyle(fontSize: 8)),
          SizedBox(width: 4),
          Text(
            'Đang dọn dẹp',
            style: TextStyle(fontSize: 10, fontWeight: FontWeight.w800, color: Color(0xFF6B21A8)),
          ),
        ],
      ),
    );
  }
}

/// Sheet Tác vụ nhanh cho Bàn phục vụ (Chuyển bàn, Ghép bàn, Gộp order, Thanh toán)
class TableActionSheet extends StatelessWidget {
  const TableActionSheet({
    super.key,
    required this.table,
    required this.onSwitch,
    required this.onLink,
    required this.onMerge,
    required this.onPay,
    required this.onCancel,
  });

  final Map<String, dynamic> table;
  final VoidCallback onSwitch;
  final VoidCallback onLink;
  final VoidCallback onMerge;
  final VoidCallback onPay;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final tableName = text(table['tableName']);
    final areaName = text(table['areaName'], 'Khu chung');
    final order = table['activeOrder'] is Map ? jsonMap(table['activeOrder']) : null;
    final orderId = order != null ? integer(order['orderId']) : 0;
    final totalAmount = order != null ? number(order['totalAmount'] ?? order['subtotal']) : 0;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Header
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: isDark ? const Color(0xFF1E293B) : const Color(0xFFEFF6FF),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.table_restaurant, color: Color(0xFF2563EB), size: 24),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '$tableName · $areaName',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                          color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                        ),
                      ),
                      if (orderId > 0)
                        Text(
                          'Đơn #$orderId · Tạm tính: ${money(totalAmount)}',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: isDark ? gold : const Color(0xFFD97706),
                          ),
                        ),
                    ],
                  ),
                ),
                IconButton(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.close),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Text(
              'TÁC VỤ BÀN PHỤC VỤ',
              style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: Color(0xFF64748B), letterSpacing: 0.5),
            ),
            const SizedBox(height: 8),

            // Option 1: Chuyển bàn
            _ActionTile(
              icon: Icons.swap_horiz_rounded,
              iconColor: const Color(0xFF2563EB),
              bgColor: const Color(0xFFEFF6FF),
              title: 'Chuyển bàn phục vụ',
              subtitle: 'Chuyển toàn bộ order sang một bàn trống khác',
              onTap: onSwitch,
              isDark: isDark,
            ),
            const SizedBox(height: 8),

            // Option 2: Ghép thêm bàn trống
            _ActionTile(
              icon: Icons.call_merge_rounded,
              iconColor: const Color(0xFFD97706),
              bgColor: const Color(0xFFFFFBEB),
              title: 'Ghép thêm bàn trống',
              subtitle: 'Ghép thêm bàn vào đơn này để phục vụ đoàn đông',
              onTap: onLink,
              isDark: isDark,
            ),
            const SizedBox(height: 8),

            // Option 3: Gộp 2 order
            _ActionTile(
              icon: Icons.merge_type_rounded,
              iconColor: const Color(0xFF7C3AED),
              bgColor: const Color(0xFFF5F3FF),
              title: 'Gộp hai order bàn lại làm một',
              subtitle: 'Gộp chung món của bàn này vào một bàn đang ăn khác',
              onTap: onMerge,
              isDark: isDark,
            ),
            const SizedBox(height: 8),

            // Option 4: Thanh toán nhanh
            _ActionTile(
              icon: Icons.payments_rounded,
              iconColor: const Color(0xFF16A34A),
              bgColor: const Color(0xFFF0FDF4),
              title: 'Thanh toán hóa đơn ngay',
              subtitle: 'Mở trang thanh toán tiền và in hóa đơn cho bàn này',
              onTap: onPay,
              isDark: isDark,
            ),
            const SizedBox(height: 8),

            // Option 5: Hủy order
            _ActionTile(
              icon: Icons.cancel_outlined,
              iconColor: const Color(0xFFDC2626),
              bgColor: const Color(0xFFFEF2F2),
              title: 'Hủy order bàn này',
              subtitle: 'Chỉ hủy được khi các món chưa bắt đầu nấu',
              onTap: onCancel,
              danger: true,
              isDark: isDark,
            ),
          ],
        ),
      ),
    );
  }
}

/// Sheet Chi tiết Đặt Bàn & Check-in / Giải phóng bàn
class ReservedDetailSheet extends StatelessWidget {
  const ReservedDetailSheet({
    super.key,
    required this.table,
    required this.onCheckIn,
    required this.onRelease,
  });

  final Map<String, dynamic> table;
  final VoidCallback onCheckIn;
  final VoidCallback onRelease;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final tableName = text(table['tableName']);
    final areaName = text(table['areaName'], 'Khu chung');
    final reservation = table['reservation'] is Map ? jsonMap(table['reservation']) : null;
    final customerName = text(reservation?['customerName'], 'Khách đặt trước');
    final customerPhone = text(reservation?['customerPhone'], '—');
    final guestCount = integer(reservation?['guestCount'] ?? table['capacity']);
    final dateStr = shortDate(reservation?['reservationDate']);
    final note = text(reservation?['note']);

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Header
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: const Color(0xFF2563EB).withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.event_seat_rounded, color: Color(0xFF2563EB), size: 24),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        tableName,
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w900,
                          color: isDark ? darkTextPrimary : const Color(0xFF0F172A),
                        ),
                      ),
                      Text(
                        'Khu vực: $areaName',
                        style: TextStyle(
                          fontSize: 12,
                          color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: const Color(0xFFDBEAFE),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: const Text(
                    'ĐÃ ĐẶT TRƯỚC',
                    style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: Color(0xFF1E40AF)),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // Card chi tiết khách đặt
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _InfoRow(
                    icon: Icons.person_rounded,
                    label: 'Khách hàng',
                    value: customerName,
                    isDark: isDark,
                  ),
                  const SizedBox(height: 8),
                  _InfoRow(
                    icon: Icons.phone_rounded,
                    label: 'Số điện thoại',
                    value: customerPhone,
                    isDark: isDark,
                  ),
                  const SizedBox(height: 8),
                  _InfoRow(
                    icon: Icons.groups_rounded,
                    label: 'Số lượng khách',
                    value: '$guestCount người',
                    isDark: isDark,
                  ),
                  const SizedBox(height: 8),
                  _InfoRow(
                    icon: Icons.access_time_filled_rounded,
                    label: 'Thời gian đặt',
                    value: dateStr,
                    isDark: isDark,
                  ),
                  if (note.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    _InfoRow(
                      icon: Icons.note_alt_rounded,
                      label: 'Ghi chú',
                      value: note,
                      isDark: isDark,
                      isNote: true,
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Action Button 1: Đón khách vào bàn (Check-in)
            SizedBox(
              height: 48,
              child: FilledButton.icon(
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFF2563EB),
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
                onPressed: onCheckIn,
                icon: const Icon(Icons.login_rounded, size: 20),
                label: const Text(
                  'Đón Khách Vào Bàn (Check-in)',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.w800),
                ),
              ),
            ),
            const SizedBox(height: 10),

            // Action Button 2: Hủy giữ chỗ / giải phóng bàn
            SizedBox(
              height: 44,
              child: OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  foregroundColor: const Color(0xFFDC2626),
                  side: const BorderSide(color: Color(0xFFFCA5A5)),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
                onPressed: onRelease,
                icon: const Icon(Icons.free_cancellation_rounded, size: 18),
                label: const Text(
                  'Khách không đến / Hủy giữ chỗ',
                  style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ActionTile extends StatelessWidget {
  const _ActionTile({
    required this.icon,
    required this.iconColor,
    required this.bgColor,
    required this.title,
    required this.subtitle,
    required this.onTap,
    required this.isDark,
    this.danger = false,
  });

  final IconData icon;
  final Color iconColor;
  final Color bgColor;
  final String title;
  final String subtitle;
  final VoidCallback onTap;
  final bool isDark;
  final bool danger;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          color: isDark ? const Color(0xFF1E293B) : Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: danger
                ? const Color(0xFFFCA5A5)
                : isDark
                ? const Color(0xFF334155)
                : const Color(0xFFE2E8F0),
          ),
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: isDark ? iconColor.withValues(alpha: 0.2) : bgColor,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, color: iconColor, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                      color: danger
                          ? const Color(0xFFDC2626)
                          : isDark
                          ? darkTextPrimary
                          : const Color(0xFF0F172A),
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    style: TextStyle(
                      fontSize: 11,
                      color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                    ),
                  ),
                ],
              ),
            ),
            Icon(
              Icons.chevron_right_rounded,
              color: isDark ? darkTextSecondary : const Color(0xFF94A3B8),
              size: 20,
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
    required this.isDark,
    this.isNote = false,
  });

  final IconData icon;
  final String label;
  final String value;
  final bool isDark;
  final bool isNote;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: isNote ? CrossAxisAlignment.start : CrossAxisAlignment.center,
      children: [
        Icon(icon, size: 16, color: const Color(0xFF64748B)),
        const SizedBox(width: 8),
        SizedBox(
          width: 95,
          child: Text(
            label,
            style: TextStyle(
              fontSize: 12,
              color: isDark ? darkTextSecondary : const Color(0xFF64748B),
            ),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: isNote
                  ? const Color(0xFFD97706)
                  : isDark
                  ? darkTextPrimary
                  : const Color(0xFF0F172A),
            ),
          ),
        ),
      ],
    );
  }
}

class OrderSummary extends StatelessWidget {
  const OrderSummary({super.key, required this.table, required this.order});
  final Map<String, dynamic> table;
  final Map<String, dynamic> order;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Order #${integer(order['orderId'])} · ${text(table['tableName'])}',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w900,
                      color: isDark ? gold : brand,
                    ),
                  ),
                ),
                StatusPill(status: text(order['orderStatus'])),
              ],
            ),
            const SizedBox(height: 6),
            Text(
              'Mở lúc ${shortDate(order['createdAt'])} · Phục vụ: ${text(order['createdByName'], 'Nhân viên')}',
              style: TextStyle(
                fontSize: 12,
                color: isDark ? darkTextSecondary : const Color(0xFF6E7C77),
              ),
            ),
            Divider(height: 20, color: themedBorder(context)),
            Row(
              children: [
                Text(
                  'Tạm tính hiện tại',
                  style: TextStyle(
                    color: isDark ? darkTextSecondary : const Color(0xFF5A6E66),
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const Spacer(),
                Text(
                  money(number(order['subtotal'])),
                  style: TextStyle(
                    fontWeight: FontWeight.w900,
                    fontSize: 16,
                    color: isDark ? gold : brand,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class OrderItem extends StatelessWidget {
  const OrderItem({
    super.key,
    required this.item,
    this.onServe,
    this.onEdit,
    this.onDelete,
  });

  final Map<String, dynamic> item;
  final VoidCallback? onServe;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);

    return Card(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(14, 12, 10, 10),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    text(item['foodNameSnapshot']),
                    style: TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 14,
                      color: isDark ? darkTextPrimary : brand,
                    ),
                  ),
                  if (text(item['variantSnapshot']).isNotEmpty &&
                      text(item['variantSnapshot']) != 'Mặc định')
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        'Biến thể: ${text(item['variantSnapshot'])}',
                        style: TextStyle(
                          fontSize: 12,
                          color: isDark ? darkTextSecondary : const Color(0xFF6D7B76),
                        ),
                      ),
                    ),
                  if (text(item['note']).isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        '📝 ${text(item['note'])}',
                        style: const TextStyle(
                          fontSize: 12,
                          color: gold,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      StatusPill(status: text(item['itemStatus'])),
                      const SizedBox(width: 8),
                      Text(
                        '${integer(item['quantity'])} × ${money(number(item['unitPrice']))}',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: isDark ? darkTextSecondary : const Color(0xFF697972),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  money(number(item['lineTotal'])),
                  style: TextStyle(
                    fontWeight: FontWeight.w800,
                    fontSize: 14,
                    color: isDark ? gold : brand,
                  ),
                ),
                const SizedBox(height: 6),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (onEdit != null)
                      IconButton(
                        onPressed: onEdit,
                        tooltip: 'Chỉnh sửa món',
                        icon: Icon(
                          Icons.edit_outlined,
                          color: isDark ? gold : brand,
                          size: 20,
                        ),
                      ),
                  if (onServe != null)
                    FilledButton.tonal(
                      style: FilledButton.styleFrom(
                        backgroundColor: const Color(0xFFE8F5E9),
                        foregroundColor: const Color(0xFF2E7D32),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 6,
                        ),
                        minimumSize: Size.zero,
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      ),
                      onPressed: onServe,
                      child: const Text(
                        'Đã phục vụ ✓',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                  if (onDelete != null)
                    IconButton(
                      onPressed: onDelete,
                      tooltip: 'Hủy món',
                      icon: const Icon(
                        Icons.delete_outline,
                        color: Color(0xFFC62828),
                        size: 20,
                      ),
                    ),
                ],
              ),
            ],
          ),
        ],
      ),
    ),
  );
  }
}

class EditOrderItemSheet extends StatefulWidget {
  const EditOrderItemSheet({
    super.key,
    required this.item,
    this.variants = const [],
  });

  final Map<String, dynamic> item;
  final List<Map<String, dynamic>> variants;

  @override
  State<EditOrderItemSheet> createState() => _EditOrderItemSheetState();
}

class _EditOrderItemSheetState extends State<EditOrderItemSheet> {
  late int quantity = integer(widget.item['quantity']).clamp(1, 999);
  late final TextEditingController note = TextEditingController(
    text: text(widget.item['note']),
  );
  Map<String, dynamic>? selectedVariant;

  @override
  void initState() {
    super.initState();
    if (widget.variants.isNotEmpty) {
      final currentVarId = integer(widget.item['variantId']);
      selectedVariant = widget.variants.firstWhere(
        (v) => integer(v['variantId']) == currentVarId,
        orElse: () => widget.variants.first,
      );
    }
  }

  @override
  void dispose() {
    note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final unitPrice = selectedVariant != null
        ? number(selectedVariant!['price'])
        : number(widget.item['unitPrice']);

    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
          20,
          8,
          20,
          24 + MediaQuery.viewInsetsOf(context).bottom,
        ),
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
                      color: brand.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: const Icon(Icons.edit_note, color: brand, size: 22),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Chỉnh sửa · ${text(widget.item['foodNameSnapshot'])}',
                          style: const TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 16,
                            color: brand,
                          ),
                        ),
                        Text(
                          'Đơn giá: ${money(unitPrice)}',
                          style: const TextStyle(
                            fontSize: 12,
                            color: Color(0xFF6F7E78),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // CHỌN LỰA BIẾN THỂ / SIZE (nếu có biến thể)
              if (widget.variants.isNotEmpty) ...[
                DropdownButtonFormField<Map<String, dynamic>>(
                  initialValue: selectedVariant,
                  decoration: const InputDecoration(
                    labelText: 'Kích cỡ / Biến thể món',
                    prefixIcon: Icon(Icons.straighten_outlined),
                  ),
                  items: widget.variants
                      .map(
                        (v) => DropdownMenuItem(
                          value: v,
                          child: Text(
                            '${text(v['variantName'])} · ${money(number(v['price']))}',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: (val) {
                    if (val != null) {
                      setState(() => selectedVariant = val);
                    }
                  },
                ),
                const SizedBox(height: 14),
              ],

              // Chỉnh số lượng
              Row(
                children: [
                  const Text(
                    'Số lượng phần:',
                    style: TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
                  ),
                  const Spacer(),
                  IconButton(
                    onPressed: quantity > 1
                        ? () => setState(() => quantity--)
                        : null,
                    icon: const Icon(Icons.remove_circle, color: gold, size: 28),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 10),
                    child: Text(
                      '$quantity',
                      style: const TextStyle(
                        fontWeight: FontWeight.w900,
                        fontSize: 18,
                        color: brand,
                      ),
                    ),
                  ),
                  IconButton(
                    onPressed: () => setState(() => quantity++),
                    icon: const Icon(Icons.add_circle, color: brand, size: 28),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Chỉnh ghi chú
              TextField(
                controller: note,
                maxLines: 2,
                decoration: const InputDecoration(
                  labelText: 'Ghi chú chế biến gửi bếp',
                  hintText: 'Ví dụ: ít cay, không hành, làm chín kĩ...',
                  prefixIcon: Icon(Icons.notes),
                ),
              ),
              const SizedBox(height: 20),

              FilledButton.icon(
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(48),
                ),
                onPressed: () => Navigator.pop(context, {
                  'variantId': selectedVariant != null
                      ? integer(selectedVariant!['variantId'])
                      : integer(widget.item['variantId']),
                  'quantity': quantity,
                  'note': note.text.trim(),
                }),
                icon: const Icon(Icons.check),
                label: Text(
                  'Lưu Thay Đổi · ${money(unitPrice * quantity)}',
                  style: const TextStyle(
                    fontWeight: FontWeight.w900,
                    fontSize: 15,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class MenuTab extends StatefulWidget {
  const MenuTab({
    super.key,
    required this.categories,
    required this.foods,
    required this.selectedCategory,
    required this.onCategory,
    required this.onFood,
  });

  final List<Map<String, dynamic>> categories;
  final List<Map<String, dynamic>> foods;
  final String? selectedCategory;
  final ValueChanged<String?> onCategory;
  final ValueChanged<Map<String, dynamic>> onFood;

  @override
  State<MenuTab> createState() => _MenuTabState();
}

class _MenuTabState extends State<MenuTab> {
  final TextEditingController searchCtrl = TextEditingController();
  String searchQuery = '';

  @override
  void dispose() {
    searchCtrl.dispose();
    super.dispose();
  }

  void _openCategoryPicker() {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 4, 16, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(Icons.category_outlined, color: brand, size: 20),
                  SizedBox(width: 8),
                  Text(
                    'Chọn danh mục món ăn',
                    style: TextStyle(
                      fontSize: 17,
                      fontWeight: FontWeight.w900,
                      color: brand,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              ConstrainedBox(
                constraints: const BoxConstraints(maxHeight: 340),
                child: ListView(
                  shrinkWrap: true,
                  children: [
                    ListTile(
                      leading: const Icon(Icons.restaurant_menu, color: brand),
                      title: const Text(
                        'Tất cả danh mục',
                        style: TextStyle(fontWeight: FontWeight.w700),
                      ),
                      trailing: widget.selectedCategory == null
                          ? const Icon(Icons.check, color: gold)
                          : null,
                      onTap: () {
                        widget.onCategory(null);
                        Navigator.pop(ctx);
                      },
                    ),
                    const Divider(height: 1, color: cardBorder),
                    ...widget.categories.map((cat) {
                      final id = '${cat['categoryId']}';
                      final isSelected = widget.selectedCategory == id;
                      final count = widget.foods
                          .where((f) => '${f['categoryId']}' == id)
                          .length;
                      return ListTile(
                        leading: const Icon(
                          Icons.lunch_dining_outlined,
                          color: brand,
                        ),
                        title: Text(
                          text(cat['categoryName']),
                          style: TextStyle(
                            fontWeight: isSelected
                                ? FontWeight.w900
                                : FontWeight.w600,
                            color: isSelected ? brand : null,
                          ),
                        ),
                        subtitle: Text(
                          '$count món',
                          style: const TextStyle(fontSize: 12),
                        ),
                        trailing: isSelected
                            ? const Icon(Icons.check, color: gold)
                            : null,
                        onTap: () {
                          widget.onCategory(id);
                          Navigator.pop(ctx);
                        },
                      );
                    }),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    // 1. Lọc theo danh mục
    var visible = widget.selectedCategory == null
        ? widget.foods
        : widget.foods
              .where(
                (food) => '${food['categoryId']}' == widget.selectedCategory,
              )
              .toList();

    // 2. Lọc theo từ khóa tìm kiếm (hỗ trợ không dấu/hoa thường)
    if (searchQuery.trim().isNotEmpty) {
      final q = searchQuery.trim().toLowerCase();
      visible = visible.where((food) {
        final name = text(food['foodName']).toLowerCase();
        final desc = text(food['description']).toLowerCase();
        return name.contains(q) || desc.contains(q);
      }).toList();
    }

    final selectedCatName = widget.selectedCategory == null
        ? 'Tất cả món'
        : text(
            widget.categories
                .firstWhere(
                  (c) => '${c['categoryId']}' == widget.selectedCategory,
                  orElse: () => {'categoryName': 'Danh mục'},
                )['categoryName'],
            'Danh mục',
          );

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // THANH TÌM KIẾM & NÚT DANH MỤC THU GỌN
        Row(
          children: [
            // Ô TÌM KIẾM TÊN MÓN
            Expanded(
              child: TextField(
                controller: searchCtrl,
                onChanged: (val) => setState(() => searchQuery = val),
                decoration: InputDecoration(
                  hintText: 'Tìm tên món ăn...',
                  prefixIcon: const Icon(Icons.search, size: 20),
                  suffixIcon: searchQuery.isNotEmpty
                      ? IconButton(
                          icon: const Icon(Icons.clear, size: 18),
                          onPressed: () {
                            searchCtrl.clear();
                            setState(() => searchQuery = '');
                          },
                        )
                      : null,
                ),
              ),
            ),
            const SizedBox(width: 10),

            // NÚT DANH MỤC THU GỌN
            OutlinedButton.icon(
              style: OutlinedButton.styleFrom(
                backgroundColor: widget.selectedCategory != null
                    ? gold.withValues(alpha: 0.15)
                    : Colors.white,
                side: BorderSide(
                  color: widget.selectedCategory != null ? gold : cardBorder,
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 12,
                ),
              ),
              onPressed: _openCategoryPicker,
              icon: Icon(
                Icons.category,
                size: 18,
                color: widget.selectedCategory != null ? gold : brand,
              ),
              label: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 90),
                    child: Text(
                      selectedCatName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w800,
                        color: widget.selectedCategory != null ? brand : null,
                      ),
                    ),
                  ),
                  const SizedBox(width: 4),
                  const Icon(Icons.arrow_drop_down, size: 18),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),

        // HÀNG THỐNG KÊ KẾT QUẢ & NÚT BỎ LỌC
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              'Tìm thấy ${visible.length} món ăn',
              style: const TextStyle(
                fontSize: 12,
                color: Color(0xFF6F7E78),
                fontWeight: FontWeight.w600,
              ),
            ),
            if (widget.selectedCategory != null || searchQuery.isNotEmpty)
              InkWell(
                onTap: () {
                  searchCtrl.clear();
                  setState(() => searchQuery = '');
                  widget.onCategory(null);
                },
                child: const Padding(
                  padding: EdgeInsets.symmetric(vertical: 4, horizontal: 6),
                  child: Text(
                    'Xóa bộ lọc',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFFC62828),
                    ),
                  ),
                ),
              ),
          ],
        ),
        const SizedBox(height: 10),

        // DANH SÁCH MÓN ĂN
        if (visible.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 40),
            child: Center(
              child: Column(
                children: [
                  Icon(
                    Icons.search_off,
                    size: 40,
                    color: Color(0xFFB0BEC5),
                  ),
                  SizedBox(height: 10),
                  Text(
                    'Không tìm thấy món ăn nào phù hợp.',
                    style: TextStyle(color: Color(0xFF78909C)),
                  ),
                ],
              ),
            ),
          )
        else
          ...visible.map((food) {
            final variants = jsonMaps(food['variants']);
            final minPrice = variants.isNotEmpty
                ? number(variants.first['price'])
                : 0;
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Card(
                child: ListTile(
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 6,
                  ),
                  leading: ClipRRect(
                    borderRadius: BorderRadius.circular(10),
                    child: text(food['imageUrl']).isNotEmpty
                        ? Image.network(
                            text(food['imageUrl']),
                            width: 46,
                            height: 46,
                            fit: BoxFit.cover,
                            errorBuilder: (_, _, _) => Container(
                              width: 46,
                              height: 46,
                              decoration: BoxDecoration(
                                color: brand.withValues(alpha: 0.08),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: const Icon(
                                Icons.fastfood,
                                color: brand,
                                size: 24,
                              ),
                            ),
                          )
                        : Container(
                            width: 46,
                            height: 46,
                            decoration: BoxDecoration(
                              color: brand.withValues(alpha: 0.08),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: const Icon(
                              Icons.fastfood,
                              color: brand,
                              size: 24,
                            ),
                          ),
                  ),
                  title: Text(
                    text(food['foodName']),
                    style: const TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 14,
                      color: brand,
                    ),
                  ),
                  subtitle: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (text(food['description']).isNotEmpty)
                        Text(
                          text(food['description']),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 11,
                            color: Color(0xFF6F7E78),
                          ),
                        ),
                      const SizedBox(height: 2),
                      Text(
                        minPrice > 0 ? money(minPrice) : 'Xem giá',
                        style: const TextStyle(
                          fontWeight: FontWeight.w900,
                          fontSize: 13,
                          color: gold,
                        ),
                      ),
                    ],
                  ),
                  trailing: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 6,
                    ),
                    decoration: BoxDecoration(
                      color: brand.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.add, size: 16, color: brand),
                        SizedBox(width: 4),
                        Text(
                          'Chọn',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w800,
                            color: brand,
                          ),
                        ),
                      ],
                    ),
                  ),
                  onTap: () => widget.onFood(food),
                ),
              ),
            );
          }),
      ],
    );
  }
}

class PaymentSummary extends StatelessWidget {
  const PaymentSummary({
    super.key,
    required this.preview,
    required this.items,
    required this.subtotal,
    required this.voucherDiscount,
    required this.pointDiscount,
    required this.total,
  });
  final Map<String, dynamic> preview;
  final List<Map<String, dynamic>> items;
  final num subtotal;
  final num voucherDiscount;
  final num pointDiscount;
  final num total;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.receipt_long, color: brand, size: 20),
              const SizedBox(width: 8),
              Text(
                'Tạm tính · ${text(preview['tableDisplay'])}',
                style: const TextStyle(
                  fontWeight: FontWeight.w900,
                  fontSize: 15,
                  color: brand,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...items.map(
            (item) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      '${text(item['foodNameSnapshot'])} × ${integer(item['quantity'])}',
                      style: const TextStyle(fontSize: 13),
                    ),
                  ),
                  Text(
                    money(number(item['lineTotal'])),
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
            ),
          ),
          const Divider(height: 20, color: cardBorder),
          PriceRow(label: 'Tạm tính món', amount: subtotal),
          if (voucherDiscount > 0)
            PriceRow(label: 'Ưu đãi Voucher', amount: -voucherDiscount),
          if (pointDiscount > 0)
            PriceRow(label: 'Đổi điểm thưởng', amount: -pointDiscount),
          const Divider(height: 18, color: cardBorder),
          PriceRow(label: 'TỔNG PHẢI TRẢ', amount: total, emphasis: true),
        ],
      ),
    ),
  );
}

class PriceRow extends StatelessWidget {
  const PriceRow({
    super.key,
    required this.label,
    required this.amount,
    this.emphasis = false,
  });
  final String label;
  final num amount;
  final bool emphasis;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 6),
    child: Row(
      children: [
        Text(
          label,
          style: TextStyle(
            fontWeight: emphasis ? FontWeight.w900 : FontWeight.w600,
            fontSize: emphasis ? 15 : 13,
            color: emphasis ? brand : const Color(0xFF5A6E66),
          ),
        ),
        const Spacer(),
        Text(
          '${amount < 0 ? '-' : ''}${money(amount.abs())}',
          style: TextStyle(
            fontWeight: FontWeight.w900,
            fontSize: emphasis ? 18 : 13,
            color: emphasis
                ? gold
                : amount < 0
                ? const Color(0xFFC62828)
                : const Color(0xFF1B2A24),
          ),
        ),
      ],
    ),
  );
}

class StatusPill extends StatelessWidget {
  const StatusPill({super.key, required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    final color = itemColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .12),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        itemStatusText(status),
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w800,
          color: color,
        ),
      ),
    );
  }
}

class ActionRow extends StatelessWidget {
  const ActionRow({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
    this.danger = false,
  });
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;
  final bool danger;

  @override
  Widget build(BuildContext context) => ListTile(
    leading: Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: (danger ? const Color(0xFFC62828) : brand).withValues(
          alpha: 0.1,
        ),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Icon(icon, color: danger ? const Color(0xFFC62828) : brand),
    ),
    title: Text(
      title,
      style: TextStyle(
        fontWeight: FontWeight.w700,
        color: danger ? const Color(0xFFC62828) : brand,
      ),
    ),
    subtitle: Text(subtitle, style: const TextStyle(fontSize: 12)),
    onTap: onTap,
  );
}

class TablePicker extends StatelessWidget {
  const TablePicker({super.key, required this.title, required this.tables});
  final String title;
  final List<Map<String, dynamic>> tables;

  @override
  Widget build(BuildContext context) => SafeArea(
    child: Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w800,
              color: brand,
            ),
          ),
          const SizedBox(height: 12),
          if (tables.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Center(
                child: Text(
                  'Không có bàn phù hợp.',
                  style: TextStyle(color: Color(0xFF8A9992)),
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
                    leading: const Icon(
                      Icons.table_restaurant_outlined,
                      color: brand,
                    ),
                    title: Text(
                      text(table['tableName']),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    subtitle: Text(
                      '${text(table['areaName'], 'Khu chung')} · ${integer(table['capacity'])} chỗ',
                    ),
                    onTap: () => Navigator.pop(context, table),
                  );
                },
              ),
            ),
        ],
      ),
    ),
  );
}

class EmptyState extends StatelessWidget {
  const EmptyState({super.key, this.message});
  final String? message;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 40),
    child: Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: brand.withValues(alpha: 0.06),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.restaurant_menu_outlined,
              size: 36,
              color: Color(0xFF8A9992),
            ),
          ),
          const SizedBox(height: 12),
          Text(
            message ?? 'Chưa có dữ liệu nào.',
            style: const TextStyle(color: Color(0xFF6F7E78), fontSize: 13),
          ),
        ],
      ),
    ),
  );
}

class RetryState extends StatelessWidget {
  const RetryState({super.key, required this.message, required this.onRetry});
  final String message;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_outlined,
            size: 46,
            color: Color(0xFF8A9992),
          ),
          const SizedBox(height: 14),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(color: Color(0xFF5A6E66), fontSize: 14),
          ),
          const SizedBox(height: 16),
          FilledButton.icon(
            onPressed: onRetry,
            icon: const Icon(Icons.refresh, size: 18),
            label: const Text('Thử lại'),
          ),
        ],
      ),
    ),
  );
}

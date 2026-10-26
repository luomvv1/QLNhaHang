import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import '../staff/shared_widgets.dart';
import 'invoice_detail_page.dart';

class InvoicesPage extends StatefulWidget {
  const InvoicesPage({
    super.key,
    required this.api,
    this.session,
  });

  final ApiClient api;
  final StaffSession? session;

  @override
  State<InvoicesPage> createState() => _InvoicesPageState();
}

class _InvoicesPageState extends State<InvoicesPage> {
  List<Map<String, dynamic>> invoices = [];
  bool loading = true;
  String? error;

  final TextEditingController searchController = TextEditingController();
  String searchQuery = '';

  // Bộ lọc tinh gọn
  DateTime? selectedDate = DateTime.now(); // Mặc định là Hôm nay
  String activePayment = 'all'; // 'all' | 'cash' | 'transfer'

  @override
  void initState() {
    super.initState();
    searchController.addListener(() {
      final q = searchController.text.trim().toLowerCase();
      if (q != searchQuery) {
        setState(() => searchQuery = q);
      }
    });
    load();
  }

  @override
  void dispose() {
    searchController.dispose();
    super.dispose();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final values = jsonMaps(
        await widget.api.get(
          '/staff/payment/recent-invoices',
          query: {'limit': '100'},
        ),
      );
      if (mounted) setState(() => invoices = values);
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } catch (_) {
      if (mounted) setState(() => error = 'Không thể kết nối máy chủ');
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  bool _isSameDay(DateTime a, DateTime b) {
    return a.year == b.year && a.month == b.month && a.day == b.day;
  }

  // Danh sách đã lọc theo các tiêu chí tinh gọn
  List<Map<String, dynamic>> get filteredList {
    return invoices.where((inv) {
      // 1. Lọc theo phương thức
      final method = text(inv['paymentMethod'], 'cash').toLowerCase();
      if (activePayment == 'cash' && method != 'cash') return false;
      if (activePayment == 'transfer' &&
          method != 'transfer' &&
          method != 'vietqr' &&
          method != 'bank') {
        return false;
      }

      // 2. Lọc theo ngày được chọn
      if (selectedDate != null) {
        final issued = parseDateTime(inv['issuedAt']);
        if (issued == null) return false;
        if (!_isSameDay(issued, selectedDate!)) return false;
      }

      // 3. Lọc theo từ khóa tìm kiếm
      if (searchQuery.isNotEmpty) {
        final code = text(inv['invoiceCode']).toLowerCase();
        final table = text(inv['tableDisplay']).toLowerCase();
        final customer = text(inv['customerName']).toLowerCase();
        final phone = text(inv['customerPhone']).toLowerCase();
        final cashier = text(inv['cashierName']).toLowerCase();

        final match = code.contains(searchQuery) ||
            table.contains(searchQuery) ||
            customer.contains(searchQuery) ||
            phone.contains(searchQuery) ||
            cashier.contains(searchQuery);

        if (!match) return false;
      }

      return true;
    }).toList();
  }

  // Thống kê doanh thu theo danh sách đang hiển thị
  num get currentRevenue =>
      filteredList.fold<num>(0, (sum, inv) => sum + number(inv['totalAmount']));

  num get currentCashRevenue => filteredList
      .where((i) => text(i['paymentMethod'], 'cash').toLowerCase() == 'cash')
      .fold<num>(0, (sum, inv) => sum + number(inv['totalAmount']));

  num get currentTransferRevenue => filteredList
      .where((i) {
        final m = text(i['paymentMethod'], 'cash').toLowerCase();
        return m == 'transfer' || m == 'vietqr' || m == 'bank';
      })
      .fold<num>(0, (sum, inv) => sum + number(inv['totalAmount']));

  String get dateLabel {
    if (selectedDate == null) return 'Tất cả';
    final now = DateTime.now();
    if (_isSameDay(selectedDate!, now)) return 'Hôm nay';
    final yesterday = now.subtract(const Duration(days: 1));
    if (_isSameDay(selectedDate!, yesterday)) return 'Hôm qua';
    return '${selectedDate!.day.toString().padLeft(2, '0')}/${selectedDate!.month.toString().padLeft(2, '0')}';
  }

  bool get isCustomFilterActive =>
      selectedDate == null ||
      !_isSameDay(selectedDate!, DateTime.now()) ||
      activePayment != 'all';

  Future<void> _pickCustomDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: selectedDate ?? now,
      firstDate: DateTime(2023),
      lastDate: now,
      helpText: 'CHỌN NGÀY XEM HÓA ĐƠN',
      cancelText: 'HỦY',
      confirmText: 'CHỌN NGÀY',
      builder: (context, child) {
        final isDark = isDarkMode(context);
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: isDark
                ? const ColorScheme.dark(
                    primary: brand,
                    onPrimary: Colors.white,
                    surface: darkSurface,
                    onSurface: darkTextPrimary,
                  )
                : const ColorScheme.light(
                    primary: brand,
                    onPrimary: Colors.white,
                    surface: Colors.white,
                    onSurface: textPrimary,
                  ),
          ),
          child: child!,
        );
      },
    );
    if (picked != null) {
      setState(() => selectedDate = picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final isTablet = MediaQuery.sizeOf(context).width >= 750;
    final list = filteredList;

    return Scaffold(
      backgroundColor: isDark ? darkCanvas : canvas,
      appBar: AppBar(
        elevation: 0,
        scrolledUnderElevation: 0,
        backgroundColor: isDark ? darkSurface : Colors.white,
        title: Row(
          children: [
            Text(
              'Hóa đơn gần đây',
              style: TextStyle(
                fontWeight: FontWeight.w900,
                fontSize: 17.5,
                color: isDark ? darkTextPrimary : textPrimary,
                letterSpacing: -0.3,
              ),
            ),
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
              decoration: BoxDecoration(
                color: const Color(0xFFDCFCE7),
                borderRadius: BorderRadius.circular(6),
                border: Border.all(color: const Color(0xFF86EFAC)),
              ),
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '●',
                    style: TextStyle(
                      color: Color(0xFF15803D),
                      fontSize: 8.5,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  SizedBox(width: 3),
                  Text(
                    'ĐỐI SOÁT CA',
                    style: TextStyle(
                      color: Color(0xFF15803D),
                      fontSize: 9,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 0.3,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        // Đã bỏ nút reload IconButton ở AppBar theo yêu cầu của bạn vì hệ thống realtime
      ),
      body: error != null
          ? RetryState(message: error!, onRetry: load)
          : RefreshIndicator(
              onRefresh: load,
              color: brand,
              child: Center(
                child: ConstrainedBox(
                  constraints:
                      BoxConstraints(maxWidth: isTablet ? 780 : double.infinity),
                  child: Column(
                    children: [
                      // 1. THANH TÓM TẮT DOANH THU SIÊU MỎNG (SLIM SUMMARY BAR)
                      _buildSlimSummaryBar(isDark),

                      // 2. BỘ LỌC TINH GỌN (TÌM KIẾM + CHỌN NGÀY + PHƯƠNG THỨC TRÊN 1 HÀNG)
                      _buildCompactFilterBar(isDark),

                      // 3. DANH SÁCH THẺ HÓA ĐƠN GỌN GÀNG, DỄ QUAN SÁT
                      Expanded(
                        child: loading
                            ? ListView.separated(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 12, vertical: 10),
                                itemCount: 5,
                                separatorBuilder: (_, index) =>
                                    const SizedBox(height: 8),
                                itemBuilder: (context, index) =>
                                    _buildSkeletonCard(isDark),
                              )
                            : list.isEmpty
                                ? Center(
                                    child: Padding(
                                      padding: const EdgeInsets.all(28),
                                      child: Column(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          Container(
                                            padding: const EdgeInsets.all(16),
                                            decoration: BoxDecoration(
                                              color: brand
                                                  .withValues(alpha: 0.08),
                                              shape: BoxShape.circle,
                                            ),
                                            child: const Icon(
                                              Icons.receipt_long_rounded,
                                              size: 38,
                                              color: brand,
                                            ),
                                          ),
                                          const SizedBox(height: 12),
                                          Text(
                                            searchQuery.isNotEmpty
                                                ? 'Không tìm thấy hóa đơn khớp'
                                                : 'Không có hóa đơn nào trong khoảng này',
                                            style: TextStyle(
                                              fontWeight: FontWeight.w800,
                                              fontSize: 14.5,
                                              color: isDark
                                                  ? darkTextPrimary
                                                  : textPrimary,
                                            ),
                                          ),
                                          const SizedBox(height: 4),
                                          Text(
                                            searchQuery.isNotEmpty
                                                ? 'Thử đổi từ khóa hoặc xóa lọc ngày/phương thức.'
                                                : 'Hãy chọn ngày khác hoặc chọn Tất cả ngày để xem.',
                                            textAlign: TextAlign.center,
                                            style: TextStyle(
                                              fontSize: 12,
                                              color: isDark
                                                  ? darkTextSecondary
                                                  : textSecondary,
                                            ),
                                          ),
                                          if (isCustomFilterActive ||
                                              searchQuery.isNotEmpty) ...[
                                            const SizedBox(height: 12),
                                            FilledButton.tonal(
                                              style: FilledButton.styleFrom(
                                                backgroundColor: brand
                                                    .withValues(alpha: 0.12),
                                                foregroundColor: brand,
                                              ),
                                              onPressed: () {
                                                searchController.clear();
                                                setState(() {
                                                  selectedDate = null;
                                                  activePayment = 'all';
                                                });
                                              },
                                              child: const Text(
                                                  'Xem tất cả hóa đơn'),
                                            ),
                                          ],
                                        ],
                                      ),
                                    ),
                                  )
                                : ListView.separated(
                                    padding: const EdgeInsets.symmetric(
                                        horizontal: 12, vertical: 8),
                                    itemCount: list.length,
                                    separatorBuilder: (_, index) =>
                                        const SizedBox(height: 8),
                                    itemBuilder: (context, index) {
                                      final inv = list[index];
                                      return _buildInvoiceCard(
                                          context, isDark, inv);
                                    },
                                  ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
    );
  }

  // 1. THANH TÓM TẮT DOANH THU & SỐ LƯỢNG TINH GỌN (THAY CHO 4 KHỐI TO ĐÙNG)
  Widget _buildSlimSummaryBar(bool isDark) {
    final count = filteredList.length;
    final rev = currentRevenue;
    final cash = currentCashRevenue;
    final transfer = currentTransferRevenue;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
      decoration: BoxDecoration(
        color: isDark
            ? const Color(0xFF0F2922).withValues(alpha: 0.6)
            : const Color(0xFFF0FDF4),
        border: Border(
          bottom: BorderSide(
            color: isDark
                ? brand.withValues(alpha: 0.3)
                : const Color(0xFFDCFCE7),
          ),
        ),
      ),
      child: Row(
        children: [
          Icon(
            Icons.query_stats_rounded,
            size: 16,
            color: isDark ? const Color(0xFF34D399) : const Color(0xFF16A34A),
          ),
          const SizedBox(width: 6),
          RichText(
            text: TextSpan(
              children: [
                TextSpan(
                  text: '$count HĐ · ',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: isDark ? darkTextSecondary : const Color(0xFF475569),
                  ),
                ),
                TextSpan(
                  text: money(rev),
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w900,
                    color: isDark ? const Color(0xFF34D399) : brand,
                  ),
                ),
              ],
            ),
          ),
          const Spacer(),
          // Mini tag tiền mặt
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
            decoration: BoxDecoration(
              color: isDark
                  ? const Color(0xFF14532D).withValues(alpha: 0.4)
                  : Colors.white,
              borderRadius: BorderRadius.circular(6),
              border: Border.all(
                color: isDark
                    ? const Color(0xFF15803D)
                    : const Color(0xFFBBF7D0),
              ),
            ),
            child: Text(
              '💵 ${money(cash)}',
              style: const TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                color: Color(0xFF16A34A),
              ),
            ),
          ),
          const SizedBox(width: 5),
          // Mini tag chuyển khoản
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
            decoration: BoxDecoration(
              color: isDark
                  ? const Color(0xFF312E81).withValues(alpha: 0.4)
                  : Colors.white,
              borderRadius: BorderRadius.circular(6),
              border: Border.all(
                color: isDark
                    ? const Color(0xFF4338CA)
                    : const Color(0xFFC7D2FE),
              ),
            ),
            child: Text(
              '📱 ${money(transfer)}',
              style: const TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                color: Color(0xFF4F46E5),
              ),
            ),
          ),
        ],
      ),
    );
  }

  // 2. BỘ LỌC TINH GỌN (TÌM KIẾM + NÚT NGÀY + NÚT PHƯƠNG THỨC TRÊN CÙNG 1 DÒNG)
  Widget _buildCompactFilterBar(bool isDark) {
    final isDateActive = selectedDate != null;

    return Container(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 8),
      color: isDark ? darkSurface : Colors.white,
      child: Column(
        children: [
          Row(
            children: [
              // Ô tìm kiếm gọn gàng
              Expanded(
                child: Container(
                  height: 38,
                  decoration: BoxDecoration(
                    color: isDark ? darkCanvas : const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: isDark ? darkCardBorder : const Color(0xFFE2E8F0),
                    ),
                  ),
                  child: Row(
                    children: [
                      const SizedBox(width: 9),
                      Icon(
                        Icons.search_rounded,
                        color: isDark
                            ? darkTextSecondary
                            : const Color(0xFF94A3B8),
                        size: 17,
                      ),
                      const SizedBox(width: 6),
                      Expanded(
                        child: TextField(
                          controller: searchController,
                          style: TextStyle(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w600,
                            color: isDark
                                ? darkTextPrimary
                                : const Color(0xFF1E293B),
                          ),
                          decoration: InputDecoration(
                            hintText: 'Tìm bàn, mã HĐ, khách...',
                            hintStyle: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.normal,
                              color: isDark
                                  ? darkTextSecondary
                                  : const Color(0xFF94A3B8),
                            ),
                            border: InputBorder.none,
                            isDense: true,
                            contentPadding: EdgeInsets.zero,
                          ),
                        ),
                      ),
                      if (searchQuery.isNotEmpty)
                        IconButton(
                          icon: const Icon(Icons.close_rounded, size: 15),
                          color: isDark
                              ? darkTextSecondary
                              : const Color(0xFF64748B),
                          onPressed: () => searchController.clear(),
                        ),
                      const SizedBox(width: 4),
                    ],
                  ),
                ),
              ),
              const SizedBox(width: 7),

              // Nút Chọn Ngày (Menu nhanh + Mở Lịch)
              PopupMenuButton<String>(
                tooltip: 'Lọc theo ngày',
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                color: isDark ? darkSurface : Colors.white,
                onSelected: (val) {
                  if (val == 'all') {
                    setState(() => selectedDate = null);
                  } else if (val == 'today') {
                    setState(() => selectedDate = DateTime.now());
                  } else if (val == 'yesterday') {
                    setState(() => selectedDate =
                        DateTime.now().subtract(const Duration(days: 1)));
                  } else if (val == 'custom') {
                    _pickCustomDate();
                  }
                },
                itemBuilder: (context) => [
                  _buildPopupMenuItem(
                    value: 'today',
                    label: 'Hôm nay',
                    icon: Icons.today_rounded,
                    isSelected: selectedDate != null &&
                        _isSameDay(selectedDate!, DateTime.now()),
                    isDark: isDark,
                  ),
                  _buildPopupMenuItem(
                    value: 'yesterday',
                    label: 'Hôm qua',
                    icon: Icons.history_rounded,
                    isSelected: selectedDate != null &&
                        _isSameDay(
                            selectedDate!,
                            DateTime.now()
                                .subtract(const Duration(days: 1))),
                    isDark: isDark,
                  ),
                  _buildPopupMenuItem(
                    value: 'all',
                    label: 'Tất cả các ngày',
                    icon: Icons.date_range_rounded,
                    isSelected: selectedDate == null,
                    isDark: isDark,
                  ),
                  const PopupMenuDivider(),
                  _buildPopupMenuItem(
                    value: 'custom',
                    label: '🗓️ Chọn ngày trên lịch...',
                    icon: Icons.calendar_month_rounded,
                    isSelected: selectedDate != null &&
                        !_isSameDay(selectedDate!, DateTime.now()) &&
                        !_isSameDay(
                            selectedDate!,
                            DateTime.now()
                                .subtract(const Duration(days: 1))),
                    isDark: isDark,
                  ),
                ],
                child: Container(
                  height: 38,
                  padding: const EdgeInsets.symmetric(horizontal: 8),
                  decoration: BoxDecoration(
                    color: isDateActive
                        ? brand.withValues(alpha: isDark ? 0.35 : 0.12)
                        : (isDark ? darkCanvas : const Color(0xFFF1F5F9)),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: isDateActive
                          ? brand
                          : (isDark
                              ? darkCardBorder
                              : const Color(0xFFCBD5E1)),
                      width: isDateActive ? 1.2 : 1.0,
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        Icons.calendar_today_rounded,
                        size: 13.5,
                        color: isDateActive
                            ? (isDark ? const Color(0xFF5EEAD4) : brand)
                            : (isDark
                                ? darkTextSecondary
                                : const Color(0xFF475569)),
                      ),
                      const SizedBox(width: 4),
                      Text(
                        dateLabel,
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: isDateActive
                              ? FontWeight.w800
                              : FontWeight.w600,
                          color: isDateActive
                              ? (isDark ? const Color(0xFF5EEAD4) : brand)
                              : (isDark
                                  ? darkTextPrimary
                                  : const Color(0xFF334155)),
                        ),
                      ),
                      Icon(
                        Icons.arrow_drop_down_rounded,
                        size: 17,
                        color: isDateActive
                            ? brand
                            : (isDark
                                ? darkTextSecondary
                                : const Color(0xFF94A3B8)),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(width: 6),

              // Nút Chọn Phương Thức (Tiền mặt / Chuyển khoản)
              PopupMenuButton<String>(
                tooltip: 'Lọc phương thức',
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                color: isDark ? darkSurface : Colors.white,
                onSelected: (val) {
                  setState(() => activePayment = val);
                },
                itemBuilder: (context) => [
                  _buildPopupMenuItem(
                    value: 'all',
                    label: 'Tất cả PT',
                    icon: Icons.payments_outlined,
                    isSelected: activePayment == 'all',
                    isDark: isDark,
                  ),
                  _buildPopupMenuItem(
                    value: 'cash',
                    label: '💵 Tiền mặt',
                    icon: Icons.attach_money_rounded,
                    isSelected: activePayment == 'cash',
                    isDark: isDark,
                  ),
                  _buildPopupMenuItem(
                    value: 'transfer',
                    label: '📱 Chuyển khoản (VietQR)',
                    icon: Icons.qr_code_rounded,
                    isSelected: activePayment == 'transfer',
                    isDark: isDark,
                  ),
                ],
                child: Container(
                  height: 38,
                  padding: const EdgeInsets.symmetric(horizontal: 8),
                  decoration: BoxDecoration(
                    color: activePayment != 'all'
                        ? (activePayment == 'cash'
                            ? const Color(0xFF16A34A)
                                .withValues(alpha: isDark ? 0.35 : 0.12)
                            : const Color(0xFF4F46E5)
                                .withValues(alpha: isDark ? 0.35 : 0.12))
                        : (isDark ? darkCanvas : const Color(0xFFF1F5F9)),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: activePayment != 'all'
                          ? (activePayment == 'cash'
                              ? const Color(0xFF16A34A)
                              : const Color(0xFF4F46E5))
                          : (isDark
                              ? darkCardBorder
                              : const Color(0xFFCBD5E1)),
                      width: activePayment != 'all' ? 1.2 : 1.0,
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        activePayment == 'cash'
                            ? Icons.attach_money_rounded
                            : (activePayment == 'transfer'
                                ? Icons.qr_code_rounded
                                : Icons.tune_rounded),
                        size: 14.5,
                        color: activePayment == 'cash'
                            ? const Color(0xFF16A34A)
                            : (activePayment == 'transfer'
                                ? const Color(0xFF4F46E5)
                                : (isDark
                                    ? darkTextSecondary
                                    : const Color(0xFF475569))),
                      ),
                      const SizedBox(width: 3),
                      Text(
                        activePayment == 'cash'
                            ? 'Tiền mặt'
                            : (activePayment == 'transfer'
                                ? 'VietQR'
                                : 'PT'),
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: activePayment != 'all'
                              ? FontWeight.w800
                              : FontWeight.w600,
                          color: activePayment == 'cash'
                              ? const Color(0xFF16A34A)
                              : (activePayment == 'transfer'
                                  ? const Color(0xFF4F46E5)
                                  : (isDark
                                      ? darkTextPrimary
                                      : const Color(0xFF334155))),
                        ),
                      ),
                      Icon(
                        Icons.arrow_drop_down_rounded,
                        size: 17,
                        color: isDark
                            ? darkTextSecondary
                            : const Color(0xFF94A3B8),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),

          // Dải hiển thị chip lọc đang active để dễ gỡ lọc
          if (isCustomFilterActive) ...[
            const SizedBox(height: 7),
            Row(
              children: [
                if (selectedDate != null &&
                    !_isSameDay(selectedDate!, DateTime.now()))
                  _buildActiveFilterChip(
                    label: 'Ngày: $dateLabel',
                    onRemove: () => setState(() => selectedDate = DateTime.now()),
                    isDark: isDark,
                  )
                else if (selectedDate == null)
                  _buildActiveFilterChip(
                    label: 'Tất cả các ngày',
                    onRemove: () => setState(() => selectedDate = DateTime.now()),
                    isDark: isDark,
                  ),
                if (activePayment != 'all') ...[
                  const SizedBox(width: 6),
                  _buildActiveFilterChip(
                    label: activePayment == 'cash'
                        ? '💵 Tiền mặt'
                        : '📱 Chuyển khoản',
                    onRemove: () => setState(() => activePayment = 'all'),
                    isDark: isDark,
                  ),
                ],
                const Spacer(),
                InkWell(
                  onTap: () {
                    setState(() {
                      selectedDate = DateTime.now();
                      activePayment = 'all';
                      searchController.clear();
                    });
                  },
                  child: Padding(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 4, vertical: 2),
                    child: Text(
                      'Đặt lại hôm nay',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: isDark
                            ? const Color(0xFF94A3B8)
                            : const Color(0xFF64748B),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  PopupMenuItem<String> _buildPopupMenuItem({
    required String value,
    required String label,
    required IconData icon,
    required bool isSelected,
    required bool isDark,
  }) {
    return PopupMenuItem<String>(
      value: value,
      height: 38,
      child: Row(
        children: [
          Icon(
            icon,
            size: 16,
            color: isSelected
                ? brand
                : (isDark ? darkTextSecondary : const Color(0xFF64748B)),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              label,
              style: TextStyle(
                fontSize: 12.5,
                fontWeight: isSelected ? FontWeight.w800 : FontWeight.w500,
                color: isSelected
                    ? brand
                    : (isDark ? darkTextPrimary : textPrimary),
              ),
            ),
          ),
          if (isSelected)
            const Icon(Icons.check_rounded, size: 16, color: brand),
        ],
      ),
    );
  }

  Widget _buildActiveFilterChip({
    required String label,
    required VoidCallback onRemove,
    required bool isDark,
  }) {
    return Container(
      padding: const EdgeInsets.fromLTRB(7, 2, 4, 2),
      decoration: BoxDecoration(
        color: brand.withValues(alpha: isDark ? 0.25 : 0.08),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(
          color: brand.withValues(alpha: isDark ? 0.4 : 0.2),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: 10.5,
              fontWeight: FontWeight.w700,
              color: isDark ? const Color(0xFF5EEAD4) : brand,
            ),
          ),
          const SizedBox(width: 3),
          InkWell(
            onTap: onRemove,
            child: Icon(
              Icons.close_rounded,
              size: 13,
              color: isDark ? const Color(0xFF5EEAD4) : brand,
            ),
          ),
        ],
      ),
    );
  }

  // 3. THẺ HÓA ĐƠN TINH GỌN, CHUẨN FINTECH POS (HIỂN THỊ ĐƯỢC 4-5 HÓA ĐƠN TRÊN MÀN HÌNH)
  Widget _buildInvoiceCard(
      BuildContext context, bool isDark, Map<String, dynamic> inv) {
    final invoiceId = integer(inv['invoiceId']);
    final code = text(inv['invoiceCode'], '$invoiceId');
    final table = text(inv['tableDisplay'], 'Bàn ?');
    final customer = text(inv['customerName'], 'Khách vãng lai');
    final customerPhone = text(inv['customerPhone']);
    final cashier = text(inv['cashierName'], 'Thu ngân');
    final totalAmount = number(inv['totalAmount']);
    final discountAmount = number(inv['discountAmount']);
    final pointsEarned = integer(inv['pointsEarned']);
    final method = text(inv['paymentMethod'], 'cash').toLowerCase();

    final isCash = method == 'cash';
    final isTransfer =
        method == 'transfer' || method == 'vietqr' || method == 'bank';

    final String methodLabel;
    final Color methodColor;
    final Color methodBg;

    if (isCash) {
      methodLabel = 'Tiền mặt';
      methodColor = const Color(0xFF16A34A);
      methodBg = isDark ? const Color(0xFF052E16) : const Color(0xFFDCFCE7);
    } else if (isTransfer) {
      methodLabel = 'VietQR';
      methodColor = const Color(0xFF4F46E5);
      methodBg = isDark ? const Color(0xFF1E1B4B) : const Color(0xFFE0E7FF);
    } else {
      methodLabel = 'Thẻ/Khác';
      methodColor = const Color(0xFF9333EA);
      methodBg = isDark ? const Color(0xFF3B0764) : const Color(0xFFF3E8FF);
    }

    return Container(
      decoration: BoxDecoration(
        color: isDark ? darkSurface : Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isDark ? darkCardBorder : const Color(0xFFE2E8F0),
          width: 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: isDark ? 0.2 : 0.03),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(12),
          onTap: () {
            if (invoiceId > 0) {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => InvoiceDetailPage(
                    api: widget.api,
                    invoiceId: invoiceId,
                  ),
                ),
              );
            }
          },
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Hàng 1: Bàn + Giờ + Mã HĐ + Nút In Bill nhỏ gọn
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 7, vertical: 3),
                      decoration: BoxDecoration(
                        color: brand.withValues(alpha: isDark ? 0.3 : 0.1),
                        borderRadius: BorderRadius.circular(6),
                        border: Border.all(
                          color: brand.withValues(alpha: isDark ? 0.5 : 0.25),
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.table_restaurant_rounded,
                              size: 13, color: brand),
                          const SizedBox(width: 4),
                          Text(
                            table,
                            style: TextStyle(
                              fontSize: 12.5,
                              fontWeight: FontWeight.w800,
                              color: isDark ? const Color(0xFF5EEAD4) : brand,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      shortDate(inv['issuedAt']),
                      style: TextStyle(
                        fontSize: 11,
                        color: isDark
                            ? darkTextSecondary
                            : const Color(0xFF64748B),
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(width: 6),
                    Text(
                      '#$code',
                      style: TextStyle(
                        fontSize: 11,
                        fontFamily: 'monospace',
                        color: isDark
                            ? darkTextSecondary
                            : const Color(0xFF94A3B8),
                      ),
                    ),
                    const Spacer(),
                    // Nút In bill nhỏ gọn, tiện lợi
                    InkWell(
                      onTap: () {
                        if (invoiceId > 0) {
                          Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (_) => InvoiceDetailPage(
                                api: widget.api,
                                invoiceId: invoiceId,
                              ),
                            ),
                          );
                        }
                      },
                      borderRadius: BorderRadius.circular(6),
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 7, vertical: 3),
                        decoration: BoxDecoration(
                          color: isDark
                              ? const Color(0xFF334155)
                              : const Color(0xFFF1F5F9),
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(
                            color: isDark
                                ? darkCardBorder
                                : const Color(0xFFCBD5E1),
                          ),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.print_outlined,
                                size: 13,
                                color: isDark
                                    ? darkTextPrimary
                                    : const Color(0xFF334155)),
                            const SizedBox(width: 3),
                            Text(
                              'In',
                              style: TextStyle(
                                fontSize: 10.5,
                                fontWeight: FontWeight.w700,
                                color: isDark
                                    ? darkTextPrimary
                                    : const Color(0xFF1E293B),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 7),

                // Hàng 2: Khách hàng + Thu ngân
                Row(
                  children: [
                    Icon(
                      Icons.person_outline_rounded,
                      size: 13,
                      color: isDark
                          ? darkTextSecondary
                          : const Color(0xFF64748B),
                    ),
                    const SizedBox(width: 4),
                    Flexible(
                      child: Text(
                        customer == 'Khách vãng lai'
                            ? customer
                            : '$customer${customerPhone.isNotEmpty ? " ($customerPhone)" : ""}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: customer != 'Khách vãng lai'
                              ? FontWeight.w700
                              : FontWeight.w500,
                          color: isDark
                              ? darkTextPrimary
                              : const Color(0xFF334155),
                        ),
                      ),
                    ),
                    Text(
                      ' · Thu ngân: $cashier',
                      style: TextStyle(
                        fontSize: 11,
                        color: isDark
                            ? darkTextSecondary
                            : const Color(0xFF64748B),
                      ),
                    ),
                    if (pointsEarned > 0) ...[
                      const SizedBox(width: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 4, vertical: 1),
                        decoration: BoxDecoration(
                          color: const Color(0xFFDCFCE7),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          '+$pointsEarned đ',
                          style: const TextStyle(
                            fontSize: 9.5,
                            fontWeight: FontWeight.w800,
                            color: Color(0xFF15803D),
                          ),
                        ),
                      ),
                    ],
                  ],
                ),

                const SizedBox(height: 6),

                // Hàng 3: Badge Phương thức thanh toán (Trái) & Số tiền nổi bật (Phải)
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: methodBg,
                        borderRadius: BorderRadius.circular(5),
                        border: Border.all(
                            color: methodColor.withValues(alpha: 0.3)),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            isCash ? '💵' : (isTransfer ? '📱' : '💳'),
                            style: const TextStyle(fontSize: 10),
                          ),
                          const SizedBox(width: 3),
                          Text(
                            methodLabel,
                            style: TextStyle(
                              fontSize: 10.5,
                              fontWeight: FontWeight.w800,
                              color: methodColor,
                            ),
                          ),
                          if (discountAmount > 0) ...[
                            const SizedBox(width: 4),
                            Text(
                              '(-${money(discountAmount)})',
                              style: const TextStyle(
                                fontSize: 9.5,
                                color: Color(0xFFDC2626),
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                    Text(
                      money(totalAmount),
                      style: TextStyle(
                        fontSize: 15.5,
                        fontWeight: FontWeight.w900,
                        color: isDark ? const Color(0xFFFBBF24) : brand,
                        letterSpacing: -0.3,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSkeletonCard(bool isDark) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isDark ? darkSurface : Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isDark ? darkCardBorder : const Color(0xFFE2E8F0),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: const [
              SkeletonBox(width: 75, height: 20, borderRadius: 6),
              SizedBox(width: 8),
              SkeletonBox(width: 60, height: 14, borderRadius: 4),
              Spacer(),
              SkeletonBox(width: 60, height: 22, borderRadius: 6),
            ],
          ),
          const SizedBox(height: 8),
          const SkeletonBox(width: 160, height: 12, borderRadius: 4),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: const [
              SkeletonBox(width: 80, height: 18, borderRadius: 5),
              SkeletonBox(width: 90, height: 20, borderRadius: 6),
            ],
          ),
        ],
      ),
    );
  }
}

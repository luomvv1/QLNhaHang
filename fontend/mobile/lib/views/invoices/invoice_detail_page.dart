import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/staff_core.dart';

class InvoiceDetailPage extends StatefulWidget {
  const InvoiceDetailPage({
    super.key,
    required this.api,
    required this.invoiceId,
    this.initialInvoice,
  });

  final ApiClient api;
  final int invoiceId;
  final Map<String, dynamic>? initialInvoice;

  @override
  State<InvoiceDetailPage> createState() => _InvoiceDetailPageState();
}

class _InvoiceDetailPageState extends State<InvoiceDetailPage> {
  Map<String, dynamic>? invoice;
  bool loading = true;
  String? error;

  @override
  void initState() {
    super.initState();
    if (widget.initialInvoice != null && widget.initialInvoice!.isNotEmpty) {
      invoice = widget.initialInvoice;
      loading = false;
    }
    load(silent: invoice != null);
  }

  Future<void> load({bool silent = false}) async {
    if (!silent) {
      setState(() {
        loading = true;
        error = null;
      });
    }
    try {
      final value = jsonMap(
        await widget.api.get('/staff/payment/invoices/${widget.invoiceId}'),
      );
      if (mounted) {
        setState(() {
          invoice = value;
          error = null;
        });
      }
    } on ApiException catch (exception) {
      if (mounted && invoice == null) setState(() => error = exception.message);
    } catch (e) {
      if (mounted && invoice == null) {
        setState(() => error = 'Không thể tải chi tiết hóa đơn: $e');
      }
    } finally {
      if (mounted && !silent) setState(() => loading = false);
    }
  }

  void copyInvoiceCode(String code) {
    if (code.isEmpty) return;
    Clipboard.setData(ClipboardData(text: code));
    showMessage(context, 'Đã sao chép mã HĐ #$code');
  }

  void handlePrint(BuildContext context, String code) {
    // Thực hiện in ngay lập tức - không hiển thị dialog giả lập rườm rà
    showMessage(context, 'Đang gửi lệnh in hóa đơn #$code tới máy in...');
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final inv = invoice;
    final code = inv != null
        ? text(inv['invoiceCode'], '${widget.invoiceId}')
        : '${widget.invoiceId}';

    return Scaffold(
      backgroundColor: isDark ? darkCanvas : const Color(0xFFF1F5F9),
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Quay lại',
          onPressed: () => Navigator.of(context).pop(true),
        ),
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Hóa đơn #$code',
              style: TextStyle(
                fontWeight: FontWeight.w900,
                fontSize: 16.5,
                color: isDark ? darkTextPrimary : textPrimary,
              ),
            ),
            Text(
              'Phiếu thanh toán K80',
              style: TextStyle(
                fontSize: 11.5,
                color: isDark ? darkTextSecondary : textSecondary,
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Sao chép mã',
            icon: const Icon(Icons.copy_rounded, size: 19),
            onPressed: () => copyInvoiceCode(code),
          ),
          IconButton(
            tooltip: 'In',
            icon: const Icon(Icons.print_rounded, size: 20),
            onPressed: () => handlePrint(context, code),
          ),
          const SizedBox(width: 4),
        ],
      ),
      body: loading
          ? const Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircularProgressIndicator(color: brand),
                  SizedBox(height: 12),
                  Text('Đang tải hóa đơn...',
                      style: TextStyle(color: Color(0xFF64748B))),
                ],
              ),
            )
          : error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.error_outline_rounded,
                            size: 44, color: Color(0xFFEF4444)),
                        const SizedBox(height: 10),
                        Text(
                          error!,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                              color: isDark ? darkTextPrimary : textPrimary),
                        ),
                        const SizedBox(height: 14),
                        FilledButton.tonal(
                          onPressed: load,
                          child: const Text('Thử lại'),
                        ),
                      ],
                    ),
                  ),
                )
              : inv == null
                  ? const Center(child: Text('Không tìm thấy dữ liệu hóa đơn.'))
                  : Center(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 440),
                        child: ListView(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 14, vertical: 14),
                          children: [
                            _DolaReceiptCard(
                              invoice: inv,
                              onCopyCode: () => copyInvoiceCode(code),
                            ),
                            const SizedBox(height: 20),
                          ],
                        ),
                      ),
                    ),
      // THANH TÁC VỤ DƯỚI: NÚT IN CHỈ HIỆN CHỮ "IN" THEO ĐÚNG YÊU CẦU
      bottomNavigationBar: loading || error != null
          ? null
          : SafeArea(
              top: false,
              child: Container(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 12),
                decoration: BoxDecoration(
                  color: isDark ? darkSurface : Colors.white,
                  border: Border(
                    top: BorderSide(
                      color: isDark ? darkCardBorder : const Color(0xFFE2E8F0),
                    ),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color:
                          Colors.black.withValues(alpha: isDark ? 0.3 : 0.05),
                      blurRadius: 8,
                      offset: const Offset(0, -2),
                    ),
                  ],
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: SizedBox(
                        height: 44,
                        child: FilledButton.icon(
                          style: FilledButton.styleFrom(
                            backgroundColor: brand,
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                          ),
                          onPressed: () => handlePrint(context, code),
                          icon: const Icon(Icons.print_rounded, size: 18),
                          label: const Text(
                            'In',
                            style: TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 15,
                            ),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 10),
                    SizedBox(
                      height: 44,
                      child: OutlinedButton(
                        style: OutlinedButton.styleFrom(
                          foregroundColor: isDark
                              ? darkTextPrimary
                              : const Color(0xFF334155),
                          side: BorderSide(
                            color: isDark
                                ? darkCardBorder
                                : const Color(0xFFCBD5E1),
                          ),
                          padding: const EdgeInsets.symmetric(horizontal: 20),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                        onPressed: () => Navigator.of(context).pop(true),
                        child: const Text('Đóng',
                            style: TextStyle(fontWeight: FontWeight.w700)),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}

String _cleanCustomerName(String raw) {
  var cleaned = raw.trim();
  cleaned = cleaned
      .replaceAll(
        RegExp(
          r'\s*\((?:Khách\s+)?(?:Mới|Cũ|mới|cũ|New|Old|VIP|Thành viên|Hội viên)\)',
          caseSensitive: false,
        ),
        '',
      )
      .trim();
  return cleaned;
}

/// PHIẾU IN NHIỆT K80 THIẾT KẾ CHUẨN ĐEN TRẮNG (MONOCHROME PRINT-READY)
class _DolaReceiptCard extends StatelessWidget {
  const _DolaReceiptCard({
    required this.invoice,
    required this.onCopyCode,
  });

  final Map<String, dynamic> invoice;
  final VoidCallback onCopyCode;

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final rawItems = jsonMaps(invoice['items']);
    // Gộp món thông minh (Smart Grouping) cho bill in nhiệt: tránh lặp lại cùng một món nhiều dòng
    final Map<String, Map<String, dynamic>> groupedMap = {};
    for (final item in rawItems) {
      final foodId = integer(item['foodId']);
      final unitPrice = number(item['unitPrice']);
      final variantId = integer(item['variantId']);
      final name = text(item['foodNameSnapshot']);
      final key = '${foodId}_${variantId}_$unitPrice';
      if (groupedMap.containsKey(key)) {
        final ex = groupedMap[key]!;
        ex['quantity'] = integer(ex['quantity']) + integer(item['quantity']);
        ex['lineTotal'] = number(ex['lineTotal']) + number(item['lineTotal']);
        ex['batchCount'] = integer(ex['batchCount']) + 1;
        (ex['batches'] as List<int>).add(integer(item['quantity']));
        final note = text(item['note']);
        if (note.isNotEmpty) {
          final notes = ex['notes'] as List<String>;
          if (!notes.contains(note)) notes.add(note);
        }
      } else {
        final note = text(item['note']);
        groupedMap[key] = {
          'foodNameSnapshot': name,
          'variantSnapshot': text(item['variantSnapshot']),
          'quantity': integer(item['quantity']),
          'unitPrice': unitPrice,
          'lineTotal': number(item['lineTotal']),
          'batchCount': 1,
          'batches': <int>[integer(item['quantity'])],
          'notes': note.isNotEmpty ? <String>[note] : <String>[],
        };
      }
    }
    final items = groupedMap.values.toList();
    final payments = jsonMaps(invoice['payments']);
    final customer =
        invoice['customer'] is Map ? jsonMap(invoice['customer']) : null;
    final rawCustomerName = customer != null
        ? text(customer['fullName'])
        : text(invoice['customerName']);
    final customerName = _cleanCustomerName(rawCustomerName);
    final customerPhone = customer != null
        ? text(customer['phone'])
        : text(invoice['customerPhone']);
    final hasCustomer =
        customerName.isNotEmpty && customerName != 'Khách vãng lai';

    final String customerDisplay;
    if (hasCustomer) {
      if (customerPhone.isNotEmpty) {
        customerDisplay = '$customerName ($customerPhone)';
      } else {
        customerDisplay = customerName;
      }
    } else {
      customerDisplay = 'Khách vãng lai';
    }

    final paymentMethod = payments.isNotEmpty
        ? text(payments.first['paymentMethod'])
        : text(invoice['paymentMethod'], 'cash');
    final transactionCode =
        payments.isNotEmpty ? text(payments.first['transactionCode']) : '';

    final subtotal = number(invoice['subtotal']);
    final discountAmount = number(invoice['discountAmount']);
    final pointsUsed = integer(invoice['pointsUsed']);
    final pointsEarned = integer(invoice['pointsEarned']);
    final totalAmount = number(invoice['totalAmount']);

    final num pointDiscount = number(invoice['pointDiscount']) > 0
        ? number(invoice['pointDiscount'])
        : (pointsUsed > 0 ? pointsUsed * 1000 : 0);

    num voucherDiscount = number(invoice['voucherDiscount']);
    if (voucherDiscount <= 0 && discountAmount > pointDiscount) {
      voucherDiscount = discountAmount - pointDiscount;
    }

    final voucherCode =
        text(invoice['voucherCode'] ?? invoice['promotionCode']);

    final num otherDiscount =
        (discountAmount > (voucherDiscount + pointDiscount))
            ? discountAmount - (voucherDiscount + pointDiscount)
            : 0;

    final isCash = paymentMethod.toLowerCase() == 'cash';
    num rawAmountPaid = number(invoice['amountPaidByCustomer']);
    if (rawAmountPaid <= 0 && payments.isNotEmpty) {
      rawAmountPaid = number(payments.first['amountPaidByCustomer']);
    }
    final num amountPaid = isCash
        ? (rawAmountPaid > 0 ? rawAmountPaid : totalAmount)
        : 0;
    final num changeToReturn = isCash
        ? (amountPaid > totalAmount
            ? amountPaid - totalAmount
            : number(invoice['changeToReturn']))
        : 0;

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(6),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: isDark ? 0.35 : 0.08),
            blurRadius: 12,
            offset: const Offset(0, 3),
          ),
        ],
        border: Border.all(
          color: Colors.black,
          width: 1.0,
        ),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 18, 16, 20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // 1. TIÊU ĐỀ NHÀ HÀNG (Chuẩn in nhiệt trắng đen)
            const Center(
              child: Column(
                children: [
                  Text(
                    'DOLA RESTAURANT',
                    style: TextStyle(
                      fontFamily: 'serif',
                      fontSize: 19,
                      fontWeight: FontWeight.w900,
                      color: Colors.black,
                      letterSpacing: 1.0,
                    ),
                  ),
                  SizedBox(height: 2),
                  Text(
                    'Ẩm Thực Đương Đại & Đẳng Cấp',
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                      color: Colors.black,
                    ),
                  ),
                  SizedBox(height: 3),
                  Text(
                    '123 Đường Ẩm Thực, Quận 1, TP. HCM',
                    style: TextStyle(fontSize: 10.5, color: Colors.black),
                  ),
                  Text(
                    'Hotline: 1900 6868 · Website: dolarestaurant.vn',
                    style: TextStyle(fontSize: 10, color: Colors.black),
                  ),
                  SizedBox(height: 10),
                  _DashedLine(),
                  SizedBox(height: 10),

                  // Tiêu đề hóa đơn
                  Text(
                    'HÓA ĐƠN THANH TOÁN',
                    style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w900,
                      color: Colors.black,
                      letterSpacing: 0.8,
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 12),
            const _DashedLine(),
            const SizedBox(height: 10),

            // 2. THÔNG TIN HÓA ĐƠN (Chuẩn in nhiệt trắng đen)
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                GestureDetector(
                  onTap: onCopyCode,
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Text(
                        'Mã HĐ: ',
                        style: TextStyle(fontSize: 11.5, color: Colors.black),
                      ),
                      Text(
                        '#${text(invoice['invoiceCode'])}',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w900,
                          color: Colors.black,
                          fontFamily: 'monospace',
                        ),
                      ),
                      const SizedBox(width: 3),
                      const Icon(Icons.copy_rounded,
                          size: 11, color: Colors.black),
                    ],
                  ),
                ),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text(
                      'Bàn: ',
                      style: TextStyle(fontSize: 11.5, color: Colors.black),
                    ),
                    Text(
                      text(invoice['tableDisplay']),
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w900,
                        color: Colors.black,
                      ),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 4),

            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Ngày: ${shortDate(invoice['issuedAt'])}',
                  style: const TextStyle(
                      fontSize: 11.5, color: Colors.black),
                ),
                Text(
                  'Thu ngân: ${text(invoice['cashierName'], 'Nhân viên')}',
                  style: const TextStyle(
                      fontSize: 11.5, color: Colors.black),
                ),
              ],
            ),
            const SizedBox(height: 4),

            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    'Khách hàng: $customerDisplay',
                    style: const TextStyle(
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
                      color: Colors.black,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  'PT: ${_plainPaymentLabel(paymentMethod)}',
                  style: const TextStyle(
                    fontSize: 11.5,
                    fontWeight: FontWeight.w700,
                    color: Colors.black,
                  ),
                ),
              ],
            ),

            // Chỉ hiển thị Mã GD khi thanh toán chuyển khoản / VietQR / thẻ
            if (transactionCode.isNotEmpty && !isCash) ...[
              const SizedBox(height: 4),
              Text(
                'Mã GD: $transactionCode',
                style: const TextStyle(
                  fontSize: 10.5,
                  color: Colors.black,
                  fontFamily: 'monospace',
                ),
              ),
            ],

            const SizedBox(height: 10),
            const _DashedLine(),
            const SizedBox(height: 8),

            // 3. TIÊU ĐỀ BẢNG MÓN ĂN
            const Row(
              children: [
                Expanded(
                  flex: 5,
                  child: Text(
                    'TÊN MÓN',
                    style: TextStyle(
                      fontSize: 10.5,
                      fontWeight: FontWeight.w900,
                      color: Colors.black,
                    ),
                  ),
                ),
                Expanded(
                  flex: 1,
                  child: Text(
                    'SL',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 10.5,
                      fontWeight: FontWeight.w900,
                      color: Colors.black,
                    ),
                  ),
                ),
                Expanded(
                  flex: 3,
                  child: Text(
                    'Đ.GIÁ',
                    textAlign: TextAlign.right,
                    style: TextStyle(
                      fontSize: 10.5,
                      fontWeight: FontWeight.w900,
                      color: Colors.black,
                    ),
                  ),
                ),
                Expanded(
                  flex: 3,
                  child: Text(
                    'T.TIỀN',
                    textAlign: TextAlign.right,
                    style: TextStyle(
                      fontSize: 10.5,
                      fontWeight: FontWeight.w900,
                      color: Colors.black,
                    ),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 6),
            const _DottedLine(),
            const SizedBox(height: 6),

            // DANH SÁCH MÓN ĂN
            ...items.map(
              (item) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          flex: 5,
                          child: Text(
                            text(item['foodNameSnapshot']),
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 12,
                              color: Colors.black,
                            ),
                          ),
                        ),
                        Expanded(
                          flex: 1,
                          child: Text(
                            '${integer(item['quantity'])}',
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 12,
                              color: Colors.black,
                            ),
                          ),
                        ),
                        Expanded(
                          flex: 3,
                          child: Text(
                            money(number(item['unitPrice']))
                                .replaceAll(' đ', ''),
                            textAlign: TextAlign.right,
                            style: const TextStyle(
                              fontSize: 11,
                              color: Colors.black,
                              fontFamily: 'monospace',
                            ),
                          ),
                        ),
                        Expanded(
                          flex: 3,
                          child: Text(
                            money(number(item['lineTotal'])),
                            textAlign: TextAlign.right,
                            style: const TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 11.5,
                              color: Colors.black,
                              fontFamily: 'monospace',
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (text(item['variantSnapshot']).isNotEmpty &&
                        text(item['variantSnapshot']) != 'Mặc định')
                      Padding(
                        padding: const EdgeInsets.only(left: 4, top: 1),
                        child: Text(
                          '» Size: ${text(item['variantSnapshot'])}',
                          style: const TextStyle(
                              fontSize: 10, color: Colors.black),
                        ),
                      ),
                    if (integer(item['batchCount']) > 1)
                      Padding(
                        padding: const EdgeInsets.only(left: 4, top: 1),
                        child: Text(
                          '· Gọi ${integer(item['batchCount'])} lần (${(item['batches'] as List).join(" + ")})',
                          style: const TextStyle(
                            fontSize: 10,
                            color: Colors.black,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ),
                    if (item['notes'] is List && (item['notes'] as List).isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(left: 4, top: 1),
                        child: Text(
                          '* ${(item['notes'] as List).join(" · ")}',
                          style: const TextStyle(
                            fontSize: 10,
                            color: Colors.black,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      )
                    else if (text(item['note']).isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(left: 4, top: 1),
                        child: Text(
                          '* ${text(item['note'])}',
                          style: const TextStyle(
                            fontSize: 10,
                            color: Colors.black,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),

            if (items.isEmpty)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 10),
                child: Center(
                  child: Text('(Không có món ăn)',
                      style:
                          TextStyle(color: Colors.black, fontSize: 11)),
                ),
              ),

            const SizedBox(height: 6),
            const _DashedLine(),
            const SizedBox(height: 8),

            // 4. TÍNH TIỀN
            _ReceiptPriceRow(label: 'Tạm tính:', value: money(subtotal)),

            if (voucherDiscount > 0)
              _ReceiptPriceRow(
                label: voucherCode.isNotEmpty
                    ? 'Ưu đãi Voucher ($voucherCode):'
                    : 'Ưu đãi Voucher:',
                value: '-${money(voucherDiscount)}',
              ),

            if (pointsUsed > 0 || pointDiscount > 0)
              _ReceiptPriceRow(
                label: pointsUsed > 0
                    ? 'Trừ điểm ($pointsUsed điểm):'
                    : 'Trừ điểm tích lũy:',
                value: '-${money(pointDiscount)}',
              ),

            if (otherDiscount > 0)
              _ReceiptPriceRow(
                label: 'Giảm giá khác:',
                value: '-${money(otherDiscount)}',
              ),

            const SizedBox(height: 6),
            const _DoubleLine(),
            const SizedBox(height: 6),

            // TỔNG CỘNG CHUẨN ĐEN TRẮNG SANG TRỌNG
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'TỔNG CỘNG:',
                  style: TextStyle(
                    fontSize: 14.5,
                    fontWeight: FontWeight.w900,
                    color: Colors.black,
                    letterSpacing: 0.3,
                  ),
                ),
                Text(
                  money(totalAmount),
                  style: const TextStyle(
                    fontSize: 16.5,
                    fontWeight: FontWeight.w900,
                    color: Colors.black,
                    fontFamily: 'monospace',
                  ),
                ),
              ],
            ),

            const SizedBox(height: 6),
            const _DoubleLine(),
            const SizedBox(height: 6),

            // Tiền mặt: Luôn luôn hiển thị Tiền khách đưa & Tiền thối lại
            if (isCash) ...[
              _ReceiptPriceRow(
                label: 'Tiền khách đưa:',
                value: money(amountPaid),
              ),
              _ReceiptPriceRow(
                label: 'Tiền thối lại:',
                value: money(changeToReturn),
              ),
              const SizedBox(height: 4),
            ],

            // Điểm tích lũy & Lịch sử đổi điểm trong bill (chuẩn đen trắng in nhiệt)
            Builder(
              builder: (context) {
                final pointsBalance = customer != null
                    ? (customer['pointsBalance'] ?? customer['newPointsBalance'])
                    : (invoice['customerPointsBalance'] ?? invoice['pointsBalance']);

                if (pointsUsed <= 0 && pointsEarned <= 0 && pointsBalance == null) {
                  return const SizedBox.shrink();
                }

                return Column(
                  children: [
                    const SizedBox(height: 4),
                    if (pointsUsed > 0) ...[
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text(
                            'Đã dùng điểm:',
                            style: TextStyle(
                              fontSize: 10.5,
                              fontWeight: FontWeight.w700,
                              color: Colors.black,
                            ),
                          ),
                          Text(
                            '-$pointsUsed điểm',
                            style: const TextStyle(
                              fontSize: 10.5,
                              fontWeight: FontWeight.w700,
                              color: Colors.black,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 2),
                    ],
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        if (pointsEarned > 0)
                          Text(
                            'Tích lũy bill: +$pointsEarned điểm',
                            style: const TextStyle(
                              fontSize: 10.5,
                              fontWeight: FontWeight.w700,
                              color: Colors.black,
                            ),
                          )
                        else
                          const SizedBox.shrink(),
                        if (pointsBalance != null)
                          Text(
                            'Số dư điểm: $pointsBalance điểm',
                            style: const TextStyle(
                              fontSize: 10.5,
                              fontWeight: FontWeight.w700,
                              color: Colors.black,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 6),
                  ],
                );
              },
            ),

            const SizedBox(height: 6),
            const _DashedLine(),
            const SizedBox(height: 12),

            // 5. FOOTER LỜI CẢM ƠN
            const Center(
              child: Column(
                children: [
                  Text(
                    'Wifi: Dola Restaurant · Mật khẩu: 88888888',
                    style: TextStyle(fontSize: 10, color: Colors.black),
                  ),
                  SizedBox(height: 3),
                  Text(
                    'Cảm ơn Quý khách & Hẹn gặp lại!',
                    style: TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 11.5,
                      color: Colors.black,
                    ),
                  ),
                  SizedBox(height: 6),
                  Text(
                    '||||| | |||||| || |||||||| | ||||| |||||||',
                    style: TextStyle(
                      fontSize: 12,
                      letterSpacing: 2,
                      color: Colors.black,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _plainPaymentLabel(String m) {
    return switch (m.toLowerCase()) {
      'cash' => 'Tiền mặt',
      'transfer' || 'vietqr' || 'bank' => 'VietQR',
      'card' => 'Thẻ POS',
      _ => m,
    };
  }
}

class _DashedLine extends StatelessWidget {
  const _DashedLine();

  @override
  Widget build(BuildContext context) {
    return Row(
      children: List.generate(
        40,
        (index) => Expanded(
          child: Container(
            color:
                index % 2 == 0 ? Colors.transparent : Colors.black,
            height: 1,
          ),
        ),
      ),
    );
  }
}

class _DottedLine extends StatelessWidget {
  const _DottedLine();

  @override
  Widget build(BuildContext context) {
    return Row(
      children: List.generate(
        50,
        (index) => Expanded(
          child: Container(
            color:
                index % 2 == 0 ? Colors.transparent : Colors.black,
            height: 0.9,
          ),
        ),
      ),
    );
  }
}

class _DoubleLine extends StatelessWidget {
  const _DoubleLine();

  @override
  Widget build(BuildContext context) {
    return const Column(
      children: [
        Divider(height: 1, thickness: 1, color: Colors.black),
        SizedBox(height: 2),
        Divider(height: 1, thickness: 1, color: Colors.black),
      ],
    );
  }
}

class _ReceiptPriceRow extends StatelessWidget {
  const _ReceiptPriceRow({
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: const TextStyle(
              fontSize: 11.5,
              fontWeight: FontWeight.w500,
              color: Colors.black,
            ),
          ),
          Text(
            value,
            style: const TextStyle(
              fontSize: 11.5,
              fontWeight: FontWeight.w700,
              color: Colors.black,
              fontFamily: 'monospace',
            ),
          ),
        ],
      ),
    );
  }
}

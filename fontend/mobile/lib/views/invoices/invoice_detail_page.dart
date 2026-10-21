import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import '../staff/shared_widgets.dart';

class InvoiceDetailPage extends StatefulWidget {
  const InvoiceDetailPage({
    super.key,
    required this.api,
    required this.invoiceId,
  });

  final ApiClient api;
  final int invoiceId;

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
    load();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final value = jsonMap(
        await widget.api.get('/staff/payment/invoices/${widget.invoiceId}'),
      );
      if (mounted) setState(() => invoice = value);
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Chi tiết hóa đơn', style: TextStyle(fontWeight: FontWeight.w900)),
        actions: const [],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? RetryState(message: error!, onRetry: load)
          : Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 620),
                child: ListView(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
                  children: [
                    _DolaReceiptCard(invoice: invoice!),
                  ],
                ),
              ),
            ),
    );
  }
}

class _DolaReceiptCard extends StatelessWidget {
  const _DolaReceiptCard({required this.invoice});
  final Map<String, dynamic> invoice;

  @override
  Widget build(BuildContext context) {
    final items = jsonMaps(invoice['items']);
    final payments = jsonMaps(invoice['payments']);
    final customer = invoice['customer'] is Map ? jsonMap(invoice['customer']) : null;
    final paymentMethod = payments.isNotEmpty ? text(payments.first['paymentMethod']) : text(invoice['paymentMethod'], 'cash');

    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: cardBorder),
      ),
      child: Padding(
        padding: const EdgeInsets.all(22),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // 1. BRAND HEADER
            Center(
              child: Column(
                children: [
                  const Text('🌿', style: TextStyle(fontSize: 28)),
                  const Text(
                    'DOLA RESTAURANT',
                    style: TextStyle(
                      fontFamily: 'serif',
                      fontSize: 20,
                      fontWeight: FontWeight.w900,
                      color: brand,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const Text(
                    'TINH HOA ẨM THỰC VIỆT NAM',
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w800,
                      color: gold,
                      letterSpacing: 1.5,
                    ),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    '123 Đường Ẩm Thực, Quận 1, TP.HCM · Hotline: 0909 123 456',
                    style: TextStyle(fontSize: 11, color: Color(0xFF6F7E78)),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                    decoration: BoxDecoration(
                      color: brand,
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: const Text(
                      'HÓA ĐƠN THANH TOÁN',
                      style: TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w800, letterSpacing: 0.8),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // 2. METADATA GRID
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFF7FAF8),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: cardBorder),
              ),
              child: Column(
                children: [
                  _ReceiptRow(label: 'Số hóa đơn:', value: '#${text(invoice['invoiceCode'])}', isHighlight: true),
                  _ReceiptRow(label: 'Thời gian xuất:', value: shortDate(invoice['issuedAt'])),
                  _ReceiptRow(label: 'Bàn phục vụ:', value: text(invoice['tableDisplay'])),
                  _ReceiptRow(label: 'Thu ngân:', value: text(invoice['cashierName'], 'Admin')),
                  _ReceiptRow(
                    label: 'Khách hàng:',
                    value: customer == null ? 'Khách vãng lai' : '${text(customer['fullName'])} (${text(customer['phone'])})',
                  ),
                  _ReceiptRow(label: 'Phương thức:', value: _paymentLabel(paymentMethod)),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // 3. DANH SÁCH MÓN ĂN
            const Text(
              'DANH SÁCH MÓN ĂN',
              style: TextStyle(fontWeight: FontWeight.w900, fontSize: 12, color: brand, letterSpacing: 0.5),
            ),
            const Divider(color: brand, thickness: 1.2),
            ...items.map(
              (item) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            text(item['foodNameSnapshot']),
                            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13, color: brand),
                          ),
                          if (text(item['variantSnapshot']).isNotEmpty && text(item['variantSnapshot']) != 'Mặc định')
                            Text(
                              'Size: ${text(item['variantSnapshot'])}',
                              style: const TextStyle(fontSize: 11, color: Color(0xFF6F7E78)),
                            ),
                          Text(
                            '${integer(item['quantity'])} × ${money(number(item['unitPrice']))}',
                            style: const TextStyle(fontSize: 11, color: Color(0xFF6F7E78)),
                          ),
                        ],
                      ),
                    ),
                    Text(
                      money(number(item['lineTotal'])),
                      style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13, color: brand),
                    ),
                  ],
                ),
              ),
            ),
            const Divider(color: cardBorder, thickness: 1),

            // 4. TỔNG KẾT TÀI CHÍNH
            _PriceSummaryRow(label: 'Tạm tính:', value: money(number(invoice['subtotal']))),
            if (number(invoice['discountAmount']) > 0)
              _PriceSummaryRow(
                label: 'Khuyến mãi / Giảm giá:',
                value: '-${money(number(invoice['discountAmount']))}',
                color: const Color(0xFFC62828),
              ),
            if (integer(invoice['pointsUsed']) > 0)
              _PriceSummaryRow(
                label: 'Điểm thành viên đã dùng:',
                value: '${integer(invoice['pointsUsed'])} điểm',
                color: const Color(0xFF1565C0),
              ),
            const Divider(color: brand, thickness: 1.2),
            _PriceSummaryRow(
              label: 'TỔNG CỘNG:',
              value: money(number(invoice['totalAmount'])),
              isTotal: true,
            ),
            if (number(invoice['amountPaidByCustomer']) > 0) ...[
              const SizedBox(height: 4),
              _PriceSummaryRow(label: 'Tiền khách đưa:', value: money(number(invoice['amountPaidByCustomer']))),
            ],
            if (number(invoice['amountPaidByCustomer']) > number(invoice['totalAmount'])) ...[
              _PriceSummaryRow(
                label: 'Tiền thừa trả khách:',
                value: money(number(invoice['amountPaidByCustomer']) - number(invoice['totalAmount'])),
                color: const Color(0xFF2E7D32),
              ),
            ],
            if (integer(invoice['pointsEarned']) > 0) ...[
              const SizedBox(height: 6),
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFFE8F5E9),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  '🎁 Tích lũy hội viên: +${integer(invoice['pointsEarned'])} điểm thưởng',
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Color(0xFF2E7D32), fontWeight: FontWeight.bold, fontSize: 12),
                ),
              ),
            ],

            const SizedBox(height: 20),
            // 5. FOOTER LỜI CẢM ƠN
            const Center(
              child: Column(
                children: [
                  Text(
                    '✨ Xin Cảm Ơn Quý Khách & Hẹn Gặp Lại! ✨',
                    style: TextStyle(
                      fontFamily: 'serif',
                      fontWeight: FontWeight.w700,
                      fontSize: 14,
                      color: brand,
                    ),
                  ),
                  SizedBox(height: 4),
                  Text(
                    'Wifi: Dola Restaurant · Pass: 88888888',
                    style: TextStyle(fontSize: 11, color: Color(0xFF8A9992)),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _paymentLabel(String m) {
    return switch (m) {
      'cash' => '💵 Tiền mặt',
      'transfer' => '🏦 Chuyển khoản QR',
      'card' => '💳 Thẻ ngân hàng',
      'e_wallet' => '📱 Ví điện tử',
      _ => m,
    };
  }
}

class _ReceiptRow extends StatelessWidget {
  const _ReceiptRow({required this.label, required this.value, this.isHighlight = false});
  final String label;
  final String value;
  final bool isHighlight;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78))),
          Text(
            value,
            style: TextStyle(
              fontSize: 12,
              fontWeight: isHighlight ? FontWeight.w900 : FontWeight.w700,
              color: isHighlight ? gold : brand,
            ),
          ),
        ],
      ),
    );
  }
}

class _PriceSummaryRow extends StatelessWidget {
  const _PriceSummaryRow({
    required this.label,
    required this.value,
    this.color,
    this.isTotal = false,
  });

  final String label;
  final String value;
  final Color? color;
  final bool isTotal;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: isTotal ? 15 : 13,
              fontWeight: isTotal ? FontWeight.w900 : FontWeight.w600,
              color: isTotal ? brand : const Color(0xFF5A6E66),
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontSize: isTotal ? 18 : 13,
              fontWeight: FontWeight.w900,
              color: isTotal ? gold : (color ?? brand),
            ),
          ),
        ],
      ),
    );
  }
}

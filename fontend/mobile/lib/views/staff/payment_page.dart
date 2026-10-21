import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import '../invoices/invoice_detail_page.dart';
import 'shared_widgets.dart';

class PaymentPage extends StatefulWidget {
  const PaymentPage({super.key, required this.api, required this.orderId});
  final ApiClient api;
  final int orderId;

  @override
  State<PaymentPage> createState() => _PaymentPageState();
}

class _PaymentPageState extends State<PaymentPage> {
  Map<String, dynamic>? preview;
  Map<String, dynamic>? customer;
  final phone = TextEditingController();
  final voucher = TextEditingController();
  final cash = TextEditingController();
  List<Map<String, dynamic>> results = [];
  String method = 'cash';
  num voucherDiscount = 0;
  int points = 0;
  bool loading = true;
  bool paying = false;
  String? error;

  List<Map<String, dynamic>> get items => jsonMaps(preview?['items']);
  num get subtotal => number(preview?['subtotal']);
  int get pointRate => integer(preview?['pointConversionRate']) == 0
      ? 1000
      : integer(preview?['pointConversionRate']);
  int get maxPoints {
    if (customer == null) return 0;
    return number(customer!['pointsBalance'])
        .toInt()
        .clamp(0, ((subtotal - voucherDiscount) / pointRate).floor())
        .toInt();
  }

  num get pointDiscount => points * pointRate;
  num get total =>
      (subtotal - voucherDiscount - pointDiscount).clamp(0, double.infinity);
  num get paid => number(cash.text);

  Map<String, dynamic>? get vietQr =>
      preview?['vietQr'] is Map ? jsonMap(preview!['vietQr']) : null;

  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  void dispose() {
    phone.dispose();
    voucher.dispose();
    cash.dispose();
    super.dispose();
  }

  Future<void> load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final value = jsonMap(
        await widget.api.get('/staff/payment/checkout-preview/${widget.orderId}'),
      );
      if (mounted) {
        setState(() {
          preview = value;
          customer = value['customer'] is Map ? jsonMap(value['customer']) : null;
          cash.text = number(value['subtotal']).toStringAsFixed(0);
        });
      }
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> searchCustomer() async {
    if (phone.text.trim().isEmpty) return;
    try {
      final values = jsonMaps(
        await widget.api.get(
          '/staff/payment/customers/search',
          query: {'phone': phone.text.trim()},
        ),
      );
      if (mounted) setState(() => results = values);
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  Future<void> quickCreateCustomer() async {
    final fullName = TextEditingController();
    final customerPhone = TextEditingController(text: phone.text.trim());
    final form = await showDialog<Map<String, String>>(
      context: context,
      builder: (dialog) => AlertDialog(
        title: const Text('Thêm thành viên mới', style: TextStyle(fontWeight: FontWeight.w800)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: fullName,
              autofocus: true,
              decoration: const InputDecoration(labelText: 'Họ tên khách hàng', prefixIcon: Icon(Icons.person_outline)),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: customerPhone,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Số điện thoại', prefixIcon: Icon(Icons.phone_outlined)),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialog), child: const Text('Hủy')),
          FilledButton(
            onPressed: () => Navigator.pop(dialog, {
              'fullName': fullName.text.trim(),
              'phone': customerPhone.text.trim(),
            }),
            child: const Text('Tạo & Gán khách'),
          ),
        ],
      ),
    );
    fullName.dispose();
    customerPhone.dispose();
    if (!mounted || form == null || form['fullName']!.isEmpty || form['phone']!.isEmpty) {
      return;
    }
    try {
      final result = jsonMap(
        await widget.api.post('/staff/payment/customers/quick-create', data: form),
      );
      final created = result['customer'];
      if (mounted && created is Map) {
        setState(() {
          customer = jsonMap(created);
          results = [];
          phone.text = text(created['phone']);
        });
        showMessage(context, 'Đã thêm thành viên ${text(created['fullName'])}.');
      }
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  Future<void> applyVoucher() async {
    if (voucher.text.trim().isEmpty) return;
    try {
      final result = jsonMap(
        await widget.api.post(
          '/staff/payment/validate-voucher',
          data: {'voucherCode': voucher.text.trim().toUpperCase(), 'subtotal': subtotal},
        ),
      );
      if (mounted) {
        setState(() => voucherDiscount = number(result['discountAmount']));
      }
      if (!mounted) return;
      showMessage(context, 'Áp dụng mã ${text(result['promotionCode'])} thành công!');
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  Future<void> pay() async {
    if (method == 'cash' && paid < total) {
      showMessage(context, 'Số tiền khách đưa chưa đủ để thanh toán.', isError: true);
      return;
    }
    setState(() => paying = true);
    try {
      final result = jsonMap(
        await widget.api.post(
          '/staff/payment/checkout',
          data: {
            'orderId': widget.orderId,
            'customerId': customer == null ? null : integer(customer!['customerId']),
            'promotionCode': voucherDiscount > 0 ? voucher.text.trim().toUpperCase() : null,
            'pointsToUse': points,
            'paymentMethod': method,
            'amountPaidByCustomer': method == 'cash' ? paid : total,
            'transactionCode': null,
          },
        ),
      );
      if (!mounted) return;
      final invoice = jsonMap(result['invoice']);

      // Hộp thoại thành công với tùy chọn xem bill ngay
      final invoiceId = integer(invoice['invoiceId']);
      await showDialog<void>(
        context: context,
        barrierDismissible: false,
        builder: (dialog) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          title: const Row(
            children: [
              Icon(Icons.check_circle, color: Color(0xFF2E7D32), size: 28),
              SizedBox(width: 10),
              Text('Thanh toán thành công', style: TextStyle(fontWeight: FontWeight.w900)),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Mã HĐ: ${text(invoice['invoiceCode'])}',
                style: const TextStyle(fontWeight: FontWeight.w800, color: brand, fontSize: 16),
              ),
              const SizedBox(height: 10),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Tổng thanh toán:'),
                  Text(money(number(invoice['totalAmount'])), style: const TextStyle(fontWeight: FontWeight.w900, color: brand, fontSize: 16)),
                ],
              ),
              if (number(invoice['changeToReturn']) > 0) ...[
                const SizedBox(height: 6),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Tiền thừa trả khách:'),
                    Text(money(number(invoice['changeToReturn'])), style: const TextStyle(fontWeight: FontWeight.w800, color: Color(0xFF2E7D32), fontSize: 15)),
                  ],
                ),
              ],
              if (integer(invoice['pointsEarned']) > 0) ...[
                const SizedBox(height: 6),
                Text('🎁 Tích lũy thêm: +${integer(invoice['pointsEarned'])} điểm', style: const TextStyle(color: Color(0xFF2E7D32), fontWeight: FontWeight.bold)),
              ],
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialog),
              child: const Text('Đóng'),
            ),
            if (invoiceId > 0)
              FilledButton.icon(
                style: FilledButton.styleFrom(backgroundColor: brand),
                onPressed: () {
                  Navigator.pop(dialog);
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (_) => InvoiceDetailPage(api: widget.api, invoiceId: invoiceId),
                    ),
                  );
                },
                icon: const Icon(Icons.receipt_long, size: 18),
                label: const Text('Xem hóa đơn'),
              ),
          ],
        ),
      );
      if (mounted) Navigator.pop(context, true);
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    } finally {
      if (mounted) setState(() => paying = false);
    }
  }

  Widget _buildLeftColumn() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PaymentSummary(
          preview: preview!,
          items: items,
          subtotal: subtotal,
          voucherDiscount: voucherDiscount,
          pointDiscount: pointDiscount,
          total: total,
        ),
        const SizedBox(height: 12),

        // Thẻ Ưu Đãi / Voucher
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.discount_outlined, color: gold, size: 18),
                    SizedBox(width: 8),
                    Text('Mã khuyến mãi / Voucher', style: TextStyle(fontWeight: FontWeight.w800)),
                  ],
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: voucher,
                        enabled: voucherDiscount == 0,
                        textCapitalization: TextCapitalization.characters,
                        decoration: const InputDecoration(
                          hintText: 'Nhập mã (VD: DOLA10, SALE50)...',
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    voucherDiscount > 0
                        ? OutlinedButton(
                            style: OutlinedButton.styleFrom(foregroundColor: const Color(0xFFC62828)),
                            onPressed: () => setState(() {
                              voucherDiscount = 0;
                              voucher.clear();
                            }),
                            child: const Text('Bỏ mã'),
                          )
                        : FilledButton(
                            style: FilledButton.styleFrom(backgroundColor: gold, foregroundColor: brand),
                            onPressed: applyVoucher,
                            child: const Text('Áp dụng', style: TextStyle(fontWeight: FontWeight.bold)),
                          ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),

        // Thẻ Thành Viên & Tích Điểm
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.card_membership, color: brand, size: 18),
                    SizedBox(width: 8),
                    Text('Khách hàng thành viên', style: TextStyle(fontWeight: FontWeight.w800)),
                  ],
                ),
                const SizedBox(height: 10),
                if (customer == null) ...[
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: phone,
                          keyboardType: TextInputType.phone,
                          onSubmitted: (_) => searchCustomer(),
                          decoration: const InputDecoration(
                            hintText: 'Nhập số điện thoại khách...',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      IconButton.filled(
                        style: IconButton.styleFrom(backgroundColor: brand),
                        onPressed: searchCustomer,
                        icon: const Icon(Icons.search),
                      ),
                    ],
                  ),
                  ...results.map(
                    (item) => ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const CircleAvatar(backgroundColor: Color(0xFFE8F5EF), child: Icon(Icons.person, color: brand)),
                      title: Text(text(item['fullName']), style: const TextStyle(fontWeight: FontWeight.w700)),
                      subtitle: Text('${text(item['phone'])} · ${integer(item['pointsBalance'])} điểm'),
                      trailing: FilledButton.tonal(
                        onPressed: () => setState(() {
                          customer = item;
                          results = [];
                        }),
                        child: const Text('Chọn'),
                      ),
                    ),
                  ),
                  Align(
                    alignment: Alignment.centerLeft,
                    child: TextButton.icon(
                      onPressed: quickCreateCustomer,
                      icon: const Icon(Icons.person_add_alt_1),
                      label: const Text('Tạo mới thành viên nhanh'),
                    ),
                  ),
                ] else ...[
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      backgroundColor: brand,
                      child: Text(
                        text(customer!['fullName']).isNotEmpty ? text(customer!['fullName'])[0] : 'K',
                        style: const TextStyle(color: gold, fontWeight: FontWeight.bold),
                      ),
                    ),
                    title: Text(text(customer!['fullName']), style: const TextStyle(fontWeight: FontWeight.w800)),
                    subtitle: Text('${text(customer!['phone'])} · Khả dụng: ${integer(customer!['pointsBalance'])} điểm'),
                    trailing: TextButton(
                      style: TextButton.styleFrom(foregroundColor: const Color(0xFFC62828)),
                      onPressed: () => setState(() {
                        customer = null;
                        points = 0;
                      }),
                      child: const Text('Bỏ chọn'),
                    ),
                  ),
                  if (maxPoints > 0) ...[
                    const SizedBox(height: 6),
                    TextField(
                      keyboardType: TextInputType.number,
                      onChanged: (val) => setState(() {
                        points = (int.tryParse(val) ?? 0).clamp(0, maxPoints);
                      }),
                      decoration: InputDecoration(
                        labelText: 'Đổi điểm thanh toán (tối đa $maxPoints điểm)',
                        helperText: '1 điểm = $pointRate đ (Giảm trừ ${money(pointDiscount)})',
                        suffixIcon: IconButton(
                          icon: const Text('TỐI ĐA', style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: brand)),
                          onPressed: () => setState(() => points = maxPoints),
                        ),
                      ),
                    ),
                  ],
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRightColumn() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.payment, color: brand, size: 18),
                    SizedBox(width: 8),
                    Text('Phương thức thanh toán', style: TextStyle(fontWeight: FontWeight.w800)),
                  ],
                ),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final entry in const {
                      'cash': '💵 Tiền mặt',
                      'transfer': '🏦 Chuyển khoản QR',
                      'card': '💳 Quẹt thẻ',
                      'e_wallet': '📱 Ví điện tử',
                    }.entries)
                      ChoiceChip(
                        label: Text(entry.value),
                        selected: method == entry.key,
                        selectedColor: gold.withValues(alpha: 0.25),
                        onSelected: (_) => setState(() {
                          method = entry.key;
                          if (method != 'cash') {
                            cash.text = total.toStringAsFixed(0);
                          }
                        }),
                      ),
                  ],
                ),

                // KHỐI VIETQR ĐỘNG KHI CHỌN CHUYỂN KHOẢN
                if (method == 'transfer') ...[
                  const SizedBox(height: 16),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF4F8F6),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: cardBorder),
                    ),
                    child: Column(
                      children: [
                        const Text(
                          'QUÉT MÃ VIETQR ĐỂ THANH TOÁN',
                          style: TextStyle(fontWeight: FontWeight.w900, color: brand, letterSpacing: 0.8),
                        ),
                        const SizedBox(height: 10),
                        if (vietQr != null) ...[
                          ClipRRect(
                            borderRadius: BorderRadius.circular(8),
                            child: Image.network(
                              'https://img.vietqr.io/image/${vietQr!['bankId']}-${vietQr!['accountNo']}-compact2.jpg?amount=${total.toInt()}&addInfo=${vietQr!['memo']}&accountName=${vietQr!['accountName']}',
                              height: 180,
                              fit: BoxFit.contain,
                              errorBuilder: (context, error, stackTrace) => const Padding(
                                padding: EdgeInsets.all(20),
                                child: Text('Không thể tải mã QR trực tuyến.'),
                              ),
                            ),
                          ),
                          const SizedBox(height: 10),
                          Text(
                            'Ngân hàng: ${text(vietQr!['bankId'])} · STK: ${text(vietQr!['accountNo'])}',
                            style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 13),
                          ),
                          Text(
                            'Chủ TK: ${text(vietQr!['accountName'])}',
                            style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78)),
                          ),
                          Text(
                            'Nội dung: ${text(vietQr!['memo'])}',
                            style: const TextStyle(fontWeight: FontWeight.w900, color: gold, fontSize: 13),
                          ),
                        ] else
                          const Text('Chưa có cấu hình tài khoản VietQR.'),
                      ],
                    ),
                  ),
                ],

                // TIỀN MẶT
                if (method == 'cash') ...[
                  const SizedBox(height: 16),
                  TextField(
                    controller: cash,
                    keyboardType: TextInputType.number,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(
                      labelText: 'Tiền khách đưa',
                      suffixText: 'VNĐ',
                      prefixIcon: Icon(Icons.attach_money),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Align(
                    alignment: Alignment.centerRight,
                    child: Text(
                      paid >= total
                          ? 'Tiền thừa trả khách: ${money(paid - total)}'
                          : 'Còn thiếu: ${money(total - paid)}',
                      style: TextStyle(
                        color: paid >= total ? const Color(0xFF2E7D32) : const Color(0xFFC62828),
                        fontWeight: FontWeight.w800,
                        fontSize: 14,
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 18),

        FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: gold,
            foregroundColor: brand,
            minimumSize: const Size.fromHeight(52),
          ),
          onPressed: paying ? null : pay,
          icon: paying
              ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2, color: brand))
              : const Icon(Icons.check_circle_outline, size: 22),
          label: Text(
            paying ? 'Đang xuất hóa đơn...' : 'HOÀN TẤT THANH TOÁN · ${money(total)}',
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 16),
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final isTablet = MediaQuery.sizeOf(context).width >= 750;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thanh toán hóa đơn (POS)', style: TextStyle(fontWeight: FontWeight.w900)),
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? RetryState(message: error!, onRetry: load)
          : isTablet
          // BỐ CỤC TABLET: 2 CỘT
          ? SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(flex: 5, child: _buildLeftColumn()),
                  const SizedBox(width: 16),
                  Expanded(flex: 5, child: _buildRightColumn()),
                ],
              ),
            )
          // BỐ CỤC ĐIỆN THOẠI: 1 CỘT CUỘN DỌC
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _buildLeftColumn(),
                const SizedBox(height: 12),
                _buildRightColumn(),
              ],
            ),
    );
  }
}

import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';

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
  final pointsInput = TextEditingController(text: '0');
  List<Map<String, dynamic>> results = [];
  List<Map<String, dynamic>> availablePromotions = [];
  Map<String, dynamic>? selectedPromotion;
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
  bool get canCheckout => preview?['canCheckout'] != false;

  Map<String, dynamic>? get vietQr =>
      preview?['vietQr'] is Map ? jsonMap(preview!['vietQr']) : null;

  Map<String, dynamic> get activeVietQr {
    if (preview?['vietQr'] is Map) return jsonMap(preview!['vietQr']);
    if (preview?['VietQr'] is Map) return jsonMap(preview!['VietQr']);
    final orderIdFormatted = widget.orderId.toString().padLeft(4, '0');
    return {
      'bankId': 'BIDV',
      'accountNo': '5704484382',
      'accountName': 'VO VAN LUOM',
      'memo': 'ORD$orderIdFormatted',
    };
  }

  static String _generateVietQrPayload({
    required String binOrBankCode,
    required String accountNo,
    int? amount,
    String? memo,
  }) {
    final binMap = {
      'BIDV': '970418',
      'VCB': '970436',
      'VIETCOMBANK': '970436',
      'MB': '970422',
      'MBBANK': '970422',
      'TCB': '970407',
      'TECHCOMBANK': '970407',
      'ACB': '970416',
      'VPB': '970432',
      'VPBANK': '970432',
      'TPB': '970423',
      'TPBANK': '970423',
      'VIB': '970441',
      'STB': '970403',
      'SACOMBANK': '970403',
      'CTG': '970415',
      'VIETINBANK': '970415',
      'AGR': '970405',
      'AGRIBANK': '970405',
    };
    final cleanBank = binOrBankCode.toUpperCase().trim();
    final bin = binMap[cleanBank] ?? (RegExp(r'^\d+$').hasMatch(cleanBank) ? cleanBank : '970418');

    String res = '000201';
    res += (amount != null && amount > 0) ? '010212' : '010211';

    final guid = '0010A000000727';
    final subBin = '00${bin.length.toString().padLeft(2, '0')}$bin';
    final subAcc = '01${accountNo.length.toString().padLeft(2, '0')}$accountNo';
    final sub01 = subBin + subAcc;
    final sub01Full = '01${sub01.length.toString().padLeft(2, '0')}$sub01';
    final sub02 = '0208QRIBFTTA';
    final tag38Val = guid + sub01Full + sub02;
    res += '38${tag38Val.length.toString().padLeft(2, '0')}$tag38Val';

    res += '5303704';
    if (amount != null && amount > 0) {
      final amtStr = amount.toString();
      res += '54${amtStr.length.toString().padLeft(2, '0')}$amtStr';
    }
    res += '5802VN';
    if (memo != null && memo.isNotEmpty) {
      final memoSub = '08${memo.length.toString().padLeft(2, '0')}$memo';
      res += '62${memoSub.length.toString().padLeft(2, '0')}$memoSub';
    }
    res += '6304';

    int crc = 0xFFFF;
    for (int i = 0; i < res.length; i++) {
      crc ^= (res.codeUnitAt(i) << 8);
      for (int j = 0; j < 8; j++) {
        if ((crc & 0x8000) != 0) {
          crc = ((crc << 1) ^ 0x1021) & 0xFFFF;
        } else {
          crc = (crc << 1) & 0xFFFF;
        }
      }
    }
    return res + crc.toRadixString(16).toUpperCase().padLeft(4, '0');
  }

  Widget _buildVietQrImage({double? width, double? height}) {
    final qr = activeVietQr;
    final bankId = text(qr['bankId']).isEmpty ? 'BIDV' : text(qr['bankId']);
    final accountNo = text(qr['accountNo']).isEmpty ? '5704484382' : text(qr['accountNo']);
    final memo = text(qr['memo']).isEmpty ? 'ORD${widget.orderId.toString().padLeft(4, '0')}' : text(qr['memo']);
    final amountInt = total.toInt();

    final emvcoPayload = _generateVietQrPayload(
      binOrBankCode: bankId,
      accountNo: accountNo,
      amount: amountInt,
      memo: memo,
    );

    return Container(
      width: width ?? 180,
      height: height ?? 180,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
      ),
      padding: const EdgeInsets.all(4),
      child: QrImageView(
        data: emvcoPayload,
        version: QrVersions.auto,
        size: (width ?? 180) - 8,
        backgroundColor: Colors.white,
        errorCorrectionLevel: QrErrorCorrectLevel.M,
      ),
    );
  }


  void showCustomerFullscreenQr(BuildContext context) {
    final qr = activeVietQr;
    final bankId = text(qr['bankId']).isEmpty ? 'BIDV' : text(qr['bankId']);
    final accountNo = text(qr['accountNo']).isEmpty ? '5704484382' : text(qr['accountNo']);
    final accountName = text(qr['accountName']).isEmpty ? 'VO VAN LUOM' : text(qr['accountName']);
    final memo = text(qr['memo']).isEmpty ? 'ORD${widget.orderId.toString().padLeft(4, '0')}' : text(qr['memo']);
    final tableName = text(preview?['tableDisplay']).isEmpty ? text(preview?['tableName']) : text(preview?['tableDisplay']);
    final orderCode = text(preview?['orderCode']).isEmpty ? 'ORD-${widget.orderId.toString().padLeft(4, '0')}' : text(preview?['orderCode']);

    showDialog(
      context: context,
      barrierDismissible: true,
      builder: (dialogCtx) => Dialog(
        backgroundColor: Colors.white,
        insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 420),
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Header
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        '🌿 HƯƠNG SEN RESTAURANT',
                        style: TextStyle(fontWeight: FontWeight.w900, fontSize: 13, color: Color(0xFF1E7E55)),
                      ),
                      Text(
                        'BÀN: ${tableName.isEmpty ? "Tại bàn" : tableName} · ĐƠN #$orderCode',
                        style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11.5, color: Color(0xFF64748B)),
                      ),
                    ],
                  ),
                  IconButton(
                    onPressed: () => Navigator.pop(dialogCtx),
                    icon: const Icon(Icons.close, color: Color(0xFF64748B)),
                    tooltip: 'Đóng',
                  ),
                ],
              ),
              const Divider(height: 24),

              // Title
              const Text(
                'QUÉT MÃ ĐỂ THANH TOÁN',
                style: TextStyle(fontWeight: FontWeight.w900, fontSize: 16, color: Color(0xFF0F172A), letterSpacing: 0.5),
              ),
              const SizedBox(height: 12),

              // 300px QR Code
              Container(
                width: 280,
                height: 280,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: const Color(0xFFCBD5E1), width: 2),
                  boxShadow: [
                    BoxShadow(color: Colors.black.withValues(alpha: 0.06), blurRadius: 16, offset: const Offset(0, 4)),
                  ],
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(10),
                  child: _buildVietQrImage(width: 260, height: 260),
                ),
              ),
              const SizedBox(height: 14),

              // Blue bold amount
              const Text('Số tiền cần thanh toán', style: TextStyle(fontSize: 12, color: Color(0xFF64748B), fontWeight: FontWeight.w600)),
              Text(
                money(total),
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 26, color: Color(0xFF0284C7), fontFamily: 'monospace'),
              ),
              const SizedBox(height: 8),

              // Memo Pill
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: const Color(0xFFBFDBFE)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text('Nội dung: ', style: TextStyle(fontSize: 12, color: Color(0xFF1E40AF))),
                    Text(memo, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w900, color: Color(0xFF1E3A8A), fontFamily: 'monospace')),
                  ],
                ),
              ),
              const SizedBox(height: 6),
              Text(
                'Chủ TK: $accountName · STK: $accountNo ($bankId)',
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 11, color: Color(0xFF64748B), fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 16),

              // Radar indicator
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: const Color(0xFFF0FDF4),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: const Color(0xFFBBF7D0)),
                ),
                child: const Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    SizedBox.square(
                      dimension: 14,
                      child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFF16A34A)),
                    ),
                    SizedBox(width: 8),
                    Flexible(
                      child: Text(
                        'Hệ thống đang tự động nhận diện thanh toán...',
                        style: TextStyle(fontSize: 11, color: Color(0xFF15803D), fontWeight: FontWeight.w700),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // Gợi ý các mệnh giá tiền mặt thông minh (Smart Cash Presets)
  List<num> get cashPresets {
    final t = total;
    if (t <= 0) return [];
    final set = <num>{t};
    for (final step in [50000, 100000, 200000, 500000]) {
      final next = (t / step).ceil() * step;
      if (next > t && set.length < 5) set.add(next);
    }
    for (final denom in [100000, 200000, 500000, 1000000, 2000000]) {
      if (denom > t && set.length < 5) set.add(denom);
    }
    final list = set.toList()..sort();
    return list;
  }

  @override
  void initState() {
    super.initState();
    load();
  }

  Timer? _pollTimer;

  void _startTransferPolling() {
    _pollTimer?.cancel();
    _pollTimer = Timer.periodic(const Duration(seconds: 3), (_) async {
      if (!mounted || method != 'transfer' || paying) return;
      try {
        final value = jsonMap(
          await widget.api.get('/staff/payment/checkout-preview/${widget.orderId}'),
        );
        if (!mounted) return;
        if (value['isPaid'] == true || value['orderStatus'] == 'paid') {
          _pollTimer?.cancel();
          final inv = value['invoice'] is Map ? jsonMap(value['invoice']) : null;
          final invoiceId = inv != null ? integer(inv['invoiceId']) : 0;
          final invoiceCode = inv != null ? text(inv['invoiceCode']) : '';

          if (invoiceId > 0) {
            showMessage(context, '🎉 Đã nhận thanh toán SePay! Hóa đơn #$invoiceCode');
            Navigator.pushReplacement(
              context,
              MaterialPageRoute(
                builder: (_) => InvoiceDetailPage(
                  api: widget.api,
                  invoiceId: invoiceId,
                  initialInvoice: inv,
                ),
              ),
            );
          } else {
            showMessage(context, '🎉 Đã nhận chuyển khoản thành công!');
            Navigator.pop(context, true);
          }
        }
      } catch (_) {}
    });
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    phone.dispose();
    voucher.dispose();
    cash.dispose();
    pointsInput.dispose();
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
          if (value['availablePromotions'] is List) {
            availablePromotions = jsonMaps(value['availablePromotions']);
          }
        });
        // Tải thêm danh sách khuyến mãi nếu backend chưa gắn sẵn
        if (availablePromotions.isEmpty) {
          fetchAvailablePromotions();
        }
      }
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> fetchAvailablePromotions() async {
    try {
      final promos = jsonMaps(
        await widget.api.get(
          '/staff/payment/available-promotions',
          query: {'subtotal': subtotal.toString()},
        ),
      );
      if (mounted) setState(() => availablePromotions = promos);
    } catch (_) {}
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

  Future<void> applyVoucher([String? specificCode]) async {
    final codeToApply = (specificCode ?? voucher.text).trim().toUpperCase();
    if (codeToApply.isEmpty) return;
    try {
      final result = jsonMap(
        await widget.api.post(
          '/staff/payment/validate-voucher',
          data: {'voucherCode': codeToApply, 'subtotal': subtotal},
        ),
      );
      if (mounted) {
        setState(() {
          voucher.text = codeToApply;
          voucherDiscount = number(result['discountAmount']);
          // Tìm thông tin chi tiết voucher trong availablePromotions nếu có
          selectedPromotion = availablePromotions.firstWhere(
            (p) => text(p['promotionCode']).toUpperCase() == codeToApply,
            orElse: () => {
              'promotionCode': codeToApply,
              'promotionName': text(result['promotionName']),
            },
          );
          if (points > maxPoints) {
            points = maxPoints;
            pointsInput.text = points.toString();
          }
          if (method == 'cash') {
            cash.text = total.toStringAsFixed(0);
          }
        });
      }
      if (!mounted) return;
      showMessage(context, 'Áp dụng mã $codeToApply thành công (-${money(voucherDiscount)})!');
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    }
  }

  void removeVoucher() {
    setState(() {
      voucherDiscount = 0;
      voucher.clear();
      selectedPromotion = null;
      if (method == 'cash') {
        cash.text = total.toStringAsFixed(0);
      }
    });
  }

  // BOTTOM SHEET: CHỌN MÃ GIẢM GIÁ TỪ CHƯƠNG TRÌNH KHUYẾN MÃI CỦA NHÀ HÀNG
  Future<void> openPromotionPicker() async {
    final isDark = isDarkMode(context);

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      backgroundColor: isDark ? darkSurface : Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        minChildSize: 0.45,
        maxChildSize: 0.9,
        expand: false,
        builder: (_, scrollController) => Column(
          children: [
            // Handle bar
            Container(
              margin: const EdgeInsets.only(top: 10, bottom: 6),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Colors.grey.withValues(alpha: 0.4),
                borderRadius: BorderRadius.circular(2),
              ),
            ),

            // Header Modal
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(7),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEF3C7),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Icon(Icons.confirmation_number_outlined, color: Color(0xFFD97706), size: 22),
                      ),
                      const SizedBox(width: 10),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Khuyến mãi & Voucher',
                            style: TextStyle(fontWeight: FontWeight.w900, fontSize: 17),
                          ),
                          Text(
                            'Sắp xếp giảm nhiều đến ít · Đơn: ${money(subtotal)}',
                            style: const TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                          ),
                        ],
                      ),
                    ],
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(ctx),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),

            // Danh sách các Voucher (Sắp xếp theo thứ tự giảm nhiều đến ít)
            Expanded(
              child: availablePromotions.isEmpty
                  ? Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.discount_outlined, size: 48, color: Color(0xFFCBD5E1)),
                          const SizedBox(height: 10),
                          const Text(
                            'Chưa có khuyến mãi nào đang diễn ra',
                            style: TextStyle(fontWeight: FontWeight.w700, color: Color(0xFF64748B)),
                          ),
                          const SizedBox(height: 4),
                          const Text(
                            'Nhà hàng chưa phát hành hoặc tất cả voucher đã hết hạn.',
                            style: TextStyle(fontSize: 12, color: Color(0xFF94A3B8)),
                          ),
                        ],
                      ),
                    )
                  : Builder(
                      builder: (context) {
                        // Sắp xếp mã theo thứ tự giảm nhiều đến ít
                        final sortedPromotions = List<Map<String, dynamic>>.from(availablePromotions)
                          ..sort((a, b) {
                            final aVal = number(a['promotionValue']);
                            final bVal = number(b['promotionValue']);
                            final aType = text(a['promotionType']);
                            final bType = text(b['promotionType']);
                            final num aDiscount = aType == 'percent'
                                ? (subtotal * aVal / 100).round()
                                : (aVal > subtotal ? subtotal : aVal);
                            final num bDiscount = bType == 'percent'
                                ? (subtotal * bVal / 100).round()
                                : (bVal > subtotal ? subtotal : bVal);
                            return bDiscount.compareTo(aDiscount);
                          });

                        return ListView.separated(
                          controller: scrollController,
                          padding: const EdgeInsets.all(16),
                          itemCount: sortedPromotions.length,
                          separatorBuilder: (_, _) => const SizedBox(height: 12),
                          itemBuilder: (context, index) {
                            final p = sortedPromotions[index];
                            final code = text(p['promotionCode']).toUpperCase();
                            final name = text(p['promotionName']);
                            final type = text(p['promotionType']);
                            final val = number(p['promotionValue']);
                            final minOrder = number(p['minimumOrderValue']);
                            final isEligible = subtotal >= minOrder;
                            final isApplied = voucher.text.trim().toUpperCase() == code && voucherDiscount > 0;

                            final num discountEstimated = type == 'percent'
                                ? (subtotal * val / 100).round()
                                : (val > subtotal ? subtotal : val);

                        return Container(
                          decoration: BoxDecoration(
                            color: isApplied
                                ? const Color(0xFFECFDF5)
                                : isEligible
                                    ? (isDark ? const Color(0xFF1E293B) : Colors.white)
                                    : (isDark ? const Color(0xFF0F172A) : const Color(0xFFF8FAFC)),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(
                              color: isApplied
                                  ? const Color(0xFF10B981)
                                  : isEligible
                                      ? const Color(0xFFCBD5E1)
                                      : const Color(0xFFE2E8F0),
                              width: isApplied ? 1.8 : 1,
                            ),
                          ),
                          child: InkWell(
                            onTap: isEligible
                                ? () {
                                    Navigator.pop(ctx);
                                    applyVoucher(code);
                                  }
                                : null,
                            borderRadius: BorderRadius.circular(14),
                            child: Padding(
                              padding: const EdgeInsets.all(14),
                              child: Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  // Vé voucher bên trái
                                  Container(
                                    width: 72,
                                    padding: const EdgeInsets.symmetric(vertical: 8),
                                    decoration: BoxDecoration(
                                      color: isEligible
                                          ? const Color(0xFFFEF3C7)
                                          : const Color(0xFFF1F5F9),
                                      borderRadius: BorderRadius.circular(10),
                                      border: Border.all(
                                        color: isEligible ? const Color(0xFFFDE68A) : const Color(0xFFE2E8F0),
                                      ),
                                    ),
                                    child: Column(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Icon(
                                          Icons.confirmation_number,
                                          size: 20,
                                          color: isEligible ? const Color(0xFFD97706) : const Color(0xFF94A3B8),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          type == 'percent' ? '-$val%' : '-${money(val)}',
                                          textAlign: TextAlign.center,
                                          style: TextStyle(
                                            fontSize: 12,
                                            fontWeight: FontWeight.w900,
                                            color: isEligible ? const Color(0xFFB45309) : const Color(0xFF94A3B8),
                                            fontFamily: 'monospace',
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                  const SizedBox(width: 12),

                                  // Thông tin chi tiết voucher
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Row(
                                          children: [
                                            Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
                                              decoration: BoxDecoration(
                                                color: isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                                                borderRadius: BorderRadius.circular(6),
                                              ),
                                              child: Text(
                                                code,
                                                style: const TextStyle(
                                                  fontFamily: 'monospace',
                                                  fontWeight: FontWeight.w900,
                                                  fontSize: 12.5,
                                                  letterSpacing: 0.5,
                                                ),
                                              ),
                                            ),
                                            if (isApplied) ...[
                                              const SizedBox(width: 6),
                                              Container(
                                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                                decoration: BoxDecoration(
                                                  color: const Color(0xFF10B981),
                                                  borderRadius: BorderRadius.circular(6),
                                                ),
                                                child: const Text(
                                                  'Đang dùng',
                                                  style: TextStyle(fontSize: 10, color: Colors.white, fontWeight: FontWeight.bold),
                                                ),
                                              ),
                                            ],
                                          ],
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          name,
                                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13.5),
                                        ),
                                        const SizedBox(height: 2),
                                        Text(
                                          minOrder > 0
                                              ? 'Đơn tối thiểu: ${money(minOrder)}'
                                              : 'Không yêu cầu đơn tối thiểu',
                                          style: const TextStyle(fontSize: 11.5, color: Color(0xFF64748B)),
                                        ),

                                        const SizedBox(height: 6),
                                        if (isEligible) ...[
                                          Text(
                                            'Tiết kiệm dự kiến: -${money(discountEstimated)}',
                                            style: const TextStyle(
                                              fontWeight: FontWeight.w800,
                                              fontSize: 12,
                                              color: Color(0xFF16A34A),
                                            ),
                                          ),
                                        ] else ...[
                                          Text(
                                            'Cần mua thêm ${money(minOrder - subtotal)} để dùng mã này',
                                            style: const TextStyle(
                                              fontSize: 11.5,
                                              fontWeight: FontWeight.w600,
                                              color: Color(0xFFDC2626),
                                            ),
                                          ),
                                        ],
                                      ],
                                    ),
                                  ),

                                  // Nút áp dụng
                                  if (isApplied)
                                    IconButton(
                                      icon: const Icon(Icons.check_circle, color: Color(0xFF10B981), size: 24),
                                      onPressed: () {
                                        Navigator.pop(ctx);
                                        removeVoucher();
                                      },
                                      tooltip: 'Bỏ dùng mã',
                                    )
                                  else if (isEligible)
                                    Padding(
                                      padding: const EdgeInsets.only(top: 6),
                                      child: FilledButton.tonal(
                                        style: FilledButton.styleFrom(
                                          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                                          minimumSize: Size.zero,
                                          tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                        ),
                                        onPressed: () {
                                          Navigator.pop(ctx);
                                          applyVoucher(code);
                                        },
                                        child: const Text('Dùng mã', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
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
                ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> pay() async {
    if (!canCheckout) {
      showMessage(context, 'Cần xác nhận phục vụ tất cả món trước khi thanh toán.', isError: true);
      return;
    }
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
      final invoiceId = integer(invoice['invoiceId']);
      final invoiceCode = text(invoice['invoiceCode']);

      // TỰ ĐỘNG CHUYỂN HƯỚNG SANG HÓA ĐƠN & CHẾ ĐỘ IN CHO KHÁCH / NHÂN VIÊN
      if (invoiceId > 0) {
        showMessage(context, 'Thanh toán thành công! Hóa đơn #$invoiceCode');
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (_) => InvoiceDetailPage(
              api: widget.api,
              invoiceId: invoiceId,
              initialInvoice: invoice,
            ),
          ),
        );
      } else {
        showMessage(context, 'Thanh toán thành công!');
        Navigator.pop(context, true);
      }
    } on ApiException catch (exception) {
      if (mounted) showMessage(context, exception.message, isError: true);
    } finally {
      if (mounted) setState(() => paying = false);
    }
  }

  Widget _buildLeftColumn() {
    final isDark = isDarkMode(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (!canCheckout)
          Container(
            margin: const EdgeInsets.only(bottom: 12), padding: const EdgeInsets.all(12),
            color: const Color(0xFFFFF3D8),
            child: Row(children: [
              Expanded(child: Text('Còn ${integer(preview?['unservedQuantity'])} suất chưa xác nhận phục vụ. Chưa thể thanh toán.',
                style: const TextStyle(color: Color(0xFF855016), fontSize: 13))),
              TextButton(onPressed: load, child: const Text('Tải lại')),
            ]),
          ),
        // 1. TẠM TÍNH MÓN ĂN & GỘP MÓN THÔNG MINH
        PaymentSummary(
          preview: preview!,
          items: items,
          subtotal: subtotal,
          voucherDiscount: voucherDiscount,
          pointDiscount: pointDiscount,
          total: total,
          voucherCode: voucherDiscount > 0 ? voucher.text.trim().toUpperCase() : null,
          pointsUsed: points > 0 ? points : null,
        ),
        const SizedBox(height: 14),

        // 2. THẺ MÃ KHUYẾN MÃI / VOUCHER CHUẨN UI/UX PRO
        Card(
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(color: isDark ? darkCardBorder : const Color(0xFFE2E8F0)),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Expanded(child: Row(
                      children: [
                        Icon(Icons.confirmation_number_outlined, color: Color(0xFFD97706), size: 20),
                        SizedBox(width: 8),
                        Expanded(child: Text('Mã khuyến mãi / Voucher', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14.5))),
                      ],
                    )),
                    InkWell(
                      onTap: openPromotionPicker,
                      borderRadius: BorderRadius.circular(8),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(
                              availablePromotions.isNotEmpty
                                  ? '${availablePromotions.length} mã ưu đãi'
                                  : 'Chọn mã',
                              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: brand),
                            ),
                            const Icon(Icons.chevron_right, size: 16, color: brand),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // Khi ĐÃ ÁP DỤNG VOUCHER: Hiển thị Thẻ Ticket sang trọng
                if (voucherDiscount > 0) ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFFFBEB),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFFDE68A)),
                    ),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFEF3C7),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Icon(Icons.check_circle, color: Color(0xFFD97706), size: 20),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Text(
                                    voucher.text.toUpperCase(),
                                    style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 14, fontFamily: 'monospace'),
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    '-${money(voucherDiscount)}',
                                    style: const TextStyle(fontWeight: FontWeight.w900, color: Color(0xFF16A34A), fontSize: 14),
                                  ),
                                ],
                              ),
                              if (selectedPromotion != null && text(selectedPromotion!['promotionName']).isNotEmpty)
                                Text(
                                  text(selectedPromotion!['promotionName']),
                                  style: const TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                            ],
                          ),
                        ),
                        OutlinedButton(
                          style: OutlinedButton.styleFrom(
                            foregroundColor: const Color(0xFFDC2626),
                            side: const BorderSide(color: Color(0xFFFCA5A5)),
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                            minimumSize: Size.zero,
                          ),
                          onPressed: removeVoucher,
                          child: const Text('Bỏ mã', style: TextStyle(fontSize: 11.5, fontWeight: FontWeight.bold)),
                        ),
                      ],
                    ),
                  ),
                ] else ...[
                  // Khi CHƯA ÁP DỤNG: Ô nhập tay tinh gọn + Nút áp dụng (Chuẩn tối giản hiện đại)
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: voucher,
                          textCapitalization: TextCapitalization.characters,
                          onSubmitted: (_) => applyVoucher(),
                          decoration: InputDecoration(
                            isDense: true,
                            hintText: 'Nhập mã khuyến mãi...',
                            prefixIcon: const Icon(Icons.discount_outlined, size: 18),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      FilledButton(
                        style: FilledButton.styleFrom(
                          backgroundColor: brand,
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 11),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        ),
                        onPressed: () => applyVoucher(),
                        child: const Text('Áp dụng', style: TextStyle(fontWeight: FontWeight.bold)),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 14),

        // 3. THẺ THÀNH VIÊN & TÍCH/ĐỔI ĐIỂM
        Card(
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(color: isDark ? darkCardBorder : const Color(0xFFE2E8F0)),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.person_pin_outlined, color: brand, size: 20),
                    SizedBox(width: 8),
                    Expanded(child: Text('Khách hàng thành viên', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14.5))),
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
                          decoration: InputDecoration(
                            isDense: true,
                            hintText: 'Nhập số điện thoại khách để tích điểm...',
                            prefixIcon: const Icon(Icons.phone_outlined, size: 18),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                            border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
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
                      leading: CircleAvatar(
                        backgroundColor: const Color(0xFFE8F5EF),
                        child: Text(
                          text(item['fullName']).isNotEmpty ? text(item['fullName'])[0].toUpperCase() : 'K',
                          style: const TextStyle(fontWeight: FontWeight.bold, color: brand),
                        ),
                      ),
                      title: Text(text(item['fullName']), style: const TextStyle(fontWeight: FontWeight.w700)),
                      subtitle: Text('${text(item['phone'])} · ${integer(item['pointsBalance'])} điểm tích lũy'),
                      trailing: FilledButton.tonal(
                        onPressed: () => setState(() {
                          customer = item;
                          results = [];
                          points = 0;
                          pointsInput.text = '0';
                        }),
                        child: const Text('Chọn khách'),
                      ),
                    ),
                  ),
                  Align(
                    alignment: Alignment.centerLeft,
                    child: TextButton.icon(
                      onPressed: quickCreateCustomer,
                      icon: const Icon(Icons.person_add_alt_1, size: 18),
                      label: const Text('Tạo mới thành viên nhanh'),
                    ),
                  ),
                ] else ...[
                  // Hồ sơ khách đã chọn
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: isDark ? darkCardBorder : const Color(0xFFE2E8F0)),
                    ),
                    child: Row(
                      children: [
                        CircleAvatar(
                          backgroundColor: brand,
                          child: Text(
                            text(customer!['fullName']).isNotEmpty ? text(customer!['fullName'])[0].toUpperCase() : 'K',
                            style: const TextStyle(color: gold, fontWeight: FontWeight.bold),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                text(customer!['fullName']),
                                style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
                              ),
                              Text(
                                '${text(customer!['phone'])} · Điểm khả dụng: ${integer(customer!['pointsBalance'])} đ',
                                style: const TextStyle(fontSize: 12, color: Color(0xFF64748B)),
                              ),
                            ],
                          ),
                        ),
                        TextButton(
                          style: TextButton.styleFrom(foregroundColor: const Color(0xFFDC2626)),
                          onPressed: () => setState(() {
                            customer = null;
                            points = 0;
                            pointsInput.text = '0';
                          }),
                          child: const Text('Bỏ chọn'),
                        ),
                      ],
                    ),
                  ),

                  // Nhập số điểm muốn đổi trực tiếp (không cần các nút tùy chọn rườm rà)
                  if (maxPoints > 0) ...[
                    const SizedBox(height: 12),
                    TextField(
                      controller: pointsInput,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        isDense: true,
                        labelText: 'Nhập điểm muốn dùng (Tối đa $maxPoints điểm)',
                        hintText: 'Nhập số điểm cần đổi...',
                        prefixIcon: const Icon(Icons.stars_rounded, size: 20, color: Color(0xFFD97706)),
                        suffixText: points > 0 ? '-${money(pointDiscount)}' : null,
                        suffixStyle: const TextStyle(fontWeight: FontWeight.w900, color: Color(0xFF16A34A), fontSize: 13),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      onChanged: (val) {
                        final parsed = int.tryParse(val) ?? 0;
                        final clamped = parsed.clamp(0, maxPoints);
                        if (clamped != parsed && val.isNotEmpty) {
                          pointsInput.text = clamped.toString();
                          pointsInput.selection = TextSelection.fromPosition(
                            TextPosition(offset: pointsInput.text.length),
                          );
                        }
                        setState(() {
                          points = clamped;
                          if (method == 'cash') {
                            cash.text = total.toStringAsFixed(0);
                          }
                        });
                      },
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
    final isDark = isDarkMode(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // THẺ PHƯƠNG THỨC THANH TOÁN
        Card(
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(color: isDark ? darkCardBorder : const Color(0xFFE2E8F0)),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.payment_outlined, color: brand, size: 20),
                    SizedBox(width: 8),
                    Expanded(child: Text('Phương thức thanh toán', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14.5))),
                  ],
                ),
                const SizedBox(height: 12),

                // 2 PHƯƠNG THỨC THANH TOÁN (TIỀN MẶT & CHUYỂN KHOẢN VIETQR)
                Row(
                  children: [
                    for (final entry in const {
                      'cash': 'Tiền Mặt\n💵',
                      'transfer': 'Chuyển Khoản VietQR\n📱',
                    }.entries)
                      Expanded(
                        child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 4),
                          child: InkWell(
                            onTap: () => setState(() {
                              method = entry.key;
                              if (method != 'cash') {
                                cash.text = total.toStringAsFixed(0);
                                _startTransferPolling();
                              } else {
                                _pollTimer?.cancel();
                              }
                            }),
                            borderRadius: BorderRadius.circular(12),
                            child: Container(
                              padding: const EdgeInsets.symmetric(vertical: 12),
                              decoration: BoxDecoration(
                                color: method == entry.key
                                    ? const Color(0xFFE8F5EF)
                                    : (isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC)),
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                  color: method == entry.key ? brand : const Color(0xFFE2E8F0),
                                  width: method == entry.key ? 2 : 1,
                                ),
                              ),
                              child: Text(
                                entry.value,
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: method == entry.key ? FontWeight.w900 : FontWeight.w700,
                                  color: method == entry.key ? brand : const Color(0xFF334155),
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),

                // KHỐI TIỀN MẶT + GỢI Ý MỆNH GIÁ + THỐI TIỀN
                if (method == 'cash') ...[
                  const SizedBox(height: 16),
                  const Text('Tiền khách đưa:', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w700)),
                  const SizedBox(height: 6),

                  // Nút gợi ý mệnh giá tiền nhanh (Quick Cash Presets)
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: cashPresets.map((amt) {
                        final isExact = amt == total;
                        return Padding(
                          padding: const EdgeInsets.only(right: 6, bottom: 6),
                          child: ActionChip(
                            label: Text(
                              isExact ? 'Đủ tiền (${money(amt)})' : money(amt),
                              style: TextStyle(
                                fontSize: 11.5,
                                fontWeight: FontWeight.w800,
                                color: isExact ? brand : const Color(0xFF334155),
                              ),
                            ),
                            backgroundColor: isExact ? const Color(0xFFE8F5EF) : const Color(0xFFF1F5F9),
                            side: BorderSide(color: isExact ? brand : const Color(0xFFCBD5E1)),
                            onPressed: () => setState(() => cash.text = amt.toStringAsFixed(0)),
                          ),
                        );
                      }).toList(),
                    ),
                  ),

                  const SizedBox(height: 6),
                  TextField(
                    controller: cash,
                    keyboardType: TextInputType.number,
                    onChanged: (_) => setState(() {}),
                    style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800, fontFamily: 'monospace'),
                    decoration: InputDecoration(
                      labelText: 'Số tiền khách đưa (VNĐ)',
                      suffixText: 'đ',
                      prefixIcon: const Icon(Icons.attach_money),
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                  ),
                  const SizedBox(height: 10),

                  // KHỐI HIỂN THỊ TIỀN THỪA TRẢ KHÁCH (NỔI BẬT KHÔNG THỐI NHẦM)
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: paid >= total ? const Color(0xFFECFDF5) : const Color(0xFFFEF2F2),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: paid >= total ? const Color(0xFFA7F3D0) : const Color(0xFFFECACA),
                      ),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(child: Row(
                          children: [
                            Icon(
                              paid >= total ? Icons.check_circle : Icons.warning_amber_rounded,
                              size: 20,
                              color: paid >= total ? const Color(0xFF059669) : const Color(0xFFDC2626),
                            ),
                            const SizedBox(width: 8),
                            Expanded(child: Text(
                              paid >= total ? 'Tiền thừa trả khách:' : 'Khách đưa còn thiếu:',
                              style: TextStyle(
                                fontWeight: FontWeight.w700,
                                fontSize: 13,
                                color: paid >= total ? const Color(0xFF065F46) : const Color(0xFF991B1B),
                              ),
                            )),
                          ],
                        )),
                        const SizedBox(width: 10),
                        Flexible(child: Text(
                          money((paid - total).abs()),
                          textAlign: TextAlign.right,
                          style: TextStyle(
                            fontWeight: FontWeight.w900,
                            fontSize: 17,
                            fontFamily: 'monospace',
                            color: paid >= total ? const Color(0xFF059669) : const Color(0xFFDC2626),
                          ),
                        )),
                      ],
                    ),
                  ),
                ],

                // KHỐI CHUYỂN KHOẢN VIETQR
                if (method == 'transfer') ...[
                  const SizedBox(height: 16),
                  // NÚT PHÓNG TO TOÀN MÀN HÌNH ĐƯA KHÁCH QUÉT
                  FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: const Color(0xFF0284C7),
                      foregroundColor: Colors.white,
                      minimumSize: const Size.fromHeight(52),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    ),
                    onPressed: () => showCustomerFullscreenQr(context),
                    icon: const Text('📱', style: TextStyle(fontSize: 22)),
                    label: const Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text('ĐƯA KHÁCH QUÉT (TOÀN MÀN HÌNH)', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 13)),
                        Text('Phóng to 300px · Số tiền xanh · Che thao tác nội bộ', style: TextStyle(fontSize: 10.5, color: Colors.white70)),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Chỉ báo radar pulse
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF0FDF4),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: const Color(0xFFBBF7D0)),
                    ),
                    child: const Row(
                      children: [
                        SizedBox.square(
                          dimension: 16,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFF16A34A)),
                        ),
                        SizedBox(width: 10),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('Hệ thống đang tự động nhận diện thanh toán...', style: TextStyle(fontWeight: FontWeight.w800, fontSize: 12, color: Color(0xFF15803D))),
                              Text('Vui lòng không tắt màn hình khi khách đang chuyển tiền', style: TextStyle(fontSize: 10.5, color: Color(0xFF166534))),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Thẻ mã QR tại chỗ
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: const Color(0xFFF4F8F6),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: cardBorder),
                    ),
                    child: Column(
                      children: [
                        InkWell(
                          onTap: () => showCustomerFullscreenQr(context),
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(8),
                            child: _buildVietQrImage(
                              width: 180,
                              height: 180,
                            ),
                          ),
                        ),
                        const SizedBox(height: 10),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              'STK: ${text(activeVietQr['accountNo'])}',
                              style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 13.5, fontFamily: 'monospace', color: Color(0xFF0284C7)),
                            ),
                            const SizedBox(width: 6),
                            InkWell(
                              onTap: () {
                                Clipboard.setData(ClipboardData(text: text(activeVietQr['accountNo'])));
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(content: Text('Đã sao chép số tài khoản!'), duration: Duration(seconds: 2)),
                                );
                              },
                              borderRadius: BorderRadius.circular(4),
                              child: const Padding(
                                padding: EdgeInsets.all(4),
                                child: Icon(Icons.copy, size: 16, color: Color(0xFF0284C7)),
                              ),
                            ),
                          ],
                        ),
                        Text(
                          'Ngân hàng: ${text(activeVietQr['bankId'])} · Chủ TK: ${text(activeVietQr['accountName'])}',
                          style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78), fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 4),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              'Nội dung: ${text(activeVietQr['memo'])}',
                              style: const TextStyle(fontWeight: FontWeight.w900, color: Color(0xFFDC2626), fontSize: 13.5, fontFamily: 'monospace'),
                            ),
                            const SizedBox(width: 6),
                            InkWell(
                              onTap: () {
                                Clipboard.setData(ClipboardData(text: text(activeVietQr['memo'])));
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(content: Text('Đã sao chép nội dung chuyển khoản!'), duration: Duration(seconds: 2)),
                                );
                              },
                              borderRadius: BorderRadius.circular(4),
                              child: const Padding(
                                padding: EdgeInsets.all(4),
                                child: Icon(Icons.copy, size: 16, color: Color(0xFFDC2626)),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),

        // Nút thanh toán cho tablet
        if (MediaQuery.sizeOf(context).width >= 750) ...[
          const SizedBox(height: 16),
          FilledButton.icon(
            style: FilledButton.styleFrom(
              backgroundColor: brand,
              foregroundColor: Colors.white,
              minimumSize: const Size.fromHeight(54),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
            onPressed: paying || !canCheckout ? null : pay,
            icon: paying
                ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.check_circle_outline, size: 22),
            label: Text(
              paying ? 'Đang xuất hóa đơn...' : 'HOÀN TẤT THANH TOÁN · ${money(total)}',
              style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 16),
            ),
          ),
        ],
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final isTablet = MediaQuery.sizeOf(context).width >= 750;
    final isDark = isDarkMode(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thanh toán hóa đơn (POS)', style: TextStyle(fontWeight: FontWeight.w900)),
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
              ? RetryState(message: error!, onRetry: load)
              : isTablet
                  // BỐ CỤC TABLET: 2 CỘT RÕ RÀNG
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
                  // BỐ CỤC ĐIỆN THOẠI: 1 CỘT CUỘN
                  : ListView(
                      padding: const EdgeInsets.fromLTRB(16, 16, 16, 100),
                      children: [
                        _buildLeftColumn(),
                        const SizedBox(height: 14),
                        _buildRightColumn(),
                      ],
                    ),

      // STICKY BOTTOM ACTION BAR CHO MOBILE (THU NGÂN DỄ DÀNG BẤM THANH TOÁN KHÔNG CẦN CUỘN)
      bottomNavigationBar: isTablet || loading || error != null
          ? null
          : Container(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 16),
              decoration: BoxDecoration(
                color: isDark ? darkSurface : Colors.white,
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: isDark ? 0.4 : 0.08),
                    blurRadius: 10,
                    offset: const Offset(0, -3),
                  ),
                ],
                border: Border(
                  top: BorderSide(color: isDark ? darkCardBorder : const Color(0xFFE2E8F0)),
                ),
              ),
              child: SafeArea(
                child: Row(
                  children: [
                    Expanded(
                      flex: 4,
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text('TỔNG PHẢI THU', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF64748B))),
                          Text(
                            money(total),
                            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 18, color: brand, fontFamily: 'monospace'),
                          ),
                          if (method == 'cash' && paid >= total)
                            Text('Thối: ${money(paid - total)}', style: const TextStyle(fontSize: 11, color: Color(0xFF059669), fontWeight: FontWeight.w700)),
                        ],
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      flex: 6,
                      child: FilledButton.icon(
                        style: FilledButton.styleFrom(
                          backgroundColor: brand,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        onPressed: paying || !canCheckout ? null : pay,
                        icon: paying
                            ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                            : const Icon(Icons.check_circle_outline, size: 20),
                        label: Text(
                          paying ? 'Đang xử lý...' : 'THANH TOÁN',
                          style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 15, letterSpacing: 0.5),
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

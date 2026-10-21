import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import '../staff/shared_widgets.dart';
import 'invoice_detail_page.dart';

class InvoicesPage extends StatefulWidget {
  const InvoicesPage({super.key, required this.api});
  final ApiClient api;

  @override
  State<InvoicesPage> createState() => _InvoicesPageState();
}

class _InvoicesPageState extends State<InvoicesPage> {
  List<Map<String, dynamic>> invoices = [];
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
      final values = jsonMaps(
        await widget.api.get(
          '/staff/payment/recent-invoices',
          query: {'limit': '40'},
        ),
      );
      if (mounted) setState(() => invoices = values);
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isTablet = MediaQuery.sizeOf(context).width >= 750;

    return Scaffold(
      appBar: AppBar(
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Hóa đơn gần đây', style: TextStyle(fontWeight: FontWeight.w900)),
            Text(
              'Tra cứu lịch sử thanh toán & in lại phiếu bill',
              style: TextStyle(fontSize: 12, color: Color(0xFF6F7E78), fontWeight: FontWeight.normal),
            ),
          ],
        ),
        actions: [
          IconButton(onPressed: load, tooltip: 'Làm mới', icon: const Icon(Icons.refresh)),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? RetryState(message: error!, onRetry: load)
          : invoices.isEmpty
          ? const EmptyState(message: 'Chưa có hóa đơn nào được thanh toán gần đây.')
          : Center(
              child: ConstrainedBox(
                constraints: BoxConstraints(maxWidth: isTablet ? 900 : double.infinity),
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                  itemCount: invoices.length,
                  separatorBuilder: (_, index) => const SizedBox(height: 8),
                  itemBuilder: (_, index) {
                    final invoice = invoices[index];
                    final paymentMethod = text(invoice['paymentMethod'], 'cash');
                    return Card(
                      child: InkWell(
                        borderRadius: BorderRadius.circular(12),
                        onTap: () {
                          final id = integer(invoice['invoiceId']);
                          if (id > 0) {
                            Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => InvoiceDetailPage(api: widget.api, invoiceId: id),
                              ),
                            );
                          }
                        },
                        child: Padding(
                          padding: const EdgeInsets.all(14),
                          child: Row(
                            children: [
                              Container(
                                width: 44,
                                height: 44,
                                decoration: BoxDecoration(
                                  color: brand.withValues(alpha: 0.08),
                                  borderRadius: BorderRadius.circular(10),
                                ),
                                child: const Icon(Icons.receipt_long, color: brand, size: 24),
                              ),
                              const SizedBox(width: 14),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: [
                                        Text(
                                          text(invoice['invoiceCode']),
                                          style: const TextStyle(
                                            fontWeight: FontWeight.w900,
                                            fontSize: 14,
                                            color: brand,
                                          ),
                                        ),
                                        const SizedBox(width: 6),
                                        Text(
                                          '· ${text(invoice['tableDisplay'])}',
                                          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 3),
                                    Text(
                                      '${shortDate(invoice['issuedAt'])} · ${text(invoice['customerName'], 'Khách vãng lai')}',
                                      style: const TextStyle(fontSize: 12, color: Color(0xFF6F7E78)),
                                    ),
                                    Text(
                                      'Thu ngân: ${text(invoice['cashierName'], 'Admin')} · ${_paymentIconText(paymentMethod)}',
                                      style: const TextStyle(fontSize: 11, color: Color(0xFF8A9992)),
                                    ),
                                  ],
                                ),
                              ),
                              Column(
                                crossAxisAlignment: CrossAxisAlignment.end,
                                children: [
                                  Text(
                                    money(number(invoice['totalAmount'])),
                                    style: const TextStyle(
                                      fontWeight: FontWeight.w900,
                                      fontSize: 15,
                                      color: gold,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  const Icon(Icons.chevron_right, color: Color(0xFFB0BEC5), size: 20),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),
    );
  }

  String _paymentIconText(String m) {
    return switch (m) {
      'cash' => 'Tiền mặt',
      'transfer' => 'Chuyển khoản QR',
      'card' => 'Thẻ',
      'e_wallet' => 'Ví điện tử',
      _ => m,
    };
  }
}

import 'package:flutter/material.dart';

import '../../core/staff_core.dart';
import '../account/account_page.dart';
import '../home/home_page.dart';
import '../invoices/invoices_page.dart';
import '../reservations/reservations_page.dart';
import 'tables_page.dart';

class StaffShell extends StatefulWidget {
  const StaffShell({
    super.key,
    required this.session,
    required this.onLogout,
    this.onSessionUpdated,
  });

  final StaffSession session;
  final VoidCallback onLogout;
  final ValueChanged<StaffSession>? onSessionUpdated;

  @override
  State<StaffShell> createState() => _StaffShellState();
}

class _StaffShellState extends State<StaffShell> {
  int index = 0;
  String? tablesFilter;
  String? tablesActionPrompt;

  void _navigateToTables({String? filter, String? prompt}) {
    setState(() {
      index = 1;
      tablesFilter = filter;
      tablesActionPrompt = prompt;
    });
  }

  @override
  Widget build(BuildContext context) {
    final isWide = MediaQuery.sizeOf(context).width >= 800;
    final isDark = isDarkMode(context);

    final pages = [
      HomePage(
        session: widget.session,
        onTables: () => _navigateToTables(filter: 'all', prompt: null),
        onReservations: () => setState(() => index = 2),
        onInvoices: () => setState(() => index = 3),
        onOpenNewOrder: () => _navigateToTables(filter: 'empty', prompt: 'open_table'),
        onServingOrder: () => _navigateToTables(filter: 'serving', prompt: 'add_order'),
        onTransferMerge: () => _navigateToTables(filter: 'serving', prompt: 'transfer_merge'),
        onPayment: () => _navigateToTables(filter: 'serving', prompt: 'payment'),
        onKitchenReady: () => _navigateToTables(filter: 'ready', prompt: 'kitchen_ready'),
      ),
      TablesPage(
        api: widget.session.api,
        initialFilter: tablesFilter,
        actionPrompt: tablesActionPrompt,
        onClearPrompt: () {
          setState(() {
            tablesFilter = 'all';
            tablesActionPrompt = null;
          });
        },
      ),
      ReservationsPage(api: widget.session.api),
      InvoicesPage(api: widget.session.api, session: widget.session),
      AccountPage(
        session: widget.session,
        onLogout: widget.onLogout,
        onSessionUpdated: widget.onSessionUpdated,
      ),
    ];

    if (isWide) {
      // Giao diện Tablet / Màn hình rộng (NavigationRail)
      return Scaffold(
        body: SafeArea(
          child: Row(
            children: [
              NavigationRail(
                selectedIndex: index,
                onDestinationSelected: (value) => setState(() => index = value),
                backgroundColor: themedSurface(context),
                leading: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  child: Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: isDark ? const Color(0xFF0F172A) : brand,
                      borderRadius: BorderRadius.circular(10),
                      border: isDark ? Border.all(color: gold.withValues(alpha: 0.4)) : null,
                    ),
                    child: const Icon(
                      Icons.restaurant_menu,
                      color: gold,
                      size: 26,
                    ),
                  ),
                ),
                indicatorColor: gold.withValues(alpha: 0.25),
                selectedIconTheme: IconThemeData(color: isDark ? gold : brand, size: 24),
                unselectedIconTheme: IconThemeData(
                  color: isDark ? darkTextSecondary : const Color(0xFF75857F),
                  size: 22,
                ),
                selectedLabelTextStyle: TextStyle(
                  color: isDark ? gold : brand,
                  fontWeight: FontWeight.w800,
                  fontSize: 12,
                ),
                unselectedLabelTextStyle: TextStyle(
                  color: isDark ? darkTextSecondary : const Color(0xFF75857F),
                  fontWeight: FontWeight.w600,
                  fontSize: 12,
                ),
                labelType: NavigationRailLabelType.all,
                destinations: const [
                  NavigationRailDestination(
                    icon: Icon(Icons.dashboard_outlined),
                    selectedIcon: Icon(Icons.dashboard),
                    label: Text('Tổng quan'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.table_restaurant_outlined),
                    selectedIcon: Icon(Icons.table_restaurant),
                    label: Text('Sơ đồ bàn'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.event_seat_outlined),
                    selectedIcon: Icon(Icons.event_seat),
                    label: Text('Đặt bàn'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.receipt_long_outlined),
                    selectedIcon: Icon(Icons.receipt_long),
                    label: Text('Hóa đơn'),
                  ),
                  NavigationRailDestination(
                    icon: Icon(Icons.person_outline),
                    selectedIcon: Icon(Icons.person),
                    label: Text('Tài khoản'),
                  ),
                ],
              ),
              VerticalDivider(width: 1, thickness: 1, color: themedBorder(context)),
              Expanded(child: IndexedStack(index: index, children: pages)),
            ],
          ),
        ),
      );
    }

    // Giao diện Điện thoại (NavigationBar)
    return Scaffold(
      body: SafeArea(child: IndexedStack(index: index, children: pages)),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => setState(() => index = value),
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.dashboard_outlined),
            selectedIcon: Icon(Icons.dashboard),
            label: 'Tổng quan',
          ),
          NavigationDestination(
            icon: Icon(Icons.table_restaurant_outlined),
            selectedIcon: Icon(Icons.table_restaurant),
            label: 'Sơ đồ bàn',
          ),
          NavigationDestination(
            icon: Icon(Icons.event_seat_outlined),
            selectedIcon: Icon(Icons.event_seat),
            label: 'Đặt bàn',
          ),
          NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            selectedIcon: Icon(Icons.receipt_long),
            label: 'Hóa đơn',
          ),
          NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Tài khoản',
          ),
        ],
      ),
    );
  }
}

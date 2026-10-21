import 'package:flutter_test/flutter_test.dart';
import 'package:restaurant_staff_app/main.dart';

void main() {
  testWidgets('shows the staff sign-in screen', (WidgetTester tester) async {
    await tester.pumpWidget(const StaffApp());

    expect(find.text('DOLA STAFF'), findsOneWidget);
    expect(find.text('Đăng nhập'), findsWidgets);
  });
}

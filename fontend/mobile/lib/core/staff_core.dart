import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

// Màu sắc nhận diện Dola Restaurant Brand
const brand = Color(0xFF143B36); // Deep Teal / Forest Green chính
const brandLight = Color(0xFF1E524C);
const gold = Color(0xFFD69C52); // Vàng Gold thương hiệu Dola
const canvas = Color(0xFFF5F8F6);
const cardBorder = Color(0xFFE1E9E4);

// Typography Colors
const textPrimary = Color(0xFF172033); // Tiêu đề & giá tiền đậm nét
const textSecondary = Color(0xFF64748B); // Mô tả / chữ phụ

// Màu sắc Dark Mode chuẩn UI/UX Pro Max (OLED & Midnight Slate)
const darkCanvas = Color(0xFF0F172A); // Slate 900
const darkSurface = Color(0xFF1E293B); // Slate 800
const darkCardBorder = Color(0xFF334155); // Slate 700
const darkTextPrimary = Color(0xFFF8FAFC); // Slate 50
const darkTextSecondary = Color(0xFF94A3B8); // Slate 400

/// Trình quản lý trạng thái Theme (Sáng / Tối) toàn hệ thống
class ThemeController extends ChangeNotifier {
  ThemeController._();
  static final ThemeController instance = ThemeController._();

  ThemeMode _themeMode = ThemeMode.light;
  ThemeMode get themeMode => _themeMode;
  bool get isDark => _themeMode == ThemeMode.dark;

  void toggleTheme() {
    _themeMode = _themeMode == ThemeMode.dark ? ThemeMode.light : ThemeMode.dark;
    notifyListeners();
  }

  void setThemeMode(ThemeMode mode) {
    if (_themeMode != mode) {
      _themeMode = mode;
      notifyListeners();
    }
  }
}

class StaffSession {
  const StaffSession({
    required this.api,
    required this.fullName,
    required this.role,
    required this.userId,
    this.email = '',
    this.phone = '',
    this.avatarUrl = '',
  });

  final ApiClient api;
  final String fullName;
  final String role;
  final int userId;
  final String email;
  final String phone;
  final String avatarUrl;

  StaffSession copyWith({
    ApiClient? api,
    String? fullName,
    String? role,
    int? userId,
    String? email,
    String? phone,
    String? avatarUrl,
  }) {
    return StaffSession(
      api: api ?? this.api,
      fullName: fullName ?? this.fullName,
      role: role ?? this.role,
      userId: userId ?? this.userId,
      email: email ?? this.email,
      phone: phone ?? this.phone,
      avatarUrl: avatarUrl ?? this.avatarUrl,
    );
  }
}

class ApiException implements Exception {
  ApiException(this.message);
  final String message;
}

class ApiClient {
  ApiClient(String server) : server = server.replaceFirst(RegExp(r'/+$'), '');

  final String server;
  String? token;
  VoidCallback? onUnauthorized;

  Future<dynamic> get(String path, {Map<String, String>? query}) {
    return _send('GET', path, query: query);
  }

  Future<dynamic> post(String path, {Object? data}) {
    return _send('POST', path, data: data);
  }

  Future<dynamic> put(String path, {Object? data}) {
    return _send('PUT', path, data: data);
  }

  Future<dynamic> patch(String path, {Object? data}) {
    return _send('PATCH', path, data: data);
  }

  Future<dynamic> delete(String path) => _send('DELETE', path);

  /// Kiểm tra nhanh kết nối tới máy chủ API
  Future<bool> checkConnection() async {
    try {
      final uri = Uri.parse('$server/health');
      final res = await http.get(uri).timeout(const Duration(seconds: 4));
      return res.statusCode >= 200 && res.statusCode < 400;
    } catch (_) {
      try {
        // Thử ping trực tiếp gốc server
        final rootUri = Uri.parse(server.replaceFirst(RegExp(r'/api$'), ''));
        final res = await http.get(rootUri).timeout(const Duration(seconds: 4));
        return res.statusCode >= 200 && res.statusCode < 500;
      } catch (_) {
        return false;
      }
    }
  }

  Future<dynamic> _send(
    String method,
    String path, {
    Object? data,
    Map<String, String>? query,
  }) async {
    final uri = Uri.parse('$server$path').replace(queryParameters: query);
    final headers = <String, String>{'Accept': 'application/json'};
    if (data != null) headers['Content-Type'] = 'application/json';
    if (token != null) headers['Authorization'] = 'Bearer $token';

    try {
      final body = data == null ? null : jsonEncode(data);
      final request = switch (method) {
        'GET' => http.get(uri, headers: headers),
        'POST' => http.post(uri, headers: headers, body: body),
        'PUT' => http.put(uri, headers: headers, body: body),
        'PATCH' => http.patch(uri, headers: headers, body: body),
        'DELETE' => http.delete(uri, headers: headers),
        _ => throw StateError('HTTP method is not supported'),
      };

      final response = await request.timeout(const Duration(seconds: 15));
      if (response.statusCode == 401 && token != null) {
        token = null;
        onUnauthorized?.call();
        throw ApiException('Phiên đăng nhập đã hết hạn hoặc tài khoản đã bị khóa. Vui lòng đăng nhập lại.');
      }
      if (response.statusCode == 403) {
        throw ApiException('Tài khoản không có quyền thực hiện thao tác này.');
      }
      final decoded = response.body.isEmpty ? null : jsonDecode(response.body);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        final message = decoded is Map ? decoded['message']?.toString() : null;
        throw ApiException(
          message ?? 'Máy chủ trả về lỗi ${response.statusCode}.',
        );
      }
      return decoded;
    } on ApiException {
      rethrow;
    } catch (_) {
      throw ApiException(
        'Không kết nối được máy chủ. Kiểm tra địa chỉ API và kết nối mạng.',
      );
    }
  }
}

// Helpers chuyển đổi dữ liệu an toàn
Map<String, dynamic> jsonMap(dynamic value) =>
    Map<String, dynamic>.from(value as Map);
List<Map<String, dynamic>> jsonMaps(dynamic value) =>
    (value as List<dynamic>? ?? const []).map(jsonMap).toList();
String text(dynamic value, [String fallback = '']) =>
    value?.toString() ?? fallback;
num number(dynamic value) => value is num ? value : num.tryParse('$value') ?? 0;
int integer(dynamic value) => number(value).toInt();

String money(num value) {
  return '${value.toStringAsFixed(0).replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => '.')} đ';
}

DateTime? parseDateTime(dynamic value) {
  if (value == null) return null;
  var str = value.toString().trim();
  if (str.isEmpty) return null;
  // If backend string has no timezone indicator, treat as UTC
  if (!str.endsWith('Z') && !str.contains('+') && !RegExp(r'-\d{2}:\d{2}$').hasMatch(str)) {
    str += 'Z';
  }
  return DateTime.tryParse(str)?.toLocal();
}

String shortDate(dynamic value) {
  final date = parseDateTime(value);
  if (date == null) return '—';
  String two(int v) => v.toString().padLeft(2, '0');
  return '${two(date.day)}/${two(date.month)} ${two(date.hour)}:${two(date.minute)}';
}

void showMessage(BuildContext context, String message, {bool isError = false}) {
  ScaffoldMessenger.of(context).hideCurrentSnackBar();
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(message),
      backgroundColor: isError ? const Color(0xFFC62828) : brand,
      behavior: SnackBarBehavior.floating,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
    ),
  );
}

String tableStatusText(String status) =>
    const {
      'empty': 'Bàn trống',
      'serving': 'Đang phục vụ',
      'reserved': 'Đã đặt trước',
      'cleaning': 'Đang dọn dẹp',
      'disabled': 'Ngừng sử dụng',
    }[status] ??
    status;

Color tableColor(String status) =>
    const {
      'empty': Color(0xFF2E7D32), // Xanh lá tươi
      'serving': Color(0xFFE65100), // Cam ấm
      'reserved': Color(0xFF1565C0), // Xanh dương
      'cleaning': Color(0xFF6A1B9A), // Tím dọn dẹp
      'disabled': Color(0xFF757575),
    }[status] ??
    const Color(0xFF757575);

String itemStatusText(String status) =>
    const {
      'pending': 'Chờ gửi bếp',
      'confirmed': 'Đã gửi bếp',
      'cooking': 'Đang nấu',
      'ready': 'Bếp báo xong',
      'served': 'Đã phục vụ',
      'cancelled': 'Đã hủy',
      'open': 'Đang mở',
      'preparing': 'Đang chuẩn bị',
      'serving': 'Đang phục vụ',
    }[status] ??
    status;

Color itemColor(String status) =>
    const {
      'pending': Color(0xFFB78103),
      'confirmed': Color(0xFF1565C0),
      'cooking': Color(0xFF7B1FA2),
      'ready': Color(0xFF2E7D32),
      'served': Color(0xFF455A64),
      'cancelled': Color(0xFFC62828),
      'open': Color(0xFFB78103),
      'preparing': Color(0xFF1565C0),
      'serving': Color(0xFF2E7D32),
    }[status] ??
    const Color(0xFF64736D);

bool isTabletScreen(BuildContext context) =>
    MediaQuery.sizeOf(context).width >= 700;

bool isDarkMode(BuildContext context) =>
    Theme.of(context).brightness == Brightness.dark;

Color themedSurface(BuildContext context) =>
    isDarkMode(context) ? darkSurface : Colors.white;

Color themedBorder(BuildContext context) =>
    isDarkMode(context) ? darkCardBorder : cardBorder;

Color themedText(BuildContext context) =>
    isDarkMode(context) ? darkTextPrimary : const Color(0xFF1E293B);

Color themedSubText(BuildContext context) =>
    isDarkMode(context) ? darkTextSecondary : const Color(0xFF64748B);

/// Widget tạo hiệu ứng Skeleton Loading Cards đồng bộ Deep Teal & Slate
class SkeletonBox extends StatefulWidget {
  const SkeletonBox({
    super.key,
    this.width,
    this.height,
    this.borderRadius = 8,
  });

  final double? width;
  final double? height;
  final double borderRadius;

  @override
  State<SkeletonBox> createState() => _SkeletonBoxState();
}

class _SkeletonBoxState extends State<SkeletonBox>
    with SingleTickerProviderStateMixin {
  late AnimationController _anim;

  @override
  void initState() {
    super.initState();
    _anim = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _anim.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final baseColor =
        isDark ? const Color(0xFF1E293B) : const Color(0xFFE2E8F0);
    final highlightColor =
        isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9);

    return AnimatedBuilder(
      animation: _anim,
      builder: (context, child) {
        return Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            color: Color.lerp(baseColor, highlightColor, _anim.value),
            borderRadius: BorderRadius.circular(widget.borderRadius),
          ),
        );
      },
    );
  }
}


import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'staff_core.dart';

/// Quản lý phiên làm việc của nhân viên và lưu trữ cấu hình cục bộ
class SessionManager {
  SessionManager._();

  static const String _keyToken = 'dola_staff_token';
  static const String _keyServerUrl = 'dola_staff_server_url';
  static const String _keyUserId = 'dola_staff_user_id';
  static const String _keyFullName = 'dola_staff_full_name';
  static const String _keyRole = 'dola_staff_role';
  static const String _keyEmail = 'dola_staff_email';
  static const String _keyPhone = 'dola_staff_phone';
  static const String _keyAvatarUrl = 'dola_staff_avatar_url';
  static const String _keySavedEmail = 'dola_staff_saved_email';
  static const String _keyRememberMe = 'dola_staff_remember_me';
  static const String _keyIntroSeen = 'dola_staff_intro_seen';

  /// Lấy địa chỉ máy chủ mặc định theo môi trường
  static String getDefaultServerUrl() {
    const configured = String.fromEnvironment('API_BASE_URL');
    if (configured.isNotEmpty) return configured;
    if (kIsWeb) return 'http://localhost:5079/api';
    try {
      if (defaultTargetPlatform == TargetPlatform.android) {
        return 'http://10.0.2.2:5079/api';
      }
    } catch (_) {}
    return 'http://localhost:5079/api';
  }

  /// Lấy địa chỉ máy chủ API đã lưu
  static Future<String> getServerUrl() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final saved = prefs.getString(_keyServerUrl);
      if (saved != null && saved.trim().isNotEmpty) {
        return saved.trim();
      }
    } catch (e) {
      debugPrint('Lỗi đọc ServerUrl từ SharedPreferences: $e');
    }
    return getDefaultServerUrl();
  }

  /// Lưu địa chỉ máy chủ API
  static Future<void> saveServerUrl(String url) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_keyServerUrl, url.trim());
    } catch (e) {
      debugPrint('Lỗi lưu ServerUrl: $e');
    }
  }

  /// Lưu phiên làm việc của nhân viên
  static Future<void> saveSession(
    StaffSession session, {
    bool rememberMe = true,
  }) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_keyServerUrl, session.api.server);
      await prefs.setString(_keyToken, session.api.token ?? '');
      await prefs.setInt(_keyUserId, session.userId);
      await prefs.setString(_keyFullName, session.fullName);
      await prefs.setString(_keyRole, session.role);
      await prefs.setString(_keyEmail, session.email);
      await prefs.setString(_keyPhone, session.phone);
      await prefs.setString(_keyAvatarUrl, session.avatarUrl);
      await prefs.setBool(_keyRememberMe, rememberMe);

      if (rememberMe && session.email.isNotEmpty) {
        await prefs.setString(_keySavedEmail, session.email);
      }
    } catch (e) {
      debugPrint('Lỗi lưu Session: $e');
    }
  }

  /// Lấy thông tin phiên đã lưu trong bộ nhớ máy (chưa qua kiểm tra API)
  static Future<StaffSession?> getStoredSession() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString(_keyToken);
      if (token == null || token.trim().isEmpty) return null;

      final server = prefs.getString(_keyServerUrl) ?? getDefaultServerUrl();
      final userId = prefs.getInt(_keyUserId) ?? 0;
      final fullName = prefs.getString(_keyFullName) ?? 'Nhân viên';
      final role = prefs.getString(_keyRole) ?? 'staff';
      final email = prefs.getString(_keyEmail) ?? '';
      final phone = prefs.getString(_keyPhone) ?? '';
      final avatarUrl = prefs.getString(_keyAvatarUrl) ?? '';

      final api = ApiClient(server);
      api.token = token;

      return StaffSession(
        api: api,
        fullName: fullName,
        role: role,
        userId: userId,
        email: email,
        phone: phone,
        avatarUrl: avatarUrl,
      );
    } catch (e) {
      debugPrint('Lỗi đọc Session: $e');
      return null;
    }
  }

  /// Kiểm tra tính hợp lệ của phiên làm việc với máy chủ
  /// Nếu token còn hạn và tài khoản hoạt động -> Trả về StaffSession mới nhất
  /// Nếu hết hạn / bị khóa -> Xóa token và trả về null
  static Future<StaffSession?> verifyAndRestoreSession() async {
    final stored = await getStoredSession();
    if (stored == null) return null;

    try {
      // Gọi thử endpoint /auth/me để kiểm tra token
      final res = await stored.api.get('/auth/me');
      if (res is Map) {
        final data = jsonMap(res);
        final roleCode = text(data['roleCode']).toLowerCase();

        // Kiểm tra quyền nhân viên / quản lý
        if (!{'staff', 'manager', 'admin'}.contains(roleCode)) {
          await clearSession(keepSavedEmail: true);
          return null;
        }

        final updatedSession = stored.copyWith(
          fullName: text(data['fullName'], stored.fullName),
          email: text(data['email'], stored.email),
          phone: text(data['phone'], stored.phone),
          avatarUrl: text(data['avatarUrl'], stored.avatarUrl),
          role: roleCode,
          userId: integer(data['userId']),
        );

        // Cập nhật lại thông tin mới nhất vào SharedPreferences
        await saveSession(updatedSession);
        return updatedSession;
      }
    } catch (e) {
      debugPrint('Xác thực phiên với máy chủ không thành công: $e');
      // Nếu là lỗi 401 Unauthorized thì xóa phiên
      if (e is ApiException && e.message.contains('hết hạn')) {
        await clearSession(keepSavedEmail: true);
        return null;
      }
      // Nếu là lỗi mạng tạm thời (máy chủ chưa bật hoặc rớt wifi), vẫn giữ phiên lưu cục bộ
      return stored;
    }

    return stored;
  }

  /// Xóa phiên đăng nhập (khi nhân viên Đăng xuất)
  static Future<void> clearSession({bool keepSavedEmail = true}) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_keyToken);
      await prefs.remove(_keyUserId);
      await prefs.remove(_keyFullName);
      await prefs.remove(_keyRole);
      await prefs.remove(_keyEmail);
      await prefs.remove(_keyPhone);
      await prefs.remove(_keyAvatarUrl);

      if (!keepSavedEmail) {
        await prefs.remove(_keySavedEmail);
        await prefs.remove(_keyRememberMe);
      }
    } catch (e) {
      debugPrint('Lỗi xóa Session: $e');
    }
  }

  /// Lấy email đã lưu trước đó cho ô đăng nhập
  static Future<String> getSavedEmail() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.getString(_keySavedEmail) ?? '';
    } catch (_) {
      return '';
    }
  }

  /// Lấy trạng thái ghi nhớ đăng nhập
  static Future<bool> getRememberMe() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.getBool(_keyRememberMe) ?? true;
    } catch (_) {
      return true;
    }
  }

  /// Đánh dấu đã xem intro
  static Future<void> setIntroSeen(bool seen) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool(_keyIntroSeen, seen);
    } catch (_) {}
  }

  /// Kiểm tra đã xem intro chưa
  static Future<bool> isIntroSeen() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.getBool(_keyIntroSeen) ?? false;
    } catch (_) {
      return false;
    }
  }
}

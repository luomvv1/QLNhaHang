import 'package:flutter/material.dart';
import 'package:google_sign_in/google_sign_in.dart';

import '../../core/session_manager.dart';
import '../../core/staff_core.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({
    super.key,
    required this.onLoggedIn,
    this.onBackToIntro,
  });

  final ValueChanged<StaffSession> onLoggedIn;
  final VoidCallback? onBackToIntro;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final email = TextEditingController();
  final password = TextEditingController();
  final server = TextEditingController();

  bool loading = false;
  bool obscurePassword = true;
  bool rememberMe = true;
  bool showServer = false;
  bool checkingConnection = false;
  String? connectionStatus;
  bool isConnected = false;
  String? error;

  @override
  void initState() {
    super.initState();
    _loadInitialSettings();
  }

  Future<void> _loadInitialSettings() async {
    final savedServer = await SessionManager.getServerUrl();
    final savedEmail = await SessionManager.getSavedEmail();
    final savedRemember = await SessionManager.getRememberMe();

    if (mounted) {
      setState(() {
        server.text = savedServer;
        if (savedEmail.isNotEmpty) {
          email.text = savedEmail;
        }
        rememberMe = savedRemember;
      });
    }
  }

  @override
  void dispose() {
    email.dispose();
    password.dispose();
    server.dispose();
    super.dispose();
  }

  Future<void> testConnection() async {
    setState(() {
      checkingConnection = true;
      connectionStatus = null;
    });
    final api = ApiClient(server.text.trim());
    final ok = await api.checkConnection();
    if (mounted) {
      setState(() {
        checkingConnection = false;
        isConnected = ok;
        connectionStatus = ok
            ? 'Kết nối máy chủ Dola API thành công!'
            : 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra cổng 5079.';
      });
      if (ok) {
        await SessionManager.saveServerUrl(server.text.trim());
      }
    }
  }

  Future<void> login() async {
    final emailText = email.text.trim();
    final passText = password.text;

    if (emailText.isEmpty || passText.isEmpty) {
      setState(() {
        error = 'Vui lòng nhập đầy đủ Email và Mật khẩu ca làm.';
      });
      return;
    }

    FocusScope.of(context).unfocus();
    setState(() {
      loading = true;
      error = null;
    });

    final serverUrl = server.text.trim();
    await SessionManager.saveServerUrl(serverUrl);
    final api = ApiClient(serverUrl);

    try {
      final response = jsonMap(
        await api.post(
          '/auth/login',
          data: {'email': emailText, 'password': passText},
        ),
      );
      final role = text(response['roleCode']).toLowerCase();
      if (!{'staff', 'manager', 'admin'}.contains(role)) {
        throw ApiException(
          'Tài khoản này thuộc vai trò "$role". Ứng dụng này chỉ dành cho Nhân viên hoặc Quản lý nhà hàng.',
        );
      }

      api.token = text(response['token']);
      final session = StaffSession(
        api: api,
        fullName: text(response['fullName'], 'Nhân viên'),
        role: role,
        userId: integer(response['userId']),
        email: emailText,
      );

      // Lưu phiên vào bộ nhớ cục bộ
      await SessionManager.saveSession(session, rememberMe: rememberMe);

      if (mounted) {
        widget.onLoggedIn(session);
      }
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } catch (e) {
      if (mounted) setState(() => error = 'Đăng nhập không thành công: $e');
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> loginWithGoogle() async {
    FocusScope.of(context).unfocus();
    setState(() {
      loading = true;
      error = null;
    });

    final serverUrl = server.text.trim();
    await SessionManager.saveServerUrl(serverUrl);
    final api = ApiClient(serverUrl);

    try {
      String? idToken;
      String? accessToken;
      String? googleEmail;
      String? googleName;
      String? googleAvatar;

      try {
        const configuredClientId = String.fromEnvironment(
          'GOOGLE_CLIENT_ID',
          defaultValue:
              '726319516526-ta8jkgbkml0pqblp1fivh3hhva9lvi3f.apps.googleusercontent.com',
        );
        final googleSignIn = GoogleSignIn(
          scopes: ['email', 'profile'],
          serverClientId: configuredClientId,
        );
        final account = await googleSignIn.signIn();
        if (account != null) {
          final auth = await account.authentication;
          idToken = auth.idToken;
          accessToken = auth.accessToken;
          googleEmail = account.email;
          googleName = account.displayName;
          googleAvatar = account.photoUrl;
        }
      } catch (e) {
        debugPrint('Google Sign-In native exception: $e');
      }

      if (idToken == null && accessToken == null) {
        if (googleEmail == null || googleEmail.isEmpty) {
          throw ApiException(
            'Đăng nhập Google chưa hoàn tất hoặc thiết bị chưa kết nối dịch vụ Google Play.',
          );
        }
      }

      final payload = <String, dynamic>{};
      if (idToken != null) payload['idToken'] = idToken;
      if (accessToken != null) payload['accessToken'] = accessToken;
      if (googleEmail != null) payload['email'] = googleEmail;
      if (googleName != null) payload['fullName'] = googleName;
      if (googleAvatar != null) payload['avatarUrl'] = googleAvatar;

      final response = jsonMap(
        await api.post('/auth/google-login', data: payload),
      );

      final role = text(response['roleCode']).toLowerCase();
      if (!{'staff', 'manager', 'admin'}.contains(role)) {
        throw ApiException(
          'Tài khoản Google ($googleEmail) thuộc vai trò "$role". Ứng dụng này chỉ dành cho Nhân viên/Quản lý.',
        );
      }

      api.token = text(response['token']);
      final session = StaffSession(
        api: api,
        fullName: text(response['fullName'], googleName ?? 'Nhân viên Google'),
        role: role,
        userId: integer(response['userId']),
        email: text(response['email'], googleEmail ?? ''),
        avatarUrl: text(response['avatarUrl'], googleAvatar ?? ''),
      );

      await SessionManager.saveSession(session, rememberMe: rememberMe);

      if (mounted) {
        widget.onLoggedIn(session);
      }
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } catch (e) {
      if (mounted) setState(() => error = 'Đăng nhập Google thất bại: $e');
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  void _fillQuickCredentials(String emailVal, String passVal) {
    setState(() {
      email.text = emailVal;
      password.text = passVal;
      error = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final size = MediaQuery.sizeOf(context);
    final isTablet = size.width >= 700;

    return Scaffold(
      backgroundColor: isDark ? darkCanvas : const Color(0xFFF3F7F5),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: EdgeInsets.symmetric(
              horizontal: isTablet ? 36 : 20,
              vertical: 24,
            ),
            child: ConstrainedBox(
              constraints: BoxConstraints(maxWidth: isTablet ? 480 : 440),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Top Navigation Bar
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      if (widget.onBackToIntro != null)
                        TextButton.icon(
                          onPressed: widget.onBackToIntro,
                          style: TextButton.styleFrom(
                            foregroundColor: isDark ? gold : brand,
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                          ),
                          icon: const Icon(Icons.arrow_back_ios_rounded, size: 16),
                          label: const Text(
                            'Giới thiệu',
                            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                          ),
                        )
                      else
                        const SizedBox.shrink(),
                      IconButton(
                        tooltip: isDark ? 'Giao diện Sáng' : 'Giao diện Tối',
                        onPressed: () => ThemeController.instance.toggleTheme(),
                        icon: Icon(
                          isDark ? Icons.light_mode_rounded : Icons.dark_mode_rounded,
                          color: isDark ? gold : brand,
                          size: 20,
                        ),
                        style: IconButton.styleFrom(
                          backgroundColor: isDark
                              ? darkSurface
                              : Colors.white,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                            side: BorderSide(
                              color: isDark ? darkCardBorder : cardBorder,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),

                  // Brand Hero Header
                  Center(
                    child: Column(
                      children: [
                        Container(
                          width: 68,
                          height: 68,
                          decoration: BoxDecoration(
                            color: brand,
                            shape: BoxShape.circle,
                            border: Border.all(color: gold, width: 2),
                            boxShadow: [
                              BoxShadow(
                                color: brand.withValues(alpha: 0.3),
                                blurRadius: 18,
                                offset: const Offset(0, 6),
                              ),
                            ],
                          ),
                          child: const Icon(
                            Icons.restaurant_rounded,
                            size: 34,
                            color: gold,
                          ),
                        ),
                        const SizedBox(height: 12),
                        Text(
                          'DOLA RESTAURANT',
                          style: TextStyle(
                            color: isDark ? darkTextPrimary : brand,
                            fontSize: 22,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 1.5,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Cổng Đăng Nhập Ca Làm · Nhân Viên POS',
                          style: TextStyle(
                            color: isDark ? darkTextSecondary : const Color(0xFF5A6E66),
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 24),

                  // Form Container Card (Glassmorphic / Elevated Card)
                  Container(
                    decoration: BoxDecoration(
                      color: isDark ? darkSurface : Colors.white,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(
                        color: isDark ? darkCardBorder : cardBorder,
                        width: 1.2,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: isDark
                              ? Colors.black.withValues(alpha: 0.3)
                              : brand.withValues(alpha: 0.06),
                          blurRadius: 20,
                          offset: const Offset(0, 8),
                        ),
                      ],
                    ),
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // Card Header with Status Pill
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(
                              children: [
                                Container(
                                  width: 4,
                                  height: 18,
                                  decoration: BoxDecoration(
                                    color: gold,
                                    borderRadius: BorderRadius.circular(2),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Text(
                                  'Đăng nhập ca làm',
                                  style: TextStyle(
                                    fontSize: 17,
                                    fontWeight: FontWeight.w800,
                                    color: isDark ? darkTextPrimary : brand,
                                  ),
                                ),
                              ],
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                              decoration: BoxDecoration(
                                color: const Color(0xFF10B981).withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Container(
                                    width: 6,
                                    height: 6,
                                    decoration: const BoxDecoration(
                                      color: Color(0xFF10B981),
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                                  const SizedBox(width: 4),
                                  const Text(
                                    'Online',
                                    style: TextStyle(
                                      color: Color(0xFF10B981),
                                      fontSize: 11,
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),

                        const SizedBox(height: 20),

                        // Input: Email
                        TextField(
                          controller: email,
                          keyboardType: TextInputType.emailAddress,
                          textInputAction: TextInputAction.next,
                          decoration: InputDecoration(
                            labelText: 'Email nhân viên',
                            hintText: 'VD: nhanvien@restaurant.com',
                            prefixIcon: Icon(
                              Icons.alternate_email_rounded,
                              color: isDark ? gold : brand,
                              size: 20,
                            ),
                            suffixIcon: email.text.isNotEmpty
                                ? IconButton(
                                    icon: const Icon(Icons.clear, size: 18),
                                    onPressed: () => setState(() => email.clear()),
                                  )
                                : null,
                          ),
                          onChanged: (_) => setState(() {}),
                        ),

                        const SizedBox(height: 14),

                        // Input: Mật khẩu
                        TextField(
                          controller: password,
                          obscureText: obscurePassword,
                          textInputAction: TextInputAction.done,
                          onSubmitted: (_) => login(),
                          decoration: InputDecoration(
                            labelText: 'Mật khẩu',
                            hintText: 'Nhập mật khẩu...',
                            prefixIcon: Icon(
                              Icons.lock_outline_rounded,
                              color: isDark ? gold : brand,
                              size: 20,
                            ),
                            suffixIcon: IconButton(
                              icon: Icon(
                                obscurePassword
                                    ? Icons.visibility_off_rounded
                                    : Icons.visibility_rounded,
                                size: 20,
                                color: isDark ? darkTextSecondary : Colors.grey,
                              ),
                              onPressed: () => setState(
                                () => obscurePassword = !obscurePassword,
                              ),
                            ),
                          ),
                        ),

                        const SizedBox(height: 12),

                        // Remember Me & Forgot Password
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            InkWell(
                              onTap: () => setState(() => rememberMe = !rememberMe),
                              borderRadius: BorderRadius.circular(6),
                              child: Padding(
                                padding: const EdgeInsets.symmetric(vertical: 4),
                                child: Row(
                                  children: [
                                    SizedBox(
                                      width: 22,
                                      height: 22,
                                      child: Checkbox(
                                        value: rememberMe,
                                        activeColor: brand,
                                        checkColor: gold,
                                        materialTapTargetSize:
                                            MaterialTapTargetSize.shrinkWrap,
                                        onChanged: (val) =>
                                            setState(() => rememberMe = val ?? true),
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                    Text(
                                      'Ghi nhớ đăng nhập',
                                      style: TextStyle(
                                        fontSize: 13,
                                        fontWeight: FontWeight.w600,
                                        color: isDark ? darkTextSecondary : const Color(0xFF4A5568),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),

                        const SizedBox(height: 12),

                        // Quick Test Account Chips (Tiện ích chọn nhanh tài khoản thử nghiệm)
                        Wrap(
                          spacing: 8,
                          runSpacing: 6,
                          children: [
                            ActionChip(
                              avatar: const Icon(Icons.badge_outlined, size: 14, color: gold),
                              label: const Text('Staff Demo'),
                              tooltip: 'staff@restaurant.com',
                              labelStyle: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
                              padding: const EdgeInsets.symmetric(horizontal: 4),
                              onPressed: () => _fillQuickCredentials('staff@restaurant.com', '123456'),
                            ),
                            ActionChip(
                              avatar: const Icon(Icons.admin_panel_settings_outlined, size: 14, color: Color(0xFF10B981)),
                              label: const Text('Admin Demo'),
                              tooltip: 'admin@restaurant.com',
                              labelStyle: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
                              padding: const EdgeInsets.symmetric(horizontal: 4),
                              onPressed: () => _fillQuickCredentials('admin@restaurant.com', '123456'),
                            ),
                          ],
                        ),

                        // Error Banner
                        if (error != null) ...[
                          const SizedBox(height: 14),
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: const Color(0xFFEF4444).withValues(alpha: 0.12),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: const Color(0xFFEF4444).withValues(alpha: 0.35),
                              ),
                            ),
                            child: Row(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Icon(
                                  Icons.error_outline_rounded,
                                  color: Color(0xFFDC2626),
                                  size: 20,
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: Text(
                                    error!,
                                    style: const TextStyle(
                                      color: Color(0xFFDC2626),
                                      fontSize: 13,
                                      fontWeight: FontWeight.w600,
                                      height: 1.35,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],

                        const SizedBox(height: 20),

                        // Submit Button
                        FilledButton.icon(
                          style: FilledButton.styleFrom(
                            backgroundColor: brand,
                            foregroundColor: Colors.white,
                            elevation: 2,
                            minimumSize: const Size.fromHeight(50),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                          ),
                          onPressed: loading ? null : login,
                          icon: loading
                              ? const SizedBox.square(
                                  dimension: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: gold,
                                  ),
                                )
                              : const Icon(Icons.login_rounded, size: 20, color: gold),
                          label: Text(
                            loading ? 'Đang xác thực ca làm...' : 'Đăng Nhập Ca Làm',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              letterSpacing: 0.4,
                            ),
                          ),
                        ),

                        const SizedBox(height: 18),

                        // OR Divider
                        Row(
                          children: [
                            Expanded(
                              child: Divider(
                                color: isDark ? darkCardBorder : const Color(0xFFE2E8F0),
                              ),
                            ),
                            Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 12),
                              child: Text(
                                'HOẶC ĐĂNG NHẬP VỚI',
                                style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w700,
                                  color: isDark ? darkTextSecondary : const Color(0xFF94A3B8),
                                  letterSpacing: 0.8,
                                ),
                              ),
                            ),
                            Expanded(
                              child: Divider(
                                color: isDark ? darkCardBorder : const Color(0xFFE2E8F0),
                              ),
                            ),
                          ],
                        ),

                        const SizedBox(height: 16),

                        // Google Sign-In Button
                        OutlinedButton(
                          style: OutlinedButton.styleFrom(
                            minimumSize: const Size.fromHeight(50),
                            side: BorderSide(
                              color: isDark ? darkCardBorder : const Color(0xFFCBD5E1),
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                            backgroundColor: isDark ? const Color(0xFF0F172A) : Colors.white,
                          ),
                          onPressed: loading ? null : loginWithGoogle,
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Image.network(
                                'https://www.gstatic.com/images/branding/product/2x/googleg_48dp.png',
                                width: 22,
                                height: 22,
                                errorBuilder: (context, error, stackTrace) => const Icon(
                                  Icons.g_mobiledata_rounded,
                                  size: 26,
                                  color: Color(0xFF4285F4),
                                ),
                              ),
                              const SizedBox(width: 10),
                              Flexible(
                                child: Text(
                                  'Tài khoản Google Workspace',
                                  overflow: TextOverflow.ellipsis,
                                  style: TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w700,
                                    color: isDark ? darkTextPrimary : const Color(0xFF1E293B),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),

                        const SizedBox(height: 20),

                        // Collapsible Server Configuration
                        InkWell(
                          onTap: () => setState(() => showServer = !showServer),
                          borderRadius: BorderRadius.circular(8),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 4),
                            child: Row(
                              children: [
                                Icon(
                                  showServer
                                      ? Icons.keyboard_arrow_up_rounded
                                      : Icons.tune_rounded,
                                  size: 18,
                                  color: gold,
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    showServer
                                        ? 'Thu gọn cài đặt máy chủ API'
                                        : 'Cấu hình kết nối máy chủ API',
                                    style: TextStyle(
                                      fontSize: 13,
                                      fontWeight: FontWeight.w700,
                                      color: isDark ? gold : brand,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),

                        if (showServer) ...[
                          const SizedBox(height: 10),
                          TextField(
                            controller: server,
                            keyboardType: TextInputType.url,
                            decoration: const InputDecoration(
                              labelText: 'Địa chỉ máy chủ API',
                              prefixIcon: Icon(Icons.dns_rounded),
                            ),
                          ),
                          const SizedBox(height: 8),
                          Wrap(
                            spacing: 6,
                            runSpacing: 6,
                            children: [
                              ActionChip(
                                label: const Text('Localhost PC (5079)', style: TextStyle(fontSize: 11)),
                                onPressed: () => setState(
                                  () => server.text = 'http://localhost:5079/api',
                                ),
                              ),
                              ActionChip(
                                label: const Text('Android Emulator (10.0.2.2)', style: TextStyle(fontSize: 11)),
                                onPressed: () => setState(
                                  () => server.text = 'http://10.0.2.2:5079/api',
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 10),
                          OutlinedButton.icon(
                            onPressed: checkingConnection ? null : testConnection,
                            icon: checkingConnection
                                ? const SizedBox.square(
                                    dimension: 16,
                                    child: CircularProgressIndicator(strokeWidth: 2),
                                  )
                                : Icon(
                                    isConnected ? Icons.check_circle_rounded : Icons.network_check_rounded,
                                    size: 18,
                                    color: isConnected ? const Color(0xFF10B981) : brand,
                                  ),
                            label: Text(
                              checkingConnection
                                  ? 'Đang kiểm tra kết nối...'
                                  : 'Kiểm tra kết nối máy chủ',
                              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                            ),
                          ),
                          if (connectionStatus != null)
                            Padding(
                              padding: const EdgeInsets.only(top: 8),
                              child: Text(
                                connectionStatus!,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: isConnected
                                      ? const Color(0xFF10B981)
                                      : const Color(0xFFDC2626),
                                ),
                              ),
                            ),
                        ],
                      ],
                    ),
                  ),

                  const SizedBox(height: 24),

                  // Footer info
                  Center(
                    child: Text(
                      '© Dola Restaurant · Hệ thống quản lý order & phục vụ bàn v2.9',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 12,
                        color: isDark ? darkTextSecondary : const Color(0xFF8F9E98),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

import 'dart:io' show Platform;
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import '../../core/staff_core.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key, required this.onLoggedIn});
  final ValueChanged<StaffSession> onLoggedIn;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final email = TextEditingController(text: 'nhanvien1@gmail.com');
  final password = TextEditingController(text: '123');
  final server = TextEditingController();
  bool loading = false;
  bool showServer = false;
  bool checkingConnection = false;
  String? connectionStatus;
  bool isConnected = false;
  String? error;

  @override
  void initState() {
    super.initState();
    const configured = String.fromEnvironment('API_BASE_URL');
    if (configured.isNotEmpty) {
      server.text = configured;
    } else if (kIsWeb) {
      server.text = 'http://localhost:5079/api';
    } else {
      try {
        if (Platform.isAndroid) {
          server.text = 'http://10.0.2.2:5079/api';
        } else {
          server.text = 'http://localhost:5079/api';
        }
      } catch (_) {
        server.text = 'http://localhost:5079/api';
      }
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
            ? 'Kết nối thành công tới máy chủ API!'
            : 'Không thể kết nối. Kiểm tra máy chủ có đang chạy trên cổng 5079 không.';
      });
    }
  }

  Future<void> login() async {
    FocusScope.of(context).unfocus();
    setState(() {
      loading = true;
      error = null;
    });
    final api = ApiClient(server.text.trim());
    try {
      final response = jsonMap(
        await api.post(
          '/auth/login',
          data: {'email': email.text.trim(), 'password': password.text},
        ),
      );
      final role = text(response['roleCode']).toLowerCase();
      if (!{'staff', 'manager', 'admin'}.contains(role)) {
        throw ApiException(
          'Tài khoản này không có quyền truy cập ứng dụng nhân viên.',
        );
      }
      api.token = text(response['token']);
      widget.onLoggedIn(
        StaffSession(
          api: api,
          fullName: text(response['fullName'], 'Nhân viên'),
          role: role,
          userId: integer(response['userId']),
          email: email.text.trim(),
        ),
      );
    } on ApiException catch (exception) {
      if (mounted) setState(() => error = exception.message);
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Logo & Brand Header
                  Container(
                    width: 72,
                    height: 72,
                    margin: const EdgeInsets.only(bottom: 16),
                    decoration: BoxDecoration(
                      color: brand,
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: brand.withValues(alpha: 0.25),
                          blurRadius: 16,
                          offset: const Offset(0, 6),
                        ),
                      ],
                    ),
                    child: const Icon(
                      Icons.restaurant_rounded,
                      size: 38,
                      color: gold,
                    ),
                  ),
                  const Text(
                    'DOLA RESTAURANT',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: brand,
                      fontSize: 22,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const SizedBox(height: 4),
                  const Text(
                    'Ứng Dụng Phục Vụ & Order Tại Bàn (Staff POS)',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: Color(0xFF6F7E78),
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 28),

                  // Login Form Card
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Row(
                            children: [
                              Container(
                                width: 4,
                                height: 20,
                                decoration: BoxDecoration(
                                  color: gold,
                                  borderRadius: BorderRadius.circular(2),
                                ),
                              ),
                              const SizedBox(width: 8),
                              const Text(
                                'Đăng nhập ca làm',
                                style: TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w800,
                                  color: brand,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 20),
                          TextField(
                            controller: email,
                            keyboardType: TextInputType.emailAddress,
                            decoration: const InputDecoration(
                              labelText: 'Email nhân viên',
                              prefixIcon: Icon(Icons.email_outlined),
                            ),
                          ),
                          const SizedBox(height: 14),
                          TextField(
                            controller: password,
                            obscureText: true,
                            onSubmitted: (_) => login(),
                            decoration: const InputDecoration(
                              labelText: 'Mật khẩu',
                              prefixIcon: Icon(Icons.lock_outline),
                            ),
                          ),
                          const SizedBox(height: 12),

                          // Toggle Server Config
                          InkWell(
                            onTap: () =>
                                setState(() => showServer = !showServer),
                            borderRadius: BorderRadius.circular(6),
                            child: Padding(
                              padding: const EdgeInsets.symmetric(vertical: 6),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(
                                    showServer
                                        ? Icons.tune
                                        : Icons.tune_outlined,
                                    size: 16,
                                    color: gold,
                                  ),
                                  const SizedBox(width: 6),
                                  Text(
                                    showServer
                                        ? 'Ẩn cài đặt máy chủ API'
                                        : 'Cấu hình kết nối máy chủ API',
                                    style: const TextStyle(
                                      fontSize: 13,
                                      fontWeight: FontWeight.w700,
                                      color: brand,
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
                                prefixIcon: Icon(Icons.dns_outlined),
                              ),
                            ),
                            const SizedBox(height: 8),
                            Wrap(
                              spacing: 6,
                              runSpacing: 6,
                              children: [
                                ActionChip(
                                  label: const Text(
                                    'PC Localhost (5079)',
                                    style: TextStyle(fontSize: 11),
                                  ),
                                  onPressed: () => setState(
                                    () => server.text =
                                        'http://localhost:5079/api',
                                  ),
                                ),
                                ActionChip(
                                  label: const Text(
                                    'Android Emulator (10.0.2.2)',
                                    style: TextStyle(fontSize: 11),
                                  ),
                                  onPressed: () => setState(
                                    () => server.text =
                                        'http://10.0.2.2:5079/api',
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            OutlinedButton.icon(
                              onPressed: checkingConnection
                                  ? null
                                  : testConnection,
                              icon: checkingConnection
                                  ? const SizedBox.square(
                                      dimension: 16,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : Icon(
                                      isConnected
                                          ? Icons.check_circle
                                          : Icons.network_check,
                                      size: 18,
                                      color: isConnected
                                          ? const Color(0xFF2E7D32)
                                          : brand,
                                    ),
                              label: Text(
                                checkingConnection
                                    ? 'Đang kiểm tra kết nối...'
                                    : 'Kiểm tra kết nối máy chủ',
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
                                        ? const Color(0xFF2E7D32)
                                        : const Color(0xFFC62828),
                                  ),
                                ),
                              ),
                          ],

                          if (error != null) ...[
                            const SizedBox(height: 14),
                            Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: const Color(0xFFFFEBEE),
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(
                                  color: const Color(0xFFFFCDD2),
                                ),
                              ),
                              child: Row(
                                children: [
                                  const Icon(
                                    Icons.error_outline,
                                    color: Color(0xFFC62828),
                                    size: 20,
                                  ),
                                  const SizedBox(width: 8),
                                  Expanded(
                                    child: Text(
                                      error!,
                                      style: const TextStyle(
                                        color: Color(0xFFC62828),
                                        fontSize: 13,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],

                          const SizedBox(height: 22),
                          FilledButton.icon(
                            style: FilledButton.styleFrom(
                              minimumSize: const Size.fromHeight(50),
                            ),
                            onPressed: loading ? null : login,
                            icon: loading
                                ? const SizedBox.square(
                                    dimension: 20,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: Colors.white,
                                    ),
                                  )
                                : const Icon(Icons.login),
                            label: Text(
                              loading ? 'Đang xác thực...' : 'Đăng Nhập Ca Làm',
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 24),
                  const Text(
                    '© Dola Restaurant · Hệ thống quản lý gọi món tại bàn',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 12, color: Color(0xFF8F9E98)),
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

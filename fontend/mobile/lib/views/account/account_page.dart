import 'package:flutter/material.dart';

import '../../core/staff_core.dart';

class AccountPage extends StatefulWidget {
  const AccountPage({
    super.key,
    required this.session,
    required this.onLogout,
    this.onSessionUpdated,
  });

  final StaffSession session;
  final VoidCallback onLogout;
  final ValueChanged<StaffSession>? onSessionUpdated;

  @override
  State<AccountPage> createState() => _AccountPageState();
}

class _AccountPageState extends State<AccountPage> {
  late StaffSession _session = widget.session;
  bool _loading = false;
  bool _testingConnection = false;
  String? _connectionStatus;
  bool? _isConnectionOk;

  @override
  void initState() {
    super.initState();
    _fetchProfile();
  }

  @override
  void didUpdateWidget(covariant AccountPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.session != widget.session) {
      _session = widget.session;
    }
  }

  // Tải thông tin tài khoản mới nhất từ API
  Future<void> _fetchProfile() async {
    try {
      final res = await _session.api.get('/auth/me');
      if (res is Map && mounted) {
        final data = jsonMap(res);
        setState(() {
          _session = _session.copyWith(
            fullName: text(data['fullName'], _session.fullName),
            email: text(data['email'], _session.email),
            phone: text(data['phone'], _session.phone),
            avatarUrl: text(data['avatarUrl'], _session.avatarUrl),
          );
        });
        widget.onSessionUpdated?.call(_session);
      }
    } catch (_) {
      // Bỏ qua nếu lỗi mạng để không ảnh hưởng luồng
    }
  }

  // Kiểm tra kết nối nhanh tới máy chủ
  Future<void> _checkServerConnection() async {
    setState(() {
      _testingConnection = true;
      _connectionStatus = null;
      _isConnectionOk = null;
    });

    final ok = await _session.api.checkConnection();
    if (mounted) {
      setState(() {
        _testingConnection = false;
        _isConnectionOk = ok;
        _connectionStatus = ok
            ? 'Kết nối ổn định tới máy chủ Dola API'
            : 'Mất kết nối máy chủ hoặc cổng 5079 không khả dụng';
      });
    }
  }

  // Mở BottomSheet chỉnh sửa thông tin cá nhân (Họ tên, SĐT, Avatar)
  Future<void> _openEditProfileDialog() async {
    final nameCtrl = TextEditingController(text: _session.fullName);
    final phoneCtrl = TextEditingController(text: _session.phone);
    final avatarCtrl = TextEditingController(text: _session.avatarUrl);

    final updated = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (sheetContext) {
        bool saving = false;
        String? editError;

        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            return SafeArea(
              child: Padding(
                padding: EdgeInsets.fromLTRB(
                  20,
                  8,
                  20,
                  24 + MediaQuery.viewInsetsOf(ctx).bottom,
                ),
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: gold.withValues(alpha: 0.2),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: const Icon(Icons.person_pin, color: gold, size: 24),
                          ),
                          const SizedBox(width: 12),
                          const Text(
                            'Chỉnh sửa thông tin cá nhân',
                            style: TextStyle(
                              fontSize: 17,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 18),
                      if (editError != null) ...[
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFEE2E2),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Text(
                            editError!,
                            style: const TextStyle(
                              color: Color(0xFF991B1B),
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                        const SizedBox(height: 12),
                      ],
                      TextField(
                        controller: nameCtrl,
                        decoration: const InputDecoration(
                          labelText: 'Họ và tên nhân viên',
                          prefixIcon: Icon(Icons.badge_outlined),
                        ),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: phoneCtrl,
                        keyboardType: TextInputType.phone,
                        decoration: const InputDecoration(
                          labelText: 'Số điện thoại',
                          prefixIcon: Icon(Icons.phone_outlined),
                        ),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: avatarCtrl,
                        decoration: const InputDecoration(
                          labelText: 'Đường dẫn ảnh đại diện (URL ảnh online)',
                          hintText: 'https://res.cloudinary.com/...',
                          prefixIcon: Icon(Icons.image_outlined),
                        ),
                      ),
                      const SizedBox(height: 20),
                      FilledButton(
                        style: FilledButton.styleFrom(
                          minimumSize: const Size.fromHeight(48),
                        ),
                        onPressed: saving
                            ? null
                            : () async {
                                final name = nameCtrl.text.trim();
                                if (name.isEmpty) {
                                  setSheetState(() => editError = 'Họ tên không được để trống.');
                                  return;
                                }

                                setSheetState(() {
                                  saving = true;
                                  editError = null;
                                });

                                try {
                                  await _session.api.put(
                                    '/auth/profile',
                                    data: {
                                      'fullName': name,
                                      'phone': phoneCtrl.text.trim(),
                                      'avatarUrl': avatarCtrl.text.trim(),
                                    },
                                  );
                                  if (ctx.mounted) {
                                    Navigator.pop(ctx, true);
                                  }
                                } on ApiException catch (e) {
                                  if (ctx.mounted) {
                                    setSheetState(() {
                                      saving = false;
                                      editError = e.message;
                                    });
                                  }
                                } catch (_) {
                                  if (ctx.mounted) {
                                    setSheetState(() {
                                      saving = false;
                                      editError = 'Không thể kết nối máy chủ. Thử lại sau.';
                                    });
                                  }
                                }
                              },
                        child: saving
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                              )
                            : const Text('Lưu Thay Đổi', style: TextStyle(fontWeight: FontWeight.w900)),
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        );
      },
    );

    nameCtrl.dispose();
    phoneCtrl.dispose();
    avatarCtrl.dispose();

    if (updated == true && mounted) {
      showMessage(context, 'Cập nhật thông tin cá nhân thành công!');
      await _fetchProfile();
    }
  }

  // Mở Dialog Đổi mật khẩu
  Future<void> _openChangePasswordDialog() async {
    final oldPassCtrl = TextEditingController();
    final newPassCtrl = TextEditingController();
    final confirmPassCtrl = TextEditingController();

    final result = await showDialog<bool>(
      context: context,
      builder: (dialogCtx) {
        bool updating = false;
        String? dialogError;
        bool obscureOld = true;
        bool obscureNew = true;

        return StatefulBuilder(
          builder: (ctx, setDialogState) {
            return AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              title: const Row(
                children: [
                  Icon(Icons.lock_reset, color: gold, size: 26),
                  SizedBox(width: 10),
                  Text('Đổi mật khẩu', style: TextStyle(fontWeight: FontWeight.w900)),
                ],
              ),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (dialogError != null) ...[
                      Container(
                        padding: const EdgeInsets.all(8),
                        margin: const EdgeInsets.only(bottom: 12),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEE2E2),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          dialogError!,
                          style: const TextStyle(
                            color: Color(0xFF991B1B),
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                    TextField(
                      controller: oldPassCtrl,
                      obscureText: obscureOld,
                      decoration: InputDecoration(
                        labelText: 'Mật khẩu hiện tại',
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          icon: Icon(obscureOld ? Icons.visibility_off : Icons.visibility, size: 20),
                          onPressed: () => setDialogState(() => obscureOld = !obscureOld),
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: newPassCtrl,
                      obscureText: obscureNew,
                      decoration: InputDecoration(
                        labelText: 'Mật khẩu mới (tối thiểu 6 ký tự)',
                        prefixIcon: const Icon(Icons.key_outlined),
                        suffixIcon: IconButton(
                          icon: Icon(obscureNew ? Icons.visibility_off : Icons.visibility, size: 20),
                          onPressed: () => setDialogState(() => obscureNew = !obscureNew),
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: confirmPassCtrl,
                      obscureText: obscureNew,
                      decoration: const InputDecoration(
                        labelText: 'Xác nhận mật khẩu mới',
                        prefixIcon: Icon(Icons.check_circle_outline),
                      ),
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(
                  onPressed: updating ? null : () => Navigator.pop(dialogCtx, false),
                  child: const Text('Hủy'),
                ),
                FilledButton(
                  onPressed: updating
                      ? null
                      : () async {
                          final oldPass = oldPassCtrl.text;
                          final newPass = newPassCtrl.text;
                          final confirmPass = confirmPassCtrl.text;

                          if (oldPass.isEmpty) {
                            setDialogState(() => dialogError = 'Vui lòng nhập mật khẩu hiện tại.');
                            return;
                          }
                          if (newPass.length < 6) {
                            setDialogState(() => dialogError = 'Mật khẩu mới phải có ít nhất 6 ký tự.');
                            return;
                          }
                          if (newPass != confirmPass) {
                            setDialogState(() => dialogError = 'Mật khẩu xác nhận không trùng khớp.');
                            return;
                          }

                          setDialogState(() {
                            updating = true;
                            dialogError = null;
                          });

                          try {
                            await _session.api.put(
                              '/auth/profile',
                              data: {
                                'currentPassword': oldPass,
                                'newPassword': newPass,
                              },
                            );
                            if (dialogCtx.mounted) {
                              Navigator.pop(dialogCtx, true);
                            }
                          } on ApiException catch (e) {
                            if (dialogCtx.mounted) {
                              setDialogState(() {
                                updating = false;
                                dialogError = e.message;
                              });
                            }
                          } catch (_) {
                            if (dialogCtx.mounted) {
                              setDialogState(() {
                                updating = false;
                                dialogError = 'Không thể kết nối máy chủ.';
                              });
                            }
                          }
                        },
                  child: updating
                      ? const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Cập nhật'),
                ),
              ],
            );
          },
        );
      },
    );

    oldPassCtrl.dispose();
    newPassCtrl.dispose();
    confirmPassCtrl.dispose();

    if (result == true && mounted) {
      showMessage(context, 'Đổi mật khẩu thành công! Hãy ghi nhớ mật khẩu mới.');
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final themeController = ThemeController.instance;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Hồ sơ nhân viên', style: TextStyle(fontWeight: FontWeight.w900)),
        actions: [
          IconButton(
            tooltip: 'Tải lại thông tin',
            icon: const Icon(Icons.sync),
            onPressed: () async {
              setState(() => _loading = true);
              await _fetchProfile();
              if (mounted) setState(() => _loading = false);
            },
          ),
        ],
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 550),
          child: ListView(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
            children: [
              if (_loading) ...[
                const LinearProgressIndicator(),
                const SizedBox(height: 12),
              ],
              // 1. THẺ HỒ SƠ NHÂN VIÊN VIP (HEADER PROFILE CARD)
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(18),
                  child: Row(
                    children: [
                      // Avatar với viền Gold cao cấp
                      Stack(
                        children: [
                          Container(
                            width: 64,
                            height: 64,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              gradient: LinearGradient(
                                colors: isDark
                                    ? [const Color(0xFF1E293B), const Color(0xFF334155)]
                                    : [brand, brandLight],
                                begin: Alignment.topLeft,
                                end: Alignment.bottomRight,
                              ),
                              border: Border.all(color: gold, width: 2.5),
                              boxShadow: [
                                BoxShadow(
                                  color: gold.withValues(alpha: 0.25),
                                  blurRadius: 10,
                                  offset: const Offset(0, 3),
                                ),
                              ],
                            ),
                            child: ClipOval(
                              child: _session.avatarUrl.isNotEmpty
                                  ? Image.network(
                                      _session.avatarUrl,
                                      fit: BoxFit.cover,
                                      errorBuilder: (_, _, _) => Center(
                                        child: Text(
                                          _session.fullName.isNotEmpty
                                              ? _session.fullName[0].toUpperCase()
                                              : 'N',
                                          style: const TextStyle(
                                            color: gold,
                                            fontSize: 26,
                                            fontWeight: FontWeight.w900,
                                          ),
                                        ),
                                      ),
                                    )
                                  : Center(
                                      child: Text(
                                        _session.fullName.isNotEmpty
                                            ? _session.fullName[0].toUpperCase()
                                            : 'N',
                                        style: const TextStyle(
                                          color: gold,
                                          fontSize: 26,
                                          fontWeight: FontWeight.w900,
                                        ),
                                      ),
                                    ),
                            ),
                          ),
                          Positioned(
                            bottom: 0,
                            right: 0,
                            child: Container(
                              width: 18,
                              height: 18,
                              decoration: BoxDecoration(
                                color: const Color(0xFF10B981), // Xanh lá online
                                shape: BoxShape.circle,
                                border: Border.all(
                                  color: isDark ? darkSurface : Colors.white,
                                  width: 2,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(width: 16),
                      // Tên & Chức vụ
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              _session.fullName,
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w900,
                                color: isDark ? darkTextPrimary : brand,
                              ),
                            ),
                            const SizedBox(height: 4),
                            // Chức vụ / Vai trò Badge
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                              decoration: BoxDecoration(
                                color: gold.withValues(alpha: 0.18),
                                borderRadius: BorderRadius.circular(6),
                                border: Border.all(color: gold.withValues(alpha: 0.3)),
                              ),
                              child: Text(
                                _roleTitle(_session.role),
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w800,
                                  color: gold,
                                ),
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'Mã nhân viên: #${_session.userId} · Đang trong ca làm',
                              style: TextStyle(
                                fontSize: 12,
                                color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // 2. KHỐI TÙY CHỈNH GIAO DIỆN (THEME / DARK MODE) - UI/UX PRO MAX
              _buildSectionHeader('TÙY CHỈNH GIAO DIỆN HỆ THỐNG', Icons.palette_outlined),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: isDark
                                  ? const Color(0xFF3B82F6).withValues(alpha: 0.2)
                                  : const Color(0xFFF59E0B).withValues(alpha: 0.2),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: Icon(
                              isDark ? Icons.dark_mode : Icons.light_mode,
                              color: isDark ? const Color(0xFF60A5FA) : const Color(0xFFD97706),
                              size: 24,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  isDark ? 'Giao diện Tối (Dark Mode)' : 'Giao diện Sáng (Light Mode)',
                                  style: const TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  isDark
                                      ? 'Dịu mắt, tiết kiệm pin cho ca làm tối'
                                      : 'Độ sáng rõ nét cho môi trường ban ngày',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Switch(
                            value: themeController.isDark,
                            onChanged: (val) {
                              themeController.toggleTheme();
                            },
                            activeThumbColor: gold,
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      const Divider(height: 1),
                      const SizedBox(height: 12),
                      // 2 Nút chọn nhanh chế độ giao diện
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              style: OutlinedButton.styleFrom(
                                side: BorderSide(
                                  color: !isDark ? brand : themedBorder(context),
                                  width: !isDark ? 2 : 1,
                                ),
                                backgroundColor: !isDark ? brand.withValues(alpha: 0.08) : null,
                                padding: const EdgeInsets.symmetric(vertical: 10),
                              ),
                              onPressed: () => themeController.setThemeMode(ThemeMode.light),
                              icon: Icon(Icons.wb_sunny_outlined, size: 18, color: !isDark ? brand : null),
                              label: Text(
                                'Chế độ Sáng',
                                style: TextStyle(
                                  fontWeight: !isDark ? FontWeight.w900 : FontWeight.w600,
                                  color: !isDark ? brand : null,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton.icon(
                              style: OutlinedButton.styleFrom(
                                side: BorderSide(
                                  color: isDark ? gold : themedBorder(context),
                                  width: isDark ? 2 : 1,
                                ),
                                backgroundColor: isDark ? gold.withValues(alpha: 0.15) : null,
                                padding: const EdgeInsets.symmetric(vertical: 10),
                              ),
                              onPressed: () => themeController.setThemeMode(ThemeMode.dark),
                              icon: Icon(Icons.nightlight_round, size: 18, color: isDark ? gold : null),
                              label: Text(
                                'Chế độ Tối',
                                style: TextStyle(
                                  fontWeight: isDark ? FontWeight.w900 : FontWeight.w600,
                                  color: isDark ? gold : null,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // 3. KHỐI THÔNG TIN TÀI KHOẢN & LIÊN HỆ
              _buildSectionHeader('THÔNG TIN LIÊN HỆ & TÀI KHOẢN', Icons.contact_mail_outlined),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      leading: Icon(Icons.email_outlined, color: isDark ? gold : brand),
                      title: const Text('Email đăng nhập', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      subtitle: Text(_session.email.isNotEmpty ? _session.email : 'staff@restaurant.com', style: const TextStyle(fontSize: 13)),
                    ),
                    const Divider(height: 1),
                    ListTile(
                      leading: Icon(Icons.phone_outlined, color: isDark ? gold : brand),
                      title: const Text('Số điện thoại', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      subtitle: Text(_session.phone.isNotEmpty ? _session.phone : 'Chưa cập nhật', style: const TextStyle(fontSize: 13)),
                      trailing: TextButton.icon(
                        style: TextButton.styleFrom(visualDensity: VisualDensity.compact),
                        onPressed: _openEditProfileDialog,
                        icon: const Icon(Icons.edit, size: 16),
                        label: const Text('Sửa'),
                      ),
                    ),
                    const Divider(height: 1),
                    ListTile(
                      leading: Icon(Icons.lock_outline, color: isDark ? gold : brand),
                      title: const Text('Bảo mật & Mật khẩu', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      subtitle: const Text('Đổi mật khẩu tài khoản', style: TextStyle(fontSize: 12)),
                      trailing: FilledButton.tonal(
                        style: FilledButton.styleFrom(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                          visualDensity: VisualDensity.compact,
                        ),
                        onPressed: _openChangePasswordDialog,
                        child: const Text('Đổi mật khẩu', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // 4. KHỐI HỆ THỐNG & KẾT NỐI MÁY CHỦ API
              _buildSectionHeader('HỆ THỐNG & KẾT NỐI API', Icons.dns_outlined),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      leading: Icon(Icons.cloud_done_outlined, color: isDark ? gold : brand),
                      title: const Text('Máy chủ API Backend', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      subtitle: Text(_session.api.server, style: const TextStyle(fontSize: 12)),
                      trailing: OutlinedButton(
                        style: OutlinedButton.styleFrom(
                          visualDensity: VisualDensity.compact,
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        ),
                        onPressed: _testingConnection ? null : _checkServerConnection,
                        child: _testingConnection
                            ? const SizedBox(
                                width: 14,
                                height: 14,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Text('Kiểm tra', style: TextStyle(fontSize: 12)),
                      ),
                    ),
                    if (_connectionStatus != null) ...[
                      Padding(
                        padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                        child: Row(
                          children: [
                            Icon(
                              _isConnectionOk == true ? Icons.check_circle : Icons.error,
                              size: 16,
                              color: _isConnectionOk == true ? const Color(0xFF10B981) : const Color(0xFFEF4444),
                            ),
                            const SizedBox(width: 6),
                            Expanded(
                              child: Text(
                                _connectionStatus!,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: _isConnectionOk == true ? const Color(0xFF10B981) : const Color(0xFFEF4444),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                    const Divider(height: 1),
                    ListTile(
                      leading: Icon(Icons.info_outline, color: isDark ? gold : brand),
                      title: const Text('Phiên bản phần mềm', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
                      trailing: Text(
                        'Dola POS v2.4.0 (Staff)',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                          color: isDark ? darkTextSecondary : const Color(0xFF64748B),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              // 5. NÚT ĐĂNG XUẤT CA LÀM
              OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  foregroundColor: const Color(0xFFEF4444),
                  side: BorderSide(color: const Color(0xFFEF4444).withValues(alpha: 0.5)),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                onPressed: () async {
                  final confirm = await showDialog<bool>(
                    context: context,
                    builder: (dialog) => AlertDialog(
                      title: const Text('Đăng xuất ca làm'),
                      content: const Text(
                        'Bạn có chắc chắn muốn đăng xuất khỏi phiên làm việc hiện tại?',
                      ),
                      actions: [
                        TextButton(
                          onPressed: () => Navigator.pop(dialog, false),
                          child: const Text('Hủy'),
                        ),
                        FilledButton(
                          style: FilledButton.styleFrom(backgroundColor: const Color(0xFFDC2626)),
                          onPressed: () => Navigator.pop(dialog, true),
                          child: const Text('Đăng xuất ngay'),
                        ),
                      ],
                    ),
                  );
                  if (confirm == true) widget.onLogout();
                },
                icon: const Icon(Icons.logout),
                label: const Text(
                  'Đăng xuất khỏi ca làm việc',
                  style: TextStyle(fontWeight: FontWeight.w800, fontSize: 15),
                ),
              ),
              const SizedBox(height: 20),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSectionHeader(String title, IconData icon) {
    final isDark = isDarkMode(context);
    return Padding(
      padding: const EdgeInsets.only(left: 4, bottom: 8),
      child: Row(
        children: [
          Icon(icon, size: 16, color: isDark ? gold : brand),
          const SizedBox(width: 6),
          Text(
            title,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w900,
              letterSpacing: 0.5,
              color: isDark ? gold : brand,
            ),
          ),
        ],
      ),
    );
  }

  String _roleTitle(String r) {
    return switch (r.toLowerCase()) {
      'admin' => 'QUẢN TRỊ VIÊN HỆ THỐNG',
      'manager' => 'QUẢN LÝ NHÀ HÀNG',
      'staff' => 'NHÂN VIÊN PHỤC VỤ (POS)',
      _ => r.toUpperCase(),
    };
  }
}

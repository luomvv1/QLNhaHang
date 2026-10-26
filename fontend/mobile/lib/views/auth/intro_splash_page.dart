import 'dart:async';
import 'package:flutter/material.dart';

import '../../core/session_manager.dart';
import '../../core/staff_core.dart';

class IntroSplashPage extends StatefulWidget {
  const IntroSplashPage({
    super.key,
    required this.onContinueToWork,
    required this.onGoToLogin,
  });

  final ValueChanged<StaffSession> onContinueToWork;
  final VoidCallback onGoToLogin;

  @override
  State<IntroSplashPage> createState() => _IntroSplashPageState();
}

class _IntroSplashPageState extends State<IntroSplashPage>
    with SingleTickerProviderStateMixin {
  final PageController _pageController = PageController();
  int _currentPage = 0;
  Timer? _autoSlideTimer;

  bool _isCheckingSession = true;
  StaffSession? _existingSession;
  Timer? _autoEnterTimer;
  double _autoEnterProgress = 0.0;
  Timer? _progressTimer;

  late AnimationController _glowController;
  late Animation<double> _glowAnimation;

  final List<IntroFeatureItem> _features = const [
    IntroFeatureItem(
      icon: Icons.table_restaurant_rounded,
      badge: 'TIẾP NHẬN & PHỤC VỤ',
      title: 'Order Tại Bàn Siêu Tốc',
      description:
          'Sơ đồ bàn trực quan theo phân khu. Mở bàn, chọn món, tùy biến size và ghi chú khẩu vị nhanh chóng chỉ trong vài thao tác chạm.',
      accentColor: gold,
    ),
    IntroFeatureItem(
      icon: Icons.soup_kitchen_rounded,
      badge: 'BẾP & BAR (KDS)',
      title: 'Đồng Bộ Bếp Realtime',
      description:
          'Món gửi tự động xuống hàng đợi bếp FIFO. Đầu bếp chế biến xong báo "Sẵn sàng" tức thì về màn hình điện thoại nhân viên.',
      accentColor: Color(0xFF10B981),
    ),
    IntroFeatureItem(
      icon: Icons.qr_code_2_rounded,
      badge: 'THANH TOÁN THÔNG MINH',
      title: 'VietQR Động & Ghép Bàn',
      description:
          'Hỗ trợ VietQR động cho từng bàn, in bill tạm tính, chuyển bàn linh hoạt và gộp hóa đơn đoàn khách dễ dàng.',
      accentColor: Color(0xFF38BDF8),
    ),
  ];

  @override
  void initState() {
    super.initState();

    _glowController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1800),
    )..repeat(reverse: true);

    _glowAnimation = Tween<double>(begin: 0.85, end: 1.15).animate(
      CurvedAnimation(parent: _glowController, curve: Curves.easeInOut),
    );

    _startAutoSlide();
    _checkSavedSession();
  }

  @override
  void dispose() {
    _autoSlideTimer?.cancel();
    _autoEnterTimer?.cancel();
    _progressTimer?.cancel();
    _pageController.dispose();
    _glowController.dispose();
    super.dispose();
  }

  void _startAutoSlide() {
    _autoSlideTimer?.cancel();
    _autoSlideTimer = Timer.periodic(const Duration(seconds: 4), (timer) {
      if (_existingSession != null) return; // Nếu đã có session thì không cần auto slide
      if (!mounted) return;
      final nextPage = (_currentPage + 1) % _features.length;
      _pageController.animateToPage(
        nextPage,
        duration: const Duration(milliseconds: 450),
        curve: Curves.easeInOutCubic,
      );
    });
  }

  Future<void> _checkSavedSession() async {
    setState(() => _isCheckingSession = true);
    final session = await SessionManager.verifyAndRestoreSession();
    if (!mounted) return;

    setState(() {
      _isCheckingSession = false;
      _existingSession = session;
    });

    if (session != null) {
      // Đã có phiên đăng nhập hợp lệ: chạy đếm ngược 2.5s để tự vào việc
      _startAutoEnterTimer();
    }
  }

  void _startAutoEnterTimer() {
    _autoSlideTimer?.cancel();
    const totalMs = 2400;
    const intervalMs = 60;
    int elapsed = 0;

    _progressTimer?.cancel();
    _progressTimer = Timer.periodic(const Duration(milliseconds: intervalMs), (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }
      elapsed += intervalMs;
      setState(() {
        _autoEnterProgress = (elapsed / totalMs).clamp(0.0, 1.0);
      });
      if (elapsed >= totalMs) {
        timer.cancel();
      }
    });

    _autoEnterTimer?.cancel();
    _autoEnterTimer = Timer(const Duration(milliseconds: totalMs), () {
      if (mounted && _existingSession != null) {
        widget.onContinueToWork(_existingSession!);
      }
    });
  }

  void _cancelAutoEnter() {
    _autoEnterTimer?.cancel();
    _progressTimer?.cancel();
    setState(() {
      _autoEnterProgress = 0.0;
    });
  }

  Future<void> _handleSwitchAccount() async {
    _cancelAutoEnter();
    await SessionManager.clearSession(keepSavedEmail: true);
    if (mounted) {
      setState(() {
        _existingSession = null;
      });
      _startAutoSlide();
    }
  }

  void _enterWorkImmediately() {
    _cancelAutoEnter();
    if (_existingSession != null) {
      widget.onContinueToWork(_existingSession!);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = isDarkMode(context);
    final size = MediaQuery.sizeOf(context);
    final isTablet = size.width >= 700;

    return Scaffold(
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: isDark
                ? const [
                    Color(0xFF061512),
                    Color(0xFF0F2622),
                    Color(0xFF0A1C19),
                  ]
                : const [
                    Color(0xFF0E302B),
                    Color(0xFF143B36),
                    Color(0xFF1D4A44),
                  ],
          ),
        ),
        child: SafeArea(
          child: Column(
            children: [
              // Top Bar: Theme Toggle & App Badge
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.08),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(
                          color: gold.withValues(alpha: 0.35),
                          width: 1,
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 7,
                            height: 7,
                            decoration: const BoxDecoration(
                              color: Color(0xFF10B981),
                              shape: BoxShape.circle,
                            ),
                          ),
                          const SizedBox(width: 6),
                          const Text(
                            'STAFF POS v2.9',
                            style: TextStyle(
                              color: gold,
                              fontSize: 11,
                              fontWeight: FontWeight.w800,
                              letterSpacing: 0.8,
                            ),
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: isDark ? 'Chuyển sang giao diện Sáng' : 'Chuyển sang giao diện Tối',
                      onPressed: () => ThemeController.instance.toggleTheme(),
                      icon: Icon(
                        isDark ? Icons.light_mode_rounded : Icons.dark_mode_rounded,
                        color: gold,
                        size: 20,
                      ),
                      style: IconButton.styleFrom(
                        backgroundColor: Colors.white.withValues(alpha: 0.08),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              // Hero Brand Emblem
              Padding(
                padding: const EdgeInsets.only(top: 8, bottom: 12),
                child: Column(
                  children: [
                    AnimatedBuilder(
                      animation: _glowAnimation,
                      builder: (context, child) {
                        return Transform.scale(
                          scale: _glowAnimation.value,
                          child: Container(
                            width: 76,
                            height: 76,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              gradient: RadialGradient(
                                colors: [
                                  gold.withValues(alpha: 0.35),
                                  Colors.transparent,
                                ],
                              ),
                            ),
                            child: Center(
                              child: Container(
                                width: 62,
                                height: 62,
                                decoration: BoxDecoration(
                                  color: const Color(0xFF0F2622),
                                  shape: BoxShape.circle,
                                  border: Border.all(color: gold, width: 2),
                                  boxShadow: [
                                    BoxShadow(
                                      color: gold.withValues(alpha: 0.3),
                                      blurRadius: 18,
                                      spreadRadius: 2,
                                    ),
                                  ],
                                ),
                                child: const Icon(
                                  Icons.restaurant_menu_rounded,
                                  size: 30,
                                  color: gold,
                                ),
                              ),
                            ),
                          ),
                        );
                      },
                    ),
                    const SizedBox(height: 6),
                    const Text(
                      'DOLA RESTAURANT',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 22,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 2.2,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      'Hệ Thống Phục Vụ & Gọi Món Tại Bàn',
                      style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.75),
                        fontSize: 13,
                        fontWeight: FontWeight.w500,
                        letterSpacing: 0.4,
                      ),
                    ),
                  ],
                ),
              ),

              // Feature Highlights Carousel ("giới thiệu chút")
              Expanded(
                child: Center(
                  child: ConstrainedBox(
                    constraints: BoxConstraints(maxWidth: isTablet ? 580 : 440),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Expanded(
                          child: PageView.builder(
                            controller: _pageController,
                            onPageChanged: (i) => setState(() => _currentPage = i),
                            itemCount: _features.length,
                            itemBuilder: (context, index) {
                              final item = _features[index];
                              return Padding(
                                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                                child: Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
                                  decoration: BoxDecoration(
                                    color: Colors.white.withValues(alpha: 0.06),
                                    borderRadius: BorderRadius.circular(20),
                                    border: Border.all(
                                      color: Colors.white.withValues(alpha: 0.12),
                                    ),
                                    boxShadow: [
                                      BoxShadow(
                                        color: Colors.black.withValues(alpha: 0.25),
                                        blurRadius: 20,
                                        offset: const Offset(0, 8),
                                      ),
                                    ],
                                  ),
                                  child: Center(
                                    child: SingleChildScrollView(
                                      physics: const ClampingScrollPhysics(),
                                      child: Column(
                                        mainAxisSize: MainAxisSize.min,
                                        mainAxisAlignment: MainAxisAlignment.center,
                                        children: [
                                          Container(
                                            width: 54,
                                            height: 54,
                                            decoration: BoxDecoration(
                                              color: item.accentColor.withValues(alpha: 0.15),
                                              shape: BoxShape.circle,
                                              border: Border.all(
                                                color: item.accentColor.withValues(alpha: 0.5),
                                                width: 1.5,
                                              ),
                                            ),
                                            child: Icon(
                                              item.icon,
                                              size: 28,
                                              color: item.accentColor,
                                            ),
                                          ),
                                          const SizedBox(height: 10),
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                                            decoration: BoxDecoration(
                                              color: item.accentColor.withValues(alpha: 0.18),
                                              borderRadius: BorderRadius.circular(12),
                                            ),
                                            child: Text(
                                              item.badge,
                                              style: TextStyle(
                                                color: item.accentColor,
                                                fontSize: 10,
                                                fontWeight: FontWeight.w800,
                                                letterSpacing: 0.8,
                                              ),
                                            ),
                                          ),
                                          const SizedBox(height: 8),
                                          Text(
                                            item.title,
                                            textAlign: TextAlign.center,
                                            style: const TextStyle(
                                              color: Colors.white,
                                              fontSize: 18,
                                              fontWeight: FontWeight.w800,
                                            ),
                                          ),
                                          const SizedBox(height: 6),
                                          Text(
                                            item.description,
                                            textAlign: TextAlign.center,
                                            style: TextStyle(
                                              color: Colors.white.withValues(alpha: 0.72),
                                              fontSize: 12.5,
                                              height: 1.4,
                                              fontWeight: FontWeight.w400,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ),
                                ),
                              );
                            },
                          ),
                        ),

                        // Carousel Dots Indicator
                        Padding(
                          padding: const EdgeInsets.only(top: 8, bottom: 12),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: List.generate(_features.length, (i) {
                              final isSelected = _currentPage == i;
                              return AnimatedContainer(
                                duration: const Duration(milliseconds: 300),
                                margin: const EdgeInsets.symmetric(horizontal: 4),
                                width: isSelected ? 24 : 7,
                                height: 7,
                                decoration: BoxDecoration(
                                  color: isSelected ? gold : Colors.white.withValues(alpha: 0.25),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                              );
                            }),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),

              // Bottom Action Card: Check Session vs Auto-Enter vs Login Button
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxWidth: isTablet ? 580 : 440),
                  child: _buildBottomAction(context),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBottomAction(BuildContext context) {
    if (_isCheckingSession) {
      // Đang kiểm tra phiên đăng nhập trong nền, đồng thời vẫn hiển thị nút sẵn sàng
      return Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          FilledButton.icon(
            style: FilledButton.styleFrom(
              backgroundColor: gold,
              foregroundColor: const Color(0xFF0F172A),
              elevation: 4,
              padding: const EdgeInsets.symmetric(vertical: 16),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
              ),
            ),
            onPressed: widget.onGoToLogin,
            icon: const Icon(Icons.login_rounded, size: 20),
            label: const Text(
              'Đăng Nhập Ca Làm',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w900,
                letterSpacing: 0.5,
              ),
            ),
          ),
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const SizedBox.square(
                dimension: 12,
                child: CircularProgressIndicator(
                  strokeWidth: 1.5,
                  color: gold,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                'Đang kiểm tra ca làm việc...',
                style: TextStyle(
                  color: Colors.white.withValues(alpha: 0.7),
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ),
        ],
      );
    }

    if (_existingSession != null) {
      // ĐÃ ĐĂNG NHẬP: Hiển thị lời chào & nút vào làm việc ngay
      final session = _existingSession!;
      final roleText = switch (session.role.toLowerCase()) {
        'admin' => 'Quản Trị Viên',
        'manager' => 'Quản Lý Nhà Hàng',
        'staff' => 'Nhân Viên Phục Vụ',
        _ => 'Nhân Viên',
      };

      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: const Color(0xFF0F2622).withValues(alpha: 0.95),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: gold.withValues(alpha: 0.4), width: 1.2),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.35),
              blurRadius: 18,
              offset: const Offset(0, 6),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // User preview row
            Row(
              children: [
                CircleAvatar(
                  radius: 22,
                  backgroundColor: gold.withValues(alpha: 0.2),
                  child: Text(
                    session.fullName.isNotEmpty ? session.fullName[0].toUpperCase() : 'N',
                    style: const TextStyle(
                      color: gold,
                      fontSize: 18,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Flexible(
                            child: Text(
                              session.fullName,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 16,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          ),
                          const SizedBox(width: 6),
                          const Icon(Icons.verified, color: gold, size: 16),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Text(
                        roleText,
                        style: TextStyle(
                          color: gold.withValues(alpha: 0.9),
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
                TextButton(
                  onPressed: _handleSwitchAccount,
                  style: TextButton.styleFrom(
                    foregroundColor: Colors.white70,
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  ),
                  child: const Text(
                    'Đổi tài khoản',
                    style: TextStyle(fontSize: 12, decoration: TextDecoration.underline),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 12),

            // Progress bar auto-enter indicator
            if (_autoEnterProgress > 0 && _autoEnterProgress < 1.0) ...[
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: _autoEnterProgress,
                  minHeight: 3,
                  backgroundColor: Colors.white.withValues(alpha: 0.1),
                  color: gold,
                ),
              ),
              const SizedBox(height: 8),
            ],

            // Enter work button
            FilledButton.icon(
              style: FilledButton.styleFrom(
                backgroundColor: gold,
                foregroundColor: const Color(0xFF0F172A),
                padding: const EdgeInsets.symmetric(vertical: 14),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              onPressed: _enterWorkImmediately,
              icon: const Icon(Icons.arrow_forward_rounded, size: 20),
              label: const Text(
                'Vào Ca Làm Việc Ngay',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 0.4,
                ),
              ),
            ),
          ],
        ),
      );
    }

    // CHƯA ĐĂNG NHẬP: Hiển thị nút Bắt đầu ca làm -> Mở màn hình đăng nhập
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: gold,
            foregroundColor: const Color(0xFF0F172A),
            elevation: 4,
            padding: const EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
            ),
          ),
          onPressed: widget.onGoToLogin,
          icon: const Icon(Icons.login_rounded, size: 20),
          label: const Text(
            'Đăng Nhập Ca Làm',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w900,
              letterSpacing: 0.5,
            ),
          ),
        ),
        const SizedBox(height: 8),
        TextButton(
          onPressed: widget.onGoToLogin,
          style: TextButton.styleFrom(
            foregroundColor: Colors.white.withValues(alpha: 0.75),
          ),
          child: const Text(
            'Bỏ qua giới thiệu & Đăng nhập ngay',
            style: TextStyle(fontSize: 13, fontWeight: FontWeight.w500),
          ),
        ),
      ],
    );
  }
}

class IntroFeatureItem {
  const IntroFeatureItem({
    required this.icon,
    required this.badge,
    required this.title,
    required this.description,
    required this.accentColor,
  });

  final IconData icon;
  final String badge;
  final String title;
  final String description;
  final Color accentColor;
}

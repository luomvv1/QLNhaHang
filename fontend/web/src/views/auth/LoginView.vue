<template>
  <main class="auth-page-wrapper d-flex align-items-center justify-content-center p-3 p-md-4">
    <div class="auth-container shadow-2xl overflow-hidden">
      <!-- ============================================================== -->
      <!-- CỘT TRÁI: SHOWCASE NHÀ HÀNG HƯƠNG SEN VỚI HÌNH ẢNH ẨM THỰC THẬT -->
      <!-- ============================================================== -->
      <section class="auth-hero d-flex flex-column justify-content-between p-4 p-xl-5 text-white position-relative">
        <!-- Background Image with Cinematic Dark Gradient Overlay -->
        <div class="hero-bg-image" style="background-image: url('/images/auth/hero-dining.jpg');"></div>
        <div class="hero-gradient-overlay"></div>

        <!-- Top Header & Brand Crest -->
        <div class="position-relative z-2">
          <div class="d-flex align-items-center gap-3 mb-3">
            <div class="brand-crest-frame shadow-lg">
              <Utensils :size="24" class="text-amber-gold" />
            </div>
            <div>
              <div class="brand-sub-badge">HƯƠNG SEN LUXURY DINING</div>
              <h2 class="brand-title fw-bolder mb-0">NHÀ HÀNG HƯƠNG SEN</h2>
            </div>
          </div>

          <div class="hero-quote-card p-3 rounded-4 backdrop-blur mt-3">
            <div class="d-flex align-items-center gap-2 mb-1.5">
              <Sparkles :size="15" class="text-warning" />
              <span class="quote-category text-uppercase fw-bold">Tinh Hoa Ẩm Thực Cố Đô</span>
            </div>
            <p class="quote-text mb-0">
              Không gian ẩm thực đẳng cấp kết hợp công nghệ điều hành bếp tự động và phục vụ thông minh theo thời gian thực.
            </p>
          </div>
        </div>

        <!-- Real Visual Showcase Cards (Thay thế emoji bằng hình ảnh ẩm thực thật) -->
        <div class="hero-visual-cards my-3 position-relative z-2">
          <div class="row g-2.5">
            <!-- Card 1: Bếp KDS với hình ảnh bếp thật -->
            <div class="col-12">
              <div class="feature-photo-card p-2 rounded-3 d-flex align-items-center gap-3">
                <img
                  src="/images/auth/kds-kitchen.jpg"
                  alt="Bếp KDS Nhà Hàng"
                  class="feature-thumb rounded-3 object-fit-cover shadow-sm"
                />
                <div class="flex-grow-1 overflow-hidden">
                  <div class="d-flex align-items-center gap-1.5 mb-0.5">
                    <Flame :size="14" class="text-danger" />
                    <span class="fw-bold text-white small">Điều Phối 3 Quầy Bếp (KDS)</span>
                  </div>
                  <div class="text-white-70 xs-text text-truncate">Quầy Nướng, Nấu & Pha Chế đồng bộ theo mẻ tức thì</div>
                </div>
                <span class="badge bg-emerald-subtle text-emerald border border-emerald-subtle px-2 py-1 rounded-pill xs-text fw-bold">Live</span>
              </div>
            </div>

            <!-- Card 2: Sơ đồ bàn POS với hình ảnh bàn tiệc thật -->
            <div class="col-12">
              <div class="feature-photo-card p-2 rounded-3 d-flex align-items-center gap-3">
                <img
                  src="/images/auth/pos-table.jpg"
                  alt="Không Gian Nhà Hàng"
                  class="feature-thumb rounded-3 object-fit-cover shadow-sm"
                />
                <div class="flex-grow-1 overflow-hidden">
                  <div class="d-flex align-items-center gap-1.5 mb-0.5">
                    <Smartphone :size="14" class="text-warning" />
                    <span class="fw-bold text-white small">Smart POS & Sơ Đồ Bàn Ăn</span>
                  </div>
                  <div class="text-white-70 xs-text text-truncate">Gọi món tại bàn, kiểm soát trạng thái phòng tiệc VIP</div>
                </div>
                <span class="badge bg-amber-subtle text-amber border border-amber-subtle px-2 py-1 rounded-pill xs-text fw-bold">POS</span>
              </div>
            </div>

            <!-- Card 3: Ẩm thực & Đồ uống thượng hạng -->
            <div class="col-12">
              <div class="feature-photo-card p-2 rounded-3 d-flex align-items-center gap-3">
                <img
                  src="/images/auth/food-hotpot.jpg"
                  alt="Ẩm Thực Thượng Hạng"
                  class="feature-thumb rounded-3 object-fit-cover shadow-sm"
                />
                <div class="flex-grow-1 overflow-hidden">
                  <div class="d-flex align-items-center gap-1.5 mb-0.5">
                    <Wine :size="14" class="text-info" />
                    <span class="fw-bold text-white small">Thực Đơn & Đặt Bàn Đẳng Cấp</span>
                  </div>
                  <div class="text-white-70 xs-text text-truncate">Hải sản tươi sống, lẩu cung đình & quầy bar giải khát</div>
                </div>
                <span class="badge bg-info-subtle text-info border border-info-subtle px-2 py-1 rounded-pill xs-text fw-bold">5 Sao</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer With Chef Signature Quote & Rating -->
        <div class="hero-footer pt-3 border-top border-white border-opacity-10 d-flex justify-content-between align-items-center position-relative z-2">
          <div class="d-flex align-items-center gap-2">
            <img
              src="/images/auth/chef-portrait.jpg"
              alt="Bếp Trưởng Điều Hành"
              class="chef-mini-avatar rounded-circle border border-warning"
            />
            <div>
              <div class="fw-bold text-white xs-text">Lê Bếp Trưởng</div>
              <div class="text-white-50" style="font-size: 0.68rem;">Bếp Trưởng Điều Hành Hương Sen</div>
            </div>
          </div>

          <router-link to="/customer/menu" target="_blank" class="text-warning text-decoration-none fw-bold small d-flex align-items-center gap-1">
            <span>Khám phá thực đơn</span>
            <ArrowRight :size="14" />
          </router-link>
        </div>
      </section>

      <!-- ============================================================== -->
      <!-- CỘT PHẢI: FORM ĐĂNG NHẬP / ĐĂNG KÝ HOẶC BẢNG CA LÀM ĐẦU BẾP     -->
      <!-- ============================================================== -->
      <section class="auth-form-card p-4 p-md-5 bg-white d-flex flex-column justify-content-center position-relative">
        <!-- ============================================================ -->
        <!-- TRƯỜNG HỢP 1: BẢNG XÁC NHẬN HỒ SƠ & VÀO CA LÀM BẾP (CHEF)    -->
        <!-- ============================================================ -->
        <div v-if="showChefShiftCard && chefSession" class="chef-onboarding-panel animate-fade-in">
          <!-- Chef Card Header with Real Kitchen Photo Banner -->
          <div class="chef-card-banner position-relative rounded-4 overflow-hidden mb-4 shadow-sm">
            <img
              src="/images/auth/kds-kitchen.jpg"
              alt="Gian Bếp Nhà Hàng"
              class="w-100 object-fit-cover banner-img"
            />
            <div class="banner-overlay p-3 d-flex flex-column justify-content-end">
              <span class="badge bg-warning text-dark align-self-start px-2.5 py-1 rounded-pill fw-bold text-uppercase mb-1" style="font-size: 0.68rem;">
                👨‍🍳 Bộ Phận Bếp Nhà Hàng
              </span>
              <h5 class="text-white fw-bold mb-0">Hồ Sơ Đầu Bếp & Xác Nhận Vào Ca</h5>
            </div>
          </div>

          <!-- Chef Profile Info with Real Portrait Photo -->
          <div class="d-flex align-items-center gap-3 p-3 bg-slate-50 border rounded-4 mb-3 shadow-xs">
            <div class="position-relative">
              <img
                src="/images/auth/chef-portrait.jpg"
                alt="Chân Dung Đầu Bếp"
                class="chef-portrait-img rounded-circle border border-warning shadow-md"
              />
              <span class="chef-online-badge" title="Đang trực ca"></span>
            </div>
            <div class="overflow-hidden flex-grow-1">
              <div class="d-flex align-items-center gap-2 mb-1">
                <h5 class="fw-bolder text-dark mb-0 text-truncate">{{ chefSession.fullName }}</h5>
              </div>
              <div class="text-muted small text-truncate mb-1">{{ chefSession.email }}</div>
              <span class="badge bg-warning-subtle text-warning border border-warning-subtle px-2.5 py-0.5 rounded-pill fw-bold" style="font-size: 0.7rem;">
                Đầu Bếp / Bếp Trưởng Điều Hành
              </span>
            </div>
          </div>

          <!-- 3 Station Badges with Real Food Pictures -->
          <div class="card border rounded-4 p-3 mb-3 bg-white shadow-xs">
            <div class="d-flex justify-content-between align-items-center mb-2.5">
              <span class="text-muted small fw-bold text-uppercase" style="font-size: 0.7rem; letter-spacing: 0.05em;">3 Quầy Bếp Phụ Trách:</span>
              <span class="badge bg-success-subtle text-success border border-success-subtle px-2.5 py-1 rounded-pill fw-bold" style="font-size: 0.68rem;">
                🟢 ĐÃ SẴN SÀNG NHẬN ĐƠN
              </span>
            </div>
            <div class="row g-2">
              <div class="col-4">
                <div class="station-mini-card p-1.5 rounded-3 border text-center">
                  <img src="/images/auth/hero-dining.jpg" alt="Quầy Nướng" class="w-100 rounded-2 station-thumb mb-1" />
                  <div class="fw-bold text-dark xs-text text-truncate">Quầy Nướng</div>
                  <small class="text-success" style="font-size: 0.65rem;">● Sẵn sàng</small>
                </div>
              </div>
              <div class="col-4">
                <div class="station-mini-card p-1.5 rounded-3 border text-center">
                  <img src="/images/auth/food-hotpot.jpg" alt="Quầy Lẩu" class="w-100 rounded-2 station-thumb mb-1" />
                  <div class="fw-bold text-dark xs-text text-truncate">Quầy Nấu & Lẩu</div>
                  <small class="text-success" style="font-size: 0.65rem;">● Sẵn sàng</small>
                </div>
              </div>
              <div class="col-4">
                <div class="station-mini-card p-1.5 rounded-3 border text-center">
                  <img src="/images/auth/food-drink.jpg" alt="Quầy Pha Chế" class="w-100 rounded-2 station-thumb mb-1" />
                  <div class="fw-bold text-dark xs-text text-truncate">Quầy Pha Chế</div>
                  <small class="text-success" style="font-size: 0.65rem;">● Sẵn sàng</small>
                </div>
              </div>
            </div>

            <div class="d-flex justify-content-between align-items-center pt-2.5 mt-2.5 border-top" style="font-size: 0.78rem;">
              <span class="text-muted">Thời gian vào ca:</span>
              <span class="fw-bold text-dark font-monospace">{{ chefSession.loginTime || currentTime }}</span>
            </div>
          </div>

          <!-- Primary & Secondary Action CTAs -->
          <div class="d-grid gap-2 mb-3">
            <button
              type="button"
              class="btn btn-warning py-3 fw-bolder text-dark rounded-3 shadow-md d-flex align-items-center justify-content-center gap-2 enter-shift-btn"
              @click="enterKitchenShift"
            >
              <Flame :size="20" class="text-danger" />
              <span class="fs-6">VÀO CA LÀM BẾP (KDS)</span>
            </button>

            <div class="row g-2">
              <div class="col-6">
                <button
                  type="button"
                  class="btn btn-outline-primary py-2 w-100 fw-bold rounded-3 small d-flex align-items-center justify-content-center gap-1.5"
                  @click="goToStaffPOS"
                >
                  <Smartphone :size="16" />
                  <span>Trang Phục Vụ</span>
                </button>
              </div>
              <div class="col-6">
                <button
                  type="button"
                  class="btn btn-outline-success py-2 w-100 fw-bold rounded-3 small d-flex align-items-center justify-content-center gap-1.5"
                  @click="goToCustomerMenu"
                >
                  <Utensils :size="16" />
                  <span>Trang Khách</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Logout / Switch Account -->
          <div class="text-center pt-2 border-top">
            <button type="button" class="btn btn-link btn-sm text-muted text-decoration-none" @click="logoutChef">
              <LogOut :size="14" class="me-1" /> Đổi tài khoản khác
            </button>
          </div>
        </div>

        <!-- ============================================================ -->
        <!-- TRƯỜNG HỢP 2: FORM ĐĂNG NHẬP / ĐĂNG KÝ HỆ THỐNG             -->
        <!-- ============================================================ -->
        <div v-else>
          <!-- Switch Tabs: Đăng Nhập vs Đăng Ký -->
          <div class="auth-tab-switcher p-1 bg-light rounded-3 d-flex mb-4">
            <button
              type="button"
              class="tab-btn flex-grow-1 py-2 rounded-3 fw-bold small transition"
              :class="activeTab === 'login' ? 'bg-white text-dark shadow-xs' : 'text-muted border-0 bg-transparent'"
              @click="activeTab = 'login'"
            >
              🔑 Đăng Nhập
            </button>
            <button
              type="button"
              class="tab-btn flex-grow-1 py-2 rounded-3 fw-bold small transition"
              :class="activeTab === 'register' ? 'bg-white text-dark shadow-xs' : 'text-muted border-0 bg-transparent'"
              @click="activeTab = 'register'"
            >
              📝 Đăng Ký Khách Hàng
            </button>
          </div>

          <!-- Error Alert Banner -->
          <div v-if="errorMessage" class="alert alert-danger py-2.5 px-3 small rounded-3 mb-3 d-flex align-items-center gap-2 shadow-xs">
            <span class="fs-6">⚠️</span>
            <span class="flex-grow-1">{{ errorMessage }}</span>
            <button type="button" class="btn-close btn-close-sm" @click="errorMessage = ''"></button>
          </div>

          <!-- Success Alert Banner -->
          <div v-if="successMessage" class="alert alert-success py-2.5 px-3 small rounded-3 mb-3 d-flex align-items-center gap-2 shadow-xs">
            <span class="fs-6">✅</span>
            <span class="flex-grow-1">{{ successMessage }}</span>
          </div>

          <!-- SUB-TAB 1: FORM ĐĂNG NHẬP -->
          <div v-if="activeTab === 'login'">
            <div class="mb-3 text-center text-md-start">
              <h4 class="fw-bold text-dark mb-1">Đăng Nhập Hệ Thống</h4>
              <p class="text-muted small mb-0">Truy cập không gian làm việc hoặc tài khoản thành viên.</p>
            </div>

            <form @submit.prevent="handleLogin" class="mb-3">
              <div class="mb-3">
                <label class="form-label fw-bold text-secondary text-uppercase" style="font-size: 0.72rem; letter-spacing: 0.05em;">Email đăng nhập</label>
                <div class="input-group">
                  <span class="input-group-text bg-light border-end-0 text-muted">
                    <Mail :size="17" />
                  </span>
                  <input
                    v-model.trim="email"
                    type="email"
                    class="form-control form-control-lg fs-6 border-start-0 ps-1"
                    placeholder="VD: admin@huongsen.vn"
                    required
                    autocomplete="username"
                  />
                </div>
              </div>

              <div class="mb-3">
                <div class="d-flex justify-content-between align-items-center mb-1">
                  <label class="form-label fw-bold text-secondary text-uppercase mb-0" style="font-size: 0.72rem; letter-spacing: 0.05em;">Mật khẩu</label>
                  <a href="javascript:void(0)" class="text-muted text-decoration-none" style="font-size: 0.75rem;" @click="fillDefaultPass">Mặc định: 123</a>
                </div>
                <div class="input-group">
                  <span class="input-group-text bg-light border-end-0 text-muted">
                    <Lock :size="17" />
                  </span>
                  <input
                    v-model="password"
                    :type="showPassword ? 'text' : 'password'"
                    class="form-control form-control-lg fs-6 border-start-0 border-end-0 ps-1"
                    placeholder="Nhập mật khẩu..."
                    required
                    autocomplete="current-password"
                  />
                  <button
                    type="button"
                    class="btn btn-light border border-start-0 text-muted"
                    @click="showPassword = !showPassword"
                    :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                  >
                    <EyeOff v-if="showPassword" :size="18" />
                    <Eye v-else :size="18" />
                  </button>
                </div>
              </div>

              <button
                type="submit"
                class="btn btn-primary w-100 py-3 fw-bolder text-uppercase rounded-3 shadow-sm login-submit-btn mb-3"
                :disabled="isLoading || isGoogleLoading"
              >
                <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                <span>{{ isLoading ? 'Đang xác thực hệ thống...' : 'ĐĂNG NHẬP' }}</span>
              </button>
            </form>

            <!-- Divider -->
            <div class="d-flex align-items-center my-3">
              <hr class="flex-grow-1 text-muted opacity-25 m-0" />
              <span class="px-2 text-muted fw-bold" style="font-size: 0.7rem; letter-spacing: 0.08em;">HOẶC ĐĂNG NHẬP VỚI GOOGLE</span>
              <hr class="flex-grow-1 text-muted opacity-25 m-0" />
            </div>

            <!-- Real Google OAuth Button (GIS Token Client Popup) -->
            <div class="mb-3">
              <button
                type="button"
                class="btn btn-outline-secondary w-100 py-2.5 d-flex align-items-center justify-content-center gap-2.5 fw-semibold rounded-3 shadow-xs bg-white text-dark border google-action-btn"
                :disabled="isLoading || isGoogleLoading"
                @click="triggerRealGoogleLogin"
                title="Mở cửa sổ chọn tài khoản Google thật trong trình duyệt"
              >
                <span v-if="isGoogleLoading" class="spinner-border spinner-border-sm text-primary me-1"></span>
                <svg v-else width="20" height="20" viewBox="0 0 24 24">
                  <path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17Z"/>
                  <path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.25v3.15C3.26 21.36 7.33 24 12 24Z"/>
                  <path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.25C.45 8.18 0 9.99 0 12s.45 3.82 1.25 5.42l4.03-3.15Z"/>
                  <path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.33 0 3.26 2.64 1.25 6.58l4.03 3.15c.95-2.83 3.6-4.98 6.72-4.98Z"/>
                </svg>
                <span class="fs-7">{{ isGoogleLoading ? 'Đang kết nối Google...' : 'Đăng nhập với Google' }}</span>
              </button>

              <div id="googleOfficialBtn" class="mt-2 d-flex justify-content-center"></div>
            </div>

            <!-- Quick Demo Accounts Row -->
            <div class="pt-3 border-top">
              <div class="d-flex justify-content-between align-items-center mb-2">
                <span class="text-muted fw-bold" style="font-size: 0.72rem; letter-spacing: 0.04em;">⚡ ĐĂNG NHẬP NHANH TÀI KHOẢN:</span>
              </div>
              <div class="d-flex flex-wrap gap-2">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-danger px-2.5 py-1 small fw-bold rounded-pill d-flex align-items-center gap-1"
                  @click="quickFill('admintest4@gmail.com', '123')"
                  title="Quản Trị Viên Toàn Quyền"
                >
                  <Crown :size="13" />
                  <span>Admin</span>
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-warning text-dark px-2.5 py-1 small fw-bold rounded-pill d-flex align-items-center gap-1"
                  @click="quickFill('daubep1@gmail.com', '123')"
                  title="Đầu Bếp / Bếp Trưởng (Hiện ca làm)"
                >
                  <ChefHat :size="13" />
                  <span>Đầu Bếp</span>
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-primary px-2.5 py-1 small fw-bold rounded-pill d-flex align-items-center gap-1"
                  @click="quickFill('nhanvien1@gmail.com', '123')"
                  title="Nhân Viên Phục Vụ"
                >
                  <Smartphone :size="13" />
                  <span>Phục Vụ</span>
                </button>
                <button
                  type="button"
                  class="btn btn-sm btn-outline-info text-dark px-2.5 py-1 small fw-bold rounded-pill d-flex align-items-center gap-1"
                  @click="quickFill('test@gmail.com', '123')"
                  title="Khách Hàng Thành Viên"
                >
                  <Utensils :size="13" />
                  <span>Khách Hàng</span>
                </button>
              </div>
            </div>
          </div>

          <!-- SUB-TAB 2: FORM ĐĂNG KÝ KHÁCH HÀNG -->
          <div v-else-if="activeTab === 'register'">
            <div class="mb-3 text-center text-md-start">
              <h4 class="fw-bold text-dark mb-1">Đăng Ký Thành Viên</h4>
              <p class="text-muted small mb-0">Tạo tài khoản để nhận ưu đãi và tích điểm khi dùng bữa.</p>
            </div>

            <!-- Fast Register with Google -->
            <button
              type="button"
              class="btn btn-outline-secondary w-100 py-2.5 d-flex align-items-center justify-content-center gap-2.5 fw-semibold rounded-3 shadow-xs bg-white text-dark border mb-3 google-action-btn"
              :disabled="isLoading || isGoogleLoading"
              @click="triggerRealGoogleLogin"
              title="Đăng ký hoặc liên kết ngay với tài khoản Google thật"
            >
              <span v-if="isGoogleLoading" class="spinner-border spinner-border-sm text-primary me-1"></span>
              <svg v-else width="20" height="20" viewBox="0 0 24 24">
                <path fill="#4285F4" d="M23.745 12.27c0-.7-.06-1.4-.19-2.07H12v4.51h6.6c-.29 1.52-1.14 2.82-2.4 3.68v3.05h3.88c2.27-2.09 3.665-5.17 3.665-9.17Z"/>
                <path fill="#34A853" d="M12 24c3.24 0 5.95-1.08 7.93-2.91l-3.88-3.05c-1.08.72-2.45 1.16-4.05 1.16-3.12 0-5.77-2.1-6.72-4.93H1.25v3.15C3.26 21.36 7.33 24 12 24Z"/>
                <path fill="#FBBC05" d="M5.28 14.27c-.25-.72-.38-1.49-.38-2.27s.13-1.55.38-2.27V6.58H1.25C.45 8.18 0 9.99 0 12s.45 3.82 1.25 5.42l4.03-3.15Z"/>
                <path fill="#EA4335" d="M12 4.75c1.77 0 3.35.61 4.6 1.8l3.42-3.42C17.95 1.19 15.24 0 12 0 7.33 0 3.26 2.64 1.25 6.58l4.03 3.15c.95-2.83 3.6-4.98 6.72-4.98Z"/>
              </svg>
              <span class="fs-7">{{ isGoogleLoading ? 'Đang kết nối Google...' : 'Đăng ký nhanh với Google' }}</span>
            </button>

            <!-- Divider -->
            <div class="d-flex align-items-center my-3">
              <hr class="flex-grow-1 text-muted opacity-25 m-0" />
              <span class="px-2 text-muted fw-bold" style="font-size: 0.7rem; letter-spacing: 0.08em;">HOẶC ĐIỀN THÔNG TIN</span>
              <hr class="flex-grow-1 text-muted opacity-25 m-0" />
            </div>

            <form @submit.prevent="handleRegister">
              <div class="mb-2">
                <label class="form-label fw-bold text-secondary small mb-1">Họ và tên</label>
                <input
                  v-model.trim="registerForm.fullName"
                  type="text"
                  class="form-control"
                  placeholder="VD: Nguyễn Văn An"
                  required
                />
              </div>

              <div class="row g-2 mb-2">
                <div class="col-md-6">
                  <label class="form-label fw-bold text-secondary small mb-1">Số điện thoại</label>
                  <input
                    v-model.trim="registerForm.phone"
                    type="tel"
                    class="form-control"
                    placeholder="VD: 0901234567"
                    required
                  />
                </div>
                <div class="col-md-6">
                  <label class="form-label fw-bold text-secondary small mb-1">Email</label>
                  <input
                    v-model.trim="registerForm.email"
                    type="email"
                    class="form-control"
                    placeholder="ban@gmail.com"
                    required
                  />
                </div>
              </div>

              <div class="row g-2 mb-3">
                <div class="col-md-6">
                  <label class="form-label fw-bold text-secondary small mb-1">Mật khẩu</label>
                  <input
                    v-model="registerForm.password"
                    type="password"
                    class="form-control"
                    minlength="3"
                    placeholder="Mật khẩu"
                    required
                  />
                </div>
                <div class="col-md-6">
                  <label class="form-label fw-bold text-secondary small mb-1">Xác nhận</label>
                  <input
                    v-model="registerForm.confirmPassword"
                    type="password"
                    class="form-control"
                    minlength="3"
                    placeholder="Nhập lại"
                    required
                  />
                </div>
              </div>

              <button
                type="submit"
                class="btn btn-warning w-100 py-2.5 fw-bolder text-dark rounded-3 shadow-sm mb-3"
                :disabled="isLoading || isGoogleLoading"
              >
                <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                <span>{{ isLoading ? 'Đang tạo tài khoản...' : 'HOÀN TẤT ĐĂNG KÝ' }}</span>
              </button>
            </form>

            <div class="text-center">
              <span class="small text-muted">Đã có tài khoản?</span>
              <button type="button" class="btn btn-link btn-sm fw-bold text-primary p-0 ms-1 text-decoration-none" @click="activeTab = 'login'">
                Đăng nhập ngay
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>
  </main>
</template>

<script setup>
import axios from 'axios';
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { roleHome } from '@/auth/access.mjs';
import {
  Utensils,
  Sparkles,
  Flame,
  Smartphone,
  Wine,
  Crown,
  ChefHat,
  Mail,
  Lock,
  Eye,
  EyeOff,
  ArrowRight,
  LogOut
} from '@lucide/vue';

const router = useRouter();

// Tab state: 'login' | 'register'
const activeTab = ref('login');

// Form States
const email = ref('');
const password = ref('');
const showPassword = ref(false);
const isLoading = ref(false);
const isGoogleLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

// Registration Form
const registerForm = reactive({
  fullName: '',
  phone: '',
  email: '',
  password: '',
  confirmPassword: ''
});

// Chef Shift Onboarding State
const showChefShiftCard = ref(false);
const chefSession = ref(null);
const currentTime = ref(new Date().toLocaleTimeString('vi-VN'));

// Google OAuth states
const defaultGoogleClientId = '726319516526-ta8jkgbkml0pqblp1fivh3hhva9lvi3f.apps.googleusercontent.com';
const googleClientId = ref(localStorage.getItem('google_client_id') || import.meta.env.VITE_GOOGLE_CLIENT_ID || defaultGoogleClientId);

onMounted(() => {
  initGoogleGsi();

  // Kiểm tra nếu Bếp đã đăng nhập sẵn mà vào lại trang Login
  const token = localStorage.getItem('token');
  const role = (localStorage.getItem('role') || '').toLowerCase();
  if (token && role === 'chef') {
    chefSession.value = {
      fullName: localStorage.getItem('fullName') || 'Bếp Trưởng Điều Hành',
      email: 'daubep1@gmail.com',
      role: 'chef',
      loginTime: new Date().toLocaleTimeString('vi-VN') + ' - ' + new Date().toLocaleDateString('vi-VN')
    };
    showChefShiftCard.value = true;
  }
});

// Khởi tạo Google Identity Services (GIS)
function initGoogleGsi() {
  if (typeof window === 'undefined' || !googleClientId.value) return;

  const setup = () => {
    if (window.google?.accounts?.id) {
      try {
        window.google.accounts.id.initialize({
          client_id: googleClientId.value,
          callback: handleGoogleIdTokenResponse,
          auto_select: false,
          cancel_on_tap_outside: true
        });

        window.google.accounts.id.prompt();

        const container = document.getElementById('googleOfficialBtn');
        if (container) {
          container.innerHTML = '';
          window.google.accounts.id.renderButton(container, {
            theme: 'outline',
            size: 'large',
            width: 320,
            text: 'signin_with',
            shape: 'rectangular'
          });
        }
      } catch (err) {
        console.warn('Google GSI init warning:', err);
      }
    }
  };

  if (window.google?.accounts?.id) {
    setup();
  } else {
    const timer = setInterval(() => {
      if (window.google?.accounts?.id) {
        clearInterval(timer);
        setup();
      }
    }, 200);
    setTimeout(() => clearInterval(timer), 4000);
  }
}

async function handleGoogleIdTokenResponse(response) {
  if (response?.credential) {
    await submitGoogleAuth({ idToken: response.credential });
  }
}

function triggerRealGoogleLogin() {
  isGoogleLoading.value = true;
  errorMessage.value = '';

  if (window.google?.accounts?.oauth2 && googleClientId.value) {
    try {
      const tokenClient = window.google.accounts.oauth2.initTokenClient({
        client_id: googleClientId.value,
        scope: 'email profile openid',
        callback: async (tokenResponse) => {
          if (tokenResponse.error) {
            isGoogleLoading.value = false;
            if (tokenResponse.error !== 'popup_closed_by_user') {
              errorMessage.value = 'Lỗi xác thực Google: ' + (tokenResponse.error_description || tokenResponse.error);
            }
            return;
          }
          if (tokenResponse.access_token) {
            await submitGoogleAuth({ accessToken: tokenResponse.access_token });
          } else {
            isGoogleLoading.value = false;
          }
        }
      });

      tokenClient.requestAccessToken({ prompt: 'select_account' });
      return;
    } catch (e) {
      console.warn('OAuth2 token client error:', e);
      isGoogleLoading.value = false;
    }
  }

  if (window.google?.accounts?.id) {
    window.google.accounts.id.prompt();
  }
  isGoogleLoading.value = false;
}

async function submitGoogleAuth(payload) {
  isGoogleLoading.value = true;
  errorMessage.value = '';
  try {
    const { data } = await axios.post('http://localhost:5079/api/Auth/google-login', payload);

    const token = data.token ?? data.Token;
    const role = (data.roleCode ?? data.RoleCode ?? 'customer').toLowerCase();
    const fullName = data.fullName ?? data.FullName ?? 'Khách Hàng Google';
    const emailStr = data.email ?? data.Email ?? 'google.user@huongsen.vn';

    if (!token) throw new Error('API Backend không trả về mã JWT Token.');

    localStorage.setItem('token', token);
    localStorage.setItem('role', role);
    localStorage.setItem('fullName', fullName);
    localStorage.setItem('userId', String(data.userId ?? data.UserId));
    if (data.avatarUrl) localStorage.setItem('avatarUrl', data.avatarUrl);
    window.dispatchEvent(new Event('auth-changed'));

    if (role === 'chef') {
      chefSession.value = {
        fullName,
        email: emailStr,
        role,
        avatarUrl: data.avatarUrl || null,
        loginTime: new Date().toLocaleTimeString('vi-VN') + ' - ' + new Date().toLocaleDateString('vi-VN')
      };
      showChefShiftCard.value = true;
      return;
    }

    const redirect = router.currentRoute.value.query.redirect;
    await router.push(typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')
      ? redirect : roleHome(role));
  } catch (error) {
    errorMessage.value = error.response?.data?.message ?? error.message ?? 'Đăng nhập Google thất bại.';
  } finally {
    isGoogleLoading.value = false;
  }
}

async function handleLogin() {
  isLoading.value = true;
  errorMessage.value = '';
  try {
    const { data } = await axios.post('http://localhost:5079/api/Auth/login', {
      email: email.value,
      password: password.value
    });

    const token = data.token ?? data.Token;
    const role = (data.roleCode ?? data.RoleCode ?? '').toLowerCase();
    const fullName = data.fullName ?? data.FullName ?? 'Người dùng';

    if (!token) throw new Error('API không trả về mã xác thực (Token).');

    localStorage.setItem('token', token);
    localStorage.setItem('role', role);
    localStorage.setItem('fullName', fullName);
    localStorage.setItem('userId', String(data.userId ?? data.UserId));
    window.dispatchEvent(new Event('auth-changed'));

    if (role === 'chef') {
      chefSession.value = {
        fullName,
        email: email.value,
        role,
        loginTime: new Date().toLocaleTimeString('vi-VN') + ' - ' + new Date().toLocaleDateString('vi-VN')
      };
      showChefShiftCard.value = true;
      return;
    }

    const redirect = router.currentRoute.value.query.redirect;
    await router.push(typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')
      ? redirect : roleHome(role));
  } catch (error) {
    errorMessage.value = error.response?.data?.message ?? error.message ?? 'Đăng nhập thất bại. Vui lòng kiểm tra lại email hoặc mật khẩu.';
  } finally {
    isLoading.value = false;
  }
}

async function handleRegister() {
  if (registerForm.password !== registerForm.confirmPassword) {
    errorMessage.value = 'Mật khẩu xác nhận không khớp!';
    return;
  }

  isLoading.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.post('http://localhost:5079/api/Auth/register', {
      fullName: registerForm.fullName,
      phone: registerForm.phone,
      email: registerForm.email,
      password: registerForm.password
    });

    const { data } = await axios.post('http://localhost:5079/api/Auth/login', {
      email: registerForm.email,
      password: registerForm.password
    });

    localStorage.setItem('token', data.token ?? data.Token);
    localStorage.setItem('role', (data.roleCode ?? data.RoleCode ?? 'customer').toLowerCase());
    localStorage.setItem('fullName', data.fullName ?? data.FullName ?? registerForm.fullName);
    localStorage.setItem('userId', String(data.userId ?? data.UserId));
    window.dispatchEvent(new Event('auth-changed'));

    successMessage.value = 'Đăng ký thành công! Đang chuyển đến trang tài khoản...';
    setTimeout(() => {
      router.push('/customer/account');
    }, 800);
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin.';
  } finally {
    isLoading.value = false;
  }
}

function enterKitchenShift() {
  router.push('/chef');
}

function goToStaffPOS() {
  router.push('/staff');
}

function goToCustomerMenu() {
  router.push('/customer/menu');
}

function logoutChef() {
  localStorage.removeItem('token');
  localStorage.removeItem('role');
  localStorage.removeItem('fullName');
  localStorage.removeItem('userId');
  showChefShiftCard.value = false;
  chefSession.value = null;
  window.dispatchEvent(new Event('auth-changed'));
}

function quickFill(userEmail, userPassword) {
  email.value = userEmail;
  password.value = userPassword;
  activeTab.value = 'login';
}

function fillDefaultPass() {
  password.value = '123';
}
</script>

<style scoped>
.auth-page-wrapper {
  min-height: 100vh;
  background: radial-gradient(circle at 10% 20%, #0d2824 0%, #061513 100%);
  font-family: 'Quicksand', 'Plus Jakarta Sans', system-ui, sans-serif;
}

.auth-container {
  width: 100%;
  max-width: 1120px;
  background: #ffffff;
  border-radius: 24px;
  display: grid;
  grid-template-columns: 1.15fr 1fr;
  min-height: 670px;
  border: 1px solid rgba(255, 255, 255, 0.15);
}

/* ==============================================================
   CỘT TRÁI: HERO VỚI ẢNH ẨM THỰC THẬT
   ============================================================== */
.auth-hero {
  position: relative;
  overflow: hidden;
  border-right: 1px solid rgba(255, 255, 255, 0.1);
}

.hero-bg-image {
  position: absolute;
  inset: 0;
  background-size: cover;
  background-position: center;
  filter: brightness(0.65) saturate(1.2);
  transform: scale(1.05);
  transition: transform 10s ease;
  z-index: 0;
}
.auth-hero:hover .hero-bg-image {
  transform: scale(1.1);
}

.hero-gradient-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(145deg, rgba(6, 22, 19, 0.92) 0%, rgba(13, 38, 34, 0.84) 50%, rgba(4, 15, 13, 0.94) 100%);
  backdrop-filter: blur(2px);
  z-index: 1;
}

.brand-crest-frame {
  width: 54px;
  height: 54px;
  background: rgba(255, 255, 255, 0.12);
  border: 2px solid rgba(212, 175, 55, 0.5);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(8px);
}
.text-amber-gold {
  color: #f3c68f;
}

.brand-sub-badge {
  font-size: 0.68rem;
  letter-spacing: 0.2em;
  color: #e5b85c;
  font-weight: 800;
}

.brand-title {
  font-size: 1.55rem;
  color: #ffffff;
  letter-spacing: 0.04em;
}

.hero-quote-card {
  background: rgba(255, 255, 255, 0.07);
  border: 1px solid rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(10px);
}

.quote-category {
  font-size: 0.68rem;
  letter-spacing: 0.12em;
  color: #e5b85c;
}

.quote-text {
  font-size: 0.86rem;
  line-height: 1.6;
  color: #e2ece8;
}

/* Feature photo cards */
.feature-photo-card {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(8px);
  transition: transform 0.2s ease, background 0.2s ease;
}
.feature-photo-card:hover {
  background: rgba(255, 255, 255, 0.14);
  transform: translateX(4px);
}

.feature-thumb {
  width: 56px;
  height: 56px;
  flex-shrink: 0;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.chef-mini-avatar {
  width: 36px;
  height: 36px;
  object-fit: cover;
  flex-shrink: 0;
}

.xs-text {
  font-size: 0.76rem;
}
.text-white-70 {
  color: rgba(255, 255, 255, 0.75);
}

.bg-emerald-subtle { background: rgba(16, 185, 129, 0.2); }
.text-emerald { color: #34d399; }
.border-emerald-subtle { border-color: rgba(16, 185, 129, 0.3); }

.bg-amber-subtle { background: rgba(245, 158, 11, 0.2); }
.text-amber { color: #fbbf24; }
.border-amber-subtle { border-color: rgba(245, 158, 11, 0.3); }

/* ==============================================================
   CỘT PHẢI: FORM CARD
   ============================================================== */
.auth-form-card {
  background: #ffffff;
}

.auth-tab-switcher {
  border: 1px solid #e2e8f0;
}

.tab-btn {
  border: none;
  cursor: pointer;
}

.login-submit-btn {
  background: linear-gradient(135deg, #0d3831 0%, #174f46 100%);
  border: none;
  letter-spacing: 0.06em;
  transition: all 0.25s ease;
}
.login-submit-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, #b87d32 0%, #d49a4e 100%);
  color: #111;
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(184, 125, 50, 0.3);
}

.google-action-btn {
  transition: all 0.2s ease;
}
.google-action-btn:hover:not(:disabled) {
  background-color: #f8fafc !important;
  border-color: #cbd5e1 !important;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06) !important;
}

/* ==============================================================
   CHEF SHIFT ONBOARDING PANEL VỚI ẢNH THẬT
   ============================================================== */
.chef-card-banner {
  height: 90px;
}
.banner-img {
  height: 90px;
  filter: brightness(0.7);
}
.banner-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(to top, rgba(15, 23, 42, 0.85) 0%, rgba(15, 23, 42, 0.2) 100%);
}

.chef-portrait-img {
  width: 66px;
  height: 66px;
  object-fit: cover;
}

.chef-online-badge {
  position: absolute;
  bottom: 2px;
  right: 2px;
  width: 14px;
  height: 14px;
  background: #10b981;
  border: 2.5px solid #ffffff;
  border-radius: 50%;
  animation: pulseDot 1.5s infinite;
}
@keyframes pulseDot {
  0% { transform: scale(0.9); opacity: 0.8; box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
  70% { transform: scale(1.1); opacity: 1; box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
  100% { transform: scale(0.9); opacity: 0.8; box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}

.station-mini-card {
  background: #fafaf9;
  border-color: #e7e5e4 !important;
  transition: transform 0.15s ease;
}
.station-mini-card:hover {
  transform: translateY(-2px);
  border-color: #f59e0b !important;
}

.station-thumb {
  height: 48px;
  object-fit: cover;
}

.enter-shift-btn {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  border: none;
  transition: all 0.2s ease;
  letter-spacing: 0.04em;
}
.enter-shift-btn:hover {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(245, 158, 11, 0.35);
}

.animate-fade-in {
  animation: fadeIn 0.3s ease;
}
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(8px); }
  to { opacity: 1; transform: translateY(0); }
}

@media (max-width: 900px) {
  .auth-container {
    grid-template-columns: 1fr;
    max-width: 480px;
  }
  .auth-hero {
    display: none !important;
  }
}
</style>

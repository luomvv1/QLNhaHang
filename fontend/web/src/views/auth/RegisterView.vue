<template>
  <main class="auth-page-wrapper d-flex align-items-center justify-content-center p-3 p-md-4">
    <div class="auth-container shadow-2xl overflow-hidden">
      <!-- ============================================================== -->
      <!-- CỘT TRÁI: SHOWCASE ĐẶC QUYỀN HỘI VIÊN VỚI HÌNH ẢNH THẬT        -->
      <!-- ============================================================== -->
      <section class="auth-hero d-flex flex-column justify-content-between p-4 p-xl-5 text-white position-relative">
        <!-- Background Image with Cinematic Dark Gradient Overlay -->
        <div class="hero-bg-image" style="background-image: url('/images/auth/hero-interior.jpg');"></div>
        <div class="hero-gradient-overlay"></div>

        <!-- Top Header & Brand Crest -->
        <div class="position-relative z-2">
          <div class="d-flex align-items-center gap-3 mb-3">
            <div class="brand-crest-frame shadow-lg">
              <Award :size="24" class="text-amber-gold" />
            </div>
            <div>
              <div class="brand-sub-badge">THÀNH VIÊN VIP HƯƠNG SEN</div>
              <h2 class="brand-title fw-bolder mb-0">ĐĂNG KÝ HỘI VIÊN</h2>
            </div>
          </div>

          <div class="hero-quote-card p-3 rounded-4 backdrop-blur mt-3">
            <div class="d-flex align-items-center gap-2 mb-1.5">
              <Sparkles :size="15" class="text-warning" />
              <span class="quote-category text-uppercase fw-bold">Đặc Quyền Dành Cho Thực Khách</span>
            </div>
            <p class="quote-text mb-0">
              Trở thành hội viên để tích điểm tự động khi dùng bữa, đặt bàn ưu tiên và nhận thực đơn đặc sắc theo mùa.
            </p>
          </div>
        </div>

        <!-- Real Visual Showcase Cards (Hình ảnh ẩm thực & phòng tiệc thật) -->
        <div class="hero-visual-cards my-3 position-relative z-2">
          <div class="row g-2.5">
            <!-- Perk 1: Tích điểm mọi hóa đơn -->
            <div class="col-12">
              <div class="feature-photo-card p-2 rounded-3 d-flex align-items-center gap-3">
                <img
                  src="/images/auth/hero-dining.jpg"
                  alt="Tích Điểm Hội Viên"
                  class="feature-thumb rounded-3 object-fit-cover shadow-sm"
                />
                <div class="flex-grow-1 overflow-hidden">
                  <div class="d-flex align-items-center gap-1.5 mb-0.5">
                    <Award :size="14" class="text-warning" />
                    <span class="fw-bold text-white small">Tích Điểm Mọi Hóa Đơn</span>
                  </div>
                  <div class="text-white-70 xs-text text-truncate">Tự động quy đổi điểm thưởng trực tiếp vào hóa đơn thanh toán</div>
                </div>
                <span class="badge bg-amber-subtle text-amber border border-amber-subtle px-2 py-1 rounded-pill xs-text fw-bold">VIP</span>
              </div>
            </div>

            <!-- Perk 2: Đặt bàn tiệc VIP ưu tiên -->
            <div class="col-12">
              <div class="feature-photo-card p-2 rounded-3 d-flex align-items-center gap-3">
                <img
                  src="/images/auth/pos-table.jpg"
                  alt="Bàn Tiệc Ưu Tiên"
                  class="feature-thumb rounded-3 object-fit-cover shadow-sm"
                />
                <div class="flex-grow-1 overflow-hidden">
                  <div class="d-flex align-items-center gap-1.5 mb-0.5">
                    <Utensils :size="14" class="text-emerald" />
                    <span class="fw-bold text-white small">Đặt Bàn Ưu Tiên &amp; Giữ Chỗ</span>
                  </div>
                  <div class="text-white-70 xs-text text-truncate">Lựa chọn trước vị trí sảnh tiệc hoặc phòng VIP riêng tư</div>
                </div>
                <span class="badge bg-emerald-subtle text-emerald border border-emerald-subtle px-2 py-1 rounded-pill xs-text fw-bold">Ưu tiên</span>
              </div>
            </div>

            <!-- Perk 3: Món quà sinh nhật & sự kiện -->
            <div class="col-12">
              <div class="feature-photo-card p-2 rounded-3 d-flex align-items-center gap-3">
                <img
                  src="/images/auth/food-drink.jpg"
                  alt="Ưu Đãi Sinh Nhật"
                  class="feature-thumb rounded-3 object-fit-cover shadow-sm"
                />
                <div class="flex-grow-1 overflow-hidden">
                  <div class="d-flex align-items-center gap-1.5 mb-0.5">
                    <Wine :size="14" class="text-info" />
                    <span class="fw-bold text-white small">Quà Tặng Sinh Nhật &amp; Sự Kiện</span>
                  </div>
                  <div class="text-white-70 xs-text text-truncate">Voucher giảm giá và món tráng miệng thượng hạng miễn phí</div>
                </div>
                <span class="badge bg-info-subtle text-info border border-info-subtle px-2 py-1 rounded-pill xs-text fw-bold">Quà tặng</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer With Return to Login Link -->
        <div class="hero-footer pt-3 border-top border-white border-opacity-10 d-flex justify-content-between align-items-center position-relative z-2">
          <small class="text-white-50">© 2026 Hương Sen Dining Suite</small>
          <router-link to="/login" class="text-warning text-decoration-none fw-bold small d-flex align-items-center gap-1">
            <span>Đã có tài khoản? Đăng nhập</span>
            <ArrowRight :size="14" />
          </router-link>
        </div>
      </section>

      <!-- ============================================================== -->
      <!-- CỘT PHẢI: FORM ĐĂNG KÝ TÀI KHOẢN MỚI                           -->
      <!-- ============================================================== -->
      <section class="auth-form-card p-4 p-md-5 bg-white d-flex flex-column justify-content-center position-relative">
        <div class="mb-3 text-center text-md-start">
          <div class="d-flex align-items-center justify-content-between mb-1">
            <span class="badge bg-warning-subtle text-warning border border-warning-subtle px-2.5 py-1 rounded-pill fw-bold text-uppercase" style="font-size: 0.68rem;">
              Khách Hàng Mới
            </span>
            <router-link to="/login" class="small fw-bold text-primary text-decoration-none">
              Đăng nhập ngay
            </router-link>
          </div>
          <h3 class="fw-bolder text-dark mb-1">Tạo Tài Khoản Thành Viên</h3>
          <p class="text-muted small mb-0">Đăng ký bằng email hoặc kết nối nhanh bằng tài khoản Google.</p>
        </div>

        <!-- Alerts -->
        <div v-if="errorMessage" class="alert alert-danger py-2.5 px-3 small rounded-3 mb-3 d-flex align-items-center gap-2 shadow-xs">
          <span>⚠️</span>
          <span class="flex-grow-1">{{ errorMessage }}</span>
          <button type="button" class="btn-close btn-close-sm" @click="errorMessage = ''"></button>
        </div>

        <div v-if="successMessage" class="alert alert-success py-2.5 px-3 small rounded-3 mb-3 d-flex align-items-center gap-2 shadow-xs">
          <span>✅</span>
          <span class="flex-grow-1">{{ successMessage }}</span>
        </div>

        <!-- Fast Register with Google OAuth 2.0 -->
        <button
          type="button"
          class="btn btn-outline-secondary w-100 py-2.5 d-flex align-items-center justify-content-center gap-2.5 fw-semibold rounded-3 shadow-xs bg-white text-dark border mb-3 google-action-btn"
          :disabled="loading || isGoogleLoading"
          @click="triggerRealGoogleRegister"
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

        <!-- Registration Form -->
        <form @submit.prevent="register">
          <div class="mb-2.5">
            <label class="form-label fw-bold text-secondary text-uppercase small mb-1" style="font-size: 0.72rem; letter-spacing: 0.04em;">Họ và tên</label>
            <div class="input-group">
              <span class="input-group-text bg-light border-end-0 text-muted">
                <User :size="17" />
              </span>
              <input
                v-model.trim="form.fullName"
                type="text"
                class="form-control border-start-0 ps-1"
                placeholder="Nguyễn Văn An"
                required
              />
            </div>
          </div>

          <div class="row g-2 mb-2.5">
            <div class="col-md-6">
              <label class="form-label fw-bold text-secondary text-uppercase small mb-1" style="font-size: 0.72rem; letter-spacing: 0.04em;">Số điện thoại</label>
              <div class="input-group">
                <span class="input-group-text bg-light border-end-0 text-muted">
                  <Phone :size="17" />
                </span>
                <input
                  v-model.trim="form.phone"
                  type="tel"
                  class="form-control border-start-0 ps-1"
                  placeholder="0901234567"
                  required
                />
              </div>
            </div>
            <div class="col-md-6">
              <label class="form-label fw-bold text-secondary text-uppercase small mb-1" style="font-size: 0.72rem; letter-spacing: 0.04em;">Email</label>
              <div class="input-group">
                <span class="input-group-text bg-light border-end-0 text-muted">
                  <Mail :size="17" />
                </span>
                <input
                  v-model.trim="form.email"
                  type="email"
                  class="form-control border-start-0 ps-1"
                  placeholder="ban@gmail.com"
                  required
                />
              </div>
            </div>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-md-6">
              <label class="form-label fw-bold text-secondary text-uppercase small mb-1" style="font-size: 0.72rem; letter-spacing: 0.04em;">Mật khẩu</label>
              <div class="input-group">
                <span class="input-group-text bg-light border-end-0 text-muted">
                  <Lock :size="17" />
                </span>
                <input
                  v-model="form.password"
                  :type="showPass ? 'text' : 'password'"
                  class="form-control border-start-0 ps-1"
                  minlength="3"
                  placeholder="Mật khẩu"
                  required
                />
              </div>
            </div>
            <div class="col-md-6">
              <label class="form-label fw-bold text-secondary text-uppercase small mb-1" style="font-size: 0.72rem; letter-spacing: 0.04em;">Xác nhận</label>
              <div class="input-group">
                <span class="input-group-text bg-light border-end-0 text-muted">
                  <Lock :size="17" />
                </span>
                <input
                  v-model="form.confirmPassword"
                  :type="showPass ? 'text' : 'password'"
                  class="form-control border-start-0 ps-1"
                  minlength="3"
                  placeholder="Nhập lại"
                  required
                />
              </div>
            </div>
          </div>

          <div class="d-flex align-items-center justify-content-between mb-3">
            <div class="form-check">
              <input class="form-check-input" type="checkbox" id="showPassToggle" v-model="showPass" />
              <label class="form-check-label text-muted small cursor-pointer" for="showPassToggle">
                Hiện mật khẩu
              </label>
            </div>
          </div>

          <button
            type="submit"
            class="btn btn-warning w-100 py-3 fw-bolder text-dark rounded-3 shadow-sm mb-3 submit-reg-btn"
            :disabled="loading || isGoogleLoading"
          >
            <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
            <span>{{ loading ? 'Đang tạo tài khoản...' : 'HOÀN TẤT ĐĂNG KÝ HỘI VIÊN' }}</span>
          </button>
        </form>

        <p class="text-center small text-muted mb-0">
          Đã có tài khoản?
          <router-link to="/login" class="fw-bold text-primary text-decoration-none ms-1">Đăng nhập ngay</router-link>
        </p>
      </section>
    </div>
  </main>
</template>

<script setup>
import { reactive, ref, onMounted } from 'vue';
import axios from 'axios';
import { useRouter } from 'vue-router';
import {
  Award,
  Sparkles,
  Utensils,
  Wine,
  ArrowRight,
  User,
  Phone,
  Mail,
  Lock
} from '@lucide/vue';

const router = useRouter();
const loading = ref(false);
const isGoogleLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const showPass = ref(false);

const form = reactive({
  fullName: '',
  phone: '',
  email: '',
  password: '',
  confirmPassword: ''
});

const defaultGoogleClientId = '726319516526-ta8jkgbkml0pqblp1fivh3hhva9lvi3f.apps.googleusercontent.com';
const googleClientId = ref(localStorage.getItem('google_client_id') || import.meta.env.VITE_GOOGLE_CLIENT_ID || defaultGoogleClientId);

onMounted(() => {
  if (typeof window !== 'undefined' && window.google?.accounts?.id && googleClientId.value) {
    try {
      window.google.accounts.id.initialize({
        client_id: googleClientId.value,
        callback: (res) => {
          if (res?.credential) {
            handleGoogleAuthSuccess({ idToken: res.credential });
          }
        }
      });
      window.google.accounts.id.prompt();
    } catch (e) {
      console.warn('GSI init:', e);
    }
  }
});

function triggerRealGoogleRegister() {
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
            await handleGoogleAuthSuccess({ accessToken: tokenResponse.access_token });
          } else {
            isGoogleLoading.value = false;
          }
        }
      });

      tokenClient.requestAccessToken({ prompt: 'select_account' });
      return;
    } catch (e) {
      console.warn('OAuth2 error:', e);
      isGoogleLoading.value = false;
    }
  }

  if (window.google?.accounts?.id) {
    window.google.accounts.id.prompt();
  }
  isGoogleLoading.value = false;
}

async function handleGoogleAuthSuccess(payload) {
  isGoogleLoading.value = true;
  errorMessage.value = '';
  try {
    const { data } = await axios.post('http://localhost:5079/api/Auth/google-login', payload);
    const token = data.token ?? data.Token;
    const role = (data.roleCode ?? data.RoleCode ?? 'customer').toLowerCase();
    const fullName = data.fullName ?? data.FullName ?? 'Khách Hàng Google';

    if (!token) throw new Error('API không trả về mã xác thực.');

    localStorage.setItem('token', token);
    localStorage.setItem('role', role);
    localStorage.setItem('fullName', fullName);
    localStorage.setItem('userId', String(data.userId ?? data.UserId));
    if (data.avatarUrl) localStorage.setItem('avatarUrl', data.avatarUrl);
    window.dispatchEvent(new Event('auth-changed'));

    router.push('/customer/account');
  } catch (error) {
    errorMessage.value = error.response?.data?.message || error.message || 'Đăng ký bằng Google thất bại.';
  } finally {
    isGoogleLoading.value = false;
  }
}

async function register() {
  if (form.password !== form.confirmPassword) {
    errorMessage.value = 'Mật khẩu xác nhận không khớp!';
    return;
  }

  loading.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.post('http://localhost:5079/api/Auth/register', {
      fullName: form.fullName,
      phone: form.phone,
      email: form.email,
      password: form.password
    });

    const { data } = await axios.post('http://localhost:5079/api/Auth/login', {
      email: form.email,
      password: form.password
    });

    localStorage.setItem('token', data.token ?? data.Token);
    localStorage.setItem('role', (data.roleCode ?? data.RoleCode ?? 'customer').toLowerCase());
    localStorage.setItem('fullName', data.fullName ?? data.FullName ?? form.fullName);
    localStorage.setItem('userId', String(data.userId ?? data.UserId));
    window.dispatchEvent(new Event('auth-changed'));

    successMessage.value = 'Tạo tài khoản thành công! Đang chuyển hướng...';
    setTimeout(() => {
      router.push('/customer/account');
    }, 800);
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tạo tài khoản. Vui lòng thử lại.';
  } finally {
    loading.value = false;
  }
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

.xs-text {
  font-size: 0.76rem;
}
.text-white-70 {
  color: rgba(255, 255, 255, 0.75);
}

.bg-amber-subtle { background: rgba(245, 158, 11, 0.2); }
.text-amber { color: #fbbf24; }
.border-amber-subtle { border-color: rgba(245, 158, 11, 0.3); }

.bg-emerald-subtle { background: rgba(16, 185, 129, 0.2); }
.text-emerald { color: #34d399; }
.border-emerald-subtle { border-color: rgba(16, 185, 129, 0.3); }

.bg-info-subtle { background: rgba(59, 130, 246, 0.2); }
.text-info { color: #60a5fa; }
.border-info-subtle { border-color: rgba(59, 130, 246, 0.3); }

.auth-form-card {
  background: #ffffff;
}

.submit-reg-btn {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  border: none;
  transition: all 0.2s ease;
  letter-spacing: 0.04em;
}
.submit-reg-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, #fbbf24 0%, #f59e0b 100%);
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(245, 158, 11, 0.35);
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

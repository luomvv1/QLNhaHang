<template>
  <main class="login-wrapper d-flex justify-content-center align-items-center min-vh-100 p-3">
    <div class="card-login card-modern shadow-lg overflow-hidden">
      <!-- Top Brand Header -->
      <div class="login-header text-center p-4 text-white">
        <div class="brand-icon-circle mx-auto mb-3">
          🍽️
        </div>
        <h3 class="fw-bold mb-1 tracking-wide">NHÀ HÀNG HƯƠNG SEN</h3>
        <p class="small text-white-50 mb-0">Hệ Thống Quản Lý & Vận Hành Nhà Hàng Thông Minh</p>
      </div>

      <!-- Login Form Body -->
      <div class="p-4 p-md-5 bg-white">
        <div class="mb-4 text-center">
          <h5 class="fw-bold text-dark mb-1">Đăng Nhập Hệ Thống</h5>
          <small class="text-muted">Nhập thông tin tài khoản được cấp để tiếp tục</small>
        </div>

        <div v-if="errorMessage" class="alert alert-danger py-2 small mb-4 d-flex align-items-center gap-2">
          <span>⚠️</span>
          <span>{{ errorMessage }}</span>
        </div>

        <form @submit.prevent="handleLogin">
          <div class="mb-3">
            <label class="form-label fw-semibold text-muted small">EMAIL ĐĂNG NHẬP</label>
            <div class="input-group">
              <span class="input-group-text bg-light border-end-0 text-muted">✉️</span>
              <input
                v-model="email"
                type="email"
                class="form-control border-start-0 ps-1"
                placeholder="VD: admin@gmail.com"
                required
              />
            </div>
          </div>

          <div class="mb-4">
            <label class="form-label fw-semibold text-muted small">MẬT KHẨU</label>
            <div class="input-group">
              <span class="input-group-text bg-light border-end-0 text-muted">🔒</span>
              <input
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                class="form-control border-start-0 border-end-0 ps-1"
                placeholder="Nhập mật khẩu..."
                required
              />
              <button
                type="button"
                class="btn btn-light border border-start-0 text-muted"
                @click="showPassword = !showPassword"
              >
                {{ showPassword ? '🙈' : '👁️' }}
              </button>
            </div>
          </div>

          <button
            type="submit"
            class="btn btn-primary w-100 py-2 fw-bold text-uppercase shadow-sm mb-4"
            :disabled="isLoading"
            style="letter-spacing: 0.05em; border-radius: 10px;"
          >
            <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
            {{ isLoading ? 'Đang xác thực...' : 'ĐĂNG NHẬP' }}
          </button>
        </form>

        <div class="text-center mb-4">
          <span class="small text-muted">Khach hang chua co tai khoan?</span>
          <router-link to="/register" class="small fw-bold ms-1">Dang ky tai khoan</router-link>
        </div>

        <!-- Quick Demo Accounts -->
        <div class="pt-3 border-top">
          <div class="text-center mb-2">
            <small class="text-muted fw-semibold">⚡ Đăng nhập nhanh tài khoản thử nghiệm:</small>
          </div>
          <div class="d-flex flex-wrap gap-2 justify-content-center">
            <button
              type="button"
              class="btn btn-sm btn-outline-danger px-2 py-1 small fw-semibold"
              @click="quickFill('admintest4@gmail.com', '123')"
              title="Quản trị viên"
            >
              👑 Admin
            </button>
            <button
              type="button"
              class="btn btn-sm btn-outline-success px-2 py-1 small fw-semibold"
              @click="quickFill('nhanvien1@gmail.com', '123')"
              title="Nhân viên phục vụ"
            >
              🤵 Phục Vụ
            </button>
            <button
              type="button"
              class="btn btn-sm btn-outline-warning text-dark px-2 py-1 small fw-semibold"
              @click="quickFill('daubep1@gmail.com', '123')"
              title="Khu vực Bếp"
            >
              👨‍🍳 Đầu Bếp
            </button>
            <button
              type="button"
              class="btn btn-sm btn-outline-info text-dark px-2 py-1 small fw-semibold"
              @click="quickFill('test@gmail.com', '123')"
              title="Khách hàng"
            >
              🙋 Khách Hàng
            </button>
          </div>
        </div>
      </div>
    </div>
  </main>
</template>

<script setup>
import axios from 'axios';
import { ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const email = ref('admintest4@gmail.com');
const password = ref('123');
const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref('');

function quickFill(userEmail, userPassword) {
  email.value = userEmail;
  password.value = userPassword;
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

    if (role === 'chef') {
      router.push('/chef');
    } else if (role === 'staff') {
      router.push('/staff');
    } else if (role === 'customer') {
      router.push('/customer');
    } else {
      router.push('/admin');
    }
  } catch (error) {
    errorMessage.value = error.response?.data?.message ?? error.message ?? 'Không thể kết nối đến máy chủ Backend.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
.login-wrapper {
  background: linear-gradient(135deg, #0f172a 0%, #1e1b4b 50%, #0f172a 100%);
}

.card-login {
  width: 100%;
  max-width: 440px;
  border-radius: 20px;
}

.login-header {
  background: linear-gradient(135deg, #4f46e5 0%, #6366f1 100%);
}

.brand-icon-circle {
  width: 58px;
  height: 58px;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(8px);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.8rem;
  border: 2px solid rgba(255, 255, 255, 0.3);
}
</style>

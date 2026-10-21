<template>
  <div class="d-flex min-vh-100 bg-slate-50">
    <!-- Sidebar Navigation -->
    <aside class="sidebar-wrapper d-flex flex-column text-white shadow-lg">
      <!-- Brand Logo & Title -->
      <div class="p-3 px-4 border-bottom border-secondary border-opacity-25 d-flex align-items-center gap-3">
        <div class="brand-logo-badge">🍽️</div>
        <div>
          <h5 class="fw-bold m-0 tracking-wide text-white">HƯƠNG SEN</h5>
          <span class="badge bg-warning text-dark px-2 py-0 fw-semibold" style="font-size: 0.65rem;">
            QUẢN TRỊ VIÊN
          </span>
        </div>
      </div>

      <!-- Navigation Menu Items -->
      <div class="flex-grow-1 p-3 overflow-auto">
        <div class="text-uppercase text-white-50 fw-bold px-3 mb-2" style="font-size: 0.7rem; letter-spacing: 0.08em;">
          Hệ Thống & Vận Hành
        </div>

        <ul class="nav flex-column gap-1">
          <li class="nav-item">
            <router-link
              to="/admin"
              class="nav-link-custom"
              exact-active-class="nav-active"
            >
              <span class="nav-icon">📊</span>
              <span>Tổng Quan</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/categories"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">📂</span>
              <span>Danh Mục Món</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/foods"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">🍔</span>
              <span>Thực Đơn & Món Ăn</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/tables"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">🪑</span>
              <span>Sơ Đồ Bàn Ăn</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/reservations"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">📅</span>
              <span>Đặt Bàn & Lịch Hẹn</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/users"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">👥</span>
              <span>Tài Khoản & Nhân Viên</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/payment-settings"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">💳</span>
              <span>Cấu Hình Ngân Hàng</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/inventory"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">📦</span>
              <span>Kho & Nguyên Liệu</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/daily-portions"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">🍲</span>
              <span>Điều Phối Suất Ăn</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/stock-receipts"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">🧾</span>
              <span>Phiếu Kho</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/kitchen-reconciliation"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">↩</span>
              <span>Đối Soát Bếp & Kho</span>
            </router-link>
          </li>

          <li class="nav-item">
            <router-link
              to="/admin/reviews"
              class="nav-link-custom"
              active-class="nav-active"
            >
              <span class="nav-icon">⭐</span>
              <span>Đánh Giá Khách Hàng</span>
            </router-link>
          </li>
        </ul>

        <div class="text-uppercase text-white-50 fw-bold px-3 mt-4 mb-2" style="font-size: 0.7rem; letter-spacing: 0.08em;">
          Không Gian Khác
        </div>
        <ul class="nav flex-column gap-1">
          <li class="nav-item">
            <router-link to="/staff" class="nav-link-custom text-white-50">
              <span class="nav-icon">🤵</span>
              <span>Giao Diện Phục Vụ</span>
            </router-link>
          </li>
          <li class="nav-item">
            <router-link to="/chef" class="nav-link-custom text-white-50">
              <span class="nav-icon">👨‍🍳</span>
              <span>Giao Diện Bếp</span>
            </router-link>
          </li>
          <li class="nav-item">
            <router-link to="/customer" class="nav-link-custom text-white-50">
              <span class="nav-icon">📱</span>
              <span>Menu Khách Hàng</span>
            </router-link>
          </li>
        </ul>
      </div>

      <!-- User Profile & Logout Bottom -->
      <div class="p-3 border-top border-secondary border-opacity-25 bg-black bg-opacity-20">
        <div class="d-flex align-items-center justify-content-between">
          <div class="d-flex align-items-center gap-2 overflow-hidden">
            <div class="user-avatar-circle">
              {{ userInitial }}
            </div>
            <div class="text-truncate">
              <div class="fw-bold text-white small text-truncate">{{ userName }}</div>
              <div class="text-white-50 text-truncate" style="font-size: 0.75rem;">Admin Quản trị</div>
            </div>
          </div>
          <button
            class="btn btn-sm btn-outline-danger p-1 px-2"
            @click="handleLogout"
            title="Đăng xuất khỏi hệ thống"
          >
            🚪
          </button>
        </div>
      </div>
    </aside>

    <!-- Main Workspace Content Area -->
    <div class="flex-grow-1 d-flex flex-column min-vh-100 overflow-hidden">
      <!-- Top Navbar -->
      <header class="bg-white border-bottom px-4 py-3 shadow-xs d-flex justify-content-between align-items-center">
        <div class="d-flex align-items-center gap-2">
          <span class="fs-5 fw-bold text-dark">Hệ Thống Quản Trị Nhà Hàng</span>
        </div>

        <div class="d-flex align-items-center gap-3">
          <div class="d-none d-md-flex align-items-center gap-2 text-muted small border-end pe-3">
            <span>🕒</span>
            <span>{{ liveTime }}</span>
          </div>

          <div class="d-flex align-items-center gap-2">
            <span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1 small">
              🟢 Hệ Thống Trực Tuyến
            </span>
            <AdminReservationNotifier />
            <button class="btn btn-sm btn-light border" @click="handleLogout">
              Đăng xuất
            </button>
          </div>
        </div>
      </header>

      <!-- View Router Injection Area -->
      <main class="flex-grow-1 p-4 overflow-auto">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import AdminReservationNotifier from '@/components/AdminReservationNotifier.vue';

const router = useRouter();
const userName = ref(localStorage.getItem('fullName') || 'Admin');
const userInitial = computed(() => (userName.value ? userName.value.charAt(0).toUpperCase() : 'A'));

const liveTime = ref('');
let timerId = null;

function updateTime() {
  const now = new Date();
  liveTime.value = now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) +
    ' - ' + now.toLocaleDateString('vi-VN');
}

onMounted(() => {
  updateTime();
  timerId = setInterval(updateTime, 1000);
});

onUnmounted(() => {
  if (timerId) clearInterval(timerId);
});

function handleLogout() {
  if (confirm('Bạn có chắc chắn muốn đăng xuất khỏi trang quản trị?')) {
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    localStorage.removeItem('fullName');
    router.push('/login');
  }
}
</script>

<style scoped>
.sidebar-wrapper {
  width: 260px;
  min-width: 260px;
  background-color: #0f172a;
}

.brand-logo-badge {
  width: 42px;
  height: 42px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  border: 1px solid rgba(255, 255, 255, 0.15);
}

.nav-link-custom {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  color: #cbd5e1;
  text-decoration: none;
  border-radius: 10px;
  font-size: 0.9rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

.nav-link-custom:hover {
  background-color: rgba(255, 255, 255, 0.08);
  color: #ffffff;
}

.nav-active {
  background: linear-gradient(90deg, #4f46e5 0%, #6366f1 100%) !important;
  color: #ffffff !important;
  font-weight: 600 !important;
  box-shadow: 0 4px 12px rgba(79, 70, 229, 0.35);
}

.nav-icon {
  font-size: 1.15rem;
}

.user-avatar-circle {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #4f46e5;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.85rem;
  border: 2px solid rgba(255, 255, 255, 0.2);
}
</style>

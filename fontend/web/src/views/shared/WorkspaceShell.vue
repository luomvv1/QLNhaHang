<template>
  <div class="min-vh-100 bg-slate-50 d-flex flex-column">
    <!-- Top Bar for Actor Workspaces -->
    <header class="bg-white border-bottom px-4 py-3 shadow-xs d-flex justify-content-between align-items-center">
      <div class="d-flex align-items-center gap-3">
        <div class="brand-badge-circle">🍽️</div>
        <div>
          <div class="d-flex align-items-center gap-2">
            <h5 class="fw-bold m-0 text-dark">HƯƠNG SEN RESTAURANT</h5>
            <span class="badge" :class="actorBadgeClass">{{ actorLabel }}</span>
          </div>
          <small class="text-muted">{{ title }}</small>
        </div>
      </div>

      <div class="d-flex align-items-center gap-3">
        <div class="d-flex align-items-center gap-2">
          <div class="user-avatar-small">{{ userInitial }}</div>
          <div class="d-none d-sm-block text-end">
            <div class="fw-bold small text-dark">{{ userName }}</div>
            <div class="text-muted" style="font-size: 0.72rem;">{{ actorLabel }}</div>
          </div>
        </div>
        <router-link v-if="isAdmin" to="/admin" class="btn btn-sm btn-outline-primary fw-semibold">
          ⚙️ Về Trang Quản Trị
        </router-link>
        <button class="btn btn-sm btn-outline-danger fw-semibold" @click="logout">
          🚪 Đăng Xuất
        </button>
      </div>
    </header>

    <!-- Workspace Main Content -->
    <main class="flex-grow-1 container-fluid py-4 px-md-5">
      <!-- Title Banner -->
      <div class="mb-4">
        <h3 class="fw-bold text-dark mb-1">{{ title }}</h3>
        <p class="text-muted mb-0">{{ subtitle }}</p>
      </div>

      <!-- Live Notification Banner if any -->
      <div v-if="message" class="alert alert-info py-2 small shadow-sm mb-4 d-flex justify-content-between align-items-center">
        <div class="d-flex align-items-center gap-2">
          <span>ℹ️</span>
          <span>{{ message }}</span>
        </div>
        <button class="btn-close" @click="message = ''"></button>
      </div>

      <!-- Metrics Row -->
      <div class="row g-3 mb-4">
        <div v-for="metric in metrics" :key="metric.label" class="col-sm-6 col-xl-3">
          <div class="card-modern p-3 h-100">
            <div class="text-muted small fw-semibold text-uppercase mb-1">{{ metric.label }}</div>
            <div class="display-6 fw-bold text-primary">{{ metric.value }}</div>
          </div>
        </div>
      </div>

      <!-- Actions Grid -->
      <h5 class="fw-bold text-dark mb-3">🛠️ Nghiệp Vụ & Chức Năng</h5>
      <div class="row g-3">
        <div v-for="action in actions" :key="action.title" class="col-md-6 col-xl-4">
          <div class="card-modern p-4 h-100 d-flex flex-column justify-content-between">
            <div>
              <div class="fs-2 mb-3">{{ action.icon }}</div>
              <h5 class="fw-bold text-dark mb-2">{{ action.title }}</h5>
              <p class="text-muted small mb-4">{{ action.description }}</p>
            </div>
            <button
              class="btn btn-primary fw-bold w-100 py-2 shadow-xs"
              @click="message = `${action.title} đang ở chế độ xem mẫu. Hệ thống đang sẵn sàng cho bước tích hợp tiếp theo.`"
            >
              Mở Tính Năng
            </button>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';

const props = defineProps({
  actorLabel: String,
  title: String,
  subtitle: String,
  metrics: Array,
  actions: Array
});

const router = useRouter();
const message = ref('');
const userName = ref(localStorage.getItem('fullName') || 'Nhân viên');
const userInitial = computed(() => (userName.value ? userName.value.charAt(0).toUpperCase() : 'U'));
const userRole = ref((localStorage.getItem('role') || '').toLowerCase());
const isAdmin = computed(() => userRole.value === 'admin' || userRole.value === 'manager');

const actorBadgeClass = computed(() => {
  if (props.actorLabel.includes('BẾP')) return 'bg-warning text-dark';
  if (props.actorLabel.includes('NHÂN VIÊN')) return 'bg-success text-white';
  if (props.actorLabel.includes('KHÁCH')) return 'bg-info text-dark';
  return 'bg-primary text-white';
});

function logout() {
  localStorage.clear();
  router.push('/login');
}
</script>

<style scoped>
.brand-badge-circle {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  background: #f1f5f9;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.4rem;
}

.user-avatar-small {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  background: #4f46e5;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.85rem;
}
</style>

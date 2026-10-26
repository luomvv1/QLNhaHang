<template>
  <div>
    <!-- Welcome Header Banner -->
    <div class="card-modern p-4 mb-4 text-white overflow-hidden position-relative" style="background: linear-gradient(135deg, #1e1b4b 0%, #312e81 60%, #4338ca 100%);">
      <div class="d-flex justify-content-between align-items-center position-relative" style="z-index: 2;">
        <div>
          <span class="badge bg-warning text-dark px-3 py-1 fw-bold mb-2">HỆ THỐNG QUẢN LÝ NHÀ HÀNG POS</span>
          <h2 class="fw-bold mb-1">Xin chào, {{ adminName }}! 👋</h2>
          <p class="text-white-50 mb-0">Theo dõi thông tin tổng quan, trạng thái phục vụ và các chức năng quản trị nhanh chóng.</p>
        </div>
        <div class="d-none d-lg-block text-end">
          <div class="badge bg-white bg-opacity-10 text-white p-3 rounded-3 border border-white border-opacity-20">
            <small class="d-block text-white-50 mb-1">Thời gian hệ thống</small>
            <strong class="fs-6">{{ currentDate }}</strong>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Statistics 4 Cards -->
    <div class="row g-3 mb-4">
      <!-- Danh mục -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-3">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Danh Mục Món</span>
              <h3 class="fw-bold text-dark mt-1 mb-0">{{ stats.categories }}</h3>
            </div>
            <div class="stat-icon-badge bg-info-subtle text-info">📂</div>
          </div>
          <div class="d-flex justify-content-between align-items-center pt-2 border-top">
            <span class="text-muted small">Phân loại món ăn</span>
            <router-link to="/admin/categories" class="small fw-bold text-info text-decoration-none">
              Chi tiết &rarr;
            </router-link>
          </div>
        </div>
      </div>

      <!-- Món ăn -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-3">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Thực Đơn Món</span>
              <h3 class="fw-bold text-dark mt-1 mb-0">{{ stats.foods }}</h3>
            </div>
            <div class="stat-icon-badge bg-warning-subtle text-warning">🍔</div>
          </div>
          <div class="d-flex justify-content-between align-items-center pt-2 border-top">
            <span class="small text-muted">{{ activeFoodsCount }} đang kinh doanh</span>
            <router-link to="/admin/foods" class="small fw-bold text-warning text-decoration-none">
              Quản lý &rarr;
            </router-link>
          </div>
        </div>
      </div>

      <!-- Bàn ăn -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-3">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Bàn Ăn & Phòng</span>
              <h3 class="fw-bold text-dark mt-1 mb-0">{{ stats.tables }}</h3>
            </div>
            <div class="stat-icon-badge bg-success-subtle text-success">🪑</div>
          </div>
          <div class="d-flex justify-content-between align-items-center pt-2 border-top">
            <span class="small text-muted">{{ emptyTablesCount }} bàn đang trống</span>
            <router-link to="/admin/tables" class="small fw-bold text-success text-decoration-none">
              Sơ đồ bàn &rarr;
            </router-link>
          </div>
        </div>
      </div>

      <!-- Nhân sự -->
      <div v-if="isAdmin" class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-3">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Nhân Viên & TK</span>
              <h3 class="fw-bold text-dark mt-1 mb-0">{{ stats.users }}</h3>
            </div>
            <div class="stat-icon-badge bg-primary-subtle text-primary">👥</div>
          </div>
          <div class="d-flex justify-content-between align-items-center pt-2 border-top">
            <span class="small text-muted">{{ activeUsersCount }} đang hoạt động</span>
            <router-link to="/admin/users" class="small fw-bold text-primary text-decoration-none">
              Cấp tài khoản &rarr;
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Quick Operations & Operational Summary -->
    <div class="row g-4">
      <!-- Quick Action Cards -->
      <div class="col-lg-8">
        <div class="card-modern p-4 h-100">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="fw-bold m-0 text-dark">⚡ Phím Tắt Thao Tác Nghiệp Vụ</h5>
            <span class="text-muted small">Truy cập tức thì các tính năng</span>
          </div>

          <div class="row g-3">
            <div v-if="isAdmin" class="col-md-6">
              <router-link to="/admin/users" class="action-shortcut-card p-3 rounded-3 d-flex align-items-center gap-3 text-decoration-none">
                <div class="shortcut-icon bg-primary text-white">➕</div>
                <div>
                  <strong class="d-block text-dark">Cấp Tài Khoản Mới</strong>
                  <small class="text-muted">Tạo thông tin đăng nhập cho Nhân viên, Bếp</small>
                </div>
              </router-link>
            </div>

            <div class="col-md-6">
              <router-link to="/admin/foods" class="action-shortcut-card p-3 rounded-3 d-flex align-items-center gap-3 text-decoration-none">
                <div class="shortcut-icon bg-warning text-dark">🍲</div>
                <div>
                  <strong class="d-block text-dark">Thêm Món Ăn Mới</strong>
                  <small class="text-muted">Tạo món ăn, định giá và cấu hình biến thể</small>
                </div>
              </router-link>
            </div>

            <div class="col-md-6">
              <router-link to="/admin/tables" class="action-shortcut-card p-3 rounded-3 d-flex align-items-center gap-3 text-decoration-none">
                <div class="shortcut-icon bg-success text-white">🪑</div>
                <div>
                  <strong class="d-block text-dark">Sơ Đồ Bàn Ăn</strong>
                  <small class="text-muted">Xem bàn trống, phục vụ, dọn dẹp theo khu vực</small>
                </div>
              </router-link>
            </div>

            <div class="col-md-6">
              <router-link to="/admin/categories" class="action-shortcut-card p-3 rounded-3 d-flex align-items-center gap-3 text-decoration-none">
                <div class="shortcut-icon bg-info text-white">🏷️</div>
                <div>
                  <strong class="d-block text-dark">Quản Lý Danh Mục</strong>
                  <small class="text-muted">Tạo và phân loại danh mục khai vị, lẩu, nướng</small>
                </div>
              </router-link>
            </div>

            <div class="col-md-6">
              <router-link to="/admin/inventory" class="action-shortcut-card p-3 rounded-3 d-flex align-items-center gap-3 text-decoration-none">
                <div class="shortcut-icon bg-success text-white">📦</div>
                <div>
                  <strong class="d-block text-dark">Kho & Định Lượng</strong>
                  <small class="text-muted">Nhập xuất kho, định lượng món & cảnh báo tồn</small>
                </div>
              </router-link>
            </div>

            <div class="col-md-6">
              <router-link to="/admin/reviews" class="action-shortcut-card p-3 rounded-3 d-flex align-items-center gap-3 text-decoration-none">
                <div class="shortcut-icon bg-secondary text-white">⭐</div>
                <div>
                  <strong class="d-block text-dark">Đánh Giá Khách Hàng</strong>
                  <small class="text-muted">Xem phản hồi thực khách, số sao & trả lời</small>
                </div>
              </router-link>
            </div>
          </div>
        </div>
      </div>

      <!-- Quick System Overview -->
      <div class="col-lg-4 d-flex flex-column gap-3">
        <!-- Tables status -->
        <div class="card-modern p-4">
          <h5 class="fw-bold mb-3 text-dark">📋 Tình Trạng Bàn Ăn</h5>
          <div class="d-flex flex-column gap-3">
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small fw-semibold text-muted">🟢 Bàn đang trống</span>
              <strong class="text-success">{{ emptyTablesCount }} bàn</strong>
            </div>
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small fw-semibold text-muted">🟡 Đang phục vụ</span>
              <strong class="text-warning">{{ servingTablesCount }} bàn</strong>
            </div>
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small fw-semibold text-muted">🔵 Đã đặt trước</span>
              <strong class="text-primary">{{ reservedTablesCount }} bàn</strong>
            </div>
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small fw-semibold text-muted">⚪ Đang dọn dẹp</span>
              <strong class="text-secondary">{{ cleaningTablesCount }} bàn</strong>
            </div>
          </div>

          <div class="mt-3 pt-2 border-top text-center">
            <router-link to="/admin/tables" class="btn btn-sm btn-outline-primary w-100 fw-bold">
              Xem Bản Đồ Bàn POS
            </router-link>
          </div>
        </div>

        <!-- Inventory quick glance -->
        <div class="card-modern p-4">
          <div class="d-flex justify-content-between align-items-center mb-3">
            <h5 class="fw-bold m-0 text-dark">📦 Tình Trạng Kho Hàng</h5>
            <span v-if="inventorySummary.lowStockCount > 0" class="badge bg-danger">
              ⚠️ {{ inventorySummary.lowStockCount }} cần nhập
            </span>
            <span v-else class="badge bg-success">🟢 Tồn kho an toàn</span>
          </div>

          <div class="d-flex flex-column gap-2 mb-3">
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small text-muted">Tổng số mặt hàng:</span>
              <strong>{{ inventorySummary.totalItems }} nguyên liệu</strong>
            </div>
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small text-muted">Giá trị lô còn dùng được:</span>
              <strong class="text-success">{{ (inventorySummary.totalValue || 0).toLocaleString('vi-VN') }} đ</strong>
            </div>
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small text-muted">Lô quá hạn còn tồn:</span>
              <strong :class="inventorySummary.expiredBatchesCount ? 'text-danger' : 'text-success'">{{ inventorySummary.expiredBatchesCount || 0 }} lô</strong>
            </div>
            <div class="d-flex justify-content-between align-items-center p-2 rounded bg-light">
              <span class="small text-muted">Phiếu chờ duyệt:</span>
              <strong :class="inventorySummary.pendingReceiptCount ? 'text-warning' : 'text-success'">{{ inventorySummary.pendingReceiptCount || 0 }} phiếu</strong>
            </div>
          </div>

          <router-link to="/admin/inventory" class="btn btn-sm btn-outline-success w-100 fw-bold">
            Quản Lý Kho Chi Tiết &rarr;
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import axios from 'axios';

const adminName = ref(localStorage.getItem('fullName') || 'Admin');
const isAdmin = localStorage.getItem('role') === 'admin';
const currentDate = new Date().toLocaleDateString('vi-VN', {
  weekday: 'long',
  year: 'numeric',
  month: 'long',
  day: 'numeric'
});

const stats = ref({
  categories: 0,
  foods: 0,
  tables: 0,
  users: 0
});

const activeFoodsCount = ref(0);
const emptyTablesCount = ref(0);
const servingTablesCount = ref(0);
const reservedTablesCount = ref(0);
const cleaningTablesCount = ref(0);
const activeUsersCount = ref(0);
const inventorySummary = ref({
  totalItems: 0,
  lowStockCount: 0,
  totalValue: 0
});

onMounted(async () => {
  try {
    const [catRes, foodRes, tblRes, usrRes, invRes] = await Promise.allSettled([
      axios.get('http://localhost:5079/api/admin/category/list'),
      axios.get('http://localhost:5079/api/admin/food/list'),
      axios.get('http://localhost:5079/api/admin/table/list'),
      isAdmin ? axios.get('http://localhost:5079/api/admin/user/list') : Promise.resolve({ data: { data: [] } }),
      axios.get('http://localhost:5079/api/admin/inventory/summary')
    ]);

    if (catRes.status === 'fulfilled') {
      stats.value.categories = catRes.value.data?.data?.length ?? 0;
    }
    if (foodRes.status === 'fulfilled') {
      const foodList = foodRes.value.data?.data ?? [];
      stats.value.foods = foodList.length;
      activeFoodsCount.value = foodList.filter(f => f.isActive).length;
    }
    if (tblRes.status === 'fulfilled') {
      const tableList = tblRes.value.data?.data ?? [];
      stats.value.tables = tableList.length;
      emptyTablesCount.value = tableList.filter(t => t.tableStatus === 'empty').length;
      servingTablesCount.value = tableList.filter(t => t.tableStatus === 'serving').length;
      reservedTablesCount.value = tableList.filter(t => t.tableStatus === 'reserved').length;
      cleaningTablesCount.value = tableList.filter(t => t.tableStatus === 'cleaning').length;
    }
    if (usrRes.status === 'fulfilled') {
      const userList = usrRes.value.data?.data ?? [];
      stats.value.users = userList.length;
      activeUsersCount.value = userList.filter(u => u.isActive).length;
    }
    if (invRes.status === 'fulfilled' && invRes.value.data?.success) {
      inventorySummary.value = invRes.value.data.data;
    }
  } catch (err) {
    console.error('Error loading dashboard stats:', err);
  }
});
</script>

<style scoped>
.stat-icon-badge {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
}

.action-shortcut-card {
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  transition: all 0.2s ease;
}

.action-shortcut-card:hover {
  transform: translateY(-2px);
  border-color: #cbd5e1;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.08);
}

.shortcut-icon {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
  flex-shrink: 0;
}
</style>

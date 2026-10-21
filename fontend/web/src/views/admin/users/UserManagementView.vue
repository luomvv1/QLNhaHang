<template>
  <section>
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
      <div>
        <h3 class="fw-bold mb-1 text-primary">👥 Quản Lý Tài Khoản & Nhân Viên</h3>
        <p class="text-muted mb-0">Quản trị danh sách nhân sự nhà hàng, phân quyền vai trò và cấp tài khoản đăng nhập.</p>
      </div>
      <button class="btn btn-primary fw-bold px-3 py-2 shadow-sm" @click="openCreateModal">
        + Thêm Nhân Viên Mới
      </button>
    </div>

    <!-- Thanh tìm kiếm và bộ lọc vai trò -->
    <div class="card card-body shadow-sm mb-4 border-0">
      <div class="row g-3 align-items-center">
        <!-- Tìm kiếm -->
        <div class="col-md-5">
          <input
            v-model="searchQuery"
            class="form-control"
            placeholder="🔍 Tìm nhân viên theo họ tên, email hoặc số điện thoại..."
          />
        </div>

        <!-- Lọc theo Vai trò -->
        <div class="col-md-4">
          <div class="input-group">
            <span class="input-group-text bg-light text-muted small">🛡️ Vai trò</span>
            <select v-model="selectedRole" class="form-select">
              <option value="">Tất cả vai trò</option>
              <option v-for="r in roles" :key="r.roleId" :value="r.roleId">
                {{ r.roleName }} ({{ r.roleCode }})
              </option>
            </select>
          </div>
        </div>

        <!-- Lọc theo Trạng thái -->
        <div class="col-md-3">
          <select v-model="selectedStatus" class="form-select">
            <option value="">Tất cả trạng thái</option>
            <option value="active">🟢 Đang làm việc</option>
            <option value="locked">🔴 Đã tạm khóa</option>
          </select>
        </div>
      </div>

      <div class="d-flex justify-content-between align-items-center mt-3 pt-3 border-top text-muted small">
        <span>Tổng số nhân sự: <strong class="text-primary">{{ filteredUsers.length }}</strong> tài khoản</span>
        <span>
          💡 Quản lý: <span class="badge bg-danger">Quản lý</span> |
          Phục vụ: <span class="badge bg-success">Nhân viên</span> |
          Bếp: <span class="badge bg-warning text-dark">Đầu bếp</span>
        </span>
      </div>
    </div>

    <div v-if="errorMessage" class="alert alert-danger py-2 small shadow-sm">{{ errorMessage }}</div>
    <div v-if="successMessage" class="alert alert-success py-2 small shadow-sm">{{ successMessage }}</div>

    <!-- Bảng danh sách nhân viên -->
    <div class="card shadow-sm border-0">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-dark">
            <tr>
              <th style="width: 70px;" class="text-center">ID</th>
              <th>Nhân Viên / Họ Tên</th>
              <th>Email Đăng Nhập</th>
              <th>Số Điện Thoại</th>
              <th class="text-center">Vai Trò Hệ Thống</th>
              <th class="text-center" style="width: 140px;">Trạng Thái</th>
              <th class="text-center" style="width: 180px;">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="u in filteredUsers"
              :key="u.userId"
              :class="{'table-secondary opacity-75': !u.isActive}"
            >
              <td class="text-center text-muted fw-bold">#{{ u.userId }}</td>
              <td>
                <div class="d-flex align-items-center gap-2">
                  <div class="avatar-circle" :class="avatarColorClass(u.roleCode)">
                    {{ getInitials(u.fullName) }}
                  </div>
                  <div>
                    <span class="fw-bold text-dark fs-6">{{ u.fullName }}</span>
                    <div class="text-muted" style="font-size: 0.75rem;">
                      Tham gia: {{ formatDate(u.createdAt) }}
                    </div>
                  </div>
                </div>
              </td>
              <td class="fw-semibold text-primary">{{ u.email }}</td>
              <td class="text-muted">{{ u.phone || 'Chưa cập nhật' }}</td>
              <td class="text-center">
                <span class="badge px-2 py-1" :class="roleBadgeClass(u.roleCode)">
                  {{ u.roleName }}
                </span>
              </td>
              <td class="text-center">
                <span v-if="u.isActive" class="badge bg-success-subtle text-success border px-2 py-1">
                  ● Đang hoạt động
                </span>
                <span v-else class="badge bg-danger-subtle text-danger border px-2 py-1">
                  ✕ Đang tạm khóa
                </span>
              </td>
              <td class="text-center">
                <div class="d-flex justify-content-center gap-1">
                  <button
                    class="btn btn-sm btn-outline-primary px-2"
                    @click="openEditModal(u)"
                    title="Chỉnh sửa thông tin hoặc đổi mật khẩu"
                  >
                    ✏️ Sửa
                  </button>
                  <button
                    v-if="u.isActive"
                    class="btn btn-sm btn-outline-danger px-2"
                    @click="toggleStatus(u)"
                    title="Khóa tài khoản này"
                  >
                    🔒 Khóa
                  </button>
                  <button
                    v-else
                    class="btn btn-sm btn-outline-success px-2 fw-semibold"
                    @click="toggleStatus(u)"
                    title="Mở khóa tài khoản"
                  >
                    🔓 Mở
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!filteredUsers.length">
              <td colspan="7" class="text-center text-muted py-5">
                Không tìm thấy nhân viên nào phù hợp với bộ lọc.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal Thêm / Chỉnh Sửa Nhân Viên -->
    <div v-if="showModal" class="modal-backdrop-custom">
      <form class="card p-4 dialog shadow-lg border-0" @submit.prevent="saveUser">
        <div class="d-flex justify-content-between align-items-center mb-3 border-bottom pb-2">
          <h4 class="fw-bold m-0 text-primary">
            {{ isEditing ? '✏️ Cập Nhật Thông Tin Nhân Viên' : '➕ Cấp Tài Khoản Nhân Viên Mới' }}
          </h4>
          <button type="button" class="btn-close" @click="showModal = false"></button>
        </div>

        <div class="row g-3 mb-3">
          <div class="col-md-6">
            <label class="form-label fw-semibold small">
              Họ và Tên <span class="text-danger">*</span>
            </label>
            <input
              v-model="form.fullName"
              class="form-control"
              placeholder="VD: Trần Văn Hùng"
              required
            />
          </div>

          <div class="col-md-6">
            <label class="form-label fw-semibold small">
              Vai trò / Chức vụ <span class="text-danger">*</span>
            </label>
            <select v-model="form.roleId" class="form-select" required>
              <option value="" disabled>-- Chọn vai trò --</option>
              <option v-for="r in roles" :key="r.roleId" :value="r.roleId">
                {{ r.roleName }} ({{ r.roleCode }})
              </option>
            </select>
          </div>
        </div>

        <div class="row g-3 mb-3">
          <div class="col-md-6">
            <label class="form-label fw-semibold small">
              Email đăng nhập <span class="text-danger">*</span>
            </label>
            <input
              v-model="form.email"
              type="email"
              class="form-control"
              placeholder="VD: nhanvien@gmail.com"
              required
            />
          </div>

          <div class="col-md-6">
            <label class="form-label fw-semibold small">Số điện thoại</label>
            <input
              v-model="form.phone"
              class="form-control"
              placeholder="VD: 0912345678"
            />
          </div>
        </div>

        <!-- Mật khẩu -->
        <div class="mb-3">
          <label class="form-label fw-semibold small">
            {{ isEditing ? 'Mật khẩu mới (Để trống nếu giữ nguyên)' : 'Mật khẩu khởi tạo *' }}
          </label>
          <input
            v-model="form.password"
            type="password"
            class="form-control"
            :placeholder="isEditing ? 'Nhập mật khẩu mới nếu muốn đổi...' : 'Nhập mật khẩu (VD: 123)...'"
            :required="!isEditing"
          />
        </div>

        <div v-if="isEditing" class="mb-3">
          <label class="form-label fw-semibold small">Trạng thái tài khoản</label>
          <select v-model="form.isActive" class="form-select">
            <option :value="true">🟢 Đang hoạt động bình thường</option>
            <option :value="false">🔴 Tạm khóa tài khoản</option>
          </select>
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3">
          <button type="button" class="btn btn-light" @click="showModal = false">Đóng</button>
          <button type="submit" class="btn btn-primary fw-bold px-4">
            {{ isEditing ? 'Cập Nhật' : 'Cấp Tài Khoản' }}
          </button>
        </div>
      </form>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import axios from 'axios';

const api = 'http://localhost:5079/api/admin/user';
const users = ref([]);
const roles = ref([]);
const searchQuery = ref('');
const selectedRole = ref('');
const selectedStatus = ref('');

const showModal = ref(false);
const isEditing = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const form = ref({
  userId: 0,
  fullName: '',
  email: '',
  phone: '',
  roleId: '',
  password: '',
  isActive: true
});

async function loadData() {
  errorMessage.value = '';
  try {
    const [rolesRes, usersRes] = await Promise.all([
      axios.get(`${api}/roles`),
      axios.get(`${api}/list`)
    ]);

    roles.value = rolesRes.data?.data ?? [];
    users.value = usersRes.data?.data ?? [];
  } catch (e) {
    errorMessage.value = 'Lỗi kết nối máy chủ khi tải danh sách người dùng.';
  }
}

const filteredUsers = computed(() => {
  return users.value.filter(u => {
    // Lọc theo vai trò
    if (selectedRole.value && u.roleId !== Number(selectedRole.value)) {
      return false;
    }
    // Lọc theo trạng thái
    if (selectedStatus.value === 'active' && !u.isActive) return false;
    if (selectedStatus.value === 'locked' && u.isActive) return false;

    // Lọc theo từ khóa tìm kiếm
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase();
      const matchName = u.fullName?.toLowerCase().includes(q);
      const matchEmail = u.email?.toLowerCase().includes(q);
      const matchPhone = u.phone?.toLowerCase().includes(q);
      if (!matchName && !matchEmail && !matchPhone) return false;
    }
    return true;
  });
});

function openCreateModal() {
  isEditing.value = false;
  form.value = {
    userId: 0,
    fullName: '',
    email: '',
    phone: '',
    roleId: roles.value.find(r => r.roleCode === 'staff')?.roleId ?? (roles.value[0]?.roleId ?? ''),
    password: '',
    isActive: true
  };
  showModal.value = true;
}

function openEditModal(user) {
  isEditing.value = true;
  form.value = {
    userId: user.userId,
    fullName: user.fullName,
    email: user.email,
    phone: user.phone || '',
    roleId: user.roleId,
    password: '', // Để trống nếu không muốn đổi
    isActive: user.isActive
  };
  showModal.value = true;
}

async function saveUser() {
  errorMessage.value = '';
  successMessage.value = '';

  try {
    if (isEditing.value) {
      await axios.put(`${api}/update`, {
        userId: form.value.userId,
        fullName: form.value.fullName,
        email: form.value.email,
        phone: form.value.phone,
        roleId: form.value.roleId,
        newPassword: form.value.password || null,
        isActive: form.value.isActive
      });
      successMessage.value = `Đã cập nhật thông tin tài khoản '${form.value.fullName}' thành công!`;
    } else {
      await axios.post(`${api}/create`, {
        fullName: form.value.fullName,
        email: form.value.email,
        phone: form.value.phone,
        roleId: form.value.roleId,
        password: form.value.password
      });
      successMessage.value = `Đã cấp tài khoản thành công cho '${form.value.fullName}'!`;
    }

    showModal.value = false;
    await loadData();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi lưu tài khoản.';
  }
}

async function toggleStatus(user) {
  const actionText = user.isActive ? 'khóa' : 'mở khóa';
  if (!confirm(`Bạn có chắc muốn ${actionText} tài khoản của '${user.fullName}' không?`)) return;

  errorMessage.value = '';
  successMessage.value = '';

  try {
    const res = await axios.patch(`${api}/toggle-status`, null, {
      params: { userId: user.userId }
    });
    successMessage.value = res.data?.message ?? `Thao tác thành công!`;
    await loadData();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi cập nhật trạng thái tài khoản.';
  }
}

function roleBadgeClass(roleCode) {
  const map = {
    manager: 'bg-danger',
    staff: 'bg-success',
    chef: 'bg-warning text-dark',
    customer: 'bg-info text-dark'
  };
  return map[roleCode] ?? 'bg-secondary';
}

function avatarColorClass(roleCode) {
  const map = {
    manager: 'avatar-manager',
    staff: 'avatar-staff',
    chef: 'avatar-chef',
    customer: 'avatar-customer'
  };
  return map[roleCode] ?? 'avatar-default';
}

function getInitials(name) {
  if (!name) return 'U';
  const parts = name.trim().split(' ');
  return parts[parts.length - 1].charAt(0).toUpperCase();
}

function formatDate(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('vi-VN');
}

onMounted(loadData);
</script>

<style scoped>
.avatar-circle {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  color: white;
  font-size: 0.9rem;
}
.avatar-manager { background: #dc3545; }
.avatar-staff { background: #198754; }
.avatar-chef { background: #fd7e14; }
.avatar-customer { background: #0dcaf0; color: #212529; }
.avatar-default { background: #6c757d; }

.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  display: grid;
  place-items: center;
  z-index: 1050;
  backdrop-filter: blur(2px);
}
.dialog {
  width: min(600px, calc(100vw - 2rem));
  border-radius: 16px;
}
</style>
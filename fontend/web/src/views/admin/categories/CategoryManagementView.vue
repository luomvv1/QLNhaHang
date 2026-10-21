<template>
  <section>
    <!-- Header chức năng -->
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
      <div>
        <h3 class="fw-bold mb-1 text-dark">📂 Quản Lý Danh Mục Món Ăn</h3>
        <p class="text-muted mb-0">Phân loại thực đơn nhà hàng theo các nhóm món để khách hàng và phục vụ dễ dàng lựa chọn.</p>
      </div>
      <button class="btn btn-primary fw-bold px-3 py-2 shadow-sm d-flex align-items-center gap-2" @click="openCreateModal">
        <span>➕</span>
        <span>Thêm Danh Mục Mới</span>
      </button>
    </div>

    <!-- Thanh tìm kiếm & Thống kê -->
    <div class="card-modern p-3 mb-4">
      <div class="row g-2 align-items-center">
        <div class="col-md-6">
          <div class="input-group">
            <span class="input-group-text bg-light border-end-0 text-muted">🔍</span>
            <input
              v-model="searchQuery"
              class="form-control border-start-0"
              placeholder="Tìm kiếm danh mục theo tên hoặc mô tả..."
            />
          </div>
        </div>
        <div class="col-md-6 text-md-end text-muted small">
          Tổng cộng: <strong class="text-primary fs-6">{{ filteredCategories.length }}</strong> nhóm thực đơn
        </div>
      </div>
    </div>

    <!-- Thông báo lỗi / thành công -->
    <div v-if="errorMessage" class="alert alert-danger py-2 small shadow-sm d-flex align-items-center gap-2">
      <span>⚠️</span>
      <span>{{ errorMessage }}</span>
    </div>
    <div v-if="successMessage" class="alert alert-success py-2 small shadow-sm d-flex align-items-center gap-2">
      <span>✅</span>
      <span>{{ successMessage }}</span>
    </div>

    <!-- Bảng danh sách danh mục -->
    <div class="card-modern overflow-hidden">
      <div class="table-responsive">
        <table class="table table-modern align-middle mb-0">
          <thead>
            <tr>
              <th style="width: 80px;" class="text-center">Mã ID</th>
              <th>Tên Danh Mục Món</th>
              <th>Mô Tả Chi Tiết</th>
              <th class="text-center" style="width: 180px;">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="category in filteredCategories" :key="category.categoryId">
              <td class="text-center text-muted fw-bold">#{{ category.categoryId }}</td>
              <td>
                <span class="fw-bold text-dark fs-6">{{ category.categoryName }}</span>
              </td>
              <td>
                <span class="text-muted">{{ category.description || 'Chưa có thông tin mô tả.' }}</span>
              </td>
              <td class="text-center">
                <div class="d-flex justify-content-center gap-2">
                  <button
                    class="btn btn-sm btn-outline-primary px-2"
                    @click="openEditModal(category)"
                    title="Chỉnh sửa danh mục"
                  >
                    ✏️ Sửa
                  </button>
                  <button
                    class="btn btn-sm btn-outline-danger px-2"
                    @click="removeCategory(category.categoryId)"
                    title="Xóa danh mục"
                  >
                    🗑️ Xóa
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!filteredCategories.length">
              <td colspan="4" class="text-center text-muted py-5">
                <div class="fs-1 mb-2">📂</div>
                <div>Không tìm thấy danh mục món ăn nào phù hợp với từ khóa.</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal Thêm / Chỉnh Sửa Danh Mục -->
    <div v-if="showModal" class="modal-backdrop-custom">
      <form class="modal-dialog-custom p-4" @submit.prevent="saveCategory">
        <div class="d-flex justify-content-between align-items-center mb-3 border-bottom pb-2">
          <h5 class="fw-bold m-0 text-dark">
            {{ isEditing ? '✏️ Cập Nhật Danh Mục' : '➕ Thêm Danh Mục Mới' }}
          </h5>
          <button type="button" class="btn-close" @click="showModal = false"></button>
        </div>

        <div class="mb-3">
          <label class="form-label fw-semibold small text-muted">
            TÊN DANH MỤC <span class="text-danger">*</span>
          </label>
          <input
            v-model="form.categoryName"
            class="form-control"
            placeholder="VD: Món Khai Vị, Lẩu Đặc Biệt, Đồ Uống..."
            required
          />
        </div>

        <div class="mb-4">
          <label class="form-label fw-semibold small text-muted">MÔ TẢ CHI TIẾT</label>
          <textarea
            v-model="form.description"
            class="form-control"
            rows="3"
            placeholder="Ghi chú tóm tắt về nhóm món ăn này..."
          ></textarea>
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3">
          <button type="button" class="btn btn-light" @click="showModal = false">Hủy Bỏ</button>
          <button type="submit" class="btn btn-primary fw-bold px-4">
            {{ isEditing ? 'Lưu Thay Đổi' : 'Tạo Danh Mục' }}
          </button>
        </div>
      </form>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import axios from 'axios';

const api = 'http://localhost:5079/api/admin/category';
const categories = ref([]);
const searchQuery = ref('');
const showModal = ref(false);
const isEditing = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const form = ref({
  categoryId: 0,
  categoryName: '',
  description: ''
});

async function loadCategories() {
  errorMessage.value = '';
  try {
    const res = await axios.get(`${api}/list`);
    categories.value = res.data?.data ?? [];
  } catch (e) {
    errorMessage.value = 'Không thể tải danh sách danh mục. Hãy kiểm tra Backend.';
  }
}

const filteredCategories = computed(() =>
  categories.value.filter(c =>
    `${c.categoryName} ${c.description ?? ''}`
      .toLowerCase()
      .includes(searchQuery.value.toLowerCase())
  )
);

function openCreateModal() {
  isEditing.value = false;
  form.value = { categoryId: 0, categoryName: '', description: '' };
  showModal.value = true;
}

function openEditModal(category) {
  isEditing.value = true;
  form.value = {
    categoryId: category.categoryId,
    categoryName: category.categoryName,
    description: category.description ?? ''
  };
  showModal.value = true;
}

async function saveCategory() {
  errorMessage.value = '';
  successMessage.value = '';

  try {
    if (isEditing.value) {
      await axios.put(`${api}/update`, null, {
        params: {
          categoryId: form.value.categoryId,
          categoryName: form.value.categoryName,
          description: form.value.description
        }
      });
      successMessage.value = 'Cập nhật danh mục thành công!';
    } else {
      await axios.post(`${api}/insert`, null, {
        params: {
          categoryName: form.value.categoryName,
          description: form.value.description
        }
      });
      successMessage.value = 'Thêm danh mục món ăn mới thành công!';
    }

    showModal.value = false;
    await loadCategories();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi lưu danh mục.';
  }
}

async function removeCategory(id) {
  if (!confirm('Bạn có chắc muốn xóa danh mục này không?')) return;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.delete(`${api}/delete`, {
      params: { categoryId: id }
    });
    successMessage.value = 'Đã xóa danh mục thành công!';
    await loadCategories();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Không thể xóa danh mục.';
  }
}

onMounted(loadCategories);
</script>

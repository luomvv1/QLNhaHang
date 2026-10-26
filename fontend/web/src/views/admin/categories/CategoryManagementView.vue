<template>
  <section class="category-management-showcase">
    <!-- ============================================================== -->
    <!-- HEADER: TIÊU ĐỀ TRANG & NÚT TẠO MỚI                           -->
    <!-- ============================================================== -->
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3 mb-4">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge rounded-pill bg-primary-subtle text-primary border border-primary-subtle px-3 py-1 fw-bold fs-7">
            📂 QUẢN TRỊ THỰC ĐƠN
          </span>
          <span class="badge rounded-pill bg-light text-muted border px-2.5 py-1 small">
            {{ categories.length }} nhóm món ăn
          </span>
        </div>
        <h3 class="fw-bold mb-1 text-dark d-flex align-items-center gap-2">
          <span>📂</span> Danh Mục Món Ăn & Phân Quầy Bếp
        </h3>
        <p class="text-muted mb-0 small">
          Phân nhóm thực đơn khoa học theo từng quầy chế biến, giúp tối ưu tốc độ gọi món và điều phối bếp nhịp nhàng.
        </p>
      </div>

      <div class="d-flex align-items-center gap-2 flex-wrap">
        <router-link
          to="/admin/foods"
          class="btn btn-outline-secondary fw-semibold px-3 py-2 rounded-3 shadow-xs d-flex align-items-center gap-1.5"
        >
          <span>🍽️</span>
          <span>Xem Menu Chi Tiết</span>
        </router-link>

        <button
          type="button"
          class="btn btn-primary fw-bold px-3.5 py-2 rounded-3 shadow-sm d-flex align-items-center gap-2"
          @click="openCreateModal"
        >
          <span class="fs-6">➕</span>
          <span>Thêm Danh Mục Mới</span>
        </button>
      </div>
    </div>



    <!-- ============================================================== -->
    <!-- BỘ LỌC TÌM KIẾM & CHUYỂN ĐỔI CHẾ ĐỘ XEM                        -->
    <!-- ============================================================== -->
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4 bg-white">
      <div class="row g-3 align-items-center justify-content-between">
        <!-- Tìm kiếm -->
        <div class="col-12 col-md-5">
          <div class="position-relative">
            <span class="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted">🔍</span>
            <input
              v-model="searchQuery"
              class="form-control ps-5 rounded-3 border shadow-none"
              placeholder="Tìm theo tên nhóm món, từ khóa mô tả..."
            />
            <button
              v-if="searchQuery"
              type="button"
              class="btn btn-sm btn-link position-absolute top-50 end-0 translate-middle-y me-2 text-muted text-decoration-none p-0"
              @click="searchQuery = ''"
            >
              ✕
            </button>
          </div>
        </div>

        <!-- Bộ lọc trạng thái & Switcher Lưới / Bảng -->
        <div class="col-12 col-md-7 d-flex align-items-center justify-content-md-end gap-2 flex-wrap">
          <!-- Trạng thái lọc -->
          <div class="btn-group shadow-xs rounded-pill p-1 bg-light border">
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 fw-semibold border-0"
              :class="statusFilter === 'all' ? 'bg-white text-dark shadow-xs' : 'text-muted'"
              @click="statusFilter = 'all'"
            >
              Tất Cả ({{ categories.length }})
            </button>
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 fw-semibold border-0"
              :class="statusFilter === 'active' ? 'bg-white text-success shadow-xs' : 'text-muted'"
              @click="statusFilter = 'active'"
            >
              🟢 Đang Bán ({{ countActiveCategories }})
            </button>
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 fw-semibold border-0"
              :class="statusFilter === 'inactive' ? 'bg-white text-secondary shadow-xs' : 'text-muted'"
              @click="statusFilter = 'inactive'"
            >
              🔴 Đang Ẩn ({{ countInactiveCategories }})
            </button>
          </div>

          <!-- Chế độ xem: Grid / Table -->
          <div class="btn-group shadow-xs rounded-pill p-1 bg-light border ms-1">
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 py-1 border-0 fw-semibold"
              :class="viewMode === 'grid' ? 'bg-white text-primary shadow-xs' : 'text-muted'"
              @click="viewMode = 'grid'"
              title="Chế độ xem dạng thẻ danh mục sáng sủa"
            >
              🎴 Thẻ Danh Mục
            </button>
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 py-1 border-0 fw-semibold"
              :class="viewMode === 'table' ? 'bg-white text-primary shadow-xs' : 'text-muted'"
              @click="viewMode = 'table'"
              title="Chế độ xem dạng bảng chi tiết"
            >
              📋 Dạng Bảng
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Thông báo lỗi / thành công -->
    <div v-if="errorMessage" class="alert alert-danger py-2.5 px-3 rounded-3 shadow-xs mb-3 d-flex align-items-center justify-content-between">
      <div class="d-flex align-items-center gap-2">
        <span>⚠️</span>
        <span class="small fw-semibold">{{ errorMessage }}</span>
      </div>
      <button type="button" class="btn-close btn-close-sm" @click="errorMessage = ''"></button>
    </div>

    <div v-if="successMessage" class="alert alert-success py-2.5 px-3 rounded-3 shadow-xs mb-3 d-flex align-items-center justify-content-between">
      <div class="d-flex align-items-center gap-2">
        <span>✅</span>
        <span class="small fw-semibold">{{ successMessage }}</span>
      </div>
      <button type="button" class="btn-close btn-close-sm" @click="successMessage = ''"></button>
    </div>

    <!-- ============================================================== -->
    <!-- VIEW MODE 1: DẠNG THẺ GỌN GÀNG, TẬP TRUNG DANH MỤC (GRID CARDS)-->
    <!-- ============================================================== -->
    <div v-if="viewMode === 'grid'" class="row g-3">
      <div
        v-for="category in filteredCategories"
        :key="category.categoryId"
        class="col-12 col-md-6 col-xl-4"
      >
        <div class="card h-100 border-0 shadow-sm rounded-4 category-clean-card overflow-hidden d-flex flex-column bg-white">
          <!-- 1. HEADER CARD: ICON & TÊN DANH MỤC & MÃ -->
          <div
            class="category-card-header p-3 border-bottom d-flex align-items-center justify-content-between"
            :style="{ background: getCategoryTheme(category).gradient }"
          >
            <div class="d-flex align-items-center gap-2.5">
              <!-- Thumbnail hoặc Icon tròn đại diện cho quầy / nhóm -->
              <div
                class="category-thumb-box rounded-3 shadow-xs border bg-white overflow-hidden d-flex align-items-center justify-content-center"
                :style="{ borderColor: getCategoryTheme(category).border + ' !important' }"
              >
                <img
                  v-if="getCategoryTopImage(category) && !failedImageMap[category.categoryId]"
                  :src="getCategoryTopImage(category)"
                  :alt="category.categoryName"
                  class="w-100 h-100 object-fit-cover"
                  @error="handleImageError(category.categoryId)"
                />
                <span v-else class="fs-4">{{ getCategoryIcon(category.categoryName) }}</span>
              </div>

              <div>
                <div class="d-flex align-items-center gap-1.5 mb-1">
                  <span class="badge bg-white text-dark shadow-xs border px-2 py-0.5 rounded-pill fw-bold" style="font-size: 0.7rem;">
                    Mã #{{ category.categoryId }}
                  </span>
                  <span
                    class="badge rounded-pill px-2 py-0.5 fw-semibold"
                    :class="category.isActive !== false ? 'bg-success text-white' : 'bg-secondary text-white'"
                    style="font-size: 0.7rem;"
                  >
                    {{ category.isActive !== false ? '● Đang bán' : '○ Tạm ẩn' }}
                  </span>
                </div>
                <h5 class="fw-bold mb-0 text-dark category-header-title fs-6">
                  {{ category.categoryName }}
                </h5>
              </div>
            </div>

            <!-- Huy hiệu đếm số món -->
            <span class="badge bg-white text-dark shadow-xs border fs-7 fw-bold px-2.5 py-1.5 rounded-pill text-nowrap">
              {{ category.foodCount ?? 0 }} món
            </span>
          </div>

          <!-- 2. NỘI DUNG THÂN CARD: ĐỦ CHI TIẾT VỀ DANH MỤC -->
          <div class="p-3 d-flex flex-column flex-grow-1">
            <!-- Mô tả danh mục -->
            <p class="text-muted small mb-3 category-desc-text flex-grow-1">
              {{ category.description || 'Chưa cập nhật mô tả chi tiết cho nhóm danh mục này.' }}
            </p>

            <!-- Dòng tóm tắt món ăn & liên kết xem danh sách món -->
            <div class="p-2 px-2.5 rounded-3 bg-light border mb-3">
              <div class="d-flex align-items-center justify-content-between mb-1">
                <div class="d-flex align-items-center gap-1.5">
                  <span class="small text-muted">Thực đơn:</span>
                  <span class="badge bg-white text-primary border shadow-xs fw-bold small">
                    🍽️ {{ category.foodCount ?? 0 }} món
                  </span>
                </div>

                <router-link
                  :to="{ path: '/admin/foods', query: { category: category.categoryName } }"
                  class="small text-primary text-decoration-none fw-semibold d-flex align-items-center gap-1"
                  title="Xem danh sách các món thuộc nhóm này"
                >
                  <span>Xem chi tiết</span>
                  <span>→</span>
                </router-link>
              </div>

              <!-- Danh sách món mẫu hiển thị dạng tag nhỏ nếu có -->
              <div v-if="getCategoryFoods(category).length > 0" class="d-flex flex-wrap gap-1 mt-1.5">
                <span
                  v-for="food in getCategoryFoods(category).slice(0, 3)"
                  :key="food.foodId"
                  class="badge bg-white text-dark border shadow-xs small text-truncate"
                  style="max-width: 140px; font-weight: 500; font-size: 0.73rem;"
                  :title="food.foodName"
                >
                  • {{ food.foodName }}
                </span>
                <span v-if="(category.foodCount || 0) > 3" class="badge bg-light text-muted border small" style="font-size: 0.7rem;">
                  +{{ (category.foodCount || 0) - 3 }} món
                </span>
              </div>
              <div v-else class="text-muted small fst-italic mt-1" style="font-size: 0.75rem;">
                Nhóm này hiện chưa có món ăn nào.
              </div>
            </div>

            <!-- 3. THANH NÚT THAO TÁC GỌN GÀNG -->
            <div class="pt-2.5 border-top d-flex align-items-center justify-content-between gap-1.5">
              <div class="d-flex align-items-center gap-1.5">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-primary px-2.5 py-1 rounded-2 fw-semibold shadow-xs d-flex align-items-center gap-1"
                  @click="openEditModal(category)"
                  title="Chỉnh sửa thông tin danh mục"
                >
                  <span>✏️</span>
                  <span>Sửa</span>
                </button>

                <button
                  type="button"
                  class="btn btn-sm btn-outline-danger px-2.5 py-1 rounded-2 fw-semibold shadow-xs d-flex align-items-center gap-1"
                  @click="confirmPermanentDeleteCategory(category)"
                  title="Xóa danh mục này"
                >
                  <span>🗑️</span>
                  <span>Xóa</span>
                </button>
              </div>

              <!-- Toggle Bán / Ẩn -->
              <button
                v-if="category.isActive !== false"
                type="button"
                class="btn btn-sm btn-light text-muted px-2.5 py-1 rounded-2 border fw-semibold small"
                @click="toggleCategoryStatus(category)"
                title="Tạm ẩn danh mục"
              >
                👁️‍🗨️ Tạm Ẩn
              </button>
              <button
                v-else
                type="button"
                class="btn btn-sm btn-outline-success px-2.5 py-1 rounded-2 fw-semibold small"
                @click="toggleCategoryStatus(category)"
                title="Mở lại danh mục"
              >
                ✅ Mở Bán
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Trạng thái trống (Empty State) -->
      <div v-if="!filteredCategories.length" class="col-12 text-center text-muted py-5">
        <div class="card p-5 border-0 shadow-sm rounded-4 bg-white mx-auto" style="max-width: 440px;">
          <div class="fs-1 mb-2">📂</div>
          <h5 class="fw-bold text-dark">Không tìm thấy danh mục món ăn nào</h5>
          <p class="text-muted small mb-3">Thử thay đổi từ khóa tìm kiếm hoặc bấm nút bên dưới để tạo nhóm món mới.</p>
          <button class="btn btn-primary fw-bold px-4 py-2 rounded-3 shadow-sm" @click="openCreateModal">
            ➕ Thêm Danh Mục Mới Ngay
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- VIEW MODE 2: DẠNG BẢNG DANH SÁCH CHI TIẾT (TABLE VIEW)         -->
    <!-- ============================================================== -->
    <div v-else class="card border-0 shadow-sm rounded-4 overflow-hidden bg-white">
      <div class="table-responsive mb-0">
        <table class="table table-hover align-middle mb-0 modern-category-table">
          <thead class="bg-light border-bottom">
            <tr>
              <th style="width: 75px;" class="text-center text-muted small fw-bold">MÃ ID</th>
              <th style="min-width: 240px;" class="small fw-bold">NHÓM DANH MỤC</th>
              <th class="small fw-bold">MÔ TẢ CHI TIẾT</th>
              <th style="width: 160px;" class="text-center small fw-bold">MÓN ĂN TRONG NHÓM</th>
              <th style="width: 140px;" class="text-center small fw-bold">TRẠNG THÁI</th>
              <th style="width: 200px;" class="text-center small fw-bold">THAO TÁC</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="category in filteredCategories" :key="category.categoryId">
              <td class="text-center text-muted fw-bold small">
                #{{ category.categoryId }}
              </td>
              <td>
                <div class="d-flex align-items-center gap-3">
                  <div
                    class="table-cat-thumb rounded-3 overflow-hidden shadow-xs border d-flex align-items-center justify-content-center bg-light"
                    :style="{ background: getCategoryTheme(category).gradient }"
                  >
                    <img
                      v-if="getCategoryTopImage(category) && !failedImageMap[category.categoryId]"
                      :src="getCategoryTopImage(category)"
                      :alt="category.categoryName"
                      class="w-100 h-100 object-fit-cover"
                      @error="handleImageError(category.categoryId)"
                    />
                    <span v-else class="fs-4">{{ getCategoryIcon(category.categoryName) }}</span>
                  </div>
                  <div>
                    <div class="fw-bold text-dark fs-6 d-flex align-items-center gap-1.5">
                      <span>{{ category.categoryName }}</span>
                    </div>
                    <small class="text-muted d-block" style="font-size: 0.72rem;">Phân loại thực đơn</small>
                  </div>
                </div>
              </td>
              <td>
                <span class="text-muted small text-truncate-2">
                  {{ category.description || 'Chưa có thông tin mô tả chi tiết.' }}
                </span>
              </td>
              <td class="text-center">
                <router-link
                  :to="{ path: '/admin/foods', query: { category: category.categoryName } }"
                  class="badge bg-primary-subtle text-primary border border-primary-subtle text-decoration-none px-3 py-1.5 rounded-pill fw-bold"
                  title="Bấm để xem danh sách món ăn thuộc nhóm này"
                >
                  🍽️ {{ category.foodCount ?? 0 }} món
                </router-link>
              </td>
              <td class="text-center">
                <span
                  class="badge rounded-pill px-2.5 py-1 small fw-semibold"
                  :class="category.isActive !== false ? 'bg-success-subtle text-success border border-success-subtle' : 'bg-secondary-subtle text-secondary border border-secondary-subtle'"
                >
                  {{ category.isActive !== false ? '● Đang Bán' : '○ Đang Ẩn' }}
                </span>
              </td>
              <td class="text-center">
                <div class="d-flex justify-content-center align-items-center gap-1.5">
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-primary px-2.5 py-1 rounded-2 fw-semibold"
                    @click="openEditModal(category)"
                    title="Chỉnh sửa danh mục"
                  >
                    ✏️ Sửa
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-danger px-2.5 py-1 rounded-2 fw-semibold"
                    @click="confirmPermanentDeleteCategory(category)"
                    title="Xóa vĩnh viễn danh mục này"
                  >
                    🗑️ Xóa
                  </button>
                  <button
                    v-if="category.isActive !== false"
                    type="button"
                    class="btn btn-sm btn-light text-muted px-2 py-1 rounded-2 border"
                    @click="toggleCategoryStatus(category)"
                    title="Ẩn danh mục"
                  >
                    👁️‍🗨️
                  </button>
                  <button
                    v-else
                    type="button"
                    class="btn btn-sm btn-outline-success px-2 py-1 rounded-2"
                    @click="toggleCategoryStatus(category)"
                    title="Mở lại danh mục"
                  >
                    ✅
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!filteredCategories.length">
              <td colspan="6" class="text-center text-muted py-5">
                <div class="fs-1 mb-2">📂</div>
                <div>Không tìm thấy danh mục món ăn nào phù hợp với từ khóa.</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL 1: THÊM MỚI / CHỈNH SỬA DANH MỤC                         -->
    <!-- ============================================================== -->
    <div v-if="showModal" class="modal-backdrop-custom" @click.self="showModal = false">
      <form class="card p-4 dialog shadow-lg border-0 rounded-4" style="max-width: 500px;" @submit.prevent="saveCategory">
        <div class="d-flex justify-content-between align-items-start mb-3 border-bottom pb-2">
          <div>
            <h5 class="fw-bold m-0 text-dark d-flex align-items-center gap-2">
              <span>{{ isEditing ? '✏️' : '➕' }}</span>
              <span>{{ isEditing ? 'Cập Nhật Danh Mục Món' : 'Thêm Danh Mục Món Mới' }}</span>
            </h5>
            <p class="text-muted small mb-0 mt-0.5">
              {{ isEditing ? 'Chỉnh sửa tên và mô tả nhóm thực đơn' : 'Tạo nhóm thực đơn mới cho nhà hàng' }}
            </p>
          </div>
          <button type="button" class="btn-close" @click="showModal = false"></button>
        </div>

        <div class="mb-3">
          <label class="form-label fw-semibold small text-dark mb-1">
            Tên Danh Mục Món Ăn <span class="text-danger">*</span>
          </label>
          <input
            v-model="form.categoryName"
            class="form-control rounded-3"
            placeholder="VD: Món Khai Vị, Lẩu Thượng Hạng, Đồ Uống..."
            required
            ref="categoryInputRef"
          />
          <small class="text-muted mt-1 d-block" style="font-size: 0.72rem;">
            * Tên nhóm sẽ hiển thị trực tiếp trên menu khách hàng và trang thu ngân / phục vụ.
          </small>
        </div>

        <div class="mb-4">
          <label class="form-label fw-semibold small text-dark mb-1">Mô Tả Nhóm Món Ăn</label>
          <textarea
            v-model="form.description"
            class="form-control rounded-3"
            rows="3"
            placeholder="Mô tả phong cách chế biến, hương vị đặc trưng của nhóm món này..."
          ></textarea>
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3">
          <button type="button" class="btn btn-light rounded-3 px-3" @click="showModal = false" :disabled="isSubmitting">
            Hủy Bỏ
          </button>
          <button type="submit" class="btn btn-primary fw-bold px-4 rounded-3 shadow-sm" :disabled="isSubmitting">
            {{ isSubmitting ? 'Đang Lưu...' : (isEditing ? 'Lưu Thay Đổi' : 'Tạo Danh Mục') }}
          </button>
        </div>
      </form>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL 2: XÁC NHẬN XÓA DANH MỤC THÔNG MINH (CHUYỂN MÓN / XÓA)    -->
    <!-- ============================================================== -->
    <div v-if="showDeleteCategoryModal" class="modal-backdrop-custom" @click.self="showDeleteCategoryModal = false">
      <div class="card p-4 dialog shadow-lg border-0 rounded-4" style="max-width: 520px;">
        <div class="d-flex align-items-center gap-2 mb-3 text-danger">
          <span class="fs-3">🗑️</span>
          <h5 class="fw-bold m-0">Xóa Danh Mục Món Ăn</h5>
        </div>

        <p class="text-dark mb-3">
          Bạn đang yêu cầu xóa danh mục <strong>"{{ categoryToDelete?.categoryName }}"</strong>. 
          Hiện có <strong>{{ categoryToDelete?.foodCount ?? 0 }} món ăn</strong> thuộc nhóm này.
        </p>

        <!-- Nếu danh mục có món: cung cấp 2 lựa chọn xử lý -->
        <div v-if="(categoryToDelete?.foodCount ?? 0) > 0" class="mb-3">
          <label class="form-label fw-semibold small text-dark mb-2">Chọn phương án xử lý các món ăn:</label>

          <div
            class="form-check p-3 border rounded-3 mb-2 bg-light cursor-pointer"
            :class="{ 'border-primary bg-primary-subtle': deleteCategoryMode === 'transfer' }"
          >
            <input
              class="form-check-input"
              type="radio"
              name="deleteCategoryMode"
              id="catModeTransfer"
              value="transfer"
              v-model="deleteCategoryMode"
            >
            <label class="form-check-label fw-semibold text-dark" for="catModeTransfer">
              Chuyển {{ categoryToDelete?.foodCount }} món này sang danh mục khác
            </label>
            <div v-if="deleteCategoryMode === 'transfer'" class="mt-2">
              <label class="form-label small text-muted mb-1">Chọn danh mục tiếp nhận:</label>
              <select v-model="targetTransferCategoryId" class="form-select form-select-sm rounded-2">
                <option
                  v-for="cat in categories.filter(c => c.categoryId !== categoryToDelete?.categoryId)"
                  :key="cat.categoryId"
                  :value="cat.categoryId"
                >
                  📂 {{ cat.categoryName }} ({{ cat.foodCount ?? 0 }} món)
                </option>
              </select>
            </div>
          </div>

          <div
            class="form-check p-3 border rounded-3 bg-light cursor-pointer"
            :class="{ 'border-danger bg-danger-subtle': deleteCategoryMode === 'delete_foods' }"
          >
            <input
              class="form-check-input"
              type="radio"
              name="deleteCategoryMode"
              id="catModeDeleteFoods"
              value="delete_foods"
              v-model="deleteCategoryMode"
            >
            <label class="form-check-label fw-semibold text-danger" for="catModeDeleteFoods">
              Xóa luôn toàn bộ {{ categoryToDelete?.foodCount }} món ăn trong danh mục này
            </label>
            <small class="d-block text-muted mt-1" style="font-size: 0.75rem;">
              * Thích hợp khi bạn tạo nháp hoặc không còn kinh doanh nhóm món này. Hóa đơn giấy đã in tại thời điểm thanh toán nên không làm sai lệch báo cáo doanh thu.
            </small>
          </div>
        </div>

        <div v-else class="p-3 bg-light border rounded-3 text-muted small mb-3">
          Danh mục này hiện không có món ăn nào, sẽ được xóa ngay lập tức khỏi hệ thống.
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3">
          <button type="button" class="btn btn-light rounded-3 px-3" @click="showDeleteCategoryModal = false" :disabled="isDeletingCategory">
            Hủy Bỏ
          </button>
          <button type="button" class="btn btn-danger fw-bold px-3 rounded-3 shadow-xs" @click="executePermanentDeleteCategory" :disabled="isDeletingCategory">
            {{ isDeletingCategory ? 'Đang Xử Lý...' : 'Xác Nhận Xóa Danh Mục' }}
          </button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, nextTick, onMounted, ref } from 'vue';
import axios from 'axios';

const api = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/category`;
const categories = ref([]);
const searchQuery = ref('');
const statusFilter = ref('all'); // 'all', 'active', 'inactive'
const viewMode = ref('grid'); // 'grid' | 'table'

const showModal = ref(false);
const isEditing = ref(false);
const isSubmitting = ref(false);
const categoryInputRef = ref(null);
const errorMessage = ref('');
const successMessage = ref('');

const form = ref({
  categoryId: 0,
  categoryName: '',
  description: ''
});

// Modal Xóa Danh Mục Thông Minh
const showDeleteCategoryModal = ref(false);
const categoryToDelete = ref(null);
const deleteCategoryMode = ref('transfer'); // 'transfer' | 'delete_foods'
const targetTransferCategoryId = ref(null);
const isDeletingCategory = ref(false);

async function loadCategories() {
  errorMessage.value = '';
  try {
    const res = await axios.get(`${api}/list`);
    categories.value = res.data?.data ?? [];
  } catch (e) {
    errorMessage.value = 'Không thể tải danh sách danh mục món ăn. Hãy kiểm tra Backend.';
  }
}

// Helpers thống kê
const countActiveCategories = computed(() => categories.value.filter(c => c.isActive !== false).length);
const countInactiveCategories = computed(() => categories.value.filter(c => c.isActive === false).length);


// Bộ lọc danh mục
const filteredCategories = computed(() => {
  return categories.value.filter(c => {
    // 1. Lọc theo trạng thái
    if (statusFilter.value === 'active' && c.isActive === false) return false;
    if (statusFilter.value === 'inactive' && c.isActive !== false) return false;

    // 2. Lọc theo tìm kiếm
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = (c.categoryName ?? '').toLowerCase().includes(q);
      const matchDesc = (c.description ?? '').toLowerCase().includes(q);
      if (!matchName && !matchDesc) return false;
    }
    return true;
  });
});

// Lưu vết ảnh lỗi để tự động fallback sang icon đẹp mắt
const failedImageMap = reactive({});
function handleImageError(id) {
  if (id) failedImageMap[id] = true;
}

// Helper lấy danh sách món ăn trong danh mục (hỗ trợ cả camelCase và PascalCase)
function getCategoryFoods(category) {
  return category.sampleFoods || category.SampleFoods || [];
}

// Lấy ảnh món đầu tiên có ảnh hợp lệ
function getCategoryTopImage(category) {
  const list = getCategoryFoods(category);
  const found = list.find(f => f.imageUrl && typeof f.imageUrl === 'string' && f.imageUrl.trim().length > 0);
  if (!found) return '';
  let url = found.imageUrl.trim();
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  if (url.startsWith('/')) return `http://localhost:5079${url}`;
  return `http://localhost:5079/${url}`;
}

// Icon ngữ cảnh món ăn
function getCategoryIcon(name) {
  const n = (name || '').toLowerCase();
  if (n.includes('nướng') || n.includes('chiên') || n.includes('bbq') || n.includes('bít tết')) return '🍢';
  if (n.includes('lẩu') || n.includes('nấu') || n.includes('canh') || n.includes('súp') || n.includes('hotpot')) return '🍲';
  if (n.includes('pha chế') || n.includes('uống') || n.includes('nước') || n.includes('drink') || n.includes('bar')) return '🍹';
  if (n.includes('hải sản') || n.includes('seafood') || n.includes('tôm') || n.includes('cá') || n.includes('mực')) return '🦐';
  if (n.includes('khai vị') || n.includes('starter') || n.includes('salad')) return '🥗';
  if (n.includes('tráng miệng') || n.includes('dessert') || n.includes('chè') || n.includes('kem')) return '🍨';
  if (n.includes('rượu') || n.includes('wine') || n.includes('bia')) return '🍷';
  if (n.includes('cơm') || n.includes('mì') || n.includes('cháo')) return '🍚';
  if (n.includes('chay') || n.includes('rau')) return '🌱';
  return '📂';
}

// Theme màu Pastel sáng sủa, thanh lịch cho từng quầy
function getCategoryTheme(category) {
  const n = (category.categoryName || '').toLowerCase();
  if (n.includes('nướng') || n.includes('chiên') || n.includes('bbq') || n.includes('bít tết')) {
    return {
      gradient: 'linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 100%)',
      border: '#FDBA74'
    };
  }
  if (n.includes('lẩu') || n.includes('nấu') || n.includes('canh') || n.includes('súp') || n.includes('hotpot')) {
    return {
      gradient: 'linear-gradient(135deg, #FEFCE8 0%, #FEF08A 100%)',
      border: '#FDE047'
    };
  }
  if (n.includes('pha chế') || n.includes('uống') || n.includes('nước') || n.includes('bar') || n.includes('drink')) {
    return {
      gradient: 'linear-gradient(135deg, #F0FDFA 0%, #CCFBF1 100%)',
      border: '#5EEAD4'
    };
  }
  if (n.includes('hải sản') || n.includes('tôm') || n.includes('cá')) {
    return {
      gradient: 'linear-gradient(135deg, #EFF6FF 0%, #DBEAFE 100%)',
      border: '#93C5FD'
    };
  }
  return {
    gradient: 'linear-gradient(135deg, #F8FAFC 0%, #F1F5F9 100%)',
    border: '#E2E8F0'
  };
}



function openCreateModal() {
  isEditing.value = false;
  form.value = { categoryId: 0, categoryName: '', description: '' };
  showModal.value = true;
  nextTick(() => {
    categoryInputRef.value?.focus();
  });
}

function openEditModal(category) {
  isEditing.value = true;
  form.value = {
    categoryId: category.categoryId,
    categoryName: category.categoryName,
    description: category.description ?? ''
  };
  showModal.value = true;
  nextTick(() => {
    categoryInputRef.value?.focus();
  });
}

async function saveCategory() {
  if (!form.value.categoryName || !form.value.categoryName.trim()) {
    errorMessage.value = 'Vui lòng nhập tên danh mục món ăn!';
    return;
  }

  errorMessage.value = '';
  successMessage.value = '';
  isSubmitting.value = true;

  try {
    if (isEditing.value) {
      await axios.put(`${api}/update`, null, {
        params: {
          categoryId: form.value.categoryId,
          categoryName: form.value.categoryName.trim(),
          description: form.value.description ? form.value.description.trim() : ''
        }
      });
      successMessage.value = `Đã cập nhật danh mục "${form.value.categoryName}" thành công!`;
    } else {
      await axios.post(`${api}/insert`, null, {
        params: {
          categoryName: form.value.categoryName.trim(),
          description: form.value.description ? form.value.description.trim() : ''
        }
      });
      successMessage.value = `Đã thêm danh mục "${form.value.categoryName}" vào thực đơn thành công!`;
    }

    showModal.value = false;
    await loadCategories();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi lưu danh mục.';
  } finally {
    isSubmitting.value = false;
  }
}

// Bật / tắt ẩn danh mục
async function toggleCategoryStatus(category) {
  errorMessage.value = '';
  successMessage.value = '';

  try {
    if (category.isActive !== false) {
      await axios.delete(`${api}/delete`, {
        params: { categoryId: category.categoryId }
      });
      successMessage.value = `Đã tạm ẩn danh mục "${category.categoryName}".`;
    } else {
      await axios.patch(`${api}/reactivate`, null, {
        params: { categoryId: category.categoryId }
      });
      successMessage.value = `Đã mở lại hoạt động cho danh mục "${category.categoryName}".`;
    }
    await loadCategories();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi cập nhật trạng thái danh mục.';
  }
}

// Xóa vĩnh viễn danh mục
function confirmPermanentDeleteCategory(category) {
  categoryToDelete.value = category;
  deleteCategoryMode.value = 'transfer';
  const otherCats = categories.value.filter(c => c.categoryId !== category.categoryId);
  targetTransferCategoryId.value = otherCats.length > 0 ? otherCats[0].categoryId : null;
  showDeleteCategoryModal.value = true;
}

async function executePermanentDeleteCategory() {
  if (!categoryToDelete.value) return;
  errorMessage.value = '';
  successMessage.value = '';
  isDeletingCategory.value = true;

  try {
    const params = {
      categoryId: categoryToDelete.value.categoryId
    };

    if ((categoryToDelete.value.foodCount || 0) > 0) {
      if (deleteCategoryMode.value === 'transfer') {
        if (!targetTransferCategoryId.value) {
          errorMessage.value = 'Vui lòng chọn danh mục đích để chuyển món sang!';
          isDeletingCategory.value = false;
          return;
        }
        params.transferToCategoryId = targetTransferCategoryId.value;
      } else {
        params.deleteFoods = true;
      }
    }

    const res = await axios.delete(`${api}/permanent-delete`, { params });
    successMessage.value = res.data?.message || `Đã xóa danh mục "${categoryToDelete.value.categoryName}" thành công!`;
    showDeleteCategoryModal.value = false;
    categoryToDelete.value = null;
    await loadCategories();
  } catch (e) {
    errorMessage.value = e.response?.data?.message || 'Lỗi khi xóa danh mục.';
  } finally {
    isDeletingCategory.value = false;
  }
}

onMounted(() => {
  loadCategories();
});
</script>

<style scoped>
/* Page Layout */
.category-management-showcase {
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

/* KPI Widgets */
.kpi-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  border: 1px solid #ECE7E1 !important;
}
.kpi-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.05) !important;
}
.kpi-icon-pill {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.15rem;
}

/* Clean Category Card */
.category-clean-card {
  transition: transform 0.25s ease, box-shadow 0.25s ease;
  border: 1px solid #ECE7E1 !important;
}
.category-clean-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.07) !important;
}

.category-card-header {
  border-top-left-radius: 16px;
  border-top-right-radius: 16px;
}

.category-thumb-box {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
}

.category-header-title {
  color: #1e293b;
  letter-spacing: -0.01em;
}

.category-desc-text {
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  min-height: 2.8em;
  color: #64748b !important;
}

/* Modern Table */
.modern-category-table tbody tr {
  transition: background-color 0.15s ease;
}
.modern-category-table tbody tr:hover {
  background-color: #F8FAFC;
}
.table-cat-thumb {
  width: 44px;
  height: 44px;
  flex-shrink: 0;
}

.text-truncate-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* Modals */
.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1050;
  padding: 1rem;
  animation: fadeIn 0.2s ease;
}

.dialog {
  width: 100%;
  animation: modalScale 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  background: #ffffff;
}

@keyframes modalScale {
  from { opacity: 0; transform: scale(0.96) translateY(8px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}

.cursor-pointer {
  cursor: pointer;
}
</style>

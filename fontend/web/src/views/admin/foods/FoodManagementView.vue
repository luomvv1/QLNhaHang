<template>
  <div class="food-management-page">
    <!-- Top Header -->
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3 mb-4">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge rounded-pill bg-primary-subtle text-primary border border-primary-subtle px-3 py-1 fw-bold fs-7">
            ⭐ THỰC ĐƠN NHÀ HÀNG
          </span>
          <span class="badge rounded-pill bg-success-subtle text-success border border-success-subtle px-2 py-1 fw-bold small">
            Realtime Auto
          </span>
        </div>
        <h2 class="fw-black text-dark mb-1 d-flex align-items-center gap-2">
          <span>🍔</span> Quản Lý Món Ăn & Biến Thể
        </h2>
        <p class="text-muted mb-0 small">
          Quản lý toàn diện hình ảnh món ăn, phân loại, giá bán và các kích thước phần ăn linh hoạt.
        </p>
      </div>

      <div class="d-flex align-items-center gap-2">
        <button class="btn btn-outline-secondary fw-bold px-3 py-2 rounded-3 shadow-xs d-flex align-items-center gap-2" @click="loadData" :disabled="isLoading">
          <span :class="{ 'spin-anim': isLoading }">🔄</span>
          <span class="d-none d-sm-inline">Làm mới</span>
        </button>
        <button class="btn btn-primary fw-bold px-4 py-2 rounded-3 shadow-sm d-flex align-items-center gap-2 add-food-btn" @click="openCreateModal">
          <span>➕</span>
          <span>Thêm Món Ăn Mới</span>
        </button>
      </div>
    </div>

    <!-- KPI Metric Cards Bar -->
    <div class="row g-3 mb-4">
      <!-- Total Dishes -->
      <div class="col-6 col-lg-3">
        <div class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-muted small fw-semibold">Tổng Thực Đơn</span>
            <span class="kpi-icon-badge bg-primary-subtle text-primary">🍽️</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h3 class="fw-black mb-0 text-dark">{{ foods.length }}</h3>
            <span class="text-muted small">món</span>
          </div>
          <div class="text-muted small mt-1">
            Trên <strong>{{ categories.length }}</strong> danh mục
          </div>
        </div>
      </div>

      <!-- Active Selling -->
      <div class="col-6 col-lg-3">
        <div class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-muted small fw-semibold">Đang Kinh Doanh</span>
            <span class="kpi-icon-badge bg-success-subtle text-success">🟢</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h3 class="fw-black mb-0 text-success">{{ countActive }}</h3>
            <span class="text-success small fw-semibold">sẵn sàng</span>
          </div>
          <div class="text-muted small mt-1">
            Khách có thể gọi món
          </div>
        </div>
      </div>

      <!-- Inactive / Stopped -->
      <div class="col-6 col-lg-3">
        <div class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-muted small fw-semibold">Tạm Ngừng Bán</span>
            <span class="kpi-icon-badge bg-danger-subtle text-danger">⛔</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h3 class="fw-black mb-0 text-danger">{{ countInactive }}</h3>
            <span class="text-danger small fw-semibold">ngừng</span>
          </div>
          <div class="text-muted small mt-1">
            Hết món hoặc ngừng vụ
          </div>
        </div>
      </div>

      <!-- Image Coverage -->
      <div class="col-6 col-lg-3">
        <div class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100">
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-muted small fw-semibold">Có Hình Ảnh</span>
            <span class="kpi-icon-badge bg-warning-subtle text-warning">🖼️</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h3 class="fw-black mb-0 text-dark">{{ countWithImage }}/{{ foods.length }}</h3>
            <span class="badge bg-warning-subtle text-dark border small fw-bold">
              {{ foods.length ? Math.round((countWithImage / foods.length) * 100) : 0 }}%
            </span>
          </div>
          <div class="progress mt-2" style="height: 6px;">
            <div
              class="progress-bar bg-warning progress-bar-striped"
              :style="{ width: (foods.length ? (countWithImage / foods.length) * 100 : 0) + '%' }"
            ></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Thanh Lọc, Tìm Kiếm & Chuyển Đổi View Mode -->
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4 bg-white">
      <div class="row g-2 align-items-center">
        <!-- Ô Tìm Kiếm -->
        <div class="col-12 col-md-4 col-lg-4">
          <div class="input-group search-input-group">
            <span class="input-group-text bg-light border-end-0 text-muted">🔍</span>
            <input
              v-model="searchQuery"
              class="form-control bg-light border-start-0 ps-0"
              placeholder="Tìm theo tên món, mô tả, size..."
            />
            <button v-if="searchQuery" class="btn btn-light border-start-0 text-muted" @click="searchQuery = ''">
              ✕
            </button>
          </div>
        </div>

        <!-- Lọc Danh Mục -->
        <div class="col-6 col-md-3 col-lg-3">
          <select v-model="selectedCategory" class="form-select bg-light border-1">
            <option value="">📂 Tất cả danh mục ({{ foods.length }})</option>
            <option v-for="c in categories" :key="c.categoryId" :value="c.categoryId">
              {{ getCategoryIcon(c.categoryName) }} {{ c.categoryName }} ({{ getCategoryCount(c.categoryId) }})
            </option>
          </select>
        </div>

        <!-- Lọc Trạng Thái Kinh Doanh -->
        <div class="col-6 col-md-3 col-lg-3">
          <select v-model="activeFilter" class="form-select bg-light border-1">
            <option :value="null">📋 Tất cả trạng thái ({{ foods.length }})</option>
            <option :value="true">🟢 Đang kinh doanh ({{ countActive }})</option>
            <option :value="false">⛔ Tạm ngừng bán ({{ countInactive }})</option>
          </select>
        </div>

        <!-- Toggle View Mode (Lưới / Bảng) -->
        <div class="col-12 col-md-2 col-lg-2 text-md-end mt-2 mt-md-0">
          <div class="btn-group view-toggle-group shadow-xs p-1 bg-light rounded-3">
            <button
              class="btn btn-sm px-3 rounded-2 fw-bold"
              :class="viewMode === 'grid' ? 'btn-white shadow-xs text-primary' : 'text-muted'"
              @click="viewMode = 'grid'"
              title="Chế độ xem Thẻ Hình Ảnh (Grid)"
            >
              ▦ Lưới
            </button>
            <button
              class="btn btn-sm px-3 rounded-2 fw-bold"
              :class="viewMode === 'table' ? 'btn-white shadow-xs text-primary' : 'text-muted'"
              @click="viewMode = 'table'"
              title="Chế độ xem Bảng Danh Sách (Table)"
            >
              ≡ Bảng
            </button>
          </div>
        </div>
      </div>

      <!-- Quick Chips: Lọc theo có ảnh/chưa ảnh -->
      <div class="d-flex align-items-center gap-2 mt-3 pt-3 border-top flex-wrap">
        <span class="text-muted small fw-semibold me-1">Bộ lọc nhanh:</span>
        <button
          class="btn btn-sm rounded-pill quick-chip"
          :class="imageFilter === 'all' ? 'btn-primary' : 'btn-light border'"
          @click="imageFilter = 'all'"
        >
          Tất cả ảnh ({{ foods.length }})
        </button>
        <button
          class="btn btn-sm rounded-pill quick-chip"
          :class="imageFilter === 'has_image' ? 'btn-success text-white' : 'btn-light border'"
          @click="imageFilter = 'has_image'"
        >
          🖼️ Đã có ảnh ({{ countWithImage }})
        </button>
        <button
          class="btn btn-sm rounded-pill quick-chip"
          :class="imageFilter === 'no_image' ? 'btn-warning text-dark' : 'btn-light border'"
          @click="imageFilter = 'no_image'"
        >
          ⚠️ Chưa có ảnh ({{ countWithoutImage }})
        </button>

        <span class="ms-auto small text-muted">
          Đang hiển thị <strong>{{ filteredFoods.length }}</strong> / {{ foods.length }} món
        </span>
      </div>
    </div>

    <!-- Thông báo lỗi / thành công -->
    <transition name="fade">
      <div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show shadow-sm rounded-3 py-2 px-3 small mb-3">
        <span>⚠️ {{ errorMessage }}</span>
        <button type="button" class="btn-close py-2" @click="errorMessage = ''"></button>
      </div>
    </transition>
    <transition name="fade">
      <div v-if="successMessage" class="alert alert-success alert-dismissible fade show shadow-sm rounded-3 py-2 px-3 small mb-3">
        <span>✅ {{ successMessage }}</span>
        <button type="button" class="btn-close py-2" @click="successMessage = ''"></button>
      </div>
    </transition>

    <!-- LOADING STATE -->
    <div v-if="isLoading" class="text-center py-5">
      <div class="spinner-border text-primary" role="status"></div>
      <p class="text-muted small mt-2">Đang tải danh sách món ăn từ máy chủ...</p>
    </div>

    <!-- EMPTY STATE -->
    <div v-else-if="filteredFoods.length === 0" class="card border-0 shadow-sm rounded-4 p-5 text-center bg-white my-4">
      <div class="fs-1 mb-2">🍽️</div>
      <h5 class="fw-bold text-dark mb-1">Không tìm thấy món ăn phù hợp</h5>
      <p class="text-muted small mb-3">Vui lòng thử tìm kiếm với từ khóa khác hoặc điều chỉnh bộ lọc.</p>
      <div>
        <button class="btn btn-outline-primary btn-sm rounded-3 fw-bold px-3" @click="resetFilters">
          🔄 Đặt lại bộ lọc
        </button>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- VIEW MODE 1: GRID VIEW (LƯỚI THẺ HÌNH ẢNH SANG TRỌNG)           -->
    <!-- ============================================================== -->
    <div v-else-if="viewMode === 'grid'" class="food-cards-grid">
      <div
        v-for="food in filteredFoods"
        :key="food.foodId"
        class="food-card-item rounded-4 bg-white shadow-sm overflow-hidden border transition"
        :class="{ 'is-inactive-card opacity-75': !food.isActive }"
      >
        <!-- Card Image Header -->
        <div class="food-card-img-wrap" @click="openImageLightbox(food)">
          <template v-if="food.imageUrl">
            <img
              :src="food.imageUrl"
              :alt="food.foodName"
              class="food-card-img"
              loading="lazy"
              @error="handleImageError(food)"
            />
            <div class="img-hover-overlay">
              <span>🔍 Phóng to ảnh</span>
            </div>
          </template>
          <!-- Fallback when no image -->
          <div v-else class="no-image-placeholder">
            <span class="placeholder-emoji">{{ getCategoryIcon(food.categoryName) }}</span>
            <span class="placeholder-text">Chưa có ảnh</span>
            <button
              class="btn btn-xs btn-light shadow-xs rounded-pill px-2 py-1 add-img-hint"
              @click.stop="openEditModal(food)"
            >
              + Thêm ảnh
            </button>
          </div>

          <!-- Top-left category badge -->
          <span class="category-badge-pill">
            {{ food.categoryName }}
          </span>

          <!-- Top-right status pill -->
          <span
            class="status-badge-pill"
            :class="food.isActive ? 'status-active' : 'status-inactive'"
          >
            {{ food.isActive ? '● Đang bán' : '○ Ngừng bán' }}
          </span>
        </div>

        <!-- Card Body -->
        <div class="food-card-body p-3 d-flex flex-column">
          <!-- Food Name & ID -->
          <div class="d-flex justify-content-between align-items-start mb-1">
            <h5 class="fw-bold text-dark mb-0 food-name-clamp" :title="food.foodName">
              {{ food.foodName }}
            </h5>
            <span class="text-muted small fw-semibold ms-2">#{{ food.foodId }}</span>
          </div>

          <!-- Description -->
          <p class="text-muted small food-desc-clamp mb-2" :title="food.description">
            {{ food.description || 'Chưa cập nhật mô tả món ăn này.' }}
          </p>

          <!-- Variants List (Sizes & Prices) -->
          <div class="variants-chips-wrap mb-3">
            <div v-if="food.variants && food.variants.length > 0" class="d-flex flex-wrap gap-1">
              <span
                v-for="v in food.variants"
                :key="v.variantId"
                class="variant-tag-chip"
              >
                🏷️ {{ v.variantName }}: <strong>{{ formatCurrency(v.price) }}</strong>
              </span>
            </div>
            <div v-else class="text-muted small fst-italic">
              Chưa có biến thể giá
            </div>
          </div>

          <!-- Card Footer (Price & Actions) -->
          <div class="mt-auto pt-2 border-top d-flex justify-content-between align-items-center">
            <div>
              <span class="text-muted small d-block" style="font-size: 0.72rem;">Giá khởi điểm</span>
              <span class="food-min-price fw-black text-danger fs-5">
                {{ formatCurrency(food.minPrice) }}
              </span>
            </div>

            <div class="d-flex align-items-center gap-1">
              <button
                type="button"
                class="btn btn-sm px-2 py-1 rounded-2 fw-semibold"
                :class="isPublishedToNews(food.foodId) ? 'btn-warning text-dark' : 'btn-outline-secondary'"
                @click="togglePublishToNews(food)"
                :title="isPublishedToNews(food.foodId) ? 'Đang hiển thị trên Tin Tức (Nhấn để gỡ)' : 'Đăng món này lên trang Tin Tức'"
              >
                📰 {{ isPublishedToNews(food.foodId) ? 'Đã Đăng' : 'Đăng Tin' }}
              </button>

              <button
                class="btn btn-sm btn-outline-primary px-2 py-1 rounded-2 fw-semibold"
                @click="openEditModal(food)"
                title="Chỉnh sửa thông tin món và hình ảnh"
              >
                ✏️ Sửa
              </button>

              <button
                type="button"
                class="btn btn-sm btn-outline-danger px-2 py-1 rounded-2 fw-semibold"
                @click="confirmPermanentDeleteFood(food)"
                title="Xóa vĩnh viễn món ăn này khỏi hệ thống"
              >
                🗑️
              </button>

              <button
                v-if="food.isActive"
                class="btn btn-sm btn-outline-danger px-2 py-1 rounded-2"
                @click="stopSelling(food.foodId)"
                title="Chuyển sang trạng thái ngừng bán"
              >
                ⛔
              </button>
              <button
                v-else
                class="btn btn-sm btn-outline-success px-2 py-1 rounded-2 fw-bold"
                @click="reactivateFood(food.foodId)"
                title="Mở bán trở lại cho khách"
              >
                ✅ Bán lại
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- VIEW MODE 2: TABLE VIEW (BẢNG DANH SÁCH CHI TIẾT CÓ ẢNH)        -->
    <!-- ============================================================== -->
    <div v-else class="card border-0 shadow-sm rounded-4 overflow-hidden bg-white mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-dark">
            <tr>
              <th style="width: 70px;" class="text-center">Mã</th>
              <th style="width: 90px;" class="text-center">Hình Ảnh</th>
              <th>Tên Món Ăn & Mô Tả</th>
              <th>Danh Mục</th>
              <th>Giá Khởi Điểm</th>
              <th>Biến Thể (Size / Giá)</th>
              <th class="text-center" style="width: 130px;">Trạng Thái</th>
              <th class="text-center" style="width: 170px;">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="food in filteredFoods"
              :key="food.foodId"
              :class="{ 'table-secondary opacity-75': !food.isActive }"
            >
              <td class="text-center text-muted fw-bold">#{{ food.foodId }}</td>

              <!-- Cột Ảnh Món Ăn -->
              <td class="text-center">
                <div
                  class="table-img-thumb rounded-3 shadow-xs position-relative cursor-pointer"
                  @click="openImageLightbox(food)"
                  title="Nhấn để xem ảnh phóng to"
                >
                  <img
                    v-if="food.imageUrl"
                    :src="food.imageUrl"
                    :alt="food.foodName"
                    class="w-100 h-100 object-fit-cover rounded-3"
                    loading="lazy"
                    @error="handleImageError(food)"
                  />
                  <div v-else class="table-no-img d-flex align-items-center justify-content-center h-100 text-muted rounded-3">
                    <span style="font-size: 1.4rem;">{{ getCategoryIcon(food.categoryName) }}</span>
                  </div>
                </div>
              </td>

              <!-- Tên Món -->
              <td>
                <div class="fw-bold text-dark fs-6">{{ food.foodName }}</div>
                <small class="text-muted d-block text-truncate" style="max-width: 280px;">
                  {{ food.description || 'Chưa có mô tả' }}
                </small>
              </td>

              <!-- Danh Mục -->
              <td>
                <span class="badge bg-primary-subtle text-primary border border-primary-subtle px-2 py-1 rounded-pill">
                  {{ food.categoryName }}
                </span>
              </td>

              <!-- Giá Khởi Điểm -->
              <td class="fw-bold text-danger">
                {{ formatCurrency(food.minPrice) }}
              </td>

              <!-- Biến Thể -->
              <td>
                <div class="d-flex flex-wrap gap-1" style="max-width: 320px;">
                  <span
                    v-for="v in food.variants"
                    :key="v.variantId"
                    class="badge bg-light text-dark border p-1 small"
                  >
                    🏷️ {{ v.variantName }}: <b>{{ formatCurrency(v.price) }}</b>/{{ v.unit }}
                  </span>
                  <span v-if="!food.variants?.length" class="text-muted small">Chưa có biến thể</span>
                </div>
              </td>

              <!-- Trạng Thái -->
              <td class="text-center">
                <span v-if="food.isActive" class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1 rounded-pill fw-semibold">
                  ● Đang bán
                </span>
                <span v-else class="badge bg-secondary-subtle text-secondary border px-2 py-1 rounded-pill">
                  ○ Ngừng bán
                </span>
              </td>

              <!-- Thao Tác -->
              <td class="text-center">
                <div class="d-flex justify-content-center gap-1">
                  <button
                    type="button"
                    class="btn btn-sm px-2 rounded-2 fw-semibold"
                    :class="isPublishedToNews(food.foodId) ? 'btn-warning text-dark' : 'btn-outline-secondary'"
                    @click="togglePublishToNews(food)"
                    :title="isPublishedToNews(food.foodId) ? 'Đang hiển thị trên Tin Tức (Nhấn để gỡ)' : 'Đăng món này lên trang Tin Tức'"
                  >
                    📰 {{ isPublishedToNews(food.foodId) ? 'Đã Đăng' : 'Đăng Tin' }}
                  </button>
                  <button
                    class="btn btn-sm btn-outline-primary px-2 rounded-2"
                    @click="openEditModal(food)"
                    title="Chỉnh sửa thông tin và hình ảnh"
                  >
                    ✏️ Sửa
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-danger px-2 rounded-2 fw-semibold"
                    @click="confirmPermanentDeleteFood(food)"
                    title="Xóa vĩnh viễn món ăn này"
                  >
                    🗑️ Xóa
                  </button>
                  <button
                    v-if="food.isActive"
                    class="btn btn-sm btn-outline-danger px-2 rounded-2"
                    @click="stopSelling(food.foodId)"
                    title="Chuyển sang ngừng bán"
                  >
                    ⛔ Ngừng
                  </button>
                  <button
                    v-else
                    class="btn btn-sm btn-outline-success px-2 rounded-2 fw-semibold"
                    @click="reactivateFood(food.foodId)"
                    title="Mở bán trở lại"
                  >
                    ✅ Mở lại
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL THÊM / CHỈNH SỬA MÓN ĂN & HÌNH ẢNH (PRO UI/UX)           -->
    <!-- ============================================================== -->
    <div v-if="showModal" class="modal-backdrop-custom">
      <form class="card p-0 dialog shadow-xl border-0 overflow-hidden" @submit.prevent="saveFood">
        <!-- Modal Header -->
        <div class="modal-header-custom p-3 px-4 d-flex justify-content-between align-items-center border-bottom bg-white">
          <div class="d-flex align-items-center gap-2">
            <span class="fs-4">{{ isEditing ? '✏️' : '➕' }}</span>
            <div>
              <h5 class="fw-bold m-0 text-primary">
                {{ isEditing ? 'Cập Nhật Món Ăn & Hình Ảnh' : 'Thêm Món Ăn Mới Vào Thực Đơn' }}
              </h5>
              <small class="text-muted">Nhập thông tin món, tải hình ảnh đại diện và thiết lập các size giá bán.</small>
            </div>
          </div>
          <button type="button" class="btn-close" @click="showModal = false"></button>
        </div>

        <!-- Modal Body (Scrollable) -->
        <div class="modal-body-scrollable p-4">
          <div class="row g-4">
            <!-- CỘT TRÁI: QUẢN LÝ HÌNH ẢNH MÓN ĂN -->
            <div class="col-12 col-lg-5">
              <label class="form-label fw-bold small text-dark d-flex align-items-center justify-content-between">
                <span>🖼️ Hình Ảnh Đại Diện Món Ăn</span>
                <span v-if="form.imageUrl" class="text-success small fw-normal">✓ Đã có ảnh</span>
              </label>

              <!-- Image Preview Box -->
              <div class="modal-img-preview-box rounded-3 border overflow-hidden position-relative mb-2">
                <template v-if="form.imageUrl">
                  <img
                    :src="form.imageUrl"
                    alt="Xem trước ảnh món"
                    class="w-100 h-100 object-fit-cover preview-img"
                    @error="form.imageUrl = ''"
                  />
                  <div class="preview-img-actions">
                    <button
                      type="button"
                      class="btn btn-sm btn-danger px-2 py-1 rounded-pill shadow-xs"
                      @click="form.imageUrl = ''"
                      title="Xóa ảnh này"
                    >
                      🗑️ Xóa ảnh
                    </button>
                  </div>
                </template>
                <div v-else class="preview-placeholder-box d-flex flex-column align-items-center justify-content-center p-3 text-center">
                  <span class="placeholder-big-icon mb-1">📸</span>
                  <span class="fw-semibold text-dark small">Chưa có ảnh món ăn</span>
                  <span class="text-muted small" style="font-size: 0.75rem;">
                    Tải ảnh từ máy tính hoặc dán link URL bên dưới
                  </span>
                </div>

                <!-- Spinner when uploading -->
                <div v-if="isUploadingImage" class="uploading-overlay">
                  <div class="spinner-border text-primary" role="status"></div>
                  <span class="small text-white fw-bold mt-2">Đang tải ảnh lên...</span>
                </div>
              </div>

              <!-- Upload Button (File from Computer) -->
              <input
                ref="fileInputRef"
                type="file"
                accept="image/jpeg,image/png,image/webp"
                class="d-none"
                @change="handleFileUpload"
              />
              <div class="d-grid mb-2">
                <button
                  type="button"
                  class="btn btn-outline-primary btn-sm rounded-3 fw-bold d-flex align-items-center justify-content-center gap-2"
                  :disabled="isUploadingImage"
                  @click="triggerFileInput"
                >
                  <span>📁</span>
                  <span>Tải ảnh từ máy tính (JPG, PNG, WEBP)</span>
                </button>
              </div>

              <!-- URL Input -->
              <div class="mb-2">
                <div class="input-group input-group-sm">
                  <span class="input-group-text bg-light text-muted">🔗</span>
                  <input
                    v-model="form.imageUrl"
                    type="url"
                    class="form-control"
                    placeholder="Hoặc dán URL ảnh trực tuyến..."
                  />
                </div>
              </div>

              <!-- Preset Sample Images Shortcut -->
              <div class="preset-images-box p-2 bg-light rounded-3 border">
                <span class="small text-muted d-block mb-1 fw-semibold" style="font-size: 0.72rem;">
                  💡 Gợi ý mẫu ảnh đẹp sẵn có (Nhấn để dùng):
                </span>
                <div class="d-flex flex-wrap gap-1">
                  <button
                    v-for="sample in sampleFoodImages"
                    :key="sample.name"
                    type="button"
                    class="btn btn-xs btn-white border rounded-pill py-0 px-2 small text-dark"
                    @click="form.imageUrl = sample.url"
                  >
                    {{ sample.icon }} {{ sample.name }}
                  </button>
                </div>
              </div>
            </div>

            <!-- CỘT PHẢI: THÔNG TIN MÓN ĂN & BIẾN THỂ -->
            <div class="col-12 col-lg-7">
              <div class="row g-3 mb-3">
                <!-- Danh mục -->
                <div class="col-md-6">
                  <label class="form-label fw-bold small text-dark">
                    Danh mục món <span class="text-danger">*</span>
                  </label>
                  <select v-model="form.categoryId" class="form-select" required>
                    <option value="" disabled>-- Chọn danh mục --</option>
                    <option v-for="c in categories" :key="c.categoryId" :value="c.categoryId">
                      {{ c.categoryName }}
                    </option>
                  </select>
                </div>

                <!-- Tên món ăn -->
                <div class="col-md-6">
                  <label class="form-label fw-bold small text-dark">
                    Tên món ăn <span class="text-danger">*</span>
                  </label>
                  <input
                    v-model="form.foodName"
                    class="form-control"
                    placeholder="VD: Bò Bít Tết Sốt Tiêu Đen"
                    required
                  />
                </div>
              </div>

              <div class="row g-3 mb-3">
                <!-- Giá khởi điểm -->
                <div class="col-md-6">
                  <label class="form-label fw-bold small text-dark">
                    Giá khởi điểm (VNĐ) <span class="text-danger">*</span>
                  </label>
                  <div class="input-group">
                    <input
                      v-model.number="form.minPrice"
                      type="number"
                      min="0"
                      step="1000"
                      class="form-control"
                      placeholder="VD: 139000"
                      required
                    />
                    <span class="input-group-text bg-light text-muted">₫</span>
                  </div>
                </div>

                <!-- Trạng thái kinh doanh -->
                <div class="col-md-6">
                  <label class="form-label fw-bold small text-dark">Trạng thái bán</label>
                  <select v-model="form.foodStatus" class="form-select">
                    <option value="available">🟢 Sẵn sàng phục vụ (Available)</option>
                    <option value="sold_out">🔴 Tạm hết món (Sold out)</option>
                  </select>
                </div>
              </div>

              <!-- Mô tả -->
              <div class="mb-3">
                <label class="form-label fw-bold small text-dark">Mô tả hương vị & nguyên liệu</label>
                <textarea
                  v-model="form.description"
                  class="form-control"
                  rows="2"
                  placeholder="Giới thiệu cách chế biến, hương vị đặc trưng..."
                ></textarea>
              </div>

              <!-- DANH SÁCH BIẾN THỂ (SIZE / GIÁ) -->
              <div class="p-3 bg-light rounded-3 border">
                <div class="d-flex justify-content-between align-items-center mb-2">
                  <div>
                    <h6 class="fw-bold m-0 text-dark">🏷️ Biến Thể & Định Lượng (Size, Giá)</h6>
                    <small class="text-muted">Các size phần ăn (VD: Size Nhỏ, Size Lớn, Đĩa, Nồi...)</small>
                  </div>
                  <button type="button" class="btn btn-sm btn-outline-primary fw-bold rounded-2 px-2" @click="addVariantRow">
                    + Thêm Size
                  </button>
                </div>

                <div
                  v-for="(v, index) in form.variants"
                  :key="index"
                  class="row g-2 align-items-center mb-2 bg-white p-2 rounded-2 border"
                >
                  <div class="col-5">
                    <input
                      v-model="v.variantName"
                      class="form-control form-control-sm"
                      placeholder="Tên size (VD: Size Vừa)"
                      required
                    />
                  </div>
                  <div class="col-4">
                    <input
                      v-model.number="v.price"
                      type="number"
                      min="0"
                      step="1000"
                      class="form-control form-control-sm"
                      placeholder="Giá (VNĐ)"
                      required
                    />
                  </div>
                  <div class="col-2">
                    <input
                      v-model="v.unit"
                      class="form-control form-control-sm"
                      placeholder="ĐVT"
                      required
                    />
                  </div>
                  <div class="col-1 text-end">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-danger p-1 rounded"
                      :disabled="form.variants.length <= 1"
                      title="Xóa biến thể này"
                      @click="removeVariantRow(index, v)"
                    >
                      ✕
                    </button>
                  </div>
                </div>
              </div>

              <!-- TÙY CHỌN ĐĂNG LÊN TRANG TIN TỨC -->
              <div class="p-3 mt-3 bg-warning-subtle rounded-3 border border-warning-subtle d-flex align-items-center justify-content-between">
                <div>
                  <div class="fw-bold small text-dark d-flex align-items-center gap-1">
                    <span>📰</span>
                    <span>Đăng bài giới thiệu món mới lên trang Tin Tức</span>
                  </div>
                  <small class="text-muted">Món ăn này sẽ tự động xuất hiện trên chuyên mục Món Mới Ra Mắt của trang Tin Tức cho thực khách.</small>
                </div>
                <div class="form-check form-switch mb-0 fs-5">
                  <input class="form-check-input" type="checkbox" v-model="form.publishToNews" role="switch">
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="p-3 px-4 d-flex justify-content-end gap-2 border-top bg-light">
          <button type="button" class="btn btn-light border px-3 rounded-3" @click="showModal = false">
            Đóng
          </button>
          <button type="submit" class="btn btn-primary fw-bold px-4 rounded-3 shadow-sm" :disabled="isSubmitting">
            <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-1"></span>
            {{ isEditing ? 'Cập Nhật Món Ăn' : 'Lưu Vào Thực Đơn' }}
          </button>
        </div>
      </form>
    </div>

    <!-- ============================================================== -->
    <!-- LIGHTBOX: XEM ẢNH FULL-SIZE SẮC NÉT                             -->
    <!-- ============================================================== -->
    <div v-if="lightboxFood" class="modal-backdrop-custom" @click="lightboxFood = null">
      <div class="lightbox-dialog" @click.stop>
        <div class="lightbox-img-wrap rounded-4 overflow-hidden shadow-2xl bg-dark position-relative">
          <button class="lightbox-close-btn" @click="lightboxFood = null">×</button>
          <img
            :src="lightboxFood.imageUrl"
            :alt="lightboxFood.foodName"
            class="lightbox-img"
          />
          <div class="lightbox-caption p-3 px-4 text-white">
            <div class="d-flex justify-content-between align-items-center">
              <div>
                <h4 class="fw-bold mb-1">{{ lightboxFood.foodName }}</h4>
                <p class="small text-white-50 mb-0">{{ lightboxFood.description || lightboxFood.categoryName }}</p>
              </div>
              <div class="text-end">
                <span class="badge bg-warning text-dark fw-bold fs-6 px-3 py-2 rounded-pill">
                  {{ formatCurrency(lightboxFood.minPrice) }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- MODAL XÁC NHẬN XÓA VĨNH VIỄN MÓN ĂN -->
    <div v-if="showDeleteFoodModal" class="modal-backdrop-custom" @click.self="showDeleteFoodModal = false">
      <div class="card p-4 dialog shadow-lg border-0 rounded-4" style="max-width: 480px;">
        <div class="d-flex align-items-center gap-2 mb-3 text-danger">
          <span class="fs-3">🗑️</span>
          <h5 class="fw-bold m-0">Xóa Vĩnh Viễn Món Ăn</h5>
        </div>

        <div class="d-flex align-items-center gap-3 p-3 bg-light rounded-3 mb-3 border">
          <img 
            :src="foodToDelete?.imageUrl || 'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=120&auto=format&fit=crop&q=80'" 
            class="rounded-3 object-fit-cover shadow-xs border"
            style="width: 60px; height: 60px;"
          />
          <div>
            <h6 class="fw-bold text-dark mb-1">{{ foodToDelete?.foodName }}</h6>
            <div class="d-flex align-items-center gap-2">
              <span class="badge bg-primary-subtle text-primary border border-primary-subtle">{{ foodToDelete?.categoryName }}</span>
              <span class="text-danger fw-bold small">{{ formatCurrency(foodToDelete?.minPrice) }}</span>
            </div>
          </div>
        </div>

        <p class="text-dark small mb-2">
          Bạn có chắc chắn muốn xóa vĩnh viễn món <strong>"{{ foodToDelete?.foodName }}"</strong> khỏi thực đơn nhà hàng không?
        </p>

        <div class="p-3 bg-light rounded-3 border mb-3 small text-muted">
          <div class="d-flex align-items-center gap-1.5 text-secondary fw-semibold mb-1">
            <span>ℹ️</span> <span>Ghi chú hệ thống:</span>
          </div>
          <div>Các phiếu hóa đơn và đơn gọi món trước đây đã lưu vết ảnh chụp tên món và giá tiền tại thời điểm thanh toán. Việc xóa món này giúp làm gọn thực đơn mà không ảnh hưởng báo cáo doanh thu.</div>
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3">
          <button type="button" class="btn btn-light rounded-3 px-3" @click="showDeleteFoodModal = false" :disabled="isDeletingFood">
            Hủy Bỏ
          </button>
          <button type="button" class="btn btn-danger fw-bold px-3 rounded-3 shadow-xs" @click="executePermanentDeleteFood" :disabled="isDeletingFood">
            {{ isDeletingFood ? 'Đang Xóa...' : 'Xác Nhận Xóa Món' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import axios from 'axios';

const api = 'http://localhost:5079/api/admin';
const foods = ref([]);
const categories = ref([]);
const activeFilter = ref(null); // null = all, true = active, false = inactive
const searchQuery = ref('');
const selectedCategory = ref('');
const imageFilter = ref('all'); // 'all', 'has_image', 'no_image'
const viewMode = ref('grid'); // 'grid' | 'table'

const showModal = ref(false);
const isEditing = ref(false);
const isLoading = ref(false);
const isSubmitting = ref(false);
const isUploadingImage = ref(false);
const showDeleteFoodModal = ref(false);
const foodToDelete = ref(null);
const isDeletingFood = ref(false);

function confirmPermanentDeleteFood(food) {
  foodToDelete.value = food;
  showDeleteFoodModal.value = true;
}

async function executePermanentDeleteFood() {
  if (!foodToDelete.value) return;
  errorMessage.value = '';
  successMessage.value = '';
  isDeletingFood.value = true;

  try {
    const res = await axios.delete(`${api}/food/permanent-delete`, {
      params: { foodId: foodToDelete.value.foodId }
    });
    successMessage.value = res.data?.message || `Đã xóa vĩnh viễn món "${foodToDelete.value.foodName}" thành công!`;
    
    // Gỡ món khỏi danh sách tin tức nếu có
    const idx = publishedNewsFoodIds.value.indexOf(foodToDelete.value.foodId);
    if (idx > -1) {
      publishedNewsFoodIds.value.splice(idx, 1);
      localStorage.setItem(PUBLISHED_FOODS_KEY, JSON.stringify(publishedNewsFoodIds.value));
    }

    showDeleteFoodModal.value = false;
    foodToDelete.value = null;
    await loadData();
  } catch (e) {
    errorMessage.value = e.response?.data?.message || 'Lỗi khi xóa món ăn.';
  } finally {
    isDeletingFood.value = false;
  }
}
const errorMessage = ref('');
const successMessage = ref('');
const fileInputRef = ref(null);
const lightboxFood = ref(null);

const PUBLISHED_FOODS_KEY = 'published_news_food_ids';
const publishedNewsFoodIds = ref([]);

function loadPublishedNewsFoodIds() {
  try {
    const raw = localStorage.getItem(PUBLISHED_FOODS_KEY);
    publishedNewsFoodIds.value = raw ? JSON.parse(raw) : [];
  } catch {
    publishedNewsFoodIds.value = [];
  }
}

function isPublishedToNews(foodId) {
  return publishedNewsFoodIds.value.includes(foodId);
}

function togglePublishToNews(food) {
  const id = food.foodId;
  const idx = publishedNewsFoodIds.value.indexOf(id);
  if (idx > -1) {
    publishedNewsFoodIds.value.splice(idx, 1);
    successMessage.value = `Đã gỡ món "${food.foodName}" khỏi trang Tin Tức.`;
  } else {
    publishedNewsFoodIds.value.push(id);
    successMessage.value = `Đã đăng món "${food.foodName}" lên trang Tin Tức thành công!`;
  }
  localStorage.setItem(PUBLISHED_FOODS_KEY, JSON.stringify(publishedNewsFoodIds.value));
}

const form = ref({
  foodId: 0,
  categoryId: '',
  foodName: '',
  description: '',
  imageUrl: '',
  minPrice: 0,
  foodStatus: 'available',
  isActive: true,
  publishToNews: true,
  variants: [
    { variantId: 0, variantName: 'Phần Tiêu Chuẩn', price: 0, unit: 'Phần' }
  ]
});

// Sample food images for quick demonstration / one-click assignment
const sampleFoodImages = [
  { name: 'Bò Bít Tết', icon: '🥩', url: 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127415/qloder_foods/qmdaim2jdzjommucj27d.jpg' },
  { name: 'Ba Chỉ Nướng', icon: '🥓', url: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=700&auto=format&fit=crop&q=80' },
  { name: 'Gà Hấp Lá Chanh', icon: '🍗', url: 'https://images.unsplash.com/photo-1598103442097-8b74394b95c6?w=700&auto=format&fit=crop&q=80' },
  { name: 'Gà Nướng Muối Ớt', icon: '🔥', url: 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127669/qloder_foods/dea60vv3avxcndjr3zjz.jpg' },
  { name: 'Lẩu Thái Hải Sản', icon: '🍲', url: 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127718/qloder_foods/cmoj48uwhpunh34hhbwl.jpg' },
  { name: 'Lẩu Nấm Gà Ta', icon: '🍄', url: 'https://images.unsplash.com/photo-1547592180-85f173990554?w=700&auto=format&fit=crop&q=80' },
  { name: 'Tôm Sú Nướng', icon: '🦐', url: 'https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=700&auto=format&fit=crop&q=80' },
  { name: 'Mực Trứng Nướng', icon: '🦑', url: 'https://images.unsplash.com/photo-1599488615731-7e5c2823ff28?w=700&auto=format&fit=crop&q=80' },
  { name: 'Cơm Chiên Hải Sản', icon: '🍚', url: 'https://res.cloudinary.com/dvnfpynw5/image/upload/v1792127736/qloder_foods/glo97ts3b0g0wtyuqtec.jpg' },
  { name: 'Khoai Tây Chiên', icon: '🍟', url: 'https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=700&auto=format&fit=crop&q=80' },
  { name: 'Bia Heineken', icon: '🍺', url: 'https://images.unsplash.com/photo-1618886614638-80e3c15cd819?w=700&auto=format&fit=crop&q=80' },
  { name: 'Coca Cola', icon: '🥤', url: 'https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=700&auto=format&fit=crop&q=80' }
];

async function loadData() {
  isLoading.value = true;
  errorMessage.value = '';
  try {
    const [catRes, foodRes] = await Promise.all([
      axios.get(`${api}/category/list`),
      axios.get(`${api}/food/list`)
    ]);

    categories.value = catRes.data?.data ?? [];
    foods.value = foodRes.data?.data ?? [];
  } catch (e) {
    errorMessage.value = 'Lỗi kết nối máy chủ khi tải danh sách món ăn.';
  } finally {
    isLoading.value = false;
  }
}

const countActive = computed(() => foods.value.filter(f => f.isActive).length);
const countInactive = computed(() => foods.value.filter(f => !f.isActive).length);
const countWithImage = computed(() => foods.value.filter(f => f.imageUrl && f.imageUrl.trim() !== '').length);
const countWithoutImage = computed(() => foods.value.filter(f => !f.imageUrl || f.imageUrl.trim() === '').length);

function getCategoryCount(catId) {
  return foods.value.filter(f => f.categoryId === catId).length;
}

function getCategoryIcon(name) {
  if (!name) return '🍽️';
  if (name.includes('Uống') || name.includes('Pha Chế') || name.includes('Bia') || name.includes('Nước')) return '🍹';
  if (name.includes('Khai Vị') || name.includes('Gỏi') || name.includes('Salad')) return '🥗';
  if (name.includes('Nướng') || name.includes('BBQ') || name.includes('Chiên') || name.includes('Bò')) return '🥩';
  if (name.includes('Lẩu') || name.includes('Cơm') || name.includes('Canh')) return '🍲';
  if (name.includes('Hải Sản') || name.includes('Tôm') || name.includes('Mực')) return '🦐';
  return '🍽️';
}

function handleImageError(food) {
  food.imageUrl = '';
}

const filteredFoods = computed(() => {
  return foods.value.filter(f => {
    // 1. Trạng thái kinh doanh
    if (activeFilter.value !== null && f.isActive !== activeFilter.value) {
      return false;
    }
    // 2. Danh mục
    if (selectedCategory.value && f.categoryId !== Number(selectedCategory.value)) {
      return false;
    }
    // 3. Có ảnh / chưa có ảnh
    if (imageFilter.value === 'has_image' && (!f.imageUrl || f.imageUrl.trim() === '')) {
      return false;
    }
    if (imageFilter.value === 'no_image' && f.imageUrl && f.imageUrl.trim() !== '') {
      return false;
    }
    // 4. Từ khóa tìm kiếm
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase().trim();
      const matchName = f.foodName?.toLowerCase().includes(q);
      const matchDesc = f.description?.toLowerCase().includes(q);
      const matchCategory = f.categoryName?.toLowerCase().includes(q);
      const matchVariant = f.variants?.some(v => v.variantName?.toLowerCase().includes(q));
      if (!matchName && !matchDesc && !matchCategory && !matchVariant) return false;
    }
    return true;
  });
});

function resetFilters() {
  searchQuery.value = '';
  selectedCategory.value = '';
  activeFilter.value = null;
  imageFilter.value = 'all';
}

function openCreateModal() {
  isEditing.value = false;
  form.value = {
    foodId: 0,
    categoryId: categories.value[0]?.categoryId ?? '',
    foodName: '',
    description: '',
    imageUrl: '',
    minPrice: 0,
    foodStatus: 'available',
    isActive: true,
    publishToNews: true,
    variants: [
      { variantId: 0, variantName: 'Phần Tiêu Chuẩn', price: 0, unit: 'Phần' }
    ]
  };
  showModal.value = true;
}

function openEditModal(food) {
  isEditing.value = true;
  form.value = {
    foodId: food.foodId,
    categoryId: food.categoryId,
    foodName: food.foodName,
    description: food.description ?? '',
    imageUrl: food.imageUrl ?? '',
    minPrice: food.minPrice,
    foodStatus: food.foodStatus ?? 'available',
    isActive: food.isActive,
    publishToNews: isPublishedToNews(food.foodId),
    variants: (food.variants ?? []).map(v => ({
      variantId: v.variantId,
      variantName: v.variantName,
      price: v.price,
      unit: v.unit
    }))
  };

  if (!form.value.variants.length) {
    form.value.variants.push({ variantId: 0, variantName: 'Phần Tiêu Chuẩn', price: food.minPrice, unit: 'Phần' });
  }

  showModal.value = true;
}

function triggerFileInput() {
  fileInputRef.value?.click();
}

async function handleFileUpload(event) {
  const file = event.target.files?.[0];
  if (!file) return;

  const formData = new FormData();
  formData.append('file', file);

  isUploadingImage.value = true;
  errorMessage.value = '';

  try {
    const res = await axios.post(`${api}/food/upload-image`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    if (res.data?.url) {
      form.value.imageUrl = res.data.url;
      successMessage.value = 'Đã tải ảnh lên thành công!';
    }
  } catch (err) {
    errorMessage.value = err.response?.data?.message ?? 'Lỗi khi tải ảnh lên server.';
  } finally {
    isUploadingImage.value = false;
    event.target.value = ''; // Reset input
  }
}

function addVariantRow() {
  form.value.variants.push({
    variantId: 0,
    variantName: '',
    price: form.value.minPrice || 0,
    unit: 'Phần'
  });
}

async function removeVariantRow(index, variant) {
  if (variant && variant.variantId > 0) {
    if (!confirm(`Bạn có chắc muốn xóa vĩnh viễn biến thể "${variant.variantName}" này không?`)) return;
    try {
      await axios.delete(`${api}/food/variant/delete`, {
        params: { variantId: variant.variantId }
      });
      form.value.variants.splice(index, 1);
      successMessage.value = `Đã xóa biến thể "${variant.variantName}" thành công!`;
      await loadData();
    } catch (e) {
      errorMessage.value = e.response?.data?.message ?? 'Lỗi khi xóa biến thể.';
    }
  } else {
    form.value.variants.splice(index, 1);
  }
}

async function saveFood() {
  errorMessage.value = '';
  successMessage.value = '';
  isSubmitting.value = true;

  try {
    if (isEditing.value) {
      await axios.put(`${api}/food/update`, form.value);
      successMessage.value = `Đã cập nhật món "${form.value.foodName}" thành công!`;
    } else {
      await axios.post(`${api}/food/insert`, form.value);
      successMessage.value = `Đã thêm món "${form.value.foodName}" vào thực đơn thành công!`;
    }

    showModal.value = false;
    await loadData();

    // Update publish to news in localStorage
    const savedFood = foods.value.find(f => f.foodName === form.value.foodName) || { foodId: form.value.foodId };
    if (savedFood && savedFood.foodId) {
      const idx = publishedNewsFoodIds.value.indexOf(savedFood.foodId);
      if (form.value.publishToNews && idx === -1) {
        publishedNewsFoodIds.value.push(savedFood.foodId);
      } else if (!form.value.publishToNews && idx > -1) {
        publishedNewsFoodIds.value.splice(idx, 1);
      }
      localStorage.setItem(PUBLISHED_FOODS_KEY, JSON.stringify(publishedNewsFoodIds.value));
    }
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi lưu món ăn.';
  } finally {
    isSubmitting.value = false;
  }
}

async function stopSelling(id) {
  if (!confirm('Bạn có chắc muốn Ngừng Bán món này? Lịch sử hóa đơn vẫn được bảo toàn nguyên vẹn.')) return;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.delete(`${api}/food/delete`, { params: { foodId: id } });
    successMessage.value = 'Đã chuyển món sang trạng thái Ngừng Bán!';
    await loadData();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi ngừng bán món.';
  }
}

async function reactivateFood(id) {
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.patch(`${api}/food/reactivate`, null, { params: { foodId: id } });
    successMessage.value = 'Đã mở bán lại món ăn thành công!';
    await loadData();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi mở bán lại món.';
  }
}

function openImageLightbox(food) {
  if (food.imageUrl) {
    lightboxFood.value = food;
  }
}

function formatCurrency(amount) {
  if (!amount) return '0 ₫';
  return Number(amount).toLocaleString('vi-VN') + ' ₫';
}

onMounted(() => {
  loadPublishedNewsFoodIds();
  loadData();
});
</script>

<style scoped>
/* Page Layout */
.food-management-page {
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

.fw-black {
  font-weight: 900;
}

/* KPI Cards */
.kpi-metric-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  border: 1px solid #f1f5f9;
}
.kpi-metric-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.06) !important;
}
.kpi-icon-badge {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.15rem;
}

/* View Mode Toggle */
.view-toggle-group {
  border: 1px solid #e2e8f0;
}
.btn-white {
  background: #ffffff;
}

/* Search input */
.search-input-group .form-control:focus {
  background: #ffffff !important;
  box-shadow: none;
  border-color: #3b82f6;
}

/* Quick Chips */
.quick-chip {
  font-size: 0.78rem;
  font-weight: 700;
  padding: 4px 12px;
  transition: all 0.15s ease;
}

/* ============================================================== */
/* FOOD CARDS GRID (PRO UI)                                       */
/* ============================================================== */
.food-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
  margin-bottom: 24px;
}

.food-card-item {
  display: flex;
  flex-direction: column;
  transition: transform 0.25s ease, box-shadow 0.25s ease, border-color 0.25s ease;
}
.food-card-item:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 28px rgba(0, 0, 0, 0.08) !important;
  border-color: #cbd5e1;
}

/* Card Image Container */
.food-card-img-wrap {
  width: 100%;
  aspect-ratio: 16 / 10;
  position: relative;
  overflow: hidden;
  background: #f8fafc;
  cursor: pointer;
}

.food-card-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.4s ease;
}
.food-card-item:hover .food-card-img {
  transform: scale(1.06);
}

.img-hover-overlay {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  font-weight: 700;
  opacity: 0;
  transition: opacity 0.2s ease;
}
.food-card-img-wrap:hover .img-hover-overlay {
  opacity: 1;
}

/* Fallback placeholder */
.no-image-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
  color: #64748b;
  gap: 4px;
}
.placeholder-emoji {
  font-size: 2.8rem;
}
.placeholder-text {
  font-size: 0.75rem;
  font-weight: 700;
  color: #94a3b8;
}
.add-img-hint {
  font-size: 0.7rem;
  font-weight: 800;
  color: #2563eb;
}

/* Floating Badges */
.category-badge-pill {
  position: absolute;
  top: 10px;
  left: 10px;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(4px);
  color: #0f172a;
  font-size: 0.72rem;
  font-weight: 800;
  padding: 3px 10px;
  border-radius: 999px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
  z-index: 2;
}

.status-badge-pill {
  position: absolute;
  top: 10px;
  right: 10px;
  font-size: 0.72rem;
  font-weight: 800;
  padding: 3px 10px;
  border-radius: 999px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
  z-index: 2;
}
.status-active {
  background: #10b981;
  color: #ffffff;
}
.status-inactive {
  background: #64748b;
  color: #ffffff;
}

/* Card Body */
.food-card-body {
  flex: 1;
}
.food-name-clamp {
  display: -webkit-box;
  -webkit-line-clamp: 1;
  line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
  font-size: 1.05rem;
}
.food-desc-clamp {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  line-height: 1.4;
  min-height: 2.8em;
}

.variants-chips-wrap {
  min-height: 28px;
}
.variant-tag-chip {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  color: #475569;
  font-size: 0.72rem;
  padding: 2px 8px;
  border-radius: 6px;
  white-space: nowrap;
}

/* ============================================================== */
/* TABLE VIEW (THUMBNAIL STYLING)                                 */
/* ============================================================== */
.table-img-thumb {
  width: 58px;
  height: 58px;
  overflow: hidden;
  background: #f1f5f9;
  margin: 0 auto;
  border: 1px solid #e2e8f0;
}
.table-img-thumb img {
  transition: transform 0.2s ease;
}
.table-img-thumb:hover img {
  transform: scale(1.1);
}

/* ============================================================== */
/* MODAL STYLES                                                   */
/* ============================================================== */
.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.65);
  display: grid;
  place-items: center;
  z-index: 1050;
  backdrop-filter: blur(4px);
  padding: 16px;
}

.dialog {
  width: min(920px, calc(100vw - 2rem));
  max-height: 92vh;
  display: flex;
  flex-direction: column;
  border-radius: 20px;
}

.modal-body-scrollable {
  overflow-y: auto;
  max-height: calc(92vh - 140px);
}

/* Image Preview in Modal */
.modal-img-preview-box {
  aspect-ratio: 16 / 10;
  background: #f8fafc;
  position: relative;
}
.preview-img-actions {
  position: absolute;
  bottom: 10px;
  right: 10px;
}
.preview-placeholder-box {
  height: 100%;
}
.placeholder-big-icon {
  font-size: 2.5rem;
}

.uploading-overlay {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.75);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 5;
}

/* ============================================================== */
/* LIGHTBOX                                                       */
/* ============================================================== */
.lightbox-dialog {
  width: min(760px, 92vw);
}
.lightbox-img-wrap {
  width: 100%;
  max-height: 85vh;
  display: flex;
  flex-direction: column;
}
.lightbox-img {
  width: 100%;
  max-height: 70vh;
  object-fit: contain;
  background: #090d16;
}
.lightbox-caption {
  background: linear-gradient(to top, #090d16 0%, #1e293b 100%);
}
.lightbox-close-btn {
  position: absolute;
  top: 14px;
  right: 14px;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: #ffffff;
  font-size: 1.5rem;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 10;
  transition: background 0.2s ease;
}
.lightbox-close-btn:hover {
  background: rgba(220, 38, 38, 0.85);
}

/* Animations */
.spin-anim {
  display: inline-block;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.btn-xs {
  font-size: 0.72rem;
  padding: 2px 6px;
}
</style>
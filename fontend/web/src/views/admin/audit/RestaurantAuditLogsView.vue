<template>
  <div class="audit-page-wrapper p-3 p-md-4">
    <!-- 1. Header Khu Vực Điều Khiển -->
    <header class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4 pb-3 border-bottom border-slate-200">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge-subtle badge-indigo">
            <span class="dot-indicator bg-indigo"></span>
            Audit Trail & Management (UC-I04)
          </span>
          <span class="text-secondary small">Giám sát vận hành 3 phân nhóm trọng yếu</span>
        </div>
        <h2 class="page-title m-0 d-flex align-items-center gap-2">
          <span>📋</span>
          <span>Lịch Sử Thao Tác & Kiểm Toán Nhà Hàng</span>
        </h2>
        <p class="text-secondary small mb-0 mt-1">
          Theo dõi minh bạch mọi thay đổi về <strong>Hệ thống & Con người</strong>, <strong>Kho & Giá trị tiền hàng</strong>, và <strong>Khách hàng, Thực đơn & Quyết định Bếp</strong>.
        </p>
      </div>

      <!-- Action Tools -->
      <div class="d-flex flex-wrap align-items-center gap-2">
        <!-- Chuyển đổi chế độ xem: Dòng sự kiện (Feed) vs Bảng (Table) -->
        <div class="view-mode-toggle bg-white p-1 rounded-3 border border-slate-200 shadow-2xs d-flex">
          <button
            type="button"
            class="view-btn"
            :class="{ active: viewMode === 'feed' }"
            title="Dạng Dòng Hoạt Động (Trực quan, dễ đọc nhất)"
            @click="viewMode = 'feed'"
          >
            <span>📰</span>
            <span class="d-none d-sm-inline">Dòng sự kiện</span>
          </button>
          <button
            type="button"
            class="view-btn"
            :class="{ active: viewMode === 'table' }"
            title="Dạng Bảng Dữ Liệu (Gọn gàng, đối chiếu nhanh)"
            @click="viewMode = 'table'"
          >
            <span>📊</span>
            <span class="d-none d-sm-inline">Bảng chi tiết</span>
          </button>
        </div>

        <button
          type="button"
          class="btn btn-sm btn-white border border-slate-200 shadow-2xs d-flex align-items-center gap-1.5 px-3 py-2 fw-medium text-slate-700"
          :disabled="isLoading"
          @click="fetchData"
        >
          <span v-if="isLoading" class="spinner-border spinner-border-sm text-primary"></span>
          <span v-else>🔄</span>
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          class="btn btn-sm btn-primary-gradient shadow-xs d-flex align-items-center gap-1.5 px-3 py-2 fw-medium text-white"
          @click="exportCsv"
        >
          <span>📥</span>
          <span>Xuất CSV</span>
        </button>
      </div>
    </header>

    <!-- Alert lỗi -->
    <div v-if="errorMsg" class="alert alert-danger alert-dismissible fade show shadow-2xs mb-4 rounded-3 border-danger-subtle" role="alert">
      <strong>⚠️ Lỗi tải dữ liệu:</strong> {{ errorMsg }}
      <button type="button" class="btn-close" @click="errorMsg = ''"></button>
    </div>

    <!-- 2. Thẻ KPI Thống Kê Theo Đúng 3 Dạng Trọng Tâm -->
    <section class="row g-3 mb-4">
      <!-- 0. Tổng thao tác -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-card bg-white p-3 rounded-4 shadow-2xs border border-slate-200 h-100 cursor-pointer"
          :class="{ 'active-card-primary': filters.category === 'all' }"
          @click="changeCategory('all')"
        >
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="kpi-label text-slate-500">TỔNG NHẬT KÝ</span>
              <div class="kpi-number text-slate-900 mt-1">{{ summary.totalCount }}</div>
            </div>
            <div class="kpi-icon-box bg-slate-100 text-slate-700">📊</div>
          </div>
          <div class="kpi-footer text-slate-500 mt-2 small">
            Hôm nay: <strong class="text-slate-800">{{ summary.todayCount }}</strong> thao tác mới
          </div>
        </div>
      </div>

      <!-- DẠNG 1: Hệ Thống & Quản Trị Con Người -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-card bg-white p-3 rounded-4 shadow-2xs border border-slate-200 h-100 cursor-pointer"
          :class="{ 'active-card-indigo': filters.category === 'system' }"
          @click="changeCategory('system')"
        >
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="kpi-label text-indigo-700">⚙️ HỆ THỐNG & NHÂN SỰ</span>
              <div class="kpi-number text-indigo-900 mt-1">{{ summary.systemCount }}</div>
            </div>
            <div class="kpi-icon-box bg-indigo-50 text-indigo-600">👤</div>
          </div>
          <div class="kpi-footer text-slate-500 mt-2 small text-truncate" title="Admin cấp quyền, sửa, khóa/mở nhân viên">
            Admin quản trị tài khoản & bảo mật
          </div>
        </div>
      </div>

      <!-- DẠNG 2: Kho & Giá Trị Hàng Hóa -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-card bg-white p-3 rounded-4 shadow-2xs border border-slate-200 h-100 cursor-pointer"
          :class="{ 'active-card-amber': filters.category === 'inventory' }"
          @click="changeCategory('inventory')"
        >
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="kpi-label text-amber-700">📦 KHO & GIÁ TRỊ HÀNG</span>
              <div class="kpi-number text-amber-900 mt-1">{{ summary.inventoryCount }}</div>
            </div>
            <div class="kpi-icon-box bg-amber-50 text-amber-600">💰</div>
          </div>
          <div class="kpi-footer text-slate-500 mt-2 small text-truncate" title="Duyệt phiếu kho, xuất nhập, hao hụt kho">
            Phiếu kho, tồn kho & duyệt hao hụt
          </div>
        </div>
      </div>

      <!-- DẠNG 3: Khách Hàng, Thực Đơn & Vận Hành Bếp -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-card bg-white p-3 rounded-4 shadow-2xs border border-slate-200 h-100 cursor-pointer"
          :class="{ 'active-card-emerald': filters.category === 'customer' }"
          @click="changeCategory('customer')"
        >
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="kpi-label text-emerald-700">🍳 KHÁCH HÀNG & BẾP</span>
              <div class="kpi-number text-emerald-900 mt-1">{{ summary.customerCount }}</div>
            </div>
            <div class="kpi-icon-box bg-emerald-50 text-emerald-600">🍽️</div>
          </div>
          <div class="kpi-footer text-slate-500 mt-2 small text-truncate" title="Đặt bàn khách, đổi giá món, quyết định KDS Bếp">
            Đặt bàn, đổi giá món & KDS Bếp
          </div>
        </div>
      </div>
    </section>

    <!-- 3. Bộ Lọc Nghiệp Vụ Tinh Gọn (Single Clean Filter Bar) -->
    <section class="card border-0 shadow-2xs rounded-4 p-3 mb-4 bg-white border border-slate-200">
      <!-- Row 1: Segmented Pill Switcher cho 3 Dạng -->
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 pb-3 border-bottom border-slate-100 mb-3">
        <div class="category-pill-group">
          <button
            v-for="cat in categoryTabs"
            :key="cat.id"
            type="button"
            class="pill-tab-btn"
            :class="{ active: filters.category === cat.id }"
            @click="changeCategory(cat.id)"
          >
            <span>{{ cat.icon }}</span>
            <span>{{ cat.label }}</span>
            <span class="tab-count-badge">{{ cat.count ?? 0 }}</span>
          </button>
        </div>

        <div v-if="filters.category !== 'all'" class="text-slate-500 small d-none d-md-block">
          Đang xem: <strong class="text-slate-800">{{ getCategoryDescription(filters.category) }}</strong>
        </div>
      </div>

      <!-- Row 2: Bộ Tìm Kiếm & Các Dropdown Nhanh -->
      <div class="row g-2 align-items-center">
        <!-- Ô Tìm kiếm nhanh -->
        <div class="col-12 col-md-5 col-lg-4">
          <div class="search-input-box">
            <span class="search-icon">🔍</span>
            <input
              v-model="filters.search"
              type="text"
              class="form-control form-control-sm search-field"
              placeholder="Tìm nhân viên, món ăn, số tiền, lý do..."
              @keyup.enter="onFilterChange"
            />
            <button
              v-if="filters.search"
              type="button"
              class="clear-search-btn"
              title="Xóa tìm kiếm"
              @click="filters.search = ''; onFilterChange()"
            >
              ×
            </button>
          </div>
        </div>

        <!-- Lọc Nhân sự (Admin / Bếp / Quản lý) -->
        <div class="col-6 col-md-3 col-lg-2.5">
          <select v-model="filters.userId" class="form-select form-select-sm filter-select" @change="onFilterChange">
            <option :value="null">👥 Tất cả người thao tác</option>
            <option v-for="actor in actorsList" :key="actor.userId" :value="actor.userId">
              {{ actor.fullName }} ({{ actor.roleName }})
            </option>
          </select>
        </div>

        <!-- Lọc Đối tượng thực thể -->
        <div class="col-6 col-md-2 col-lg-2">
          <select v-model="filters.entityName" class="form-select form-select-sm filter-select" @change="onFilterChange">
            <option value="all">⚡ Tất cả đối tượng</option>
            <option value="User">👤 Nhân sự (Admin)</option>
            <option value="StockReceipt">📦 Phiếu kho hàng</option>
            <option value="Food">🍽️ Món ăn & Giá bán</option>
            <option value="Reservation">📅 Đặt bàn khách</option>
            <option value="DiningTable">🪑 Bàn ăn</option>
            <option value="OrderItem">🍳 Món order (Bếp)</option>
          </select>
        </div>

        <!-- Khoảng ngày Từ - Đến -->
        <div class="col-6 col-md-2 col-lg-1.5">
          <input
            v-model="filters.fromDate"
            type="date"
            class="form-control form-control-sm filter-date"
            title="Từ ngày"
            @change="onFilterChange"
          />
        </div>
        <div class="col-6 col-md-2 col-lg-1.5">
          <input
            v-model="filters.toDate"
            type="date"
            class="form-control form-control-sm filter-date"
            title="Đến ngày"
            @change="onFilterChange"
          />
        </div>

        <!-- Nút Reset bộ lọc -->
        <div v-if="hasActiveFilters" class="col-12 col-lg-auto ms-lg-auto">
          <button
            type="button"
            class="btn btn-sm btn-light border text-secondary px-2.5 py-1 small d-flex align-items-center gap-1"
            @click="resetFilters"
          >
            <span>↺</span>
            <span>Đặt lại</span>
          </button>
        </div>
      </div>
    </section>

    <!-- 4. KHU VỰC HIỂN THỊ DỮ LIỆU: FEED HOẶC TABLE -->

    <!-- ============================================================== -->
    <!-- CHẾ ĐỘ 1: DÒNG HOẠT ĐỘNG (FEED / TIMELINE MODE) - ĐẸP & DỄ ĐỌC -->
    <!-- ============================================================== -->
    <div v-if="viewMode === 'feed'" class="activity-feed-container mb-4">
      <!-- Loading Skeleton -->
      <div v-if="isLoading" class="d-flex flex-column gap-3">
        <div v-for="i in 4" :key="'feed-skel-' + i" class="feed-item-card bg-white p-3 rounded-4 shadow-2xs border border-slate-200 placeholder-glow">
          <span class="placeholder col-6 mb-2 rounded"></span>
          <span class="placeholder col-10 rounded"></span>
        </div>
      </div>

      <!-- Không có dữ liệu -->
      <div v-else-if="auditLogs.length === 0" class="empty-state-box bg-white p-5 rounded-4 shadow-2xs border border-slate-200 text-center">
        <div class="fs-1 mb-2">🌿</div>
        <h5 class="fw-bold text-slate-800">Không tìm thấy bản ghi nhật ký nào</h5>
        <p class="text-slate-500 small mb-3">Chưa có thao tác nào phù hợp với bộ lọc hiện tại.</p>
        <button v-if="hasActiveFilters" type="button" class="btn btn-sm btn-outline-primary" @click="resetFilters">
          Xóa toàn bộ bộ lọc
        </button>
      </div>

      <!-- Danh sách thẻ hoạt động -->
      <div v-else class="d-flex flex-column gap-2.5">
        <div
          v-for="log in auditLogs"
          :key="log.auditLogId"
          class="feed-card bg-white rounded-3 p-3 shadow-2xs border border-slate-200 hover-lift d-flex flex-column flex-md-row align-items-md-center justify-content-between gap-3"
        >
          <!-- Left: Phân loại icon + Nội dung chính -->
          <div class="d-flex align-items-start gap-3 flex-grow-1">
            <!-- Icon đại diện 3 Dạng -->
            <div class="category-icon-circle flex-shrink-0" :class="getCategoryIconClass(log.category)">
              {{ log.categoryIcon }}
            </div>

            <div class="flex-grow-1">
              <!-- Top Row: Người làm + Vai trò + Phân loại badge + Trọng tâm giá trị -->
              <div class="d-flex flex-wrap align-items-center gap-1.5 mb-1">
                <span class="fw-bold text-slate-900 actor-name">{{ log.userName }}</span>
                <span class="badge-role" :class="getRoleBadgeClass(log.userRole)">
                  {{ log.userRole }}
                </span>

                <span class="badge-cat" :class="getCategoryPillClass(log.category)">
                  {{ log.categoryLabel }}
                </span>

                <span class="badge-action" :class="getActionBadgeClass(log.actionSeverity)">
                  {{ log.actionBadge }}
                </span>

                <!-- Trọng tâm Giá trị / Nhân sự / Quyết định Bếp -->
                <span v-if="log.valueHighlight" class="highlight-chip" :class="getHighlightChipClass(log.valueHighlightType)">
                  <span v-if="log.valueHighlightType === 'money'">💰</span>
                  <span v-else-if="log.valueHighlightType === 'human'">👤</span>
                  <span v-else-if="log.valueHighlightType === 'alert'">⚠️</span>
                  <span>{{ log.valueHighlight }}</span>
                </span>
              </div>

              <!-- Main Event Text -->
              <div class="event-summary text-slate-700">
                {{ log.summary }}
              </div>
            </div>
          </div>

          <!-- Right: Thời gian + Nút xem chi tiết -->
          <div class="d-flex align-items-center justify-content-between justify-content-md-end gap-3 flex-shrink-0 border-top border-md-0 pt-2 pt-md-0">
            <div class="text-md-end">
              <div class="text-nowrap small text-slate-500 font-monospace">
                {{ timeAgo(log.createdAt) }}
              </div>
              <small class="text-slate-400 font-monospace" style="font-size: 0.72rem;">
                {{ formatDateTime(log.createdAt) }}
              </small>
            </div>

            <button
              type="button"
              class="btn-diff-action"
              title="Xem đối chiếu dữ liệu trước & sau"
              @click="openDiffModal(log)"
            >
              <span>👁️</span>
              <span>Chi tiết</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- CHẾ ĐỘ 2: BẢNG TINH GỌN (COMPACT MODERN TABLE)                 -->
    <!-- ============================================================== -->
    <div v-else class="card border-0 shadow-2xs rounded-4 overflow-hidden bg-white border border-slate-200 mb-4">
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0 modern-table">
          <thead>
            <tr>
              <th scope="col" style="width: 170px;">Thời gian</th>
              <th scope="col" style="width: 220px;">Người thực hiện</th>
              <th scope="col">Hoạt động & Trọng tâm nghiệp vụ</th>
              <th scope="col" class="text-center" style="width: 100px;">Đối chiếu</th>
            </tr>
          </thead>
          <tbody>
            <!-- Skeleton Loading -->
            <tr v-if="isLoading" v-for="i in 5" :key="'table-skel-' + i">
              <td colspan="4" class="py-3 text-center text-muted">
                <div class="placeholder-glow">
                  <span class="placeholder col-12 rounded"></span>
                </div>
              </td>
            </tr>

            <!-- Không có dữ liệu -->
            <tr v-else-if="auditLogs.length === 0">
              <td colspan="4" class="text-center py-5 text-muted">
                <div class="fs-2 mb-2">🌿</div>
                <div class="fw-bold text-slate-700">Không có dữ liệu nhật ký phù hợp</div>
                <small class="text-slate-400">Thử chọn tab phân loại khác hoặc xóa bộ lọc tìm kiếm</small>
              </td>
            </tr>

            <!-- Dòng dữ liệu tinh gọn -->
            <tr v-else v-for="log in auditLogs" :key="'tbl-' + log.auditLogId" class="modern-row">
              <!-- 1. Thời gian -->
              <td>
                <div class="fw-semibold text-slate-800 small text-nowrap font-monospace">
                  {{ formatDateTime(log.createdAt) }}
                </div>
                <div class="text-slate-400 small" style="font-size: 0.72rem;">
                  {{ timeAgo(log.createdAt) }}
                </div>
              </td>

              <!-- 2. Người thực hiện -->
              <td>
                <div class="d-flex align-items-center gap-2">
                  <div class="avatar-circle-sm" :class="getRoleColorClass(log.userRole)">
                    {{ getInitials(log.userName) }}
                  </div>
                  <div class="overflow-hidden">
                    <div class="fw-bold text-slate-900 text-truncate small" :title="log.userName">
                      {{ log.userName }}
                    </div>
                    <span class="badge-role" :class="getRoleBadgeClass(log.userRole)">
                      {{ log.userRole }}
                    </span>
                  </div>
                </div>
              </td>

              <!-- 3. Hoạt động & Trọng tâm nghiệp vụ (Gộp thông minh) -->
              <td>
                <!-- Dòng trên: Dạng + Hành động + Highlight -->
                <div class="d-flex flex-wrap align-items-center gap-1.5 mb-1">
                  <span class="badge-cat" :class="getCategoryPillClass(log.category)">
                    {{ log.categoryIcon }} {{ log.categoryLabel }}
                  </span>

                  <span class="badge-action" :class="getActionBadgeClass(log.actionSeverity)">
                    {{ log.actionBadge }}
                  </span>

                  <span v-if="log.valueHighlight" class="highlight-chip" :class="getHighlightChipClass(log.valueHighlightType)">
                    <span v-if="log.valueHighlightType === 'money'">💰</span>
                    <span v-else-if="log.valueHighlightType === 'human'">👤</span>
                    <span v-else-if="log.valueHighlightType === 'alert'">⚠️</span>
                    <span>{{ log.valueHighlight }}</span>
                  </span>
                </div>

                <!-- Dòng dưới: Diễn giải chi tiết -->
                <div class="event-summary text-slate-700 small">
                  {{ log.summary }}
                </div>
              </td>

              <!-- 4. Chi tiết Diff -->
              <td class="text-center">
                <button
                  type="button"
                  class="btn-diff-action"
                  title="Xem đối chiếu trước & sau"
                  @click="openDiffModal(log)"
                >
                  <span>👁️</span>
                  <span>Xem</span>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 5. Phân Trang Tinh Gọn (Pagination Footer) -->
    <footer v-if="pagination.totalPages > 1" class="d-flex flex-wrap justify-content-between align-items-center p-3 rounded-4 bg-white shadow-2xs border border-slate-200 gap-2 mb-4">
      <div class="small text-slate-500">
        Hiển thị trang <strong>{{ pagination.currentPage }}</strong> / <strong>{{ pagination.totalPages }}</strong> (Tổng cộng <strong>{{ pagination.totalItems }}</strong> thao tác)
      </div>

      <nav aria-label="Phân trang nhật ký">
        <ul class="pagination pagination-sm m-0 gap-1">
          <li class="page-item" :class="{ disabled: pagination.currentPage <= 1 }">
            <button class="page-link rounded-2" @click="changePage(pagination.currentPage - 1)">«</button>
          </li>
          <li
            v-for="p in visiblePages"
            :key="p"
            class="page-item"
            :class="{ active: p === pagination.currentPage }"
          >
            <button class="page-link rounded-2" @click="changePage(p)">{{ p }}</button>
          </li>
          <li class="page-item" :class="{ disabled: pagination.currentPage >= pagination.totalPages }">
            <button class="page-link rounded-2" @click="changePage(pagination.currentPage + 1)">»</button>
          </li>
        </ul>
      </nav>
    </footer>

    <!-- 6. Modal So Sánh Dữ Liệu Trước & Sau (Audit Diff Modal) -->
    <div
      v-if="activeLog"
      class="modal fade show d-block"
      style="background: rgba(15, 23, 42, 0.7); backdrop-filter: blur(6px);"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
    >
      <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable">
        <div class="modal-content border-0 shadow-2xl rounded-4 overflow-hidden">
          <!-- Modal Header -->
          <div class="modal-header bg-slate-900 text-white p-3 px-4 border-0">
            <div>
              <div class="d-flex align-items-center gap-2 mb-1">
                <span class="badge bg-indigo-500 text-white px-2 py-0.5 rounded-pill small">
                  {{ activeLog.categoryIcon }} {{ activeLog.categoryLabel }}
                </span>
                <span class="text-slate-400 small font-monospace">Log #{{ activeLog.auditLogId }} • {{ formatDateTime(activeLog.createdAt) }}</span>
              </div>
              <h5 class="modal-title fw-bold text-white m-0 d-flex align-items-center gap-2">
                <span>{{ activeLog.actionBadge }} trên {{ activeLog.entityLabel }} #{{ activeLog.entityId }}</span>
              </h5>
            </div>
            <button type="button" class="btn-close btn-close-white" aria-label="Đóng" @click="activeLog = null"></button>
          </div>

          <!-- Modal Body -->
          <div class="modal-body p-4 bg-slate-50">
            <!-- Metadata Strip -->
            <div class="card border-0 shadow-2xs rounded-3 p-3 bg-white mb-3 border border-slate-200">
              <div class="row g-2">
                <div class="col-md-3">
                  <div class="text-slate-400 small">Người thực hiện:</div>
                  <strong class="text-slate-900">{{ activeLog.userName }}</strong>
                  <span class="badge ms-1.5" :class="getRoleBadgeClass(activeLog.userRole)">{{ activeLog.userRole }}</span>
                </div>
                <div class="col-md-3">
                  <div class="text-slate-400 small">Phân loại dạng:</div>
                  <strong class="text-slate-800">{{ activeLog.categoryIcon }} {{ activeLog.categoryLabel }}</strong>
                </div>
                <div class="col-md-3">
                  <div class="text-slate-400 small">Hành động hệ thống:</div>
                  <code class="text-indigo-600 fw-bold">{{ activeLog.actionName }}</code>
                </div>
                <div class="col-md-3">
                  <div class="text-slate-400 small">Trọng tâm giá trị:</div>
                  <span v-if="activeLog.valueHighlight" class="highlight-chip" :class="getHighlightChipClass(activeLog.valueHighlightType)">
                    {{ activeLog.valueHighlight }}
                  </span>
                  <span v-else class="text-slate-400 small">-</span>
                </div>
              </div>
              <div class="mt-2 pt-2 border-top border-slate-100">
                <div class="text-slate-400 small">Tóm tắt sự kiện:</div>
                <div class="fw-semibold text-slate-800">{{ activeLog.summary }}</div>
              </div>
            </div>

            <!-- Two-Column Diff Comparison -->
            <div class="row g-3">
              <!-- Cột Trái: Dữ liệu cũ (Old Data) -->
              <div class="col-12 col-md-6">
                <div class="diff-panel old-diff-panel shadow-2xs rounded-3 p-3 bg-white h-100 border border-slate-200">
                  <div class="d-flex align-items-center justify-content-between mb-2 pb-2 border-bottom border-slate-100">
                    <span class="fw-bold text-danger d-flex align-items-center gap-1.5">
                      <span>🔴</span>
                      <span>Dữ Liệu Trước Thay Đổi (Old Data)</span>
                    </span>
                    <span v-if="!activeLog.oldData" class="badge bg-secondary-subtle text-secondary small">Không có</span>
                  </div>

                  <div v-if="!activeLog.oldData" class="text-slate-400 small py-4 text-center fst-italic">
                    (Thao tác thêm mới ban đầu, không có dữ liệu tiền nhiệm)
                  </div>
                  <div v-else class="json-code-box">
                    <pre class="m-0 font-monospace small">{{ formatJson(activeLog.oldData) }}</pre>
                  </div>
                </div>
              </div>

              <!-- Cột Phải: Dữ liệu mới (New Data) -->
              <div class="col-12 col-md-6">
                <div class="diff-panel new-diff-panel shadow-2xs rounded-3 p-3 bg-white h-100 border border-slate-200">
                  <div class="d-flex align-items-center justify-content-between mb-2 pb-2 border-bottom border-slate-100">
                    <span class="fw-bold text-success d-flex align-items-center gap-1.5">
                      <span>🟢</span>
                      <span>Dữ Liệu Sau Thay Đổi (New Data)</span>
                    </span>
                    <span v-if="!activeLog.newData" class="badge bg-secondary-subtle text-secondary small">Không có</span>
                  </div>

                  <div v-if="!activeLog.newData" class="text-slate-400 small py-4 text-center fst-italic">
                    (Thao tác xóa vĩnh viễn, không còn dữ liệu mới)
                  </div>
                  <div v-else class="json-code-box">
                    <pre class="m-0 font-monospace small">{{ formatJson(activeLog.newData) }}</pre>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Modal Footer -->
          <div class="modal-footer bg-white border-top border-slate-100 p-3 px-4">
            <button type="button" class="btn btn-secondary btn-sm px-4 fw-medium" @click="activeLog = null">
              Đóng cửa sổ
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import axios from 'axios';
import signalRService from '@/services/signalr';

const API_BASE = 'http://localhost:5079/api/admin/audit-logs';

const isLoading = ref(false);
const errorMsg = ref('');
const auditLogs = ref([]);
const actorsList = ref([]);
const activeLog = ref(null);
const viewMode = ref('feed'); // 'feed' (dòng sự kiện) hoặc 'table' (bảng)

const summary = reactive({
  totalCount: 0,
  todayCount: 0,
  systemCount: 0,
  inventoryCount: 0,
  customerCount: 0
});

const pagination = reactive({
  currentPage: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 1
});

const filters = reactive({
  category: 'all', // all | system | inventory | customer
  entityName: 'all',
  userId: null,
  fromDate: '',
  toDate: '',
  search: ''
});

// 3 Dạng Chính được phân loại rõ ràng theo yêu cầu
const categoryTabs = computed(() => [
  { id: 'all', label: 'Tất Cả Thao Tác', icon: '⚡', count: summary.totalCount },
  { id: 'system', label: 'Hệ Thống & Nhân Sự', icon: '⚙️', count: summary.systemCount },
  { id: 'inventory', label: 'Kho & Giá Trị Hàng', icon: '📦', count: summary.inventoryCount },
  { id: 'customer', label: 'Khách Hàng & Bếp', icon: '🍳', count: summary.customerCount }
]);

const hasActiveFilters = computed(() => {
  return (
    filters.category !== 'all' ||
    filters.entityName !== 'all' ||
    filters.userId !== null ||
    filters.fromDate !== '' ||
    filters.toDate !== '' ||
    filters.search.trim() !== ''
  );
});

const visiblePages = computed(() => {
  const pages = [];
  const current = pagination.currentPage;
  const total = pagination.totalPages;
  const start = Math.max(1, current - 2);
  const end = Math.min(total, current + 2);
  for (let i = start; i <= end; i++) {
    pages.push(i);
  }
  return pages;
});

function getCategoryDescription(cat) {
  switch (cat) {
    case 'system': return 'Hệ Thống & Nhân Sự (Admin cấp quyền, sửa, khóa/mở tài khoản)';
    case 'inventory': return 'Kho Hàng & Giá Trị Hàng Hóa (Phiếu kho, duyệt nhập xuất, hủy hao hụt)';
    case 'customer': return 'Khách Hàng, Thực Đơn & Bếp (Đặt bàn, giá món, quyết định KDS Bếp)';
    default: return 'Toàn bộ thao tác kiểm toán';
  }
}

async function fetchData() {
  isLoading.value = true;
  errorMsg.value = '';

  try {
    const params = {
      page: pagination.currentPage,
      pageSize: pagination.pageSize,
      category: filters.category,
      entityName: filters.entityName,
      userId: filters.userId,
      fromDate: filters.fromDate || undefined,
      toDate: filters.toDate || undefined,
      search: filters.search.trim() || undefined
    };

    const [logsRes, summaryRes, actorsRes] = await Promise.all([
      axios.get(API_BASE, { params }),
      axios.get(`${API_BASE}/summary`),
      actorsList.value.length === 0 ? axios.get(`${API_BASE}/actors`) : Promise.resolve({ data: { data: actorsList.value } })
    ]);

    if (logsRes.data && logsRes.data.success) {
      auditLogs.value = logsRes.data.data || [];
      if (logsRes.data.pagination) {
        pagination.currentPage = logsRes.data.pagination.currentPage;
        pagination.totalItems = logsRes.data.pagination.totalItems;
        pagination.totalPages = logsRes.data.pagination.totalPages;
      }
    }

    if (summaryRes.data && summaryRes.data.success) {
      Object.assign(summary, summaryRes.data);
    }

    if (actorsRes.data && actorsRes.data.data) {
      actorsList.value = actorsRes.data.data;
    }
  } catch (err) {
    console.error('Lỗi khi tải lịch sử kiểm toán:', err);
    errorMsg.value = err.response?.data?.message || err.message || 'Không thể kết nối đến máy chủ API.';
  } finally {
    isLoading.value = false;
  }
}

function onFilterChange() {
  pagination.currentPage = 1;
  fetchData();
}

function changeCategory(catId) {
  filters.category = catId;
  onFilterChange();
}

function resetFilters() {
  filters.category = 'all';
  filters.entityName = 'all';
  filters.userId = null;
  filters.fromDate = '';
  filters.toDate = '';
  filters.search = '';
  onFilterChange();
}

function changePage(page) {
  if (page < 1 || page > pagination.totalPages) return;
  pagination.currentPage = page;
  fetchData();
}

function openDiffModal(log) {
  activeLog.value = log;
}

function formatJson(raw) {
  if (!raw) return '';
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return JSON.stringify(parsed, null, 2);
  } catch {
    return raw;
  }
}

function formatDateTime(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
}

function timeAgo(iso) {
  if (!iso) return '';
  const diffSec = Math.floor((new Date() - new Date(iso)) / 1000);
  if (diffSec < 60) return 'Vừa xong';
  if (diffSec < 3600) return `${Math.floor(diffSec / 60)} phút trước`;
  if (diffSec < 86400) return `${Math.floor(diffSec / 3600)} giờ trước`;
  return `${Math.floor(diffSec / 86400)} ngày trước`;
}

function getInitials(name) {
  if (!name) return 'HT';
  const parts = name.trim().split(' ');
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function getRoleColorClass(role) {
  const r = (role || '').toLowerCase();
  if (r.includes('admin')) return 'avatar-admin';
  if (r.includes('manager') || r.includes('quản')) return 'avatar-manager';
  if (r.includes('chef') || r.includes('bếp')) return 'avatar-chef';
  return 'avatar-staff';
}

function getRoleBadgeClass(role) {
  const r = (role || '').toLowerCase();
  if (r.includes('admin')) return 'role-badge-admin';
  if (r.includes('manager') || r.includes('quản')) return 'role-badge-manager';
  if (r.includes('chef') || r.includes('bếp')) return 'role-badge-chef';
  return 'role-badge-staff';
}

function getCategoryIconClass(category) {
  switch (category) {
    case 'system': return 'cat-icon-system';
    case 'inventory': return 'cat-icon-inventory';
    case 'customer': return 'cat-icon-customer';
    default: return 'cat-icon-default';
  }
}

function getCategoryPillClass(category) {
  switch (category) {
    case 'system': return 'cat-pill-system';
    case 'inventory': return 'cat-pill-inventory';
    case 'customer': return 'cat-pill-customer';
    default: return 'cat-pill-default';
  }
}

function getActionBadgeClass(severity) {
  switch (severity) {
    case 'success': return 'action-badge-success';
    case 'danger': return 'action-badge-danger';
    case 'warning': return 'action-badge-warning';
    case 'purple': return 'action-badge-purple';
    default: return 'action-badge-info';
  }
}

function getHighlightChipClass(type) {
  switch (type) {
    case 'money': return 'chip-money';
    case 'human': return 'chip-human';
    case 'alert': return 'chip-alert';
    default: return 'chip-info';
  }
}

function exportCsv() {
  if (auditLogs.value.length === 0) {
    alert('Không có dữ liệu để xuất file.');
    return;
  }

  const headers = ['Mã Log', 'Thời gian', 'Người thực hiện', 'Email', 'Vai trò', 'Dạng phân loại', 'Hành động', 'Đối tượng', 'Mã đối tượng', 'Trọng tâm giá trị', 'Tóm tắt nội dung'];
  const rows = auditLogs.value.map(l => [
    l.auditLogId,
    formatDateTime(l.createdAt),
    `"${(l.userName || '').replace(/"/g, '""')}"`,
    `"${(l.userEmail || '').replace(/"/g, '""')}"`,
    l.userRole,
    l.categoryLabel,
    l.actionBadge,
    l.entityLabel,
    l.entityId || '',
    `"${(l.valueHighlight || '').replace(/"/g, '""')}"`,
    `"${(l.summary || '').replace(/"/g, '""')}"`
  ]);

  const csvContent = '\uFEFF' + [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.setAttribute('href', url);
  link.setAttribute('download', `Lich_Su_Kiem_Toan_${new Date().toISOString().slice(0, 10)}.csv`);
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

// Realtime SignalR handler
function handleAuditLogCreated() {
  summary.totalCount++;
  summary.todayCount++;
  if (pagination.currentPage === 1) {
    fetchData();
  }
}

onMounted(() => {
  fetchData();
  signalRService.on('AuditLogCreated', handleAuditLogCreated);
});

onUnmounted(() => {
  signalRService.off('AuditLogCreated', handleAuditLogCreated);
});
</script>

<style scoped>
/* Base Page & Variables */
.audit-page-wrapper {
  background-color: #f8fafc;
  min-height: 100vh;
  color: #1e293b;
  font-family: inherit;
}

.page-title {
  font-size: 1.45rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: #0f172a;
}

/* Subtle Badges */
.badge-subtle {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.72rem;
  font-weight: 600;
  padding: 0.25rem 0.65rem;
  border-radius: 9999px;
}
.badge-indigo {
  background: #eef2ff;
  color: #4338ca;
  border: 1px solid #e0e7ff;
}
.dot-indicator {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}
.bg-indigo { background-color: #4f46e5; }

/* View Mode Switcher */
.view-mode-toggle {
  display: flex;
  gap: 2px;
}
.view-btn {
  border: none;
  background: transparent;
  padding: 0.35rem 0.75rem;
  border-radius: 6px;
  font-size: 0.8rem;
  font-weight: 500;
  color: #64748b;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: all 0.15s ease;
}
.view-btn.active {
  background: #0f172a;
  color: #ffffff;
  font-weight: 600;
}

/* Gradient Primary Button */
.btn-primary-gradient {
  background: linear-gradient(135deg, #4f46e5 0%, #3b82f6 100%);
  border: none;
  border-radius: 8px;
  transition: all 0.2s ease;
}
.btn-primary-gradient:hover {
  background: linear-gradient(135deg, #4338ca 0%, #2563eb 100%);
  transform: translateY(-1px);
}

/* KPI Cards */
.kpi-card {
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  position: relative;
}
.kpi-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 20px -5px rgba(0, 0, 0, 0.05);
}
.active-card-primary {
  border-color: #3b82f6 !important;
  background-color: #f8faff !important;
  outline: 2px solid #3b82f6;
}
.active-card-indigo {
  border-color: #4f46e5 !important;
  background-color: #faf5ff !important;
  outline: 2px solid #4f46e5;
}
.active-card-amber {
  border-color: #d97706 !important;
  background-color: #fffbeb !important;
  outline: 2px solid #d97706;
}
.active-card-emerald {
  border-color: #059669 !important;
  background-color: #f0fdf4 !important;
  outline: 2px solid #059669;
}

.kpi-label {
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}
.kpi-number {
  font-size: 1.85rem;
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -0.03em;
}
.kpi-icon-box {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  font-size: 1.25rem;
  flex-shrink: 0;
}

/* Segmented Pill Tabs */
.category-pill-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
.pill-tab-btn {
  border: 1px solid #e2e8f0;
  background: #f8fafc;
  color: #475569;
  font-size: 0.82rem;
  font-weight: 600;
  padding: 0.4rem 0.85rem;
  border-radius: 9999px;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  cursor: pointer;
  transition: all 0.15s ease;
}
.pill-tab-btn:hover {
  background: #f1f5f9;
  color: #1e293b;
}
.pill-tab-btn.active {
  background: #0f172a;
  color: #ffffff;
  border-color: #0f172a;
  box-shadow: 0 4px 6px -1px rgba(15, 23, 42, 0.15);
}
.tab-count-badge {
  font-size: 0.68rem;
  padding: 0.1rem 0.45rem;
  border-radius: 9999px;
  background: rgba(0, 0, 0, 0.08);
}
.pill-tab-btn.active .tab-count-badge {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

/* Search Box */
.search-input-box {
  position: relative;
  display: flex;
  align-items: center;
}
.search-icon {
  position: absolute;
  left: 0.75rem;
  font-size: 0.85rem;
  pointer-events: none;
  opacity: 0.6;
}
.search-field {
  padding-left: 2.25rem;
  padding-right: 2rem;
  border-radius: 9999px;
  border: 1px solid #cbd5e1;
  background: #ffffff;
  font-size: 0.82rem;
}
.search-field:focus {
  border-color: #4f46e5;
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.15);
}
.clear-search-btn {
  position: absolute;
  right: 0.75rem;
  border: none;
  background: none;
  font-size: 1rem;
  color: #94a3b8;
  cursor: pointer;
}
.clear-search-btn:hover {
  color: #1e293b;
}

.filter-select, .filter-date {
  border-radius: 8px;
  border: 1px solid #cbd5e1;
  font-size: 0.82rem;
}

/* Feed Mode Cards */
.hover-lift {
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.hover-lift:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 12px -2px rgba(15, 23, 42, 0.06);
}

.category-icon-circle {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  font-size: 1.35rem;
}
.cat-icon-system { background: #eef2ff; color: #4338ca; }
.cat-icon-inventory { background: #fef3c7; color: #b45309; }
.cat-icon-customer { background: #ecfdf5; color: #047857; }
.cat-icon-default { background: #f1f5f9; color: #475569; }

.actor-name {
  font-size: 0.88rem;
  letter-spacing: -0.01em;
}

/* Micro Badges */
.badge-role {
  font-size: 0.65rem;
  font-weight: 600;
  padding: 0.15rem 0.45rem;
  border-radius: 4px;
}
.role-badge-admin { background: #fee2e2; color: #991b1b; }
.role-badge-manager { background: #e0e7ff; color: #3730a3; }
.role-badge-chef { background: #ccfbf1; color: #115e59; }
.role-badge-staff { background: #f1f5f9; color: #475569; }

.badge-cat {
  font-size: 0.68rem;
  font-weight: 600;
  padding: 0.15rem 0.5rem;
  border-radius: 9999px;
}
.cat-pill-system { background: #f5f3ff; color: #6d28d9; border: 1px solid #ddd6fe; }
.cat-pill-inventory { background: #fffbeb; color: #b45309; border: 1px solid #fde68a; }
.cat-pill-customer { background: #ecfdf5; color: #047857; border: 1px solid #a7f3d0; }

.badge-action {
  font-size: 0.68rem;
  font-weight: 600;
  padding: 0.15rem 0.5rem;
  border-radius: 6px;
}
.action-badge-success { background: #dcfce7; color: #15803d; }
.action-badge-danger { background: #fee2e2; color: #b91c1c; }
.action-badge-warning { background: #fef3c7; color: #b45309; }
.action-badge-purple { background: #f3e8ff; color: #7e22ce; }
.action-badge-info { background: #e0f2fe; color: #0369a1; }

/* Value Highlight Chips */
.highlight-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.72rem;
  font-weight: 700;
  padding: 0.15rem 0.5rem;
  border-radius: 6px;
}
.chip-money {
  background: #ecfdf5;
  color: #047857;
  border: 1px solid #6ee7b7;
}
.chip-human {
  background: #fff1f2;
  color: #be123c;
  border: 1px solid #fecdd3;
}
.chip-alert {
  background: #fffbeb;
  color: #b45309;
  border: 1px solid #fde68a;
}
.chip-info {
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #bfdbfe;
}

.event-summary {
  font-size: 0.85rem;
  line-height: 1.45;
  color: #334155;
}

/* Action Button */
.btn-diff-action {
  border: 1px solid #c7d2fe;
  background: #f5f3ff;
  color: #4338ca;
  font-size: 0.76rem;
  font-weight: 600;
  padding: 0.35rem 0.75rem;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-diff-action:hover {
  background: #4f46e5;
  color: #ffffff;
  border-color: #4f46e5;
  transform: translateY(-1px);
}

/* Modern Table */
.modern-table thead th {
  background-color: #f8fafc;
  color: #64748b;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid #e2e8f0;
}
.modern-table tbody td {
  padding: 0.85rem 1rem;
  border-bottom: 1px solid #f1f5f9;
}
.modern-row:hover {
  background-color: #f8fafc;
}

.avatar-circle-sm {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 700;
  font-size: 0.72rem;
  flex-shrink: 0;
}
.avatar-admin { background: #fee2e2; color: #991b1b; }
.avatar-manager { background: #e0e7ff; color: #3730a3; }
.avatar-chef { background: #ccfbf1; color: #115e59; }
.avatar-staff { background: #f1f5f9; color: #475569; }

/* Diff Modal Styling */
.diff-panel {
  border: 1px solid #e2e8f0;
}
.old-diff-panel { border-left: 4px solid #ef4444; }
.new-diff-panel { border-left: 4px solid #10b981; }

.json-code-box {
  background: #0f172a;
  color: #e2e8f0;
  padding: 1rem;
  border-radius: 8px;
  max-height: 400px;
  overflow-y: auto;
}
.json-code-box pre {
  white-space: pre-wrap;
  word-break: break-all;
}

/* Utility Shadows */
.shadow-2xs {
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.04);
}
</style>

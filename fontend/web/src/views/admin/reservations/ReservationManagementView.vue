<template>
  <div class="reservation-view">
    <!-- Top Header -->
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3 mb-4">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1 flex-wrap">
          <span class="badge rounded-pill bg-primary-subtle text-primary border border-primary-subtle px-3 py-1 fw-bold fs-7">
            ⭐ QUẢN TRỊ ĐẶT BÀN & LỊCH HẸN
          </span>
          <span class="badge rounded-pill bg-success-subtle text-success border border-success-subtle px-2 py-1 fw-bold small d-inline-flex align-items-center gap-1">
            <span class="pulse-dot"></span> SignalR Realtime Auto
          </span>
        </div>
        <h3 class="fw-black text-dark mb-1 d-flex align-items-center gap-2">
          <span>📅</span> Quản Lý Đặt Bàn & Điều Phối Bàn Ăn
        </h3>
        <p class="text-muted mb-0 small">
          Tiếp nhận khách đặt trước, lọc và xếp bàn thông minh theo số người, tự động đồng bộ bàn phục vụ khi check-in.
        </p>
      </div>

      <div class="d-flex align-items-center gap-2 flex-wrap">
        <button
          class="btn btn-outline-secondary fw-bold px-3 py-2 rounded-3 shadow-xs d-flex align-items-center gap-2"
          @click="fetchReservations"
          :disabled="loading"
        >
          <span :class="{ 'spin-anim': loading }">🔄</span>
          <span>Làm mới</span>
        </button>
        <button
          class="btn btn-primary fw-bold px-4 py-2 rounded-3 shadow-sm d-flex align-items-center gap-2 hotline-btn"
          @click="openCreateModal"
        >
          <span>➕</span>
          <span>Nhận Đặt Bàn Hotline</span>
        </button>
      </div>
    </div>

    <!-- Stats KPI Metric Cards (Interactive click-to-filter by Date & Status) -->
    <div class="row g-3 mb-4">
      <!-- Total Bookings (Dynamic by Date) -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100 cursor-pointer"
          :class="{ 'kpi-card-active': filterStatus === 'all' }"
          @click="quickFilterCard('all')"
          :title="`Bấm để lọc tất cả lịch hẹn ${currentDateLabel}`"
        >
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-muted small fw-semibold">{{ totalCardTitle }}</span>
            <span class="kpi-icon-badge bg-primary-subtle text-primary">📅</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h2 class="fw-black mb-0 text-dark">{{ stats.totalToday }}</h2>
            <span class="text-muted small">lượt đặt</span>
          </div>
          <div class="text-muted small mt-1 d-flex align-items-center gap-1">
            <span>{{ totalCardSubtitle }}</span>
          </div>
        </div>
      </div>

      <!-- Pending (Chờ xác nhận) -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100 cursor-pointer"
          :class="{ 'kpi-card-active': filterStatus === 'pending' }"
          @click="quickFilterCard('pending')"
          :title="`Bấm để lọc các phiếu chờ xác nhận ${currentDateLabel}`"
        >
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-warning-emphasis small fw-semibold">⏳ Chờ Xác Nhận</span>
            <span class="kpi-icon-badge bg-warning-subtle text-warning position-relative">
              ⏳
              <span v-if="stats.pendingCount > 0" class="position-absolute top-0 start-100 translate-middle p-1 bg-danger border border-light rounded-circle"></span>
            </span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h2 class="fw-black mb-0 text-warning">{{ stats.pendingCount }}</h2>
            <span class="text-warning small fw-semibold">chờ duyệt</span>
          </div>
          <div class="text-muted small mt-1">
            {{ pendingCardSubtitle }}
          </div>
        </div>
      </div>

      <!-- Confirmed (Đã xác nhận) -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100 cursor-pointer"
          :class="{ 'kpi-card-active': filterStatus === 'confirmed' }"
          @click="quickFilterCard('confirmed')"
          :title="`Bấm để lọc các phiếu đã xác nhận ${currentDateLabel}`"
        >
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-primary-emphasis small fw-semibold">✅ Đã Xác Nhận</span>
            <span class="kpi-icon-badge bg-primary-subtle text-primary">✅</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h2 class="fw-black mb-0 text-primary">{{ stats.confirmedCount }}</h2>
            <span class="text-primary small fw-semibold">đã duyệt</span>
          </div>
          <div class="text-muted small mt-1">
            {{ confirmedCardSubtitle }}
          </div>
        </div>
      </div>

      <!-- Checked In (Đang phục vụ) -->
      <div class="col-6 col-lg-3">
        <div
          class="kpi-metric-card border-0 shadow-sm rounded-4 p-3 bg-white h-100 cursor-pointer"
          :class="{ 'kpi-card-active': filterStatus === 'checked_in' }"
          @click="quickFilterCard('checked_in')"
          :title="`Bấm để lọc các khách đã Check-in ${currentDateLabel}`"
        >
          <div class="d-flex align-items-center justify-content-between mb-2">
            <span class="text-success-emphasis small fw-semibold">🎉 Đã Check-In</span>
            <span class="kpi-icon-badge bg-success-subtle text-success">🍽️</span>
          </div>
          <div class="d-flex align-items-baseline gap-2">
            <h2 class="fw-black mb-0 text-success">{{ stats.checkedInCount }}</h2>
            <span class="text-success small fw-semibold">đang ăn</span>
          </div>
          <div class="text-muted small mt-1">
            {{ checkedInCardSubtitle }}
          </div>
        </div>
      </div>
    </div>

    <!-- Filter & Search Toolbar (UI/UX Pro Design) -->
    <div class="card border-0 shadow-sm rounded-4 p-3 mb-4 bg-white filter-toolbar">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-3">
        <!-- Left Group: Date Filters -->
        <div class="d-flex flex-wrap align-items-center gap-2">
          <!-- Segmented Buttons -->
          <div class="date-segmented-group p-1 bg-light rounded-3 d-inline-flex border">
            <button
              class="btn btn-sm date-segment-btn"
              :class="{ 'active': filterDateQuick === 'today' }"
              @click="setQuickDate('today')"
            >
              Hôm nay
            </button>
            <button
              class="btn btn-sm date-segment-btn"
              :class="{ 'active': filterDateQuick === 'tomorrow' }"
              @click="setQuickDate('tomorrow')"
            >
              Ngày mai
            </button>
            <button
              class="btn btn-sm date-segment-btn"
              :class="{ 'active': filterDateQuick === 'all' }"
              @click="setQuickDate('all')"
            >
              Tất cả ngày
            </button>
          </div>

          <!-- Date Picker (if not all days) -->
          <div class="d-flex align-items-center" v-if="filterDateQuick !== 'all'">
            <div class="input-group input-group-sm date-picker-input-group">
              <span class="input-group-text bg-white border-end-0 text-muted">📅</span>
              <input
                type="date"
                class="form-control form-control-sm border-start-0 ps-1 bg-white font-monospace"
                v-model="filterDate"
                @change="onDatePickerChange"
                style="width: 140px;"
              />
            </div>
          </div>
        </div>

        <!-- Right Group: Status, Search, and Count Badge -->
        <div class="d-flex flex-wrap align-items-center gap-2 flex-grow-1 justify-content-md-end">
          <!-- Status Dropdown -->
          <div style="min-width: 180px;">
            <select class="form-select form-select-sm rounded-3 border-light-subtle bg-light text-dark fw-medium" v-model="filterStatus" @change="fetchReservations">
              <option value="all">📁 Tất cả trạng thái</option>
              <option value="pending">⏳ Chờ xác nhận</option>
              <option value="confirmed">✅ Đã xác nhận</option>
              <option value="checked_in">🎉 Đang phục vụ (Check-in)</option>
              <option value="completed">🏁 Đã hoàn tất</option>
              <option value="cancelled">❌ Đã hủy</option>
              <option value="no_show">🚫 Vắng mặt</option>
            </select>
          </div>

          <!-- Search Box (Integrated icon, clean clear button) -->
          <div class="search-box-wrapper position-relative" style="min-width: 240px; max-width: 300px;">
            <span class="search-icon-inside position-absolute top-50 start-0 translate-middle-y ms-2 text-muted" style="font-size: 0.85rem;">🔍</span>
            <input
              type="text"
              class="form-control form-control-sm ps-4 pe-4 rounded-3 border-light-subtle bg-light"
              placeholder="Tìm tên khách hàng, SĐT..."
              v-model="searchKeyword"
              @keyup.enter="fetchReservations"
            />
            <button
              v-if="searchKeyword"
              type="button"
              class="btn btn-sm position-absolute top-50 end-0 translate-middle-y text-muted border-0 bg-transparent me-1 p-0 px-1"
              @click="clearSearch"
              style="font-size: 0.75rem;"
            >
              ✕
            </button>
          </div>

          <!-- Total Count Badge -->
          <span class="badge bg-light text-dark border px-3 py-2 rounded-3 small fw-semibold shadow-xs">
            Tổng cộng: <strong class="text-primary">{{ reservations.length }}</strong> phiếu
          </span>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- DẠNG BẢNG QUẢN LÝ (UI/UX PRO - VỪA VẶN MÀN HÌNH, KHÔNG CUỘN NGANG)-->
    <!-- ============================================================== -->
    <div class="card border-0 shadow-sm rounded-4 overflow-hidden bg-white mb-4 table-card-container">
      <div class="table-responsive-fit">
        <table class="table table-hover align-middle mb-0 custom-res-table">
          <colgroup>
            <col style="width: 5%;" />
            <col style="width: 17%;" />
            <col style="width: 13%;" />
            <col style="width: 8%;" />
            <col style="width: 15%;" />
            <col style="width: 13%;" />
            <col style="width: 12%;" />
            <col style="width: 17%;" />
          </colgroup>
          <thead>
            <tr>
              <th class="ps-2 pe-1 py-2 text-center text-nowrap">Mã</th>
              <th class="py-2 text-nowrap">Khách Hàng</th>
              <th class="py-2 text-nowrap">Thời Gian Đến</th>
              <th class="py-2 text-center text-nowrap">Số Khách</th>
              <th class="py-2 text-nowrap">Bàn Phục Vụ</th>
              <th class="py-2 text-nowrap">Ghi Chú Đặc Biệt</th>
              <th class="py-2 text-center text-nowrap">Trạng Thái</th>
              <th class="py-2 text-end pe-2 text-nowrap actions-header">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <!-- Loading skeleton -->
            <tr v-if="loading">
              <td colspan="8" class="text-center py-5 text-muted">
                <div class="spinner-border spinner-border-sm text-primary me-2"></div>
                <span class="fw-semibold">Đang tải danh sách đặt bàn realtime...</span>
              </td>
            </tr>

            <!-- Empty State -->
            <tr v-else-if="reservations.length === 0">
              <td colspan="8" class="text-center py-5">
                <div class="empty-res-box py-4">
                  <div class="fs-1 mb-2">📭</div>
                  <h6 class="fw-bold text-dark mb-1">Không tìm thấy phiếu đặt bàn nào</h6>
                  <p class="text-muted small mb-3">
                    Thử chọn ngày khác, xóa từ khóa tìm kiếm hoặc tạo lịch hẹn mới cho khách gọi hotline.
                  </p>
                  <button class="btn btn-sm btn-outline-primary fw-bold px-3 py-2 rounded-3" @click="openCreateModal">
                    ➕ Nhận Đặt Bàn Mới
                  </button>
                </div>
              </td>
            </tr>

            <!-- Table Rows -->
            <tr
              v-for="item in reservations"
              :key="item.reservationId"
              class="res-table-row"
              :class="{ 'table-warning-subtle': item.reservationStatus === 'pending' }"
            >
              <!-- ID -->
              <td class="ps-2 pe-1 text-center">
                <span class="badge res-id-badge font-monospace">#{{ item.reservationId }}</span>
              </td>

              <!-- Customer info -->
              <td class="px-1">
                <div class="d-flex align-items-center gap-2 overflow-hidden">
                  <div class="customer-avatar-circle flex-shrink-0">
                    {{ getInitials(item.customerName) }}
                  </div>
                  <div class="customer-info-meta text-truncate flex-grow-1" style="min-width: 0;">
                    <div class="fw-bold text-dark text-truncate d-flex align-items-center gap-1" :title="item.customerName">
                      <span class="text-truncate">{{ item.customerName }}</span>
                      <span v-if="item.customerPoints > 0" class="badge bg-warning-subtle text-dark border vip-point-badge flex-shrink-0" :title="`Điểm tích lũy: ${item.customerPoints}đ`">
                        ⭐ {{ item.customerPoints }}đ
                      </span>
                    </div>
                    <div class="text-muted mt-0.5 text-truncate" style="font-size: 0.74rem;">
                      <a :href="`tel:${item.customerPhone}`" class="text-decoration-none text-muted font-monospace phone-link" title="Bấm để gọi hotline">
                        📞 {{ item.customerPhone }}
                      </a>
                    </div>
                  </div>
                </div>
              </td>

              <!-- Time & Date -->
              <td class="px-1">
                <div class="d-flex align-items-center gap-1 text-truncate">
                  <span class="fw-bold text-dark font-monospace" style="font-size: 0.84rem;">🕒 {{ formatTime(item.reservationDate) }}</span>
                  <span
                    class="badge border py-0 px-1 rounded-pill flex-shrink-0"
                    :class="getRelativeDateBadgeClass(item.reservationDate)"
                    style="font-size: 0.65rem;"
                  >
                    {{ getRelativeDateLabel(item.reservationDate) }}
                  </span>
                </div>
                <div class="text-muted small mt-0.5 text-truncate" style="font-size: 0.72rem;">
                  {{ formatDate(item.reservationDate) }}
                </div>
              </td>

              <!-- Guest Count -->
              <td class="text-center px-1">
                <span
                  class="badge px-2 py-1 rounded-pill fw-bold d-inline-flex align-items-center gap-1 text-nowrap"
                  :class="item.guestCount >= 8 ? 'bg-purple-subtle text-purple border-purple' : 'bg-light text-dark border'"
                  style="font-size: 0.74rem;"
                >
                  <span>👥</span> {{ item.guestCount }} khách
                </span>
                <div v-if="item.guestCount >= 8" class="text-purple fw-semibold mt-0.5 text-nowrap" style="font-size: 0.62rem;">
                  Nhóm đông
                </div>
              </td>

              <!-- Assigned Table -->
              <td class="px-1" style="min-width: 0;">
                <!-- Gộp bàn -->
                <div v-if="getMergedTableTitle(item)" class="assigned-table-chip" style="min-width: 0;">
                  <span
                    class="badge bg-purple-subtle text-purple border-purple px-2 py-1 fw-bold d-inline-block text-truncate cursor-pointer"
                    style="max-width: 100%; font-size: 0.73rem;"
                    :title="getMergedTableTitle(item)"
                    @click="openAssignTableModal(item)"
                  >
                    <span>🔗</span> {{ getMergedShortLabel(item) }}
                  </span>
                  <div class="text-muted mt-0.5 text-truncate" style="font-size: 0.7rem;" :title="`${getMergedArea(item)} · Cụm gộp`">
                    📍 {{ getMergedArea(item) }} · Cụm gộp
                  </div>
                </div>
                <!-- Bàn đơn -->
                <div v-else-if="item.table" class="assigned-table-chip" style="min-width: 0;">
                  <span class="badge bg-primary-subtle text-primary border border-primary-subtle px-2 py-1 fw-bold d-inline-block text-truncate" style="max-width: 100%; font-size: 0.73rem;">
                    <span>🪑</span> {{ item.table.tableName }}
                  </span>
                  <div class="text-muted mt-0.5 text-truncate" style="font-size: 0.7rem;" :title="`${item.table.areaName} (${item.table.capacity} chỗ)`">
                    📍 {{ item.table.areaName }} ({{ item.table.capacity }} chỗ)
                  </div>
                </div>
                <!-- Chưa xếp bàn -->
                <div v-else>
                  <span class="badge bg-light text-muted border border-dashed px-2 py-1 rounded-pill small fst-italic text-nowrap" style="font-size: 0.72rem;">
                    ⏳ Chưa xếp bàn
                  </span>
                </div>
              </td>

              <!-- Notes (Clean & Sanitized) -->
              <td class="px-1" style="min-width: 0;">
                <div v-if="getCleanCustomerNote(item.note)" class="res-note-bubble" :title="getCleanCustomerNote(item.note)">
                  <span class="note-quote-icon">💬</span>
                  <span class="note-quote-text text-truncate">{{ getCleanCustomerNote(item.note) }}</span>
                </div>
                <div v-if="getCancellationReason(item.note)" class="mt-1">
                  <span class="badge bg-danger-subtle text-danger border border-danger-subtle text-truncate d-inline-block" style="max-width: 100%; font-size: 0.7rem;" :title="`Lý do hủy: ${getCancellationReason(item.note)}`">
                    ⚠️ {{ getCancellationReason(item.note) }}
                  </span>
                </div>
                <span v-if="!getCleanCustomerNote(item.note) && !getCancellationReason(item.note)" class="text-muted small opacity-50">
                  —
                </span>
              </td>

              <!-- Status badge -->
              <td class="text-center px-1 text-nowrap">
                <span
                  class="badge px-2 py-1 rounded-pill fw-semibold status-pill-badge"
                  :class="getStatusBadgeClass(item.reservationStatus)"
                >
                  <span class="status-indicator-dot" :class="getStatusDotClass(item.reservationStatus)"></span>
                  {{ getReservationStatusText(item) }}
                </span>
              </td>

              <!-- Actions Column (UI/UX Pro Button Group - Compact, Fitted) -->
              <td class="text-end pe-2 text-nowrap actions-cell">
                <div class="d-flex justify-content-end align-items-center gap-1">
                  <!-- Pending -> Confirm button -->
                  <button
                    v-if="item.reservationStatus === 'pending'"
                    class="btn btn-sm btn-primary-action text-nowrap fw-bold px-2 py-0 shadow-xs"
                    @click="handleConfirm(item)"
                    title="Xác nhận tiếp nhận lịch đặt bàn"
                  >
                    <span>✓</span> Duyệt
                  </button>

                  <!-- Confirmed without table -> Assign Table button (Pastel soft tint) -->
                  <button
                    v-if="item.reservationStatus === 'confirmed' && !item.table"
                    class="btn btn-sm btn-assign-brand text-nowrap fw-bold px-2 py-0"
                    @click="openAssignTableModal(item)"
                    title="Chọn bàn ăn phù hợp"
                  >
                    <span>🪑</span> Xếp bàn
                  </button>

                  <!-- Confirmed with table -> Check-In button -->
                  <button
                    v-if="item.reservationStatus === 'confirmed' && item.table"
                    class="btn btn-sm btn-emerald-action text-nowrap fw-bold text-white px-2 py-0 shadow-xs"
                    @click="handleCheckIn(item)"
                    title="Khách đã đến: Check-in đón khách vào bàn"
                  >
                    <span>🛎️</span> Check-in
                  </button>

                  <!-- Confirmed with table -> Change Table button -->
                  <button
                    v-if="item.reservationStatus === 'confirmed' && item.table"
                    class="btn btn-sm btn-action-ghost"
                    @click="openAssignTableModal(item)"
                    title="Đổi sang bàn khác"
                  >
                    <span>🪑</span>
                  </button>

                  <!-- View detail info -->
                  <button
                    type="button"
                    class="btn btn-sm btn-action-ghost"
                    @click="openDetailModal(item)"
                    title="Xem chi tiết đầy đủ phiếu"
                  >
                    ℹ️
                  </button>

                  <!-- Cancel button -->
                  <button
                    v-if="item.reservationStatus === 'pending' || item.reservationStatus === 'confirmed'"
                    class="btn btn-sm btn-action-danger"
                    @click="handleCancel(item)"
                    title="Hủy đặt bàn này"
                  >
                    ✕
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>


    <!-- ============================================================== -->
    <!-- MODAL: XẾP BÀN THÔNG MINH (SMART TABLE ASSIGNMENT)              -->
    <!-- Tuân thủ rule: Ẩn bàn đang serving & tùy chỉnh theo số khách   -->
    <!-- ============================================================== -->
    <div
      v-if="showAssignModal"
      class="modal-backdrop-custom"
      @click.self="showAssignModal = false"
    >
      <div class="assign-table-dialog bg-white shadow-2xl rounded-4 overflow-hidden border">
        <!-- Modal Header -->
        <div class="p-3 px-4 d-flex justify-content-between align-items-center border-bottom bg-gradient-assign">
          <div>
            <div class="small text-white-50 fw-semibold text-uppercase">Điều Phối Chỗ Ngồi Nhà Hàng</div>
            <h5 class="fw-bold m-0 text-white d-flex align-items-center gap-2">
              <span>🪑</span> Xếp Bàn Cho Khách #{{ selectedReservation?.reservationId }}
            </h5>
          </div>
          <button type="button" class="btn-close-custom" @click="showAssignModal = false">✕</button>
        </div>

        <div class="modal-body-scrollable p-3 p-md-4">
          <!-- Reservation Context Card -->
          <div class="reservation-summary-card p-3 rounded-3 mb-3 border bg-light">
            <div class="row g-2 align-items-center">
              <div class="col-md-6">
                <div class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
                  <span>👤 {{ selectedReservation?.customerName }}</span>
                  <span class="badge bg-primary-subtle text-primary border small">
                    📞 {{ selectedReservation?.customerPhone }}
                  </span>
                </div>
                <div class="small text-muted mt-1 d-flex align-items-center gap-1 flex-wrap">
                  <span>🕒 Thời gian: <strong>{{ formatDateTime(selectedReservation?.reservationDate) }}</strong></span>
                  <span class="badge bg-primary-subtle text-primary border border-primary-subtle py-0 px-2">
                    📅 {{ getRelativeDateLabel(selectedReservation?.reservationDate) }}
                  </span>
                </div>
                <div class="small text-success mt-1 fw-semibold">
                  ✨ Đang lọc bàn trống khả dụng vào ngày: <strong>{{ formatDate(selectedReservation?.reservationDate) }}</strong>
                </div>
              </div>
              <div class="col-md-6 text-md-end">
                <div class="d-inline-flex align-items-center gap-2">
                  <span class="fs-6 fw-bold text-dark">Số lượng khách:</span>
                  <span class="badge bg-warning text-dark fs-6 fw-black px-3 py-2 rounded-pill shadow-xs">
                    👥 {{ selectedReservation?.guestCount }} Người
                  </span>
                </div>
                <div v-if="selectedReservation?.table" class="small text-muted mt-1">
                  Bàn hiện tại: <span class="badge bg-info-subtle text-info-emphasis border">{{ selectedReservation.table.tableName }}</span>
                </div>
              </div>
            </div>
            <div v-if="selectedReservation?.note" class="mt-2 pt-2 border-top small text-muted">
              📌 <strong>Ghi chú khách:</strong> {{ selectedReservation.note }}
            </div>
          </div>

          <!-- Navigation Mode Tabs: Xếp 1 bàn đơn vs Gộp nhiều bàn trống -->
          <div class="d-flex align-items-center justify-content-between flex-wrap gap-2 mb-3">
            <div class="nav-assign-segmented p-1 bg-light rounded-3 d-inline-flex border">
              <button
                type="button"
                class="btn btn-sm nav-assign-btn d-inline-flex align-items-center gap-1"
                :class="{ 'active': assignMode === 'single' }"
                @click="assignMode = 'single'"
              >
                <span>🪑</span>
                <span>Xếp 1 Bàn Đơn (Từ {{ selectedReservation?.guestCount }} chỗ)</span>
                <span class="badge rounded-pill bg-primary ms-1" v-if="singleEligibleCount > 0">{{ singleEligibleCount }}</span>
              </button>
              <button
                type="button"
                class="btn btn-sm nav-assign-btn d-inline-flex align-items-center gap-1"
                :class="{ 'active': assignMode === 'merge' }"
                @click="assignMode = 'merge'"
              >
                <span>🔗</span>
                <span>Gộp Nhiều Bàn Trống</span>
                <span class="badge rounded-pill bg-purple-subtle text-purple border-purple ms-1" v-if="selectedMergeTableIds.length > 0">
                  {{ selectedMergeTableIds.length }} bàn
                </span>
              </button>
            </div>

            <span class="badge bg-success-subtle text-success border border-success-subtle small py-1 px-2">
              🟢 Chỉ hiển thị các bàn TRỐNG để giữ chỗ
            </span>
          </div>

          <!-- ============================================================== -->
          <!-- BỐ CỤC CHỌN KHU VỰC DẠNG SIDEBAR + BÀN ĂN WORKSPACE (UI/UX PRO)   -->
          <!-- ============================================================== -->
          <div class="assign-layout-container d-flex flex-column flex-md-row gap-3">
            <!-- 1. SIDEBAR PHÂN KHU VỰC BÀN -->
            <aside class="area-sidebar-card">
              <div class="sidebar-header d-flex align-items-center justify-content-between mb-2 pb-2 border-bottom">
                <span class="small fw-bold text-uppercase text-dark d-flex align-items-center gap-1" style="font-size: 0.75rem; letter-spacing: 0.05em;">
                  <span>📍</span> Phân Khu Vực
                </span>
                <span class="badge bg-secondary-subtle text-dark border rounded-pill" style="font-size: 0.65rem;">
                  {{ areaList.length }} khu
                </span>
              </div>

              <!-- Danh sách khu vực khi ở Chế độ 1 Bàn Đơn -->
              <div v-if="assignMode === 'single'" class="area-sidebar-list">
                <button
                  type="button"
                  class="area-sidebar-item"
                  :class="{ 'active': selectedAreaTab === 'all' }"
                  @click="selectedAreaTab = 'all'"
                >
                  <div class="d-flex align-items-center gap-2 text-truncate">
                    <span class="area-item-icon">🏢</span>
                    <span class="area-item-name text-truncate">Tất cả khu vực</span>
                  </div>
                  <span class="badge area-item-badge" :class="singleEligibleTables.length > 0 ? 'badge-has-tables' : 'badge-empty'">
                    {{ singleEligibleTables.length }}
                  </span>
                </button>

                <button
                  v-for="area in areaList"
                  :key="area"
                  type="button"
                  class="area-sidebar-item"
                  :class="{ 'active': selectedAreaTab === area }"
                  @click="selectedAreaTab = area"
                >
                  <div class="d-flex align-items-center gap-2 text-truncate">
                    <span class="area-item-icon">📍</span>
                    <span class="area-item-name text-truncate">{{ area }}</span>
                  </div>
                  <span class="badge area-item-badge" :class="getSingleAreaCount(area) > 0 ? 'badge-has-tables' : 'badge-empty'">
                    {{ getSingleAreaCount(area) }} bàn
                  </span>
                </button>
              </div>

              <!-- Danh sách khu vực khi ở Chế độ Gộp Bàn (Bắt buộc chọn 1 khu vực cụ thể) -->
              <div v-else class="area-sidebar-list">
                <button
                  v-for="area in areaList"
                  :key="area"
                  type="button"
                  class="area-sidebar-item"
                  :class="{ 'active': selectedMergeArea === area }"
                  @click="changeMergeArea(area)"
                >
                  <div class="d-flex align-items-center gap-2 text-truncate">
                    <span class="area-item-icon">📍</span>
                    <div class="text-start text-truncate">
                      <div class="area-item-name text-truncate">{{ area }}</div>
                      <div class="area-item-sub">{{ getEmptyAreaCount(area) }} bàn trống</div>
                    </div>
                  </div>
                  <span class="badge area-item-badge" :class="getEmptyAreaCount(area) > 0 ? 'badge-has-tables' : 'badge-empty'">
                    {{ getEmptyAreaCount(area) }}
                  </span>
                </button>
              </div>

              <!-- Gợi ý an toàn khi gộp bàn ở đáy sidebar -->
              <div v-if="assignMode === 'merge'" class="sidebar-safety-hint mt-auto pt-3">
                <div class="sidebar-safety-card">
                  <div class="d-flex align-items-start gap-1-5">
                    <span class="fs-6 flex-shrink-0">🔒</span>
                    <div style="font-size: 0.72rem; line-height: 1.35;" class="text-dark">
                      <strong>Ràng buộc an toàn:</strong><br>
                      Chỉ được gộp các bàn trong <strong>cùng 1 khu vực</strong> để tránh gộp nhầm và đảm bảo phục vụ chu đáo.
                    </div>
                  </div>
                </div>
              </div>
            </aside>

            <!-- 2. WORKSPACE BÀN ĂN (BÊN PHẢI) -->
            <div class="area-workspace-panel flex-grow-1 min-w-0">
              <!-- ============================================================== -->
              <!-- CHẾ ĐỘ 1: XẾP 1 BÀN THEO SỐ NGƯỜI (SINGLE TABLE MODE)           -->
              <!-- ============================================================== -->
              <div v-if="assignMode === 'single'">
                <!-- Status banner when NO single table is large enough -->
                <div v-if="singleEligibleTables.length === 0" class="alert alert-warning border border-warning-subtle rounded-3 p-3 mb-3 shadow-xs">
                  <div class="d-flex align-items-start gap-2">
                    <span class="fs-4">⚠️</span>
                    <div>
                      <h6 class="fw-bold text-dark mb-1">
                        Không có bàn đơn nào đủ {{ selectedReservation?.guestCount }} chỗ!
                      </h6>
                      <p class="small text-muted mb-2">
                        Hiện tại tất cả các bàn trống đều có sức chứa nhỏ hơn <strong>{{ selectedReservation?.guestCount }} khách</strong> (hoặc các bàn lớn đều đã được đặt). Vui lòng chuyển sang chế độ <strong>Gộp nhiều bàn trống</strong> để kết hợp 2 hoặc nhiều bàn lại với nhau.
                      </p>
                      <button
                        type="button"
                        class="btn btn-sm btn-primary fw-bold rounded-pill px-3 py-1 shadow-xs"
                        @click="assignMode = 'merge'"
                      >
                        👉 Chuyển Sang Chế Độ Gộp Nhiều Bàn Trống
                      </button>
                    </div>
                  </div>
                </div>

                <!-- Single mode guide and selection info -->
                <div v-else class="p-2-5 px-3 rounded-3 bg-light border mb-3 d-flex align-items-center justify-content-between flex-wrap gap-2">
                  <div class="small text-muted">
                    Đang xem: <strong>{{ selectedAreaTab === 'all' ? 'Tất cả khu vực' : selectedAreaTab }}</strong> ({{ filteredSingleTables.length }} bàn đủ từ {{ selectedReservation?.guestCount }} chỗ):
                  </div>
                  <div v-if="selectedSingleTable" class="badge bg-primary px-3 py-1 rounded-pill fw-bold">
                    ✓ Đã chọn: {{ selectedSingleTable.tableName }} ({{ selectedSingleTable.capacity }} chỗ)
                  </div>
                </div>

                <!-- Single Tables Grid -->
                <div v-if="filteredSingleTables.length > 0" class="row g-2-5">
                  <div
                    v-for="t in filteredSingleTables"
                    :key="t.tableId"
                    class="col-12 col-sm-6 col-lg-4"
                  >
                    <div
                      class="table-selection-card p-3 rounded-3 border cursor-pointer position-relative h-100"
                      :class="{ 'active-selected-table': selectedSingleTableId === t.tableId }"
                      @click="selectedSingleTableId = t.tableId"
                    >
                      <!-- Selected badge -->
                      <div v-if="selectedSingleTableId === t.tableId" class="selected-check-badge">
                        ✓
                      </div>

                      <div class="d-flex justify-content-between align-items-start mb-2">
                        <div>
                          <span class="badge bg-secondary-subtle text-dark border small fw-bold font-monospace">
                            {{ t.tableCode }}
                          </span>
                          <h6 class="fw-black text-dark mb-0 mt-1">
                            {{ t.tableName }}
                          </h6>
                        </div>
                        <span class="badge bg-light text-muted border small">
                          {{ t.areaName }}
                        </span>
                      </div>

                      <!-- Capacity & Best Fit Badge -->
                      <div class="d-flex align-items-center justify-content-between p-2 rounded bg-light border my-2">
                        <div class="d-flex align-items-center gap-1">
                          <span class="fs-6">👥</span>
                          <span class="fw-bold text-dark">{{ t.capacity }} Chỗ</span>
                        </div>
                        <span
                          class="badge small"
                          :class="t.capacity === selectedReservation?.guestCount ? 'bg-success text-white' : 'bg-success-subtle text-success border border-success-subtle'"
                        >
                          {{ t.capacity === selectedReservation?.guestCount ? '⭐ Vừa khít số khách' : `+${t.capacity - (selectedReservation?.guestCount || 0)} chỗ thừa` }}
                        </span>
                      </div>

                      <div class="d-flex justify-content-between align-items-center mt-2 pt-1 border-top small">
                        <span v-if="selectedSingleTableId === t.tableId" class="text-primary fw-bold">
                          ✓ Đang chọn bàn này
                        </span>
                        <span v-else class="text-muted">
                          Bấm để chọn bàn
                        </span>
                        <span class="badge bg-success-subtle text-success border" style="font-size: 0.65rem;">
                          🟢 Bàn trống
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Empty state if no single tables in this area -->
                <div v-else-if="singleEligibleTables.length > 0" class="empty-tables-box p-4 text-center rounded-3 border bg-light my-2">
                  <div class="fs-1 mb-2">🪑🔍</div>
                  <h6 class="fw-bold text-dark mb-1">Không có bàn trống đủ chỗ</h6>
                  <p class="text-muted small mb-0">Không tìm thấy bàn đơn nào đủ {{ selectedReservation?.guestCount }} khách tại khu vực <strong>{{ selectedAreaTab }}</strong>.</p>
                </div>
              </div>

              <!-- ============================================================== -->
              <!-- CHẾ ĐỘ 2: GỘP NHIỀU BÀN TRỐNG (MERGE EMPTY TABLES MODE)        -->
              <!-- ============================================================== -->
              <div v-if="assignMode === 'merge'">
                <!-- Multi-Table Live Summary Card -->
                <div class="selected-tables-summary p-3 rounded-3 bg-light border mb-3">
                  <div class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-2">
                    <div>
                      <div class="small text-muted fw-semibold mb-1 d-flex align-items-center gap-2">
                        <span>CÁC BÀN ĐÃ CHỌN ĐỂ GỘP ({{ selectedMergeTableIds.length }} BÀN):</span>
                        <span v-if="selectedMergeArea" class="badge bg-purple text-white">
                          📍 Khu vực: {{ selectedMergeArea }}
                        </span>
                      </div>
                      <div v-if="selectedMergeTableIds.length > 0" class="d-flex align-items-center gap-1 flex-wrap">
                        <span
                          v-for="st in selectedMergeTables"
                          :key="st.tableId"
                          class="badge px-2 py-1 rounded-pill fw-bold border bg-purple-subtle text-purple border-purple d-inline-flex align-items-center gap-1"
                        >
                          <span>🟢</span>
                          {{ st.tableName }} ({{ st.capacity }} chỗ)
                          <span role="button" class="ms-1 text-danger cursor-pointer" @click.stop="toggleMergeTableSelection(st.tableId)">×</span>
                        </span>
                        <button
                          type="button"
                          class="btn btn-xs btn-outline-danger ms-2 rounded-pill px-2"
                          @click="clearMergeSelections"
                        >
                          ✕ Bỏ chọn tất cả
                        </button>
                      </div>
                      <div v-else class="text-danger small fst-italic">
                        Chưa chọn bàn nào. Bấm vào các thẻ bàn bên dưới để chọn các bàn cần gộp trong khu vực <strong>{{ selectedMergeArea }}</strong>.
                      </div>
                    </div>

                    <div class="text-md-end flex-shrink-0" v-if="selectedMergeTableIds.length > 0">
                      <div class="d-flex align-items-center gap-2 justify-content-md-end">
                        <span class="small text-muted">Tổng sức chứa gộp:</span>
                        <span class="fs-6 fw-black" :class="totalMergeCapacity >= (selectedReservation?.guestCount || 0) ? 'text-success' : 'text-danger'">
                          {{ totalMergeCapacity }} / {{ selectedReservation?.guestCount }} chỗ
                        </span>
                      </div>
                      <div class="small mt-1">
                        <span v-if="totalMergeCapacity >= (selectedReservation?.guestCount || 0)" class="badge bg-success-subtle text-success border border-success">
                          ✅ Đủ chỗ (+{{ totalMergeCapacity - (selectedReservation?.guestCount || 0) }} chỗ thừa)
                        </span>
                        <span v-else class="badge bg-danger-subtle text-danger border border-danger">
                          ⚠️ Còn thiếu {{ (selectedReservation?.guestCount || 0) - totalMergeCapacity }} chỗ
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Merge Tables Grid -->
                <div v-if="filteredMergeTables.length === 0" class="empty-tables-box p-4 text-center rounded-3 border bg-light my-3">
                  <div class="fs-1 mb-2">🪑🔍</div>
                  <h6 class="fw-bold text-dark mb-1">Không tìm thấy bàn trống nào</h6>
                  <p class="text-muted small mb-0">Không có bàn trống nào ở khu vực <strong>{{ selectedMergeArea }}</strong>.</p>
                </div>

                <div v-else class="row g-2-5">
                  <div
                    v-for="t in filteredMergeTables"
                    :key="t.tableId"
                    class="col-12 col-sm-6 col-lg-4"
                  >
                    <div
                      class="table-selection-card p-3 rounded-3 border cursor-pointer position-relative h-100"
                      :class="{ 'active-selected-table': selectedMergeTableIds.includes(t.tableId) }"
                      @click="toggleMergeTableSelection(t.tableId)"
                    >
                      <!-- Checked badge -->
                      <div v-if="selectedMergeTableIds.includes(t.tableId)" class="selected-check-badge">
                        ✓
                      </div>

                      <div class="d-flex justify-content-between align-items-start mb-2">
                        <div>
                          <span class="badge bg-secondary-subtle text-dark border small fw-bold font-monospace">
                            {{ t.tableCode }}
                          </span>
                          <h6 class="fw-black text-dark mb-0 mt-1">
                            {{ t.tableName }}
                          </h6>
                        </div>
                        <span class="badge bg-light text-muted border small">
                          {{ t.areaName }}
                        </span>
                      </div>

                      <div class="d-flex align-items-center justify-content-between p-2 rounded bg-light border my-2">
                        <div class="d-flex align-items-center gap-1">
                          <span class="fs-6">👥</span>
                          <span class="fw-bold text-dark">{{ t.capacity }} Chỗ</span>
                        </div>
                        <span class="badge bg-success-subtle text-success border border-success-subtle small">
                          🟢 Bàn trống
                        </span>
                      </div>

                      <div class="d-flex justify-content-between align-items-center mt-2 pt-1 border-top small">
                        <span v-if="selectedMergeTableIds.includes(t.tableId)" class="text-purple fw-bold">
                          ✓ Đã chọn (Click để bỏ)
                        </span>
                        <span v-else class="text-muted">
                          Bấm để gộp bàn này
                        </span>
                        <span class="badge bg-light text-muted border" style="font-size: 0.65rem;">
                          Khả dụng
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="p-3 px-4 d-flex justify-content-between align-items-center border-top bg-light">
          <div class="small text-muted">
            <span v-if="assignMode === 'single'">
              <span v-if="selectedSingleTable">
                Đã chọn <strong>{{ selectedSingleTable.tableName }}</strong> (Sức chứa: <strong>{{ selectedSingleTable.capacity }} chỗ</strong>)
              </span>
              <span v-else class="text-danger">
                Vui lòng chọn 1 bàn để tiếp tục
              </span>
            </span>
            <span v-else>
              <span v-if="selectedMergeTableIds.length > 0">
                Đang gộp <strong>{{ selectedMergeTableIds.length }} bàn</strong> (Tổng <strong>{{ totalMergeCapacity }} chỗ</strong>)
              </span>
              <span v-else class="text-danger">
                Vui lòng chọn ít nhất 1 bàn để tiếp tục
              </span>
            </span>
          </div>

          <div class="d-flex gap-2">
            <button type="button" class="btn btn-light border px-3 rounded-3" @click="showAssignModal = false">
              Hủy
            </button>
            <button
              type="button"
              class="btn btn-primary fw-bold px-4 rounded-3 shadow-sm"
              :disabled="isAssignSubmitDisabled || submittingAssign"
              @click="submitAssignTable"
            >
              <span v-if="submittingAssign" class="spinner-border spinner-border-sm me-1"></span>
              {{ getAssignSubmitLabel() }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL: NHẬN ĐẶT BÀN HOTLINE TRỰC TIẾP                           -->
    <!-- ============================================================== -->
    <div
      v-if="showCreateModal"
      class="modal-backdrop-custom"
      @click.self="showCreateModal = false"
    >
      <div class="hotline-dialog bg-white shadow-2xl rounded-4 overflow-hidden border">
        <!-- Header -->
        <div class="p-3 px-4 d-flex justify-content-between align-items-center border-bottom bg-gradient-hotline">
          <div>
            <div class="small text-white-50 fw-semibold text-uppercase">Tiếp Nhận Khách Hẹn</div>
            <h5 class="fw-bold m-0 text-white d-flex align-items-center gap-2">
              <span>📞</span> Nhận Đặt Bàn Trực Tiếp / Hotline
            </h5>
          </div>
          <button type="button" class="btn-close-custom" @click="showCreateModal = false">✕</button>
        </div>

        <form @submit.prevent="submitCreateReservation">
          <div class="modal-body-scrollable p-3 p-md-4">
            <div class="row g-3">
              <!-- Họ và tên -->
              <div class="col-md-6">
                <label class="form-label fw-bold small text-dark">
                  Họ và tên khách hàng <span class="text-danger">*</span>
                </label>
                <div class="input-group">
                  <span class="input-group-text bg-light text-muted">👤</span>
                  <input
                    type="text"
                    class="form-control"
                    v-model.trim="createForm.customerName"
                    placeholder="VD: Anh Nam, Chị Hương..."
                    required
                  />
                </div>
              </div>

              <!-- Số điện thoại -->
              <div class="col-md-6">
                <label class="form-label fw-bold small text-dark">
                  Số điện thoại liên hệ <span class="text-danger">*</span>
                </label>
                <div class="input-group">
                  <span class="input-group-text bg-light text-muted">📞</span>
                  <input
                    type="tel"
                    class="form-control"
                    v-model.trim="createForm.customerPhone"
                    placeholder="VD: 0912345678"
                    required
                  />
                </div>
              </div>

              <!-- Thời gian hẹn -->
              <div class="col-md-6">
                <label class="form-label fw-bold small text-dark">
                  Thời gian đến ăn <span class="text-danger">*</span>
                </label>
                <input
                  type="datetime-local"
                  class="form-control"
                  v-model="createForm.reservationDate"
                  required
                />
              </div>

              <!-- Số lượng khách -->
              <div class="col-md-6">
                <label class="form-label fw-bold small text-dark">
                  Số lượng người <span class="text-danger">*</span>
                </label>
                <div class="d-flex align-items-center gap-2">
                  <div class="input-group">
                    <button
                      type="button"
                      class="btn btn-outline-secondary"
                      @click="createForm.guestCount = Math.max(1, createForm.guestCount - 1)"
                    >
                      -
                    </button>
                    <input
                      type="number"
                      min="1"
                      max="100"
                      class="form-control text-center fw-bold"
                      v-model.number="createForm.guestCount"
                      required
                    />
                    <button
                      type="button"
                      class="btn btn-outline-secondary"
                      @click="createForm.guestCount++"
                    >
                      +
                    </button>
                  </div>
                  <span class="text-muted small">người</span>
                </div>

                <!-- Quick guest chips -->
                <div class="d-flex gap-1 mt-2 flex-wrap">
                  <button
                    type="button"
                    v-for="cnt in [2, 4, 6, 8, 10, 12, 16]"
                    :key="cnt"
                    class="btn btn-xs rounded-pill"
                    :class="createForm.guestCount === cnt ? 'btn-primary' : 'btn-light border'"
                    @click="createForm.guestCount = cnt"
                  >
                    {{ cnt }}
                  </button>
                </div>
              </div>

              <!-- Ghi chú đặc biệt -->
              <div class="col-12">
                <label class="form-label fw-bold small text-dark">Ghi chú & Yêu cầu đặc biệt</label>
                <textarea
                  rows="2"
                  class="form-control"
                  v-model="createForm.note"
                  placeholder="Yêu cầu vị trí ngồi yên tĩnh, sinh nhật, ăn chay, mang bánh kem..."
                ></textarea>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <div class="p-3 px-4 d-flex justify-content-end gap-2 border-top bg-light">
            <button type="button" class="btn btn-light border px-3 rounded-3" @click="showCreateModal = false">
              Hủy
            </button>
            <button
              type="submit"
              class="btn btn-primary fw-bold px-4 rounded-3 shadow-sm"
              :disabled="submittingCreate"
            >
              <span v-if="submittingCreate" class="spinner-border spinner-border-sm me-1"></span>
              Lưu Phiếu Đặt Bàn
            </button>
          </div>
        </form>
      </div>
    </div>
    <!-- ============================================================== -->
    <!-- MODAL: CHI TIẾT ĐẶT BÀN (UI/UX PRO EXECUTIVE DETAIL MODAL)      -->
    <!-- ============================================================== -->
    <div
      v-if="showDetailModal && selectedDetailItem"
      class="modal-backdrop-custom"
      @click.self="showDetailModal = false"
    >
      <div class="detail-pro-dialog bg-white shadow-2xl rounded-4 overflow-hidden border">
        <!-- Executive Header -->
        <div class="p-3 px-4 d-flex justify-content-between align-items-center bg-gradient-detail text-white">
          <div class="d-flex align-items-center gap-3">
            <div class="detail-header-icon-box">
              📋
            </div>
            <div>
              <div class="d-flex align-items-center gap-2">
                <span class="badge bg-white-20 text-white font-monospace small px-2 py-0.5 rounded">#{{ selectedDetailItem.reservationId }}</span>
                <span class="badge px-2.5 py-1 rounded-pill small fw-semibold" :class="getStatusBadgeClass(selectedDetailItem.reservationStatus)">
                  <span class="status-indicator-dot me-1" :class="getStatusDotClass(selectedDetailItem.reservationStatus)"></span>
                  {{ getReservationStatusText(selectedDetailItem) }}
                </span>
              </div>
              <h5 class="fw-bold m-0 text-white mt-1">
                Lịch Hẹn: {{ selectedDetailItem.customerName }}
              </h5>
            </div>
          </div>
          <button type="button" class="btn-close-custom" @click="showDetailModal = false" title="Đóng">✕</button>
        </div>

        <div class="p-4 modal-body-scrollable bg-slate-50-subtle">
          <!-- 1. VIP Customer Card -->
          <div class="modal-customer-card p-3 rounded-4 mb-3 border bg-white shadow-xs d-flex align-items-center gap-3">
            <div class="modal-avatar-circle">
              {{ getInitials(selectedDetailItem.customerName) }}
            </div>
            <div class="flex-grow-1">
              <div class="d-flex align-items-center justify-content-between flex-wrap gap-2">
                <div class="d-flex align-items-center gap-2">
                  <h5 class="fw-bold text-dark mb-0">{{ selectedDetailItem.customerName }}</h5>
                  <span v-if="selectedDetailItem.customerPoints > 0" class="badge bg-warning-subtle text-dark border px-2 py-1 rounded-pill small">
                    ⭐ {{ selectedDetailItem.customerPoints }} điểm tích lũy
                  </span>
                </div>
              </div>
              <div class="d-flex align-items-center gap-2 mt-2 flex-wrap">
                <a :href="`tel:${selectedDetailItem.customerPhone}`" class="phone-chip text-decoration-none">
                  <span>📞</span> <strong>{{ selectedDetailItem.customerPhone }}</strong>
                </a>
                <span v-if="selectedDetailItem.customerTotalSpent" class="badge bg-light text-secondary border px-2.5 py-1.5 rounded-3">
                  💳 Tổng chi tiêu: <strong class="text-dark">{{ formatCurrency(selectedDetailItem.customerTotalSpent) }}</strong>
                </span>
              </div>
            </div>
          </div>

          <!-- 2. Schedule & Guests Highlights -->
          <div class="row g-3 mb-3">
            <!-- Thời gian đến -->
            <div class="col-6">
              <div class="p-3 rounded-4 bg-white border h-100 shadow-xs modal-info-tile">
                <div class="d-flex align-items-center justify-content-between mb-1">
                  <span class="text-muted small fw-bold text-uppercase" style="font-size: 0.72rem; letter-spacing: 0.05em;">Thời gian đến</span>
                  <span class="modal-tile-icon text-primary bg-primary-subtle">🕒</span>
                </div>
                <div class="fw-black text-dark fs-4 font-monospace mt-1">
                  {{ formatTime(selectedDetailItem.reservationDate) }}
                </div>
                <div class="d-flex align-items-center gap-1.5 mt-1 flex-wrap">
                  <span class="text-muted small">{{ formatDate(selectedDetailItem.reservationDate) }}</span>
                  <span class="badge border py-0 px-2 rounded-pill small fw-semibold" :class="getRelativeDateBadgeClass(selectedDetailItem.reservationDate)">
                    {{ getRelativeDateLabel(selectedDetailItem.reservationDate) }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Số lượng khách -->
            <div class="col-6">
              <div class="p-3 rounded-4 bg-white border h-100 shadow-xs modal-info-tile">
                <div class="d-flex align-items-center justify-content-between mb-1">
                  <span class="text-muted small fw-bold text-uppercase" style="font-size: 0.72rem; letter-spacing: 0.05em;">Số lượng khách</span>
                  <span class="modal-tile-icon text-purple bg-purple-subtle">👥</span>
                </div>
                <div class="fw-black text-dark fs-4 mt-1">
                  {{ selectedDetailItem.guestCount }} <span class="fs-6 fw-normal text-muted">người</span>
                </div>
                <div class="mt-1">
                  <span v-if="selectedDetailItem.guestCount >= 8" class="badge bg-purple-subtle text-purple border-purple rounded-pill small fw-semibold">
                    🎉 Đoàn tiệc đông
                  </span>
                  <span v-else class="text-muted small">
                    Bàn ăn tiêu chuẩn
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- 3. Table Assignment Section -->
          <div class="modal-section-card p-3 rounded-4 mb-3 border bg-white shadow-xs">
            <div class="d-flex align-items-center justify-content-between mb-2">
              <span class="small text-muted fw-bold text-uppercase" style="font-size: 0.72rem; letter-spacing: 0.05em;">Vị Trí Bàn Ăn</span>
              <button
                v-if="selectedDetailItem.reservationStatus === 'confirmed'"
                type="button"
                class="btn btn-xs btn-outline-primary fw-bold rounded-pill px-3 py-1"
                @click="openAssignTableModal(selectedDetailItem); showDetailModal = false;"
              >
                {{ selectedDetailItem.table || getMergedTableTitle(selectedDetailItem) ? '🔄 Đổi Bàn Khác' : '🪑 Chọn Bàn Ngay' }}
              </button>
            </div>

            <!-- Trường hợp Gộp bàn -->
            <div v-if="getMergedTableTitle(selectedDetailItem)" class="p-3 rounded-3 border border-purple-subtle bg-purple-subtle-50 d-flex align-items-center gap-3">
              <div class="modal-tile-icon bg-purple-subtle text-purple fs-5">
                🔗
              </div>
              <div class="flex-grow-1">
                <div class="fw-bold text-purple fs-6 d-flex align-items-center gap-2">
                  <span>{{ getMergedTableTitle(selectedDetailItem) }}</span>
                  <span class="badge bg-purple text-white px-2 py-0.5 rounded-pill small">Cụm gộp</span>
                </div>
                <div class="small text-muted mt-0.5">
                  📍 Khu vực: <strong>{{ getMergedArea(selectedDetailItem) }}</strong> · Sức chứa đáp ứng hoàn hảo cho đoàn
                </div>
              </div>
            </div>

            <!-- Trường hợp Bàn đơn -->
            <div v-else-if="selectedDetailItem.table" class="p-3 rounded-3 border bg-primary-subtle-30 d-flex align-items-center justify-content-between">
              <div class="d-flex align-items-center gap-3">
                <div class="modal-tile-icon bg-primary-subtle text-primary fs-5">
                  🪑
                </div>
                <div>
                  <div class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
                    <span>{{ selectedDetailItem.table.tableName }}</span>
                    <span class="badge bg-light text-muted border font-monospace small">Mã: {{ selectedDetailItem.table.tableCode }}</span>
                  </div>
                  <div class="small text-muted mt-0.5">
                    📍 Khu vực: <strong>{{ selectedDetailItem.table.areaName }}</strong> · Sức chứa: <strong>{{ selectedDetailItem.table.capacity }} chỗ ngồi</strong>
                  </div>
                </div>
              </div>
              <span class="badge bg-success-subtle text-success border border-success-subtle px-2.5 py-1.5 rounded-pill small fw-semibold">
                🟢 Đã xếp bàn
              </span>
            </div>

            <!-- Trường hợp Chưa xếp bàn -->
            <div v-else class="unassigned-table-box p-3 rounded-3 d-flex align-items-center justify-content-between flex-wrap gap-2">
              <div class="d-flex align-items-center gap-3">
                <div class="modal-tile-icon bg-warning-subtle text-warning fs-5">
                  ⏳
                </div>
                <div>
                  <div class="fw-bold text-dark mb-0">Chưa xếp bàn ăn phục vụ</div>
                  <div class="small text-muted mt-0.5">
                    Khách đã được xác nhận hẹn nhưng chưa có vị trí bàn ngồi cụ thể.
                  </div>
                </div>
              </div>
              <button
                v-if="selectedDetailItem.reservationStatus === 'confirmed'"
                type="button"
                class="btn btn-sm btn-assign-brand shadow-xs"
                @click="openAssignTableModal(selectedDetailItem); showDetailModal = false;"
              >
                🪑 Xếp Bàn Ngay
              </button>
            </div>
          </div>

          <!-- 4. Special Notes -->
          <div class="modal-section-card p-3 rounded-4 mb-3 border bg-white shadow-xs">
            <div class="small text-muted fw-bold text-uppercase mb-2" style="font-size: 0.72rem; letter-spacing: 0.05em;">Ghi Chú & Yêu Cầu Của Khách</div>
            <div v-if="selectedDetailItem.note" class="modal-note-box p-3 rounded-3 bg-light border">
              <div class="d-flex gap-2 align-items-start">
                <span class="fs-5 flex-shrink-0">💬</span>
                <div class="text-dark fst-italic">
                  “{{ selectedDetailItem.note }}”
                </div>
              </div>
            </div>
            <div v-else class="p-2 text-muted small fst-italic">
              Khách không để lại yêu cầu đặc biệt.
            </div>
          </div>

          <!-- 5. Audit & Meta -->
          <div class="d-flex justify-content-between align-items-center small text-muted pt-2 border-top">
            <div>
              Mã phiếu: <strong class="font-monospace text-dark">#{{ selectedDetailItem.reservationId }}</strong>
            </div>
            <div v-if="selectedDetailItem.confirmedByName">
              Người duyệt: <strong class="text-dark">{{ selectedDetailItem.confirmedByName }}</strong>
            </div>
          </div>
        </div>

        <!-- Footer -->
        <div class="p-3 px-4 d-flex justify-content-between align-items-center border-top bg-light">
          <button type="button" class="btn btn-light border px-4 py-2 rounded-3 text-secondary fw-semibold" @click="showDetailModal = false">
            Đóng
          </button>
          <div class="d-flex gap-2">
            <!-- Pending -> Confirm -->
            <button
              v-if="selectedDetailItem.reservationStatus === 'pending'"
              class="btn btn-primary fw-bold px-4 py-2 rounded-3 shadow-sm d-inline-flex align-items-center gap-2"
              @click="handleConfirm(selectedDetailItem); showDetailModal = false;"
            >
              <span>✅</span> Duyệt Đặt Bàn
            </button>

            <!-- Confirmed without table -> Assign Table (Pastel soft tint) -->
            <button
              v-if="selectedDetailItem.reservationStatus === 'confirmed' && !selectedDetailItem.table"
              class="btn btn-assign-brand-modal fw-bold px-4 py-2 rounded-3 d-inline-flex align-items-center gap-2"
              @click="openAssignTableModal(selectedDetailItem); showDetailModal = false;"
            >
              <span>🪑</span> Xếp Bàn Ngay
            </button>

            <!-- Confirmed with table -> Change Table -->
            <button
              v-if="selectedDetailItem.reservationStatus === 'confirmed' && selectedDetailItem.table"
              class="btn btn-light border fw-bold px-3 py-2 rounded-3 text-secondary"
              @click="openAssignTableModal(selectedDetailItem); showDetailModal = false;"
            >
              🪑 Đổi Bàn
            </button>

            <!-- Confirmed with table -> Check-in -->
            <button
              v-if="selectedDetailItem.reservationStatus === 'confirmed' && selectedDetailItem.table"
              class="btn btn-emerald-action text-white fw-bold px-4 py-2 rounded-3 shadow-sm d-inline-flex align-items-center gap-2"
              @click="handleCheckIn(selectedDetailItem); showDetailModal = false;"
            >
              <span>🛎️</span> Check-in Đón Khách
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- CUSTOM DIALOG: XÁC NHẬN / CẢNH BÁO / NHẬP LÝ DO (UI/UX PRO)      -->
    <!-- Thay thế hoàn toàn alert(), confirm(), prompt() mặc định của web-->
    <!-- ============================================================== -->
    <div
      v-if="confirmState.isOpen"
      class="modal-backdrop-custom custom-confirm-backdrop"
      @click.self="handleConfirmDialogCancel"
    >
      <div class="custom-confirm-card bg-white shadow-2xl rounded-4 overflow-hidden border">
        <!-- Header / Icon Graphic -->
        <div class="p-4 text-center pb-2">
          <div
            class="confirm-icon-bubble mx-auto mb-3"
            :class="`confirm-icon-${confirmState.type}`"
          >
            <span>{{ confirmState.icon }}</span>
          </div>

          <h5 class="fw-bold text-dark mb-1">
            {{ confirmState.title }}
          </h5>

          <p class="text-secondary small mb-0 px-2" style="font-size: 0.88rem; line-height: 1.5;">
            {{ confirmState.message }}
          </p>

          <p v-if="confirmState.subMessage" class="text-muted small mt-2 mb-0 px-2 fst-italic" style="font-size: 0.78rem;">
            {{ confirmState.subMessage }}
          </p>
        </div>

        <!-- Optional Context Card (when item is provided) -->
        <div v-if="confirmState.item" class="px-4 py-2">
          <div class="p-2-5 px-3 rounded-3 bg-light border confirm-item-context">
            <div class="d-flex justify-content-between align-items-center mb-1">
              <span class="fw-bold text-dark small d-flex align-items-center gap-1">
                <span>👤</span> {{ confirmState.item.customerName }}
              </span>
              <span class="badge bg-primary-subtle text-primary border font-monospace small">
                📞 {{ confirmState.item.customerPhone }}
              </span>
            </div>
            <div class="d-flex justify-content-between align-items-center small text-muted" style="font-size: 0.75rem;">
              <span>🕒 {{ formatDateTime(confirmState.item.reservationDate) }}</span>
              <span class="badge bg-warning text-dark fw-bold">👥 {{ confirmState.item.guestCount }} khách</span>
            </div>
            <div v-if="confirmState.item.table" class="mt-1 pt-1 border-top small text-success fw-semibold" style="font-size: 0.75rem;">
              🪑 Bàn: {{ getMergedTableTitle(confirmState.item) || confirmState.item.table.tableName }}
            </div>
          </div>
        </div>

        <!-- Optional Input Field (Prompt Mode for cancellation reason, etc.) -->
        <div v-if="confirmState.showInput" class="px-4 py-2">
          <label class="form-label small fw-bold text-dark mb-1">
            {{ confirmState.inputLabel || 'Lý do:' }}
          </label>
          <input
            type="text"
            class="form-control form-control-sm mb-2"
            v-model.trim="confirmState.inputValue"
            :placeholder="confirmState.inputPlaceholder || 'Nhập nội dung...'"
            @keyup.enter="handleConfirmDialogSubmit"
            autofocus
          />
          <div class="d-flex flex-wrap gap-1">
            <button
              v-for="quick in ['Khách bận đột xuất', 'Khách đổi ngày hẹn', 'Khách không đến', 'Trùng lịch đặt']"
              :key="quick"
              type="button"
              class="btn btn-xs btn-outline-secondary rounded-pill"
              @click="confirmState.inputValue = quick"
            >
              {{ quick }}
            </button>
          </div>
        </div>

        <!-- Footer Actions -->
        <div class="p-3 px-4 d-flex justify-content-end gap-2 border-top bg-light mt-2">
          <button
            type="button"
            class="btn btn-light border px-3 rounded-3 fw-semibold text-secondary"
            @click="handleConfirmDialogCancel"
          >
            {{ confirmState.cancelText }}
          </button>
          <button
            type="button"
            class="btn fw-bold px-4 rounded-3 shadow-sm d-inline-flex align-items-center gap-1"
            :class="getConfirmButtonClass(confirmState.type)"
            @click="handleConfirmDialogSubmit"
          >
            <span>{{ confirmState.confirmText }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- TOAST NOTIFICATION STACK (MODERN GLASSMORPHISM FLOATING)       -->
    <!-- ============================================================== -->
    <div class="custom-toast-container" aria-live="polite">
      <transition-group name="toast-anim">
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="custom-toast-item"
          :class="`toast-${toast.type}`"
        >
          <div class="toast-accent-bar" :class="`bar-${toast.type}`"></div>
          <div class="toast-icon-box" :class="`icon-box-${toast.type}`">
            <span v-if="toast.type === 'success'">✓</span>
            <span v-else-if="toast.type === 'error' || toast.type === 'danger'">✕</span>
            <span v-else-if="toast.type === 'warning'">⚠️</span>
            <span v-else>ℹ️</span>
          </div>
          <div class="toast-content flex-grow-1 min-w-0">
            <div class="toast-title fw-bold text-dark small">{{ toast.title }}</div>
            <div class="toast-message text-muted small">{{ toast.message }}</div>
          </div>
          <button
            type="button"
            class="toast-close-btn"
            @click="removeToast(toast.id)"
            title="Đóng"
          >
            ✕
          </button>
        </div>
      </transition-group>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import axios from 'axios';
import signalRService from '@/services/signalr';

const API_BASE = 'http://localhost:5079/api';

// ==============================================================
// CUSTOM CONFIRM DIALOG & TOAST NOTIFICATION (UI/UX PRO)
// ==============================================================
const confirmState = ref({
  isOpen: false,
  title: '',
  message: '',
  subMessage: '',
  type: 'primary', // 'primary' | 'success' | 'warning' | 'danger'
  icon: '📋',
  confirmText: 'Xác Nhận',
  cancelText: 'Hủy',
  item: null,
  showInput: false,
  inputLabel: '',
  inputValue: '',
  inputPlaceholder: '',
  resolve: null
});

function askConfirm(options) {
  return new Promise((resolve) => {
    confirmState.value = {
      isOpen: true,
      title: options.title || 'Xác nhận thao tác',
      message: options.message || '',
      subMessage: options.subMessage || '',
      type: options.type || 'primary',
      icon: options.icon || (options.type === 'danger' ? '🛑' : options.type === 'success' ? '🛎️' : options.type === 'warning' ? '⚠️' : '📋'),
      confirmText: options.confirmText || 'Xác Nhận',
      cancelText: options.cancelText || 'Hủy',
      item: options.item || null,
      showInput: !!options.showInput,
      inputLabel: options.inputLabel || '',
      inputValue: options.inputValue || '',
      inputPlaceholder: options.inputPlaceholder || '',
      resolve
    };
  });
}

function handleConfirmDialogSubmit() {
  const res = confirmState.value.resolve;
  const isInput = confirmState.value.showInput;
  const val = confirmState.value.inputValue;
  confirmState.value.isOpen = false;
  if (res) {
    if (isInput) {
      res({ confirmed: true, value: val });
    } else {
      res(true);
    }
  }
}

function handleConfirmDialogCancel() {
  const res = confirmState.value.resolve;
  const isInput = confirmState.value.showInput;
  confirmState.value.isOpen = false;
  if (res) {
    if (isInput) {
      res({ confirmed: false, value: null });
    } else {
      res(false);
    }
  }
}

function getConfirmButtonClass(type) {
  switch (type) {
    case 'danger':
      return 'btn-danger';
    case 'success':
      return 'btn-success text-white';
    case 'warning':
      return 'btn-warning text-dark';
    case 'primary':
    default:
      return 'btn-primary';
  }
}

const toasts = ref([]);
let toastCounter = 0;

function showToast(message, type = 'success', title = '') {
  const id = ++toastCounter;
  let finalTitle = title;
  if (!finalTitle) {
    if (type === 'success') finalTitle = 'Thành công';
    else if (type === 'error' || type === 'danger') finalTitle = 'Thông báo lỗi';
    else if (type === 'warning') finalTitle = 'Lưu ý';
    else finalTitle = 'Thông báo';
  }
  toasts.value.push({ id, message, type, title: finalTitle });
  setTimeout(() => {
    removeToast(id);
  }, 4200);
}

function removeToast(id) {
  toasts.value = toasts.value.filter(t => t.id !== id);
}

const reservations = ref([]);
const availableTables = ref([]);
const loading = ref(false);

const stats = ref({
  totalToday: 0,
  pendingCount: 0,
  confirmedCount: 0,
  checkedInCount: 0,
  cancelledCount: 0
});

function getLocalDateString(dateObj = new Date()) {
  const year = dateObj.getFullYear();
  const month = String(dateObj.getMonth() + 1).padStart(2, '0');
  const day = String(dateObj.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

// Filters
const filterDateQuick = ref('today');
const filterDate = ref(getLocalDateString(new Date()));
const filterStatus = ref('all');
const searchKeyword = ref('');

// Computed dynamic titles and subtitles for KPI metric cards based on selected date
const currentDateLabel = computed(() => {
  if (filterDateQuick.value === 'today') return 'hôm nay';
  if (filterDateQuick.value === 'tomorrow') return 'ngày mai';
  if (filterDateQuick.value === 'all' || !filterDate.value) return 'toàn bộ';
  try {
    const parts = filterDate.value.split('-');
    if (parts.length === 3) {
      return `ngày ${parts[2]}/${parts[1]}/${parts[0]}`;
    }
  } catch {}
  return `ngày ${filterDate.value}`;
});

const totalCardTitle = computed(() => {
  if (filterDateQuick.value === 'today') return 'Tổng Lịch Hẹn Hôm Nay';
  if (filterDateQuick.value === 'tomorrow') return 'Tổng Lịch Hẹn Ngày Mai';
  if (filterDateQuick.value === 'all' || !filterDate.value) return 'Tổng Lịch Hẹn (Toàn Bộ)';
  try {
    const parts = filterDate.value.split('-');
    if (parts.length === 3) return `Tổng Lịch Hẹn (${parts[2]}/${parts[1]}/${parts[0]})`;
  } catch {}
  return `Tổng Lịch Hẹn (${filterDate.value})`;
});

const totalCardSubtitle = computed(() => {
  if (filterDateQuick.value === 'today') return 'Toàn bộ khách hẹn hôm nay';
  if (filterDateQuick.value === 'tomorrow') return 'Toàn bộ khách hẹn ngày mai';
  if (filterDateQuick.value === 'all' || !filterDate.value) return 'Tất cả lịch hẹn từ trước đến nay';
  return `Toàn bộ khách hẹn ${currentDateLabel.value}`;
});

const pendingCardSubtitle = computed(() => {
  if (filterDateQuick.value === 'today') return 'Cần kiểm tra và gọi xác nhận hôm nay';
  if (filterDateQuick.value === 'tomorrow') return 'Cần kiểm tra và gọi xác nhận ngày mai';
  if (filterDateQuick.value === 'all' || !filterDate.value) return 'Cần kiểm tra và gọi xác nhận toàn bộ';
  return `Cần kiểm tra và gọi xác nhận ${currentDateLabel.value}`;
});

const confirmedCardSubtitle = computed(() => {
  if (filterDateQuick.value === 'today') return 'Chờ phân bàn hoặc sẵn sàng đón hôm nay';
  if (filterDateQuick.value === 'tomorrow') return 'Đã duyệt cho ngày mai';
  if (filterDateQuick.value === 'all' || !filterDate.value) return 'Đã duyệt toàn bộ';
  return `Chờ phân bàn hoặc sẵn sàng đón ${currentDateLabel.value}`;
});

const checkedInCardSubtitle = computed(() => {
  if (filterDateQuick.value === 'today') return 'Đang phục vụ tại bàn ăn hôm nay';
  if (filterDateQuick.value === 'tomorrow') return 'Khách check-in ngày mai';
  if (filterDateQuick.value === 'all' || !filterDate.value) return 'Đang phục vụ tại bàn ăn toàn bộ';
  return `Đang phục vụ tại bàn ăn ${currentDateLabel.value}`;
});

// Details Modal state
const showDetailModal = ref(false);
const selectedDetailItem = ref(null);

// Modals & Assign Table state
const showAssignModal = ref(false);
const selectedReservation = ref(null);
const assignMode = ref('single'); // 'single': Xếp 1 bàn theo số người, 'merge': Gộp nhiều bàn trống
const selectedSingleTableId = ref(null);
const selectedMergeTableIds = ref([]);
const selectedMergeArea = ref(''); // Khu vực đang chọn để gộp bàn (bắt buộc cùng khu vực)
const submittingAssign = ref(false);
const selectedAreaTab = ref('all');

// Bàn khả dụng cho việc đặt bàn: CHỈ LẤY CÁC BÀN TRỐNG (không lấy bàn đang phục vụ)
// hoặc bàn hiện tại đang được giữ chỗ cho phiếu này
const allEmptyTables = computed(() => {
  return availableTables.value.filter(t => {
    if (t.tableStatus === 'disabled') return false;
    if (t.tableStatus === 'empty') return true;
    if (selectedReservation.value?.table && t.tableId === selectedReservation.value.table.tableId) return true;
    return false;
  });
});

// Chế độ 1: Danh sách bàn đơn trống có sức chứa >= số khách, sắp xếp vừa vặn nhất lên đầu
const singleEligibleTables = computed(() => {
  if (!selectedReservation.value) return [];
  const guests = selectedReservation.value.guestCount || 1;
  return allEmptyTables.value
    .filter(t => t.capacity >= guests)
    .sort((a, b) => a.capacity - b.capacity);
});

const singleEligibleCount = computed(() => singleEligibleTables.value.length);

const filteredSingleTables = computed(() => {
  if (selectedAreaTab.value === 'all') return singleEligibleTables.value;
  return singleEligibleTables.value.filter(t => t.areaName === selectedAreaTab.value);
});

const selectedSingleTable = computed(() => {
  if (!selectedSingleTableId.value) return null;
  return allEmptyTables.value.find(t => t.tableId === selectedSingleTableId.value) || null;
});

// Chế độ 2: Gộp nhiều bàn trống (CHỈ HIỂN THỊ VÀ GỘP CÁC BÀN TRONG CÙNG KHU VỰC ĐÃ CHỌN)
const filteredMergeTables = computed(() => {
  if (!selectedMergeArea.value) {
    if (areaList.value.length > 0) {
      return allEmptyTables.value.filter(t => t.areaName === areaList.value[0]);
    }
    return [];
  }
  return allEmptyTables.value.filter(t => t.areaName === selectedMergeArea.value);
});

const selectedMergeTables = computed(() => {
  return selectedMergeTableIds.value
    .map(id => allEmptyTables.value.find(t => t.tableId === id))
    .filter(Boolean);
});

const totalMergeCapacity = computed(() => {
  return selectedMergeTables.value.reduce((sum, t) => sum + (t?.capacity || 0), 0);
});

const normalizeArea = area => (area || 'Sảnh chung').trim().toLowerCase();

const mergeAreasAreValid = computed(() => {
  if (selectedMergeTables.value.length <= 1) return true;
  const firstArea = normalizeArea(selectedMergeTables.value[0]?.areaName);
  return selectedMergeTables.value.every(table => normalizeArea(table.areaName) === firstArea);
});

async function changeMergeArea(area) {
  if (selectedMergeArea.value === area) return;
  if (selectedMergeTableIds.value.length > 0) {
    const ok = await askConfirm({
      title: 'Đổi Khu Vực Gộp Bàn',
      icon: '📍',
      type: 'warning',
      message: `Bạn đang chọn ${selectedMergeTableIds.value.length} bàn tại khu vực "${selectedMergeArea.value}".`,
      subMessage: `Quy tắc hệ thống: Chỉ được gộp các bàn trong CÙNG MỘT khu vực. Đổi sang khu vực "${area}" sẽ bỏ chọn các bàn cũ để hiển thị bàn của khu vực mới.`,
      confirmText: `Đổi Sang "${area}"`,
      cancelText: 'Giữ Khu Vực Cũ'
    });
    if (!ok) return;
    selectedMergeTableIds.value = [];
  }
  selectedMergeArea.value = area;
}

function clearMergeSelections() {
  selectedMergeTableIds.value = [];
}

function toggleMergeTableSelection(tableId) {
  const targetTable = allEmptyTables.value.find(t => t.tableId === tableId);
  const index = selectedMergeTableIds.value.indexOf(tableId);

  if (index > -1) {
    selectedMergeTableIds.value.splice(index, 1);
    return;
  }

  if (!targetTable) return;

  // Đảm bảo chỉ được gộp bàn trong cùng khu vực
  if (!selectedMergeArea.value) {
    selectedMergeArea.value = targetTable.areaName || 'Sảnh chung';
  } else if (normalizeArea(targetTable.areaName) !== normalizeArea(selectedMergeArea.value)) {
    showToast(`Bàn "${targetTable.tableName}" thuộc khu vực "${targetTable.areaName}", không thể gộp cùng khu vực "${selectedMergeArea.value}". Vui lòng chọn các bàn cùng khu vực.`, 'warning', 'Khác khu vực');
    return;
  }

  selectedMergeTableIds.value.push(tableId);
}

watch(assignMode, (newMode) => {
  if (newMode === 'merge') {
    if (selectedSingleTable.value?.areaName) {
      selectedMergeArea.value = selectedSingleTable.value.areaName;
      if (selectedMergeTableIds.value.length === 0) {
        selectedMergeTableIds.value = [selectedSingleTable.value.tableId];
      }
    } else if (!selectedMergeArea.value && areaList.value.length > 0) {
      const areaWithEmpty = areaList.value.find(a => getEmptyAreaCount(a) > 0);
      selectedMergeArea.value = areaWithEmpty || areaList.value[0];
    }
  }
});

function getSingleAreaCount(area) {
  return singleEligibleTables.value.filter(t => t.areaName === area).length;
}

function getEmptyAreaCount(area) {
  return allEmptyTables.value.filter(t => t.areaName === area).length;
}

const isAssignSubmitDisabled = computed(() => {
  if (assignMode.value === 'single') {
    return !selectedSingleTableId.value;
  }
  return selectedMergeTableIds.value.length === 0 || !mergeAreasAreValid.value;
});

function getAssignSubmitLabel() {
  if (assignMode.value === 'single') {
    return selectedSingleTable.value
      ? `Xác Nhận Xếp Bàn ${selectedSingleTable.value.tableName} (${selectedSingleTable.value.capacity} chỗ)`
      : 'Xác Nhận Xếp Bàn';
  }
  return selectedMergeTableIds.value.length > 1
    ? `Xác Nhận Gộp ${selectedMergeTableIds.value.length} Bàn (${totalMergeCapacity.value} chỗ)`
    : 'Xác Nhận Xếp Bàn';
}

function getMergedTableTitle(item) {
  if (!item?.note) return '';
  const match = item.note.match(/\[Gộp bàn:\s*([^\|\]]+)/);
  if (match && match[1]) {
    return match[1].trim();
  }
  return '';
}

// Hotline Booking Modal
const showCreateModal = ref(false);
const submittingCreate = ref(false);
const createForm = ref({
  customerName: '',
  customerPhone: '',
  reservationDate: '',
  guestCount: 2,
  note: ''
});

onMounted(async () => {
  await fetchTables();
  await fetchReservations();
  initSignalR();
});

onUnmounted(() => {
  signalRService.off('ReservationUpdated', handleSignalRUpdate);
  signalRService.off('StaffOrderUpdated', handleSignalRUpdate);
});

function initSignalR() {
  signalRService.on('ReservationUpdated', handleSignalRUpdate);
  signalRService.on('StaffOrderUpdated', handleSignalRUpdate);
}

function handleSignalRUpdate() {
  fetchReservations();
  fetchTables();
}

function quickFilterCard(status) {
  filterStatus.value = status;
  fetchReservations();
}

function setQuickDate(type) {
  filterDateQuick.value = type;
  const now = new Date();
  if (type === 'today') {
    filterDate.value = getLocalDateString(now);
  } else if (type === 'tomorrow') {
    const tmr = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
    filterDate.value = getLocalDateString(tmr);
  } else {
    filterDate.value = '';
  }
  fetchReservations();
}

function onDatePickerChange() {
  if (!filterDate.value) {
    filterDateQuick.value = 'all';
    fetchReservations();
    return;
  }
  const now = new Date();
  const todayStr = getLocalDateString(now);
  const tmrStr = getLocalDateString(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1));

  if (filterDate.value === todayStr) {
    filterDateQuick.value = 'today';
  } else if (filterDate.value === tmrStr) {
    filterDateQuick.value = 'tomorrow';
  } else {
    filterDateQuick.value = 'custom';
  }
  fetchReservations();
}

function clearSearch() {
  searchKeyword.value = '';
  fetchReservations();
}

async function fetchTables(date = null, excludeReservationId = null) {
  try {
    const params = {};
    if (date) params.date = date;
    if (excludeReservationId) params.excludeReservationId = excludeReservationId;
    const res = await axios.get(`${API_BASE}/admin/reservations/tables`, { params });
    if (res.data?.success) {
      availableTables.value = res.data.data;
    }
  } catch (err) {
    console.error('Lỗi tải danh sách bàn:', err);
  }
}

async function fetchReservations() {
  loading.value = true;
  try {
    const params = {
      status: filterStatus.value,
      search: searchKeyword.value
    };
    if (filterDateQuick.value !== 'all' && filterDate.value) {
      params.date = filterDate.value;
    }

    const res = await axios.get(`${API_BASE}/admin/reservations`, { params });
    if (res.data?.success) {
      reservations.value = res.data.data || [];
      if (res.data.stats) {
        stats.value = res.data.stats;
      }
    }
  } catch (err) {
    console.error('Lỗi tải danh sách đặt bàn:', err);
  } finally {
    loading.value = false;
  }
}

// Distinct areas list
const areaList = computed(() => {
  const set = new Set();
  availableTables.value.forEach(t => {
    if (t.areaName) set.add(t.areaName);
  });
  return Array.from(set);
});

async function openAssignTableModal(reservation) {
  if (reservation.reservationStatus !== 'confirmed') {
    showToast('Vui lòng duyệt tiếp nhận phiếu đặt bàn trước khi phân bàn cho khách.', 'warning', 'Chưa duyệt phiếu');
    return;
  }
  selectedReservation.value = reservation;
  selectedAreaTab.value = 'all';

  // Tải danh sách bàn khả dụng theo ĐÚNG NGÀY HẸN của phiếu đặt bàn
  await fetchTables(reservation.reservationDate, reservation.reservationId);

  // Lấy các bàn đã gộp trước đó nếu có
  const initialMergedIds = [];
  if (reservation.note && reservation.note.includes('MergedTableIds:')) {
    try {
      const marker = 'MergedTableIds:';
      const start = reservation.note.indexOf(marker) + marker.length;
      const end = reservation.note.indexOf(']', start);
      const raw = (end > start ? reservation.note.substring(start, end) : reservation.note.substring(start)).split(',');
      raw.forEach(idStr => {
        const parsed = parseInt(idStr.trim(), 10);
        if (!isNaN(parsed) && !initialMergedIds.includes(parsed)) initialMergedIds.push(parsed);
      });
    } catch { }
  }

  // Khởi tạo khu vực gộp ban đầu (chỉ gộp các bàn cùng khu vực)
  let initialArea = reservation.table?.areaName || '';
  if (initialMergedIds.length > 0) {
    const firstTable = availableTables.value.find(t => t.tableId === initialMergedIds[0]);
    if (firstTable?.areaName) initialArea = firstTable.areaName;
  }
  if (!initialArea && areaList.value.length > 0) {
    const areaWithEmpty = areaList.value.find(a => getEmptyAreaCount(a) > 0);
    initialArea = areaWithEmpty || areaList.value[0];
  }
  selectedMergeArea.value = initialArea;

  // Kiểm tra xem có bàn đơn nào đủ sức chứa không
  const guests = reservation.guestCount || 1;
  const hasEligibleSingle = allEmptyTables.value.some(t => t.capacity >= guests);

  if (initialMergedIds.length > 1) {
    // Nếu phiếu này đã gộp nhiều bàn trước đó -> mở tab gộp
    assignMode.value = 'merge';
    selectedMergeTableIds.value = initialMergedIds;
    selectedSingleTableId.value = null;
  } else if (hasEligibleSingle) {
    // Có bàn đơn đủ sức chứa -> ƯU TIÊN MỞ CHẾ ĐỘ XẾP 1 BÀN THEO SỐ NGƯỜI
    assignMode.value = 'single';
    selectedSingleTableId.value = reservation.table?.tableId || null;
    selectedMergeTableIds.value = [];
  } else {
    // KHÔNG CÓ BÀN ĐƠN NÀO ĐỦ SỨC CHỨA -> TỰ ĐỘNG CHUYỂN SANG CHẾ ĐỘ GỘP BÀN TRỐNG
    assignMode.value = 'merge';
    selectedMergeTableIds.value = reservation.table?.tableId ? [reservation.table.tableId] : [];
    selectedSingleTableId.value = null;
  }

  showAssignModal.value = true;
}

async function handleConfirm(item) {
  const confirmed = await askConfirm({
    title: 'Tiếp Nhận Đặt Bàn',
    icon: '📋',
    type: 'primary',
    message: `Xác nhận tiếp nhận lịch đặt bàn của khách ${item.customerName}?`,
    subMessage: 'Sau bước này bạn có thể phân bàn phù hợp hoặc check-in đón khách.',
    item: item,
    confirmText: 'Duyệt Tiếp Nhận',
    cancelText: 'Quay Lại'
  });
  if (!confirmed) return;

  try {
    const res = await axios.post(`${API_BASE}/admin/reservations/${item.reservationId}/confirm`);
    if (res.data?.success) {
      showToast(res.data.message || 'Đã tiếp nhận lịch đặt bàn thành công!', 'success', 'Duyệt thành công');
      await fetchReservations();
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Không thể xác nhận phiếu đặt bàn.', 'error', 'Lỗi tiếp nhận');
  }
}

async function submitAssignTable() {
  if (!selectedReservation.value) return;

  let targetIds = [];
  if (assignMode.value === 'single') {
    if (!selectedSingleTableId.value) return;
    targetIds = [selectedSingleTableId.value];
  } else {
    if (selectedMergeTableIds.value.length === 0) return;
    if (!mergeAreasAreValid.value) {
      showToast('Chỉ được gộp các bàn trong cùng một khu vực.', 'warning', 'Ràng buộc khu vực');
      return;
    }
    targetIds = selectedMergeTableIds.value;
  }

  submittingAssign.value = true;
  try {
    const res = await axios.post(`${API_BASE}/admin/reservations/${selectedReservation.value.reservationId}/assign-table`, {
      tableId: targetIds[0],
      tableIds: targetIds
    });

    if (res.data?.success) {
      showToast(res.data.message || 'Xếp bàn thành công!', 'success', 'Hoàn tất xếp bàn');
      showAssignModal.value = false;
      await fetchReservations();
      await fetchTables();
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi xếp bàn.', 'error', 'Lỗi xếp bàn');
  } finally {
    submittingAssign.value = false;
  }
}

async function handleCheckIn(item) {
  if (!item.table) {
    showToast('Vui lòng phân bàn trước khi check-in cho khách.', 'warning', 'Chưa xếp bàn');
    return;
  }

  const mergedInfo = getMergedTableTitle(item);
  const targetName = mergedInfo ? mergedInfo : item.table.tableName;

  const confirmed = await askConfirm({
    title: 'Check-in Đón Khách Vào Bàn',
    icon: '🛎️',
    type: 'success',
    message: `Xác nhận đón khách ${item.customerName} vào bàn ${targetName}?`,
    subMessage: 'Hệ thống sẽ tự động kích hoạt tất cả các bàn sang trạng thái "Đang phục vụ" (Serving) và tạo Order mở mới cho bàn này.',
    item: item,
    confirmText: 'Xác Nhận Đón Khách',
    cancelText: 'Chưa Đón'
  });
  if (!confirmed) return;

  try {
    const res = await axios.post(`${API_BASE}/admin/reservations/${item.reservationId}/check-in`);
    if (res.data?.success) {
      showToast(res.data.message || 'Đã đón khách vào bàn thành công!', 'success', 'Check-in thành công');
      await fetchReservations();
      await fetchTables();
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi check-in cho khách.', 'error', 'Lỗi check-in');
  }
}

async function handleCancel(item) {
  const result = await askConfirm({
    title: 'Hủy Lịch Đặt Bàn',
    icon: '🛑',
    type: 'danger',
    message: `Bạn có chắc chắn muốn hủy lịch hẹn của khách ${item.customerName}?`,
    subMessage: 'Thao tác này sẽ giải phóng bàn đã xếp (nếu có) và đánh dấu phiếu bị hủy.',
    item: item,
    showInput: true,
    inputLabel: 'Lý do hủy đặt bàn (tùy chọn):',
    inputValue: 'Khách bận đột xuất',
    inputPlaceholder: 'Nhập lý do hủy...',
    confirmText: 'Xác Nhận Hủy Phiếu',
    cancelText: 'Giữ Lại Phiếu'
  });
  if (!result || !result.confirmed) return;

  try {
    const res = await axios.post(`${API_BASE}/admin/reservations/${item.reservationId}/cancel`, {
      reason: result.value || 'Khách hủy'
    });
    if (res.data?.success) {
      showToast('Đã hủy đặt bàn thành công.', 'success', 'Đã hủy phiếu');
      await fetchReservations();
      await fetchTables();
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi hủy đặt bàn.', 'error', 'Lỗi hủy');
  }
}

function openCreateModal() {
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  createForm.value = {
    customerName: '',
    customerPhone: '',
    reservationDate: now.toISOString().slice(0, 16),
    guestCount: 4,
    note: ''
  };
  showCreateModal.value = true;
}

async function submitCreateReservation() {
  if (!createForm.value.customerName || !createForm.value.customerPhone || !createForm.value.reservationDate) {
    showToast('Vui lòng điền đầy đủ họ tên, số điện thoại và thời gian hẹn.', 'warning', 'Thiếu thông tin');
    return;
  }

  submittingCreate.value = true;
  try {
    const res = await axios.post(`${API_BASE}/admin/reservations/create`, {
      customerName: createForm.value.customerName,
      customerPhone: createForm.value.customerPhone,
      reservationDate: createForm.value.reservationDate,
      guestCount: createForm.value.guestCount,
      note: createForm.value.note
    });

    if (res.data?.success) {
      showToast('Đã tiếp nhận đặt bàn hotline thành công!', 'success', 'Đặt bàn thành công');
      showCreateModal.value = false;
      await fetchReservations();
      await fetchTables();
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi tạo đặt bàn.', 'error', 'Lỗi tạo phiếu');
  } finally {
    submittingCreate.value = false;
  }
}

function getInitials(name) {
  if (!name) return 'KH';
  const parts = name.trim().split(' ');
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function formatTime(dtStr) {
  if (!dtStr) return '--:--';
  const d = new Date(dtStr);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function formatDate(dtStr) {
  if (!dtStr) return '';
  const d = new Date(dtStr);
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatDateTime(dtStr) {
  if (!dtStr) return '';
  const d = new Date(dtStr);
  return d.toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' });
}

function getRelativeDateLabel(dtStr) {
  if (!dtStr) return '';
  const d = new Date(dtStr);
  const now = new Date();
  const dDate = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const todayDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const diffDays = Math.round((dDate - todayDate) / 86400000);

  if (diffDays === 0) return 'Hôm nay';
  if (diffDays === 1) return 'Ngày mai';
  if (diffDays === -1) return 'Hôm qua';
  if (diffDays > 1) return `Còn ${diffDays} ngày`;
  return 'Đã qua';
}

function getStatusBadgeClass(status) {
  switch (status) {
    case 'pending': return 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
    case 'confirmed': return 'bg-primary-subtle text-primary border border-primary-subtle';
    case 'checked_in': return 'bg-success-subtle text-success border border-success-subtle';
    case 'completed': return 'bg-secondary-subtle text-secondary border';
    case 'cancelled': case 'rejected': return 'bg-danger-subtle text-danger border border-danger-subtle';
    case 'no_show': return 'bg-dark-subtle text-dark border';
    default: return 'bg-light text-dark border';
  }
}

function getStatusDotClass(status) {
  switch (status) {
    case 'pending': return 'bg-warning';
    case 'confirmed': return 'bg-primary';
    case 'checked_in': return 'bg-success';
    case 'completed': return 'bg-secondary';
    case 'cancelled': case 'rejected': return 'bg-danger';
    case 'no_show': return 'bg-dark';
    default: return 'bg-secondary';
  }
}

function getReservationStatusText(item) {
  switch (item.reservationStatus) {
    case 'pending': return 'Chờ duyệt';
    case 'confirmed': return item.table ? 'Đã xếp bàn' : 'Chờ xếp bàn';
    case 'checked_in': return 'Đã Check-in';
    case 'completed': return 'Hoàn tất';
    case 'cancelled': return 'Đã hủy';
    case 'rejected': return 'Từ chối';
    case 'no_show': return 'Vắng mặt';
    default: return item.reservationStatus;
  }
}

function openDetailModal(item) {
  selectedDetailItem.value = item;
  showDetailModal.value = true;
}

function getCleanCustomerNote(rawNote) {
  if (!rawNote) return '';
  let clean = rawNote
    .replace(/\[Gộp bàn:[^\]]+\]/gi, '')
    .replace(/\[MergedTableIds:[^\]]+\]/gi, '')
    .replace(/\[Hủy bởi[^\]]+\]/gi, '')
    .replace(/\s*-\s*\|\s*/g, ' ')
    .replace(/\s*\|\s*/g, ' ')
    .replace(/^\s*-\s*/, '')
    .replace(/\s*-\s*$/, '')
    .trim();
  return clean;
}

function getCancellationReason(rawNote) {
  if (!rawNote) return '';
  const match = rawNote.match(/\[Hủy bởi[^:]*:\s*([^\]]+)\]/i);
  return match ? match[1].trim() : '';
}

function getMergedShortLabel(item) {
  if (!item?.note) return 'Cụm gộp bàn';
  const match = item.note.match(/\[Gộp bàn:\s*([^\|\]]+)/);
  if (!match || !match[1]) return 'Cụm gộp bàn';
  const raw = match[1].trim();

  // Extract total capacity if present
  const capMatch = raw.match(/\(Tổng\s*(\d+)\s*chỗ\)/i);
  const capText = capMatch ? `${capMatch[1]} chỗ` : '';

  // Extract table names cleanly
  const tableParts = raw.split('+').map(p => {
    return p.replace(/\([^)]*\)/g, '').trim();
  }).filter(Boolean);

  if (tableParts.length > 0) {
    const joined = tableParts.join(' + ');
    return capText ? `${joined} (${capText})` : joined;
  }
  return raw;
}

function getMergedArea(item) {
  if (item?.table?.areaName) return item.table.areaName;
  if (!item?.note) return 'Khu vực';
  const match = item.note.match(/\(([^)]+)\)/);
  return match ? match[1] : 'Khu vực';
}

function formatCurrency(val) {
  if (!val) return '0 ₫';
  return Number(val).toLocaleString('vi-VN') + ' ₫';
}

function getRelativeDateBadgeClass(dtStr) {
  if (!dtStr) return 'bg-light text-muted';
  const d = new Date(dtStr);
  const now = new Date();
  const dDate = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const todayDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const diffDays = Math.round((dDate - todayDate) / 86400000);

  if (diffDays === 0) return 'bg-primary-subtle text-primary border-primary-subtle fw-bold';
  if (diffDays === 1) return 'bg-info-subtle text-info border-info-subtle fw-bold';
  if (diffDays > 1) return 'bg-success-subtle text-success border-success-subtle';
  return 'bg-light text-muted';
}
</script>

<style scoped>
.reservation-view {
  min-height: 100%;
}

.fw-black {
  font-weight: 800;
}

.fs-7 {
  font-size: 0.85rem;
}

.shadow-xs {
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
}

.cursor-pointer {
  cursor: pointer;
}

/* Header Hotline Button */
.hotline-btn {
  background: linear-gradient(135deg, #4f46e5 0%, #3b82f6 100%);
  border: none;
  transition: all 0.2s ease;
}
.hotline-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px -4px rgba(79, 70, 229, 0.35);
}

/* KPI Metric Cards */
.kpi-metric-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease, border-color 0.2s ease;
  border: 2px solid transparent !important;
}
.kpi-metric-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 28px -6px rgba(15, 23, 42, 0.08) !important;
}
.kpi-card-active {
  border-color: #3b82f6 !important;
  background-color: #f8fafc !important;
}
.kpi-icon-badge {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  font-size: 1.25rem;
}

/* Filter Toolbar */
.filter-toolbar {
  border: 1px solid rgba(226, 232, 240, 0.8);
}
.date-segment-btn {
  border-radius: 8px;
  font-weight: 600;
  color: #64748b;
  border: none;
  padding: 4px 12px;
  transition: all 0.15s ease;
}
.date-segment-btn.active {
  background-color: #0f172a;
  color: #ffffff;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

/* Table Styling & Geometry */
.table-card-container {
  border: 1px solid #e2e8f0 !important;
}

.custom-res-table {
  width: 100%;
  table-layout: fixed;
  border-collapse: separate;
  border-spacing: 0;
}

.custom-res-table thead th {
  background-color: #f8fafc;
  color: #475569;
  font-size: 0.73rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  padding: 10px 6px;
  border-bottom: 2px solid #e2e8f0;
  border-top: none;
  vertical-align: middle;
}

.custom-res-table td {
  padding: 8px 6px;
  vertical-align: middle;
  border-bottom: 1px solid #f1f5f9;
}

.res-table-row {
  transition: background-color 0.15s ease;
}
.res-table-row:hover {
  background-color: #f8fafc;
}

/* Non-sticky In-flow Actions Column (Never overlaps or covers status!) */
.actions-header {
  background-color: #f8fafc !important;
}

.actions-cell {
  background-color: transparent;
}

/* Avatar Circle */
.customer-avatar-circle {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
  color: #1d4ed8;
  font-weight: 700;
  font-size: 0.72rem;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: 1px solid #bfdbfe;
}

/* Status Pill Badge & Indicator Dot */
.status-pill-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 0.72rem;
  padding: 3px 8px !important;
  white-space: nowrap;
}

.status-indicator-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  display: inline-block;
  flex-shrink: 0;
}

/* ID Badge */
.res-id-badge {
  background-color: #f1f5f9;
  color: #475569;
  border: 1px solid #e2e8f0;
  font-size: 0.72rem;
  padding: 2px 6px;
  border-radius: 5px;
  font-weight: 600;
}

/* Customer & Links */
.phone-link {
  transition: color 0.15s ease;
}
.phone-link:hover {
  color: #2563eb !important;
}

/* Note Bubble */
.res-note-bubble {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  background-color: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  padding: 2px 6px;
  font-size: 0.74rem;
  color: #334155;
  max-width: 100%;
  width: auto;
  transition: all 0.15s ease;
}
.res-note-bubble:hover {
  background-color: #f1f5f9;
  border-color: #cbd5e1;
}
.note-quote-icon {
  font-size: 0.8rem;
  flex-shrink: 0;
}
.note-quote-text {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.vip-point-badge {
  font-size: 0.65rem;
  padding: 1px 5px;
}

/* Action Buttons (UI/UX Pro) */
.btn-primary-action {
  background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
  color: #ffffff;
  border: none;
  border-radius: 6px;
  height: 28px;
  padding: 0 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 3px;
  font-size: 0.75rem;
  white-space: nowrap;
  transition: all 0.15s ease;
}
.btn-primary-action:hover {
  background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(37, 99, 235, 0.25);
  color: #ffffff;
}

.btn-assign-brand {
  background-color: #eef2ff;
  color: #4338ca !important;
  border: 1px solid #c7d2fe;
  border-radius: 6px;
  height: 28px;
  padding: 0 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  font-size: 0.75rem;
  font-weight: 700;
  white-space: nowrap;
  transition: all 0.15s ease;
  box-shadow: 0 1px 2px rgba(79, 70, 229, 0.08);
}
.btn-assign-brand:hover {
  background-color: #e0e7ff;
  color: #3730a3 !important;
  border-color: #a5b4fc;
  transform: translateY(-1px);
  box-shadow: 0 2px 5px rgba(79, 70, 229, 0.15);
}

.btn-assign-brand-modal {
  background-color: #eef2ff;
  border: 1px solid #c7d2fe;
  color: #4338ca !important;
  font-weight: 700;
  box-shadow: 0 2px 6px rgba(79, 70, 229, 0.08);
  transition: all 0.2s ease;
}
.btn-assign-brand-modal:hover {
  background-color: #e0e7ff;
  color: #3730a3 !important;
  border-color: #a5b4fc;
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(79, 70, 229, 0.14);
}

/* Responsive Table Fit (Exact 100% desktop fit without scrollbar) */
.table-responsive-fit {
  width: 100%;
  overflow-x: hidden;
}

@media (max-width: 991px) {
  .table-responsive-fit {
    overflow-x: auto;
  }
}

.btn-emerald-action {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  border: none;
  border-radius: 6px;
  height: 28px;
  padding: 0 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 3px;
  font-size: 0.75rem;
  white-space: nowrap;
  transition: all 0.15s ease;
}
.btn-emerald-action:hover {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(5, 150, 105, 0.25);
  color: #ffffff;
}

.btn-action-ghost {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
  background-color: #f8fafc;
  color: #475569;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.78rem;
  padding: 0;
  transition: all 0.15s ease;
}
.btn-action-ghost:hover {
  background-color: #f1f5f9;
  border-color: #cbd5e1;
  color: #0f172a;
}

.btn-action-danger {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid #fee2e2;
  background-color: #fff1f2;
  color: #e11d48;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.78rem;
  padding: 0;
  transition: all 0.15s ease;
}
.btn-action-danger:hover {
  background-color: #e11d48;
  color: #ffffff;
  border-color: #e11d48;
  box-shadow: 0 4px 10px rgba(225, 29, 72, 0.25);
}

/* Purple group badge */
.bg-purple-subtle {
  background-color: #f3e8ff;
}
.text-purple {
  color: #7e22ce;
}
.border-purple {
  border: 1px solid #d8b4fe !important;
}

/* ============================================================== */
/* MODAL STYLING WITH OVERSCROLL BEHAVIOR CONTAINMENT            */
/* ============================================================== */
.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.7);
  display: grid;
  place-items: center;
  z-index: 1050;
  backdrop-filter: blur(5px);
  padding: 16px;
  overflow: hidden;
  overscroll-behavior: contain;
}

.assign-table-dialog {
  width: min(1040px, calc(100vw - 2rem));
  max-height: 92vh;
  display: flex;
  flex-direction: column;
  border-radius: 20px;
  overscroll-behavior: contain;
  animation: modalPop 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.hotline-dialog,
.detail-pro-dialog {
  width: min(660px, calc(100vw - 2rem));
  max-height: 92vh;
  display: flex;
  flex-direction: column;
  border-radius: 20px;
  overscroll-behavior: contain;
  animation: modalPop 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalPop {
  from { opacity: 0; transform: translateY(16px) scale(0.98); }
  to { opacity: 1; transform: translateY(0) scale(1); }
}

.modal-body-scrollable {
  overflow-y: auto;
  max-height: calc(92vh - 140px);
  overscroll-behavior: contain;
}

.bg-gradient-assign {
  background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
}
.bg-gradient-hotline {
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
}
.bg-gradient-detail {
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
}

.detail-header-icon-box {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
}

.bg-white-20 {
  background-color: rgba(255, 255, 255, 0.2);
}

.bg-slate-50-subtle {
  background-color: #f8fafc;
}

.modal-avatar-circle {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: linear-gradient(135deg, #4f46e5 0%, #3b82f6 100%);
  color: #ffffff;
  font-weight: 800;
  font-size: 1.15rem;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
  flex-shrink: 0;
}

.phone-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: #eff6ff;
  color: #2563eb;
  border: 1px solid #bfdbfe;
  padding: 4px 12px;
  border-radius: 8px;
  font-size: 0.85rem;
  font-family: monospace;
  transition: all 0.15s ease;
}
.phone-chip:hover {
  background: #dbeafe;
  color: #1d4ed8;
}

.modal-tile-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
}

.unassigned-table-box {
  background-color: #fffbeb;
  border: 1px dashed #fcd34d;
}

.bg-purple-subtle-50 {
  background-color: #faf5ff;
}
.bg-primary-subtle-30 {
  background-color: #f0fdf4;
}

.btn-close-custom {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1px solid rgba(255, 255, 255, 0.25);
  background: rgba(255, 255, 255, 0.1);
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1rem;
  transition: all 0.15s ease;
}
.btn-close-custom:hover {
  background: rgba(239, 68, 68, 0.85);
  border-color: transparent;
}

/* Area Tab Buttons */
.nav-assign-segmented {
  background-color: #f1f5f9;
  border-radius: 10px;
}
.nav-assign-btn {
  border-radius: 8px;
  font-weight: 700;
  color: #475569;
  border: none;
  padding: 6px 14px;
  transition: all 0.15s ease;
}
.nav-assign-btn.active {
  background-color: #0f172a;
  color: #ffffff;
  box-shadow: 0 2px 6px rgba(15, 23, 42, 0.15);
}

/* ============================================================== */
/* SMART TABLE ASSIGNMENT: SIDEBAR & WORKSPACE LAYOUT (UI/UX PRO) */
/* ============================================================== */
.assign-layout-container {
  min-height: 460px;
}

/* Sidebar Phân Khu Vực Bàn */
.area-sidebar-card {
  width: 250px;
  min-width: 250px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  padding: 14px;
  display: flex;
  flex-direction: column;
}

@media (max-width: 767.98px) {
  .area-sidebar-card {
    width: 100%;
    min-width: 100%;
  }
}

.area-sidebar-list {
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.area-sidebar-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  padding: 9px 12px;
  border-radius: 12px;
  border: 1px solid #e2e8f0;
  background-color: #ffffff;
  color: #334155;
  text-align: left;
  cursor: pointer;
  transition: all 0.18s cubic-bezier(0.16, 1, 0.3, 1);
}

.area-sidebar-item:hover {
  background-color: #f1f5f9;
  border-color: #cbd5e1;
  transform: translateX(2px);
  color: #0f172a;
}

.area-sidebar-item.active {
  background-color: #0f172a !important;
  border-color: #0f172a !important;
  color: #ffffff !important;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.18);
}

.area-sidebar-item.active .area-item-name {
  color: #ffffff !important;
}

.area-item-icon {
  font-size: 1.05rem;
  line-height: 1;
  flex-shrink: 0;
}

.area-item-name {
  font-size: 0.82rem;
  font-weight: 700;
  line-height: 1.25;
  color: #1e293b;
}

.area-item-sub {
  font-size: 0.68rem;
  color: #64748b;
  margin-top: 2px;
}

.area-sidebar-item.active .area-item-sub {
  color: #94a3b8 !important;
}

.area-item-badge {
  font-size: 0.68rem;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 999px;
  flex-shrink: 0;
}

.badge-has-tables {
  background-color: #dcfce7;
  color: #15803d;
  border: 1px solid #bbf7d0;
}

.area-sidebar-item.active .badge-has-tables {
  background-color: #22c55e;
  color: #ffffff;
  border-color: #22c55e;
}

.badge-empty {
  background-color: #f1f5f9;
  color: #94a3b8;
  border: 1px solid #e2e8f0;
}

.area-sidebar-item.active .badge-empty {
  background-color: rgba(255, 255, 255, 0.15);
  color: #cbd5e1;
  border-color: rgba(255, 255, 255, 0.2);
}

/* Sidebar Safety Card (Merge mode hint) */
.sidebar-safety-hint {
  margin-top: auto;
}

.sidebar-safety-card {
  background: #fefce8;
  border: 1px dashed #facc15;
  border-radius: 12px;
  padding: 10px 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.03);
}

/* Workspace Panel */
.area-workspace-panel {
  min-width: 0;
}

.selected-tables-summary {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
}

/* Table Selection Cards */
.table-selection-card {
  background-color: #ffffff;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  border: 2px solid #e2e8f0 !important;
  border-radius: 14px;
}
.table-selection-card:hover {
  border-color: #94a3b8 !important;
  transform: translateY(-2px);
  box-shadow: 0 8px 16px -4px rgba(15, 23, 42, 0.08);
}
.active-selected-table {
  border-color: #3b82f6 !important;
  background-color: #eff6ff !important;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.25), 0 8px 20px -4px rgba(59, 130, 246, 0.2);
}
.selected-check-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #3b82f6;
  color: #ffffff;
  font-weight: 900;
  font-size: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 4px rgba(59, 130, 246, 0.4);
}

/* Purple Badge & Palette Helpers */
.bg-purple {
  background-color: #7c3aed !important;
  color: #ffffff !important;
}
.text-purple {
  color: #7c3aed !important;
}
.border-purple {
  border-color: #c4b5fd !important;
}
.bg-purple-subtle {
  background-color: #f5f3ff !important;
}

/* Utility Helpers */
.g-2-5 {
  --bs-gutter-x: 0.75rem;
  --bs-gutter-y: 0.75rem;
}
.p-2-5 {
  padding: 0.625rem !important;
}
.gap-1-5 {
  gap: 0.375rem !important;
}
.shadow-xs {
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}
.fw-black {
  font-weight: 900;
}

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
  padding: 2px 8px;
}

/* ============================================================== */
/* CUSTOM CONFIRM DIALOG STYLING (UI/UX PRO EXECUTIVE DIALOG)     */
/* ============================================================== */
.custom-confirm-backdrop {
  z-index: 1060;
  backdrop-filter: blur(8px);
  background: rgba(15, 23, 42, 0.65);
}

.custom-confirm-card {
  width: min(460px, calc(100vw - 2rem));
  border-radius: 20px;
  animation: modalPop 0.22s cubic-bezier(0.16, 1, 0.3, 1);
}

.confirm-icon-bubble {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.6rem;
  box-shadow: 0 8px 20px -4px rgba(0, 0, 0, 0.15);
}

.confirm-icon-primary {
  background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);
  color: #ffffff;
  box-shadow: 0 8px 20px -4px rgba(59, 130, 246, 0.35);
}

.confirm-icon-success {
  background: linear-gradient(135deg, #10b981 0%, #059669 100%);
  color: #ffffff;
  box-shadow: 0 8px 20px -4px rgba(16, 185, 129, 0.35);
}

.confirm-icon-warning {
  background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
  color: #ffffff;
  box-shadow: 0 8px 20px -4px rgba(245, 158, 11, 0.35);
}

.confirm-icon-danger {
  background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
  color: #ffffff;
  box-shadow: 0 8px 20px -4px rgba(239, 68, 68, 0.35);
}

.confirm-item-context {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
}

/* ============================================================== */
/* TOAST NOTIFICATIONS (MODERN FLOATING NOTIFICATION SYSTEM)      */
/* ============================================================== */
.custom-toast-container {
  position: fixed;
  top: 24px;
  right: 24px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 400px;
  width: calc(100vw - 48px);
  pointer-events: none;
}

.custom-toast-item {
  pointer-events: auto;
  background: #ffffff;
  border-radius: 14px;
  box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.12), 0 8px 10px -6px rgba(15, 23, 42, 0.08);
  border: 1px solid #e2e8f0;
  padding: 12px 14px 12px 18px;
  display: flex;
  align-items: flex-start;
  gap: 12px;
  position: relative;
  overflow: hidden;
  backdrop-filter: blur(8px);
}

.toast-accent-bar {
  position: absolute;
  top: 0;
  left: 0;
  bottom: 0;
  width: 5px;
}
.bar-success { background: #10b981; }
.bar-error, .bar-danger { background: #ef4444; }
.bar-warning { background: #f59e0b; }
.bar-info, .bar-primary { background: #3b82f6; }

.toast-icon-box {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
  font-weight: 900;
  flex-shrink: 0;
}
.icon-box-success {
  background: #dcfce7;
  color: #15803d;
}
.icon-box-error, .icon-box-danger {
  background: #fee2e2;
  color: #b91c1c;
}
.icon-box-warning {
  background: #fef3c7;
  color: #b45309;
}
.icon-box-info, .icon-box-primary {
  background: #dbeafe;
  color: #1d4ed8;
}

.toast-title {
  line-height: 1.25;
  margin-bottom: 2px;
}
.toast-message {
  line-height: 1.35;
  font-size: 0.8rem !important;
}

.toast-close-btn {
  background: none;
  border: none;
  color: #94a3b8;
  font-size: 0.85rem;
  padding: 2px 6px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s ease;
}
.toast-close-btn:hover {
  background: #f1f5f9;
  color: #334155;
}

/* Toast animations */
.toast-anim-enter-active,
.toast-anim-leave-active {
  transition: all 0.28s cubic-bezier(0.16, 1, 0.3, 1);
}
.toast-anim-enter-from {
  opacity: 0;
  transform: translateX(30px) scale(0.95);
}
.toast-anim-leave-to {
  opacity: 0;
  transform: translateY(-15px) scale(0.95);
}
</style>

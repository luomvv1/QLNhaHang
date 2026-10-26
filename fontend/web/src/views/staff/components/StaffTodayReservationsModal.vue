<template>
  <div v-if="show" class="reservation-overlay" @click.self="$emit('close')">
    <div class="reservation-dialog">
      <!-- HEADER -->
      <div class="reservation-header">
        <div class="d-flex align-items-center gap-3">
          <div class="header-icon-box">
            <span>📅</span>
          </div>
          <div>
            <div class="d-flex align-items-center gap-2">
              <span class="reservation-eyebrow">HÔM NAY · {{ todayDisplayDate }}</span>
              <span class="live-pill"><span class="pulse-dot"></span> LIVE POS</span>
            </div>
            <h5 class="header-title mb-0">Lịch Khách Đặt Bàn Hôm Nay</h5>
          </div>
        </div>

        <div class="d-flex align-items-center gap-2">
          <button
            type="button"
            class="btn-hotline"
            @click="openHotlineBooking"
            title="Tạo lịch đặt bàn mới cho khách gọi điện thoại hoặc walk-in"
          >
            <span>➕</span>
            <span class="d-none d-sm-inline">Nhận Đặt Bàn Hotline</span>
          </button>
          <button
            type="button"
            class="btn-refresh"
            :disabled="loading"
            @click="loadData"
            title="Làm mới danh sách"
          >
            <span :class="{ 'spin-anim': loading }">🔄</span>
          </button>
          <button
            type="button"
            class="reservation-close"
            title="Đóng modal"
            @click="$emit('close')"
          >
            ×
          </button>
        </div>
      </div>

      <!-- SEARCH & KPI FILTER STRIP -->
      <div class="reservation-toolbar">
        <div class="search-box">
          <span class="search-icon">🔍</span>
          <input
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="Tìm theo tên khách, số điện thoại, tên bàn..."
          />
          <button
            v-if="searchQuery"
            type="button"
            class="search-clear"
            @click="searchQuery = ''"
          >
            ✕
          </button>
        </div>

        <!-- KPI Chips -->
        <div class="kpi-filter-strip">
          <button
            type="button"
            class="kpi-chip"
            :class="{ active: activeFilter === 'all' }"
            @click="activeFilter = 'all'"
          >
            <span>Tất cả</span>
            <span class="chip-count">{{ stats.total }}</span>
          </button>
          <button
            type="button"
            class="kpi-chip chip-pending"
            :class="{ active: activeFilter === 'pending' }"
            @click="activeFilter = 'pending'"
          >
            <span>⏳ Chờ duyệt</span>
            <span class="chip-count">{{ stats.pending }}</span>
          </button>
          <button
            type="button"
            class="kpi-chip chip-unassigned"
            :class="{ active: activeFilter === 'unassigned' }"
            @click="activeFilter = 'unassigned'"
          >
            <span>⚠️ Chưa xếp bàn</span>
            <span class="chip-count">{{ stats.unassigned }}</span>
          </button>
          <button
            type="button"
            class="kpi-chip chip-confirmed"
            :class="{ active: activeFilter === 'confirmed' }"
            @click="activeFilter = 'confirmed'"
          >
            <span>✅ Đã xếp bàn</span>
            <span class="chip-count">{{ stats.confirmed }}</span>
          </button>
          <button
            type="button"
            class="kpi-chip chip-checked-in"
            :class="{ active: activeFilter === 'checked_in' }"
            @click="activeFilter = 'checked_in'"
          >
            <span>🍽️ Đang phục vụ</span>
            <span class="chip-count">{{ stats.checkedIn }}</span>
          </button>
        </div>
      </div>

      <!-- BODY: CARDS GRID -->
      <div class="reservation-body">
        <!-- Toast feedback inline -->
        <transition name="fade">
          <div v-if="toastMsg" class="inline-toast" :class="toastType">
            <span>{{ toastMsg }}</span>
          </div>
        </transition>

        <!-- Loading -->
        <div v-if="loading && reservations.length === 0" class="reservation-empty">
          <div class="spinner-border text-emerald" role="status"></div>
          <div class="mt-2 text-muted fw-semibold">Đang đồng bộ dữ liệu đặt bàn hôm nay...</div>
        </div>

        <!-- Empty state -->
        <div v-else-if="filteredReservations.length === 0" class="reservation-empty">
          <div class="empty-icon">📅</div>
          <h6 class="fw-bold mt-2 mb-1">
            {{ searchQuery ? 'Không tìm thấy lịch đặt phù hợp' : 'Không có lịch đặt bàn trong mục này' }}
          </h6>
          <p class="text-muted mb-3 small">
            {{ searchQuery ? 'Thử tìm với từ khóa khác (số điện thoại hoặc tên khách).' : 'Khi có khách đặt qua hotline hoặc web, lịch sẽ tự động hiển thị.' }}
          </p>
          <button
            v-if="!searchQuery"
            type="button"
            class="btn btn-sm btn-primary-emerald fw-bold px-3 py-2 rounded-3"
            @click="openHotlineBooking"
          >
            ➕ Nhận Đặt Bàn Hotline Mới
          </button>
        </div>

        <!-- Grid of cards -->
        <div v-else class="reservation-grid">
          <article
            v-for="res in filteredReservations"
            :key="res.reservationId"
            class="reservation-card"
            :class="[
              `status-${res.reservationStatus}`,
              { 'has-no-table': res.reservationStatus === 'confirmed' && !res.table }
            ]"
          >
            <!-- Card Header: Arrival Time & Countdown Badge -->
            <div class="reservation-card-top">
              <div class="time-block">
                <span class="time-text">{{ formatTime(res.reservationDate) }}</span>
                <span class="countdown-badge" :class="getCountdownClass(res.reservationDate, res.reservationStatus)">
                  {{ getCountdownLabel(res.reservationDate, res.reservationStatus) }}
                </span>
              </div>
              <span class="status-pill" :class="`pill-${res.reservationStatus}`">
                {{ getStatusText(res.reservationStatus) }}
              </span>
            </div>

            <!-- Customer Details -->
            <div class="customer-info">
              <div class="customer-name-row">
                <h5 class="customer-name">{{ res.customerName }}</h5>
                <span class="guest-badge">
                  👥 <strong>{{ res.guestCount }}</strong> khách
                </span>
              </div>

              <div class="customer-sub-meta">
                <div class="phone-chip" @click="copyPhone(res.customerPhone)">
                  <span>📞 {{ res.customerPhone }}</span>
                  <span class="copy-icon" :title="copiedPhone === res.customerPhone ? 'Đã sao chép!' : 'Chép SĐT'">
                    {{ copiedPhone === res.customerPhone ? '✓' : '📋' }}
                  </span>
                </div>
                <span v-if="res.customerPoints" class="vip-chip">
                  ⭐ {{ res.customerPoints }}đ
                </span>
              </div>
            </div>

            <!-- Note -->
            <div v-if="res.note" class="reservation-note">
              <span class="note-quote">“</span>
              <span class="note-text">{{ res.note }}</span>
              <span class="note-quote">”</span>
            </div>

            <!-- Table Allocation Section -->
            <div class="table-alloc-box" :class="{ 'alloc-empty': !res.table }">
              <div v-if="res.table" class="alloc-content">
                <div class="table-tag">
                  <span class="table-icon">🪑</span>
                  <div>
                    <div class="table-name">{{ res.table.tableName }}</div>
                    <div class="table-area">{{ res.table.areaName || 'Sảnh chung' }} · Sức chứa {{ res.table.capacity }} khách</div>
                  </div>
                </div>
                <button
                  v-if="res.reservationStatus !== 'checked_in'"
                  type="button"
                  class="btn-swap-table"
                  title="Đổi sang bàn khác"
                  @click="openAssignModal(res)"
                >
                  Đổi bàn
                </button>
              </div>

              <div v-else class="alloc-unassigned">
                <div class="unassigned-text">
                  <span class="alert-icon">⚠️</span>
                  <span>Chưa phân bàn ăn cho khách!</span>
                </div>
                <button
                  type="button"
                  class="btn-assign-quick"
                  @click="openAssignModal(res)"
                >
                  🪑 Xếp bàn ngay
                </button>
              </div>
            </div>

            <!-- Action Footer -->
            <div class="card-actions">
              <!-- Case 1: Confirmed with table -> Check-in -->
              <button
                v-if="res.reservationStatus === 'confirmed' && res.table"
                type="button"
                class="btn-action btn-checkin"
                :disabled="actionLoadingId === res.reservationId"
                @click="handleCheckIn(res)"
              >
                <span>🛎️</span>
                <span>Đón Khách Vào Bàn (Check-in)</span>
              </button>

              <!-- Case 2: Confirmed without table -> Assign table -->
              <button
                v-else-if="res.reservationStatus === 'confirmed' && !res.table"
                type="button"
                class="btn-action btn-assign"
                @click="openAssignModal(res)"
              >
                <span>🪑</span>
                <span>Chọn Bàn Đón Khách</span>
              </button>

              <!-- Case 3: Pending -> Confirm / Approve -->
              <div v-else-if="res.reservationStatus === 'pending'" class="pending-actions-row">
                <button
                  type="button"
                  class="btn-action btn-confirm"
                  :disabled="actionLoadingId === res.reservationId"
                  @click="handleConfirm(res)"
                >
                  <span>✓</span>
                  <span>Duyệt Nhận Chỗ</span>
                </button>
                <button
                  type="button"
                  class="btn-action btn-cancel-sm"
                  title="Hủy đặt bàn"
                  :disabled="actionLoadingId === res.reservationId"
                  @click="handleCancel(res)"
                >
                  ✕
                </button>
              </div>

              <!-- Case 4: Checked in -->
              <div v-else-if="res.reservationStatus === 'checked_in'" class="checked-in-banner">
                <span>🍽️ Khách đang dùng bữa tại {{ res.table?.tableName || 'bàn' }}</span>
              </div>

              <!-- Case 5: Cancelled -->
              <div v-else class="cancelled-banner">
                <span>Đã hủy đặt bàn</span>
              </div>
            </div>
          </article>
        </div>
      </div>

      <!-- FOOTER SUMMARY -->
      <div class="reservation-footer">
        <div class="text-muted small">
          Hiển thị <strong>{{ filteredReservations.length }}</strong> / {{ reservations.length }} lượt đặt bàn hôm nay
        </div>
        <div class="d-flex align-items-center gap-2">
          <button
            type="button"
            class="btn btn-sm btn-outline-secondary fw-bold px-3 py-1 rounded-3"
            @click="$emit('close')"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL CON 1: XẾP BÀN ĂN (ASSIGN TABLE MODAL)                   -->
    <!-- ============================================================== -->
    <div v-if="assigningReservation" class="sub-modal-overlay" @click.self="assigningReservation = null">
      <div class="sub-modal-dialog">
        <div class="sub-modal-header">
          <div>
            <span class="sub-eyebrow">ĐIỀU PHỐI BÀN ĂN</span>
            <h6 class="mb-0 fw-bold">
              Xếp bàn cho: {{ assigningReservation.customerName }} ({{ assigningReservation.guestCount }} khách)
            </h6>
          </div>
          <button type="button" class="sub-close" @click="assigningReservation = null">×</button>
        </div>

        <div class="sub-modal-body">
          <div v-if="loadingTables" class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary"></div>
            <div class="mt-2 text-muted small">Đang tải sơ đồ bàn...</div>
          </div>

          <div v-else-if="availableTables.length === 0" class="text-center py-4 text-muted">
            Không tìm thấy bàn nào khả dụng.
          </div>

          <div v-else class="area-group-list">
            <div v-for="(tablesInArea, area) in tablesByArea" :key="area" class="area-group mb-3">
              <div class="area-title">📍 {{ area }}</div>
              <div class="table-chip-grid">
                <button
                  v-for="tbl in tablesInArea"
                  :key="tbl.tableId"
                  type="button"
                  class="table-select-card"
                  :class="{
                    'selected': selectedTableId === tbl.tableId,
                    'is-serving': tbl.tableStatus === 'serving',
                    'is-booked': tbl.isBookedOnDate,
                    'is-fit': tbl.capacity >= assigningReservation.guestCount
                  }"
                  :disabled="tbl.tableStatus === 'serving' || (tbl.isBookedOnDate && tbl.tableId !== assigningReservation.table?.tableId)"
                  @click="selectedTableId = tbl.tableId"
                >
                  <div class="table-select-top">
                    <span class="select-table-name">{{ tbl.tableName }}</span>
                    <span class="select-cap">👥 {{ tbl.capacity }}</span>
                  </div>
                  <div class="select-table-status">
                    <span v-if="tbl.tableStatus === 'serving'" class="text-warning">Đang ăn</span>
                    <span v-else-if="tbl.isBookedOnDate" class="text-danger">Đã có khách</span>
                    <span v-else-if="tbl.capacity >= assigningReservation.guestCount" class="text-success fw-bold">✓ Phù hợp</span>
                    <span v-else class="text-muted">Nhỏ hơn</span>
                  </div>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="sub-modal-footer">
          <button
            type="button"
            class="btn btn-sm btn-outline-secondary fw-bold px-3"
            @click="assigningReservation = null"
          >
            Hủy
          </button>
          <button
            type="button"
            class="btn btn-sm btn-primary-emerald fw-bold px-4"
            :disabled="!selectedTableId || isSavingAssign"
            @click="submitAssignTable"
          >
            <span v-if="isSavingAssign" class="spinner-border spinner-border-sm me-1"></span>
            Xác Nhận Xếp Bàn
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL CON 2: TẠO ĐẶT BÀN HOTLINE                              -->
    <!-- ============================================================== -->
    <div v-if="showHotlineModal" class="sub-modal-overlay" @click.self="showHotlineModal = false">
      <div class="sub-modal-dialog">
        <div class="sub-modal-header bg-emerald-dark">
          <div>
            <span class="sub-eyebrow text-emerald-light">HOTLINE BOOKING</span>
            <h6 class="mb-0 fw-bold text-white">Nhận Đặt Bàn Qua Điện Thoại</h6>
          </div>
          <button type="button" class="sub-close text-white" @click="showHotlineModal = false">×</button>
        </div>

        <form @submit.prevent="submitHotlineBooking" class="sub-modal-body">
          <div class="mb-3">
            <label class="form-label small fw-bold">Tên khách hàng <span class="text-danger">*</span></label>
            <input
              v-model="hotlineForm.name"
              type="text"
              class="form-control form-control-sm"
              placeholder="VD: Anh Minh, Chị Lan..."
              required
            />
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Số điện thoại <span class="text-danger">*</span></label>
            <input
              v-model="hotlineForm.phone"
              type="tel"
              class="form-control form-control-sm"
              placeholder="VD: 0912345678"
              required
            />
          </div>

          <div class="row g-2 mb-3">
            <div class="col-6">
              <label class="form-label small fw-bold">Số lượng khách</label>
              <input
                v-model.number="hotlineForm.guestCount"
                type="number"
                min="1"
                max="50"
                class="form-control form-control-sm"
                required
              />
            </div>
            <div class="col-6">
              <label class="form-label small fw-bold">Giờ đến hôm nay</label>
              <input
                v-model="hotlineForm.time"
                type="time"
                class="form-control form-control-sm"
                required
              />
            </div>
          </div>

          <div class="mb-2">
            <label class="form-label small fw-bold">Ghi chú đặc biệt</label>
            <textarea
              v-model="hotlineForm.note"
              class="form-control form-control-sm"
              rows="2"
              placeholder="VD: Ngồi tầng 2, cần ghế trẻ em, sinh nhật..."
            ></textarea>
          </div>

          <div class="sub-modal-footer px-0 pb-0 pt-3 border-top mt-3">
            <button
              type="button"
              class="btn btn-sm btn-outline-secondary fw-bold px-3"
              @click="showHotlineModal = false"
            >
              Hủy
            </button>
            <button
              type="submit"
              class="btn btn-sm btn-primary-emerald fw-bold px-4"
              :disabled="isSubmittingHotline"
            >
              <span v-if="isSubmittingHotline" class="spinner-border spinner-border-sm me-1"></span>
              Tạo Lịch Đặt Bàn
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import axios from 'axios';

const props = defineProps({
  show: Boolean
});

const emit = defineEmits(['close', 'check-in', 'table-assigned']);

const apiBase = import.meta.env.VITE_API_URL || 'http://localhost:5079';

// Core State
const reservations = ref([]);
const loading = ref(false);
const searchQuery = ref('');
const activeFilter = ref('all'); // 'all' | 'pending' | 'unassigned' | 'confirmed' | 'checked_in'
const copiedPhone = ref('');
const toastMsg = ref('');
const toastType = ref('success');
const actionLoadingId = ref(null);

// Assign Table Sub-Modal State
const assigningReservation = ref(null);
const availableTables = ref([]);
const loadingTables = ref(false);
const selectedTableId = ref(null);
const isSavingAssign = ref(false);

// Hotline Booking Sub-Modal State
const showHotlineModal = ref(false);
const isSubmittingHotline = ref(false);
const hotlineForm = ref({
  name: '',
  phone: '',
  guestCount: 2,
  time: '',
  note: ''
});

// Watch show prop
watch(() => props.show, (val) => {
  if (val) {
    loadData();
  } else {
    assigningReservation.value = null;
    showHotlineModal.value = false;
  }
});

function getAuthHeaders() {
  const token = localStorage.getItem('token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function showToast(msg, type = 'success') {
  toastMsg.value = msg;
  toastType.value = type;
  setTimeout(() => {
    if (toastMsg.value === msg) toastMsg.value = '';
  }, 3200);
}

// -------------------------------------------------------------
// LOAD DATA
// -------------------------------------------------------------
async function loadData() {
  loading.value = true;
  try {
    const today = new Date().toLocaleDateString('en-CA');
    const res = await axios.get(`${apiBase}/api/admin/reservations`, {
      params: { date: today },
      headers: getAuthHeaders()
    });
    if (res.data?.success) {
      reservations.value = res.data.data || [];
    }
  } catch (err) {
    console.error('Lỗi tải danh sách đặt bàn hôm nay:', err);
    showToast('Không thể tải lịch đặt bàn. Vui lòng thử lại.', 'error');
  } finally {
    loading.value = false;
  }
}

// -------------------------------------------------------------
// COMPUTED & STATS
// -------------------------------------------------------------
const todayDisplayDate = computed(() => {
  return new Date().toLocaleDateString('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
});

const stats = computed(() => {
  const total = reservations.value.length;
  const pending = reservations.value.filter(r => r.reservationStatus === 'pending').length;
  const unassigned = reservations.value.filter(r => r.reservationStatus === 'confirmed' && !r.table).length;
  const confirmed = reservations.value.filter(r => r.reservationStatus === 'confirmed' && r.table).length;
  const checkedIn = reservations.value.filter(r => r.reservationStatus === 'checked_in').length;
  return { total, pending, unassigned, confirmed, checkedIn };
});

const filteredReservations = computed(() => {
  let list = reservations.value;

  // Status Filter
  if (activeFilter.value === 'pending') {
    list = list.filter(r => r.reservationStatus === 'pending');
  } else if (activeFilter.value === 'unassigned') {
    list = list.filter(r => r.reservationStatus === 'confirmed' && !r.table);
  } else if (activeFilter.value === 'confirmed') {
    list = list.filter(r => r.reservationStatus === 'confirmed' && r.table);
  } else if (activeFilter.value === 'checked_in') {
    list = list.filter(r => r.reservationStatus === 'checked_in');
  }

  // Search Filter
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase();
    list = list.filter(r => {
      const name = (r.customerName || '').toLowerCase();
      const phone = (r.customerPhone || '').toLowerCase();
      const table = (r.table?.tableName || '').toLowerCase();
      const area = (r.table?.areaName || '').toLowerCase();
      return name.includes(q) || phone.includes(q) || table.includes(q) || area.includes(q);
    });
  }

  // Sort: pending & confirmed unassigned first, then by reservationDate
  return [...list].sort((a, b) => {
    const timeA = new Date(a.reservationDate).getTime();
    const timeB = new Date(b.reservationDate).getTime();
    return timeA - timeB;
  });
});

// Group available tables by area for sub-modal
const tablesByArea = computed(() => {
  const groups = {};
  for (const t of availableTables.value) {
    const area = t.areaName || 'Sảnh chung';
    if (!groups[area]) groups[area] = [];
    groups[area].push(t);
  }
  return groups;
});

// -------------------------------------------------------------
// HELPERS FOR TIME & STATUS
// -------------------------------------------------------------
function formatTime(dt) {
  if (!dt) return '';
  return new Date(dt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function getCountdownLabel(dateStr, status) {
  if (status === 'checked_in') return 'Đang ăn';
  if (status === 'cancelled') return 'Đã hủy';
  if (!dateStr) return '';

  const now = new Date();
  const target = new Date(dateStr);
  const diffMinutes = Math.round((target - now) / 60000);

  if (diffMinutes < -15) {
    return `Quá ${Math.abs(diffMinutes)}p`;
  }
  if (diffMinutes < 0) {
    return 'Đến giờ hẹn';
  }
  if (diffMinutes <= 30) {
    return `Còn ${diffMinutes}p`;
  }
  const hours = Math.floor(diffMinutes / 60);
  const mins = diffMinutes % 60;
  return hours > 0 ? `Sau ${hours}h${mins > 0 ? mins + 'p' : ''}` : `Còn ${mins}p`;
}

function getCountdownClass(dateStr, status) {
  if (status === 'checked_in') return 'badge-checked-in';
  if (status === 'cancelled') return 'badge-cancelled';
  if (!dateStr) return '';

  const now = new Date();
  const target = new Date(dateStr);
  const diffMinutes = Math.round((target - now) / 60000);

  if (diffMinutes < -15) return 'badge-overdue';
  if (diffMinutes <= 30) return 'badge-upcoming';
  return 'badge-normal';
}

function getStatusText(status) {
  switch (status) {
    case 'pending': return 'Chờ duyệt';
    case 'confirmed': return 'Đã duyệt';
    case 'checked_in': return 'Đang ăn';
    case 'cancelled': return 'Đã hủy';
    case 'no_show': return 'Vắng mặt';
    default: return status;
  }
}

function copyPhone(phone) {
  if (!phone) return;
  navigator.clipboard.writeText(phone);
  copiedPhone.value = phone;
  showToast(`Đã sao chép SĐT: ${phone}`);
  setTimeout(() => {
    if (copiedPhone.value === phone) copiedPhone.value = '';
  }, 2000);
}

// -------------------------------------------------------------
// ACTIONS (CONFIRM, CHECK-IN, CANCEL)
// -------------------------------------------------------------
async function handleConfirm(res) {
  actionLoadingId.value = res.reservationId;
  try {
    const resp = await axios.post(
      `${apiBase}/api/admin/reservations/${res.reservationId}/confirm`,
      {},
      { headers: getAuthHeaders() }
    );
    if (resp.data?.success) {
      showToast(`Đã duyệt lịch đặt của ${res.customerName}!`);
      await loadData();
      // Prompt to assign table immediately
      const updated = reservations.value.find(r => r.reservationId === res.reservationId);
      if (updated && !updated.table) {
        openAssignModal(updated);
      }
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi duyệt phiếu đặt bàn.', 'error');
  } finally {
    actionLoadingId.value = null;
  }
}

function handleCheckIn(res) {
  // Delegate check-in to parent POS so it opens the table and order view seamlessly
  emit('check-in', res);
}

async function handleCancel(res) {
  if (!confirm(`Xác nhận hủy lịch đặt bàn của khách "${res.customerName}"?`)) return;

  actionLoadingId.value = res.reservationId;
  try {
    const resp = await axios.post(
      `${apiBase}/api/admin/reservations/${res.reservationId}/cancel`,
      { reason: 'Nhân viên hủy tại quầy POS' },
      { headers: getAuthHeaders() }
    );
    if (resp.data?.success) {
      showToast(`Đã hủy lịch đặt của ${res.customerName}.`);
      await loadData();
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi hủy lịch.', 'error');
  } finally {
    actionLoadingId.value = null;
  }
}

// -------------------------------------------------------------
// TABLE ASSIGNMENT SUB-MODAL
// -------------------------------------------------------------
async function openAssignModal(res) {
  assigningReservation.value = res;
  selectedTableId.value = res.table?.tableId || null;
  loadingTables.value = true;
  try {
    const today = new Date().toLocaleDateString('en-CA');
    const resp = await axios.get(`${apiBase}/api/admin/reservations/tables`, {
      params: { date: today, excludeReservationId: res.reservationId },
      headers: getAuthHeaders()
    });
    if (resp.data?.success) {
      availableTables.value = resp.data.data || [];
    }
  } catch (err) {
    showToast('Không thể tải danh sách bàn ăn.', 'error');
  } finally {
    loadingTables.value = false;
  }
}

async function submitAssignTable() {
  if (!assigningReservation.value || !selectedTableId.value) return;

  isSavingAssign.value = true;
  try {
    const id = assigningReservation.value.reservationId;
    const resp = await axios.post(
      `${apiBase}/api/admin/reservations/${id}/assign-table`,
      { tableId: selectedTableId.value, tableIds: [selectedTableId.value] },
      { headers: getAuthHeaders() }
    );
    if (resp.data?.success) {
      showToast('Đã xếp bàn thành công!');
      assigningReservation.value = null;
      await loadData();
      emit('table-assigned');
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi khi xếp bàn.', 'error');
  } finally {
    isSavingAssign.value = false;
  }
}

// -------------------------------------------------------------
// HOTLINE BOOKING SUB-MODAL
// -------------------------------------------------------------
function openHotlineBooking() {
  const now = new Date();
  now.setMinutes(now.getMinutes() + 30);
  const timeStr = now.toTimeString().slice(0, 5);

  hotlineForm.value = {
    name: '',
    phone: '',
    guestCount: 2,
    time: timeStr,
    note: ''
  };
  showHotlineModal.value = true;
}

async function submitHotlineBooking() {
  isSubmittingHotline.value = true;
  try {
    const [hh, mm] = hotlineForm.value.time.split(':').map(Number);
    const dateObj = new Date();
    dateObj.setHours(hh, mm, 0, 0);

    const payload = {
      customerName: hotlineForm.value.name.trim(),
      customerPhone: hotlineForm.value.phone.trim(),
      guestCount: hotlineForm.value.guestCount,
      reservationDate: dateObj.toISOString(),
      note: hotlineForm.value.note.trim()
    };

    const resp = await axios.post(`${apiBase}/api/admin/reservations/create`, payload, {
      headers: getAuthHeaders()
    });

    if (resp.data?.success) {
      showToast('Đã tạo lịch đặt bàn hotline thành công!');
      showHotlineModal.value = false;
      await loadData();

      // Find created reservation & offer to assign table
      const createdId = resp.data.data?.reservationId;
      if (createdId) {
        const found = reservations.value.find(r => r.reservationId === createdId);
        if (found) {
          openAssignModal(found);
        }
      }
    }
  } catch (err) {
    showToast(err.response?.data?.message || 'Lỗi tạo lịch đặt hotline.', 'error');
  } finally {
    isSubmittingHotline.value = false;
  }
}
</script>

<style scoped>
/* =========================================================
   OVERLAY & DIALOG CONTAINER
   ========================================================= */
.reservation-overlay {
  position: fixed;
  inset: 0;
  z-index: 1200;
  display: grid;
  place-items: center;
  padding: 16px;
  background: rgba(8, 23, 22, 0.76);
  backdrop-filter: blur(8px);
  animation: fadeIn 0.15s ease-out;
}

.reservation-dialog {
  width: min(1160px, 100%);
  max-height: min(880px, 94vh);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-radius: 18px;
  background: #f8fafc;
  box-shadow: 0 25px 60px -15px rgba(0, 0, 0, 0.35), 0 0 0 1px rgba(255, 255, 255, 0.1);
}

/* =========================================================
   HEADER
   ========================================================= */
.reservation-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 18px 24px;
  background: linear-gradient(135deg, #0f2b26 0%, #17423b 100%);
  color: #fff;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.header-icon-box {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.12);
  display: grid;
  place-items: center;
  font-size: 1.4rem;
}

.reservation-eyebrow {
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  color: #7ee0cb;
}

.live-pill {
  font-size: 0.62rem;
  font-weight: 800;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(16, 185, 129, 0.2);
  color: #34d399;
  border: 1px solid rgba(52, 211, 153, 0.3);
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.pulse-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #10b981;
  box-shadow: 0 0 6px #10b981;
  animation: pulseAnim 1.8s infinite;
}

.header-title {
  font-size: 1.25rem;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: #fff;
}

.btn-hotline {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  color: #fff;
  border: 1px solid rgba(255, 255, 255, 0.2);
  padding: 8px 16px;
  border-radius: 10px;
  font-size: 0.82rem;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.18s ease;
  box-shadow: 0 4px 12px rgba(5, 150, 105, 0.3);
}

.btn-hotline:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(5, 150, 105, 0.4);
}

.btn-refresh {
  width: 36px;
  height: 36px;
  border-radius: 9px;
  border: 1px solid rgba(255, 255, 255, 0.2);
  background: rgba(255, 255, 255, 0.1);
  color: #fff;
  display: grid;
  place-items: center;
  cursor: pointer;
  transition: all 0.18s;
}

.btn-refresh:hover {
  background: rgba(255, 255, 255, 0.2);
}

.reservation-close {
  width: 36px;
  height: 36px;
  border-radius: 9px;
  border: 1px solid rgba(255, 255, 255, 0.2);
  background: transparent;
  color: #fff;
  font-size: 1.6rem;
  line-height: 1;
  display: grid;
  place-items: center;
  cursor: pointer;
  transition: all 0.18s;
}

.reservation-close:hover {
  background: rgba(239, 68, 68, 0.3);
  border-color: rgba(239, 68, 68, 0.5);
}

/* =========================================================
   TOOLBAR & KPI STRIP
   ========================================================= */
.reservation-toolbar {
  padding: 14px 24px;
  background: #fff;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.search-box {
  position: relative;
  flex: 1 1 280px;
  max-width: 420px;
}

.search-icon {
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 0.85rem;
  color: #94a3b8;
}

.search-input {
  width: 100%;
  padding: 8px 36px 8px 34px;
  border-radius: 10px;
  border: 1px solid #cbd5e1;
  background: #f8fafc;
  font-size: 0.85rem;
  outline: none;
  transition: all 0.18s;
}

.search-input:focus {
  background: #fff;
  border-color: #059669;
  box-shadow: 0 0 0 3px rgba(5, 150, 105, 0.15);
}

.search-clear {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 0.8rem;
  cursor: pointer;
}

.kpi-filter-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.kpi-chip {
  padding: 6px 12px;
  border-radius: 999px;
  border: 1px solid #e2e8f0;
  background: #f1f5f9;
  color: #475569;
  font-size: 0.78rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  transition: all 0.18s;
}

.kpi-chip:hover {
  background: #e2e8f0;
}

.kpi-chip .chip-count {
  padding: 1px 7px;
  border-radius: 999px;
  background: #fff;
  font-size: 0.72rem;
  font-weight: 800;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.kpi-chip.active {
  background: #0f172a;
  color: #fff;
  border-color: #0f172a;
}

.kpi-chip.active .chip-count {
  background: rgba(255, 255, 255, 0.2);
  color: #fff;
}

.kpi-chip.chip-pending.active {
  background: #d97706;
  border-color: #d97706;
}

.kpi-chip.chip-unassigned.active {
  background: #ea580c;
  border-color: #ea580c;
}

.kpi-chip.chip-confirmed.active {
  background: #2563eb;
  border-color: #2563eb;
}

.kpi-chip.chip-checked-in.active {
  background: #059669;
  border-color: #059669;
}

/* =========================================================
   BODY & CARDS GRID
   ========================================================= */
.reservation-body {
  flex: 1 1 auto;
  overflow-y: auto;
  padding: 20px 24px;
  background: #f8fafc;
}

.reservation-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
}

.reservation-card {
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  border-top: 4px solid #94a3b8;
  padding: 16px;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.04);
  transition: all 0.2s ease;
}

.reservation-card:hover {
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08);
  transform: translateY(-2px);
}

.reservation-card.status-pending {
  border-top-color: #f59e0b;
}

.reservation-card.status-confirmed {
  border-top-color: #3b82f6;
}

.reservation-card.status-confirmed.has-no-table {
  border-top-color: #f97316;
  background: #fffdfa;
}

.reservation-card.status-checked_in {
  border-top-color: #10b981;
}

.reservation-card.status-cancelled {
  border-top-color: #94a3b8;
  opacity: 0.65;
}

/* Card Header */
.reservation-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 12px;
}

.time-block {
  display: flex;
  align-items: center;
  gap: 8px;
}

.time-text {
  font-size: 1.45rem;
  font-weight: 900;
  color: #0f172a;
  letter-spacing: -0.03em;
}

.countdown-badge {
  font-size: 0.7rem;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 999px;
}

.badge-overdue {
  background: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  animation: pulseAlert 1.5s infinite;
}

.badge-upcoming {
  background: #fef3c7;
  color: #b45309;
  border: 1px solid #fde68a;
  font-weight: 800;
}

.badge-normal {
  background: #e0f2fe;
  color: #0369a1;
  border: 1px solid #bae6fd;
}

.badge-checked-in {
  background: #dcfce7;
  color: #15803d;
  border: 1px solid #bbf7d0;
}

.badge-cancelled {
  background: #f1f5f9;
  color: #64748b;
}

.status-pill {
  font-size: 0.7rem;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 6px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.pill-pending {
  background: #fffbeb;
  color: #b45309;
  border: 1px solid #fde68a;
}

.pill-confirmed {
  background: #eff6ff;
  color: #1d4ed8;
  border: 1px solid #bfdbfe;
}

.pill-checked_in {
  background: #ecfdf5;
  color: #047857;
  border: 1px solid #a7f3d0;
}

.pill-cancelled {
  background: #f8fafc;
  color: #94a3b8;
  border: 1px solid #e2e8f0;
}

/* Customer Info */
.customer-info {
  margin-bottom: 12px;
}

.customer-name-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 4px;
}

.customer-name {
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.guest-badge {
  font-size: 0.78rem;
  font-weight: 700;
  color: #334155;
  background: #f1f5f9;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
}

.customer-sub-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.phone-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.78rem;
  color: #475569;
  background: #f8fafc;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1px solid #e2e8f0;
  cursor: pointer;
  transition: all 0.15s;
}

.phone-chip:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.copy-icon {
  font-size: 0.8rem;
  color: #059669;
}

.vip-chip {
  font-size: 0.72rem;
  font-weight: 800;
  color: #b45309;
  background: #fef3c7;
  padding: 2px 6px;
  border-radius: 6px;
  border: 1px solid #fde68a;
}

/* Note */
.reservation-note {
  font-size: 0.78rem;
  color: #475569;
  font-style: italic;
  background: #f8fafc;
  border-left: 3px solid #cbd5e1;
  padding: 6px 10px;
  border-radius: 0 6px 6px 0;
  margin-bottom: 12px;
}

.note-quote {
  color: #94a3b8;
  font-weight: 800;
}

/* Table Allocation */
.table-alloc-box {
  margin-top: auto;
  margin-bottom: 14px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
}

.table-alloc-box.alloc-empty {
  background: #fff7ed;
  border-color: #fed7aa;
}

.alloc-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.table-tag {
  display: flex;
  align-items: center;
  gap: 8px;
}

.table-icon {
  font-size: 1.15rem;
}

.table-name {
  font-size: 0.85rem;
  font-weight: 800;
  color: #166534;
}

.table-area {
  font-size: 0.7rem;
  color: #15803d;
}

.btn-swap-table {
  font-size: 0.72rem;
  font-weight: 800;
  color: #047857;
  background: #dcfce7;
  border: 1px solid #86efac;
  padding: 4px 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-swap-table:hover {
  background: #bbf7d0;
}

.alloc-unassigned {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.unassigned-text {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.78rem;
  font-weight: 800;
  color: #c2410c;
}

.btn-assign-quick {
  font-size: 0.75rem;
  font-weight: 800;
  color: #fff;
  background: #ea580c;
  border: none;
  padding: 5px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s;
}

.btn-assign-quick:hover {
  background: #c2410c;
}

/* Card Actions */
.card-actions {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.btn-action {
  width: 100%;
  padding: 9px 12px;
  border-radius: 8px;
  border: 1px solid transparent;
  font-size: 0.82rem;
  font-weight: 800;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  cursor: pointer;
  transition: all 0.18s;
}

.btn-checkin {
  background: linear-gradient(135deg, #059669 0%, #047857 100%);
  color: #fff;
  box-shadow: 0 2px 8px rgba(5, 150, 105, 0.25);
}

.btn-checkin:hover:not(:disabled) {
  background: #047857;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(5, 150, 105, 0.35);
}

.btn-assign {
  background: #2563eb;
  color: #fff;
}

.btn-assign:hover {
  background: #1d4ed8;
}

.pending-actions-row {
  display: flex;
  align-items: center;
  gap: 6px;
}

.btn-confirm {
  flex: 1;
  background: #f59e0b;
  color: #fff;
}

.btn-confirm:hover:not(:disabled) {
  background: #d97706;
}

.btn-cancel-sm {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  border: 1px solid #fecaca;
  background: #fef2f2;
  color: #ef4444;
  font-size: 0.9rem;
  display: grid;
  place-items: center;
  cursor: pointer;
}

.btn-cancel-sm:hover:not(:disabled) {
  background: #fee2e2;
}

.checked-in-banner {
  text-align: center;
  padding: 8px;
  border-radius: 8px;
  background: #ecfdf5;
  border: 1px solid #a7f3d0;
  color: #047857;
  font-size: 0.78rem;
  font-weight: 700;
}

.cancelled-banner {
  text-align: center;
  padding: 8px;
  border-radius: 8px;
  background: #f1f5f9;
  color: #64748b;
  font-size: 0.78rem;
  font-weight: 700;
}

/* =========================================================
   FOOTER
   ========================================================= */
.reservation-footer {
  padding: 12px 24px;
  background: #fff;
  border-top: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

/* =========================================================
   SUB-MODAL (XẾP BÀN & HOTLINE)
   ========================================================= */
.sub-modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 1300;
  display: grid;
  place-items: center;
  padding: 16px;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(4px);
  animation: fadeIn 0.15s ease-out;
}

.sub-modal-dialog {
  width: min(540px, 100%);
  max-height: 85vh;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.3);
  overflow: hidden;
}

.sub-modal-header {
  padding: 16px 20px;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.sub-modal-header.bg-emerald-dark {
  background: linear-gradient(135deg, #065f46 0%, #047857 100%);
}

.sub-eyebrow {
  font-size: 0.65rem;
  font-weight: 800;
  color: #059669;
  letter-spacing: 0.08em;
  display: block;
}

.text-emerald-light {
  color: #6ee7b7 !important;
}

.sub-close {
  background: transparent;
  border: none;
  font-size: 1.5rem;
  line-height: 1;
  color: #64748b;
  cursor: pointer;
}

.sub-modal-body {
  padding: 18px 20px;
  overflow-y: auto;
  max-height: calc(85vh - 120px);
}

.sub-modal-footer {
  padding: 12px 20px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
}

/* Area Group & Table select cards */
.area-title {
  font-size: 0.8rem;
  font-weight: 800;
  color: #334155;
  margin-bottom: 6px;
}

.table-chip-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  gap: 8px;
}

.table-select-card {
  padding: 8px 10px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background: #fff;
  text-align: left;
  cursor: pointer;
  transition: all 0.15s;
}

.table-select-card:hover:not(:disabled) {
  border-color: #059669;
  background: #f0fdf4;
}

.table-select-card.selected {
  border-color: #059669;
  background: #ecfdf5;
  box-shadow: 0 0 0 2px #059669;
}

.table-select-card.is-serving,
.table-select-card.is-booked {
  opacity: 0.5;
  cursor: not-allowed;
  background: #f8fafc;
}

.table-select-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 4px;
}

.select-table-name {
  font-size: 0.82rem;
  font-weight: 800;
  color: #0f172a;
}

.select-cap {
  font-size: 0.7rem;
  color: #64748b;
}

.select-table-status {
  font-size: 0.68rem;
}

/* Primary emerald button */
.btn-primary-emerald {
  background: #059669;
  color: #fff;
  border: none;
}

.btn-primary-emerald:hover:not(:disabled) {
  background: #047857;
}

/* Inline Toast */
.inline-toast {
  padding: 10px 16px;
  border-radius: 8px;
  font-size: 0.82rem;
  font-weight: 700;
  margin-bottom: 14px;
  text-align: center;
}

.inline-toast.success {
  background: #dcfce7;
  color: #15803d;
  border: 1px solid #bbf7d0;
}

.inline-toast.error {
  background: #fee2e2;
  color: #b91c1c;
  border: 1px solid #fecaca;
}

/* Empty State */
.reservation-empty {
  min-height: 280px;
  display: grid;
  place-content: center;
  text-align: center;
}

.empty-icon {
  width: 56px;
  height: 56px;
  margin: 0 auto;
  border-radius: 16px;
  background: #e0f2fe;
  display: grid;
  place-items: center;
  font-size: 1.8rem;
}

/* Animation utilities */
.spin-anim {
  display: inline-block;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes pulseAnim {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

@keyframes pulseAlert {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.6; }
}

/* Responsive */
@media (max-width: 680px) {
  .reservation-dialog {
    max-height: 98vh;
    border-radius: 12px;
  }
  .reservation-header {
    padding: 14px 16px;
  }
  .reservation-toolbar {
    padding: 10px 16px;
  }
  .reservation-body {
    padding: 14px 16px;
  }
  .reservation-grid {
    grid-template-columns: 1fr;
  }
}
</style>

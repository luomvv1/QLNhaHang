<template>
  <div class="p-3 p-md-4 bg-slate-50 min-vh-100">
    <!-- 1. Header Section -->
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 pb-2 border-bottom gap-3">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge bg-teal-100 text-teal-800 fw-bold px-2.5 py-1 rounded-pill small">
            Chef KDS Audit Trail
          </span>
          <span class="text-muted small">Cập nhật theo thời gian thực</span>
        </div>
        <h3 class="fw-bold text-dark m-0 d-flex align-items-center gap-2">
          <span>👨‍🍳</span>
          <span>Lịch Sử & Nhật Ký Quyết Định Quản Lý Bếp</span>
        </h3>
        <p class="text-muted small mb-0 mt-1">
          Theo dõi minh bạch toàn bộ các quyết định vận hành của Bếp trưởng: báo hết món, hủy/từ chối món, điều chỉnh suất ăn và lý do giải trình.
        </p>
      </div>

      <!-- Action Buttons -->
      <div class="d-flex flex-wrap gap-2">
        <button
          type="button"
          class="btn btn-outline-secondary btn-sm d-flex align-items-center gap-1.5 px-3 py-2 fw-semibold"
          :disabled="isLoading"
          @click="fetchData"
        >
          <span v-if="isLoading" class="spinner-border spinner-border-sm"></span>
          <span v-else>🔄</span>
          <span>Làm mới</span>
        </button>

        <button
          type="button"
          class="btn btn-outline-primary btn-sm d-flex align-items-center gap-1.5 px-3 py-2 fw-semibold"
          @click="exportCsv"
        >
          <span>📥</span>
          <span>Xuất Báo Cáo</span>
        </button>
      </div>
    </div>

    <!-- Alert thông báo -->
    <div v-if="errorMsg" class="alert alert-danger alert-dismissible fade show shadow-xs mb-4" role="alert">
      <strong>⚠️ Lỗi:</strong> {{ errorMsg }}
      <button type="button" class="btn-close" @click="errorMsg = ''"></button>
    </div>

    <!-- 2. Thẻ KPI Thống Kê Nhanh (Metric Cards) -->
    <div class="row g-3 mb-4">
      <div class="col-6 col-lg-3">
        <div class="card border-0 shadow-sm rounded-4 p-3 bg-white h-100 border-start border-4 border-warning">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <div class="text-muted small fw-bold text-uppercase">Báo Hết Món</div>
              <h3 class="fw-bold text-dark mb-0 mt-1 font-monospace text-warning">{{ summary.soldOutCount }}</h3>
            </div>
            <div class="metric-icon-circle bg-warning-subtle text-warning">🛑</div>
          </div>
          <div class="small text-muted mt-2">
            Mở lại: <strong class="text-success">{{ summary.reopenedCount }}</strong> lần
          </div>
        </div>
      </div>

      <div class="col-6 col-lg-3">
        <div class="card border-0 shadow-sm rounded-4 p-3 bg-white h-100 border-start border-4 border-danger">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <div class="text-muted small fw-bold text-uppercase">Bếp Hủy / Từ Chối</div>
              <h3 class="fw-bold text-dark mb-0 mt-1 font-monospace text-danger">{{ summary.cancelledCount }}</h3>
            </div>
            <div class="metric-icon-circle bg-danger-subtle text-danger">❌</div>
          </div>
          <div class="small text-muted mt-2">
            Cần giải trình lý do rõ ràng
          </div>
        </div>
      </div>

      <div class="col-6 col-lg-3">
        <div class="card border-0 shadow-sm rounded-4 p-3 bg-white h-100 border-start border-4 border-info">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <div class="text-muted small fw-bold text-uppercase">Nấu Lại / Hoàn Tác</div>
              <h3 class="fw-bold text-dark mb-0 mt-1 font-monospace text-info">{{ summary.undoCount }}</h3>
            </div>
            <div class="metric-icon-circle bg-info-subtle text-info">🔄</div>
          </div>
          <div class="small text-muted mt-2">
            Hoàn tất cả bàn: <strong class="text-primary">{{ summary.readyAllCount }}</strong>
          </div>
        </div>
      </div>

      <div class="col-6 col-lg-3">
        <div class="card border-0 shadow-sm rounded-4 p-3 bg-white h-100 border-start border-4 border-success">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <div class="text-muted small fw-bold text-uppercase">Quyết Định Hôm Nay</div>
              <h3 class="fw-bold text-dark mb-0 mt-1 font-monospace text-success">{{ summary.todayCount }}</h3>
            </div>
            <div class="metric-icon-circle bg-success-subtle text-success">📋</div>
          </div>
          <div class="small text-muted mt-2">
            Tổng toàn bộ: <strong class="text-dark">{{ summary.totalDecisions }}</strong>
          </div>
        </div>
      </div>
    </div>

    <!-- 3. Phân Tích Lý Do Bếp Ra Quyết Định Phổ Biến (Top Reasons Chips) -->
    <div v-if="summary.topReasons && summary.topReasons.length > 0" class="card border-0 shadow-sm rounded-4 p-3 bg-white mb-4">
      <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-2">
        <div class="small fw-bold text-secondary d-flex align-items-center gap-1.5">
          <span>💡</span>
          <span>LÝ DO GIẢI TRÌNH PHỔ BIẾN TỪ BẾP TRƯỞNG:</span>
        </div>
        <small class="text-muted">Nhấp vào lý do để lọc nhanh các quyết định tương ứng</small>
      </div>
      <div class="d-flex flex-wrap gap-2">
        <button
          v-for="r in summary.topReasons"
          :key="r.reason"
          type="button"
          class="btn btn-sm reason-chip-btn"
          :class="{ active: searchKeyword === r.reason }"
          @click="toggleReasonFilter(r.reason)"
        >
          <span class="reason-text">"{{ r.reason }}"</span>
          <span class="badge bg-secondary rounded-pill ms-1">{{ r.count }}</span>
        </button>
        <button
          v-if="searchKeyword"
          type="button"
          class="btn btn-sm btn-link text-decoration-none text-muted small p-0 ms-2"
          @click="searchKeyword = ''; fetchData()"
        >
          ✕ Bỏ lọc lý do
        </button>
      </div>
    </div>

    <!-- 4. Thanh Công Cụ Tìm Kiếm & Bộ Lọc Nâng Cao -->
    <div class="card border-0 shadow-sm rounded-4 p-3 bg-white mb-4">
      <div class="row g-2 align-items-center">
        <!-- Ô tìm kiếm từ khóa -->
        <div class="col-12 col-md-4">
          <div class="input-group">
            <span class="input-group-text bg-light border-end-0 text-muted">🔍</span>
            <input
              v-model="searchKeyword"
              type="text"
              class="form-control border-start-0 ps-1"
              placeholder="Tìm theo món, bàn, bếp trưởng, lý do..."
              @keyup.enter="applyFilters"
            />
            <button
              v-if="searchKeyword"
              type="button"
              class="btn btn-light border border-start-0 text-muted"
              @click="searchKeyword = ''; applyFilters()"
            >
              ✕
            </button>
          </div>
        </div>

        <!-- Lọc theo loại quyết định -->
        <div class="col-6 col-md-3">
          <select v-model="selectedActionType" class="form-select fw-semibold" @change="applyFilters">
            <option value="all">⚡ Tất cả loại quyết định</option>
            <option value="sold_out">🛑 Báo Hết Món (Sold out)</option>
            <option value="reopened">✅ Mở Bán Lại Món (Reopened)</option>
            <option value="cancelled">❌ Bếp Hủy / Từ Chối (Cancelled)</option>
            <option value="undo">🔄 Nấu Lại / Hoàn Tác (Undo)</option>
            <option value="ready_all">✨ Nấu Xong Toàn Bộ Bàn (Ready all)</option>
          </select>
        </div>

        <!-- Lọc theo Bếp trưởng -->
        <div class="col-6 col-md-3">
          <select v-model="selectedChefId" class="form-select" @change="applyFilters">
            <option :value="null">👤 Tất cả Bếp trưởng / Nhân viên</option>
            <option v-for="c in chefsList" :key="c.userId" :value="c.userId">
              {{ c.fullName }} ({{ c.roleName }})
            </option>
          </select>
        </div>

        <!-- Lọc theo mốc thời gian -->
        <div class="col-12 col-md-2 d-flex gap-1">
          <select v-model="timePreset" class="form-select" @change="onTimePresetChange">
            <option value="all">Toàn bộ</option>
            <option value="today">Hôm nay</option>
            <option value="7days">7 ngày qua</option>
            <option value="30days">30 ngày qua</option>
          </select>
          <button type="button" class="btn btn-primary px-3 fw-bold" @click="applyFilters">
            Lọc
          </button>
        </div>
      </div>
    </div>

    <!-- 5. Bảng Danh Sách Lịch Sử Quyết Định Chi Tiết -->
    <div class="card border-0 shadow-sm rounded-4 overflow-hidden bg-white mb-4">
      <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center px-4">
        <span class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
          <span>📜</span>
          <span>Dòng Thời Gian Quyết Định Quản Lý Bếp</span>
        </span>
        <span class="badge bg-light text-secondary border fw-semibold">
          {{ totalItems }} bản ghi lịch sử
        </span>
      </div>

      <div class="card-body p-0">
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="bg-light table-light small text-secondary">
              <tr>
                <th class="ps-4" style="width: 170px;">Thời Gian</th>
                <th style="width: 170px;">Loại Quyết Định</th>
                <th style="width: 180px;">Người Ra Quyết Định</th>
                <th>Món Ăn & Bàn Liên Quan</th>
                <th>Lý Do Giải Trình (Justification)</th>
                <th class="text-center" style="width: 110px;">Chi Tiết</th>
              </tr>
            </thead>
            <tbody class="small">
              <!-- Loading Skeleton / Empty State -->
              <tr v-if="isLoading">
                <td colspan="6" class="text-center py-5 text-muted">
                  <div class="spinner-border text-primary spinner-border-sm me-2"></div>
                  <span>Đang tải nhật ký quyết định bếp...</span>
                </td>
              </tr>

              <tr v-else-if="decisions.length === 0">
                <td colspan="6" class="text-center py-5 text-muted">
                  <div class="fs-1 mb-2">🍳</div>
                  <div class="fw-bold">Chưa có quyết định nào từ quản lý bếp trong bộ lọc này</div>
                  <small class="text-secondary">Các hành động báo hết món, hủy món, hoàn tác từ màn hình KDS sẽ được ghi nhận tại đây.</small>
                </td>
              </tr>

              <!-- Data Rows -->
              <tr v-for="d in decisions" :key="d.auditLogId">
                <!-- 1. Thời Gian -->
                <td class="ps-4">
                  <div class="fw-bold text-dark font-monospace">{{ formatTime(d.createdAt) }}</div>
                  <div class="text-muted small" style="font-size: 0.72rem;">{{ formatDate(d.createdAt) }}</div>
                </td>

                <!-- 2. Loại Quyết Định Badge -->
                <td>
                  <span :class="['badge d-inline-flex align-items-center gap-1 px-2.5 py-1.5 fw-bold rounded-pill', getBadgeClass(d.decisionType)]">
                    <span>{{ getDecisionIcon(d.decisionType) }}</span>
                    <span>{{ d.decisionTitle }}</span>
                  </span>
                </td>

                <!-- 3. Người Ra Quyết Định -->
                <td>
                  <div class="d-flex align-items-center gap-2">
                    <div class="avatar-circle-sm">
                      {{ (d.chefName || 'B').charAt(0).toUpperCase() }}
                    </div>
                    <div class="text-truncate">
                      <div class="fw-bold text-dark text-truncate">{{ d.chefName }}</div>
                      <span class="badge bg-secondary-subtle text-secondary" style="font-size: 0.65rem;">
                        {{ d.chefRole }}
                      </span>
                    </div>
                  </div>
                </td>

                <!-- 4. Món Ăn & Bàn Liên Quan -->
                <td>
                  <div v-if="d.foodName" class="fw-bold text-dark d-flex align-items-center gap-1.5">
                    <span>{{ d.foodName }}</span>
                    <span v-if="d.variantName" class="badge bg-light text-dark border font-monospace" style="font-size: 0.7rem;">
                      {{ d.variantName }}
                    </span>
                  </div>

                  <div class="d-flex align-items-center gap-2 mt-1 small">
                    <span v-if="d.tableDisplay" class="badge bg-primary-subtle text-primary fw-semibold">
                      Bàn: {{ d.tableDisplay }}
                    </span>
                    <span v-if="d.orderId" class="badge bg-secondary-subtle text-secondary font-monospace">
                      Đơn #{{ d.orderId }}
                    </span>
                    <span v-if="d.detailsSummary" class="text-muted small">
                      {{ d.detailsSummary }}
                    </span>
                  </div>
                </td>

                <!-- 5. Lý Do Giải Trình (Prominent Highlight) -->
                <td>
                  <div v-if="d.reason" class="reason-highlight-box p-2 rounded-3 border">
                    <span class="quote-mark">“</span>
                    <span class="fw-semibold text-dark">{{ d.reason }}</span>
                    <span class="quote-mark">”</span>
                  </div>
                  <div v-else class="text-muted fst-italic small">
                    (Không có ghi chú giải trình kèm theo)
                  </div>
                </td>

                <!-- 6. Thao Tác / Modal -->
                <td class="text-center">
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-secondary px-2.5 py-1"
                    title="Xem chi tiết dữ liệu snapshot"
                    @click="openDetailModal(d)"
                  >
                    🔍 Chi tiết
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- 6. Phân Trang (Pagination) -->
        <div v-if="totalPages > 1" class="d-flex justify-content-between align-items-center p-3 border-top bg-light">
          <small class="text-muted">
            Hiển thị trang <strong>{{ currentPage }}</strong> / <strong>{{ totalPages }}</strong> (Tổng {{ totalItems }} bản ghi)
          </small>
          <div class="d-flex gap-1">
            <button
              class="btn btn-sm btn-outline-secondary px-3"
              :disabled="currentPage <= 1 || isLoading"
              @click="goToPage(currentPage - 1)"
            >
              ◀ Trước
            </button>
            <button
              class="btn btn-sm btn-outline-secondary px-3"
              :disabled="currentPage >= totalPages || isLoading"
              @click="goToPage(currentPage + 1)"
            >
              Sau ▶
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 7. MODAL XEM CHI TIẾT DỮ LIỆU SNAPSHOT QUYẾT ĐỊNH -->
    <div v-if="activeDetailItem" class="modal-backdrop-custom d-flex justify-content-center align-items-center p-3" @click.self="activeDetailItem = null">
      <div class="modal-card bg-white rounded-4 shadow-xl border overflow-hidden" style="max-width: 600px; width: 100%;">
        <div class="modal-header p-3.5 border-bottom bg-slate-50 d-flex justify-content-between align-items-center">
          <div class="d-flex align-items-center gap-2">
            <span class="fs-5">{{ getDecisionIcon(activeDetailItem.decisionType) }}</span>
            <strong class="fs-6 text-dark">{{ activeDetailItem.decisionTitle }}</strong>
          </div>
          <button type="button" class="btn-close" @click="activeDetailItem = null"></button>
        </div>

        <div class="modal-body p-4">
          <div class="mb-3 p-3 bg-light rounded-3 border">
            <div class="row g-2 small">
              <div class="col-6">
                <span class="text-muted">Thời gian:</span>
                <div class="fw-bold">{{ formatFullDate(activeDetailItem.createdAt) }}</div>
              </div>
              <div class="col-6">
                <span class="text-muted">Người quyết định:</span>
                <div class="fw-bold text-primary">{{ activeDetailItem.chefName }} ({{ activeDetailItem.chefRole }})</div>
              </div>
              <div v-if="activeDetailItem.foodName" class="col-6">
                <span class="text-muted">Món ăn:</span>
                <div class="fw-bold">{{ activeDetailItem.foodName }} {{ activeDetailItem.variantName ? `(${activeDetailItem.variantName})` : '' }}</div>
              </div>
              <div v-if="activeDetailItem.orderId" class="col-6">
                <span class="text-muted">Đơn hàng / Bàn:</span>
                <div class="fw-bold">Đơn #{{ activeDetailItem.orderId }} - Bàn: {{ activeDetailItem.tableDisplay || 'N/A' }}</div>
              </div>
            </div>
          </div>

          <div v-if="activeDetailItem.reason" class="mb-3">
            <label class="form-label small fw-bold text-secondary">Lý Do Giải Trình Từ Bếp:</label>
            <div class="p-3 bg-warning-subtle text-dark rounded-3 border border-warning-subtle fw-semibold">
              "{{ activeDetailItem.reason }}"
            </div>
          </div>

          <div>
            <label class="form-label small fw-bold text-secondary">Dữ Liệu Snapshot Kèm Theo (JSON Log):</label>
            <pre class="bg-dark text-light p-3 rounded-3 small font-monospace overflow-auto" style="max-height: 220px;">{{ JSON.stringify(activeDetailItem.rawData, null, 2) }}</pre>
          </div>
        </div>

        <div class="modal-footer p-3 bg-slate-50 border-top text-end">
          <button type="button" class="btn btn-secondary btn-sm px-4 fw-semibold" @click="activeDetailItem = null">
            Đóng
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import axios from 'axios';

const apiBase = 'http://localhost:5079/api/admin/kitchen-history';

const isLoading = ref(false);
const errorMsg = ref('');
const decisions = ref([]);
const summary = ref({
  totalDecisions: 0,
  soldOutCount: 0,
  reopenedCount: 0,
  cancelledCount: 0,
  undoCount: 0,
  readyAllCount: 0,
  todayCount: 0,
  topReasons: []
});
const chefsList = ref([]);

// Filter params
const searchKeyword = ref('');
const selectedActionType = ref('all');
const selectedChefId = ref(null);
const timePreset = ref('all');
const fromDate = ref(null);
const toDate = ref(null);
const currentPage = ref(1);
const totalPages = ref(1);
const totalItems = ref(0);

const activeDetailItem = ref(null);

async function fetchData() {
  isLoading.value = true;
  errorMsg.value = '';
  try {
    const params = {
      actionType: selectedActionType.value,
      chefId: selectedChefId.value,
      search: searchKeyword.value || undefined,
      fromDate: fromDate.value || undefined,
      toDate: toDate.value || undefined,
      page: currentPage.value,
      pageSize: 20
    };

    const [decisionsRes, summaryRes, chefsRes] = await Promise.all([
      axios.get(`${apiBase}/decisions`, { params }),
      axios.get(`${apiBase}/summary`, { params: { fromDate: fromDate.value, toDate: toDate.value } }),
      axios.get(`${apiBase}/chefs`)
    ]);

    if (decisionsRes.data) {
      decisions.value = decisionsRes.data.items || [];
      totalPages.value = decisionsRes.data.totalPages || 1;
      totalItems.value = decisionsRes.data.totalItems || 0;
    }

    if (summaryRes.data) {
      summary.value = summaryRes.data;
    }

    if (chefsRes.data) {
      chefsList.value = chefsRes.data || [];
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải dữ liệu lịch sử quyết định bếp.';
  } finally {
    isLoading.value = false;
  }
}

function applyFilters() {
  currentPage.value = 1;
  fetchData();
}

function goToPage(page) {
  currentPage.value = page;
  fetchData();
}

function toggleReasonFilter(reason) {
  if (searchKeyword.value === reason) {
    searchKeyword.value = '';
  } else {
    searchKeyword.value = reason;
  }
  applyFilters();
}

function onTimePresetChange() {
  const now = new Date();
  if (timePreset.value === 'today') {
    fromDate.value = now.toISOString().slice(0, 10);
    toDate.value = now.toISOString().slice(0, 10);
  } else if (timePreset.value === '7days') {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    fromDate.value = d.toISOString().slice(0, 10);
    toDate.value = now.toISOString().slice(0, 10);
  } else if (timePreset.value === '30days') {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    fromDate.value = d.toISOString().slice(0, 10);
    toDate.value = now.toISOString().slice(0, 10);
  } else {
    fromDate.value = null;
    toDate.value = null;
  }
  applyFilters();
}

function openDetailModal(item) {
  activeDetailItem.value = item;
}

function getDecisionIcon(type) {
  switch (type) {
    case 'sold_out': return '🛑';
    case 'reopened': return '✅';
    case 'cancelled': return '❌';
    case 'undo': return '🔄';
    case 'ready_all': return '✨';
    default: return '🍳';
  }
}

function getBadgeClass(type) {
  switch (type) {
    case 'sold_out': return 'bg-warning-subtle text-warning border border-warning-subtle';
    case 'reopened': return 'bg-success-subtle text-success border border-success-subtle';
    case 'cancelled': return 'bg-danger-subtle text-danger border border-danger-subtle';
    case 'undo': return 'bg-info-subtle text-info border border-info-subtle';
    case 'ready_all': return 'bg-primary-subtle text-primary border border-primary-subtle';
    default: return 'bg-secondary-subtle text-secondary';
  }
}

function formatTime(isoStr) {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function formatDate(isoStr) {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatFullDate(isoStr) {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  return d.toLocaleString('vi-VN', { hour12: false });
}

function exportCsv() {
  if (!decisions.value.length) {
    alert('Không có dữ liệu để xuất.');
    return;
  }
  const headers = ['Mã Log', 'Thời Gian', 'Loại Quyết Định', 'Bếp Trưởng', 'Món Ăn', 'Bàn / Đơn', 'Lý Do'];
  const rows = decisions.value.map(d => [
    d.auditLogId,
    `"${formatFullDate(d.createdAt)}"`,
    `"${d.decisionTitle}"`,
    `"${d.chefName}"`,
    `"${d.foodName || ''} ${d.variantName ? `(${d.variantName})` : ''}"`,
    `"Bàn ${d.tableDisplay || ''} (#${d.orderId || ''})"`,
    `"${(d.reason || '').replace(/"/g, '""')}"`
  ]);

  const csvContent = '\uFEFF' + [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.setAttribute('href', url);
  link.setAttribute('download', `Lich_Su_Quyet_Dinh_Bep_${new Date().toISOString().slice(0, 10)}.csv`);
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

onMounted(() => {
  fetchData();
});
</script>

<style scoped>
.metric-icon-circle {
  width: 44px;
  height: 44px;
  display: grid;
  place-items: center;
  border-radius: 12px;
  font-size: 1.3rem;
}

.avatar-circle-sm {
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: #0f766e;
  color: #ffffff;
  font-weight: bold;
  font-size: 0.8rem;
  flex-shrink: 0;
}

.reason-chip-btn {
  border: 1px dashed #cbd5e1;
  background: #f8fafc;
  color: #334155;
  border-radius: 20px;
  font-size: 0.78rem;
  padding: 4px 10px;
  transition: all 0.15s ease;
}

.reason-chip-btn:hover {
  background: #f1f5f9;
  border-color: #94a3b8;
}

.reason-chip-btn.active {
  background: #0f766e;
  color: #ffffff;
  border-color: #0f766e;
}

.reason-chip-btn.active .badge {
  background: #ffffff !important;
  color: #0f766e !important;
}

.reason-highlight-box {
  background: #fffbeb;
  border-color: #fde68a !important;
  color: #92400e;
  max-width: 360px;
  font-size: 0.78rem;
  line-height: 1.4;
}

.quote-mark {
  color: #d97706;
  font-weight: 900;
  font-size: 1rem;
}

/* Modal Backdrop */
.modal-backdrop-custom {
  position: fixed;
  z-index: 1099;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
}
</style>

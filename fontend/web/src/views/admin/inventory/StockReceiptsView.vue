<template>
  <div class="stock-receipts-page">
    <!-- Header Banner -->
    <div class="card-modern p-4 mb-4 text-white position-relative overflow-hidden" 
         style="background: linear-gradient(135deg, #064e3b 0%, #047857 50%, #059669 100%); border-radius: 12px; box-shadow: 0 4px 20px rgba(4, 120, 87, 0.15);">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 position-relative" style="z-index: 2;">
        <div>
          <div class="d-flex align-items-center gap-2 mb-2">
            <span class="badge bg-warning text-dark px-3 py-1 fw-bold">WMS · STOCK RECEIPTS</span>
            <span class="badge bg-success-subtle text-white border border-light border-opacity-25 px-2 py-1">PHẦN 3: PHIẾU XUẤT NHẬP KHO</span>
          </div>
          <h2 class="fw-bold mb-1">📑 Quản Lý Phiếu Kho & Trích Xuất Hao Hụt</h2>
          <p class="text-white-50 mb-0">Lập phiếu nhập NCC, trích xuất hao hụt sơ chế tự động, xuất bếp theo định mức BOM và duyệt biến động tồn chuẩn FEFO.</p>
        </div>
        <div class="d-flex flex-wrap gap-2">
          <router-link to="/admin/inventory" class="btn btn-outline-light fw-semibold d-flex align-items-center gap-2">
            <span>📦</span>
            <span>Kho Nguyên Liệu</span>
          </router-link>
          <router-link to="/admin/kitchen-reconciliation" class="btn btn-outline-light fw-semibold d-flex align-items-center gap-2">
            <span>↩</span>
            <span>Đối Soát Cuối Ca</span>
          </router-link>
          <button class="btn btn-warning fw-bold d-flex align-items-center gap-2 shadow-sm" type="button" @click="openCreateReceipt">
            <span>➕</span>
            <span>Tạo Phiếu Kho</span>
          </button>
        </div>
      </div>
    </div>

    <!-- 4 Metric Cards -->
    <div class="row g-3 mb-4">
      <div class="col-6 col-xl-3">
        <div class="metric-card h-100">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="metric-label">Tổng phiếu hiển thị</span>
              <strong>{{ receipts.length }}</strong>
              <small>{{ totalCount }} phiếu trong bộ lọc</small>
            </div>
            <div class="stat-badge bg-primary-subtle text-primary p-2 rounded-circle fs-5">📋</div>
          </div>
        </div>
      </div>
      <div class="col-6 col-xl-3">
        <div class="metric-card metric-warning h-100">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="metric-label">Chờ duyệt (Nháp)</span>
              <strong class="text-warning">{{ draftCount }}</strong>
              <small>Cần kiểm tra trước khi ghi sổ</small>
            </div>
            <div class="stat-badge bg-warning-subtle text-warning p-2 rounded-circle fs-5">⏳</div>
          </div>
        </div>
      </div>
      <div class="col-6 col-xl-3">
        <div class="metric-card metric-success h-100">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="metric-label">Đã duyệt (Ghi sổ)</span>
              <strong class="text-success">{{ approvedCount }}</strong>
              <small>Đã cập nhật tồn kho chính thức</small>
            </div>
            <div class="stat-badge bg-success-subtle text-success p-2 rounded-circle fs-5">✅</div>
          </div>
        </div>
      </div>
      <div class="col-6 col-xl-3">
        <div class="metric-card metric-danger h-100">
          <div class="d-flex justify-content-between align-items-start">
            <div>
              <span class="metric-label">Đã hủy bỏ</span>
              <strong class="text-danger">{{ cancelledCount }}</strong>
              <small>Không làm thay đổi tồn kho</small>
            </div>
            <div class="stat-badge bg-danger-subtle text-danger p-2 rounded-circle fs-5">🚫</div>
          </div>
        </div>
      </div>
    </div>

    <section class="content-panel">
      <div class="filter-toolbar">
        <div class="search-box">
          <span aria-hidden="true">⌕</span>
          <input v-model="search" type="search" placeholder="Tìm mã phiếu, nhà cung cấp..." />
        </div>
        <select v-model="filters.type" class="form-select form-select-sm filter-select" @change="reloadReceipts">
          <option value="all">Tất cả loại phiếu</option>
          <option v-for="type in receiptTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
        </select>
        <select v-model="filters.status" class="form-select form-select-sm filter-select" @change="reloadReceipts">
          <option value="all">Tất cả trạng thái</option>
          <option value="draft">Chờ duyệt</option>
          <option value="approved">Đã duyệt</option>
          <option value="cancelled">Đã hủy</option>
        </select>
        <input v-model="filters.fromDate" type="date" class="form-control form-control-sm date-filter" @change="reloadReceipts" />
        <input v-model="filters.toDate" type="date" class="form-control form-control-sm date-filter" @change="reloadReceipts" />
        <button class="btn btn-light border btn-sm" type="button" title="Tải lại" @click="reloadReceipts">↻</button>
      </div>

      <div v-if="errorMessage" class="alert alert-danger mx-3 mt-3 mb-0">{{ errorMessage }}</div>

      <div class="table-responsive">
        <table class="table align-middle mb-0 receipt-table">
          <thead>
            <tr>
              <th>Phiếu kho</th>
              <th>Loại phiếu</th>
              <th>Đối tác / Ghi chú</th>
              <th class="text-center">Mặt hàng</th>
              <th class="text-end">Giá trị</th>
              <th>Ngày lập</th>
              <th class="text-center">Trạng thái</th>
              <th class="text-end">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td colspan="8" class="empty-state">Đang tải danh sách phiếu kho...</td>
            </tr>
            <tr v-else-if="filteredReceipts.length === 0">
              <td colspan="8" class="empty-state">Không tìm thấy phiếu kho phù hợp.</td>
            </tr>
            <tr v-for="receipt in filteredReceipts" v-else :key="receipt.receiptId">
              <td>
                <button class="receipt-code" type="button" @click="openDetail(receipt.receiptId)">{{ receipt.receiptCode }}</button>
                <small class="d-block text-muted">#{{ receipt.receiptId }}</small>
              </td>
              <td><span class="type-chip" :class="`type-${receipt.receiptType}`">{{ typeLabel(receipt.receiptType, receipt.receiptCode) }}</span></td>
              <td>
                <strong v-if="receipt.supplierName" class="d-block">{{ receipt.supplierName }}</strong>
                <span class="text-muted small">{{ receipt.note || 'Không có ghi chú' }}</span>
              </td>
              <td class="text-center">{{ receipt.itemCount }}</td>
              <td class="text-end fw-semibold">{{ formatCurrency(receipt.totalAmount) }}</td>
              <td class="text-muted small">{{ formatDateTime(receipt.createdAt) }}</td>
              <td class="text-center"><span class="status-chip" :class="`status-${receipt.status}`">{{ statusLabel(receipt.status) }}</span></td>
              <td class="text-end">
                <div class="action-group">
                  <button class="btn btn-sm btn-light border" type="button" title="Xem chi tiết" @click="openDetail(receipt.receiptId)">Xem</button>
                  <button v-if="receipt.status === 'draft'" class="btn btn-sm btn-success" type="button" title="Duyệt phiếu" @click="approveReceipt(receipt)">Duyệt</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination-bar" v-if="totalPages > 1">
        <span class="text-muted small">Trang {{ page }} / {{ totalPages }}</span>
        <div class="d-flex gap-2">
          <button class="btn btn-sm btn-light border" type="button" :disabled="page <= 1" @click="changePage(page - 1)">‹ Trước</button>
          <button class="btn btn-sm btn-light border" type="button" :disabled="page >= totalPages" @click="changePage(page + 1)">Sau ›</button>
        </div>
      </div>
    </section>

    <div v-if="showDetail" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3" @click.self="showDetail = false">
      <section class="receipt-modal detail-modal" role="dialog" aria-modal="true">
        <div class="modal-head">
          <div>
            <div class="eyebrow">CHI TIẾT PHIẾU KHO</div>
            <h4 class="mb-0">{{ selectedReceipt?.receiptCode }}</h4>
          </div>
          <button class="btn-close" type="button" aria-label="Đóng" @click="showDetail = false"></button>
        </div>
        <div v-if="detailLoading" class="empty-state py-5">Đang tải chi tiết...</div>
        <template v-else-if="selectedReceipt">
          <div class="detail-summary">
            <div><span>Loại phiếu</span><strong>{{ typeLabel(selectedReceipt.receiptType, selectedReceipt.receiptCode) }}</strong></div>
            <div><span>Trạng thái</span><strong><span class="status-chip" :class="`status-${selectedReceipt.status}`">{{ statusLabel(selectedReceipt.status) }}</span></strong></div>
            <div><span>Người lập</span><strong>{{ selectedReceipt.createdByName || 'N/A' }}</strong></div>
            <div><span>Thời gian</span><strong>{{ formatDateTime(selectedReceipt.createdAt) }}</strong></div>
          </div>

          <div v-if="selectedReceipt.status === 'approved'" class="inventory-posted-note">
            <strong>✓ Đã ghi nhận vào tồn kho</strong>
            <span>Phiếu đã tạo biến động kho và trừ tồn theo lô FEFO/FIFO. Các lô thực tế được hiển thị ở cột “Lô”.</span>
            <small v-if="selectedReceipt.approvedAt">Thời điểm duyệt: {{ formatDateTime(selectedReceipt.approvedAt) }}</small>
          </div>
          <div v-else-if="selectedReceipt.status === 'draft'" class="inventory-pending-note">
            <strong>! Chưa ghi nhận vào tồn kho</strong>
            <span>Tồn kho chỉ thay đổi sau khi thủ kho bấm “Duyệt và ghi nhận kho”.</span>
          </div>

          <div class="detail-note" v-if="selectedReceipt.note"><strong>Ghi chú:</strong> {{ selectedReceipt.note }}</div>

          <div class="table-responsive detail-table-wrap">
            <table class="table align-middle mb-0">
              <thead><tr><th>Nguyên liệu</th><th class="text-end">Số lượng</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th><th>Lô</th></tr></thead>
              <tbody>
                <tr v-for="detail in selectedReceipt.details" :key="detail.detailId">
                  <td><strong>{{ detail.itemName }}</strong><small class="d-block text-muted">{{ detail.baseUnit }} · {{ detail.purchaseUnit }}</small></td>
                  <td class="text-end">Mua: {{ formatQuantity(detail.grossQuantityInPurchaseUnit) }} {{ detail.purchaseUnit }}<small class="d-block text-muted">Dự kiến dùng: {{ formatQuantity(detail.expectedUsableQuantity ?? detail.quantity) }} {{ detail.baseUnit }}</small><small class="d-block text-success">Thực nhận: {{ formatQuantity(detail.quantity) }} {{ detail.baseUnit }}</small><small v-if="detail.wastageQuantity > 0" class="d-block text-warning-emphasis">Hao hụt: {{ formatQuantity(detail.wastageQuantity) }} {{ detail.baseUnit }} · thực tế {{ formatQuantity(actualLossPercent(detail)) }}%, dự kiến {{ detail.wastagePercent }}%</small><small v-if="detail.adjustmentNote" class="d-block text-danger">Lý do: {{ detail.adjustmentNote }}</small></td>
                  <td class="text-end">{{ formatCurrency(detail.unitCost) }}/{{ detail.purchaseUnit }}</td>
                  <td class="text-end fw-semibold">{{ formatCurrency(detail.subtotal) }}</td>
                  <td><span class="font-monospace small">{{ detail.batchCode || detail.newBatchCode || 'FEFO tự phân bổ' }}</span></td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="modal-total"><span>Tổng phiếu</span><strong>{{ formatCurrency(selectedReceipt.totalAmount) }}</strong></div>
          <div class="modal-actions">
            <button v-if="selectedReceipt.status === 'draft'" class="btn btn-outline-danger" type="button" @click="cancelReceipt(selectedReceipt)">Hủy phiếu</button>
            <span class="flex-spacer"></span>
            <button v-if="selectedReceipt.status === 'draft'" class="btn btn-success fw-bold" type="button" @click="approveReceipt(selectedReceipt)">Duyệt và ghi nhận kho</button>
            <button class="btn btn-light border" type="button" @click="showDetail = false">Đóng</button>
          </div>
        </template>
      </section>
    </div>

    <div v-if="showForm" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3" @click.self="showForm = false">
      <section class="receipt-modal form-modal" role="dialog" aria-modal="true">
        <div class="modal-head">
          <div>
            <div class="eyebrow">PHIẾU NHÁP</div>
            <h4 class="mb-0">Tạo phiếu kho</h4>
          </div>
          <button class="btn-close" type="button" aria-label="Đóng" @click="showForm = false"></button>
        </div>

        <form class="receipt-form" @submit.prevent="saveReceipt">
          <div class="receipt-form-scroll">
          <div class="receipt-intro mb-3">
            <div class="receipt-intro-icon" aria-hidden="true">+</div>
            <div>
              <strong>Nhập nhiều nguyên liệu trong cùng một phiếu</strong>
              <span>Thêm từng nguyên liệu, nhập số lượng mua và thực nhận sau sơ chế. Tồn kho chỉ tăng sau khi phiếu được duyệt.</span>
            </div>
          </div>
          <div v-if="formError" class="receipt-form-error mb-3" role="alert">{{ formError }}</div>

          <div class="row g-3 mb-3">
            <div class="col-md-5">
              <label class="form-label">Loại phiếu <span class="text-danger">*</span></label>
              <select v-model="receiptForm.receiptType" class="form-select" required>
                <option v-for="type in createReceiptTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
              </select>
            </div>
            <div class="col-md-7">
              <label class="form-label">Nhà cung cấp</label>
              <select v-model="receiptForm.supplierId" class="form-select">
                <option :value="null">-- Không chọn / nội bộ --</option>
                <option v-for="supplier in suppliers" :key="supplier.supplierId" :value="supplier.supplierId">{{ supplier.supplierName }}</option>
              </select>
            </div>
            <div class="col-12">
              <label class="form-label">Ghi chú phiếu</label>
              <input v-model="receiptForm.note" class="form-control" type="text" placeholder="Ví dụ: Nhập hàng ca sáng, xuất bếp theo kế hoạch..." />
            </div>
          </div>

          <div class="form-section-head">
            <div>
              <div class="d-flex align-items-center gap-2"><strong>Danh sách nguyên liệu nhập</strong><span class="line-count">{{ receiptForm.details.length }} dòng</span></div>
              <small>Mỗi dòng là một nguyên liệu khác nhau. Giá mua có thể thay đổi theo từng dòng/lô.</small>
            </div>
            <button class="btn btn-sm btn-outline-success add-line-button" type="button" @click="addDetail">+ Thêm nguyên liệu</button>
          </div>

          <div class="detail-editor">
            <div v-if="receiptForm.details.length === 0" class="empty-state py-4">Chưa có mặt hàng. Hãy thêm ít nhất một dòng.</div>
            <div v-for="(detail, index) in receiptForm.details" :key="detail.clientKey" class="editor-row">
              <div class="editor-row-heading">
                <span class="editor-row-number">{{ String(index + 1).padStart(2, '0') }}</span>
                <strong>{{ selectedItem(detail)?.itemName || 'Chưa chọn nguyên liệu' }}</strong>
                <span v-if="receiptForm.receiptType === 'import_supplier' && selectedItem(detail)" class="editor-wastage-badge">Hao hụt {{ formatWastagePercent(detail.wastagePercent) }}%</span>
                <button class="editor-remove" type="button" :aria-label="`Xóa dòng ${index + 1}`" title="Xóa dòng" @click="removeDetail(index)">×</button>
              </div>
              <div class="editor-grid">
                <div class="editor-item">
                  <label :for="`receipt-item-${detail.clientKey}`">Nguyên liệu</label>
                  <select :id="`receipt-item-${detail.clientKey}`" v-model="detail.inventoryItemId" class="form-select form-select-sm" required @change="syncDetailItem(detail)">
                    <option :value="null">-- Chọn nguyên liệu --</option>
                    <option v-for="item in items" :key="item.inventoryItemId" :value="item.inventoryItemId" :disabled="isItemSelectedElsewhere(item.inventoryItemId, detail)">{{ item.itemName }} ({{ item.purchaseUnit }})</option>
                  </select>
                </div>
                <div class="editor-item">
                  <label>Số lượng mua</label>
                  <div class="input-group input-group-sm">
                    <input v-model.number="detail.purchaseQuantity" class="form-control" type="number" min="0.0001" step="any" required />
                    <span class="input-group-text">{{ detailPurchaseUnit(detail) }}</span>
                  </div>
                </div>
                <div class="editor-item">
                  <label>Giá mua thực tế</label>
                  <div class="input-group input-group-sm">
                    <input v-model.number="detail.unitCost" class="form-control" type="number" min="0" step="any" />
                    <span class="input-group-text">đ/{{ detailPurchaseUnit(detail) }}</span>
                  </div>
                </div>
                <div class="editor-item editor-amount">
                  <label>Thành tiền</label>
                  <strong>{{ formatCurrency(detailAmount(detail)) }}</strong>
                </div>
              </div>
              <div v-if="receiptForm.receiptType === 'import_supplier'" class="batch-fields">
                <div class="batch-field"><label :for="`receipt-batch-${detail.clientKey}`">Mã lô</label><input :id="`receipt-batch-${detail.clientKey}`" v-model="detail.newBatchCode" class="form-control form-control-sm" type="text" placeholder="Tự sinh nếu bỏ trống" /></div>
                <div class="batch-field"><label :for="`receipt-made-${detail.clientKey}`">Ngày sản xuất</label><input :id="`receipt-made-${detail.clientKey}`" v-model="detail.manufacturingDate" class="form-control form-control-sm" type="date" /></div>
                <div class="batch-field"><label :for="`receipt-expiry-${detail.clientKey}`">Hạn sử dụng <span class="text-danger">*</span></label><input :id="`receipt-expiry-${detail.clientKey}`" v-model="detail.expiryDate" class="form-control form-control-sm" type="date" required /></div>
                <div class="batch-summary"><span>Dự kiến dùng</span><strong>{{ formatQuantity(expectedUsable(detail)) }} {{ selectedItem(detail)?.baseUnit || '—' }}</strong></div>
                <div v-if="isWeightUnit(selectedItem(detail)?.baseUnit)" class="batch-field actual-field"><label :for="`receipt-actual-${detail.clientKey}`">Thực nhận sau sơ chế</label><div class="input-group input-group-sm"><input :id="`receipt-actual-${detail.clientKey}`" :value="actualUsable(detail)" class="form-control" type="number" min="0.0001" step="any" @input="detail.actualUsableQuantity = Number($event.target.value)" /><span class="input-group-text">{{ selectedItem(detail)?.baseUnit }}</span></div></div>
                <div class="batch-summary" :class="actualUsable(detail) < expectedUsable(detail) ? 'summary-danger' : 'summary-success'"><span>Hao hụt thực tế</span><strong>{{ formatQuantity(grossBaseQuantity(detail) - actualUsable(detail)) }} {{ selectedItem(detail)?.baseUnit || '—' }}</strong></div>
                <label v-if="requiresAdjustmentReason(detail)" class="batch-field adjustment-reason">Lý do hao hụt vượt dự kiến <span class="text-danger">*</span>
                  <input v-model="detail.adjustmentNote" class="form-control form-control-sm" type="text" maxlength="500" required placeholder="VD: loại bỏ phần dập, hư hỏng..." />
                </label>
              </div>
            </div>
          </div>

          </div>
          <div class="form-total"><span>{{ receiptForm.details.length }} nguyên liệu · Tổng phiếu nháp</span><strong>{{ formatCurrency(formTotal) }}</strong></div>
          <div class="modal-actions">
            <span class="text-muted small">Phiếu mới chỉ làm thay đổi tồn kho sau khi được duyệt.</span>
            <span class="flex-spacer"></span>
            <button class="btn btn-light border" type="button" @click="showForm = false">Hủy</button>
            <button class="btn btn-success fw-bold" type="submit" :disabled="saving">{{ saving ? 'Đang lưu...' : 'Lưu phiếu nháp' }}</button>
          </div>
        </form>
      </section>
    </div>

  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import axios from 'axios';

const API_BASE = 'http://localhost:5079/api/admin/inventory';
const RECEIPTS_API = `${API_BASE}/receipts`;

const receiptTypes = [
  { value: 'import_supplier', label: 'Nhập từ nhà cung cấp' },
  { value: 'export_kitchen', label: 'Xuất cho bếp' },
  { value: 'return_from_kitchen', label: 'Hoàn từ bếp' },
  { value: 'waste_disposal', label: 'Xuất hủy' },
  { value: 'audit_adjustment', label: 'Điều chỉnh kiểm kê' }
];

const createReceiptTypes = receiptTypes.filter(type => !['audit_adjustment', 'return_from_kitchen'].includes(type.value));
const items = ref([]);
const suppliers = ref([]);
const receipts = ref([]);
const search = ref('');
const loading = ref(false);
const saving = ref(false);
const detailLoading = ref(false);
const errorMessage = ref('');
const formError = ref('');
const page = ref(1);
const pageSize = ref(15);
const totalCount = ref(0);
const totalPages = ref(1);
const showDetail = ref(false);
const showForm = ref(false);
const selectedReceipt = ref(null);
const filters = ref({ type: 'all', status: 'all', fromDate: '', toDate: '' });
const receiptForm = ref({ receiptType: 'import_supplier', supplierId: null, note: '', details: [] });

const filteredReceipts = computed(() => {
  const term = search.value.trim().toLowerCase();
  if (!term) return receipts.value;
  return receipts.value.filter(receipt =>
    receipt.receiptCode?.toLowerCase().includes(term) ||
    receipt.supplierName?.toLowerCase().includes(term) ||
    receipt.note?.toLowerCase().includes(term)
  );
});

const draftCount = computed(() => receipts.value.filter(receipt => receipt.status === 'draft').length);
const approvedCount = computed(() => receipts.value.filter(receipt => receipt.status === 'approved').length);
const cancelledCount = computed(() => receipts.value.filter(receipt => receipt.status === 'cancelled').length);
const formTotal = computed(() => receiptForm.value.details.reduce((sum, detail) => sum + detailAmount(detail), 0));

function typeLabel(value, receiptCode = '') {
  if (value === 'export_kitchen' && String(receiptCode).startsWith('PXK-BS-')) return 'Xuất bếp bổ sung';
  return receiptTypes.find(type => type.value === value)?.label || value;
}

function statusLabel(value) {
  return { draft: 'Chờ duyệt', approved: 'Đã duyệt', cancelled: 'Đã hủy' }[value] || value;
}

function formatCurrency(value) {
  return `${Number(value || 0).toLocaleString('vi-VN')} đ`;
}

function formatQuantity(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 4 });
}

function formatWastagePercent(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 1 });
}

function apiErrorMessage(error, fallback) {
  const data = error?.response?.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data?.message) return data.message;
  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat().filter(Boolean);
    if (messages.length) return messages.join(' ');
  }
  if (error?.response?.status === 401) return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
  if (error?.response?.status === 403) return 'Tài khoản hiện tại không có quyền lập phiếu kho.';
  if (error?.code === 'ERR_NETWORK') return 'Không kết nối được máy chủ. Hãy kiểm tra backend tại http://localhost:5079.';
  return `${fallback}${error?.response?.status ? ` (HTTP ${error.response.status})` : ''}`;
}

function actualLossPercent(detail) {
  const gross = Number(detail.grossQuantity) || Number(detail.quantity) || 0;
  return gross > 0 ? (Number(detail.wastageQuantity) || 0) * 100 / gross : 0;
}

function formatDateTime(value) {
  if (!value) return 'N/A';
  const timestamp = String(value);
  const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(timestamp);
  const date = new Date(hasTimezone ? timestamp : `${timestamp}Z`);
  return date.toLocaleString('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'Asia/Ho_Chi_Minh',
    hour12: false
  });
}

function selectedItem(detail) {
  return items.value.find(item => Number(item.inventoryItemId) === Number(detail.inventoryItemId));
}

function detailPurchaseUnit(detail) {
  return selectedItem(detail)?.purchaseUnit || 'đơn vị';
}

function detailAmount(detail) {
  return (Number(detail.purchaseQuantity) || 0) * (Number(detail.unitCost) || 0);
}

function grossBaseQuantity(detail) {
  const rate = Number(selectedItem(detail)?.conversionRate) > 0 ? Number(selectedItem(detail).conversionRate) : 1;
  return (Number(detail.purchaseQuantity) || 0) * rate;
}

function expectedUsable(detail) {
  return grossBaseQuantity(detail) * (1 - (Number(detail.wastagePercent) || 0) / 100);
}

function actualUsable(detail) {
  return detail.actualUsableQuantity == null || detail.actualUsableQuantity === ''
    ? expectedUsable(detail)
    : Number(detail.actualUsableQuantity) || 0;
}

function requiresAdjustmentReason(detail) {
  return (Number(detail.wastagePercent) > 0 || actualUsable(detail) < grossBaseQuantity(detail)) && actualUsable(detail) < expectedUsable(detail) - 0.0001;
}

function isWeightUnit(unit) {
  return ['g', 'gam', 'gram', 'kg', 'kilogram', 'mg'].includes(String(unit || '').trim().toLowerCase());
}

function blankDetail() {
  return {
    clientKey: `${Date.now()}-${Math.random()}`,
    inventoryItemId: null,
    purchaseQuantity: 1,
    unitCost: 0,
    wastagePercent: 0,
    actualUsableQuantity: null,
    adjustmentNote: '',
    newBatchCode: '',
    manufacturingDate: '',
    expiryDate: ''
  };
}

function syncDetailItem(detail) {
  const item = selectedItem(detail);
  detail.unitCost = item?.costPerUnit ?? item?.costPerPurchaseUnit ?? 0;
  detail.purchaseQuantity = 1;
  detail.wastagePercent = Number(item?.prepWastagePercent) || 0;
  detail.actualUsableQuantity = null;
  detail.adjustmentNote = '';
}

function isItemSelectedElsewhere(itemId, currentDetail) {
  return receiptForm.value.details.some(detail => detail !== currentDetail && Number(detail.inventoryItemId) === Number(itemId));
}

function addDetail() {
  receiptForm.value.details.push(blankDetail());
}

function removeDetail(index) {
  receiptForm.value.details.splice(index, 1);
}

async function fetchCatalog() {
  const [itemsRes, suppliersRes] = await Promise.all([
    axios.get(`${API_BASE}/items`, { params: { isActive: true } }),
    axios.get(`${API_BASE}/suppliers`, { params: { isActive: true } })
  ]);
  items.value = itemsRes.data?.data || [];
  suppliers.value = suppliersRes.data?.data || [];
}

async function fetchReceipts() {
  loading.value = true;
  errorMessage.value = '';
  try {
    const params = {
      page: page.value,
      pageSize: pageSize.value,
      type: filters.value.type,
      status: filters.value.status
    };
    if (filters.value.fromDate) params.fromDate = filters.value.fromDate;
    if (filters.value.toDate) params.toDate = filters.value.toDate;

    const response = await axios.get(RECEIPTS_API, { params });
    if (response.data?.success) {
      receipts.value = response.data.data || [];
      totalCount.value = response.data.totalCount || 0;
      totalPages.value = response.data.totalPages || 1;
    }
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tải danh sách phiếu kho.';
  } finally {
    loading.value = false;
  }
}

async function reloadReceipts() {
  page.value = 1;
  await fetchReceipts();
}

async function changePage(nextPage) {
  page.value = nextPage;
  await fetchReceipts();
}

async function openDetail(receiptId) {
  showDetail.value = true;
  detailLoading.value = true;
  selectedReceipt.value = null;
  try {
    const response = await axios.get(`${RECEIPTS_API}/${receiptId}`);
    selectedReceipt.value = response.data?.data || null;
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tải chi tiết phiếu kho.';
    showDetail.value = false;
  } finally {
    detailLoading.value = false;
  }
}

function openCreateReceipt() {
  receiptForm.value = { receiptType: 'import_supplier', supplierId: null, note: '', details: [] };
  formError.value = '';
  addDetail();
  showForm.value = true;
}

async function saveReceipt() {
  formError.value = '';
  if (receiptForm.value.details.length === 0 || receiptForm.value.details.some(detail => !detail.inventoryItemId || Number(detail.purchaseQuantity) <= 0)) {
    formError.value = 'Phiếu kho phải có ít nhất một mặt hàng hợp lệ.';
    return;
  }

  const selectedItemIds = receiptForm.value.details.map(detail => Number(detail.inventoryItemId));
  if (new Set(selectedItemIds).size !== selectedItemIds.length) {
    formError.value = 'Mỗi nguyên liệu chỉ được nhập một lần trong cùng một phiếu. Hãy gộp số lượng vào cùng dòng.';
    return;
  }

  const invalidPrepLine = receiptForm.value.receiptType === 'import_supplier' && receiptForm.value.details.find(detail => {
    const actual = actualUsable(detail);
    return actual <= 0 || actual > grossBaseQuantity(detail) || (requiresAdjustmentReason(detail) && !detail.adjustmentNote?.trim());
  });
  if (invalidPrepLine) {
    formError.value = `Kiểm tra lượng thực nhận và lý do hao hụt của ${selectedItem(invalidPrepLine)?.itemName}.`;
    return;
  }

  saving.value = true;
  try {
    const payload = {
      receiptType: receiptForm.value.receiptType,
      supplierId: receiptForm.value.supplierId ? Number(receiptForm.value.supplierId) : null,
      note: receiptForm.value.note?.trim() || null,
      details: receiptForm.value.details.map(detail => {
        const item = selectedItem(detail);
        const rate = Number(item?.conversionRate) > 0 ? Number(item.conversionRate) : 1;
        return {
          inventoryItemId: Number(detail.inventoryItemId),
          quantity: Number(detail.purchaseQuantity) * rate,
          unitCost: Number(detail.unitCost) > 0 ? Number(detail.unitCost) : null,
          wastagePercent: receiptForm.value.receiptType === 'import_supplier' ? Number(detail.wastagePercent) || 0 : 0,
          actualUsableQuantity: receiptForm.value.receiptType === 'import_supplier' ? actualUsable(detail) : Number(detail.purchaseQuantity) * rate,
          adjustmentNote: detail.adjustmentNote?.trim() || null,
          newBatchCode: detail.newBatchCode?.trim() || null,
          manufacturingDate: detail.manufacturingDate || null,
          expiryDate: detail.expiryDate || null
        };
      })
    };

    await axios.post(RECEIPTS_API, payload);
    showForm.value = false;
    await fetchReceipts();
  } catch (error) {
    console.error('Lỗi tạo phiếu kho:', error);
    formError.value = apiErrorMessage(error, 'Không thể tạo phiếu kho.');
  } finally {
    saving.value = false;
  }
}

async function approveReceipt(receipt) {
  if (!confirm(`Duyệt phiếu ${receipt.receiptCode}? Tồn kho sẽ được cập nhật ngay.`)) return;
  try {
    await axios.post(`${RECEIPTS_API}/${receipt.receiptId}/approve`);
    await fetchReceipts();
    if (showDetail.value) await openDetail(receipt.receiptId);
  } catch (error) {
    alert(error.response?.data?.message || 'Không thể duyệt phiếu kho.');
  }
}

async function cancelReceipt(receipt) {
  if (!confirm(`Hủy phiếu ${receipt.receiptCode}?`)) return;
  try {
    await axios.post(`${RECEIPTS_API}/${receipt.receiptId}/cancel`);
    showDetail.value = false;
    await fetchReceipts();
  } catch (error) {
    alert(error.response?.data?.message || 'Không thể hủy phiếu kho.');
  }
}

onMounted(async () => {
  try {
    await Promise.all([fetchCatalog(), fetchReceipts()]);
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tải dữ liệu quản lý kho.';
  }
});
</script>

<style scoped>
.stock-receipts-page { max-width: 1600px; margin: 0 auto; }
.eyebrow { color: #16805f; font-size: 0.72rem; font-weight: 800; letter-spacing: 0.1em; }
.page-title { color: #17231f; font-weight: 800; }
.metric-card { min-height: 118px; padding: 18px; border: 1px solid #e4ebe7; border-radius: 8px; background: #fff; box-shadow: 0 3px 12px rgba(25, 54, 43, 0.05); }
.metric-card strong { display: block; margin: 7px 0 2px; color: #17231f; font-size: 1.65rem; }
.metric-card small { color: #7a8881; font-size: 0.76rem; }
.metric-label { color: #68766f; font-size: 0.76rem; font-weight: 700; text-transform: uppercase; }
.metric-warning { border-top: 3px solid #e6a400; }
.metric-success { border-top: 3px solid #16805f; }
.metric-danger { border-top: 3px solid #c94b4b; }
.content-panel { overflow: hidden; border: 1px solid #e4ebe7; border-radius: 8px; background: #fff; box-shadow: 0 3px 12px rgba(25, 54, 43, 0.05); }
.filter-toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; padding: 14px; border-bottom: 1px solid #e9efec; background: #fbfcfb; }
.search-box { display: flex; flex: 1 1 250px; align-items: center; gap: 7px; height: 34px; padding: 0 10px; border: 1px solid #d8e1dc; border-radius: 6px; background: #fff; }
.search-box span { color: #16805f; font-size: 1.2rem; }
.search-box input { width: 100%; border: 0; outline: 0; color: #27342e; font-size: 0.88rem; }
.filter-select { width: auto; min-width: 150px; }
.date-filter { width: 140px; }
.receipt-table thead th { padding: 12px 14px; color: #6b7871; background: #f7faf8; font-size: 0.72rem; letter-spacing: 0.04em; text-transform: uppercase; white-space: nowrap; }
.receipt-table tbody td { padding: 13px 14px; border-color: #edf2ef; }
.receipt-code { padding: 0; border: 0; background: transparent; color: #126f54; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-weight: 800; }
.receipt-code:hover { text-decoration: underline; }
.type-chip, .status-chip { display: inline-flex; align-items: center; padding: 4px 8px; border-radius: 999px; font-size: 0.73rem; font-weight: 700; white-space: nowrap; }
.type-import_supplier { color: #126f54; background: #e4f5ee; }
.type-export_kitchen { color: #895f00; background: #fff2cb; }
.type-return_from_kitchen { color: #075c9e; background: #e4f1ff; }
.type-waste_disposal { color: #a83d3d; background: #fde8e8; }
.type-audit_adjustment { color: #6249a1; background: #eee9fc; }
.status-draft { color: #895f00; background: #fff2cb; }
.status-approved { color: #126f54; background: #e4f5ee; }
.status-cancelled { color: #69756f; background: #edf0ee; }
.action-group { display: flex; justify-content: flex-end; gap: 6px; }
.empty-state { padding: 42px 18px !important; color: #75827c; text-align: center; }
.pagination-bar { display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; border-top: 1px solid #e9efec; }
.modal-backdrop-custom { position: fixed; z-index: 1050; inset: 0; background: rgba(19, 29, 25, 0.58); }
.receipt-modal { width: min(980px, 100%); max-height: calc(100vh - 32px); overflow: auto; border-radius: 8px; background: #fff; box-shadow: 0 18px 55px rgba(0, 0, 0, 0.2); }
.detail-modal { width: min(1000px, 100%); }
.form-modal { display: flex; width: min(1080px, 100%); max-height: calc(100dvh - 24px); overflow: hidden; flex-direction: column; }
.form-modal .modal-head { flex: 0 0 auto; }
.receipt-form { display: flex; min-height: 0; flex: 1 1 auto; flex-direction: column; }
.receipt-form-scroll { min-height: 0; overflow-y: auto; padding: 18px 24px 0; overscroll-behavior: contain; }
.receipt-intro { display: flex; align-items: flex-start; gap: 12px; padding: 12px 14px; color: #315044; background: #f0faf5; border: 1px solid #cfeadd; border-radius: 8px; }
.receipt-intro-icon { display: grid; width: 28px; height: 28px; flex: 0 0 auto; place-items: center; color: #fff; background: #16805f; border-radius: 7px; font-size: 1.25rem; font-weight: 700; }
.receipt-intro strong, .receipt-intro span { display: block; }
.receipt-intro span { margin-top: 3px; color: #667a70; font-size: 0.78rem; line-height: 1.45; }
.receipt-form-error { padding: 10px 12px; color: #9f2f39; background: #fff2f3; border: 1px solid #f2c5c9; border-radius: 7px; font-size: 0.82rem; line-height: 1.45; }
.line-count { padding: 3px 7px; color: #126f54; background: #e4f5ee; border-radius: 999px; font-size: 0.7rem; font-weight: 800; }
.add-line-button { min-height: 38px; white-space: nowrap; }
.modal-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; padding: 22px 24px 16px; border-bottom: 1px solid #e5ece8; }
.modal-head h4 { color: #17231f; font-weight: 800; }
.detail-summary { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; padding: 18px 24px; background: #f7faf8; }
.detail-summary span { display: block; margin-bottom: 4px; color: #748079; font-size: 0.76rem; }
.detail-summary strong { color: #25332c; font-size: 0.88rem; }
.detail-note { margin: 18px 24px 0; padding: 11px 12px; border-left: 3px solid #16805f; color: #53635b; background: #f4f9f6; font-size: 0.88rem; }
.detail-table-wrap { margin: 18px 24px; border: 1px solid #e4ebe7; border-radius: 6px; }
.detail-table-wrap th { color: #6b7871; background: #f7faf8; font-size: 0.72rem; text-transform: uppercase; }
.modal-total, .form-total { display: flex; align-items: center; justify-content: flex-end; gap: 18px; margin: 0 24px; padding: 14px 0; border-top: 1px solid #dfe8e2; color: #65736c; }
.modal-total strong, .form-total strong { color: #bd3e48; font-size: 1.35rem; }
.modal-actions { display: flex; align-items: center; gap: 8px; margin-top: 0; padding: 15px 24px; border-top: 1px solid #e5ece8; background: #fff; }
.flex-spacer { flex: 1; }
.form-section-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 12px; margin-top: 12px; padding-bottom: 9px; border-bottom: 1px solid #e5ece8; }
.form-section-head small { display: block; margin-top: 3px; color: #77847d; font-size: 0.78rem; }
.detail-editor { max-height: 360px; overflow: auto; padding: 8px 0; }
.editor-row { padding: 10px 0; border-bottom: 1px solid #edf2ef; }
.editor-row-heading { display: flex; align-items: center; gap: 8px; margin-bottom: 9px; color: #273b32; font-size: 0.82rem; }
.editor-row-heading strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.editor-row-number { display: inline-grid; width: 26px; height: 26px; flex: 0 0 auto; place-items: center; color: #126f54; background: #e4f5ee; border-radius: 6px; font-size: 0.7rem; font-weight: 800; }
.editor-wastage-badge { margin-left: auto; padding: 3px 7px; color: #895f00; background: #fff5d6; border: 1px solid #f2d889; border-radius: 999px; font-size: 0.68rem; font-weight: 700; white-space: nowrap; }
.editor-remove { width: 36px; height: 36px; flex: 0 0 auto; margin-left: auto; padding: 0; color: #a83d3d; background: #fff; border: 1px solid #f3c7c7; border-radius: 6px; font-size: 1.2rem; line-height: 1; }
.editor-remove:hover, .editor-remove:focus-visible { color: #fff; background: #c94b4b; }
.editor-grid { display: grid; grid-template-columns: minmax(230px, 1.7fr) minmax(145px, 1fr) minmax(160px, 1fr) minmax(130px, 0.8fr); align-items: end; gap: 9px; }
.editor-item label { display: block; margin-bottom: 4px; color: #69766f; font-size: 0.72rem; font-weight: 700; }
.editor-amount strong { display: block; height: 31px; padding: 6px 0; color: #16805f; font-size: 0.9rem; text-align: right; }
.batch-fields {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 10px;
  margin: 10px 37px 0 0;
  padding: 12px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
}
.batch-field label, .batch-summary span { display: block; margin-bottom: 4px; color: #69766f; font-size: 0.7rem; font-weight: 700; }
.batch-summary { align-self: end; min-height: 56px; padding: 8px 10px; background: #fff; border: 1px solid #dce8e0; border-radius: 6px; }
.batch-summary strong { color: #315044; font-size: 0.86rem; }
.summary-success { border-color: #b9e2cd; background: #f4fbf7; }
.summary-success strong { color: #16805f; }
.summary-danger { border-color: #efc4c4; background: #fff7f7; }
.summary-danger strong { color: #b23c45; }
.actual-field { min-width: 0; }
.adjustment-reason {
  grid-column: 1 / -1;
}

@media (max-width: 900px) {
  .detail-summary { grid-template-columns: repeat(2, 1fr); }
  .editor-grid { grid-template-columns: 1fr 1fr; }
  .editor-amount { grid-column: span 1; }
  .editor-remove { align-self: center; }
  .batch-fields { margin-right: 0; }
}

@media (max-width: 600px) {
  .filter-select, .date-filter { flex: 1 1 140px; width: auto; }
  .detail-summary { grid-template-columns: 1fr 1fr; }
  .detail-table-wrap { margin: 14px 12px; }
  .modal-head, .modal-actions { padding-left: 14px; padding-right: 14px; }
  .receipt-form-scroll { padding: 14px 14px 0; }
  .modal-total, .form-total { margin-left: 14px; margin-right: 14px; }
  .editor-grid { grid-template-columns: 1fr; }
  .editor-amount strong { text-align: left; }
  .batch-fields { grid-template-columns: 1fr; margin-right: 0; }
  .modal-actions { flex-wrap: wrap; }
  .form-section-head { align-items: stretch; flex-direction: column; }
  .add-line-button { align-self: stretch; }
  .receipt-intro { padding: 10px; }
  .editor-wastage-badge { margin-left: auto; }
}
.inventory-posted-note, .inventory-pending-note { display: grid; gap: 3px; margin: 14px 18px; padding: 11px 13px; border: 1px solid; border-radius: 7px; font-size: 12px; }
.inventory-posted-note { color: #166534; background: #f0fdf4; border-color: #bbf7d0; }
.inventory-pending-note { color: #92400e; background: #fffbeb; border-color: #fde68a; }
.inventory-posted-note strong, .inventory-pending-note strong { color: #172033; }
.inventory-posted-note span, .inventory-pending-note span, .inventory-posted-note small { color: #64748b; }
</style>

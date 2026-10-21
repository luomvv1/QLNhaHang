<template>
  <!-- ============================================================== -->
  <!-- 1. MODAL ĐỔI BÀN (SWITCH TABLE)                                -->
  <!-- ============================================================== -->
  <div v-if="showSwitch" class="modal-backdrop-staff" @click.self="$emit('close-switch')">
    <div class="modal-card-staff bg-white shadow-2xl rounded-4 overflow-hidden border">
      <div class="modal-header-staff p-3 px-4 d-flex justify-content-between align-items-center bg-gradient-info text-white">
        <div>
          <div class="small text-white-50 fw-semibold text-uppercase">Điều Phối Chỗ Ngồi</div>
          <h5 class="fw-bold m-0 text-white d-flex align-items-center gap-2">
            <span>🔄</span> Đổi Bàn · Hiện Tại: {{ activeTable?.tableName }}
          </h5>
        </div>
        <button type="button" class="btn-close-custom" @click="$emit('close-switch')">✕</button>
      </div>

      <div class="modal-body-staff p-3 p-md-4">
        <p class="text-muted small mb-3">
          Chuyển toàn bộ order của bàn <strong>{{ activeTable?.tableName }}</strong> ({{ activeTable?.areaName }}) sang một bàn trống khác.
        </p>

        <div class="mb-3">
          <label class="form-label fw-bold small text-dark">Chọn bàn trống để chuyển đến:</label>
          <div v-if="emptyTables.length === 0" class="alert alert-warning small py-2 mb-0">
            ⚠️ Hiện tại không có bàn trống nào khả dụng để đổi.
          </div>
          <div v-else class="row g-2 max-h-table-grid overflow-auto">
            <div
              v-for="t in emptyTables"
              :key="t.tableId"
              class="col-6 col-sm-4"
            >
              <div
                class="staff-table-pick-card p-2 rounded-3 border cursor-pointer text-center"
                :class="{ 'active-card': targetTableId === t.tableId }"
                @click="targetTableId = t.tableId"
              >
                <div class="fw-bold text-dark">{{ t.tableName }}</div>
                <div class="small text-muted">{{ t.areaName }} · {{ t.capacity }} chỗ</div>
                <span class="badge bg-success-subtle text-success border border-success-subtle mt-1" style="font-size: 0.65rem;">
                  🟢 Trống
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="modal-footer-staff p-3 px-4 d-flex justify-content-end gap-2 bg-light border-top">
        <button type="button" class="btn btn-light border px-3 rounded-3" @click="$emit('close-switch')">Hủy</button>
        <button
          type="button"
          class="btn btn-info fw-bold text-white px-4 rounded-3 shadow-sm"
          :disabled="!targetTableId || isSubmitting"
          @click="submitSwitch"
        >
          <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-1"></span>
          Xác Nhận Đổi Bàn
        </button>
      </div>
    </div>
  </div>

  <!-- ============================================================== -->
  <!-- 2. MODAL GHÉP / GỘP BÀN TINH GỌN (SMART MERGE TABLES)          -->
  <!-- Chỉ cần click là xong, tự động hiểu bàn trống hay phục vụ       -->
  <!-- ============================================================== -->
  <div v-if="showMerge" class="modal-backdrop-staff" @click.self="$emit('close-merge')">
    <div class="modal-card-staff bg-white shadow-2xl rounded-4 overflow-hidden border">
      <!-- Header -->
      <div class="modal-header-staff p-3 px-4 d-flex justify-content-between align-items-center bg-gradient-merge text-white">
        <div>
          <div class="small text-white-50 fw-semibold text-uppercase">Nghiệp Vụ Phục Vụ Tại Bàn</div>
          <h5 class="fw-bold m-0 text-white d-flex align-items-center gap-2">
            <span>🔗</span> Ghép / Gộp Bàn · Bàn {{ activeTable?.tableName }}
          </h5>
        </div>
        <button type="button" class="btn-close-custom" @click="$emit('close-merge')">✕</button>
      </div>

      <div class="modal-body-staff p-3 p-md-4">
        <!-- Active Table Context Notice -->
        <div class="p-2 px-3 rounded-3 bg-light border mb-3 d-flex align-items-center justify-content-between flex-wrap gap-2">
          <div class="small text-muted">
            Bàn gốc đang thao tác: <strong class="text-dark">{{ activeTable?.tableName }}</strong> ({{ activeTable?.areaName }} · Sức chứa: {{ activeTable?.capacity }} chỗ)
          </div>
          <span class="badge bg-primary-subtle text-primary border border-primary-subtle small">
            Đơn hiện tại: #{{ currentOrder?.orderId || 'Đang mở' }}
          </span>
        </div>
        <div class="alert alert-info py-2 px-3 small mb-3">
          Chỉ hiển thị bàn cùng khu vực với bàn gốc để tránh gộp sai khu vực.
        </div>

        <!-- Filter Pills -->
        <div class="d-flex align-items-center justify-content-between flex-wrap gap-2 mb-3">
          <div class="d-flex gap-1 flex-wrap">
            <button
              type="button"
              class="btn btn-xs rounded-pill px-3 py-1 fw-bold"
              :class="tableFilter === 'all' ? 'btn-dark' : 'btn-light border'"
              @click="tableFilter = 'all'"
            >
              Tất cả ({{ allCandidateTables.length }})
            </button>
            <button
              type="button"
              class="btn btn-xs rounded-pill px-3 py-1 fw-semibold"
              :class="tableFilter === 'empty' ? 'btn-success text-white' : 'btn-light border'"
              @click="tableFilter = 'empty'"
            >
              🟢 Bàn trống ({{ emptyTables.length }})
            </button>
            <button
              type="button"
              class="btn btn-xs rounded-pill px-3 py-1 fw-semibold"
              :class="tableFilter === 'serving' ? 'btn-warning text-dark' : 'btn-light border'"
              @click="tableFilter = 'serving'"
            >
              🟡 Bàn đang phục vụ ({{ otherServingTables.length }})
            </button>
          </div>

          <span class="small text-muted">
            👉 <em>Bấm vào 1 bàn bên dưới để ghép</em>
          </span>
        </div>

        <!-- Empty State if no candidates -->
        <div v-if="displayTables.length === 0" class="text-center p-4 border rounded-3 bg-light my-2">
          <div class="fs-2 mb-2">🪑</div>
          <div class="fw-bold text-muted small">Không tìm thấy bàn nào phù hợp với bộ lọc hiện tại.</div>
        </div>

        <!-- Visual Table Cards Grid -->
        <div v-else class="row g-2 max-h-table-grid overflow-auto">
          <div
            v-for="t in displayTables"
            :key="t.tableId"
            class="col-6 col-sm-4"
          >
            <div
              class="staff-table-pick-card p-3 rounded-3 border cursor-pointer position-relative h-100"
              :class="{
                'active-card': selectedMergeTableId === t.tableId,
                'border-warning-emphasis': t.tableStatus === 'serving'
              }"
              @click="handleSelectCandidate(t)"
            >
              <!-- Check icon -->
              <div v-if="selectedMergeTableId === t.tableId" class="selected-indicator">
                ✓
              </div>

              <div class="d-flex justify-content-between align-items-start mb-1">
                <span class="badge bg-secondary-subtle text-dark border small fw-bold font-monospace">
                  {{ t.tableCode }}
                </span>
                <span class="badge bg-light text-muted border" style="font-size: 0.65rem;">
                  {{ t.areaName }}
                </span>
              </div>

              <div class="fw-black text-dark fs-6 mt-1 mb-1">
                {{ t.tableName }}
              </div>

              <div class="small text-muted mb-2">
                👥 Sức chứa: <strong>{{ t.capacity }} chỗ</strong>
              </div>

              <!-- Status Badge -->
              <div>
                <span
                  v-if="t.tableStatus === 'serving'"
                  class="badge bg-warning-subtle text-dark border border-warning small d-inline-flex align-items-center gap-1"
                >
                  🟡 Đang ăn (Đơn #{{ t.activeOrder?.orderId }})
                </span>
                <span
                  v-else
                  class="badge bg-success-subtle text-success border border-success-subtle small d-inline-flex align-items-center gap-1"
                >
                  🟢 Bàn trống
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Smart Auto-Detection Helper Box (Explaining what will happen) -->
        <div v-if="selectedTableObj" class="mt-3 p-3 rounded-3 border bg-light shadow-xs animate-fadeIn">
          <div class="d-flex align-items-center gap-2 mb-1">
            <span class="badge px-2 py-1 rounded-pill fw-bold" :class="selectedTableObj.tableStatus === 'serving' ? 'bg-warning text-dark' : 'bg-success text-white'">
              {{ selectedTableObj.tableStatus === 'serving' ? '🟡 BÀN ĐANG PHỤC VỤ' : '🟢 BÀN TRỐNG' }}
            </span>
            <span class="fw-bold small text-dark">
              Đã chọn: {{ selectedTableObj.tableName }} ({{ selectedTableObj.areaName }})
            </span>
          </div>

          <!-- Auto explanation based on detected status -->
          <div v-if="selectedTableObj.tableStatus === 'serving'" class="small text-muted mt-2">
            💡 <strong>Hệ thống tự động hiểu:</strong> Gộp toàn bộ món ăn của bàn <strong>{{ selectedTableObj.tableName }}</strong> (Đơn #{{ selectedTableObj.activeOrder?.orderId }}) sang bàn <strong>{{ activeTable?.tableName }}</strong>. Sau khi gộp, hai bàn liên kết thành cụm bàn phục vụ chung 1 hóa đơn.
          </div>
          <div v-else class="small text-muted mt-2">
            💡 <strong>Hệ thống tự động hiểu:</strong> Ghép thêm bàn trống <strong>{{ selectedTableObj.tableName }}</strong> vào bàn <strong>{{ activeTable?.tableName }}</strong> để mở rộng chỗ ngồi cho khách. Cả 2 bàn cùng phục vụ chung đơn hàng hiện tại.
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="modal-footer-staff p-3 px-4 d-flex justify-content-between align-items-center bg-light border-top">
        <div class="small text-muted">
          <span v-if="selectedTableObj">
            Sẵn sàng ghép <strong>{{ activeTable?.tableName }}</strong> + <strong>{{ selectedTableObj.tableName }}</strong>
          </span>
          <span v-else class="text-danger small">
            Vui lòng bấm chọn 1 bàn ở trên để tiến hành
          </span>
        </div>

        <div class="d-flex gap-2">
          <button type="button" class="btn btn-light border px-3 rounded-3" @click="$emit('close-merge')">Hủy</button>
          <button
            type="button"
            class="btn btn-primary fw-bold px-4 rounded-3 shadow-sm"
            :disabled="!selectedMergeTableId || isSubmitting"
            @click="submitMerge"
          >
            <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-1"></span>
            {{ getSubmitButtonLabel() }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import axios from 'axios';

const props = defineProps({
  showSwitch: Boolean,
  showMerge: Boolean,
  activeTable: Object,
  currentOrder: Object,
  emptyTables: {
    type: Array,
    default: () => []
  },
  otherServingTables: {
    type: Array,
    default: () => []
  },
  orderItemsCount: Number
});

const emit = defineEmits(['close-switch', 'close-merge', 'switched', 'merged']);

const apiBase = 'http://localhost:5079/api/staff/orders';
const isSubmitting = ref(false);

// Switch state
const targetTableId = ref(null);

// Merge state
const tableFilter = ref('all'); // 'all', 'empty', 'serving'
const selectedMergeTableId = ref(null);

const allCandidateTables = computed(() => {
  const result = [];
  // Thêm bàn trống
  if (props.emptyTables) {
    props.emptyTables.forEach(t => {
      result.push({
        ...t,
        tableStatus: 'empty'
      });
    });
  }
  // Thêm bàn đang phục vụ khác
  if (props.otherServingTables) {
    props.otherServingTables.forEach(t => {
      result.push({
        ...t,
        tableStatus: 'serving'
      });
    });
  }
  const activeArea = (props.activeTable?.areaName || 'Sảnh chung').trim().toLowerCase();
  return result.filter(table => (table.areaName || 'Sảnh chung').trim().toLowerCase() === activeArea);
});

const displayTables = computed(() => {
  if (tableFilter.value === 'empty') {
    return allCandidateTables.value.filter(t => t.tableStatus === 'empty');
  }
  if (tableFilter.value === 'serving') {
    return allCandidateTables.value.filter(t => t.tableStatus === 'serving');
  }
  return allCandidateTables.value;
});

const selectedTableObj = computed(() => {
  if (!selectedMergeTableId.value) return null;
  return allCandidateTables.value.find(t => t.tableId === selectedMergeTableId.value) || null;
});

function handleSelectCandidate(t) {
  if (selectedMergeTableId.value === t.tableId) {
    selectedMergeTableId.value = null;
  } else {
    selectedMergeTableId.value = t.tableId;
  }
}

function getSubmitButtonLabel() {
  if (!selectedTableObj.value) return 'Xác Nhận Ghép Bàn';
  if (selectedTableObj.value.tableStatus === 'serving') {
    return `🔗 Xác Nhận Gộp Hóa Đơn Bàn ${selectedTableObj.value.tableName}`;
  }
  return `🔗 Xác Nhận Ghép Bàn ${selectedTableObj.value.tableName}`;
}

async function submitSwitch() {
  if (!props.activeTable || !targetTableId.value) return;
  isSubmitting.value = true;
  try {
    await axios.post(`${apiBase}/switch-table`, {
      fromTableId: props.activeTable.tableId,
      toTableId: targetTableId.value
    });
    
    const targetTable = props.emptyTables.find(t => t.tableId === targetTableId.value);
    emit('switched', targetTable);
    targetTableId.value = null;
  } catch (err) {
    alert(err.response?.data?.message || 'Không thể đổi bàn.');
  } finally {
    isSubmitting.value = false;
  }
}

async function submitMerge() {
  if (!props.activeTable || !selectedMergeTableId.value || !selectedTableObj.value) return;
  isSubmitting.value = true;

  // Tự động nhận diện actionType dựa trên trạng thái của bàn được chọn
  const isServing = selectedTableObj.value.tableStatus === 'serving';
  const actionType = isServing ? 'merge_orders' : 'link_empty';
  const keepSourceTableLinked = true; // Luôn giữ bàn liên kết khi gộp để khách ngồi cả 2 bàn

  try {
    await axios.post(`${apiBase}/merge-tables`, {
      currentTableId: props.activeTable.tableId,
      targetTableId: selectedMergeTableId.value,
      actionType,
      keepSourceTableLinked
    });

    emit('merged', { actionType, targetTableId: selectedMergeTableId.value });
    selectedMergeTableId.value = null;
  } catch (err) {
    alert(err.response?.data?.message || 'Lỗi khi thực hiện ghép/gộp bàn.');
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<style scoped>
.modal-backdrop-staff {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.7);
  display: grid;
  place-items: center;
  z-index: 1055;
  backdrop-filter: blur(5px);
  padding: 16px;
  overflow: hidden;
  overscroll-behavior: contain;
}

.modal-card-staff {
  width: min(720px, calc(100vw - 2rem));
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  border-radius: 20px;
  animation: staffModalPop 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  overscroll-behavior: contain;
}

@keyframes staffModalPop {
  from { opacity: 0; transform: translateY(16px) scale(0.98); }
  to { opacity: 1; transform: translateY(0) scale(1); }
}

.bg-gradient-info {
  background: linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%);
}

.bg-gradient-merge {
  background: linear-gradient(135deg, #4f46e5 0%, #3b82f6 100%);
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

.modal-body-staff {
  overflow-y: auto;
  max-height: calc(90vh - 130px);
  overscroll-behavior: contain;
}

.max-h-table-grid {
  max-height: 360px;
}

.staff-table-pick-card {
  background: #ffffff;
  border: 2px solid #e2e8f0 !important;
  transition: all 0.2s ease;
}
.staff-table-pick-card:hover {
  border-color: #94a3b8 !important;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.08);
}
.staff-table-pick-card.active-card {
  border-color: #3b82f6 !important;
  background-color: #eff6ff !important;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.25);
}

.selected-indicator {
  position: absolute;
  top: 8px;
  right: 8px;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: #3b82f6;
  color: #ffffff;
  font-weight: 900;
  font-size: 0.75rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-xs {
  font-size: 0.75rem;
}

.shadow-xs {
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.animate-fadeIn {
  animation: fadeIn 0.2s ease;
}
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(4px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>

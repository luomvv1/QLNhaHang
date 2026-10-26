<template>
  <InventoryWorkspaceNav />
  <main class="reconciliation-page">
    <header class="page-head">
      <div>
        <div class="eyebrow">BẾP & KHO</div>
        <h1>Hoàn / hủy cuối ca</h1>
      </div>
      <div class="head-actions">
        <label class="date-field">Ngày làm việc <input v-model="selectedDate" type="date" @change="loadReport" /></label>
        <button class="button secondary" type="button" :disabled="loading" @click="loadReport" title="Tải lại dữ liệu"><RefreshCw :size="16" /> <span>Làm mới</span></button>
        <button class="button secondary" type="button" :disabled="!report?.receipts?.length && !report?.warehouseWaste?.length" @click="downloadCsv"><Download :size="16" /> <span>Xuất CSV</span></button>
      </div>
    </header>

    <div class="view-tabs" role="tablist" aria-label="Nơi phát sinh nguyên liệu hủy">
      <button type="button" role="tab" :aria-selected="tab === 'kitchen'" :class="{ active: tab === 'kitchen' }" @click="tab = 'kitchen'">Sau xuất bếp</button>
      <button type="button" role="tab" :aria-selected="tab === 'warehouse'" :class="{ active: tab === 'warehouse' }" @click="tab = 'warehouse'">Hủy tại kho</button>
    </div>

    <div v-if="loadError" class="message error" role="alert">{{ loadError }}</div>
    <div v-if="loading" class="empty">Đang tải dữ liệu đối soát...</div>

    <template v-else-if="tab === 'kitchen' && report">
      <div class="metrics" aria-label="Tổng quan ca">
        <div><span>Phiếu xuất bếp</span><strong>{{ report.summary.receiptCount }}</strong></div>
        <div><span>Dòng đã đối soát</span><strong>{{ report.summary.closedCount }} / {{ report.summary.lineCount }}</strong></div>
        <div><span>Còn chờ đối soát</span><strong>{{ report.summary.pendingCount }}</strong></div>
        <div><span>Hủy bếp trong ngày</span><strong>{{ money(report.summary.kitchenWasteValue) }}</strong></div>
      </div>
      <div v-if="report.usageErrors?.length" class="message error" role="alert">{{ report.usageErrors.join(' ') }}</div>

      <div v-if="!report.receipts.length" class="empty">Ngày này chưa có phiếu xuất bếp đã duyệt.</div>
      <div v-else class="workspace">
        <aside class="receipt-list" aria-label="Phiếu xuất bếp">
          <div class="section-title">Phiếu xuất đã duyệt</div>
          <button v-for="receipt in report.receipts" :key="receipt.receiptId" type="button" class="receipt-option"
            :class="{ active: activeReceipt?.receiptId === receipt.receiptId }" @click="selectReceipt(receipt)">
            <span class="receipt-option-top"><strong>{{ receipt.receiptCode }}</strong><span :class="receipt.lines.every(line => line.reconciledAt) ? 'done' : 'pending'">{{ receipt.lines.every(line => line.reconciledAt) ? 'Đã chốt' : 'Chưa chốt' }}</span></span>
            <small>{{ dateTime(receipt.approvedAt) }} · {{ receipt.lines.length }} dòng nguyên liệu</small>
          </button>
        </aside>

        <section v-if="activeReceipt" class="receipt-detail">
          <div class="detail-head">
            <div><div class="eyebrow">PHIẾU XUẤT BẾP</div><h2>{{ activeReceipt.receiptCode }}</h2><small>Duyệt {{ dateTime(activeReceipt.approvedAt) }}</small></div>
            <span :class="activeReceipt.lines.every(line => line.reconciledAt) ? 'status done' : 'status pending'">{{ activeReceipt.lines.every(line => line.reconciledAt) ? 'Đã đối soát' : 'Cần đối soát' }}</span>
          </div>

          <div v-if="saveError" class="message error" role="alert">{{ saveError }}</div>
          <div v-if="activeReservedPortions > 0" class="message warning" role="status">
            Còn {{ quantity(activeReservedPortions) }} suất đang giữ chỗ. Chưa thể hoàn / hủy nguyên liệu của phiếu này.
          </div>
          <div v-if="successMessage" class="message success" role="status">{{ successMessage }}</div>

          <div class="line-head" aria-hidden="true"><span>Nguyên liệu / lô</span><span>Đã xuất</span><span>Đã dùng theo phục vụ</span><span>Hoàn (dùng ngày mai)</span><span>Hủy tại bếp</span></div>
          <div v-for="line in activeReceipt.lines" :key="line.sourceDetailId" class="ingredient-line">
            <div class="ingredient-name"><strong>{{ line.itemName }}</strong><small>Lô {{ line.batchCode || 'Chưa phân lô' }}<template v-if="line.reconciledAt"> · Chốt {{ dateTime(line.reconciledAt) }}</template></small><small v-if="!line.reconciledAt && line.usageBreakdown?.length">{{ usageLabel(line) }}</small></div>
            <div class="quantity-cell"><span class="mobile-label">Đã xuất</span><strong>{{ quantity(line.issuedQuantity) }} {{ line.baseUnit }}</strong></div>
            <div class="quantity-cell"><span class="mobile-label">Đã dùng theo phục vụ</span><strong>{{ quantity(line.usedQuantity) }} {{ line.baseUnit }}</strong></div>
            <div class="quantity-cell"><span class="mobile-label">Hoàn kho</span><template v-if="line.reconciledAt"><strong>{{ quantity(line.returnedQuantity) }} {{ line.baseUnit }}</strong></template><input v-else v-model.number="draft[line.sourceDetailId].returnedQuantity" type="number" min="0" :max="line.issuedQuantity" step="0.0001" :aria-label="`Số lượng hoàn ${line.itemName}`" /></div>
            <div class="quantity-cell"><span class="mobile-label">Hủy tại bếp</span><template v-if="line.reconciledAt"><strong>{{ quantity(line.wastedQuantity) }} {{ line.baseUnit }}</strong></template><input v-else v-model.number="draft[line.sourceDetailId].wastedQuantity" type="number" min="0" :max="line.issuedQuantity" step="0.0001" :aria-label="`Số lượng hủy ${line.itemName}`" /></div>
            <label v-if="!line.reconciledAt && Number(draft[line.sourceDetailId]?.wastedQuantity) > 0" class="reason-field">Lý do hủy <span class="required">*</span><input v-model.trim="draft[line.sourceDetailId].wasteReason" type="text" maxlength="500" placeholder="Ví dụ: rơi vãi, hư hỏng trong bếp" /></label>
            <small v-if="line.reconciledAt && line.wasteReason" class="reason-readonly">Lý do hủy: {{ line.wasteReason }}</small>
            <small v-if="line.reconciledAt && (line.returnReceiptCode || line.wasteReceiptCode)" class="reason-readonly">{{ [line.returnReceiptCode, line.wasteReceiptCode].filter(Boolean).join(' · ') }}</small>
            <small v-if="!line.reconciledAt" :id="`balance-${line.sourceDetailId}`" class="line-balance" :class="{ 'line-error': lineBalance(line) !== 0 }" aria-live="polite">{{ balanceMessage(line) }}</small>
          </div>

          <div v-if="!activeReceipt.lines.every(line => line.reconciledAt)" class="detail-footer">
            <label>Ghi chú bàn giao <input v-model.trim="note" type="text" maxlength="300" placeholder="Ca sáng, ca tối hoặc người bàn giao" /></label>
            <button type="button" class="button primary" :disabled="saving || hasInvalidLine || report.usageErrors?.length || activeReservedPortions > 0" @click="completeReceipt">{{ saving ? 'Đang chốt...' : 'Chốt hoàn / hủy' }}</button>
          </div>
          <div v-else class="detail-footer closed-note">Đã ghi nhận. Phiếu hoàn và phiếu hủy có thể xem tại <router-link to="/admin/stock-receipts">Phiếu kho</router-link>.</div>
        </section>
      </div>
      <section class="daily-report">
        <div class="section-title">Hoàn và hủy đã chốt trong ngày</div>
        <div v-if="!report.dailyReconciled?.length" class="empty small">Chưa có đối soát được chốt trong ngày này.</div>
        <div v-for="line in report.dailyReconciled || []" :key="line.sourceDetailId" class="daily-row">
          <div><strong>{{ line.itemName }}</strong><small>{{ line.sourceReceiptCode }} · Lô {{ line.batchCode }} · {{ dateTime(line.reconciledAt) }}</small><small>Đã dùng theo phục vụ: {{ quantity(line.usedQuantity) }} {{ line.baseUnit }}</small><small v-if="line.wasteReason">Lý do hủy: {{ line.wasteReason }}</small></div>
          <div class="daily-amounts"><span>Hoàn {{ quantity(line.returnedQuantity) }} {{ line.baseUnit }}</span><span>Hủy {{ quantity(line.wastedQuantity) }} {{ line.baseUnit }}</span><strong v-if="line.wastedQuantity">{{ money(line.wasteValue) }}</strong></div>
        </div>
      </section>
    </template>

    <template v-else-if="tab === 'warehouse'">
      <div class="warehouse-layout">
        <section v-if="selectedDate === today" class="warehouse-form">
          <div class="section-title">Lập phiếu hủy tại kho</div>
          <p class="section-subtitle">Chọn đúng lô quá hạn hoặc hư hỏng. Hệ thống sẽ trừ đúng lô và ghi nhận giá trị hủy.</p>
          <div v-if="warehouseError" class="message error" role="alert">{{ warehouseError }}</div>
          <div v-if="successMessage" class="message success" role="status">{{ successMessage }}</div>
          <div v-for="(row, index) in warehouseRows" :key="row.key" class="warehouse-row">
            <div class="row-title"><strong>Dòng {{ index + 1 }}</strong><button type="button" class="text-button" :aria-label="`Xóa dòng ${index + 1}`" @click="warehouseRows.splice(index, 1)">Xóa</button></div>
            <div class="warehouse-fields">
              <label>Nguyên liệu<select v-model.number="row.inventoryItemId" @change="row.batchId = null"><option :value="null">Chọn nguyên liệu</option><option v-for="item in items" :key="item.inventoryItemId" :value="item.inventoryItemId">{{ item.itemName }} ({{ item.baseUnit }})</option></select></label>
              <label>Lô hàng<select v-model.number="row.batchId" :disabled="!row.inventoryItemId"><option :value="null">Chọn lô</option><option v-for="batch in availableBatches(row.inventoryItemId)" :key="batch.batchId" :value="batch.batchId">{{ batch.batchCode }} · {{ batch.isExpired ? 'quá hạn' : `HSD ${batch.expiryDate?.slice(0, 10)}` }} · còn {{ quantity(batch.quantityRemaining) }} {{ itemUnit(row.inventoryItemId) }}</option></select></label>
              <label>Số lượng <span v-if="row.inventoryItemId">({{ itemUnit(row.inventoryItemId) }})</span><input v-model.number="row.quantity" type="number" min="0" :max="batchRemaining(row.batchId)" step="any" /></label>
              <label>Lý do hủy <span class="required">*</span><input v-model.trim="row.reason" type="text" maxlength="300" placeholder="Hết hạn, hư hỏng..." /></label>
            </div>
          </div>
          <div class="warehouse-actions"><button type="button" class="button secondary" @click="addWarehouseRow">+ Thêm nguyên liệu</button><button type="button" class="button danger" :disabled="warehouseSaving || !warehouseRows.length" @click="submitWarehouseWaste">{{ warehouseSaving ? 'Đang ghi nhận...' : 'Xác nhận hủy tại kho' }}</button></div>
        </section>
        <div v-else class="empty">Chọn ngày hôm nay để lập phiếu hủy kho. Lịch sử bên dưới vẫn xem được theo ngày đã chọn.</div>
        <section class="warehouse-history">
          <div class="section-title">Phiếu hủy kho trong ngày</div>
          <div v-if="!report?.warehouseWaste?.length" class="empty small">Chưa có phiếu hủy kho ngày này.</div>
          <div v-for="receipt in report?.warehouseWaste || []" :key="receipt.receiptId" class="history-row">
            <div class="history-info"><strong>{{ receipt.receiptCode }}</strong><small>{{ dateTime(receipt.approvedAt) }}</small>
              <small v-for="(detail, index) in receipt.details" :key="index">{{ detail.itemName }} · {{ detail.batchCode }} · {{ quantity(detail.quantity) }} {{ detail.baseUnit }}<template v-if="detail.reason"> · {{ detail.reason }}</template></small>
            </div>
            <strong>{{ money(receipt.totalAmount) }}</strong>
          </div>
          <div class="history-total"><span>Tổng giá trị hủy kho</span><strong>{{ money(report?.summary?.warehouseWasteValue) }}</strong></div>
        </section>
      </div>
    </template>
  </main>
</template>

<script setup>
import InventoryWorkspaceNav from '@/components/InventoryWorkspaceNav.vue';
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { Download, RefreshCw } from '@lucide/vue';
import signalRService from '@/services/signalr';
import { useRoute } from 'vue-router';
import axios from 'axios';

const API = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/inventory`;
const route = useRoute();
const today = new Date().toLocaleDateString('en-CA', { timeZone: 'Asia/Ho_Chi_Minh' });
const selectedDate = ref(today);
const report = ref(null);
const tab = ref('kitchen');
const loading = ref(false);
const saving = ref(false);
const warehouseSaving = ref(false);
const loadError = ref('');
const saveError = ref('');
const warehouseError = ref('');
const successMessage = ref('');
const activeReceiptId = ref(null);
const draft = ref({});
const note = ref('');
const items = ref([]);
const batches = ref([]);
const warehouseRows = ref([]);
const activeReceipt = computed(() => report.value?.receipts?.find(r => r.receiptId === activeReceiptId.value));
const activeReservedPortions = computed(() => report.value?.receiptReservations?.find(r => r.receiptId === activeReceiptId.value)?.reservedPortions || 0);
const hasInvalidLine = computed(() => (activeReceipt.value?.lines || []).some(line => {
  const row = draft.value[line.sourceDetailId];
  return !line.reconciledAt && (!row || !isValidAmount(row.returnedQuantity) || !isValidAmount(row.wastedQuantity)
    || lineBalance(line) !== 0
    || (Number(row.wastedQuantity) > 0 && !row.wasteReason?.trim()));
}));

function quantity(value) { return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 4 }); }
function money(value) { return `${Number(value || 0).toLocaleString('vi-VN')} đ`; }
function dateTime(value) { return value ? new Date(value.endsWith?.('Z') ? value : `${value}Z`).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' }) : ''; }
function message(error, fallback) { return error.response?.data?.message || fallback; }
function usageLabel(line) { return line.usageBreakdown.map(part => `${part.foodName} (${part.variantName}): ${quantity(part.soldPortions)} suất × ${quantity(part.quantityPerUnit)} ${line.baseUnit}`).join('; '); }
function isValidAmount(value) {
  const amount = Number(value);
  return value !== null && value !== '' && value !== undefined && Number.isFinite(amount) && amount >= 0
    && Math.abs(amount * 10000 - Math.round(amount * 10000)) < 0.000001;
}
function lineBalance(line) {
  const row = draft.value[line.sourceDetailId];
  return (Math.round(Number(line.issuedQuantity) * 10000) - Math.round(Number(line.usedQuantity || 0) * 10000)
    - Math.round(Number(row?.returnedQuantity || 0) * 10000) - Math.round(Number(row?.wastedQuantity || 0) * 10000)) / 10000;
}
function balanceMessage(line) {
  const row = draft.value[line.sourceDetailId];
  if (!isValidAmount(row?.returnedQuantity) || !isValidAmount(row?.wastedQuantity)) return 'Lượng hoàn và hủy phải là số không âm, tối đa 4 chữ số thập phân.';
  const difference = lineBalance(line);
  if (difference > 0) return `Còn ${quantity(difference)} ${line.baseUnit} chưa phân loại.`;
  if (difference < 0) return `Vượt lượng đã xuất ${quantity(-difference)} ${line.baseUnit}.`;
  return 'Đã cân bằng lượng xuất.';
}

function selectReceipt(receipt) {
  activeReceiptId.value = receipt.receiptId;
  draft.value = Object.fromEntries(receipt.lines.map(line => [line.sourceDetailId, {
    returnedQuantity: line.returnedQuantity || 0, wastedQuantity: line.wastedQuantity || 0, wasteReason: line.wasteReason || ''
  }]));
  note.value = '';
  saveError.value = '';
  successMessage.value = '';
}

async function loadReport({ preserveDraft = false } = {}) {
  if (!preserveDraft) loading.value = true;
  loadError.value = '';
  try {
    const response = await axios.get(`${API}/kitchen-reconciliation`, { params: { date: selectedDate.value } });
    report.value = response.data;
    const current = report.value.receipts.find(r => r.receiptId === activeReceiptId.value);
    const receipt = current || report.value.receipts.find(r => r.lines.some(line => !line.reconciledAt)) || report.value.receipts[0];
    if (!receipt) activeReceiptId.value = null;
    else if (!(preserveDraft && current)) selectReceipt(receipt);
  } catch (error) { report.value = null; activeReceiptId.value = null; loadError.value = message(error, 'Không tải được báo cáo đối soát.'); }
  finally { loading.value = false; }
}

async function loadCatalog() {
  try {
    const [itemsResponse, batchesResponse] = await Promise.all([
      axios.get(`${API}/items`, { params: { isActive: true } }),
      axios.get(`${API}/batches`)
    ]);
    items.value = itemsResponse.data?.data || [];
    batches.value = batchesResponse.data?.data || [];
  } catch (error) { warehouseError.value = message(error, 'Không tải được danh sách nguyên liệu và lô.'); }
}

async function completeReceipt() {
  if (!activeReceipt.value || hasInvalidLine.value || report.value?.usageErrors?.length || activeReservedPortions.value > 0) return;
  if (!confirm(`Chốt đối soát ${activeReceipt.value.receiptCode}? Số lượng hoàn sẽ nhập lại kho ngay.`)) return;
  saving.value = true;
  saveError.value = '';
  try {
    await axios.post(`${API}/kitchen-reconciliation/complete`, {
      sourceReceiptId: activeReceipt.value.receiptId,
      note: note.value,
      lines: activeReceipt.value.lines.map(line => ({ sourceDetailId: line.sourceDetailId, ...draft.value[line.sourceDetailId] }))
    });
    await loadReport();
    successMessage.value = 'Đã chốt đối soát. Phiếu hoàn và phiếu hủy đã được ghi nhận.';
  } catch (error) { saveError.value = message(error, 'Không thể chốt đối soát.'); }
  finally { saving.value = false; }
}

function newWarehouseRow() { return { key: `${Date.now()}-${Math.random()}`, inventoryItemId: null, batchId: null, quantity: 0, reason: '' }; }
function addWarehouseRow() { warehouseRows.value.push(newWarehouseRow()); }
function itemUnit(itemId) { return items.value.find(item => item.inventoryItemId === itemId)?.baseUnit || ''; }
function availableBatches(itemId) {
  return batches.value
    .filter(batch => batch.inventoryItemId === itemId && batch.quantityRemaining > 0 && ['active', 'expired'].includes(batch.status))
    .sort((a, b) => Number(b.isExpired) - Number(a.isExpired) || new Date(a.expiryDate) - new Date(b.expiryDate));
}
function batchRemaining(batchId) { return Number(batches.value.find(batch => batch.batchId === batchId)?.quantityRemaining || 0); }

function selectBatchFromQuery() {
  if (route.query.tab !== 'warehouse' || !route.query.batchId) return;
  const batch = batches.value.find(item => item.batchId === Number(route.query.batchId));
  if (!batch || batch.quantityRemaining <= 0) return;
  tab.value = 'warehouse';
  warehouseRows.value = [{
    ...newWarehouseRow(),
    inventoryItemId: batch.inventoryItemId,
    batchId: batch.batchId,
    quantity: batch.quantityRemaining,
    reason: batch.isExpired ? 'Hết hạn' : ''
  }];
}

async function submitWarehouseWaste() {
  warehouseError.value = '';
  if (selectedDate.value !== today) {
    warehouseError.value = 'Chỉ có thể lập phiếu hủy kho cho ngày hôm nay.';
    return;
  }
  const rows = warehouseRows.value;
  if (rows.some(row => !row.inventoryItemId || !row.batchId || !row.reason?.trim() || row.quantity <= 0 || row.quantity > batchRemaining(row.batchId))) {
    warehouseError.value = 'Chọn nguyên liệu, lô, số lượng hợp lệ và nhập lý do cho từng dòng.';
    return;
  }
  const totals = new Map();
  for (const row of rows) totals.set(row.batchId, (totals.get(row.batchId) || 0) + Number(row.quantity));
  if ([...totals].some(([batchId, total]) => total > batchRemaining(batchId))) {
    warehouseError.value = 'Tổng số lượng hủy của một lô không được vượt tồn lô.';
    return;
  }
  if (!confirm('Xác nhận hủy các nguyên liệu đã chọn tại kho? Tồn kho sẽ giảm ngay.')) return;
  warehouseSaving.value = true;
  try {
    await axios.post(`${API}/receipts/quick-waste`, { note: `Hủy tại kho ${selectedDate.value}`, items: rows.map(row => ({
      inventoryItemId: row.inventoryItemId, batchId: row.batchId, quantity: Number(row.quantity), reason: row.reason.trim()
    })) });
    warehouseRows.value = [newWarehouseRow()];
    await Promise.all([loadReport(), loadCatalog()]);
    successMessage.value = 'Đã ghi phiếu hủy kho và cập nhật tồn.';
  } catch (error) { warehouseError.value = message(error, 'Không thể lập phiếu hủy kho.'); }
  finally { warehouseSaving.value = false; }
}

function downloadCsv() {
  const columns = ['Ngày', 'Nguồn', 'Mã phiếu', 'Nguyên liệu', 'Lô', 'Đơn vị', 'Đã xuất', 'Đã dùng', 'Hoàn kho', 'Số lượng hủy', 'Lý do hủy', 'Giá trị hủy', 'Trạng thái'];
  const kitchenRows = (report.value.dailyReconciled || []).map(line => [
    selectedDate.value, 'Sau xuất bếp', line.sourceReceiptCode, line.itemName, line.batchCode, line.baseUnit,
    line.issuedQuantity, line.usedQuantity, line.returnedQuantity, line.wastedQuantity,
    line.wasteReason || '', line.wasteValue, 'Đã chốt'
  ]);
  const warehouseRowsCsv = (report.value.warehouseWaste || []).flatMap(receipt => receipt.details.map(detail => [
    selectedDate.value, 'Hủy tại kho', receipt.receiptCode, detail.itemName, detail.batchCode, detail.baseUnit,
    '', '', '', detail.quantity, detail.reason || '', detail.subtotal, 'Đã duyệt'
  ]));
  const csv = [columns, ...kitchenRows, ...warehouseRowsCsv].map(row => row.map(value => `"${String(value ?? '').replaceAll('"', '""')}"`).join(',')).join('\r\n');
  const url = URL.createObjectURL(new Blob(['\ufeff', csv], { type: 'text/csv;charset=utf-8' }));
  const link = document.createElement('a'); link.href = url; link.download = `doi-soat-bep-${selectedDate.value}.csv`; link.click();
  URL.revokeObjectURL(url);
}

const refreshPortions = () => loadReport({ preserveDraft: true });
onMounted(async () => {
  warehouseRows.value = [newWarehouseRow()];
  signalRService.on('PortionAvailabilityChanged', refreshPortions);
  await Promise.all([loadReport(), loadCatalog()]);
  selectBatchFromQuery();
});
onUnmounted(() => signalRService.off('PortionAvailabilityChanged', refreshPortions));
</script>

<style scoped>
.line-balance { grid-column: 1 / -1; color: #08765d; }
.reconciliation-page { max-width: 1500px; margin: 0 auto; padding: 22px; color: #182925; background: #f5f8f7; min-height: 100vh; }
.page-head, .detail-head, .detail-footer, .warehouse-actions { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.page-head { margin-bottom: 18px; }
.page-head h1 { margin: 2px 0; font-size: 27px; font-weight: 700; }
.page-head p, .section-subtitle { margin: 0; color: #61736d; font-size: 14px; }
.eyebrow { color: #08765d; font-size: 11px; font-weight: 800; }
.head-actions { display: flex; align-items: end; gap: 8px; flex-wrap: wrap; }
.date-field, .detail-footer label, .warehouse-fields label, .reason-field { display: grid; gap: 5px; color: #425750; font-size: 13px; font-weight: 600; }
input, select { width: 100%; min-height: 40px; padding: 7px 9px; border: 1px solid #cad7d2; border-radius: 5px; color: #182925; background: #fff; font: inherit; }
input:focus, select:focus, button:focus-visible { outline: 2px solid #10896a; outline-offset: 2px; }
.button { display: inline-flex; align-items: center; justify-content: center; gap: 7px; min-height: 44px; padding: 8px 13px; border: 1px solid transparent; border-radius: 5px; font-weight: 600; white-space: nowrap; }
.button:disabled { opacity: .55; cursor: not-allowed; }
.button.primary { background: #08765d; color: #fff; }.button.secondary { background: #fff; color: #25433a; border-color: #cbd9d3; }.button.danger { background: #b53636; color: #fff; }
.view-tabs { display: inline-flex; margin-bottom: 18px; padding: 3px; border: 1px solid #d3ded9; border-radius: 6px; background: #eaf0ed; }
.view-tabs button { min-height: 39px; padding: 7px 15px; border: 0; border-radius: 4px; background: transparent; color: #52665d; font-weight: 700; }
.view-tabs button.active { background: #fff; color: #08765d; box-shadow: 0 1px 4px #0001; }
.metrics { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 16px; }
.metrics > div { padding: 13px 15px; border: 1px solid #dbe5df; border-radius: 6px; background: #fff; }.metrics span, .metrics strong { display: block; }.metrics span { color: #61736d; font-size: 12px; }.metrics strong { margin-top: 4px; font-size: 19px; }
.workspace, .warehouse-layout { display: grid; grid-template-columns: 265px minmax(0, 1fr); gap: 14px; align-items: start; }.warehouse-layout { grid-template-columns: minmax(0, 1fr) 330px; }
.receipt-list, .receipt-detail, .warehouse-form, .warehouse-history, .daily-report { border: 1px solid #dbe5df; border-radius: 6px; background: #fff; }
.daily-report { margin-top: 16px; overflow: hidden; }.daily-row { display: flex; justify-content: space-between; gap: 12px; padding: 12px 16px; border-top: 1px solid #e8eeea; }.daily-row small { display: block; margin-top: 4px; color: #698078; }.daily-amounts { display: flex; align-items: center; gap: 16px; flex-wrap: wrap; text-align: right; }.daily-amounts strong { color: #a63832; }
.receipt-list { overflow: hidden; }.section-title { padding: 14px 16px; font-weight: 700; border-bottom: 1px solid #e8eeea; }.receipt-option { width: 100%; padding: 13px 15px; border: 0; border-bottom: 1px solid #e8eeea; background: #fff; text-align: left; color: #20382f; }.receipt-option.active { background: #e9f6ef; border-left: 3px solid #08765d; padding-left: 12px; }.receipt-option-top { display: flex; align-items: center; justify-content: space-between; gap: 7px; }.receipt-option small, .history-row small { display: block; margin-top: 5px; color: #698078; }
.done { color: #08765d; }.pending { color: #a15a09; }.receipt-option-top span { font-size: 11px; white-space: nowrap; }
.detail-head { padding: 17px 18px; border-bottom: 1px solid #e8eeea; }.detail-head h2 { margin: 2px 0; font-size: 20px; }.detail-head small { color: #61736d; }.status { padding: 4px 8px; border-radius: 4px; font-size: 12px; background: #f2f8f4; }.status.pending { background: #fff5e6; }
.equation { margin: 14px 18px; padding: 9px 12px; border-left: 3px solid #08765d; background: #f0f7f3; color: #405f52; font-size: 13px; }
.line-head, .ingredient-line { display: grid; grid-template-columns: minmax(190px, 2fr) repeat(4, minmax(100px, 1fr)); gap: 10px; align-items: start; padding: 10px 18px; }.line-head { color: #677c72; background: #f8faf8; font-size: 11px; font-weight: 700; text-transform: uppercase; }.ingredient-line { border-top: 1px solid #ebf0ec; }.ingredient-name strong, .ingredient-name small { display: block; }.ingredient-name small { color: #6b7e75; font-size: 12px; margin-top: 3px; }.quantity-cell { padding-top: 4px; }.quantity-cell input { margin-top: -4px; }.mobile-label { display: none; }.reason-field, .reason-readonly, .line-error { grid-column: 1 / -1; }.reason-field { max-width: 490px; }.reason-readonly { color: #98502e; }.line-error, .required { color: #b53636; }
.detail-footer { border-top: 1px solid #e8eeea; padding: 15px 18px; }.detail-footer label { flex: 1; max-width: 430px; }.closed-note { justify-content: flex-start; color: #52665d; }.closed-note a { color: #08765d; }
.warehouse-form { padding-bottom: 16px; }.section-subtitle { padding: 9px 16px 5px; }.warehouse-row { margin: 10px 16px; padding: 12px; border: 1px solid #e1e9e4; border-radius: 5px; }.row-title { display: flex; justify-content: space-between; margin-bottom: 10px; }.text-button { border: 0; background: transparent; color: #b53636; font-weight: 700; }.warehouse-fields { display: grid; grid-template-columns: minmax(180px, 1fr) minmax(180px, 1fr) 120px minmax(170px, 1fr); gap: 10px; }.warehouse-actions { padding: 6px 16px; }.warehouse-history { overflow: hidden; }.history-row { display: flex; justify-content: space-between; gap: 12px; padding: 12px 16px; border-bottom: 1px solid #e8eeea; }.history-total { display: flex; justify-content: space-between; gap: 12px; padding: 14px 16px; background: #f2f7f4; }
.empty { padding: 40px 18px; text-align: center; color: #688077; border: 1px dashed #cad7d2; background: #fff; border-radius: 5px; }.empty.small { padding: 25px 12px; border: 0; }.message { margin: 10px 16px; padding: 9px 12px; border-radius: 5px; font-size: 13px; }.message.error { background: #fff0ed; color: #a52e2e; }.message.success { background: #ebf8f0; color: #0a704d; }
.message.warning { background: #fff7df; color: #87520b; border-left: 3px solid #be820e; }
.receipt-detail, .warehouse-form, .warehouse-history, .daily-report { min-width: 0; border-radius: 0; border-left: 0; border-right: 0; }
.metrics { gap: 0; padding: 14px 0; border-block: 1px solid #dbe5df; }
.metrics > div { background: transparent; border: 0; border-radius: 0; border-right: 1px solid #dbe5df; }
.metrics > div:last-child { border-right: 0; }
.receipt-option-top, .detail-head { flex-wrap: wrap; }
.receipt-option-top strong, .detail-head h2 { overflow-wrap: anywhere; }
@media (max-width: 1050px) { .workspace, .warehouse-layout { grid-template-columns: 1fr; }.receipt-list { display: flex; overflow-x: auto; }.receipt-list .section-title { min-width: 130px; }.receipt-option { min-width: 220px; }.warehouse-fields { grid-template-columns: 1fr 1fr; } }
@media (max-width: 690px) { .reconciliation-page { padding: 12px; }.page-head { align-items: stretch; flex-direction: column; }.head-actions { align-items: end; }.date-field { flex: 1; }.metrics { grid-template-columns: 1fr 1fr; }.line-head { display: none; }.ingredient-line { grid-template-columns: 1fr 1fr; }.ingredient-name { grid-column: 1 / -1; }.mobile-label { display: block; color: #688077; font-size: 11px; }.quantity-cell { min-width: 0; }.detail-footer, .warehouse-actions, .daily-row { align-items: stretch; flex-direction: column; }.daily-amounts { justify-content: flex-start; text-align: left; }.detail-footer label { max-width: none; }.warehouse-fields { grid-template-columns: 1fr; } }
</style>

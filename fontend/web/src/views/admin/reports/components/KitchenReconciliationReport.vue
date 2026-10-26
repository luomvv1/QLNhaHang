<template>
  <section class="report-module" aria-labelledby="kitchen-reconciliation-title">
    <header class="module-heading">
      <div><p class="module-eyebrow">BẾP & HAO HỤT ĐẦU VÀO</p><h2 id="kitchen-reconciliation-title">Theo dõi nguyên liệu bếp</h2><p>Phân biệt lượng đã dùng, hoàn kho và hủy. Hao hụt sơ chế được theo dõi riêng từ lúc nhập lô.</p></div>
    </header>

    <div v-if="error" class="module-error" role="alert"><span>{{ error }}</span><button type="button" @click="load">Thử lại</button></div>
    <div v-else-if="loading" class="module-loading" role="status" aria-live="polite">Đang tải đối soát bếp…</div>
    <template v-else-if="data">
      <div v-if="data.summary.expiredBatchCount || data.summary.expiringBatchCount || data.summary.returnUseNextDayCount" class="module-warning" role="status">
        Tình trạng kho hôm nay: {{ number(data.summary.expiredBatchCount) }} lô quá hạn còn tồn, {{ number(data.summary.expiringBatchCount) }} lô sắp hết hạn, {{ number(data.summary.returnUseNextDayCount) }} lô hoàn nhập hôm nay cần ưu tiên dùng ngày mai. Danh sách hiển thị tối đa 100 lô.
      </div>

      <div class="kpi-strip" aria-label="Tổng quan đối soát bếp">
        <div><span>Dòng xuất đã chốt trong kỳ</span><strong>{{ number(data.summary.kitchenClosedLineCount) }}</strong></div>
        <div><span>Dòng còn chờ chốt đến cuối kỳ</span><strong :class="data.summary.pendingLineCount ? 'warning-text' : 'positive-text'">{{ number(data.summary.pendingLineCount) }}</strong></div>
        <div><span>Tiền nguyên liệu đã dùng</span><strong>{{ money(data.summary.usedValue) }}</strong></div>
        <div><span>Tiền hàng hủy</span><strong class="negative-text">{{ money(data.summary.wastedValue) }}</strong><small>Hoàn kho: {{ money(data.summary.returnedValue) }}</small></div>
      </div>

      <div class="mode-switch" role="tablist" aria-label="Chọn dữ liệu bếp">
        <button type="button" role="tab" :aria-selected="mode === 'kitchen'" :class="{ active: mode === 'kitchen' }" @click="mode = 'kitchen'">Đối soát bếp <span>{{ number(data.summary.kitchenClosedLineCount + data.summary.pendingLineCount) }}</span></button>
        <button type="button" role="tab" :aria-selected="mode === 'prep'" :class="{ active: mode === 'prep' }" @click="mode = 'prep'">Sơ chế lúc nhập <span>{{ number(data.summary.prepItemCount) }}</span></button>
      </div>

      <template v-if="mode === 'prep'">
        <div class="section-heading"><div><h3>Hao hụt theo nguyên liệu</h3><p>So sánh lượng mua, hao hụt cho phép, hao hụt thực tế và lượng sạch nhập kho.</p></div><span v-if="data.summary.prepExcessItemCount" class="status-chip status-warn">{{ number(data.summary.prepExcessItemCount) }} nguyên liệu vượt dự kiến</span></div>
        <div v-if="data.summary.prepExcessByUnit?.length" class="unit-summary" aria-label="Tổng hao hụt vượt theo đơn vị">
          <span v-for="unit in data.summary.prepExcessByUnit" :key="unit.baseUnit">Vượt dự kiến: <strong>{{ quantity(unit.excessQuantity) }} {{ unit.baseUnit }}</strong></span>
        </div>
        <div v-if="!data.prepByItem.length" class="module-empty">Chưa có phiếu nhập nhà cung cấp được duyệt trong kỳ.</div>
        <div v-else class="table-scroll"><table class="report-table"><thead><tr><th>Nguyên liệu</th><th class="number-cell">Mua vào</th><th class="number-cell">Hao hụt dự kiến</th><th class="number-cell">Hao hụt thực tế</th><th class="number-cell">Sạch nhập kho</th><th class="number-cell">Vượt dự kiến</th><th>Ghi chú</th></tr></thead><tbody>
          <tr v-for="item in filteredPrepRows" :key="item.inventoryItemId"><td><strong>{{ item.itemName }}</strong><small>{{ item.baseUnit }}</small></td><td class="number-cell">{{ quantity(item.grossQuantity) }}</td><td class="number-cell">{{ quantity(item.expectedWastageQuantity) }}</td><td class="number-cell">{{ quantity(item.actualWastageQuantity) }}</td><td class="number-cell">{{ quantity(item.actualUsableQuantity) }}</td><td class="number-cell" :class="item.excessQuantity > 0 ? 'warning-text' : 'positive-text'">{{ quantity(item.excessQuantity) }}</td><td>{{ item.reasons?.join('; ') || '—' }}</td></tr>
        </tbody></table></div>
      </template>

      <template v-else>
        <div class="module-toolbar">
          <label class="toolbar-select"><span>Hiển thị</span><select v-model="status"><option value="">Đã chốt và chờ chốt</option><option value="closed">Đã chốt trong kỳ</option><option value="pending">Còn chờ chốt đến cuối kỳ</option></select></label>
          <span class="toolbar-note">Đã chốt tính theo ngày đối soát; phiếu chờ tính tại {{ formatDate(data.toDate) }}.</span>
        </div>
        <div v-if="!filteredKitchenRows.length" class="module-empty">Không có dòng đã chốt phù hợp trong kỳ.</div>
        <div v-else class="table-scroll"><table class="report-table"><thead><tr><th>Phiếu xuất / nguyên liệu</th><th class="number-cell">Đã xuất</th><th class="number-cell">Đã dùng</th><th class="number-cell">Hoàn kho</th><th class="number-cell">Hủy</th><th>Ngày đối soát</th><th>Lý do hủy</th></tr></thead><tbody>
          <tr v-for="row in filteredKitchenRows" :key="row.detailId"><td><strong>{{ row.itemName }}</strong><small>{{ row.receiptCode }} · {{ row.batchCode || 'Không rõ lô' }} · {{ quantity(row.issuedPortions) }} suất theo kế hoạch</small><small v-if="row.batchExpiryDate">Hạn lô: {{ formatDate(row.batchExpiryDate) }}</small></td><td class="number-cell">{{ quantity(row.sourceQuantity) }} {{ row.baseUnit }}</td><td class="number-cell">{{ quantity(row.usedQuantity) }} {{ row.baseUnit }}</td><td class="number-cell positive-text">{{ quantity(row.returnedQuantity) }} {{ row.baseUnit }}</td><td class="number-cell negative-text">{{ quantity(row.wastedQuantity) }} {{ row.baseUnit }}</td><td>{{ formatDateTime(row.reconciledAt) }}</td><td>{{ row.wasteReason || '—' }}</td></tr>
        </tbody></table></div>

        <section v-if="status !== 'closed' && data.pending.length" class="pending-section" aria-labelledby="pending-title">
          <div class="section-heading"><div><h3 id="pending-title">Chưa chốt tại cuối kỳ</h3><p>Giá trị đang theo dõi riêng, chưa tính là nguyên liệu đã dùng.</p></div><strong>{{ number(data.pending.length) }} dòng · {{ money(data.summary.pendingValue) }}</strong></div>
          <div class="table-scroll"><table class="report-table"><thead><tr><th>Phiếu xuất</th><th>Nguyên liệu / lô</th><th class="number-cell">Lượng xuất</th><th>Ngày xuất</th></tr></thead><tbody>
            <tr v-for="row in data.pending" :key="`pending-${row.detailId}`"><td>{{ row.receiptCode }}<small>{{ quantity(row.issuedPortions) }} suất theo kế hoạch</small></td><td>{{ row.itemName }}<small>{{ row.batchCode || 'Không rõ lô' }}</small></td><td class="number-cell">{{ quantity(row.issuedQuantity) }} {{ row.baseUnit }}</td><td>{{ formatDateTime(row.approvedAt) }}</td></tr>
          </tbody></table></div>
        </section>
      </template>
    </template>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5079'
const props = defineProps({ fromDate: { type: String, required: true }, toDate: { type: String, required: true } })
const loading = ref(false)
const error = ref('')
const data = ref(null)
const mode = ref('kitchen')
const status = ref('')
let requestVersion = 0
let controller

const filteredKitchenRows = computed(() => {
  if (status.value === 'pending') return []
  return data.value?.kitchenRows || []
})
const filteredPrepRows = computed(() => data.value?.prepByItem || [])

async function load() {
  const version = ++requestVersion
  controller?.abort()
  controller = new AbortController()
  loading.value = true
  error.value = ''
  try {
    const response = await axios.get(`${API_BASE}/api/admin/reports/kitchen-reconciliation-summary`, {
      params: { fromDate: props.fromDate, toDate: props.toDate }, signal: controller.signal, timeout: 5000
    })
    if (version !== requestVersion) return
    data.value = response.data?.data || null
    if (!data.value) throw new Error('Dữ liệu đối soát không hợp lệ.')
  } catch (requestError) {
    if (version !== requestVersion || requestError.code === 'ERR_CANCELED') return
    error.value = requestError.response?.data?.message || requestError.message || 'Chưa tải được đối soát bếp.'
  } finally {
    if (version === requestVersion) loading.value = false
  }
}
watch(() => [props.fromDate, props.toDate], load, { immediate: true })
function number(value) { return Number(value || 0).toLocaleString('vi-VN') }
function quantity(value) { return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 3 }) }
function money(value) { return value == null ? 'Chưa tính' : `${Number(value).toLocaleString('vi-VN', { maximumFractionDigits: 0 })} ₫` }
function formatDate(value) { return value ? new Date(`${String(value).slice(0, 10)}T12:00:00`).toLocaleDateString('vi-VN') : '—' }
function formatDateTime(value) { return value ? new Date(value).toLocaleString('vi-VN') : '—' }
</script>

<style scoped>
.report-module{display:flex;flex-direction:column;gap:16px;color:#172033}.module-heading{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.module-eyebrow{margin:0 0 5px;color:#0f766e;font-size:.72rem;font-weight:750}.module-heading h2{margin:0;font-size:1.15rem}.module-heading p:not(.module-eyebrow){margin:5px 0 0;color:#64748b;font-size:.84rem;line-height:1.45}.module-action,.module-error button{min-height:40px;display:inline-flex;align-items:center;gap:8px;padding:0 12px;border:1px solid #cbd5e1;border-radius:7px;background:#fff;color:#164e63;font:inherit;font-weight:650;cursor:pointer}.module-action:disabled{opacity:.6;cursor:wait}.module-warning{padding:11px 13px;color:#854d0e;background:#fffbeb;border:1px solid #fcd34d;border-radius:7px;font-size:.82rem;line-height:1.45}.kpi-strip{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));border-top:1px solid #e2e8f0;border-bottom:1px solid #e2e8f0}.kpi-strip div{min-width:0;padding:13px 15px;border-right:1px solid #e2e8f0}.kpi-strip div:first-child{padding-left:0}.kpi-strip div:last-child{border:0}.kpi-strip span,.kpi-strip small{display:block;color:#64748b;font-size:.75rem;line-height:1.4}.kpi-strip strong{display:block;margin-top:5px;font-size:1.03rem}.kpi-strip small{margin-top:3px}.warning-text{color:#b45309}.positive-text{color:#047857}.negative-text{color:#b91c1c}.mode-switch{display:flex;gap:8px;border-bottom:1px solid #e2e8f0}.mode-switch button{min-height:42px;padding:0 12px;border:0;border-bottom:3px solid transparent;background:transparent;color:#64748b;font:inherit;font-weight:700;cursor:pointer;white-space:nowrap}.mode-switch button.active{color:#0f766e;border-bottom-color:#0f766e}.mode-switch span{display:inline-flex;min-width:22px;justify-content:center;margin-left:4px;padding:2px 5px;border-radius:999px;background:#ecfdf5;color:#047857;font-size:.72rem}.section-heading{display:flex;justify-content:space-between;align-items:center;gap:12px}.section-heading h3{margin:0;font-size:.97rem}.section-heading p{margin:4px 0 0;color:#64748b;font-size:.78rem}.section-heading>strong{color:#334155;font-size:.8rem;white-space:nowrap}.unit-summary{display:flex;flex-wrap:wrap;gap:8px 20px;padding:10px 0;border-block:1px solid #e2e8f0;color:#64748b;font-size:.78rem}.unit-summary strong{color:#92400e}.module-toolbar{display:flex;justify-content:space-between;align-items:center;gap:12px}.toolbar-select{display:flex;align-items:center;gap:8px;color:#64748b;font-size:.78rem;font-weight:600}.toolbar-select select{min-height:40px;max-width:100%;padding:0 30px 0 9px;border:1px solid #cbd5e1;border-radius:7px;background:#fff;color:#172033;font:inherit}.toolbar-note{color:#64748b;font-size:.78rem}.table-scroll{overflow:auto;border:1px solid #e2e8f0;border-radius:7px}.report-table{width:100%;min-width:880px;border-collapse:collapse;font-size:.8rem}.report-table th,.report-table td{padding:10px;border-bottom:1px solid #edf0f5;text-align:left;vertical-align:middle}.report-table th{background:#f8fafc;color:#64748b;font-size:.7rem;text-transform:uppercase;white-space:nowrap}.report-table tbody tr:last-child td{border:0}.report-table small{display:block;margin-top:3px;color:#94a3b8;font-size:.72rem}.number-cell{text-align:right!important;white-space:nowrap}.status-chip{display:inline-flex;align-items:center;min-height:25px;padding:3px 7px;border-radius:6px;font-size:.7rem;font-weight:700;white-space:nowrap}.status-warn{color:#92400e;background:#fef3c7}.pending-section{display:flex;flex-direction:column;gap:10px;padding-top:14px;border-top:1px solid #e2e8f0}.pending-section .table-scroll{background:#fff}.module-error{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:11px 13px;color:#991b1b;background:#fef2f2;border:1px solid #fecaca;border-radius:7px;font-size:.83rem}.module-loading,.module-empty{display:grid;place-items:center;min-height:130px;padding:20px;color:#64748b;background:#f8fafc;border:1px dashed #cbd5e1;border-radius:7px;text-align:center}
@media(max-width:820px){.kpi-strip{grid-template-columns:repeat(2,minmax(0,1fr))}.kpi-strip div:nth-child(2){border-right:0}.kpi-strip div{border-bottom:1px solid #e2e8f0}.kpi-strip div:nth-child(n+3){border-bottom:0}.module-toolbar{align-items:flex-start;flex-direction:column}}
@media(max-width:560px){.module-heading{flex-direction:column}.module-action{align-self:flex-end}.section-heading{align-items:flex-start;flex-direction:column}.mode-switch{overflow:auto}.mode-switch button{padding:0 7px;font-size:.82rem}}
</style>

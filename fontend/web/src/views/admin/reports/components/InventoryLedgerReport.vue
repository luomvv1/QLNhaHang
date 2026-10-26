<template>
  <section class="inventory-module" aria-labelledby="inventory-ledger-title">
    <header class="module-heading">
      <div>
        <p class="module-eyebrow">BÁO CÁO KHO</p>
        <h2 id="inventory-ledger-title">Nhập, xuất và tồn nguyên liệu</h2>
        <p>Tồn đầu và cuối kỳ lấy từ sổ biến động. Tồn dùng được tính theo lô còn hạn hôm nay để cảnh báo nhập hàng.</p>
      </div>
      <button v-if="data" class="module-action" type="button" @click="exportItemsCsv">Xuất CSV tổng hợp</button>
    </header>

    <div v-if="error" class="module-error" role="alert">
      <span>{{ error }}</span><button type="button" @click="load">Thử lại</button>
    </div>
    <div v-else-if="loading" class="module-loading" role="status" aria-live="polite">Đang tải sổ kho…</div>
    <template v-else-if="data">
      <div v-if="data.summary.mismatchCount" class="module-warning" role="status">
        <strong>{{ number(data.summary.mismatchCount) }} nguyên liệu đang lệch sổ.</strong>
        <span>Đối chiếu tồn tính từ toàn bộ biến động với tồn hiện tại để kiểm tra phiếu hoặc kiểm kê.</span>
        <ul>
          <li v-for="item in data.warnings.filter(row => row.type === 'ledger_mismatch')" :key="item.inventoryItemId">
            {{ item.itemName }}: sổ {{ quantity(item.currentLedgerQuantity) }} {{ item.baseUnit }}, hiện có {{ quantity(item.currentQuantity) }} {{ item.baseUnit }}
          </li>
        </ul>
      </div>

      <div class="kpi-strip" aria-label="Tổng quan sổ kho">
        <div><span>Nguyên liệu có số liệu</span><strong>{{ number(data.summary.itemCount) }}</strong></div>
        <div><span>Biến động trong kỳ</span><strong>{{ number(data.summary.movementCount) }}</strong></div>
        <div><span>Thiếu tồn dùng được</span><strong :class="data.summary.belowMinimumCount ? 'warning-text' : ''">{{ number(data.summary.belowMinimumCount) }}</strong></div>
        <div><span>Lệch giữa sổ và tồn hiện tại</span><strong :class="data.summary.mismatchCount ? 'warning-text' : 'positive-text'">{{ number(data.summary.mismatchCount) }}</strong></div>
      </div>

      <section class="stock-trend" aria-labelledby="stock-trend-title">
        <div class="section-heading">
          <div><h3 id="stock-trend-title">Giá trị luân chuyển kho</h3><p>{{ trendMode === 'day' ? 'Theo ngày' : 'Theo tháng' }} trong kỳ · giá vốn ghi trên biến động, không phải doanh thu.</p></div>
          <div class="trend-legend"><span><i class="trend-in"></i> Nhập mua</span><span><i class="trend-out"></i> Xuất bếp</span><span><i class="trend-waste"></i> Hủy</span></div>
        </div>
        <div v-if="trendRows.length" class="trend-scroll">
          <div class="trend-chart" role="img" :aria-label="`Giá trị nhập mua, xuất bếp và hủy kho theo ${trendMode === 'day' ? 'ngày' : 'tháng'}`">
            <div v-for="row in trendRows" :key="row.key" class="trend-column">
              <div class="trend-bars">
                <span class="trend-bar trend-in" :style="{ height: trendHeight(row.importValue) }" :title="`${row.label}: Nhập mua ${currency(row.importValue)}`"></span>
                <span class="trend-bar trend-out" :style="{ height: trendHeight(row.exportValue) }" :title="`${row.label}: Xuất bếp ${currency(row.exportValue)}`"></span>
                <span class="trend-bar trend-waste" :style="{ height: trendHeight(row.wasteValue) }" :title="`${row.label}: Hủy ${currency(row.wasteValue)}`"></span>
              </div>
              <small>{{ row.label }}</small>
            </div>
          </div>
        </div>
        <p v-else class="toolbar-note">Chưa có biến động trong kỳ đã chọn.</p>
        <p v-if="trendMissingCostCount" class="trend-warning">{{ number(trendMissingCostCount) }} biến động chưa có giá vốn; giá trị trên biểu đồ chưa bao gồm các dòng đó.</p>
        <details v-if="trendRows.length" class="trend-details"><summary>Xem số liệu biểu đồ</summary><div class="table-scroll"><table class="report-table"><thead><tr><th>Kỳ</th><th class="number-cell">Nhập mua</th><th class="number-cell">Xuất bếp</th><th class="number-cell">Hủy</th><th class="number-cell">Hoàn bếp</th></tr></thead><tbody><tr v-for="row in trendRows" :key="row.key"><td>{{ row.label }}</td><td class="number-cell">{{ currency(row.importValue) }}</td><td class="number-cell">{{ currency(row.exportValue) }}</td><td class="number-cell">{{ currency(row.wasteValue) }}</td><td class="number-cell">{{ currency(row.returnValue) }}</td></tr></tbody></table></div></details>
      </section>

      <section class="unit-summary" aria-labelledby="unit-summary-title">
        <div class="section-heading">
          <div><h3 id="unit-summary-title">Tổng hợp theo đơn vị</h3><p>Không cộng chéo kg, g, ml, lít và số con.</p></div>
        </div>
        <div class="table-scroll">
          <table class="report-table unit-table">
            <thead><tr><th>Đơn vị</th><th class="number-cell">Mặt hàng</th><th class="number-cell">Tồn đầu</th><th class="number-cell">Tăng tồn</th><th class="number-cell">Giảm tồn</th><th class="number-cell">Tồn cuối kỳ</th></tr></thead>
            <tbody><tr v-for="unit in data.summary.quantitiesByUnit" :key="unit.baseUnit">
              <td data-label="Đơn vị"><strong>{{ unit.baseUnit }}</strong></td><td class="number-cell" data-label="Mặt hàng">{{ number(unit.itemCount) }}</td>
              <td class="number-cell" data-label="Tồn đầu kỳ">{{ quantity(unit.openingQuantity) }}</td>
              <td class="number-cell positive-text" data-label="Tăng tồn">{{ quantity(totalIn(unit)) }}<small>Mua {{ quantity(unit.importedQuantity) }} · Hoàn {{ quantity(unit.returnedQuantity) }} · ĐC +{{ quantity(unit.adjustmentIncrease) }}</small></td>
              <td class="number-cell negative-text" data-label="Giảm tồn">{{ quantity(totalOut(unit)) }}<small>Bếp {{ quantity(unit.exportedQuantity) }} · Hủy {{ quantity(unit.wastedQuantity) }} · ĐC −{{ quantity(unit.adjustmentDecrease) }}</small></td>
              <td class="number-cell" data-label="Tồn cuối kỳ"><strong>{{ quantity(unit.closingQuantity) }}</strong></td>
            </tr></tbody>
          </table>
        </div>
      </section>

      <section class="item-section" aria-labelledby="item-ledger-title">
        <div class="section-heading section-heading-inline">
          <div><h3 id="item-ledger-title">Chi tiết theo nguyên liệu</h3><p>{{ number(filteredItems.length) }} dòng trong kỳ {{ formatDate(data.fromDate) }} – {{ formatDate(data.toDate) }}</p></div>
          <label class="toolbar-select"><span class="sr-only">Lọc nguyên liệu theo trạng thái</span><select v-model="itemFilter">
            <option value="all">Tất cả nguyên liệu</option><option value="low">Dưới tồn tối thiểu</option><option value="mismatch">Lệch sổ</option><option value="moved">Có biến động trong kỳ</option>
          </select></label>
        </div>
        <div class="module-toolbar">
          <label class="search-field"><Search :size="17" aria-hidden="true" /><span class="sr-only">Tìm nguyên liệu</span><input v-model.trim="search" type="search" placeholder="Tìm nguyên liệu hoặc đơn vị…" /></label>
          <label class="toolbar-select"><span class="sr-only">Sắp xếp nguyên liệu</span><select v-model="itemSort"><option value="name">Tên A–Z</option><option value="low">Cần nhập trước</option><option value="movements">Biến động nhiều nhất</option></select></label>
          <span class="toolbar-note">Tồn dùng được là số tại thời điểm xem, không phải tồn cuối kỳ.</span>
        </div>
        <div v-if="!filteredItems.length" class="module-empty">Không có nguyên liệu phù hợp với bộ lọc này.</div>
        <div v-else class="table-scroll">
          <table class="report-table item-table">
            <thead><tr><th>Nguyên liệu</th><th class="number-cell">Tồn đầu kỳ</th><th class="number-cell">Tăng tồn</th><th class="number-cell">Giảm tồn</th><th class="number-cell">Tồn cuối kỳ</th><th class="number-cell">Tồn dùng được</th><th>Đối chiếu</th></tr></thead>
            <tbody><tr v-for="item in filteredItems" :key="item.inventoryItemId">
              <td data-label="Nguyên liệu"><strong>{{ item.itemName }}</strong><small>{{ item.baseUnit }} · {{ number(item.movementCount) }} biến động</small></td>
              <td class="number-cell" data-label="Tồn đầu kỳ">{{ quantity(item.openingQuantity) }} {{ item.baseUnit }}</td>
              <td class="number-cell positive-text" data-label="Tăng tồn">{{ quantity(totalIn(item)) }} {{ item.baseUnit }}<small>Mua {{ quantity(item.importQuantity) }} · Hoàn {{ quantity(item.returnQuantity) }} · ĐC +{{ quantity(item.adjustmentIncrease) }}</small></td>
              <td class="number-cell negative-text" data-label="Giảm tồn">{{ quantity(totalOut(item)) }} {{ item.baseUnit }}<small>Bếp {{ quantity(item.exportQuantity) }} · Hủy {{ quantity(item.wasteQuantity) }} · ĐC −{{ quantity(item.adjustmentDecrease) }}</small></td>
              <td class="number-cell" data-label="Tồn cuối kỳ"><strong>{{ quantity(item.closingQuantity) }} {{ item.baseUnit }}</strong></td>
              <td class="number-cell" data-label="Tồn dùng được"><strong>{{ quantity(item.availableQuantity ?? item.currentQuantity) }} {{ item.baseUnit }}</strong><small>Tồn sổ {{ quantity(item.currentQuantity) }} · Tối thiểu {{ quantity(item.minimumQuantity) }}</small></td>
              <td data-label="Đối chiếu"><span class="status-chip" :class="item.currentMismatch ? 'status-warn' : item.isBelowMinimum ? 'status-low' : 'status-ok'">{{ item.currentMismatch ? 'Lệch sổ' : item.isBelowMinimum ? 'Dưới mức' : 'Khớp' }}</span></td>
            </tr></tbody>
          </table>
        </div>
      </section>

      <details class="ledger-details">
        <summary>Biến động chi tiết · {{ number(data.summary.movementCount) }} dòng trong kỳ</summary>
        <div class="module-toolbar detail-toolbar">
          <label class="toolbar-select"><span>Loại biến động</span><select v-model="movementFilter"><option value="all">Tất cả</option><option value="import">Nhập mua</option><option value="return">Hoàn từ bếp</option><option value="export">Xuất bếp</option><option value="waste">Hủy</option><option value="adjustment">Kiểm kê</option></select></label>
          <span class="toolbar-note">Đang hiện {{ number(filteredMovements.length) }} dòng phù hợp.</span>
        </div>
        <div class="table-scroll">
          <table class="report-table report-table-detail">
            <thead><tr><th>Thời điểm</th><th>Nguyên liệu</th><th>Loại</th><th class="number-cell">Lượng tăng / giảm</th><th>Lô / chứng từ</th><th>Lý do</th></tr></thead>
            <tbody><tr v-for="movement in filteredMovements" :key="movement.stockMovementId">
              <td>{{ formatDateTime(movement.createdAt) }}</td><td>{{ movement.itemName }}<small>{{ movement.baseUnit }}</small></td>
              <td><span class="status-chip">{{ movement.movementLabel }}</span></td><td class="number-cell" :class="movement.quantityDelta >= 0 ? 'positive-text' : 'negative-text'">{{ movement.quantityDelta > 0 ? '+' : '' }}{{ quantity(movement.quantityDelta) }} {{ movement.baseUnit }}</td>
              <td>{{ movement.batchCode || 'Không gắn lô' }}<small v-if="movement.referenceId">{{ movement.referenceType }} #{{ movement.referenceId }}</small></td><td>{{ movement.reason || '—' }}</td>
            </tr></tbody>
          </table>
        </div>
        <p v-if="data.movements.length < data.summary.movementCount" class="detail-limit">Chi tiết giới hạn ở 1.000 biến động gần nhất. CSV chỉ xuất bảng tổng hợp nguyên liệu, các tổng phía trên vẫn tính đủ toàn bộ kỳ.</p>
      </details>
    </template>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { Search } from '@lucide/vue'
import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5079'
const props = defineProps({ fromDate: { type: String, required: true }, toDate: { type: String, required: true } })
const loading = ref(false)
const error = ref('')
const data = ref(null)
const search = ref('')
const itemFilter = ref('all')
const itemSort = ref('name')
const movementFilter = ref('all')
let requestVersion = 0
let controller

const filteredItems = computed(() => {
  const needle = search.value.trim().toLocaleLowerCase('vi-VN')
  const rows = (data.value?.items || []).filter(item => {
    const matchesSearch = !needle || `${item.itemName} ${item.baseUnit}`.toLocaleLowerCase('vi-VN').includes(needle)
    const matchesFilter = itemFilter.value === 'all'
      || (itemFilter.value === 'low' && item.isBelowMinimum)
      || (itemFilter.value === 'mismatch' && item.currentMismatch)
      || (itemFilter.value === 'moved' && item.movementCount > 0)
    return matchesSearch && matchesFilter
  })
  return rows.sort((a, b) => itemSort.value === 'low'
    ? Number(b.isBelowMinimum) - Number(a.isBelowMinimum) || Number(a.availableQuantity ?? a.currentQuantity) - Number(b.availableQuantity ?? b.currentQuantity) || a.itemName.localeCompare(b.itemName, 'vi')
    : itemSort.value === 'movements'
      ? Number(b.movementCount) - Number(a.movementCount) || a.itemName.localeCompare(b.itemName, 'vi')
      : a.itemName.localeCompare(b.itemName, 'vi'))
})
const trendMode = computed(() => {
  const start = new Date(`${props.fromDate}T12:00:00`)
  const end = new Date(`${props.toDate}T12:00:00`)
  return (end - start) / 86400000 > 31 ? 'month' : 'day'
})
const trendRows = computed(() => {
  const groups = new Map()
  for (const row of data.value?.dailyTrend || []) {
    const date = String(row.date).slice(0, 10)
    const key = trendMode.value === 'month' ? date.slice(0, 7) : date
    if (!groups.has(key)) groups.set(key, { key, label: trendMode.value === 'month' ? `${key.slice(5)}/${key.slice(0, 4)}` : `${key.slice(8)}/${key.slice(5, 7)}`, importValue: 0, exportValue: 0, wasteValue: 0, returnValue: 0 })
    const target = groups.get(key)
    for (const field of ['importValue', 'exportValue', 'wasteValue', 'returnValue']) target[field] += Number(row[field]) || 0
  }
  return [...groups.values()].sort((a, b) => a.key.localeCompare(b.key))
})
const trendMaximum = computed(() => Math.max(0, ...trendRows.value.flatMap(row => [row.importValue, row.exportValue, row.wasteValue])))
const trendMissingCostCount = computed(() => (data.value?.dailyTrend || []).reduce((total, row) => total + (Number(row.missingCostCount) || 0), 0))
function trendHeight(value) {
  const amount = Number(value) || 0
  return amount > 0 && trendMaximum.value > 0 ? `${Math.max(3, amount / trendMaximum.value * 100)}%` : '0%'
}
function currency(value) { return `${Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 0 })} đ` }
function exportItemsCsv() {
  if (!data.value) return
  const columns = ['Nguyên liệu', 'Đơn vị', 'Tồn đầu', 'Nhập mua', 'Hoàn bếp', 'Điều chỉnh tăng', 'Xuất bếp', 'Hủy', 'Điều chỉnh giảm', 'Tồn cuối kỳ', 'Tồn sổ hiện tại', 'Tồn dùng được hiện tại', 'Tồn tối thiểu', 'Biến động trong kỳ', 'Cảnh báo']
  const csvCell = value => `"${String(value ?? '').replaceAll('"', '""')}"`
  const lines = [columns, ...filteredItems.value.map(item => [item.itemName, item.baseUnit, item.openingQuantity, item.importQuantity, item.returnQuantity, item.adjustmentIncrease, item.exportQuantity, item.wasteQuantity, item.adjustmentDecrease, item.closingQuantity, item.currentQuantity, item.availableQuantity ?? item.currentQuantity, item.minimumQuantity, item.movementCount, item.currentMismatch ? 'Lệch sổ' : item.isBelowMinimum ? 'Dưới mức' : 'Khớp'])]
  const blob = new Blob(['\uFEFF', lines.map(row => row.map(csvCell).join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `xuat-nhap-ton-${props.fromDate}-${props.toDate}.csv`
  document.body.append(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}
const filteredMovements = computed(() => (data.value?.movements || []).filter(row => {
  const needle = search.value.trim().toLocaleLowerCase('vi-VN')
  const matchesSearch = !needle || `${row.itemName} ${row.baseUnit} ${row.batchCode || ''} ${row.reason || ''}`.toLocaleLowerCase('vi-VN').includes(needle)
  const matchesType = movementFilter.value === 'all'
    || (movementFilter.value === 'return' && row.movementType === 'import' && row.movementLabel === 'Hoàn từ bếp')
    || (movementFilter.value === 'import' && row.movementType === 'import' && row.movementLabel !== 'Hoàn từ bếp')
    || (movementFilter.value === 'adjustment' && row.movementType === 'adjustment')
    || row.movementType === movementFilter.value
  return matchesSearch && matchesType
}))

async function load() {
  const version = ++requestVersion
  controller?.abort()
  controller = new AbortController()
  loading.value = true
  error.value = ''
  try {
    const response = await axios.get(`${API_BASE}/api/admin/reports/inventory-ledger`, {
      params: { fromDate: props.fromDate, toDate: props.toDate }, signal: controller.signal, timeout: 5000
    })
    if (version !== requestVersion) return
    data.value = response.data?.data || null
    if (!data.value) throw new Error('Dữ liệu sổ kho không hợp lệ.')
  } catch (requestError) {
    if (version !== requestVersion || requestError.code === 'ERR_CANCELED') return
    error.value = requestError.response?.data?.message || requestError.message || 'Chưa tải được sổ kho.'
  } finally {
    if (version === requestVersion) loading.value = false
  }
}
watch(() => [props.fromDate, props.toDate], load, { immediate: true })
function number(value) { return Number(value || 0).toLocaleString('vi-VN') }
function quantity(value) { return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 3 }) }
function totalIn(row) { return Number(row.importedQuantity ?? row.importQuantity ?? 0) + Number(row.returnedQuantity ?? row.returnQuantity ?? 0) + Number(row.adjustmentIncrease ?? 0) }
function totalOut(row) { return Number(row.exportedQuantity ?? row.exportQuantity ?? 0) + Number(row.wastedQuantity ?? row.wasteQuantity ?? 0) + Number(row.adjustmentDecrease ?? 0) }
function formatDate(value) { return new Date(`${String(value).slice(0, 10)}T12:00:00`).toLocaleDateString('vi-VN') }
function formatDateTime(value) { return value ? new Date(value).toLocaleString('vi-VN') : '—' }
</script>

<style scoped>
.stock-trend{display:flex;flex-direction:column;gap:12px;padding:15px;border:1px solid #dce8e2;border-radius:9px;background:#fbfdfc}.trend-legend{display:flex;flex-wrap:wrap;gap:12px;color:#52645d;font-size:.75rem}.trend-legend span{display:flex;align-items:center;gap:5px}.trend-legend i{width:9px;height:9px;border-radius:2px}.trend-in{background:#20aa85}.trend-out{background:#dc8b53}.trend-waste{background:#d96b6b}.trend-scroll{overflow-x:auto;border-bottom:1px solid #dce8e2}.trend-chart{display:flex;align-items:end;justify-content:flex-start;gap:9px;min-width:max-content;height:166px;padding:5px 2px}.trend-column{display:flex;flex-direction:column;align-items:center;justify-content:end;width:40px;height:100%;flex:none;color:#64748b}.trend-bars{display:flex;align-items:end;justify-content:center;gap:2px;width:100%;height:138px}.trend-bar{width:10px;border-radius:2px 2px 0 0}.trend-column small{height:18px;margin-top:5px;white-space:nowrap;font-size:.65rem}.trend-warning{margin:0;color:#9a6200;font-size:.76rem}.trend-details summary{width:fit-content;color:#0f766e;font-size:.78rem;font-weight:700;cursor:pointer}.trend-details .table-scroll{margin-top:8px}.trend-details .report-table{min-width:510px}
.inventory-module{display:flex;flex-direction:column;gap:18px;color:#172033}.module-heading{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.module-eyebrow{margin:0 0 5px;color:#0f766e;font-size:.72rem;font-weight:750}.module-heading h2{margin:0;font-size:1.15rem}.module-heading p{margin:5px 0 0;color:#64748b;font-size:.84rem;line-height:1.45}.module-action,.module-error button{min-height:40px;display:inline-flex;align-items:center;justify-content:center;gap:8px;padding:0 12px;border:1px solid #cbd5e1;border-radius:7px;background:#fff;color:#164e63;font:inherit;font-weight:650;cursor:pointer}.module-action:disabled{opacity:.6;cursor:wait}.module-warning{display:grid;gap:5px;padding:12px 14px;color:#854d0e;background:#fffbeb;border:1px solid #fcd34d;border-radius:7px;font-size:.83rem;line-height:1.45}.module-warning ul{margin:3px 0 0;padding-left:19px}.module-warning li+li{margin-top:3px}.kpi-strip{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));border-top:1px solid #e2e8f0;border-bottom:1px solid #e2e8f0}.kpi-strip div{min-width:0;padding:14px 16px;border-right:1px solid #e2e8f0}.kpi-strip div:first-child{padding-left:0}.kpi-strip div:last-child{border-right:0}.kpi-strip span{display:block;color:#64748b;font-size:.76rem;line-height:1.35}.kpi-strip strong{display:block;margin-top:5px;font-size:1.15rem}.positive-text{color:#047857}.negative-text{color:#b91c1c}.warning-text{color:#b45309}.unit-summary,.item-section,.ledger-details{display:flex;flex-direction:column;gap:11px}.section-heading{display:flex;justify-content:space-between;align-items:flex-start;gap:12px}.section-heading h3{margin:0;font-size:.98rem}.section-heading p{margin:4px 0 0;color:#64748b;font-size:.79rem}.section-heading-inline{align-items:center}.table-scroll{overflow:auto;border:1px solid #e2e8f0;border-radius:7px}.report-table{width:100%;border-collapse:collapse;font-size:.8rem}.report-table th,.report-table td{padding:10px;border-bottom:1px solid #edf0f5;text-align:left;vertical-align:middle}.report-table th{background:#f8fafc;color:#64748b;font-size:.7rem;font-weight:700;white-space:nowrap}.report-table tbody tr:last-child td{border-bottom:0}.report-table tbody tr:hover{background:#f8fbfa}.unit-table{min-width:1050px}.item-table{min-width:1450px}.report-table strong{font-weight:700}.report-table small{display:block;margin-top:3px;color:#94a3b8;font-size:.72rem}.number-cell{text-align:right!important;white-space:nowrap}.module-toolbar{display:flex;align-items:center;justify-content:space-between;gap:12px}.search-field{display:flex;align-items:center;gap:8px;min-height:40px;width:min(380px,100%);padding:0 10px;border:1px solid #cbd5e1;border-radius:7px;color:#64748b}.search-field input{min-width:0;width:100%;border:0;outline:0;font:inherit;color:#172033}.toolbar-select select{min-height:40px;max-width:100%;padding:0 30px 0 10px;border:1px solid #cbd5e1;border-radius:7px;background:#fff;color:#172033;font:inherit}.toolbar-note{color:#64748b;font-size:.78rem}.status-chip{display:inline-flex;align-items:center;min-height:25px;padding:3px 7px;border-radius:6px;color:#475569;background:#f1f5f9;font-size:.7rem;font-weight:700;white-space:nowrap}.status-ok{color:#047857;background:#dcfce7}.status-warn,.status-low{color:#92400e;background:#fef3c7}.ledger-details{border-top:1px solid #e2e8f0;padding-top:13px}.ledger-details summary{width:fit-content;color:#0f766e;font-size:.84rem;font-weight:700;cursor:pointer}.detail-toolbar{padding:4px 0}.report-table-detail{min-width:880px}.detail-limit{margin:0;color:#64748b;font-size:.76rem}.module-error{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:11px 13px;color:#991b1b;background:#fef2f2;border:1px solid #fecaca;border-radius:7px;font-size:.83rem}.module-loading,.module-empty{display:grid;place-items:center;min-height:130px;padding:20px;color:#64748b;background:#f8fafc;border:1px dashed #cbd5e1;border-radius:7px}.sr-only{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap}
@media(max-width:900px){.kpi-strip{grid-template-columns:repeat(2,minmax(0,1fr))}.kpi-strip div:nth-child(2){border-right:0}.kpi-strip div{border-bottom:1px solid #e2e8f0}.kpi-strip div:nth-child(n+3){border-bottom:0}.section-heading-inline{align-items:flex-start;flex-direction:column}.module-toolbar{align-items:flex-start;flex-direction:column}.search-field{width:100%}.toolbar-select,.toolbar-select select{width:100%}}
@media(max-width:560px){.module-heading{flex-direction:column}.module-action{align-self:flex-end}.kpi-strip span{font-size:.72rem}.kpi-strip strong{font-size:1rem}}
.report-table small{color:#64748b}.unit-table{min-width:740px}.item-table{min-width:930px}
@media(max-width:640px){.unit-table,.item-table{min-width:0;width:100%}.unit-table thead,.item-table thead{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}.unit-table tbody,.item-table tbody{display:block}.unit-table tbody tr,.item-table tbody tr{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:0 10px;margin:0 0 9px;padding:6px;border:1px solid #e2e8f0;border-radius:7px;background:#fff}.unit-table tbody tr:last-child,.item-table tbody tr:last-child{margin-bottom:0}.unit-table tbody td,.item-table tbody td{display:flex;flex-direction:column;align-items:flex-start;min-width:0;padding:7px 5px;border:0;text-align:left!important;white-space:normal}.unit-table tbody td:first-child,.item-table tbody td:first-child{grid-column:1/-1;border-bottom:1px solid #edf0f5;font-size:.86rem}.unit-table tbody td::before,.item-table tbody td::before{content:attr(data-label);margin-bottom:3px;color:#64748b;font-size:.68rem;font-weight:650}.unit-table tbody td:first-child::before,.item-table tbody td:first-child::before{display:none}.unit-table small,.item-table small{line-height:1.4}.table-scroll:has(.unit-table),.table-scroll:has(.item-table){overflow:visible;border:0}}
</style>

<template>
  <section class="report-module payment-report" aria-labelledby="payment-reconciliation-title">
    <header class="module-heading">
      <div>
        <p class="module-eyebrow">THU TIỀN TRONG KỲ</p>
        <h2 id="payment-reconciliation-title">Tổng hợp hóa đơn & thanh toán</h2>
        <p>Kiểm tra nhanh số hóa đơn và số tiền đã thu theo từng phương thức.</p>
      </div>
      <button class="module-action" type="button" :disabled="loading" aria-label="Làm mới báo cáo thanh toán" title="Làm mới" @click="load">
        <span aria-hidden="true">↻</span>{{ loading ? 'Đang tải' : 'Làm mới' }}
      </button>
    </header>

    <div v-if="error" class="module-error" role="alert">
      <span>{{ error }}</span>
      <button type="button" aria-label="Thử tải lại báo cáo thanh toán" @click="load">Thử lại</button>
    </div>
    <div v-else-if="loading" class="module-loading" role="status" aria-live="polite">Đang tải tổng hợp thanh toán…</div>

    <template v-else-if="data">
      <div class="payment-summary">
        <div class="summary-item">
          <span class="summary-label">Tổng số hóa đơn</span>
          <strong>{{ number(data.summary.invoiceCount) }}</strong>
          <small>Hóa đơn trong kỳ đang xem</small>
        </div>
        <div class="summary-item">
          <span class="summary-label">Tiền mặt</span>
          <strong>{{ money(data.summary.cashAmount ?? paymentTotals.cash) }}</strong>
          <button class="detail-button" type="button" @click="openDetails('cash')">Xem chi tiết</button>
        </div>
        <div class="summary-item">
          <span class="summary-label">Chuyển khoản</span>
          <strong>{{ money(data.summary.transferAmount ?? paymentTotals.transfer) }}</strong>
          <button class="detail-button" type="button" @click="openDetails('transfer')">Xem chi tiết</button>
        </div>
      </div>

      <div class="summary-note">
        Tiền mặt/chuyển khoản chỉ cộng giao dịch hoàn tất theo ngày nhận tiền; số hóa đơn tính theo ngày lập bill. Bấm <strong>Xem bill</strong> để xem đầy đủ món ăn, bàn, khách hàng và thanh toán.
        <span v-if="data.paymentEventsTruncated"> Đang hiện {{ number(data.paymentEvents.length) }} / {{ number(data.summary.paymentEventCount) }} giao dịch gần nhất; tổng tiền vẫn tính đủ.</span>
      </div>

      <details ref="detailsElement" class="payment-details">
        <summary>Xem chi tiết giao dịch ({{ number(filteredEvents.length) }})</summary>
        <div class="details-toolbar">
          <span>Phương thức đang xem:</span>
          <select v-model="detailMethod" aria-label="Lọc phương thức thanh toán">
            <option value="all">Tất cả</option>
            <option value="cash">Tiền mặt</option>
            <option value="transfer">Chuyển khoản</option>
          </select>
        </div>
        <div v-if="!filteredEvents.length" class="module-empty">Chưa có giao dịch phù hợp trong kỳ.</div>
        <div v-else class="table-scroll">
          <table class="report-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Hóa đơn</th>
                <th>Đơn hàng / bàn</th>
                <th>Phương thức</th>
                <th>Mã giao dịch</th>
                <th class="number-cell">Số tiền</th>
                <th>Trạng thái</th>
                <th class="action-cell">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="payment in filteredEvents" :key="payment.paymentId">
                <td>{{ formatDateTime(payment.paidAt) }}</td>
                <td>
                  <button class="invoice-link" type="button" @click="openInvoiceDetail(payment.invoiceId)">
                    <strong>{{ payment.invoiceCode }}</strong>
                  </button>
                </td>
                <td>
                  <strong>#ORD-{{ payment.orderId || '—' }}</strong>
                  <small class="payment-context-line">{{ payment.tableDisplay || 'Không có bàn' }}</small>
                  <small class="payment-context-line">{{ payment.customerName || 'Khách vãng lai' }}</small>
                  <small v-if="payment.customerPhone" class="payment-context-line">{{ payment.customerPhone }}</small>
                </td>
                <td><span class="payment-method-tag" :class="paymentMethodClass(payment.paymentMethod)">{{ paymentMethodLabel(payment.paymentMethod) }}</span></td>
                <td><span class="transaction-code">{{ payment.transactionCode || '—' }}</span></td>
                <td class="number-cell">{{ money(payment.amount) }}</td>
                <td><span class="payment-status-tag" :class="paymentStatusClass(payment.paymentStatus)">{{ paymentStatusLabel(payment.paymentStatus) }}</span></td>
                <td class="action-cell">
                  <button class="view-invoice-button" type="button" @click="openInvoiceDetail(payment.invoiceId)">Xem bill</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </details>

      <div v-if="false && invoiceDetailOpen" class="invoice-modal-backdrop" @click.self="closeInvoiceDetail">
        <section class="invoice-modal" role="dialog" aria-modal="true" aria-labelledby="invoice-detail-title">
          <header class="invoice-modal-heading">
            <div>
              <p class="module-eyebrow">CHI TIẾT HÓA ĐƠN</p>
              <h3 id="invoice-detail-title">{{ invoiceDetail?.invoiceCode || 'Đang tải...' }}</h3>
            </div>
            <button class="modal-close" type="button" aria-label="Đóng chi tiết hóa đơn" @click="closeInvoiceDetail">×</button>
          </header>

          <div v-if="invoiceDetailLoading" class="module-loading">Đang tải chi tiết hóa đơn…</div>
          <div v-else-if="invoiceDetailError" class="module-error" role="alert">
            <span>{{ invoiceDetailError }}</span>
            <button type="button" @click="openInvoiceDetail(invoiceDetail?.invoiceId)">Thử lại</button>
          </div>
          <template v-else-if="invoiceDetail">
            <div class="invoice-meta-grid">
              <div><span>Mã đơn hàng</span><strong>#{{ invoiceDetail.orderId }}</strong></div>
              <div><span>Bàn</span><strong>{{ invoiceDetail.tableDisplay || 'Không xác định' }}</strong></div>
              <div><span>Thời gian</span><strong>{{ formatDateTime(invoiceDetail.issuedAt) }}</strong></div>
              <div><span>Thu ngân</span><strong>{{ invoiceDetail.cashierName || 'Thu ngân' }}</strong></div>
            </div>

            <div v-if="invoiceDetail.customer" class="invoice-customer">
              Khách hàng: <strong>{{ invoiceDetail.customer.fullName }}</strong>
              <span v-if="invoiceDetail.customer.phone"> · {{ invoiceDetail.customer.phone }}</span>
            </div>

            <div class="invoice-items-scroll">
              <table class="report-table invoice-items-table">
                <thead>
                  <tr>
                    <th>Món / biến thể</th>
                    <th class="number-cell">SL</th>
                    <th class="number-cell">Đơn giá</th>
                    <th class="number-cell">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in invoiceDetail.items || []" :key="item.invoiceItemId">
                    <td>
                      <strong>{{ item.foodNameSnapshot }}</strong>
                      <small v-if="item.variantSnapshot">{{ item.variantSnapshot }}</small>
                    </td>
                    <td class="number-cell">{{ item.quantity }}</td>
                    <td class="number-cell">{{ money(item.unitPrice) }}</td>
                    <td class="number-cell"><strong>{{ money(item.lineTotal) }}</strong></td>
                  </tr>
                  <tr v-if="!(invoiceDetail.items || []).length">
                    <td colspan="4" class="empty-cell">Hóa đơn chưa có món.</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <div class="invoice-summary">
              <div><span>Tạm tính</span><strong>{{ money(invoiceDetail.subtotal) }}</strong></div>
              <div><span>Giảm giá</span><strong>-{{ money(invoiceDetail.discountAmount) }}</strong></div>
              <div class="invoice-total"><span>Tổng thanh toán</span><strong>{{ money(invoiceDetail.totalAmount) }}</strong></div>
            </div>

            <div class="invoice-payments">
              <span>Thanh toán:</span>
              <span v-for="payment in invoiceDetail.payments || []" :key="`${payment.paidAt}-${payment.amount}`" class="payment-chip">
                {{ paymentMethodLabel(payment.paymentMethod) }} · {{ money(payment.amount) }}
              </span>
            </div>
          </template>
        </section>
      </div>

      <div v-if="invoiceDetailOpen && (invoiceDetailLoading || invoiceDetailError)" class="invoice-state-backdrop">
        <section class="invoice-state-modal" role="dialog" aria-modal="true" aria-labelledby="invoice-state-title">
          <h3 id="invoice-state-title">{{ invoiceDetailLoading ? 'Đang tải hóa đơn' : 'Không tải được hóa đơn' }}</h3>
          <p v-if="invoiceDetailLoading">Đang lấy dữ liệu bill, vui lòng chờ một chút.</p>
          <p v-else>{{ invoiceDetailError }}</p>
          <div class="invoice-state-actions">
            <button v-if="invoiceDetailError" type="button" class="module-action" @click="openInvoiceDetail(invoiceDetail?.invoiceId)">Thử lại</button>
            <button type="button" class="module-action" @click="closeInvoiceDetail">Đóng</button>
          </div>
        </section>
      </div>

      <AdminInvoiceReceiptModal
        v-if="invoiceDetailOpen && invoiceDetail && !invoiceDetailLoading && !invoiceDetailError"
        :show="true"
        :invoice="invoiceDetail"
        @close="closeInvoiceDetail"
      />
    </template>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import axios from 'axios'
import AdminInvoiceReceiptModal from './AdminInvoiceReceiptModal.vue'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5079'
const props = defineProps({ fromDate: { type: String, required: true }, toDate: { type: String, required: true } })
const loading = ref(false)
const error = ref('')
const data = ref(null)
const detailMethod = ref('all')
const detailsElement = ref(null)
const invoiceDetail = ref(null)
const invoiceDetailOpen = ref(false)
const invoiceDetailLoading = ref(false)
const invoiceDetailError = ref('')
let loadRequestId = 0
let invoiceRequestId = 0

const completedEvents = computed(() => (data.value?.paymentEvents || []).filter(payment => String(payment.paymentStatus || '').toLowerCase() === 'completed'))
const paymentTotals = computed(() => completedEvents.value.reduce((totals, payment) => {
  const method = String(payment.paymentMethod || '').toLowerCase()
  if (method === 'cash') totals.cash += Number(payment.amount || 0)
  else if (['transfer', 'bank_transfer', 'banktransfer'].includes(method)) totals.transfer += Number(payment.amount || 0)
  return totals
}, { cash: 0, transfer: 0 }))
const filteredEvents = computed(() => {
  if (detailMethod.value === 'all') return data.value?.paymentEvents || []
  return (data.value?.paymentEvents || []).filter(payment => {
    const method = String(payment.paymentMethod || '').toLowerCase()
    return detailMethod.value === 'cash'
      ? method === 'cash'
      : ['transfer', 'bank_transfer', 'banktransfer'].includes(method)
  })
})

async function load() {
  const requestId = ++loadRequestId
  loading.value = true
  error.value = ''
  try {
    const response = await axios.get(API_BASE + '/api/admin/reports/payment-reconciliation', {
      params: { fromDate: props.fromDate, toDate: props.toDate },
      timeout: 5000
    })
    if (requestId !== loadRequestId) return
    data.value = response.data?.data || null
    if (!data.value) throw new Error('Dữ liệu thanh toán không hợp lệ.')
  } catch (requestError) {
    if (requestId === loadRequestId) error.value = requestError.response?.data?.message || requestError.message || 'Chưa tải được báo cáo thanh toán.'
  } finally {
    if (requestId === loadRequestId) loading.value = false
  }
}

function openDetails(method) {
  detailMethod.value = method
  requestAnimationFrame(() => { detailsElement.value?.setAttribute('open', '') })
}

async function openInvoiceDetail(invoiceId) {
  if (!invoiceId) return
  const requestId = ++invoiceRequestId
  invoiceDetailOpen.value = true
  invoiceDetailLoading.value = true
  invoiceDetailError.value = ''
  invoiceDetail.value = null
  try {
    const response = await axios.get(API_BASE + '/api/staff/payment/invoices/' + invoiceId, { timeout: 5000 })
    if (requestId !== invoiceRequestId) return
    invoiceDetail.value = response.data?.data || response.data || null
    if (!invoiceDetail.value) throw new Error('Không có dữ liệu chi tiết hóa đơn.')
  } catch (requestError) {
    if (requestId === invoiceRequestId) invoiceDetailError.value = requestError.response?.data?.message || requestError.message || 'Chưa tải được chi tiết hóa đơn.'
  } finally {
    if (requestId === invoiceRequestId) invoiceDetailLoading.value = false
  }
}

function closeInvoiceDetail() {
  invoiceRequestId++
  invoiceDetailOpen.value = false
  invoiceDetailLoading.value = false
  invoiceDetail.value = null
  invoiceDetailError.value = ''
}

watch(() => [props.fromDate, props.toDate], load, { immediate: true })
function number(value) { return Number(value || 0).toLocaleString('vi-VN') }
function money(value) { return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫' }
function formatDateTime(value) { return value ? new Date(value).toLocaleString('vi-VN') : '—' }
function paymentMethodLabel(value) { return ({ cash: 'Tiền mặt', transfer: 'Chuyển khoản', bank_transfer: 'Chuyển khoản', banktransfer: 'Chuyển khoản' })[String(value || '').toLowerCase()] || 'Khác' }
function paymentStatusLabel(value) { return String(value || '').toLowerCase() === 'completed' ? 'Đã hoàn tất' : value || 'Chưa rõ' }
function paymentMethodClass(value) {
  const method = String(value || '').toLowerCase()
  if (method === 'cash') return 'is-cash'
  if (['transfer', 'bank_transfer', 'banktransfer'].includes(method)) return 'is-transfer'
  return 'is-other'
}
function paymentStatusClass(value) { return String(value || '').toLowerCase() === 'completed' ? 'is-completed' : 'is-pending' }
</script>

<style scoped>
.report-module{display:flex;flex-direction:column;gap:16px;padding:20px;background:#fff;border:1px solid #dfe5ef;border-radius:12px;color:#172033}
.module-heading{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}
.module-eyebrow{margin:0 0 5px;color:#0f766e;font-size:.7rem;font-weight:800;letter-spacing:.11em}
.module-heading h2{margin:0;font-size:1.1rem}
.module-heading p:not(.module-eyebrow){margin:5px 0 0;color:#64748b;font-size:.82rem}
.module-action,.module-error button{min-height:40px;padding:0 12px;border:1px solid #cbd5e1;border-radius:8px;background:#fff;color:#164e63;font:inherit;font-weight:700;cursor:pointer}
.module-action:disabled{opacity:.6;cursor:wait}
.module-error{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:11px 13px;color:#991b1b;background:#fef2f2;border:1px solid #fecaca;border-radius:8px;font-size:.83rem}
.module-loading,.module-empty{display:grid;place-items:center;min-height:150px;padding:20px;color:#64748b;background:#f8fafc;border:1px dashed #cbd5e1;border-radius:8px}
.payment-summary{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px}
.summary-item{display:flex;flex-direction:column;gap:6px;min-width:0;padding:16px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px}
.summary-label{color:#64748b;font-size:.8rem;font-weight:700}
.summary-item strong{font-size:1.3rem;color:#172033;white-space:nowrap}
.summary-item small{color:#94a3b8;font-size:.72rem}
.detail-button{align-self:flex-start;padding:0;border:0;background:transparent;color:#1d4ed8;font:inherit;font-size:.76rem;font-weight:700;cursor:pointer}
.summary-note{padding:11px 13px;color:#475569;background:#f0fdfa;border:1px solid #99f6e4;border-radius:8px;font-size:.8rem;line-height:1.5}
.summary-note strong{color:#0f766e}
.payment-details{border:1px solid #e2e8f0;border-radius:10px;padding:0 12px 12px;background:#fff}
.payment-details summary{padding:13px 2px;color:#164e63;font-size:.84rem;font-weight:700;cursor:pointer;list-style-position:inside}
.details-toolbar{display:flex;align-items:center;justify-content:space-between;gap:8px;margin:0 0 12px;padding-top:10px;color:#64748b;font-size:.78rem;border-top:1px solid #edf0f5}
.details-toolbar select{min-height:36px;padding:0 26px 0 8px;border:1px solid #cbd5e1;border-radius:7px;background:#fff;color:#172033;font:inherit}
.table-scroll{overflow-x:auto;border:1px solid #e2e8f0;border-radius:8px}
.report-table{width:100%;min-width:980px;border-collapse:collapse;font-size:.8rem}
.report-table th,.report-table td{padding:12px 10px;border-bottom:1px solid #edf0f5;text-align:left;vertical-align:middle}
.report-table th{background:#f8fafc;color:#64748b;font-size:.7rem;text-transform:uppercase;white-space:nowrap}
.report-table tbody tr:last-child td{border-bottom:0}
.number-cell{text-align:right!important;white-space:nowrap}
.invoice-link{padding:0;border:0;background:transparent;color:#1d4ed8;font:inherit;text-align:left;cursor:pointer}
.invoice-link:hover{text-decoration:underline}
.payment-context-line{display:block;margin-top:3px;color:#64748b;font-size:.72rem;line-height:1.35}
.transaction-code{color:#64748b;font-size:.74rem;word-break:break-word}
.payment-method-tag,.payment-status-tag{display:inline-flex;align-items:center;min-height:25px;padding:0 8px;border-radius:999px;font-size:.72rem;font-weight:700;white-space:nowrap}
.payment-method-tag.is-cash{color:#166534;background:#dcfce7}.payment-method-tag.is-transfer{color:#075985;background:#e0f2fe}.payment-method-tag.is-other{color:#475569;background:#f1f5f9}
.payment-status-tag.is-completed{color:#166534;background:#dcfce7}.payment-status-tag.is-pending{color:#92400e;background:#fef3c7}
.action-cell{text-align:center!important;white-space:nowrap}
.view-invoice-button{min-height:34px;padding:0 10px;border:1px solid #99f6e4;border-radius:7px;background:#f0fdfa;color:#0f766e;font:inherit;font-size:.76rem;font-weight:700;cursor:pointer}
.view-invoice-button:hover{background:#0f766e;color:#fff}
.invoice-state-backdrop{position:fixed;inset:0;z-index:30;display:grid;place-items:center;padding:20px;background:rgba(15,23,42,.48)}
.invoice-state-modal{width:min(420px,100%);padding:24px;background:#fff;border:1px solid #dbe3ee;border-radius:12px;box-shadow:0 22px 60px rgba(15,23,42,.24)}
.invoice-state-modal h3{margin:0;color:#172033;font-size:1.05rem}
.invoice-state-modal p{margin:8px 0 0;color:#64748b;font-size:.85rem;line-height:1.5}
.invoice-state-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:18px}
.invoice-modal-backdrop{position:fixed;inset:0;z-index:30;display:grid;place-items:center;padding:20px;background:rgba(15,23,42,.48)}
.invoice-modal{width:min(760px,100%);max-height:min(720px,calc(100vh - 40px));overflow:auto;padding:20px;background:#fff;border:1px solid #dbe3ee;border-radius:12px;box-shadow:0 22px 60px rgba(15,23,42,.24)}
.invoice-modal-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:16px;padding-bottom:14px;border-bottom:1px solid #e2e8f0}
.invoice-modal-heading h3{margin:0;font-size:1.15rem}
.modal-close{width:36px;height:36px;border:1px solid #cbd5e1;border-radius:8px;background:#fff;color:#475569;font-size:1.3rem;line-height:1;cursor:pointer}
.invoice-meta-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin:16px 0}
.invoice-meta-grid div{display:flex;flex-direction:column;gap:4px;padding:10px;background:#f8fafc;border-radius:8px}
.invoice-meta-grid span,.invoice-customer{color:#64748b;font-size:.74rem}
.invoice-meta-grid strong{color:#172033;font-size:.82rem}
.invoice-customer{padding:10px 12px;margin-bottom:12px;background:#f0fdfa;border:1px solid #ccfbf1;border-radius:8px}
.invoice-items-scroll{overflow-x:auto;border:1px solid #e2e8f0;border-radius:8px}
.invoice-items-table{min-width:580px}
.invoice-items-table small{display:block;margin-top:3px;color:#64748b}
.empty-cell{text-align:center!important;color:#64748b}
.invoice-summary{display:flex;flex-direction:column;align-items:flex-end;gap:6px;padding:14px 0;border-bottom:1px solid #e2e8f0}
.invoice-summary div{display:flex;justify-content:space-between;gap:36px;min-width:220px;color:#64748b;font-size:.8rem}
.invoice-summary strong{color:#172033}
.invoice-summary .invoice-total{padding-top:6px;color:#172033;font-size:.95rem;font-weight:700}
.invoice-summary .invoice-total strong{color:#0f766e;font-size:1.1rem}
.invoice-payments{display:flex;flex-wrap:wrap;align-items:center;gap:8px;padding-top:14px;color:#64748b;font-size:.78rem}
.payment-chip{padding:5px 8px;color:#075985;background:#e0f2fe;border-radius:999px}
@media(max-width:900px){.payment-summary{grid-template-columns:1fr}.module-heading{flex-direction:column}.module-action{align-self:flex-start}}
@media(max-width:640px){.details-toolbar{align-items:flex-start;flex-direction:column}.invoice-meta-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.invoice-modal-backdrop{padding:10px}.invoice-modal{max-height:calc(100vh - 20px);padding:15px}.invoice-summary div{min-width:0;width:100%}}
</style>

<template>
  <Teleport to="body">
  <div v-if="show" class="receipt-overlay invoice-receipt-overlay" @click.self="$emit('close')">
    <section class="receipt-dialog" role="dialog" aria-modal="true" aria-labelledby="receipt-title">
      <header class="receipt-toolbar">
        <strong class="receipt-toolbar-title">Chi tiết hóa đơn</strong>
        <div class="receipt-toolbar-actions">
          <button type="button" class="receipt-action" title="In lại hóa đơn" @click="printReceipt">
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M6 9V3h12v6M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2M6 14h12v7H6z" /></svg>
            <span>In lại hóa đơn</span>
          </button>
          <button type="button" class="receipt-close" title="Đóng" aria-label="Đóng" @click="$emit('close')">×</button>
        </div>
      </header>

      <div class="receipt-scroll-area">
        <article ref="receiptPaper" class="receipt-paper">
          <header class="restaurant-heading">
            <h1>DOLA RESTAURANT</h1>
            <p>Ẩm Thực Đương Đại &amp; Đẳng Cấp</p>
            <p>123 Đường Ẩm Thực, Quận 1, TP. HCM</p>
            <p>Hotline: 1900 6868 · Website: dolarestaurant.vn</p>
          </header>
          <div class="receipt-rule" aria-hidden="true"></div>
          <h2 id="receipt-title">HÓA ĐƠN THANH TOÁN</h2>
          <div class="receipt-rule" aria-hidden="true"></div>

          <section class="receipt-meta" aria-label="Thông tin hóa đơn">
            <div class="meta-row"><span>Mã HĐ: <strong>#{{ invoiceCode }}</strong></span><span>Bàn: <strong>{{ invoice?.tableDisplay || 'Không có bàn' }}</strong></span></div>
            <div class="meta-row"><span>Ngày: {{ shortDate(invoice?.issuedAt) }}</span><span>Thu ngân: {{ invoice?.cashierName || 'Nhân viên' }}</span></div>
            <div class="meta-row customer-row"><span>Khách hàng: <strong>{{ customerLabel }}</strong></span><span>PT: <strong>{{ paymentMethodLabel }}</strong></span></div>
            <div v-if="transactionCode && !isCash" class="transaction-row">Mã GD: {{ transactionCode }}</div>
          </section>

          <div class="receipt-rule" aria-hidden="true"></div>

          <section class="receipt-items" aria-label="Danh sách món ăn">
            <div class="items-header receipt-grid"><strong>TÊN MÓN</strong><strong>SL</strong><strong>Đ.GIÁ</strong><strong>T.TIỀN</strong></div>
            <div class="receipt-dotted-rule" aria-hidden="true"></div>
            <div v-for="(item, index) in invoice?.items || []" :key="item.invoiceItemId || index" class="receipt-item">
              <div class="receipt-grid item-main-row">
                <strong class="item-name">{{ item.foodNameSnapshot || item.foodName || 'Món ăn' }}</strong>
                <strong class="item-quantity">{{ quantity(item.quantity) }}</strong>
                <span class="item-unit-price">{{ moneyWithoutUnit(item.unitPrice) }}</span>
                <strong class="item-line-total">{{ money(item.lineTotal) }}</strong>
              </div>
              <div v-if="item.variantSnapshot && item.variantSnapshot !== 'Mặc định'" class="item-subline">» Size: {{ item.variantSnapshot }}</div>
              <div v-if="item.note" class="item-subline item-note">* {{ item.note }}</div>
            </div>
            <div v-if="!(invoice?.items || []).length" class="empty-items">Không có món ăn</div>
          </section>

          <div class="receipt-rule" aria-hidden="true"></div>
          <section class="receipt-totals" aria-label="Tổng tiền hóa đơn">
            <div class="price-row"><span>Tạm tính:</span><strong>{{ money(invoice?.subtotal) }}</strong></div>
            <div v-if="voucherDiscount > 0" class="price-row"><span>Ưu đãi Voucher{{ voucherCode ? ` (${voucherCode})` : '' }}:</span><strong>-{{ money(voucherDiscount) }}</strong></div>
            <div v-if="pointsUsed > 0 || pointDiscount > 0" class="price-row"><span>Trừ điểm{{ pointsUsed > 0 ? ` (${pointsUsed} điểm)` : '' }}:</span><strong>-{{ money(pointDiscount) }}</strong></div>
            <div v-if="discountAmount > (voucherDiscount + pointDiscount)" class="price-row"><span>Giảm giá khác:</span><strong>-{{ money(discountAmount - voucherDiscount - pointDiscount) }}</strong></div>
            <div class="double-rule" aria-hidden="true"></div>
            <div class="total-row"><strong>TỔNG CỘNG:</strong><strong>{{ money(invoice?.totalAmount) }}</strong></div>
            <div class="double-rule" aria-hidden="true"></div>
            <template v-if="isCash">
              <div class="price-row"><span>Tiền khách đưa:</span><strong>{{ money(amountPaid) }}</strong></div>
              <div class="price-row"><span>Tiền thối lại:</span><strong>{{ money(changeToReturn) }}</strong></div>
            </template>
          </section>

          <section v-if="pointsUsed > 0 || pointsEarned > 0 || customerPointsBalance !== null" class="loyalty-row">
            <span v-if="pointsUsed > 0">Đã dùng: -{{ pointsUsed }} điểm</span>
            <span v-if="pointsEarned > 0">Tích lũy bill: +{{ pointsEarned }} điểm</span>
            <span v-if="customerPointsBalance !== null">Số dư điểm: {{ customerPointsBalance }} điểm</span>
          </section>
          <div class="receipt-rule" aria-hidden="true"></div>
          <footer class="receipt-footer">
            <p>Wifi: Dola Restaurant · Mật khẩu: 88888888</p>
            <strong>Cảm ơn Quý khách &amp; Hẹn gặp lại!</strong>
            <p class="barcode" aria-hidden="true">||||| | |||||| || |||||||| | ||||| |||||||</p>
          </footer>
        </article>
      </div>
    </section>
  </div>
  </Teleport>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  show: { type: Boolean, default: false },
  invoice: { type: Object, default: null }
})
defineEmits(['close'])

const printBodyClass = 'printing-invoice-receipt'
const receiptPaper = ref(null)
let printPageStyle = null
watch(() => props.show, show => {
  document.body.classList.toggle(printBodyClass, show)
  if (!show) resetPrintLayout()
}, { immediate: true })
onMounted(() => {
  window.addEventListener('beforeprint', preparePrintLayout)
  window.addEventListener('afterprint', resetPrintLayout)
})
onBeforeUnmount(() => {
  window.removeEventListener('beforeprint', preparePrintLayout)
  window.removeEventListener('afterprint', resetPrintLayout)
  resetPrintLayout()
  document.body.classList.remove(printBodyClass)
})

const invoiceCode = computed(() => props.invoice?.invoiceCode || props.invoice?.invoiceId || '—')
const customer = computed(() => props.invoice?.customer || null)
const customerLabel = computed(() => {
  if (!customer.value) return 'Khách vãng lai'
  let name = customer.value.fullName || 'Khách hàng'
  name = name.replace(/\s*\((?:Khách\s+)?(?:Mới|Cũ|mới|cũ|New|Old|VIP|Thành viên|Hội viên)\)/gi, '').trim()
  return customer.value.phone ? `${name} (${customer.value.phone})` : name
})
const payment = computed(() => props.invoice?.payments?.[0] || {})
const paymentMethod = computed(() => payment.value.paymentMethod || props.invoice?.paymentMethod || '')
const paymentMethodLabel = computed(() => {
  const method = String(paymentMethod.value).toLowerCase()
  if (method === 'cash') return 'Tiền mặt'
  if (['transfer', 'vietqr', 'bank', 'bank_transfer', 'banktransfer'].includes(method)) return 'VietQR'
  if (method === 'card') return 'Thẻ POS'
  if (method === 'e_wallet' || method === 'ewallet') return 'Ví điện tử'
  return paymentMethod.value || 'Chưa xác định'
})
const transactionCode = computed(() => payment.value.transactionCode || '')
const isCash = computed(() => String(paymentMethod.value).toLowerCase() === 'cash')
const discountAmount = computed(() => Number(props.invoice?.discountAmount || 0))
const pointsUsed = computed(() => Number(props.invoice?.pointsUsed || 0))
const pointDiscount = computed(() => {
  const explicit = Number(props.invoice?.pointDiscount || 0)
  if (explicit > 0) return explicit
  return pointsUsed.value > 0 ? pointsUsed.value * 1000 : 0
})
const voucherDiscount = computed(() => {
  const explicit = Number(props.invoice?.voucherDiscount || 0)
  if (explicit > 0) return explicit
  const total = discountAmount.value
  const pts = pointDiscount.value
  return total > pts ? total - pts : 0
})
const voucherCode = computed(() => props.invoice?.voucherCode || props.invoice?.promotionCode || '')
const pointsEarned = computed(() => Number(props.invoice?.pointsEarned || 0))
const customerPointsBalance = computed(() => {
  const value = customer.value?.pointsBalance
  return value === null || value === undefined ? null : Number(value)
})
const amountPaid = computed(() => {
  const paid = Number(props.invoice?.amountPaidByCustomer || payment.value.amountPaidByCustomer || 0)
  return paid > 0 ? paid : Number(props.invoice?.totalAmount || 0)
})
const changeToReturn = computed(() => {
  const explicitChange = Number(props.invoice?.changeToReturn || 0)
  return Math.max(Number(amountPaid.value) - Number(props.invoice?.totalAmount || 0), explicitChange)
})

function quantity(value) {
  const numberValue = Number(value || 0)
  return Number.isInteger(numberValue) ? String(numberValue) : numberValue.toLocaleString('vi-VN')
}
function money(value) {
  return `${Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 0 })} đ`
}
function moneyWithoutUnit(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 0 })
}
function shortDate(value) {
  if (!value) return '—'
  let str = String(value).trim()
  if (!str.endsWith('Z') && !str.includes('+') && !/-\d{2}:\d{2}$/.test(str)) {
    str += 'Z'
  }
  const date = new Date(str)
  if (Number.isNaN(date.getTime())) return '—'
  const parts = new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false }).formatToParts(date)
  const get = type => parts.find(part => part.type === type)?.value || ''
  return `${get('day')}/${get('month')} ${get('hour')}:${get('minute')}`
}
function preparePrintLayout() {
  const paper = receiptPaper.value
  if (!props.show || !paper) return

  // Measure at the actual print width and type sizes before choosing the page length.
  paper.classList.add('receipt-print-layout')
  const heightPixels = Math.max(paper.scrollHeight, paper.getBoundingClientRect().height)
  const heightMillimeters = Math.ceil(heightPixels * 25.4 / 96) + 2
  if (!printPageStyle) {
    printPageStyle = document.createElement('style')
    printPageStyle.media = 'print'
    printPageStyle.dataset.invoicePageSize = ''
    document.head.appendChild(printPageStyle)
  }
  printPageStyle.textContent = `@page { size: 80mm ${heightMillimeters}mm; margin: 0; }`
}
function resetPrintLayout() {
  receiptPaper.value?.classList.remove('receipt-print-layout')
  printPageStyle?.remove()
  printPageStyle = null
}
function printReceipt() {
  preparePrintLayout()
  window.print()
}
</script>

<style scoped>
.receipt-overlay{position:fixed;inset:0;z-index:60;display:grid;place-items:center;padding:18px;background:rgba(15,23,42,.6)}
.receipt-dialog{display:flex;flex-direction:column;width:min(600px,100%);max-height:min(920px,calc(100vh - 24px));overflow:hidden;background:#f1f5f9;border:1px solid #cbd5e1;border-radius:12px;box-shadow:0 24px 70px rgba(15,23,42,.3)}
.receipt-toolbar{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:12px 14px;background:#fff;border-bottom:1px solid #e2e8f0}
.receipt-toolbar-title{color:#172033;font-size:.88rem}
.receipt-toolbar-actions{display:flex;align-items:center;gap:8px}
.receipt-action{display:inline-flex;align-items:center;gap:7px;min-height:36px;padding:0 10px;border:1px solid #cbd5e1;border-radius:8px;background:#fff;color:#164e63;font:inherit;font-size:.76rem;font-weight:700;cursor:pointer}
.receipt-action:hover{background:#ecfeff;border-color:#5eead4}
.receipt-action svg{width:16px;height:16px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round}
.receipt-close{width:36px;height:36px;border:1px solid #cbd5e1;border-radius:8px;background:#fff;color:#475569;font-size:1.45rem;line-height:1;cursor:pointer}
.receipt-scroll-area{overflow:auto;padding:16px}
.receipt-paper{width:min(520px,100%);margin:0 auto;padding:24px 20px 26px;background:#fff;color:#0f172a;border:1px solid #e2e8f0;border-radius:10px;box-shadow:0 8px 20px rgba(15,23,42,.08);font-family:Arial,Helvetica,sans-serif}
.restaurant-heading{text-align:center}.restaurant-heading h1{margin:0;font-family:Georgia,'Times New Roman',serif;font-size:24px;line-height:1.15;letter-spacing:1px;font-weight:800}.restaurant-heading p{margin:4px 0 0;color:#475569;font-size:12px;line-height:1.3}
.receipt-paper h2{margin:0;text-align:center;font-size:20px;line-height:1.2;letter-spacing:.6px;font-weight:800}.receipt-rule{height:1px;margin:13px 0;background:repeating-linear-gradient(to right,#94a3b8 0 14px,transparent 14px 24px)}.receipt-dotted-rule{height:1px;margin:5px 0 6px;background:repeating-linear-gradient(to right,#cbd5e1 0 8px,transparent 8px 16px)}
.receipt-meta{font-size:14px;color:#475569;line-height:1.55}.meta-row{display:flex;justify-content:space-between;gap:18px;margin:3px 0}.meta-row>span:first-child{min-width:0}.meta-row>span:last-child{text-align:right}.meta-row strong{color:#1e293b;font-weight:650}.customer-row>span:first-child{flex:1;min-width:0}.customer-row>span:first-child strong{overflow-wrap:anywhere}.transaction-row{margin-top:3px;color:#64748b;font-family:ui-monospace,SFMono-Regular,Menlo,monospace;font-size:12px;overflow-wrap:anywhere}
.receipt-grid{display:grid;grid-template-columns:minmax(0,5fr) minmax(28px,1fr) minmax(70px,3fr) minmax(82px,3fr);column-gap:8px;align-items:start}.items-header{color:#0f172a;font-size:12px;line-height:1.2}.items-header strong:nth-child(n+2){text-align:right}.receipt-item{padding:5px 0}.item-main-row{font-size:14px;line-height:1.45;color:#1e293b}.item-main-row>*:nth-child(n+2){text-align:right}.item-name{font-weight:600;overflow-wrap:anywhere}.item-quantity{font-weight:600}.item-unit-price,.item-line-total{font-family:ui-monospace,SFMono-Regular,Menlo,monospace;font-size:13px;white-space:nowrap}.item-unit-price{color:#475569}.item-line-total{font-weight:600}.item-subline{padding:1px 0 0 4px;color:#64748b;font-size:12px;line-height:1.45;overflow-wrap:anywhere}.item-note{font-style:italic;color:#475569}.empty-items{padding:12px 0;text-align:center;color:#94a3b8;font-size:13px}
.price-row{display:flex;justify-content:space-between;gap:20px;padding:3px 0;color:#334155;font-size:14px;line-height:1.45}.price-row strong{color:#1e293b;font-family:ui-monospace,SFMono-Regular,Menlo,monospace;font-size:13px;font-weight:600;white-space:nowrap}.double-rule{height:5px;margin:5px 0;border-top:1px solid #0f172a;border-bottom:1px solid #0f172a}.total-row{display:flex;justify-content:space-between;gap:18px;padding:3px 0;color:#0f172a;font-size:19px;line-height:1.25}.total-row strong:last-child{font-family:ui-monospace,SFMono-Regular,Menlo,monospace;white-space:nowrap}.loyalty-row{display:flex;justify-content:space-between;gap:12px;margin:10px 0 0;color:#475569;font-size:13px;font-weight:500}.loyalty-row span:last-child{text-align:right}.receipt-footer{text-align:center;color:#64748b;font-size:12px;line-height:1.45}.receipt-footer p{margin:0}.receipt-footer strong{display:block;margin-top:4px;color:#1e293b;font-size:14px;font-weight:700}.barcode{margin-top:8px!important;letter-spacing:2px;color:#475569}
@media(max-width:560px){.receipt-overlay{padding:0}.receipt-dialog{width:100%;height:100%;max-height:none;border:0;border-radius:0}.receipt-scroll-area{padding:10px}.receipt-paper{padding:20px 14px 24px;border-radius:8px}.receipt-toolbar{padding:10px}.receipt-action span{display:none}.restaurant-heading h1{font-size:21px}.receipt-paper h2{font-size:18px}.meta-row{gap:10px;font-size:12px}.receipt-grid{column-gap:5px}.item-main-row{font-size:13px}.item-unit-price,.item-line-total{font-size:12px}.total-row{font-size:17px}}
.receipt-paper.receipt-print-layout { box-sizing:border-box; width:80mm; max-width:none; margin:0; padding:5mm 4mm; border:0; border-radius:0; box-shadow:none; }
.receipt-print-layout .receipt-rule { margin:4mm 0; }
.receipt-print-layout h2 { font-size:16px; }
.receipt-print-layout .restaurant-heading h1 { font-size:19px; }
.receipt-print-layout .restaurant-heading p,.receipt-print-layout .receipt-footer,.receipt-print-layout .receipt-meta,.receipt-print-layout .price-row { font-size:10px; }
.receipt-print-layout .meta-row { gap:10px; font-size:10px; }
.receipt-print-layout .receipt-grid { column-gap:5px; }
.receipt-print-layout .item-main-row { font-size:11px; }
.receipt-print-layout .item-unit-price,.receipt-print-layout .item-line-total { font-size:10px; }
.receipt-print-layout .item-subline { font-size:9px; }
.receipt-print-layout .total-row { font-size:14px; }
.receipt-print-layout .loyalty-row { font-size:10px; }
.receipt-print-layout .barcode { display:none; }
@media print {
  /* Keep the report out of print layout, including the space it occupies. */
  :global(body.printing-invoice-receipt) { margin:0!important; padding:0!important; background:#fff!important; }
  :global(body.printing-invoice-receipt > :not(.invoice-receipt-overlay)) { display:none!important; }
  :global(body.printing-invoice-receipt > .invoice-receipt-overlay),
  :global(body.printing-invoice-receipt > .invoice-receipt-overlay *) { visibility:visible!important; }
  .receipt-overlay { position:static; display:block; padding:0; background:#fff; }
  .receipt-dialog { display:block; width:100%; height:auto; max-height:none; overflow:visible; border:0; border-radius:0; box-shadow:none; background:#fff; }
  .receipt-toolbar { display:none; }
  .receipt-scroll-area { overflow:visible; padding:0; }
  .receipt-paper { box-sizing:border-box; width:80mm; max-width:100%; margin:0; padding:5mm 4mm; background:#fff; border:0; border-radius:0; box-shadow:none; }
  .receipt-rule { margin:4mm 0; }
  .receipt-paper h2 { font-size:16px; }
  .restaurant-heading h1 { font-size:19px; }
  .restaurant-heading p,.receipt-footer,.receipt-meta,.price-row { font-size:10px; }
  .item-main-row { font-size:11px; }
  .item-unit-price,.item-line-total { font-size:10px; }
  .item-subline { font-size:9px; }
  .total-row { font-size:14px; }
  .loyalty-row { font-size:10px; }
  .receipt-item,.receipt-meta,.receipt-totals,.receipt-footer,.loyalty-row { break-inside:avoid; }
  .barcode { display:none; }
}
</style>

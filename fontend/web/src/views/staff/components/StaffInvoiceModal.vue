<template>
  <div v-if="show" class="invoice-overlay" @click.self="$emit('close')">
    <section class="invoice-dialog" aria-label="Hóa đơn thanh toán">
      <header class="invoice-toolbar">
        <div>
          <span>Hóa đơn</span>
          <h2>Thanh toán hoàn tất</h2>
        </div>
        <button class="close-button" type="button" title="Đóng" @click="$emit('close')">×</button>
      </header>

      <div id="printableInvoice" class="invoice-paper">
        <div class="invoice-heading">
          <strong>DOLA RESTAURANT</strong>
          <span>HÓA ĐƠN THANH TOÁN</span>
          <small>Đã thanh toán</small>
        </div>

        <div class="invoice-info">
          <p>
            <span>Bàn:</span>
            <strong>{{ tableDisplay || currentInvoice.tableDisplay || `Bàn ${currentInvoice.orderId || '—'}` }}</strong>
          </p>
          <p>
            <span>Đơn:</span>
            <strong>#{{ currentInvoice.invoiceCode || currentInvoice.invoiceId || currentInvoice.orderId || '—' }}</strong>
            <span>Thời gian:</span>
            <strong>{{ formatDateTime(currentInvoice.issuedAt || currentInvoice.createdAt) }}</strong>
          </p>
          <p>
            <span>Thu ngân:</span>
            <strong>{{ cashierName || currentInvoice.cashierName || 'Nhân viên' }}</strong>
          </p>
          <p v-if="customerName">
            <span>Khách hàng:</span>
            <strong>{{ customerName }}</strong>
            <small v-if="customerPhone">{{ customerPhone }}</small>
          </p>
          <p>
            <span>Thanh toán:</span>
            <strong>{{ getPaymentMethod(resolvedPaymentMethod) }}</strong>
          </p>
        </div>

        <div class="invoice-items">
          <div class="items-head">
            <span>Món</span>
            <span>SL</span>
            <span>Đơn giá</span>
            <span>Thành tiền</span>
          </div>
          <div v-for="(item, index) in invoiceItems" :key="item.invoiceItemId || item.orderItemId || index" class="item-row">
            <div class="item-name">
              <strong>{{ item.foodNameSnapshot || item.foodName || 'Món ăn' }}</strong>
              <small v-if="item.variantSnapshot && item.variantSnapshot !== 'Mặc định'">{{ item.variantSnapshot }}</small>
              <em v-if="item.note">{{ item.note }}</em>
            </div>
            <span class="item-quantity">{{ item.quantity || 1 }}</span>
            <span class="item-price">{{ formatCurrency(item.unitPrice) }}</span>
            <strong class="item-total">{{ formatCurrency(item.lineTotal || (item.unitPrice * item.quantity)) }}</strong>
          </div>
          <div v-if="invoiceItems.length === 0" class="items-empty">Chưa có chi tiết món ăn.</div>
        </div>

        <div class="invoice-summary">
          <div><span>Tạm tính</span><strong>{{ formatCurrency(currentInvoice.subtotal) }}</strong></div>
          <div v-if="voucherDiscountAmount > 0" class="discount"><span>Voucher</span><strong>-{{ formatCurrency(voucherDiscountAmount) }}</strong></div>
          <div v-if="pointDiscountAmount > 0" class="discount"><span>Trừ điểm ({{ currentInvoice.pointsUsed }} điểm)</span><strong>-{{ formatCurrency(pointDiscountAmount) }}</strong></div>
          <div v-if="totalDiscountWithoutDetail > 0" class="discount"><span>Giảm giá khác</span><strong>-{{ formatCurrency(totalDiscountWithoutDetail) }}</strong></div>
          <div class="summary-total"><span>Tổng tiền</span><strong>{{ formatCurrency(finalTotalAmount) }}</strong></div>
          <div v-if="currentInvoice.amountPaidByCustomer > 0" class="summary-minor"><span>Khách đưa</span><strong>{{ formatCurrency(currentInvoice.amountPaidByCustomer) }}</strong></div>
          <div v-if="changeReturnAmount > 0" class="summary-minor"><span>Tiền thừa trả khách</span><strong class="positive">{{ formatCurrency(changeReturnAmount) }}</strong></div>
        </div>

        <div class="invoice-note">* Quý khách vui lòng kiểm tra lại món và số tiền trước khi rời nhà hàng.</div>
        <footer class="invoice-footer">Cảm ơn quý khách và hẹn gặp lại.</footer>
      </div>

      <footer class="invoice-actions">
        <button type="button" class="secondary-button" @click="$emit('close')">Đóng</button>
        <button type="button" class="primary-button" @click="handlePrint"><span aria-hidden="true">↗</span> In phiếu</button>
      </footer>
    </section>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import axios from 'axios';

const props = defineProps({
  show: Boolean,
  invoice: Object,
  tableDisplay: String,
  cashierName: String
});

defineEmits(['close']);

const paymentApiBase = 'http://localhost:5079/api/staff/payment';
const fetchedInvoiceDetail = ref(null);

watch(
  () => [props.show, props.invoice?.invoiceId],
  async ([isVisible, invoiceId]) => {
    fetchedInvoiceDetail.value = null;
    if (!isVisible || !invoiceId) return;

    if (!props.invoice?.items?.length && !props.invoice?.invoiceItems?.length) {
      try {
        const response = await axios.get(`${paymentApiBase}/invoices/${invoiceId}`);
        fetchedInvoiceDetail.value = response.data;
      } catch (error) {
        console.warn('Không thể tải chi tiết hóa đơn:', error);
      }
    }
  }
);

const currentInvoice = computed(() => fetchedInvoiceDetail.value || props.invoice || {});
const invoiceItems = computed(() => currentInvoice.value.items || currentInvoice.value.invoiceItems || []);
const customerName = computed(() => currentInvoice.value.customerName || currentInvoice.value.customer?.fullName || '');
const customerPhone = computed(() => currentInvoice.value.customerPhone || currentInvoice.value.customer?.phone || '');
const resolvedPaymentMethod = computed(() => currentInvoice.value.paymentMethod || currentInvoice.value.payments?.[0]?.paymentMethod || 'cash');
const voucherDiscountAmount = computed(() => currentInvoice.value.voucherDiscount || 0);
const pointDiscountAmount = computed(() => {
  const invoice = currentInvoice.value;
  if (invoice.pointDiscount) return invoice.pointDiscount;
  if (invoice.pointsUsed && !invoice.voucherDiscount) return invoice.pointsUsed * 1000;
  return 0;
});
const totalDiscountWithoutDetail = computed(() => {
  const total = currentInvoice.value.discountAmount || 0;
  const known = voucherDiscountAmount.value + pointDiscountAmount.value;
  return total > known ? total - known : 0;
});
const finalTotalAmount = computed(() => currentInvoice.value.totalAmount || currentInvoice.value.finalAmount || currentInvoice.value.subtotal || 0);
const changeReturnAmount = computed(() => {
  const invoice = currentInvoice.value;
  const paid = invoice.amountPaidByCustomer || 0;
  return paid > finalTotalAmount.value ? paid - finalTotalAmount.value : (invoice.changeToReturn || 0);
});

function formatCurrency(value) {
  if (!value && value !== 0) return '0 đ';
  return `${new Intl.NumberFormat('vi-VN').format(value)} đ`;
}

function formatDateTime(value) {
  if (!value) return new Date().toLocaleString('vi-VN', { hour12: false });
  return new Date(value).toLocaleString('vi-VN', { hour12: false });
}

function getPaymentMethod(method) {
  return {
    cash: 'Tiền mặt',
    transfer: 'Chuyển khoản',
    card: 'Thẻ ngân hàng',
    e_wallet: 'Ví điện tử'
  }[method] || method || 'Tiền mặt';
}

function handlePrint() {
  window.print();
}
</script>

<style scoped>
.invoice-overlay { position: fixed; inset: 0; z-index: 1100; display: grid; place-items: center; padding: 16px; overflow-y: auto; background: rgba(20, 27, 25, .58); backdrop-filter: blur(3px); }
.invoice-dialog { width: min(650px, 100%); max-height: 94vh; overflow: hidden; border: 1px solid #d8dfdc; border-radius: 10px; background: #fff; box-shadow: 0 20px 60px rgba(0, 0, 0, .22); }
.invoice-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 14px 20px; border-bottom: 1px solid #e3e8e5; color: #23342f; background: #fff; }.invoice-toolbar span { color: #6e7b76; font-size: .65rem; font-weight: 800; letter-spacing: .1em; text-transform: uppercase; }.invoice-toolbar h2 { margin: 3px 0 0; font-size: 1rem; }.close-button { display: grid; width: 32px; height: 32px; place-items: center; border: 1px solid #d4dcd7; border-radius: 6px; color: #5e6b65; background: #fff; font-size: 1.35rem; line-height: 1; cursor: pointer; }.close-button:hover { background: #f3f6f4; }
.invoice-paper { max-height: calc(94vh - 122px); overflow-y: auto; padding: 27px 30px 20px; color: #292f2d; background: #fff; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace; }.invoice-heading { display: grid; justify-items: center; gap: 5px; padding-bottom: 17px; border-bottom: 1px solid #d9dfdc; text-align: center; }.invoice-heading strong { color: #292f2d; font-family: Inter, ui-sans-serif, system-ui, sans-serif; font-size: 1.2rem; }.invoice-heading span { color: #606b67; font-size: .8rem; }.invoice-heading small { color: #26774c; font-size: .68rem; font-weight: 800; text-transform: uppercase; }
.invoice-info { display: grid; gap: 5px; padding: 13px 0 17px; border-bottom: 1px solid #d9dfdc; }.invoice-info p { display: flex; flex-wrap: wrap; justify-content: center; gap: 0 7px; margin: 0; color: #656f6b; font-size: .76rem; line-height: 1.45; }.invoice-info p span { color: #707a76; }.invoice-info p strong { color: #303836; }.invoice-info p small { color: #707a76; }
.invoice-items { padding-top: 19px; }.items-head, .item-row { display: grid; grid-template-columns: minmax(0, 1fr) 34px 90px 106px; gap: 9px; align-items: start; }.items-head { padding-bottom: 8px; border-bottom: 1px solid #303735; color: #4e5954; font-size: .68rem; font-weight: 800; text-transform: uppercase; }.items-head span:not(:first-child), .item-quantity, .item-price, .item-total { text-align: right; }.item-row { padding: 11px 0; border-bottom: 1px solid #e0e4e2; color: #303735; font-size: .77rem; }.item-name { min-width: 0; }.item-name strong { display: block; overflow-wrap: anywhere; color: #2c3330; }.item-name small, .item-name em { display: block; margin-top: 3px; color: #6f7974; font-size: .68rem; font-style: normal; }.item-name em { color: #956a35; }.item-quantity, .item-price { color: #606b66; }.item-total { color: #292f2d; white-space: nowrap; }.items-empty { padding: 24px 0; color: #707a76; text-align: center; }
.invoice-summary { display: grid; gap: 7px; margin-top: 18px; padding-top: 13px; border-top: 1px solid #d9dfdc; font-size: .78rem; }.invoice-summary > div { display: flex; justify-content: space-between; gap: 18px; }.invoice-summary .discount strong { color: #ae5151; }.summary-total { align-items: center; margin-top: 5px; padding-top: 12px; border-top: 2px solid #303735; color: #292f2d; font-size: 1rem; font-weight: 800; text-transform: uppercase; }.summary-total strong { color: #db3f50; font-size: 1.25rem; white-space: nowrap; }.summary-minor { color: #6c7772; font-size: .72rem; }.summary-minor strong { color: #3a4641; }.summary-minor .positive { color: #26774c; }.invoice-note { margin-top: 24px; color: #636d68; font-size: .72rem; line-height: 1.5; text-align: center; }.invoice-footer { margin-top: 20px; padding-top: 13px; border-top: 1px solid #d9dfdc; color: #636d68; font-size: .72rem; text-align: center; }
.invoice-actions { display: flex; justify-content: flex-end; gap: 10px; padding: 14px 20px; border-top: 1px solid #e3e8e5; }.secondary-button, .primary-button { min-height: 38px; padding: 8px 17px; border-radius: 6px; font: inherit; font-size: .76rem; font-weight: 800; cursor: pointer; }.secondary-button { border: 1px solid #cfd8d3; color: #596761; background: #fff; }.secondary-button:hover { background: #f3f6f4; }.primary-button { border: 1px solid #176b55; color: #fff; background: #176b55; }.primary-button:hover { background: #125a47; }.primary-button span { margin-right: 4px; }
@media (max-width: 560px) { .invoice-overlay { padding: 8px; }.invoice-toolbar { padding: 13px 15px; }.invoice-paper { max-height: calc(97vh - 113px); padding: 22px 16px 17px; }.invoice-info p { justify-content: flex-start; }.items-head, .item-row { grid-template-columns: minmax(0, 1fr) 28px 78px; gap: 6px; }.items-head span:nth-child(3), .item-price { display: none; }.invoice-actions { padding: 12px 14px; }.secondary-button, .primary-button { flex: 1; } }
@media print { :global(body *) { visibility: hidden !important; }.invoice-overlay, .invoice-paper, .invoice-paper * { visibility: visible !important; }.invoice-overlay { position: absolute; display: block; inset: 0; padding: 0; background: #fff; }.invoice-dialog { width: 80mm; max-height: none; overflow: visible; border: 0; border-radius: 0; box-shadow: none; }.invoice-toolbar, .invoice-actions { display: none !important; }.invoice-paper { width: 80mm; max-height: none; overflow: visible; padding: 4mm 3.5mm; font-family: Arial, sans-serif; }.invoice-heading strong { font-size: 12pt; }.invoice-heading span { font-size: 8pt; }.invoice-info { padding: 8px 0 10px; }.invoice-info p { font-size: 7pt; }.items-head, .item-row { grid-template-columns: minmax(0, 1fr) 20px 62px; gap: 4px; }.items-head span:nth-child(3), .item-price { display: none; }.items-head { font-size: 7pt; }.item-row { padding: 6px 0; font-size: 8pt; }.item-name small, .item-name em { font-size: 7pt; }.invoice-summary { margin-top: 10px; padding-top: 8px; font-size: 8pt; }.summary-total { font-size: 9pt; }.summary-total strong { font-size: 11pt; }.invoice-note, .invoice-footer { font-size: 7pt; } }
</style>

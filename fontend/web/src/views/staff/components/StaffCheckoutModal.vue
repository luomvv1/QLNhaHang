<template>
  <div v-if="show" class="checkout-overlay" @click.self="$emit('close')">
    <section class="checkout-dialog" aria-label="Thanh toán hóa đơn">
      <header class="checkout-header">
        <div>
          <span class="checkout-eyebrow">Thanh toán</span>
          <h2>{{ tableDisplay || tableName || 'Đơn tại bàn' }}</h2>
        </div>
        <button class="close-button" type="button" title="Đóng" @click="$emit('close')">×</button>
      </header>

      <div v-if="loading" class="checkout-state">
        <div class="state-spinner" aria-hidden="true"></div>
        <strong>Đang tải thông tin hóa đơn</strong>
        <span>Vui lòng chờ trong giây lát.</span>
      </div>
      <div v-else-if="!checkoutData" class="checkout-state checkout-state-error">
        <strong>Không thể tải thông tin thanh toán.</strong>
        <span>Vui lòng đóng cửa sổ và thử lại.</span>
      </div>

      <div v-else class="checkout-content">
        <section class="order-panel">
          <div class="panel-heading">
            <div>
              <span class="section-kicker">Phiếu tạm tính</span>
              <h3>#{{ checkoutData.orderCode || checkoutData.orderId }}</h3>
            </div>
            <span class="unpaid-badge">Chưa thanh toán</span>
          </div>

          <div class="order-meta">
            <span><b>Bàn:</b> {{ tableDisplay || tableName || '—' }}</span>
            <span v-if="userName"><b>Phục vụ:</b> {{ userName }}</span>
          </div>

          <div class="order-items">
            <div class="item-heading">
              <span>Món</span>
              <span>SL</span>
              <span>Đơn giá</span>
              <span>Thành tiền</span>
            </div>
            <div v-for="item in checkoutData.items" :key="item.orderItemId || item.invoiceItemId" class="order-item">
              <div class="item-name">
                <strong>{{ item.foodNameSnapshot || 'Món ăn' }}</strong>
                <small v-if="item.variantSnapshot">{{ item.variantSnapshot }}</small>
                <em v-if="item.note">{{ item.note }}</em>
              </div>
              <span class="item-quantity">{{ item.quantity }}</span>
              <span class="item-price">{{ formatCurrency(item.unitPrice) }}</span>
              <strong class="item-total">{{ formatCurrency(item.quantity * item.unitPrice) }}</strong>
            </div>
            <div v-if="!checkoutData.items?.length" class="empty-items">Chưa có món ăn trong hóa đơn.</div>
          </div>

          <div class="order-total">
            <div class="summary-line">
              <span>Tạm tính</span>
              <strong>{{ formatCurrency(checkoutData.subtotal) }}</strong>
            </div>
            <div v-if="checkoutData.voucherDiscount > 0" class="summary-line discount-line">
              <span>Voucher</span>
              <strong>-{{ formatCurrency(checkoutData.voucherDiscount) }}</strong>
            </div>
            <div v-if="checkoutData.pointsDiscount > 0" class="summary-line discount-line">
              <span>Trừ điểm thành viên</span>
              <strong>-{{ formatCurrency(checkoutData.pointsDiscount) }}</strong>
            </div>

            <div class="voucher-row">
              <input v-model="voucherCode" type="text" placeholder="Mã voucher" :disabled="checkoutData.voucherDiscount > 0" @keyup.enter="applyVoucher">
              <button v-if="checkoutData.voucherDiscount === 0" type="button" @click="applyVoucher" :disabled="!voucherCode">Áp dụng</button>
              <button v-else type="button" class="remove-button" @click="removeVoucher">Bỏ mã</button>
            </div>
            <p v-if="voucherMessage" class="form-message" :class="voucherSuccess ? 'success-message' : 'error-message'">{{ voucherMessage }}</p>

            <div class="total-line">
              <span>Tổng phải trả</span>
              <strong>{{ formatCurrency(checkoutData.finalAmount) }}</strong>
            </div>
            <div v-if="checkoutData.estimatedEarnPoints > 0 && !checkoutData.pointsDiscount" class="earned-points">
              Dự kiến nhận {{ checkoutData.estimatedEarnPoints }} điểm
            </div>
          </div>
        </section>

        <section class="payment-panel">
          <div class="customer-section">
            <div class="panel-title-row">
              <div>
                <span class="section-kicker">Thông tin khách</span>
                <h3>Khách hàng / Thành viên</h3>
              </div>
              <span v-if="checkoutData.customer" class="member-badge">Đã gán</span>
            </div>

            <div v-if="!checkoutData.customer" class="customer-search">
              <div class="search-row">
                <input v-model="searchPhone" type="tel" placeholder="Tìm theo số điện thoại" @keyup.enter="searchCustomer">
                <button type="button" class="search-button" :disabled="searchingCustomer" @click="searchCustomer">
                  {{ searchingCustomer ? '...' : 'Tìm' }}
                </button>
              </div>
              <p v-if="searchCustomerMsg" class="form-message error-message">{{ searchCustomerMsg }}</p>
              <div v-if="customerResults.length" class="customer-results">
                <button v-for="customer in customerResults" :key="customer.customerId" type="button" class="customer-result" @click="selectCustomer(customer)">
                  <span>{{ customer.fullName }}</span>
                  <small>{{ customer.phone }} · {{ customer.pointsBalance }} điểm</small>
                </button>
              </div>
              <div v-if="searchPhone && customerResults.length === 0 && searchCustomerMsg" class="quick-create">
                <span>Chưa có khách hàng này?</span>
                <div class="quick-create-row">
                  <input v-model="newCustomerName" type="text" placeholder="Tên khách hàng">
                  <button type="button" @click="quickCreateCustomer" :disabled="!newCustomerName || creatingCustomer">Thêm</button>
                </div>
              </div>
            </div>

            <div v-else class="selected-customer">
              <div class="customer-avatar">{{ checkoutData.customer.fullName?.charAt(0) || 'K' }}</div>
              <div class="selected-customer-info">
                <strong>{{ checkoutData.customer.fullName }}</strong>
                <span>{{ checkoutData.customer.phone }}</span>
              </div>
              <button type="button" class="unlink-button" title="Bỏ gán khách hàng" @click="removeCustomer">Bỏ</button>
              <div class="points-balance">
                <span>Điểm hiện có</span>
                <strong>{{ checkoutData.customer.pointsBalance }}</strong>
              </div>
            </div>

            <div v-if="checkoutData.customer && checkoutData.customer.pointsBalance > 0" class="points-form">
              <label for="pointsToUse">Dùng điểm <small>1 điểm = {{ checkoutData.pointConversionRate }} đ</small></label>
              <div class="points-input-row">
                <input id="pointsToUse" v-model.number="pointsToUse" type="number" min="0" :max="maxPointsCanUse" placeholder="0">
                <button type="button" @click="applyPoints" :disabled="!pointsToUse || pointsToUse <= 0 || pointsToUse > maxPointsCanUse">Đổi điểm</button>
              </div>
              <small class="field-hint">Có thể dùng tối đa {{ maxPointsCanUse }} điểm cho hóa đơn này.</small>
            </div>
          </div>

          <div class="payment-section">
            <div class="panel-title-row">
              <div>
                <span class="section-kicker">Bước cuối</span>
                <h3>Phương thức thanh toán</h3>
              </div>
            </div>

            <div class="payment-methods">
              <label class="method-option" :class="{ selected: paymentMethod === 'cash' }">
                <input v-model="paymentMethod" type="radio" value="cash">
                <span class="method-icon">₫</span>
                <span>Tiền mặt</span>
              </label>
              <label class="method-option" :class="{ selected: paymentMethod === 'transfer' }">
                <input v-model="paymentMethod" type="radio" value="transfer">
                <span class="method-icon">QR</span>
                <span>Chuyển khoản</span>
              </label>
              <label class="method-option" :class="{ selected: paymentMethod === 'card' }">
                <input v-model="paymentMethod" type="radio" value="card">
                <span class="method-icon">CARD</span>
                <span>Thẻ ngân hàng</span>
              </label>
              <label class="method-option" :class="{ selected: paymentMethod === 'e_wallet' }">
                <input v-model="paymentMethod" type="radio" value="e_wallet">
                <span class="method-icon">Ví</span>
                <span>Ví điện tử</span>
              </label>
            </div>

            <div v-if="paymentMethod === 'transfer' && checkoutData.vietQr" class="qr-box">
              <span class="qr-title">Quét mã VietQR</span>
              <img :src="`https://img.vietqr.io/image/${checkoutData.vietQr.bankId}-${checkoutData.vietQr.accountNo}-compact2.jpg?amount=${checkoutData.finalAmount}&addInfo=${checkoutData.vietQr.memo}&accountName=${checkoutData.vietQr.accountName}`" alt="Mã QR thanh toán">
              <small>{{ checkoutData.vietQr.bankId }} · {{ checkoutData.vietQr.accountNo }}</small>
            </div>

            <div v-if="paymentMethod === 'cash'" class="cash-field">
              <label for="amountPaid">Khách đưa</label>
              <div class="amount-input">
                <input id="amountPaid" v-model.number="amountPaid" type="number" min="0">
                <span>đ</span>
              </div>
              <div v-if="amountPaid >= checkoutData.finalAmount" class="change-row">
                <span>Tiền thừa trả khách</span>
                <strong>{{ formatCurrency(amountPaid - checkoutData.finalAmount) }}</strong>
              </div>
              <div v-else-if="amountPaid > 0" class="shortage-message">Còn thiếu {{ formatCurrency(checkoutData.finalAmount - amountPaid) }}</div>
            </div>

            <div class="payable-row">
              <span>Tổng thanh toán</span>
              <strong>{{ formatCurrency(checkoutData.finalAmount) }}</strong>
            </div>
            <button type="button" class="complete-button" :disabled="isSubmitting || (paymentMethod === 'cash' && amountPaid < checkoutData.finalAmount)" @click="processPayment">
              <span v-if="isSubmitting" class="button-spinner"></span>
              {{ isSubmitting ? 'Đang xử lý...' : 'Hoàn tất thanh toán' }}
            </button>
          </div>
        </section>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import axios from 'axios';

const props = defineProps({
  show: Boolean,
  orderId: Number,
  tableName: String,
  tableDisplay: String,
  userName: String
});

const emit = defineEmits(['close', 'success']);
const paymentApiBase = 'http://localhost:5079/api/staff/payment';

const loading = ref(false);
const isSubmitting = ref(false);
const checkoutData = ref(null);
const paymentMethod = ref('cash');
const amountPaid = ref(0);
const searchPhone = ref('');
const customerResults = ref([]);
const searchingCustomer = ref(false);
const searchCustomerMsg = ref('');
const newCustomerName = ref('');
const creatingCustomer = ref(false);
const pointsToUse = ref(0);
const voucherCode = ref('');
const voucherMessage = ref('');
const voucherSuccess = ref(false);

const maxPointsCanUse = computed(() => {
  if (!checkoutData.value || !checkoutData.value.customer) return 0;
  const remainingNeedToPay = checkoutData.value.subtotal - checkoutData.value.voucherDiscount;
  if (remainingNeedToPay <= 0) return 0;
  const maxPoints = Math.floor(remainingNeedToPay / checkoutData.value.pointConversionRate);
  return Math.min(maxPoints, checkoutData.value.customer.pointsBalance);
});

watch(() => props.show, (val) => {
  if (val && props.orderId) loadCheckoutData();
});

async function loadCheckoutData() {
  loading.value = true;
  checkoutData.value = null;
  resetForms();
  try {
    const res = await axios.get(`${paymentApiBase}/checkout-preview/${props.orderId}`);
    checkoutData.value = res.data;
    checkoutData.value.voucherDiscount = 0;
    checkoutData.value.pointsDiscount = 0;
    checkoutData.value.finalAmount = checkoutData.value.subtotal;
    amountPaid.value = checkoutData.value.finalAmount;
  } catch (err) {
    console.error(err);
    alert('Không thể tải thông tin thanh toán');
    emit('close');
  } finally {
    loading.value = false;
  }
}

function resetForms() {
  voucherCode.value = '';
  voucherMessage.value = '';
  voucherSuccess.value = false;
  amountPaid.value = 0;
  searchPhone.value = '';
  customerResults.value = [];
  searchCustomerMsg.value = '';
  newCustomerName.value = '';
  pointsToUse.value = 0;
  paymentMethod.value = 'cash';
}

function updateFinalAmount() {
  let finalAmt = checkoutData.value.subtotal - checkoutData.value.voucherDiscount - checkoutData.value.pointsDiscount;
  if (finalAmt < 0) finalAmt = 0;
  checkoutData.value.finalAmount = finalAmt;
  if (paymentMethod.value !== 'cash') amountPaid.value = finalAmt;
}

watch(paymentMethod, (newVal) => {
  if (checkoutData.value && newVal !== 'cash') amountPaid.value = checkoutData.value.finalAmount;
});

async function applyVoucher() {
  if (!voucherCode.value) return;
  try {
    const res = await axios.post(`${paymentApiBase}/validate-voucher`, {
      voucherCode: voucherCode.value,
      subtotal: checkoutData.value.subtotal
    });
    const discount = res.data.discountAmount || 0;
    checkoutData.value.voucherDiscount = discount;
    updateFinalAmount();
    voucherSuccess.value = true;
    voucherMessage.value = `Áp dụng thành công! Giảm ${formatCurrency(discount)}`;
  } catch (err) {
    voucherSuccess.value = false;
    voucherMessage.value = err.response?.data?.message || 'Mã không hợp lệ';
  }
}

function removeVoucher() {
  checkoutData.value.voucherDiscount = 0;
  voucherCode.value = '';
  voucherSuccess.value = false;
  voucherMessage.value = '';
  updateFinalAmount();
}

async function searchCustomer() {
  if (!searchPhone.value) return;
  searchingCustomer.value = true;
  customerResults.value = [];
  searchCustomerMsg.value = '';
  try {
    const res = await axios.get(`${paymentApiBase}/customers/search?phone=${searchPhone.value}`);
    customerResults.value = res.data;
    if (customerResults.value.length === 0) searchCustomerMsg.value = 'Không tìm thấy khách hàng nào.';
  } catch (err) {
    searchCustomerMsg.value = 'Lỗi tìm kiếm.';
  } finally {
    searchingCustomer.value = false;
  }
}

async function quickCreateCustomer() {
  if (!newCustomerName.value || !searchPhone.value) return;
  creatingCustomer.value = true;
  try {
    const res = await axios.post(`${paymentApiBase}/customers/quick-create`, {
      fullName: newCustomerName.value,
      phone: searchPhone.value
    });
    selectCustomer(res.data);
  } catch (err) {
    alert(err.response?.data?.message || 'Lỗi thêm khách');
  } finally {
    creatingCustomer.value = false;
  }
}

function selectCustomer(customer) {
  checkoutData.value.customer = customer;
  searchPhone.value = '';
  customerResults.value = [];
  searchCustomerMsg.value = '';
  newCustomerName.value = '';
}

function removeCustomer() {
  checkoutData.value.customer = null;
  removePoints();
}

function applyPoints() {
  if (pointsToUse.value > maxPointsCanUse.value) {
    alert(`Chỉ có thể dùng tối đa ${maxPointsCanUse.value} điểm.`);
    return;
  }
  checkoutData.value.pointsDiscount = pointsToUse.value * checkoutData.value.pointConversionRate;
  updateFinalAmount();
}

function removePoints() {
  pointsToUse.value = 0;
  checkoutData.value.pointsDiscount = 0;
  updateFinalAmount();
}

async function processPayment() {
  if (!checkoutData.value) return;
  isSubmitting.value = true;
  try {
    const payload = {
      orderId: props.orderId,
      customerId: checkoutData.value.customer?.customerId || null,
      promotionCode: voucherSuccess.value ? voucherCode.value : null,
      pointsToUse: checkoutData.value.pointsDiscount > 0 ? pointsToUse.value : 0,
      paymentMethod: paymentMethod.value,
      amountPaidByCustomer: amountPaid.value,
      transactionCode: null
    };
    const res = await axios.post(`${paymentApiBase}/checkout`, payload);
    emit('success', res.data.invoice);
  } catch (err) {
    alert(err.response?.data?.message || 'Lỗi thanh toán');
  } finally {
    isSubmitting.value = false;
  }
}

function formatCurrency(val) {
  if (!val && val !== 0) return '0 đ';
  return `${new Intl.NumberFormat('vi-VN').format(val)} đ`;
}
</script>

<style scoped>
.checkout-overlay { position: fixed; inset: 0; z-index: 1100; display: grid; place-items: center; padding: 18px; overflow-y: auto; background: rgba(17, 26, 23, .62); backdrop-filter: blur(4px); }
.checkout-dialog { width: min(1040px, 100%); max-height: 94vh; overflow: hidden; border: 1px solid #dbe4df; border-radius: 12px; background: #fff; box-shadow: 0 22px 60px rgba(0, 0, 0, .22); }
.checkout-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 17px 24px; color: #fff; background: #176b55; }.checkout-header h2 { margin: 3px 0 0; font-size: 1.16rem; font-weight: 800; }.checkout-eyebrow, .section-kicker { display: block; color: #b9e4d2; font-size: .66rem; font-weight: 800; letter-spacing: .1em; text-transform: uppercase; }.close-button { display: grid; width: 34px; height: 34px; place-items: center; border: 1px solid rgba(255,255,255,.35); border-radius: 7px; color: #fff; background: transparent; font-size: 1.5rem; line-height: 1; cursor: pointer; }.close-button:hover { background: rgba(255,255,255,.12); }
.checkout-content { display: grid; grid-template-columns: minmax(0, 1.02fr) minmax(390px, .98fr); max-height: calc(94vh - 70px); overflow: hidden; }.order-panel, .payment-panel { min-width: 0; overflow-y: auto; }.order-panel { padding: 26px 28px; background: #f7f9f8; }.payment-panel { padding: 26px 28px; background: #fff; }
.panel-heading, .panel-title-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 14px; }.panel-heading { padding-bottom: 14px; border-bottom: 1px solid #dbe5df; }.panel-heading h3, .panel-title-row h3 { margin: 4px 0 0; color: #20352f; font-size: 1rem; }.unpaid-badge, .member-badge { padding: 5px 8px; border-radius: 5px; color: #9a651d; background: #fff5df; font-size: .67rem; font-weight: 800; white-space: nowrap; }.member-badge { color: #26774c; background: #eaf7ef; }
.order-meta { display: flex; flex-wrap: wrap; gap: 6px 22px; padding: 13px 0 18px; color: #63736d; font-size: .77rem; }.order-meta b { color: #2e4740; }.order-items { min-height: 170px; }.item-heading, .order-item { display: grid; grid-template-columns: minmax(0, 1fr) 34px 90px 102px; gap: 10px; align-items: start; }.item-heading { padding-bottom: 8px; border-bottom: 1px solid #2f5145; color: #718079; font-size: .64rem; font-weight: 800; text-transform: uppercase; }.item-heading span:not(:first-child), .item-quantity, .item-price, .item-total { text-align: right; }.order-item { padding: 11px 0; border-bottom: 1px solid #e0e8e4; color: #2c413a; font-size: .79rem; }.item-name { min-width: 0; }.item-name strong { display: block; overflow-wrap: anywhere; color: #1e3930; }.item-name small, .item-name em { display: block; margin-top: 3px; color: #718079; font-size: .68rem; font-style: normal; }.item-name em { color: #9b6c2d; }.item-quantity, .item-price { color: #687771; }.item-total { color: #1e3930; white-space: nowrap; }.empty-items { padding: 28px 0; color: #718079; text-align: center; }
.order-total { margin-top: 18px; padding-top: 16px; border-top: 1px solid #d4dfd9; }.summary-line, .total-line, .payable-row, .change-row { display: flex; align-items: center; justify-content: space-between; gap: 16px; }.summary-line { margin-bottom: 7px; color: #687771; font-size: .78rem; }.summary-line strong { color: #2d443c; }.discount-line strong { color: #b25555; }.voucher-row { display: flex; gap: 8px; margin: 15px 0 4px; }.voucher-row input, .voucher-row button, .search-row input, .search-button, .quick-create-row input, .quick-create-row button, .points-input-row input, .points-input-row button { min-height: 35px; border: 1px solid #ccd9d2; border-radius: 6px; font: inherit; font-size: .75rem; }.voucher-row input, .search-row input, .quick-create-row input, .points-input-row input { min-width: 0; padding: 7px 10px; color: #253c34; background: #fff; }.voucher-row input { flex: 1; text-transform: uppercase; }.voucher-row button, .search-button, .quick-create-row button, .points-input-row button { padding: 7px 12px; border-color: #6caf92; color: #176b55; background: #f3fbf6; font-weight: 800; cursor: pointer; }.voucher-row button:disabled, .search-button:disabled, .quick-create-row button:disabled, .points-input-row button:disabled { cursor: not-allowed; opacity: .5; }.remove-button { border-color: #e0b5b5 !important; color: #a34f4f !important; background: #fff7f7 !important; }.form-message { margin: 5px 0 0; font-size: .7rem; }.success-message { color: #27734b; }.error-message { color: #a34f4f; }.total-line { margin-top: 15px; padding-top: 14px; border-top: 2px solid #234f40; color: #243c33; font-size: .96rem; font-weight: 800; text-transform: uppercase; }.total-line strong { color: #db4050; font-size: 1.28rem; white-space: nowrap; }.earned-points { margin-top: 8px; color: #26774c; font-size: .72rem; text-align: right; }
.customer-section, .payment-section { padding-bottom: 22px; border-bottom: 1px solid #e1e9e5; }.payment-section { padding-top: 22px; padding-bottom: 0; border-bottom: 0; }.search-row, .quick-create-row, .points-input-row { display: flex; gap: 8px; }.search-row { margin-top: 15px; }.search-row input, .quick-create-row input { flex: 1; }.search-button { min-width: 52px; }.customer-results { display: grid; gap: 5px; max-height: 150px; margin-top: 9px; overflow-y: auto; }.customer-result { display: grid; gap: 3px; padding: 9px 10px; border: 1px solid #e0e8e4; border-radius: 6px; color: #23463a; background: #fbfdfc; text-align: left; cursor: pointer; }.customer-result:hover { border-color: #72ad90; background: #f0faf4; }.customer-result span { font-size: .76rem; font-weight: 800; }.customer-result small { color: #73827c; font-size: .68rem; }.quick-create { display: grid; gap: 7px; margin-top: 12px; color: #76847e; font-size: .71rem; }.selected-customer { display: grid; grid-template-columns: 40px minmax(0, 1fr) auto; gap: 10px; align-items: center; margin-top: 15px; padding: 11px; border: 1px solid #cfe4d8; border-radius: 8px; background: #f3faf5; }.customer-avatar { display: grid; width: 38px; height: 38px; place-items: center; border-radius: 50%; color: #176b55; background: #d6efdf; font-weight: 800; }.selected-customer-info { display: grid; gap: 3px; min-width: 0; }.selected-customer-info strong { overflow-wrap: anywhere; color: #214a3b; font-size: .8rem; }.selected-customer-info span { color: #6d7c75; font-size: .7rem; }.unlink-button { padding: 4px 7px; border: 1px solid #dfb3b3; border-radius: 5px; color: #a34f4f; background: #fff; font: inherit; font-size: .68rem; cursor: pointer; }.points-balance { grid-column: 2 / -1; display: flex; justify-content: space-between; padding-top: 8px; border-top: 1px solid #dcebe1; color: #718079; font-size: .7rem; }.points-balance strong { color: #23784e; font-size: .85rem; }.points-form { display: grid; gap: 6px; margin-top: 14px; }.points-form label, .cash-field label { display: flex; justify-content: space-between; gap: 10px; color: #334c43; font-size: .72rem; font-weight: 800; }.points-form label small { color: #7a8983; font-weight: 400; }.points-input-row input { flex: 1; }.field-hint { color: #77857f; font-size: .67rem; }
.payment-methods { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 7px; margin-top: 15px; }.method-option { display: grid; min-height: 72px; place-items: center; align-content: center; gap: 5px; padding: 8px 4px; border: 1px solid #d4dfda; border-radius: 7px; color: #54655e; background: #fff; font-size: .68rem; font-weight: 800; text-align: center; cursor: pointer; }.method-option input { position: absolute; opacity: 0; pointer-events: none; }.method-option.selected { border-color: #197855; color: #176b55; background: #edf8f1; box-shadow: inset 0 0 0 1px #197855; }.method-icon { display: grid; min-width: 30px; min-height: 24px; place-items: center; color: #176b55; font-size: .8rem; font-weight: 900; }.method-option.selected .method-icon { color: #197855; }.qr-box { display: grid; justify-items: center; gap: 7px; margin-top: 14px; padding: 12px; border: 1px solid #d8e4de; border-radius: 8px; background: #f8fbf9; }.qr-title { color: #2d4d42; font-size: .72rem; font-weight: 800; }.qr-box img { width: min(170px, 60%); border-radius: 5px; }.qr-box small { color: #718079; font-size: .68rem; }.cash-field { margin-top: 17px; }.amount-input { display: flex; align-items: center; margin-top: 7px; border: 1px solid #cbd9d1; border-radius: 7px; background: #fff; }.amount-input:focus-within { border-color: #4b9a76; box-shadow: 0 0 0 3px rgba(75, 154, 118, .12); }.amount-input input { width: 100%; min-width: 0; padding: 10px 12px; border: 0; outline: 0; color: #176b55; background: transparent; font: inherit; font-size: 1.08rem; font-weight: 800; text-align: right; }.amount-input span { padding-right: 12px; color: #75847d; font-size: .8rem; }.change-row { margin-top: 8px; padding-top: 8px; border-top: 1px solid #e0e8e4; color: #6d7c75; font-size: .73rem; }.change-row strong { color: #26774c; font-size: .88rem; }.shortage-message { margin-top: 7px; color: #b25555; font-size: .72rem; text-align: right; }.payable-row { margin-top: 22px; padding: 13px 0; border-top: 1px solid #dbe5df; color: #2d443b; font-size: .85rem; font-weight: 800; }.payable-row strong { color: #db4050; font-size: 1.25rem; white-space: nowrap; }.complete-button { width: 100%; min-height: 48px; margin-top: 8px; border: 0; border-radius: 7px; color: #fff; background: #176b55; font: inherit; font-size: .88rem; font-weight: 800; cursor: pointer; }.complete-button:hover { background: #125a47; }.complete-button:disabled { cursor: not-allowed; opacity: .55; }.button-spinner, .state-spinner { display: inline-block; width: 15px; height: 15px; margin-right: 7px; border: 2px solid rgba(255,255,255,.45); border-top-color: #fff; border-radius: 50%; animation: spin .7s linear infinite; vertical-align: -2px; }.checkout-state { display: grid; min-height: 360px; place-content: center; justify-items: center; gap: 7px; color: #465e54; font-size: .82rem; }.checkout-state span { color: #7b8983; font-size: .73rem; }.checkout-state-error { color: #a34f4f; }.checkout-state-error .state-spinner { display: none; }
@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 800px) { .checkout-content { grid-template-columns: 1fr; max-height: calc(96vh - 68px); overflow-y: auto; }.order-panel, .payment-panel { overflow: visible; }.payment-panel { border-top: 1px solid #dbe5df; }.payment-methods { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 520px) { .checkout-overlay { padding: 8px; }.checkout-header { padding: 15px 16px; }.checkout-content { max-height: calc(97vh - 64px); }.order-panel, .payment-panel { padding: 20px 16px; }.item-heading, .order-item { grid-template-columns: minmax(0, 1fr) 28px 76px; gap: 6px; }.item-heading span:nth-child(3), .item-price { display: none; }.panel-title-row { align-items: flex-start; }.selected-customer { grid-template-columns: 36px minmax(0, 1fr) auto; }.customer-avatar { width: 34px; height: 34px; }.points-balance { grid-column: 1 / -1; }.total-line, .payable-row { align-items: flex-start; }.total-line strong, .payable-row strong { font-size: 1.08rem; } }
</style>

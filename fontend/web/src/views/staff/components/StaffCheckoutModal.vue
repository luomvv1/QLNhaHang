<template>
  <div v-if="show" class="checkout-overlay" @click.self="$emit('close')">
    <section class="checkout-dialog" aria-label="Thanh toán hóa đơn">
      <!-- HEADER THU NGÂN -->
      <header class="checkout-header">
        <div>
          <span class="checkout-eyebrow">Thu ngân & Thanh toán</span>
          <h2>{{ tableDisplay || tableName || 'Đơn tại bàn' }}</h2>
        </div>
        <button class="close-button" type="button" title="Đóng" @click="$emit('close')">×</button>
      </header>

      <!-- TRẠNG THÁI LOADING / LỖI -->
      <div v-if="loading" class="checkout-state">
        <div class="state-spinner" aria-hidden="true"></div>
        <strong>Đang tải thông tin hóa đơn...</strong>
        <span>Vui lòng chờ trong giây lát.</span>
      </div>
      <div v-else-if="!checkoutData" class="checkout-state checkout-state-error">
        <strong>Không thể tải thông tin thanh toán.</strong>
        <span>Vui lòng đóng cửa sổ và thử lại.</span>
      </div>

      <!-- NỘI DUNG CHÍNH -->
      <div v-else class="checkout-content">
        <!-- Thông báo nếu còn món chưa xác nhận phục vụ -->
        <div v-if="checkoutData.unservedQuantity > 0" class="checkout-service-warning" role="status">
          ℹ️ Bàn còn {{ checkoutData.unservedQuantity }} món chưa xác nhận phục vụ. Khi thanh toán, hệ thống sẽ tự động hoàn tất món và giải phóng bàn.
        </div>

        <!-- CỘT TRÁI: PHIẾU TẠM TÍNH & MÓN ĂN -->
        <section class="order-panel">
          <div class="panel-heading">
            <div>
              <span class="section-kicker">Phiếu tạm tính</span>
              <h3>#{{ checkoutData.orderCode || checkoutData.orderId }}</h3>
            </div>
            <span class="unpaid-badge">Chờ thanh toán</span>
          </div>

          <div class="order-meta">
            <span><b>Bàn:</b> {{ tableDisplay || tableName || '—' }}</span>
            <span v-if="userName"><b>Thu ngân:</b> {{ userName }}</span>
          </div>

          <!-- Danh sách món -->
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

          <!-- Tổng tiền & Voucher / Điểm thưởng -->
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

            <!-- Nhập Voucher -->
            <div class="voucher-row">
              <input
                v-model="voucherCode"
                type="text"
                placeholder="Mã voucher (nếu có)"
                :disabled="checkoutData.voucherDiscount > 0"
                @keyup.enter="applyVoucher"
              />
              <button
                v-if="checkoutData.voucherDiscount === 0"
                type="button"
                :disabled="!voucherCode"
                @click="applyVoucher"
              >
                Áp dụng
              </button>
              <button v-else type="button" class="remove-button" @click="removeVoucher">Bỏ mã</button>
            </div>
            <p v-if="voucherMessage" class="form-message" :class="voucherSuccess ? 'success-message' : 'error-message'">
              {{ voucherMessage }}
            </p>

            <div class="total-line">
              <span>Tổng phải thu</span>
              <strong>{{ formatCurrency(checkoutData.finalAmount) }}</strong>
            </div>
            <div v-if="checkoutData.estimatedEarnPoints > 0 && !checkoutData.pointsDiscount" class="earned-points">
              ⭐ Tích lũy dự kiến: +{{ checkoutData.estimatedEarnPoints }} điểm
            </div>
          </div>
        </section>

        <!-- CỘT PHẢI: KHÁCH HÀNG & PHƯƠNG THỨC THANH TOÁN (TIỀN MẶT / CHUYỂN KHOẢN) -->
        <section class="payment-panel">
          <!-- KHÁCH HÀNG / THÀNH VIÊN -->
          <div class="customer-section">
            <div class="panel-title-row">
              <div>
                <span class="section-kicker">Thông tin khách</span>
                <h3>Khách hàng / Thành viên</h3>
              </div>
              <span v-if="checkoutData.customer" class="member-badge">Đã liên kết</span>
            </div>

            <div v-if="!checkoutData.customer" class="customer-search">
              <div class="search-row">
                <input
                  v-model="searchPhone"
                  type="tel"
                  placeholder="Nhập số điện thoại khách hàng"
                  @keyup.enter="searchCustomer"
                />
                <button type="button" class="search-button" :disabled="searchingCustomer" @click="searchCustomer">
                  {{ searchingCustomer ? '...' : 'Tìm' }}
                </button>
              </div>
              <p v-if="searchCustomerMsg" class="form-message error-message">{{ searchCustomerMsg }}</p>
              <div v-if="customerResults.length" class="customer-results">
                <button
                  v-for="c in customerResults"
                  :key="c.customerId"
                  type="button"
                  class="customer-result"
                  @click="selectCustomer(c)"
                >
                  <span>{{ c.fullName }}</span>
                  <small>{{ c.phone }} · {{ c.pointsBalance }} điểm</small>
                </button>
              </div>
              <div v-if="searchPhone && customerResults.length === 0 && searchCustomerMsg" class="quick-create">
                <span>Khách hàng mới chưa có tài khoản?</span>
                <div class="quick-create-row">
                  <input v-model="newCustomerName" type="text" placeholder="Họ tên khách hàng" />
                  <button type="button" :disabled="!newCustomerName || creatingCustomer" @click="quickCreateCustomer">
                    Thêm nhanh
                  </button>
                </div>
              </div>
            </div>

            <div v-else class="selected-customer">
              <div class="customer-avatar">{{ checkoutData.customer.fullName?.charAt(0) || 'K' }}</div>
              <div class="selected-customer-info">
                <strong>{{ checkoutData.customer.fullName }}</strong>
                <span>{{ checkoutData.customer.phone }}</span>
              </div>
              <button type="button" class="unlink-button" title="Bỏ gán khách hàng" @click="removeCustomer">Bỏ gán</button>
              <div class="points-balance">
                <span>Điểm khả dụng:</span>
                <strong>{{ checkoutData.customer.pointsBalance }} điểm</strong>
              </div>
            </div>

            <div v-if="checkoutData.customer && checkoutData.customer.pointsBalance > 0" class="points-form">
              <label for="pointsToUse">
                <span>Dùng điểm đổi giảm giá</span>
                <small>1 điểm = {{ checkoutData.pointConversionRate }} đ</small>
              </label>
              <div class="points-input-row">
                <input
                  id="pointsToUse"
                  v-model.number="pointsToUse"
                  type="number"
                  min="0"
                  :max="maxPointsCanUse"
                  placeholder="0"
                />
                <button
                  type="button"
                  :disabled="!pointsToUse || pointsToUse <= 0 || pointsToUse > maxPointsCanUse"
                  @click="applyPoints"
                >
                  Đổi điểm
                </button>
              </div>
              <small class="field-hint">Tối đa {{ maxPointsCanUse }} điểm cho hóa đơn này.</small>
            </div>
          </div>

          <!-- BƯỚC CUỐI: PHƯƠNG THỨC THANH TOÁN (2 PHƯƠNG THỨC: TIỀN MẶT & CHUYỂN KHOẢN) -->
          <div class="payment-section">
            <div class="panel-title-row">
              <div>
                <span class="section-kicker">Phương thức</span>
                <h3>Hình thức thanh toán</h3>
              </div>
            </div>

            <!-- CHỌN 2 PHƯƠNG THỨC RÕ RÀNG -->
            <div class="payment-methods-two">
              <label class="method-option-two" :class="{ selected: paymentMethod === 'cash' }">
                <input v-model="paymentMethod" type="radio" value="cash" />
                <span class="method-icon-lg">💵</span>
                <div class="method-label-group">
                  <strong>Tiền Mặt</strong>
                  <small>Thu tiền & trả tiền thừa</small>
                </div>
              </label>

              <label class="method-option-two" :class="{ selected: paymentMethod === 'transfer' }">
                <input v-model="paymentMethod" type="radio" value="transfer" />
                <span class="method-icon-lg">📱</span>
                <div class="method-label-group">
                  <strong>Chuyển Khoản VietQR</strong>
                  <small>Napas 247 · Tự động khớp</small>
                </div>
              </label>
            </div>

            <!-- 1. GIAO DIỆN CHUYỂN KHOẢN VIETQR TRỰC TUYẾN -->
            <div v-if="paymentMethod === 'transfer'" class="transfer-box-card">
              <!-- NÚT NỔI BẬT: ĐƯA KHÁCH QUÉT (TOÀN MÀN HÌNH) -->
              <button
                type="button"
                class="btn-customer-fullscreen-qr"
                @click="showFullscreenQr = true"
                title="Phóng to mã QR toàn màn hình che giấu thông tin nội bộ để đưa khách quét"
              >
                <span class="fs-4">📱</span>
                <div class="text-start">
                  <div class="fw-black fs-6">ĐƯA KHÁCH QUÉT (TOÀN MÀN HÌNH)</div>
                  <small class="opacity-90">Phóng to 300px · Số tiền xanh đậm · Che thao tác nội bộ</small>
                </div>
                <span class="fs-5 ms-auto">🔍</span>
              </button>

              <!-- CHỈ BÁO RADAR PULSE CHỜ REALTIME -->
              <div class="radar-listening-banner">
                <div class="radar-pulse-ring-wrapper">
                  <div class="radar-pulse-ring"></div>
                  <div class="radar-pulse-ring-2"></div>
                  <div class="radar-pulse-core"></div>
                </div>
                <div class="radar-text-content">
                  <strong class="radar-title">Hệ thống đang tự động nhận diện thanh toán...</strong>
                  <span class="radar-subtitle">Vui lòng không tắt màn hình khi khách đang chuyển tiền</span>
                </div>
              </div>

              <!-- HỘP THÔNG TIN QR THU GỌN TẠI CHỖ -->
              <div class="compact-qr-preview-card">
                <div class="qr-img-wrapper" @click="showFullscreenQr = true" title="Bấm để phóng to">
                  <img :src="vietQrUrl" alt="Mã VietQR" class="compact-qr-img" />
                  <div class="qr-zoom-hint">🔍 Chạm phóng to</div>
                </div>

                <div class="compact-qr-details">
                  <div class="qr-bank-badge">
                    <span>🏦 {{ activeVietQr.bankId }} · Napas 247</span>
                  </div>

                  <div class="detail-row">
                    <span class="detail-label">STK:</span>
                    <strong class="font-monospace text-primary fs-6">{{ activeVietQr.accountNo }}</strong>
                    <button
                      type="button"
                      class="btn-copy-mini"
                      @click="copyText(activeVietQr.accountNo, 'Số tài khoản')"
                      title="Sao chép STK"
                    >
                      📋
                    </button>
                  </div>

                  <div class="detail-row">
                    <span class="detail-label">Chủ TK:</span>
                    <span class="fw-bold text-dark text-truncate">{{ activeVietQr.accountName }}</span>
                  </div>

                  <div class="detail-row">
                    <span class="detail-label">Nội dung:</span>
                    <strong class="font-monospace text-danger fs-6">{{ activeVietQr.memo }}</strong>
                    <button
                      type="button"
                      class="btn-copy-mini"
                      @click="copyText(activeVietQr.memo, 'Nội dung chuyển khoản')"
                      title="Sao chép cú pháp"
                    >
                      📋
                    </button>
                  </div>
                </div>
              </div>

              <!-- Thông báo copy thành công -->
              <div v-if="copiedNotice" class="copy-toast-inline">
                ✅ Đã sao chép <b>{{ copiedNotice }}</b> vào bộ nhớ tạm!
              </div>
            </div>

            <!-- 2. GIAO DIỆN TIỀN MẶT -->
            <div v-if="paymentMethod === 'cash'" class="cash-field">
              <label for="amountPaid">Số tiền khách đưa:</label>
              <div class="amount-input">
                <input
                  id="amountPaid"
                  v-model.number="amountPaid"
                  type="number"
                  min="0"
                  step="1000"
                  placeholder="0"
                />
                <span>đ</span>
              </div>

              <!-- Gợi ý nhanh mệnh giá -->
              <div class="quick-cash-tags">
                <button
                  type="button"
                  class="cash-tag"
                  @click="amountPaid = checkoutData.finalAmount"
                >
                  Đủ tiền ({{ formatCurrency(checkoutData.finalAmount) }})
                </button>
                <button
                  v-for="amt in quickCashOptions"
                  :key="amt"
                  type="button"
                  class="cash-tag"
                  @click="amountPaid = amt"
                >
                  {{ formatCurrency(amt) }}
                </button>
              </div>

              <div v-if="amountPaid >= checkoutData.finalAmount" class="change-row">
                <span>Tiền thừa gửi lại khách:</span>
                <strong class="text-success fs-5">{{ formatCurrency(amountPaid - checkoutData.finalAmount) }}</strong>
              </div>
              <div v-else-if="amountPaid > 0" class="shortage-message">
                Khách còn thiếu: <strong>{{ formatCurrency(checkoutData.finalAmount - amountPaid) }}</strong>
              </div>
            </div>

            <!-- TỔNG TIỀN PHẢI THU & NÚT HOÀN TẤT -->
            <div class="payable-row">
              <span>Tổng thanh toán</span>
              <strong>{{ formatCurrency(checkoutData.finalAmount) }}</strong>
            </div>

            <button
              type="button"
              class="complete-button"
              :disabled="isSubmitting || (paymentMethod === 'cash' && amountPaid < checkoutData.finalAmount)"
              @click="processPayment"
            >
              <span v-if="isSubmitting" class="button-spinner"></span>
              <span>{{ isSubmitting ? 'Đang xử lý...' : (paymentMethod === 'transfer' ? 'Xác Nhận Đã Nhận Tiền & In Bill' : 'Hoàn Tất Thanh Toán Tiền Mặt') }}</span>
            </button>
          </div>
        </section>
      </div>
    </section>

    <!-- ========================================================================= -->
    <!-- CỬA SỔ TOÀN MÀN HÌNH: "📱 ĐƯA KHÁCH QUÉT" (CUSTOMER-FACING QR VIEW)       -->
    <!-- Che giấu toàn bộ thông tin nội bộ, mã QR 300px sắc nét, số tiền xanh đậm   -->
    <!-- ========================================================================= -->
    <div v-if="showFullscreenQr" class="customer-qr-backdrop" @click.self="showFullscreenQr = false">
      <div class="customer-qr-card shadow-2xl">
        <!-- Header màn hình khách -->
        <div class="customer-qr-header">
          <div>
            <div class="restaurant-logo-text">🌿 HƯƠNG SEN RESTAURANT</div>
            <div class="table-badge-kicker">
              BÀN: <b>{{ tableDisplay || tableName || 'Tại bàn' }}</b> · ĐƠN <b>#{{ checkoutData?.orderCode || checkoutData?.orderId }}</b>
            </div>
          </div>
          <button type="button" class="btn-close-customer-qr" @click="showFullscreenQr = false">
            ✕ Quay lại
          </button>
        </div>

        <!-- Thân màn hình khách -->
        <div class="customer-qr-body">
          <div class="customer-qr-title">QUÉT MÃ ĐỂ THANH TOÁN</div>

          <!-- MÃ QR 300px SẮC NÉT -->
          <div class="qr-canvas-box-300">
            <img :src="vietQrUrl" alt="Mã VietQR 300px" class="qr-image-300" />
            <div class="qr-scan-badge">Napas 247 · {{ activeVietQr.bankId }}</div>
          </div>

          <!-- SỐ TIỀN IN ĐẬM MÀU XANH -->
          <div class="customer-amount-highlight">
            <span class="amount-sublabel">Số tiền cần thanh toán</span>
            <div class="amount-number-blue">{{ formatCurrency(checkoutData?.finalAmount) }}</div>
          </div>

          <!-- MÃ ĐƠN HÀNG & THÔNG TIN CHUYỂN KHOẢN -->
          <div class="customer-transfer-memo-pill">
            <span>Nội dung chuyển:</span>
            <strong class="font-monospace fs-5 text-dark">{{ activeVietQr.memo }}</strong>
          </div>

          <div class="customer-bank-account-meta">
            Chủ TK: <b class="text-uppercase">{{ activeVietQr.accountName }}</b> · STK: <b class="font-monospace">{{ activeVietQr.accountNo }}</b>
          </div>

          <!-- SÓNG RADAR ĐỘNG NGAY TRÊN MÀN HÌNH KHÁCH -->
          <div class="customer-radar-pulse">
            <div class="radar-pulse-ring-wrapper">
              <div class="radar-pulse-ring"></div>
              <div class="radar-pulse-ring-2"></div>
              <div class="radar-pulse-core"></div>
            </div>
            <span>Hệ thống đang tự động nhận diện thanh toán... Vui lòng không tắt màn hình</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MÀN HÌNH TICK XANH CỠ LỚN & ÂM BÁO "TING TING" KHI TIỀN VÀO TÀI KHOẢN     -->
    <!-- ========================================================================= -->
    <div v-if="showSuccessTick" class="success-tick-overlay">
      <div class="success-tick-card">
        <!-- Vòng tròn Tick xanh cỡ lớn với hiệu ứng nảy -->
        <div class="success-tick-circle">
          <svg class="success-tick-svg" viewBox="0 0 52 52">
            <circle class="success-tick-circle-bg" cx="26" cy="26" r="24" fill="none" />
            <path class="success-tick-check" fill="none" d="M14.1 27.2l7.1 7.2 16.7-16.8" />
          </svg>
        </div>

        <h3 class="success-tick-title">🎉 THANH TOÁN THÀNH CÔNG!</h3>
        <p class="success-tick-subtitle">Đã ghi nhận thanh toán vào tài khoản nhà hàng</p>

        <div class="success-tick-amount">
          {{ formatCurrency(successInfo?.amount || checkoutData?.finalAmount) }}
        </div>

        <div class="success-tick-meta">
          <span>Bàn: <b>{{ tableDisplay || tableName }}</b></span>
          <span>Hóa đơn: <b class="font-monospace">{{ successInfo?.invoiceCode || 'INV' }}</b></span>
        </div>

        <div class="success-auto-print-hint">
          <div class="spinner-grow spinner-grow-sm text-success me-2" role="status"></div>
          <span>Đang tự động mở lệnh in hóa đơn K80...</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, computed, onMounted, onUnmounted } from 'vue';
import axios from 'axios';
import signalRService from '../../../services/signalr';

const props = defineProps({
  show: Boolean,
  orderId: Number,
  tableName: String,
  tableDisplay: String,
  userName: String
});

const emit = defineEmits(['close', 'success']);
const paymentApiBase = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/staff/payment`;

const loading = ref(false);
const isSubmitting = ref(false);
const checkoutData = ref(null);
const paymentMethod = ref('transfer'); // Mặc định mở Chuyển khoản VietQR tiện lợi
const amountPaid = ref(0);

// Khách hàng & Voucher & Điểm
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

// Chế độ phóng to QR đưa khách quét
const showFullscreenQr = ref(false);

// Trạng thái thành công & Tick xanh
const showSuccessTick = ref(false);
const successInfo = ref(null);
const copiedNotice = ref('');

// Thông tin ngân hàng sinh mã VietQR với dự phòng an toàn tuyệt đối
const activeVietQr = computed(() => {
  const qr = checkoutData.value?.vietQr || checkoutData.value?.VietQr;
  const orderMemo = props.orderId ? `ORD${String(props.orderId).padStart(4, '0')}` : 'ORD0001';
  if (qr) {
    return {
      bankId: qr.bankId || qr.BankId || 'BIDV',
      accountNo: qr.accountNo || qr.AccountNo || '5704484382',
      accountName: qr.accountName || qr.AccountName || 'VO VAN LUOM',
      memo: qr.memo || qr.Memo || orderMemo
    };
  }
  return {
    bankId: 'BIDV',
    accountNo: '5704484382',
    accountName: 'VO VAN LUOM',
    memo: orderMemo
  };
});

// Hàm sinh chuỗi mã VietQR chuẩn EMVCo Napas 247 tương thích 100% MoMo, ZaloPay và tất cả App Ngân Hàng
function generateVietQrPayload({ binOrBankCode, accountNo, amount, memo }) {
  const binMap = {
    'BIDV': '970418',
    'VCB': '970436',
    'VIETCOMBANK': '970436',
    'MB': '970422',
    'MBBANK': '970422',
    'TCB': '970407',
    'TECHCOMBANK': '970407',
    'ACB': '970416',
    'VPB': '970432',
    'VPBANK': '970432',
    'TPB': '970423',
    'TPBANK': '970423',
    'VIB': '970441',
    'STB': '970403',
    'SACOMBANK': '970403',
    'CTG': '970415',
    'VIETINBANK': '970415',
    'AGR': '970405',
    'AGRIBANK': '970405',
  };
  const cleanBank = (binOrBankCode || 'BIDV').toUpperCase().trim();
  const bin = binMap[cleanBank] || (/^\d+$/.test(cleanBank) ? cleanBank : '970418');

  let res = '000201';
  res += (amount && amount > 0) ? '010212' : '010211';

  const guid = '0010A000000727';
  const subBin = '00' + String(bin.length).padStart(2, '0') + bin;
  const subAcc = '01' + String(accountNo.length).padStart(2, '0') + accountNo;
  const sub01 = subBin + subAcc;
  const sub01Full = '01' + String(sub01.length).padStart(2, '0') + sub01;
  const sub02 = '0208QRIBFTTA';
  const tag38Val = guid + sub01Full + sub02;
  res += '38' + String(tag38Val.length).padStart(2, '0') + tag38Val;

  res += '5303704';
  if (amount && amount > 0) {
    const amtStr = String(Math.round(amount));
    res += '54' + String(amtStr.length).padStart(2, '0') + amtStr;
  }
  res += '5802VN';
  if (memo) {
    res += '62' + String(memo.length + 4).padStart(2, '0') + '08' + String(memo.length).padStart(2, '0') + memo;
  }
  res += '6304';

  let crc = 0xFFFF;
  for (let i = 0; i < res.length; i++) {
    crc ^= (res.charCodeAt(i) << 8);
    for (let j = 0; j < 8; j++) {
      if ((crc & 0x8000) !== 0) {
        crc = ((crc << 1) ^ 0x1021) & 0xFFFF;
      } else {
        crc = (crc << 1) & 0xFFFF;
      }
    }
  }
  return res + crc.toString(16).toUpperCase().padStart(4, '0');
}

// URL tạo ảnh VietQR động chuẩn 300px sắc nét tương thích 100% MoMo và ngân hàng
const vietQrUrl = computed(() => {
  const qr = activeVietQr.value;
  const bankId = qr.bankId || 'BIDV';
  const accountNo = qr.accountNo || '5704484382';
  const memo = qr.memo || (props.orderId ? `ORD${String(props.orderId).padStart(4, '0')}` : 'ORD0001');
  const amount = Math.round(checkoutData.value?.finalAmount || 0);

  const payload = generateVietQrPayload({
    binOrBankCode: bankId,
    accountNo,
    amount,
    memo
  });

  return `https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=8&data=${encodeURIComponent(payload)}`;
});

// Gợi ý làm tròn tiền mặt
const quickCashOptions = computed(() => {
  const amt = checkoutData.value?.finalAmount || 0;
  if (!amt) return [];
  const next50k = Math.ceil(amt / 50000) * 50000;
  const next100k = Math.ceil(amt / 100000) * 100000;
  const next200k = Math.ceil(amt / 200000) * 200000;
  const next500k = Math.ceil(amt / 500000) * 500000;
  const set = new Set([next50k, next100k, next200k, next500k]);
  set.delete(amt);
  return Array.from(set).filter(x => x > amt).slice(0, 3);
});

const maxPointsCanUse = computed(() => {
  if (!checkoutData.value || !checkoutData.value.customer) return 0;
  const remainingNeedToPay = checkoutData.value.subtotal - checkoutData.value.voucherDiscount;
  if (remainingNeedToPay <= 0) return 0;
  const maxPoints = Math.floor(remainingNeedToPay / checkoutData.value.pointConversionRate);
  return Math.min(maxPoints, checkoutData.value.customer.pointsBalance);
});

watch(() => props.show, (val) => {
  if (val && props.orderId) {
    loadCheckoutData();
  } else {
    showFullscreenQr.value = false;
    showSuccessTick.value = false;
    successInfo.value = null;
  }
});

async function loadCheckoutData() {
  loading.value = true;
  checkoutData.value = null;
  showFullscreenQr.value = false;
  showSuccessTick.value = false;
  successInfo.value = null;
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
    alert('Không thể tải thông tin thanh toán.');
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
  paymentMethod.value = 'transfer';
}

function updateFinalAmount() {
  let finalAmt = checkoutData.value.subtotal - checkoutData.value.voucherDiscount - checkoutData.value.pointsDiscount;
  if (finalAmt < 0) finalAmt = 0;
  checkoutData.value.finalAmount = finalAmt;
  if (paymentMethod.value !== 'cash') {
    amountPaid.value = finalAmt;
  }
}

watch(paymentMethod, (newVal) => {
  if (checkoutData.value && newVal !== 'cash') {
    amountPaid.value = checkoutData.value.finalAmount;
  }
});

// Copy thông tin nhanh
function copyText(text, label) {
  if (!text) return;
  navigator.clipboard.writeText(String(text)).then(() => {
    copiedNotice.value = label;
    setTimeout(() => {
      copiedNotice.value = '';
    }, 2500);
  });
}

// ÂM BÁO "TING TING" CHUÔNG NGÂN VÀNG ĐÓN TIỀN VÀO TÀI KHOẢN (WEB AUDIO SYNTHESIZER)
function playCashChime() {
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    const ctx = new AudioContext();
    const now = ctx.currentTime;

    // Nốt 1 (D5 - 587.33Hz)
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.frequency.setValueAtTime(587.33, now);
    gain1.gain.setValueAtTime(0.35, now);
    gain1.gain.exponentialRampToValueAtTime(0.01, now + 0.35);
    osc1.start(now);
    osc1.stop(now + 0.35);

    // Nốt 2 (A5 - 880Hz)
    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.frequency.setValueAtTime(880, now + 0.12);
    gain2.gain.setValueAtTime(0.4, now + 0.12);
    gain2.gain.exponentialRampToValueAtTime(0.01, now + 0.7);
    osc2.start(now + 0.12);
    osc2.stop(now + 0.7);

    // Nốt 3 (D6 - 1174.66Hz) ngân vang
    const osc3 = ctx.createOscillator();
    const gain3 = ctx.createGain();
    osc3.connect(gain3);
    gain3.connect(ctx.destination);
    osc3.frequency.setValueAtTime(1174.66, now + 0.24);
    gain3.gain.setValueAtTime(0.45, now + 0.24);
    gain3.gain.exponentialRampToValueAtTime(0.001, now + 1.25);
    osc3.start(now + 0.24);
    osc3.stop(now + 1.25);
  } catch (e) {}
}

// Kích hoạt chuỗi hiển thị Tick xanh + Ting Ting + Chuyển sang in hóa đơn
function triggerSuccessSequence(invoiceData) {
  playCashChime();
  successInfo.value = {
    amount: invoiceData?.totalAmount || invoiceData?.amount || checkoutData.value?.finalAmount,
    invoiceCode: invoiceData?.invoiceCode || `INV-${props.orderId}`
  };
  showSuccessTick.value = true;

  // Giữ màn hình tick xanh 1.3 giây để nhân viên & khách chiêm ngưỡng
  setTimeout(() => {
    emit('success', invoiceData);
  }, 1350);
}

// LẮNG NGHE SIGNALR REALTIME (WEBHOOK NGÂN HÀNG BẮN VỀ)
function handleSignalRPayment(data) {
  if (!props.show) return;
  if (data.orderId === props.orderId) {
    triggerSuccessSequence(data.invoice || data);
  }
}

onMounted(() => {
  signalRService.on('PaymentReceived', handleSignalRPayment);
});

onUnmounted(() => {
  signalRService.off('PaymentReceived', handleSignalRPayment);
});

// Xử lý thanh toán thủ công (Nút bấm)
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
    triggerSuccessSequence(res.data.invoice);
  } catch (err) {
    alert(err.response?.data?.message || 'Lỗi khi thực hiện thanh toán.');
  } finally {
    isSubmitting.value = false;
  }
}

// Voucher & Khách hàng
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
    voucherMessage.value = err.response?.data?.message || 'Mã voucher không hợp lệ';
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
    selectCustomer(res.data.customer || res.data);
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

function formatCurrency(val) {
  if (!val && val !== 0) return '0 đ';
  return `${new Intl.NumberFormat('vi-VN').format(val)} đ`;
}
</script>

<style scoped>
/* ============================================================== */
/* DIALOG & LAYOUT CƠ BẢN                                         */
/* ============================================================== */
.checkout-overlay {
  position: fixed;
  inset: 0;
  z-index: 1100;
  display: grid;
  place-items: center;
  padding: 16px;
  overflow-y: auto;
  background: rgba(15, 23, 42, 0.68);
  backdrop-filter: blur(5px);
}

.checkout-dialog {
  width: min(1040px, 100%);
  max-height: 94vh;
  overflow: hidden;
  border-radius: 16px;
  background: #fff;
  box-shadow: 0 25px 60px rgba(0, 0, 0, 0.28);
  display: flex;
  flex-direction: column;
}

.checkout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 24px;
  color: #fff;
  background: linear-gradient(135deg, #0f766e 0%, #115e59 100%);
}

.checkout-header h2 {
  margin: 2px 0 0;
  font-size: 1.25rem;
  font-weight: 800;
  letter-spacing: -0.01em;
}

.checkout-eyebrow, .section-kicker {
  display: block;
  color: #99f6e4;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.close-button {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border: 1px solid rgba(255, 255, 255, 0.35);
  border-radius: 8px;
  color: #fff;
  background: transparent;
  font-size: 1.5rem;
  line-height: 1;
  cursor: pointer;
  transition: all 0.15s ease;
}
.close-button:hover {
  background: rgba(255, 255, 255, 0.2);
}

.checkout-content {
  display: grid;
  grid-template-columns: minmax(0, 1.05fr) minmax(380px, 0.95fr);
  max-height: calc(94vh - 72px);
  overflow: hidden;
}

.order-panel, .payment-panel {
  min-width: 0;
  overflow-y: auto;
  padding: 22px 24px;
}
.order-panel {
  background: #f8fafc;
  border-right: 1px solid #e2e8f0;
}
.payment-panel {
  background: #fff;
}

.checkout-service-warning {
  grid-column: 1 / -1;
  padding: 12px 16px;
  color: #92400e;
  background: #fef3c7;
  border-bottom: 1px solid #fde68a;
  font-size: 13px;
  font-weight: 600;
}
.checkout-service-warning button {
  margin-left: 10px;
  border: 0;
  background: transparent;
  color: #0d9488;
  font-weight: 700;
  text-decoration: underline;
  cursor: pointer;
}

/* ============================================================== */
/* CỘT TRÁI: DANH SÁCH MÓN ĂN & TỔNG TIỀN                         */
/* ============================================================== */
.panel-heading, .panel-title-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
}
.panel-heading {
  padding-bottom: 12px;
  border-bottom: 1px solid #e2e8f0;
}
.panel-heading h3, .panel-title-row h3 {
  margin: 3px 0 0;
  color: #0f172a;
  font-size: 1.05rem;
  font-weight: 800;
}

.unpaid-badge {
  padding: 5px 10px;
  border-radius: 6px;
  color: #b45309;
  background: #fef3c7;
  font-size: 0.72rem;
  font-weight: 800;
  white-space: nowrap;
}

.order-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 20px;
  padding: 10px 0 16px;
  color: #64748b;
  font-size: 0.8rem;
}
.order-meta b {
  color: #1e293b;
}

.order-items {
  min-height: 150px;
}
.item-heading, .order-item {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 34px 92px 104px;
  gap: 10px;
  align-items: start;
}
.item-heading {
  padding-bottom: 8px;
  border-bottom: 2px solid #cbd5e1;
  color: #64748b;
  font-size: 0.68rem;
  font-weight: 800;
  text-transform: uppercase;
}
.item-heading span:not(:first-child), .item-quantity, .item-price, .item-total {
  text-align: right;
}
.order-item {
  padding: 10px 0;
  border-bottom: 1px solid #e2e8f0;
  color: #1e293b;
  font-size: 0.82rem;
}
.item-name strong {
  display: block;
  color: #0f172a;
  font-weight: 700;
}
.item-name small, .item-name em {
  display: block;
  margin-top: 2px;
  color: #64748b;
  font-size: 0.7rem;
}
.item-name em {
  color: #d97706;
}
.item-quantity, .item-price {
  color: #475569;
}
.item-total {
  color: #0f172a;
  font-weight: 800;
}
.empty-items {
  padding: 24px 0;
  color: #94a3b8;
  text-align: center;
}

.order-total {
  margin-top: 16px;
  padding-top: 14px;
  border-top: 2px dashed #cbd5e1;
}
.summary-line, .total-line, .payable-row, .change-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}
.summary-line {
  margin-bottom: 6px;
  color: #64748b;
  font-size: 0.82rem;
}
.summary-line strong {
  color: #1e293b;
}
.discount-line strong {
  color: #dc2626;
}

.voucher-row {
  display: flex;
  gap: 8px;
  margin: 12px 0 4px;
}
.voucher-row input, .voucher-row button, .search-row input, .search-button, .quick-create-row input, .quick-create-row button, .points-input-row input, .points-input-row button {
  min-height: 36px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font: inherit;
  font-size: 0.78rem;
}
.voucher-row input, .search-row input, .quick-create-row input, .points-input-row input {
  padding: 6px 12px;
  background: #fff;
  color: #0f172a;
}
.voucher-row input {
  flex: 1;
  text-transform: uppercase;
}
.voucher-row button, .search-button, .quick-create-row button, .points-input-row button {
  padding: 6px 14px;
  border-color: #0d9488;
  color: #0f766e;
  background: #f0fdfa;
  font-weight: 800;
  cursor: pointer;
}
.remove-button {
  border-color: #fca5a5 !important;
  color: #dc2626 !important;
  background: #fef2f2 !important;
}

.total-line {
  margin-top: 14px;
  padding-top: 12px;
  border-top: 2px solid #0f766e;
  color: #0f172a;
  font-size: 0.95rem;
  font-weight: 800;
  text-transform: uppercase;
}
.total-line strong {
  color: #dc2626;
  font-size: 1.35rem;
}
.earned-points {
  margin-top: 6px;
  color: #059669;
  font-size: 0.76rem;
  text-align: right;
  font-weight: 700;
}

/* ============================================================== */
/* CỘT PHẢI: KHÁCH HÀNG & PHƯƠNG THỨC THANH TOÁN                  */
/* ============================================================== */
.customer-section {
  padding-bottom: 18px;
  border-bottom: 1px solid #e2e8f0;
}
.payment-section {
  padding-top: 18px;
}

.member-badge {
  padding: 4px 8px;
  border-radius: 6px;
  color: #059669;
  background: #d1fae5;
  font-size: 0.7rem;
  font-weight: 800;
}

.search-row, .quick-create-row, .points-input-row {
  display: flex;
  gap: 8px;
  margin-top: 10px;
}
.search-row input, .quick-create-row input {
  flex: 1;
}
.customer-results {
  display: grid;
  gap: 6px;
  max-height: 140px;
  margin-top: 8px;
  overflow-y: auto;
}
.customer-result {
  display: grid;
  gap: 2px;
  padding: 8px 10px;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  color: #1e293b;
  background: #f8fafc;
  text-align: left;
  cursor: pointer;
}
.customer-result:hover {
  border-color: #0d9488;
  background: #f0fdfa;
}
.customer-result span {
  font-weight: 800;
}
.customer-result small {
  color: #64748b;
}

.selected-customer {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr) auto;
  gap: 10px;
  align-items: center;
  margin-top: 12px;
  padding: 10px 12px;
  border: 1px solid #99f6e4;
  border-radius: 10px;
  background: #f0fdfa;
}
.customer-avatar {
  display: grid;
  width: 40px;
  height: 40px;
  place-items: center;
  border-radius: 50%;
  color: #0f766e;
  background: #ccfbf1;
  font-weight: 800;
  font-size: 1.1rem;
}
.unlink-button {
  padding: 4px 8px;
  border: 1px solid #fecaca;
  border-radius: 6px;
  color: #dc2626;
  background: #fff;
  font-size: 0.72rem;
  cursor: pointer;
}
.points-balance {
  grid-column: 2 / -1;
  display: flex;
  justify-content: space-between;
  padding-top: 6px;
  border-top: 1px dashed #99f6e4;
  color: #64748b;
  font-size: 0.75rem;
}
.points-balance strong {
  color: #059669;
}
.points-form {
  margin-top: 10px;
}
.points-form label {
  display: flex;
  justify-content: space-between;
  font-size: 0.75rem;
  font-weight: 700;
}

/* ============================================================== */
/* PHƯƠNG THỨC THANH TOÁN (2 NÚT TO RÕ: TIỀN MẶT & CHUYỂN KHOẢN) */
/* ============================================================== */
.payment-methods-two {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-top: 12px;
  margin-bottom: 14px;
}

.method-option-two {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  border: 2px solid #e2e8f0;
  border-radius: 12px;
  background: #fff;
  cursor: pointer;
  transition: all 0.2s ease;
}
.method-option-two input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}
.method-option-two:hover {
  border-color: #cbd5e1;
  background: #f8fafc;
}
.method-option-two.selected {
  border-color: #0f766e;
  background: #f0fdfa;
  box-shadow: 0 4px 12px rgba(15, 118, 110, 0.12);
}

.method-icon-lg {
  font-size: 1.6rem;
  flex-shrink: 0;
}
.method-label-group strong {
  display: block;
  font-size: 0.88rem;
  color: #0f172a;
}
.method-label-group small {
  display: block;
  color: #64748b;
  font-size: 0.68rem;
}
.method-option-two.selected .method-label-group strong {
  color: #0f766e;
}

/* ============================================================== */
/* 1. KHỐI CHUYỂN KHOẢN VIETQR TRỰC TUYẾN                         */
/* ============================================================== */
.transfer-box-card {
  margin-top: 10px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* Nút Đưa khách quét toàn màn hình */
.btn-customer-fullscreen-qr {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border: 0;
  border-radius: 12px;
  background: linear-gradient(135deg, #0284c7 0%, #0369a1 100%);
  color: #fff;
  cursor: pointer;
  box-shadow: 0 6px 18px rgba(2, 132, 199, 0.25);
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.btn-customer-fullscreen-qr:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 22px rgba(2, 132, 199, 0.35);
}

/* Sóng Radar Pulse Realtime */
.radar-listening-banner {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  border: 1px solid #bbf7d0;
  border-radius: 10px;
  background: #f0fdf4;
}

.radar-pulse-ring-wrapper {
  position: relative;
  width: 24px;
  height: 24px;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}
.radar-pulse-core {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #16a34a;
}
.radar-pulse-ring, .radar-pulse-ring-2 {
  position: absolute;
  inset: 0;
  border: 2px solid #22c55e;
  border-radius: 50%;
  animation: radarWave 1.8s cubic-bezier(0.215, 0.61, 0.355, 1) infinite;
}
.radar-pulse-ring-2 {
  animation-delay: 0.7s;
}

@keyframes radarWave {
  0% { transform: scale(0.4); opacity: 1; }
  100% { transform: scale(1.6); opacity: 0; }
}

.radar-text-content {
  display: flex;
  flex-direction: column;
  line-height: 1.3;
}
.radar-title {
  color: #15803d;
  font-size: 0.78rem;
  font-weight: 800;
}
.radar-subtitle {
  color: #166534;
  font-size: 0.68rem;
}

/* Thẻ QR thu gọn tại chỗ */
.compact-qr-preview-card {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  background: #f8fafc;
}

.qr-img-wrapper {
  position: relative;
  width: 110px;
  height: 110px;
  flex-shrink: 0;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #cbd5e1;
  background: #fff;
  cursor: pointer;
}
.compact-qr-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
.qr-zoom-hint {
  position: absolute;
  inset-inline: 0;
  bottom: 0;
  background: rgba(15, 23, 42, 0.7);
  color: #fff;
  font-size: 0.62rem;
  font-weight: 700;
  text-align: center;
  padding: 2px 0;
}

.compact-qr-details {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.qr-bank-badge {
  font-size: 0.74rem;
  font-weight: 800;
  color: #0f766e;
  margin-bottom: 2px;
}
.detail-row {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.78rem;
}
.detail-label {
  color: #64748b;
  min-width: 54px;
}
.btn-copy-mini {
  padding: 1px 6px;
  border: 1px solid #cbd5e1;
  border-radius: 4px;
  background: #fff;
  font-size: 0.7rem;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-copy-mini:hover {
  background: #f1f5f9;
}
.copy-toast-inline {
  padding: 6px 10px;
  border-radius: 6px;
  background: #dcfce7;
  color: #15803d;
  font-size: 0.74rem;
  font-weight: 700;
  text-align: center;
}

/* ============================================================== */
/* 2. KHỐI TIỀN MẶT                                               */
/* ============================================================== */
.cash-field {
  margin-top: 10px;
  padding: 12px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  background: #f8fafc;
}
.cash-field label {
  display: block;
  font-size: 0.76rem;
  font-weight: 800;
  color: #1e293b;
  margin-bottom: 6px;
}
.amount-input {
  display: flex;
  align-items: center;
  border: 2px solid #cbd5e1;
  border-radius: 8px;
  background: #fff;
}
.amount-input:focus-within {
  border-color: #0f766e;
  box-shadow: 0 0 0 3px rgba(15, 118, 110, 0.15);
}
.amount-input input {
  width: 100%;
  min-width: 0;
  padding: 8px 12px;
  border: 0;
  outline: 0;
  color: #0f766e;
  background: transparent;
  font: inherit;
  font-size: 1.15rem;
  font-weight: 900;
  text-align: right;
}
.amount-input span {
  padding-right: 12px;
  color: #64748b;
  font-weight: 700;
}

.quick-cash-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 8px;
}
.cash-tag {
  padding: 4px 8px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  background: #fff;
  color: #334155;
  font-size: 0.72rem;
  font-weight: 700;
  cursor: pointer;
}
.cash-tag:hover {
  background: #f1f5f9;
  border-color: #94a3b8;
}

.change-row {
  margin-top: 10px;
  padding-top: 8px;
  border-top: 1px dashed #cbd5e1;
  font-size: 0.82rem;
  color: #475569;
}
.shortage-message {
  margin-top: 8px;
  color: #dc2626;
  font-size: 0.78rem;
  font-weight: 700;
  text-align: right;
}

/* Nút hoàn tất */
.payable-row {
  margin-top: 16px;
  padding: 10px 0;
  border-top: 1px solid #e2e8f0;
  font-size: 0.88rem;
  font-weight: 800;
  color: #0f172a;
}
.payable-row strong {
  color: #dc2626;
  font-size: 1.3rem;
}
.complete-button {
  width: 100%;
  min-height: 48px;
  margin-top: 6px;
  border: 0;
  border-radius: 10px;
  color: #fff;
  background: linear-gradient(135deg, #0f766e 0%, #115e59 100%);
  font: inherit;
  font-size: 0.92rem;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(15, 118, 110, 0.25);
  transition: all 0.15s ease;
}
.complete-button:hover {
  background: #0d9488;
}
.complete-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

/* ========================================================================= */
/* CỬA SỔ TOÀN MÀN HÌNH: "📱 ĐƯA KHÁCH QUÉT" (CUSTOMER-FACING QR VIEW)       */
/* ========================================================================= */
.customer-qr-backdrop {
  position: fixed;
  inset: 0;
  z-index: 2000;
  background: rgba(15, 23, 42, 0.85);
  backdrop-filter: blur(8px);
  display: grid;
  place-items: center;
  padding: 16px;
  animation: fadeIn 0.2s ease;
}

.customer-qr-card {
  width: min(440px, 100%);
  background: #ffffff;
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 25px 60px rgba(0, 0, 0, 0.4);
  animation: scaleUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}

.customer-qr-header {
  padding: 14px 20px;
  background: linear-gradient(135deg, #0f766e 0%, #134e4a 100%);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.restaurant-logo-text {
  font-weight: 900;
  font-size: 0.92rem;
  letter-spacing: 0.05em;
  color: #99f6e4;
}
.table-badge-kicker {
  font-size: 0.72rem;
  opacity: 0.95;
}

.btn-close-customer-qr {
  padding: 6px 12px;
  border: 1px solid rgba(255, 255, 255, 0.35);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}
.btn-close-customer-qr:hover {
  background: rgba(255, 255, 255, 0.3);
}

.customer-qr-body {
  padding: 24px 20px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
}

.customer-qr-title {
  color: #0f172a;
  font-size: 1.05rem;
  font-weight: 900;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  margin-bottom: 12px;
}

/* Mã QR 300px sắc nét */
.qr-canvas-box-300 {
  position: relative;
  width: 300px;
  height: 300px;
  border-radius: 16px;
  padding: 10px;
  background: #fff;
  border: 2px solid #0f766e;
  box-shadow: 0 10px 30px rgba(15, 118, 110, 0.18);
  display: grid;
  place-items: center;
}
.qr-image-300 {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: 10px;
}
.qr-scan-badge {
  position: absolute;
  top: -12px;
  padding: 3px 12px;
  border-radius: 20px;
  background: #0f766e;
  color: #fff;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.15);
}

/* Số tiền in đậm màu xanh */
.customer-amount-highlight {
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.amount-sublabel {
  font-size: 0.74rem;
  color: #64748b;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
.amount-number-blue {
  color: #0284c7; /* Xanh dương đậm sắc sảo */
  font-size: 1.9rem;
  font-weight: 900;
  letter-spacing: -0.02em;
}

.customer-transfer-memo-pill {
  margin-top: 10px;
  padding: 6px 16px;
  border-radius: 30px;
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.85rem;
  color: #334155;
}

.customer-bank-account-meta {
  margin-top: 8px;
  font-size: 0.75rem;
  color: #64748b;
}

/* Sóng radar trên màn hình khách */
.customer-radar-pulse {
  margin-top: 18px;
  padding: 10px 14px;
  border-radius: 12px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  display: flex;
  align-items: center;
  gap: 12px;
  color: #15803d;
  font-size: 0.76rem;
  font-weight: 700;
  text-align: left;
}

/* ========================================================================= */
/* MÀN HÌNH TICK XANH CỠ LỚN & ÂM BÁO "TING TING"                             */
/* ========================================================================= */
.success-tick-overlay {
  position: fixed;
  inset: 0;
  z-index: 2500;
  background: rgba(15, 23, 42, 0.9);
  backdrop-filter: blur(10px);
  display: grid;
  place-items: center;
  padding: 16px;
  animation: fadeIn 0.2s ease;
}

.success-tick-card {
  width: min(420px, 100%);
  padding: 32px 24px;
  background: #fff;
  border-radius: 24px;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  box-shadow: 0 30px 70px rgba(0, 0, 0, 0.4);
  animation: bounceIn 0.45s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.success-tick-circle {
  width: 90px;
  height: 90px;
  margin-bottom: 16px;
  border-radius: 50%;
  background: #22c55e;
  box-shadow: 0 0 0 10px rgba(34, 197, 94, 0.2), 0 0 0 20px rgba(34, 197, 94, 0.1);
  display: grid;
  place-items: center;
  animation: pulseScale 1s ease infinite alternate;
}

.success-tick-svg {
  width: 54px;
  height: 54px;
  stroke: #ffffff;
  stroke-width: 4.5;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.success-tick-check {
  stroke-dasharray: 48;
  stroke-dashoffset: 48;
  animation: strokeCheck 0.45s cubic-bezier(0.65, 0, 0.45, 1) 0.15s forwards;
}

@keyframes strokeCheck {
  100% { stroke-dashoffset: 0; }
}

@keyframes bounceIn {
  0% { transform: scale(0.6); opacity: 0; }
  100% { transform: scale(1); opacity: 1; }
}

@keyframes pulseScale {
  0% { transform: scale(1); }
  100% { transform: scale(1.05); }
}

.success-tick-title {
  color: #15803d;
  font-size: 1.35rem;
  font-weight: 900;
  margin: 0 0 4px;
}
.success-tick-subtitle {
  color: #64748b;
  font-size: 0.82rem;
  margin: 0 0 16px;
}
.success-tick-amount {
  font-size: 1.85rem;
  font-weight: 900;
  color: #0f172a;
  margin-bottom: 12px;
}
.success-tick-meta {
  display: flex;
  gap: 14px;
  color: #475569;
  font-size: 0.82rem;
  margin-bottom: 20px;
}
.success-auto-print-hint {
  display: flex;
  align-items: center;
  color: #059669;
  font-size: 0.78rem;
  font-weight: 700;
}

/* Spinner */
.button-spinner, .state-spinner {
  display: inline-block;
  width: 16px;
  height: 16px;
  margin-right: 8px;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-top-color: #fff;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
  vertical-align: -2px;
}
.checkout-state {
  display: grid;
  min-height: 340px;
  place-content: center;
  justify-items: center;
  gap: 8px;
  color: #475569;
  font-size: 0.85rem;
}
.checkout-state-error {
  color: #dc2626;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes scaleUp {
  from { opacity: 0; transform: scale(0.94); }
  to { opacity: 1; transform: scale(1); }
}

/* Responsive */
@media (max-width: 800px) {
  .checkout-content {
    grid-template-columns: 1fr;
    max-height: calc(96vh - 68px);
    overflow-y: auto;
  }
  .order-panel, .payment-panel {
    overflow: visible;
  }
  .payment-panel {
    border-top: 1px solid #e2e8f0;
  }
}
</style>

<template>
  <div v-if="show" class="invoice-modal-overlay" @click.self="$emit('close')">
    <div class="invoice-modal-dialog" :class="{ 'receipt-only-dialog': receiptOnly }">
      <!-- 1. TOOLBAR / HEADER -->
      <header class="modal-toolbar">
        <div class="toolbar-left">
          <div class="toolbar-brand-badge">
            <svg class="h-svg-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="16" y1="13" x2="8" y2="13"></line>
              <line x1="16" y1="17" x2="8" y2="17"></line>
            </svg>
          </div>
          <div>
            <div class="d-flex align-items-center gap-2">
              <span class="status-pill status-paid">
                <span class="dot-success"></span> ĐÃ THANH TOÁN
              </span>
              <span class="inv-code-tag">
                #{{ invoiceCodeDisplay }}
                <button
                  type="button"
                  class="btn-copy-code"
                  :title="copiedCode ? 'Đã sao chép!' : 'Sao chép mã hóa đơn'"
                  @click="copyInvoiceCode"
                >
                  <span v-if="copiedCode">✓ Đã chép</span>
                  <span v-else>📋</span>
                </button>
              </span>
            </div>
            <h5 class="toolbar-title mb-0">Hóa Đơn Bán Hàng & Phiếu Thanh Toán</h5>
          </div>
        </div>

        <div class="toolbar-actions">
          <!-- Switch View on small screens -->
          <div class="view-switch-tabs d-lg-none">
            <button
              type="button"
              class="switch-tab"
              :class="{ active: activeTab === 'audit' }"
              @click="activeTab = 'audit'"
            >
              Chi tiết
            </button>
            <button
              type="button"
              class="switch-tab"
              :class="{ active: activeTab === 'print' }"
              @click="activeTab = 'print'"
            >
              Bản in K80
            </button>
          </div>

          <button
            type="button"
            class="btn-primary-print"
            @click="handlePrint"
            :title="`${printActionLabel} (Ctrl+P)`"
          >
            <span class="print-icon">🖨️</span>
            <span class="d-none d-sm-inline">{{ printActionLabel }}</span>
          </button>

          <button
            type="button"
            class="btn-close-toolbar"
            title="Đóng (Esc)"
            @click="$emit('close')"
          >
            ✕
          </button>
        </div>
      </header>

      <!-- 2. BODY CONTENT (DUAL COLUMN DESKTOP / RESPONSIVE) -->
      <div class="modal-body-wrapper">
        <!-- Loading State -->
        <div v-if="loadingDetail" class="loading-state-box py-5 text-center">
          <div class="spinner-border text-primary" role="status"></div>
          <p class="mt-3 text-muted fw-semibold">Đang tải chi tiết hóa đơn...</p>
        </div>

        <div v-else class="split-view-container">
          <!-- ========================================== -->
          <!-- LEFT: THÔNG TIN NGHIỆP VỤ & ĐỐI SOÁT      -->
          <!-- ========================================== -->
          <section
            v-if="!receiptOnly"
            class="audit-panel"
            :class="{ 'd-none d-lg-block': activeTab === 'print' }"
          >
            <!-- A. THÔNG TIN BÀN & ĐƠN HÀNG -->
            <div class="audit-card">
              <div class="audit-card-head">
                <span class="head-icon">🪑</span>
                <span class="head-title">Thông tin bàn & phục vụ</span>
              </div>
              <div class="audit-grid-2">
                <div class="info-block">
                  <span class="info-label">Bàn phục vụ:</span>
                  <strong class="info-val text-primary fs-6">{{ resolvedTableDisplay }}</strong>
                </div>
                <div class="info-block">
                  <span class="info-label">Mã đơn gọi món:</span>
                  <span class="info-val font-monospace">#ORD-{{ currentInvoice.orderId || '—' }}</span>
                </div>
                <div class="info-block">
                  <span class="info-label">Thời gian xuất bill:</span>
                  <span class="info-val">{{ formatDateTime(currentInvoice.issuedAt || currentInvoice.createdAt) }}</span>
                </div>
                <div class="info-block">
                  <span class="info-label">Thu ngân phụ trách:</span>
                  <span class="info-val fw-semibold">👤 {{ cashierName || currentInvoice.cashierName || 'Nhân viên' }}</span>
                </div>
              </div>
            </div>

            <!-- B. THÔNG TIN KHÁCH HÀNG & TÍCH ĐIỂM -->
            <div class="audit-card">
              <div class="audit-card-head">
                <span class="head-icon">👑</span>
                <span class="head-title">Khách hàng & Hội viên</span>
              </div>
              <div class="audit-grid-2">
                <div class="info-block">
                  <span class="info-label">Khách hàng:</span>
                  <strong class="info-val">{{ customerName || 'Khách vãng lai' }}</strong>
                </div>
                <div class="info-block">
                  <span class="info-label">Số điện thoại:</span>
                  <span class="info-val font-monospace">{{ customerPhone || '—' }}</span>
                </div>
              </div>

              <!-- Loyalty Points Details (nếu có khách hàng) -->
              <div v-if="hasCustomerLoyalty" class="loyalty-strip mt-3">
                <div class="loyalty-item">
                  <span class="loy-label">Trừ điểm bill này:</span>
                  <strong class="loy-val text-danger">
                    {{ currentInvoice.pointsUsed || 0 }} điểm
                    <span v-if="pointDiscountAmount > 0">(-{{ formatCurrency(pointDiscountAmount) }})</span>
                  </strong>
                </div>
                <div class="loyalty-item">
                  <span class="loy-label">Điểm tích lũy mới:</span>
                  <strong class="loy-val text-success">+{{ currentInvoice.pointsEarned || 0 }} điểm</strong>
                </div>
                <div v-if="customerPointsBalance !== null" class="loyalty-item">
                  <span class="loy-label">Số dư điểm hiện có:</span>
                  <strong class="loy-val text-primary">{{ customerPointsBalance }} điểm</strong>
                </div>
              </div>
            </div>

            <!-- C. HÌNH THỨC THANH TOÁN & ĐỐI SOÁT TIỀN TỆ -->
            <div class="audit-card highlight-card">
              <div class="audit-card-head">
                <span class="head-icon">💳</span>
                <span class="head-title">Phương thức thanh toán</span>
              </div>

              <div class="payment-summary-box">
                <div class="d-flex align-items-center justify-content-between mb-2">
                  <span class="payment-method-badge" :class="getMethodBadgeClass(resolvedPaymentMethod)">
                    {{ getMethodIcon(resolvedPaymentMethod) }} {{ getPaymentMethod(resolvedPaymentMethod) }}
                  </span>
                  <span v-if="transactionCode" class="trans-code-badge">
                    Mã GD: <strong>{{ transactionCode }}</strong>
                  </span>
                </div>

                <!-- Tiền mặt: Khách đưa và tiền thừa -->
                <div v-if="isCashPayment && (currentInvoice.amountPaidByCustomer > 0 || changeReturnAmount > 0)" class="cash-calc-grid mt-2">
                  <div class="cash-calc-item">
                    <span>Tiền khách đưa:</span>
                    <strong>{{ formatCurrency(currentInvoice.amountPaidByCustomer) }}</strong>
                  </div>
                  <div class="cash-calc-item">
                    <span>Tiền thừa thối lại:</span>
                    <strong class="text-success fs-6">{{ formatCurrency(changeReturnAmount) }}</strong>
                  </div>
                </div>
              </div>
            </div>

            <!-- D. DANH SÁCH MÓN ĂN & CHI TIẾT DỊCH VỤ -->
            <div class="audit-card p-0 overflow-hidden">
              <div class="audit-card-head px-3 pt-3">
                <span class="head-icon">🍽️</span>
                <span class="head-title">Chi tiết món ăn ({{ invoiceItems.length }} món)</span>
              </div>

              <div class="table-responsive">
                <table class="table items-audit-table mb-0 align-middle">
                  <thead>
                    <tr>
                      <th class="ps-3">TÊN MÓN & GHI CHÚ</th>
                      <th class="text-center" style="width: 50px;">SL</th>
                      <th class="text-end" style="width: 100px;">ĐƠN GIÁ</th>
                      <th class="text-end pe-3" style="width: 120px;">THÀNH TIỀN</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(item, idx) in invoiceItems" :key="item.invoiceItemId || idx">
                      <td class="ps-3">
                        <div class="item-name-block">
                          <strong class="item-name-text">{{ item.foodNameSnapshot || item.foodName || 'Món ăn' }}</strong>
                          <span v-if="item.variantSnapshot && item.variantSnapshot !== 'Mặc định'" class="variant-tag">
                            {{ item.variantSnapshot }}
                          </span>
                          <span v-if="item.note" class="note-tag">
                            * {{ item.note }}
                          </span>
                        </div>
                      </td>
                      <td class="text-center fw-bold">{{ item.quantity || 1 }}</td>
                      <td class="text-end text-muted font-monospace">{{ formatCurrency(item.unitPrice) }}</td>
                      <td class="text-end pe-3 fw-bold text-dark font-monospace">
                        {{ formatCurrency(item.lineTotal || (item.unitPrice * (item.quantity || 1))) }}
                      </td>
                    </tr>
                    <tr v-if="invoiceItems.length === 0">
                      <td colspan="4" class="text-center py-4 text-muted">
                        Chưa có chi tiết danh sách món.
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <!-- E. TỔNG KẾT TÀI CHÍNH (BREAKDOWN) -->
            <div class="audit-card summary-breakdown-card">
              <div class="summary-line">
                <span>Tạm tính món ăn:</span>
                <strong class="font-monospace">{{ formatCurrency(currentInvoice.subtotal) }}</strong>
              </div>

              <div v-if="voucherDiscountAmount > 0" class="summary-line discount-line">
                <span>Ưu đãi Voucher{{ currentInvoice.voucherCode ? ` (${currentInvoice.voucherCode})` : '' }}:</span>
                <strong class="font-monospace">-{{ formatCurrency(voucherDiscountAmount) }}</strong>
              </div>

              <div v-if="pointDiscountAmount > 0" class="summary-line discount-line">
                <span>Giảm trừ điểm tích lũy ({{ currentInvoice.pointsUsed }} điểm):</span>
                <strong class="font-monospace">-{{ formatCurrency(pointDiscountAmount) }}</strong>
              </div>

              <div v-if="totalDiscountWithoutDetail > 0" class="summary-line discount-line">
                <span>Giảm trừ khác:</span>
                <strong class="font-monospace">-{{ formatCurrency(totalDiscountWithoutDetail) }}</strong>
              </div>

              <div class="summary-line grand-total-line">
                <span class="total-label">TỔNG TIỀN THANH TOÁN:</span>
                <strong class="total-value font-monospace">{{ formatCurrency(finalTotalAmount) }}</strong>
              </div>
            </div>
          </section>

          <!-- ========================================== -->
          <!-- RIGHT: PHIẾU IN NHIỆT K80 CHUYÊN DỤNG      -->
          <!-- ========================================== -->
          <section
            class="receipt-panel"
            :class="{ 'd-none d-lg-flex': !receiptOnly && activeTab === 'audit', 'receipt-only-panel': receiptOnly }"
          >
            <div v-if="!receiptOnly" class="receipt-panel-header d-flex align-items-center justify-content-between mb-2">
              <span class="preview-badge">
                <span class="dot-preview"></span> MÔ PHỎNG PHIẾU IN NHIỆT K80 (80MM)
              </span>
              <button type="button" class="btn-instant-print" @click="handlePrint">
                <span>🖨️ {{ printActionLabel }}</span>
              </button>
            </div>

            <!-- PHIẾU IN K80 THỰC TẾ (TARGET CHO @media print) -->
            <div id="printableInvoice" class="thermal-receipt-paper">
              <!-- Header Nhà hàng -->
              <div class="receipt-header">
                <div class="restaurant-name">DOLA RESTAURANT</div>
                <div class="restaurant-sub">Ẩm Thực Đương Đại & Đẳng Cấp</div>
                <div class="restaurant-info">Đ/c: 123 Đường Ẩm Thực, Quận 1, TP. HCM</div>
                <div class="restaurant-info">Hotline: 1900 6868 · Website: dolarestaurant.vn</div>
                <div class="receipt-divider-dashed">-----------------------------------------</div>
                <div class="receipt-title">HÓA ĐƠN THANH TOÁN</div>
                <div class="receipt-status-text">*** ĐÃ THANH TOÁN HOÀN TẤT ***</div>
              </div>

              <!-- Metadata đơn hàng -->
              <div class="receipt-meta">
                <div class="meta-row">
                  <span>Mã HĐ: <strong>#{{ invoiceCodeDisplay }}</strong></span>
                  <span>Bàn: <strong class="fs-6">{{ resolvedTableDisplay }}</strong></span>
                </div>
                <div class="meta-row">
                  <span>Ngày: {{ formatDateTime(currentInvoice.issuedAt || currentInvoice.createdAt) }}</span>
                </div>
                <div class="meta-row">
                  <span>Thu ngân: {{ cashierName || currentInvoice.cashierName || 'Nhân viên' }}</span>
                  <span>Đơn: #{{ currentInvoice.orderId || '—' }}</span>
                </div>
                <div v-if="customerName" class="meta-row customer-row">
                  <span>Khách hàng: <strong>{{ customerDisplay }}</strong></span>
                </div>
              </div>

              <div class="receipt-divider-dashed">-----------------------------------------</div>

              <!-- Bảng món ăn K80 -->
              <div class="receipt-items-table">
                <div class="r-table-header">
                  <span class="col-name">TÊN MÓN</span>
                  <span class="col-qty">SL</span>
                  <span class="col-price">Đ.GIÁ</span>
                  <span class="col-total">T.TIỀN</span>
                </div>
                <div class="receipt-divider-dotted">- - - - - - - - - - - - - - - - - - - - -</div>

                <div
                  v-for="(item, idx) in invoiceItems"
                  :key="item.invoiceItemId || idx"
                  class="r-item-row"
                >
                  <div class="r-item-main">
                    <span class="r-item-name">{{ item.foodNameSnapshot || item.foodName || 'Món ăn' }}</span>
                    <span class="r-item-qty">{{ item.quantity || 1 }}</span>
                    <span class="r-item-price">{{ formatShortCurrency(item.unitPrice) }}</span>
                    <span class="r-item-total">{{ formatShortCurrency(item.lineTotal || (item.unitPrice * (item.quantity || 1))) }}</span>
                  </div>
                  <div v-if="item.variantSnapshot && item.variantSnapshot !== 'Mặc định'" class="r-item-variant">
                    » {{ item.variantSnapshot }}
                  </div>
                  <div v-if="item.note" class="r-item-note">
                    * {{ item.note }}
                  </div>
                </div>

                <div v-if="invoiceItems.length === 0" class="r-empty-row">
                  (Chưa có món)
                </div>
              </div>

              <div class="receipt-divider-dashed">-----------------------------------------</div>

              <!-- Tổng tiền & Giảm trừ -->
              <div class="receipt-summary">
                <div class="r-sum-row">
                  <span>Tạm tính:</span>
                  <span>{{ formatCurrency(currentInvoice.subtotal) }}</span>
                </div>

                <div v-if="voucherDiscountAmount > 0" class="r-sum-row r-discount">
                  <span>Ưu đãi Voucher{{ currentInvoice.voucherCode ? ` (${currentInvoice.voucherCode})` : '' }}:</span>
                  <span>-{{ formatCurrency(voucherDiscountAmount) }}</span>
                </div>

                <div v-if="pointDiscountAmount > 0" class="r-sum-row r-discount">
                  <span>Trừ điểm ({{ currentInvoice.pointsUsed }} điểm):</span>
                  <span>-{{ formatCurrency(pointDiscountAmount) }}</span>
                </div>

                <div v-if="totalDiscountWithoutDetail > 0" class="r-sum-row r-discount">
                  <span>Giảm giá khác:</span>
                  <span>-{{ formatCurrency(totalDiscountWithoutDetail) }}</span>
                </div>

                <div class="receipt-divider-double">=========================================</div>

                <div class="r-grand-total">
                  <span>TỔNG CỘNG:</span>
                  <span class="r-total-price">{{ formatCurrency(finalTotalAmount) }}</span>
                </div>

                <div class="receipt-divider-double">=========================================</div>

                <!-- Phương thức thanh toán K80 -->
                <div class="r-payment-info">
                  <div class="r-sum-row">
                    <span>Thanh toán:</span>
                    <strong>{{ getPaymentMethod(resolvedPaymentMethod).toUpperCase() }}</strong>
                  </div>
                  <div v-if="transactionCode" class="r-sum-row small">
                    <span>Mã GD:</span>
                    <span>{{ transactionCode }}</span>
                  </div>
                  <div v-if="isCashPayment && currentInvoice.amountPaidByCustomer > 0" class="r-sum-row small">
                    <span>Tiền khách đưa:</span>
                    <span>{{ formatCurrency(currentInvoice.amountPaidByCustomer) }}</span>
                  </div>
                  <div v-if="isCashPayment && changeReturnAmount > 0" class="r-sum-row small">
                    <span>Tiền thừa trả khách:</span>
                    <span>{{ formatCurrency(changeReturnAmount) }}</span>
                  </div>
                  <div v-if="currentInvoice.pointsEarned > 0" class="r-sum-row r-loyalty small">
                    <span>Điểm tích lũy mới:</span>
                    <span>+{{ currentInvoice.pointsEarned }} điểm</span>
                  </div>
                  <div v-if="customerPointsBalance !== null" class="r-sum-row r-loyalty small">
                    <span>Số dư điểm:</span>
                    <span>{{ customerPointsBalance }} điểm</span>
                  </div>
                </div>
              </div>

              <div class="receipt-divider-dashed">-----------------------------------------</div>

              <!-- Lời chào & Ghi chú cuối phiếu -->
              <div class="receipt-footer">
                <div class="r-footer-text">Wifi: <strong>DolaRestaurant</strong> · Pass: <strong>dola8888</strong></div>
                <div class="r-footer-text mt-1">* Quý khách vui lòng kiểm tra lại món ăn và tiền thừa.</div>
                <div class="r-footer-farewell mt-2">CẢM ƠN QUÝ KHÁCH & HẸN GẶP LẠI!</div>
                <div class="receipt-barcode">
                  ||||| | |||||| || |||||||| | ||||| |||||||
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>

      <!-- 3. FOOTER ACTIONS -->
      <footer v-if="!receiptOnly" class="modal-footer-bar">
        <div class="footer-left text-muted small d-none d-md-block">
          <span>Nhấn <strong>Ctrl + P</strong> để in nhanh hoặc <strong>Esc</strong> để đóng</span>
        </div>
        <div class="footer-buttons">
          <button type="button" class="btn-footer-close" @click="$emit('close')">
            Đóng
          </button>
          <button type="button" class="btn-footer-print" @click="handlePrint">
            <span>🖨️</span> In Hóa Đơn K80
          </button>
        </div>
      </footer>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import axios from 'axios';

const props = defineProps({
  show: Boolean,
  invoice: Object,
  tableDisplay: String,
  cashierName: String,
  printActionLabel: { type: String, default: 'In lại hóa đơn' },
  receiptOnly: { type: Boolean, default: false }
});

const emit = defineEmits(['close']);

const apiBase = import.meta.env.VITE_API_URL || 'http://localhost:5079';
const paymentApiBase = `${apiBase}/api/staff/payment`;

const fetchedInvoiceDetail = ref(null);
const loadingDetail = ref(false);
const copiedCode = ref(false);
const activeTab = ref('audit'); // 'audit' | 'print' for mobile

watch(
  () => [props.show, props.invoice?.invoiceId],
  async ([isVisible, invoiceId]) => {
    fetchedInvoiceDetail.value = null;
    if (!isVisible || !invoiceId) return;

    // Nếu object invoice truyền vào chưa có chi tiết items thì chủ động fetch
    if (!props.invoice?.items?.length && !props.invoice?.invoiceItems?.length) {
      loadingDetail.value = true;
      try {
        const response = await axios.get(`${paymentApiBase}/invoices/${invoiceId}`);
        fetchedInvoiceDetail.value = response.data;
      } catch (error) {
        console.warn('Không thể tải chi tiết hóa đơn:', error);
      } finally {
        loadingDetail.value = false;
      }
    }
  },
  { immediate: true }
);

// Bắt phím tắt Esc & Ctrl+P
function handleGlobalKeyDown(e) {
  if (!props.show) return;

  if (e.key === 'Escape') {
    emit('close');
  } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'p') {
    e.preventDefault();
    handlePrint();
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalKeyDown);
});

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalKeyDown);
});

const currentInvoice = computed(() => fetchedInvoiceDetail.value || props.invoice || {});
const invoiceItems = computed(() => currentInvoice.value.items || currentInvoice.value.invoiceItems || []);

const invoiceCodeDisplay = computed(() => {
  return currentInvoice.value.invoiceCode || currentInvoice.value.invoiceId || currentInvoice.value.orderId || '—';
});

const resolvedTableDisplay = computed(() => {
  return props.tableDisplay || currentInvoice.value.tableDisplay || currentInvoice.value.tableName || `Bàn ${currentInvoice.value.orderId || '—'}`;
});

const customerName = computed(() => {
  const raw = currentInvoice.value.customerName || currentInvoice.value.customer?.fullName || '';
  return raw.replace(/\s*\((?:Khách\s+)?(?:Mới|Cũ|mới|cũ|New|Old|VIP|Thành viên|Hội viên)\)/gi, '').trim();
});

const customerPhone = computed(() => {
  return currentInvoice.value.customerPhone || currentInvoice.value.customer?.phone || '';
});

const customerDisplay = computed(() => {
  const name = customerName.value;
  if (!name) return '';
  const phone = customerPhone.value;
  return phone ? `${name} (${phone})` : name;
});

const customerPointsBalance = computed(() => {
  return currentInvoice.value.customer?.pointsBalance ?? null;
});

const hasCustomerLoyalty = computed(() => {
  return !!customerName.value || currentInvoice.value.pointsUsed > 0 || currentInvoice.value.pointsEarned > 0;
});

const resolvedPaymentMethod = computed(() => {
  return currentInvoice.value.paymentMethod || currentInvoice.value.payments?.[0]?.paymentMethod || 'cash';
});

const isCashPayment = computed(() => {
  return (resolvedPaymentMethod.value || '').toLowerCase() === 'cash';
});

const transactionCode = computed(() => {
  return currentInvoice.value.payments?.[0]?.transactionCode || currentInvoice.value.transactionCode || '';
});

const pointDiscountAmount = computed(() => {
  const inv = currentInvoice.value;
  if (inv.pointDiscount !== undefined && inv.pointDiscount !== null && Number(inv.pointDiscount) > 0) {
    return Number(inv.pointDiscount);
  }
  if (inv.pointsUsed > 0) return Number(inv.pointsUsed) * 1000;
  return 0;
});

const voucherDiscountAmount = computed(() => {
  const inv = currentInvoice.value;
  if (inv.voucherDiscount !== undefined && inv.voucherDiscount !== null && Number(inv.voucherDiscount) > 0) {
    return Number(inv.voucherDiscount);
  }
  const total = Number(inv.discountAmount || 0);
  const pointDisc = pointDiscountAmount.value;
  return total > pointDisc ? total - pointDisc : 0;
});

const totalDiscountWithoutDetail = computed(() => {
  const total = currentInvoice.value.discountAmount || 0;
  const known = voucherDiscountAmount.value + pointDiscountAmount.value;
  return total > known ? total - known : 0;
});

const finalTotalAmount = computed(() => {
  return currentInvoice.value.totalAmount || currentInvoice.value.finalAmount || currentInvoice.value.subtotal || 0;
});

const changeReturnAmount = computed(() => {
  const inv = currentInvoice.value;
  const paid = inv.amountPaidByCustomer || 0;
  return paid > finalTotalAmount.value ? paid - finalTotalAmount.value : (inv.changeToReturn || 0);
});

function formatCurrency(value) {
  if (!value && value !== 0) return '0 đ';
  return `${new Intl.NumberFormat('vi-VN').format(value)} đ`;
}

function formatShortCurrency(value) {
  if (!value && value !== 0) return '0';
  return new Intl.NumberFormat('vi-VN').format(value);
}

function formatDateTime(value) {
  if (!value) return new Date().toLocaleString('vi-VN', { hour12: false });
  let str = String(value).trim();
  if (!str.endsWith('Z') && !str.includes('+') && !/-\d{2}:\d{2}$/.test(str)) {
    str += 'Z';
  }
  return new Date(str).toLocaleString('vi-VN', { hour12: false });
}

function getPaymentMethod(method) {
  const m = (method || '').toLowerCase();
  return {
    cash: 'Tiền mặt',
    transfer: 'Chuyển khoản VietQR',
    vietqr: 'Chuyển khoản VietQR',
    bank: 'Chuyển khoản Ngân hàng',
    card: 'Thẻ thanh toán POS',
    e_wallet: 'Ví điện tử'
  }[m] || method || 'Tiền mặt';
}

function getMethodIcon(method) {
  const m = (method || '').toLowerCase();
  if (m === 'cash') return '💵';
  if (m === 'transfer' || m === 'vietqr' || m === 'bank') return '📱';
  if (m === 'card') return '💳';
  if (m === 'e_wallet') return '👛';
  return '💰';
}

function getMethodBadgeClass(method) {
  const m = (method || '').toLowerCase();
  if (m === 'cash') return 'badge-cash';
  if (m === 'transfer' || m === 'vietqr' || m === 'bank') return 'badge-transfer';
  return 'badge-other';
}

async function copyInvoiceCode() {
  const code = String(invoiceCodeDisplay.value);
  if (!code || code === '—') return;

  try {
    await navigator.clipboard.writeText(code);
    copiedCode.value = true;
    setTimeout(() => {
      copiedCode.value = false;
    }, 2000);
  } catch (err) {
    console.warn('Lỗi sao chép mã:', err);
  }
}

function handlePrint() {
  window.print();
}
</script>

<style scoped>
.invoice-modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 1100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  background: rgba(15, 23, 42, 0.7);
  backdrop-filter: blur(6px);
  animation: fadeIn 0.18s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

.invoice-modal-dialog {
  width: min(1140px, 100%);
  max-height: 94vh;
  display: flex;
  flex-direction: column;
  background: #ffffff;
  border-radius: 16px;
  box-shadow: 0 25px 65px -15px rgba(15, 23, 42, 0.4);
  overflow: hidden;
  border: 1px solid rgba(226, 232, 240, 0.9);
}

/* Compact receipt view used when an invoice is opened from reports. */
.receipt-only-dialog {
  width: min(620px, 100%);
  background: #e2e8f0;
}

.receipt-only-dialog .modal-toolbar {
  padding: 10px 14px;
  background: #ffffff;
  color: #172033;
  border-bottom: 1px solid #dbe3ee;
}

.receipt-only-dialog .toolbar-brand-badge,
.receipt-only-dialog .status-pill,
.receipt-only-dialog .view-switch-tabs {
  display: none;
}

.receipt-only-dialog .toolbar-left {
  gap: 0;
}

.receipt-only-dialog .toolbar-title {
  color: #172033;
  font-size: 0.92rem;
}

.receipt-only-dialog .inv-code-tag {
  background: #f8fafc;
  border-color: #cbd5e1;
  color: #475569;
}

.receipt-only-dialog .btn-close-toolbar {
  background: #f8fafc;
  border-color: #cbd5e1;
  color: #475569;
}

.receipt-only-dialog .modal-body-wrapper {
  max-height: calc(94vh - 70px);
  background: #e2e8f0;
}

.receipt-only-dialog .split-view-container {
  display: block;
  padding: 16px;
}

.receipt-only-dialog .receipt-only-panel {
  display: flex !important;
  width: 100%;
}

.receipt-only-dialog .receipt-only-panel .thermal-receipt-paper {
  width: 100%;
  max-width: 540px;
  padding: 28px 26px 30px;
  border-radius: 10px;
}

.receipt-only-dialog .receipt-only-panel .restaurant-name {
  font-size: 1.45rem;
}

.receipt-only-dialog .receipt-only-panel .receipt-title {
  font-size: 1.05rem;
}

/* 1. TOOLBAR / HEADER */
.modal-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  background: linear-gradient(135deg, #0f766e 0%, #115e59 100%);
  color: #ffffff;
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.toolbar-brand-badge {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.25);
  border-radius: 12px;
}

.h-svg-icon {
  width: 22px;
  height: 22px;
  stroke: #ffffff;
}

.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.status-paid {
  background: rgba(16, 185, 129, 0.2);
  border: 1px solid rgba(16, 185, 129, 0.4);
  color: #34d399;
}

.dot-success {
  width: 6px;
  height: 6px;
  background: #10b981;
  border-radius: 50%;
}

.inv-code-tag {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 8px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.18);
  border-radius: 6px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.76rem;
  font-weight: 700;
  color: #93c5fd;
}

.btn-copy-code {
  background: transparent;
  border: none;
  color: #bfdbfe;
  font-size: 0.72rem;
  cursor: pointer;
  padding: 0 2px;
  transition: color 0.15s ease;
}

.btn-copy-code:hover {
  color: #ffffff;
}

.toolbar-title {
  font-size: 1.05rem;
  font-weight: 700;
  margin-top: 4px;
  letter-spacing: -0.01em;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.view-switch-tabs {
  display: flex;
  background: rgba(255, 255, 255, 0.1);
  padding: 3px;
  border-radius: 8px;
}

.switch-tab {
  padding: 6px 12px;
  background: transparent;
  border: none;
  border-radius: 6px;
  color: #cbd5e1;
  font-size: 0.76rem;
  font-weight: 600;
  cursor: pointer;
}

.switch-tab.active {
  background: #3b82f6;
  color: #ffffff;
}

.btn-primary-print {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 18px;
  background: #059669;
  border: 1px solid #10b981;
  border-radius: 8px;
  color: #ffffff;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
  box-shadow: 0 4px 12px rgba(5, 150, 105, 0.25);
}

.btn-primary-print:hover {
  background: #047857;
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(5, 150, 105, 0.35);
}

.btn-close-toolbar {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  color: #cbd5e1;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-close-toolbar:hover {
  background: rgba(239, 68, 68, 0.25);
  color: #fca5a5;
  border-color: rgba(239, 68, 68, 0.4);
}

/* 2. BODY LAYOUT */
.modal-body-wrapper {
  flex: 1;
  overflow-y: auto;
  max-height: calc(94vh - 130px);
  background: #f1f5f9;
}

.split-view-container {
  display: grid;
  grid-template-columns: minmax(0, 1.35fr) minmax(360px, 1fr);
  gap: 16px;
  padding: 18px 24px;
}

/* LEFT: AUDIT PANEL */
.audit-panel {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.audit-card {
  padding: 16px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.03);
}

.audit-card-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.head-icon {
  font-size: 1.1rem;
}

.head-title {
  font-size: 0.84rem;
  font-weight: 700;
  color: #334155;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.audit-grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px 16px;
}

.info-block {
  display: flex;
  flex-direction: column;
}

.info-label {
  font-size: 0.72rem;
  color: #64748b;
  margin-bottom: 2px;
}

.info-val {
  font-size: 0.85rem;
  color: #1e293b;
}

/* Loyalty Strip */
.loyalty-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  padding: 10px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
}

.loyalty-item {
  display: flex;
  flex-direction: column;
}

.loy-label {
  font-size: 0.7rem;
  color: #64748b;
}

.loy-val {
  font-size: 0.84rem;
}

/* Highlight Card */
.highlight-card {
  background: #f8fafc;
  border-color: #cbd5e1;
}

.payment-summary-box {
  background: #ffffff;
  padding: 12px 14px;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
}

.payment-method-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.82rem;
  font-weight: 700;
}

.badge-cash {
  background: #f0fdf4;
  color: #15803d;
  border: 1px solid #bbf7d0;
}

.badge-transfer {
  background: #eef2ff;
  color: #4338ca;
  border: 1px solid #c7d2fe;
}

.badge-other {
  background: #faf5ff;
  color: #7e22ce;
  border: 1px solid #e9d5ff;
}

.trans-code-badge {
  font-size: 0.76rem;
  color: #475569;
}

.cash-calc-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  padding-top: 10px;
  border-top: 1px dashed #e2e8f0;
}

.cash-calc-item {
  display: flex;
  flex-direction: column;
}

.cash-calc-item span {
  font-size: 0.72rem;
  color: #64748b;
}

/* Items Table */
.items-audit-table {
  font-size: 0.8rem;
}

.items-audit-table thead th {
  padding: 10px 12px;
  background: #f8fafc;
  color: #475569;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  border-bottom: 1px solid #e2e8f0;
}

.items-audit-table tbody td {
  padding: 10px 12px;
  border-bottom: 1px solid #f1f5f9;
}

.item-name-block {
  display: flex;
  flex-direction: column;
}

.item-name-text {
  color: #1e293b;
  font-size: 0.82rem;
}

.variant-tag {
  font-size: 0.7rem;
  color: #64748b;
  margin-top: 2px;
}

.note-tag {
  font-size: 0.7rem;
  color: #b45309;
  font-style: italic;
  margin-top: 2px;
}

/* Financial Breakdown */
.summary-breakdown-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.summary-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.84rem;
  color: #475569;
}

.discount-line {
  color: #dc2626;
}

.grand-total-line {
  margin-top: 6px;
  padding-top: 10px;
  border-top: 2px solid #0f172a;
}

.total-label {
  font-size: 0.92rem;
  font-weight: 800;
  color: #0f172a;
  letter-spacing: 0.02em;
}

.total-value {
  font-size: 1.35rem;
  font-weight: 900;
  color: #b91c1c;
}

/* RIGHT: RECEIPT PANEL */
.receipt-panel {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.receipt-panel-header {
  width: 100%;
  max-width: 380px;
}

.preview-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  color: #475569;
  text-transform: uppercase;
}

.dot-preview {
  width: 6px;
  height: 6px;
  background: #3b82f6;
  border-radius: 50%;
}

.btn-instant-print {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  color: #1e293b;
  font-size: 0.75rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-instant-print:hover {
  background: #0f172a;
  color: #ffffff;
  border-color: #0f172a;
}

/* MÔ PHỎNG GIẤY IN NHIỆT K80 */
.thermal-receipt-paper {
  width: 100%;
  max-width: 380px;
  background: #ffffff;
  padding: 24px 20px 28px;
  border-radius: 4px;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.12), 0 1px 3px rgba(0, 0, 0, 0.08);
  border: 1px solid #e2e8f0;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  color: #000000;
  position: relative;
}

.receipt-header {
  text-align: center;
  margin-bottom: 10px;
}

.restaurant-name {
  font-family: Inter, system-ui, -apple-system, sans-serif;
  font-size: 1.25rem;
  font-weight: 900;
  letter-spacing: -0.01em;
  color: #000000;
}

.restaurant-sub {
  font-size: 0.72rem;
  color: #000000;
  margin-bottom: 4px;
}

.restaurant-info {
  font-size: 0.68rem;
  color: #000000;
  line-height: 1.35;
}

.receipt-divider-dashed {
  font-size: 0.7rem;
  color: #000000;
  margin: 6px 0;
  letter-spacing: -0.05em;
  overflow: hidden;
  white-space: nowrap;
}

.receipt-divider-dotted {
  font-size: 0.68rem;
  color: #000000;
  margin: 3px 0;
  letter-spacing: 0.02em;
  overflow: hidden;
  white-space: nowrap;
}

.receipt-divider-double {
  font-size: 0.7rem;
  color: #000000;
  margin: 4px 0;
  letter-spacing: -0.05em;
  overflow: hidden;
  white-space: nowrap;
}

.receipt-title {
  font-family: Inter, system-ui, sans-serif;
  font-size: 0.95rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  margin-top: 4px;
}

.receipt-status-text {
  font-size: 0.68rem;
  font-weight: 700;
  color: #000000;
  letter-spacing: 0.05em;
  margin-top: 2px;
}

.receipt-meta {
  display: flex;
  flex-direction: column;
  gap: 3px;
  font-size: 0.72rem;
}

.meta-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.customer-row {
  border-top: 1px dotted #000000;
  padding-top: 2px;
  margin-top: 2px;
}

/* Items in Receipt */
.receipt-items-table {
  margin: 6px 0;
  font-size: 0.72rem;
}

.r-table-header {
  display: flex;
  justify-content: space-between;
  font-weight: 800;
  font-size: 0.68rem;
}

.col-name { flex: 1; text-align: left; }
.col-qty { width: 30px; text-align: center; }
.col-price { width: 65px; text-align: right; }
.col-total { width: 75px; text-align: right; }

.r-item-row {
  padding: 4px 0;
  border-bottom: 1px dotted #000000;
}

.r-item-main {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
}

.r-item-name {
  flex: 1;
  font-weight: 700;
  padding-right: 4px;
  word-break: break-word;
}

.r-item-qty {
  width: 30px;
  text-align: center;
}

.r-item-price {
  width: 65px;
  text-align: right;
  color: #000000;
}

.r-item-total {
  width: 75px;
  text-align: right;
  font-weight: 700;
}

.r-item-variant {
  font-size: 0.65rem;
  color: #000000;
  padding-left: 6px;
}

.r-item-note {
  font-size: 0.65rem;
  color: #000000;
  font-style: italic;
  padding-left: 6px;
}

.r-empty-row {
  text-align: center;
  padding: 8px 0;
  color: #000000;
}

/* Receipt Summary */
.receipt-summary {
  display: flex;
  flex-direction: column;
  gap: 3px;
  font-size: 0.72rem;
}

.r-sum-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.r-discount {
  color: #000000;
}

.r-grand-total {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-family: Inter, system-ui, sans-serif;
  font-weight: 900;
  font-size: 0.92rem;
  padding: 3px 0;
}

.r-total-price {
  font-size: 1.15rem;
  color: #000000;
}

.r-payment-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin-top: 2px;
}

.r-loyalty {
  color: #000000;
  font-weight: 700;
}

/* Receipt Footer */
.receipt-footer {
  text-align: center;
  margin-top: 8px;
}

.r-footer-text {
  font-size: 0.65rem;
  color: #000000;
}

.r-footer-farewell {
  font-family: Inter, system-ui, sans-serif;
  font-size: 0.76rem;
  font-weight: 800;
  letter-spacing: 0.04em;
}

.receipt-barcode {
  letter-spacing: 2px;
  font-size: 0.85rem;
  margin-top: 6px;
  color: #000000;
}

/* 3. FOOTER */
.modal-footer-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 24px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
}

.footer-buttons {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
}

.btn-footer-close {
  padding: 8px 18px;
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  color: #334155;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-footer-close:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.btn-footer-print {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 20px;
  background: #059669;
  border: 1px solid #10b981;
  border-radius: 8px;
  color: #ffffff;
  font-size: 0.82rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-footer-print:hover {
  background: #047857;
}

@media (max-width: 991px) {
  .split-view-container {
    grid-template-columns: 1fr;
    padding: 14px 16px;
  }

  .thermal-receipt-paper {
    max-width: 100%;
  }
}

@media (max-width: 640px) {
  .invoice-modal-overlay {
    padding: 6px;
  }

  .invoice-modal-dialog {
    max-height: 98vh;
    border-radius: 12px;
  }

  .modal-toolbar {
    padding: 12px 14px;
  }

  .modal-footer-bar {
    padding: 10px 14px;
  }

  .audit-grid-2 {
    grid-template-columns: 1fr;
    gap: 8px;
  }
}

/* ========================================================= */
/* ĐỊNH DẠNG IN NHIỆT K80 CHUYÊN NGHIỆP (@media print)       */
/* ========================================================= */
@media print {
  /* Ẩn toàn bộ giao diện màn hình web, header, buttons, backdrop */
  :global(body *) {
    visibility: hidden !important;
  }

  /* Chỉ hiển thị khu vực phiếu in K80 */
  #printableInvoice,
  #printableInvoice * {
    visibility: visible !important;
  }

  #printableInvoice {
    position: absolute !important;
    left: 0 !important;
    top: 0 !important;
    width: 76mm !important;
    max-width: 76mm !important;
    margin: 0 !important;
    padding: 2mm 1mm !important;
    background: #ffffff !important;
    color: #000000 !important;
    box-shadow: none !important;
    border: none !important;
    font-size: 8pt !important;
    line-height: 1.25 !important;
  }

  .receipt-header .restaurant-name {
    font-size: 11pt !important;
  }

  .receipt-header .restaurant-info {
    font-size: 6.5pt !important;
  }

  .receipt-title {
    font-size: 9pt !important;
  }

  .receipt-meta {
    font-size: 7pt !important;
  }

  .receipt-items-table {
    font-size: 7pt !important;
  }

  .r-grand-total {
    font-size: 8.5pt !important;
  }

  .r-total-price {
    font-size: 10.5pt !important;
  }

  .receipt-footer {
    font-size: 6.5pt !important;
  }

  .receipt-divider-dashed,
  .receipt-divider-dotted,
  .receipt-divider-double {
    color: #000000 !important;
  }
}
</style>

<template>
  <div class="p-4 bg-slate-50 min-vh-100">
    <!-- Header Section -->
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-4 pb-2 border-bottom">
      <div>
        <h3 class="fw-bold text-dark m-0 d-flex align-items-center gap-2">
          <span>💳</span>
          <span>Quản Lý Tài Khoản Ngân Hàng & Webhook Thanh Toán</span>
        </h3>
        <p class="text-muted small mb-0 mt-1">
          Cấu hình tài khoản nhận tiền VietQR động và cổng Webhook tự động nhận kết quả thanh toán từ ngân hàng (Realtime).
        </p>
      </div>
      <div class="d-flex gap-2">
        <button class="btn btn-outline-secondary btn-sm" @click="fetchSettings" :disabled="isLoading">
          <span v-if="isLoading" class="spinner-border spinner-border-sm me-1"></span>
          <span>🔄 Tải lại</span>
        </button>
      </div>
    </div>

    <!-- Alert thông báo -->
    <div v-if="successMsg" class="alert alert-success alert-dismissible fade show shadow-xs mb-4" role="alert">
      <strong>🎉 Thành công!</strong> {{ successMsg }}
      <button type="button" class="btn-close" @click="successMsg = ''"></button>
    </div>
    <div v-if="errorMsg" class="alert alert-danger alert-dismissible fade show shadow-xs mb-4" role="alert">
      <strong>⚠️ Lỗi:</strong> {{ errorMsg }}
      <button type="button" class="btn-close" @click="errorMsg = ''"></button>
    </div>

    <div class="row g-4">
      <!-- CỘT TRÁI: CẤU HÌNH TÀI KHOẢN NGÂN HÀNG & WEBHOOK -->
      <div class="col-12 col-xl-7">
        <!-- 1. Cấu hình Tài khoản Ngân hàng (VietQR) -->
        <div class="card border-0 shadow-sm rounded-4 mb-4 overflow-hidden">
          <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
            <span class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
              <span>🏦</span>
              <span>Tài Khoản Ngân Hàng Nhận Tiền Nhà Hàng</span>
            </span>
            <span class="badge bg-success-subtle text-success fw-bold px-3 py-2">
              Chuẩn Napas 247 / VietQR
            </span>
          </div>
          <div class="card-body p-4">
            <form @submit.prevent="saveSettings">
              <div class="row g-3">
                <div class="col-md-6">
                  <label class="form-label small fw-bold text-secondary">Ngân Hàng Thụ Hưởng:</label>
                  <select v-model="form.bankId" class="form-select form-select-lg fw-semibold" required>
                    <option v-for="b in popularBanks" :key="b.code" :value="b.code">
                      {{ b.code }} - {{ b.name }}
                    </option>
                  </select>
                </div>

                <div class="col-md-6">
                  <label class="form-label small fw-bold text-secondary">Số Tài Khoản (STK):</label>
                  <input
                    v-model="form.accountNo"
                    type="text"
                    class="form-control form-control-lg font-monospace fw-bold text-primary"
                    placeholder="Ví dụ: 0333888999"
                    required
                  />
                </div>

                <div class="col-md-8">
                  <label class="form-label small fw-bold text-secondary">Tên Chủ Tài Khoản (In hoa không dấu):</label>
                  <input
                    v-model="form.accountName"
                    type="text"
                    class="form-control form-control-lg text-uppercase fw-bold"
                    placeholder="Ví dụ: NHA HANG HUONG SEN"
                    required
                  />
                </div>

                <div class="col-md-4">
                  <label class="form-label small fw-bold text-secondary">Chi Nhánh / Tỉnh Thành:</label>
                  <input
                    v-model="form.branch"
                    type="text"
                    class="form-control form-control-lg"
                    placeholder="Ví dụ: TP. Hồ Chí Minh"
                  />
                </div>
              </div>

              <!-- Xem trước thông tin tài khoản -->
              <div class="mt-4 p-3 bg-light rounded-3 border d-flex justify-content-between align-items-center">
                <div class="small">
                  <div>Ngân hàng: <strong class="text-primary">{{ form.bankId }}</strong> | STK: <strong class="font-monospace">{{ form.accountNo || '---' }}</strong></div>
                  <div>Chủ TK: <strong class="text-uppercase">{{ form.accountName || '---' }}</strong></div>
                  <div class="text-muted">Chi nhánh: {{ form.branch || 'Mặc định' }}</div>
                </div>
                <button
                  type="submit"
                  class="btn btn-primary btn-lg fw-bold px-4 shadow-sm"
                  :disabled="isSaving"
                >
                  <span v-if="isSaving" class="spinner-border spinner-border-sm me-2"></span>
                  <span>💾 Lưu Cấu Hình</span>
                </button>
              </div>
            </form>
          </div>
        </div>

        <!-- 2. Cổng Webhook Tự Động Nhận Tiền Từ Ngân Hàng (Realtime) -->
        <div class="card border-0 shadow-sm rounded-4 mb-4 overflow-hidden">
          <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
            <span class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
              <span>⚡</span>
              <span>Cổng Webhook Ngân Hàng (Tự Động Xuất Hóa Đơn Realtime)</span>
            </span>
            <span class="badge bg-primary-subtle text-primary fw-semibold">
              SignalR 0s Latency
            </span>
          </div>
          <div class="card-body p-4">
            <div class="mb-3">
              <label class="form-label small fw-bold text-secondary">Địa Chỉ Webhook URL (Cung cấp cho Ngân hàng / SePay / Casso / PayOS):</label>
              <div class="input-group">
                <input
                  type="text"
                  class="form-control font-monospace bg-light fw-semibold text-dark"
                  :value="webhookUrl"
                  readonly
                />
                <button class="btn btn-outline-primary fw-semibold px-3" type="button" @click="copyWebhookUrl">
                  {{ copied ? '✅ Đã Chép' : '📋 Copy URL' }}
                </button>
              </div>
              <small class="text-muted mt-1 d-block">
                Cổng thanh toán hoặc bot ngân hàng sẽ gửi HTTP POST về URL này mỗi khi tài khoản nhận được tiền.
              </small>
            </div>

            <div class="row g-3 mb-3">
              <div class="col-md-7">
                <label class="form-label small fw-bold text-secondary">Khóa Bí Mật Webhook (API Key SePay):</label>
                <div class="input-group">
                  <input
                    v-model="form.webhookSecret"
                    type="text"
                    class="form-control font-monospace"
                    minlength="8"
                    autocomplete="new-password"
                    placeholder="Ví dụ: SEN_SECRET_KEY_888 hoặc khóa SePay"
                  />
                  <button class="btn btn-outline-secondary btn-sm px-3" type="button" @click="copySecret" title="Sao chép khóa">
                    {{ copiedSecret ? '✅ Đã chép' : '📋 Copy' }}
                  </button>
                  <button class="btn btn-outline-primary btn-sm px-3" type="button" @click="generateRandomSecret" title="Tạo khóa ngẫu nhiên">
                    🎲 Tạo ngẫu nhiên
                  </button>
                </div>
                <small class="text-muted mt-1 d-block">
                  Dán khóa này vào mục <strong>API Key</strong> trên trang cấu hình Webhook của <a href="https://my.sepay.vn" target="_blank" rel="noopener" class="text-decoration-none fw-bold">SePay (my.sepay.vn)</a>.
                </small>
              </div>

              <div class="col-md-5">
                <label class="form-label small fw-bold text-secondary">Nhà Cung Cấp Cổng / Gateway:</label>
                <input
                  v-model="form.gatewayProvider"
                  type="text"
                  class="form-control"
                  placeholder="Ví dụ: SePay / VietQR Auto / Casso"
                />
              </div>
            </div>

            <div class="form-check form-switch mb-3 p-2 bg-light rounded-3 border ps-5">
              <input
                id="autoConfirmSwitch"
                v-model="form.isAutoConfirmEnabled"
                class="form-check-input ms-n4"
                type="checkbox"
              />
              <label class="form-check-label fw-bold text-dark" for="autoConfirmSwitch">
                Bật chế độ tự động xác nhận đơn hàng & xuất hóa đơn ngay khi nhận tiền
              </label>
              <div class="small text-muted">
                Khi khách hàng chuyển đúng số tiền với nội dung <code>ORDxxxx</code>, hệ thống tự động hoàn tất đơn hàng, giải phóng bàn và mở hóa đơn K80 trên máy phục vụ ngay lập tức.
              </div>
            </div>

            <div class="text-end">
              <button
                type="button"
                class="btn btn-outline-primary fw-bold px-4"
                :disabled="isSaving"
                @click="saveSettings"
              >
                <span v-if="isSaving" class="spinner-border spinner-border-sm me-2"></span>
                <span>💾 Cập Nhật Thiết Lập Webhook</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- CỘT PHẢI: XEM TRƯỚC VIETQR & BỘ GIẢ LẬP TEST WEBHOOK -->
      <div class="col-12 col-xl-5">
        <!-- 3. Xem trước Mã VietQR Động -->
        <div class="card border-0 shadow-sm rounded-4 mb-4 text-center p-4 bg-white">
          <h6 class="fw-bold text-secondary mb-3">📱 Xem Trước Mã VietQR Động Của Nhà Hàng</h6>
          <div class="d-flex justify-content-center mb-3">
            <div class="p-3 bg-white rounded-3 border shadow-xs" style="max-width: 250px;">
              <img
                :src="sampleQrUrl"
                alt="Mã VietQR Demo"
                class="img-fluid rounded"
                style="min-height: 220px;"
              />
            </div>
          </div>
          <div class="small text-muted">
            Mã QR trên hiển thị mẫu với số tiền <strong>100.000 VNĐ</strong> và nội dung <code>ORD_TEST</code>. Khi thanh toán tại bàn, hệ thống sẽ tự động chèn mã đơn và số tiền chính xác của khách.
          </div>
        </div>

        <!-- 4. Bộ Giả Lập & Kiểm Thử Ngân Hàng Báo Có (Webhook Simulator) -->
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden border-top border-4 border-warning">
          <div class="card-header bg-warning-subtle py-3 border-bottom d-flex justify-content-between align-items-center">
            <span class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
              <span>🧪</span>
              <span>Bộ Giả Lập Ngân Hàng Báo Có (Test Webhook)</span>
            </span>
            <span class="badge bg-warning text-dark fw-bold">Test Realtime</span>
          </div>
          <div class="card-body p-4 bg-white">
            <p class="small text-muted mb-3">
              Bạn có thể giả lập việc khách hàng vừa quét mã chuyển khoản thành công để kiểm tra màn hình Phục vụ có tự động nổ đơn và hiện hóa đơn K80 ngay không:
            </p>

            <div class="mb-3">
              <label class="form-label small fw-bold text-secondary">Chọn Đơn Hàng Đang Phục Vụ Cần Thanh Toán:</label>
              <select v-model="simOrderSelect" class="form-select form-select-lg fw-semibold" @change="onOrderSelectChange">
                <option :value="null">-- Chọn đơn hàng đang mở --</option>
                <option v-for="ord in activeOrders" :key="ord.orderId" :value="ord">
                  Đơn #{{ ord.orderId }} - {{ ord.tableDisplay }} ({{ formatCurrency(ord.totalAmount || ord.subtotal) }})
                </option>
              </select>
            </div>

            <div class="row g-2 mb-3">
              <div class="col-6">
                <label class="form-label small fw-bold text-secondary">Mã Đơn (OrderId):</label>
                <input
                  v-model.number="simOrderId"
                  type="number"
                  class="form-control fw-bold text-primary font-monospace"
                  placeholder="Ví dụ: 4"
                />
              </div>
              <div class="col-6">
                <label class="form-label small fw-bold text-secondary">Số Tiền Chuyển:</label>
                <input
                  v-model.number="simAmount"
                  type="number"
                  class="form-control fw-bold text-danger font-monospace"
                  placeholder="Ví dụ: 1060000"
                />
              </div>
              <div class="col-12">
                <label class="form-label small fw-bold text-secondary">Nội Dung Chuyển Khoản (Chứa ORDxxxx):</label>
                <input
                  v-model="simContent"
                  type="text"
                  class="form-control font-monospace"
                  placeholder="Ví dụ: ORD0004"
                />
              </div>
            </div>

            <button
              class="btn btn-warning btn-lg w-100 fw-bold shadow-sm py-2 d-flex align-items-center justify-content-center gap-2"
              :disabled="isSimulating || !simOrderId"
              @click="triggerSimulateWebhook"
            >
              <span v-if="isSimulating" class="spinner-border spinner-border-sm"></span>
              <span>⚡ BẮN WEBHOOK GIẢ LẬP (TIỀN ĐÃ VÀO TÀI KHOẢN)</span>
            </button>

            <!-- Kết quả giả lập -->
            <div v-if="simResult" class="mt-3 p-3 bg-light rounded-3 border small">
              <div class="fw-bold text-success mb-1">✅ Phản hồi từ Server:</div>
              <div>{{ simResult.message }}</div>
              <div v-if="simResult.invoice" class="mt-2 text-muted">
                Mã HĐ: <strong class="text-primary font-monospace">{{ simResult.invoice.invoiceCode }}</strong> | 
                Bàn: <strong>{{ simResult.invoice.tableDisplay }}</strong> | 
                Tổng tiền: <strong>{{ formatCurrency(simResult.invoice.totalAmount) }}</strong>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 5. LỊCH SỬ GIAO DỊCH NGÂN HÀNG GẦN ĐÂY -->
      <div class="col-12">
        <div class="card border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
            <span class="fw-bold text-dark fs-6 d-flex align-items-center gap-2">
              <span>📑</span>
              <span>Nhật Ký Giao Dịch Chuyển Khoản Ngân Hàng Gần Đây</span>
            </span>
            <button class="btn btn-sm btn-outline-secondary" @click="fetchRecentTransactions">
              🔄 Làm mới
            </button>
          </div>
          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-hover align-middle mb-0">
                <thead class="bg-light table-light small text-secondary">
                  <tr>
                    <th class="ps-4">Mã Giao Dịch</th>
                    <th>Hóa Đơn</th>
                    <th>Bàn Ăn</th>
                    <th>Khách Hàng</th>
                    <th class="text-end">Số Tiền</th>
                    <th>Thời Gian</th>
                    <th class="text-center pe-4">Trạng Thái</th>
                  </tr>
                </thead>
                <tbody class="small">
                  <tr v-if="recentTransactions.length === 0">
                    <td colspan="7" class="text-center py-4 text-muted">
                      Chưa có giao dịch chuyển khoản ngân hàng nào gần đây.
                    </td>
                  </tr>
                  <tr v-for="t in recentTransactions" :key="t.paymentId">
                    <td class="ps-4 font-monospace fw-bold text-primary">{{ t.transactionCode }}</td>
                    <td class="font-monospace fw-semibold">{{ t.invoiceCode }}</td>
                    <td><span class="badge bg-secondary-subtle text-secondary">{{ t.tableDisplay }}</span></td>
                    <td>{{ t.customerName }}</td>
                    <td class="text-end fw-bold text-success">{{ formatCurrency(t.amount) }}</td>
                    <td class="text-muted">{{ formatDateTime(t.paidAt) }}</td>
                    <td class="text-center pe-4">
                      <span class="badge bg-success-subtle text-success fw-semibold">Thành công 🟢</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import axios from 'axios';

const apiBase = 'http://localhost:5079/api/admin/payment-settings';

const popularBanks = [
  { code: 'MB', name: 'MBBank (Quân Đội)' },
  { code: 'VCB', name: 'Vietcombank (Ngoại Thương)' },
  { code: 'TCB', name: 'Techcombank (Kỹ Thương)' },
  { code: 'VPB', name: 'VPBank (Việt Nam Thịnh Vượng)' },
  { code: 'ACB', name: 'ACB (Á Châu)' },
  { code: 'BIDV', name: 'BIDV (Đầu Tư & Phát Triển)' },
  { code: 'ICB', name: 'VietinBank (Công Thương)' },
  { code: 'VBA', name: 'Agribank (Nông Nghiệp)' },
  { code: 'TPB', name: 'TPBank (Tiên Phong)' },
  { code: 'STB', name: 'Sacombank (Sài Gòn Thương Tín)' },
  { code: 'HDB', name: 'HDBank (Phát Triển TP.HCM)' },
  { code: 'VIB', name: 'VIB (Quốc Tế)' },
  { code: 'MSB', name: 'MSB (Hàng Hải)' },
  { code: 'OCB', name: 'OCB (Phương Đông)' },
  { code: 'SHB', name: 'SHB (Sài Gòn - Hà Nội)' }
];

const isLoading = ref(false);
const isSaving = ref(false);
const successMsg = ref('');
const errorMsg = ref('');
const copied = ref(false);

const form = ref({
  bankId: 'MB',
  accountNo: '',
  accountName: '',
  branch: '',
  webhookSecret: '',
  isAutoConfirmEnabled: true,
  gatewayProvider: 'SePay / VietQR Auto'
});

const webhookUrl = ref('http://localhost:5079/api/payment/bank-webhook');
const activeOrders = ref([]);

// Giả lập Webhook state
const simOrderSelect = ref(null);
const simOrderId = ref(null);
const simAmount = ref(null);
const simContent = ref('');
const isSimulating = ref(false);
const simResult = ref(null);

// Lịch sử giao dịch
const recentTransactions = ref([]);

const sampleQrUrl = computed(() => {
  const bank = form.value.bankId || 'MB';
  const acc = form.value.accountNo || '0333888999';
  const name = encodeURIComponent(form.value.accountName || 'NHA HANG HUONG SEN');
  return `https://img.vietqr.io/image/${bank}-${acc}-compact2.png?amount=100000&addInfo=ORD_TEST&accountName=${name}`;
});

async function fetchSettings() {
  isLoading.value = true;
  errorMsg.value = '';
  try {
    const res = await axios.get(apiBase);
    if (res.data) {
      if (res.data.config) {
        form.value = { ...form.value, ...res.data.config };
      }
      if (res.data.webhookUrl) {
        webhookUrl.value = res.data.webhookUrl;
      }
      activeOrders.value = res.data.activeOrders || [];
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải cấu hình thanh toán.';
  } finally {
    isLoading.value = false;
  }
}

async function saveSettings() {
  isSaving.value = true;
  errorMsg.value = '';
  successMsg.value = '';
  try {
    const res = await axios.post(apiBase, form.value);
    successMsg.value = res.data.message || 'Lưu cấu hình thành công!';
    setTimeout(() => { if (successMsg.value) successMsg.value = ''; }, 4000);
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi lưu cấu hình thanh toán.';
  } finally {
    isSaving.value = false;
  }
}

function copyWebhookUrl() {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(webhookUrl.value);
    copied.value = true;
    setTimeout(() => { copied.value = false; }, 2000);
  }
}

const copiedSecret = ref(false);
function copySecret() {
  if (navigator.clipboard && form.value.webhookSecret) {
    navigator.clipboard.writeText(form.value.webhookSecret);
    copiedSecret.value = true;
    setTimeout(() => { copiedSecret.value = false; }, 2000);
  }
}

function generateRandomSecret() {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_';
  let result = 'SEPAY_';
  for (let i = 0; i < 26; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  form.value.webhookSecret = result;
}

function onOrderSelectChange() {
  if (simOrderSelect.value) {
    simOrderId.value = simOrderSelect.value.orderId;
    simAmount.value = simOrderSelect.value.totalAmount || simOrderSelect.value.subtotal;
    simContent.value = `ORD${String(simOrderSelect.value.orderId).padStart(4, '0')}`;
  } else {
    simOrderId.value = null;
    simAmount.value = null;
    simContent.value = '';
  }
}

async function triggerSimulateWebhook() {
  if (!simOrderId.value) return;
  isSimulating.value = true;
  simResult.value = null;
  errorMsg.value = '';
  try {
    const res = await axios.post(`${apiBase}/simulate-webhook`, {
      orderId: simOrderId.value,
      amount: simAmount.value,
      content: simContent.value
    });
    simResult.value = res.data;
    successMsg.value = 'Đã gửi Webhook thành công! Màn hình Phục vụ và Bếp đã được thông báo realtime qua SignalR.';
    await fetchSettings();
    await fetchRecentTransactions();
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi thực hiện giả lập Webhook.';
  } finally {
    isSimulating.value = false;
  }
}

async function fetchRecentTransactions() {
  try {
    const res = await axios.get(`${apiBase}/recent-transactions`);
    recentTransactions.value = res.data || [];
  } catch (err) {
    console.error('Lỗi tải lịch sử giao dịch:', err);
  }
}

function formatCurrency(val) {
  if (val === null || val === undefined) return '0 đ';
  return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
}

function formatDateTime(d) {
  if (!d) return '';
  const date = new Date(d);
  return date.toLocaleString('vi-VN', { hour12: false });
}

onMounted(async () => {
  await Promise.all([fetchSettings(), fetchRecentTransactions()]);
});
</script>

<style scoped>
.shadow-xs {
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}
</style>

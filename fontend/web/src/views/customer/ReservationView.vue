<template>
  <div class="dola-theme py-5">
    <div class="container py-3">

      <!-- Header -->
      <div class="text-center mb-5">
        <span class="dola-pill-badge mb-2">📅 Dịch Vụ Tiếp Đón</span>
        <h1 class="dola-font-title text-gold display-4 fw-bold">Đặt Bàn Trực Tuyến</h1>
        <p class="text-body-dola mx-auto fs-5" style="max-width: 600px;">
          Giữ chỗ hoàn toàn miễn phí. Nhà hàng Hương Sen hân hạnh đón tiếp và chuẩn bị chu đáo nhất cho buổi tiệc của quý khách.
        </p>
      </div>

      <!-- Mode Selector Tabs -->
      <div class="d-flex justify-content-center mb-5">
        <div class="p-1 rounded-pill" style="background: var(--dola-surface-alt); border: 1px solid var(--dola-hairline);">
          <button
            class="btn rounded-pill px-4 py-2 fw-bold"
            :class="activeTab === 'book' ? 'dola-btn-primary' : 'text-body-dola'"
            @click="activeTab = 'book'"
          >
            ✍️ Tạo Phiếu Đặt Bàn Mới
          </button>
          <button
            class="btn rounded-pill px-4 py-2 fw-bold ms-1"
            :class="activeTab === 'lookup' ? 'dola-btn-primary' : 'text-body-dola'"
            @click="switchToLookupTab"
          >
            🔍 Tra Cứu &amp; Quản Lý Lịch Đặt
          </button>
        </div>
      </div>

      <!-- Main Layout -->
      <div class="row g-4 justify-content-center">

        <!-- Left Column: Form or Lookup -->
        <div class="col-12 col-lg-8">

          <!-- TAB 1: BOOKING FORM -->
          <div v-if="activeTab === 'book'">

            <!-- Success State -->
            <div v-if="bookingSuccess" class="dola-card p-5 text-center">
              <div class="fs-1 mb-3">🎉</div>
              <h3 class="dola-font-title text-gold fs-1 mb-2">Đặt Bàn Thành Công!</h3>
              <p class="text-body-dola mb-4">
                Nhà hàng Hương Sen đã tiếp nhận yêu cầu đặt bàn của quý khách. Nhân viên sẽ liên hệ lại trong ít phút để xác nhận.
              </p>

              <div class="p-4 rounded-3 text-start mx-auto mb-4" style="max-width: 480px; background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);">
                <div class="d-flex justify-content-between mb-2">
                  <span class="text-white-50">Mã phiếu:</span>
                  <strong class="text-gold">#{{ bookingResult?.reservationId }}</strong>
                </div>
                <div class="d-flex justify-content-between mb-2">
                  <span class="text-white-50">Khách hàng:</span>
                  <strong class="text-white">{{ bookingResult?.customerName }}</strong>
                </div>
                <div class="d-flex justify-content-between mb-2">
                  <span class="text-white-50">Số điện thoại:</span>
                  <strong class="text-white">{{ bookingResult?.customerPhone }}</strong>
                </div>
                <div class="d-flex justify-content-between mb-2">
                  <span class="text-white-50">Số khách:</span>
                  <strong class="text-white">{{ bookingResult?.guestCount }} người</strong>
                </div>
                <div class="d-flex justify-content-between">
                  <span class="text-white-50">Thời gian hẹn:</span>
                  <strong class="text-gold">{{ formatDateTime(bookingResult?.reservationDate) }}</strong>
                </div>
              </div>

              <div class="d-flex justify-content-center gap-3">
                <button class="dola-btn-outline" @click="resetBooking">
                  Đặt Thêm Bàn Khác
                </button>
                <button class="dola-btn-primary" @click="activeTab = 'lookup'; handleLookup();">
                  Xem Lịch Sử Đặt Bàn
                </button>
              </div>
            </div>

            <!-- Booking Form -->
            <div v-else class="dola-card p-4 p-md-5">
              <form @submit.prevent="handleSubmitBooking">

                <!-- Section 1: Customer Contact -->
                <div class="d-flex align-items-center gap-2 mb-4 pb-2 border-bottom border-dola-gold">
                  <span class="dola-step-badge">1</span>
                  <h4 class="text-white fw-bold mb-0">Thông Tin Liên Hệ</h4>
                </div>

                <div class="row g-3 mb-4">
                  <div class="col-md-6">
                    <label class="form-label text-body-dola small fw-semibold">Họ và tên quý khách *</label>
                    <input
                      type="text"
                      class="dola-input"
                      placeholder="Ví dụ: Nguyễn Văn An"
                      v-model="bookingForm.customerName"
                      :readonly="isAuthenticated"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label text-body-dola small fw-semibold">Số điện thoại liên hệ *</label>
                    <input
                      type="tel"
                      class="dola-input"
                      placeholder="Ví dụ: 0912 345 678"
                      v-model="bookingForm.customerPhone"
                      :readonly="isAuthenticated"
                      required
                    />
                  </div>
                  <div class="col-12">
                    <label class="form-label text-body-dola small fw-semibold">Địa chỉ Email (tùy chọn - nhận xác nhận)</label>
                    <input
                      type="email"
                      class="dola-input"
                      placeholder="email.quykhach@gmail.com"
                      v-model="bookingForm.email"
                      :readonly="isAuthenticated"
                    />
                  </div>
                </div>

                <!-- Section 2: Reservation Details -->
                <div class="d-flex align-items-center gap-2 mb-4 pb-2 border-bottom border-dola-gold">
                  <span class="dola-step-badge">2</span>
                  <h4 class="text-white fw-bold mb-0">Chi Tiết Lịch Hẹn</h4>
                </div>

                <div class="row g-3 mb-4">
                  <div class="col-md-6">
                    <label class="form-label text-body-dola small fw-semibold">Ngày dùng bữa *</label>
                    <input
                      type="date"
                      class="dola-input"
                      :min="minDate"
                      v-model="bookingForm.date"
                      required
                    />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label text-body-dola small fw-semibold">Khung giờ đón khách *</label>
                    <select class="dola-input" v-model="bookingForm.time" required>
                      <optgroup label="Buổi Trưa (11:00 – 14:00)">
                        <option value="11:00">11:00 Trưa</option>
                        <option value="11:30">11:30 Trưa</option>
                        <option value="12:00">12:00 Trưa</option>
                        <option value="12:30">12:30 Trưa</option>
                        <option value="13:00">13:00 Chiều</option>
                      </optgroup>
                      <optgroup label="Buổi Tối (17:30 – 21:30)">
                        <option value="17:30">17:30 Chiều</option>
                        <option value="18:00">18:00 Tối</option>
                        <option value="18:30">18:30 Tối</option>
                        <option value="19:00">19:00 Tối (Cao điểm)</option>
                        <option value="19:30">19:30 Tối</option>
                        <option value="20:00">20:00 Tối</option>
                        <option value="20:30">20:30 Tối</option>
                      </optgroup>
                    </select>
                  </div>

                  <!-- Guest Count Counter -->
                  <div class="col-12">
                    <label class="form-label d-flex justify-content-between text-body-dola small fw-semibold">
                      <span>Số lượng khách dự kiến *</span>
                      <strong class="text-gold fs-6">{{ bookingForm.guestCount }} khách</strong>
                    </label>
                    <div class="d-flex align-items-center gap-2 mb-2">
                      <button
                        type="button"
                        class="dola-counter-btn"
                        @click="bookingForm.guestCount = Math.max(1, bookingForm.guestCount - 1)"
                      >
                        −
                      </button>
                      <input
                        type="number"
                        min="1"
                        max="100"
                        class="dola-input text-center fw-bold fs-5"
                        v-model.number="bookingForm.guestCount"
                      />
                      <button
                        type="button"
                        class="dola-counter-btn"
                        @click="bookingForm.guestCount = Math.min(100, bookingForm.guestCount + 1)"
                      >
                        +
                      </button>
                    </div>
                    <!-- Quick guests select -->
                    <div class="d-flex gap-2 flex-wrap">
                      <button
                        type="button"
                        v-for="cnt in [2, 4, 6, 8, 10, 15, 20]"
                        :key="cnt"
                        class="dola-quick-btn"
                        :class="{ 'dola-quick-btn-active': bookingForm.guestCount === cnt }"
                        @click="bookingForm.guestCount = cnt"
                      >
                        {{ cnt }} khách
                      </button>
                    </div>
                  </div>

                  <!-- Special Note -->
                  <div class="col-12">
                    <label class="form-label text-body-dola small fw-semibold">Yêu cầu đặc biệt (Ghi chú)</label>
                    <textarea
                      class="dola-input"
                      rows="3"
                      placeholder="Ví dụ: Bàn gần cửa sổ, tiệc sinh nhật (cần set hoa), có trẻ em cần ghế trẻ em..."
                      v-model="bookingForm.note"
                    ></textarea>
                  </div>

                </div>

                <button
                  type="submit"
                  class="dola-btn-primary w-100 py-3 fs-5"
                  :disabled="submittingBooking"
                >
                  <span v-if="submittingBooking" class="spinner-border spinner-border-sm me-2"></span>
                  <span>{{ submittingBooking ? 'Đang gửi thông tin...' : 'Xác Nhận Đặt Bàn Ngay' }}</span>
                </button>

              </form>
            </div>
          </div>

          <!-- TAB 2: LOOKUP BOOKINGS -->
          <div v-else class="dola-card p-4 p-md-5">
            <h4 class="text-gold dola-font-title fs-2 mb-2">Tra Cứu Phiếu Đặt Bàn</h4>
            <p class="text-body-dola small mb-4">
              Nhập số điện thoại đã sử dụng để đặt bàn để xem trạng thái phê duyệt hoặc hủy lịch nếu có việc bận.
            </p>

            <div class="d-flex gap-2 mb-4">
              <input
                type="tel"
                class="dola-input"
                placeholder="Nhập số điện thoại..."
                v-model="lookupPhone"
                @keyup.enter="handleLookup"
              />
              <button class="dola-btn-primary text-nowrap" @click="handleLookup" :disabled="loadingLookup">
                <span v-if="loadingLookup" class="spinner-border spinner-border-sm me-1"></span>
                Tra Cứu
              </button>
            </div>

            <!-- Lookup Results -->
            <div v-if="hasSearched">
              <div v-if="lookupBookings.length === 0" class="p-4 text-center text-body-dola rounded-3" style="background: rgba(0,0,0,0.2);">
                <div class="fs-1 mb-2">📭</div>
                <p class="small mb-0">Không tìm thấy phiếu đặt bàn nào với số điện thoại <strong>{{ lookupPhone }}</strong></p>
              </div>

              <div v-else class="d-flex flex-column gap-3">
                <div
                  v-for="b in lookupBookings"
                  :key="b.reservationId"
                  class="p-4 rounded-3"
                  style="background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);"
                >
                  <div class="d-flex justify-content-between align-items-start mb-2">
                    <div>
                      <span class="fw-bold text-gold fs-5">Phiếu #{{ b.reservationId }}</span>
                      <span class="badge ms-2" :class="getStatusBadgeClass(b.reservationStatus)">
                        {{ getStatusText(b.reservationStatus) }}
                      </span>
                    </div>
                    <span class="dola-pill-badge">👥 {{ b.guestCount }} khách</span>
                  </div>

                  <div class="small text-body-dola mb-2">
                    🕒 <strong>{{ formatDateTime(b.reservationDate) }}</strong> ·
                    🪑 {{ b.assignedTable ? `${b.assignedTable.tableName} (${b.assignedTable.areaName})` : 'Đang chờ xếp bàn' }}
                  </div>

                  <div v-if="b.note" class="small text-white-50 mb-3 fst-italic">
                    📝 {{ b.note }}
                  </div>

                  <div class="d-flex justify-content-between align-items-center pt-2 border-top border-dola-gold">
                    <small class="text-white-50" style="font-size: 0.75rem;">
                      Ngày tạo: {{ formatDateTime(b.createdAt) }}
                    </small>
                    <button
                      v-if="['pending', 'confirmed'].includes(b.reservationStatus)"
                      class="btn btn-outline-danger btn-sm rounded-pill px-3"
                      @click="cancelBooking(b)"
                    >
                      Hủy Lịch Đặt
                    </button>
                  </div>
                </div>
              </div>
            </div>

          </div>

        </div>

        <!-- Right Column: Sidebar Policy & Hotline -->
        <div class="col-12 col-lg-4">
          <div class="d-flex flex-column gap-4 sticky-top" style="top: 100px;">

            <!-- Policy Card -->
            <div class="dola-card p-4">
              <h5 class="text-gold fw-bold mb-3 small text-uppercase" style="letter-spacing: 0.05em;">
                📋 Chính Sách Đặt Chỗ
              </h5>
              <ul class="list-unstyled d-flex flex-column gap-3 small text-body-dola mb-0" style="line-height: 1.6;">
                <li>✅ <strong class="text-white">Giữ chỗ miễn phí 100%</strong>, không bắt buộc đặt cọc trước.</li>
                <li>⏰ Nhà hàng giữ bàn tối đa <strong class="text-white">30 phút</strong> sau giờ hẹn đã đăng ký.</li>
                <li>📞 Nhân viên sẽ liên hệ xác nhận trong vòng <strong class="text-white">15 – 30 phút</strong>.</li>
                <li>❌ Quý khách có thể tự hủy lịch trên cổng trực tuyến trước giờ hẹn.</li>
                <li>👥 Đoàn tiệc &gt; 20 khách vui lòng gọi hotline để nhận ưu đãi set menu đặc biệt.</li>
              </ul>
            </div>

            <!-- Hotline Support -->
            <div class="dola-card p-4 text-center">
              <div class="fs-1 mb-2">📞</div>
              <h5 class="text-gold fw-bold mb-1">Hotline Đặt Tiệc Nhanh</h5>
              <div class="display-6 fw-bold text-white mb-2 dola-font-title">1900 6868</div>
              <p class="small text-body-dola mb-3">Tư vấn chọn món, chuẩn bị phòng tiệc VIP và trang trí sự kiện.</p>
              <a href="tel:19006868" class="dola-btn-primary w-100">
                Gọi Ngay Cho Chúng Tôi
              </a>
            </div>

          </div>
        </div>

      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import axios from 'axios';

const API_BASE = 'http://localhost:5079/api';
const isAuthenticated = Boolean(localStorage.getItem('token'));

const activeTab = ref('book');
const submittingBooking = ref(false);
const bookingSuccess = ref(false);
const bookingResult = ref(null);

const minDate = new Date().toISOString().split('T')[0];

const bookingForm = ref({
  customerName: '',
  customerPhone: '',
  email: '',
  date: minDate,
  time: '18:30',
  guestCount: 4,
  note: ''
});

// Lookup
const lookupPhone = ref(localStorage.getItem('customer_lookup_phone') || '');
const lookupBookings = ref([]);
const loadingLookup = ref(false);
const hasSearched = ref(false);

function switchToLookupTab() {
  activeTab.value = 'lookup';
  if (lookupPhone.value.trim()) {
    handleLookup();
  }
}

async function handleSubmitBooking() {
  submittingBooking.value = true;
  try {
    const reservationDate = `${bookingForm.value.date}T${bookingForm.value.time}:00`;
    const payload = {
      customerName: bookingForm.value.customerName.trim(),
      customerPhone: bookingForm.value.customerPhone.trim(),
      email: bookingForm.value.email?.trim() || null,
      reservationDate,
      guestCount: bookingForm.value.guestCount,
      note: bookingForm.value.note?.trim() || null
    };

    const res = await axios.post(`${API_BASE}/customer/reservations/book`, payload);
    if (res.data?.success) {
      bookingResult.value = res.data.data;
      bookingSuccess.value = true;
      localStorage.setItem('customer_lookup_phone', bookingForm.value.customerPhone.trim());
      lookupPhone.value = bookingForm.value.customerPhone.trim();
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Có lỗi xảy ra khi gửi yêu cầu đặt bàn. Vui lòng thử lại.');
  } finally {
    submittingBooking.value = false;
  }
}

function resetBooking() {
  bookingSuccess.value = false;
  bookingResult.value = null;
  bookingForm.value.note = '';
}

async function handleLookup() {
  if (!lookupPhone.value.trim()) {
    alert('Vui lòng nhập số điện thoại để tra cứu.');
    return;
  }
  loadingLookup.value = true;
  hasSearched.value = true;
  try {
    const res = await axios.get(`${API_BASE}/customer/reservations/lookup`, {
      params: { phone: lookupPhone.value.trim() }
    });
    if (res.data?.success) {
      lookupBookings.value = res.data.data || [];
      localStorage.setItem('customer_lookup_phone', lookupPhone.value.trim());
    }
  } catch (err) {
    console.error('Lỗi tra cứu:', err);
  } finally {
    loadingLookup.value = false;
  }
}

async function cancelBooking(b) {
  if (!confirm(`Xác nhận hủy phiếu đặt bàn #${b.reservationId} lúc ${formatDateTime(b.reservationDate)}?`)) return;

  try {
    const res = await axios.post(`${API_BASE}/customer/reservations/${b.reservationId}/cancel`, {
      phone: lookupPhone.value.trim(),
      reason: 'Khách tự hủy lịch qua cổng trực tuyến'
    });
    if (res.data?.success) {
      alert('Đã hủy lịch đặt bàn thành công.');
      handleLookup();
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Không thể hủy phiếu này.');
  }
}

function formatDateTime(dtStr) {
  if (!dtStr) return '';
  return new Date(dtStr).toLocaleString('vi-VN', {
    hour: '2-digit', minute: '2-digit',
    day: '2-digit', month: '2-digit', year: 'numeric'
  });
}

function getStatusBadgeClass(status) {
  const map = {
    pending: 'bg-warning text-dark',
    confirmed: 'bg-primary text-white',
    checked_in: 'bg-info text-dark',
    completed: 'bg-success text-white',
    cancelled: 'bg-danger text-white',
    rejected: 'bg-secondary text-white'
  };
  return map[status] || 'bg-secondary text-white';
}

function getStatusText(status) {
  const map = {
    pending: 'Chờ Quản Lý Xác Nhận',
    confirmed: 'Đã Xác Nhận',
    checked_in: 'Đã Đón Khách',
    completed: 'Đã Hoàn Thành',
    cancelled: 'Đã Hủy Lịch',
    rejected: 'Nhà Hàng Từ Chối'
  };
  return map[status] || status;
}

onMounted(async () => {
  if (!isAuthenticated) return;

  try {
    const { data } = await axios.get(`${API_BASE}/customer/account`);
    const profile = data?.data?.account;
    if (!profile) return;

    bookingForm.value.customerName = profile.fullName || '';
    bookingForm.value.customerPhone = profile.phone || '';
    bookingForm.value.email = profile.email || '';
    lookupPhone.value = profile.phone || lookupPhone.value;
  } catch (error) {
    console.warn('Không thể tải thông tin tài khoản đặt bàn:', error);
  }
});
</script>

<style scoped>
.text-body-dola {
  color: var(--dola-body);
}

.dola-step-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--dola-primary);
  color: #1b1814;
  font-weight: 700;
  font-size: 14px;
}

.dola-counter-btn {
  width: 44px;
  height: 44px;
  border-radius: 8px;
  background: var(--dola-surface-alt);
  color: var(--dola-primary);
  border: 1px solid var(--dola-hairline);
  font-size: 20px;
  font-weight: bold;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s ease;
}
.dola-counter-btn:hover {
  background: var(--dola-primary);
  color: #1b1814;
}

.dola-quick-btn {
  background: var(--dola-surface-alt);
  border: 1px solid var(--dola-hairline);
  color: var(--dola-body);
  border-radius: 9999px;
  padding: 4px 12px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
}
.dola-quick-btn:hover {
  background: rgba(214, 156, 82, 0.2);
  color: #FFFFFF;
}
.dola-quick-btn-active {
  background: var(--dola-primary) !important;
  color: #1b1814 !important;
  font-weight: 700;
}
</style>

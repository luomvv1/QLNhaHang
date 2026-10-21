<template>
  <div class="reviews-admin-container">
    <!-- Header Banner -->
    <div class="card-modern p-4 mb-4 text-white position-relative overflow-hidden"
         style="background: linear-gradient(135deg, #1e1b4b 0%, #3730a3 50%, #4338ca 100%);">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 position-relative" style="z-index: 2;">
        <div>
          <span class="badge bg-warning text-dark px-3 py-1 fw-bold mb-2">GIAI ĐOẠN 7 - PHÂN HỆ M14</span>
          <h2 class="fw-bold mb-1">⭐ Đánh Giá & Phản Hồi Từ Khách Hàng</h2>
          <p class="text-white-50 mb-0">Lắng nghe ý kiến của thực khách, theo dõi mức độ hài lòng và phản hồi giải đáp trải nghiệm ẩm thực.</p>
        </div>
        <button class="btn btn-light fw-bold" @click="fetchReviews">
          🔄 Làm mới dữ liệu
        </button>
      </div>
    </div>

    <!-- Rating Summary 3 Cards -->
    <div class="row g-3 mb-4">
      <div class="col-md-4">
        <div class="card-modern p-4 h-100 text-center d-flex flex-column justify-content-center">
          <div class="text-muted small text-uppercase fw-bold mb-2">Điểm Đánh Giá Trung Bình</div>
          <div class="display-3 fw-bold text-warning mb-1">
            {{ stats.averageRating }}
          </div>
          <div class="fs-4 text-warning mb-2">
            {{ renderStars(Math.round(stats.averageRating)) }}
          </div>
          <div class="text-muted small">
            Dựa trên <strong>{{ stats.totalReviews }}</strong> lượt đánh giá của thực khách
          </div>
        </div>
      </div>

      <div class="col-md-8">
        <div class="card-modern p-4 h-100">
          <h6 class="fw-bold text-dark mb-3">📊 Phân Bổ Mức Độ Hài Lòng</h6>
          <div class="d-flex flex-column gap-2">
            <div v-for="star in [5, 4, 3, 2, 1]" :key="star" class="d-flex align-items-center gap-3">
              <span class="small fw-bold text-nowrap" style="width: 50px;">{{ star }} ⭐</span>
              <div class="progress flex-grow-1" style="height: 10px;">
                <div class="progress-bar" 
                     :class="star >= 4 ? 'bg-success' : star === 3 ? 'bg-warning' : 'bg-danger'"
                     :style="{ width: getRatingPercentage(star) + '%' }">
                </div>
              </div>
              <span class="small text-muted text-end" style="width: 70px;">
                {{ stats.breakdown['star' + star] || 0 }} lượt
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Review Management Table -->
    <div class="card-modern overflow-hidden">
      <!-- Filter Bar -->
      <div class="p-3 border-bottom bg-light d-flex flex-wrap justify-content-between align-items-center gap-2">
        <div class="d-flex flex-wrap gap-2 flex-grow-1" style="max-width: 600px;">
          <input v-model="searchQuery" type="text" class="form-control form-control-sm" placeholder="🔍 Tìm kiếm theo tên khách, SĐT, nội dung..." style="max-width: 280px;" />
          <select v-model="selectedRatingFilter" class="form-select form-select-sm" style="max-width: 150px;">
            <option value="">Tất cả sao</option>
            <option value="5">⭐⭐⭐⭐⭐ (5 sao)</option>
            <option value="4">⭐⭐⭐⭐ (4 sao)</option>
            <option value="3">⭐⭐⭐ (3 sao)</option>
            <option value="2">⭐⭐ (2 sao)</option>
            <option value="1">⭐ (1 sao)</option>
          </select>
          <select v-model="selectedVisibilityFilter" class="form-select form-select-sm" style="max-width: 150px;">
            <option value="">Tất cả trạng thái</option>
            <option value="true">🟢 Đang hiển thị</option>
            <option value="false">⚪ Đang ẩn</option>
          </select>
        </div>
      </div>

      <!-- Table Body -->
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-light text-muted small text-uppercase">
            <tr>
              <th style="width: 60px;">Mã</th>
              <th>Khách Hàng</th>
              <th>Đánh Giá</th>
              <th>Hóa Đơn</th>
              <th>Bình Luận Của Khách</th>
              <th>Phản Hồi Của Nhà Hàng</th>
              <th>Thời Gian</th>
              <th class="text-center" style="width: 120px;">Trạng Thái</th>
              <th class="text-center" style="width: 130px;">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="filteredReviews.length === 0">
              <td colspan="9" class="text-center text-muted py-5">
                Không có đánh giá nào phù hợp với bộ lọc.
              </td>
            </tr>
            <tr v-for="rev in filteredReviews" :key="rev.reviewId">
              <td class="text-muted fw-bold">#{{ rev.reviewId }}</td>
              <td>
                <strong class="text-dark d-block">{{ rev.customerName }}</strong>
                <small class="text-muted">{{ rev.customerPhone || '—' }}</small>
              </td>
              <td>
                <div class="text-warning fw-bold text-nowrap">
                  {{ '★'.repeat(rev.rating) }}{{ '☆'.repeat(5 - rev.rating) }}
                  <span class="small text-dark ms-1">({{ rev.rating }}/5)</span>
                </div>
              </td>
              <td>
                <span v-if="rev.invoiceCode" class="badge bg-light text-dark border">
                  #{{ rev.invoiceCode }}
                </span>
                <span v-else class="text-muted small">—</span>
              </td>
              <td>
                <div class="small text-dark fst-italic" style="max-width: 250px;">
                  "{{ rev.comment || 'Không có nhận xét viết tay' }}"
                </div>
              </td>
              <td>
                <div v-if="rev.reply" class="small text-success border-start border-3 border-success ps-2" style="max-width: 220px;">
                  <strong>Hương Sen:</strong> {{ rev.reply }}
                </div>
                <span v-else class="badge bg-secondary-subtle text-muted">Chưa phản hồi</span>
              </td>
              <td class="small text-muted text-nowrap">
                {{ formatDateTime(rev.createdAt) }}
              </td>
              <td class="text-center">
                <span v-if="rev.isVisible" class="badge bg-success-subtle text-success">🟢 Hiển thị</span>
                <span v-else class="badge bg-secondary">⚪ Đã ẩn</span>
              </td>
              <td class="text-center">
                <div class="btn-group btn-group-sm">
                  <button class="btn btn-outline-primary" title="Phản hồi" @click="openReplyModal(rev)">
                    💬
                  </button>
                  <button class="btn" :class="rev.isVisible ? 'btn-outline-warning' : 'btn-outline-success'" 
                          :title="rev.isVisible ? 'Ẩn đánh giá' : 'Hiển thị đánh giá'"
                          @click="toggleVisibility(rev)">
                    {{ rev.isVisible ? '👁️' : '🚫' }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- MODAL TRẢ LỜI ĐÁNH GIÁ -->
    <div v-if="showReplyModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center">
      <div class="card-modern bg-white p-4 shadow-lg" style="max-width: 500px; width: 90%;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <h5 class="fw-bold mb-0 text-dark">💬 Phản Hồi Đánh Giá Khách Hàng</h5>
          <button class="btn-close" @click="showReplyModal = false"></button>
        </div>

        <div class="p-2 rounded bg-light mb-3">
          <div class="small fw-bold text-dark">{{ activeReview?.customerName }} ({{ activeReview?.rating }} ⭐)</div>
          <p class="small text-muted mb-0 fst-italic">"{{ activeReview?.comment || 'Không có nhận xét' }}"</p>
        </div>

        <form @submit.prevent="submitReply">
          <div class="mb-3">
            <label class="form-label small fw-bold">Nội dung phản hồi từ Nhà Hàng Hương Sen</label>
            <textarea v-model="replyText" rows="4" class="form-control" placeholder="Cảm ơn quý khách đã ghé thăm và gửi gắm đánh giá quý báu..." required></textarea>
          </div>

          <div class="d-flex justify-content-end gap-2 border-top pt-3">
            <button type="button" class="btn btn-sm btn-light border" @click="showReplyModal = false">Hủy</button>
            <button type="submit" class="btn btn-sm btn-primary fw-bold px-3">Gửi Phản Hồi</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import axios from 'axios';

const reviews = ref([]);
const searchQuery = ref('');
const selectedRatingFilter = ref('');
const selectedVisibilityFilter = ref('');

const stats = ref({
  totalReviews: 0,
  averageRating: 5.0,
  breakdown: {}
});

const showReplyModal = ref(false);
const activeReview = ref(null);
const replyText = ref('');

const filteredReviews = computed(() => {
  return reviews.value.filter(r => {
    const matchSearch = !searchQuery.value ||
      r.customerName.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      (r.customerPhone && r.customerPhone.includes(searchQuery.value)) ||
      (r.comment && r.comment.toLowerCase().includes(searchQuery.value.toLowerCase()));

    const matchRating = !selectedRatingFilter.value || r.rating === parseInt(selectedRatingFilter.value);
    const matchVisibility = selectedVisibilityFilter.value === '' || r.isVisible === (selectedVisibilityFilter.value === 'true');

    return matchSearch && matchRating && matchVisibility;
  });
});

async function fetchReviews() {
  try {
    const [adminRes, publicRes] = await Promise.all([
      axios.get('http://localhost:5079/api/reviews/admin/list'),
      axios.get('http://localhost:5079/api/reviews/list')
    ]);

    if (adminRes.data?.success) {
      reviews.value = adminRes.data.data;
    }
    if (publicRes.data?.success) {
      stats.value.totalReviews = publicRes.data.data.totalReviews;
      stats.value.averageRating = publicRes.data.data.averageRating;
      stats.value.breakdown = publicRes.data.data.ratingBreakdown;
    }
  } catch (err) {
    console.error('Lỗi khi tải danh sách đánh giá:', err);
  }
}

async function toggleVisibility(rev) {
  try {
    const res = await axios.post(`http://localhost:5079/api/reviews/admin/${rev.reviewId}/toggle-visibility`);
    if (res.data?.success) {
      rev.isVisible = res.data.isVisible;
    }
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  }
}

function openReplyModal(rev) {
  activeReview.value = rev;
  replyText.value = rev.reply || '';
  showReplyModal.value = true;
}

async function submitReply() {
  if (!activeReview.value) return;
  try {
    const res = await axios.post(`http://localhost:5079/api/reviews/admin/${activeReview.value.reviewId}/reply`, {
      reply: replyText.value
    });
    if (res.data?.success) {
      activeReview.value.reply = res.data.reply;
      showReplyModal.value = false;
    }
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  }
}

function getRatingPercentage(star) {
  if (!stats.value.totalReviews) return 0;
  const count = stats.value.breakdown['star' + star] || 0;
  return Math.round((count / stats.value.totalReviews) * 100);
}

function renderStars(rating) {
  const r = Math.max(1, Math.min(5, rating || 5));
  return '★'.repeat(r) + '☆'.repeat(5 - r);
}

function formatDateTime(val) {
  if (!val) return '—';
  return new Date(val).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  });
}

onMounted(() => {
  fetchReviews();
});
</script>

<style scoped>
.reviews-admin-container {
  animation: fadeIn 0.25s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

.card-modern {
  background: #ffffff;
  border-radius: 14px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.05);
  border: 1px solid rgba(0, 0, 0, 0.06);
}

.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  backdrop-filter: blur(3px);
  z-index: 1050;
  animation: fadeIn 0.2s ease-out;
}
</style>

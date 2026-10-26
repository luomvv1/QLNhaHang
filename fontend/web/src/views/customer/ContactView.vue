<template>
  <div class="dola-theme py-5">
    <div class="container py-3">

      <!-- Header -->
      <div class="text-center mb-5">
        <span class="dola-pill-badge mb-2">📞 Kết Nối Với Chúng Tôi</span>
        <h1 class="dola-font-title text-gold display-4 fw-bold">Liên Hệ &amp; Đóng Góp Ý Kiến</h1>
        <p class="text-body-dola mx-auto fs-5" style="max-width: 600px;">
          Đội ngũ chăm sóc khách hàng của Hương Sen luôn sẵn sàng lắng nghe mọi phản hồi và hỗ trợ quý khách chu đáo nhất.
        </p>
      </div>

      <div class="row g-4">

        <!-- Contact Info Sidebar -->
        <div class="col-12 col-lg-5">
          <div class="d-flex flex-column gap-3">

            <div class="dola-card p-4" v-for="info in contactList" :key="info.title">
              <div class="d-flex gap-3 align-items-start">
                <div class="dola-contact-icon">{{ info.icon }}</div>
                <div>
                  <h5 class="fw-bold mb-1 fs-6" style="color: #1F1A17;">{{ info.title }}</h5>
                  <div class="small" style="color: #574A40;" v-html="info.content"></div>
                </div>
              </div>
            </div>

            <!-- Social channels -->
            <div class="dola-card p-4">
              <h5 class="fw-bold mb-3 fs-6" style="color: var(--dola-wine);">🌐 Kênh Truyền Thông Chính Thức</h5>
              <div class="d-flex gap-2 flex-wrap">
                <a href="#" class="social-pill">
                  📘 Facebook Fanpage
                </a>
                <a href="#" class="social-pill">
                  📸 Instagram Food
                </a>
                <a href="#" class="social-pill">
                  🎵 Kênh TikTok
                </a>
              </div>
            </div>

          </div>
        </div>

        <!-- Contact Form -->
        <div class="col-12 col-lg-7">
          <div class="dola-card p-4 p-md-5 h-100">

            <div v-if="submitted" class="text-center py-5">
              <div class="fs-1 mb-3">💌</div>
              <h3 class="dola-font-title fs-1 mb-2" style="color: var(--dola-wine);">Cảm Ơn Quý Khách!</h3>
              <p class="mb-4" style="color: #574A40;">
                Lời nhắn của bạn đã được gửi đến ban quản lý nhà hàng Hương Sen. Chúng tôi sẽ phản hồi qua điện thoại hoặc email trong thời gian sớm nhất.
              </p>
              <button class="dola-btn-outline" @click="submitted = false">
                Gửi Lời Nhắn Khác
              </button>
            </div>

            <form v-else @submit.prevent="handleSubmit">
              <h3 class="dola-font-title fs-2 mb-2" style="color: var(--dola-wine);">Gửi Tin Nhắn Trực Tiếp</h3>
              <p class="small mb-4" style="color: #6E6157;">
                Quý khách có thể yêu cầu đặt tiệc riêng, đóng góp ý kiến về chất lượng phục vụ hoặc hỏi đáp thực đơn.
              </p>

              <div class="row g-3 mb-4">
                <div class="col-md-6">
                  <label class="form-label text-body-dola small fw-semibold">Họ và tên của bạn *</label>
                  <input type="text" class="dola-input" placeholder="Nguyễn Văn An" v-model="form.name" required />
                </div>
                <div class="col-md-6">
                  <label class="form-label text-body-dola small fw-semibold">Số điện thoại *</label>
                  <input type="tel" class="dola-input" placeholder="0912 345 678" v-model="form.phone" required />
                </div>
                <div class="col-12">
                  <label class="form-label text-body-dola small fw-semibold">Email liên hệ (nếu có)</label>
                  <input type="email" class="dola-input" placeholder="email@gmail.com" v-model="form.email" />
                </div>
                <div class="col-12">
                  <label class="form-label text-body-dola small fw-semibold">Chủ đề cần hỗ trợ *</label>
                  <select class="dola-input" v-model="form.subject" required>
                    <option value="party">🎉 Tư vấn đặt tiệc sinh nhật / công ty</option>
                    <option value="vip">👑 Yêu cầu phòng VIP yến tiệc riêng tư</option>
                    <option value="feedback">💬 Đóng góp ý kiến về món ăn / dịch vụ</option>
                    <option value="other">❓ Các câu hỏi khác</option>
                  </select>
                </div>
                <div class="col-12">
                  <label class="form-label text-body-dola small fw-semibold">Nội dung chi tiết *</label>
                  <textarea class="dola-input" rows="4" placeholder="Nhập nội dung tin nhắn của quý khách tại đây..." v-model="form.message" required></textarea>
                </div>
              </div>

              <button type="submit" class="dola-btn-primary w-100 py-3 fs-6">
                ✉️ Gửi Tin Nhắn Ngay
              </button>
            </form>

          </div>
        </div>

      </div>

    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';

const submitted = ref(false);

const form = ref({
  name: '',
  phone: '',
  email: '',
  subject: 'party',
  message: ''
});

const contactList = [
  {
    icon: '📍',
    title: 'Địa Chỉ Nhà Hàng',
    content: '123 Đường Ẩm Thực, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh<br><span style="color: #8C7F75;">(Có bãi đỗ xe ô tô và xe máy rộng rãi)</span>'
  },
  {
    icon: '📞',
    title: 'Hotline & Tổng Đài CSKH',
    content: '<strong style="color: var(--dola-wine);">1900 6868</strong> – Hỗ trợ đặt bàn 24/7<br>Di động / Zalo quản lý: <strong style="color: #1F1A17;">0933 11 22 33</strong>'
  },
  {
    icon: '✉️',
    title: 'Hộp Thư Điện Tử',
    content: 'info@huongsen-restaurant.vn<br>booking@huongsen-restaurant.vn'
  },
  {
    icon: '🕒',
    title: 'Khung Giờ Đón Khách',
    content: 'Ca trưa: <strong style="color: #1F1A17;">10:00 – 14:00</strong><br>Ca tối: <strong style="color: #1F1A17;">17:00 – 22:30</strong> (Tất cả các ngày trong tuần)'
  }
];

function handleSubmit() {
  submitted.value = true;
  form.value = {
    name: '',
    phone: '',
    email: '',
    subject: 'party',
    message: ''
  };
}
</script>

<style scoped>
.text-body-dola {
  color: var(--dola-body);
}

.dola-contact-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: var(--dola-surface-alt);
  border: 1px solid var(--dola-hairline);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.4rem;
  flex-shrink: 0;
  box-shadow: 0 2px 6px rgba(44, 30, 20, 0.04);
}

.social-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 16px;
  border-radius: 9999px;
  font-size: 13px;
  font-weight: 600;
  text-decoration: none;
  background: #FFFFFF;
  color: #574A40;
  border: 1px solid var(--dola-hairline);
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 2px 5px rgba(0,0,0,0.03);
}

.social-pill:hover {
  background: var(--dola-wine);
  color: #FFFFFF;
  border-color: var(--dola-wine);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(122, 28, 40, 0.25);
}
</style>

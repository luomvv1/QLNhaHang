<template>
  <div class="dola-theme py-5">
    <div class="container py-3">

      <!-- Header -->
      <div class="text-center mb-5">
        <span class="dola-pill-badge mb-2">🪑 Không Gian Kiến Trúc</span>
        <h1 class="dola-font-title text-gold display-4 fw-bold">Các Phân Khu Nhà Hàng</h1>
        <p class="text-body-dola mx-auto fs-5" style="max-width: 620px;">
          Hương Sen được thiết kế với nhiều không gian đa dạng, dung hòa giữa nét mộc mạc và sự sang trọng hiện đại.
        </p>
      </div>

      <!-- Areas Grid -->
      <div class="row g-4">
        <div class="col-12 col-lg-6" v-for="area in areas" :key="area.code">
          <div class="dola-card overflow-hidden h-100 d-flex flex-column">

            <!-- Area Visual Box with Real Photography -->
            <div class="position-relative overflow-hidden area-banner-box" style="aspect-ratio: 16 / 10; min-height: 220px;">
              <img :src="area.image" :alt="area.name" class="w-100 h-100 object-fit-cover area-img" loading="lazy" />
              <div class="area-banner-overlay"></div>
              <span class="badge bg-warning text-dark position-absolute top-0 end-0 m-3 px-3 py-2 fw-bold rounded-pill shadow-sm">
                {{ area.badge }}
              </span>
              <span class="badge bg-success position-absolute top-0 start-0 m-3 px-3 py-2 fw-semibold rounded-pill shadow-sm">
                ● Đang mở cửa
              </span>
            </div>

            <!-- Content -->
            <div class="p-4 d-flex flex-column flex-grow-1">
              <div class="d-flex justify-content-between align-items-start mb-2">
                <h3 class="dola-font-title mb-0 fs-2" style="color: var(--dola-wine);">{{ area.name }}</h3>
                <span class="small" style="color: #6E6157;">Sức chứa: <strong style="color: #1F1A17;">{{ area.capacity }}</strong></span>
              </div>
              <p class="small mb-4" style="color: #574A40; line-height: 1.6;">{{ area.desc }}</p>

              <!-- Highlights table -->
              <div class="row g-2 mb-4">
                <div class="col-6" v-for="detail in area.details" :key="detail.label">
                  <div class="p-2 rounded-2" style="background: var(--dola-surface-alt); border: 1px solid var(--dola-hairline);">
                    <div class="small" style="font-size: 0.72rem; color: #8C7F75;">{{ detail.label }}</div>
                    <div class="fw-bold small" style="color: #1F1A17;">{{ detail.value }}</div>
                  </div>
                </div>
              </div>

              <!-- Tags & CTA -->
              <div class="d-flex flex-wrap gap-2 mb-4">
                <span class="badge" style="background: rgba(184, 125, 50, 0.12); color: var(--dola-primary); border: 1px solid var(--dola-hairline);"
                  v-for="tag in area.tags" :key="tag">
                  ✓ {{ tag }}
                </span>
              </div>

              <div class="mt-auto pt-3 border-top d-flex justify-content-between align-items-center" style="border-color: var(--dola-hairline) !important;">
                <div class="small" style="color: #574A40;">Phù hợp: <strong style="color: #1F1A17;">{{ area.suitability }}</strong></div>
                <router-link to="/customer/reservation"
                  class="dola-btn-primary" style="height: 38px; font-size: 13px;">
                  Đặt Bàn
                </router-link>
              </div>

            </div>
          </div>
        </div>
      </div>

      <!-- Bottom Banner -->
      <div class="dola-card p-4 p-md-5 mt-5 text-center">
        <h3 class="dola-font-title text-gold fs-1 mb-2">Bạn Cần Tổ Chức Sự Kiện Riêng Hoặc Hội Nghị?</h3>
        <p class="text-body-dola mx-auto mb-4" style="max-width: 600px;">
          Chúng tôi nhận trang trí tiệc theo yêu cầu, lên thực đơn set menu riêng và hỗ trợ âm thanh ánh sáng chuyên nghiệp.
        </p>
        <div class="d-flex justify-content-center gap-3">
          <router-link to="/customer/contact" class="dola-btn-primary">
            📞 Liên Hệ Đặt Tiệc: 1900 6868
          </router-link>
          <router-link to="/customer/reservation" class="dola-btn-outline">
            Xem Lịch Đặt Trực Tuyến
          </router-link>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
const areas = [
  {
    code: 'tang1',
    name: 'Đại Sảnh Tầng 1',
    icon: '🏛️',
    badge: 'Khu Vực Chính',
    capacity: '80 khách',
    image: 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800&auto=format&fit=crop&q=85',
    desc: 'Không gian mở trang hoàng đèn lồng tre ấm cúng, view nhìn thẳng quầy bar và bếp mở, rất phù hợp cho các buổi gặp mặt ấm cúng.',
    suitability: 'Gặp gỡ, bữa trưa văn phòng, gia đình nhỏ',
    details: [
      { label: 'Loại bàn', value: 'Bàn 2 – 8 khách' },
      { label: 'Không khí', value: 'Sôi nổi, ấm cúng' },
      { label: 'Tiện ích', value: 'Wifi, Điều hòa, Nhạc nhẹ' },
      { label: 'Vị trí', value: 'Tầng trệt lối vào' },
    ],
    tags: ['Bếp mở', 'Quầy bar', 'Thân thiện', 'Lối đi rộng']
  },
  {
    code: 'garden',
    name: 'Sân Vườn Sinh Thái',
    icon: '🌿',
    badge: 'Không Gian Xanh',
    capacity: '50 khách',
    image: 'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800&auto=format&fit=crop&q=85',
    desc: 'Khu vườn phủ đầy cây xanh râm mát, tiếng nước chảy róc rách bên hồ cá Koi tạo cảm giác thư giãn tuyệt đối cho thực khách.',
    suitability: 'Hẹn hò, tiệc tối lãng mạn, họp mặt bạn bè',
    details: [
      { label: 'Loại bàn', value: 'Bàn tròn 4 – 10 khách' },
      { label: 'Không khí', value: 'Gió trời, xanh mát' },
      { label: 'Tiện ích', value: 'Hồ cá Koi, Cây cổ thụ' },
      { label: 'Vị trí', value: 'Khuôn viên ngoài trời' },
    ],
    tags: ['Thiên nhiên', 'Hồ cá Koi', 'Gió mát', 'Chụp ảnh đẹp']
  },
  {
    code: 'tang2',
    name: 'Lầu Vọng Cảnh Tầng 2',
    icon: '🏮',
    badge: 'Tầm Nhìn Đẹp',
    capacity: '60 khách',
    image: 'https://images.unsplash.com/photo-1514933651103-005eec06c04b?w=800&auto=format&fit=crop&q=85',
    desc: 'Nằm trên tầng cao với ô cửa kính lớn nhìn xuống phố thị về đêm, không gian yên tĩnh và riêng tư hơn dành cho các bữa tối tao nhã.',
    suitability: 'Tiệc họp mặt gia đình, kỷ niệm, liên hoan',
    details: [
      { label: 'Loại bàn', value: 'Bàn dài ghép linh hoạt' },
      { label: 'Không khí', value: 'Yên tĩnh, trang nhã' },
      { label: 'Tiện ích', value: 'Cửa sổ panorama, Sofa' },
      { label: 'Vị trí', value: 'Tầng 2 cầu thang chính' },
    ],
    tags: ['View phố', 'Yên tĩnh', 'Ghép bàn dài', 'Ánh sáng êm']
  },
  {
    code: 'vip',
    name: 'Phòng Tiệc VIP Đẳng Cấp',
    icon: '👑',
    badge: 'Phòng Riêng Biệt',
    capacity: '12 – 30 khách',
    image: 'https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?w=800&auto=format&fit=crop&q=85',
    desc: 'Hệ thống phòng tiệc khép kín cách âm hoàn hảo, nội thất gỗ óc chó cao cấp cùng nhân viên phục vụ riêng biệt theo chuẩn yến tiệc.',
    suitability: 'Tiếp khách VIP, ký kết hợp đồng, sinh nhật',
    details: [
      { label: 'Loại bàn', value: 'Bàn tiệc tròn lớn xoay' },
      { label: 'Không khí', value: 'Biệt lập, sang trọng' },
      { label: 'Tiện ích', value: 'TV 75", Karaoke, Phục vụ riêng' },
      { label: 'Vị trí', value: 'Khu VIP tầng 2' },
    ],
    tags: ['Cách âm', 'Karaoke', 'Bàn tròn xoay', 'Phục vụ riêng']
  }
];
</script>

<style scoped>
.text-body-dola {
  color: var(--dola-body);
}

.area-banner-box {
  background: var(--dola-surface-alt);
}

.area-img {
  transition: transform 0.6s cubic-bezier(0.16, 1, 0.3, 1);
}

.dola-card:hover .area-img {
  transform: scale(1.06);
}

.area-banner-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(0, 0, 0, 0.1) 0%, rgba(31, 26, 23, 0.45) 100%);
  pointer-events: none;
}
</style>

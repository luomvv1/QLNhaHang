<template>
  <div class="dola-theme">

    <!-- ===== HERO SECTION (Dola Deep Forest Canvas & Candlelit Gold) ===== -->
    <section class="dola-hero-section position-relative d-flex align-items-center">
      <div class="dola-hero-bg-overlay"></div>
      <div class="container position-relative z-2 py-5">
        <div class="row align-items-center min-vh-75 py-5">
          <div class="col-12 col-lg-7">
            <!-- Pill Badge Indicator -->
            <div class="d-inline-flex mb-3">
              <span class="dola-pill-badge">
                🌿 Tinh Hoa Ẩm Thực Việt Nam
              </span>
            </div>

            <!-- Display Headline in Dancing Script -->
            <h1 class="dola-script-title dola-hero-title mb-3">
              Hương Vị Truyền Thống,<br>
              <span class="text-gold">Không Gian Tinh Tế</span>
            </h1>

            <p class="dola-hero-desc mb-4">
              Nhà hàng Hương Sen mang đến trải nghiệm ẩm thực đỉnh cao với nguồn nguyên liệu tuyển chọn khắt khe mỗi sáng, 
              không gian ấm cúng kết hợp kiến trúc sân vườn và đội ngũ đầu bếp hơn 15 năm gắn bó với nghề.
            </p>

            <!-- Buttons: Primary 45px, 8px radius + Outline -->
            <div class="d-flex flex-wrap gap-3">
              <router-link to="/customer/reservation" class="dola-btn-primary">
                📅 Đặt Bàn Ngay
              </router-link>
              <router-link to="/customer/menu" class="dola-btn-outline">
                📖 Khám Phá Thực Đơn
              </router-link>
            </div>

            <!-- Stats Bar -->
            <div class="d-flex flex-wrap gap-4 mt-5 pt-3 border-top border-secondary border-opacity-25">
              <div class="stat-box">
                <div class="dola-script-title stat-number text-gold">15+</div>
                <div class="stat-label">Năm Phục Vụ</div>
              </div>
              <div class="vr border-secondary opacity-25 d-none d-md-block"></div>
              <div class="stat-box">
                <div class="dola-script-title stat-number text-gold">60+</div>
                <div class="stat-label">Món Đặc Sản</div>
              </div>
              <div class="vr border-secondary opacity-25 d-none d-md-block"></div>
              <div class="stat-box">
                <div class="dola-script-title stat-number text-gold">15.000+</div>
                <div class="stat-label">Lượt Khách Hài Lòng</div>
              </div>
              <div class="vr border-secondary opacity-25 d-none d-md-block"></div>
              <div class="stat-box">
                <div class="dola-script-title stat-number text-gold">4.9 ★</div>
                <div class="stat-label">Đánh Giá Trung Bình</div>
              </div>
            </div>
          </div>

          <!-- Hero Graphic Column -->
          <div class="col-12 col-lg-5 mt-5 mt-lg-0 text-center">
            <div class="hero-card-featured mx-auto p-4 p-md-5">
              <div class="dish-badge-top mb-3">⭐ SIGNATURE DISH</div>
              <div class="hero-dish-emoji mb-3">🍲</div>
              <h3 class="dola-script-title text-gold fs-2 mb-2">Lẩu Thảo Mộc Hương Sen</h3>
              <p class="small text-body-dola mb-3">
                Nước dùng hầm 12 tiếng cùng sâm ngọc linh, táo đỏ và hải sản tươi rói miền duyên hải.
              </p>
              <div class="d-flex justify-content-between align-items-center pt-3 border-top border-secondary border-opacity-25">
                <span class="fs-5 fw-bold text-white">380.000 ₫</span>
                <router-link to="/customer/reservation" class="dola-btn-primary" style="height: 38px; font-size: 14px; padding: 0 16px;">
                  Thưởng thức
                </router-link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 1: BOTANICAL FEATURES (Surface Alt: #10302C) ===== -->
    <section class="py-5 dola-surface-band">
      <div class="container">
        <div class="row g-4 text-center">
          <div class="col-6 col-lg-3" v-for="f in features" :key="f.title">
            <div class="dola-card p-4 h-100 text-center">
              <div class="feature-icon-circle mb-3">{{ f.icon }}</div>
              <h5 class="fw-bold text-white mb-2 fs-6">{{ f.title }}</h5>
              <p class="small text-body-dola mb-0">{{ f.desc }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 2: CÂU CHUYỆN THƯƠNG HIỆU (Canvas: #143B36) ===== -->
    <section class="py-5 my-3">
      <div class="container">
        <div class="row align-items-center g-5">
          <div class="col-12 col-lg-6">
            <div class="story-frame position-relative p-3">
              <div class="story-box-inner p-4 p-md-5 text-center">
                <span class="fs-1 d-block mb-2">🌿</span>
                <h3 class="dola-script-title fs-1 text-gold mb-2">Nghệ Thuật Ẩm Thực</h3>
                <p class="small text-body-dola mb-4">
                  "Chúng tôi không chỉ phục vụ món ăn, chúng tôi kể câu chuyện về văn hóa và lòng hiếu khách của người Việt qua từng phong vị."
                </p>
                <div class="dola-pill-badge mx-auto">
                  🏮 Không Gian Yên Ả Giữa Lòng Đô Thị
                </div>
              </div>
            </div>
          </div>

          <div class="col-12 col-lg-6">
            <div class="section-tag mb-2">🌱 VỀ CHÚNG TÔI</div>
            <h2 class="dola-script-title section-heading mb-3">
              Hương Sen – Nơi Ký Ức <br><span class="text-gold">Ẩm Thực Bừng Sáng</span>
            </h2>
            <p class="text-body-dola mb-3">
              Ra đời từ khát khao gìn giữ những công thức gia truyền của ẩm thực Việt, 
              Hương Sen kết hợp kỹ thuật chế biến hiện đại cùng triết lý cân bằng âm dương ngũ hành.
            </p>
            <p class="text-body-dola mb-4">
              Từ bữa tiệc họp mặt gia đình ấm cúng đến sự kiện doanh nghiệp trang trọng, 
              chúng tôi luôn đảm bảo không gian riêng tư, phong cách phục vụ chu đáo và chất lượng món ăn hoàn hảo.
            </p>
            <div class="row g-3 mb-4">
              <div class="col-6" v-for="val in values" :key="val.title">
                <div class="d-flex align-items-start gap-2">
                  <span class="text-gold fs-5">{{ val.icon }}</span>
                  <div>
                    <strong class="text-white small d-block">{{ val.title }}</strong>
                    <small class="text-body-dola" style="font-size: 0.76rem;">{{ val.desc }}</small>
                  </div>
                </div>
              </div>
            </div>
            <router-link to="/customer/area" class="dola-btn-outline">
              🪑 Xem Không Gian &amp; Khu Vực
            </router-link>
          </div>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 3: ĐỘI NGŨ BẾP TRƯỞNG (Surface Alt: #10302C) ===== -->
    <section class="py-5 dola-surface-band">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">👨‍🍳 ĐỘI NGŨ NGHỆ NHÂN</div>
          <h2 class="dola-script-title section-heading">Những Bậc Thầy Bếp Hương Sen</h2>
          <p class="text-body-dola small">Tâm huyết tạo nên từng phong vị tinh tế gửi đến quý thực khách</p>
        </div>

        <div class="row g-4 justify-content-center">
          <div class="col-12 col-md-4" v-for="chef in chefs" :key="chef.name">
            <div class="dola-card p-4 h-100 text-center">
              <div class="chef-avatar-circle mx-auto mb-3">
                <span>{{ chef.avatar }}</span>
              </div>
              <span class="badge bg-gold text-dark fw-bold px-3 py-1 rounded-pill mb-2" style="font-size: 0.72rem;">
                {{ chef.specialty }}
              </span>
              <h4 class="text-white fw-bold mb-1 fs-5">{{ chef.name }}</h4>
              <div class="text-gold small mb-3 fw-semibold">{{ chef.title }}</div>
              <p class="text-body-dola small mb-0">{{ chef.desc }}</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 4: THỰC ĐƠN TIÊU BIỂU (Canvas: #143B36) ===== -->
    <section class="py-5 my-3">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">🍜 TINH HOA THỰC ĐƠN</div>
          <h2 class="dola-script-title section-heading">Món Ăn Được Yêu Thích Nhất</h2>
          <p class="text-body-dola small">Những món ăn mang đậm dấu ấn phong vị Hương Sen</p>
        </div>

        <div class="row g-4">
          <div class="col-12 col-sm-6 col-lg-3" v-for="dish in featuredDishes" :key="dish.name">
            <div class="dola-card p-4 h-100 d-flex flex-column justify-content-between">
              <div>
                <div class="dish-item-icon mb-3 text-center">{{ dish.emoji }}</div>
                <h5 class="text-white fw-bold mb-1 fs-6">{{ dish.name }}</h5>
                <p class="text-body-dola small mb-3" style="font-size: 0.8rem;">{{ dish.desc }}</p>
              </div>
              <div class="d-flex justify-content-between align-items-center pt-3 border-top border-secondary border-opacity-25">
                <span class="text-gold fw-bold">{{ dish.price }}</span>
                <span class="badge rounded-pill text-gold px-2 py-1" style="background: rgba(214, 156, 82, 0.15); border: 1px solid rgba(214, 156, 82, 0.3); font-size: 0.75rem;">
                  ⭐ Món Đặc Sản
                </span>
              </div>
            </div>
          </div>
        </div>

        <div class="text-center mt-5">
          <router-link to="/customer/menu" class="dola-btn-primary">
            📖 Xem Toàn Bộ Thực Đơn →
          </router-link>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 5: QUY TRÌNH ĐẶT BÀN 3 BƯỚC (Surface Alt: #10302C) ===== -->
    <section class="py-5 dola-surface-band">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">📋 TIỆN ÍCH TRỰC TUYẾN</div>
          <h2 class="dola-script-title section-heading">Đặt Bàn Dễ Dàng Chỉ Với 3 Bước</h2>
        </div>

        <div class="row g-4 justify-content-center">
          <div class="col-12 col-md-4" v-for="(step, idx) in steps" :key="idx">
            <div class="dola-card p-4 text-center h-100 position-relative">
              <div class="step-num-pill mb-3">BƯỚC {{ idx + 1 }}</div>
              <div class="fs-1 mb-2">{{ step.icon }}</div>
              <h5 class="text-white fw-bold mb-2 fs-6">{{ step.title }}</h5>
              <p class="text-body-dola small mb-0">{{ step.desc }}</p>
            </div>
          </div>
        </div>

        <div class="text-center mt-5">
          <router-link to="/customer/reservation" class="dola-btn-primary" style="height: 48px; font-size: 16px;">
            ✨ Bắt Đầu Đặt Bàn Ngay Bây Giờ
          </router-link>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 6: NHẬN XÉT CỦA THỰC KHÁCH (Canvas: #143B36) ===== -->
    <section class="py-5 my-3">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">💬 TRẢI NGHIỆM THỰC KHÁCH</div>
          <h2 class="dola-script-title section-heading">Cảm Nhận Từ Khách Hàng</h2>
        </div>

        <div class="row g-4">
          <div class="col-12 col-md-4" v-for="r in reviews" :key="r.name">
            <div class="dola-card p-4 h-100">
              <div class="text-gold mb-2">★★★★★</div>
              <p class="text-body-dola small fst-italic mb-3">"{{ r.text }}"</p>
              <div class="d-flex align-items-center gap-2 pt-2 border-top border-secondary border-opacity-25">
                <div class="reviewer-badge">{{ r.avatar }}</div>
                <div>
                  <div class="text-white small fw-bold">{{ r.name }}</div>
                  <div class="text-body-dola" style="font-size: 0.72rem;">{{ r.date }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ===== SECTION 7: CTA CUỐI TRANG (Warm Candlelit Golden Tone) ===== -->
    <section class="py-5 text-center dola-bottom-cta">
      <div class="container py-4">
        <h2 class="dola-script-title text-white mb-2 fs-1">Sẵn Sàng Trải Nghiệm Phong Vị Đẳng Cấp?</h2>
        <p class="text-body-dola mb-4 mx-auto" style="max-width: 580px;">
          Đặt bàn ngay hôm nay để nhận không gian đẹp nhất và ưu đãi đặc quyền cho hội viên mới.
        </p>
        <div class="d-flex justify-content-center gap-3 flex-wrap">
          <router-link to="/customer/reservation" class="dola-btn-primary">
            📅 Đặt Bàn Giữ Chỗ
          </router-link>
          <router-link to="/customer/contact" class="dola-btn-outline">
            📞 Liên Hệ Tư Vấn
          </router-link>
        </div>
      </div>
    </section>

  </div>
</template>

<script setup>
const features = [
  { icon: '🥬', title: 'Nguyên Liệu Tươi Sạch', desc: 'Tuyển chọn từ nông trại hữu cơ chuẩn VietGAP mỗi sớm mai.' },
  { icon: '👨‍🍳', title: 'Nghệ Nhân Ẩm Thực', desc: 'Đội ngũ đầu bếp tâm huyết hơn 15 năm kinh nghiệm trong nghề.' },
  { icon: '🌿', title: 'Không Gian Sân Vườn', desc: 'Kiến trúc mộc mạc, gần gũi thiên nhiên, yên tĩnh và thanh lịch.' },
  { icon: '💎', title: 'Dịch Vụ Chu Đáo', desc: 'Phong cách phục vụ ân cần, tận tâm như đón tiếp người thân.' },
];

const values = [
  { icon: '🌿', title: 'Rau củ hữu cơ', desc: 'Nhập trực tiếp từ vườn Đà Lạt' },
  { icon: '🍲', title: 'Nước dùng ninh xương', desc: 'Hầm liên tục 12h chuẩn vị thanh ngọt' },
  { icon: '🏮', title: 'Phòng VIP riêng tư', desc: 'Cách âm hoàn hảo cho tiệc tiếp khách' },
  { icon: '✨', title: 'Tích lũy điểm thưởng', desc: 'Trừ trực tiếp tiền mặt khi thanh toán' },
];

const chefs = [
  {
    avatar: '👨‍🍳',
    name: 'Nguyễn Văn Phúc',
    title: 'Bếp Trưởng Điều Hành',
    specialty: 'Ẩm Thực Nam Bộ',
    desc: 'Hơn 20 năm kinh nghiệm ẩm thực truyền thống, từng đoạt giải Chiếc Thìa Vàng và gắn bó với Hương Sen từ ngày đầu.'
  },
  {
    avatar: '👩‍🍳',
    name: 'Trần Thị Lan',
    title: 'Bếp Trưởng Cung Đình',
    specialty: 'Món Cung Đình Huế',
    desc: 'Người con xứ Huế kế thừa tinh hoa ẩm thực cung đình, tỉ mỉ trong từng đường dao và cách nêm nếm ngũ vị.'
  },
  {
    avatar: '🧑‍🍳',
    name: 'Lê Minh Tuấn',
    title: 'Bếp Phó & Pha Chế',
    specialty: 'Tráng Miệng & Trà Hoa',
    desc: 'Chuyên gia nghiên cứu các loại trà thảo mộc, chè dưỡng nhan và bánh tráng miệng thanh mát chuẩn phong vị xưa.'
  }
];

const featuredDishes = [
  { emoji: '🦐', name: 'Lẩu Tôm Càng Sen Vàng', desc: 'Tôm càng xanh tươi rói, nước lẩu hoa sen thơm dịu thanh mát.', price: '320.000 ₫' },
  { emoji: '🐟', name: 'Cá Bống Kho Tiêu Phú Quốc', desc: 'Cá bống tươi đun trong tộ đất với nước màu dừa Bến Tre và tiêu sọ thơm nồng.', price: '165.000 ₫' },
  { emoji: '🍖', name: 'Bò Tơ Hấp Cuốn Rau Rừng', desc: 'Thịt bò tơ mềm ngọt cuốn cùng 10 loại rau rừng Tây Ninh chấm mắm nêm đậm đà.', price: '210.000 ₫' },
  { emoji: '🥗', name: 'Gỏi Ngó Sen Tôm Thịt', desc: 'Ngó sen tươi giòn sần sật trộn tôm sú, thịt ba chỉ và nước mắm chua ngọt tuyệt hảo.', price: '125.000 ₫' },
];

const steps = [
  { icon: '📝', title: '1. Nhập Thông Tin', desc: 'Chọn ngày giờ, số lượng khách và khu vực ngồi mong muốn chỉ trong 1 phút.' },
  { icon: '📞', title: '2. Nhà Hàng Xác Nhận', desc: 'Nhân viên gọi lại xác nhận và giữ bàn chu đáo theo đúng yêu cầu quý khách.' },
  { icon: '🎉', title: '3. Đến & Tận Hưởng', desc: 'Đến đúng giờ, check-in nhận bàn và thưởng thức bữa tiệc ẩm thực trọn vẹn.' },
];

const reviews = [
  {
    avatar: '👩', name: 'Chị Hoàng Yến', date: 'Thực khách quen',
    text: 'Không gian trầm ấm, yên tĩnh đúng chất Dola phong cách sang trọng. Món ăn rất tròn vị, người lớn tuổi trong nhà đều khen.'
  },
  {
    avatar: '👨', name: 'Anh Minh Trí', date: 'Tiệc kỷ niệm ngày cưới',
    text: 'Đặt bàn trước được chuẩn bị bàn nến hoa rất lãng mạn. Nhân viên chu đáo, món lẩu hoa sen thơm ngát khó quên!'
  },
  {
    avatar: '👩', name: 'Cô Thanh Mai', date: 'Hội thảo gia đình',
    text: 'Phòng VIP cách âm tốt, đồ ăn lên nóng hổi và thanh đạm. Hệ thống tích điểm và tra cứu lịch rất tiện lợi.'
  }
];
</script>

<style scoped>
/* Hero */
.dola-hero-section {
  min-height: 80vh;
  background-color: var(--dola-canvas);
  overflow: hidden;
}

.dola-hero-bg-overlay {
  position: absolute;
  inset: 0;
  background: radial-gradient(ellipse at 80% 30%, rgba(214, 156, 82, 0.12) 0%, transparent 65%);
  pointer-events: none;
}

.dola-hero-title {
  font-size: clamp(2.4rem, 5.5vw, 3.6rem);
  font-weight: 600;
  line-height: 1.25;
}

.dola-hero-desc {
  font-size: 1.05rem;
  font-weight: 300;
  line-height: 1.7;
  color: var(--dola-body);
  max-width: 580px;
}

.text-gold { color: var(--dola-primary) !important; }
.bg-gold { background-color: var(--dola-primary) !important; }
.text-body-dola { color: var(--dola-body) !important; }

.stat-number {
  font-size: 2.2rem;
  font-weight: 600;
  line-height: 1;
}

.stat-label {
  font-size: 0.78rem;
  font-weight: 500;
  color: var(--dola-body);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.hero-card-featured {
  background-color: var(--dola-surface-alt);
  border: 2px solid var(--dola-primary);
  border-radius: 8px;
  max-width: 360px;
  box-shadow: 0 16px 36px rgba(16, 48, 44, 0.6);
}

.dish-badge-top {
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  color: var(--dola-primary);
}

.hero-dish-emoji {
  font-size: 4.5rem;
}

/* Sections & Color Blocking */
.dola-surface-band {
  background-color: var(--dola-surface-alt);
}

.section-tag {
  font-family: var(--font-body);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.15em;
  color: var(--dola-primary);
  text-transform: uppercase;
}

.section-heading {
  font-size: clamp(2rem, 4vw, 3rem);
  font-weight: 600;
  color: var(--dola-ink);
}

/* Feature Cards */
.feature-icon-circle {
  font-size: 2.5rem;
}

/* Story Frame */
.story-frame {
  border: 1px solid var(--dola-primary);
  border-radius: 8px;
}

.story-box-inner {
  background-color: var(--dola-surface-alt);
  border-radius: 6px;
}

/* Chef */
.chef-avatar-circle {
  width: 90px;
  height: 90px;
  background-color: var(--dola-canvas);
  border: 2px solid var(--dola-primary);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 3rem;
}

/* Dishes */
.dish-item-icon {
  font-size: 3.5rem;
}

/* Steps */
.step-num-pill {
  display: inline-block;
  background-color: var(--dola-primary);
  color: var(--dola-on-primary);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  padding: 3px 14px;
  border-radius: 9999px;
}

/* Reviews */
.reviewer-badge {
  width: 36px;
  height: 36px;
  background-color: var(--dola-canvas);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.2rem;
}

/* Bottom CTA */
.dola-bottom-cta {
  background: linear-gradient(180deg, var(--dola-canvas) 0%, var(--dola-surface-alt) 100%);
  border-top: 1px solid var(--dola-hairline);
}
</style>

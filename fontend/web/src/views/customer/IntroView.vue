<template>
  <div class="dola-theme intro-page">

    <!-- ================================================================= -->
    <!-- 1. HERO SECTION (Cinematic Fine Dining Canvas & Golden Glow)      -->
    <!-- ================================================================= -->
    <!-- ================================================================= -->
    <!-- 1. HERO SECTION (Savory Table Inspired: Full-Bleed & Cinematic)   -->
    <!-- ================================================================= -->
    <section class="dola-hero-section position-relative d-flex align-items-center">
      <!-- Full-Bleed Atmospheric Restaurant Photography with Ken Burns effect -->
      <div class="hero-bg-media">
        <img
          src="https://images.unsplash.com/photo-1544025162-d76694265947?w=1920&auto=format&fit=crop&q=85"
          alt="Bàn tiệc mỹ vị nhà hàng Hương Sen"
          class="hero-bg-img"
          fetchpriority="high"
          loading="eager"
        />
        <div class="hero-bg-vignette"></div>
        <div class="hero-ambient-glow"></div>
        <div class="hero-light-specks">
          <span class="speck speck-1"></span>
          <span class="speck speck-2"></span>
          <span class="speck speck-3"></span>
          <span class="speck speck-4"></span>
        </div>
      </div>

      <div class="container position-relative z-2 py-5">
        <div class="row align-items-center min-vh-hero py-4 py-lg-5">
          
          <!-- Hero Text & Content (Aligned to left with high contrast overlay) -->
          <div class="col-12 col-lg-8 col-xl-7">
            <!-- Kicker Tagline with glowing sparkle -->
            <div class="hero-kicker-tag mb-3 animate-fade-in">
              <span class="kicker-sparkle">✨</span>
              <span class="kicker-text">TINH HOA ẨM THỰC VIỆT · KHÔNG GIAN ĐẲNG CẤP</span>
            </div>

            <!-- Display Headline (PRESERVING EXACT FONT: dola-script-title) -->
            <h1 class="dola-script-title dola-hero-title mb-3 animate-fade-in-up">
              Hương Sen<br>
              <span class="hero-gold-gradient">Nhà Hàng &amp; Sân Vườn</span>
            </h1>

            <p class="dola-hero-desc mb-4 animate-fade-in-up delay-1">
              Mỹ vị ba miền truyền thống giao hòa cùng tinh thần ẩm thực đương đại. 
              Trải nghiệm ẩm thực lắng đọng, tròn vẹn ngũ quan giữa không gian sân vườn Indochine thanh nhã và tiếng nước róc rách an yên.
            </p>

            <!-- Dual Pill Buttons (Savory Table High-Conversion Style) -->
            <div class="d-flex flex-wrap gap-3 mb-4 pb-2 animate-fade-in-up delay-2">
              <router-link to="/customer/reservation" class="dola-btn-pill-primary btn-glow">
                <span>📅 Đặt Bàn Ngay</span>
                <span class="pill-btn-arrow">→</span>
              </router-link>
              <router-link to="/customer/menu" class="dola-btn-pill-outline">
                <span>📖 Khám Phá Thực Đơn</span>
              </router-link>
              <router-link to="/customer/area" class="dola-btn-pill-glass d-none d-sm-inline-flex">
                <span>🪑 Xem Không Gian</span>
              </router-link>
            </div>

            <!-- Symmetrical 3-Column Luxury Stats Board (Zero Wrap on Desktop) -->
            <div class="luxury-hero-stats-board animate-fade-in-up delay-3">
              <!-- Col 1: Rating -->
              <div class="stat-unit">
                <div class="stat-unit-icon stars-gold">★★★★★</div>
                <div class="stat-unit-val">
                  <span class="val-num">{{ animatedRating.toFixed(1) }}</span>
                  <span class="val-sub">/5</span>
                </div>
                <div class="stat-unit-label">
                  <strong class="text-amber-gold">{{ animatedReviews.toLocaleString('vi-VN') }}+</strong> đánh giá
                </div>
              </div>

              <div class="stat-unit-divider"></div>

              <!-- Col 2: 100% VietGAP -->
              <div class="stat-unit">
                <div class="stat-unit-icon">🌿</div>
                <div class="stat-unit-val">
                  <span class="val-num">{{ Math.round(animatedOrganic) }}%</span>
                </div>
                <div class="stat-unit-label">
                  Hữu cơ <span class="badge-vietgap">VietGAP</span>
                </div>
              </div>

              <div class="stat-unit-divider"></div>

              <!-- Col 3: Top 10 Culinary -->
              <div class="stat-unit">
                <div class="stat-unit-icon">🏆</div>
                <div class="stat-unit-val">
                  <span class="val-prefix">Top</span>
                  <span class="val-num">{{ Math.round(animatedTop) }}</span>
                </div>
                <div class="stat-unit-label">
                  Ẩm Thực <span class="badge-year">2026</span>
                </div>
              </div>
            </div>

          </div>

        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 2. SAVORY TABLE STYLE 4-PILLAR VALUE STRIP (Right Under Hero)    -->
    <!-- ================================================================= -->
    <section class="savory-value-strip py-4 position-relative">
      <div class="container">
        <div class="row g-4 align-items-center">
          <div class="col-6 col-lg-3" v-for="(f, idx) in features" :key="f.title">
            <div class="savory-pillar-item d-flex align-items-center gap-3 p-3 rounded-3" :style="{ animationDelay: `${idx * 0.1}s` }">
              <div class="pillar-icon-box">
                <span class="pillar-icon">{{ f.icon }}</span>
              </div>
              <div class="pillar-text">
                <h4 class="pillar-title fw-bold mb-1 fs-6" style="color: #1F1A17;">{{ f.title }}</h4>
                <p class="pillar-subtitle text-body-dola mb-0">{{ f.subtitle }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 3. BRAND STORY & HERITAGE (Editorial Split Collage)              -->
    <!-- ================================================================= -->
    <section class="py-5 my-3 position-relative">
      <div class="container">
        <div class="row align-items-center g-5">
          
          <!-- Editorial Photo Collage -->
          <div class="col-12 col-lg-6">
            <div class="story-collage-container position-relative">
              <div class="main-story-photo-wrap">
                <img
                  src="https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800&auto=format&fit=crop&q=85"
                  alt="Không gian nhà hàng Hương Sen"
                  class="story-main-img"
                  loading="lazy"
                />
              </div>
              
              <div class="sub-story-photo-wrap d-none d-sm-block">
                <img
                  src="https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600&auto=format&fit=crop&q=85"
                  alt="Bàn tiệc sang trọng Hương Sen"
                  class="story-sub-img"
                  loading="lazy"
                />
              </div>

              <!-- Overlapping Heritage Stamp -->
              <div class="story-heritage-stamp">
                <div class="stamp-inner">
                  <span class="stamp-icon">🪷</span>
                  <strong class="stamp-year">2011 - 2026</strong>
                  <span class="stamp-text">15 Năm Di Sản</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Story Text Content -->
          <div class="col-12 col-lg-6">
            <div class="section-tag mb-2">🌱 VỀ CHÚNG TÔI</div>
            <h2 class="dola-script-title section-heading mb-3">
              Hương Sen – Nơi Ký Ức <br><span class="text-gold">Ẩm Thực Bừng Sáng</span>
            </h2>
            <p class="text-body-dola mb-3" style="line-height: 1.7;">
              Khởi nguồn từ niềm đam mê gìn giữ những phong vị truyền thống thuần khiết của ba miền, <strong>Hương Sen</strong> dung hòa giữa triết lý ngũ hành phương Đông và nghệ thuật ẩm thực hiện đại.
            </p>
            <p class="text-body-dola mb-4" style="line-height: 1.7;">
              Dưới bóng cây xanh rợp mát và hương hoa sen thơm ngát, mỗi bữa ăn tại Hương Sen không đơn thuần là ẩm thực, mà là khoảnh khắc kết nối tình thân, gặp gỡ tri giao và lưu giữ những kỷ niệm đẹp đẽ.
            </p>

            <!-- 4 Value Points -->
            <div class="row g-3 mb-4">
              <div class="col-12 col-sm-6" v-for="val in values" :key="val.title">
                <div class="value-item-card p-3 rounded-3">
                  <div class="d-flex align-items-start gap-3">
                    <span class="value-icon">{{ val.icon }}</span>
                    <div>
                      <strong class="small d-block mb-1" style="color: #1F1A17;">{{ val.title }}</strong>
                      <span class="text-body-dola" style="font-size: 0.78rem; line-height: 1.4;">{{ val.desc }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Call to Area Page -->
            <div class="d-flex flex-wrap gap-3">
              <router-link to="/customer/area" class="dola-btn-outline">
                <span>🪑</span> Khám Phá Không Gian Nhà Hàng
              </router-link>
              <router-link to="/customer/reservation" class="dola-btn-primary">
                <span>📅</span> Đặt Bàn Ngay
              </router-link>
            </div>
          </div>

        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 4. SIGNATURE DISHES GALLERY (Real High-Res Food Photography)     -->
    <!-- ================================================================= -->
    <section class="py-5 dola-surface-band">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">🍜 TINH HOA ẨM THỰC</div>
          <h2 class="dola-script-title section-heading">Món Ăn Được Yêu Thích Nhất</h2>
          <p class="text-body-dola small mx-auto" style="max-width: 540px;">
            Được tuyển chọn từ công thức gia truyền và nêm nếm bằng cả tấm lòng của những nghệ nhân bếp Hương Sen
          </p>

          <!-- Category Filter Pills -->
          <div class="d-flex justify-content-center flex-wrap gap-2 mt-4">
            <button
              v-for="cat in dishCategories"
              :key="cat.id"
              type="button"
              class="dish-filter-pill"
              :class="{ active: selectedCategory === cat.id }"
              @click="selectedCategory = cat.id"
            >
              {{ cat.label }}
            </button>
          </div>
        </div>

        <!-- Dishes Grid -->
        <div class="row g-4">
          <div
            class="col-12 col-sm-6 col-lg-4 col-xl-3"
            v-for="dish in filteredDishes"
            :key="dish.name"
          >
            <div class="dish-card-lux h-100 d-flex flex-column">
              <!-- Dish Photo -->
              <div class="dish-photo-wrap position-relative overflow-hidden">
                <img
                  :src="dish.image"
                  :alt="dish.name"
                  class="dish-img"
                  loading="lazy"
                />
                <span class="dish-category-badge">{{ dish.tag }}</span>
                <span class="dish-rating-badge">★ 5.0</span>
              </div>

              <!-- Dish Info -->
              <div class="p-3 d-flex flex-column flex-grow-1">
                <h5 class="fw-bold mb-1 fs-6" style="color: #1F1A17;">{{ dish.name }}</h5>
                <p class="text-body-dola small mb-3 flex-grow-1" style="font-size: 0.8rem; line-height: 1.5;">
                  {{ dish.desc }}
                </p>

                <div class="d-flex justify-content-between align-items-center pt-2 border-top border-secondary border-opacity-25 mt-auto">
                  <span class="dish-price-text">{{ dish.price }}</span>
                  <router-link to="/customer/reservation" class="btn-dish-reserve">
                    Đặt bàn thưởng thức
                  </router-link>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Menu CTA -->
        <div class="text-center mt-5">
          <router-link to="/customer/menu" class="dola-btn-primary btn-glow px-4">
            📖 Khám Phá Toàn Bộ Thực Đơn (80+ Món) →
          </router-link>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 4.5 PHOTO GALLERY (Khoảnh Khắc Mỹ Vị & Không Gian Đa Dạng)        -->
    <!-- ================================================================= -->
    <section class="py-5 photo-gallery-section position-relative">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">📸 THƯ VIỆN HÌNH ẢNH</div>
          <h2 class="dola-script-title section-heading">Khoảnh Khắc Mỹ Vị &amp; Không Gian</h2>
          <p class="text-body-dola small mx-auto" style="max-width: 580px;">
            Mỗi góc nhìn là một tác phẩm nghệ thuật, mỗi món ăn là một tuyệt tác đong đầy tâm huyết từ Bếp Hương Sen
          </p>
        </div>

        <!-- 5-Photo Bento Masonry Grid -->
        <div class="row g-3 g-md-4">
          <div
            v-for="(item, idx) in galleryImages"
            :key="idx"
            :class="item.span"
          >
            <div class="gallery-photo-card position-relative overflow-hidden h-100 rounded-4">
              <img
                :src="item.image"
                :alt="item.title"
                class="gallery-photo-img"
                loading="lazy"
              />
              <div class="gallery-photo-overlay p-4 d-flex flex-column justify-content-end">
                <span class="gallery-badge align-self-start mb-2">{{ item.category }}</span>
                <h4 class="text-white fw-bold fs-5 mb-1">{{ item.title }}</h4>
                <p class="small text-white-50 mb-0">{{ item.desc }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="text-center mt-4 pt-2">
          <router-link to="/customer/area" class="dola-btn-pill-outline px-4">
            📷 Khám Phá Thêm Góc Chụp Check-in Đẹp →
          </router-link>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 5. THREE SIGNATURE DINING SPACES (Ambiance Showcase)             -->
    <!-- ================================================================= -->
    <section class="py-5 my-3">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">🪷 KHÔNG GIAN KIẾN TRÚC</div>
          <h2 class="dola-script-title section-heading">Góc Nhìn Không Gian Đa Dạng</h2>
          <p class="text-body-dola small mx-auto" style="max-width: 540px;">
            Từ góc sân vườn thoáng đãng đến phòng VIP riêng tư, Hương Sen mang đến trải nghiệm phù hợp cho mọi dịp gặp gỡ
          </p>
        </div>

        <div class="row g-4">
          <div class="col-12 col-md-4" v-for="area in diningAreas" :key="area.title">
            <div class="area-showcase-card h-100 overflow-hidden d-flex flex-column">
              <div class="area-media position-relative overflow-hidden">
                <img
                  :src="area.image"
                  :alt="area.title"
                  class="area-img"
                  loading="lazy"
                />
                <span class="area-badge">{{ area.badge }}</span>
              </div>
              <div class="p-4 d-flex flex-column flex-grow-1">
                <div class="d-flex justify-content-between align-items-baseline mb-2">
                  <h4 class="text-gold dola-script-title fs-3 mb-0">{{ area.title }}</h4>
                  <span class="small text-white-50">{{ area.capacity }}</span>
                </div>
                <p class="small text-body-dola mb-3 flex-grow-1" style="line-height: 1.6;">
                  {{ area.desc }}
                </p>
                <div class="d-flex flex-wrap gap-2 pt-2 border-top border-secondary border-opacity-25">
                  <span class="area-chip" v-for="tag in area.tags" :key="tag">
                    ✓ {{ tag }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="text-center mt-4">
          <router-link to="/customer/area" class="dola-btn-outline">
            🪑 Xem Chi Tiết Sơ Đồ &amp; Đặt Bàn Khu Vực →
          </router-link>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 6. MASTER CHEFS SECTION (Portraits of Culinary Masters)          -->
    <!-- ================================================================= -->
    <section class="py-5 dola-surface-band">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">👨‍🍳 ĐỘI NGŨ NGHỆ NHÂN</div>
          <h2 class="dola-script-title section-heading">Những Bậc Thầy Bếp Hương Sen</h2>
          <p class="text-body-dola small mx-auto" style="max-width: 540px;">
            Tâm huyết và đam mê tạo nên từng phong vị tinh tế gửi đến quý thực khách
          </p>
        </div>

        <div class="row g-4 justify-content-center">
          <div class="col-12 col-md-4" v-for="chef in chefs" :key="chef.name">
            <div class="chef-card-lux h-100 p-4 text-center d-flex flex-column align-items-center">
              <div class="chef-portrait-wrap mb-3 position-relative">
                <img
                  :src="chef.image"
                  :alt="chef.name"
                  class="chef-portrait-img"
                  loading="lazy"
                />
                <span class="chef-star-badge">★</span>
              </div>
              <span class="chef-specialty-pill mb-2">
                {{ chef.specialty }}
              </span>
              <h4 class="fw-bold mb-1 fs-5" style="color: #1F1A17;">{{ chef.name }}</h4>
              <div class="text-gold small mb-2 fw-semibold">{{ chef.title }}</div>
              <p class="text-body-dola small mb-0 flex-grow-1" style="line-height: 1.6;">
                {{ chef.desc }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 7. THREE-STEP RESERVATION PROCESS (Smart Flow)                   -->
    <!-- ================================================================= -->
    <section class="py-5 my-3">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">📋 TIỆN ÍCH TRỰC TUYẾN</div>
          <h2 class="dola-script-title section-heading">Đặt Bàn Chu Đáo Chỉ Với 3 Bước</h2>
          <p class="text-body-dola small">Nhanh chóng, chuẩn xác và không cần xếp hàng chờ đợi</p>
        </div>

        <div class="row g-4 justify-content-center">
          <div class="col-12 col-md-4" v-for="(step, idx) in steps" :key="idx">
            <div class="step-card-modern p-4 text-center h-100 position-relative">
              <div class="step-badge-number">BƯỚC {{ idx + 1 }}</div>
              <div class="step-icon-large mb-3">{{ step.icon }}</div>
              <h5 class="fw-bold mb-2 fs-6" style="color: #1F1A17;">{{ step.title }}</h5>
              <p class="text-body-dola small mb-0" style="line-height: 1.6;">{{ step.desc }}</p>
            </div>
          </div>
        </div>

        <div class="text-center mt-5">
          <router-link to="/customer/reservation" class="dola-btn-primary btn-glow px-4 py-2" style="height: 48px; font-size: 16px;">
            ✨ Bắt Đầu Đặt Bàn Ngay Bây Giờ
          </router-link>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 8. GUEST REVIEWS & TESTIMONIALS (Verified Social Proof)           -->
    <!-- ================================================================= -->
    <section class="py-5 dola-surface-band">
      <div class="container py-3">
        <div class="text-center mb-5">
          <div class="section-tag mb-2">💬 TRẢI NGHIỆM THỰC KHÁCH</div>
          <h2 class="dola-script-title section-heading">Cảm Nhận Từ Khách Hàng</h2>
          <p class="text-body-dola small">Niềm hạnh phúc của thực khách là động lực lớn nhất của chúng tôi</p>
        </div>

        <div class="row g-4">
          <div class="col-12 col-md-4" v-for="r in reviews" :key="r.name">
            <div class="review-card-modern p-4 h-100 d-flex flex-column justify-content-between">
              <div>
                <div class="d-flex align-items-center justify-content-between mb-3">
                  <span class="text-gold fs-6">★★★★★</span>
                  <span class="verified-tag">✓ Đã thưởng thức</span>
                </div>
                <p class="text-body-dola small fst-italic mb-4" style="line-height: 1.7;">
                  "{{ r.text }}"
                </p>
              </div>

              <div class="d-flex align-items-center gap-3 pt-3 border-top border-secondary border-opacity-25">
                <img
                  :src="r.avatar"
                  :alt="r.name"
                  class="reviewer-avatar-img"
                  loading="lazy"
                />
                <div>
                  <div class="small fw-bold" style="color: #1F1A17;">{{ r.name }}</div>
                  <div class="text-body-dola" style="font-size: 0.72rem;">{{ r.role }} · {{ r.date }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ================================================================= -->
    <!-- 9. BOTTOM CALL TO ACTION (Warm Candlelit Golden Tone)             -->
    <!-- ================================================================= -->
    <section class="py-5 text-center dola-bottom-cta position-relative overflow-hidden">
      <div class="cta-bg-glow"></div>
      <div class="container py-5 position-relative z-2">
        <span class="dola-pill-badge mb-3">🪷 Chào Đón Quý Khách</span>
        <h2 class="dola-script-title mb-3" style="color: #1F1A17; font-size: clamp(2rem, 4vw, 3.2rem);">
          Sẵn Sàng Trải Nghiệm Phong Vị Đẳng Cấp?
        </h2>
        <p class="text-body-dola mb-4 mx-auto" style="max-width: 620px; line-height: 1.7;">
          Đặt bàn ngay hôm nay để được ưu tiên lựa chọn không gian đẹp nhất và tận hưởng ưu đãi giảm giá đặc quyền cho hội viên mới.
        </p>
        <div class="d-flex justify-content-center gap-3 flex-wrap">
          <router-link to="/customer/reservation" class="dola-btn-primary btn-glow px-4">
            📅 Đặt Bàn Giữ Chỗ Ngay
          </router-link>
          <router-link to="/customer/contact" class="dola-btn-outline px-4">
            📞 Hotline 1900 6868
          </router-link>
        </div>
      </div>
    </section>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';

const animatedRating = ref(0.0);
const animatedReviews = ref(0);
const animatedOrganic = ref(0);
const animatedTop = ref(0);

function runCountUp() {
  const duration = 1800; // ms
  const startTime = performance.now();

  function update(currentTime) {
    const elapsed = currentTime - startTime;
    const progress = Math.min(elapsed / duration, 1);
    // Smooth cubic ease out
    const ease = 1 - Math.pow(1 - progress, 3);

    animatedRating.value = Number((ease * 4.9).toFixed(1));
    animatedReviews.value = Math.floor(ease * 1500);
    animatedOrganic.value = Math.floor(ease * 100);
    animatedTop.value = Math.floor(ease * 10);

    if (progress < 1) {
      requestAnimationFrame(update);
    } else {
      animatedRating.value = 4.9;
      animatedReviews.value = 1500;
      animatedOrganic.value = 100;
      animatedTop.value = 10;
    }
  }

  requestAnimationFrame(update);
}

onMounted(() => {
  setTimeout(() => {
    runCountUp();
  }, 250);
});

const selectedCategory = ref('all');

const dishCategories = [
  { id: 'all', label: 'Tất Cả Món Tinh Hoa' },
  { id: 'specialty', label: 'Món Đặc Sản' },
  { id: 'hotpot', label: 'Lẩu Thảo Mộc' },
  { id: 'bbq', label: 'Món Nướng Đậm Vị' },
  { id: 'appetizer', label: 'Khai Vị & Rau Rừng' },
];

const features = [
  { icon: '🌿', title: 'Nguyên Liệu Hữu Cơ', subtitle: '100% Chuẩn VietGAP & OCOP', desc: 'Tuyển chọn từ nông trại hữu cơ chuẩn VietGAP tại Đà Lạt & Mộc Châu mỗi sớm mai.' },
  { icon: '👨‍🍳', title: 'Bếp Trưởng 15+ Năm', subtitle: 'Công Thức Gia Truyền Độc Bản', desc: 'Đội ngũ nghệ nhân từng đứng bếp tại các khách sạn 5 sao và phục vụ đại tiệc quốc yến.' },
  { icon: '🪷', title: 'Kiến Trúc Indochine', subtitle: 'Sân Vườn & Phòng VIP Biệt Lập', desc: 'Không gian giao hòa giữa nét hoài cổ Đông Dương và vườn sen thanh tịnh giữa phố thị.' },
  { icon: '🏆', title: 'Dịch Vụ Chu Đáo', subtitle: 'Trải Nghiệm Chuẩn Mực 5 Sao', desc: 'Phục vụ ân cần, tận tâm như đón tiếp người thân trong gia đình, chuẩn mực hiếu khách.' },
];

const galleryImages = [
  {
    title: 'Bàn Tiệc Sân Vườn Hoa Sen',
    category: '🌿 Không Gian Ngoài Trời',
    desc: 'Bàn ăn lung linh dưới tán vòm hoa và đèn lồng ấm cúng bên hồ sen',
    image: 'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=900&auto=format&fit=crop&q=85',
    span: 'col-12 col-md-7 col-lg-8',
  },
  {
    title: 'Nghệ Thuật Bếp Đỉnh Cao',
    category: '👨‍🍳 Mỹ Vị Tinh Hoa',
    desc: 'Tỉ mỉ và nêm nếm bằng trọn vẹn đam mê của nghệ nhân bếp',
    image: 'https://images.unsplash.com/photo-1559339352-11d035aa65de?w=800&auto=format&fit=crop&q=85',
    span: 'col-12 col-md-5 col-lg-4',
  },
  {
    title: 'Trà Sen Tây Hồ & Khai Vị Hoàng Gia',
    category: '🪷 Ẩm Thực Thanh Tịnh',
    desc: 'Thưởng trà thanh tao khai mở các giác quan thuần khiết',
    image: 'https://images.unsplash.com/photo-1563245372-f21724e3856d?w=800&auto=format&fit=crop&q=85',
    span: 'col-12 col-md-4 col-lg-4',
  },
  {
    title: 'Phòng Tiệc VIP Hoàng Gia',
    category: '👑 Không Gian Riêng Tư',
    desc: 'Đẳng cấp và trang trọng cho những cuộc tiếp khách quan trọng',
    image: 'https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?w=800&auto=format&fit=crop&q=85',
    span: 'col-12 col-md-4 col-lg-4',
  },
  {
    title: 'Hải Sản & Món Nướng Than Hoa',
    category: '🔥 Nướng Cung Đình',
    desc: 'Hương vị thơm nồng xém cạnh giữ trọn vị ngọt tự nhiên',
    image: 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800&auto=format&fit=crop&q=85',
    span: 'col-12 col-md-4 col-lg-4',
  },
];

const values = [
  { icon: '🌿', title: 'Rau củ hữu cơ chuẩn sạch', desc: 'Nhập trực tiếp từ nông trại Đà Lạt mỗi sớm mai' },
  { icon: '🍲', title: 'Nước dùng hầm 12 tiếng', desc: 'Ninh xương ống nguyên chất không phụ gia hay mì chính' },
  { icon: '🏮', title: 'Phòng VIP riêng tư', desc: 'Cách âm hoàn hảo cho tiệc tiếp khách và gia đình' },
  { icon: '✨', title: 'Tích điểm đổi tiền mặt', desc: 'Tích lũy 10% giá trị hóa đơn, trừ tiền ngay lần tới' },
];

const featuredDishes = [
  {
    category: 'hotpot',
    tag: 'SIGNATURE HOTPOT',
    name: 'Lẩu Nấm Gà Ta Thảo Mộc',
    desc: 'Gà ta thả đồi chắc thịt hầm cùng 6 loại nấm quý và sâm thảo mộc thanh ngọt tự nhiên.',
    price: '349.000 ₫',
    image: 'https://images.unsplash.com/photo-1547592180-85f173990554?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'bbq',
    tag: 'MÓN NƯỚNG CUNG ĐÌNH',
    name: 'Tôm Sú Nướng Muối Ớt',
    desc: 'Tôm sú tươi sống nướng than hoa trên muối ớt hồng đậm đà, thịt tôm giòn sần sật ngọt lịm.',
    price: '189.000 ₫',
    image: 'https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'bbq',
    tag: 'BESTSELLER NƯỚNG',
    name: 'Ba Chỉ Heo Nướng Kim Chi',
    desc: 'Ba chỉ heo rút sườn ướp sốt cay bí truyền, nướng xém cạnh thơm phức ăn kèm kim chi giòn cay.',
    price: '169.000 ₫',
    image: 'https://images.unsplash.com/photo-1544025162-d76694265947?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'specialty',
    tag: 'ĐẶC SẢN DÂN GIAN',
    name: 'Gà Ta Hấp Lá Chanh Hoa Tiêu',
    desc: 'Gà ta da vàng óng giòn sần sật, hấp thơm nồng hương lá chanh bánh tẻ chấm muối tiêu chanh ớt.',
    price: '119.000 ₫',
    image: 'https://images.unsplash.com/photo-1598103442097-8b74394b95c6?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'bbq',
    tag: 'HẢI SẢN CAO CẤP',
    name: 'Mực Trứng Nướng Sa Tế Côn Đảo',
    desc: 'Mực trứng căng tròn bùi béo nướng sa tế tôm cay nồng, ăn kèm rau răm và muối tiêu xanh.',
    price: '189.000 ₫',
    image: 'https://images.unsplash.com/photo-1599488615731-7e5c2823ff28?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'appetizer',
    tag: 'KHAI VỊ THANH MÁT',
    name: 'Gỏi Ngó Sen Tôm Thịt Hoàng Gia',
    desc: 'Ngó sen giòn rụm trộn tôm sú biển, thịt ba chỉ luộc và nước mắm chua ngọt thảo mộc dịu nhẹ.',
    price: '125.000 ₫',
    image: 'https://images.unsplash.com/photo-1540420773420-3366772f4999?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'specialty',
    tag: 'MÓN TỘ ĐẬM ĐÀ',
    name: 'Cá Bống Kho Tiêu Phú Quốc',
    desc: 'Cá bống tươi kho trong tộ đất với nước màu dừa Bến Tre và hạt tiêu sọ thơm cay ấm nồng.',
    price: '165.000 ₫',
    image: 'https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=700&auto=format&fit=crop&q=80',
  },
  {
    category: 'appetizer',
    tag: 'ĂN KÈM GIÒN RỤM',
    name: 'Khoai Tây Chiên Bơ Tỏi Thơm Lừng',
    desc: 'Khoai tây củ vàng thái dày chiên giòn tan lắc đều bơ béo và tỏi phi vàng thơm ngậy khó cưỡng.',
    price: '59.000 ₫',
    image: 'https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=700&auto=format&fit=crop&q=80',
  },
];

const filteredDishes = computed(() => {
  if (selectedCategory.value === 'all') return featuredDishes;
  return featuredDishes.filter(d => d.category === selectedCategory.value);
});

const diningAreas = [
  {
    title: 'Sân Vườn Hoa Sen',
    badge: '🌿 NGOÀI TRỜI THƯ THÁI',
    capacity: 'Sức chứa 80 - 100 khách',
    desc: 'Tận hưởng gió trời tự nhiên bên hồ sen thơm ngát, bàn ăn dưới vòm hoa dây leo và đèn lồng ấm cúng.',
    image: 'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=800&auto=format&fit=crop&q=85',
    tags: ['Gần gũi thiên nhiên', 'Bàn 4 - 10 người', 'Chụp ảnh check-in tuyệt đẹp'],
  },
  {
    title: 'Đại Sảnh Indochine',
    badge: '🏛️ KIẾN TRÚC ĐÔNG DƯƠNG',
    capacity: 'Sức chứa 120 khách',
    desc: 'Giao thoa giữa nét đẹp kiến trúc cổ điển Pháp và văn hóa Việt Nam, điều hòa mát mẻ, âm nhạc du dương.',
    image: 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=800&auto=format&fit=crop&q=85',
    tags: ['Điều hòa 24/7', 'Tiệc gia đình & sinh nhật', 'Ghế bọc nệm êm ái'],
  },
  {
    title: 'Phòng VIP Hoàng Gia',
    badge: '👑 RIÊNG TƯ & ĐẲNG CẤP',
    capacity: 'Sức chứa 8 - 25 khách/phòng',
    desc: 'Không gian cách âm biệt lập cao cấp, có nhân viên phục vụ riêng chuyên nghiệp và hệ thống âm thanh hiện đại.',
    image: 'https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?w=800&auto=format&fit=crop&q=85',
    tags: ['Phục vụ chuyên biệt', 'Tiếp khách đối tác', 'Bàn tiệc tròn hoàng gia'],
  },
];

const chefs = [
  {
    name: 'Nguyễn Văn Phúc',
    title: 'Bếp Trưởng Điều Hành',
    specialty: '🏆 Nghệ Nhân Chiếc Thìa Vàng',
    desc: 'Hơn 20 năm gắn bó với ẩm thực ba miền, từng phụ trách quốc yến chiêu đãi tại khách sạn 5 sao quốc tế.',
    image: 'https://images.unsplash.com/photo-1577219491135-ce391730fb2c?w=600&auto=format&fit=crop&q=85',
  },
  {
    name: 'Trần Thị Lan',
    title: 'Bếp Trưởng Cung Đình',
    specialty: '👑 Ẩm Thực Cung Đình Huế',
    desc: 'Kế thừa công thức gia truyền của gia đình nghệ nhân xứ Huế, tỉ mỉ trong từng đường dao và cách nêm ngũ vị.',
    image: 'https://images.unsplash.com/photo-1581299894007-aaa50297cf16?w=600&auto=format&fit=crop&q=85',
  },
  {
    name: 'Lê Minh Tuấn',
    title: 'Nghệ Nhân Trà & Tráng Miệng',
    specialty: '🍵 Trà Sen & Chè Dưỡng Nhan',
    desc: 'Chuyên gia nghiên cứu các loại trà thảo mộc hoa sen Tây Hồ và các món chè thanh mát chuẩn phong vị xưa.',
    image: 'https://images.unsplash.com/photo-1607631568010-a87245c0daf8?w=600&auto=format&fit=crop&q=85',
  },
];

const steps = [
  { icon: '📝', title: '1. Chọn Bàn Trực Tuyến', desc: 'Chọn ngày giờ, số lượng khách và khu vực ngồi yêu thích chỉ trong 30 giây.' },
  { icon: '📞', title: '2. Xác Nhận Tức Thì', desc: 'Nhà hàng lưu giữ bàn hoa nến chu đáo và nhân viên hỗ trợ mọi yêu cầu riêng.' },
  { icon: '🎉', title: '3. Đến & Tận Hưởng', desc: 'Đến đúng giờ, check-in nhận bàn và thưởng thức phong vị ẩm thực tuyệt hảo.' },
];

const reviews = [
  {
    name: 'Chị Hoàng Yến',
    role: 'Doanh nhân tại TP.HCM',
    date: 'Thực khách thân thiết 3 năm',
    avatar: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=160&auto=format&fit=crop&q=85',
    text: 'Không gian sân vườn thoáng mát, ẩm thực tròn vị và rất thanh tao. Gia đình tôi mỗi dịp kỷ niệm đều tin tưởng chọn Hương Sen.',
  },
  {
    name: 'Anh Minh Trí',
    role: 'Kiến trúc sư',
    date: 'Tiệc kỷ niệm ngày cưới',
    avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=160&auto=format&fit=crop&q=85',
    text: 'Bàn tiệc hoa nến được chuẩn bị vô cùng lãng mạn. Món lẩu thảo mộc hoa sen thơm ngát dịu dàng, nhân viên phục vụ cực kỳ chu đáo!',
  },
  {
    name: 'Cô Thanh Mai',
    role: 'Hội thảo gia đình 20 người',
    date: 'Tiệc mừng thọ',
    avatar: 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=160&auto=format&fit=crop&q=85',
    text: 'Phòng VIP riêng tư và sang trọng. Đồ ăn lên nóng hổi, thịt gà chắc ngọt, đặc biệt người lớn tuổi rất khen vì không hề nêm mì chính.',
  },
];
</script>

<style scoped>
/* ==========================================================================
   DOLA RESTAURANT CUISINE - UI/UX PRO DESIGN SYSTEM
   Deep Forest Canvas (#143B36), Golden Amber (#D69C52), Crisp Ivory (#FFFFFF)
   ========================================================================== */

.intro-page {
  background-color: var(--dola-canvas);
  color: var(--dola-body);
  overflow-x: hidden;
}

/* 1. HERO SECTION */
.dola-hero-section {
  min-height: max(80dvh, 650px);
  background-color: var(--dola-canvas);
  overflow: hidden;
  position: relative;
}

/* Full-bleed Hero Background Media with Ken Burns Zoom */
.hero-bg-media {
  position: absolute;
  inset: 0;
  z-index: 1;
  overflow: hidden;
  pointer-events: none;
}

.hero-bg-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: right 30%;
  filter: brightness(0.96) contrast(1.18) saturate(1.15);
  animation: heroKenBurns 22s ease-in-out infinite alternate;
}

@keyframes heroKenBurns {
  0% { transform: scale(1.0); }
  50% { transform: scale(1.05) translate(-1%, -0.5%); }
  100% { transform: scale(1.08) translate(0.5%, 0%); }
}

.hero-bg-vignette {
  position: absolute;
  inset: 0;
  background: 
    linear-gradient(90deg, rgba(18, 14, 12, 0.96) 0%, rgba(18, 14, 12, 0.82) 42%, rgba(18, 14, 12, 0.22) 75%, rgba(18, 14, 12, 0.08) 100%),
    linear-gradient(180deg, rgba(18, 14, 12, 0.5) 0%, transparent 40%, rgba(18, 14, 12, 0.95) 100%);
}

.hero-ambient-glow {
  position: absolute;
  top: 15%;
  left: 15%;
  width: 600px;
  height: 500px;
  background: radial-gradient(circle, rgba(184, 125, 50, 0.22) 0%, rgba(18, 14, 12, 0) 70%);
  pointer-events: none;
  filter: blur(50px);
}

/* Floating ambient light specks */
.hero-light-specks .speck {
  position: absolute;
  border-radius: 50%;
  background: rgba(214, 156, 82, 0.65);
  box-shadow: 0 0 10px rgba(214, 156, 82, 0.85);
  pointer-events: none;
  animation: floatSpeck 7s infinite ease-in-out alternate;
}

.speck-1 { width: 6px; height: 6px; top: 22%; left: 35%; animation-duration: 6s; }
.speck-2 { width: 10px; height: 10px; top: 58%; left: 48%; animation-duration: 8.5s; animation-delay: 1s; }
.speck-3 { width: 5px; height: 5px; top: 38%; left: 70%; animation-duration: 7.5s; animation-delay: 2s; }
.speck-4 { width: 8px; height: 8px; top: 72%; left: 22%; animation-duration: 9s; }

@keyframes floatSpeck {
  0% { transform: translateY(0) scale(0.9); opacity: 0.3; }
  50% { transform: translateY(-14px) scale(1.2); opacity: 0.8; }
  100% { transform: translateY(-28px) scale(0.9); opacity: 0.2; }
}

.min-vh-hero {
  min-height: max(78dvh, 620px);
}

/* Kicker Tagline */
.hero-kicker-tag {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 6px 16px;
  background: rgba(184, 125, 50, 0.16);
  border: 1px solid rgba(214, 156, 82, 0.4);
  border-radius: 9999px;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  color: #fce7b0;
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
}

.kicker-sparkle {
  animation: floatSpeck 3s infinite ease-in-out alternate;
}

.dola-hero-title {
  font-size: clamp(2.4rem, 1.2rem + 4vw, 4.2rem);
  font-weight: 700;
  line-height: 1.18;
  color: #ffffff;
}

.hero-gold-gradient {
  background: linear-gradient(135deg, #fce7b0 0%, #d69c52 50%, #e0aa62 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  display: inline-block;
}

.dola-hero-desc {
  font-size: 1.08rem;
  font-weight: 300;
  line-height: 1.75;
  color: #e4ded7;
  max-width: 640px;
}

/* Pill Action Buttons (Savory Table Style) */
.dola-btn-pill-primary {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 13px 30px;
  background: linear-gradient(135deg, #7A1C28 0%, #5A121A 100%);
  color: #ffffff !important;
  font-weight: 700;
  font-size: 0.95rem;
  border-radius: 9999px;
  text-decoration: none;
  box-shadow: 0 8px 24px rgba(122, 28, 40, 0.45);
  position: relative;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.dola-btn-pill-primary:hover {
  background: linear-gradient(135deg, #942433 0%, #6E1823 100%);
  transform: translateY(-2px) scale(1.02);
  box-shadow: 0 12px 32px rgba(122, 28, 40, 0.6);
  color: #ffffff !important;
}

.pill-btn-arrow {
  display: inline-block;
  transition: transform 0.25s ease;
}

.dola-btn-pill-primary:hover .pill-btn-arrow {
  transform: translateX(4px);
}

.dola-btn-pill-outline {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 13px 28px;
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.4);
  border-radius: 9999px;
  color: #ffffff;
  font-weight: 600;
  font-size: 0.95rem;
  text-decoration: none;
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.dola-btn-pill-outline:hover {
  background: rgba(255, 255, 255, 0.22);
  border-color: #ffffff;
  color: #ffffff;
  transform: translateY(-2px);
}

.dola-btn-pill-glass {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 13px 26px;
  background: rgba(184, 125, 50, 0.18);
  border: 1px solid rgba(214, 156, 82, 0.45);
  border-radius: 9999px;
  color: #fce7b0;
  font-weight: 600;
  font-size: 0.92rem;
  text-decoration: none;
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  transition: all 0.3s ease;
}

.dola-btn-pill-glass:hover {
  background: rgba(184, 125, 50, 0.32);
  border-color: #fce7b0;
  color: #ffffff;
  transform: translateY(-2px);
}

/* Symmetrical 3-Column Luxury Stats (Borderless, Clean & Airy) */
.luxury-hero-stats-board {
  display: inline-grid;
  grid-template-columns: 1fr auto 1fr auto 1fr;
  align-items: center;
  gap: 24px;
  padding: 8px 0;
  background: transparent;
  border: none;
  box-shadow: none;
  transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  margin-top: 14px;
  max-width: 100%;
}

.luxury-hero-stats-board:hover {
  transform: translateY(-2px);
}

.stat-unit {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  min-width: 105px;
}

.stat-unit-icon {
  font-size: 1.1rem;
  line-height: 1;
  margin-bottom: 4px;
}

.stars-gold {
  color: #F8D38D;
  letter-spacing: 2px;
  font-size: 0.85rem;
  filter: drop-shadow(0 2px 8px rgba(248, 211, 141, 0.65));
}

.stat-unit-val {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 3px;
  line-height: 1.15;
  margin-bottom: 3px;
}

.val-num {
  font-family: 'Plus Jakarta Sans', sans-serif;
  font-size: 1.45rem;
  font-weight: 800;
  color: #FCE7B0;
  letter-spacing: -0.01em;
  text-shadow: 0 2px 14px rgba(0, 0, 0, 0.7), 0 0 16px rgba(252, 231, 176, 0.5);
}

.val-sub {
  font-size: 0.85rem;
  color: rgba(255, 255, 255, 0.5);
  font-weight: 600;
  text-shadow: 0 1px 6px rgba(0, 0, 0, 0.7);
}

.val-prefix {
  font-size: 0.88rem;
  font-weight: 800;
  color: #F8D38D;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin-right: 3px;
  text-shadow: 0 1px 6px rgba(0, 0, 0, 0.7);
}

.stat-unit-label {
  font-size: 0.75rem;
  color: rgba(250, 247, 242, 0.85);
  font-weight: 500;
  white-space: nowrap;
  text-shadow: 0 1px 8px rgba(0, 0, 0, 0.8);
}

.stat-unit-divider {
  width: 1px;
  height: 42px;
  background: linear-gradient(180deg, transparent, rgba(214, 156, 82, 0.5), transparent);
}

.text-amber-gold {
  color: #F8D38D !important;
}

.badge-vietgap {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 4px;
  background: rgba(16, 185, 129, 0.25);
  color: #6ee7b7;
  border: 1px solid rgba(16, 185, 129, 0.45);
  font-size: 0.65rem;
  font-weight: 700;
  box-shadow: 0 1px 6px rgba(0, 0, 0, 0.4);
}

.badge-year {
  display: inline-block;
  padding: 1px 6px;
  border-radius: 4px;
  background: rgba(184, 125, 50, 0.25);
  color: #fde68a;
  border: 1px solid rgba(184, 125, 50, 0.5);
  font-size: 0.65rem;
  font-weight: 700;
  box-shadow: 0 1px 6px rgba(0, 0, 0, 0.4);
}

@media (max-width: 576px) {
  .luxury-hero-stats-board {
    display: flex;
    flex-direction: column;
    width: 100%;
    gap: 12px;
    padding: 8px 0;
  }
  .stat-unit {
    width: 100%;
    min-width: 0;
  }
  .stat-unit-divider {
    width: 60%;
    height: 1px;
    background: linear-gradient(90deg, transparent, rgba(214, 156, 82, 0.5), transparent);
  }
}

/* Savory Table Style 4-Pillar Value Strip (Warm Ivory & Bronze) */
.savory-value-strip {
  background: #F4ECE1;
  border-top: 1px solid #E6DDD2;
  border-bottom: 1px solid #E6DDD2;
  box-shadow: 0 4px 20px rgba(60, 45, 35, 0.05);
}

.savory-pillar-item {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  box-shadow: 0 3px 12px rgba(60, 45, 35, 0.04);
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.savory-pillar-item:hover {
  transform: translateY(-4px);
  background: #FFFFFF;
  border-color: #B87D32;
  box-shadow: 0 10px 25px rgba(60, 45, 35, 0.12);
}

.pillar-icon-box {
  width: 46px;
  height: 46px;
  border-radius: 10px;
  background: #F8F3EC;
  border: 1px solid #DFC9B0;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: transform 0.3s ease;
}

.savory-pillar-item:hover .pillar-icon-box {
  transform: scale(1.1) rotate(4deg);
  background: #EFE6D8;
}

.pillar-icon {
  font-size: 1.5rem;
}

.pillar-title {
  color: #1F1A17;
  font-size: 0.95rem;
  font-weight: 700;
}

.pillar-subtitle {
  font-size: 0.78rem;
  color: #6E6157;
  line-height: 1.35;
}

/* Section 4.5: Photo Gallery (Khoảnh Khắc Mỹ Vị & Không Gian) */
.photo-gallery-section {
  background: var(--dola-canvas);
}

.gallery-photo-card {
  height: 280px;
  border: 1px solid rgba(214, 156, 82, 0.2);
  transition: transform 0.4s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.4s ease, border-color 0.4s ease;
}

.gallery-photo-card:hover {
  transform: translateY(-6px);
  border-color: rgba(214, 156, 82, 0.6);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.5);
}

.gallery-photo-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.7s cubic-bezier(0.16, 1, 0.3, 1);
}

.gallery-photo-card:hover .gallery-photo-img {
  transform: scale(1.08);
}

.gallery-photo-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, transparent 35%, rgba(16, 48, 44, 0.92) 100%);
  opacity: 0.85;
  transition: opacity 0.3s ease;
}

.gallery-photo-card:hover .gallery-photo-overlay {
  opacity: 1;
}

.gallery-badge {
  font-size: 0.7rem;
  font-weight: 700;
  color: #fce7b0;
  background: rgba(20, 59, 54, 0.85);
  border: 1px solid rgba(214, 156, 82, 0.45);
  padding: 4px 10px;
  border-radius: 6px;
  backdrop-filter: blur(8px);
}

/* Animations & Staggered Transitions */
.animate-fade-in {
  animation: fadeIn 0.8s ease forwards;
}

.animate-fade-in-up {
  animation: fadeInUp 0.8s cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

.delay-1 {
  animation-delay: 0.15s;
}

.delay-2 {
  animation-delay: 0.3s;
}

.delay-3 {
  animation-delay: 0.45s;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(24px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Enhanced hover transitions for existing dish cards */
.dish-card-lux {
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.35s ease, border-color 0.35s ease;
}

.dish-card-lux:hover {
  transform: translateY(-8px);
  border-color: rgba(214, 156, 82, 0.5) !important;
  box-shadow: 0 20px 45px rgba(0, 0, 0, 0.45);
}

.dish-card-lux:hover .dish-img {
  transform: scale(1.08);
}

.area-showcase-card {
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.35s ease;
}

.area-showcase-card:hover {
  transform: translateY(-8px);
  box-shadow: 0 20px 45px rgba(0, 0, 0, 0.45);
}

.area-showcase-card:hover .area-img {
  transform: scale(1.08);
}

.chef-card-lux {
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.35s ease;
}

.chef-card-lux:hover {
  transform: translateY(-8px);
  box-shadow: 0 20px 45px rgba(0, 0, 0, 0.45);
}

.chef-card-lux:hover .chef-portrait-img {
  transform: scale(1.08);
}

/* 2. CORE FEATURES */
.dola-surface-band {
  background-color: var(--dola-surface-alt);
  border-top: 1px solid #E6DDD2;
  border-bottom: 1px solid #E6DDD2;
}

.feature-card-modern {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  border-radius: 12px;
  box-shadow: 0 4px 15px rgba(60, 45, 35, 0.05);
  transition: all 0.25s ease;
}

.feature-card-modern:hover {
  transform: translateY(-4px);
  border-color: #B87D32;
  background: #FFFFFF;
  box-shadow: 0 12px 28px rgba(60, 45, 35, 0.12);
}

.feature-icon-wrapper {
  width: 48px;
  height: 48px;
  border-radius: 10px;
  background: #F8F3EC;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #DFC9B0;
}

.feature-emoji {
  font-size: 1.6rem;
}

/* 3. STORY COLLAGE */
.story-collage-container {
  padding: 10px;
}

.main-story-photo-wrap {
  width: 88%;
  aspect-ratio: 4 / 3;
  border-radius: 16px;
  overflow: hidden;
  border: 2px solid rgba(184, 125, 50, 0.3);
  box-shadow: 0 20px 40px rgba(60, 45, 35, 0.15);
}

.story-main-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.sub-story-photo-wrap {
  position: absolute;
  right: 0;
  bottom: -20px;
  width: 52%;
  aspect-ratio: 4 / 3;
  border-radius: 12px;
  overflow: hidden;
  border: 3px solid #FAF7F2;
  box-shadow: 0 20px 40px rgba(60, 45, 35, 0.2);
  z-index: 2;
}

.story-sub-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.story-heritage-stamp {
  position: absolute;
  top: -15px;
  left: -10px;
  z-index: 3;
  background: #7A1C28;
  color: #FFFFFF;
  padding: 10px 14px;
  border-radius: 12px;
  box-shadow: 0 10px 25px rgba(122, 28, 40, 0.35);
  text-align: center;
}

.stamp-inner {
  display: flex;
  flex-direction: column;
  align-items: center;
  line-height: 1.1;
}

.stamp-icon { font-size: 1.2rem; }
.stamp-year { font-size: 0.82rem; font-weight: 800; }
.stamp-text { font-size: 0.65rem; font-weight: 700; text-transform: uppercase; }

.section-tag {
  font-size: 0.75rem;
  font-weight: 800;
  letter-spacing: 0.15em;
  color: #8C5E28;
  text-transform: uppercase;
}

.section-heading {
  font-size: clamp(2rem, 1rem + 2.5vw, 2.9rem);
  font-weight: 600;
  color: #1F1A17;
  line-height: 1.25;
}

.value-item-card {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  box-shadow: 0 3px 12px rgba(60, 45, 35, 0.04);
  transition: transform 0.2s ease, border-color 0.2s ease;
}

.value-item-card:hover {
  transform: translateX(4px);
  border-color: #B87D32;
}

.value-icon {
  font-size: 1.4rem;
}

/* 4. DISHES GALLERY */
.dish-filter-pill {
  padding: 7px 18px;
  background: #FFFFFF;
  border: 1px solid #DFC9B0;
  border-radius: 9999px;
  color: #574A40;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.dish-filter-pill:hover,
.dish-filter-pill.active {
  background: #7A1C28;
  color: #FFFFFF;
  border-color: #7A1C28;
  box-shadow: 0 4px 14px rgba(122, 28, 40, 0.35);
}

.dish-card-lux {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 6px 20px rgba(60, 45, 35, 0.06);
}

.dish-card-lux:hover {
  transform: translateY(-6px);
  border-color: #B87D32;
  box-shadow: 0 16px 36px rgba(60, 45, 35, 0.14);
}

.dish-photo-wrap {
  width: 100%;
  aspect-ratio: 16 / 10;
}

.dish-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.5s ease;
}

.dish-card-lux:hover .dish-img {
  transform: scale(1.06);
}

.dish-category-badge {
  position: absolute;
  top: 10px;
  left: 10px;
  background: #7A1C28;
  color: #FFFFFF;
  font-size: 0.65rem;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 6px;
  letter-spacing: 0.05em;
}

.dish-rating-badge {
  position: absolute;
  top: 10px;
  right: 10px;
  background: rgba(0, 0, 0, 0.65);
  color: #f59e0b;
  font-size: 0.72rem;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 6px;
}

.dish-price-text {
  font-size: 1.05rem;
  font-weight: 800;
  color: #7A1C28;
}

.btn-dish-reserve {
  font-size: 0.76rem;
  font-weight: 700;
  color: #7A1C28;
  background: #F8F3EC;
  border: 1px solid #DFC9B0;
  text-decoration: none;
  padding: 4px 10px;
  border-radius: 6px;
  transition: all 0.15s ease;
}

.btn-dish-reserve:hover {
  color: #FFFFFF;
  background: #7A1C28;
  border-color: #7A1C28;
}

/* 5. DINING SPACES */
.area-showcase-card {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  border-radius: 14px;
  box-shadow: 0 6px 20px rgba(60, 45, 35, 0.06);
  transition: all 0.3s ease;
}

.area-showcase-card:hover {
  transform: translateY(-5px);
  border-color: #B87D32;
  box-shadow: 0 16px 36px rgba(60, 45, 35, 0.12);
}

.area-media {
  width: 100%;
  aspect-ratio: 16 / 10;
}

.area-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.6s ease;
}

.area-showcase-card:hover .area-img {
  transform: scale(1.05);
}

.area-badge {
  position: absolute;
  bottom: 12px;
  left: 12px;
  background: #7A1C28;
  color: #FFFFFF;
  font-size: 0.7rem;
  font-weight: 800;
  padding: 4px 10px;
  border-radius: 6px;
}

.area-chip {
  font-size: 0.72rem;
  color: #6E6157;
  background: #F4ECE1;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1px solid #E6DDD2;
}

/* 6. CHEFS */
.chef-card-lux {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  border-radius: 14px;
  box-shadow: 0 6px 20px rgba(60, 45, 35, 0.06);
  transition: all 0.3s ease;
}

.chef-card-lux:hover {
  transform: translateY(-5px);
  border-color: #B87D32;
  background: #FFFFFF;
  box-shadow: 0 16px 36px rgba(60, 45, 35, 0.12);
}

.chef-portrait-wrap {
  width: 120px;
  height: 120px;
  border-radius: 50%;
  overflow: hidden;
  border: 3px solid #B87D32;
  box-shadow: 0 8px 24px rgba(60, 45, 35, 0.15);
}

.chef-portrait-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.5s ease;
}

.chef-card-lux:hover .chef-portrait-img {
  transform: scale(1.08);
}

.chef-star-badge {
  position: absolute;
  bottom: 2px;
  right: 4px;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background: #B87D32;
  color: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
  font-weight: 800;
  border: 2px solid #FFFFFF;
}

.chef-specialty-pill {
  display: inline-block;
  background: #F8F3EC;
  border: 1px solid #DFC9B0;
  color: #8C5E28;
  font-size: 0.74rem;
  font-weight: 700;
  padding: 3px 12px;
  border-radius: 9999px;
}

/* 7. STEPS */
.step-card-modern {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  border-radius: 14px;
  box-shadow: 0 6px 20px rgba(60, 45, 35, 0.06);
  transition: all 0.25s ease;
}

.step-card-modern:hover {
  transform: translateY(-4px);
  border-color: #B87D32;
  background: #FFFFFF;
}

.step-badge-number {
  display: inline-block;
  background: #7A1C28;
  color: #FFFFFF;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  padding: 3px 12px;
  border-radius: 9999px;
  margin-bottom: 16px;
}

.step-icon-large {
  font-size: 2.8rem;
}

/* 8. REVIEWS */
.review-card-modern {
  background: #FFFFFF;
  border: 1px solid #EADFD0;
  border-radius: 14px;
  box-shadow: 0 6px 20px rgba(60, 45, 35, 0.06);
  transition: all 0.25s ease;
}

.review-card-modern:hover {
  transform: translateY(-4px);
  border-color: #B87D32;
  box-shadow: 0 12px 28px rgba(60, 45, 35, 0.12);
}

.verified-tag {
  font-size: 0.68rem;
  color: #10b981;
  font-weight: 700;
  background: rgba(16, 185, 129, 0.1);
  padding: 2px 8px;
  border-radius: 4px;
  border: 1px solid rgba(16, 185, 129, 0.25);
}

.reviewer-avatar-img {
  width: 44px;
  height: 44px;
  border-radius: 50%;
  object-fit: cover;
  border: 2px solid #B87D32;
}

/* 9. BOTTOM CTA */
.dola-bottom-cta {
  background: linear-gradient(180deg, #F4ECE1 0%, #EAE0D3 100%);
  border-top: 1px solid #E6DDD2;
}

.cta-bg-glow {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 600px;
  height: 300px;
  background: radial-gradient(ellipse, rgba(214, 156, 82, 0.12) 0%, transparent 70%);
  pointer-events: none;
}

/* RESPONSIVE BREAKPOINTS */
@media (max-width: 991px) {
  .hero-stats-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: 14px;
  }

  .main-story-photo-wrap {
    width: 100%;
  }

  .sub-story-photo-wrap {
    display: none !important;
  }

  .story-heritage-stamp {
    position: static;
    display: inline-block;
    margin-bottom: 16px;
  }
}

@media (max-width: 575px) {
  .hero-stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .trust-strip {
    flex-direction: column;
    align-items: flex-start !important;
    gap: 8px !important;
  }

  .trust-divider {
    display: none;
  }
}
</style>

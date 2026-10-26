<template>
  <div class="dola-theme py-5">
    <div class="container py-3">

      <!-- ================================================================= -->
      <!-- 1. TẠP CHÍ ẨM THỰC HEADER & MASTHEAD                              -->
      <!-- ================================================================= -->
      <header class="news-masthead text-center mb-5 pb-4 border-bottom position-relative">
        <div class="d-inline-flex align-items-center gap-2 mb-2 px-3 py-1 rounded-pill bg-wine-subtle text-wine fw-bold small">
          <span>🏛️</span>
          <span style="letter-spacing: 0.12em; text-transform: uppercase; font-size: 0.76rem;">Tạp Chí Ẩm Thực &amp; Sự Kiện Hương Sen</span>
        </div>
        <h1 class="dola-font-title text-gold display-3 fw-bold mb-2">Bản Tin &amp; Bài Viết Ẩm Thực</h1>
        <p class="text-body-dola mx-auto fs-5 mb-3" style="max-width: 680px;">
          Nơi lưu giữ những câu chuyện văn hóa ẩm thực truyền thống, công bố tuyệt phẩm món mới và các bản tin ưu đãi tri ân đặc quyền dành riêng cho quý thực khách.
        </p>
        <div class="d-flex justify-content-center align-items-center gap-3 small text-muted flex-wrap">
          <span>📅 Xuất bản: <strong>Hôm nay</strong></span>
          <span>•</span>
          <span>✍️ Ban Biên Tập: <strong>Hương Sen Cuisine</strong></span>
          <span>•</span>
          <span>✨ <strong>Số Phát Hành Mùa Lễ Hội 2026</strong></span>
        </div>
      </header>

      <!-- ================================================================= -->
      <!-- 2. BỘ LỌC CHUYÊN MỤC BÁO CHÍ & TÌM KIẾM (ĐẶT Ở ĐẦU TRANG TIỆN DỤNG) -->
      <!-- ================================================================= -->
      <div class="news-filter-bar d-flex flex-column flex-lg-row gap-3 align-items-lg-center justify-content-between my-4 p-3 bg-white rounded-4 shadow-sm border">
        <!-- Chuyên mục bài viết -->
        <div class="d-flex gap-2 overflow-auto pb-1 dola-chips-scroll">
          <button
            type="button"
            class="dola-filter-chip"
            :class="{ 'dola-filter-chip-active': selectedCategory === 'all' }"
            @click="selectedCategory = 'all'"
          >
            📰 Tất Cả ({{ allArticles.length }})
          </button>
          <button
            type="button"
            class="dola-filter-chip"
            :class="{ 'dola-filter-chip-active': selectedCategory === 'promotion' }"
            @click="selectedCategory = 'promotion'"
          >
            🎁 Voucher &amp; Sự Kiện ({{ promoArticles.length }})
          </button>
          <button
            type="button"
            class="dola-filter-chip"
            :class="{ 'dola-filter-chip-active': selectedCategory === 'food' }"
            @click="selectedCategory = 'food'"
          >
            🍳 Món Mới &amp; Tuyệt Kỹ ({{ foodArticles.length }})
          </button>
          <button
            type="button"
            class="dola-filter-chip"
            :class="{ 'dola-filter-chip-active': selectedCategory === 'story' }"
            @click="selectedCategory = 'story'"
          >
            🌿 Câu Chuyện Bếp Nhà ({{ storyArticles.length }})
          </button>
        </div>

        <!-- Ô Tìm kiếm bài báo -->
        <div class="position-relative" style="min-width: 260px; max-width: 340px; width: 100%;">
          <span class="position-absolute top-50 start-0 translate-middle-y ps-3 text-gold">🔍</span>
          <input
            type="text"
            class="dola-input ps-5"
            placeholder="Tìm kiếm bài viết, voucher, món..."
            v-model="searchQuery"
          />
        </div>
      </div>

      <!-- ================================================================= -->
      <!-- 3. DANH SÁCH BÀI BÁO (FEED DỌC BÀI VIẾT, HÌNH ẢNH MÓN VỪA VẶN ĐẦY ĐỦ) -->
      <!-- ================================================================= -->
      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border text-gold mb-3" style="width: 3rem; height: 3rem;"></div>
        <p class="text-body-dola">Đang tải những bài viết ẩm thực &amp; bản tin mới nhất...</p>
      </div>

      <div v-else-if="filteredArticles.length === 0" class="dola-card p-5 text-center my-4">
        <div class="fs-1 mb-3">📰</div>
        <h4 class="text-gold dola-font-title fs-2">Không Có Bài Báo Nào Phù Hợp</h4>
        <p class="text-body-dola small mb-3">
          Không tìm thấy bài viết nào khớp với từ khóa "{{ searchQuery }}". Vui lòng thử tìm kiếm với từ khóa khác.
        </p>
        <button class="dola-btn-primary mx-auto" @click="resetFilters">
          Xem Tất Cả Bài Báo
        </button>
      </div>

      <!-- Danh sách bài báo theo hàng dọc (Feed bài viết chuẩn Editorial Magazine) -->
      <div v-else class="mb-5">
        <!-- Tiêu đề đề mục phân loại -->
        <div class="d-flex align-items-center justify-content-between mb-3">
          <h4 class="dola-font-title fs-4 text-dark mb-0">
            <span v-if="selectedCategory === 'all' && !searchQuery">📰 Bản Tin &amp; Chuyên Mục Mới Nhất</span>
            <span v-else-if="selectedCategory === 'promotion'">🎁 Danh Sách Sự Kiện &amp; Voucher ({{ filteredArticles.length }})</span>
            <span v-else-if="selectedCategory === 'food'">🍳 Món Mới &amp; Tuyệt Kỹ Bếp Trưởng ({{ filteredArticles.length }})</span>
            <span v-else-if="selectedCategory === 'story'">🌿 Câu Chuyện Bếp Nhà &amp; Di Sản ({{ filteredArticles.length }})</span>
            <span v-else>🔍 Kết Quả Tìm Kiếm: "{{ searchQuery }}" ({{ filteredArticles.length }})</span>
          </h4>
          <small class="text-muted" style="font-size: 0.8rem;">
            Hiển thị {{ filteredArticles.length }} bài viết
          </small>
        </div>

        <!-- Dòng bài viết nối tiếp xuống dưới (Mỗi bài viết 1 dòng gọn gàng, hình ảnh vừa vặn) -->
        <div class="d-flex flex-column gap-3">
          <article
            v-for="(article, idx) in filteredArticles"
            :key="article.id"
            class="editorial-feed-card rounded-4 border bg-white overflow-hidden p-3 p-md-3.5 d-flex flex-column flex-md-row gap-3 gap-md-4 align-items-stretch"
            @click="openArticle(article)"
          >
            <!-- Khung hình ảnh món ăn vừa vặn (Tỷ lệ 16:10 chuẩn, hiển thị trọn vẹn món ăn) -->
            <div class="feed-img-container position-relative rounded-3 overflow-hidden flex-shrink-0">
              <img
                :src="article.coverImage"
                :alt="article.title"
                class="feed-img"
                loading="lazy"
                @error="onImageError"
              />
              <!-- Tag phân loại nổi bật góc trên -->
              <div class="position-absolute top-0 start-0 m-2.5">
                <span v-if="idx === 0 && selectedCategory === 'all' && !searchQuery" class="badge bg-wine text-white px-2.5 py-1 rounded-pill small fw-bold">
                  ⭐ TIÊU ĐIỂM
                </span>
                <span v-else-if="article.type === 'promotion'" class="badge bg-wine text-white px-2.5 py-1 rounded-pill small fw-bold">
                  🎁 VOUCHER
                </span>
                <span v-else-if="article.type === 'food'" class="badge bg-gold text-dark px-2.5 py-1 rounded-pill small fw-bold">
                  🍳 MÓN MỚI
                </span>
                <span v-else class="badge bg-dark text-white px-2.5 py-1 rounded-pill small fw-bold">
                  🌿 BẾP NHÀ
                </span>
              </div>
              <!-- Thời gian đọc góc trên phải -->
              <div class="position-absolute top-0 end-0 m-2.5">
                <span class="badge bg-dark bg-opacity-75 text-white px-2 py-1 rounded small">
                  ⏱️ {{ article.readTime }} phút
                </span>
              </div>
              <!-- Chuyên mục góc dưới trái -->
              <div class="feed-category-tag position-absolute bottom-0 start-0 m-2.5">
                {{ article.categoryName }}
              </div>
            </div>

            <!-- Khối nội dung chi tiết bài viết (Bên phải) -->
            <div class="d-flex flex-column justify-content-between flex-grow-1">
              <div>
                <!-- Ngày đăng & tác giả -->
                <div class="d-flex align-items-center gap-2 mb-1 text-muted small" style="font-size: 0.76rem;">
                  <span>🕒 {{ formatDate(article.publishedAt) }}</span>
                  <span>•</span>
                  <span>✍️ {{ article.author }}</span>
                </div>

                <!-- Tiêu đề bài viết -->
                <h3 class="feed-title fs-5 fw-bold mb-1.5">
                  {{ article.title }}
                </h3>

                <!-- Đoạn trích tóm tắt -->
                <p class="feed-excerpt text-muted small mb-2.5">
                  {{ article.excerpt }}
                </p>

                <!-- Dải thông tin đặc quyền (Callout strip) -->
                <!-- 1. Voucher Strip -->
                <div v-if="article.type === 'promotion'" class="feed-callout-strip feed-callout-promo p-2 px-3 rounded-3 mb-2 d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2">
                  <div class="d-flex align-items-center gap-2 flex-wrap">
                    <span class="badge bg-wine text-white px-2.5 py-1 rounded-pill font-monospace fw-bold" style="font-size: 0.82rem;">
                      MÃ: {{ article.promoCode }}
                    </span>
                    <strong class="text-wine fs-6">{{ article.promoSummary }}</strong>
                    <small class="text-muted" style="font-size: 0.72rem;">
                      ({{ article.minimumOrderValue > 0 ? ('Đơn từ ' + formatCurrency(article.minimumOrderValue)) : 'Mọi hóa đơn tiệc' }} · HSD: {{ formatDate(article.endAt) }})
                    </small>
                  </div>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-danger rounded-pill px-2.5 py-1 fw-semibold flex-shrink-0 d-inline-flex align-items-center gap-1"
                    style="font-size: 0.75rem;"
                    @click.stop="copyVoucher(article.promoCode)"
                    title="Sao chép mã voucher"
                  >
                    <span v-if="copiedCode === article.promoCode">✓ Đã Chép</span>
                    <span v-else>📋 Chép Mã</span>
                  </button>
                </div>

                <!-- 2. Food Strip -->
                <div v-else-if="article.type === 'food'" class="feed-callout-strip feed-callout-food p-2 px-3 rounded-3 mb-2 d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2">
                  <div class="d-flex align-items-center gap-2 flex-wrap">
                    <span class="badge bg-gold text-dark px-2.5 py-1 rounded-pill fw-bold" style="font-size: 0.75rem;">
                      TUYỆT PHẨM
                    </span>
                    <strong class="text-wine fs-6">{{ article.foodName }}</strong>
                    <small class="text-muted" style="font-size: 0.72rem;">
                      (100% Nông sản VietGAP tươi sạch · Chế biến độc bản trong ngày)
                    </small>
                  </div>
                  <div class="text-sm-end flex-shrink-0">
                    <span class="badge bg-wine text-white px-3 py-1.5 rounded-pill fw-bold" style="font-size: 0.82rem;">
                      Giá từ: {{ formatCurrency(article.foodPrice) }}
                    </span>
                  </div>
                </div>

                <!-- 3. Story Strip -->
                <div v-else class="feed-callout-strip feed-callout-story p-2 px-3 rounded-3 mb-2 d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2">
                  <div class="d-flex align-items-center gap-2 flex-wrap">
                    <span class="badge bg-dark text-white px-2.5 py-1 rounded-pill fw-bold" style="font-size: 0.72rem;">
                      ✨ TINH HOA 2026
                    </span>
                    <strong class="text-dark fs-6">Triết Lý Ẩm Thực Cung Đình &amp; Lòng Hiếu Khách</strong>
                  </div>
                  <small class="text-muted" style="font-size: 0.72rem;">
                    Lưu giữ trọn vẹn hồn cốt truyền thống gia truyền
                  </small>
                </div>
              </div>

              <!-- Chân bài viết: Tác giả & Nút Đọc bài -->
              <div class="d-flex align-items-center justify-content-between pt-2 border-top mt-1">
                <div class="d-flex align-items-center gap-2">
                  <div class="author-avatar-sm">🪷</div>
                  <small class="fw-semibold text-dark" style="font-size: 0.8rem;">{{ article.author }}</small>
                </div>
                <button type="button" class="btn-read-story btn-sm">
                  <span>Đọc Toàn Bộ Bài Báo</span>
                  <span>➔</span>
                </button>
              </div>
            </div>
          </article>
        </div>
      </div>

      <!-- 5. BANNER ĐĂNG KÝ BẢN TIN CUỐI TRANG                             -->
      <!-- ================================================================= -->
      <section class="dola-card p-4 p-md-5 mt-5 text-center position-relative overflow-hidden">
        <span class="dola-pill-badge mb-3">📬 BẢN TIN ẨM THỰC HÀNG TUẦN</span>
        <h3 class="dola-font-title text-gold fs-1 mb-2">Đăng Ký Nhận Tin Tức &amp; Voucher Sớm Nhất</h3>
        <p class="text-body-dola mx-auto mb-4" style="max-width: 620px;">
          Trở thành thành viên thân thiết của Nhà hàng Hương Sen để nhận thông báo sớm nhất về các tuần lễ món ngon, vé mời tiệc rượu vang và mã ưu đãi sinh nhật độc quyền.
        </p>
        <div class="d-flex justify-content-center gap-3 flex-wrap">
          <router-link to="/register" class="dola-btn-primary px-4">
            <span>✨</span>
            <span>Đăng Ký Thành Viên Miễn Phí</span>
          </router-link>
          <router-link to="/customer/reservation" class="dola-btn-outline px-4">
            <span>📅</span>
            <span>Đặt Bàn Thưởng Thức Ngay</span>
          </router-link>
        </div>
      </section>

    </div>

    <!-- ================================================================= -->
    <!-- 6. MODAL ĐỌC BÀI BÁO HOÀN CHỈNH (FULL ARTICLE EDITORIAL READER)   -->
    <!-- ================================================================= -->
    <div
      v-if="selectedArticle"
      class="article-reader-backdrop"
      @click.self="closeArticle"
    >
      <div class="article-reader-container animate-fade-in-up">
        <!-- Close Button -->
        <button type="button" class="article-reader-close-btn" @click="closeArticle" aria-label="Đóng bài báo">
          ✕
        </button>

        <article class="article-reader-content">
          <!-- Article Category Header -->
          <div class="text-center mb-3">
            <span class="article-tag px-3 py-1.5" :class="selectedArticle.tagClass">
              {{ selectedArticle.categoryName }}
            </span>
          </div>

          <!-- Article Title -->
          <h1 class="article-reader-title text-center fw-bold mb-3">
            {{ selectedArticle.title }}
          </h1>

          <!-- Meta Author & Date -->
          <div class="d-flex justify-content-center align-items-center gap-3 small text-muted mb-4 pb-3 border-bottom flex-wrap">
            <span>✍️ Tác giả: <strong class="text-dark">{{ selectedArticle.author }}</strong></span>
            <span>•</span>
            <span>🕒 Xuất bản: <strong>{{ formatDate(selectedArticle.publishedAt) }}</strong></span>
            <span>•</span>
            <span>⏱️ <strong>{{ selectedArticle.readTime }} phút đọc</strong></span>
          </div>

          <!-- Hero Article Image -->
          <div class="article-reader-hero-img-wrap rounded-4 overflow-hidden mb-4 shadow-sm">
            <img
              :src="selectedArticle.coverImage"
              :alt="selectedArticle.title"
              class="w-100 h-100 object-fit-cover"
              @error="onImageError"
            />
          </div>

          <!-- Article Lead Paragraph (Sapo) -->
          <div class="article-reader-lead p-3.5 rounded-3 mb-4">
            {{ selectedArticle.leadParagraph }}
          </div>

          <!-- Article Body Paragraphs -->
          <div class="article-reader-body text-body-dola mb-4">
            <p v-for="(pText, pIdx) in selectedArticle.bodyParagraphs" :key="pIdx" class="mb-3">
              {{ pText }}
            </p>
          </div>

          <!-- SPECIAL CONTENT: VOUCHER TICKET BOX (If article is about promotion) -->
          <div v-if="selectedArticle.type === 'promotion'" class="article-voucher-showcase p-4 rounded-4 mb-4 text-center">
            <div class="d-inline-flex align-items-center gap-2 mb-2 px-3 py-1 rounded-pill bg-white text-wine fw-bold small">
              <span>🎟️</span>
              <span>ĐẶC QUYỀN VOUCHER BÀI VIẾT</span>
            </div>
            <h4 class="fw-bold mb-2 text-white">{{ selectedArticle.promoSummary }}</h4>
            <p class="text-white-50 small mb-3">
              Áp dụng cho mọi bàn đặt tại Hương Sen từ ngày <strong>{{ formatDate(selectedArticle.startAt) }}</strong> đến <strong>{{ formatDate(selectedArticle.endAt) }}</strong>.
              {{ selectedArticle.minimumOrderValue > 0 ? `Đơn hàng tối thiểu: ${formatCurrency(selectedArticle.minimumOrderValue)}.` : '' }}
            </p>

            <div class="d-flex align-items-center justify-content-center gap-3 flex-wrap">
              <div class="voucher-code-badge-lg font-monospace">
                {{ selectedArticle.promoCode }}
              </div>
              <button
                type="button"
                class="btn-copy-code-lg"
                :class="{ 'copied': copiedCode === selectedArticle.promoCode }"
                @click="copyVoucher(selectedArticle.promoCode)"
              >
                {{ copiedCode === selectedArticle.promoCode ? '✓ Đã Lưu Mã!' : '📋 Sao Chép Mã Voucher' }}
              </button>
            </div>
          </div>

          <!-- SPECIAL CONTENT: FOOD PRESENTATION (If article is about food) -->
          <div v-else-if="selectedArticle.type === 'food'" class="article-food-showcase p-4 rounded-4 mb-4">
            <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3">
              <div>
                <small class="text-muted text-uppercase fw-bold" style="font-size: 0.72rem;">Tuyệt phẩm bếp trưởng khuyên dùng</small>
                <h4 class="fw-bold text-wine mb-1">{{ selectedArticle.foodName }}</h4>
                <div class="text-muted small">100% Nông sản hữu cơ VietGAP tươi mới nhập trong ngày</div>
              </div>
              <div class="text-md-end">
                <small class="d-block text-muted" style="font-size: 0.72rem;">Giá thưởng thức từ</small>
                <strong class="fs-3 text-wine">{{ formatCurrency(selectedArticle.foodPrice) }}</strong>
              </div>
            </div>
          </div>

          <!-- Quote from Head Chef -->
          <blockquote class="article-chef-quote p-4 rounded-3 my-4">
            <p class="fst-italic mb-2 fs-6 text-dark">
              "Tại Hương Sen, mỗi món ăn không chỉ đơn thuần là sự kết hợp của gia vị và nhiệt độ, mà là câu chuyện văn hóa, là lòng trân trọng gửi trao đến từng thực khách trong từng khoảnh khắc sum vầy."
            </p>
            <footer class="text-end small fw-bold text-wine">
              — Bếp Trưởng Nguyễn Khắc Huy &amp; Đội Ngũ Hương Sen Cuisine
            </footer>
          </blockquote>

          <!-- Action Buttons Bottom -->
          <div class="d-flex justify-content-between align-items-center pt-4 border-top flex-wrap gap-3">
            <button type="button" class="btn btn-outline-secondary rounded-pill px-4" @click="closeArticle">
              ✕ Đóng Bài Báo
            </button>
            <router-link
              :to="{ path: '/customer/reservation', query: selectedArticle.promoCode ? { promo: selectedArticle.promoCode } : {} }"
              class="dola-btn-primary px-4"
              @click="closeArticle"
            >
              <span>📅</span>
              <span>Đặt Bàn Thưởng Thức Ngay</span>
            </router-link>
          </div>
        </article>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import axios from 'axios';

const API_BASE = 'http://localhost:5079/api';

const loading = ref(true);
const selectedCategory = ref('all'); // 'all' | 'promotion' | 'food' | 'story'
const searchQuery = ref('');
const copiedCode = ref('');
const selectedArticle = ref(null);

const rawPromotions = ref([]);
const rawFoods = ref([]);

// Default fallback images
const defaultPromoImages = [
  'https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=1000&auto=format&fit=crop&q=80',
  'https://images.unsplash.com/photo-1544025162-d76694265947?w=1000&auto=format&fit=crop&q=80',
  'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1000&auto=format&fit=crop&q=80'
];

const defaultFoodImage = 'https://images.unsplash.com/photo-1544025162-d76694265947?w=800&auto=format&fit=crop&q=80';

// LocalStorage Keys for admin selection
const PUBLISHED_FOODS_KEY = 'published_news_food_ids';
const PUBLISHED_PROMOS_KEY = 'published_news_promo_ids';

async function fetchNewsData() {
  loading.value = true;
  try {
    // 1. Fetch Promotions from public customer endpoint
    try {
      const promoRes = await axios.get(`${API_BASE}/customer/promotions`);
      if (promoRes.data?.success) {
        rawPromotions.value = promoRes.data.data || [];
      }
    } catch (err) {
      console.warn('Lỗi khi tải khuyến mãi:', err);
    }

    // 2. Fetch Foods from customer menu
    try {
      const menuRes = await axios.get(`${API_BASE}/customer/menu`);
      if (menuRes.data?.success) {
        const categories = menuRes.data.data || [];
        const flattened = [];
        categories.forEach(cat => {
          (cat.foods || []).forEach(f => {
            flattened.push({
              ...f,
              categoryName: cat.categoryName
            });
          });
        });
        rawFoods.value = flattened;
      }
    } catch (err) {
      console.warn('Lỗi khi tải thực đơn món ăn:', err);
    }
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  fetchNewsData();
});

// =========================================================================
// TRANSFORM DATABASE PROMOTIONS INTO RICH EDITORIAL ARTICLES
// =========================================================================
const promoArticles = computed(() => {
  let allowedIds = null;
  try {
    const saved = localStorage.getItem(PUBLISHED_PROMOS_KEY);
    if (saved) allowedIds = JSON.parse(saved);
  } catch {
    allowedIds = null;
  }

  // Filter by admin selection if configured, otherwise include all active
  const filteredPromos = rawPromotions.value.filter(p => {
    if (allowedIds !== null && Array.isArray(allowedIds)) {
      return allowedIds.includes(p.promotionId);
    }
    return true;
  });

  return filteredPromos.map((p, idx) => {
    const isPercent = p.promotionType === 'percent';
    const discountText = isPercent ? `Giảm ${p.promotionValue}%` : `Giảm ${formatCurrency(p.promotionValue)}`;
    const coverImage = defaultPromoImages[idx % defaultPromoImages.length];

    return {
      id: `promo-${p.promotionId}`,
      type: 'promotion',
      categoryName: 'SỰ KIỆN & ƯU ĐÃI',
      tagClass: 'badge-promo-tag',
      title: `Chương Trình Tri Ân: Tặng Voucher [${p.promotionCode}] ${discountText} Cho Bữa Tiệc Sum Vầy`,
      author: 'Ban Quản Trị Hương Sen',
      publishedAt: p.createdAt || p.startAt,
      readTime: 3,
      coverImage: coverImage,
      excerpt: `Nhằm mang đến những trải nghiệm ẩm thực trọn vẹn nhất, Hương Sen trân trọng gửi trao chương trình "${p.promotionName}" với ưu đãi ${discountText} cho mọi bàn tiệc.`,
      leadParagraph: `Hương Sen tin rằng mỗi bữa ăn là một dịp sum họp quý giá của gia đình và đối tác. Chúng tôi hân hạnh mang đến chương trình ưu đãi đặc biệt "${p.promotionName}", áp dụng cho các lượt đặt bàn trực tuyến và dùng bữa trực tiếp tại nhà hàng.`,
      bodyParagraphs: [
        `Chương trình ưu đãi chính thức có hiệu lực từ ngày ${formatDate(p.startAt)} đến hết ngày ${formatDate(p.endAt)}. Thực khách có thể dễ dàng sử dụng mã voucher "${p.promotionCode}" khi đặt bàn trực tuyến hoặc đọc mã cho nhân viên phục vụ khi thanh toán hóa đơn.`,
        p.minimumOrderValue > 0
          ? `Điều kiện áp dụng cực kỳ linh hoạt: Chỉ cần hóa đơn tiệc đạt từ ${formatCurrency(p.minimumOrderValue)}, quý khách sẽ được khấu trừ trực tiếp ${discountText} vào tổng hóa đơn ăn uống.`
          : `Đặc biệt, ưu đãi này được áp dụng cho mọi hóa đơn mà không giới hạn mức chi tiêu tối thiểu, giúp quý khách hoàn toàn thoải mái thưởng thức mọi tuyệt phẩm ẩm thực trong thực đơn.`,
        `Số lượng quà tặng có hạn nhằm đảm bảo chất lượng phục vụ tiêu chuẩn cao nhất. Quý khách vui lòng đặt bàn trước để giữ chỗ trong khung giờ vàng và nhận trọn vẹn các dịch vụ tiếp đón chu đáo từ đội ngũ Hương Sen.`
      ],
      promoCode: p.promotionCode,
      promoSummary: `${discountText} Toàn Bộ Hóa Đơn`,
      startAt: p.startAt,
      endAt: p.endAt,
      minimumOrderValue: p.minimumOrderValue
    };
  });
});

// =========================================================================
// TRANSFORM DATABASE FOODS INTO EDITORIAL ARTICLES
// =========================================================================
const foodArticles = computed(() => {
  let allowedIds = null;
  try {
    const saved = localStorage.getItem(PUBLISHED_FOODS_KEY);
    if (saved) allowedIds = JSON.parse(saved);
  } catch {
    allowedIds = null;
  }

  // Filter foods selected by admin or default to first 6 available foods with images if never configured
  let filtered = [];
  if (allowedIds !== null && Array.isArray(allowedIds)) {
    filtered = rawFoods.value.filter(f => allowedIds.includes(f.foodId));
  } else {
    // Default: first 6 available foods with image
    filtered = rawFoods.value.filter(f => f.foodStatus === 'available' && f.imageUrl).slice(0, 6);
  }

  return filtered.map(f => {
    const foodPrice = f.price || f.minPrice || 0;
    const catName = f.categoryName || 'Món Ăn';

    return {
      id: `food-${f.foodId}`,
      type: 'food',
      categoryName: `MÓN MỚI · ${catName.toUpperCase()}`,
      tagClass: 'badge-food-tag',
      title: `Khám Phá Tuyệt Phẩm "${f.foodName}": Bản Giao Hưởng Từ Nông Sản Tươi Sạch`,
      author: 'Bếp Trưởng Hương Sen Cuisine',
      publishedAt: f.createdAt || new Date().toISOString(),
      readTime: 4,
      coverImage: f.imageUrl || defaultFoodImage,
      excerpt: f.description || `Món ăn đại diện cho tinh hoa ẩm thực ${catName}, kết hợp nhuần nhuyễn giữa bí quyết chế biến gia truyền và kỹ thuật bài trí hiện đại.`,
      leadParagraph: `Tại Hương Sen, mỗi món ăn mới ra mắt đều là kết tinh của hàng tuần thử nghiệm công thức, nếm thử nguyên liệu và lắng nghe đóng góp từ những chuyên gia ẩm thực hàng đầu. "${f.foodName}" tự hào là sáng tạo ẩm thực mới nhất gia nhập vào đại tiệc của nhà hàng.`,
      bodyParagraphs: [
        f.description
          ? `Về hương vị: ${f.description}`
          : `Món ăn gây ấn tượng thị giác ngay từ cái nhìn đầu tiên với màu sắc hài hòa tự nhiên từ thảo mộc tươi, nước sốt sánh mịn đậm đà và độ mềm ngọt mọng nước của nguyên liệu chính.`,
        `Toàn bộ nguyên liệu dùng trong món "${f.foodName}" được thu hoạch mỗi sớm mai từ các nông trại hữu cơ đạt chuẩn VietGAP tại vùng cao nguyên, đảm bảo giữ trọn vẹn độ giòn ngọt và dinh dưỡng cao nhất khi lên đĩa.`,
        `Thực khách có thể thưởng thức món ăn này với mức giá khởi điểm từ ${formatCurrency(foodPrice)}. Kính mời quý khách đặt bàn ngay hôm nay để trở thành những thực khách đầu tiên trải nghiệm sáng tạo ẩm thực độc bản này.`
      ],
      foodName: f.foodName,
      foodPrice: foodPrice
    };
  });
});

// =========================================================================
// PERMANENT BRAND STORY ARTICLES (CÂU CHUYỆN BẾP NHÀ & VĂN HÓA)
// =========================================================================
const storyArticles = ref([
  {
    id: 'story-1',
    type: 'story',
    categoryName: 'CÂU CHUYỆN BẾP NHÀ',
    tagClass: 'badge-story-tag',
    title: 'Hành Trình Gìn Giữ Hương Vị Cung Đình: Từ Đài Sen Đến Bàn Tiệc Việt',
    author: 'Nghệ Nhân Ẩm Thực Hương Sen',
    publishedAt: '2026-10-20T08:00:00Z',
    readTime: 5,
    coverImage: 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1000&auto=format&fit=crop&q=80',
    excerpt: 'Tìm hiểu triết lý cân bằng âm dương và nghệ thuật chưng cất hương sen thuần khiết trong từng gia vị bí truyền của nhà hàng.',
    leadParagraph: 'Hoa sen từ ngàn đời nay đã là biểu tượng thanh cao của văn hóa Việt Nam. Nhưng trong ẩm thực cung đình, từng cánh hoa, nhụy sen và hạt sen lại chính là linh hồn tạo nên những bài thuốc bồi bổ và hương vị tao nhã không thể lẫn vào đâu.',
    bodyParagraphs: [
      'Để lưu giữ được mùi thơm thanh khiết của sen, bếp trưởng Hương Sen phải dậy từ 4 giờ sáng, đích thân chọn lựa những búp sen còn đọng sương sớm tại vùng đầm hồ ngoại ô. Từng cánh sen được ướp trà, từng hạt sen tươi được hấp chín mềm cùng thảo mộc quý.',
      'Triết lý ẩm thực tại Hương Sen xoay quanh sự hòa hợp của đất trời: Vị ngọt thanh của hải sản biển sâu, vị đậm đà của thịt non vùng núi và hương thơm thoang thoảng của thảo mộc đồng bằng. Tất cả tạo nên một hành trình vị giác đong đầy cảm xúc cho thực khách thưởng ngoạn.'
    ]
  },
  {
    id: 'story-2',
    type: 'story',
    categoryName: 'TIÊU CHUẨN NGUYÊN LIỆU',
    tagClass: 'badge-story-tag',
    title: 'Cam Kết 100% Nông Sản VietGAP: Minh Bạch Nguồn Gốc Trong Từng Bữa Ăn',
    author: 'Ban Kiểm Soát Chất Lượng',
    publishedAt: '2026-10-18T09:30:00Z',
    readTime: 3,
    coverImage: 'https://images.unsplash.com/photo-1498837167922-ddd27525d352?w=1000&auto=format&fit=crop&q=80',
    excerpt: 'Khám phá quy trình kiểm định 4 bước khắt khe từ nông trại đến bàn ăn, đảm bảo mỗi đĩa thức ăn phục vụ quý khách đều an lành và bổ dưỡng tuyệt đối.',
    leadParagraph: 'Sức khỏe và sự an tâm của thực khách là kim chỉ nam trong mọi hoạt động của Nhà hàng Hương Sen. Chúng tôi tự hào áp dụng hệ thống truy xuất nguồn gốc nông sản thông minh trên từng lô nguyên vật liệu nhập bếp.',
    bodyParagraphs: [
      'Mỗi buổi sáng, bộ phận kiểm nghiệm kho của nhà hàng tiến hành đối soát chứng từ xuất xứ, đo lường độ tươi và loại bỏ ngay lập tức những mẻ rau quả không đạt chuẩn độ mọng nước hay có dư lượng chất bảo quản.',
      'Sự tỉ mỉ này chính là lời cam kết vững chắc nhất mà Hương Sen muốn gửi gắm: Mỗi món ăn quý khách thưởng thức cùng người thân yêu đều là món quà sạch lành và trọn vẹn tinh túy thiên nhiên.'
    ]
  }
]);

// Combined All Articles
const allArticles = computed(() => {
  const list = [...promoArticles.value, ...foodArticles.value, ...storyArticles.value];
  list.sort((a, b) => new Date(b.publishedAt) - new Date(a.publishedAt));
  return list;
});

// Featured Hero Article (First article with big promo or first story)
const featuredArticle = computed(() => {
  if (allArticles.value.length === 0) return null;
  // Prioritize promo article if available
  const p = allArticles.value.find(a => a.type === 'promotion');
  return p || allArticles.value[0];
});

// Filtered Articles based on category chip and search query
const filteredArticles = computed(() => {
  let list = allArticles.value;

  if (selectedCategory.value !== 'all') {
    list = list.filter(a => a.type === selectedCategory.value);
  }

  const q = searchQuery.value.trim().toLowerCase();
  if (!q) return list;

  return list.filter(a => {
    return (
      a.title?.toLowerCase().includes(q) ||
      a.excerpt?.toLowerCase().includes(q) ||
      a.author?.toLowerCase().includes(q) ||
      a.categoryName?.toLowerCase().includes(q) ||
      (a.promoCode && a.promoCode.toLowerCase().includes(q))
    );
  });
});

// Catalog Articles for the 2-column grid
const catalogArticles = computed(() => {
  // When 'all' is selected and no search query, featuredArticle is shown as the top compact banner
  // Exclude it from the catalog grid below so there is no duplicate
  if (selectedCategory.value === 'all' && !searchQuery.value && featuredArticle.value) {
    return filteredArticles.value.filter(a => a.id !== featuredArticle.value.id);
  }
  return filteredArticles.value;
});

function openArticle(article) {
  selectedArticle.value = article;
  document.body.style.overflow = 'hidden';
}

function closeArticle() {
  selectedArticle.value = null;
  document.body.style.overflow = '';
}

function copyVoucher(code) {
  if (!code) return;
  navigator.clipboard.writeText(code).then(() => {
    copiedCode.value = code;
    setTimeout(() => {
      if (copiedCode.value === code) copiedCode.value = '';
    }, 2500);
  });
}

function resetFilters() {
  selectedCategory.value = 'all';
  searchQuery.value = '';
}

function formatCurrency(val) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val || 0);
}

function formatDate(isoStr) {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function onImageError(e) {
  e.target.src = defaultFoodImage;
}
</script>

<style scoped>
/* ========================================================================= */
/* EDITORIAL MAGAZINE THEME STYLES (Savory Table / Warm Ivory & Wine)       */
/* ========================================================================= */

.news-masthead {
  border-color: var(--dola-hairline) !important;
}

.text-wine {
  color: var(--dola-wine) !important;
}

.bg-wine {
  background-color: var(--dola-wine) !important;
}

.bg-wine-subtle {
  background-color: rgba(122, 28, 40, 0.08);
}

/* ========================================================================= */
/* FEATURED HERO ARTICLE CARD                                                */
/* ========================================================================= */

/* ========================================================================= */
/* EDITORIAL FEED CARDS (HÀNG DỌC BÀI VIẾT, HÌNH ẢNH MÓN VỪA VẶN, KHÔNG BỊ ZOOM) */
/* ========================================================================= */
.editorial-feed-card {
  border-color: var(--dola-hairline) !important;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 2px 8px rgba(44, 30, 20, 0.04);
}

.editorial-feed-card:hover {
  transform: translateY(-3px);
  border-color: var(--dola-gold) !important;
  box-shadow: 0 12px 28px rgba(122, 28, 40, 0.12) !important;
}

/* Khung hình ảnh món ăn: kích thước vừa vặn 260px x 175px (tỷ lệ 16:10) */
.feed-img-container {
  width: 260px;
  min-width: 260px;
  height: 175px;
  background: #FAF7F2;
  overflow: hidden;
}

@media (max-width: 767px) {
  .feed-img-container {
    width: 100% !important;
    min-width: 100% !important;
    height: 190px !important;
  }
}

.feed-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center center;
  transition: transform 0.5s ease;
}

.editorial-feed-card:hover .feed-img {
  transform: scale(1.04);
}

.feed-category-tag {
  background: rgba(26, 20, 16, 0.82);
  color: #FCE7B0;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 4px;
  backdrop-filter: blur(6px);
}

.feed-title {
  color: #1F1A17;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  transition: color 0.25s ease;
}

.editorial-feed-card:hover .feed-title {
  color: var(--dola-wine);
}

.feed-excerpt {
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* Dải Callout thu gọn vừa vặn */
.feed-callout-strip {
  border-radius: 8px;
  transition: background-color 0.2s ease;
}

.feed-callout-promo {
  background: #FAF7F2;
  border: 1px dashed var(--dola-wine);
}

.feed-callout-food {
  background: #FAF7F2;
  border: 1px dashed var(--dola-gold);
}

.feed-callout-story {
  background: #F4ECE1;
  border: 1px solid #E6DDD2;
}

.author-avatar-sm {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: rgba(184, 125, 50, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
}

.btn-read-story {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 16px;
  border-radius: 9999px;
  background: var(--dola-wine);
  color: #FFFFFF;
  font-size: 0.8rem;
  font-weight: 700;
  border: none;
  transition: all 0.25s ease;
  box-shadow: 0 3px 10px rgba(122, 28, 40, 0.2);
}

.editorial-feed-card:hover .btn-read-story {
  background: #942433;
  transform: translateX(3px);
}

.featured-story-card {

  background: #FFFFFF;
  border-color: var(--dola-hairline) !important;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
}

.featured-story-card:hover {
  transform: translateY(-4px);
  border-color: var(--dola-gold) !important;
  box-shadow: 0 20px 40px rgba(122, 28, 40, 0.15) !important;
}


.featured-img-wrap {
  height: 280px;
  max-height: 280px;
  min-height: 260px;
  overflow: hidden;
  background: #1F1A17;
}

@media (max-width: 767px) {
  .featured-img-wrap {
    height: 200px !important;
    max-height: 200px !important;
    min-height: 200px !important;
  }
}

/* ========================================================================= */
/* CATALOG ARTICLE CARD (GRID 2 CỘT GỌN GÀNG, HÌNH ẢNH VỪA VẶN)              */
/* ========================================================================= */
.catalog-article-card {
  border-color: var(--dola-hairline) !important;
  cursor: pointer;
  transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

.catalog-article-card:hover {
  transform: translateY(-4px);
  border-color: var(--dola-gold) !important;
  box-shadow: 0 14px 30px rgba(122, 28, 40, 0.12) !important;
}

.catalog-card-img-wrap {
  height: 180px;
  overflow: hidden;
  background: #1F1A17;
}

.catalog-card-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.5s ease;
}

.catalog-article-card:hover .catalog-card-img {
  transform: scale(1.05);
}

.catalog-category-tag {
  background: rgba(26, 20, 16, 0.8);
  color: #FCE7B0;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 4px;
  backdrop-filter: blur(6px);
}

.catalog-title {
  color: #1F1A17;
  line-height: 1.35;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  transition: color 0.25s ease;
}

.catalog-article-card:hover .catalog-title {
  color: var(--dola-wine);
}

.catalog-excerpt {
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.catalog-promo-box {
  background: #FAF7F2;
  border: 1px dashed var(--dola-wine);
}

.catalog-food-box {
  background: #FAF7F2;
  border: 1px dashed var(--dola-gold);
}

.catalog-story-box {
  background: #F4ECE1;
  border: 1px solid #E6DDD2;
}

.btn-read-compact {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 5px 14px;
  border-radius: 9999px;
  background: rgba(122, 28, 40, 0.08);
  color: var(--dola-wine);
  font-size: 0.78rem;
  font-weight: 700;
  border: none;
  transition: all 0.2s ease;
}

.catalog-article-card:hover .btn-read-compact {
  background: var(--dola-wine);
  color: #FFFFFF;
  transform: translateX(2px);
}

.featured-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.6s ease;
}

.featured-story-card:hover .featured-img {
  transform: scale(1.05);
}

.featured-badge-pill {
  background: rgba(122, 28, 40, 0.9);
  color: #FFFFFF;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  padding: 5px 12px;
  border-radius: 9999px;
  backdrop-filter: blur(8px);
}

.category-chip {
  background: rgba(26, 20, 16, 0.8);
  color: #FCE7B0;
  font-size: 0.74rem;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
  backdrop-filter: blur(8px);
}

.featured-title {
  color: #1F1A17;
  line-height: 1.3;
  transition: color 0.25s ease;
}

.featured-story-card:hover .featured-title {
  color: var(--dola-wine);
}

.featured-excerpt {
  color: #574A40;
  line-height: 1.65;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

/* Callout Boxes */
.promo-callout-box {
  background: #FAF7F2;
  border: 1.5px dashed var(--dola-wine);
}

.food-callout-box {
  background: #FAF7F2;
  border: 1.5px dashed var(--dola-gold);
}

.story-callout-box {
  background: #F4ECE1;
  border: 1.5px solid #E6DDD2;
}

.author-avatar-sm {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: rgba(184, 125, 50, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
}

.btn-read-story {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 18px;
  border-radius: 9999px;
  background: var(--dola-wine);
  color: #FFFFFF;
  font-size: 0.82rem;
  font-weight: 700;
  border: none;
  transition: all 0.25s ease;
  box-shadow: 0 4px 14px rgba(122, 28, 40, 0.25);
}

.featured-story-card:hover .btn-read-story,
.magazine-article-horizontal:hover .btn-read-story {
  background: #942433;
  transform: translateX(4px);
}

@media (max-width: 991px) {
  .featured-img-wrap {
    min-height: 240px !important;
    height: 240px !important;
  }
}

/* ========================================================================= */
/* FILTER CHIPS & SEARCH BAR STYLES                                          */
/* ========================================================================= */
.dola-chips-scroll {
  white-space: nowrap;
  scrollbar-width: thin;
}

.dola-filter-chip {
  background: #FFFFFF;
  color: #574A40;
  border: 1px solid var(--dola-hairline);
  padding: 9px 20px;
  border-radius: 9999px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 2px 6px rgba(44, 30, 20, 0.04);
}

.dola-filter-chip:hover {
  background: var(--dola-surface-alt);
  color: var(--dola-wine);
  border-color: var(--dola-gold);
  transform: translateY(-2px);
}

.dola-filter-chip-active {
  background: var(--dola-wine) !important;
  color: #FFFFFF !important;
  border-color: var(--dola-wine) !important;
  font-weight: 700;
  box-shadow: 0 6px 18px rgba(122, 28, 40, 0.35);
  transform: translateY(-2px);
}

/* ========================================================================= */
/* ARTICLE READER MODAL (FULL MAGAZINE ARTICLE VIEW)                         */
/* ========================================================================= */
.article-reader-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(20, 14, 11, 0.75);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  z-index: 1060;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 20px;
  overflow-y: auto;
}

.article-reader-container {
  background: #FFFFFF;
  border: 1.5px solid var(--dola-hairline-dark);
  border-radius: 20px;
  width: 100%;
  max-width: 860px;
  max-height: 90vh;
  overflow-y: auto;
  position: relative;
  box-shadow: 0 25px 60px rgba(0, 0, 0, 0.45);
}

.article-reader-close-btn {
  position: sticky;
  top: 16px;
  right: 16px;
  float: right;
  z-index: 10;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  border: 1px solid var(--dola-hairline);
  background: #FFFFFF;
  color: #1F1A17;
  font-size: 1.1rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.article-reader-close-btn:hover {
  background: var(--dola-wine);
  color: #FFFFFF;
  border-color: var(--dola-wine);
}

.article-reader-content {
  padding: 36px 44px;
}

@media (max-width: 576px) {
  .article-reader-content {
    padding: 24px 18px;
  }
}

.article-reader-title {
  font-size: clamp(1.6rem, 1rem + 2vw, 2.3rem);
  color: #1F1A17;
  line-height: 1.25;
}

.article-reader-hero-img-wrap {
  width: 100%;
  height: 360px;
}

@media (max-width: 576px) {
  .article-reader-hero-img-wrap {
    height: 220px;
  }
}

.article-reader-lead {
  background: #FAF7F2;
  border-left: 4px solid var(--dola-wine);
  font-size: 1.05rem;
  line-height: 1.7;
  font-weight: 500;
  color: #1F1A17;
}

.article-reader-body {
  font-size: 1.02rem;
  line-height: 1.8;
  color: #3C2D23;
}

/* Article Voucher Showcase Box */
.article-voucher-showcase {
  background: linear-gradient(135deg, #7A1C28 0%, #5E141D 100%);
  color: #FFFFFF;
  box-shadow: 0 10px 24px rgba(122, 28, 40, 0.25);
}

.voucher-code-badge-lg {
  font-size: 1.45rem;
  font-weight: 900;
  color: #FCE7B0;
  padding: 8px 24px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.12);
  border: 1.5px dashed rgba(252, 231, 176, 0.6);
  letter-spacing: 0.08em;
}

.btn-copy-code-lg {
  padding: 10px 24px;
  border-radius: 10px;
  font-size: 0.92rem;
  font-weight: 800;
  background: #FCE7B0;
  color: var(--dola-wine);
  border: none;
  cursor: pointer;
  transition: all 0.25s ease;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.2);
}

.btn-copy-code-lg:hover {
  background: #FFFFFF;
  transform: translateY(-2px);
}

.btn-copy-code-lg.copied {
  background: #10B981;
  color: #FFFFFF;
}

/* Food Showcase */
.article-food-showcase {
  background: #FAF7F2;
  border: 1.5px solid var(--dola-hairline-dark);
}

/* Chef Quote */
.article-chef-quote {
  background: #F4ECE1;
  border-left: 4px solid var(--dola-gold);
}

/* Modal Animation */
@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}

.animate-fade-in-up {
  animation: fadeInUp 0.3s ease;
}
</style>

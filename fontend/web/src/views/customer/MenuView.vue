<template>
  <div class="dola-theme py-5">
    <div class="container py-3">

      <!-- Header -->
      <div class="text-center mb-5">
        <span class="dola-pill-badge mb-2">📖 Mỹ Vị Hương Sen</span>
        <h1 class="dola-font-title text-gold display-4 fw-bold">Thực Đơn Tinh Hoa</h1>
        <p class="text-body-dola mx-auto fs-5" style="max-width: 600px;">
          Tất cả món ăn được chế biến từ nông sản tươi sạch chọn lọc trong ngày, nêm nếm theo công thức độc bản lưu giữ trọn vẹn hương vị truyền thống.
        </p>
      </div>

      <!-- Search & Count Bar -->
      <div class="d-flex flex-column flex-md-row gap-3 align-items-md-center justify-content-between mb-4">
        <div class="position-relative" style="max-width: 420px; width: 100%;">
          <span class="position-absolute top-50 start-0 translate-middle-y ps-3 text-gold">🔍</span>
          <input
            type="text"
            class="dola-input ps-5"
            placeholder="Tìm món ngon, nguyên liệu, đồ uống..."
            v-model="foodSearch"
          />
        </div>
        <div class="small text-body-dola">
          Đang hiển thị <strong class="text-gold">{{ totalVisible }}</strong> / {{ totalFoodCount }} món ăn
        </div>
      </div>

      <!-- Category Filter Chips -->
      <div class="d-flex gap-2 overflow-auto pb-3 mb-4 dola-chips-scroll">
        <button
          class="dola-filter-chip"
          :class="{ 'dola-filter-chip-active': selectedCategory === 0 }"
          @click="selectedCategory = 0"
        >
          🍽️ Tất Cả Món ({{ totalFoodCount }})
        </button>
        <button
          v-for="cat in categories"
          :key="cat.categoryId"
          class="dola-filter-chip"
          :class="{ 'dola-filter-chip-active': selectedCategory === cat.categoryId }"
          @click="selectedCategory = cat.categoryId"
        >
          {{ cat.categoryName }} ({{ cat.foods?.length || 0 }})
        </button>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="text-center py-5">
        <div class="spinner-border text-gold mb-3" style="width: 3rem; height: 3rem;"></div>
        <p class="text-body-dola">Đang chuẩn bị thực đơn tươi mới...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredCategories.length === 0" class="dola-card p-5 text-center my-4">
        <div class="fs-1 mb-3">🔍</div>
        <h4 class="text-gold dola-font-title fs-2">Không Tìm Thấy Món Phù Hợp</h4>
        <p class="text-body-dola small mb-3">Vui lòng thử tìm kiếm với từ khóa khác hoặc chọn danh mục khác.</p>
        <button class="dola-btn-outline" @click="resetFilters">
          Xem Toàn Bộ Thực Đơn
        </button>
      </div>

      <!-- Categories & Dishes Grid -->
      <div v-else class="d-flex flex-column gap-5">
        <div v-for="cat in filteredCategories" :key="cat.categoryId" class="category-block">

          <!-- Category Title -->
          <div class="d-flex align-items-center gap-3 mb-4 pb-2 border-bottom" style="border-color: var(--dola-hairline) !important;">
            <h2 class="dola-font-title fs-1 mb-0" style="color: var(--dola-wine);">{{ cat.categoryName }}</h2>
            <span class="badge rounded-pill px-3 py-1" style="background: rgba(184, 125, 50, 0.12); color: var(--dola-primary); border: 1px solid var(--dola-hairline); font-size: 0.85rem; font-weight: 700;">
              {{ cat.foods.length }} món
            </span>
            <div class="small ms-auto d-none d-md-block" style="color: #6E6157;">{{ cat.description }}</div>
          </div>

          <!-- Food Grid -->
          <div class="row g-4">
            <div class="col-12 col-md-6 col-lg-4" v-for="(food, idx) in cat.foods" :key="food.foodId">
              <div class="dola-card h-100 d-flex flex-column overflow-hidden food-menu-card">

                <!-- Image / Icon Header -->
                <div class="position-relative text-center p-0 d-flex align-items-center justify-content-center overflow-hidden"
                  style="background: var(--dola-surface-alt); height: 200px;">
                  <template v-if="food.imageUrl">
                    <img
                      :src="food.imageUrl"
                      :alt="food.foodName"
                      class="position-absolute top-0 start-0 w-100 h-100 object-fit-cover food-card-img"
                      @error="food.imageUrl = null"
                    />
                    <div class="position-absolute top-0 start-0 w-100 h-100" style="background: linear-gradient(to top, rgba(31,26,23,0.35) 0%, transparent 60%); pointer-events: none;"></div>
                  </template>
                  <span v-else style="font-size: 4rem; filter: drop-shadow(0 4px 10px rgba(0,0,0,0.08));">{{ getCategoryIcon(cat.categoryName) }}</span>

                  <span class="badge position-absolute top-0 end-0 m-3 fw-bold rounded-pill shadow-sm"
                    :class="food.canOrder !== false ? 'bg-success' : 'bg-secondary'">
                    {{ food.availabilityText || (food.canOrder !== false ? 'Đang phục vụ' : 'Hết món') }}
                  </span>
                </div>

                <!-- Food Info -->
                <div class="p-4 d-flex flex-column flex-grow-1">
                  <div class="d-flex justify-content-between align-items-start mb-2">
                    <h4 class="fw-bold fs-5 mb-0" style="color: #1F1A17;">{{ food.foodName }}</h4>
                  </div>
                  <p class="small flex-grow-1 mb-3" style="color: #574A40; line-height: 1.6;">
                    {{ food.description || 'Hương vị thơm ngon tinh túy, được chế biến nóng sốt theo yêu cầu của quý khách.' }}
                  </p>

                  <!-- Variants Pills -->
                  <div v-if="food.variants && food.variants.length > 0" class="d-flex flex-wrap gap-2 mb-3">
                    <span
                      v-for="v in food.variants"
                      :key="v.variantId"
                      class="dola-variant-badge"
                    >
                      <span style="color: #574A40;">{{ v.variantName }}:</span>
                      <strong style="color: var(--dola-wine); font-weight: 700;">{{ formatCurrency(v.price) }}</strong>
                      <span v-if="v.canOrder === false" class="ms-1 text-warning">{{ v.availabilityText || 'Hết món' }}</span>
                    </span>
                  </div>

                  <!-- Footer Price & Status -->
                  <div class="mt-auto pt-3 border-top d-flex justify-content-between align-items-center" style="border-color: var(--dola-hairline) !important;">
                    <div>
                      <small class="d-block" style="font-size: 0.72rem; color: #8C7F75;">Đơn giá từ</small>
                      <span class="fw-bold fs-5" style="color: var(--dola-wine);">{{ formatCurrency(food.price || food.minPrice) }}</span>
                    </div>
                    <span class="badge rounded-pill px-2.5 py-1" style="background: rgba(16, 185, 129, 0.12); color: #059669; border: 1px solid rgba(16, 185, 129, 0.3); font-size: 0.75rem; font-weight: 600;">
                      ✓ Tươi mới mỗi ngày
                    </span>
                  </div>

                </div>
              </div>
            </div>
          </div>

        </div>

        <!-- Banner Đặt Bàn Cuối Trang Thực Đơn Hợp Lý -->
        <div class="dola-card p-4 p-md-5 mt-5 text-center">
          <span class="dola-pill-badge mb-3">📅 TIỆN ÍCH TIẾP ĐÓN</span>
          <h3 class="dola-font-title text-gold fs-1 mb-2">Quý Khách Đã Chọn Được Món Ăn Ưng Ý?</h3>
          <p class="text-body-dola mx-auto mb-4" style="max-width: 580px;">
            Đặt bàn trước để Nhà hàng Hương Sen chuẩn bị sẵn sàng không gian tiệc sang trọng và nguyên liệu tươi ngon nhất.
          </p>
          <div class="d-flex justify-content-center gap-3 flex-wrap">
            <router-link to="/customer/reservation" class="dola-btn-primary px-4">
              <span>📅</span>
              <span>Đặt Bàn Ngay Bây Giờ</span>
            </router-link>
            <router-link to="/customer/contact" class="dola-btn-outline px-4">
              <span>📞</span>
              <span>Hotline 1900 6868</span>
            </router-link>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import axios from 'axios';

const API_BASE = 'http://localhost:5079/api';

const categories = ref([]);
const loading = ref(true);
const foodSearch = ref('');
const selectedCategory = ref(0);

async function loadMenu() {
  loading.value = true;
  try {
    const res = await axios.get(`${API_BASE}/customer/menu`);
    if (res.data?.success) {
      categories.value = res.data.data || [];
    }
  } catch (err) {
    console.error('Lỗi tải thực đơn:', err);
  } finally {
    loading.value = false;
  }
}

const totalFoodCount = computed(() => {
  return categories.value.reduce((sum, c) => sum + (c.foods?.length || 0), 0);
});

const filteredCategories = computed(() => {
  const q = foodSearch.value.trim().toLowerCase();

  return categories.value
    .map(cat => {
      // Filter by category selection
      if (selectedCategory.value !== 0 && cat.categoryId !== selectedCategory.value) {
        return null;
      }

      // Filter foods by search query
      const matchedFoods = (cat.foods || []).filter(food => {
        if (!q) return true;
        const nameMatch = food.foodName?.toLowerCase().includes(q);
        const descMatch = food.description?.toLowerCase().includes(q);
        const variantMatch = food.variants?.some(v => v.variantName?.toLowerCase().includes(q));
        return nameMatch || descMatch || variantMatch;
      });

      if (matchedFoods.length === 0) return null;

      return {
        ...cat,
        foods: matchedFoods
      };
    })
    .filter(Boolean);
});

const totalVisible = computed(() => {
  return filteredCategories.value.reduce((sum, c) => sum + c.foods.length, 0);
});

function resetFilters() {
  foodSearch.value = '';
  selectedCategory.value = 0;
}

function getCategoryIcon(name) {
  if (!name) return '🍲';
  if (name.includes('Uống') || name.includes('Pha Chế')) return '🍹';
  if (name.includes('Khai Vị') || name.includes('Gỏi')) return '🥗';
  if (name.includes('Nướng') || name.includes('BBQ')) return '🥩';
  if (name.includes('Lẩu') || name.includes('Cơm')) return '🍲';
  if (name.includes('Hải Sản')) return '🦐';
  return '🍽️';
}

function formatCurrency(val) {
  if (!val && val !== 0) return '0 ₫';
  return Number(val).toLocaleString('vi-VN') + ' ₫';
}

onMounted(() => {
  loadMenu();
});
</script>

<style scoped>
.text-body-dola {
  color: var(--dola-body);
}

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
  box-shadow: 0 2px 6px rgba(44, 30, 20, 0.03);
}

.dola-filter-chip:hover {
  background: var(--dola-surface-alt);
  color: var(--dola-wine);
  border-color: var(--dola-primary);
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

.dola-variant-badge {
  background: var(--dola-surface-alt);
  border: 1px solid var(--dola-hairline);
  border-radius: 8px;
  padding: 4px 10px;
  font-size: 12px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

/* Card hover animation */
.food-menu-card {
  transition: transform 0.35s cubic-bezier(0.16, 1, 0.3, 1), box-shadow 0.35s ease, border-color 0.35s ease;
}

.food-menu-card:hover {
  transform: translateY(-6px);
  box-shadow: 0 16px 36px rgba(60, 45, 35, 0.12);
  border-color: rgba(184, 125, 50, 0.5);
}

.food-card-img {
  transition: transform 0.5s cubic-bezier(0.16, 1, 0.3, 1);
}

.food-menu-card:hover .food-card-img {
  transform: scale(1.08);
}
</style>

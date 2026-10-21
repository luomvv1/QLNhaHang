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
          <div class="d-flex align-items-center gap-3 mb-4 pb-2 border-bottom border-dola-gold">
            <h2 class="dola-font-title text-gold fs-1 mb-0">{{ cat.categoryName }}</h2>
            <span class="badge rounded-pill" style="background: rgba(214, 156, 82, 0.15); color: var(--dola-primary); border: 1px solid var(--dola-hairline);">
              {{ cat.foods.length }} món
            </span>
            <div class="small text-body-dola ms-auto d-none d-md-block">{{ cat.description }}</div>
          </div>

          <!-- Food Grid -->
          <div class="row g-4">
            <div class="col-12 col-md-6 col-lg-4" v-for="food in cat.foods" :key="food.foodId">
              <div class="dola-card h-100 d-flex flex-column overflow-hidden">

                <!-- Image / Icon Header -->
                <div class="position-relative text-center p-4 d-flex align-items-center justify-content-center"
                  style="background: linear-gradient(135deg, rgba(0,0,0,0.4), rgba(16,48,44,0.6)); min-height: 160px;">
                  <template v-if="food.imageUrl">
                    <img
                      :src="food.imageUrl"
                      :alt="food.foodName"
                      class="position-absolute top-0 start-0 w-100 h-100 object-fit-cover transition-transform"
                      style="transition: transform 0.4s ease;"
                      @error="food.imageUrl = null"
                    />
                    <div class="position-absolute top-0 start-0 w-100 h-100" style="background: linear-gradient(to top, rgba(0,0,0,0.6) 0%, transparent 60%); pointer-events: none;"></div>
                  </template>
                  <span v-else style="font-size: 4rem;">{{ getCategoryIcon(cat.categoryName) }}</span>

                  <span class="badge position-absolute top-0 end-0 m-3 fw-bold rounded-pill"
                    :class="food.foodStatus === 'available' ? 'bg-success' : 'bg-secondary'">
                    {{ food.foodStatus === 'available' ? 'Đang phục vụ' : 'Tạm hết' }}
                  </span>
                </div>

                <!-- Food Info -->
                <div class="p-4 d-flex flex-column flex-grow-1">
                  <div class="d-flex justify-content-between align-items-start mb-2">
                    <h4 class="text-white fw-bold fs-5 mb-0">{{ food.foodName }}</h4>
                  </div>
                  <p class="text-body-dola small flex-grow-1 mb-3" style="line-height: 1.5;">
                    {{ food.description || 'Hương vị thơm ngon tinh túy, được chế biến nóng sốt theo yêu cầu của quý khách.' }}
                  </p>

                  <!-- Variants Pills -->
                  <div v-if="food.variants && food.variants.length > 0" class="d-flex flex-wrap gap-2 mb-3">
                    <span
                      v-for="v in food.variants"
                      :key="v.variantId"
                      class="dola-variant-badge"
                    >
                      {{ v.variantName }}: <strong class="text-gold">{{ formatCurrency(v.price) }}</strong>
                    </span>
                  </div>

                  <!-- Footer Price & Status -->
                  <div class="mt-auto pt-3 border-top border-dola-gold d-flex justify-content-between align-items-center">
                    <div>
                      <small class="text-white-50 d-block" style="font-size: 0.72rem;">Đơn giá từ</small>
                      <span class="text-gold fw-bold fs-5">{{ formatCurrency(food.price || food.minPrice) }}</span>
                    </div>
                    <span class="badge rounded-pill px-2 py-1" style="background: rgba(16, 185, 129, 0.12); color: #34d399; border: 1px solid rgba(16, 185, 129, 0.3); font-size: 0.72rem;">
                      ✓ Sẵn sàng phục vụ
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
}

.dola-filter-chip {
  background: var(--dola-surface-alt);
  color: var(--dola-body);
  border: 1px solid var(--dola-hairline);
  padding: 8px 18px;
  border-radius: 9999px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.dola-filter-chip:hover {
  background: rgba(214, 156, 82, 0.2);
  color: #FFFFFF;
}

.dola-filter-chip-active {
  background: var(--dola-primary) !important;
  color: #1b1814 !important;
  font-weight: 700;
  box-shadow: 0 4px 12px rgba(214, 156, 82, 0.35);
}

.dola-variant-badge {
  background: rgba(0, 0, 0, 0.3);
  border: 1px solid var(--dola-hairline);
  border-radius: 6px;
  padding: 3px 8px;
  font-size: 12px;
  color: var(--dola-body);
}
</style>

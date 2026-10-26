<template>
  <div class="daily-portions-container">
    <!-- Header Page Banner -->
    <div class="card-modern p-4 mb-4 text-white position-relative overflow-hidden"
         style="background: linear-gradient(135deg, #1e3a8a 0%, #2563eb 50%, #3b82f6 100%);">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 position-relative" style="z-index: 2;">
        <div>
          <div class="d-flex align-items-center gap-2 mb-2">
            <span class="badge bg-warning text-dark px-3 py-1 fw-bold">WMS & BOM</span>
            <span class="badge bg-primary-subtle text-white border border-light border-opacity-25 px-2 py-1">PHẦN 2: KẾ HOẠCH & ĐIỀU PHỐI SUẤT ĂN</span>
            <span v-if="hasRequisition" class="badge bg-success text-white px-2 py-1">
              ✓ Đã lập phiếu xuất bếp
            </span>
            <span v-else class="badge bg-warning text-dark px-2 py-1">
              ⏳ Chưa lập phiếu xuất bếp
            </span>
          </div>
          <h2 class="fw-bold mb-1">🍲 Kế Hoạch & Điều Phối Suất Ăn Trong Ngày</h2>
          <p class="text-white-50 mb-0">
            Quản lý số suất dự kiến, kéo tồn bán thành phẩm hôm qua, tính toán lượng cần nấu và tự động nhân BOM lập phiếu xuất kho bếp.
          </p>
          <div class="d-flex gap-2 mt-2">
            <router-link to="/admin/inventory" class="btn btn-sm btn-outline-light border-light border-opacity-50 text-white d-flex align-items-center gap-1 shadow-sm">
              <span>📦</span>
              <span>Kho & Định Mức (Phần 1)</span>
            </router-link>
            <router-link to="/admin/stock-receipts" class="btn btn-sm btn-outline-light border-light border-opacity-50 text-white d-flex align-items-center gap-1 shadow-sm">
              <span>🧾</span>
              <span>Phiếu Kho (Phần 3)</span>
            </router-link>
          </div>
        </div>

        <!-- Date Picker & Quick Actions -->
        <div class="d-flex flex-wrap align-items-center gap-2">
          <!-- Quick Date Selector -->
          <div class="bg-white bg-opacity-10 p-1 rounded-3 d-flex align-items-center gap-1 border border-light border-opacity-25">
            <button class="btn btn-sm btn-outline-light border-0 py-1 px-2" @click="changeDateOffset(-1)">◀ Hôm qua</button>
            <input 
              v-model="selectedDate" 
              type="date" 
              class="form-control form-control-sm text-center fw-bold bg-white text-dark border-0 shadow-sm"
              style="width: 145px;"
              @change="fetchDailyPortions"
            />
            <button class="btn btn-sm btn-outline-light border-0 py-1 px-2" @click="changeDateOffset(1)">Ngày mai ▶</button>
            <button class="btn btn-sm btn-light fw-bold py-1 px-2 ms-1 text-primary shadow-sm" @click="setToday">Hôm nay</button>
          </div>

          <!-- Action Buttons -->
          <button 
            class="btn btn-outline-light fw-bold d-flex align-items-center gap-2 shadow-sm"
            :disabled="isPullingCarryOver"
            @click="pullYesterdayCarryOver"
            title="Kéo các món còn dư từ tủ mát ngày hôm qua chuyển sang hôm nay"
          >
            <span>🔄</span>
            <span>{{ isPullingCarryOver ? 'Đang kéo...' : 'Kéo Tồn Hôm Qua' }}</span>
          </button>

          <button 
            class="btn btn-warning text-dark fw-bold d-flex align-items-center gap-2 shadow-sm"
            :disabled="isGeneratingRequisition || hasRequisition || summary.totalNeedToCook <= 0"
            @click="generateRequisition"
            title="Tự động nhân định mức BOM với số lượng cần nấu để tạo Phiếu xuất kho bếp"
          >
            <span>⚡</span>
            <span>{{ hasRequisition ? 'Đã lập phiếu xuất bếp' : (isGeneratingRequisition ? 'Đang lập phiếu...' : 'Lập Phiếu Xuất Bếp') }}</span>
          </button>

          <button 
            class="btn btn-light text-primary fw-bold d-flex align-items-center gap-2 shadow-sm"
            @click="openPlanModal"
          >
            <span>➕</span>
            <span>Lập Kế Hoạch Món</span>
          </button>
        </div>
      </div>
    </div>

    <!-- 4 KPI Summary Cards -->
    <div class="row g-3 mb-4">
      <!-- 1. Kế Hoạch Suất -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Kế Hoạch Bán (A)</span>
              <h3 class="fw-bold text-primary mt-1 mb-0">{{ summary.totalPlanned }} <small class="fs-6 text-muted">suất</small></h3>
            </div>
            <div class="stat-badge bg-primary-subtle text-primary">📋</div>
          </div>
          <div class="small text-muted pt-2 border-top d-flex justify-content-between">
            <span>Thực đơn trong ngày</span>
            <span class="fw-bold text-dark">{{ summary.totalDishes }} món / biến thể</span>
          </div>
        </div>
      </div>

      <!-- 2. Tồn Bán Thành Phẩm Hôm Qua -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Tồn Tủ Mát Sang (B)</span>
              <h3 class="fw-bold text-info mt-1 mb-0">{{ summary.totalCarryOver }} <small class="fs-6 text-muted">suất</small></h3>
            </div>
            <div class="stat-badge bg-info-subtle text-info">❄️</div>
          </div>
          <div class="small text-muted pt-2 border-top d-flex justify-content-between">
            <span>Tiết kiệm sơ chế</span>
            <span class="text-info fw-bold">Từ hôm trước</span>
          </div>
        </div>
      </div>

      <!-- 3. Cần Sơ Chế / Nấu Mới -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100" :class="{ 'border-warning border': summary.totalNeedToCook > 0 }">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Cần Nấu Mới (A - B)</span>
              <h3 class="fw-bold text-warning-emphasis mt-1 mb-0">{{ summary.totalNeedToCook }} <small class="fs-6 text-muted">suất</small></h3>
            </div>
            <div class="stat-badge bg-warning-subtle text-warning-emphasis">🔥</div>
          </div>
          <div class="small text-muted pt-2 border-top d-flex justify-content-between">
            <span>Tổng nhu cầu bếp</span>
            <span class="fw-bold text-warning-emphasis">Cần xuất kho BOM</span>
          </div>
        </div>
      </div>

      <!-- 4. Tiến Độ Nấu / Bán / Tồn Bếp -->
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Tồn Bếp Sẵn Sàng</span>
              <h3 class="fw-bold text-success mt-1 mb-0">{{ summary.totalSystemRemain }} <small class="fs-6 text-muted">suất</small></h3>
            </div>
            <div class="stat-badge bg-success-subtle text-success">🍽️</div>
          </div>
          <div class="small pt-2 border-top d-flex justify-content-between align-items-center">
            <span class="text-muted">Đã nấu: <strong>{{ summary.totalCooked }}</strong></span>
            <span class="text-primary">Đã bán: <strong>{{ summary.totalSold }}</strong></span>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Content Card -->
    <div class="card-modern overflow-hidden mb-4">
      <!-- Filter & Search Toolbar -->
      <div class="p-3 border-bottom bg-light d-flex flex-wrap align-items-center justify-content-between gap-3">
        <div class="d-flex flex-wrap align-items-center gap-2 flex-grow-1" style="max-width: 800px;">
          <!-- Search input -->
          <div class="position-relative flex-grow-1" style="max-width: 320px;">
            <input 
              v-model="searchQuery" 
              type="text" 
              class="form-control form-control-sm ps-4" 
              placeholder="🔍 Tìm theo tên món ăn, biến thể..." 
            />
          </div>

          <!-- Category filter -->
          <select v-model="selectedCategoryFilter" class="form-select form-select-sm" style="max-width: 200px;">
            <option value="">Tất cả danh mục</option>
            <option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">
              {{ cat.categoryName }}
            </option>
          </select>

          <!-- Status filter -->
          <select v-model="selectedStatusFilter" class="form-select form-select-sm" style="max-width: 170px;">
            <option value="all">Tất cả trạng thái</option>
            <option value="planned">Mới lập (Planned)</option>
            <option value="in_service">Đang phục vụ (In Service)</option>
            <option value="reconciled">Đã kiểm kê (Reconciled)</option>
            <option value="closed">Đã đóng (Closed)</option>
          </select>
        </div>

        <div class="d-flex align-items-center gap-2">
          <button class="btn btn-sm btn-outline-secondary d-flex align-items-center gap-1" @click="fetchDailyPortions">
            <span>🔄</span>
            <span>Làm mới</span>
          </button>
          <button class="btn btn-sm btn-primary fw-bold" @click="openPlanModal">
            ➕ Thêm Món Vào Ngày
          </button>
        </div>
      </div>

      <!-- Portion Control Table -->
      <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-light text-muted small text-uppercase">
            <tr>
              <th style="width: 50px;" class="ps-3">STT</th>
              <th>Món Ăn & Biến Thể</th>
              <th>Danh Mục</th>
              <th class="text-end" style="width: 110px;" title="Kế hoạch bán dự kiến trong ngày">Kế Hoạch (A)</th>
              <th class="text-end" style="width: 110px;" title="Tồn bán thành phẩm hôm qua chuyển sang">Tồn Hôm Qua (B)</th>
              <th class="text-end" style="width: 110px;" title="Cần nấu mới = Kế hoạch - Tồn hôm qua">Cần Nấu (A - B)</th>
              <th class="text-center" style="width: 220px;" title="Bếp đã nấu thực tế">Bếp Đã Nấu (C)</th>
              <th class="text-end" style="width: 100px;" title="Số suất đã phục vụ khách hàng">Đã Bán (D)</th>
              <th class="text-end" style="width: 110px;" title="Còn lại trong bếp = Đã nấu - Đã bán">Tồn Bếp (C - D)</th>
              <th class="text-center" style="width: 120px;">Trạng Thái</th>
              <th class="text-center pe-3" style="width: 110px;">Thao Tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading" class="text-center py-5">
              <td colspan="11" class="py-5 text-muted">
                <div class="spinner-border spinner-border-sm text-primary me-2"></div>
                Đang tải bảng điều phối suất ăn...
              </td>
            </tr>

            <tr v-else-if="filteredPortions.length === 0">
              <td colspan="11" class="text-center text-muted py-5">
                <div class="fs-2 mb-2">🍲</div>
                <div class="fw-bold mb-1">Chưa có kế hoạch suất ăn cho ngày {{ formatDateVN(selectedDate) }}</div>
                <p class="small text-muted mb-3">Bạn có thể kéo tồn hôm qua hoặc bấm "+ Lập Kế Hoạch Món" để bắt đầu ngày mới.</p>
                <div class="d-flex justify-content-center gap-2">
                  <button class="btn btn-sm btn-outline-primary" @click="pullYesterdayCarryOver">🔄 Kéo Tồn Hôm Qua</button>
                  <button class="btn btn-sm btn-primary fw-bold" @click="openPlanModal">➕ Lập Kế Hoạch Món Mới</button>
                </div>
              </td>
            </tr>

            <tr v-for="(p, idx) in filteredPortions" :key="p.portionControlId">
              <td class="ps-3 text-muted small fw-semibold">{{ idx + 1 }}</td>
              
              <!-- Dish & Variant -->
              <td>
                <div class="fw-bold text-dark">{{ p.foodName }}</div>
                <div class="small d-flex align-items-center gap-1 mt-1">
                  <span class="badge bg-light text-secondary border">Size: {{ p.variantName }}</span>
                  <span class="text-muted">{{ formatCurrency(p.price) }} / {{ p.unit }}</span>
                </div>
              </td>

              <!-- Category -->
              <td>
                <span class="badge bg-secondary-subtle text-secondary">{{ p.categoryName }}</span>
              </td>

              <!-- Planned (A) -->
              <td class="text-end fw-bold text-primary">
                {{ Number(p.plannedPortions).toLocaleString() }} <small class="text-muted">{{ p.unit }}</small>
              </td>

              <!-- CarryOver (B) -->
              <td class="text-end fw-semibold text-info">
                {{ Number(p.carryOverPortions).toLocaleString() }} <small class="text-muted">{{ p.unit }}</small>
              </td>

              <!-- Need to cook (A - B) -->
              <td class="text-end fw-bold text-warning-emphasis">
                {{ Number(p.needToCookPortions).toLocaleString() }} <small class="text-muted">{{ p.unit }}</small>
              </td>

              <!-- Cooked (C) with Quick Update -->
              <td>
                <div class="d-flex align-items-center justify-content-center gap-1 mb-1">
                  <input 
                    type="number" 
                    class="form-control form-control-sm text-center fw-bold" 
                    style="width: 75px;" 
                    min="0"
                    step="any"
                    v-model.number="p.cookedPortions" 
                    @change="updateCooked(p)"
                  />
                  <button class="btn btn-sm btn-outline-secondary py-0 px-1" title="+5 suất" @click="quickAddCooked(p, 5)">+5</button>
                  <button class="btn btn-sm btn-outline-secondary py-0 px-1" title="+10 suất" @click="quickAddCooked(p, 10)">+10</button>
                </div>
                <!-- Progress bar against Need to cook -->
                <div class="progress" style="height: 4px;" v-if="p.needToCookPortions > 0">
                  <div 
                    class="progress-bar" 
                    :class="getCookedProgressClass(p)" 
                    :style="{ width: Math.min(100, Math.round((p.cookedPortions / p.needToCookPortions) * 100)) + '%' }"
                  ></div>
                </div>
              </td>

              <!-- Sold (D) -->
              <td class="text-end fw-bold text-dark">
                {{ Number(p.soldPortions).toLocaleString() }} <small class="text-muted">{{ p.unit }}</small>
              </td>

              <!-- System Remain (C - D) -->
              <td class="text-end">
                <span 
                  class="badge px-2 py-1 fs-6"
                  :class="p.systemRemain > 0 ? 'bg-success-subtle text-success border border-success' : 'bg-danger-subtle text-danger border border-danger'"
                >
                  {{ Number(p.systemRemain).toLocaleString() }} {{ p.unit }}
                </span>
              </td>

              <!-- Status -->
              <td class="text-center">
                <span v-if="p.status === 'planned'" class="badge bg-secondary-subtle text-secondary border">Mới lập</span>
                <span v-else-if="p.status === 'in_service'" class="badge bg-primary-subtle text-primary border border-primary">Đang bán</span>
                <span v-else-if="p.status === 'reconciled'" class="badge bg-warning-subtle text-warning border border-warning">Đã kiểm kê</span>
                <span v-else-if="p.status === 'closed'" class="badge bg-dark text-white">Đã chốt ca</span>
                <span v-else class="badge bg-light text-dark border">{{ p.status }}</span>
              </td>

              <!-- Actions -->
              <td class="text-center pe-3">
                <div class="d-flex justify-content-center gap-1">
                  <!-- View BOM details -->
                  <button 
                    class="btn btn-sm btn-light border text-primary" 
                    title="Xem chi tiết định mức BOM & Nhu cầu nguyên liệu" 
                    @click="viewPortionBom(p)"
                  >
                    🔍
                  </button>
                  <!-- Edit row -->
                  <button 
                    class="btn btn-sm btn-light border text-secondary" 
                    title="Chỉnh sửa dòng kế hoạch" 
                    @click="openEditPortionModal(p)"
                  >
                    ✏️
                  </button>
                  <!-- Delete row -->
                  <button 
                    class="btn btn-sm btn-light border text-danger" 
                    title="Xóa khỏi kế hoạch" 
                    :disabled="p.soldPortions > 0 || p.cookedPortions > 0"
                    @click="deletePortion(p)"
                  >
                    🗑️
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 1: LẬP KẾ HOẠCH SUẤT ĂN HÀNG LOẠT (BATCH PLAN)                      -->
    <!-- ========================================================================= -->
    <div v-if="showPlanModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern bg-white p-4 shadow-lg modal-dialog-custom" style="max-width: 850px; width: 100%; max-height: 90vh; overflow-y: auto;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <div>
            <h5 class="fw-bold mb-0 text-dark">📋 Lập Kế Hoạch Suất Ăn Ngày {{ formatDateVN(selectedDate) }}</h5>
            <small class="text-muted">Nhập số lượng suất dự kiến bán và lượng tồn bán thành phẩm chuyển sang cho thực đơn hôm nay.</small>
          </div>
          <button class="btn-close" @click="showPlanModal = false"></button>
        </div>

        <!-- Filter menu items -->
        <div class="d-flex flex-wrap gap-2 mb-3 align-items-center justify-content-between">
          <div class="d-flex gap-2 flex-grow-1" style="max-width: 450px;">
            <input v-model="planDishSearch" type="text" class="form-control form-control-sm" placeholder="🔍 Lọc món theo tên..." />
            <select v-model="planCategoryFilter" class="form-select form-select-sm" style="max-width: 180px;">
              <option value="">Tất cả danh mục</option>
              <option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">{{ cat.categoryName }}</option>
            </select>
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-outline-secondary" @click="setAllPlannedDefault(20)">Đặt tất cả 20 suất</button>
            <button class="btn btn-sm btn-outline-secondary" @click="clearAllPlanInputs">Xóa hết</button>
          </div>
        </div>

        <!-- Table Dish Variants for Planning -->
        <div class="table-responsive border rounded-3 mb-3" style="max-height: 400px; overflow-y: auto;">
          <table class="table table-sm table-hover align-middle mb-0">
            <thead class="table-light small text-muted text-uppercase sticky-top">
              <tr>
                <th style="width: 40px;" class="ps-3">
                  <input type="checkbox" class="form-check-input" :checked="isAllSelectedForPlan" @change="toggleSelectAllPlan" />
                </th>
                <th>Tên Món Ăn & Biến Thể</th>
                <th>Danh Mục</th>
                <th class="text-end" style="width: 140px;">Kế Hoạch Bán (Suất)</th>
                <th class="text-end" style="width: 140px;">Tồn Hôm Qua (Suất)</th>
                <th class="text-end" style="width: 120px;">Cần Nấu Mới</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredPlanRows.length === 0">
                <td colspan="6" class="text-center text-muted py-4">Không tìm thấy món ăn nào phù hợp.</td>
              </tr>
              <tr v-for="row in filteredPlanRows" :key="`${row.foodId}-${row.variantId}`" :class="{ 'table-active': row.selected }">
                <td class="ps-3">
                  <input type="checkbox" class="form-check-input" v-model="row.selected" />
                </td>
                <td>
                  <strong class="text-dark">{{ row.foodName }}</strong>
                  <div class="small text-muted">Biến thể: <span class="badge bg-light text-dark border">{{ row.variantName }}</span></div>
                </td>
                <td class="small text-muted">{{ row.categoryName }}</td>
                <td>
                  <input 
                    type="number" 
                    min="0" 
                    step="1"
                    class="form-control form-control-sm text-end fw-bold" 
                    v-model.number="row.plannedPortions" 
                    @input="row.selected = true"
                  />
                </td>
                <td>
                  <input 
                    type="number" 
                    min="0" 
                    step="1"
                    class="form-control form-control-sm text-end" 
                    v-model.number="row.carryOverPortions" 
                    @input="row.selected = true"
                  />
                </td>
                <td class="text-end fw-bold text-warning-emphasis">
                  {{ Math.max(0, (row.plannedPortions || 0) - (row.carryOverPortions || 0)) }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="d-flex justify-content-between align-items-center border-top pt-3">
          <div class="small text-muted">
            Đã chọn <strong>{{ selectedPlanRowsCount }}</strong> món vào kế hoạch ngày {{ formatDateVN(selectedDate) }}.
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-light border" @click="showPlanModal = false">Đóng</button>
            <button 
              class="btn btn-sm btn-primary fw-bold px-3" 
              :disabled="isSubmittingPlan || selectedPlanRowsCount === 0"
              @click="submitBatchPlan"
            >
              {{ isSubmittingPlan ? 'Đang lưu...' : 'Lưu Kế Hoạch Suất Ăn' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 2: CHI TIẾT ĐỊNH MỨC BOM CỦA SUẤT ĂN                               -->
    <!-- ========================================================================= -->
    <div v-if="showBomModal && selectedPortionBom" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern bg-white p-4 shadow-lg" style="max-width: 750px; width: 100%;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <div>
            <h5 class="fw-bold mb-0 text-dark">🔍 Chi Tiết Định Mức BOM: {{ selectedPortionBom.foodName }}</h5>
            <small class="text-muted">Biến thể: <strong>{{ selectedPortionBom.variantName }}</strong> | Cần nấu: <strong>{{ selectedPortionBom.needToCookPortions }}</strong> suất</small>
          </div>
          <button class="btn-close" @click="showBomModal = false"></button>
        </div>

        <!-- BOM Ingredients Table -->
        <div class="table-responsive border rounded-3 mb-3">
          <table class="table table-sm table-hover align-middle mb-0">
            <thead class="table-light small text-muted text-uppercase">
              <tr>
                <th>Nguyên Liệu</th>
                <th class="text-end">Định Mức / 1 Suất</th>
                <th class="text-end">Hao Hụt (%)</th>
                <th class="text-end">Tổng Cần Cho {{ selectedPortionBom.needToCookPortions }} Suất</th>
                <th class="text-end">Tồn Kho Hiện Tại</th>
                <th class="text-center">Đánh Giá Tồn</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!selectedPortionBom.bom || selectedPortionBom.bom.length === 0">
                <td colspan="6" class="text-center text-muted py-4">Món ăn này chưa được cấu hình định mức BOM.</td>
              </tr>
              <tr v-for="ing in selectedPortionBom.bom" :key="ing.inventoryItemId">
                <td>
                  <strong class="text-dark">{{ ing.itemName }}</strong>
                  <div class="small text-muted" v-if="ing.purchaseUnit && ing.conversionRate > 1">
                    1 {{ ing.purchaseUnit }} = {{ ing.conversionRate }} {{ ing.baseUnit }}
                  </div>
                </td>
                <td class="text-end fw-semibold">
                  {{ Number(ing.quantityPerUnit).toLocaleString() }} {{ ing.baseUnit }}
                </td>
                <td class="text-end text-muted">
                  {{ ing.wastagePercent }}%
                </td>
                <td class="text-end fw-bold text-primary">
                  {{ Number(ing.totalRequired).toLocaleString() }} {{ ing.baseUnit }}
                </td>
                <td class="text-end text-dark">
                  {{ Number(ing.currentQuantity).toLocaleString() }} {{ ing.baseUnit }}
                </td>
                <td class="text-center">
                  <span v-if="ing.currentQuantity >= ing.totalRequired" class="badge bg-success-subtle text-success border border-success">
                    ✓ Đủ Hàng
                  </span>
                  <span v-else class="badge bg-danger text-white">
                    ⚠️ Thiếu {{ Number(ing.totalRequired - ing.currentQuantity).toLocaleString() }} {{ ing.baseUnit }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="d-flex justify-content-end">
          <button class="btn btn-sm btn-secondary" @click="showBomModal = false">Đóng</button>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 3: KẾT QUẢ TẠO PHIẾU XUẤT KHO BẾP TỰ ĐỘNG                           -->
    <!-- ========================================================================= -->
    <div v-if="showRequisitionResultModal && createdReceiptInfo" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern bg-white p-4 shadow-lg" style="max-width: 800px; width: 100%; max-height: 90vh; overflow-y: auto;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <div class="d-flex align-items-center gap-2">
            <span class="fs-4">🎉</span>
            <div>
              <h5 class="fw-bold mb-0 text-success">Đã Lập Phiếu Xuất Bếp Tự Động Thành Công</h5>
              <small class="text-muted">Mã phiếu: <strong class="text-dark">{{ createdReceiptInfo.receiptCode }}</strong> (Trạng thái: Chờ thủ kho duyệt)</small>
            </div>
          </div>
          <button class="btn-close" @click="showRequisitionResultModal = false"></button>
        </div>

        <div class="alert alert-info py-2 px-3 small rounded-3 mb-3 d-flex justify-content-between align-items-center gap-3">
          <div>
            <div>Hệ thống đã tự động nhân BOM của {{ summary.totalDishes }} món cần nấu, tổng hợp ra {{ createdReceiptInfo.totalIngredients }} loại nguyên liệu kho:</div>
            <div class="text-muted mt-1">Hao hụt chỉ để đối chiếu định mức và đã nằm trong lượng xuất, không cộng thêm lần nữa.</div>
          </div>
          <strong>Tổng tiền dự kiến: {{ formatCurrency(createdReceiptInfo.totalAmount) }}</strong>
        </div>

        <!-- Ingredients Table -->
        <div class="table-responsive border rounded-3 mb-3">
          <table class="table table-sm table-hover align-middle mb-0">
            <thead class="table-light small text-muted text-uppercase">
              <tr>
                <th>Nguyên Liệu</th>
                <th class="text-end">Số Lượng Xuất</th>
                <th class="text-end">Hao Hụt Định Mức</th>
                <th class="text-end">Tồn Kho Hiện Tại</th>
                <th class="text-center">Tình Trạng Kho</th>
                <th>Dùng Cho Các Món</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in createdReceiptInfo.items" :key="item.inventoryItemId">
                <td>
                  <strong class="text-dark">{{ item.itemName }}</strong>
                </td>
                <td class="text-end fw-bold text-primary">
                  {{ Number(item.quantityRequired).toLocaleString() }} {{ item.baseUnit }}
                </td>
                <td class="text-end">
                  <template v-if="item.hasWastage">
                    <span class="badge bg-warning-subtle text-warning-emphasis border border-warning">
                      {{ item.wastageSummary }}
                    </span>
                    <div class="small text-muted mt-1">Đã tính trong BOM</div>
                    <details v-if="(item.wastageDetails || []).length > 1" class="small text-start mt-1">
                      <summary class="text-primary" style="cursor: pointer;">Chi tiết</summary>
                      <div v-for="detail in item.wastageDetails" :key="`${detail.dishName}-${detail.wastagePercent}-${detail.quantityRequired}`" class="text-muted mt-1">
                        {{ detail.dishName }}: {{ formatWastageRate(detail.wastagePercent) }}
                      </div>
                    </details>
                  </template>
                  <span v-else class="text-muted small">Không áp dụng</span>
                </td>
                <td class="text-end text-muted">
                  {{ Number(item.currentStock).toLocaleString() }} {{ item.baseUnit }}
                </td>
                <td class="text-center">
                  <span v-if="item.isStockSufficient" class="badge bg-success-subtle text-success border border-success">
                    ✓ Đủ kho
                  </span>
                  <span v-else class="badge bg-danger text-white">
                    ⚠️ Thiếu hàng
                  </span>
                </td>
                <td class="small text-muted">
                  {{ (item.usedForDishes || []).join(', ') }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="d-flex justify-content-between align-items-center border-top pt-3">
          <router-link to="/admin/stock-receipts" class="btn btn-sm btn-outline-primary fw-bold">
            Xem Danh Sách Phiếu Kho &rarr;
          </router-link>
          <button class="btn btn-sm btn-success fw-bold px-3" @click="showRequisitionResultModal = false">
            Hoàn Tất
          </button>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 4: CHỈNH SỬA DÒNG SUẤT ĂN ĐƠN LẺ                                    -->
    <!-- ========================================================================= -->
    <div v-if="showEditModal && editingPortion" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern bg-white p-4 shadow-lg" style="max-width: 500px; width: 100%;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <h5 class="fw-bold mb-0 text-dark">✏️ Cập Nhật Suất Ăn: {{ editingPortion.foodName }}</h5>
          <button class="btn-close" @click="showEditModal = false"></button>
        </div>

        <form @submit.prevent="saveEditPortion">
          <div class="mb-3">
            <label class="form-label small fw-bold">Kế hoạch bán dự kiến (suất) <span class="text-danger">*</span></label>
            <input v-model.number="editForm.plannedPortions" type="number" min="0" step="any" class="form-control" required />
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Tồn bán thành phẩm chuyển sang (suất)</label>
            <input v-model.number="editForm.carryOverPortions" type="number" min="0" step="any" class="form-control" required />
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Số suất thực tế đã nấu (suất)</label>
            <input v-model.number="editForm.cookedPortions" type="number" min="0" step="any" class="form-control" required />
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Trạng thái điều phối</label>
            <select v-model="editForm.status" class="form-select">
              <option value="planned">Mới lập (Planned)</option>
              <option value="in_service">Đang phục vụ (In Service)</option>
              <option value="reconciled">Đã đối soát ca (Reconciled)</option>
              <option value="closed">Đã đóng (Closed)</option>
            </select>
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Ghi chú bếp</label>
            <textarea v-model="editForm.note" class="form-control" rows="2" placeholder="Ghi chú thêm..."></textarea>
          </div>

          <div class="d-flex justify-content-end gap-2 border-top pt-3">
            <button type="button" class="btn btn-sm btn-light border" @click="showEditModal = false">Hủy</button>
            <button type="submit" class="btn btn-sm btn-primary fw-bold px-3">Lưu Thay Đổi</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import axios from 'axios';
import * as signalR from '@microsoft/signalr';

const API_ROOT = 'http://localhost:5079/api/admin';
const API_BASE = `${API_ROOT}/inventory/daily-portions`;
const CATEGORY_API = `${API_ROOT}/category/list`;
const FOOD_API = `${API_ROOT}/food/list`;

function formatDateInput(date = new Date()) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function parseDateInput(value) {
  const [year, month, day] = String(value).split('-').map(Number);
  return new Date(year, month - 1, day);
}

// Date state
const selectedDate = ref(formatDateInput());

// Data state
const loading = ref(false);
const portions = ref([]);
const categories = ref([]);
const hasRequisition = ref(false);
const summary = ref({
  totalDishes: 0,
  totalPlanned: 0,
  totalCarryOver: 0,
  totalNeedToCook: 0,
  totalCooked: 0,
  totalSold: 0,
  totalSystemRemain: 0
});

// Filters
const searchQuery = ref('');
const selectedCategoryFilter = ref('');
const selectedStatusFilter = ref('all');

// Modals
const showPlanModal = ref(false);
const showBomModal = ref(false);
const showRequisitionResultModal = ref(false);
const showEditModal = ref(false);

const selectedPortionBom = ref(null);
const createdReceiptInfo = ref(null);
const editingPortion = ref(null);
const editForm = ref({
  plannedPortions: 0,
  carryOverPortions: 0,
  cookedPortions: 0,
  status: 'planned',
  note: ''
});

// Batch plan state
const allDishesWithVariants = ref([]);
const planDishSearch = ref('');
const planCategoryFilter = ref('');
const isSubmittingPlan = ref(false);
const isPullingCarryOver = ref(false);
const isGeneratingRequisition = ref(false);

// SignalR Connection
let hubConnection = null;

// ==========================================
// COMPUTED
// ==========================================

const filteredPortions = computed(() => {
  return portions.value.filter(p => {
    const matchSearch = !searchQuery.value || 
      p.foodName.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      (p.variantName && p.variantName.toLowerCase().includes(searchQuery.value.toLowerCase()));

    const matchCategory = !selectedCategoryFilter.value || p.categoryId === Number(selectedCategoryFilter.value);

    const matchStatus = selectedStatusFilter.value === 'all' || p.status.toLowerCase() === selectedStatusFilter.value.toLowerCase();

    return matchSearch && matchCategory && matchStatus;
  });
});

const filteredPlanRows = computed(() => {
  return allDishesWithVariants.value.filter(row => {
    const matchSearch = !planDishSearch.value || 
      row.foodName.toLowerCase().includes(planDishSearch.value.toLowerCase()) ||
      row.variantName.toLowerCase().includes(planDishSearch.value.toLowerCase());

    const matchCategory = !planCategoryFilter.value || row.categoryId === Number(planCategoryFilter.value);

    return matchSearch && matchCategory;
  });
});

const selectedPlanRowsCount = computed(() => {
  return allDishesWithVariants.value.filter(r => r.selected && r.plannedPortions > 0).length;
});

const isAllSelectedForPlan = computed(() => {
  const visible = filteredPlanRows.value;
  return visible.length > 0 && visible.every(r => r.selected);
});

// ==========================================
// METHODS: FETCH & DATE
// ==========================================

function setToday() {
  selectedDate.value = formatDateInput();
  fetchDailyPortions();
}

function changeDateOffset(days) {
  const cur = parseDateInput(selectedDate.value);
  cur.setDate(cur.getDate() + days);
  selectedDate.value = formatDateInput(cur);
  fetchDailyPortions();
}

async function fetchDailyPortions() {
  loading.value = true;
  try {
    const res = await axios.get(API_BASE, {
      params: { date: selectedDate.value }
    });

    if (res.data?.success) {
      portions.value = Array.isArray(res.data.data) ? res.data.data : [];
      summary.value = res.data.summary;
      hasRequisition.value = res.data.hasRequisition;
    }
  } catch (err) {
    console.error('Lỗi khi tải bảng điều phối suất ăn:', err);
  } finally {
    loading.value = false;
  }
}

async function fetchCategories() {
  try {
    const res = await axios.get(CATEGORY_API);
    if (res.data?.success) {
      categories.value = res.data.data;
    }
  } catch (err) {
    console.error('Lỗi khi tải danh mục món:', err);
  }
}

// ==========================================
// ACTIONS: COOKED, PULL, GENERATE, DELETE
// ==========================================

async function quickAddCooked(portion, delta) {
  portion.cookedPortions = (portion.cookedPortions || 0) + delta;
  await updateCooked(portion);
}

async function updateCooked(portion) {
  try {
    const res = await axios.put(`${API_BASE}/${portion.portionControlId}/cooked`, {
      cookedPortions: Number(portion.cookedPortions)
    });

    if (res.data?.success) {
      portion.systemRemain = res.data.data.systemRemain;
      portion.status = res.data.data.status;
      // Cập nhật lại summary
      await fetchDailyPortions();
    }
  } catch (err) {
    alert('Lỗi cập nhật suất nấu: ' + (err.response?.data?.message || err.message));
    await fetchDailyPortions();
  }
}

async function pullYesterdayCarryOver() {
  if (!confirm(`Bạn có muốn kéo số lượng tồn bán thành phẩm từ ngày hôm trước sang ngày ${formatDateVN(selectedDate.value)} không?`)) return;

  isPullingCarryOver.value = true;
  try {
    const res = await axios.post(`${API_BASE}/pull-yesterday-carryover`, null, {
      params: { date: selectedDate.value }
    });

    alert(res.data?.message || 'Kéo tồn thành công!');
    await fetchDailyPortions();
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  } finally {
    isPullingCarryOver.value = false;
  }
}

async function generateRequisition() {
  if (hasRequisition.value) {
    alert('Ngày này đã có phiếu xuất bếp. Hãy kiểm tra hoặc hủy phiếu cũ trước khi lập lại.');
    return;
  }

  if (!confirm(`Xác nhận tự động nhân định mức BOM và lập Phiếu Xuất Bếp cho ${summary.value.totalNeedToCook} suất cần nấu ngày ${formatDateVN(selectedDate.value)}?`)) return;

  isGeneratingRequisition.value = true;
  try {
    const res = await axios.post(`${API_BASE}/generate-requisition`, {
      controlDate: selectedDate.value,
      note: `Phiếu xuất kho bếp tự động từ điều phối suất ăn ngày ${selectedDate.value}`
    });

    if (res.data?.success) {
      createdReceiptInfo.value = res.data.receipt;
      showRequisitionResultModal.value = true;
      hasRequisition.value = true;
      await fetchDailyPortions();
    }
  } catch (err) {
    alert('Lỗi khi lập phiếu xuất bếp: ' + (err.response?.data?.message || err.message));
  } finally {
    isGeneratingRequisition.value = false;
  }
}

async function viewPortionBom(portion) {
  try {
    const res = await axios.get(`${API_BASE}/${portion.portionControlId}`);
    if (res.data?.success) {
      selectedPortionBom.value = res.data.data;
      showBomModal.value = true;
    }
  } catch (err) {
    alert('Lỗi khi xem BOM: ' + (err.response?.data?.message || err.message));
  }
}

function openEditPortionModal(portion) {
  editingPortion.value = portion;
  editForm.value = {
    plannedPortions: portion.plannedPortions,
    carryOverPortions: portion.carryOverPortions,
    cookedPortions: portion.cookedPortions,
    status: portion.status,
    note: portion.note || ''
  };
  showEditModal.value = true;
}

async function saveEditPortion() {
  try {
    const res = await axios.put(`${API_BASE}/${editingPortion.value.portionControlId}`, editForm.value);
    if (res.data?.success) {
      alert('Đã cập nhật thông tin suất ăn thành công!');
      showEditModal.value = false;
      await fetchDailyPortions();
    }
  } catch (err) {
    alert('Lỗi khi cập nhật: ' + (err.response?.data?.message || err.message));
  }
}

async function deletePortion(portion) {
  if (!confirm(`Bạn có chắc muốn xóa món '${portion.foodName} (${portion.variantName})' khỏi kế hoạch ngày hôm nay?`)) return;

  try {
    const res = await axios.delete(`${API_BASE}/${portion.portionControlId}`);
    if (res.data?.success) {
      alert(res.data.message);
      await fetchDailyPortions();
    }
  } catch (err) {
    alert('Lỗi khi xóa: ' + (err.response?.data?.message || err.message));
  }
}

// ==========================================
// MODAL: BATCH PLAN METHODS
// ==========================================

async function openPlanModal() {
  try {
    // Tải danh sách toàn bộ món ăn và variants từ backend
    const res = await axios.get(FOOD_API, { params: { isActive: true } });
    const foodsList = res.data?.data || [];

    const existingMap = {};
    portions.value.forEach(p => {
      existingMap[`${p.foodId}-${p.variantId}`] = p;
    });

    const rows = [];
    foodsList.forEach(food => {
      if (food.variants && food.variants.length > 0) {
        food.variants.forEach(variant => {
          const key = `${food.foodId}-${variant.variantId}`;
          const exist = existingMap[key];
          rows.push({
            foodId: food.foodId,
            foodName: food.foodName,
            categoryId: food.categoryId,
            categoryName: food.categoryName || 'Món ăn',
            variantId: variant.variantId,
            variantName: variant.variantName,
            plannedPortions: exist ? exist.plannedPortions : 0,
            carryOverPortions: exist ? exist.carryOverPortions : 0,
            selected: !!exist
          });
        });
      } else {
        const key = `${food.foodId}-null`;
        const exist = existingMap[key];
        rows.push({
          foodId: food.foodId,
          foodName: food.foodName,
          categoryId: food.categoryId,
          categoryName: food.categoryName || 'Món ăn',
          variantId: null,
          variantName: 'Mặc định',
          plannedPortions: exist ? exist.plannedPortions : 0,
          carryOverPortions: exist ? exist.carryOverPortions : 0,
          selected: !!exist
        });
      }
    });

    allDishesWithVariants.value = rows;
    showPlanModal.value = true;
  } catch (err) {
    alert('Lỗi khi chuẩn bị danh sách món: ' + err.message);
  }
}

function toggleSelectAllPlan() {
  const current = isAllSelectedForPlan.value;
  filteredPlanRows.value.forEach(r => r.selected = !current);
}

function setAllPlannedDefault(qty) {
  filteredPlanRows.value.forEach(r => {
    r.selected = true;
    if (!r.plannedPortions || r.plannedPortions === 0) {
      r.plannedPortions = qty;
    }
  });
}

function clearAllPlanInputs() {
  allDishesWithVariants.value.forEach(r => {
    r.selected = false;
    r.plannedPortions = 0;
    r.carryOverPortions = 0;
  });
}

async function submitBatchPlan() {
  const selectedItems = allDishesWithVariants.value
    .filter(r => r.selected && r.plannedPortions > 0)
    .map(r => ({
      foodId: r.foodId,
      variantId: r.variantId,
      plannedPortions: Number(r.plannedPortions),
      carryOverPortions: Number(r.carryOverPortions || 0)
    }));

  if (selectedItems.length === 0) {
    alert('Vui lòng chọn ít nhất 1 món với số lượng suất kế hoạch > 0.');
    return;
  }

  isSubmittingPlan.value = true;
  try {
    const payload = {
      controlDate: selectedDate.value,
      items: selectedItems
    };

    const res = await axios.post(`${API_BASE}/batch-plan`, payload);
    if (res.data?.success) {
      alert(res.data.message);
      showPlanModal.value = false;
      await fetchDailyPortions();
    }
  } catch (err) {
    alert('Lỗi lưu kế hoạch: ' + (err.response?.data?.message || err.message));
  } finally {
    isSubmittingPlan.value = false;
  }
}

// ==========================================
// HELPERS
// ==========================================

function formatCurrency(val) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val || 0);
}

function formatWastageRate(val) {
  return `${Number(val || 0).toLocaleString('vi-VN', { maximumFractionDigits: 2 })}%`;
}

function formatDateVN(dateStr) {
  if (!dateStr) return '';
  const d = parseDateInput(dateStr);
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function getCookedProgressClass(p) {
  const pct = p.needToCookPortions > 0
    ? (p.cookedPortions / p.needToCookPortions) * 100
    : 0;
  if (pct >= 100) return 'bg-success';
  if (pct >= 50) return 'bg-primary';
  return 'bg-warning';
}

// ==========================================
// LIFECYCLE & SIGNALR
// ==========================================

onMounted(async () => {
  await fetchCategories();
  await fetchDailyPortions();

  // Setup SignalR Realtime
  try {
    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5079/hubs/restaurant', {
        accessTokenFactory: () => localStorage.getItem('token') || '',
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    hubConnection.on('KitchenUpdated', () => {
      // Refresh portions when kitchen or orders update
      fetchDailyPortions();
    });

    hubConnection.on('InventoryUpdated', () => {
      fetchDailyPortions();
    });

    await hubConnection.start();
  } catch (e) {
    console.warn('SignalR Realtime kết nối dự phòng (polling vẫn hoạt động):', e.message);
  }
});

onUnmounted(() => {
  if (hubConnection) {
    hubConnection.stop();
  }
});
</script>

<style scoped>
.daily-portions-container {
  padding: 1.5rem;
  background-color: #f8fafc;
  min-height: calc(100vh - 70px);
}

.card-modern {
  background: #ffffff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05);
}

.stat-badge {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
}

.modal-backdrop-custom {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background-color: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(4px);
  z-index: 1050;
}

.table th {
  font-weight: 600;
  letter-spacing: 0.03em;
  font-size: 0.75rem;
}

.table td {
  font-size: 0.875rem;
}
</style>

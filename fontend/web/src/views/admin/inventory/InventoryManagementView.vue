<template>
  <div class="inventory-container">
    <!-- Header Page Banner -->
    <div class="card-modern p-4 mb-4 text-white position-relative overflow-hidden" 
         style="background: linear-gradient(135deg, #064e3b 0%, #047857 50%, #059669 100%);">
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 position-relative" style="z-index: 2;">
        <div>
          <div class="d-flex align-items-center gap-2 mb-2">
            <span class="badge bg-warning text-dark px-3 py-1 fw-bold">WMS & BOM</span>
            <span class="badge bg-success-subtle text-white border border-light border-opacity-25 px-2 py-1">PHẦN 1: NGUYÊN LIỆU & ĐỊNH LƯỢNG MÓN</span>
          </div>
          <h2 class="fw-bold mb-1">📦 Quản Lý Kho & Định Lượng Suất Ăn</h2>
          <p class="text-white-50 mb-0">Quản lý nguyên liệu linh hoạt (đơn vị đơn hoặc quy đổi mua/gốc), Lô hàng gom nhóm theo món/nguyên liệu chuẩn FEFO & Định mức BOM theo Size.</p>
        </div>
        <div class="d-flex flex-wrap gap-2">
          <router-link to="/admin/stock-receipts" class="btn btn-outline-light fw-bold d-flex align-items-center gap-2 text-decoration-none">
            <span aria-hidden="true">≡</span>
            <span>Nhập nhiều nguyên liệu</span>
          </router-link>
          <button class="btn btn-warning fw-bold d-flex align-items-center gap-2 shadow-sm" @click="openCreateBatchModal()">
            <span>🏷️</span>
            <span>Nhập nhanh 1 nguyên liệu</span>
          </button>
          <button class="btn btn-light fw-bold d-flex align-items-center gap-2 shadow-sm" @click="openItemModal()">
            <span>➕</span>
            <span>Thêm Nguyên Liệu</span>
          </button>
        </div>
      </div>
    </div>

    <!-- 4 KPI Summary Cards -->
    <div class="row g-3 mb-4">
      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Mặt Hàng Nguyên Liệu</span>
              <h3 class="fw-bold text-dark mt-1 mb-0">{{ summary.totalItems }}</h3>
            </div>
            <div class="stat-badge bg-primary-subtle text-primary">📦</div>
          </div>
          <div class="small text-muted pt-2 border-top d-flex justify-content-between">
            <span>Đang quản lý</span>
            <span class="text-primary fw-bold">{{ summary.totalBatches }} lô hàng</span>
          </div>
        </div>
      </div>

      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100" :class="{ 'border-danger border': summary.lowStockCount > 0 }">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Cảnh Báo Sắp Hết</span>
              <h3 class="fw-bold mt-1 mb-0" :class="summary.lowStockCount > 0 ? 'text-danger' : 'text-success'">
                {{ summary.lowStockCount }}
              </h3>
            </div>
            <div class="stat-badge" :class="summary.lowStockCount > 0 ? 'bg-danger-subtle text-danger' : 'bg-success-subtle text-success'">
              ⚠️
            </div>
          </div>
          <div class="small pt-2 border-top d-flex justify-content-between align-items-center">
            <span class="text-muted">Dưới mức tối thiểu</span>
            <button v-if="summary.lowStockCount > 0" class="btn btn-sm btn-link p-0 text-danger fw-bold text-decoration-none" @click="filterOnlyLowStock">
              Lọc xem ngay &rarr;
            </button>
          </div>
        </div>
      </div>

      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100" :class="{ 'border-warning border': summary.expiringBatchesCount > 0 }">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Lô Cận Hạn (FEFO)</span>
              <h3 class="fw-bold mt-1 mb-0" :class="summary.expiringBatchesCount > 0 ? 'text-warning' : 'text-success'">
                {{ summary.expiringBatchesCount }}
              </h3>
            </div>
            <div class="stat-badge bg-warning-subtle text-warning">⏳</div>
          </div>
          <div class="small pt-2 border-top d-flex justify-content-between align-items-center">
            <span class="text-muted">Sắp hết hạn: {{ summary.expiringBatchesCount }} · <span class="text-danger fw-bold">Quá hạn: {{ summary.expiredBatchesCount || 0 }}</span></span>
            <button class="btn btn-sm btn-link p-0 text-warning fw-bold text-decoration-none" @click="activeTab = 'batches'; batchFilterExpiry = '7'">
              Kiểm tra lô &rarr;
            </button>
          </div>
          <button v-if="summary.expiredBatchesCount > 0" class="btn btn-sm btn-link p-0 text-danger fw-bold text-decoration-none mt-2" @click="filterExpiredBatches">
            Xử lý lô quá hạn &rarr;
          </button>
        </div>
      </div>

      <div class="col-sm-6 col-xl-3">
        <div class="card-modern p-3 h-100">
          <div class="d-flex justify-content-between align-items-start mb-2">
            <div>
              <span class="text-muted small fw-semibold text-uppercase">Tổng Giá Trị Kho</span>
              <h3 class="fw-bold text-success mt-1 mb-0">{{ formatCurrency(summary.totalValue) }}</h3>
            </div>
            <div class="stat-badge bg-success-subtle text-success">💰</div>
          </div>
          <div class="small text-muted pt-2 border-top d-flex justify-content-between">
            <span>Theo giá mua, tồn quy đổi theo đơn vị gốc</span>
            <span class="text-muted">{{ summary.totalSuppliers }} NCC</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Navigation Tabs -->
    <div class="card-modern overflow-hidden mb-4">
      <div class="p-2 border-bottom bg-light d-flex flex-wrap align-items-center justify-content-between gap-2">
        <div class="d-flex flex-wrap gap-2">
          <button class="btn nav-tab-btn" :class="{ 'active': activeTab === 'items' }" @click="activeTab = 'items'">
            📦 Tồn Kho Nguyên Liệu
            <span v-if="summary.lowStockCount > 0" class="badge bg-danger rounded-pill ms-1">{{ summary.lowStockCount }}</span>
          </button>
          <button class="btn nav-tab-btn" :class="{ 'active': activeTab === 'batches' }" @click="activeTab = 'batches'">
            🏷️ Lô Hàng & Hạn Dùng (FEFO)
            <span v-if="summary.expiringBatchesCount > 0" class="badge bg-warning text-dark rounded-pill ms-1">{{ summary.expiringBatchesCount }}</span>
            <span v-if="summary.expiredBatchesCount > 0" class="badge bg-danger rounded-pill ms-1">{{ summary.expiredBatchesCount }} quá hạn</span>
          </button>
          <button class="btn nav-tab-btn" :class="{ 'active': activeTab === 'recipes' }" @click="activeTab = 'recipes'">
            🥗 Định Mức Suất Ăn (BOM)
          </button>
          <button class="btn nav-tab-btn" :class="{ 'active': activeTab === 'suppliers' }" @click="activeTab = 'suppliers'">
            🚚 Nhà Cung Cấp
          </button>
        </div>

        <!-- Roadmap Navigation Link -->
        <div class="d-flex gap-2 align-items-center small pe-2">
          <router-link to="/admin/daily-portions" class="btn btn-sm btn-outline-primary fw-bold text-decoration-none">
            🍲 Sang Điều Phối Suất Ăn &rarr;
          </router-link>
        </div>
      </div>

      <!-- ========================================================================= -->
      <!-- TAB 1: TỒN KHO NGUYÊN LIỆU (FLEXIBLE UNIT: SINGLE OR DUAL UNIT)           -->
      <!-- ========================================================================= -->
      <div v-if="activeTab === 'items'" class="p-3 p-md-4">
        <!-- Filter Controls -->
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div class="d-flex flex-wrap gap-2 flex-grow-1" style="max-width: 700px;">
            <input 
              v-model="itemSearch" 
              type="text" 
              class="form-control form-control-sm" 
              placeholder="🔍 Tìm theo tên nguyên liệu, đơn vị tính..."
              style="max-width: 320px;"
            />
            <select v-model="selectedSupplierFilter" class="form-select form-select-sm" style="max-width: 200px;">
              <option value="">Tất cả nhà cung cấp</option>
              <option v-for="sup in suppliers" :key="sup.supplierId" :value="sup.supplierId">
                {{ sup.supplierName }}
              </option>
            </select>
            <div class="form-check form-switch d-flex align-items-center gap-2 m-0 ps-4">
              <input class="form-check-input" type="checkbox" id="lowStockToggle" v-model="onlyLowStockFilter" />
              <label class="form-check-label small fw-bold text-danger" for="lowStockToggle">
                Chỉ hiện sắp hết
              </label>
            </div>
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-outline-secondary" @click="fetchInventoryData">
              🔄 Làm mới
            </button>
            <button class="btn btn-sm btn-success fw-bold" @click="openItemModal()">
              ➕ Thêm Nguyên Liệu
            </button>
          </div>
        </div>

        <!-- Table Items -->
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light text-muted small text-uppercase">
              <tr>
                <th style="width: 60px;">Mã</th>
                <th>Tên Nguyên Liệu</th>
                <th>Đơn Vị Tính</th>
                <th class="text-end">Tồn Kho Hiện Tại</th>
                <th class="text-end">Tối Thiểu</th>
                <th class="text-center">Hao Hụt Sơ Chế</th>
                <th class="text-end">Đơn Giá Vốn</th>
                <th class="text-end">Tổng Giá Trị</th>
                <th class="text-center">Lô & Hạn Gần Nhất</th>
                <th class="text-center">Trạng Thái</th>
                <th class="text-center" style="width: 140px;">Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredItems.length === 0">
                <td colspan="11" class="text-center text-muted py-4">
                  Không tìm thấy nguyên liệu nào phù hợp với điều kiện tìm kiếm.
                </td>
              </tr>
              <tr v-for="item in filteredItems" :key="item.inventoryItemId">
                <td class="fw-bold text-muted">#{{ item.inventoryItemId }}</td>
                <td>
                  <div class="fw-bold text-dark fs-6">{{ item.itemName }}</div>
                  <div class="small text-muted d-flex align-items-center gap-1">
                    <span>🏢</span> {{ item.supplierName || 'Chưa gắn nhà cung cấp' }}
                  </div>
                </td>
                <td>
                  <!-- Nếu có quy đổi (conversionRate > 1 và khác nhau) -->
                  <div v-if="hasUnitConversion(item)" class="badge bg-light text-dark border px-2 py-1">
                    1 {{ item.purchaseUnit }} = {{ Number(item.conversionRate).toLocaleString() }} {{ item.baseUnit }}
                  </div>
                  <!-- Nếu dùng đơn vị đơn thuần -->
                  <div v-else class="badge bg-secondary-subtle text-secondary border px-2 py-1">
                    {{ item.baseUnit }}
                  </div>
                </td>
                <td class="text-end">
                  <div class="d-flex flex-column align-items-end">
                    <div class="fw-bold fs-6" :class="item.isLowStock ? 'text-danger' : 'text-dark'">
                      {{ Number(item.currentQuantity).toLocaleString() }} <span class="small text-muted">{{ item.baseUnit }}</span>
                    </div>
                    <div class="small text-muted" v-if="hasUnitConversion(item)">
                      ≈ {{ item.quantityInPurchaseUnit }} {{ item.purchaseUnit }}
                    </div>
                    <div class="stock-progress-track mt-1" :title="`Tồn: ${Number(item.currentQuantity).toLocaleString()} / Tối thiểu: ${Number(item.minimumQuantity).toLocaleString()} ${item.baseUnit}`">
                      <div 
                        class="stock-progress-fill" 
                        :class="item.isLowStock ? 'bg-danger' : (item.currentQuantity <= item.minimumQuantity * 1.5 ? 'bg-warning' : 'bg-success')"
                        :style="{ width: `${Math.min(100, item.minimumQuantity > 0 ? (item.currentQuantity / (item.minimumQuantity * 2)) * 100 : 100)}%` }"
                      ></div>
                    </div>
                  </div>
                </td>
                <td class="text-end text-muted">
                  {{ Number(item.minimumQuantity).toLocaleString() }} <small>{{ item.baseUnit }}</small>
                </td>
                <td class="text-center">
                  <span
                    v-if="Number(item.prepWastagePercent) > 0"
                    class="badge bg-warning-subtle text-dark border border-warning px-2 py-1 fw-bold cursor-pointer"
                    :title="`Hao hụt sơ chế dự kiến: ${item.prepWastagePercent}%. Khi nhập hàng, hệ thống tự động trừ hao hụt này để tính lượng dùng thực tế.`"
                    @click="openItemModal(item)"
                  >
                    📉 {{ formatWastagePercent(item.prepWastagePercent) }}%
                  </span>
                  <span
                    v-else
                    class="badge bg-light text-muted border px-2 py-1"
                    title="Nguyên liệu chuẩn không hao hụt sơ chế"
                  >
                    0% (Chuẩn)
                  </span>
                </td>
                <td class="text-end">
                  <div class="fw-semibold text-dark">{{ formatCurrency(item.costPerPurchaseUnit ?? item.costPerUnit) }} <small class="text-muted">/{{ item.purchaseUnit }}</small></div>
                  <div class="small text-muted">
                    {{ formatCurrency(item.costPerBaseUnit ?? item.costPerUnit) }}/{{ item.baseUnit }}
                  </div>
                </td>
                <td class="text-end fw-bold text-success">
                  {{ formatCurrency(item.totalValue) }}
                  <div v-if="item.hasBatchQuantityMismatch" class="small text-danger fw-normal" :title="`Tổng lô ${Number(item.batchQuantityTotal).toLocaleString()} ${item.baseUnit}, tồn sổ ${Number(item.currentQuantity).toLocaleString()} ${item.baseUnit}`">Lệch tồn lô</div>
                </td>
                <td class="text-center">
                  <button class="btn btn-sm btn-outline-info py-0 px-2 fw-semibold" @click="viewItemBatches(item)">
                    🏷️ {{ item.activeBatchesCount || 0 }} lô
                  </button>
                  <div class="small mt-1" v-if="item.earliestExpiry">
                    <span class="badge" :class="isNearExpiry(item.earliestExpiry) ? 'bg-danger text-white' : 'bg-light text-muted border'">
                      HSD: {{ formatDate(item.earliestExpiry) }}
                    </span>
                  </div>
                </td>
                <td class="text-center">
                  <span v-if="item.isLowStock" class="badge bg-danger-subtle text-danger border border-danger px-2 py-1">
                    ⚠️ Sắp Hết Hàng
                  </span>
                  <span v-else class="badge bg-success-subtle text-success border border-success px-2 py-1">
                    ✓ Đủ Tồn Kho
                  </span>
                </td>
                <td class="text-center">
                  <div class="d-flex justify-content-center gap-1">
                    <button class="btn btn-sm btn-light border text-primary" title="Xem lô hàng" @click="viewItemBatches(item)">
                      🏷️
                    </button>
                    <button class="btn btn-sm btn-light border text-secondary" title="Chỉnh sửa" @click="openItemModal(item)">
                      ✏️
                    </button>
                    <button class="btn btn-sm btn-light border text-danger" title="Vô hiệu hóa" @click="deleteItem(item)">
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
      <!-- TAB 2: QUẢN LÝ LÔ HÀNG GOM NHÓM THEO NGUYÊN LIỆU & MÓN ĂN (FEFO TRACKER)  -->
      <!-- ========================================================================= -->
      <div v-if="activeTab === 'batches'" class="p-3 p-md-4">
        <!-- FEFO Guide Banner -->
        <div class="alert alert-info d-flex align-items-center justify-content-between p-3 mb-3 border-0 bg-info-subtle text-info-emphasis rounded-3">
          <div class="d-flex align-items-center gap-2">
            <span class="fs-4">⏳</span>
            <div>
              <strong>Quản lý Lô Hàng Gom Nhóm & Quy tắc FEFO (First-Expired, First-Out):</strong>
              <div class="small">Mỗi nguyên liệu hiển thị tổng hợp kèm các món ăn sử dụng; bấm mở rộng để xem chi tiết các lô hàng xếp theo hạn dùng gần nhất.</div>
            </div>
          </div>
          <div class="d-flex gap-2">
            <!-- View Mode Switch -->
            <div class="btn-group btn-group-sm">
              <button 
                class="btn" 
                :class="batchViewMode === 'grouped' ? 'btn-primary fw-bold' : 'btn-outline-primary'" 
                @click="batchViewMode = 'grouped'">
                📂 Gom nhóm theo Nguyên Liệu
              </button>
              <button 
                class="btn" 
                :class="batchViewMode === 'flat' ? 'btn-primary fw-bold' : 'btn-outline-primary'" 
                @click="batchViewMode = 'flat'">
                📋 Danh sách phẳng
              </button>
            </div>
            <button class="btn btn-sm btn-success fw-bold" @click="openCreateBatchModal()">
              Lập phiếu nhập
            </button>
          </div>
        </div>

        <!-- Filters -->
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <div class="d-flex flex-wrap gap-2 flex-grow-1" style="max-width: 750px;">
            <input 
              v-model="batchSearch" 
              type="text" 
              class="form-control form-control-sm" 
              placeholder="🔍 Tìm theo tên nguyên liệu, món ăn, mã lô..."
              style="max-width: 320px;"
            />
            <select v-model="batchFilterStatus" class="form-select form-select-sm" style="max-width: 170px;">
              <option value="all">Tất cả trạng thái</option>
              <option value="active">Đang sử dụng (Active)</option>
              <option value="depleted">Đã hết số lượng</option>
              <option value="expired">Đã hết hạn</option>
            </select>
            <select v-model="batchFilterExpiry" class="form-select form-select-sm" style="max-width: 180px;">
              <option value="all">Tất cả hạn dùng</option>
              <option value="7">Sắp hết hạn trong 7 ngày</option>
              <option value="14">Sắp hết hạn trong 14 ngày</option>
              <option value="30">Sắp hết hạn trong 30 ngày</option>
            </select>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button v-if="selectableExpiredBatches.length > 0" class="btn btn-sm btn-outline-danger" type="button" @click="selectAllExpiredBatches">
              Chọn tất cả quá hạn ({{ selectableExpiredBatches.length }})
            </button>
            <button class="btn btn-sm btn-outline-secondary" @click="fetchBatches">
              🔄 Làm mới
            </button>
          </div>
        </div>

        <div v-if="batchActionMessage" class="alert alert-success d-flex align-items-start justify-content-between gap-3 py-3 mb-3" role="status">
          <div>
            <strong>Đã hủy lô thành công.</strong>
            <div class="small mt-1">{{ batchActionMessage }}</div>
          </div>
          <button type="button" class="btn-close flex-shrink-0" aria-label="Đóng thông báo" @click="batchActionMessage = ''"></button>
        </div>

        <div v-if="batchActionError" class="alert alert-danger d-flex align-items-start justify-content-between gap-3 py-3 mb-3" role="alert">
          <div>
            <strong>Chưa hủy được lô.</strong>
            <div class="small mt-1">{{ batchActionError }}</div>
          </div>
          <button type="button" class="btn-close flex-shrink-0" aria-label="Đóng thông báo" @click="batchActionError = ''"></button>
        </div>

        <div v-if="selectedExpiredBatches.length > 0" class="expired-bulk-action mb-3" role="region" aria-live="polite">
          <div class="d-flex flex-wrap align-items-center justify-content-between gap-3">
            <div>
              <div class="fw-bold text-danger">Đã chọn {{ selectedExpiredBatches.length }} lô quá hạn</div>
              <div class="small text-muted">{{ selectedExpiredItemCount }} nguyên liệu · sẽ tạo 1 phiếu xuất hủy chung</div>
            </div>
            <div class="d-flex flex-wrap gap-2">
              <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="isWastingExpired" @click="clearExpiredBatchSelection">
                Bỏ chọn
              </button>
              <button type="button" class="btn btn-sm btn-danger fw-bold" :disabled="isWastingExpired" @click="confirmSelectedExpiredWaste">
                <span v-if="isWastingExpired" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
                {{ isWastingExpired ? 'Đang tạo phiếu...' : 'Xác nhận hủy và tạo phiếu' }}
              </button>
            </div>
          </div>
          <div class="small text-muted mt-2">Mỗi lô sẽ hủy toàn bộ số lượng còn lại. Phiếu được ghi nhận ngay sau khi xác nhận.</div>
        </div>

        <!-- ============================================== -->
        <!-- CHẾ ĐỘ 1: GOM NHÓM THEO NGUYÊN LIỆU (GROUPED) -->
        <!-- ============================================== -->
        <div v-if="batchViewMode === 'grouped'" class="d-flex flex-column gap-3">
          <div v-if="groupedBatchesByItem.length === 0" class="text-center text-muted py-5 card-modern">
            Không tìm thấy nguyên liệu nào phù hợp với bộ lọc.
          </div>

          <div 
            v-for="group in groupedBatchesByItem" 
            :key="group.inventoryItemId" 
            class="card-modern overflow-hidden border"
          >
            <!-- Group Header -->
            <div 
              class="p-3 d-flex flex-wrap justify-content-between align-items-center gap-3 cursor-pointer group-header-hover"
              :class="{ 'bg-warning-subtle': group.hasExpiringBatch, 'bg-light': !group.hasExpiringBatch }"
              @click="toggleGroupExpand(group.inventoryItemId)"
            >
              <div class="d-flex align-items-center gap-3">
                <button class="btn btn-sm btn-outline-secondary p-1 rounded-circle d-flex align-items-center justify-content-center" style="width: 28px; height: 28px;">
                  <span class="fw-bold" style="font-size: 0.8rem;">{{ isGroupExpanded(group.inventoryItemId) ? '▼' : '▶' }}</span>
                </button>
                <div>
                  <div class="d-flex align-items-center gap-2">
                    <h5 class="fw-bold text-dark m-0">{{ group.itemName }}</h5>
                    <span class="badge bg-light text-dark border">
                      {{ Number(group.totalRemaining).toLocaleString() }} {{ group.baseUnit }} tồn
                    </span>
                    <span v-if="group.hasExpiringBatch" class="badge bg-warning text-dark border border-warning pulse-badge">
                      ⚠️ Có lô sắp hết hạn
                    </span>
                  </div>

                  <!-- Món Ăn Sử Dụng (BOM Recipes) -->
                  <div class="d-flex flex-wrap align-items-center gap-1 mt-1">
                    <span class="small text-muted fw-semibold">🍽️ Món ăn sử dụng:</span>
                    <span v-if="group.usedInFoods && group.usedInFoods.length > 0" class="d-flex flex-wrap gap-1">
                      <span v-for="f in group.usedInFoods" :key="f" class="badge bg-white text-secondary border">
                        {{ f }}
                      </span>
                    </span>
                    <span v-else class="small text-muted fst-italic">Chưa gắn vào công thức món nào</span>
                  </div>
                </div>
              </div>

              <!-- Group Summary Stats & Quick Add -->
              <div class="d-flex align-items-center gap-3">
                <div class="text-end d-none d-md-block">
                  <div class="fw-bold text-dark">{{ group.batches.length }} lô hàng</div>
                  <div class="small text-muted">
                    HSD gần nhất: <span :class="group.hasExpiringBatch ? 'text-danger fw-bold' : 'text-dark'">{{ formatDate(group.earliestExpiry) }}</span>
                  </div>
                </div>
                <button 
                  class="btn btn-sm btn-outline-success fw-bold d-flex align-items-center gap-1 shadow-sm"
                  @click.stop="openCreateBatchModalForGroup(group)"
                >
                  <span>+</span>
                  <span>Lập phiếu nhập</span>
                </button>
              </div>
            </div>

            <!-- Group Details: Sub-table Batches (Expandable) -->
            <div v-if="isGroupExpanded(group.inventoryItemId)" class="border-top p-0 bg-white">
              <div class="table-responsive">
                <table class="table table-sm table-hover align-middle mb-0">
                  <thead class="table-light small text-muted text-uppercase">
                    <tr>
                      <th class="ps-4">Mã Lô Hàng</th>
                      <th>Nhà Cung Cấp</th>
                      <th class="text-end">Tồn Còn Lại / Ban Đầu</th>
                      <th class="text-end">Đơn Giá Vốn</th>
                      <th>Ngày Nhập Kho</th>
                      <th>Hạn Sử Dụng (FEFO)</th>
                      <th class="text-center">Đếm Ngược</th>
                      <th class="text-center pe-4">Trạng Thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="batch in group.batches" :key="batch.batchId" :class="{ 'table-warning': batch.isExpiringSoon && batch.status === 'active' }">
                      <td class="ps-4 font-monospace fw-bold text-dark">
                        <label v-if="isWasteSelectableBatch(batch)" class="batch-select-control d-inline-flex align-items-center gap-2 me-2" :title="`Chọn lô ${batch.batchCode} để hủy`">
                          <input
                            type="checkbox"
                            class="form-check-input mt-0"
                            :checked="isExpiredBatchSelected(batch)"
                            :aria-label="`Chọn hủy lô ${batch.batchCode}`"
                            @click.stop
                            @change="toggleExpiredBatch(batch)"
                          />
                        </label>
                        {{ batch.batchCode }}
                        <span v-if="batch.isKitchenReturn" class="badge bg-warning-subtle text-dark border border-warning ms-1">Hoàn bếp · ưu tiên dùng</span>
                      </td>
                      <td class="small text-muted">
                        {{ batch.supplierName || 'NCC nội bộ' }}
                      </td>
                      <td class="text-end">
                        <div class="fw-bold">Còn {{ formatQuantity(batch.quantityRemaining) }} {{ batch.baseUnit }}</div>
                        <div v-if="batch.isKitchenReturn" class="small text-muted">Hoàn từ bếp {{ formatQuantity(batch.originalQuantity) }} {{ batch.baseUnit }} · dùng trước {{ formatDate(batch.expiryDate) }}</div>
                        <div v-else class="small text-muted">Mua vào {{ formatQuantity(batch.grossReceivedQuantity ?? batch.originalQuantity) }} · thực nhận {{ formatQuantity(batch.actualUsableQuantity ?? batch.originalQuantity) }} {{ batch.baseUnit }}</div>
                        <div class="progress mt-1" style="height: 4px;">
                          <div 
                            class="progress-bar" 
                            :class="getBatchProgressClass(batch)" 
                            :style="{ width: calculateBatchPercent(batch) + '%' }">
                          </div>
                        </div>
                      </td>
                      <td class="text-end fw-semibold">
                        {{ formatCurrency(batch.unitCostPerPurchaseUnit ?? batch.unitCost) }} <small class="text-muted">/{{ batch.purchaseUnit }}</small>
                        <div class="small text-muted">{{ formatCurrency(batch.unitCostPerBaseUnit ?? batch.unitCost) }}/{{ batch.baseUnit }} (quy đổi)</div>
                      </td>
                      <td class="small text-muted">
                        {{ formatDate(batch.receivedDate) }}
                      </td>
                      <td>
                        <strong :class="batch.isExpiringSoon ? 'text-danger' : 'text-dark'">
                          {{ formatDate(batch.expiryDate) }}
                        </strong>
                      </td>
                      <td class="text-center">
                        <span v-if="batch.status === 'depleted'" class="badge bg-secondary text-white">Đã xuất hết</span>
                        <span v-else-if="batch.daysUntilExpiry < 0" class="badge bg-danger text-white">Quá hạn {{ Math.abs(batch.daysUntilExpiry) }} ngày</span>
                        <span v-else-if="batch.daysUntilExpiry <= 7" class="badge bg-warning text-dark border border-warning pulse-badge">
                          ⏳ Còn {{ batch.daysUntilExpiry }} ngày
                        </span>
                        <span v-else class="badge bg-success-subtle text-success border border-success">
                          Còn {{ batch.daysUntilExpiry }} ngày
                        </span>
                      </td>
                      <td class="text-center pe-4">
                        <span v-if="batch.isExpired && batch.quantityRemaining > 0" class="badge bg-danger text-white">Quá hạn · chờ hủy</span>
                        <span v-else-if="batch.status === 'active'" class="badge bg-success text-white">Active</span>
                        <span v-else-if="batch.status === 'depleted'" class="badge bg-secondary text-white">Depleted</span>
                        <span v-else class="badge bg-danger text-white">Expired</span>
                        <button v-if="isWasteSelectableBatch(batch)" type="button" class="btn btn-sm btn-outline-danger d-block mx-auto mt-2" :class="{ active: isExpiredBatchSelected(batch) }" @click.stop="toggleExpiredBatch(batch)">
                          {{ isExpiredBatchSelected(batch) ? 'Đã chọn hủy' : 'Chọn hủy' }}
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>

        <!-- ============================================== -->
        <!-- CHẾ ĐỘ 2: DANH SÁCH PHẲNG TRUYỀN THỐNG (FLAT)  -->
        <!-- ============================================== -->
        <div v-else class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light text-muted small text-uppercase">
              <tr>
                <th>Mã Lô Hàng</th>
                <th>Nguyên Liệu</th>
                <th class="text-end">Tồn Còn Lại / Ban Đầu</th>
                <th class="text-end">Đơn Giá Vốn</th>
                <th>Ngày Nhập Kho</th>
                <th>Hạn Sử Dụng (FEFO)</th>
                <th class="text-center">Đếm Ngược</th>
                <th class="text-center">Trạng Thái</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredBatches.length === 0">
                <td colspan="8" class="text-center text-muted py-4">
                  Không tìm thấy lô hàng nào phù hợp với bộ lọc.
                </td>
              </tr>
              <tr v-for="batch in filteredBatches" :key="batch.batchId" :class="{ 'table-warning': batch.isExpiringSoon && batch.status === 'active' }">
                <td>
                  <label v-if="isWasteSelectableBatch(batch)" class="batch-select-control d-inline-flex align-items-center gap-2 me-2" :title="`Chọn lô ${batch.batchCode} để hủy`">
                    <input
                      type="checkbox"
                      class="form-check-input mt-0"
                      :checked="isExpiredBatchSelected(batch)"
                      :aria-label="`Chọn hủy lô ${batch.batchCode}`"
                      @click.stop
                      @change="toggleExpiredBatch(batch)"
                    />
                  </label>
                  <span class="badge bg-dark-subtle text-dark border font-monospace px-2 py-1">
                    {{ batch.batchCode }}
                  </span>
                  <span v-if="batch.isKitchenReturn" class="badge bg-warning-subtle text-dark border border-warning ms-1">Hoàn bếp · ưu tiên dùng</span>
                  <div class="small text-muted">{{ batch.supplierName || 'NCC nội bộ' }}</div>
                </td>
                <td>
                  <strong class="text-dark">{{ batch.itemName }}</strong>
                </td>
                <td class="text-end">
                  <div class="fw-bold">Còn {{ formatQuantity(batch.quantityRemaining) }} {{ batch.baseUnit }}</div>
                  <div v-if="batch.isKitchenReturn" class="small text-muted">Hoàn từ bếp {{ formatQuantity(batch.originalQuantity) }} {{ batch.baseUnit }} · dùng trước {{ formatDate(batch.expiryDate) }}</div>
                  <div v-else class="small text-muted">Mua vào {{ formatQuantity(batch.grossReceivedQuantity ?? batch.originalQuantity) }} · thực nhận {{ formatQuantity(batch.actualUsableQuantity ?? batch.originalQuantity) }} {{ batch.baseUnit }}</div>
                  <div class="progress mt-1" style="height: 5px;">
                    <div 
                      class="progress-bar" 
                      :class="getBatchProgressClass(batch)" 
                      :style="{ width: calculateBatchPercent(batch) + '%' }">
                    </div>
                  </div>
                </td>
                <td class="text-end fw-semibold">
                  {{ formatCurrency(batch.unitCostPerPurchaseUnit ?? batch.unitCost) }} <small class="text-muted">/{{ batch.purchaseUnit }}</small>
                  <div class="small text-muted">{{ formatCurrency(batch.unitCostPerBaseUnit ?? batch.unitCost) }}/{{ batch.baseUnit }} (quy đổi)</div>
                </td>
                <td class="small text-muted">
                  {{ formatDate(batch.receivedDate) }}
                </td>
                <td>
                  <strong :class="batch.isExpiringSoon ? 'text-danger' : 'text-dark'">
                    {{ formatDate(batch.expiryDate) }}
                  </strong>
                </td>
                <td class="text-center">
                  <span v-if="batch.status === 'depleted'" class="badge bg-secondary text-white">Đã xuất hết</span>
                  <span v-else-if="batch.daysUntilExpiry < 0" class="badge bg-danger text-white">Quá hạn {{ Math.abs(batch.daysUntilExpiry) }} ngày</span>
                  <span v-else-if="batch.daysUntilExpiry <= 7" class="badge bg-warning text-dark border border-warning pulse-badge">
                    ⏳ Còn {{ batch.daysUntilExpiry }} ngày
                  </span>
                  <span v-else class="badge bg-success-subtle text-success border border-success">
                    Còn {{ batch.daysUntilExpiry }} ngày
                  </span>
                </td>
                <td class="text-center">
                  <span v-if="batch.isExpired && batch.quantityRemaining > 0" class="badge bg-danger text-white">Quá hạn · chờ hủy</span>
                  <span v-else-if="batch.status === 'active'" class="badge bg-success text-white">Active</span>
                  <span v-else-if="batch.status === 'depleted'" class="badge bg-secondary text-white">Depleted</span>
                  <span v-else class="badge bg-danger text-white">Expired</span>
                  <button v-if="isWasteSelectableBatch(batch)" type="button" class="btn btn-sm btn-outline-danger d-block mx-auto mt-2" :class="{ active: isExpiredBatchSelected(batch) }" @click.stop="toggleExpiredBatch(batch)">
                    {{ isExpiredBatchSelected(batch) ? 'Đã chọn hủy' : 'Chọn hủy' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- ========================================================================= -->
      <!-- TAB 3: ĐỊNH MỨC SUẤT ĂN (BOM) THEO BIẾN THỂ / SIZE & PHÂN TÍCH MARGIN      -->
      <!-- ========================================================================= -->
      <div v-if="activeTab === 'recipes'" class="p-3 p-md-4">
        <div class="row g-4">
          <!-- Cột Trái: Danh sách Món Ăn -->
          <div class="col-lg-4 col-xl-3 border-end">
            <div class="mb-3">
              <label class="form-label small fw-bold text-muted text-uppercase">Tìm Món Ăn</label>
              <input 
                v-model="foodSearch" 
                type="text" 
                class="form-control form-control-sm" 
                placeholder="🔍 Nhập tên món ăn..." 
              />
            </div>

            <div class="food-recipe-list overflow-auto" style="max-height: 600px;">
              <div 
                v-for="food in filteredFoods" 
                :key="food.foodId"
                class="food-item-card p-2 rounded-3 mb-2 cursor-pointer transition d-flex align-items-center gap-2 border"
                :class="{ 'active-food': selectedFood?.foodId === food.foodId }"
                @click="selectFood(food)"
              >
                <div class="food-avatar-thumb">
                  <img :src="food.imageUrl || 'https://placehold.co/60x60?text=Food'" class="rounded-2" width="44" height="44" style="object-fit: cover;" />
                </div>
                <div class="flex-grow-1 overflow-hidden">
                  <div class="fw-bold text-truncate text-dark" style="font-size: 0.9rem;">{{ food.foodName }}</div>
                  <div class="d-flex justify-content-between align-items-center mt-1">
                    <span class="small text-muted">{{ food.categoryName }}</span>
                    <span class="badge bg-light text-muted border" style="font-size: 0.7rem;">
                      {{ food.variants?.length || 0 }} biến thể
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Cột Phải: Định lượng chi tiết theo từng Biến thể -->
          <div class="col-lg-8 col-xl-9">
            <div v-if="!selectedFood" class="text-center py-5 text-muted">
              <div class="fs-1 mb-2">🥗</div>
              <h5>Chọn một món ăn từ danh sách bên trái để cấu hình định mức BOM.</h5>
              <p class="small text-muted">Định lượng là lượng nguyên liệu sạch thực tế dùng cho một phần. Hao hụt được ghi theo từng lô nhập kho.</p>
            </div>

            <div v-else>
              <!-- Food Header -->
              <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 border-bottom pb-3 mb-3">
                <div>
                  <h4 class="fw-bold mb-1 text-dark">🍽️ {{ selectedFood.foodName }}</h4>
                  <div class="text-muted small">Cấu hình định lượng nguyên liệu cho từng Size / Biến thể của món ăn này.</div>
                </div>
                <div class="d-flex gap-2">
                  <button class="btn btn-sm btn-success fw-bold px-3 d-flex align-items-center gap-1 shadow-sm" @click="saveBom" :disabled="isSavingBom">
                    <span v-if="isSavingBom" class="spinner-border spinner-border-sm"></span>
                    <span>💾 Lưu Định Mức BOM</span>
                  </button>
                </div>
              </div>

              <!-- Variant Selector Tabs (Size / Biến thể) -->
              <div class="mb-4">
                <label class="form-label small fw-bold text-muted text-uppercase mb-2">Chọn Biến Thể / Size Món:</label>
                <div class="d-flex flex-wrap gap-2">
                  <button 
                    v-for="v in currentFoodVariants" 
                    :key="v.variantId"
                    class="btn variant-tab-btn"
                    :class="{ 'active': selectedVariantId === v.variantId }"
                    @click="onSelectVariant(v.variantId)"
                  >
                    <span class="fw-bold">{{ v.variantName }}</span>
                  </button>
                </div>
              </div>

              <!-- Ingredients Table -->
              <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="fw-bold text-dark m-0">Danh Sách Nguyên Liệu Cấu Thành</h6>
                <span class="text-muted small">Lượng nguyên liệu sạch theo BaseUnit</span>
              </div>

              <div class="table-responsive border rounded-3 mb-3">
                <table class="table table-hover align-middle mb-0">
                  <thead class="table-light small text-muted text-uppercase">
                    <tr>
                      <th>Nguyên Liệu</th>
                      <th>Đơn Vị Gốc</th>
                      <th style="width: 160px;">Lượng sạch / phần</th>
                      <th style="width: 140px;">Hao hụt</th>
                      <th style="width: 50px;"></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="editingIngredients.length === 0">
                      <td colspan="5" class="text-center text-muted py-4">
                        Chưa có nguyên liệu nào trong định lượng của biến thể này. Vui lòng thêm nguyên liệu bên dưới.
                      </td>
                    </tr>
                    <tr v-for="(ing, idx) in editingIngredients" :key="ing.inventoryItemId">
                      <td>
                        <strong class="text-dark">{{ ing.itemName }}</strong>
                        <div class="small text-muted" v-if="ing.purchaseUnit && ing.conversionRate > 1">
                          Quy đổi: 1 {{ ing.purchaseUnit }} = {{ ing.conversionRate }} {{ ing.baseUnit }}
                        </div>
                      </td>
                      <td>
                        <span class="badge bg-light text-dark border">{{ ing.baseUnit }}</span>
                      </td>
                      <td>
                        <div class="input-group input-group-sm">
                          <input 
                            v-model.number="ing.quantityPerUnit" 
                            type="number" 
                            step="any" 
                            min="0.0001" 
                            class="form-control text-end fw-bold" 
                            required 
                          />
                          <span class="input-group-text">{{ ing.baseUnit }}</span>
                        </div>
                      </td>
                      <td>
                        <div v-if="canUseWastage(ing.baseUnit)" class="input-group input-group-sm">
                          <span class="small text-muted">Ghi theo từng lô nhập</span>
                        </div>
                        <span v-else class="small text-muted">Không áp dụng</span>
                      </td>
                      <td class="text-center">
                        <button class="btn btn-sm btn-link text-danger p-0" title="Xóa nguyên liệu" @click="removeIngredient(idx)">
                          ✖
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Add Ingredient Row -->
              <div class="card p-3 bg-light border rounded-3">
                <div class="small fw-bold text-muted text-uppercase mb-2">+ Thêm Nguyên Liệu Vào Biến Thể Này:</div>
                <div class="row g-2 align-items-center">
                  <div class="col-md-5">
                    <select v-model="newIngredientItemId" class="form-select form-select-sm">
                      <option value="">-- Chọn nguyên liệu kho --</option>
                      <option v-for="it in availableItemsForBOM" :key="it.inventoryItemId" :value="it.inventoryItemId">
                        {{ it.itemName }} (Tồn: {{ Number(it.currentQuantity).toLocaleString() }} {{ it.baseUnit }})
                      </option>
                    </select>
                  </div>
                  <div class="col-md-3">
                    <div class="input-group input-group-sm">
                      <input 
                        v-model.number="newIngredientQty" 
                        type="number" 
                        step="any" 
                        min="0.0001" 
                        class="form-control" 
                        placeholder="Lượng sạch / phần" 
                      />
                      <span class="input-group-text">{{ selectedNewItemBaseUnit || 'g' }}</span>
                    </div>
                  </div>
                  <div class="col-md-2 small text-muted">Hao hụt khai báo ở lô nhập</div>
                  <div class="col-md-2">
                    <button class="btn btn-sm btn-primary w-100 fw-bold" @click="addIngredientToBOM" :disabled="!newIngredientItemId || newIngredientQty <= 0">
                      Thêm
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ========================================================================= -->
      <!-- TAB 4: NHÀ CUNG CẤP (SUPPLIERS CRUD)                                      -->
      <!-- ========================================================================= -->
      <div v-if="activeTab === 'suppliers'" class="p-3 p-md-4">
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
          <input 
            v-model="supplierSearch" 
            type="text" 
            class="form-control form-control-sm" 
            placeholder="🔍 Tìm nhà cung cấp theo tên, số điện thoại, email..."
            style="max-width: 350px;"
          />
          <button class="btn btn-sm btn-success fw-bold" @click="openSupplierModal()">
            ➕ Thêm Nhà Cung Cấp
          </button>
        </div>

        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light text-muted small text-uppercase">
              <tr>
                <th style="width: 60px;">Mã</th>
                <th>Tên Nhà Cung Cấp</th>
                <th>Số Điện Thoại</th>
                <th>Email</th>
                <th>Địa Chỉ</th>
                <th class="text-center">Trạng Thái</th>
                <th class="text-center" style="width: 120px;">Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredSuppliers.length === 0">
                <td colspan="7" class="text-center text-muted py-4">
                  Không tìm thấy nhà cung cấp nào.
                </td>
              </tr>
              <tr v-for="sup in filteredSuppliers" :key="sup.supplierId">
                <td class="fw-bold text-muted">#{{ sup.supplierId }}</td>
                <td>
                  <strong class="text-dark">{{ sup.supplierName }}</strong>
                </td>
                <td>{{ sup.phone || '—' }}</td>
                <td>{{ sup.email || '—' }}</td>
                <td class="text-muted small">{{ sup.address || '—' }}</td>
                <td class="text-center">
                  <span v-if="sup.isActive" class="badge bg-success-subtle text-success border border-success">Đang hợp tác</span>
                  <span v-else class="badge bg-secondary text-white">Ngừng hợp tác</span>
                </td>
                <td class="text-center">
                  <div class="d-flex justify-content-center gap-1">
                    <button class="btn btn-sm btn-light border text-primary" title="Chỉnh sửa" @click="openSupplierModal(sup)">
                      ✏️
                    </button>
                    <button class="btn btn-sm btn-light border text-danger" title="Vô hiệu hóa" @click="deleteSupplier(sup)">
                      🗑️
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 1: THÊM / SỬA NGUYÊN LIỆU (FLEXIBLE UNIT & OPTIONAL INITIAL BATCH)  -->
    <!-- ========================================================================= -->
    <div v-if="showItemModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern item-modal-panel bg-white shadow-lg">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <h5 class="fw-bold mb-0 text-dark">
            {{ editingItem ? '✏️ Chỉnh Sửa Nguyên Liệu' : '➕ Thêm Nguyên Liệu Mới' }}
          </h5>
          <button class="btn-close" @click="showItemModal = false"></button>
        </div>

        <form class="item-modal-form" @submit.prevent="saveItem">
          <div class="item-modal-scroll">
          <div class="mb-3">
            <label class="form-label small fw-bold">Tên nguyên liệu thực phẩm <span class="text-danger">*</span></label>
            <input v-model="itemForm.itemName" type="text" class="form-control" placeholder="VD: Tôm Sú Sống, Thịt Bò Mỹ, Nấm Kim Châm, Muối..." required />
          </div>

          <div class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Nhà cung cấp đối tác</label>
              <select v-model="itemForm.supplierId" class="form-select">
                <option :value="null">-- Không chọn / Mua lẻ --</option>
                <option v-for="sup in suppliers" :key="sup.supplierId" :value="sup.supplierId">
                  {{ sup.supplierName }}
                </option>
              </select>
            </div>
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Trạng thái hoạt động</label>
              <select v-model="itemForm.isActive" class="form-select">
                <option :value="true">Hoạt động (Active)</option>
                <option :value="false">Tạm dừng (Inactive)</option>
              </select>
            </div>
          </div>

          <!-- YÊU CẦU 1: TOGGLE QUY ĐỔI ĐƠN VỊ TÍNH (MẶC ĐỊNH LÀ ĐƠN VỊ ĐƠN GIẢN) -->
          <div class="p-3 bg-light rounded-3 border mb-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <div class="small fw-bold text-dark">⚖️ Đơn vị tính:</div>
              <div class="form-check form-switch m-0">
                <input class="form-check-input" type="checkbox" id="conversionToggle" v-model="itemForm.enableConversion" />
                <label class="form-check-label small fw-bold text-primary" for="conversionToggle">
                  Áp dụng quy đổi đơn vị mua / gốc
                </label>
              </div>
            </div>

            <!-- Trường hợp 1: Không quy đổi (Đơn vị tính đơn giản) -->
            <div v-if="!itemForm.enableConversion" class="row g-2 align-items-center">
              <div class="col-sm-7">
                <label class="form-label small text-muted">Đơn vị tính sử dụng</label>
                <input 
                  v-model="itemForm.singleUnit" 
                  type="text" 
                  class="form-control form-control-sm" 
                  placeholder="kg, g, con, gói, chai, lon, quả..." 
                  required 
                />
              </div>
              <div class="col-sm-5">
                <label class="form-label small text-muted">Gợi ý nhanh</label>
                <div class="d-flex flex-wrap gap-1">
                  <span 
                    v-for="u in ['kg', 'g', 'con', 'gói', 'chai', 'lon', 'hộp']" 
                    :key="u" 
                    class="badge bg-white text-dark border cursor-pointer hover-badge"
                    @click="itemForm.singleUnit = u"
                  >
                    {{ u }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Trường hợp 2: Có quy đổi đơn vị (Dual Unit) -->
            <div v-else>
              <div class="row g-2 align-items-center">
                <div class="col-sm-4">
                  <label class="form-label small text-muted">Đơn vị mua (Purchase)</label>
                  <input v-model="itemForm.purchaseUnit" type="text" class="form-control form-control-sm" placeholder="thùng, kg..." required />
                </div>
                <div class="col-sm-4">
                  <label class="form-label small text-muted">Tỷ lệ quy đổi</label>
                  <input v-model.number="itemForm.conversionRate" type="number" step="any" min="0.001" class="form-control form-control-sm text-end" required />
                </div>
                <div class="col-sm-4">
                  <label class="form-label small text-muted">Đơn vị gốc (Base)</label>
                  <input v-model="itemForm.baseUnit" type="text" class="form-control form-control-sm" placeholder="lon, g, ml..." required />
                </div>
              </div>
              <div class="small text-muted mt-2">
                Ví dụ: 1 <strong>{{ itemForm.purchaseUnit || 'thùng' }}</strong> = <strong>{{ Number(itemForm.conversionRate || 24).toLocaleString() }}</strong> <strong>{{ itemForm.baseUnit || 'lon' }}</strong>.
              </div>
            </div>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Ngưỡng tối thiểu cảnh báo ({{ activeItemUnit }})</label>
              <input v-model.number="itemForm.minimumQuantity" type="number" step="any" min="0" class="form-control" required />
            </div>
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Giá vốn chuẩn (VNĐ/{{ activePurchaseUnit }})</label>
              <input v-model.number="itemForm.costPerUnit" type="number" step="any" min="0" class="form-control" required />
            </div>
          </div>
          <div class="form-text mb-3">
            Dùng để tính BOM và làm giá mặc định cho phiếu nhập mới. Giá các lô đã nhập vẫn giữ nguyên theo lịch sử.
          </div>

          <div class="p-3 bg-light-subtle rounded-3 border mb-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <label class="form-label fw-bold text-dark d-flex align-items-center gap-2 mb-0" for="prepWastagePercent">
                <span>📉 Tỷ lệ hao hụt sơ chế mặc định (%)</span>
                <span class="help-tooltip" tabindex="0">
                  <button class="help-tooltip-trigger" type="button" aria-label="Giải thích hao hụt sơ chế">?</button>
                  <span class="help-tooltip-panel" role="tooltip">
                    <strong>Hao hụt sơ chế là gì?</strong>
                    <span>Phần nguyên liệu mất đi khi rửa, lặt, gọt vỏ, lọc xương, bỏ đầu/ruột hoặc làm sạch. Tỷ lệ này là mức dự kiến để hệ thống tự động điền và tính toán lượng sạch khi lập phiếu nhập hàng.</span>
                    <span class="help-tooltip-example"><b>Ví dụ:</b> Mua 10 kg rau, dự kiến hao hụt 20% thì lượng sạch dự kiến là 8 kg. Khi nhập hàng, bạn có thể cân thực tế và ghi nhận đúng số lượng dùng được.</span>
                    <span><b>Mức tham khảo:</b> Rau lá: 15–25% | Củ quả: 10–20% | Thịt nạc/lọc: 10–20% | Gà/vịt nguyên con: 30–35% | Cá nguyên con: 20–30% | Tôm cua: 35–45%.</span>
                  </span>
                </span>
              </label>
              <span class="badge bg-warning-subtle text-dark border border-warning-subtle fw-semibold">
                {{ Number(itemForm.prepWastagePercent || 0) }}% hao hụt
              </span>
            </div>

            <div class="row g-2 align-items-center mb-2">
              <div class="col-sm-5 col-md-4">
                <div class="input-group">
                  <input id="prepWastagePercent" v-model.number="itemForm.prepWastagePercent" type="number" min="0" max="99.9" step="0.1" class="form-control fw-bold text-center" placeholder="0" />
                  <span class="input-group-text fw-bold">%</span>
                </div>
              </div>
              <div class="col-sm-7 col-md-8">
                <div class="d-flex flex-wrap gap-1 align-items-center">
                  <small class="text-muted me-1">Chọn nhanh:</small>
                  <button 
                    v-for="rate in [0, 5, 10, 15, 20, 25, 30]" 
                    :key="rate" 
                    type="button" 
                    class="btn btn-xs btn-outline-secondary py-0 px-2"
                    :class="{ 'btn-success text-white border-success': Number(itemForm.prepWastagePercent) === rate }"
                    @click="itemForm.prepWastagePercent = rate"
                  >
                    {{ rate }}%
                  </button>
                </div>
              </div>
            </div>

            <div class="small p-2 bg-white rounded border text-muted">
              <div class="d-flex align-items-center gap-1 text-primary fw-semibold mb-1">
                <span>💡 Trích xuất khi nhập hàng:</span>
              </div>
              <div v-if="Number(itemForm.prepWastagePercent) > 0">
                Khi nhập <strong>10 {{ activePurchaseUnit }}</strong> thô ➔ Lượng sạch dùng được dự kiến: <strong>{{ (10 * (1 - (Number(itemForm.prepWastagePercent) || 0) / 100)).toFixed(2) }} {{ activePurchaseUnit }}</strong> (Hao hụt sơ chế bỏ đi: {{ (10 * (Number(itemForm.prepWastagePercent) || 0) / 100).toFixed(2) }} {{ activePurchaseUnit }}).
              </div>
              <div v-else>
                Nguyên liệu không có hao hụt (100% sử dụng được). Lượng mua vào bằng đúng lượng nhập kho sạch.
              </div>
            </div>
          </div>

          <!-- Tồn đầu kỳ cũng được ghi nhận qua phiếu nhập chờ duyệt. -->
          <div v-if="!editingItem" class="p-3 bg-light rounded-3 border mb-3">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <div class="small fw-bold text-dark">🏷️ Tạo phiếu nhập tồn đầu kỳ:</div>
              <div class="form-check form-switch m-0">
                <input class="form-check-input" type="checkbox" id="initialBatchToggle" v-model="itemForm.enableInitialBatch" />
                <label class="form-check-label small fw-bold text-warning-emphasis" for="initialBatchToggle">
                  Tạo phiếu nhập đầu kỳ
                </label>
              </div>
            </div>

            <!-- Chỉ hiện thông tin phiếu nhập đầu kỳ khi bật switch -->
            <div v-if="itemForm.enableInitialBatch" class="row g-2 mt-2 pt-2 border-top">
              <div class="col-sm-6">
                <label class="form-label small text-muted">Số lượng tồn ban đầu ({{ activePurchaseUnit }})</label>
                <input v-model.number="itemForm.initialQuantity" type="number" step="any" min="0.0001" class="form-control form-control-sm" required />
                <div class="form-text">Dự kiến dùng được: {{ formatQuantity(initialExpectedUsable) }} {{ activeItemUnit }} · thực nhận: {{ formatQuantity(initialActualUsable) }} {{ activeItemUnit }}</div>
              </div>
              <div class="col-sm-6">
                <label class="form-label small text-muted">Hạn sử dụng của lô đầu kỳ</label>
                <input v-model="itemForm.initialExpiryDate" type="date" class="form-control form-control-sm" required />
              </div>
              <div class="col-sm-6" v-if="Number(itemForm.prepWastagePercent) > 0">
                <label class="form-label small text-muted">Hao hụt sơ chế dự kiến (%)</label>
                <input :value="itemForm.prepWastagePercent" type="number" min="0" max="99.9" step="0.1" class="form-control form-control-sm" disabled />
              </div>
              <div class="col-sm-6" v-if="Number(itemForm.prepWastagePercent) > 0">
                <label class="form-label small text-muted">Thực nhận sau sơ chế ({{ activeItemUnit }})</label>
                <input v-model.number="itemForm.initialActualUsableQuantity" type="number" min="0.0001" step="any" class="form-control form-control-sm" :placeholder="formatQuantity(initialExpectedUsable)" />
                <div class="form-text">Để trống nếu thực nhận đúng bằng dự kiến: {{ formatQuantity(initialExpectedUsable) }} {{ activeItemUnit }}.</div>
              </div>
              <div class="col-12" v-if="initialNeedsReason">
                <label class="form-label small text-danger">Lý do hao hụt vượt dự kiến <span class="text-danger">*</span></label>
                <input v-model="itemForm.initialAdjustmentNote" type="text" maxlength="500" class="form-control form-control-sm" required placeholder="VD: loại bỏ phần dập, hư hỏng..." />
              </div>
            </div>
            <div v-else class="small text-muted">
              Nguyên liệu sẽ được tạo với số lượng tồn kho = 0. Bạn có thể nhập kho sau qua chức năng Phiếu Kho.
            </div>
          </div>

          </div>
          <div class="item-modal-footer d-flex justify-content-end gap-2">
            <button type="button" class="btn btn-sm btn-light border" @click="showItemModal = false">Hủy bỏ</button>
            <button type="submit" class="btn btn-sm btn-success fw-bold px-3">
              {{ editingItem ? 'Lưu Thay Đổi' : 'Tạo Nguyên Liệu' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 2: TẠO LÔ HÀNG MỚI (FEFO BATCH CREATION)                            -->
    <!-- ========================================================================= -->
    <div v-if="showBatchModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern batch-modal-panel bg-white shadow-lg">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <h5 class="fw-bold mb-0 text-dark">🏷️ Lập Phiếu Nhập Kho</h5>
          <button class="btn-close" @click="showBatchModal = false"></button>
        </div>

        <form class="batch-modal-form" @submit.prevent="saveBatch">
          <div class="batch-modal-body">
          <div class="alert alert-warning py-2 small">
            Phiếu nhập sẽ ở trạng thái <strong>Chờ duyệt</strong>. Tồn kho chỉ tăng sau khi người có quyền duyệt phiếu.
          </div>
          <div class="mb-3">
            <label class="form-label small fw-bold">Chọn nguyên liệu <span class="text-danger">*</span></label>
            <select v-model="batchForm.inventoryItemId" class="form-select" required @change="onBatchItemSelect">
              <option value="">-- Chọn nguyên liệu nhập lô --</option>
              <option v-for="it in items" :key="it.inventoryItemId" :value="it.inventoryItemId">
                {{ it.itemName }} (mua: {{ it.purchaseUnit }}, tồn: {{ it.baseUnit }}, hao hụt: {{ formatWastagePercent(it.prepWastagePercent) }}%)
              </option>
            </select>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Mã Lô (Để trống tự sinh)</label>
              <input v-model="batchForm.batchCode" type="text" class="form-control" placeholder="LOT-..." />
            </div>
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Nhà cung cấp lô</label>
              <select v-model="batchForm.supplierId" class="form-select">
                <option :value="null">-- Theo nguyên liệu --</option>
                <option v-for="sup in suppliers" :key="sup.supplierId" :value="sup.supplierId">
                  {{ sup.supplierName }}
                </option>
              </select>
            </div>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Số lượng nhập ({{ selectedBatchPurchaseUnit || 'Đơn vị mua' }}) <span class="text-danger">*</span></label>
              <input v-model.number="batchForm.quantity" type="number" step="any" min="0.0001" class="form-control" required />
            </div>
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Giá mua thực tế (VNĐ/{{ selectedBatchPurchaseUnit || 'Đơn vị mua' }})</label>
              <input v-model.number="batchForm.unitCost" type="number" step="any" min="0" class="form-control" required />
            </div>
          </div>

          <div v-if="selectedBatchBaseUnit" class="small text-muted mb-3">
            Lượng mua quy đổi: <strong>{{ formatQuantity(selectedBatchBaseQuantity) }} {{ selectedBatchBaseUnit }}</strong>
            ({{ formatQuantity(selectedBatchConversionRate) }} {{ selectedBatchBaseUnit }}/{{ selectedBatchPurchaseUnit }}).
            Tồn tăng sau duyệt theo lượng thực nhận sau sơ chế.
          </div>

          <section v-if="selectedBatchBaseUnit" class="batch-wastage-summary mb-3" aria-live="polite">
            <div class="batch-wastage-heading">
              <div>
                <strong>Kiểm soát hao hụt sơ chế</strong>
                <span>Tỷ lệ mặc định từ danh mục nguyên liệu</span>
              </div>
              <span class="batch-rate-badge" :title="`Tỷ lệ hao hụt đã lưu của ${selectedBatchItemName || 'nguyên liệu'}`">
                {{ formatWastagePercent(selectedBatchWastagePercent) }}%
              </span>
            </div>
            <div class="batch-wastage-grid">
              <div><span>Mua vào</span><strong>{{ formatQuantity(selectedBatchBaseQuantity) }} {{ selectedBatchBaseUnit }}</strong></div>
              <div><span>Hao hụt dự kiến</span><strong>{{ formatQuantity(selectedBatchWasteQuantity) }} {{ selectedBatchBaseUnit }}</strong></div>
              <div><span>Thực nhận</span><strong>{{ formatQuantity(selectedBatchActualUsable) }} {{ selectedBatchBaseUnit }}</strong></div>
              <div :class="selectedBatchWasteVariance > 0.0001 ? 'is-over' : 'is-within'">
                <span>Hao hụt thực tế</span>
                <strong>{{ formatQuantity(selectedBatchActualWaste) }} {{ selectedBatchBaseUnit }}</strong>
                <small>{{ selectedBatchWasteVariance > 0.0001 ? `Vượt ${formatQuantity(selectedBatchWasteVariance)} ${selectedBatchBaseUnit}` : 'Trong định mức' }}</small>
              </div>
            </div>
            <div class="batch-cost-note">Giá vốn sau sơ chế: <strong>{{ formatCurrency(selectedBatchEffectiveCost) }}/{{ selectedBatchPurchaseUnit }}</strong></div>
            <div class="batch-rate-note">Tồn kho chỉ tăng theo số thực nhận sau khi phiếu được duyệt. Muốn đổi tỷ lệ mặc định, sửa tại danh mục nguyên liệu.</div>
          </section>

          <div v-if="Number(selectedBatchWastagePercent) > 0 || batchForm.actualUsableQuantity" class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Thực nhận sau sơ chế ({{ selectedBatchBaseUnit }})</label>
              <input v-model.number="batchForm.actualUsableQuantity" type="number" min="0.0001" step="any" class="form-control" :placeholder="formatQuantity(selectedBatchNetQuantity)" />
            </div>
            <div v-if="selectedBatchNeedsReason" class="col-sm-6">
              <label class="form-label small fw-bold text-danger">Lý do hao hụt vượt dự kiến <span class="text-danger">*</span></label>
              <input v-model="batchForm.adjustmentNote" type="text" maxlength="500" class="form-control" required placeholder="Nêu lý do điều chỉnh" />
            </div>
          </div>

          <div class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Ngày sản xuất (NSX)</label>
              <input v-model="batchForm.manufacturingDate" type="date" class="form-control" />
            </div>
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Hạn sử dụng (HSD - Bắt buộc) <span class="text-danger">*</span></label>
              <input v-model="batchForm.expiryDate" type="date" class="form-control" required />
            </div>
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Ghi chú lô hàng</label>
            <input v-model="batchForm.note" type="text" class="form-control" placeholder="VD: Nhập thêm cuối tuần, lô tươi mới..." />
          </div>

          </div>
          <div class="batch-modal-footer d-flex justify-content-end gap-2">
            <button type="button" class="btn btn-sm btn-light border" @click="showBatchModal = false">Hủy bỏ</button>
            <button type="submit" class="btn btn-sm btn-warning fw-bold px-3" v-text="'Tạo phiếu chờ duyệt'"></button>
          </div>
        </form>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 3: XEM CÁC LÔ CỦA MỘT NGUYÊN LIỆU CỤ THỂ                            -->
    <!-- ========================================================================= -->
    <div v-if="showItemBatchesModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern bg-white p-4 shadow-lg" style="max-width: 800px; width: 100%;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <div>
            <h5 class="fw-bold mb-0 text-dark">🏷️ Danh Sách Lô Hàng: {{ viewingItem?.itemName }}</h5>
            <div class="small text-muted">Sắp xếp theo thứ tự ưu tiên xuất kho FEFO (Hạn dùng gần nhất)</div>
          </div>
          <button class="btn-close" @click="showItemBatchesModal = false"></button>
        </div>

        <div class="table-responsive">
          <table class="table table-sm table-hover align-middle mb-0">
            <thead class="table-light small text-muted text-uppercase">
              <tr>
                <th>Mã Lô</th>
                <th class="text-end">Tồn Còn / Ban Đầu</th>
                <th class="text-end">Giá Vốn</th>
                <th>Ngày Nhập</th>
                <th>Hạn Sử Dụng</th>
                <th class="text-center">Đếm Ngược</th>
                <th class="text-center">Trạng Thái</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="itemSpecificBatches.length === 0">
                <td colspan="7" class="text-center text-muted py-4">
                  Chưa có lô hàng nào cho nguyên liệu này.
                </td>
              </tr>
              <tr v-for="b in itemSpecificBatches" :key="b.batchId">
                <td class="font-monospace fw-bold">{{ b.batchCode }}<span v-if="b.isKitchenReturn" class="badge bg-warning-subtle text-dark border border-warning ms-1">Hoàn bếp · ưu tiên dùng</span></td>
                <td class="text-end">
                  <div>Còn {{ formatQuantity(b.quantityRemaining) }} {{ viewingItem?.baseUnit }}</div>
                  <div v-if="b.isKitchenReturn" class="small text-muted">Hoàn từ bếp {{ formatQuantity(b.originalQuantity) }} {{ viewingItem?.baseUnit }} · dùng trước {{ formatDate(b.expiryDate) }}</div>
                  <div v-else class="small text-muted">Mua vào {{ formatQuantity(b.grossReceivedQuantity ?? b.originalQuantity) }} · thực nhận {{ formatQuantity(b.actualUsableQuantity ?? b.originalQuantity) }}</div>
                </td>
                <td class="text-end">
                  {{ formatCurrency(b.unitCostPerPurchaseUnit ?? b.unitCost) }}/{{ viewingItem?.purchaseUnit }}
                  <div class="small text-muted">{{ formatCurrency(b.unitCostPerBaseUnit ?? b.unitCost) }}/{{ viewingItem?.baseUnit }}</div>
                </td>
                <td class="small text-muted">{{ formatDate(b.receivedDate) }}</td>
                <td class="fw-bold" :class="b.isExpiringSoon ? 'text-danger' : 'text-dark'">
                  {{ formatDate(b.expiryDate) }}
                </td>
                <td class="text-center">
                  <span v-if="b.daysUntilExpiry <= 7" class="badge bg-warning text-dark">Còn {{ b.daysUntilExpiry }} ngày</span>
                  <span v-else class="badge bg-success-subtle text-success">Còn {{ b.daysUntilExpiry }} ngày</span>
                </td>
                <td class="text-center">
                  <span v-if="b.isExpired && b.quantityRemaining > 0" class="badge bg-danger">Quá hạn · chờ hủy</span>
                  <span v-else class="badge" :class="b.status === 'active' ? 'bg-success' : 'bg-secondary'">{{ b.status }}</span>
                  <button v-if="isWasteSelectableBatch(b)" type="button" class="btn btn-sm btn-outline-danger d-block mx-auto mt-2" :class="{ active: isExpiredBatchSelected(b) }" @click.stop="toggleExpiredBatch(b)">
                    {{ isExpiredBatchSelected(b) ? 'Đã chọn hủy' : 'Chọn hủy' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3 mt-3">
          <button class="btn btn-sm btn-outline-secondary" @click="showItemBatchesModal = false">Đóng</button>
          <button class="btn btn-sm btn-warning fw-bold" @click="showItemBatchesModal = false; openCreateBatchModal(viewingItem)">Lập phiếu nhập</button>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL 4: THÊM / SỬA NHÀ CUNG CẤP                                          -->
    <!-- ========================================================================= -->
    <div v-if="showSupplierModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center p-3">
      <div class="card-modern bg-white p-4 shadow-lg" style="max-width: 500px; width: 100%;">
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3">
          <h5 class="fw-bold mb-0 text-dark">
            {{ editingSupplier ? '✏️ Chỉnh Sửa Nhà Cung Cấp' : '➕ Thêm Nhà Cung Cấp Mới' }}
          </h5>
          <button class="btn-close" @click="showSupplierModal = false"></button>
        </div>

        <form @submit.prevent="saveSupplier">
          <div class="mb-3">
            <label class="form-label small fw-bold">Tên nhà cung cấp <span class="text-danger">*</span></label>
            <input v-model="supplierForm.supplierName" type="text" class="form-control" placeholder="VD: Vựa Hải Sản Phan Thiết..." required />
          </div>

          <div class="row g-2 mb-3">
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Số điện thoại</label>
              <input v-model="supplierForm.phone" type="text" class="form-control" placeholder="0987..." />
            </div>
            <div class="col-sm-6">
              <label class="form-label small fw-bold">Email</label>
              <input v-model="supplierForm.email" type="email" class="form-control" placeholder="contact@..." />
            </div>
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Địa chỉ</label>
            <input v-model="supplierForm.address" type="text" class="form-control" placeholder="Địa chỉ kho hàng, cảng..." />
          </div>

          <div class="d-flex justify-content-end gap-2 border-top pt-3">
            <button type="button" class="btn btn-sm btn-light border" @click="showSupplierModal = false">Hủy bỏ</button>
            <button type="submit" class="btn btn-sm btn-success fw-bold px-3">
              {{ editingSupplier ? 'Cập Nhật' : 'Thêm Nhà Cung Cấp' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';
import { signalrService } from '@/services/signalr.js';

const API_BASE = 'http://localhost:5079/api/admin/inventory';
const RECEIPTS_API = `${API_BASE}/receipts`;
const router = useRouter();
const activeTab = ref('items');

// 1. KPI Summary
const summary = ref({
  totalItems: 0,
  lowStockCount: 0,
  totalBatches: 0,
  totalSuppliers: 0,
  totalValue: 0,
  expiredBatchesCount: 0,
  expiringBatchesCount: 0
});

// 2. Tab Items
const items = ref([]);
const itemSearch = ref('');
const selectedSupplierFilter = ref('');
const onlyLowStockFilter = ref(false);

const filteredItems = computed(() => {
  return items.value.filter(item => {
    const matchSearch = !itemSearch.value || 
      item.itemName.toLowerCase().includes(itemSearch.value.toLowerCase()) ||
      item.baseUnit.toLowerCase().includes(itemSearch.value.toLowerCase()) ||
      item.purchaseUnit.toLowerCase().includes(itemSearch.value.toLowerCase());

    const matchSupplier = !selectedSupplierFilter.value || item.supplierId === selectedSupplierFilter.value;
    const matchLowStock = !onlyLowStockFilter.value || item.isLowStock;

    return matchSearch && matchSupplier && matchLowStock;
  });
});

function hasUnitConversion(item) {
  return item && item.conversionRate > 1 && item.purchaseUnit && item.purchaseUnit.toLowerCase() !== item.baseUnit.toLowerCase();
}

function filterOnlyLowStock() {
  activeTab.value = 'items';
  onlyLowStockFilter.value = true;
}

function filterExpiredBatches() {
  activeTab.value = 'batches';
  batchFilterStatus.value = 'expired';
  batchFilterExpiry.value = 'all';
  batchViewMode.value = 'flat';
}

// 3. Tab Batches (Grouped & Flat view)
const batches = ref([]);
const batchSearch = ref('');
const batchFilterStatus = ref('all');
const batchFilterExpiry = ref('all');
const batchViewMode = ref('grouped'); // 'grouped' | 'flat'
const expandedGroupIds = ref(new Set());
const selectedExpiredBatchIds = ref([]);
const isWastingExpired = ref(false);
const batchActionMessage = ref('');
const batchActionError = ref('');

const selectedExpiredBatches = computed(() => {
  const selectedIds = new Set(selectedExpiredBatchIds.value.map(id => String(id)));
  return batches.value.filter(batch => selectedIds.has(String(batch.batchId)) && isWasteSelectableBatch(batch));
});

const selectableExpiredBatches = computed(() => batches.value.filter(isWasteSelectableBatch));

const selectedExpiredItemCount = computed(() => {
  return new Set(selectedExpiredBatches.value.map(batch => batch.inventoryItemId)).size;
});

function isWasteSelectableBatch(batch) {
  return Boolean(batch?.isExpired && Number(batch.quantityRemaining) > 0);
}

function isExpiredBatchSelected(batch) {
  return selectedExpiredBatchIds.value.includes(String(batch.batchId));
}

function toggleExpiredBatch(batch) {
  if (!isWasteSelectableBatch(batch)) return;
  const id = String(batch.batchId);
  selectedExpiredBatchIds.value = isExpiredBatchSelected(batch)
    ? selectedExpiredBatchIds.value.filter(selectedId => selectedId !== id)
    : [...selectedExpiredBatchIds.value, id];
  batchActionError.value = '';
}

function clearExpiredBatchSelection() {
  selectedExpiredBatchIds.value = [];
}

function selectAllExpiredBatches() {
  selectedExpiredBatchIds.value = selectableExpiredBatches.value.map(batch => String(batch.batchId));
  batchActionError.value = '';
}

async function confirmSelectedExpiredWaste() {
  const selected = selectedExpiredBatches.value;
  if (selected.length === 0 || isWastingExpired.value) return;

  const summaryLines = selected.map(batch =>
    `- ${batch.itemName} · ${batch.batchCode}: ${formatQuantity(batch.quantityRemaining)} ${batch.baseUnit}`
  ).join('\n');
  const confirmed = window.confirm(
    `Xác nhận hủy ${selected.length} lô quá hạn?\n\n${summaryLines}\n\nHệ thống sẽ tạo 1 phiếu xuất hủy và trừ toàn bộ số tồn của các lô này.`
  );
  if (!confirmed) return;

  isWastingExpired.value = true;
  batchActionError.value = '';
  batchActionMessage.value = '';
  try {
    const response = await axios.post(`${RECEIPTS_API}/quick-waste`, {
      note: `Hủy ${selected.length} lô nguyên liệu quá hạn ngày ${formatDate(new Date())}`,
      items: selected.map(batch => ({
        inventoryItemId: batch.inventoryItemId,
        batchId: batch.batchId,
        quantity: Number(batch.quantityRemaining),
        reason: 'Hết hạn'
      }))
    });

    selectedExpiredBatchIds.value = [];
    const receiptCode = response.data?.receiptCode ? `Mã phiếu: ${response.data.receiptCode}.` : '';
    batchActionMessage.value = `${response.data?.message || 'Phiếu xuất hủy đã được ghi nhận.'} ${receiptCode}`.trim();
    await Promise.all([fetchInventoryData(), fetchBatches()]);
  } catch (err) {
    batchActionError.value = err.response?.data?.message || err.message || 'Không thể tạo phiếu xuất hủy.';
  } finally {
    isWastingExpired.value = false;
  }
}

function toggleGroupExpand(itemId) {
  if (expandedGroupIds.value.has(itemId)) {
    expandedGroupIds.value.delete(itemId);
  } else {
    expandedGroupIds.value.add(itemId);
  }
}

function isGroupExpanded(itemId) {
  return expandedGroupIds.value.has(itemId);
}

// Map liên kết giữa nguyên liệu và các món ăn sử dụng (BOM Recipe Map)
const itemUsedInFoodsMap = computed(() => {
  const map = {};
  for (const food of foods.value) {
    if (food.variants) {
      for (const variant of food.variants) {
        if (variant.ingredients) {
          for (const ing of variant.ingredients) {
            if (!map[ing.inventoryItemId]) {
              map[ing.inventoryItemId] = new Set();
            }
            map[ing.inventoryItemId].add(food.foodName);
          }
        }
      }
    }
  }

  // Convert Set to Array
  const result = {};
  for (const id in map) {
    result[id] = Array.from(map[id]);
  }
  return result;
});

// Gom nhóm các Lô hàng theo Nguyên Liệu
const groupedBatchesByItem = computed(() => {
  // Lấy các lô phù hợp với bộ lọc status và expiry trước
  const filtered = batches.value.filter(b => {
    const matchStatus = batchFilterStatus.value === 'all' || b.status.toLowerCase() === batchFilterStatus.value.toLowerCase();

    let matchExpiry = true;
    if (batchFilterExpiry.value !== 'all') {
      const days = parseInt(batchFilterExpiry.value, 10);
      matchExpiry = b.daysUntilExpiry <= days && b.status === 'active';
    }

    return matchStatus && matchExpiry;
  });

  // Gom theo InventoryItemId
  const groups = {};
  for (const b of filtered) {
    if (!groups[b.inventoryItemId]) {
      groups[b.inventoryItemId] = {
        inventoryItemId: b.inventoryItemId,
        itemName: b.itemName,
        baseUnit: b.baseUnit,
        purchaseUnit: b.purchaseUnit,
        conversionRate: b.conversionRate,
        batches: [],
        totalRemaining: 0,
        hasExpiringBatch: false,
        earliestExpiry: null,
        usedInFoods: itemUsedInFoodsMap.value[b.inventoryItemId] || []
      };
    }
    groups[b.inventoryItemId].batches.push(b);
    groups[b.inventoryItemId].totalRemaining += b.quantityRemaining;
    if (b.isExpiringSoon && b.status === 'active') {
      groups[b.inventoryItemId].hasExpiringBatch = true;
    }
    if (!groups[b.inventoryItemId].earliestExpiry || new Date(b.expiryDate) < new Date(groups[b.inventoryItemId].earliestExpiry)) {
      groups[b.inventoryItemId].earliestExpiry = b.expiryDate;
    }
  }

  // Filter theo search keyword
  let result = Object.values(groups);
  if (batchSearch.value) {
    const q = batchSearch.value.toLowerCase();
    result = result.filter(g => {
      const matchName = g.itemName.toLowerCase().includes(q);
      const matchBatch = g.batches.some(b => b.batchCode.toLowerCase().includes(q));
      const matchFood = g.usedInFoods.some(f => f.toLowerCase().includes(q));
      return matchName || matchBatch || matchFood;
    });
  }

  // Sắp xếp các nhóm: nguyên liệu có lô cận hạn nhất lên đầu
  result.sort((a, b) => {
    if (a.hasExpiringBatch && !b.hasExpiringBatch) return -1;
    if (!a.hasExpiringBatch && b.hasExpiringBatch) return 1;
    return (new Date(a.earliestExpiry || '2099-01-01')) - (new Date(b.earliestExpiry || '2099-01-01'));
  });

  return result;
});

const filteredBatches = computed(() => {
  return batches.value.filter(b => {
    const matchSearch = !batchSearch.value || 
      b.batchCode.toLowerCase().includes(batchSearch.value.toLowerCase()) ||
      b.itemName.toLowerCase().includes(batchSearch.value.toLowerCase());

    const matchStatus = batchFilterStatus.value === 'all' || b.status.toLowerCase() === batchFilterStatus.value.toLowerCase();

    let matchExpiry = true;
    if (batchFilterExpiry.value !== 'all') {
      const days = parseInt(batchFilterExpiry.value, 10);
      matchExpiry = b.daysUntilExpiry <= days && b.status === 'active';
    }

    return matchSearch && matchStatus && matchExpiry;
  });
});

function calculateBatchPercent(batch) {
  if (!batch.originalQuantity || batch.originalQuantity <= 0) return 0;
  return Math.min(100, Math.max(0, Math.round((batch.quantityRemaining / batch.originalQuantity) * 100)));
}

function getBatchProgressClass(batch) {
  const pct = calculateBatchPercent(batch);
  if (pct > 50) return 'bg-success';
  if (pct > 20) return 'bg-warning';
  return 'bg-danger';
}

function openCreateBatchModalForGroup(group) {
  const foundItem = items.value.find(i => i.inventoryItemId === group.inventoryItemId);
  openCreateBatchModal(foundItem || { inventoryItemId: group.inventoryItemId, costPerUnit: group.batches[0]?.unitCost || 0 });
}

// 4. Tab BOM / Recipes
const foods = ref([]);
const foodSearch = ref('');
const selectedFood = ref(null);
const currentFoodVariants = ref([]);
const selectedVariantId = ref(null);
const editingIngredients = ref([]);
const isSavingBom = ref(false);

const newIngredientItemId = ref('');
const newIngredientQty = ref(100);
const newIngredientWastage = ref(0);

const filteredFoods = computed(() => {
  if (!foodSearch.value) return foods.value;
  const s = foodSearch.value.toLowerCase();
  return foods.value.filter(f => f.foodName.toLowerCase().includes(s) || (f.categoryName && f.categoryName.toLowerCase().includes(s)));
});

const wastageBaseUnits = new Set(['g', 'kg', 'gam', 'gram', 'mg', 'kilogram']);

function canUseWastage(unit) {
  return wastageBaseUnits.has(String(unit || '').trim().toLowerCase());
}

const availableItemsForBOM = computed(() => {
  const currentIds = editingIngredients.value.map(i => i.inventoryItemId);
  return items.value.filter(i => !currentIds.includes(i.inventoryItemId));
});

const selectedNewItemBaseUnit = computed(() => {
  const found = items.value.find(i => i.inventoryItemId === newIngredientItemId.value);
  return found ? found.baseUnit : '';
});

const selectedNewItemCanUseWastage = computed(() => canUseWastage(selectedNewItemBaseUnit.value));

// 5. Tab Suppliers
const suppliers = ref([]);
const supplierSearch = ref('');

const filteredSuppliers = computed(() => {
  if (!supplierSearch.value) return suppliers.value;
  const s = supplierSearch.value.toLowerCase();
  return suppliers.value.filter(sup =>
    sup.supplierName.toLowerCase().includes(s) ||
    (sup.phone && sup.phone.includes(s)) ||
    (sup.email && sup.email.toLowerCase().includes(s))
  );
});

// MODAL STATES
const showItemModal = ref(false);
const editingItem = ref(null);
const itemForm = ref({
  itemName: '',
  singleUnit: 'kg', // Khi không quy đổi
  enableConversion: false, // Switch quy đổi
  purchaseUnit: 'thùng',
  baseUnit: 'lon',
  conversionRate: 24,
  minimumQuantity: 5,
  costPerUnit: 0,
  prepWastagePercent: 0,
  supplierId: null,
  isActive: true,
  enableInitialBatch: false, // Switch tạo lô ban đầu
  initialQuantity: 0,
  initialExpiryDate: '',
  initialActualUsableQuantity: null,
  initialAdjustmentNote: ''
});

const activeItemUnit = computed(() => {
  if (!itemForm.value.enableConversion) {
    return itemForm.value.singleUnit || 'Đơn vị';
  }
  return itemForm.value.baseUnit || 'BaseUnit';
});

const activePurchaseUnit = computed(() => {
  if (!itemForm.value.enableConversion) {
    return itemForm.value.singleUnit || 'Đơn vị';
  }
  return itemForm.value.purchaseUnit || 'PurchaseUnit';
});

const initialBaseQuantity = computed(() => {
  const conversionRate = itemForm.value.enableConversion && Number(itemForm.value.conversionRate) > 0
    ? Number(itemForm.value.conversionRate)
    : 1;
  return (Number(itemForm.value.initialQuantity) || 0) * conversionRate;
});
const initialExpectedUsable = computed(() => {
  const wastage = Number(itemForm.value.prepWastagePercent) || 0;
  return initialBaseQuantity.value * (1 - wastage / 100);
});
const initialActualUsable = computed(() => itemForm.value.initialActualUsableQuantity == null || itemForm.value.initialActualUsableQuantity === '' ? initialExpectedUsable.value : Number(itemForm.value.initialActualUsableQuantity) || 0);
const initialNeedsReason = computed(() => Number(itemForm.value.prepWastagePercent) > 0 && initialActualUsable.value < initialExpectedUsable.value - 0.0001);

const showBatchModal = ref(false);
const batchForm = ref({
  inventoryItemId: '',
  batchCode: '',
  quantity: 5000,
  unitCost: 0,
  wastagePercent: 0,
  actualUsableQuantity: null,
  adjustmentNote: '',
  manufacturingDate: '',
  expiryDate: '',
  supplierId: null,
  note: ''
});

const selectedBatchBaseUnit = computed(() => {
  const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
  return found ? found.baseUnit : '';
});

const selectedBatchPurchaseUnit = computed(() => {
  const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
  return found ? found.purchaseUnit : '';
});

const selectedBatchConversionRate = computed(() => {
  const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
  return Number(found?.conversionRate) > 0 ? Number(found.conversionRate) : 1;
});

const selectedBatchItemName = computed(() => {
  const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
  return found?.itemName || '';
});

const selectedBatchWastagePercent = computed(() => {
  const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
  return Number(found?.prepWastagePercent) || 0;
});

const selectedBatchBaseQuantity = computed(() => {
  return (Number(batchForm.value.quantity) || 0) * selectedBatchConversionRate.value;
});
const selectedBatchWasteQuantity = computed(() => selectedBatchBaseQuantity.value * selectedBatchWastagePercent.value / 100);
const selectedBatchNetQuantity = computed(() => selectedBatchBaseQuantity.value - selectedBatchWasteQuantity.value);
const selectedBatchActualUsable = computed(() => batchForm.value.actualUsableQuantity == null || batchForm.value.actualUsableQuantity === '' ? selectedBatchNetQuantity.value : Number(batchForm.value.actualUsableQuantity) || 0);
const selectedBatchActualWaste = computed(() => selectedBatchBaseQuantity.value - selectedBatchActualUsable.value);
const selectedBatchWasteVariance = computed(() => selectedBatchActualWaste.value - selectedBatchWasteQuantity.value);
const selectedBatchNeedsReason = computed(() => selectedBatchActualUsable.value < selectedBatchNetQuantity.value - 0.0001);
const selectedBatchEffectiveCost = computed(() => {
  return selectedBatchActualUsable.value > 0 ? Number(batchForm.value.unitCost || 0) * selectedBatchBaseQuantity.value / selectedBatchActualUsable.value : 0;
});

function isWeightUnit(unit) {
  return true;
}

const showItemBatchesModal = ref(false);
const viewingItem = ref(null);
const itemSpecificBatches = ref([]);

const showSupplierModal = ref(false);
const editingSupplier = ref(null);
const supplierForm = ref({
  supplierName: '',
  phone: '',
  email: '',
  address: ''
});

// ==========================================
// API FETCH FUNCTIONS
// ==========================================

async function fetchInventoryData() {
  try {
    const [summaryRes, itemsRes, supRes] = await Promise.all([
      axios.get(`${API_BASE}/summary`),
      axios.get(`${API_BASE}/items`),
      axios.get(`${API_BASE}/suppliers`)
    ]);

    if (summaryRes.data?.success) summary.value = summaryRes.data.data;
    if (itemsRes.data?.success) items.value = itemsRes.data.data;
    if (supRes.data?.success) suppliers.value = supRes.data.data;
  } catch (err) {
    console.error('Lỗi khi tải dữ liệu kho:', err);
  }
}

async function fetchBatches() {
  try {
    const res = await axios.get(`${API_BASE}/batches`);
    if (res.data?.success) {
      batches.value = res.data.data;
      const availableExpiredIds = new Set(
        batches.value.filter(isWasteSelectableBatch).map(batch => String(batch.batchId))
      );
      selectedExpiredBatchIds.value = selectedExpiredBatchIds.value.filter(id => availableExpiredIds.has(String(id)));
      // Mặc định mở rộng 3 nhóm đầu tiên
      const topIds = batches.value.slice(0, 3).map(b => b.inventoryItemId);
      topIds.forEach(id => expandedGroupIds.value.add(id));
    }
  } catch (err) {
    console.error('Lỗi tải danh sách lô hàng:', err);
  }
}

async function fetchRecipes() {
  try {
    const res = await axios.get(`${API_BASE}/boms`);
    if (res.data?.success) {
      foods.value = res.data.data;
      if (!selectedFood.value && foods.value.length > 0) {
        selectFood(foods.value[0]);
      }
    }
  } catch (err) {
    console.error('Lỗi tải danh sách món ăn & BOM:', err);
  }
}

// Food & Variant Selection for BOM
async function selectFood(food) {
  selectedFood.value = food;
  try {
    const res = await axios.get(`${API_BASE}/boms/${food.foodId}`);
    if (res.data?.success) {
      currentFoodVariants.value = res.data.data.variants || [];
      if (currentFoodVariants.value.length > 0) {
        onSelectVariant(currentFoodVariants.value[0].variantId);
      } else {
        selectedVariantId.value = null;
        editingIngredients.value = [];
      }
    }
  } catch (err) {
    console.error('Lỗi tải chi tiết BOM món:', err);
  }
}

function onSelectVariant(variantId) {
  selectedVariantId.value = variantId;
  const variant = currentFoodVariants.value.find(v => v.variantId === variantId);
  if (variant && variant.ingredients) {
    editingIngredients.value = variant.ingredients.map(i => ({
      inventoryItemId: i.inventoryItemId,
      itemName: i.itemName,
      baseUnit: i.baseUnit,
      purchaseUnit: i.purchaseUnit,
      conversionRate: i.conversionRate,
      quantityPerUnit: i.quantityPerUnit,
      wastagePercent: canUseWastage(i.baseUnit) ? (i.wastagePercent || 0) : 0
    }));
  } else {
    editingIngredients.value = [];
  }
}

function addIngredientToBOM() {
  if (!newIngredientItemId.value || newIngredientQty.value <= 0) return;
  const found = items.value.find(i => i.inventoryItemId === newIngredientItemId.value);
  if (!found) return;

  editingIngredients.value.push({
    inventoryItemId: found.inventoryItemId,
    itemName: found.itemName,
    baseUnit: found.baseUnit,
    purchaseUnit: found.purchaseUnit,
    conversionRate: found.conversionRate,
    quantityPerUnit: newIngredientQty.value,
    wastagePercent: canUseWastage(found.baseUnit) ? (newIngredientWastage.value || 0) : 0
  });

  newIngredientItemId.value = '';
  newIngredientQty.value = 100;
  newIngredientWastage.value = 0;
}

function removeIngredient(index) {
  editingIngredients.value.splice(index, 1);
}

async function saveBom() {
  if (!selectedFood.value) return;
  isSavingBom.value = true;
  try {
    const payload = {
      variantId: selectedVariantId.value,
      ingredients: editingIngredients.value.map(i => ({
        inventoryItemId: i.inventoryItemId,
        quantityPerUnit: i.quantityPerUnit,
        wastagePercent: 0
      }))
    };

    const res = await axios.post(`${API_BASE}/boms/${selectedFood.value.foodId}`, payload);
    if (res.data?.success) {
      alert(`Đã lưu định mức BOM thành công cho món '${selectedFood.value.foodName}'!`);
      await fetchRecipes();
      if (selectedFood.value) {
        await selectFood(selectedFood.value);
      }
    }
  } catch (err) {
    alert('Lỗi khi lưu định mức: ' + (err.response?.data?.message || err.message));
  } finally {
    isSavingBom.value = false;
  }
}

// Item CRUD
function openItemModal(item = null) {
  editingItem.value = item;
  if (item) {
    const hasConv = hasUnitConversion(item);
    itemForm.value = {
      itemName: item.itemName,
      singleUnit: item.baseUnit || 'kg',
      enableConversion: hasConv,
      purchaseUnit: item.purchaseUnit || 'kg',
      baseUnit: item.baseUnit || 'g',
      conversionRate: item.conversionRate || 1000,
      minimumQuantity: item.minimumQuantity,
      costPerUnit: item.costPerUnit,
      prepWastagePercent: item.prepWastagePercent || 0,
      supplierId: item.supplierId,
      isActive: item.isActive,
      enableInitialBatch: false,
      initialQuantity: 0,
      initialExpiryDate: '',
      initialActualUsableQuantity: null,
      initialAdjustmentNote: ''
    };
  } else {
    const defaultExp = new Date();
    defaultExp.setMonth(defaultExp.getMonth() + 3);

    itemForm.value = {
      itemName: '',
      singleUnit: 'kg',
      enableConversion: false, // Mặc định tắt quy đổi
      purchaseUnit: 'thùng',
      baseUnit: 'lon',
      conversionRate: 24,
      minimumQuantity: 10,
      costPerUnit: 0,
      prepWastagePercent: 0,
      supplierId: null,
      isActive: true,
      enableInitialBatch: false, // Mặc định tắt khởi tạo lô
      initialQuantity: 0,
      initialExpiryDate: defaultExp.toISOString().split('T')[0],
      initialActualUsableQuantity: null,
      initialAdjustmentNote: ''
    };
  }
  showItemModal.value = true;
}

async function saveItem() {
  try {
    let payloadPurchaseUnit = itemForm.value.singleUnit;
    let payloadBaseUnit = itemForm.value.singleUnit;
    let payloadConversionRate = 1;

    // Nếu người dùng bật switch quy đổi đơn vị
    if (itemForm.value.enableConversion) {
      payloadPurchaseUnit = itemForm.value.purchaseUnit;
      payloadBaseUnit = itemForm.value.baseUnit;
      payloadConversionRate = itemForm.value.conversionRate;
    }

    const payload = {
      itemName: itemForm.value.itemName,
      purchaseUnit: payloadPurchaseUnit,
      baseUnit: payloadBaseUnit,
      conversionRate: payloadConversionRate,
      minimumQuantity: itemForm.value.minimumQuantity,
      costPerUnit: itemForm.value.costPerUnit,
      prepWastagePercent: Number(itemForm.value.prepWastagePercent) || 0,
      supplierId: itemForm.value.supplierId,
      isActive: itemForm.value.isActive,
      // Số lượng lô ban đầu chỉ gửi nếu bật switch
      initialQuantity: itemForm.value.enableInitialBatch ? initialBaseQuantity.value : 0,
      initialExpiryDate: itemForm.value.enableInitialBatch ? itemForm.value.initialExpiryDate : null,
      initialActualUsableQuantity: itemForm.value.enableInitialBatch ? initialActualUsable.value : null,
      initialAdjustmentNote: itemForm.value.initialAdjustmentNote?.trim() || null
    };

    if (editingItem.value) {
      await axios.put(`${API_BASE}/items/${editingItem.value.inventoryItemId}`, payload);
      alert('Đã cập nhật thông tin nguyên liệu thành công!');
    } else {
      const response = await axios.post(`${API_BASE}/items`, payload);
      alert(response.data?.message || 'Đã thêm nguyên liệu mới thành công!');
      if (response.data?.pendingReceiptCode) {
        await router.push('/admin/stock-receipts');
      }
    }
    showItemModal.value = false;
    await fetchInventoryData();
    await fetchBatches();
    await fetchRecipes();

    // Làm mới BOM đang mở để giá vốn và biên lợi nhuận dùng giá nguyên liệu mới.
    if (selectedFood.value) {
      const refreshedFood = foods.value.find(food => food.foodId === selectedFood.value.foodId);
      if (refreshedFood) await selectFood(refreshedFood);
    }
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  }
}

async function deleteItem(item) {
  if (!confirm(`Bạn có chắc muốn vô hiệu hóa nguyên liệu "${item.itemName}"?`)) return;
  try {
    await axios.delete(`${API_BASE}/items/${item.inventoryItemId}`);
    await fetchInventoryData();
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  }
}

// Batch CRUD
function openCreateBatchModal(preselectedItem = null) {
  const defaultExp = new Date();
  defaultExp.setMonth(defaultExp.getMonth() + 2);

  const itemId = preselectedItem ? preselectedItem.inventoryItemId : (items.value.length > 0 ? items.value[0].inventoryItemId : '');
  const foundItem = items.value.find(i => i.inventoryItemId === itemId);

  batchForm.value = {
    inventoryItemId: itemId,
    batchCode: '',
    quantity: foundItem && hasUnitConversion(foundItem) ? 1 : 1000,
    unitCost: foundItem ? (foundItem.costPerUnit ?? foundItem.costPerPurchaseUnit ?? 0) : 0,
    wastagePercent: foundItem?.prepWastagePercent || 0,
    actualUsableQuantity: null,
    adjustmentNote: '',
    manufacturingDate: new Date().toISOString().split('T')[0],
    expiryDate: defaultExp.toISOString().split('T')[0],
    supplierId: foundItem ? foundItem.supplierId : null,
    note: 'Nhập lô hàng bổ sung'
  };
  showBatchModal.value = true;
}

function onBatchItemSelect() {
  const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
  if (found) {
    batchForm.value.quantity = hasUnitConversion(found) ? 1 : 1000;
    batchForm.value.unitCost = found.costPerUnit ?? found.costPerPurchaseUnit ?? 0;
    batchForm.value.supplierId = found.supplierId;
    batchForm.value.wastagePercent = found.prepWastagePercent || 0;
    batchForm.value.actualUsableQuantity = null;
    batchForm.value.adjustmentNote = '';
  }
}

async function saveBatch() {
  try {
    const found = items.value.find(i => i.inventoryItemId === batchForm.value.inventoryItemId);
    if (!found) {
      alert('Vui lòng chọn nguyên liệu nhập lô.');
      return;
    }

    const purchaseQuantity = Number(batchForm.value.quantity);
    const conversionRate = Number(found.conversionRate) > 0 ? Number(found.conversionRate) : 1;
    if (purchaseQuantity <= 0) {
      alert('Số lượng nhập phải lớn hơn 0.');
      return;
    }

    const payload = {
      inventoryItemId: batchForm.value.inventoryItemId,
      batchCode: batchForm.value.batchCode ? batchForm.value.batchCode.trim() : null,
      // UI nhập theo PurchaseUnit; API lưu tồn theo BaseUnit.
      quantity: purchaseQuantity * conversionRate,
      unitCost: Number(batchForm.value.unitCost),
      wastagePercent: Number(batchForm.value.wastagePercent) || 0,
      actualUsableQuantity: selectedBatchActualUsable.value,
      adjustmentNote: batchForm.value.adjustmentNote?.trim() || null,
      manufacturingDate: batchForm.value.manufacturingDate ? batchForm.value.manufacturingDate : null,
      expiryDate: batchForm.value.expiryDate,
      supplierId: batchForm.value.supplierId ? Number(batchForm.value.supplierId) : null,
      note: batchForm.value.note ? batchForm.value.note.trim() : null
    };

    const receiptPayload = {
      receiptType: 'import_supplier',
      supplierId: payload.supplierId,
      note: payload.note || 'Lập phiếu nhập hàng chờ duyệt',
      details: [{
        inventoryItemId: payload.inventoryItemId,
        quantity: payload.quantity,
        unitCost: payload.unitCost,
        wastagePercent: payload.wastagePercent,
        actualUsableQuantity: payload.actualUsableQuantity,
        adjustmentNote: payload.adjustmentNote,
        newBatchCode: payload.batchCode,
        manufacturingDate: payload.manufacturingDate,
        expiryDate: payload.expiryDate
      }]
    };

    const res = await axios.post(RECEIPTS_API, receiptPayload);
    if (res.data?.success) {
      alert(res.data.message || `Đã tạo phiếu nhập chờ duyệt thành công!`);
      showBatchModal.value = false;
      await router.push('/admin/stock-receipts');
    }
  } catch (err) {
    alert('Lỗi khi tạo phiếu nhập: ' + (err.response?.data?.message || err.message));
  }
}

async function viewItemBatches(item) {
  viewingItem.value = item;
  try {
    const res = await axios.get(`${API_BASE}/batches`, {
      params: { inventoryItemId: item.inventoryItemId }
    });
    if (res.data?.success) {
      itemSpecificBatches.value = res.data.data;
      showItemBatchesModal.value = true;
    }
  } catch (err) {
    console.error('Lỗi khi lấy danh sách lô của mặt hàng:', err);
  }
}

// Supplier CRUD
function openSupplierModal(sup = null) {
  editingSupplier.value = sup;
  if (sup) {
    supplierForm.value = {
      supplierName: sup.supplierName,
      phone: sup.phone || '',
      email: sup.email || '',
      address: sup.address || ''
    };
  } else {
    supplierForm.value = {
      supplierName: '',
      phone: '',
      email: '',
      address: ''
    };
  }
  showSupplierModal.value = true;
}

async function saveSupplier() {
  try {
    if (editingSupplier.value) {
      await axios.put(`${API_BASE}/suppliers/${editingSupplier.value.supplierId}`, supplierForm.value);
      alert('Đã cập nhật nhà cung cấp thành công!');
    } else {
      await axios.post(`${API_BASE}/suppliers`, supplierForm.value);
      alert('Đã thêm nhà cung cấp mới thành công!');
    }
    showSupplierModal.value = false;
    await fetchInventoryData();
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  }
}

async function deleteSupplier(sup) {
  if (!confirm(`Bạn có chắc muốn xóa/ngừng hợp tác với nhà cung cấp "${sup.supplierName}"?`)) return;
  try {
    await axios.delete(`${API_BASE}/suppliers/${sup.supplierId}`);
    await fetchInventoryData();
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  }
}

// Formatters
function formatCurrency(val) {
  return (val || 0).toLocaleString('vi-VN') + ' đ';
}

function formatQuantity(val) {
  return Number(val || 0).toLocaleString('vi-VN', { maximumFractionDigits: 4 });
}

function formatWastagePercent(val) {
  return Number(val || 0).toLocaleString('vi-VN', { maximumFractionDigits: 1 });
}

function formatDate(val) {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('vi-VN');
}

function isNearExpiry(expiryDate) {
  if (!expiryDate) return false;
  const diffTime = new Date(expiryDate) - new Date();
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  return diffDays <= 7;
}

// SignalR Realtime Listener
async function onInventoryChanged() {
  await fetchInventoryData();
  if (activeTab.value === 'batches') await fetchBatches();
  await fetchRecipes();

  if (selectedFood.value) {
    const refreshedFood = foods.value.find(food => food.foodId === selectedFood.value.foodId);
    if (refreshedFood) await selectFood(refreshedFood);
  }
}

onMounted(() => {
  fetchInventoryData();
  fetchBatches();
  fetchRecipes();

  signalrService.on('InventoryChanged', onInventoryChanged);
});

onUnmounted(() => {
  signalrService.off('InventoryChanged', onInventoryChanged);
});
</script>

<style scoped>
.inventory-container {
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

.expired-bulk-action {
  position: sticky;
  top: 12px;
  z-index: 12;
  padding: 14px 16px;
  border: 1px solid #f1aeb5;
  border-left: 4px solid #dc3545;
  border-radius: 10px;
  background: #fff5f5;
  box-shadow: 0 8px 20px rgba(127, 29, 29, 0.1);
}

.batch-select-control {
  min-width: 30px;
  min-height: 30px;
  justify-content: center;
  cursor: pointer;
}

.batch-select-control .form-check-input {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.btn-outline-danger.active {
  color: #fff;
  background: #dc3545;
  border-color: #dc3545;
}

.help-tooltip { position: relative; display: inline-flex; align-items: center; }
.help-tooltip-trigger { display: inline-grid; width: 22px; height: 22px; place-items: center; padding: 0; border: 1px solid #8792a2; border-radius: 50%; background: #fff; color: #526174; font-size: 0.78rem; font-weight: 800; line-height: 1; cursor: help; }
.help-tooltip-panel { position: absolute; z-index: 1080; top: 100%; left: 0; display: grid; width: min(390px, calc(100vw - 56px)); max-height: min(65vh, 480px); gap: 9px; overflow-y: auto; padding: 13px 14px; border: 1px solid #dbe4e9; border-radius: 7px; background: #fff; box-shadow: 0 10px 28px rgba(25, 40, 52, 0.2); color: #334155; font-size: 0.82rem; font-weight: 400; line-height: 1.45; visibility: hidden; opacity: 0; pointer-events: auto; transition: opacity 0.14s ease; }
.help-tooltip-panel strong { color: #17231f; font-size: 0.9rem; }
.help-tooltip-example { padding: 8px 9px; border-left: 3px solid #e3a32a; background: #fff8e9; }
.help-tooltip:hover .help-tooltip-panel, .help-tooltip:focus-within .help-tooltip-panel { visibility: visible; opacity: 1; }

.stat-badge {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
}

.nav-tab-btn {
  border: none;
  background: transparent;
  color: #4b5563;
  font-weight: 600;
  font-size: 0.9rem;
  padding: 8px 16px;
  border-radius: 8px;
  transition: all 0.2s;
}

.nav-tab-btn:hover {
  background: #e5e7eb;
  color: #111827;
}

.nav-tab-btn.active {
  background: #059669;
  color: #ffffff;
  box-shadow: 0 2px 6px rgba(5, 150, 105, 0.3);
}

.variant-tab-btn {
  border: 1px solid #d1d5db;
  background: #ffffff;
  color: #374151;
  font-size: 0.85rem;
  padding: 6px 14px;
  border-radius: 20px;
  transition: all 0.2s;
}

.variant-tab-btn:hover {
  background: #f3f4f6;
}

.variant-tab-btn.active {
  background: #047857;
  color: #ffffff;
  border-color: #047857;
  box-shadow: 0 2px 6px rgba(4, 120, 87, 0.25);
}

.food-item-card {
  border-color: #e5e7eb;
  transition: all 0.15s ease-in-out;
}

.food-item-card:hover {
  background: #f8fafc;
  border-color: #059669;
}

.food-item-card.active-food {
  background: #ecfdf5;
  border-color: #059669;
  box-shadow: 0 2px 8px rgba(5, 150, 105, 0.15);
}

.group-header-hover {
  transition: background-color 0.15s ease;
}

.group-header-hover:hover {
  background-color: #f1f5f9 !important;
}

.hover-badge {
  transition: all 0.15s;
}

.hover-badge:hover {
  background: #059669 !important;
  color: #ffffff !important;
  border-color: #059669 !important;
}

.pulse-badge {
  animation: pulseWarning 1.5s infinite;
}

@keyframes pulseWarning {
  0% { transform: scale(1); }
  50% { transform: scale(1.04); }
  100% { transform: scale(1); }
}

.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  backdrop-filter: blur(4px);
  z-index: 1050;
  animation: fadeIn 0.2s ease-out;
}

.item-modal-panel {
  display: flex;
  width: min(100%, 680px);
  max-height: calc(100dvh - 32px);
  overflow: hidden;
  flex-direction: column;
  border-radius: 10px;
}

.item-modal-panel > .d-flex:first-child {
  flex: 0 0 auto;
  padding: 20px 24px 12px;
  margin: 0 24px 12px !important;
}

.item-modal-form { display: flex; min-height: 0; flex: 1 1 auto; flex-direction: column; }
.item-modal-scroll { min-height: 0; overflow-y: auto; padding: 0 24px 12px; overscroll-behavior: contain; }
.item-modal-footer { flex: 0 0 auto; padding: 12px 24px max(12px, env(safe-area-inset-bottom)); background: #fff; border-top: 1px solid #e2e8f0; }

@media (max-width: 575.98px) {
  .item-modal-panel { max-height: calc(100dvh - 16px); }
  .item-modal-panel > .d-flex:first-child { padding: 16px 16px 10px; margin: 0 16px 10px !important; }
  .item-modal-scroll { padding: 0 16px 10px; }
  .item-modal-footer { padding: 10px 16px max(10px, env(safe-area-inset-bottom)); }
}

.batch-modal-panel {
  display: flex;
  flex-direction: column;
  width: min(100%, 680px);
  max-height: calc(100dvh - 32px);
  overflow: hidden;
  border-radius: 10px;
}

.batch-modal-panel > .d-flex:first-child {
  flex: 0 0 auto;
  padding: 20px 24px 12px;
  margin: 0 24px 12px !important;
}

.batch-modal-form {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-height: 0;
}

.batch-modal-body {
  min-height: 0;
  overflow-y: auto;
  padding: 0 24px 12px;
  overscroll-behavior: contain;
}

.batch-modal-footer {
  flex: 0 0 auto;
  padding: 12px 24px max(12px, env(safe-area-inset-bottom));
  background: #fff;
  border-top: 1px solid #e2e8f0;
}

.batch-wastage-summary {
  padding: 14px;
  color: #334155;
  background: #f8fafc;
  border: 1px solid #dbe4ec;
  border-radius: 8px;
}

.batch-wastage-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.batch-wastage-heading strong,
.batch-wastage-heading span:not(.batch-rate-badge) {
  display: block;
}

.batch-wastage-heading span:not(.batch-rate-badge),
.batch-rate-note,
.batch-wastage-grid span,
.batch-wastage-grid small {
  color: #64748b;
  font-size: 12px;
}

.batch-wastage-heading span:not(.batch-rate-badge) { margin-top: 2px; }
.batch-rate-badge { padding: 4px 9px; color: #075985; background: #e0f2fe; border-radius: 20px; font-weight: 700; }

.item-name-with-wastage { display: flex; align-items: center; flex-wrap: wrap; gap: 6px; }
.item-wastage-badge { display: inline-flex; align-items: center; padding: 2px 6px; color: #92400e; background: #fef3c7; border: 1px solid #fcd34d; border-radius: 999px; font-size: 10px; font-weight: 700; white-space: nowrap; }

.batch-wastage-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.batch-wastage-grid > div {
  min-width: 0;
  padding: 9px 10px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
}

.batch-wastage-grid span,
.batch-wastage-grid strong,
.batch-wastage-grid small { display: block; overflow-wrap: anywhere; }
.batch-wastage-grid strong { margin-top: 3px; font-size: 14px; }
.batch-wastage-grid .is-over strong, .batch-wastage-grid .is-over small { color: #b91c1c; }
.batch-wastage-grid .is-within strong { color: #047857; }
.stock-progress-track {
  width: 80px;
  height: 5px;
  background-color: #e2e8f0;
  border-radius: 999px;
  overflow: hidden;
}

.stock-progress-fill {
  height: 100%;
  border-radius: 999px;
  transition: width 0.3s ease;
}

@media (max-width: 575.98px) {
  .batch-modal-panel { max-height: calc(100dvh - 16px); }
  .batch-modal-panel > .d-flex:first-child { padding: 16px 16px 10px; margin: 0 16px 10px !important; }
  .batch-modal-body { padding: 0 16px 10px; }
  .batch-modal-footer { padding: 10px 16px max(10px, env(safe-area-inset-bottom)); }
  .batch-wastage-summary { padding: 10px; }
}
</style>

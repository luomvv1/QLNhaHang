<template>
  <div class="daily-portions-page">
    <header class="page-header">
      <div class="page-heading">
        <div class="eyebrow">BẾP & KHO</div>
        <h1>Điều phối suất ăn</h1>
        <p>{{ formatDateVN(selectedDate) }} · Nấu theo order, xuất nguyên liệu theo kế hoạch</p>
      </div>

      <div class="page-actions">
        <div class="date-picker-wrap">
          <button class="icon-button" title="Ngày hôm qua" @click="changeDateOffset(-1)">‹</button>
          <input v-model="selectedDate" type="date" @change="fetchDailyPortions" />
          <button class="icon-button" title="Ngày mai" @click="changeDateOffset(1)">›</button>
          <button class="today-button" @click="setToday">Hôm nay</button>
        </div>
        <button class="button button-light" :disabled="hasRequisition" :title="hasRequisition ? 'Hãy hủy phiếu nháp trước khi đổi kế hoạch' : 'Thêm món và số suất cần phục vụ'" @click="openPlanModal">
          <span>＋</span>
          Lập kế hoạch
        </button>
        <button
          v-if="!hasRequisition"
          class="button button-warning"
          :disabled="isGeneratingRequisition || hasRequisition || shortageInfo || summary.totalNeedToCook <= 0"
          @click="generateRequisition"
        >
          <span>⇧</span>
          {{ isGeneratingRequisition ? 'Đang lập...' : 'Lập phiếu xuất bếp' }}
        </button>
        <button v-else-if="canGenerateAdditionalRequisition" class="button button-warning" :disabled="isGeneratingAdditional || shortageInfo || hasPendingAdditionalReceipt" :title="hasPendingAdditionalReceipt ? 'Hãy duyệt hoặc hủy phiếu bổ sung đang chờ trước' : 'Lập phiếu xuất bếp bổ sung'" @click="openAdditionalModal">
          <span>＋</span>
          Xuất bổ sung
        </button>
        <button v-else class="button button-light" disabled title="Phiếu xuất bếp đang chờ thủ kho duyệt">
          <span>⏳</span>
          Chờ thủ kho duyệt
        </button>
      </div>
    </header>

    <section class="workflow-strip" aria-label="Quy trình lập phiếu xuất bếp">
      <div class="workflow-step workflow-step-active"><span class="workflow-number">1</span><div><strong>Chốt kế hoạch</strong><small>Số suất cần phục vụ</small></div></div>
      <span class="workflow-line"></span>
      <div class="workflow-step" :class="{ 'workflow-step-active': portions.length > 0 }"><span class="workflow-number">2</span><div><strong>Tính theo BOM</strong><small>Gom nguyên liệu cần xuất</small></div></div>
      <span class="workflow-line"></span>
      <div class="workflow-step" :class="{ 'workflow-step-active': hasRequisition }"><span class="workflow-number">3</span><div><strong>Chờ thủ kho duyệt</strong><small>Kho mới trừ tồn</small></div></div>
      <div class="workflow-status" :class="hasRequisition ? 'status-ready' : 'status-pending'"><span class="status-dot"></span>{{ initialReceiptStatus === 'approved' ? 'Đã duyệt' : hasRequisition ? 'Chờ thủ kho duyệt' : 'Chưa lập phiếu' }}</div>
    </section>

    <section v-if="shortageInfo" class="shortage-banner" role="alert">
      <div class="shortage-heading"><strong>Chưa thể lập phiếu xuất</strong><span>{{ shortageInfo.message }}</span></div>
      <div class="shortage-actions"><span>Hướng xử lý: nhập bổ sung nguyên liệu, giảm số suất kế hoạch hoặc tạm ngưng món đang thiếu.</span><button class="icon-button icon-button-muted" title="Đóng cảnh báo" @click="shortageInfo = null">×</button></div>
      <div class="shortage-list">
        <div v-for="item in shortageInfo.shortages" :key="item.inventoryItemId" class="shortage-item"><strong>{{ item.itemName }}</strong><span>Cần {{ formatNumber(item.requiredQuantity) }} {{ item.baseUnit }}</span><span>Lô còn hạn: {{ formatNumber(item.availableQuantity ?? item.currentQuantity) }} {{ item.baseUnit }}</span><small v-if="item.expiredQuantity > 0">Không tính {{ formatNumber(item.expiredQuantity) }} {{ item.baseUnit }} đã hết hạn</small><b>Thiếu {{ formatNumber(item.shortageQuantity) }} {{ item.baseUnit }}</b></div>
      </div>
    </section>

    <div class="plan-overview">
      <span><strong>{{ formatNumber(summary.totalDishes) }}</strong> biến thể</span>
      <span><strong>{{ formatNumber(summary.totalPlanned) }}</strong> suất cần phục vụ</span>
      <span><strong>{{ formatNumber(summary.totalSold) }}</strong> suất đã phục vụ</span>
      <span><strong>{{ formatNumber(summary.totalNeedToCook) }}</strong> suất tính BOM</span>
      <span class="overview-status" :class="hasRequisition ? 'status-ready' : 'status-pending'">
        <span class="status-dot"></span>{{ initialReceiptStatus === 'approved' ? 'Phiếu đã duyệt' : hasRequisition ? 'Đang chờ duyệt' : 'Chưa tạo phiếu xuất' }}
      </span>
    </div>

    <section class="content-section">
      <div class="section-toolbar">
        <div>
          <h2>Thực đơn trong ngày</h2>
          <p>{{ portions.length }} biến thể trong ngày</p>
        </div>
        <div class="toolbar-controls">
          <input v-model="searchQuery" class="search-input" type="search" placeholder="Tìm món hoặc biến thể" />
          <select v-model="selectedCategoryFilter" class="select-input">
            <option value="">Tất cả danh mục</option>
            <option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">{{ cat.categoryName }}</option>
          </select>
          <button class="icon-button icon-button-muted" title="Làm mới" @click="fetchDailyPortions">↻</button>
        </div>
      </div>

      <div v-if="loading" class="empty-state"><span class="loading-spinner"></span> Đang tải điều phối...</div>
      <div v-else-if="loadError" class="load-error" role="alert">
        <strong>Không tải được dữ liệu điều phối</strong>
        <span>{{ loadError }}</span>
        <button class="button button-secondary" @click="fetchDailyPortions">Thử lại</button>
      </div>
      <div v-else-if="groupedPortions.length === 0" class="empty-state">
        <strong>Chưa có món trong kế hoạch ngày {{ formatDateVN(selectedDate) }}</strong>
        <span>Thêm món và nhập số suất dự kiến để lập phiếu xuất nguyên liệu.</span>
        <div class="empty-actions">
          <button class="button button-primary" :disabled="hasRequisition" @click="openPlanModal">＋ Thêm món vào ngày</button>
        </div>
      </div>

      <div v-else class="food-groups">
        <article v-for="group in groupedPortions" :key="group.foodId" class="food-group">
          <div class="food-group-header">
            <div class="food-title-wrap">
              <div class="food-mark">M</div>
              <div>
                <h3>{{ group.foodName }}</h3>
                <div class="food-subtitle">
                  <span class="category-tag">{{ group.categoryName }}</span>
                  <span>{{ group.items.length }} biến thể</span>
                </div>
              </div>
            </div>
          </div>

          <div class="variant-table-wrap">
            <div class="variant-grid variant-grid-head">
              <div>Biến thể / giá bán</div>
              <div class="align-right">Kế hoạch</div>
              <div class="align-right">Đã phục vụ</div>
              <div class="align-right">Tính BOM / xuất</div>
              <div class="align-center">Thao tác</div>
            </div>

            <div v-for="p in group.items" :key="p.portionControlId" class="variant-grid variant-grid-row" :class="{ 'row-out-of-stock': p.plannedPortions > 0 && p.remainingPortions <= 0 }">
              <div class="variant-name-cell">
                <strong>{{ p.variantName }}</strong>
                <small>{{ formatCurrency(p.price) }} / {{ p.unit }}</small>
                <span v-if="initialReceiptStatus === 'approved'" class="portion-export-status portion-exported">✓ Đã xuất kho ({{ formatNumber(p.plannedPortions) }} {{ p.unit }})</span>
                <span v-else-if="hasRequisition" class="portion-export-status portion-waiting">Chưa ghi nhận kho</span>
              </div>
              <div class="number-cell text-blue">
                {{ formatNumber(p.plannedPortions) }} <small>{{ p.unit }}</small>
                <small v-if="p.additionalApprovedPortions > 0" class="plan-breakdown">(Đầu ca: {{ formatNumber(p.initialPlannedPortions) }} + Thêm: {{ formatNumber(p.additionalApprovedPortions) }})</small>
                <span v-if="p.pendingAdditionalPortions > 0" class="pending-portions-badge">⏳ +{{ formatNumber(p.pendingAdditionalPortions) }} suất chờ duyệt</span>
              </div>
              <div class="number-cell text-cyan">{{ formatNumber(p.soldPortions) }} <small>{{ p.unit }}</small></div>
              <div class="number-cell remaining-cell">
                <span v-if="p.remainingPortions > 5" class="remain-pill remain-positive">🟢 Còn {{ formatNumber(p.remainingPortions) }} {{ p.unit }}</span>
                <span v-else-if="p.remainingPortions > 0" class="remain-pill remain-warning">🟠 Sắp hết (còn {{ formatNumber(p.remainingPortions) }})</span>
                <span v-else class="remain-pill remain-danger">🔴 Hết suất kế hoạch!</span>
              </div>
              <div class="actions-cell">
                <button
                  v-if="canGenerateAdditionalRequisition && !hasPendingAdditionalReceipt && p.remainingPortions <= 5 && p.pendingAdditionalPortions <= 0"
                  class="additional-row-action"
                  title="Xuất thêm 10 suất cho biến thể này"
                  @click="openQuickAdditionalForDish(p)"
                >＋ Xuất thêm</button>
                <button
                  v-else-if="canGenerateAdditionalRequisition && p.remainingPortions <= 5 && (p.pendingAdditionalPortions > 0 || hasPendingAdditionalReceipt)"
                  class="additional-row-action additional-row-action-pending"
                  disabled
                  title="Phiếu bổ sung đang chờ thủ kho duyệt"
                >⏳ Đang chờ duyệt</button>
                <button class="row-action action-info" title="Xem định mức và tồn nguyên liệu" @click="viewPortionBom(p)">⌕</button>
                <button class="row-action action-edit" :disabled="hasRequisition" :title="hasRequisition ? 'Hãy hủy phiếu nháp trước khi sửa kế hoạch' : 'Sửa kế hoạch món này'" @click="openEditPortionModal(p)">✎</button>
                <button
                  class="row-action action-delete"
                  :title="hasRequisition ? 'Hãy hủy phiếu nháp trước khi xóa' : 'Xóa khỏi kế hoạch'"
                  :disabled="hasRequisition || p.soldPortions > 0"
                  @click="deletePortion(p)"
                >×</button>
              </div>
            </div>
          </div>
        </article>
      </div>
    </section>

    <!-- Batch plan modal -->
    <div v-if="showPlanModal" class="modal-backdrop-custom">
      <div class="modal-panel modal-panel-wide">
        <div class="modal-header">
          <div><span class="modal-kicker">BƯỚC 1</span><h2>Lập kế hoạch suất ăn</h2><p>{{ formatDateVN(selectedDate) }} · Chọn món rồi nhập số suất dự kiến.</p></div>
          <button class="close-button" title="Đóng" @click="showPlanModal = false">×</button>
        </div>
        <div class="plan-toolbar">
          <input v-model="planDishSearch" class="search-input" type="search" placeholder="Tìm món hoặc biến thể" />
          <select v-model="planCategoryFilter" class="select-input"><option value="">Tất cả danh mục</option><option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">{{ cat.categoryName }}</option></select>
          <button class="button button-secondary" @click="toggleSelectAllPlan">{{ isAllSelectedForPlan ? 'Bỏ chọn đang lọc' : 'Chọn tất cả đang lọc' }}</button>
          <button class="button button-secondary" @click="clearAllPlanInputs">Xóa nhập</button>
        </div>
        <div class="plan-note">Nhập số suất cần phục vụ trong ngày. Nhà hàng nấu theo order nên không cần nhập tồn món chín chuyển ngày.</div>
        <div class="plan-list">
          <div v-if="groupedPlanRows.length === 0" class="empty-state compact">Không tìm thấy món phù hợp.</div>
          <div v-for="group in groupedPlanRows" :key="group.foodId" class="plan-food-group">
            <div class="plan-food-heading">
              <div><strong>{{ group.foodName }}</strong><span>{{ group.categoryName }} · {{ group.rows.length }} biến thể</span></div>
              <span class="selection-count">{{ getGroupSelectedCount(group) }}/{{ group.rows.length }} đã chọn</span>
            </div>
            <div v-for="row in group.rows" :key="`${row.foodId}-${row.variantId}`" class="plan-row" :class="{ 'plan-row-selected': row.selected }">
              <label class="plan-check"><input type="checkbox" v-model="row.selected" /><span></span></label>
              <div class="plan-variant"><strong>{{ row.variantName }}</strong><small>Nhập số suất cho biến thể này</small></div>
              <label class="plan-field"><span>Kế hoạch bán</span><input v-model.number="row.plannedPortions" type="number" min="0" step="1" @input="row.selected = true" /></label>
              <div class="plan-needed"><span>Tính BOM / xuất</span><strong>{{ formatNumber(Math.max(0, row.plannedPortions || 0)) }} suất</strong></div>
            </div>
          </div>
        </div>
        <div class="modal-footer"><span><strong>{{ selectedPlanRowsCount }}</strong> biến thể · <strong>{{ formatNumber(selectedPlanPortionsTotal) }}</strong> suất</span><div><button class="button button-secondary" @click="showPlanModal = false">Đóng</button><button class="button button-primary" :disabled="isSubmittingPlan || selectedPlanRowsCount === 0" @click="submitBatchPlan">{{ isSubmittingPlan ? 'Đang lưu...' : 'Lưu kế hoạch' }}</button></div></div>
      </div>
    </div>

    <!-- Additional dispatch modal: dùng chung giao diện lập kế hoạch -->
    <div v-if="showAdditionalModal" class="modal-backdrop-custom">
      <div class="modal-panel modal-panel-wide">
        <div class="modal-header">
          <div><span class="modal-kicker">BƯỚC BỔ SUNG</span><h2>Điều phối xuất bếp bổ sung</h2><p>{{ formatDateVN(selectedDate) }} · Chọn món bán hết và nhập số suất cần xuất thêm.</p></div>
          <button class="close-button" title="Đóng" @click="showAdditionalModal = false">×</button>
        </div>
        <div class="plan-toolbar">
          <input v-model="additionalSearch" class="search-input" type="search" placeholder="Tìm món hoặc biến thể" />
          <select v-model="additionalCategoryFilter" class="select-input"><option value="">Tất cả danh mục</option><option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">{{ cat.categoryName }}</option></select>
          <button class="button button-secondary" @click="toggleSelectAllAdditional">{{ isAllAdditionalSelected ? 'Bỏ chọn đang lọc' : 'Chọn tất cả đang lọc' }}</button>
          <button class="button button-secondary" @click="clearAdditionalInputs">Xóa nhập</button>
        </div>
        <div class="plan-note">Phiếu bổ sung chỉ tính phần order phát sinh, không lặp lại phiếu đã xuất. Phiếu ở trạng thái chờ duyệt; tồn kho chỉ trừ sau khi thủ kho duyệt FEFO.</div>
        <div class="plan-list">
          <div v-if="groupedAdditionalRows.length === 0" class="empty-state compact">Không tìm thấy món phù hợp.</div>
          <div v-for="group in groupedAdditionalRows" :key="group.foodId" class="plan-food-group">
            <div class="plan-food-heading"><div><strong>{{ group.foodName }}</strong><span>{{ group.categoryName }} · {{ group.rows.length }} biến thể</span></div><span class="selection-count">{{ getGroupSelectedCount(group) }}/{{ group.rows.length }} đã chọn</span></div>
            <div v-for="row in group.rows" :key="row.portionControlId" class="plan-row additional-plan-row" :class="{ 'plan-row-selected': row.selected }">
              <label class="plan-check"><input v-model="row.selected" type="checkbox" /><span></span></label>
              <div class="plan-variant"><strong>{{ row.variantName }}</strong><small>Đã phục vụ: {{ formatNumber(row.soldPortions) }} {{ row.unit }}</small></div>
              <label class="plan-field"><span>Số suất xuất thêm</span><input v-model.number="row.additionalPortions" type="number" min="0" step="1" @input="row.selected = true" /></label>
              <div class="plan-needed"><span>Tính BOM / xuất</span><strong>{{ formatNumber(Math.max(0, row.additionalPortions || 0)) }} suất</strong></div>
            </div>
          </div>
        </div>
        <div class="modal-footer"><span><strong>{{ selectedAdditionalRowsCount }}</strong> biến thể · <strong>{{ formatNumber(selectedAdditionalPortionsTotal) }}</strong> suất bổ sung</span><div><button class="button button-secondary" @click="showAdditionalModal = false">Đóng</button><button class="button button-primary" :disabled="isGeneratingAdditional || selectedAdditionalPortionsTotal <= 0" @click="submitAdditionalRequisition">{{ isGeneratingAdditional ? 'Đang lập...' : 'Lập phiếu bổ sung' }}</button></div></div>
      </div>
    </div>

    <!-- BOM modal -->
    <div v-if="showBomModal && selectedPortionBom" class="modal-backdrop-custom">
      <div class="modal-panel modal-panel-medium">
        <div class="modal-header"><div><span class="modal-kicker">ĐỊNH MỨC BOM</span><h2>{{ selectedPortionBom.foodName }}</h2><p>{{ selectedPortionBom.variantName }} · {{ formatNumber(selectedPortionBom.needToCookPortions) }} suất tính định mức</p></div><button class="close-button" title="Đóng" @click="showBomModal = false">×</button></div>
        <div class="bom-table-wrap">
          <table class="bom-table"><thead><tr><th>Nguyên liệu</th><th>Định mức / suất</th><th>Lượng cần cho kế hoạch</th><th>Lô còn hạn theo ngày kế hoạch</th></tr></thead><tbody>
            <tr v-if="!selectedPortionBom.bom || selectedPortionBom.bom.length === 0"><td colspan="4" class="empty-cell">Món chưa có BOM.</td></tr>
            <tr v-for="ing in selectedPortionBom.bom" :key="ing.inventoryItemId"><td><strong>{{ ing.itemName }}</strong><small v-if="ing.purchaseUnit && ing.conversionRate > 1">1 {{ ing.purchaseUnit }} = {{ ing.conversionRate }} {{ ing.baseUnit }}</small></td><td>{{ formatNumber(ing.quantityPerUnit) }} {{ ing.baseUnit }}</td><td class="text-blue strong-number">{{ formatNumber(ing.totalRequired) }} {{ ing.baseUnit }}</td><td><strong>{{ formatNumber(ing.availableQuantity ?? ing.currentQuantity) }} {{ ing.baseUnit }}</strong><small v-if="ing.expiredQuantity > 0" class="expired-stock">Không tính {{ formatNumber(ing.expiredQuantity) }} đã hết hạn</small><span class="stock-check" :class="(ing.availableQuantity ?? ing.currentQuantity) >= ing.totalRequired ? 'stock-ok' : 'stock-low'">{{ (ing.availableQuantity ?? ing.currentQuantity) >= ing.totalRequired ? 'Đủ lô còn hạn' : 'Thiếu lô còn hạn' }}</span></td></tr>
          </tbody></table>
        </div>
        <div class="modal-footer"><span class="muted-note">Định mức là lượng sạch cần cho món. Hao hụt đã xử lý khi nhập lô; phiếu xuất không trừ thêm.</span><button class="button button-secondary" @click="showBomModal = false">Đóng</button></div>
      </div>
    </div>

    <!-- Requisition result modal -->
    <div v-if="showRequisitionResultModal && createdReceiptInfo" class="modal-backdrop-custom">
      <div class="modal-panel modal-panel-wide">
        <div class="modal-header"><div><span class="modal-kicker modal-kicker-success">{{ createdReceiptKind === 'additional' ? 'ĐÃ TẠO PHIẾU BỔ SUNG' : 'ĐÃ TẠO PHIẾU' }}</span><h2>{{ createdReceiptKind === 'additional' ? 'Phiếu xuất bếp bổ sung' : 'Phiếu xuất bếp' }}</h2><p><strong>{{ createdReceiptInfo.receiptCode }}</strong> · Chờ duyệt · {{ formatDateTime(createdReceiptInfo.createdAt) }}</p></div><button class="close-button" title="Đóng" @click="showRequisitionResultModal = false">×</button></div>
        <div class="result-summary"><div><strong>{{ createdReceiptInfo.totalIngredients }}</strong><span>loại nguyên liệu</span></div><div><strong>{{ formatCurrency(createdReceiptInfo.totalAmount) }}</strong><span>giá trị dự kiến</span></div><p>Định mức xuất theo BOM, không trừ hao hụt thêm.</p></div>
        <div class="result-table-wrap"><table class="result-table"><thead><tr><th>Nguyên liệu</th><th>Lượng cần xuất</th><th>Lô còn hạn</th><th>Kho</th></tr></thead><tbody>
          <tr v-for="item in createdReceiptInfo.items" :key="item.inventoryItemId"><td><strong>{{ item.itemName }}</strong></td><td class="text-blue strong-number">{{ formatNumber(item.quantityRequired) }} {{ item.baseUnit }}</td><td><strong>{{ formatNumber(item.availableStock ?? item.currentStock) }} {{ item.baseUnit }}</strong><small v-if="item.expiredStock > 0" class="expired-stock">Không tính {{ formatNumber(item.expiredStock) }} đã hết hạn</small></td><td><span class="stock-check" :class="item.isStockSufficient ? 'stock-ok' : 'stock-low'">{{ item.isStockSufficient ? 'Đủ lô còn hạn' : 'Thiếu lô còn hạn' }}</span></td></tr>
        </tbody></table></div>
        <div class="modal-footer"><router-link to="/admin/stock-receipts" class="button button-secondary">Xem phiếu kho</router-link><button class="button button-primary" @click="showRequisitionResultModal = false">Hoàn tất</button></div>
      </div>
    </div>

    <!-- Edit modal -->
    <div v-if="showEditModal && editingPortion" class="modal-backdrop-custom">
      <div class="modal-panel modal-panel-small"><div class="modal-header"><div><span class="modal-kicker">CHỈNH SỬA KẾ HOẠCH</span><h2>{{ editingPortion.foodName }}</h2><p>{{ editingPortion.variantName }} · {{ formatDateVN(selectedDate) }}</p></div><button class="close-button" title="Đóng" @click="showEditModal = false">×</button></div><form @submit.prevent="saveEditPortion" class="edit-form"><label>Số suất cần phục vụ<input v-model.number="editForm.plannedPortions" type="number" min="0" step="1" required /></label><p class="form-help">Số lượng này dùng để nhân định mức BOM và lập phiếu xuất bếp. Số đã phục vụ được hệ thống cập nhật tự động từ order.</p><label>Ghi chú<textarea v-model="editForm.note" rows="3" placeholder="Ví dụ: đã chốt theo đặt bàn / dự kiến trưa"></textarea></label><div class="modal-footer"><button type="button" class="button button-secondary" @click="showEditModal = false">Hủy</button><button type="submit" class="button button-primary">Lưu thay đổi</button></div></form></div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue';
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

function formatNumber(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 2 });
}

function formatCurrency(value) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0);
}

function formatDateVN(value) {
  if (!value) return '';
  return parseDateInput(value).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function formatDateTime(value) {
  if (!value) return 'Chưa có thời gian';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Không xác định';
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  });
}

const selectedDate = ref(formatDateInput());
const loading = ref(false);
const loadError = ref('');
const portions = ref([]);
const categories = ref([]);
const hasRequisition = ref(false);
const additionalReceipts = ref([]);
const pendingAdditionalReceiptCount = computed(() => additionalReceipts.value.filter((receipt) => receipt.status === 'draft').length);
const hasPendingAdditionalReceipt = computed(() => pendingAdditionalReceiptCount.value > 0);
const initialReceiptStatus = ref(null);
const canGenerateAdditionalRequisition = ref(false);
const shortageInfo = ref(null);
const summary = ref({ totalDishes: 0, totalPlanned: 0, totalCarryOver: 0, totalNeedToCook: 0, totalCooked: 0, totalSold: 0, totalSystemRemain: 0 });

const searchQuery = ref('');
const selectedCategoryFilter = ref('');

const showPlanModal = ref(false);
const showAdditionalModal = ref(false);
const showBomModal = ref(false);
const showRequisitionResultModal = ref(false);
const showEditModal = ref(false);
const selectedPortionBom = ref(null);
const createdReceiptInfo = ref(null);
const createdReceiptKind = ref('initial');
const additionalRows = ref([]);
const additionalNote = ref('');
const editingPortion = ref(null);
const editForm = ref({ plannedPortions: 0, carryOverPortions: 0, cookedPortions: 0, status: 'planned', note: '' });

const allDishesWithVariants = ref([]);
const planDishSearch = ref('');
const planCategoryFilter = ref('');
const additionalSearch = ref('');
const additionalCategoryFilter = ref('');
const isSubmittingPlan = ref(false);
const isGeneratingRequisition = ref(false);
const isGeneratingAdditional = ref(false);
let hubConnection = null;

const filteredPortions = computed(() => portions.value.filter((p) => {
  const query = searchQuery.value.trim().toLowerCase();
  const matchesSearch = !query || p.foodName.toLowerCase().includes(query) || (p.variantName || '').toLowerCase().includes(query);
  const matchesCategory = !selectedCategoryFilter.value || p.categoryId === Number(selectedCategoryFilter.value);
  return matchesSearch && matchesCategory;
}));

function groupRows(rows) {
  const groups = new Map();
  rows.forEach((row) => {
    const key = String(row.foodId);
    if (!groups.has(key)) {
      groups.set(key, { foodId: row.foodId, foodName: row.foodName, categoryName: row.categoryName || 'Món ăn', rows: [], items: [] });
    }
    const group = groups.get(key);
    group.rows.push(row);
    group.items.push(row);
  });
  return Array.from(groups.values());
}

const groupedPortions = computed(() => groupRows(filteredPortions.value));

const filteredPlanRows = computed(() => allDishesWithVariants.value.filter((row) => {
  const query = planDishSearch.value.trim().toLowerCase();
  const matchesSearch = !query || row.foodName.toLowerCase().includes(query) || row.variantName.toLowerCase().includes(query);
  const matchesCategory = !planCategoryFilter.value || row.categoryId === Number(planCategoryFilter.value);
  return matchesSearch && matchesCategory;
}));

const groupedPlanRows = computed(() => groupRows(filteredPlanRows.value));
const selectedPlanRowsCount = computed(() => allDishesWithVariants.value.filter((row) => row.selected && Number(row.plannedPortions) > 0).length);
const selectedPlanPortionsTotal = computed(() => allDishesWithVariants.value
  .filter((row) => row.selected)
  .reduce((total, row) => total + Math.max(0, Number(row.plannedPortions) || 0), 0));
const selectedAdditionalPortionsTotal = computed(() => additionalRows.value
  .filter((row) => row.selected)
  .reduce((total, row) => total + Math.max(0, Number(row.additionalPortions) || 0), 0));
const selectedAdditionalRowsCount = computed(() => additionalRows.value.filter((row) => row.selected && Number(row.additionalPortions) > 0).length);
const filteredAdditionalRows = computed(() => additionalRows.value.filter((row) => {
  const query = additionalSearch.value.trim().toLowerCase();
  const matchesSearch = !query || row.foodName.toLowerCase().includes(query) || row.variantName.toLowerCase().includes(query);
  const matchesCategory = !additionalCategoryFilter.value || row.categoryId === Number(additionalCategoryFilter.value);
  return matchesSearch && matchesCategory;
}));
const groupedAdditionalRows = computed(() => groupRows(filteredAdditionalRows.value));
const isAllAdditionalSelected = computed(() => filteredAdditionalRows.value.length > 0 && filteredAdditionalRows.value.every((row) => row.selected));
const isAllSelectedForPlan = computed(() => filteredPlanRows.value.length > 0 && filteredPlanRows.value.every((row) => row.selected));

function getGroupSelectedCount(group) {
  return group.rows.filter((row) => row.selected).length;
}

function setToday() {
  selectedDate.value = formatDateInput();
  fetchDailyPortions();
}

function changeDateOffset(days) {
  const date = parseDateInput(selectedDate.value);
  date.setDate(date.getDate() + days);
  selectedDate.value = formatDateInput(date);
  fetchDailyPortions();
}

async function fetchDailyPortions() {
  loading.value = true;
  loadError.value = '';
  shortageInfo.value = null;
  try {
    const response = await axios.get(API_BASE, { params: { date: selectedDate.value }, timeout: 8000 });
    if (!response.data?.success) throw new Error(response.data?.message || 'Máy chủ không trả về dữ liệu hợp lệ.');

    portions.value = Array.isArray(response.data.data) ? response.data.data : [];
    summary.value = response.data.summary || summary.value;
    hasRequisition.value = Boolean(response.data.hasRequisition);
    additionalReceipts.value = Array.isArray(response.data.additionalReceipts) ? response.data.additionalReceipts : [];
    initialReceiptStatus.value = response.data.initialReceiptStatus || null;
    canGenerateAdditionalRequisition.value = Boolean(response.data.canGenerateAdditionalRequisition);
  } catch (error) {
    console.error('Không thể tải điều phối suất ăn:', error);
    const status = error.response?.status;
    loadError.value = status === 401 || status === 403
      ? 'Phiên đăng nhập đã hết hạn hoặc tài khoản không có quyền. Hãy đăng nhập lại.'
      : error.code === 'ECONNABORTED'
        ? 'Máy chủ phản hồi quá 8 giây. Hãy kiểm tra API rồi thử lại.'
        : error.response?.data?.message || 'Không kết nối được máy chủ API.';
  } finally {
    loading.value = false;
  }
}

async function fetchCategories() {
  try {
    const response = await axios.get(CATEGORY_API);
    if (response.data?.success) categories.value = response.data.data || [];
  } catch (error) {
    console.error('Không thể tải danh mục món:', error);
  }
}

async function generateRequisition() {
  if (hasRequisition.value) return;
  if (!confirm(`Lập phiếu xuất bếp theo ${formatNumber(summary.value.totalNeedToCook)} suất trong ngày ${formatDateVN(selectedDate.value)}? Phiếu sẽ chờ thủ kho duyệt.`)) return;
  isGeneratingRequisition.value = true;
  try {
    const response = await axios.post(`${API_BASE}/generate-requisition`, { controlDate: selectedDate.value, note: `Phiếu xuất kho bếp tự động từ điều phối suất ăn ngày ${selectedDate.value}` });
    if (response.data?.success) {
      createdReceiptInfo.value = response.data.receipt;
      createdReceiptKind.value = 'initial';
      showRequisitionResultModal.value = true;
      hasRequisition.value = true;
      initialReceiptStatus.value = 'draft';
      canGenerateAdditionalRequisition.value = false;
      isGeneratingRequisition.value = false;
      void fetchDailyPortions();
    }
  } catch (error) {
    const data = error.response?.data;
    if (error.response?.status === 409 && data?.code === 'INSUFFICIENT_STOCK') {
      shortageInfo.value = { message: data.message, shortages: data.shortages || [] };
    } else {
      alert(`Không thể lập phiếu xuất bếp: ${data?.message || error.message}`);
    }
  } finally {
    isGeneratingRequisition.value = false;
  }
}

function openAdditionalModal() {
  additionalRows.value = portions.value.map((portion) => ({
    portionControlId: portion.portionControlId,
    foodId: portion.foodId,
    variantId: portion.variantId,
    foodName: portion.foodName,
    categoryId: portion.categoryId,
    categoryName: portion.categoryName,
    variantName: portion.variantName,
    unit: portion.unit,
    soldPortions: portion.soldPortions,
    additionalPortions: 0,
    selected: false
  }));
  additionalSearch.value = '';
  additionalCategoryFilter.value = '';
  additionalNote.value = '';
  shortageInfo.value = null;
  showAdditionalModal.value = true;
}

function openQuickAdditionalForDish(portion) {
  openAdditionalModal();
  const row = additionalRows.value.find((item) => item.portionControlId === portion.portionControlId);
  if (!row) return;
  row.selected = true;
  row.additionalPortions = 10;
}

async function submitAdditionalRequisition() {
  const items = additionalRows.value
    .filter((row) => row.selected && Number(row.additionalPortions) > 0)
    .map((row) => ({
      foodId: row.foodId,
      variantId: row.variantId,
      additionalPortions: Number(row.additionalPortions)
    }));

  if (!items.length) return;
  isGeneratingAdditional.value = true;
  try {
    const response = await axios.post(`${API_BASE}/generate-additional-requisition`, {
      controlDate: selectedDate.value,
      items,
      note: additionalNote.value.trim() || `Phiếu xuất bếp bổ sung từ order phát sinh ngày ${selectedDate.value}`
    }, { timeout: 5000 });
    if (response.data?.success) {
      createdReceiptInfo.value = response.data.receipt;
      createdReceiptKind.value = 'additional';
      showAdditionalModal.value = false;
      showRequisitionResultModal.value = true;
      hasRequisition.value = true;
      canGenerateAdditionalRequisition.value = true;
      isGeneratingAdditional.value = false;
      void fetchDailyPortions();
    }
  } catch (error) {
    const data = error.response?.data;
    if (error.response?.status === 409 && data?.code === 'INSUFFICIENT_STOCK') {
      shortageInfo.value = { message: data.message, shortages: data.shortages || [] };
      showAdditionalModal.value = false;
    } else if (error.code === 'ECONNABORTED' || error.message?.toLowerCase().includes('timeout')) {
      alert('Máy chủ phản hồi quá lâu. Hãy vào Phiếu kho kiểm tra mã PXK-BS trước khi bấm lập lại để tránh tạo trùng phiếu.');
    } else {
      alert(`Không thể lập phiếu bổ sung: ${data?.message || error.message}`);
    }
  } finally {
    isGeneratingAdditional.value = false;
  }
}

function toggleSelectAllAdditional() {
  const select = !isAllAdditionalSelected.value;
  filteredAdditionalRows.value.forEach((row) => { row.selected = select; });
}

function clearAdditionalInputs() {
  additionalRows.value.forEach((row) => { row.selected = false; row.additionalPortions = 0; });
}

async function viewPortionBom(portion) {
  try {
    const response = await axios.get(`${API_BASE}/${portion.portionControlId}`);
    if (response.data?.success) {
      selectedPortionBom.value = response.data.data;
      showBomModal.value = true;
    }
  } catch (error) {
    alert(`Không thể xem định mức: ${error.response?.data?.message || error.message}`);
  }
}

function openEditPortionModal(portion) {
  editingPortion.value = portion;
  editForm.value = { plannedPortions: portion.plannedPortions, carryOverPortions: 0, cookedPortions: 0, status: portion.status, note: portion.note || '' };
  showEditModal.value = true;
}

async function saveEditPortion() {
  try {
    const response = await axios.put(`${API_BASE}/${editingPortion.value.portionControlId}`, {
      plannedPortions: Number(editForm.value.plannedPortions),
      note: editForm.value.note
    });
    if (response.data?.success) {
      showEditModal.value = false;
      await fetchDailyPortions();
    }
  } catch (error) {
    alert(`Không thể lưu thay đổi: ${error.response?.data?.message || error.message}`);
  }
}

async function deletePortion(portion) {
  if (!confirm(`Xóa ${portion.foodName} - ${portion.variantName} khỏi kế hoạch ngày?`)) return;
  try {
    const response = await axios.delete(`${API_BASE}/${portion.portionControlId}`);
    if (response.data?.success) await fetchDailyPortions();
  } catch (error) {
    alert(`Không thể xóa món: ${error.response?.data?.message || error.message}`);
  }
}

async function openPlanModal() {
  try {
    const response = await axios.get(FOOD_API, { params: { isActive: true } });
    const foods = response.data?.data || [];
    const existingMap = {};
    portions.value.forEach((portion) => { existingMap[`${portion.foodId}-${portion.variantId}`] = portion; });
    const rows = [];
    foods.forEach((food) => {
      const variants = food.variants?.length ? food.variants : [{ variantId: null, variantName: 'Mặc định' }];
      variants.forEach((variant) => {
        const existing = existingMap[`${food.foodId}-${variant.variantId}`];
        rows.push({ foodId: food.foodId, foodName: food.foodName, categoryId: food.categoryId, categoryName: food.categoryName || 'Món ăn', variantId: variant.variantId, variantName: variant.variantName, plannedPortions: existing?.plannedPortions || 0, carryOverPortions: 0, selected: Boolean(existing) });
      });
    });
    allDishesWithVariants.value = rows;
    showPlanModal.value = true;
  } catch (error) {
    alert(`Không thể tải danh sách món: ${error.response?.data?.message || error.message}`);
  }
}

function toggleSelectAllPlan() {
  const select = !isAllSelectedForPlan.value;
  filteredPlanRows.value.forEach((row) => { row.selected = select; });
}

function clearAllPlanInputs() {
  allDishesWithVariants.value.forEach((row) => { row.selected = false; row.plannedPortions = 0; });
}

async function submitBatchPlan() {
  const items = allDishesWithVariants.value.filter((row) => row.selected && Number(row.plannedPortions) > 0).map((row) => ({ foodId: row.foodId, variantId: row.variantId, plannedPortions: Number(row.plannedPortions), carryOverPortions: 0 }));
  if (!items.length) { alert('Hãy chọn ít nhất một biến thể có số suất kế hoạch lớn hơn 0.'); return; }
  isSubmittingPlan.value = true;
  try {
    const response = await axios.post(`${API_BASE}/batch-plan`, { controlDate: selectedDate.value, items });
    if (response.data?.success) { showPlanModal.value = false; await fetchDailyPortions(); }
  } catch (error) {
    alert(`Không thể lưu kế hoạch: ${error.response?.data?.message || error.message}`);
  } finally {
    isSubmittingPlan.value = false;
  }
}

onMounted(async () => {
  await Promise.all([fetchCategories(), fetchDailyPortions()]);
  try {
    hubConnection = new signalR.HubConnectionBuilder().withUrl('http://localhost:5079/hubs/restaurant', { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets }).withAutomaticReconnect().build();
    hubConnection.on('KitchenUpdated', fetchDailyPortions);
    hubConnection.on('InventoryUpdated', fetchDailyPortions);
    await hubConnection.start();
  } catch (error) {
    console.warn('SignalR không kết nối được, màn hình vẫn dùng tải lại thủ công:', error.message);
  }
});

onUnmounted(() => { if (hubConnection) hubConnection.stop(); });
</script>

<style scoped>
.daily-portions-page { min-height: calc(100vh - 70px); padding: 24px; color: #172033; background: #f5f7fb; }
.page-header { display: flex; justify-content: space-between; gap: 24px; align-items: flex-start; padding: 24px 28px; background: #0f766e; color: #fff; border-radius: 10px; }
.page-header { padding: 16px 20px; align-items: center; }
.page-heading h1 { margin: 4px 0 4px; font-size: 24px; line-height: 1.15; letter-spacing: 0; }
.page-heading p { margin: 0; color: rgba(255,255,255,.78); }
.eyebrow, .modal-kicker { font-size: 11px; font-weight: 800; letter-spacing: .12em; color: #a7f3d0; }
.page-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; }
.date-picker-wrap { display: flex; align-items: center; gap: 4px; padding: 4px; background: rgba(255,255,255,.12); border: 1px solid rgba(255,255,255,.25); border-radius: 7px; }
.date-picker-wrap input { width: 135px; padding: 7px 8px; color: #172033; background: #fff; border: 0; border-radius: 5px; font-weight: 700; }
.button, .icon-button, .today-button { border: 1px solid transparent; border-radius: 6px; font: inherit; cursor: pointer; transition: .15s ease; }
.button { display: inline-flex; align-items: center; gap: 7px; min-height: 38px; padding: 8px 13px; font-size: 13px; font-weight: 700; white-space: nowrap; }
.button:disabled { opacity: .55; cursor: not-allowed; }
.button-light { color: #fff; background: rgba(255,255,255,.12); border-color: rgba(255,255,255,.28); }
.button-light:hover { background: rgba(255,255,255,.2); }
.button-warning { color: #172033; background: #fbbf24; }
.button-primary { color: #fff; background: #0f766e; border-color: #0f766e; }
.button-secondary { color: #334155; background: #fff; border-color: #cbd5e1; }
.button-secondary:hover { background: #f8fafc; }
.icon-button, .today-button { min-width: 32px; min-height: 32px; padding: 4px 8px; color: #fff; background: transparent; border-color: transparent; font-weight: 800; }
.today-button { color: #0f766e; background: #fff; }
.workflow-strip { display: flex; align-items: center; gap: 12px; margin: 16px 0; padding: 14px 18px; background: #fff; border: 1px solid #dbe4ec; border-radius: 8px; }
.workflow-step { display: flex; align-items: center; gap: 9px; min-width: 190px; color: #64748b; }
.workflow-step strong, .workflow-step small { display: block; }
.workflow-step strong { font-size: 13px; color: #334155; }
.workflow-step small { margin-top: 2px; font-size: 11px; }
.workflow-step-active .workflow-number { color: #fff; background: #0f766e; }
.workflow-step-active strong { color: #0f766e; }
.workflow-number { display: grid; place-items: center; width: 28px; height: 28px; color: #475569; background: #e2e8f0; border-radius: 50%; font-weight: 800; }
.workflow-line { flex: 1; height: 1px; background: #dbe4ec; }
.workflow-status { display: flex; align-items: center; gap: 7px; margin-left: auto; padding-left: 12px; font-size: 12px; font-weight: 700; white-space: nowrap; }
.status-dot { width: 8px; height: 8px; border-radius: 50%; background: currentColor; }
.status-ready { color: #15803d; }.status-pending { color: #b45309; }
.summary-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 16px; }
.plan-overview { display: flex; flex-wrap: wrap; gap: 24px; align-items: center; margin: 12px 0; padding: 10px 14px; color: #475569; background: #fff; border: 1px solid #dbe4ec; border-radius: 7px; font-size: 13px; }
.plan-overview strong { color: #172033; font-size: 15px; }
.overview-status { display: inline-flex; align-items: center; gap: 7px; margin-left: auto; font-weight: 700; }
.summary-card { padding: 16px 18px; background: #fff; border: 1px solid #dbe4ec; border-top: 3px solid #94a3b8; border-radius: 8px; }
.summary-card-blue { border-top-color: #2563eb; }.summary-card-cyan { border-top-color: #0891b2; }.summary-card-orange { border-top-color: #f59e0b; }.summary-card-green { border-top-color: #16a34a; }.summary-card-attention { box-shadow: 0 0 0 2px rgba(245,158,11,.12); }
.summary-card-top { display: flex; justify-content: space-between; align-items: center; }.summary-label { color: #64748b; font-size: 12px; font-weight: 800; text-transform: uppercase; }.summary-icon { color: #64748b; font-size: 12px; font-weight: 800; }
.summary-card > strong { display: block; margin-top: 7px; color: #172033; font-size: 25px; line-height: 1.1; }.summary-card > strong small { color: #64748b; font-size: 12px; font-weight: 600; }.summary-card p { margin: 8px 0 0; color: #64748b; font-size: 12px; }
.content-section { overflow: hidden; background: #fff; border: 1px solid #dbe4ec; border-radius: 8px; }
.section-toolbar { display: flex; justify-content: space-between; gap: 18px; align-items: center; padding: 18px 20px; border-bottom: 1px solid #e2e8f0; }.section-toolbar h2 { margin: 0; font-size: 19px; letter-spacing: 0; }.section-toolbar p { margin: 5px 0 0; color: #64748b; font-size: 12px; }.toolbar-controls, .plan-toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.search-input, .select-input { min-height: 36px; padding: 7px 10px; color: #172033; background: #fff; border: 1px solid #cbd5e1; border-radius: 6px; font: inherit; font-size: 13px; }.search-input { width: 215px; }.select-input { min-width: 145px; }.status-filter { min-width: 135px; }.icon-button-muted { color: #334155; background: #f8fafc; border-color: #cbd5e1; }
.empty-state { display: flex; align-items: center; flex-direction: column; gap: 8px; padding: 64px 20px; color: #64748b; text-align: center; }.empty-state strong { color: #334155; }.empty-state.compact { padding: 28px; }.empty-actions { display: flex; gap: 8px; margin-top: 10px; }
.food-groups { display: grid; gap: 12px; padding: 14px; background: #f8fafc; }.food-group { overflow: hidden; background: #fff; border: 1px solid #dbe4ec; border-radius: 8px; }.food-group-header { display: flex; justify-content: space-between; gap: 16px; align-items: center; padding: 15px 16px; border-bottom: 1px solid #e2e8f0; }.food-title-wrap { display: flex; align-items: center; gap: 11px; }.food-mark { display: grid; place-items: center; width: 34px; height: 34px; color: #0f766e; background: #d1fae5; border-radius: 7px; font-weight: 900; }.food-title-wrap h3 { margin: 0; font-size: 16px; }.food-subtitle { display: flex; align-items: center; gap: 8px; margin-top: 5px; color: #64748b; font-size: 12px; }.category-tag { padding: 3px 7px; color: #475569; background: #f1f5f9; border-radius: 4px; }.food-summary { display: flex; gap: 24px; }.food-summary div { min-width: 65px; text-align: right; }.food-summary span, .food-summary strong { display: block; }.food-summary span { color: #64748b; font-size: 11px; }.food-summary strong { margin-top: 3px; font-size: 15px; }.text-orange { color: #b45309 !important; }.text-green { color: #15803d !important; }.text-blue { color: #1d4ed8 !important; }.text-cyan { color: #0e7490 !important; }.strong-number { font-weight: 800; }
.variant-table-wrap { overflow-x: auto; }.variant-grid { display: grid; grid-template-columns: minmax(180px, 1.25fr) 92px 92px 104px minmax(180px, 1.2fr) 76px 104px 116px; gap: 10px; align-items: center; min-width: 1060px; padding: 10px 16px; }.variant-grid-head { color: #64748b; background: #f8fafc; font-size: 10px; font-weight: 800; text-transform: uppercase; }.variant-grid-row { min-height: 72px; border-top: 1px solid #eef2f6; font-size: 13px; }.variant-grid-row:hover { background: #fbfefd; }.align-right { text-align: right; }.align-center { text-align: center; }.variant-name-cell strong, .variant-name-cell small, .variant-name-cell .status-pill { display: block; }.variant-name-cell strong { color: #172033; }.variant-name-cell small { margin-top: 4px; color: #64748b; }.status-pill { width: max-content; margin-top: 6px; padding: 3px 6px; border: 1px solid transparent; border-radius: 4px; font-size: 10px; font-weight: 700; }.status-planned { color: #475569; background: #f1f5f9; border-color: #cbd5e1; }.status-service { color: #1d4ed8; background: #eff6ff; border-color: #bfdbfe; }.status-reconciled { color: #a16207; background: #fefce8; border-color: #fde68a; }.status-closed { color: #fff; background: #475569; }.number-cell { text-align: right; font-weight: 700; white-space: nowrap; }.number-cell small { color: #64748b; font-size: 10px; font-weight: 500; }.cooked-cell { min-width: 0; }.cooked-input-row { display: flex; gap: 4px; }.cooked-input-row input { width: 70px; min-height: 32px; padding: 5px; text-align: center; border: 1px solid #cbd5e1; border-radius: 5px; font: inherit; font-weight: 700; }.cooked-input-row button { min-width: 32px; padding: 3px 4px; color: #334155; background: #fff; border: 1px solid #cbd5e1; border-radius: 5px; cursor: pointer; font-size: 11px; font-weight: 700; }.progress-track { height: 4px; margin-top: 7px; overflow: hidden; background: #e2e8f0; border-radius: 4px; }.progress-track span { display: block; height: 100%; transition: width .2s ease; }.progress-start { background: #f59e0b; }.progress-half { background: #2563eb; }.progress-complete { background: #16a34a; }.remain-pill { display: inline-block; padding: 5px 7px; border-radius: 5px; font-size: 12px; font-weight: 800; white-space: nowrap; }.remain-positive { color: #166534; background: #dcfce7; }.remain-zero { color: #64748b; background: #f1f5f9; }.actions-cell { display: flex; justify-content: center; gap: 4px; }.row-action { width: 29px; height: 29px; color: #475569; background: #fff; border: 1px solid #cbd5e1; border-radius: 5px; cursor: pointer; font-size: 16px; line-height: 1; }.row-action:disabled { opacity: .35; cursor: not-allowed; }.action-info { color: #1d4ed8; }.action-edit { color: #a16207; }.action-delete { color: #dc2626; }
.modal-backdrop-custom { position: fixed; inset: 0; z-index: 1050; display: flex; align-items: center; justify-content: center; padding: 20px; background: rgba(15,23,42,.62); }.modal-panel { width: 100%; max-height: calc(100vh - 40px); overflow: auto; background: #fff; border-radius: 10px; box-shadow: 0 18px 50px rgba(15,23,42,.22); }.modal-panel-wide { max-width: 1060px; }.modal-panel-medium { max-width: 850px; }.modal-panel-small { max-width: 500px; }.modal-header { display: flex; justify-content: space-between; gap: 20px; padding: 20px 22px 16px; border-bottom: 1px solid #e2e8f0; }.modal-header h2 { margin: 4px 0; font-size: 21px; }.modal-header p { margin: 0; color: #64748b; font-size: 12px; }.modal-kicker { color: #0f766e; }.modal-kicker-success { color: #15803d; }.close-button { width: 32px; height: 32px; color: #64748b; background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 5px; cursor: pointer; font-size: 22px; line-height: 1; }.plan-toolbar { padding: 14px 22px 10px; }.plan-toolbar .search-input { flex: 1; min-width: 210px; }.plan-note { margin: 0 22px 12px; padding: 10px 12px; color: #475569; background: #f0fdfa; border: 1px solid #ccfbf1; border-radius: 6px; font-size: 12px; }.plan-list { display: grid; gap: 10px; max-height: 54vh; overflow-y: auto; padding: 0 22px 14px; }.plan-food-group { overflow: hidden; border: 1px solid #dbe4ec; border-radius: 7px; }.plan-food-heading { display: flex; justify-content: space-between; gap: 12px; align-items: center; padding: 11px 13px; background: #f8fafc; }.plan-food-heading strong, .plan-food-heading span { display: block; }.plan-food-heading span { margin-top: 3px; color: #64748b; font-size: 11px; }.selection-count { margin: 0 !important; color: #0f766e !important; font-weight: 700; }.plan-row { display: grid; grid-template-columns: 28px minmax(150px, 1fr) 150px 150px 125px; gap: 12px; align-items: center; padding: 10px 13px; border-top: 1px solid #eef2f6; }.plan-row-selected { background: #f0fdfa; }.plan-check input { width: 17px; height: 17px; accent-color: #0f766e; }.plan-variant strong, .plan-variant small { display: block; }.plan-variant small { margin-top: 3px; color: #64748b; font-size: 11px; }.plan-field span, .plan-needed span { display: block; color: #64748b; font-size: 10px; font-weight: 700; }.plan-field input { width: 100%; margin-top: 4px; padding: 7px 8px; border: 1px solid #cbd5e1; border-radius: 5px; text-align: right; font: inherit; }.plan-needed { text-align: right; }.plan-needed strong { display: block; margin-top: 4px; color: #b45309; font-size: 16px; }.modal-footer { display: flex; justify-content: space-between; align-items: center; gap: 10px; padding: 14px 22px; border-top: 1px solid #e2e8f0; color: #64748b; font-size: 12px; }.modal-footer > div { display: flex; gap: 8px; }.bom-table-wrap, .result-table-wrap { overflow-x: auto; padding: 16px 22px; }.bom-table, .result-table { width: 100%; border-collapse: collapse; min-width: 700px; }.bom-table th, .bom-table td, .result-table th, .result-table td { padding: 10px 8px; border-bottom: 1px solid #e2e8f0; text-align: left; font-size: 12px; vertical-align: middle; }.bom-table th, .result-table th { color: #64748b; background: #f8fafc; font-size: 10px; text-transform: uppercase; }.bom-table td small, .result-table td small { display: block; margin-top: 4px; color: #64748b; font-size: 10px; }.empty-cell { padding: 28px !important; text-align: center !important; color: #64748b; }.stock-check { display: inline-block; margin-left: 6px; padding: 3px 6px; border-radius: 4px; font-size: 10px; font-weight: 800; }.stock-ok { color: #166534; background: #dcfce7; }.stock-low { color: #b91c1c; background: #fee2e2; }.muted-note { color: #64748b; font-size: 11px; }.result-summary { display: flex; gap: 28px; align-items: center; margin: 16px 22px 0; padding: 14px; background: #f0fdfa; border: 1px solid #ccfbf1; border-radius: 7px; }.result-summary div strong, .result-summary div span { display: block; }.result-summary div strong { color: #0f766e; font-size: 20px; }.result-summary div span { margin-top: 2px; color: #64748b; font-size: 11px; }.result-summary p { margin: 0 0 0 auto; color: #64748b; font-size: 11px; }.wastage-badge { display: inline-block; padding: 4px 7px; color: #a16207; background: #fef3c7; border: 1px solid #fcd34d; border-radius: 4px; font-weight: 800; }.result-table details { margin-top: 5px; color: #1d4ed8; font-size: 10px; cursor: pointer; }.result-table details div { margin-top: 3px; color: #64748b; }.dish-use-list { max-width: 260px; color: #64748b; }.edit-form { display: grid; gap: 13px; padding: 18px 22px 0; }.edit-form label { color: #334155; font-size: 12px; font-weight: 700; }.edit-form input, .edit-form select, .edit-form textarea { display: block; width: 100%; margin-top: 5px; padding: 8px 10px; border: 1px solid #cbd5e1; border-radius: 5px; font: inherit; font-weight: 400; }.edit-form .modal-footer { margin: 4px -22px 0; padding-bottom: 20px; }.loading-spinner { width: 16px; height: 16px; border: 2px solid #cbd5e1; border-top-color: #0f766e; border-radius: 50%; animation: spin .7s linear infinite; }@keyframes spin { to { transform: rotate(360deg); } }
@media (max-width: 1100px) { .page-header, .section-toolbar { flex-direction: column; align-items: stretch; }.page-actions, .toolbar-controls { justify-content: flex-start; }.result-summary { flex-wrap: wrap; }.result-summary p { flex-basis: 100%; margin: 0; } }
@media (max-width: 650px) { .daily-portions-page { padding: 12px; }.page-header { padding: 18px; }.page-heading h1 { font-size: 24px; }.page-actions, .date-picker-wrap { width: 100%; }.page-actions .button { flex: 1; justify-content: center; }.date-picker-wrap input { flex: 1; }.summary-grid { grid-template-columns: 1fr; }.section-toolbar { padding: 15px; }.toolbar-controls { align-items: stretch; }.search-input, .select-input { width: 100%; }.food-group-header { align-items: flex-start; flex-direction: column; }.food-summary { width: 100%; justify-content: space-between; }.modal-backdrop-custom { padding: 8px; }.modal-header, .modal-footer { padding-left: 15px; padding-right: 15px; }.plan-toolbar { padding: 12px 15px 8px; }.plan-note { margin-left: 15px; margin-right: 15px; }.plan-list { padding-left: 15px; padding-right: 15px; }.plan-row { grid-template-columns: 25px 1fr; }.plan-field, .plan-needed { grid-column: 2; }.plan-needed { text-align: left; }.plan-toolbar .button { flex: 1; }.result-summary { margin-left: 15px; margin-right: 15px; }.bom-table-wrap, .result-table-wrap { padding-left: 15px; padding-right: 15px; } }
@media (max-width: 650px) { .plan-overview { gap: 10px 18px; }.overview-status { margin-left: 0; } }
.plan-row { grid-template-columns: 28px minmax(150px, 1fr) 170px 150px; }
.form-help { margin: -5px 0 0; color: #64748b; font-size: 12px; line-height: 1.5; }
.shortage-banner { margin: 0 0 16px; padding: 14px 16px; color: #7f1d1d; background: #fff7ed; border: 1px solid #fdba74; border-left: 4px solid #ea580c; border-radius: 8px; }
.shortage-heading { display: flex; flex-wrap: wrap; gap: 8px 14px; align-items: baseline; }.shortage-heading strong { color: #c2410c; }.shortage-heading span, .shortage-actions span { font-size: 13px; line-height: 1.5; }.shortage-actions { display: flex; justify-content: space-between; gap: 12px; align-items: center; margin-top: 6px; color: #9a3412; }.shortage-actions .icon-button { flex: 0 0 auto; color: #9a3412; }.shortage-list { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 8px; margin-top: 12px; }.shortage-item { display: grid; gap: 3px; padding: 9px 10px; color: #7c2d12; background: #fff; border: 1px solid #fed7aa; border-radius: 6px; font-size: 12px; }.shortage-item span { color: #9a3412; }.shortage-item b { color: #dc2626; }
.shortage-item small, .expired-stock { display: block; color: #b45309; font-size: 11px; line-height: 1.35; }
.workflow-strip { flex-wrap: wrap; }
.additional-list { max-height: 54vh; overflow-y: auto; padding: 0 22px 14px; }.additional-list-head, .additional-row { display: grid; grid-template-columns: minmax(220px, 1fr) 150px 180px; gap: 14px; align-items: center; }.additional-list-head { padding: 9px 12px; color: #64748b; background: #f8fafc; border: 1px solid #dbe4ec; border-radius: 7px 7px 0 0; font-size: 10px; font-weight: 800; text-transform: uppercase; }.additional-row { min-height: 58px; padding: 9px 12px; border: 1px solid #e2e8f0; border-top: 0; font-size: 13px; }.additional-row strong, .additional-row small { display: block; }.additional-row small { margin-top: 3px; color: #64748b; }.additional-row input { width: 100%; min-height: 36px; padding: 7px 9px; border: 1px solid #cbd5e1; border-radius: 6px; text-align: right; font: inherit; font-weight: 700; }.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
@media (max-width: 780px) { .workflow-line { display: none; }.workflow-step { min-width: 0; flex: 1 1 30%; }.workflow-status { width: 100%; margin-left: 0; padding-left: 0; } }
@media (max-width: 650px) { .additional-list { padding-left: 15px; padding-right: 15px; }.additional-list-head { display: none; }.additional-row { grid-template-columns: 1fr 120px; gap: 10px; }.additional-row > span { grid-column: 1; grid-row: 2; }.additional-row > label { grid-column: 2; grid-row: 1 / span 2; } }
.additional-receipt-modal { display: flex; flex-direction: column; max-height: calc(100vh - 32px); }
.additional-form-scroll { min-height: 0; overflow-y: auto; padding: 18px 22px 0; overscroll-behavior: contain; }
.additional-intro { display: flex; align-items: flex-start; gap: 12px; padding: 12px 14px; color: #315044; background: #f0faf5; border: 1px solid #cfeadd; border-radius: 8px; }
.additional-intro-icon { display: grid; width: 28px; height: 28px; flex: 0 0 auto; place-items: center; color: #fff; background: #16805f; border-radius: 7px; font-size: 1.1rem; font-weight: 700; }.additional-intro strong, .additional-intro span { display: block; }.additional-intro span { margin-top: 3px; color: #667a70; font-size: 12px; line-height: 1.45; }
.additional-note-field { display: block; margin: 16px 0; color: #334155; font-size: 12px; font-weight: 700; }.additional-note-field input { display: block; width: 100%; margin-top: 6px; padding: 9px 10px; border: 1px solid #cbd5e1; border-radius: 6px; font: inherit; font-weight: 400; }
.additional-section-heading { display: flex; justify-content: space-between; gap: 12px; align-items: center; margin: 4px 0 8px; color: #273b32; }.additional-section-heading strong, .additional-section-heading span { display: block; }.additional-section-heading span { margin-top: 3px; color: #667a70; font-size: 12px; font-weight: 400; }.additional-section-heading b { padding: 4px 8px; color: #126f54; background: #e4f5ee; border-radius: 999px; font-size: 11px; white-space: nowrap; }
.additional-list { padding: 0; }.additional-row { display: block; min-height: 0; padding: 12px 0; border: 0; border-bottom: 1px solid #edf2ef; }.additional-row-heading { display: flex; align-items: center; gap: 8px; margin-bottom: 10px; color: #273b32; font-size: 13px; }.additional-row-heading strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }.additional-row-number { display: inline-grid; width: 26px; height: 26px; flex: 0 0 auto; place-items: center; color: #126f54; background: #e4f5ee; border-radius: 6px; font-size: 11px; font-weight: 800; }.additional-row-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; padding-left: 34px; }.additional-readonly, .additional-row-fields label { display: block; color: #667a70; font-size: 11px; font-weight: 700; }.additional-readonly strong { display: block; margin-top: 6px; padding: 9px 10px; color: #126f54; background: #f7faf8; border: 1px solid #dbe8e0; border-radius: 6px; font-size: 13px; }.additional-input-wrap { display: flex; margin-top: 5px; }.additional-input-wrap input { width: 100%; min-height: 38px; padding: 8px 10px; border: 1px solid #cbd5e1; border-right: 0; border-radius: 6px 0 0 6px; text-align: right; font: inherit; font-weight: 700; }.additional-input-wrap em { display: grid; min-width: 52px; place-items: center; padding: 0 8px; color: #64748b; background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 0 6px 6px 0; font-size: 12px; font-style: normal; }
.additional-receipt-modal > .modal-footer { flex: 0 0 auto; }
@media (max-width: 650px) { .additional-form-scroll { padding: 14px 15px 0; }.additional-row-fields { grid-template-columns: 1fr; padding-left: 34px; gap: 8px; }.additional-section-heading { align-items: flex-start; }.additional-receipt-modal > .modal-footer { align-items: stretch; flex-direction: column; }.additional-receipt-modal > .modal-footer > div { justify-content: flex-end; } }
.portion-export-status { display: block; width: max-content; margin-top: 6px; padding: 3px 7px; border-radius: 4px; font-size: 10px; font-weight: 800; }
.portion-exported { color: #166534; background: #dcfce7; border: 1px solid #bbf7d0; }
.portion-waiting { color: #92400e; background: #fef3c7; border: 1px solid #fde68a; }
.variant-grid { grid-template-columns: minmax(210px, 1.5fr) 140px 110px 160px 150px; min-width: 790px; }
.row-out-of-stock { background-color: #fff1f2; }
.row-out-of-stock:hover { background-color: #ffe4e6; }
.plan-breakdown { display: block; margin-top: 4px; color: #64748b; white-space: normal; }
.pending-portions-badge { display: block; width: max-content; margin-top: 5px; padding: 3px 6px; color: #92400e; background: #fef3c7; border: 1px solid #fde68a; border-radius: 4px; font-size: 10px; font-weight: 800; white-space: normal; }
.remaining-cell { display: flex; justify-content: flex-end; white-space: normal; }
.remain-warning { color: #9a3412; background: #ffedd5; border: 1px solid #fed7aa; }
.remain-danger { color: #b91c1c; background: #fee2e2; border: 1px solid #fecaca; }
.additional-row-action { min-height: 31px; padding: 5px 8px; color: #92400e; background: #fef3c7; border: 1px solid #f59e0b; border-radius: 5px; cursor: pointer; font: inherit; font-size: 11px; font-weight: 800; white-space: nowrap; }
.additional-row-action:hover { background: #fde68a; }
.additional-row-action:disabled { color: #92400e; background: #fff7ed; border-color: #fdba74; cursor: not-allowed; opacity: .8; }
.load-error { display: flex; align-items: center; flex-wrap: wrap; gap: 8px 14px; padding: 30px 22px; color: #991b1b; background: #fff7f7; border-top: 1px solid #fecaca; text-align: center; }
.load-error strong { color: #b91c1c; }
.load-error span { flex: 1 1 100%; color: #7f1d1d; font-size: 13px; }
.load-error .button { margin: 0 auto; }
</style>

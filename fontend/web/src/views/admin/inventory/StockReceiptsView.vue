<template>
  <InventoryWorkspaceNav />
  <div class="stock-receipts-page">
    <header class="page-heading">
      <div class="page-heading-title">
        <span class="heading-icon"><ClipboardList :size="23" aria-hidden="true" /></span>
        <div><p class="eyebrow">QUẢN LÝ KHO</p><h1>Phiếu kho</h1></div>
      </div>
      <div class="page-actions">
        <router-link to="/admin/inventory" class="stock-button stock-button-light"><Package :size="17" aria-hidden="true" />Nguyên liệu</router-link>
        <router-link to="/admin/kitchen-reconciliation" class="stock-button stock-button-light"><ListChecks :size="17" aria-hidden="true" />Đối soát cuối ca</router-link>
        <button class="stock-button stock-button-primary" type="button" @click="openCreateReceipt"><Plus :size="18" aria-hidden="true" />Tạo phiếu kho</button>
      </div>
    </header>

    <div v-if="successMessage" class="page-feedback feedback-success" role="status"><CircleCheck :size="18" aria-hidden="true" /><span>{{ successMessage }}</span><button type="button" class="icon-button" title="Đóng thông báo" aria-label="Đóng thông báo" @click="successMessage = ''"><X :size="16" /></button></div>

    <section class="content-panel">
      <div class="list-heading">
        <div class="status-filters" role="group" aria-label="Trạng thái phiếu kho">
          <button v-for="status in statusOptions" :key="status.value" type="button" :class="{ active: filters.status === status.value }" :aria-pressed="filters.status === status.value" :disabled="loading" @click="selectStatus(status.value)">
            <span v-if="status.value !== 'all'" class="status-dot" :class="`dot-${status.value}`"></span>{{ status.label }}<span v-if="statusCounts?.[status.value] != null" class="status-count">{{ statusCounts[status.value] }}</span>
          </button>
        </div>
        <span class="result-count" aria-live="polite">{{ totalCount.toLocaleString('vi-VN') }} phiếu</span>
      </div>
      <div class="filter-toolbar">
        <label class="search-box" for="receipt-search">
          <Search :size="18" aria-hidden="true" />
          <span class="visually-hidden">Tìm mã phiếu, nguyên liệu, mã lô, nhà cung cấp</span>
          <input id="receipt-search" v-model="search" type="search" placeholder="Tìm mã phiếu, nguyên liệu, mã lô..." @keydown.enter.prevent="reloadReceipts" />
        </label>
        <label class="filter-field type-filter"><span>Loại phiếu</span><select v-model="filters.type" class="form-select filter-select" @change="reloadReceipts">
          <option value="all">Tất cả loại phiếu</option>
          <option v-for="type in receiptTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
        </select></label>
        <label class="filter-field period-filter"><span>Chế độ xem</span><select v-model="period" class="form-select" @change="applyPeriod">
          <option value="all">Tất cả thời gian</option><option value="day">Theo ngày</option><option value="week">Theo tuần</option><option value="month">Theo tháng</option><option value="custom">Khoảng ngày</option>
        </select></label>
        <label class="filter-field date-field-filter"><span>Tính theo</span><select v-model="filters.dateField" class="form-select" @change="changeDateField"><option value="created">Ngày lập phiếu</option><option value="approved">Ngày duyệt phiếu</option></select></label>
        <div class="filter-actions">
          <button v-if="hasFilters" class="icon-button" type="button" title="Xóa bộ lọc" aria-label="Xóa bộ lọc" :disabled="loading" @click="resetFilters"><FilterX :size="18" /></button>
          <button class="icon-button" type="button" title="Làm mới danh sách" aria-label="Làm mới danh sách" :disabled="loading" @click="reloadReceipts"><RefreshCw :size="18" :class="{ 'is-spinning': loading }" /></button>
        </div>
      </div>
      <div v-if="period !== 'all'" class="period-toolbar">
        <template v-if="period !== 'custom'">
          <div class="period-navigation">
            <button class="icon-button" type="button" :title="previousPeriodLabel" :aria-label="previousPeriodLabel" :disabled="loading" @click="shiftPeriod(-1)"><ChevronLeft :size="18" /></button>
            <label class="period-anchor"><span class="visually-hidden">{{ period === 'month' ? 'Chọn tháng' : period === 'week' ? 'Chọn ngày trong tuần' : 'Chọn ngày' }}</span><input v-if="period === 'month'" v-model="periodMonth" type="month" class="form-control" @change="applyPeriod" /><input v-else v-model="periodAnchor" type="date" class="form-control" @change="applyPeriod" /></label>
            <button class="icon-button" type="button" :title="nextPeriodLabel" :aria-label="nextPeriodLabel" :disabled="loading" @click="shiftPeriod(1)"><ChevronRight :size="18" /></button>
          </div>
          <button class="stock-button stock-button-light" type="button" :disabled="loading" @click="goToCurrentPeriod">{{ period === 'day' ? 'Hôm nay' : period === 'week' ? 'Tuần này' : 'Tháng này' }}</button>
        </template>
        <template v-else>
          <label class="filter-field"><span>Từ ngày</span><input v-model="filters.fromDate" type="date" class="form-control" :max="filters.toDate || undefined" @change="reloadReceipts" /></label>
          <label class="filter-field"><span>Đến ngày</span><input v-model="filters.toDate" type="date" class="form-control" :min="filters.fromDate || undefined" @change="reloadReceipts" /></label>
        </template>
        <span v-if="!dateRangeError" class="period-caption">{{ filters.dateField === 'approved' ? 'Ngày duyệt' : 'Ngày lập' }}: {{ periodCaption }}</span>
        <button class="stock-button stock-button-light period-clear" type="button" @click="clearPeriod"><X :size="15" aria-hidden="true" />Bỏ lọc ngày</button>
      </div>
      <div v-if="dateRangeError" class="page-feedback feedback-error" role="alert"><CircleAlert :size="18" aria-hidden="true" />{{ dateRangeError }}</div>

      <div v-if="errorMessage" class="page-feedback feedback-error" role="alert"><CircleAlert :size="18" aria-hidden="true" /><span>{{ errorMessage }}</span><button class="stock-button stock-button-light" type="button" @click="reloadReceipts">Thử lại</button></div>

      <div class="table-responsive">
        <table class="table align-middle mb-0 receipt-table">
          <thead>
            <tr>
              <th scope="col">Phiếu kho / {{ filters.dateField === 'approved' ? 'Ngày duyệt' : 'Ngày lập' }}</th>
              <th scope="col">Loại phiếu</th>
              <th scope="col">Đối tác / Ghi chú</th>
              <th scope="col" class="text-center">Nguyên liệu</th>
              <th class="text-end">Giá trị</th>
              <th class="text-center">Trạng thái</th>
              <th class="text-end">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td colspan="7" class="empty-state"><LoaderCircle :size="24" class="is-spinning" aria-hidden="true" /><p role="status">Đang tải phiếu kho...</p></td>
            </tr>
            <tr v-else-if="receipts.length === 0">
              <td colspan="7" class="empty-state"><ClipboardList :size="36" aria-hidden="true" /><p>Không có phiếu kho phù hợp</p><button v-if="hasFilters" class="stock-button stock-button-light" type="button" @click="resetFilters">Xóa bộ lọc</button></td>
            </tr>
            <tr v-for="receipt in receipts" v-else :key="receipt.receiptId">
              <td class="receipt-identity">
                <button class="receipt-code" type="button" @click="openDetail(receipt.receiptId)">{{ receipt.receiptCode }}</button>
                <small class="d-block text-muted">{{ formatDateTime(filters.dateField === 'approved' ? receipt.approvedAt : receipt.createdAt) }}</small>
                <small class="d-block text-muted">{{ filters.dateField === 'approved' ? receipt.approvedByName : receipt.createdByName }}</small>
              </td>
              <td class="receipt-type-cell"><span class="type-chip" :class="`type-${receipt.receiptType}`"><component :is="typeIcon(receipt.receiptType)" :size="14" aria-hidden="true" />{{ typeLabel(receipt.receiptType, receipt.receiptCode) }}</span></td>
              <td class="receipt-context">
                <strong v-if="receipt.supplierName" class="d-block">{{ receipt.supplierName }}</strong>
                <span class="receipt-note" :title="receipt.note">{{ receipt.note || '—' }}</span>
              </td>
              <td class="text-center receipt-count" data-label="Nguyên liệu">{{ receipt.itemCount }}</td>
              <td class="text-end receipt-value" data-label="Giá trị">{{ formatCurrency(receipt.totalAmount) }}</td>
              <td class="text-center receipt-status-cell"><span class="status-chip" :class="`status-${receipt.status}`"><span class="status-dot" :class="`dot-${receipt.status}`"></span>{{ statusLabel(receipt.status) }}</span></td>
              <td class="text-end receipt-row-actions">
                <div class="action-group">
                  <button class="icon-button" type="button" title="Xem chi tiết phiếu" :aria-label="`Xem phiếu ${receipt.receiptCode}`" @click="openDetail(receipt.receiptId)"><Eye :size="18" /></button>
                  <button v-if="receipt.status === 'draft'" class="stock-button stock-button-approve" type="button" title="Kiểm tra chi tiết trước khi duyệt phiếu" :disabled="pendingReceiptId !== null" @click="openDetail(receipt.receiptId)"><ListChecks :size="16" aria-hidden="true" />Xem &amp; duyệt</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination-bar">
        <span class="text-muted small">Hiển thị {{ totalCount ? (page - 1) * pageSize + 1 : 0 }}–{{ Math.min(page * pageSize, totalCount) }} / {{ totalCount }} phiếu</span>
        <div class="pagination-controls">
          <label class="page-size"><span class="visually-hidden">Số phiếu mỗi trang</span><select v-model.number="pageSize" :disabled="loading" @change="reloadReceipts"><option :value="15">15 / trang</option><option :value="30">30 / trang</option><option :value="50">50 / trang</option></select></label>
          <span class="small text-muted">Trang {{ page }} / {{ Math.max(totalPages, 1) }}</span>
          <button class="icon-button" type="button" title="Trang trước" aria-label="Trang trước" :disabled="loading || page <= 1" @click="changePage(page - 1)"><ChevronLeft :size="18" /></button>
          <button class="icon-button" type="button" title="Trang sau" aria-label="Trang sau" :disabled="loading || page >= totalPages" @click="changePage(page + 1)"><ChevronRight :size="18" /></button>
        </div>
      </div>
    </section>

    <Teleport to="body">
    <div v-if="showDetail" class="modal-backdrop-custom" @click.self="closeDetail">
      <section ref="detailDialog" class="receipt-modal detail-modal" role="dialog" aria-modal="true" aria-labelledby="stock-detail-title" tabindex="-1">
        <div class="modal-head">
          <div>
            <div class="eyebrow">CHI TIẾT PHIẾU KHO</div>
            <h2 id="stock-detail-title">{{ selectedReceipt?.receiptCode || 'Chi tiết phiếu kho' }}</h2>
            <div v-if="selectedReceipt" class="detail-heading-tags">
              <span class="type-chip" :class="`type-${selectedReceipt.receiptType}`"><component :is="typeIcon(selectedReceipt.receiptType)" :size="14" aria-hidden="true" />{{ typeLabel(selectedReceipt.receiptType, selectedReceipt.receiptCode) }}</span>
              <span class="status-chip" :class="`status-${selectedReceipt.status}`"><span class="status-dot" :class="`dot-${selectedReceipt.status}`"></span>{{ statusLabel(selectedReceipt.status) }}</span>
            </div>
          </div>
          <button class="icon-button" type="button" aria-label="Đóng chi tiết phiếu" title="Đóng" :disabled="pendingReceiptId !== null" @click="closeDetail"><X :size="20" /></button>
        </div>
        <div v-if="detailLoading" class="empty-state py-5" role="status"><LoaderCircle :size="24" class="is-spinning" aria-hidden="true" /><p>Đang tải chi tiết...</p></div>
        <template v-else-if="selectedReceipt">
          <div class="receipt-detail-scroll">
          <div class="detail-summary" :class="{ 'has-supplier': selectedReceipt.supplierName }">
            <div><span>Người lập</span><strong>{{ selectedReceipt.createdByName || 'Chưa có thông tin' }}</strong></div>
            <div><span>Ngày lập</span><strong>{{ formatDateTime(selectedReceipt.createdAt) }}</strong></div>
            <div v-if="selectedReceipt.supplierName" class="detail-supplier"><span>Nhà cung cấp</span><strong>{{ selectedReceipt.supplierName }}</strong></div>
          </div>

          <div v-if="selectedReceipt.status === 'approved'" class="inventory-posted-note">
            <CircleCheck :size="20" aria-hidden="true" />
            <div><strong>{{ receiptPostingMessage }}</strong><small v-if="selectedReceipt.approvedAt">{{ selectedReceipt.approvedByName || 'Thủ kho' }} · {{ formatDateTime(selectedReceipt.approvedAt) }}</small></div>
          </div>
          <div v-else-if="selectedReceipt.status === 'draft'" class="inventory-pending-note">
            <CircleAlert :size="20" aria-hidden="true" /><div><strong>Chờ duyệt · Chưa thay đổi tồn kho</strong></div>
          </div>
          <div v-else-if="selectedReceipt.status === 'cancelled'" class="inventory-cancelled-note">
            <CircleX :size="20" aria-hidden="true" /><div><strong>Phiếu đã hủy</strong><small>Không ghi nhận nhập / xuất kho từ phiếu này.</small></div>
          </div>

          <div class="detail-note" v-if="selectedReceipt.note"><strong>Ghi chú:</strong> {{ selectedReceipt.note }}</div>

          <div v-if="needsKitchenPreview" class="allocation-preview-summary">
            <div><strong>{{ kitchenPreview ? `Dự kiến xuất từ ${previewBatchCount} lô` : 'Chưa có phân bổ lô' }}</strong><small v-if="kitchenPreview">Tồn kho lúc {{ formatDateTime(kitchenPreview.checkedAt) }} · Kiểm tra lại khi duyệt</small></div>
            <button class="icon-button" type="button" title="Cập nhật phân bổ lô" aria-label="Cập nhật phân bổ lô" :disabled="pendingReceiptId !== null" @click="openDetail(selectedReceipt.receiptId)"><RefreshCw :size="17" /></button>
          </div>
          <div v-if="needsKitchenPreview && approvalBlocked" class="allocation-errors" role="alert"><CircleAlert :size="18" aria-hidden="true" /><div><strong>Chưa thể duyệt xuất bếp</strong><p v-for="message in kitchenPreview?.errors || ['Không tải được phân bổ lô. Vui lòng cập nhật lại.']" :key="message">{{ message }}</p></div></div>

          <div class="detail-lines-heading"><h3>Chi tiết nguyên liệu</h3><span>{{ selectedReceipt.details?.length || 0 }} dòng</span></div>
          <div class="detail-table-wrap">
            <table class="table align-middle mb-0 detail-table">
              <caption class="visually-hidden">Chi tiết nguyên liệu của phiếu {{ selectedReceipt.receiptCode }}</caption>
              <colgroup><col class="col-ingredient" /><col class="col-quantity" /><col class="col-cost" /><col class="col-amount" /><col class="col-batch" /></colgroup>
              <thead><tr><th scope="col">Nguyên liệu</th><th scope="col" class="text-end">{{ isImportReceipt ? 'Mua / Thực nhận' : 'Số lượng' }}</th><th scope="col" class="text-end">{{ isImportReceipt ? 'Giá mua' : 'Đơn giá' }}</th><th scope="col" class="text-end">Thành tiền</th><th scope="col">{{ selectedReceipt.receiptType === 'export_kitchen' ? 'Lô xuất / Số lượng' : 'Lô hàng' }}</th></tr></thead>
              <tbody>
                <tr v-for="detail in selectedReceipt.details" :key="detail.detailId">
                  <td class="detail-ingredient"><strong>{{ detail.itemName }}</strong><small v-if="isImportReceipt" class="d-block detail-wastage">Hao hụt dự kiến {{ formatWastagePercent(detail.wastagePercent) }}%</small><details v-if="detail.note" class="ingredient-context"><summary>{{ selectedReceipt.receiptType === 'export_kitchen' ? 'Món sử dụng / Ghi chú' : 'Ghi chú nguyên liệu' }}</summary><p>{{ detail.note }}</p></details></td>
                  <td class="text-end receipt-quantity-cell" :data-label="isImportReceipt ? 'Mua / Thực nhận' : 'Số lượng'">
                    <template v-if="isImportReceipt">
                      <span>Mua {{ formatQuantity(receiptPurchaseQuantity(detail, true)) }} {{ detail.purchaseUnit || detail.baseUnit }}</span>
                      <strong class="d-block">Nhận {{ formatQuantity(detail.quantity) }} {{ detail.baseUnit }}</strong>
                      <small class="d-block detail-secondary">Dự kiến {{ formatQuantity(detail.expectedUsableQuantity ?? detail.quantity) }} {{ detail.baseUnit }}</small>
                      <small v-if="detail.wastageQuantity > 0" class="d-block detail-wastage">Hao hụt {{ formatQuantity(detail.wastageQuantity) }} {{ detail.baseUnit }} ({{ formatWastagePercent(actualLossPercent(detail)) }}%)</small>
                      <small v-if="detail.adjustmentNote" class="d-block text-danger">Lý do: {{ detail.adjustmentNote }}</small>
                    </template>
                    <template v-else>
                      <strong>{{ formatQuantity(receiptPurchaseQuantity(detail)) }} {{ detail.purchaseUnit || detail.baseUnit }}</strong>
                      <small v-if="detail.purchaseUnit && detail.purchaseUnit !== detail.baseUnit" class="d-block detail-secondary">{{ formatQuantity(detail.quantity) }} {{ detail.baseUnit }}</small>
                    </template>
                  </td>
                  <td class="text-end detail-unit-cost" :data-label="isImportReceipt ? 'Giá mua' : 'Đơn giá'"><span>{{ formatCurrency(displayDetailCost(detail)) }}</span><small class="d-block detail-secondary">/ {{ detail.purchaseUnit || detail.baseUnit }}</small><small v-if="needsKitchenPreview && previewPicks(detail).length > 1" class="detail-secondary">Bình quân {{ previewPicks(detail).length }} lô</small></td>
                  <td class="text-end detail-line-amount" data-label="Thành tiền"><strong>{{ formatCurrency(displayDetailAmount(detail)) }}</strong></td>
                  <td class="detail-batch" :data-label="selectedReceipt.receiptType === 'export_kitchen' ? 'Lô xuất / Số lượng' : 'Lô hàng'">
                    <template v-if="needsKitchenPreview && kitchenPreview">
                      <div v-for="batch in previewPicks(detail)" :key="batch.batchId" class="batch-pick"><span class="batch-code">{{ batch.batchCode }}</span><strong>Dự kiến {{ formatQuantity(batch.quantity) }} {{ detail.baseUnit }}</strong><small>HSD {{ formatDate(batch.expiryDate) }}</small></div>
                      <span v-if="previewShortage(detail) > 0" class="batch-shortage">Thiếu {{ formatQuantity(previewShortage(detail)) }} {{ detail.baseUnit }}</span>
                    </template>
                    <template v-else>
                      <span :class="detail.batchCode || detail.newBatchCode ? 'batch-code' : 'batch-placeholder'">{{ receiptBatchLabel(detail) }}</span>
                      <strong v-if="selectedReceipt.receiptType === 'export_kitchen' && selectedReceipt.status === 'approved' && detail.batchCode" class="batch-issued">Đã xuất {{ formatQuantity(detail.quantity) }} {{ detail.baseUnit }}</strong>
                      <small v-if="detail.expiryDate || detail.requestedExpiryDate" class="d-block detail-secondary">HSD {{ formatDate(detail.expiryDate || detail.requestedExpiryDate) }}</small>
                    </template>
                  </td>
                </tr>
                <tr v-if="!selectedReceipt.details?.length"><td colspan="5" class="empty-state">Phiếu không có chi tiết nguyên liệu.</td></tr>
              </tbody>
            </table>
          </div>
          </div>
          <div class="modal-total detail-total"><div><span>{{ selectedReceipt.status === 'cancelled' ? 'Giá trị phiếu đã hủy' : selectedReceipt.status === 'draft' ? 'Tổng giá trị dự kiến' : 'Tổng giá trị phiếu' }}</span><small v-if="selectedReceipt.status === 'cancelled'">Không tính vào biến động kho</small><small v-else-if="needsKitchenPreview && kitchenPreview?.canApprove">Theo đơn giá các lô dự kiến xuất</small></div><strong>{{ formatCurrency(needsKitchenPreview && kitchenPreview?.canApprove ? kitchenPreview.totalAmount : selectedReceipt.totalAmount) }}</strong></div>
          <div class="modal-actions">
            <button v-if="selectedReceipt.status === 'draft'" class="stock-button stock-button-danger" type="button" :disabled="pendingReceiptId !== null" @click="cancelReceipt(selectedReceipt)"><Trash2 :size="16" aria-hidden="true" />Hủy phiếu</button>
            <span class="flex-spacer"></span>
            <button class="stock-button stock-button-light" type="button" :disabled="pendingReceiptId !== null" @click="closeDetail">Đóng</button>
            <button v-if="selectedReceipt.status === 'draft'" class="stock-button stock-button-primary" type="button" :disabled="pendingReceiptId !== null || approvalBlocked" @click="approveReceipt(selectedReceipt)"><LoaderCircle v-if="pendingReceiptId !== null" :size="17" class="is-spinning" aria-hidden="true" /><Check v-else :size="17" aria-hidden="true" />Duyệt phiếu</button>
          </div>
        </template>
      </section>
    </div>
    </Teleport>

    <Teleport to="body">
    <div v-if="showForm" class="modal-backdrop-custom" @click.self="closeForm">
      <section ref="formDialog" class="receipt-modal form-modal" role="dialog" aria-modal="true" aria-labelledby="stock-create-title" tabindex="-1">
        <div class="modal-head">
          <div>
            <div class="eyebrow">PHIẾU MỚI · CHỜ DUYỆT</div>
            <h2 id="stock-create-title">Tạo phiếu kho</h2>
          </div>
          <button class="icon-button" type="button" aria-label="Đóng phiếu mới" title="Đóng" :disabled="saving" @click="closeForm"><X :size="20" /></button>
        </div>

        <form class="receipt-form" @submit.prevent="saveReceipt">
          <div class="receipt-form-scroll">
          <div v-if="formError" class="receipt-form-error mb-3" role="alert">{{ formError }}</div>

          <div class="row g-3 mb-3">
            <div class="col-md-5">
              <label for="stock-create-type" class="form-label">Loại phiếu <span class="text-danger">*</span></label>
              <select id="stock-create-type" v-model="receiptForm.receiptType" class="form-select" required>
                <option v-for="type in createReceiptTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
              </select>
            </div>
            <div class="col-md-7">
              <label for="stock-create-supplier" class="form-label">Nhà cung cấp</label>
              <select id="stock-create-supplier" v-model="receiptForm.supplierId" class="form-select">
                <option :value="null">-- Không chọn / nội bộ --</option>
                <option v-for="supplier in suppliers" :key="supplier.supplierId" :value="supplier.supplierId">{{ supplier.supplierName }}</option>
              </select>
            </div>
            <div class="col-12">
              <label for="stock-create-note" class="form-label">Ghi chú phiếu</label>
              <input id="stock-create-note" v-model="receiptForm.note" class="form-control" type="text" placeholder="Nhập ghi chú..." />
            </div>
          </div>

          <div class="form-section-head">
            <div>
              <div class="d-flex align-items-center gap-2"><strong>Nguyên liệu</strong><span class="line-count">{{ receiptForm.details.length }}</span></div>
            </div>
            <button class="stock-button stock-button-light add-line-button" type="button" @click="addDetail"><Plus :size="17" aria-hidden="true" />Thêm nguyên liệu</button>
          </div>

          <div class="detail-editor">
            <div v-if="receiptForm.details.length === 0" class="empty-state py-4">Chưa có mặt hàng. Hãy thêm ít nhất một dòng.</div>
            <div v-for="(detail, index) in receiptForm.details" :key="detail.clientKey" class="editor-row">
              <div class="editor-row-heading">
                <span class="editor-row-number">{{ String(index + 1).padStart(2, '0') }}</span>
                <strong>{{ selectedItem(detail)?.itemName || 'Chưa chọn nguyên liệu' }}</strong>
                <span v-if="receiptForm.receiptType === 'import_supplier' && selectedItem(detail)" class="editor-wastage-badge">Hao hụt {{ formatWastagePercent(detail.wastagePercent) }}%</span>
                <button class="editor-remove icon-button" type="button" :aria-label="`Xóa dòng ${index + 1}`" title="Xóa dòng" @click="removeDetail(index)"><Trash2 :size="17" /></button>
              </div>
              <div class="editor-grid">
                <div class="editor-item">
                  <label :for="`receipt-item-${detail.clientKey}`">Nguyên liệu</label>
                  <select :id="`receipt-item-${detail.clientKey}`" v-model="detail.inventoryItemId" class="form-select form-select-sm" required @change="syncDetailItem(detail)">
                    <option :value="null">-- Chọn nguyên liệu --</option>
                    <option v-for="item in items" :key="item.inventoryItemId" :value="item.inventoryItemId" :disabled="isItemSelectedElsewhere(item.inventoryItemId, detail)">{{ item.itemName }} ({{ item.purchaseUnit }})</option>
                  </select>
                </div>
                <div class="editor-item">
                  <label :for="`receipt-quantity-${detail.clientKey}`">{{ receiptForm.receiptType === 'import_supplier' ? 'Số lượng mua' : 'Số lượng' }}</label>
                  <div class="input-group input-group-sm">
                    <input :id="`receipt-quantity-${detail.clientKey}`" v-model.number="detail.purchaseQuantity" class="form-control" type="number" min="0.0001" step="any" required />
                    <span class="input-group-text">{{ detailPurchaseUnit(detail) }}</span>
                  </div>
                </div>
                <div class="editor-item">
                  <label :for="`receipt-cost-${detail.clientKey}`">{{ receiptForm.receiptType === 'import_supplier' ? 'Giá mua' : 'Đơn giá' }}</label>
                  <div class="input-group input-group-sm">
                    <input :id="`receipt-cost-${detail.clientKey}`" v-model.number="detail.unitCost" class="form-control" type="number" min="0" step="any" />
                    <span class="input-group-text">đ/{{ detailPurchaseUnit(detail) }}</span>
                  </div>
                </div>
                <div class="editor-item editor-amount">
                  <label>Thành tiền</label>
                  <strong>{{ formatCurrency(detailAmount(detail)) }}</strong>
                </div>
              </div>
              <div v-if="receiptForm.receiptType === 'import_supplier'" class="batch-fields">
                <div class="batch-field"><label :for="`receipt-batch-${detail.clientKey}`">Mã lô</label><input :id="`receipt-batch-${detail.clientKey}`" v-model="detail.newBatchCode" class="form-control form-control-sm" type="text" placeholder="Tự sinh nếu bỏ trống" /></div>
                <div class="batch-field"><label :for="`receipt-made-${detail.clientKey}`">Ngày sản xuất</label><input :id="`receipt-made-${detail.clientKey}`" v-model="detail.manufacturingDate" class="form-control form-control-sm" type="date" /></div>
                <div class="batch-field"><label :for="`receipt-expiry-${detail.clientKey}`">Hạn sử dụng <span class="text-danger">*</span></label><input :id="`receipt-expiry-${detail.clientKey}`" v-model="detail.expiryDate" class="form-control form-control-sm" type="date" required /></div>
                <div class="batch-summary"><span>Dự kiến dùng</span><strong>{{ formatQuantity(expectedUsable(detail)) }} {{ selectedItem(detail)?.baseUnit || '—' }}</strong></div>
                <div v-if="isWeightUnit(selectedItem(detail)?.baseUnit)" class="batch-field actual-field"><label :for="`receipt-actual-${detail.clientKey}`">Thực nhận sau sơ chế</label><div class="input-group input-group-sm"><input :id="`receipt-actual-${detail.clientKey}`" :value="actualUsable(detail)" class="form-control" type="number" min="0.0001" step="any" @input="detail.actualUsableQuantity = Number($event.target.value)" /><span class="input-group-text">{{ selectedItem(detail)?.baseUnit }}</span></div></div>
                <div class="batch-summary" :class="actualUsable(detail) < expectedUsable(detail) ? 'summary-danger' : 'summary-success'"><span>Hao hụt thực tế</span><strong>{{ formatQuantity(grossBaseQuantity(detail) - actualUsable(detail)) }} {{ selectedItem(detail)?.baseUnit || '—' }}</strong></div>
                <label v-if="requiresAdjustmentReason(detail)" class="batch-field adjustment-reason">Lý do hao hụt vượt dự kiến <span class="text-danger">*</span>
                  <input v-model="detail.adjustmentNote" class="form-control form-control-sm" type="text" maxlength="500" required placeholder="VD: loại bỏ phần dập, hư hỏng..." />
                </label>
              </div>
            </div>
          </div>

          </div>
          <div class="form-total"><span>{{ receiptForm.details.length }} nguyên liệu · Tổng phiếu nháp</span><strong>{{ formatCurrency(formTotal) }}</strong></div>
          <div class="modal-actions">
            <span class="form-status-hint"><CircleAlert :size="16" aria-hidden="true" />Chưa cập nhật tồn kho</span>
            <span class="flex-spacer"></span>
            <button class="stock-button stock-button-light" type="button" :disabled="saving" @click="closeForm">Đóng</button>
            <button class="stock-button stock-button-primary" type="submit" :disabled="saving"><LoaderCircle v-if="saving" :size="17" class="is-spinning" aria-hidden="true" /><Check v-else :size="17" aria-hidden="true" />{{ saving ? 'Đang lưu...' : 'Lưu phiếu nháp' }}</button>
          </div>
        </form>
      </section>
    </div>
    </Teleport>

  </div>
</template>

<script setup>
import InventoryWorkspaceNav from '@/components/InventoryWorkspaceNav.vue';
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import axios from 'axios';
import { ArrowDownToLine, ArrowUpFromLine, Check, ChevronLeft, ChevronRight, CircleAlert, CircleCheck, CircleX, ClipboardList, Eye, FilterX, ListChecks, LoaderCircle, Package, Plus, RefreshCw, Search, SlidersHorizontal, Trash2, Undo2, X } from '@lucide/vue';

const API_BASE = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/inventory`;
const RECEIPTS_API = `${API_BASE}/receipts`;
const route = useRoute();
const statusOptions = [
  { value: 'all', label: 'Tất cả' },
  { value: 'draft', label: 'Chờ duyệt' },
  { value: 'approved', label: 'Đã duyệt' },
  { value: 'cancelled', label: 'Đã hủy' }
];

const receiptTypes = [
  { value: 'import_supplier', label: 'Nhập từ nhà cung cấp' },
  { value: 'export_kitchen', label: 'Xuất cho bếp' },
  { value: 'return_from_kitchen', label: 'Hoàn từ bếp' },
  { value: 'waste_disposal', label: 'Xuất hủy' },
  { value: 'audit_adjustment', label: 'Điều chỉnh kiểm kê' }
];

const createReceiptTypes = receiptTypes.filter(type => !['audit_adjustment', 'return_from_kitchen'].includes(type.value));
const items = ref([]);
const suppliers = ref([]);
const receipts = ref([]);
const search = ref('');
const loading = ref(false);
const saving = ref(false);
const detailLoading = ref(false);
const errorMessage = ref('');
const formError = ref('');
const successMessage = ref('');
const pendingReceiptId = ref(null);
const detailDialog = ref(null);
const formDialog = ref(null);
const page = ref(1);
const pageSize = ref(15);
const totalCount = ref(0);
const totalPages = ref(1);
const statusCounts = ref(null);
const showDetail = ref(false);
const showForm = ref(false);
const selectedReceipt = ref(null);
const filters = ref({ type: 'all', status: 'all', fromDate: '', toDate: '', dateField: 'created' });
const period = ref('all');
const periodAnchor = ref(vietnamToday());
const receiptForm = ref({ receiptType: 'import_supplier', supplierId: null, note: '', details: [] });

const hasFilters = computed(() => Boolean(search.value || filters.value.type !== 'all' || filters.value.status !== 'all' || period.value !== 'all' || filters.value.dateField !== 'created'));
const dateRangeError = computed(() => filters.value.fromDate && filters.value.toDate && filters.value.fromDate > filters.value.toDate ? 'Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.' : '');
const periodMonth = computed({ get: () => periodAnchor.value.slice(0, 7), set: value => { periodAnchor.value = value ? `${value}-01` : ''; } });
const periodCaption = computed(() => {
  const { fromDate, toDate } = filters.value;
  if (fromDate && toDate && fromDate === toDate) return formatDate(fromDate);
  if (fromDate && toDate) return `${formatDate(fromDate)} – ${formatDate(toDate)}`;
  return fromDate ? `Từ ${formatDate(fromDate)}` : toDate ? `Đến ${formatDate(toDate)}` : 'Tất cả thời gian';
});
const previousPeriodLabel = computed(() => ({ day: 'Ngày trước', week: 'Tuần trước', month: 'Tháng trước' })[period.value]);
const nextPeriodLabel = computed(() => ({ day: 'Ngày sau', week: 'Tuần sau', month: 'Tháng sau' })[period.value]);
const isImportReceipt = computed(() => selectedReceipt.value?.receiptType === 'import_supplier');
const receiptPostingMessage = computed(() => ({
  import_supplier: 'Đã cộng lượng thực nhận vào kho.',
  export_kitchen: 'Đã trừ nguyên liệu khỏi kho và ghi nhận lô đã xuất.',
  return_from_kitchen: 'Đã cộng lượng hoàn từ bếp vào kho.',
  waste_disposal: 'Đã trừ lượng hủy khỏi tồn kho.',
  audit_adjustment: 'Đã cập nhật tồn kho theo kết quả kiểm kê.'
})[selectedReceipt.value?.receiptType] || 'Đã cập nhật tồn kho.');
const formTotal = computed(() => receiptForm.value.details.reduce((sum, detail) => sum + detailAmount(detail), 0));
const kitchenPreview = computed(() => selectedReceipt.value?.allocationPreview);
const needsKitchenPreview = computed(() => selectedReceipt.value?.receiptType === 'export_kitchen' && selectedReceipt.value?.status === 'draft');
const previewBatchCount = computed(() => new Set(kitchenPreview.value?.lines?.flatMap(line => line.batches.map(batch => batch.batchId)) || []).size);
const approvalBlocked = computed(() => needsKitchenPreview.value && kitchenPreview.value?.canApprove !== true);

let searchTimer;
let listRequest = 0;
watch(search, () => { clearTimeout(searchTimer); searchTimer = setTimeout(reloadReceipts, 350); });

function vietnamToday() {
  const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
  const part = type => parts.find(p => p.type === type).value;
  return `${part('year')}-${part('month')}-${part('day')}`;
}
function isoDay(date) { return date.toISOString().slice(0, 10); }
function applyPeriod() {
  if (period.value === 'all') filters.value.fromDate = filters.value.toDate = '';
  else if (period.value !== 'custom') {
    if (!periodAnchor.value) periodAnchor.value = vietnamToday();
    const start = new Date(`${periodAnchor.value}T00:00:00Z`);
    const end = new Date(start);
    if (period.value === 'week') {
      start.setUTCDate(start.getUTCDate() - (start.getUTCDay() + 6) % 7);
      end.setTime(start.getTime()); end.setUTCDate(end.getUTCDate() + 6);
    } else if (period.value === 'month') {
      start.setUTCDate(1);
      end.setUTCMonth(start.getUTCMonth() + 1, 0);
    }
    filters.value.fromDate = isoDay(start); filters.value.toDate = isoDay(end);
  }
  reloadReceipts();
}
function shiftPeriod(direction) {
  const date = new Date(`${periodAnchor.value || vietnamToday()}T00:00:00Z`);
  if (period.value === 'month') date.setUTCMonth(date.getUTCMonth() + direction, 1);
  else date.setUTCDate(date.getUTCDate() + direction * (period.value === 'week' ? 7 : 1));
  periodAnchor.value = isoDay(date); applyPeriod();
}
function goToCurrentPeriod() { periodAnchor.value = vietnamToday(); applyPeriod(); }
function clearPeriod() { period.value = 'all'; applyPeriod(); }
function changeDateField() {
  if (filters.value.dateField === 'approved') filters.value.status = 'approved';
  reloadReceipts();
}
function previewPicks(detail) { return kitchenPreview.value?.lines?.find(line => line.detailId === detail.detailId)?.batches || []; }
function previewShortage(detail) { return kitchenPreview.value?.lines?.find(line => line.detailId === detail.detailId)?.shortageQuantity || 0; }
function displayDetailCost(detail) {
  const picks = needsKitchenPreview.value && kitchenPreview.value?.canApprove ? previewPicks(detail) : [];
  const quantity = picks.reduce((sum, batch) => sum + Number(batch.quantity), 0);
  return quantity > 0 ? picks.reduce((sum, batch) => sum + Number(batch.unitCost) * Number(batch.quantity), 0) / quantity : detail.unitCostPerPurchaseUnit ?? detail.unitCost;
}
function displayDetailAmount(detail) {
  return needsKitchenPreview.value && kitchenPreview.value?.canApprove
    ? previewPicks(detail).reduce((sum, batch) => sum + Number(batch.subtotal), 0) : detail.subtotal;
}

let previousFocus = null;
let previousReceiptCode = '';
let previousBodyOverflow = '';
let bodyLocked = false;
function unlockBody() {
  if (bodyLocked) document.body.style.overflow = previousBodyOverflow;
  bodyLocked = false;
}
watch(() => showForm.value || showDetail.value, async open => {
  if (open) {
    previousFocus = document.activeElement;
    previousReceiptCode = previousFocus?.closest('tr')?.querySelector('.receipt-code')?.textContent || '';
    previousBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    bodyLocked = true;
    await nextTick();
    (showForm.value ? formDialog.value : detailDialog.value)?.focus();
  } else {
    unlockBody();
    if (previousFocus?.isConnected) previousFocus.focus();
    else if (previousReceiptCode) [...document.querySelectorAll('.receipt-code')].find(button => button.textContent === previousReceiptCode)?.focus();
  }
});
function closeDetail() { if (pendingReceiptId.value === null) showDetail.value = false; }
function closeForm() { if (!saving.value) showForm.value = false; }
function handleModalKeydown(event) {
  const dialog = showForm.value ? formDialog.value : showDetail.value ? detailDialog.value : null;
  if (!dialog) return;
  if (event.key === 'Escape') {
    event.preventDefault();
    if (showForm.value) closeForm(); else closeDetail();
  }
  if (event.key !== 'Tab') return;
  const controls = [...dialog.querySelectorAll('button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], summary')].filter(control => control.getClientRects().length);
  const first = controls[0];
  const last = controls.at(-1);
  if (!first) { event.preventDefault(); dialog.focus(); return; }
  if (event.shiftKey && (document.activeElement === first || document.activeElement === dialog)) { event.preventDefault(); last.focus(); }
  else if (!event.shiftKey && (document.activeElement === last || document.activeElement === dialog)) { event.preventDefault(); first.focus(); }
}
onBeforeUnmount(() => {
  clearTimeout(searchTimer);
  listRequest++;
  window.removeEventListener('keydown', handleModalKeydown);
  unlockBody();
});

function typeIcon(type) {
  return ({ import_supplier: ArrowDownToLine, export_kitchen: ArrowUpFromLine, return_from_kitchen: Undo2, waste_disposal: Trash2, audit_adjustment: SlidersHorizontal })[type] || ClipboardList;
}
function selectStatus(status) {
  filters.value.status = status;
  if (status !== 'approved') filters.value.dateField = 'created';
  reloadReceipts();
}
function resetFilters() {
  search.value = '';
  period.value = 'all';
  filters.value = { type: 'all', status: 'all', fromDate: '', toDate: '', dateField: 'created' };
  reloadReceipts();
}

function typeLabel(value, receiptCode = '') {
  if (value === 'export_kitchen' && String(receiptCode).startsWith('PXK-BS-')) return 'Xuất bếp bổ sung';
  return receiptTypes.find(type => type.value === value)?.label || value;
}

function statusLabel(value) {
  return { draft: 'Chờ duyệt', approved: 'Đã duyệt', cancelled: 'Đã hủy' }[value] || value;
}

function formatCurrency(value) {
  return `${Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 2 })} đ`;
}

function formatQuantity(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 4 });
}

function formatWastagePercent(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 1 });
}

function apiErrorMessage(error, fallback) {
  const data = error?.response?.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data?.message) return data.message;
  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat().filter(Boolean);
    if (messages.length) return messages.join(' ');
  }
  if (error?.response?.status === 401) return 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
  if (error?.response?.status === 403) return 'Tài khoản hiện tại không có quyền lập phiếu kho.';
  if (error?.code === 'ERR_NETWORK') return 'Không kết nối được máy chủ. Vui lòng thử lại.';
  return `${fallback}${error?.response?.status ? ` (HTTP ${error.response.status})` : ''}`;
}

function actualLossPercent(detail) {
  const gross = Number(detail.grossQuantity) || Number(detail.quantity) || 0;
  return gross > 0 ? (Number(detail.wastageQuantity) || 0) * 100 / gross : 0;
}

function receiptPurchaseQuantity(detail, gross = false) {
  const quantity = Number(gross ? (detail.grossQuantity ?? detail.quantity) : detail.quantity);
  const rate = Number(detail.conversionRate);
  if (rate > 0) return quantity / rate;
  return (gross ? detail.grossQuantityInPurchaseUnit : detail.quantityInPurchaseUnit) ?? quantity;
}

function receiptBatchLabel(detail) {
  if (detail.batchCode) return detail.batchCode;
  if (detail.newBatchCode) return `${detail.newBatchCode} (dự kiến)`;
  if (selectedReceipt.value?.status === 'cancelled') return 'Chưa ghi nhận lô';
  if (selectedReceipt.value?.status === 'approved') return 'Không có thông tin lô';
  if (selectedReceipt.value?.receiptType === 'import_supplier') return 'Tạo lô khi duyệt';
  if (selectedReceipt.value?.receiptType === 'export_kitchen') return 'Phân bổ FEFO khi duyệt';
  return 'Chưa chọn lô';
}

function formatDateTime(value) {
  if (!value) return 'N/A';
  const timestamp = String(value);
  const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(timestamp);
  const date = new Date(hasTimezone ? timestamp : `${timestamp}Z`);
  if (Number.isNaN(date.getTime())) return 'Chưa có thông tin';
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    timeZone: 'Asia/Ho_Chi_Minh',
    hour12: false
  });
}

function formatDate(value) {
  if (!value) return '—';
  const date = new Date(String(value).slice(0, 10) + 'T00:00:00');
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('vi-VN');
}

function selectedItem(detail) {
  return items.value.find(item => Number(item.inventoryItemId) === Number(detail.inventoryItemId));
}

function detailPurchaseUnit(detail) {
  return selectedItem(detail)?.purchaseUnit || 'đơn vị';
}

function detailAmount(detail) {
  return (Number(detail.purchaseQuantity) || 0) * (Number(detail.unitCost) || 0);
}

function grossBaseQuantity(detail) {
  const rate = Number(selectedItem(detail)?.conversionRate) > 0 ? Number(selectedItem(detail).conversionRate) : 1;
  return (Number(detail.purchaseQuantity) || 0) * rate;
}

function expectedUsable(detail) {
  return grossBaseQuantity(detail) * (1 - (Number(detail.wastagePercent) || 0) / 100);
}

function actualUsable(detail) {
  return detail.actualUsableQuantity == null || detail.actualUsableQuantity === ''
    ? expectedUsable(detail)
    : Number(detail.actualUsableQuantity) || 0;
}

function requiresAdjustmentReason(detail) {
  return (Number(detail.wastagePercent) > 0 || actualUsable(detail) < grossBaseQuantity(detail)) && actualUsable(detail) < expectedUsable(detail) - 0.0001;
}

function isWeightUnit(unit) {
  return ['g', 'gam', 'gram', 'kg', 'kilogram', 'mg'].includes(String(unit || '').trim().toLowerCase());
}

function blankDetail() {
  return {
    clientKey: `${Date.now()}-${Math.random()}`,
    inventoryItemId: null,
    purchaseQuantity: 1,
    unitCost: 0,
    wastagePercent: 0,
    actualUsableQuantity: null,
    adjustmentNote: '',
    newBatchCode: '',
    manufacturingDate: '',
    expiryDate: ''
  };
}

function syncDetailItem(detail) {
  const item = selectedItem(detail);
  detail.unitCost = item?.costPerUnit ?? item?.costPerPurchaseUnit ?? 0;
  detail.purchaseQuantity = 1;
  detail.wastagePercent = Number(item?.prepWastagePercent) || 0;
  detail.actualUsableQuantity = null;
  detail.adjustmentNote = '';
}

function isItemSelectedElsewhere(itemId, currentDetail) {
  return receiptForm.value.details.some(detail => detail !== currentDetail && Number(detail.inventoryItemId) === Number(itemId));
}

function addDetail() {
  receiptForm.value.details.push(blankDetail());
}

function removeDetail(index) {
  receiptForm.value.details.splice(index, 1);
}

async function fetchCatalog() {
  const [itemsRes, suppliersRes] = await Promise.all([
    axios.get(`${API_BASE}/items`, { params: { isActive: true } }),
    axios.get(`${API_BASE}/suppliers`, { params: { isActive: true } })
  ]);
  items.value = itemsRes.data?.data || [];
  suppliers.value = suppliersRes.data?.data || [];
}

async function fetchReceipts() {
  const request = ++listRequest;
  if (dateRangeError.value) {
    receipts.value = []; totalCount.value = 0; totalPages.value = 1; statusCounts.value = null; loading.value = false;
    return;
  }
  loading.value = true;
  errorMessage.value = '';
  try {
    const params = {
      page: page.value,
      pageSize: pageSize.value,
      type: filters.value.type,
      status: filters.value.status,
      dateField: filters.value.dateField,
      search: search.value.trim() || undefined
    };
    if (filters.value.fromDate) params.fromDate = filters.value.fromDate;
    if (filters.value.toDate) params.toDate = filters.value.toDate;

    const response = await axios.get(RECEIPTS_API, { params });
    if (request === listRequest && response.data?.success) {
      const lastPage = Math.max(response.data.totalPages || 1, 1);
      if (page.value > lastPage) {
        page.value = lastPage;
        await fetchReceipts();
        return;
      }
      receipts.value = response.data.data || [];
      totalCount.value = response.data.totalCount || 0;
      totalPages.value = response.data.totalPages || 1;
      statusCounts.value = response.data.statusCounts || null;
    }
  } catch (error) {
    if (request === listRequest) errorMessage.value = error.response?.data?.message || 'Không thể tải danh sách phiếu kho.';
  } finally {
    if (request === listRequest) loading.value = false;
  }
}

async function reloadReceipts() {
  clearTimeout(searchTimer);
  page.value = 1;
  await fetchReceipts();
}

async function changePage(nextPage) {
  page.value = nextPage;
  await fetchReceipts();
}

async function openDetail(receiptId) {
  showDetail.value = true;
  detailLoading.value = true;
  selectedReceipt.value = null;
  try {
    const response = await axios.get(`${RECEIPTS_API}/${receiptId}`);
    selectedReceipt.value = response.data?.data || null;
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tải chi tiết phiếu kho.';
    showDetail.value = false;
  } finally {
    detailLoading.value = false;
  }
}

function openCreateReceipt() {
  receiptForm.value = { receiptType: 'import_supplier', supplierId: null, note: '', details: [] };
  formError.value = '';
  addDetail();
  showForm.value = true;
}

async function saveReceipt() {
  if (saving.value) return;
  formError.value = '';
  if (receiptForm.value.details.length === 0 || receiptForm.value.details.some(detail => !detail.inventoryItemId || Number(detail.purchaseQuantity) <= 0)) {
    formError.value = 'Phiếu kho phải có ít nhất một mặt hàng hợp lệ.';
    return;
  }

  const selectedItemIds = receiptForm.value.details.map(detail => Number(detail.inventoryItemId));
  if (new Set(selectedItemIds).size !== selectedItemIds.length) {
    formError.value = 'Mỗi nguyên liệu chỉ được nhập một lần trong cùng một phiếu. Hãy gộp số lượng vào cùng dòng.';
    return;
  }

  const invalidPrepLine = receiptForm.value.receiptType === 'import_supplier' && receiptForm.value.details.find(detail => {
    const actual = actualUsable(detail);
    return actual <= 0 || actual > grossBaseQuantity(detail) || (requiresAdjustmentReason(detail) && !detail.adjustmentNote?.trim());
  });
  if (invalidPrepLine) {
    formError.value = `Kiểm tra lượng thực nhận và lý do hao hụt của ${selectedItem(invalidPrepLine)?.itemName}.`;
    return;
  }

  saving.value = true;
  try {
    const payload = {
      receiptType: receiptForm.value.receiptType,
      supplierId: receiptForm.value.supplierId ? Number(receiptForm.value.supplierId) : null,
      note: receiptForm.value.note?.trim() || null,
      details: receiptForm.value.details.map(detail => {
        const item = selectedItem(detail);
        const rate = Number(item?.conversionRate) > 0 ? Number(item.conversionRate) : 1;
        return {
          inventoryItemId: Number(detail.inventoryItemId),
          quantity: Number(detail.purchaseQuantity) * rate,
          unitCost: Number(detail.unitCost) > 0 ? Number(detail.unitCost) : null,
          wastagePercent: receiptForm.value.receiptType === 'import_supplier' ? Number(detail.wastagePercent) || 0 : 0,
          actualUsableQuantity: receiptForm.value.receiptType === 'import_supplier' ? actualUsable(detail) : Number(detail.purchaseQuantity) * rate,
          adjustmentNote: detail.adjustmentNote?.trim() || null,
          newBatchCode: detail.newBatchCode?.trim() || null,
          manufacturingDate: detail.manufacturingDate || null,
          expiryDate: detail.expiryDate || null
        };
      })
    };

    const response = await axios.post(RECEIPTS_API, payload);
    successMessage.value = response.data?.message || 'Đã lưu phiếu nháp. Phiếu đang chờ duyệt.';
    showForm.value = false;
    await fetchReceipts();
  } catch (error) {
    console.error('Lỗi tạo phiếu kho:', error);
    formError.value = apiErrorMessage(error, 'Không thể tạo phiếu kho.');
  } finally {
    saving.value = false;
  }
}

async function approveReceipt(receipt) {
  if (pendingReceiptId.value !== null) return;
  if (receipt.status !== 'draft' || approvalBlocked.value) return;
  if (!confirm(`Duyệt phiếu ${receipt.receiptCode}? Tồn kho sẽ được cập nhật ngay.`)) return;
  pendingReceiptId.value = receipt.receiptId;
  try {
    const response = await axios.post(`${RECEIPTS_API}/${receipt.receiptId}/approve`);
    successMessage.value = response.data?.message || `Đã duyệt phiếu ${receipt.receiptCode}.`;
    await fetchReceipts();
    if (showDetail.value) await openDetail(receipt.receiptId);
  } catch (error) {
    alert(error.response?.data?.message || 'Không thể duyệt phiếu kho.');
  } finally {
    pendingReceiptId.value = null;
  }
}

async function cancelReceipt(receipt) {
  if (pendingReceiptId.value !== null) return;
  if (!confirm(`Hủy phiếu ${receipt.receiptCode}?`)) return;
  pendingReceiptId.value = receipt.receiptId;
  try {
    const response = await axios.post(`${RECEIPTS_API}/${receipt.receiptId}/cancel`);
    successMessage.value = response.data?.message || `Đã hủy phiếu ${receipt.receiptCode}.`;
    showDetail.value = false;
    await fetchReceipts();
  } catch (error) {
    alert(error.response?.data?.message || 'Không thể hủy phiếu kho.');
  } finally {
    pendingReceiptId.value = null;
  }
}

onMounted(async () => {
  window.addEventListener('keydown', handleModalKeydown);
  try {
    const requestedStatus = String(route.query.status || '');
    if (statusOptions.some(option => option.value === requestedStatus)) filters.value.status = requestedStatus;
    await Promise.all([fetchCatalog(), fetchReceipts()]);
    const receiptId = Number(route.query.receiptId);
    if (Number.isSafeInteger(receiptId) && receiptId > 0) await openDetail(receiptId);
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tải dữ liệu quản lý kho.';
  }
});
</script>

<style scoped>
.stock-receipts-page,.receipt-modal { --stock-text:#26342e; --stock-muted:#64716b; --stock-border:#dfe6e2; --stock-primary:#147458; --stock-soft:#f5f8f6; color:var(--stock-text); letter-spacing:0; }
.stock-receipts-page { display:flex; flex-direction:column; gap:22px; width:100%; min-width:0; max-width:1560px; margin:0 auto; }
.page-heading { display:flex; justify-content:space-between; align-items:center; gap:20px; }
.page-heading-title { display:flex; align-items:center; gap:12px; }
.heading-icon { display:grid; place-items:center; width:46px; height:46px; flex-shrink:0; color:var(--stock-primary); background:#e8f3ed; border-radius:8px; }
.eyebrow { margin:0 0 4px; color:var(--stock-muted); font-size:11px; font-weight:600; letter-spacing:0; }
.page-heading h1 { margin:0; color:var(--stock-text); font-size:25px; font-weight:650; line-height:1.25; }
.page-actions { display:flex; flex-wrap:wrap; gap:8px; }
.stock-button,.icon-button { display:inline-flex; align-items:center; justify-content:center; gap:7px; min-height:40px; padding:8px 13px; border:1px solid var(--stock-border); border-radius:6px; background:#fff; color:var(--stock-text); font:inherit; font-size:13px; font-weight:600; line-height:1.3; text-decoration:none; cursor:pointer; transition:background-color .15s,border-color .15s; }
.stock-button svg,.icon-button svg { flex-shrink:0; }
.stock-button-light:hover,.icon-button:hover { color:var(--stock-primary); background:var(--stock-soft); border-color:#9db9aa; }
.stock-button-primary { color:#fff; border-color:var(--stock-primary); background:var(--stock-primary); }
.stock-button-primary:hover { color:#fff; background:#105e47; border-color:#105e47; }
.stock-button-approve { color:#116143; border-color:#c8e4d5; background:#eef8f2; }
.stock-button-approve:hover { background:#dff1e7; }
.stock-button-danger { color:#b33c3c; border-color:#edcaca; }
.stock-button-danger:hover { background:#fff1f1; }
.icon-button { width:40px; min-width:40px; padding:8px; }
button:disabled { opacity:.5; cursor:not-allowed; }
button:focus-visible,a:focus-visible,input:focus-visible,select:focus-visible { outline:3px solid #76b4a0; outline-offset:3px; }
.content-panel { min-width:0; background:#fff; border-top:1px solid var(--stock-border); border-bottom:1px solid var(--stock-border); }
.list-heading { display:flex; align-items:center; justify-content:space-between; gap:14px; padding:0 16px; border-bottom:1px solid var(--stock-border); }
.status-filters { display:flex; gap:22px; }
.status-filters button { display:flex; align-items:center; gap:7px; min-height:54px; padding:0 2px; margin-bottom:-1px; border:0; border-bottom:2px solid transparent; background:transparent; color:var(--stock-muted); font-size:13px; font-weight:500; white-space:nowrap; }
.status-filters button.active { border-bottom-color:var(--stock-primary); color:var(--stock-primary); font-weight:650; }
.status-filters button:hover { color:var(--stock-primary); }
.status-count { min-width:23px; padding:2px 5px; border-radius:4px; background:#edf1ee; color:#52635b; font-size:11px; font-variant-numeric:tabular-nums; }
.status-filters button.active .status-count { background:#e3f0e9; color:var(--stock-primary); }
.result-count { color:var(--stock-muted); font-size:12px; white-space:nowrap; font-variant-numeric:tabular-nums; }
.filter-toolbar { display:grid; grid-template-columns:minmax(200px,1.4fr) minmax(185px,1fr) 160px 160px auto; gap:12px; align-items:end; padding:18px 16px; }
.period-toolbar { display:flex; flex-wrap:wrap; align-items:center; gap:12px; padding:0 16px 18px; }
.period-navigation { display:flex; align-items:center; gap:6px; }
.period-anchor { min-width:0; }.period-anchor input { width:165px; min-height:40px; border-color:var(--stock-border); font-size:13px; }
.period-caption { color:var(--stock-muted); font-size:12px; }
.period-clear { margin-left:auto; }
.period-toolbar .filter-field { width:170px; }
.page-size select { height:34px; padding:4px 6px; border:1px solid var(--stock-border); border-radius:4px; background:#fff; color:var(--stock-muted); font-size:12px; }
.search-box { display:flex; min-width:0; height:40px; align-items:center; gap:9px; padding:0 11px; border:1px solid var(--stock-border); border-radius:6px; color:var(--stock-muted); background:#fff; }
.search-box svg { flex-shrink:0; }
.search-box:focus-within { outline:3px solid #c5e2d6; border-color:var(--stock-primary); }
.search-box input { width:100%; min-width:0; padding:0; border:0; outline:0; color:var(--stock-text); background:transparent; font-size:13px; }
.search-box input:focus-visible { outline:0; }
.filter-field { display:flex; flex-direction:column; gap:6px; min-width:0; }
.filter-field>span { color:var(--stock-muted); font-size:12px; font-weight:500; }
.filter-field .form-select,.filter-field .form-control { width:100%; min-width:0; height:40px; border-color:var(--stock-border); border-radius:6px; font-size:13px; }
.filter-actions { display:flex; gap:8px; }
.receipt-table { min-width:1010px; font-size:14px; }
.receipt-table thead th { padding:12px 16px; border-block:1px solid var(--stock-border); background:var(--stock-soft); color:var(--stock-muted); font-size:11px; letter-spacing:0; font-weight:600; text-transform:uppercase; white-space:nowrap; }
.receipt-table tbody td { padding:16px; border-color:#edf1ee; color:var(--stock-text); }
.receipt-table tbody tr:hover td { background:#fafcfb; }
.receipt-identity { width:240px; }
.receipt-code { padding:0; border:0; background:transparent; color:var(--stock-primary); font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:13px; font-weight:600; text-align:left; overflow-wrap:anywhere; cursor:pointer; }
.receipt-code:hover { text-decoration:underline; }
.receipt-identity small { margin-top:4px; font-size:12px; line-height:1.4; }
.receipt-context { max-width:270px; min-width:150px; }
.receipt-context strong { font-weight:500; }
.receipt-note { display:-webkit-box; overflow:hidden; -webkit-box-orient:vertical; -webkit-line-clamp:2; color:var(--stock-muted); font-size:13px; line-height:1.5; }
.receipt-context strong+.receipt-note { margin-top:4px; }
.receipt-value { font-weight:600; white-space:nowrap; font-variant-numeric:tabular-nums; }
.receipt-row-actions { min-width:182px; }.receipt-row-actions .stock-button { white-space:nowrap; }
.type-chip,.status-chip { display:inline-flex; align-items:center; gap:6px; padding:5px 7px; border-radius:5px; font-size:12px; font-weight:500; line-height:1.3; white-space:nowrap; }
.type-chip svg { flex-shrink:0; }
.type-import_supplier { color:#166548; background:#eaf5ee; }
.type-export_kitchen { color:#275d95; background:#edf3fb; }
.type-return_from_kitchen { color:#786122; background:#fbf5e6; }
.type-waste_disposal { color:#a83d3d; background:#fceeee; }
.type-audit_adjustment { color:#62548b; background:#f0edf7; }
.status-draft { color:#8a6516; background:#fcf5e2; }
.status-approved { color:#176846; background:#eaf6ee; }
.status-cancelled { color:#69716d; background:#f0f2f1; }
.status-dot { width:6px; height:6px; flex-shrink:0; border-radius:50%; background:currentColor; }
.dot-draft { color:#c4922b; }.dot-approved { color:#289565; }.dot-cancelled { color:#8a9490; }
.action-group { display:flex; justify-content:flex-end; align-items:center; gap:8px; }
.action-group .icon-button { border-color:transparent; color:var(--stock-muted); }
.empty-state { padding:52px 18px!important; color:var(--stock-muted); text-align:center; }
.empty-state>svg { color:#99ada2; }.empty-state p { margin:12px 0; font-size:14px; }
.pagination-bar { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:12px 16px; border-top:1px solid var(--stock-border); }
.pagination-controls { display:flex; align-items:center; gap:8px; }
.pagination-controls>span { margin-right:6px; }
.page-feedback { display:flex; align-items:center; gap:10px; padding:12px 16px; border:1px solid; border-radius:6px; font-size:13px; line-height:1.5; }
.page-feedback>svg { flex-shrink:0; }.page-feedback>span { flex:1; }
.feedback-success { color:#176846; background:#edf8f1; border-color:#c8e5d3; }
.feedback-error { margin:12px 16px; color:#a13b3b; background:#fff3f3; border-color:#f0cccc; }
.modal-backdrop-custom { position:fixed; inset:0; z-index:1050; display:flex; align-items:center; justify-content:center; padding:20px; background:rgba(24,38,31,.5); }
.receipt-modal { display:flex; flex-direction:column; width:min(1120px,100%); max-height:calc(100dvh - 40px); overflow:hidden; border:1px solid #e0e7e2; border-radius:8px; background:#fff; box-shadow:0 24px 80px rgba(15,35,24,.2); }
.receipt-modal:focus { outline:none; }
.modal-head { display:flex; align-items:center; justify-content:space-between; flex-shrink:0; gap:16px; padding:20px 24px; border-bottom:1px solid var(--stock-border); }
.modal-head>div { min-width:0; }
.modal-head h2 { margin:0; color:var(--stock-text); font-size:20px; font-weight:600; line-height:1.4; overflow-wrap:anywhere; }
.receipt-detail-scroll { min-height:0; overflow:auto; overscroll-behavior:contain; }
.detail-modal { width:min(1180px,100%); }
.detail-modal .modal-head { align-items:flex-start; }
.detail-modal .modal-head .icon-button { flex-shrink:0; }
.detail-heading-tags { display:flex; flex-wrap:wrap; gap:8px; margin-top:10px; }
.detail-summary { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:16px; margin:0 24px; padding:18px 0; border-bottom:1px solid var(--stock-border); }
.detail-summary.has-supplier { grid-template-columns:repeat(3,minmax(0,1fr)); }
.detail-summary>div>span:not(.status-chip) { display:block; margin-bottom:6px; color:var(--stock-muted); font-size:12px; }
.detail-summary strong { font-size:14px; font-weight:500; overflow-wrap:anywhere; }
.detail-note { margin:16px 24px 0; padding:10px 12px; border-left:2px solid #95b7a6; color:var(--stock-muted); background:var(--stock-soft); font-size:13px; line-height:1.5; overflow-wrap:anywhere; }
.inventory-posted-note,.inventory-pending-note,.inventory-cancelled-note { display:flex; align-items:center; gap:10px; margin:16px 24px; padding:12px 14px; border:1px solid; border-radius:6px; font-size:13px; }
.inventory-posted-note { color:#176846; background:#f0f8f3; border-color:#cfe5d8; }
.inventory-pending-note { color:#87641a; background:#fffaf0; border-color:#ecdcb7; }
.inventory-cancelled-note { color:#56625c; background:#f5f6f5; border-color:var(--stock-border); }
.inventory-posted-note>svg,.inventory-pending-note>svg,.inventory-cancelled-note>svg { flex-shrink:0; }
.inventory-posted-note strong,.inventory-pending-note strong,.inventory-cancelled-note strong { font-weight:500; }
.inventory-posted-note small,.inventory-cancelled-note small { display:block; margin-top:4px; color:var(--stock-muted); font-size:12px; }
.detail-lines-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; margin:24px 24px 12px; }
.detail-lines-heading h3 { margin:0; font-size:14px; font-weight:600; }
.detail-lines-heading>span { color:var(--stock-muted); font-size:12px; }
.detail-table-wrap { margin:0 24px 20px; }
.detail-table { width:100%; table-layout:fixed; font-size:13px; }
.col-ingredient { width:27%; }.col-quantity { width:20%; }.col-cost { width:14%; }.col-amount { width:17%; }.col-batch { width:22%; }
.detail-table-wrap th { padding:12px; border-block:1px solid var(--stock-border); color:var(--stock-muted); background:var(--stock-soft); font-size:11px; font-weight:600; text-transform:uppercase; }
.detail-table-wrap td { padding:18px 12px; border-color:#e9eeeb; color:var(--stock-text); line-height:1.6; vertical-align:top; }
.detail-table-wrap strong { font-weight:600; }
.detail-ingredient>strong { font-size:14px; overflow-wrap:anywhere; }
.receipt-quantity-cell,.detail-unit-cost,.detail-line-amount { font-variant-numeric:tabular-nums; }
.receipt-quantity-cell strong,.detail-unit-cost>span,.detail-line-amount strong { white-space:nowrap; }
.detail-secondary { margin-top:3px; color:var(--stock-muted); font-size:12px; }
.ingredient-context { margin-top:6px; font-size:12px; }
.ingredient-context summary { min-height:32px; padding:6px 0; color:var(--stock-primary); cursor:pointer; }
.ingredient-context summary:hover { text-decoration:underline; }
.ingredient-context summary:focus-visible { outline:3px solid #76b4a0; outline-offset:3px; }
.ingredient-context p { margin:4px 0 0; color:var(--stock-muted); overflow-wrap:anywhere; white-space:pre-line; }
.detail-batch { overflow-wrap:anywhere; }
.batch-placeholder { color:var(--stock-muted); font-size:12px; }
.allocation-preview-summary { display:flex; align-items:center; justify-content:space-between; gap:12px; margin:16px 24px; padding-bottom:12px; border-bottom:1px solid var(--stock-border); font-size:13px; }
.allocation-preview-summary strong { font-weight:500; }
.allocation-preview-summary small { display:block; margin-top:4px; color:var(--stock-muted); font-size:12px; }
.allocation-errors { display:flex; align-items:flex-start; gap:10px; margin:16px 24px; padding:12px; border:1px solid #edcaca; border-radius:6px; background:#fff4f4; color:#a13b3b; font-size:13px; }
.allocation-errors>svg { flex-shrink:0; }.allocation-errors p { margin:5px 0 0; }
.batch-pick { padding:0 0 10px; margin-bottom:10px; border-bottom:1px solid var(--stock-border); }
.batch-pick:last-child { margin-bottom:0; padding-bottom:0; border-bottom:0; }
.batch-pick strong,.batch-issued { display:block; margin-top:3px; color:var(--stock-primary); font-size:12px; font-weight:500; }
.batch-pick small { display:block; margin-top:3px; color:var(--stock-muted); font-size:12px; }
.batch-shortage { display:block; margin-top:6px; color:#a13b3b; font-size:12px; }
.detail-wastage { color:#987528; font-size:12px; }
.batch-code { color:#52635b; font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:12px; }
.modal-total,.form-total { display:flex; align-items:center; justify-content:space-between; flex-shrink:0; gap:16px; margin:0 24px; padding:16px 0; border-top:1px solid var(--stock-border); color:var(--stock-muted); font-size:13px; }
.modal-total strong,.form-total strong { color:var(--stock-text); font-size:23px; font-weight:600; white-space:nowrap; font-variant-numeric:tabular-nums; }
.detail-total { gap:12px; padding:16px 0; }
.detail-total>div { min-width:0; }
.detail-total small { display:block; margin-top:4px; color:var(--stock-muted); font-size:12px; }
.detail-total strong { font-size:22px; }
.modal-actions { display:flex; align-items:center; gap:10px; flex-shrink:0; padding:14px 24px; border-top:1px solid var(--stock-border); background:#fff; }
.flex-spacer { flex:1; }
.receipt-form { display:flex; min-height:0; flex:1; flex-direction:column; }
.receipt-form-scroll { min-height:0; overflow-y:auto; padding:20px 24px 0; overscroll-behavior:contain; }
.receipt-modal .form-control,.receipt-modal .form-select { min-height:40px; border-color:#d6dfd9; border-radius:6px; font-size:13px; }
.receipt-modal .form-label { margin-bottom:6px; color:var(--stock-text); font-size:13px; font-weight:500; }
.receipt-modal .form-control:focus,.receipt-modal .form-select:focus { border-color:#70a78e; box-shadow:0 0 0 3px #e0f0e7; }
.receipt-modal .input-group-text { border-color:#d6dfd9; background:#f6f9f7; color:var(--stock-muted); font-size:12px; }
.form-section-head { display:flex; align-items:center; justify-content:space-between; gap:12px; margin-top:24px; padding-bottom:12px; border-bottom:1px solid var(--stock-border); }
.form-section-head strong { font-size:14px; font-weight:600; }
.line-count { display:inline-grid; place-items:center; min-width:24px; height:24px; padding:0 6px; color:var(--stock-muted); background:#eef2ef; border-radius:5px; font-size:12px; }
.detail-editor { padding:0; }
.editor-row { padding:18px 0; border-bottom:1px solid var(--stock-border); }
.editor-row:last-child { border-bottom:0; }
.editor-row-heading { display:flex; align-items:center; gap:9px; margin-bottom:14px; font-size:13px; }
.editor-row-heading strong { min-width:0; font-size:14px; font-weight:600; overflow-wrap:anywhere; }
.editor-row-number { display:grid; width:28px; height:28px; flex-shrink:0; place-items:center; border-radius:5px; color:#426a55; background:#edf4ef; font-size:11px; font-weight:600; }
.editor-wastage-badge { margin-left:auto; padding:4px 8px; color:#8c681f; background:#fff6e1; border-radius:5px; font-size:11px; font-weight:500; white-space:nowrap; }
.editor-remove { margin-left:auto; color:#a25151; border-color:transparent; }
.editor-wastage-badge+.editor-remove { margin-left:0; }
.editor-remove:hover { color:#a13b3b; background:#fff0f0; border-color:#ebcccc; }
.editor-grid { display:grid; grid-template-columns:minmax(240px,1.6fr) minmax(130px,1fr) minmax(155px,1fr) minmax(120px,.7fr); align-items:end; gap:14px; }
.editor-item,.batch-field { min-width:0; }
.editor-item label,.batch-field label,.batch-summary span { display:block; margin-bottom:6px; color:var(--stock-muted); font-size:12px; font-weight:500; }
.editor-amount strong { display:flex; align-items:center; justify-content:flex-end; min-height:40px; color:var(--stock-primary); font-size:15px; font-weight:600; }
.batch-fields { display:grid; grid-template-columns:repeat(3,minmax(0,1fr)); gap:16px; margin-top:16px; padding:16px; background:var(--stock-soft); border-radius:6px; }
.batch-summary { align-self:end; padding:4px 10px; border-left:2px solid #baccc1; }
.batch-summary strong { color:#42594c; font-size:14px; font-weight:600; }
.summary-danger { border-color:#d99393; }.summary-danger strong { color:#b34343; }
.summary-success { border-color:#8ebda4; }.summary-success strong { color:#1f7d54; }
.adjustment-reason { grid-column:1/-1; color:#a13b3b; font-size:12px; }
.adjustment-reason input { margin-top:6px; }
.form-status-hint { display:flex; align-items:center; gap:6px; color:var(--stock-muted); font-size:12px; }
.receipt-form-error { padding:12px 14px; border:1px solid #e9bcbc; border-radius:6px; color:#a13b3b; background:#fff3f3; font-size:13px; }
.is-spinning { animation:stock-spin 1s linear infinite; }
@keyframes stock-spin { to { transform:rotate(360deg); } }
@media(max-width:1200px) { .filter-toolbar { grid-template-columns:minmax(180px,1fr) minmax(160px,1fr) 135px 135px auto; gap:8px; }.page-heading { align-items:flex-start; }.page-actions { justify-content:flex-end; } }
@media(max-width:1000px) { .filter-toolbar { grid-template-columns:1fr 1fr auto; }.search-box { grid-column:1/-1; }.type-filter { grid-column:1/-1; }.detail-summary.has-supplier { grid-template-columns:repeat(2,minmax(0,1fr)); }.detail-supplier { grid-column:1/-1; }.editor-grid { grid-template-columns:repeat(2,minmax(0,1fr)); }.batch-fields { grid-template-columns:repeat(2,minmax(0,1fr)); } }
@media(max-width:800px) {
  .detail-table,.detail-table tbody { display:block; }
  .detail-table colgroup,.detail-table thead { display:none; }
  .detail-table tbody tr { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:16px; padding:18px 0; border-bottom:1px solid var(--stock-border); }
  .detail-table tbody td { display:block; min-width:0; padding:0; border:0; text-align:left!important; background:transparent; }
  .detail-table .detail-ingredient,.detail-table .empty-state { grid-column:1/-1; }
  .detail-table td[data-label]::before { display:block; margin-bottom:4px; color:var(--stock-muted); font-size:11px; content:attr(data-label); }
  .detail-table .detail-line-amount { grid-column:2; grid-row:3; }
  .detail-table .detail-batch { grid-column:1; grid-row:3; }
  .ingredient-context summary { min-height:44px; padding:12px 0; }
}
@media(max-width:680px) {
  .stock-receipts-page { gap:18px; }.page-heading { flex-direction:column; gap:18px; }.page-heading h1 { font-size:23px; }.page-actions { display:grid; width:100%; grid-template-columns:1fr 1fr; }.page-actions .stock-button-primary { grid-column:1/-1; }
  .stock-button,.icon-button { min-height:44px; }.icon-button { width:44px; min-width:44px; }
  .list-heading { flex-direction:column; align-items:stretch; gap:0; padding:0 12px; }.status-filters { display:flex; flex-wrap:wrap; gap:4px 14px; }.status-filters button { justify-content:center; gap:5px; min-height:48px; font-size:12px; }.result-count { padding:10px 0; text-align:right; }
  .filter-toolbar { grid-template-columns:1fr 1fr; padding:14px 12px; gap:12px; }.filter-actions { grid-column:1/-1; justify-content:flex-end; }.filter-field .form-control,.filter-field .form-select,.search-box { height:44px; }
  .period-toolbar { padding:0 12px 16px; gap:10px; }.period-toolbar .filter-field { width:calc(50% - 5px); }.period-navigation { min-width:0; flex:1; }.period-anchor { flex:1; }.period-anchor input { width:100%; }.period-caption { width:100%; }.period-clear { margin-left:0; }.allocation-preview-summary,.allocation-errors { margin:16px; }.pagination-controls { flex-wrap:wrap; justify-content:flex-end; }
  .receipt-table { display:block; min-width:0; }.receipt-table thead { display:none; }.receipt-table tbody { display:grid; gap:12px; padding:0 12px 14px; }.receipt-table tbody tr { display:grid; grid-template-columns:minmax(0,1fr) auto; gap:12px 8px; padding:16px; border:1px solid var(--stock-border); border-radius:8px; }.receipt-table tbody td { display:block; padding:0; border:0; background:transparent!important; }.receipt-table tbody tr:hover td { background:transparent; }
  .receipt-table .receipt-identity { width:auto; grid-column:1/-1; }.receipt-code { font-size:13px; }.receipt-type-cell { grid-column:1; grid-row:2; }.receipt-status-cell { grid-column:2; grid-row:2; }.receipt-context { grid-column:1/-1; grid-row:3; max-width:none; min-width:0; }.receipt-count,.receipt-value { grid-row:4; font-size:13px; text-align:left!important; }.receipt-value { text-align:right!important; }.receipt-count::before,.receipt-value::before { display:block; margin-bottom:4px; color:var(--stock-muted); font-size:11px; font-weight:400; content:attr(data-label); }.receipt-row-actions { grid-column:1/-1; grid-row:5; padding-top:10px!important; border-top:1px solid #edf1ee!important; }.receipt-table .empty-state { grid-column:1/-1; }.type-chip,.status-chip { font-size:10px; padding:5px 6px; }
  .pagination-bar { flex-wrap:wrap; padding:12px; }.pagination-controls { margin-left:auto; }
  .modal-backdrop-custom { padding:10px; }.receipt-modal { max-height:calc(100dvh - 20px); }.modal-head { padding:16px; }.modal-head h2 { font-size:18px; }.detail-summary { gap:16px 12px; margin:0 16px; padding:16px 0; }.receipt-form-scroll { padding:16px 16px 0; }.detail-note,.detail-table-wrap { margin:16px; }.detail-lines-heading { margin:20px 16px 0; }.inventory-posted-note,.inventory-pending-note,.inventory-cancelled-note { margin:16px; }.modal-total,.form-total { margin:0 16px; }.modal-total strong,.form-total strong { font-size:20px; }.modal-actions { padding:12px 16px; gap:8px; }.form-status-hint { display:none; }
  .detail-modal .modal-actions .flex-spacer { display:none; }
  .detail-modal .modal-actions .stock-button { flex:1; min-width:0; padding:8px; font-size:12px; white-space:nowrap; }
  .editor-grid { grid-template-columns:repeat(2,minmax(0,1fr)); gap:12px; }.editor-grid>.editor-item:first-child { grid-column:1/-1; }.editor-amount { grid-column:1/-1; }.editor-amount strong { justify-content:flex-start; }.batch-fields { grid-template-columns:1fr; padding:14px; }.editor-row-heading { flex-wrap:wrap; }.editor-row-heading strong { max-width:calc(100% - 96px); }.editor-wastage-badge { order:4; margin-left:37px; }.editor-wastage-badge+.editor-remove { margin-left:auto; }.form-section-head { gap:8px; }.form-section-head .stock-button { padding:8px; font-size:12px; }.modal-total>span,.form-total>span { max-width:55%; font-size:12px; }
}
@media(prefers-reduced-motion:reduce) { .stock-button,.icon-button { transition:none; }.is-spinning { animation:none; } }
</style>

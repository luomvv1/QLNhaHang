<template>
  <main class="reports-page">
    <header class="page-heading">
      <div>
        <p class="eyebrow">VẬN HÀNH NHÀ HÀNG</p>
        <h1>Báo cáo thống kê</h1>
        <p class="page-description">Theo dõi doanh thu, món bán chạy và chi phí nguyên liệu, hao hụt theo thời gian.</p>
      </div>
      <button class="btn btn-outline-primary refresh-button" type="button" :disabled="loading" @click="refreshReports">
        <svg aria-hidden="true" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
          <path d="M20 7v5h-5M4 17v-5h5" /><path d="M5.6 9a7 7 0 0 1 11.7-2L20 12M4 12l2.7 5a7 7 0 0 0 11.7-2" />
        </svg>
        {{ loading ? 'Đang tải…' : 'Làm mới báo cáo' }}
      </button>
    </header>

    <section class="filter-panel" aria-label="Bộ lọc thời gian báo cáo">
      <div class="preset-list" role="group" aria-label="Khoảng thời gian nhanh">
        <button v-for="preset in presets" :key="preset.id" class="preset-button" :class="{ selected: activePreset === preset.id }"
          type="button" :aria-pressed="activePreset === preset.id" @click="selectPreset(preset.id)">
          {{ preset.label }}
        </button>
      </div>
      <form class="date-filter" @submit.prevent="applyCustomRange">
        <label> Từ ngày <input v-model="filterFromDate" type="date" :max="filterToDate" required /> </label>
        <span class="date-separator" aria-hidden="true">—</span>
        <label> Đến ngày <input v-model="filterToDate" type="date" :min="filterFromDate" :max="today" required /> </label>
        <button class="btn btn-primary apply-button" type="submit" :disabled="loading">Áp dụng</button>
        <small v-if="hasUnappliedDateFilter" class="date-filter-note">Ngày mới chỉ áp dụng sau khi bấm Áp dụng.</small>
      </form>
      <button v-if="activeReportData" class="export-button" type="button" :disabled="reportLoading[activeReport]" @click="exportCsv">
        <svg aria-hidden="true" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
          <path d="M12 3v12m0 0 4-4m-4 4-4-4M4 17v3h16v-3" />
        </svg>
        Xuất CSV
      </button>
      <button class="export-button print-button" type="button" :disabled="!activeReportData && !isExtendedReport" @click="printReport" title="In hoặc lưu PDF từ trình duyệt">
        <svg aria-hidden="true" viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9V4h12v5M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><path d="M6 14h12v7H6z"/></svg>
        In báo cáo
      </button>
    </section>

    <nav class="report-tabs" aria-label="Nhóm báo cáo">
      <button v-for="tab in reportTabs" :key="tab.id" class="report-tab" :class="{ active: activeReport === tab.id }"
        type="button" role="tab" :aria-selected="activeReport === tab.id" @click="activeReport = tab.id">
        <svg v-if="tab.id === 'sales'" aria-hidden="true" viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M4 19V5m0 14h16M8 15l3-4 3 2 5-7"/></svg>
        <svg v-else-if="tab.id === 'products'" aria-hidden="true" viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19V5M4 19h16"/><path d="m7 15 3-4 3 2 4-6"/></svg>
        <svg v-else aria-hidden="true" viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="M7 9h10M7 13h4M16 13h1"/></svg>
        {{ tab.label }}
      </button>
    </nav>

    <p v-if="errorMessage" class="report-error" role="alert">{{ errorMessage }}</p>
    <div v-if="reportLoading[activeReport] && !activeReportData" class="report-loading" role="status" aria-live="polite">Đang tải báo cáo {{ activeReportMeta.label.toLowerCase() }}…</div>

      <section v-if="activeReportData" class="report-context" aria-label="Ngữ cảnh báo cáo">
        <div class="context-icon" aria-hidden="true">
          <svg v-if="activeReport === 'sales'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19V5m0 14h16M8 15l3-4 3 2 5-7"/></svg>
          <svg v-else-if="activeReport === 'products'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M5 6h14M5 12h14M5 18h9"/><circle cx="19" cy="18" r="2"/></svg>
          <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M4 19h16M6 16V9m4 7V5m4 11v-4m4 4V7"/></svg>
        </div>
        <div class="context-copy">
          <span class="context-eyebrow">{{ activeReportMeta.label }}</span>
          <strong>{{ activeReportMeta.title }}</strong>
          <p>{{ activeReportMeta.description }}</p>
        </div>
        <div class="context-pills">
          <span class="context-pill"><span>Khoảng xem</span><strong>{{ formatDate(activeReportData.fromDate) }} – {{ formatDate(activeReportData.toDate) }}</strong></span>
          <span v-if="activeReport === 'sales'" class="context-pill context-pill-accent"><span>Cấp biểu đồ</span><strong>{{ trendGranularity.tableLabel }}</strong></span>
        </div>
      </section>

      <div v-if="!activeReportData && !isExtendedReport && !reportLoading[activeReport]" class="report-unavailable" role="status">
        <p>{{ reportErrors[activeReport] || 'Báo cáo này chưa có dữ liệu.' }}</p>
        <button class="btn btn-outline-primary" type="button" :disabled="loading" @click="refreshReports">Thử tải lại</button>
      </div>

      <template v-if="activeReport === 'sales' && report">
      <section class="metric-grid" aria-label="Chỉ số tổng quan">
        <article class="metric-card revenue-card">
          <div class="metric-topline"><span class="metric-label">Doanh thu thực nhận</span><span class="metric-icon revenue-icon" aria-hidden="true">₫</span></div>
          <strong class="metric-value">{{ money(report.summary.revenue) }}</strong>
          <span class="metric-note" :class="changeTone">
            <template v-if="report.summary.revenueChangePercent !== null && report.summary.revenueChangePercent !== undefined">
              {{ report.summary.revenueChangePercent > 0 ? '▲' : report.summary.revenueChangePercent < 0 ? '▼' : '•' }}
              {{ Math.abs(report.summary.revenueChangePercent) }}% so với kỳ trước
            </template>
            <template v-else>Chưa có dữ liệu kỳ trước để so sánh</template>
          </span>
        </article>
        <article class="metric-card">
          <div class="metric-topline"><span class="metric-label">Doanh thu trước giảm</span><span class="metric-icon gross-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M4 19V5m0 14h16M8 15l3-4 3 2 5-7"/></svg>
          </span></div>
          <strong class="metric-value">{{ money(report.summary.grossRevenue ?? report.summary.revenue) }}</strong>
          <span class="metric-note neutral-note">Tổng tiền món trước giảm giá</span>
        </article>
        <article class="metric-card">
          <div class="metric-topline"><span class="metric-label">Đã giảm giá</span><span class="metric-icon discount-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="m7 7 10 10M8 8h.01M16 16h.01"/><circle cx="8" cy="8" r="2.5"/><circle cx="16" cy="16" r="2.5"/></svg>
          </span></div>
          <strong class="metric-value">{{ money(report.summary.discountAmount) }}</strong>
          <span class="metric-note neutral-note">Voucher và giảm trực tiếp</span>
        </article>
        <article class="metric-card">
          <div class="metric-topline"><span class="metric-label">Hóa đơn đã thanh toán</span><span class="metric-icon invoice-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M6 3h12v18l-3-2-3 2-3-2-3 2z"/><path d="M9 8h6M9 12h6M9 16h3"/></svg>
          </span></div>
          <strong class="metric-value">{{ number(report.summary.invoiceCount) }}</strong>
          <span class="metric-note neutral-note">Trong khoảng thời gian đã chọn</span>
        </article>
        <article class="metric-card">
          <div class="metric-topline"><span class="metric-label">Giá trị trung bình / hóa đơn</span><span class="metric-icon average-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M4 19V5m0 14h16M8 15l3-4 3 2 5-7"/></svg>
          </span></div>
          <strong class="metric-value">{{ money(report.summary.averageInvoice) }}</strong>
          <span class="metric-note neutral-note">Tính trên hóa đơn đã thanh toán</span>
        </article>
        <article class="metric-card">
          <div class="metric-topline"><span class="metric-label">Đơn đã hủy</span><span class="metric-icon cancelled-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="12" cy="12" r="9"/><path d="m9 9 6 6m0-6-6 6"/></svg>
          </span></div>
          <strong class="metric-value">{{ number(report.summary.cancelledOrders) }}</strong>
          <span class="metric-note neutral-note">Theo thời điểm hủy đơn</span>
        </article>
      </section>

      <section class="content-grid">
        <article class="report-card trend-card">
          <div class="section-heading">
            <div><h2>{{ trendGranularity.title }}</h2><p>Chỉ tính hóa đơn đã thanh toán · {{ trendGranularity.subtitle }}</p></div>
            <span class="legend"><i aria-hidden="true"></i> Doanh thu</span>
          </div>
          <div v-if="hasTrendRevenue" class="chart-frame">
            <svg class="trend-chart" viewBox="0 0 900 286" role="img" :aria-labelledby="'trend-title trend-description'" preserveAspectRatio="none">
              <title id="trend-title">{{ trendGranularity.title }}</title>
              <desc id="trend-description">Biểu đồ doanh thu từ {{ formatDate(report.fromDate) }} đến {{ formatDate(report.toDate) }} theo {{ trendGranularity.tableLabel.toLowerCase() }}. Bảng số liệu có thể mở bên dưới.</desc>
              <g v-for="gridLine in chartGrid" :key="gridLine.value">
                <line x1="62" :y1="gridLine.y" x2="884" :y2="gridLine.y" stroke="#e7eaf2" stroke-dasharray="4 5" />
                <text x="52" :y="gridLine.y + 4" text-anchor="end" class="axis-label">{{ compactMoney(gridLine.value) }}</text>
              </g>
              <path :d="areaPath" fill="url(#revenueFill)" />
              <defs><linearGradient id="revenueFill" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stop-color="#0f766e" stop-opacity=".20"/><stop offset="100%" stop-color="#0f766e" stop-opacity="0"/></linearGradient></defs>
              <polyline :points="chartPoints" fill="none" stroke="#0f766e" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
              <g v-for="point in visiblePoints" :key="point.key">
                <circle :cx="point.x" :cy="point.y" r="4" fill="#fff" stroke="#0f766e" stroke-width="2.5"><title>{{ point.tooltipLabel }}: {{ money(point.revenue) }}</title></circle>
                <text :x="point.x" y="274" text-anchor="middle" class="axis-label">{{ point.label }}</text>
              </g>
            </svg>
          </div>
          <div v-else class="empty-chart">Chưa có doanh thu trong khoảng thời gian này.</div>
          <details class="data-details">
            <summary>Xem số liệu doanh thu {{ trendGranularity.tableLabel.toLowerCase() }}</summary>
            <div class="table-wrap"><table><thead><tr><th>{{ trendGranularity.tableLabel }}</th><th class="text-end">Doanh thu</th></tr></thead><tbody>
              <tr v-for="item in trendData" :key="item.key"><td>{{ item.tableLabel }}</td><td class="text-end">{{ money(item.revenue) }}</td></tr>
            </tbody></table></div>
          </details>
        </article>

        <article class="report-card period-card">
          <div class="section-heading"><div><h2>{{ trendMode === 'hour' ? 'Tóm tắt ngày' : 'Tóm tắt kỳ báo cáo' }}</h2><p>{{ trendMode === 'hour' ? 'Tổng hợp nhanh trong ngày' : trendGranularity.subtitle }}</p></div><span class="summary-kicker">{{ number(trendData.length) }} mốc</span></div>
          <div class="period-stat period-stat-primary"><span>Doanh thu trong kỳ</span><strong>{{ money(report.summary.revenue) }}</strong><small>{{ number(report.summary.invoiceCount) }} hóa đơn đã thanh toán</small></div>
          <div class="period-stat"><span>Mốc doanh thu cao nhất</span><strong>{{ peakTrend.tableLabel }}</strong><small>{{ money(peakTrend.revenue) }}</small></div>
          <div class="period-stat"><span>Giá trị trung bình / hóa đơn</span><strong>{{ money(report.summary.averageInvoice) }}</strong><small>So với tổng doanh thu trong kỳ</small></div>
        </article>

        <article class="report-card payment-card">
          <div class="section-heading"><div><h2>Phương thức thanh toán</h2><p>Phân bổ theo số tiền thanh toán</p></div></div>
          <div v-if="report.paymentMethods.length" class="payment-list">
            <div v-for="method in report.paymentMethods" :key="method.method" class="payment-row">
              <div class="payment-label"><span>{{ paymentMethodLabel(method.method) }}</span><strong>{{ money(method.amount) }}</strong></div>
              <div class="payment-track" role="progressbar" :aria-label="paymentMethodLabel(method.method)" :aria-valuenow="paymentShare(method.amount)" aria-valuemin="0" aria-valuemax="100">
                <span :style="{ width: `${paymentShare(method.amount)}%` }"></span>
              </div>
              <small>{{ number(method.paymentCount) }} giao dịch · {{ paymentShare(method.amount) }}%</small>
            </div>
          </div>
          <div v-else class="empty-state">Chưa có giao dịch thanh toán.</div>
        </article>
      </section>

      <article class="report-card foods-card">
        <div class="section-heading"><div><h2>Món bán chạy</h2><p>Tối đa 8 món, xếp theo số lượng bán</p></div></div>
        <div v-if="report.topFoods.length" class="table-wrap">
          <table class="foods-table"><thead><tr><th class="rank-column">#</th><th>Tên món</th><th class="text-end">Số lượng</th><th class="text-end">Doanh thu món</th></tr></thead>
            <tbody><tr v-for="(food, index) in report.topFoods" :key="`${food.foodName}-${food.variant}`"><td class="rank-column"><span class="rank-badge">{{ index + 1 }}</span></td><td class="food-name">{{ food.foodName }}<small v-if="food.variant">{{ food.variant }}</small></td><td class="text-end">{{ number(food.quantity) }}</td><td class="text-end food-revenue">{{ money(food.revenue) }}</td></tr></tbody>
          </table>
        </div>
        <div v-else class="empty-state">Chưa có món nào được bán trong khoảng thời gian này.</div>
      </article>

      <section class="location-grid">
        <article class="report-card">
          <div class="section-heading"><div><h2>Doanh thu theo khu vực</h2><p>Phân theo bàn chính của order</p></div></div>
          <div v-if="report.salesByArea?.length" class="table-wrap">
            <table><thead><tr><th>Khu vực</th><th class="text-end">Hóa đơn</th><th class="text-end">Doanh thu</th></tr></thead>
              <tbody><tr v-for="area in report.salesByArea" :key="area.areaName"><td class="food-name">{{ area.areaName }}</td><td class="text-end">{{ number(area.invoiceCount) }}</td><td class="text-end food-revenue">{{ money(area.revenue) }}</td></tr></tbody>
            </table>
          </div>
          <div v-else class="empty-state">Chưa có dữ liệu khu vực.</div>
        </article>
        <article class="report-card">
          <div class="section-heading"><div><h2>Doanh thu theo bàn</h2><p>Chỉ tính hóa đơn đã thanh toán</p></div></div>
          <div v-if="report.salesByTable?.length" class="table-wrap">
            <table><thead><tr><th>Bàn</th><th>Khu vực</th><th class="text-end">Hóa đơn</th><th class="text-end">Doanh thu</th></tr></thead>
              <tbody><tr v-for="table in report.salesByTable" :key="`${table.tableId}-${table.tableCode}`"><td class="food-name">{{ table.tableName }}<small>{{ table.tableCode }}</small></td><td>{{ table.areaName }}</td><td class="text-end">{{ number(table.invoiceCount) }}</td><td class="text-end food-revenue">{{ money(table.revenue) }}</td></tr></tbody>
            </table>
          </div>
          <div v-else class="empty-state">Chưa có dữ liệu bàn.</div>
        </article>
      </section>
      </template>

      <template v-if="activeReport === 'products' && productReport">
        <section class="metric-grid product-metrics" aria-label="Tổng quan sản phẩm">
          <article class="metric-card"><span class="metric-label">Món có bán</span><strong class="metric-value">{{ number(scopedSummary.productCount) }}</strong><span class="metric-note neutral-note">Trong kỳ đang xem</span></article>
          <article class="metric-card"><span class="metric-label">Lượt món đã bán</span><strong class="metric-value">{{ number(scopedSummary.soldQuantity) }}</strong><span class="metric-note" :class="productChangeTone(scopedSummary.soldQuantityChangePercent)">{{ changeText(scopedSummary.soldQuantityChangePercent, scopedSummary.previousSoldQuantity, scopedSummary.soldQuantity) }} so kỳ trước</span></article>
          <article class="metric-card revenue-card"><span class="metric-label">Doanh thu sau giảm</span><strong class="metric-value">{{ money(scopedSummary.revenue) }}</strong><span class="product-kpi-detail">Trước giảm {{ money(scopedSummary.grossRevenue) }}</span><span class="metric-note" :class="productChangeTone(scopedSummary.revenueChangePercent)">{{ changeText(scopedSummary.revenueChangePercent, scopedSummary.previousRevenue, scopedSummary.revenue) }} so kỳ trước</span></article>
        </section>
        <section class="product-insights-grid" aria-label="Phân tích sức bán và cơ cấu danh mục">
          <article class="report-card product-chart-card">
            <div class="section-heading product-chart-heading"><div><h2>Top {{ productTopLimit }} món bán tốt</h2><p>Tổng hợp theo món · mở chi tiết để xem từng size</p></div><div class="product-chart-actions"><label class="top-limit-select"><span class="sr-only">Số món hiển thị trong xếp hạng</span><select v-model.number="productTopLimit"><option :value="1">Top 1</option><option :value="3">Top 3</option><option :value="10">Top 10</option></select></label><div class="segmented-control" role="group" aria-label="Xếp hạng món bán"><button type="button" :class="{ selected: productChartMode === 'quantity' }" :aria-pressed="productChartMode === 'quantity'" @click="productChartMode = 'quantity'">Số lượng</button><button type="button" :class="{ selected: productChartMode === 'revenue' }" :aria-pressed="productChartMode === 'revenue'" @click="productChartMode = 'revenue'">Doanh thu</button></div></div></div>
            <div v-if="topProductRows.length" class="rank-bars">
              <article v-for="(product, index) in topProductRows" :key="`${product.foodKey}-${product.foodName}`" class="rank-food-row">
                <div class="rank-bar-heading"><span class="rank-badge">{{ index + 1 }}</span><div class="rank-bar-name" role="button" tabindex="0" @click="toggleProductExpanded(product)" @keydown.enter.prevent="toggleProductExpanded(product)" @keydown.space.prevent="toggleProductExpanded(product)">{{ product.foodName }}<small>{{ product.variants.length === 1 ? product.variants[0].variantName : `${number(product.variants.length)} size` }} · {{ product.categoryName }}</small></div><div class="rank-food-total"><strong>{{ productChartValue(product) }}</strong><small>{{ productChartSecondaryValue(product) }}</small></div></div>
                <div class="ratio-track product-bar-track"><span class="product-bar-fill" :style="{ width: `${productBarWidth(product)}%` }"></span></div>
                <div v-if="product.variants.length && isProductExpanded(product)" class="rank-variant-list"><div v-for="variant in product.variants" :key="`${product.foodKey}-${variant.variantId ?? variant.variantName}`" class="rank-variant-row"><span>{{ variant.variantName || 'Mặc định' }}</span><strong>{{ number(variant.soldQuantity) }} {{ variant.unit || 'lượt' }}</strong><b>{{ money(variant.revenue) }}</b></div></div>
              </article>
            </div>
            <div v-else class="empty-state">Chưa có món bán trong kỳ này.</div>
          </article>
          <article class="report-card product-chart-card product-category-compare-card">
            <div class="section-heading"><div><h2>So sánh 3 danh mục</h2><p>Ba danh mục dẫn đầu · kỳ này và kỳ trước</p></div></div>
            <div v-if="topCategoryRows.length" class="category-compare-list">
              <article v-for="(category, index) in topCategoryRows" :key="category.key" class="category-compare-row">
                <header><span class="rank-badge">{{ index + 1 }}</span><strong>{{ category.categoryName }}</strong><span :class="productChangeTone(category.revenueChangePercent)">{{ changeText(category.revenueChangePercent, category.previousRevenue, category.revenue) }}</span></header>
                <div class="category-compare-values"><div><span>Kỳ này</span><strong>{{ money(category.revenue) }}</strong><small>{{ number(category.soldQuantity) }} lượt bán</small></div><div><span>Kỳ trước</span><strong>{{ money(category.previousRevenue) }}</strong><small>{{ number(category.previousSoldQuantity) }} lượt bán</small></div></div>
                <footer><span :class="productChangeTone(category.soldQuantityChangePercent)">Số lượng {{ changeText(category.soldQuantityChangePercent, category.previousSoldQuantity, category.soldQuantity) }}</span><span>Tỷ trọng {{ percent(category.contributionPercent) }}</span></footer>
              </article>
            </div>
            <div v-else class="empty-state">Chưa có doanh thu danh mục trong hai kỳ này.</div>
            <p class="chart-footnote">Xếp theo doanh thu kỳ đang xem. Nếu kỳ trước bằng 0, không tính phần trăm tăng giảm.</p>
          </article>
        </section>
        <article class="report-card foods-card">
          <div class="section-heading"><div><h2>Chi tiết theo món ăn</h2><p>{{ number(filteredProductFoods.length) }} món · mở từng món để xem số lượng và doanh thu theo size</p></div></div>
          <section class="detail-filter-panel" aria-label="Bộ lọc chi tiết món ăn">
            <div class="detail-filter-heading"><div><strong>Lọc danh sách món</strong><small>Các bộ lọc này chỉ tác động đến bảng chi tiết bên dưới.</small></div><button v-if="hasDetailFilters" type="button" class="clear-product-filters" @click="resetDetailFilters">Xóa bộ lọc</button></div>
            <div class="product-toolbar" aria-label="Tìm kiếm và sắp xếp chi tiết món">
              <label class="toolbar-search"><span class="sr-only">Tìm món ăn</span><svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="11" cy="11" r="6.5"/><path d="m16 16 4 4"/></svg><input v-model.trim="productSearch" type="search" placeholder="Tìm tên món..." /></label>
              <label class="toolbar-select"><span class="sr-only">Sắp xếp sản phẩm</span><select v-model="productSort"><option value="qty_desc">Số lượng bán cao nhất</option><option value="revenue_desc">Doanh thu cao nhất</option><option value="growth_desc">Tăng trưởng số lượng cao nhất</option><option value="name_asc">Tên món A – Z</option></select></label>
            </div>
            <div class="detail-filter-grid">
              <label class="product-scope-select"><span>Danh mục món ăn</span><select v-model="detailCategoryFilter" @change="resetDetailFood"><option value="all">{{ productCatalogLoading && detailCategoryOptions.length === 0 ? 'Đang tải danh mục...' : 'Tất cả danh mục' }}</option><option v-for="category in detailCategoryOptions" :key="category.key" :value="category.key">{{ category.categoryName }}</option></select></label>
              <label class="product-scope-select"><span>Món ăn</span><select v-model="detailFoodFilter" :disabled="detailFoodOptions.length === 0" @change="resetDetailVariant"><option value="all">{{ productCatalogLoading && detailFoodOptions.length === 0 ? 'Đang tải món ăn...' : 'Tất cả món' }}</option><option v-for="food in detailFoodOptions" :key="food.key" :value="food.key">{{ food.foodName }}</option></select></label>
              <label class="product-scope-select"><span>Size / biến thể</span><select v-model="detailVariantFilter" :disabled="detailVariantOptions.length === 0"><option value="all">Tất cả size</option><option v-for="variant in detailVariantOptions" :key="variant.key" :value="variant.key">{{ variant.variantName }}</option></select></label>
              <label class="product-scope-select"><span>Tình trạng bán</span><select v-model="detailSalesFilter"><option value="all">Tất cả tình trạng</option><option value="sold">Có bán kỳ này</option><option value="new">Mới bán kỳ này</option><option value="increased">Số lượng tăng</option><option value="decreased">Số lượng giảm</option><option value="no_current">Không bán kỳ này</option><option value="no_activity">Không phát sinh cả hai kỳ</option></select></label>
            </div>
            <p v-if="productCatalogError" class="product-catalog-hint">{{ productCatalogError }}</p>
          </section>
          <template v-if="filteredProductFoods.length">
            <div class="table-wrap product-table-wrap"><table class="products-table"><thead><tr><th>Món ăn</th><th>Kỳ đang xem</th><th>Kỳ trước</th><th>Thay đổi</th><th class="text-end">Tỷ trọng DT</th></tr></thead>
              <tbody><template v-for="(product, index) in filteredProductFoods" :key="`${product.foodKey}-${product.foodName}`">
                <tr :class="{ 'product-row-expanded': isProductExpanded(product) }">
                  <td><div class="product-identity"><span class="rank-badge">{{ index + 1 }}</span><div class="food-name" role="button" tabindex="0" @click="toggleProductExpanded(product)" @keydown.enter.prevent="toggleProductExpanded(product)" @keydown.space.prevent="toggleProductExpanded(product)">{{ product.foodName }}<small>{{ product.categoryName }} · {{ number(product.variants.length) }} size</small></div></div></td>
                  <td class="product-period-cell"><strong>{{ number(product.soldQuantity) }} {{ product.unit || 'lượt' }}</strong><small>{{ money(product.revenue) }}</small></td>
                  <td class="product-period-cell"><strong>{{ number(product.previousSoldQuantity) }} {{ product.unit || 'lượt' }}</strong><small>{{ money(product.previousRevenue) }}</small></td>
                  <td class="product-change-cell"><span :class="productChangeTone(product.soldQuantityChangePercent)">SL {{ changeText(product.soldQuantityChangePercent, product.previousSoldQuantity, product.soldQuantity) }}</span><small :class="productChangeTone(product.revenueChangePercent)">DT {{ changeText(product.revenueChangePercent, product.previousRevenue, product.revenue) }}</small><span class="product-trend" :class="productTrend(product).tone">{{ productTrend(product).label }}</span></td>
                  <td class="text-end food-revenue">{{ percent(productContributionPercent(product)) }}</td>
                </tr>
                <tr v-if="isProductExpanded(product)" class="product-size-expand-row"><td colspan="5"><div class="product-size-list"><div class="product-size-grid product-size-heading"><span>Size</span><span>Kỳ này</span><span>Kỳ trước</span><span>Thay đổi</span></div><div v-for="variant in product.variants" :key="`${product.foodKey}-${variant.variantId ?? variant.variantName}`" class="product-size-grid"><span>{{ variant.variantName || 'Mặc định' }}</span><span><strong>{{ number(variant.soldQuantity) }} {{ variant.unit || 'lượt' }}</strong><small>{{ money(variant.revenue) }}</small></span><span><strong>{{ number(variant.previousSoldQuantity) }} {{ variant.unit || 'lượt' }}</strong><small>{{ money(variant.previousRevenue) }}</small></span><span><strong :class="productChangeTone(variant.soldQuantityChangePercent)">SL {{ changeText(variant.soldQuantityChangePercent, variant.previousSoldQuantity, variant.soldQuantity) }}</strong><small :class="productChangeTone(variant.revenueChangePercent)">DT {{ changeText(variant.revenueChangePercent, variant.previousRevenue, variant.revenue) }}</small></span></div></div></td></tr>
              </template></tbody>
            </table></div>
            <div class="product-mobile-list"><article v-for="(product, index) in filteredProductFoods" :key="`${product.foodKey}-${product.foodName}-mobile`" class="product-mobile-card">
              <header><span class="rank-badge">{{ index + 1 }}</span><div class="product-mobile-identity" role="button" tabindex="0" @click="toggleProductExpanded(product)" @keydown.enter.prevent="toggleProductExpanded(product)" @keydown.space.prevent="toggleProductExpanded(product)"><strong>{{ product.foodName }}</strong><small>{{ product.categoryName }} · {{ number(product.variants.length) }} size</small></div><span class="product-share">{{ percent(productContributionPercent(product)) }} DT</span></header>
              <div class="product-mobile-periods"><div><span>Kỳ này</span><strong>{{ number(product.soldQuantity) }} {{ product.unit || 'lượt' }}</strong><small>{{ money(product.revenue) }}</small></div><div><span>Kỳ trước</span><strong>{{ number(product.previousSoldQuantity) }} {{ product.unit || 'lượt' }}</strong><small>{{ money(product.previousRevenue) }}</small></div></div>
              <footer><span :class="productChangeTone(product.soldQuantityChangePercent)">SL {{ changeText(product.soldQuantityChangePercent, product.previousSoldQuantity, product.soldQuantity) }}</span><span :class="productChangeTone(product.revenueChangePercent)">DT {{ changeText(product.revenueChangePercent, product.previousRevenue, product.revenue) }}</span></footer>
              <div v-if="isProductExpanded(product)" class="product-size-list"><article v-for="variant in product.variants" :key="`${product.foodKey}-${variant.variantId ?? variant.variantName}`" class="product-size-mobile"><strong>{{ variant.variantName || 'Mặc định' }}</strong><div class="product-mobile-periods"><div><span>Kỳ này</span><strong>{{ number(variant.soldQuantity) }} {{ variant.unit || 'lượt' }}</strong><small>{{ money(variant.revenue) }}</small></div><div><span>Kỳ trước</span><strong>{{ number(variant.previousSoldQuantity) }} {{ variant.unit || 'lượt' }}</strong><small>{{ money(variant.previousRevenue) }}</small></div></div><footer><span :class="productChangeTone(variant.soldQuantityChangePercent)">SL {{ changeText(variant.soldQuantityChangePercent, variant.previousSoldQuantity, variant.soldQuantity) }}</span><span :class="productChangeTone(variant.revenueChangePercent)">DT {{ changeText(variant.revenueChangePercent, variant.previousRevenue, variant.revenue) }}</span></footer></article></div>
            </article></div>
          </template>
          <div v-else class="empty-state">{{ scopedProducts.length ? 'Không có món phù hợp với tìm kiếm hoặc bộ lọc chi tiết.' : 'Chưa có món bán trong hai kỳ đang so sánh.' }}</div>
        </article>
      </template>

      <template v-if="activeReport === 'finance' && financeReport">
        <section class="metric-grid finance-metrics" aria-label="Tổng quan tài chính">
          <article class="metric-card revenue-card"><span class="metric-label">Doanh thu sau giảm</span><strong class="metric-value">{{ money(financeReport.summary.revenue) }}</strong><span class="metric-note neutral-note">Hóa đơn đã thanh toán trong kỳ</span></article>
          <article class="metric-card"><span class="metric-label">Tiền nguyên liệu đã dùng</span><strong class="metric-value">{{ money(financeReport.summary.usedIngredientCost) }}</strong><span class="metric-note neutral-note">Theo ngày chốt đối soát bếp</span></article>
          <article class="metric-card"><span class="metric-label">Hàng hủy</span><strong class="metric-value negative-value">{{ money(financeReport.summary.wasteValue) }}</strong><span class="metric-note neutral-note">Bếp {{ money(financeReport.summary.kitchenWasteValue) }} · Kho {{ money(financeReport.summary.warehouseWasteValue) }}</span></article>
          <article class="metric-card"><span class="metric-label">Còn lại sau nguyên liệu và hủy</span><strong class="metric-value" :class="amountTone(financeReport.summary.operatingResultAfterWaste)">{{ money(financeReport.summary.operatingResultAfterWaste) }}</strong><span class="metric-note" :class="financeReport.summary.hasCompleteCosts ? 'neutral-note' : 'warning-note'">{{ financeReport.summary.hasCompleteCosts ? 'Chưa gồm lương, điện, mặt bằng và thuế' : 'Còn phiếu hoặc chi phí chưa đủ dữ liệu' }}</span></article>
        </section>
        <article class="report-card report-note-card"><strong>{{ financeReport.summary.hasCompleteCosts ? 'Cách đọc:' : 'Chưa thể chốt kết quả:' }}</strong> {{ financeReport.note }} Có {{ number(financeReport.summary.pendingLineCount) }} dòng xuất chưa đối soát ({{ money(financeReport.summary.pendingIssuedCost) }} đang theo dõi riêng), {{ number(financeReport.summary.missingCostLineCount) }} dòng đối soát và {{ number(financeReport.summary.missingWarehouseCostLineCount) }} dòng hủy kho thiếu giá vốn.</article>
        <section class="finance-grid">
          <article class="report-card">
            <div class="section-heading"><div><h2>Cơ cấu chi phí và hao hụt</h2><p>Giá trị tính theo đơn vị gốc của từng nguyên liệu</p></div></div>
            <div class="table-wrap"><table><thead><tr><th>Khoản mục</th><th class="text-end">Giá trị</th><th>Ý nghĩa</th></tr></thead><tbody>
              <tr><td>Doanh thu sau giảm</td><td class="text-end">{{ money(financeReport.summary.revenue) }}</td><td>Hóa đơn đã thanh toán</td></tr>
              <tr><td>Nguyên liệu đã dùng</td><td class="text-end">{{ money(financeReport.summary.usedIngredientCost) }}</td><td>Theo ngày chốt đối soát bếp</td></tr>
              <tr><td>Còn lại sau tiền nguyên liệu</td><td class="text-end">{{ money(financeReport.summary.grossProfit) }}</td><td>Doanh thu trừ nguyên liệu đã dùng</td></tr>
              <tr><td>Phiếu xuất đang chờ đối soát</td><td class="text-end">{{ money(financeReport.summary.pendingIssuedCost) }}</td><td>Chưa tính vào nguyên liệu đã dùng</td></tr>
              <tr><td>Nguyên liệu hoàn kho</td><td class="text-end">{{ money(financeReport.summary.returnedIngredientValue) }}</td><td>Phần hoàn trên các đối soát chốt trong kỳ</td></tr>
              <tr><td>Giá trị mua vào</td><td class="text-end">{{ money(financeReport.summary.purchaseValue) }}</td><td>Tổng phiếu nhập nhà cung cấp</td></tr>
              <tr><td>Hao hụt sơ chế</td><td class="text-end">{{ money(financeReport.summary.prepWastageValue) }}</td><td>Theo dõi riêng; đã nằm trong giá lô sạch</td></tr>
              <tr><td>Hủy tại bếp</td><td class="text-end">{{ money(financeReport.summary.kitchenWasteValue) }}</td><td>Không trừ kho lần hai</td></tr>
              <tr><td>Hủy tại kho</td><td class="text-end">{{ money(financeReport.summary.warehouseWasteValue) }}</td><td>Hủy lô tồn/quá hạn</td></tr>
            </tbody></table></div>
          </article>
          <article class="report-card ratio-card">
            <div class="section-heading"><div><h2>Tỷ trọng chi phí F&amp;B</h2><p>Giá vốn và hàng hủy theo doanh thu; hao hụt đầu vào theo giá trị mua</p></div></div>
            <div class="ratio-list">
              <div v-for="ratio in financeRatios" :key="ratio.id" class="ratio-row">
                <div class="ratio-heading"><span>{{ ratio.label }}</span><strong :class="`ratio-text-${ratio.tone}`">{{ percent(ratio.value) }}</strong></div>
                <div v-if="ratio.value !== null" class="ratio-track" role="progressbar" :aria-label="ratio.label" :aria-valuenow="ratioWidth(ratio.value)" :aria-valuetext="percent(ratio.value)" aria-valuemin="0" aria-valuemax="100"><span :class="`ratio-${ratio.tone}`" :style="{ width: `${ratioWidth(ratio.value)}%` }"></span></div>
                <small>{{ ratio.note }}</small>
              </div>
            </div>
          </article>
        </section>
        <article class="report-card foods-card">
          <div class="section-heading"><div><h2>Chi tiết hao hụt đầu vào theo nguyên liệu</h2><p>Hao hụt đầu vào đã nằm trong giá vốn lô sạch; hàng hủy được theo dõi riêng ở phần tổng hợp</p></div></div>
          <div v-if="financeReport.prepWastageByItem.length" class="table-wrap"><table><thead><tr><th>Nguyên liệu</th><th>Đơn vị gốc</th><th class="text-end">Hao hụt dự kiến</th><th class="text-end">Hao hụt thực tế</th><th class="text-end">Giá trị hao hụt</th><th class="text-end">Vượt dự kiến</th></tr></thead><tbody><tr v-for="item in financeReport.prepWastageByItem" :key="item.inventoryItemId"><td class="food-name">{{ item.itemName }}</td><td>{{ item.baseUnit }}</td><td class="text-end">{{ quantity(item.expectedQuantity) }} {{ item.baseUnit }}</td><td class="text-end">{{ quantity(item.quantity) }} {{ item.baseUnit }}</td><td class="text-end food-revenue">{{ money(item.value) }}</td><td class="text-end" :class="item.excessValue > 0 ? 'warning-value' : ''">{{ money(item.excessValue) }}</td></tr></tbody></table></div>
          <div v-else class="empty-state">Chưa có hao hụt đầu vào trong kỳ.</div>
        </article>
      </template>

      <InventoryLedgerReport v-if="activeReport === 'inventory'" :key="`inventory-${childReportRefreshKey}`" :from-date="fromDate" :to-date="toDate" />
      <KitchenReconciliationReport v-if="activeReport === 'kitchen'" :key="`kitchen-${childReportRefreshKey}`" :from-date="fromDate" :to-date="toDate" />
      <PaymentReconciliationReport v-if="activeReport === 'payments'" :key="`payments-${childReportRefreshKey}`" :from-date="fromDate" :to-date="toDate" />
      <OperationsReport v-if="activeReport === 'operations'" :key="`operations-${childReportRefreshKey}`" :from-date="fromDate" :to-date="toDate" />
  </main>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import InventoryLedgerReport from './components/InventoryLedgerReport.vue'
import KitchenReconciliationReport from './components/KitchenReconciliationReport.vue'
import PaymentReconciliationReport from './components/PaymentReconciliationReport.vue'
import OperationsReport from './components/OperationsReport.vue'

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5079'
const route = useRoute()
const today = toDateInput(new Date())
const firstOfMonth = `${today.slice(0, 8)}01`
const currentDate = new Date(`${today}T12:00:00`)
const quarterMonth = Math.floor(currentDate.getMonth() / 3) * 3 + 1
const firstOfQuarter = `${currentDate.getFullYear()}-${String(quarterMonth).padStart(2, '0')}-01`
const fromDate = ref(firstOfMonth)
const toDate = ref(today)
const filterFromDate = ref(firstOfMonth)
const filterToDate = ref(today)
const activePreset = ref('month')
const activeReport = ref(['sales', 'products', 'finance', 'inventory', 'kitchen', 'payments', 'operations'].includes(route.query.tab) ? route.query.tab : 'sales')
const loading = ref(false)
const errorMessage = ref('')
const report = ref(null)
const productReport = ref(null)
const financeReport = ref(null)
const reportLoading = ref({ sales: false, products: false, finance: false, inventory: false, kitchen: false, payments: false, operations: false })
const reportErrors = ref({})
let requestVersion = 0
let requestController
let productCatalogController
const restaurantCategories = ref([])
const restaurantFoods = ref([])
const restaurantCategoryCatalogLoaded = ref(false)
const productCatalogError = ref('')
const productCatalogLoading = ref(false)
const productSearch = ref('')
const productSort = ref('qty_desc')
const productTopLimit = ref(10)
const detailCategoryFilter = ref('all')
const detailFoodFilter = ref('all')
const detailVariantFilter = ref('all')
const detailSalesFilter = ref('all')
const productChartMode = ref('quantity')
const expandedProductKeys = ref(new Set())
const childReportRefreshKey = ref(0)
const reportTabs = [
  { id: 'sales', label: 'Doanh thu' },
  { id: 'products', label: 'Sản phẩm' },
  { id: 'finance', label: 'Tài chính' },
  { id: 'inventory', label: 'Kho' },
  { id: 'kitchen', label: 'Bếp & sơ chế' },
  { id: 'payments', label: 'Hóa đơn & Thanh toán' },
  { id: 'operations', label: 'Vận hành & Khách hàng' }
]
const coreReportTabs = reportTabs.slice(0, 3)
const reportMeta = {
  sales: {
    label: 'DOANH THU & DÒNG TIỀN',
    title: 'Theo dõi kết quả bán hàng',
    description: 'Đối chiếu doanh thu thực nhận, số hóa đơn và khu vực tạo doanh thu trong kỳ.'
  },
  products: {
    label: 'HIỆU QUẢ THỰC ĐƠN',
    title: 'Sức bán và sức hút thực đơn',
    description: 'So sánh lượng bán, doanh thu, món nổi bật và cơ cấu danh mục từ hóa đơn đã thanh toán. Không tính BOM hay giá vốn.'
  },
  finance: {
    label: 'ĐỐI SOÁT CHI PHÍ',
    title: 'Kiểm soát giá vốn và hao hụt',
    description: 'Chi phí bếp theo ngày chốt đối soát; hàng hủy tại kho theo ngày duyệt phiếu.'
  },
  inventory: {
    label: 'SỔ KHO',
    title: 'Theo dõi nhập, xuất và tồn',
    description: 'Tồn đầu và cuối kỳ tính từ biến động đã ghi nhận; cảnh báo so với tồn hiện tại được tách riêng.'
  },
  kitchen: {
    label: 'ĐỐI SOÁT BẾP & SƠ CHẾ',
    title: 'Theo dõi nguyên liệu đã xuất cho bếp',
    description: 'Xem nguyên liệu đã dùng, hoàn, hủy, phiếu chờ chốt và hao hụt lúc nhập.'
  },
  payments: {
    label: 'ĐỐI SOÁT THU TIỀN',
    title: 'Phát hiện bill và payment bất thường',
    description: 'So sánh số phải thu với payment thành công, không lặp biểu đồ thanh toán doanh thu.'
  },
  operations: {
    label: 'VẬN HÀNH & KHÁCH HÀNG',
    title: 'Đọc nhanh hoạt động nhà hàng',
    description: 'Đặt bàn, thời gian bếp, voucher và khách mua trong cùng một kỳ.'
  }
}
const isExtendedReport = computed(() => ['inventory', 'kitchen', 'payments', 'operations'].includes(activeReport.value))
const activeReportMeta = computed(() => reportMeta[activeReport.value] || reportMeta.sales)
const activeReportData = computed(() => {
  if (isExtendedReport.value) return null
  if (activeReport.value === 'products') return productReport.value
  if (activeReport.value === 'finance') return financeReport.value
  return report.value
})
const hasUnappliedDateFilter = computed(() => filterFromDate.value !== fromDate.value || filterToDate.value !== toDate.value)
const presets = [
  { id: 'today', label: 'Hôm nay' },
  { id: 'yesterday', label: 'Hôm qua' },
  { id: 'week', label: 'Tuần này' },
  { id: 'month', label: 'Tháng này' },
  { id: 'prev_month', label: 'Tháng trước' },
  { id: 'year', label: 'Năm nay' },
  { id: 'quarter', label: 'Quý này' }
]

function toDateInput(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function shiftDate(dateString, days) {
  const date = new Date(`${dateString}T12:00:00`)
  date.setDate(date.getDate() + days)
  return toDateInput(date)
}

function selectPreset(presetId) {
  activePreset.value = presetId
  toDate.value = today
  if (presetId === 'today') fromDate.value = today
  else if (presetId === 'yesterday') {
    fromDate.value = shiftDate(today, -1)
    toDate.value = fromDate.value
  }
  else if (presetId === 'week') {
    const dayOfWeek = currentDate.getDay()
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    fromDate.value = shiftDate(today, mondayOffset)
  } else if (presetId === 'prev_month') {
    const lastMonth = new Date(currentDate.getFullYear(), currentDate.getMonth(), 0, 12)
    fromDate.value = `${lastMonth.getFullYear()}-${String(lastMonth.getMonth() + 1).padStart(2, '0')}-01`
    toDate.value = toDateInput(lastMonth)
  } else if (presetId === 'year') fromDate.value = `${currentDate.getFullYear()}-01-01`
  else if (presetId === 'quarter') fromDate.value = firstOfQuarter
  else fromDate.value = firstOfMonth
  filterFromDate.value = fromDate.value
  filterToDate.value = toDate.value
  loadReport()
}

function applyCustomRange() {
  if (!filterFromDate.value || !filterToDate.value || filterFromDate.value > filterToDate.value) {
    errorMessage.value = 'Vui lòng chọn khoảng ngày hợp lệ.'
    return
  }
  activePreset.value = ''
  fromDate.value = filterFromDate.value
  toDate.value = filterToDate.value
  loadReport()
}

function refreshReports() {
  if (isExtendedReport.value) {
    childReportRefreshKey.value++
    return
  }
  loadProductCatalog()
  loadReport()
}

async function loadProductCatalog() {
  productCatalogController?.abort()
  productCatalogController = new AbortController()
  const { signal } = productCatalogController
  productCatalogLoading.value = true
  productCatalogError.value = ''
  const results = await Promise.allSettled([
    axios.get(`${API_BASE}/api/admin/category/list`, { signal, timeout: 5000 }),
    axios.get(`${API_BASE}/api/admin/food/list`, { params: { isActive: true }, signal, timeout: 5000 })
  ])
  if (signal.aborted) return

  const [categoryResult, foodResult] = results
  if (categoryResult.status === 'fulfilled') {
    const data = categoryResult.value.data?.data
    if (Array.isArray(data)) {
      restaurantCategories.value = data.filter(category => category.isActive !== false)
      restaurantCategoryCatalogLoaded.value = true
    }
  }
  if (foodResult.status === 'fulfilled') {
    const data = foodResult.value.data?.data
    if (Array.isArray(data)) restaurantFoods.value = data.filter(food => food.isActive !== false
      && (!food.foodStatus || food.foodStatus === 'available' || food.foodStatus === 'sold_out'))
  }
  if (categoryResult.status === 'rejected' && foodResult.status === 'rejected')
    productCatalogError.value = 'Không tải được danh mục thực đơn; danh sách lọc chỉ gồm món có phát sinh trong kỳ.'
  productCatalogLoading.value = false
}

async function loadReport() {
  if (!fromDate.value || !toDate.value || fromDate.value > toDate.value) {
    errorMessage.value = 'Vui lòng chọn khoảng ngày hợp lệ.'
    return
  }
  const version = ++requestVersion
  requestController?.abort()
  requestController = new AbortController()
  const signal = requestController.signal
  const params = { fromDate: fromDate.value, toDate: toDate.value }
  loading.value = true
  errorMessage.value = ''
  reportErrors.value = {}
  reportLoading.value = { ...reportLoading.value, sales: true, products: true, finance: true }
  report.value = null
  productReport.value = null
  financeReport.value = null
  try {
    const targets = { sales: report, products: productReport, finance: financeReport }
    await Promise.allSettled(coreReportTabs.map(async tab => {
      try {
        const response = await axios.get(`${API_BASE}/api/admin/reports/${tab.id}`, {
          params, signal, timeout: 5000
        })
        if (version !== requestVersion) return
        if (!response.data?.success || !response.data.data) throw new Error('Dữ liệu báo cáo không hợp lệ.')
        targets[tab.id].value = response.data.data
      } catch (error) {
        if (version !== requestVersion || signal.aborted) return
        const status = error.response?.status
        reportErrors.value[tab.id] = error.code === 'ECONNABORTED'
          ? 'Quá thời gian tải. Hãy thử lại.'
          : status
            ? `API trả về HTTP ${status}. ${error.response?.data?.message || 'Kiểm tra API và quyền truy cập.'}`
            : (error.message || 'Không kết nối được API báo cáo.')
        const failed = coreReportTabs.filter(item => reportErrors.value[item.id]).map(item => item.label)
        errorMessage.value = `Chưa tải được: ${failed.join(', ')}. Bạn vẫn có thể xem các tab đã tải xong.`
      } finally {
        if (version === requestVersion) reportLoading.value[tab.id] = false
      }
    }))
  } catch (error) {
    if (version === requestVersion) errorMessage.value = error.response?.data?.message || error.message || 'Không tải được báo cáo. Vui lòng thử lại.'
  } finally {
    if (version === requestVersion) loading.value = false
  }
}

const rangeDays = computed(() => {
  if (!report.value) return 0
  const start = new Date(`${report.value.fromDate}T12:00:00`)
  const end = new Date(`${report.value.toDate}T12:00:00`)
  return Math.round((end - start) / 86400000) + 1
})
const trendMode = computed(() => {
  if (rangeDays.value <= 1) return 'hour'
  if (rangeDays.value <= 7) return 'day'
  if (rangeDays.value <= 31) return 'week'
  return 'month'
})
const trendGranularity = computed(() => {
  if (trendMode.value === 'hour') return { title: 'Doanh thu theo giờ', subtitle: '24 khung giờ trong ngày', tableLabel: 'Khung giờ' }
  if (trendMode.value === 'day') return { title: 'Doanh thu theo ngày', subtitle: 'Từng ngày trong kỳ', tableLabel: 'Ngày' }
  if (trendMode.value === 'week') return { title: 'Doanh thu theo tuần', subtitle: 'Gom theo tuần, bắt đầu từ thứ Hai', tableLabel: 'Tuần' }
  return { title: 'Doanh thu theo tháng', subtitle: 'Gom theo từng tháng', tableLabel: 'Tháng' }
})
const trendData = computed(() => {
  if (!report.value) return []
  if (trendMode.value === 'hour') {
    return (report.value.hourlyTrend || []).map(item => ({
      key: `hour-${item.hour}`,
      label: formatHour(item.hour),
      tableLabel: formatHour(item.hour),
      tooltipLabel: formatHour(item.hour),
      revenue: Number(item.revenue || 0)
    }))
  }
  if (trendMode.value === 'day') {
    return (report.value.dailyTrend || []).map(item => ({
      key: item.date,
      label: shortDate(item.date),
      tableLabel: formatDate(item.date),
      tooltipLabel: formatDate(item.date),
      revenue: Number(item.revenue || 0)
    }))
  }

  const grouped = new Map()
  const selectedStart = new Date(`${report.value.fromDate}T12:00:00`)
  const selectedEnd = new Date(`${report.value.toDate}T12:00:00`)
  for (const item of report.value.dailyTrend || []) {
    const date = new Date(`${String(item.date).slice(0, 10)}T12:00:00`)
    let key
    let label
    let tableLabel
    let tooltipLabel
    if (trendMode.value === 'week') {
      const dayOfWeek = date.getDay()
      const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
      const weekStart = new Date(date)
      weekStart.setDate(date.getDate() + mondayOffset)
      const weekEnd = new Date(weekStart)
      weekEnd.setDate(weekStart.getDate() + 6)
      const weekStartValue = toDateInput(weekStart)
      const labelStart = weekStart < selectedStart ? selectedStart : weekStart
      const labelEnd = weekEnd > selectedEnd ? selectedEnd : weekEnd
      const labelStartValue = toDateInput(labelStart)
      const labelEndValue = toDateInput(labelEnd)
      key = weekStartValue
      label = `${shortDate(labelStartValue)}-${shortDate(labelEndValue)}`
      tableLabel = `${formatDate(labelStartValue)} - ${formatDate(labelEndValue)}`
      tooltipLabel = `Tuần ${tableLabel}`
    } else {
      key = String(item.date).slice(0, 7)
      const [year, month] = key.split('-')
      label = `T${Number(month)}`
      tableLabel = `${month}/${year}`
      tooltipLabel = `Tháng ${tableLabel}`
    }
    const current = grouped.get(key) || { key, label, tableLabel, tooltipLabel, revenue: 0 }
    current.revenue += Number(item.revenue || 0)
    grouped.set(key, current)
  }
  return Array.from(grouped.values()).sort((left, right) => left.key.localeCompare(right.key))
})
const peakTrend = computed(() => trendData.value.reduce((best, item) => Number(item.revenue) > Number(best.revenue) ? item : best, {
  tableLabel: 'Chưa có dữ liệu',
  revenue: 0
}))
const maxRevenue = computed(() => Math.max(0, ...trendData.value.map(item => Number(item.revenue))))
const chartRows = computed(() => {
  const rows = trendData.value
  const max = maxRevenue.value || 1
  return rows.map((item, index) => ({
    ...item,
    x: rows.length <= 1 ? 473 : 68 + index * (808 / (rows.length - 1)),
    y: 226 - (Number(item.revenue) / max) * 190
  }))
})
const chartPoints = computed(() => chartRows.value.map(item => `${item.x},${item.y}`).join(' '))
const areaPath = computed(() => {
  if (!chartRows.value.length) return ''
  const first = chartRows.value[0]
  const last = chartRows.value[chartRows.value.length - 1]
  return `M ${first.x} 226 L ${chartRows.value.map(item => `${item.x} ${item.y}`).join(' L ')} L ${last.x} 226 Z`
})
const chartGrid = computed(() => {
  const max = maxRevenue.value || 1
  return [0, 1, 2, 3, 4].map(step => ({ value: max * (4 - step) / 4, y: 36 + step * 47.5 }))
})
const visiblePoints = computed(() => {
  const rows = chartRows.value
  if (rows.length <= 7) return rows
  const indexes = new Set([0, Math.round((rows.length - 1) / 5), Math.round(2 * (rows.length - 1) / 5), Math.round(3 * (rows.length - 1) / 5), Math.round(4 * (rows.length - 1) / 5), rows.length - 1])
  return rows.filter((_, index) => indexes.has(index))
})
const hasTrendRevenue = computed(() => maxRevenue.value > 0)
const changeTone = computed(() => {
  const change = report.value?.summary?.revenueChangePercent
  return change > 0 ? 'positive-note' : change < 0 ? 'negative-note' : 'neutral-note'
})
const scopedProducts = computed(() => productReport.value?.products || [])
const detailCategoryOptions = computed(() => {
  const categories = new Map()
  for (const category of restaurantCategories.value) {
    const key = String(category.categoryId)
    categories.set(key, category.categoryName || 'Chưa phân loại')
  }
  for (const category of productReport.value?.menuCategories || []) {
    const key = category.categoryId == null ? 'unclassified' : String(category.categoryId)
    if (!categories.has(key)) categories.set(key, category.categoryName || 'Chưa phân loại')
  }
  for (const product of scopedProducts.value) {
    const key = product.categoryId == null ? 'unclassified' : String(product.categoryId)
    if (!categories.has(key)) categories.set(key, product.categoryName || 'Chưa phân loại')
  }
  return [...categories].map(([key, categoryName]) => ({ key, categoryName }))
    .sort((left, right) => left.categoryName.localeCompare(right.categoryName, 'vi'))
})
const detailFoodOptions = computed(() => {
  const foods = new Map()
  const activeCategoryKeys = new Set(restaurantCategories.value.map(category => String(category.categoryId)))
  const matchesCategory = categoryId => detailCategoryFilter.value === 'all'
    || (categoryId == null ? 'unclassified' : String(categoryId)) === detailCategoryFilter.value
  for (const food of restaurantFoods.value) {
    if (!matchesCategory(food.categoryId)
      || (restaurantCategoryCatalogLoaded.value && !activeCategoryKeys.has(String(food.categoryId)))) continue
    foods.set(String(food.foodId), {
      key: String(food.foodId), foodName: food.foodName, categoryId: food.categoryId
    })
  }
  for (const product of scopedProducts.value) {
    if (!matchesCategory(product.categoryId)) continue
    const key = product.foodId == null ? `snapshot:${product.foodName || 'unknown'}` : String(product.foodId)
    if (!foods.has(key)) foods.set(key, { key, foodName: product.foodName || 'Món không còn trong thực đơn', categoryId: product.categoryId })
  }
  return [...foods.values()].sort((left, right) => left.foodName.localeCompare(right.foodName, 'vi'))
})
const hasDetailFilters = computed(() => productSearch.value !== '' || productSort.value !== 'qty_desc'
  || detailCategoryFilter.value !== 'all' || detailFoodFilter.value !== 'all'
  || detailVariantFilter.value !== 'all' || detailSalesFilter.value !== 'all')
function groupProductsByFood(products) {
  const groups = new Map()
  for (const product of products) {
    const foodKey = product.foodId == null ? `snapshot:${product.foodName || 'unknown'}` : String(product.foodId)
    if (!groups.has(foodKey)) groups.set(foodKey, {
      foodKey, foodId: product.foodId, foodName: product.foodName || 'Món không còn trong thực đơn',
      categoryId: product.categoryId, categoryName: product.categoryName, variants: [],
      soldQuantity: 0, previousSoldQuantity: 0, revenue: 0, previousRevenue: 0
    })
    const food = groups.get(foodKey)
    food.variants.push(product)
    food.soldQuantity += Number(product.soldQuantity || 0)
    food.previousSoldQuantity += Number(product.previousSoldQuantity || 0)
    food.revenue += Number(product.revenue || 0)
    food.previousRevenue += Number(product.previousRevenue || 0)
  }
  return [...groups.values()].map(food => {
    const units = [...new Set(food.variants.map(variant => variant.unit).filter(Boolean))]
    return {
      ...food,
      unit: units.length === 1 ? units[0] : 'lượt',
      soldQuantityChangePercent: productGrowth(food.soldQuantity, food.previousSoldQuantity),
      revenueChangePercent: productGrowth(food.revenue, food.previousRevenue),
      variants: food.variants.sort((left, right) => Number(right.soldQuantity || 0) - Number(left.soldQuantity || 0)
        || String(left.variantName || '').localeCompare(String(right.variantName || ''), 'vi'))
    }
  })
}
function groupProductsByVariant(products) {
  const rows = new Map()
  for (const product of products) {
    const foodKey = product.foodId == null ? `snapshot:${product.foodName || 'unknown'}` : String(product.foodId)
    const variantKey = product.variantId == null ? 'default' : String(product.variantId)
    const rowKey = `${foodKey}|${variantKey}`
    if (!rows.has(rowKey)) rows.set(rowKey, {
      rowKey,
      foodKey,
      variantKey,
      foodName: product.foodName || 'Món không còn trong thực đơn',
      variantName: product.variantName || 'Mặc định',
      categoryId: product.categoryId,
      categoryName: product.categoryName || 'Chưa phân loại',
      unit: product.unit || 'lượt',
      soldQuantity: 0,
      previousSoldQuantity: 0,
      grossRevenue: 0,
      previousGrossRevenue: 0,
      revenue: 0,
      previousRevenue: 0
    })
    const row = rows.get(rowKey)
    row.soldQuantity += Number(product.soldQuantity || 0)
    row.previousSoldQuantity += Number(product.previousSoldQuantity || 0)
    row.grossRevenue += Number(product.grossRevenue || 0)
    row.previousGrossRevenue += Number(product.previousGrossRevenue || 0)
    row.revenue += Number(product.revenue || 0)
    row.previousRevenue += Number(product.previousRevenue || 0)
  }
  return [...rows.values()].map(row => ({
    ...row,
    soldQuantityChangePercent: productGrowth(row.soldQuantity, row.previousSoldQuantity),
    revenueChangePercent: productGrowth(row.revenue, row.previousRevenue)
  }))
}
function sortProductRows(rows) {
  return [...rows].sort((left, right) => {
    if (productSort.value === 'name_asc') {
      return `${left.foodName}${left.variantName}`.localeCompare(`${right.foodName}${right.variantName}`, 'vi')
    }
    if (productSort.value === 'growth_desc') {
      const leftGrowth = left.soldQuantityChangePercent
      const rightGrowth = right.soldQuantityChangePercent
      if (leftGrowth == null || rightGrowth == null) {
        if (leftGrowth == null && rightGrowth != null) return 1
        if (rightGrowth == null && leftGrowth != null) return -1
      } else if (leftGrowth !== rightGrowth) return Number(rightGrowth) - Number(leftGrowth)
    }
    const leftValue = productSort.value === 'revenue_desc' ? left.revenue : left.soldQuantity
    const rightValue = productSort.value === 'revenue_desc' ? right.revenue : right.soldQuantity
    return Number(rightValue || 0) - Number(leftValue || 0)
      || `${left.foodName}${left.variantName}`.localeCompare(`${right.foodName}${right.variantName}`, 'vi')
  })
}
const foodSalesRows = computed(() => groupProductsByFood(scopedProducts.value))
const categoryComparisonRows = computed(() => {
  const categories = new Map()
  for (const product of scopedProducts.value) {
    const key = product.categoryId == null ? 'unclassified' : String(product.categoryId)
    if (!categories.has(key)) categories.set(key, {
      key, categoryName: product.categoryName || 'Chưa phân loại',
      revenue: 0, previousRevenue: 0, soldQuantity: 0, previousSoldQuantity: 0
    })
    const category = categories.get(key)
    category.revenue += Number(product.revenue || 0)
    category.previousRevenue += Number(product.previousRevenue || 0)
    category.soldQuantity += Number(product.soldQuantity || 0)
    category.previousSoldQuantity += Number(product.previousSoldQuantity || 0)
  }
  return [...categories.values()]
    .filter(category => category.revenue > 0 || category.previousRevenue > 0)
    .map(category => ({
      ...category,
      revenueChangePercent: productGrowth(category.revenue, category.previousRevenue),
      soldQuantityChangePercent: productGrowth(category.soldQuantity, category.previousSoldQuantity)
    }))
})
const topCategoryRows = computed(() => [...categoryComparisonRows.value]
  .sort((left, right) => right.revenue - left.revenue || right.previousRevenue - left.previousRevenue)
  .slice(0, 3)
  .map(category => ({
    ...category,
    contributionPercent: scopedSummary.value.revenue > 0 ? category.revenue / scopedSummary.value.revenue * 100 : null
  })))
function resetDetailFood() {
  detailFoodFilter.value = 'all'
  detailVariantFilter.value = 'all'
}
function resetDetailVariant() {
  detailVariantFilter.value = 'all'
}
function resetDetailFilters() {
  productSearch.value = ''
  productSort.value = 'qty_desc'
  detailCategoryFilter.value = 'all'
  detailFoodFilter.value = 'all'
  detailVariantFilter.value = 'all'
  detailSalesFilter.value = 'all'
}
function productRowKey(product) {
  return `${product.foodKey}-${product.foodName}`
}
function isProductExpanded(product) {
  return expandedProductKeys.value.has(productRowKey(product))
}
function toggleProductExpanded(product) {
  const key = productRowKey(product)
  const next = new Set(expandedProductKeys.value)
  if (next.has(key)) next.delete(key)
  else next.add(key)
  expandedProductKeys.value = next
}
function productGrowth(current, previous) {
  return previous > 0 ? Math.round((current - previous) / previous * 1000) / 10 : null
}
const scopedSummary = computed(() => {
  const products = scopedProducts.value
  const sum = field => products.reduce((total, product) => total + Number(product[field] || 0), 0)
  const soldQuantity = sum('soldQuantity')
  const previousSoldQuantity = sum('previousSoldQuantity')
  const grossRevenue = sum('grossRevenue')
  const previousGrossRevenue = sum('previousGrossRevenue')
  const revenue = sum('revenue')
  const previousRevenue = sum('previousRevenue')
  const soldFoods = groupProductsByFood(products).filter(product => Number(product.soldQuantity || 0) > 0)
  return {
    productCount: soldFoods.length,
    soldQuantity, previousSoldQuantity, soldQuantityChangePercent: productGrowth(soldQuantity, previousSoldQuantity),
    grossRevenue, previousGrossRevenue, grossRevenueChangePercent: productGrowth(grossRevenue, previousGrossRevenue),
    revenue, previousRevenue, revenueChangePercent: productGrowth(revenue, previousRevenue)
  }
})
function productContributionPercent(product) {
  return scopedSummary.value.revenue > 0 ? Number(product.revenue || 0) / scopedSummary.value.revenue * 100 : null
}
const filteredAndSortedProducts = computed(() => {
  const search = productSearch.value.toLocaleLowerCase('vi-VN')
  const products = scopedProducts.value.filter(product => {
    const haystack = `${product.foodName || ''} ${product.variantName || ''}`.toLocaleLowerCase('vi-VN')
    return !search || haystack.includes(search)
  })

  return [...products].sort((left, right) => {
    if (productSort.value === 'name_asc') {
      return `${left.foodName || ''}${left.variantName || ''}`.localeCompare(`${right.foodName || ''}${right.variantName || ''}`, 'vi')
    }
    if (productSort.value === 'growth_desc') {
      const leftGrowth = left.soldQuantityChangePercent
      const rightGrowth = right.soldQuantityChangePercent
      if (leftGrowth == null || rightGrowth == null) {
        if (leftGrowth == null && rightGrowth != null) return 1
        if (rightGrowth == null && leftGrowth != null) return -1
      } else if (leftGrowth !== rightGrowth) return Number(rightGrowth) - Number(leftGrowth)
    }
    const leftValue = productSort.value === 'revenue_desc' ? left.revenue : left.soldQuantity
    const rightValue = productSort.value === 'revenue_desc' ? right.revenue : right.soldQuantity
    const difference = Number(leftValue) - Number(rightValue)
    if (difference !== 0) return -difference
    return `${left.foodName || ''}${left.variantName || ''}`.localeCompare(`${right.foodName || ''}${right.variantName || ''}`, 'vi')
  })
})
const filteredDetailProducts = computed(() => filteredAndSortedProducts.value.filter(product => {
  const categoryKey = product.categoryId == null ? 'unclassified' : String(product.categoryId)
  const foodKey = product.foodId == null ? `snapshot:${product.foodName || 'unknown'}` : String(product.foodId)
  const variantKey = product.variantId == null ? 'default' : String(product.variantId)
  const current = Number(product.soldQuantity || 0)
  const previous = Number(product.previousSoldQuantity || 0)
  const matchesStatus = detailSalesFilter.value === 'all'
    || (detailSalesFilter.value === 'sold' && current > 0)
    || (detailSalesFilter.value === 'new' && current > 0 && previous === 0)
    || (detailSalesFilter.value === 'increased' && previous > 0 && current > previous)
    || (detailSalesFilter.value === 'decreased' && previous > 0 && current < previous)
    || (detailSalesFilter.value === 'no_current' && current === 0 && previous > 0)
    || (detailSalesFilter.value === 'no_activity' && current === 0 && previous === 0)
  return (detailCategoryFilter.value === 'all' || categoryKey === detailCategoryFilter.value)
    && (detailFoodFilter.value === 'all' || foodKey === detailFoodFilter.value)
    && (detailVariantFilter.value === 'all' || variantKey === detailVariantFilter.value)
    && matchesStatus
}))
const filteredProductFoods = computed(() => sortProductRows(groupProductsByFood(filteredDetailProducts.value)))
const detailVariantOptions = computed(() => groupProductsByVariant(scopedProducts.value.filter(product => {
  const categoryKey = product.categoryId == null ? 'unclassified' : String(product.categoryId)
  const foodKey = product.foodId == null ? `snapshot:${product.foodName || 'unknown'}` : String(product.foodId)
  return (detailCategoryFilter.value === 'all' || categoryKey === detailCategoryFilter.value)
    && (detailFoodFilter.value === 'all' || foodKey === detailFoodFilter.value)
})).map(product => ({
  key: product.variantKey,
  variantName: product.variantName || 'Mặc định'
})).filter((variant, index, variants) => variants.findIndex(item => item.key === variant.key) === index)
  .sort((left, right) => left.variantName.localeCompare(right.variantName, 'vi')))
const topProductRows = computed(() => {
  const metric = productChartMode.value === 'revenue' ? 'revenue' : 'soldQuantity'
  return [...foodSalesRows.value]
    .filter(product => Number(product.soldQuantity || 0) > 0)
    .sort((left, right) => Number(right[metric] || 0) - Number(left[metric] || 0))
    .slice(0, Number(productTopLimit.value) || 10)
})
const maxTopProductValue = computed(() => {
  const metric = productChartMode.value === 'revenue' ? 'revenue' : 'soldQuantity'
  return Math.max(0, ...topProductRows.value.map(product => Number(product[metric] || 0)))
})
function productChartValue(product) {
  return productChartMode.value === 'revenue' ? money(product.revenue) : `${number(product.soldQuantity)} ${product.unit || 'lượt'}`
}
function productChartSecondaryValue(product) {
  return productChartMode.value === 'revenue'
    ? `${number(product.soldQuantity)} ${product.unit || 'lượt'} đã bán`
    : money(product.revenue)
}
function productBarWidth(product) {
  const metric = productChartMode.value === 'revenue' ? 'revenue' : 'soldQuantity'
  return maxTopProductValue.value > 0 ? Number(product[metric] || 0) / maxTopProductValue.value * 100 : 0
}
function productChangeTone(value) {
  return value > 0 ? 'positive-note' : value < 0 ? 'negative-note' : 'neutral-note'
}
function changeText(value, previous = null, current = null) {
  if (value == null) {
    if (Number(current || 0) > 0 && Number(previous || 0) === 0) return 'Mới bán'
    return 'Không phát sinh'
  }
  if (value > 0) return `▲ ${percent(value)}`
  if (value < 0) return `▼ ${percent(Math.abs(value))}`
  return '— 0%'
}
function productTrend(product) {
  const previous = Number(product.previousSoldQuantity || 0)
  const current = Number(product.soldQuantity || 0)
  if (previous === 0 && current > 0) return { label: '✨ Mới bán', tone: 'trend-new' }
  if (current > previous) return { label: '🔥 Bán chạy', tone: 'trend-up' }
  if (current < previous) return { label: '❄️ Bán chậm', tone: 'trend-down' }
  return { label: '— Ổn định', tone: 'trend-flat' }
}
const financeRatios = computed(() => {
  const summary = financeReport.value?.summary
  if (!summary) return []
  const revenue = Number(summary.revenue || 0)
  const ratio = value => revenue > 0 && value !== null ? Number(value || 0) / revenue * 100 : null
  const foodCost = summary.hasCompleteCosts ? ratio(summary.ingredientCost) : null
  const wasteCostsKnown = Number(summary.missingCostLineCount || 0) === 0 && Number(summary.missingWarehouseCostLineCount || 0) === 0
  const wasteRatio = wasteCostsKnown ? ratio(summary.wasteValue) : null
  const purchaseValue = Number(summary.purchaseValue || 0)
  const prepRatio = purchaseValue > 0 ? Number(summary.prepWastageValue || 0) / purchaseValue * 100 : null
  const expectedPrepRatio = purchaseValue > 0 ? Number(summary.expectedPrepWastageValue || 0) / purchaseValue * 100 : null
  const operatingMargin = summary.hasCompleteCosts ? ratio(summary.operatingResultAfterWaste) : null
  return [
    {
      id: 'food-cost',
      label: 'Tỷ lệ giá vốn nguyên liệu (Food Cost %)',
      value: foodCost,
      tone: 'neutral',
      note: !summary.hasCompleteCosts ? 'Chờ đủ đối soát và giá vốn' : revenue > 0 ? 'Nguyên liệu đã dùng / doanh thu trong kỳ' : 'Chưa có doanh thu để tính'
    },
    {
      id: 'waste',
      label: 'Tỷ lệ hàng hủy bếp & kho',
      value: wasteRatio,
      tone: wasteRatio === null ? 'neutral' : wasteRatio > 0 ? 'warn' : 'good',
      note: !wasteCostsKnown ? 'Chưa đủ giá vốn hàng hủy để tính' : revenue > 0 ? 'Giá trị hàng hủy / doanh thu trong kỳ' : 'Chưa có doanh thu để tính'
    },
    {
      id: 'prep',
      label: 'Hao hụt sơ chế / giá trị mua',
      value: prepRatio,
      tone: prepRatio === null ? 'neutral' : Number(summary.excessPrepWastageValue || 0) > 0 ? 'warn' : 'good',
      note: prepRatio === null ? 'Chưa có nhập hàng trong kỳ' : `Dự kiến ${percent(expectedPrepRatio)} theo định mức lúc nhập; vượt ${money(summary.excessPrepWastageValue)}`
    },
    {
      id: 'margin',
      label: 'Biên lợi nhuận sau hàng hủy',
      value: operatingMargin,
      tone: operatingMargin === null ? 'neutral' : operatingMargin >= 0 ? 'good' : 'bad',
      note: !summary.hasCompleteCosts ? 'Chờ đủ đối soát và giá vốn' : revenue > 0 ? 'Chưa bao gồm lương, điện và mặt bằng' : 'Chưa có doanh thu để tính'
    }
  ]
})

function ratioWidth(value) {
  return Math.min(100, Math.max(0, Number(value || 0)))
}

function amountTone(value) {
  return value === null || value === undefined ? 'neutral-note' : Number(value) < 0 ? 'negative-value' : 'positive-value'
}

function number(value) {
  return Number(value || 0).toLocaleString('vi-VN')
}

function money(value) {
  if (value === null || value === undefined) return 'Chưa tính'
  return `${Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 0 })} ₫`
}

function quantity(value) {
  return Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 2 })
}

function percent(value) {
  if (value === null || value === undefined) return 'Chưa tính'
  return `${Number(value || 0).toLocaleString('vi-VN', { maximumFractionDigits: 1 })}%`
}

function compactMoney(value) {
  const amount = Number(value || 0)
  if (amount >= 1_000_000) return `${(amount / 1_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })}tr`
  if (amount >= 1_000) return `${(amount / 1_000).toLocaleString('vi-VN', { maximumFractionDigits: 0 })}k`
  return `${Math.round(amount)}`
}

function formatDate(value) {
  const date = new Date(`${String(value).slice(0, 10)}T12:00:00`)
  return date.toLocaleDateString('vi-VN')
}

function shortDate(value) {
  const date = new Date(`${String(value).slice(0, 10)}T12:00:00`)
  return `${date.getDate()}/${date.getMonth() + 1}`
}

function formatHour(value) {
  return `${String(value).padStart(2, '0')}h`
}

function paymentMethodLabel(method) {
  const labels = { cash: 'Tiền mặt', transfer: 'Chuyển khoản', card: 'Thẻ', e_wallet: 'Ví điện tử' }
  return labels[String(method || '').toLowerCase()] || method || 'Khác'
}

function paymentShare(amount) {
  const total = report.value?.paymentMethods?.reduce((sum, method) => sum + Number(method.amount || 0), 0) || 0
  return total ? Math.round(Number(amount || 0) / total * 100) : 0
}

function csvCell(value) {
  return `"${String(value ?? '').replaceAll('"', '""')}"`
}

function exportCsv() {
  if (!activeReportData.value) return
  if (activeReport.value === 'products' && productReport.value) {
    const productRows = [
      ['Báo cáo sản phẩm', `${productReport.value.fromDate} - ${productReport.value.toDate}`],
      ['Kỳ so sánh', `${productReport.value.previousFromDate} - ${productReport.value.previousToDate}`],
      ['Danh mục lọc chi tiết', detailCategoryFilter.value === 'all'
        ? 'Tất cả danh mục'
        : detailCategoryOptions.value.find(category => category.key === detailCategoryFilter.value)?.categoryName || 'Danh mục đã chọn'],
      [],
      ['Hạng', 'Món ăn', 'Size / biến thể', 'Danh mục', 'SL kỳ này', 'Đơn vị', 'Doanh thu kỳ này', 'SL kỳ trước', 'Doanh thu kỳ trước', 'Thay đổi SL', 'Thay đổi doanh thu', 'Tỷ trọng doanh thu'],
      ...filteredProductFoods.value.flatMap((item, index) => item.variants.map((variant, variantIndex) => [
        variantIndex === 0 ? index + 1 : '', item.foodName, variant.variantName, item.categoryName,
        variant.soldQuantity, variant.unit, variant.revenue, variant.previousSoldQuantity, variant.previousRevenue,
        changeText(variant.soldQuantityChangePercent, variant.previousSoldQuantity, variant.soldQuantity),
        changeText(variant.revenueChangePercent, variant.previousRevenue, variant.revenue), productContributionPercent(variant)
      ]))
    ]
    downloadCsv(productRows, `bao-cao-san-pham-${productReport.value.fromDate}-${productReport.value.toDate}.csv`)
    return
  }
  if (activeReport.value === 'finance' && financeReport.value) {
    const summary = financeReport.value.summary
    const financeRows = [
      ['Báo cáo tài chính vận hành', `${financeReport.value.fromDate} - ${financeReport.value.toDate}`],
      [],
      ['Khoản mục', 'Giá trị'],
      ['Doanh thu thực nhận', summary.revenue],
      ['Giá vốn đã đối soát', summary.ingredientCost],
      ['Nguyên liệu đã dùng', summary.usedIngredientCost],
      ['Phiếu xuất chờ đối soát', summary.pendingIssuedCost],
      ['Giá trị mua vào', summary.purchaseValue],
      ['Chênh lệch thu / giá vốn', summary.grossProfit],
      ['Nguyên liệu hoàn kho', summary.returnedIngredientValue],
      ['Dòng chờ đối soát đến cuối kỳ', summary.pendingLineCount],
      ['Dòng chốt thiếu giá vốn', summary.missingCostLineCount],
      ['Dòng hủy kho thiếu giá vốn', summary.missingWarehouseCostLineCount],
      ['Hao hụt sơ chế', summary.prepWastageValue],
      ['Hủy tại bếp', summary.kitchenWasteValue],
      ['Hủy tại kho', summary.warehouseWasteValue],
      ['Kết quả sau hủy', summary.operatingResultAfterWaste],
      ['Cơ sở ghi nhận', financeReport.value.note],
      ['Tỷ trọng chi phí', 'Tỷ lệ %'],
      ...financeRatios.value.map(item => [item.label, item.value]),
      [],
      ['Hao hụt sơ chế theo nguyên liệu'],
      ['Nguyên liệu', 'Đơn vị', 'Hao hụt dự kiến', 'Hao hụt thực tế', 'Giá trị hao hụt', 'Vượt dự kiến'],
      ...(financeReport.value.prepWastageByItem || []).map(item => [item.itemName, item.baseUnit, item.expectedQuantity, item.quantity, item.value, item.excessValue]),
      [],
      ['Phương thức thanh toán', 'Số giao dịch', 'Số tiền'],
      ...financeReport.value.paymentMethods.map(item => [paymentMethodLabel(item.method), item.paymentCount, item.amount])
    ]
    downloadCsv(financeRows, `bao-cao-tai-chinh-${financeReport.value.fromDate}-${financeReport.value.toDate}.csv`)
    return
  }
  const rows = [
    ['Báo cáo thống kê nhà hàng', `${report.value.fromDate} - ${report.value.toDate}`],
    [],
    ['Tổng quan'],
    ['Doanh thu trước giảm', report.value.summary.grossRevenue ?? report.value.summary.revenue],
    ['Đã giảm giá', report.value.summary.discountAmount],
    ['Doanh thu thực nhận', report.value.summary.revenue],
    ['Hóa đơn đã thanh toán', report.value.summary.invoiceCount],
    ['Giá trị trung bình / hóa đơn', report.value.summary.averageInvoice],
    ['Đơn đã hủy', report.value.summary.cancelledOrders],
    [],
    [trendGranularity.value.title],
    [trendGranularity.value.tableLabel, 'Doanh thu'],
    ...trendData.value.map(item => [item.tableLabel, item.revenue]),
    [],
    ['Doanh thu theo giờ'],
    ['Giờ', 'Số hóa đơn', 'Doanh thu'],
    ...(report.value.hourlyTrend || []).map(item => [formatHour(item.hour), item.invoiceCount, item.revenue]),
    [],
    ['Món bán chạy'],
    ['Tên món', 'Biến thể', 'Số lượng', 'Doanh thu món'],
    ...report.value.topFoods.map(item => [item.foodName, item.variant, item.quantity, item.revenue]),
    [],
    ['Phương thức thanh toán'],
    ['Phương thức', 'Số giao dịch', 'Số tiền'],
    ...report.value.paymentMethods.map(item => [paymentMethodLabel(item.method), item.paymentCount, item.amount]),
    [],
    ['Doanh thu theo khu vực'],
    ['Khu vực', 'Số hóa đơn', 'Doanh thu'],
    ...(report.value.salesByArea || []).map(item => [item.areaName, item.invoiceCount, item.revenue]),
    [],
    ['Doanh thu theo bàn'],
    ['Bàn', 'Mã bàn', 'Khu vực', 'Số hóa đơn', 'Doanh thu'],
    ...(report.value.salesByTable || []).map(item => [item.tableName, item.tableCode, item.areaName, item.invoiceCount, item.revenue])
  ]
  downloadCsv(rows, `bao-cao-doanh-thu-${report.value.fromDate}-${report.value.toDate}.csv`)
}

function printReport() {
  window.print()
}

function downloadCsv(rows, filename) {
  const content = `\uFEFF${rows.map(row => row.map(csvCell).join(',')).join('\r\n')}`
  const url = URL.createObjectURL(new Blob([content], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  URL.revokeObjectURL(url)
}

onMounted(() => {
  loadReport()
  loadProductCatalog()
})
onBeforeUnmount(() => {
  requestVersion++
  requestController?.abort()
  productCatalogController?.abort()
})
</script>

<style scoped>
.reports-page { display: flex; flex-direction: column; gap: 20px; color: #172033; }
.page-heading { display: flex; align-items: flex-end; justify-content: space-between; gap: 20px; }
.eyebrow { color: #0f766e; font-size: .72rem; font-weight: 800; letter-spacing: 0; margin: 0 0 6px; }
.page-heading h1 { font-size: 2rem; font-weight: 750; letter-spacing: 0; margin: 0; }
.page-description, .range-caption { color: #64748b; margin: 6px 0 0; }
.refresh-button, .apply-button, .export-button { min-height: 42px; display: inline-flex; align-items: center; justify-content: center; gap: 8px; font-weight: 650; }
.refresh-button { border-color: #99f6e4; color: #0f766e; background: #fff; }
.refresh-button:hover { border-color: #0f766e; color: #115e59; background: #f0fdfa; }
.apply-button { border-color: #0f766e; background: #0f766e; }
.apply-button:hover { border-color: #115e59; background: #115e59; }
.filter-panel { display: flex; flex-wrap: wrap; align-items: center; gap: 14px; padding: 14px; background: #fff; border: 1px solid #e6e9f0; border-radius: 8px; box-shadow: 0 5px 20px rgba(15,23,42,.035); }
.preset-list { display: flex; flex-wrap: wrap; gap: 6px; }
.preset-button { min-height: 40px; padding: 8px 12px; background: #fff; border: 1px solid transparent; border-radius: 8px; color: #526078; font-weight: 650; }
.preset-button:hover { background: #f0fdfa; color: #115e59; }
.preset-button.selected { background: #ecfdf5; border-color: #99f6e4; color: #115e59; }
.date-filter { display: flex; align-items: flex-end; gap: 10px; margin-left: auto; }
.date-filter label { display: flex; flex-direction: column; gap: 5px; color: #64748b; font-size: .76rem; font-weight: 650; }
.date-filter input { min-height: 40px; padding: 7px 9px; border: 1px solid #d9deea; border-radius: 8px; color: #172033; background: #fff; font-size: .88rem; }
.date-separator { padding-bottom: 10px; color: #94a3b8; }
.date-filter-note { flex: 1 0 100%; color: #64748b; font-size: .75rem; text-align: right; }
.export-button { padding: 8px 12px; border: 1px solid #d9deea; border-radius: 8px; background: #fff; color: #34425a; white-space: nowrap; }
.export-button:hover:not(:disabled) { border-color: #5eead4; color: #115e59; background: #f0fdfa; }
.export-button:disabled { opacity: .55; cursor: not-allowed; }
.report-tabs { display: flex; gap: 6px; padding: 5px; overflow-x: auto; border: 1px solid #e6e9f0; border-radius: 8px; background: #f8fafc; }
.report-tab { display: inline-flex; min-height: 42px; align-items: center; justify-content: center; gap: 8px; padding: 8px 16px; border: 1px solid transparent; border-radius: 8px; color: #64748b; background: transparent; font-weight: 700; white-space: nowrap; cursor: pointer; }
.report-tab:hover { color: #115e59; background: #ecfdf5; }
.report-tab.active { color: #fff; background: #115e59; box-shadow: 0 5px 12px rgba(15,118,110,.15); }
.report-context { display: flex; align-items: center; gap: 14px; padding: 15px 17px; border: 1px solid #dbe7e4; border-left: 4px solid #0f766e; border-radius: 8px; background: #f5fbf9; }
.context-icon { display: grid; width: 40px; height: 40px; flex: 0 0 40px; place-items: center; color: #0f766e; background: #d9f5eb; border-radius: 8px; }
.context-icon svg { width: 21px; height: 21px; }
.context-copy { min-width: 0; }
.context-eyebrow { display: block; margin-bottom: 3px; color: #0f766e; font-size: .68rem; font-weight: 800; letter-spacing: 0; }
.context-copy strong { display: block; color: #172033; font-size: .98rem; line-height: 1.25; }
.context-copy p { margin: 4px 0 0; color: #64748b; font-size: .8rem; line-height: 1.45; }
.context-pills { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; margin-left: auto; }
.context-pill { display: flex; flex-direction: column; gap: 3px; min-width: 136px; padding: 8px 10px; border: 1px solid #dbe7e4; border-radius: 8px; background: #fff; }
.context-pill span { color: #7b8799; font-size: .68rem; font-weight: 650; }
.context-pill strong { color: #273449; font-size: .78rem; white-space: nowrap; }
.context-pill-accent { border-color: #a7e3d0; background: #ecfdf5; }.context-pill-accent strong { color: #0f766e; }
.range-caption { font-size: .86rem; margin-top: -10px; }
.metric-grid { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 14px; }
.product-metrics { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.finance-metrics { grid-template-columns: repeat(4, minmax(0, 1fr)); }
.metric-card, .report-card { background: #fff; border: 1px solid #e6e9f0; border-radius: 8px; box-shadow: 0 5px 20px rgba(15,23,42,.035); }
.metric-card { min-height: 148px; padding: 18px; display: flex; flex-direction: column; align-items: flex-start; }
.metric-topline { width: 100%; display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.metric-label { color: #64748b; font-size: .82rem; font-weight: 650; }
.metric-icon { width: 38px; height: 38px; display: grid; place-items: center; border-radius: 8px; font-size: 1.1rem; font-weight: 800; }
.revenue-icon { color: #0f766e; background: #ecfdf5; }.gross-icon { color: #0369a1; background: #e0f2fe; }.discount-icon { color: #b45309; background: #fef3c7; }.invoice-icon { color: #0369a1; background: #e0f2fe; }.average-icon { color: #047857; background: #d1fae5; }.cancelled-icon { color: #b91c1c; background: #fee2e2; }
.metric-value { max-width: 100%; margin-top: 10px; font-size: 1.2rem; letter-spacing: 0; line-height: 1.3; }
.metric-note { margin-top: auto; padding-top: 10px; font-size: .76rem; font-weight: 650; }
.positive-note { color: #047857; }.negative-note { color: #b91c1c; }.warning-note { color: #b45309; }.neutral-note { color: #718096; font-weight: 500; }
.positive-value, .positive-text { color: #047857; }.negative-value, .negative-text { color: #b91c1c; }.warning-value { color: #b45309; }
.content-grid { display: grid; grid-template-columns: minmax(0, 1.65fr) minmax(270px, .9fr) minmax(270px, .9fr); gap: 16px; align-items: stretch; }
.report-card { padding: 20px; min-width: 0; }
.section-heading { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; margin-bottom: 16px; }
.section-heading h2 { font-size: 1rem; font-weight: 750; margin: 0; letter-spacing: 0; }
.section-heading p { color: #7b8799; font-size: .8rem; margin: 5px 0 0; }
.legend { display: inline-flex; align-items: center; gap: 7px; color: #64748b; font-size: .78rem; white-space: nowrap; }
.legend i { display: inline-block; width: 9px; height: 9px; border-radius: 50%; background: #0f766e; }
.chart-frame { width: 100%; min-height: 220px; }
.trend-chart { display: block; width: 100%; height: 260px; overflow: visible; }
.axis-label { fill: #7b8799; font-size: 11px; }
.empty-chart, .empty-state { display: grid; place-items: center; min-height: 150px; padding: 20px; color: #718096; text-align: center; background: #f8fafc; border: 1px dashed #d9deea; border-radius: 8px; }
.data-details { margin-top: 12px; border-top: 1px solid #edf0f5; padding-top: 11px; }
.data-details summary { width: fit-content; color: #0f766e; font-size: .82rem; font-weight: 650; cursor: pointer; }
.table-wrap { overflow-x: auto; }
.table-wrap table { width: 100%; border-collapse: collapse; font-size: .87rem; }
.table-wrap th { color: #7b8799; font-size: .74rem; font-weight: 700; letter-spacing: 0; text-align: left; white-space: nowrap; }
.table-wrap th, .table-wrap td { padding: 11px 10px; border-bottom: 1px solid #edf0f5; }
.table-wrap tbody tr:last-child td { border-bottom: 0; }
.products-table tbody tr:not(.product-size-expand-row):hover td { background: #f8fcfb; }
.payment-list { display: flex; flex-direction: column; gap: 19px; padding-top: 4px; }
.payment-label { display: flex; justify-content: space-between; gap: 12px; margin-bottom: 8px; font-size: .84rem; }
.payment-label span { color: #475569; font-weight: 600; }.payment-label strong { color: #172033; white-space: nowrap; }
.payment-track { height: 8px; overflow: hidden; border-radius: 99px; background: #edf0f6; }
.payment-track span { display: block; height: 100%; border-radius: inherit; background: #0f766e; }
.payment-row small { display: block; margin-top: 6px; color: #7b8799; font-size: .74rem; }
.period-card { display: flex; flex-direction: column; }
.summary-kicker { flex: 0 0 auto; padding: 5px 8px; color: #0f766e; background: #ecfdf5; border: 1px solid #a7e3d0; border-radius: 7px; font-size: .72rem; font-weight: 750; }
.period-stat { display: flex; flex-direction: column; gap: 5px; padding: 14px 0; border-top: 1px solid #edf0f5; }
.period-stat span { color: #64748b; font-size: .78rem; font-weight: 650; }
.period-stat strong { color: #172033; font-size: 1.06rem; line-height: 1.25; }
.period-stat small { color: #7b8799; font-size: .76rem; }
.period-stat-primary { padding-top: 2px; border-top: 0; }
.period-stat-primary strong { color: #115e59; font-size: 1.35rem; }
.foods-card { padding: 20px; }
.foods-card .section-heading { margin-bottom: 4px; }
.product-scope-select { display: grid; gap: 5px; min-width: 0; color: #64748b; font-size: .74rem; font-weight: 700; }
.product-scope-select select { width: 100%; min-height: 42px; padding: 0 10px; border: 1px solid #cbd5e1; border-radius: 8px; color: #172033; background: #fff; font: inherit; font-size: .84rem; }
.product-scope-select select:disabled { color: #94a3b8; background: #f8fafc; cursor: not-allowed; }
.product-catalog-hint { color: #718096; font-size: .73rem; }
.clear-product-filters { flex: 0 0 auto; padding: 5px 9px; color: #0f766e; background: #f0fdfa; border: 1px solid #99f6e4; border-radius: 7px; font-size: .74rem; font-weight: 700; cursor: pointer; }
.clear-product-filters:hover { background: #ccfbf1; }
.product-metrics { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.product-metrics .metric-card { min-width: 0; min-height: 142px; }
.product-kpi-detail { display: block; margin-top: 3px; color: #64748b; font-size: .76rem; }
.product-kpi-detail span { margin-left: 5px; font-weight: 700; }
.quantity-unit { color: #7b8799; font-size: .72rem; }
.product-insights-grid { display: grid; grid-template-columns: minmax(0, 1.2fr) minmax(320px, .8fr); gap: 16px; }
.product-chart-card { min-width: 0; padding: 20px; border-color: #dfe8e8; }
.product-chart-heading { align-items: center; }
.product-chart-actions { display: flex; align-items: center; gap: 8px; }
.top-limit-select { display: inline-flex; min-height: 38px; padding: 0 9px; border: 1px solid #d9deea; border-radius: 8px; background: #fff; }
.top-limit-select select { min-width: 76px; border: 0; outline: 0; color: #273449; background: transparent; font: inherit; font-size: .78rem; font-weight: 650; cursor: pointer; }
.segmented-control { display: inline-flex; flex: 0 0 auto; padding: 3px; gap: 2px; background: #f1f5f9; border-radius: 9px; }
.segmented-control button { min-height: 32px; padding: 5px 10px; color: #64748b; background: transparent; border: 0; border-radius: 7px; font: inherit; font-size: .76rem; font-weight: 700; cursor: pointer; }
.segmented-control button.selected { color: #0f766e; background: #fff; box-shadow: 0 1px 3px #0f172a1a; }
.rank-bars, .category-share-list { display: flex; flex-direction: column; gap: 0; }
.rank-food-row { min-width: 0; padding: 14px 4px 15px; border-bottom: 1px solid #e8eef0; border-radius: 9px; transition: background-color .15s ease; }
.rank-food-row:hover { background: #f5fbfa; }
.rank-food-row:last-child { border-bottom: 0; }
.rank-bar-heading { display: flex; align-items: center; gap: 10px; min-width: 0; margin-bottom: 8px; }
.rank-bar-name { flex: 1 1 auto; min-width: 0; overflow: hidden; color: #172033; font-size: .9rem; font-weight: 750; text-overflow: ellipsis; white-space: nowrap; }
.rank-bar-name small { display: block; overflow: hidden; margin-top: 3px; color: #718096; font-size: .74rem; font-weight: 500; text-overflow: ellipsis; }
.rank-food-total { display: flex; flex-direction: column; align-items: flex-end; flex: 0 0 auto; gap: 2px; text-align: right; }
.rank-food-total strong { color: #0f766e; font-size: .86rem; white-space: nowrap; }
.rank-food-total small { color: #64748b; font-size: .72rem; white-space: nowrap; }
.product-bar-track { height: 8px; margin-left: 40px; }
.product-bar-fill { display: block; height: 100%; border-radius: inherit; background: linear-gradient(90deg, #0f766e, #2dd4bf); transition: width .2s ease; }
.category-share-row small { display: block; margin-top: 5px; color: #7b8799; font-size: .73rem; }
.category-share-fill { display: block; height: 100%; border-radius: inherit; background: #0f766e; transition: width .2s ease; }
.rank-bar-name, .product-identity, .product-mobile-identity { cursor: pointer; }
.rank-bar-name:hover, .product-identity .food-name:hover, .product-mobile-identity:hover { color: #0f766e; }
.rank-bar-name:focus-visible, .product-identity .food-name:focus-visible, .product-mobile-identity:focus-visible { border-radius: 6px; outline: 3px solid #14b8a6; outline-offset: 3px; }
.rank-variant-list { display: grid; gap: 0; margin-top: 8px; padding: 3px 10px; background: #fff; border: 1px solid #dfeae8; border-radius: 8px; }
.rank-variant-row { display: grid; grid-template-columns: minmax(0, 1fr) auto auto; gap: 12px; align-items: center; padding: 8px 0; border-bottom: 1px solid #eef3f2; }
.rank-variant-row:last-child { border-bottom: 0; }
.rank-variant-row > span { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.rank-variant-row > strong { color: #334155; font-size: .74rem; white-space: nowrap; }
.rank-variant-row > b { color: #0f766e; font-size: .74rem; text-align: right; white-space: nowrap; }
.category-compare-list { display: grid; gap: 10px; }
.category-compare-row { padding: 12px; background: #f8fafc; border: 1px solid #e8edf4; border-radius: 9px; }
.category-compare-row > header, .category-compare-row > footer { display: flex; align-items: center; gap: 8px; }
.category-compare-row > header > strong { flex: 1 1 auto; min-width: 0; overflow: hidden; color: #273449; font-size: .84rem; text-overflow: ellipsis; white-space: nowrap; }
.category-compare-row > header > span:last-child { flex: 0 0 auto; font-size: .74rem; font-weight: 700; }
.category-compare-values { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; margin: 10px 0; }
.category-compare-values > div { min-width: 0; padding: 9px; background: #fff; border: 1px solid #edf1f6; border-radius: 7px; }
.category-compare-values span, .category-compare-values strong, .category-compare-values small { display: block; }
.category-compare-values span, .category-compare-values small { color: #718096; font-size: .7rem; }
.category-compare-values strong { overflow: hidden; margin: 3px 0; color: #172033; font-size: .84rem; text-overflow: ellipsis; white-space: nowrap; }
.category-compare-row > footer { justify-content: space-between; flex-wrap: wrap; color: #64748b; font-size: .72rem; }
.chart-footnote { margin: 14px 0 0; color: #7b8799; font-size: .72rem; }
.product-compare-stat { padding: 13px 0; border-bottom: 1px solid #e2e8f0; }
.product-compare-stat > span { color: #475569; font-size: .79rem; font-weight: 700; }
.product-compare-values { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-top: 9px; }
.product-compare-values > div { min-width: 0; padding: 11px; border-radius: 8px; background: #f8fafc; }
.product-compare-values small, .product-compare-values strong { display: block; }
.product-compare-values small { margin-bottom: 4px; color: #64748b; font-size: .7rem; }
.product-compare-values strong { color: #172033; font-size: 1rem; }
.product-compare-stat p { margin: 8px 0 0; font-size: .78rem; font-weight: 700; }
.product-toolbar { display: grid; grid-template-columns: minmax(220px, 1fr) minmax(210px, 270px); gap: 10px; margin: 14px 0 12px; }
.toolbar-search, .toolbar-select { display: flex; align-items: center; min-height: 40px; border: 1px solid #d9deea; border-radius: 8px; background: #fff; }
.toolbar-search { gap: 8px; padding: 0 10px; color: #64748b; }
.toolbar-search svg { width: 17px; height: 17px; flex: 0 0 auto; }
.toolbar-search input, .toolbar-select select { width: 100%; min-width: 0; min-height: 38px; border: 0; outline: 0; color: #273449; background: transparent; font: inherit; font-size: .84rem; }
.toolbar-select { padding: 0 9px; }
.toolbar-select select { cursor: pointer; }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
.rank-column { width: 44px; }
.rank-badge { display: inline-grid; place-items: center; width: 26px; height: 26px; border-radius: 8px; color: #0f766e; background: #ecfdf5; font-size: .76rem; font-weight: 800; }
.food-name { font-weight: 650; color: #273449; }.food-name small { display: block; margin-top: 3px; color: #7b8799; font-size: .76rem; font-weight: 500; }.food-revenue { font-weight: 700; color: #172033; white-space: nowrap; }
.location-grid, .finance-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.report-note-card { padding: 14px 17px; color: #526078; background: #f8fafc; border-left: 4px solid #0f766e; font-size: .86rem; line-height: 1.55; }
.report-note-card strong { color: #273449; }
.data-warning, .data-ok { display: inline-flex; padding: 4px 7px; border-radius: 6px; font-size: .72rem; font-weight: 700; white-space: nowrap; }
.data-warning { color: #92400e; background: #fef3c7; }.data-ok { color: #047857; background: #d1fae5; }
.products-table { min-width: 830px; }
.product-row-expanded > td { background: #fbfefd; }
.product-size-expand-row > td { padding: 0 10px 10px 54px; background: #f8fafc; border-bottom: 1px solid #edf0f5; }
.product-size-list { display: grid; gap: 7px; }
.product-size-grid { display: grid; grid-template-columns: minmax(130px, 1.3fr) repeat(3, minmax(115px, 1fr)); gap: 12px; align-items: center; padding: 9px 10px; background: #fff; border: 1px solid #e8edf4; border-radius: 7px; }
.product-size-grid > span { min-width: 0; overflow-wrap: anywhere; }
.product-size-grid > span:not(:first-child) strong, .product-size-grid > span:not(:first-child) small { display: block; }
.product-size-grid > span:not(:first-child) strong { color: #273449; font-size: .74rem; }
.product-size-grid > span:not(:first-child) small { margin-top: 3px; color: #718096; font-size: .7rem; }
.product-size-grid.product-size-heading { padding-top: 4px; color: #64748b; background: transparent; border: 0; font-size: .7rem; font-weight: 700; }
.product-size-mobile { padding: 10px; background: #fff; border: 1px solid #e8edf4; border-radius: 8px; }
.product-size-mobile > strong { color: #273449; font-size: .8rem; }
.product-size-mobile footer { display: flex; justify-content: space-between; gap: 8px; margin-top: 8px; font-size: .72rem; font-weight: 700; }
.product-identity { display: flex; align-items: flex-start; gap: 9px; }
.product-identity .food-name { flex: 1 1 auto; min-width: 0; }
.product-mobile-identity { flex: 1; min-width: 0; }
.product-period-cell strong, .product-period-cell small, .product-change-cell span, .product-change-cell small { display: block; }
.product-period-cell strong { color: #172033; font-size: .84rem; }
.product-period-cell small, .product-change-cell small { margin-top: 4px; color: #64748b; font-size: .76rem; }
.product-change-cell span, .product-change-cell small { font-weight: 700; }
.product-change-cell .product-trend { display: inline-flex; margin-top: 7px; }
.product-mobile-list { display: none; }
.product-mobile-card { padding: 14px; border: 1px solid #e2e8f0; border-radius: 10px; background: #fff; }
.product-mobile-card header { display: flex; align-items: flex-start; gap: 9px; }
.product-mobile-card header > div { flex: 1; min-width: 0; }
.product-mobile-card header strong, .product-mobile-card header small { display: block; }
.product-mobile-card header strong { color: #172033; font-size: .86rem; }
.product-mobile-card header small { margin-top: 3px; color: #64748b; font-size: .73rem; }
.product-share { color: #0f766e; font-size: .7rem; font-weight: 800; white-space: nowrap; }
.product-mobile-periods { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; margin-top: 12px; }
.product-mobile-periods > div { padding: 10px; border-radius: 8px; background: #f8fafc; }
.product-mobile-periods span, .product-mobile-periods strong, .product-mobile-periods small { display: block; }
.product-mobile-periods span { color: #64748b; font-size: .7rem; }.product-mobile-periods strong { margin-top: 4px; color: #172033; font-size: .81rem; }.product-mobile-periods small { margin-top: 3px; color: #64748b; font-size: .72rem; }
.product-mobile-card footer { display: flex; flex-wrap: wrap; gap: 5px 12px; padding-top: 10px; font-size: .72rem; font-weight: 700; }
.detail-filter-panel { margin: 12px 0 16px; padding: 14px; border: 1px solid #e2e8f0; border-radius: 9px; background: #f8fafc; }
.detail-filter-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.detail-filter-heading strong, .detail-filter-heading small { display: block; }
.detail-filter-heading strong { color: #273449; font-size: .84rem; }
.detail-filter-heading small { margin-top: 3px; color: #718096; font-size: .74rem; }
.detail-filter-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; margin-top: 12px; }
.detail-filter-panel .product-toolbar { margin: 12px 0 0; }
.detail-filter-panel .product-catalog-hint { margin: 9px 0 0; }
.product-trend { display: inline-flex; align-items: center; padding: 4px 7px; border-radius: 999px; font-size: .72rem; font-weight: 700; white-space: nowrap; }
.trend-up { color: #047857; background: #d1fae5; }.trend-down { color: #b45309; background: #fef3c7; }.trend-new { color: #1d4ed8; background: #dbeafe; }.trend-flat { color: #64748b; background: #f1f5f9; }
.ratio-list { display: flex; flex-direction: column; gap: 22px; }
.ratio-row { display: flex; flex-direction: column; gap: 8px; }
.ratio-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; color: #475569; font-size: .84rem; line-height: 1.35; }
.ratio-heading strong { flex: 0 0 auto; font-size: .95rem; }
.ratio-text-good { color: #047857; }.ratio-text-warn { color: #b45309; }.ratio-text-bad { color: #b91c1c; }
.ratio-text-neutral { color: #64748b; }
.ratio-track { height: 9px; overflow: hidden; border-radius: 999px; background: #edf0f6; }
.ratio-track span { display: block; height: 100%; border-radius: inherit; transition: width .2s ease; }
.ratio-good { background: #10b981; }.ratio-warn { background: #f59e0b; }.ratio-bad { background: #ef4444; }
.ratio-neutral { background: #0284c7; }
.ratio-row small { color: #7b8799; font-size: .75rem; line-height: 1.4; }
.finance-metrics .metric-card { min-height: 132px; }
.report-error { margin: 0; padding: 12px 14px; color: #991b1b; background: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; }
.report-unavailable { display: grid; place-items: center; min-height: 170px; padding: 20px; color: #64748b; background: #fff; border: 1px dashed #d9deea; border-radius: 8px; text-align: center; }
.report-loading { display: grid; place-items: center; min-height: 220px; color: #64748b; background: #fff; border: 1px solid #e6e9f0; border-radius: 8px; }
button:focus-visible, input:focus-visible, select:focus-visible, summary:focus-visible { outline: 3px solid #14b8a6; outline-offset: 2px; }
@media (max-width: 760px) { .product-chart-actions { width: 100%; justify-content: space-between; } }
@media (max-width: 1280px) { .metric-grid { grid-template-columns: repeat(3, minmax(0, 1fr)); }.product-metrics { grid-template-columns: repeat(3, minmax(0, 1fr)); }.content-grid { grid-template-columns: minmax(0, 1.5fr) minmax(260px, .9fr); }.period-card, .payment-card { grid-column: 1 / -1; } }
@media (max-width: 1100px) { .filter-panel { flex-wrap: wrap; }.date-filter { margin-left: 0; }.metric-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.product-metrics { grid-template-columns: repeat(2, minmax(0, 1fr)); }.content-grid { grid-template-columns: 1fr; }.hourly-card { grid-column: auto; }.location-grid, .finance-grid { grid-template-columns: 1fr; }.context-pills { margin-left: 0; } }
@media (max-width: 760px) { .page-heading { align-items: flex-start; }.page-heading .refresh-button { flex-shrink: 0; }.filter-panel { align-items: flex-start; }.preset-list { width: 100%; }.date-filter { flex-wrap: wrap; }.date-filter label { flex: 1; min-width: 125px; }.date-filter input { width: 100%; }.date-separator { display: none; }.date-filter-note { text-align: left; }.export-button { margin-left: auto; }.content-grid { grid-template-columns: 1fr; }.product-insights-grid { grid-template-columns: 1fr; }.product-chart-heading { align-items: flex-start; flex-direction: column; }.product-table-wrap { display: none; }.product-mobile-list { display: grid; gap: 10px; }.trend-chart { height: 230px; }.metric-card { min-height: 140px; padding: 14px; }.report-context { align-items: flex-start; flex-wrap: wrap; }.context-pills { width: calc(100% - 54px); justify-content: flex-start; }.context-pill { min-width: 0; flex: 1 1 150px; }.product-toolbar { grid-template-columns: 1fr; } }
@media (max-width: 420px) { .reports-page { gap: 14px; }.page-heading { gap: 8px; }.page-heading h1 { font-size: 1.35rem; }.page-description { font-size: .86rem; }.page-heading .refresh-button { min-height: 38px; padding: 6px 9px; font-size: .82rem; }.metric-grid { gap: 9px; }.metric-label { font-size: .74rem; }.metric-icon { width: 32px; height: 32px; }.metric-value { font-size: 1.14rem; }.metric-note { font-size: .7rem; }.report-card { padding: 15px; } }
@media (max-width: 420px) { .report-tab { flex: 1; padding: 8px; font-size: .85rem; }.report-tab svg { display: none; } }
@media (max-width: 1100px) { .detail-filter-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 760px) { .detail-filter-grid { grid-template-columns: 1fr; }.detail-filter-heading { align-items: flex-start; flex-direction: column; } }
@media (max-width: 760px) { .rank-bar-heading { gap: 8px; }.rank-bar-name { font-size: .84rem; }.rank-food-total strong { font-size: .8rem; }.product-bar-track { margin-left: 34px; }.rank-variant-row { gap: 8px; }.product-size-expand-row > td { padding-left: 44px; } }
@media (max-width: 420px) { .rank-variant-row { grid-template-columns: minmax(0, 1fr) auto; }.rank-variant-row > span { grid-column: 1 / -1; }.product-size-expand-row > td { padding-right: 6px; padding-left: 34px; }.product-mobile-card header { gap: 6px; } }
@media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; animation-duration: .01ms !important; animation-iteration-count: 1 !important; } }
@media print {
  :global(body *) { visibility: hidden !important; }
  :global(.reports-page), :global(.reports-page *) { visibility: visible !important; }
  :global(.reports-page) { position: absolute; inset: 0; width: 100%; padding: 0; background: #fff !important; }
  .page-heading { margin-bottom: 12px; }
  .refresh-button, .filter-panel, .report-tabs, .report-error, .report-loading, .report-context .context-icon, .export-button, .module-action, button { display: none !important; }
  .report-card, .report-module, .operations-heading, .operation-block { box-shadow: none !important; break-inside: avoid; }
  .table-wrap, .table-scroll { overflow: visible !important; }
  table { page-break-inside: auto; } tr { page-break-inside: avoid; page-break-after: auto; } thead { display: table-header-group; }
}
</style>

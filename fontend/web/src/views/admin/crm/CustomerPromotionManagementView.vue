<template>
  <div class="crm-promotion-container">
    <!-- 1. HEADER SECTION (Deep Teal & Luxury Gold Gradient) -->
    <div class="page-header d-flex flex-wrap justify-content-between align-items-center gap-3 mb-4">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge bg-teal-subtle text-teal fw-bold px-2 py-1 rounded-pill">
            <span class="pulse-dot"></span> CRM & MARKETING LOYALTY
          </span>
          <span class="badge bg-gold-subtle text-gold fw-bold px-2 py-1 rounded-pill">
            DOLA RESTAURANT
          </span>
        </div>
        <h3 class="fw-black mb-1 page-title">Trung Tâm Khách Hàng & Khuyến Mãi</h3>
        <p class="text-muted mb-0 small">
          Quản trị tệp khách hàng thân thiết, chính sách tích điểm đổi thưởng & phát hành voucher ưu đãi
        </p>
      </div>

      <!-- Segmented Tab Switcher -->
      <div class="segmented-control p-1 bg-light rounded-4 shadow-sm border d-flex gap-1">
        <button
          type="button"
          class="seg-btn"
          :class="{ active: currentTab === 'customers' }"
          @click="currentTab = 'customers'"
        >
          <span class="seg-icon">👥</span>
          <span class="fw-bold">Khách Hàng & Thành Viên</span>
          <span class="seg-badge">{{ crmStats.totalCustomers || 0 }}</span>
        </button>

        <button
          type="button"
          class="seg-btn"
          :class="{ active: currentTab === 'promotions' }"
          @click="currentTab = 'promotions'"
        >
          <span class="seg-icon">🎟️</span>
          <span class="fw-bold">Khuyến Mãi & Voucher</span>
          <span class="seg-badge bg-emerald-badge">{{ crmStats.activeVouchers || 0 }}</span>
        </button>
      </div>
    </div>

    <!-- Alert thông báo -->
    <div v-if="successMsg" class="alert alert-success alert-dismissible fade show shadow-sm py-2 px-3 small rounded-3 mb-4" role="alert">
      <strong>Thành công!</strong> {{ successMsg }}
      <button type="button" class="btn-close py-2" @click="successMsg = ''"></button>
    </div>
    <div v-if="errorMsg" class="alert alert-danger alert-dismissible fade show shadow-sm py-2 px-3 small rounded-3 mb-4" role="alert">
      <strong>Lỗi!</strong> {{ errorMsg }}
      <button type="button" class="btn-close py-2" @click="errorMsg = ''"></button>
    </div>

    <!-- ================================================================= -->
    <!-- TAB 1: QUẢN LÝ KHÁCH HÀNG & THÀNH VIÊN (CRM & LOYALTY)             -->
    <!-- ================================================================= -->
    <div v-if="currentTab === 'customers'" class="tab-pane-content">
      <!-- KPI STRIP KHÁCH HÀNG -->
      <div class="row g-3 mb-4">
        <div class="col-sm-6 col-xl-3">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-teal-subtle text-teal">
                👥
              </div>
              <div>
                <div class="kpi-label">Tổng khách hàng</div>
                <div class="kpi-value text-teal">{{ crmStats.totalCustomers || 0 }} <small class="fs-6 text-muted">hội viên</small></div>
                <div class="kpi-sub text-success">Đang hoạt động: {{ crmStats.activeCustomers || 0 }}</div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-sm-6 col-xl-3">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-gold-subtle text-gold">
                👑
              </div>
              <div>
                <div class="kpi-label">Hội viên VIP (Gold/Diamond)</div>
                <div class="kpi-value text-gold">{{ crmStats.vipCustomersCount || 0 }} <small class="fs-6 text-muted">khách</small></div>
                <div class="kpi-sub text-muted">Chi tiêu từ 5.000.000 đ trở lên</div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-sm-6 col-xl-3">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-indigo-subtle text-indigo">
                💎
              </div>
              <div>
                <div class="kpi-label">Điểm thưởng lưu hành</div>
                <div class="kpi-value text-indigo">{{ formatNumber(crmStats.totalPointsInCirculation || 0) }} <small class="fs-6 text-muted">điểm</small></div>
                <div class="kpi-sub text-muted">Tương đương {{ formatCurrency((crmStats.totalPointsInCirculation || 0) * 1000) }}</div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-sm-6 col-xl-3">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-emerald-subtle text-emerald">
                💰
              </div>
              <div>
                <div class="kpi-label">Doanh thu từ thành viên</div>
                <div class="kpi-value text-emerald">{{ formatCurrency(crmStats.totalCustomerRevenue || 0) }}</div>
                <div class="kpi-sub text-muted">Thống kê từ hóa đơn tích điểm</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- FILTER & ACTION BAR (UI/UX Pro Quick Chips & Search) -->
      <div class="card card-body shadow-sm border-0 rounded-4 p-3 mb-4 bg-white">
        <div class="row g-2 align-items-center mb-3">
          <!-- Search input -->
          <div class="col-lg-5 col-md-6">
            <div class="input-group">
              <span class="input-group-text bg-light border-end-0 text-muted">🔍</span>
              <input
                v-model="customerSearch"
                class="form-control border-start-0 ps-0"
                placeholder="Tìm nhanh theo tên khách, SĐT, email..."
                @input="debounceFetchCustomers"
              />
              <button v-if="customerSearch" class="btn btn-outline-secondary border-start-0" type="button" @click="customerSearch = ''; fetchCustomers()">✕</button>
            </div>
          </div>

          <!-- Quick status select -->
          <div class="col-sm-6 col-lg-2">
            <select v-model="customerStatusFilter" class="form-select bg-light" @change="fetchCustomers">
              <option value="all">Tất cả trạng thái</option>
              <option value="active">🟢 Đang hoạt động</option>
              <option value="inactive">🔴 Đã tạm khóa</option>
            </select>
          </div>

          <!-- Sort select -->
          <div class="col-sm-6 col-lg-2">
            <select v-model="customerSortBy" class="form-select bg-light" @change="fetchCustomers">
              <option value="spent_desc">Chi tiêu cao nhất</option>
              <option value="spent_asc">Chi tiêu thấp nhất</option>
              <option value="points_desc">Điểm thưởng nhiều nhất</option>
              <option value="visits_desc">Lượt ghé nhiều nhất</option>
              <option value="recent">Mới tạo gần đây</option>
            </select>
          </div>

          <!-- Add customer button -->
          <div class="col-lg-3 col-md-6 text-end">
            <button class="btn btn-primary fw-bold px-3 w-100 rounded-3 shadow-xs d-flex align-items-center justify-content-center gap-2" @click="openCreateCustomerModal">
              <span>➕</span>
              <span>Thêm Hội Viên Mới</span>
            </button>
          </div>
        </div>

        <!-- Quick Filter Rank Chips (1-Click Filtering) -->
        <div class="d-flex flex-wrap align-items-center gap-1 pt-2 border-top">
          <span class="text-muted small fw-bold me-2">Hạng thành viên:</span>
          <button
            type="button"
            class="filter-chip"
            :class="{ active: customerRankFilter === 'all' }"
            @click="customerRankFilter = 'all'; fetchCustomers()"
          >
            Tất cả
          </button>
          <button
            type="button"
            class="filter-chip chip-diamond"
            :class="{ active: customerRankFilter === 'Diamond' }"
            @click="customerRankFilter = 'Diamond'; fetchCustomers()"
          >
            💎 Kim Cương (&ge;15tr)
          </button>
          <button
            type="button"
            class="filter-chip chip-gold"
            :class="{ active: customerRankFilter === 'Gold' }"
            @click="customerRankFilter = 'Gold'; fetchCustomers()"
          >
            🥇 Vàng (5tr - 15tr)
          </button>
          <button
            type="button"
            class="filter-chip chip-silver"
            :class="{ active: customerRankFilter === 'Silver' }"
            @click="customerRankFilter = 'Silver'; fetchCustomers()"
          >
            🥈 Bạc (1tr - 5tr)
          </button>
          <button
            type="button"
            class="filter-chip chip-bronze"
            :class="{ active: customerRankFilter === 'Bronze' }"
            @click="customerRankFilter = 'Bronze'; fetchCustomers()"
          >
            🥉 Đồng (&lt;1tr)
          </button>
        </div>
      </div>

      <!-- CUSTOMERS TABLE (Streamlined, Intuitive & High Contrast) -->
      <div class="card shadow-sm border-0 rounded-4 overflow-hidden bg-white mb-4">
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th class="ps-3 py-3" style="width: 60px;">ID</th>
                <th class="py-3">Khách Hàng / Liên Hệ</th>
                <th class="py-3 text-center" style="width: 140px;">Hạng Thẻ</th>
                <th class="py-3 text-end" style="width: 160px;">Điểm Thưởng</th>
                <th class="py-3 text-end" style="width: 180px;">Chi Tiêu & Tần Suất</th>
                <th class="py-3 text-center" style="width: 130px;">Trạng Thái</th>
                <th class="py-3 text-end pe-3" style="width: 260px;">Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="loadingCustomers">
                <td colspan="7" class="text-center py-5 text-muted">
                  <div class="spinner-border spinner-border-sm text-teal me-2"></div>
                  <span>Đang tải danh sách khách hàng...</span>
                </td>
              </tr>
              <tr v-else-if="customers.length === 0">
                <td colspan="7" class="text-center py-5">
                  <div class="text-muted fs-1 mb-2">📭</div>
                  <h6 class="fw-bold mb-1">Không tìm thấy khách hàng nào</h6>
                  <p class="text-muted small mb-0">Thử thay đổi từ khóa tìm kiếm hoặc chọn "Tất cả" hạng thẻ</p>
                </td>
              </tr>
              <tr v-for="c in customers" :key="c.customerId" class="customer-row">
                <!-- ID -->
                <td class="ps-3 text-muted small font-monospace">
                  <span class="badge bg-light text-secondary border font-monospace">#{{ c.customerId }}</span>
                </td>

                <!-- Khách hàng -->
                <td>
                  <div class="d-flex align-items-center gap-2">
                    <div class="avatar-circle shadow-xs" :class="getAvatarClass(c.rank)">
                      {{ getInitials(c.fullName) }}
                    </div>
                    <div>
                      <div class="fw-bold text-dark d-flex align-items-center gap-1">
                        <span>{{ c.fullName }}</span>
                        <span v-if="c.totalSpent >= 5000000" class="badge bg-gold text-dark py-0 px-1 rounded-pill" style="font-size: 0.6rem;">VIP</span>
                      </div>
                      <div class="text-muted small font-monospace">
                        📞 {{ c.phone }}
                        <span v-if="c.email" class="text-muted ms-1">· ✉️ {{ c.email }}</span>
                      </div>
                    </div>
                  </div>
                </td>

                <!-- Hạng thẻ -->
                <td class="text-center">
                  <span class="rank-pill shadow-xs" :class="'rank-' + c.rank.toLowerCase()">
                    <span class="rank-icon">{{ getRankIcon(c.rank) }}</span>
                    <span>{{ c.rankName }}</span>
                  </span>
                </td>

                <!-- Điểm thưởng: Interactive Chip bấm mở lịch sử -->
                <td class="text-end">
                  <button
                    type="button"
                    class="points-chip-interactive"
                    title="Bấm để xem lịch sử tích & tiêu điểm"
                    @click="openPointsHistoryModal(c)"
                  >
                    <span class="points-icon">💎</span>
                    <span class="points-val">{{ formatNumber(c.pointsBalance) }}</span>
                    <span class="points-unit">đ</span>
                    <span class="points-arrow">↗</span>
                  </button>
                </td>

                <!-- Chi tiêu & Tần suất -->
                <td class="text-end">
                  <div class="fw-bold text-dark font-monospace fs-6">
                    {{ formatCurrency(c.totalSpent) }}
                  </div>
                  <div class="small text-muted">
                    <span class="badge bg-light text-muted border py-0 px-1">{{ c.visitCount }} lần ghé</span>
                  </div>
                </td>

                <!-- Trạng thái -->
                <td class="text-center">
                  <span v-if="c.isActive" class="status-badge status-active">
                    <span class="status-dot"></span> Hoạt động
                  </span>
                  <span v-else class="status-badge status-inactive">
                    <span class="status-dot"></span> Đã khóa
                  </span>
                </td>

                <!-- Thao tác Pro: Rõ ràng, trực quan, có nhãn chữ -->
                <td class="text-end pe-3">
                  <div class="d-inline-flex align-items-center gap-1">
                    <!-- Nút 1: Xem lịch sử điểm (Primary Action) -->
                    <button
                      type="button"
                      class="btn-action-pill btn-action-teal"
                      title="Xem chi tiết lịch sử điểm thưởng"
                      @click="openPointsHistoryModal(c)"
                    >
                      <span class="action-icon">📜</span>
                      <span class="action-text">Lịch sử</span>
                    </button>

                    <!-- Nút 2: Điều chỉnh điểm -->
                    <button
                      type="button"
                      class="btn-action-pill btn-action-indigo"
                      title="Cộng / Trừ điểm thưởng"
                      @click="openAdjustPointsModal(c)"
                    >
                      <span class="action-icon">💎</span>
                      <span class="action-text">± Điểm</span>
                    </button>

                    <!-- Nút 3: Xem chi tiết toàn diện (Hóa đơn & Voucher) -->
                    <button
                      type="button"
                      class="btn-action-icon"
                      title="Xem hồ sơ đầy đủ (Hóa đơn, Voucher, Chi tiết)"
                      @click="viewCustomerDetail(c.customerId)"
                    >
                      👁️
                    </button>

                    <!-- Nút 4: Chỉnh sửa thông tin -->
                    <button
                      type="button"
                      class="btn-action-icon"
                      title="Chỉnh sửa thông tin khách"
                      @click="openEditCustomerModal(c)"
                    >
                      ✏️
                    </button>

                    <!-- Nút 5: Khóa / Mở khóa -->
                    <button
                      type="button"
                      class="btn-action-icon"
                      :class="c.isActive ? 'btn-action-lock' : 'btn-action-unlock'"
                      :title="c.isActive ? 'Tạm khóa tài khoản' : 'Kích hoạt lại'"
                      @click="toggleCustomerStatus(c)"
                    >
                      {{ c.isActive ? '🔒' : '🔓' }}
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="customerTotalPages > 1" class="d-flex justify-content-between align-items-center p-3 border-top bg-light">
          <span class="text-muted small">
            Hiển thị trang <strong>{{ customerPage }}</strong> / <strong>{{ customerTotalPages }}</strong> (Tổng {{ customerTotalItems }} khách)
          </span>
          <div class="btn-group btn-group-sm">
            <button class="btn btn-outline-secondary" :disabled="customerPage <= 1" @click="customerPage--; fetchCustomers()">
              &laquo; Trước
            </button>
            <button class="btn btn-outline-secondary" :disabled="customerPage >= customerTotalPages" @click="customerPage++; fetchCustomers()">
              Sau &raquo;
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ================================================================= -->
    <!-- TAB 2: QUẢN LÝ KHUYẾN MÃI & VOUCHER (PROMOTIONS)                  -->
    <!-- ================================================================= -->
    <div v-if="currentTab === 'promotions'" class="tab-pane-content">
      <!-- KPI STRIP VOUCHER -->
      <div class="row g-3 mb-4">
        <div class="col-sm-6 col-xl-4">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-emerald-subtle text-emerald">
                🎟️
              </div>
              <div>
                <div class="kpi-label">Chương trình đang chạy</div>
                <div class="kpi-value text-emerald">{{ crmStats.activeVouchers || 0 }} <small class="fs-6 text-muted">chiến dịch</small></div>
                <div class="kpi-sub text-success">Sẵn sàng áp dụng khi thanh toán quầy/app</div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-sm-6 col-xl-4">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-indigo-subtle text-indigo">
                📈
              </div>
              <div>
                <div class="kpi-label">Tổng lượt voucher đã dùng</div>
                <div class="kpi-value text-indigo">{{ formatNumber(crmStats.totalVouchersUsed || 0) }} <small class="fs-6 text-muted">lượt</small></div>
                <div class="kpi-sub text-muted">Ghi nhận từ các lượt thanh toán thành công</div>
              </div>
            </div>
          </div>
        </div>

        <div class="col-sm-6 col-xl-4">
          <div class="kpi-card shadow-sm border-0 rounded-4 p-3 bg-white h-100">
            <div class="d-flex align-items-center gap-3">
              <div class="kpi-icon-box bg-gold-subtle text-gold">
                🎁
              </div>
              <div>
                <div class="kpi-label">Tổng ưu đãi đã tài trợ</div>
                <div class="kpi-value text-gold">{{ formatCurrency(crmStats.totalDiscountFund || 0) }}</div>
                <div class="kpi-sub text-muted">Ngân sách chiết khấu chăm sóc khách hàng</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- FILTER & ACTION BAR VOUCHER -->
      <div class="card card-body shadow-sm border-0 rounded-4 p-3 mb-4 bg-white">
        <div class="row g-2 align-items-center">
          <div class="col-lg-4 col-md-6">
            <div class="input-group">
              <span class="input-group-text bg-light border-end-0 text-muted">🔍</span>
              <input
                v-model="promoSearch"
                class="form-control border-start-0 ps-0"
                placeholder="Tìm theo mã voucher hoặc tên..."
                @input="debounceFetchPromotions"
              />
              <button v-if="promoSearch" class="btn btn-outline-secondary border-start-0" type="button" @click="promoSearch = ''; fetchPromotions()">✕</button>
            </div>
          </div>

          <div class="col-sm-6 col-lg-3">
            <select v-model="promoTypeFilter" class="form-select bg-light" @change="fetchPromotions">
              <option value="all">Tất cả hình thức</option>
              <option value="percent">Phần trăm (%)</option>
              <option value="fixed">Số tiền cố định (đ)</option>
            </select>
          </div>

          <div class="col-sm-6 col-lg-3">
            <select v-model="promoStatusFilter" class="form-select bg-light" @change="fetchPromotions">
              <option value="all">Tất cả trạng thái</option>
              <option value="active">🟢 Đang diễn ra</option>
              <option value="upcoming">⏳ Sắp tới</option>
              <option value="expired">⌛ Đã hết hạn</option>
              <option value="paused">⏸️ Tạm dừng</option>
            </select>
          </div>

          <div class="col-lg-2 col-md-12 text-end">
            <button class="btn btn-primary fw-bold px-3 w-100 rounded-3 shadow-xs" @click="openCreatePromoModal">
              + Tạo Voucher
            </button>
          </div>
        </div>
      </div>

      <!-- PROMOTIONS TABLE / CARD VIEW -->
      <div class="card shadow-sm border-0 rounded-4 overflow-hidden bg-white mb-4">
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th class="ps-3 py-3" style="width: 140px;">Mã Voucher</th>
                <th class="py-3">Tên Chương Trình</th>
                <th class="py-3 text-center">Mức Giảm</th>
                <th class="py-3 text-end">Đơn Tối Thiểu</th>
                <th class="py-3 text-center">Tiến Độ Lượt Dùng</th>
                <th class="py-3 text-center">Thời Gian Áp Dụng</th>
                <th class="py-3 text-center">Trạng Thái</th>
                <th class="py-3 text-end pe-3" style="width: 150px;">Thao Tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="loadingPromotions">
                <td colspan="8" class="text-center py-5 text-muted">
                  <div class="spinner-border spinner-border-sm text-teal me-2"></div>
                  <span>Đang tải danh sách voucher...</span>
                </td>
              </tr>
              <tr v-else-if="promotions.length === 0">
                <td colspan="8" class="text-center py-5">
                  <div class="text-muted fs-1 mb-2">🎟️</div>
                  <h6 class="fw-bold mb-1">Chưa có voucher nào phù hợp</h6>
                  <p class="text-muted small mb-0">Bấm nút "Tạo Voucher" để khởi tạo chiến dịch khuyến mãi mới</p>
                </td>
              </tr>
              <tr v-for="p in promotions" :key="p.promotionId">
                <td class="ps-3">
                  <div class="d-flex align-items-center gap-1">
                    <span class="voucher-code-badge">{{ p.promotionCode }}</span>
                    <button class="btn btn-sm btn-link text-muted p-0" title="Sao chép mã" @click="copyText(p.promotionCode)">
                      📋
                    </button>
                  </div>
                </td>
                <td>
                  <div class="fw-bold text-dark">{{ p.promotionName }}</div>
                  <div class="text-muted small">Tạo ngày {{ formatDate(p.createdAt) }}</div>
                </td>
                <td class="text-center">
                  <span v-if="p.promotionType === 'percent'" class="badge bg-purple-subtle text-purple fs-6 px-2 py-1 rounded-pill">
                    Giảm {{ p.promotionValue }}%
                  </span>
                  <span v-else class="badge bg-emerald-subtle text-emerald fs-6 px-2 py-1 rounded-pill">
                    -{{ formatCurrency(p.promotionValue) }}
                  </span>
                </td>
                <td class="text-end font-monospace">
                  {{ p.minimumOrderValue > 0 ? formatCurrency(p.minimumOrderValue) : '0 đ (Mọi đơn)' }}
                </td>
                <td class="text-center">
                  <div class="d-flex flex-column align-items-center">
                    <div class="small fw-bold">
                      {{ p.usedCount }} / {{ p.usageLimit ? p.usageLimit : '∞' }}
                    </div>
                    <div v-if="p.usageLimit" class="progress w-75 mt-1" style="height: 5px;">
                      <div
                        class="progress-bar bg-teal"
                        :style="{ width: Math.min(100, (p.usedCount / p.usageLimit) * 100) + '%' }"
                      ></div>
                    </div>
                  </div>
                </td>
                <td class="text-center small">
                  <div>{{ formatDate(p.startAt) }}</div>
                  <div class="text-muted">&darr;</div>
                  <div>{{ formatDate(p.endAt) }}</div>
                </td>
                <td class="text-center">
                  <span v-if="p.state === 'active'" class="badge bg-success-subtle text-success rounded-pill px-2 py-1">
                    🟢 Đang diễn ra
                  </span>
                  <span v-else-if="p.state === 'upcoming'" class="badge bg-info-subtle text-info rounded-pill px-2 py-1">
                    ⏳ Sắp diễn ra
                  </span>
                  <span v-else-if="p.state === 'expired'" class="badge bg-secondary-subtle text-secondary rounded-pill px-2 py-1">
                    ⌛ Đã hết hạn
                  </span>
                  <span v-else class="badge bg-warning-subtle text-warning rounded-pill px-2 py-1">
                    ⏸️ Tạm dừng
                  </span>
                </td>
                <td class="text-end pe-3">
                  <div class="btn-group btn-group-sm">
                    <button
                      class="btn"
                      :class="isPromoPublishedToNews(p.promotionId) ? 'btn-warning text-dark fw-bold' : 'btn-outline-secondary'"
                      :title="isPromoPublishedToNews(p.promotionId) ? 'Đang đăng trên Tin Tức (Bấm để gỡ)' : 'Bấm để đăng voucher lên Tin Tức'"
                      @click="togglePromoPublishNews(p)"
                    >
                      📰
                    </button>
                    <button class="btn btn-outline-secondary" title="Xem chi tiết lượt dùng" @click="viewPromotionDetail(p.promotionId)">
                      👁️
                    </button>
                    <button
                      class="btn"
                      :class="p.isActive ? 'btn-outline-warning' : 'btn-outline-success'"
                      :title="p.isActive ? 'Tạm dừng voucher' : 'Kích hoạt lại voucher'"
                      @click="togglePromotionStatus(p)"
                    >
                      {{ p.isActive ? '⏸️' : '▶️' }}
                    </button>
                    <button class="btn btn-outline-secondary" title="Chỉnh sửa" @click="openEditPromoModal(p)">
                      ✏️
                    </button>
                    <button class="btn btn-outline-danger" title="Xóa voucher" @click="deletePromotion(p)">
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

    <!-- ================================================================= -->
    <!-- MODAL 1: THÊM / SỬA KHÁCH HÀNG                                   -->
    <!-- ================================================================= -->
    <div v-if="showCustomerModal" class="modal-backdrop-custom" @click.self="showCustomerModal = false">
      <div class="modal-dialog-custom">
        <div class="modal-header-custom">
          <h5 class="fw-bold mb-0 text-teal">
            {{ isEditingCustomer ? '✏️ Chỉnh Sửa Thông Tin Khách Hàng' : '➕ Thêm Mới Khách Hàng Thân Thiết' }}
          </h5>
          <button class="btn-close" @click="showCustomerModal = false"></button>
        </div>
        <form @submit.prevent="saveCustomer">
          <div class="p-3">
            <div class="mb-3">
              <label class="form-label small fw-bold">Họ và tên khách hàng *</label>
              <input v-model="customerForm.fullName" class="form-control" placeholder="VD: Nguyễn Văn An" required />
            </div>

            <div class="row g-2 mb-3">
              <div class="col-md-6">
                <label class="form-label small fw-bold">Số điện thoại *</label>
                <input v-model="customerForm.phone" class="form-control" placeholder="VD: 0987654321" required />
              </div>
              <div class="col-md-6">
                <label class="form-label small fw-bold">Email</label>
                <input v-model="customerForm.email" type="email" class="form-control" placeholder="VD: an@gmail.com" />
              </div>
            </div>

            <div v-if="!isEditingCustomer" class="mb-3">
              <label class="form-label small fw-bold">Điểm thưởng khởi tạo (Tùy chọn)</label>
              <input v-model.number="customerForm.initialPoints" type="number" min="0" class="form-control" placeholder="VD: 50" />
              <div class="form-text small">Điểm tích lũy khởi tạo chào mừng thành viên mới (1 điểm = 1.000 đ).</div>
            </div>

            <div v-if="isEditingCustomer" class="mb-3">
              <label class="form-label small fw-bold">Trạng thái tài khoản</label>
              <select v-model="customerForm.isActive" class="form-select">
                <option :value="true">🟢 Hoạt động bình thường</option>
                <option :value="false">🔴 Tạm khóa tài khoản</option>
              </select>
            </div>
          </div>
          <div class="modal-footer-custom">
            <button type="button" class="btn btn-light" @click="showCustomerModal = false">Hủy</button>
            <button type="submit" class="btn btn-primary fw-bold" :disabled="savingCustomer">
              {{ savingCustomer ? 'Đang lưu...' : (isEditingCustomer ? 'Cập Nhật' : 'Tạo Khách Hàng') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ================================================================= -->
    <!-- MODAL 2: ĐIỀU CHỈNH ĐIỂM TÍCH LŨY                                 -->
    <!-- ================================================================= -->
    <div v-if="showAdjustPointsModal" class="modal-backdrop-custom" @click.self="showAdjustPointsModal = false">
      <div class="modal-dialog-custom modal-sm-custom">
        <div class="modal-header-custom">
          <h5 class="fw-bold mb-0 text-indigo">💎 Điều Chỉnh Điểm Thưởng</h5>
          <button class="btn-close" @click="showAdjustPointsModal = false"></button>
        </div>
        <form @submit.prevent="submitAdjustPoints">
          <div class="p-3">
            <div class="mb-2 p-2 bg-light rounded-3 small">
              <div>Khách: <strong>{{ targetCustomer?.fullName }}</strong></div>
              <div>Số dư hiện tại: <strong class="text-teal">{{ formatNumber(targetCustomer?.pointsBalance || 0) }} điểm</strong></div>
            </div>

            <div class="mb-3">
              <label class="form-label small fw-bold">Hành động</label>
              <div class="btn-group w-100">
                <button
                  type="button"
                  class="btn btn-sm"
                  :class="adjustPointsAction === 'add' ? 'btn-success' : 'btn-outline-success'"
                  @click="adjustPointsAction = 'add'"
                >
                  + Cộng Điểm
                </button>
                <button
                  type="button"
                  class="btn btn-sm"
                  :class="adjustPointsAction === 'deduct' ? 'btn-danger' : 'btn-outline-danger'"
                  @click="adjustPointsAction = 'deduct'"
                >
                  - Trừ Điểm
                </button>
              </div>
            </div>

            <div class="mb-3">
              <label class="form-label small fw-bold">Số điểm cần {{ adjustPointsAction === 'add' ? 'cộng' : 'trừ' }} *</label>
              <input v-model.number="adjustPointsValue" type="number" min="1" class="form-control font-monospace fw-bold" required />
            </div>

            <div class="mb-3">
              <label class="form-label small fw-bold">Lý do điều chỉnh *</label>
              <input v-model="adjustPointsReason" class="form-control" placeholder="VD: Tặng điểm sinh nhật, đền bù sự cố..." required />
            </div>
          </div>
          <div class="modal-footer-custom">
            <button type="button" class="btn btn-light" @click="showAdjustPointsModal = false">Hủy</button>
            <button type="submit" class="btn btn-primary fw-bold" :disabled="submittingPoints">
              {{ submittingPoints ? 'Đang xử lý...' : 'Xác Nhận' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ================================================================= -->
    <!-- MODAL 3: CHI TIẾT KHÁCH HÀNG & LỊCH SỬ                           -->
    <!-- ================================================================= -->
    <div v-if="showCustomerDetailModal" class="modal-backdrop-custom" @click.self="showCustomerDetailModal = false">
      <div class="modal-dialog-custom modal-lg-custom">
        <div class="modal-header-custom">
          <div>
            <h5 class="fw-bold mb-0 text-teal">👤 Hồ Sơ Khách Hàng #{{ selectedCustomerDetail?.customerId }}</h5>
            <div class="text-muted small">{{ selectedCustomerDetail?.fullName }} · {{ selectedCustomerDetail?.phone }}</div>
          </div>
          <button class="btn-close" @click="showCustomerDetailModal = false"></button>
        </div>
        <div class="p-3" style="max-height: 70vh; overflow-y: auto;">
          <!-- Quick summary cards -->
          <div class="row g-2 mb-3">
            <div class="col-4">
              <div class="p-2 border rounded-3 bg-light text-center">
                <div class="text-muted small">Hạng Thành Viên</div>
                <div class="fw-bold fs-6">{{ selectedCustomerDetail?.rankName }}</div>
              </div>
            </div>
            <div class="col-4">
              <div class="p-2 border rounded-3 bg-light text-center">
                <div class="text-muted small">Tổng Chi Tiêu</div>
                <div class="fw-bold fs-6 text-teal">{{ formatCurrency(selectedCustomerDetail?.totalSpent || 0) }}</div>
              </div>
            </div>
            <div class="col-4">
              <div class="p-2 border rounded-3 bg-light text-center">
                <div class="text-muted small">Điểm Tích Lũy</div>
                <div class="fw-bold fs-6 text-indigo">{{ formatNumber(selectedCustomerDetail?.pointsBalance || 0) }} đ</div>
              </div>
            </div>
          </div>

          <!-- Lịch sử hóa đơn gần đây -->
          <div class="mb-4">
            <h6 class="fw-bold text-dark border-bottom pb-2">🧾 Lịch Sử Hóa Đơn Gần Đây</h6>
            <div v-if="!selectedCustomerDetail?.invoices?.length" class="text-muted small py-2">Chưa có hóa đơn nào</div>
            <div v-else class="table-responsive">
              <table class="table table-sm table-bordered align-middle small mb-0">
                <thead class="table-light">
                  <tr>
                    <th>Mã HĐ</th>
                    <th>Thời gian</th>
                    <th class="text-end">Tổng tiền</th>
                    <th class="text-end">Giảm giá</th>
                    <th class="text-center">Điểm +/-</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="inv in selectedCustomerDetail.invoices" :key="inv.invoiceId">
                    <td class="font-monospace fw-bold">#{{ inv.invoiceCode }}</td>
                    <td>{{ formatDateTime(inv.issuedAt) }}</td>
                    <td class="text-end fw-bold">{{ formatCurrency(inv.totalAmount) }}</td>
                    <td class="text-end text-danger">{{ inv.discountAmount > 0 ? '-' + formatCurrency(inv.discountAmount) : '—' }}</td>
                    <td class="text-center">
                      <span v-if="inv.pointsEarned > 0" class="text-success fw-bold">+{{ inv.pointsEarned }}</span>
                      <span v-if="inv.pointsUsed > 0" class="text-danger fw-bold ms-1">-{{ inv.pointsUsed }}</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Biến động điểm thưởng -->
          <div>
            <h6 class="fw-bold text-dark border-bottom pb-2">💎 Nhật Ký Biến Động Điểm</h6>
            <div v-if="!selectedCustomerDetail?.pointLogs?.length" class="text-muted small py-2">Chưa có biến động điểm nào</div>
            <div v-else class="table-responsive">
              <table class="table table-sm table-bordered align-middle small mb-0">
                <thead class="table-light">
                  <tr>
                    <th>Thời gian</th>
                    <th class="text-center">Loại</th>
                    <th class="text-end">Điểm</th>
                    <th class="text-end">Số dư sau</th>
                    <th>Lý do / Ghi chú</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="pl in selectedCustomerDetail.pointLogs" :key="pl.transactionId">
                    <td>{{ formatDateTime(pl.createdAt) }}</td>
                    <td class="text-center">
                      <span :class="pl.points > 0 ? 'badge bg-success-subtle text-success' : 'badge bg-danger-subtle text-danger'">
                        {{ pl.points > 0 ? 'Cộng điểm' : 'Trừ điểm' }}
                      </span>
                    </td>
                    <td class="text-end fw-bold font-monospace" :class="pl.points > 0 ? 'text-success' : 'text-danger'">
                      {{ pl.points > 0 ? '+' : '' }}{{ pl.points }}
                    </td>
                    <td class="text-end font-monospace">{{ formatNumber(pl.balanceAfter) }}</td>
                    <td>{{ pl.note }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
        <div class="modal-footer-custom">
          <button type="button" class="btn btn-secondary btn-sm" @click="showCustomerDetailModal = false">Đóng</button>
        </div>
      </div>
    </div>

    <!-- ================================================================= -->
    <!-- MODAL 3B: XEM LỊCH SỬ ĐIỂM THƯỞNG CHI TIẾT (UI/UX PRO)            -->
    <!-- ================================================================= -->
    <div v-if="showPointsHistoryModal" class="modal-backdrop-custom" @click.self="showPointsHistoryModal = false">
      <div class="modal-dialog-custom modal-lg-custom">
        <!-- Header với Gradient Luxury -->
        <div class="modal-header-custom bg-teal-gradient text-white border-0 py-3">
          <div class="d-flex align-items-center gap-3">
            <div class="avatar-circle-lg shadow-sm" :class="getAvatarClass(customerPointsHistory?.customer?.rank || 'Bronze')">
              {{ getInitials(customerPointsHistory?.customer?.fullName || 'KH') }}
            </div>
            <div>
              <div class="d-flex align-items-center gap-2">
                <h5 class="fw-bold mb-0 text-white">{{ customerPointsHistory?.customer?.fullName || 'Đang tải...' }}</h5>
                <span class="badge bg-white text-teal fw-bold rounded-pill px-2 py-0" style="font-size: 0.7rem;">
                  {{ customerPointsHistory?.customer?.rankName || 'Hội viên' }}
                </span>
              </div>
              <div class="text-white-50 small font-monospace">
                📞 {{ customerPointsHistory?.customer?.phone }}
                <span v-if="customerPointsHistory?.customer?.email"> · ✉️ {{ customerPointsHistory?.customer?.email }}</span>
              </div>
            </div>
          </div>

          <div class="d-flex align-items-center gap-2">
            <button
              type="button"
              class="btn btn-sm btn-light fw-bold text-teal shadow-xs d-flex align-items-center gap-1"
              @click="openAdjustPointsFromHistory"
            >
              <span>💎</span>
              <span>Điều Chỉnh Điểm</span>
            </button>
            <button type="button" class="btn-close btn-close-white" @click="showPointsHistoryModal = false"></button>
          </div>
        </div>

        <div class="p-3 bg-slate-50" style="max-height: 75vh; overflow-y: auto;">
          <!-- 3 KPI Highlight Cards -->
          <div class="row g-2 mb-3">
            <div class="col-4">
              <div class="p-3 bg-white rounded-3 shadow-xs border text-center">
                <div class="text-muted small fw-bold text-uppercase">Số Dư Điểm Hiện Tại</div>
                <div class="fw-black fs-4 text-teal font-monospace">
                  💎 {{ formatNumber(customerPointsHistory?.stats?.currentBalance || 0) }}
                </div>
                <div class="text-muted" style="font-size: 11px;">1 điểm = 1.000 VNĐ</div>
              </div>
            </div>

            <div class="col-4">
              <div class="p-3 bg-white rounded-3 shadow-xs border text-center">
                <div class="text-muted small fw-bold text-uppercase">Tổng Điểm Đã Tích</div>
                <div class="fw-black fs-4 text-success font-monospace">
                  +{{ formatNumber(customerPointsHistory?.stats?.totalEarned || 0) }}
                </div>
                <div class="text-muted" style="font-size: 11px;">Tích lũy từ các đơn hàng</div>
              </div>
            </div>

            <div class="col-4">
              <div class="p-3 bg-white rounded-3 shadow-xs border text-center">
                <div class="text-muted small fw-bold text-uppercase">Tổng Điểm Đã Dùng</div>
                <div class="fw-black fs-4 text-danger font-monospace">
                  -{{ formatNumber(customerPointsHistory?.stats?.totalRedeemed || 0) }}
                </div>
                <div class="text-muted" style="font-size: 11px;">Đổi trừ trực tiếp hóa đơn</div>
              </div>
            </div>
          </div>

          <!-- Filter chips by Type -->
          <div class="d-flex justify-content-between align-items-center mb-3">
            <div class="d-flex gap-1">
              <button
                type="button"
                class="btn btn-sm rounded-pill px-3 fw-bold"
                :class="pointsHistoryFilter === 'all' ? 'btn-teal text-white' : 'btn-light text-muted border'"
                @click="changePointsHistoryFilter('all')"
              >
                Tất cả ({{ customerPointsHistory?.totalItems || 0 }})
              </button>
              <button
                type="button"
                class="btn btn-sm rounded-pill px-3 fw-bold"
                :class="pointsHistoryFilter === 'earn' ? 'btn-success text-white' : 'btn-light text-muted border'"
                @click="changePointsHistoryFilter('earn')"
              >
                🟢 Tích điểm (+)
              </button>
              <button
                type="button"
                class="btn btn-sm rounded-pill px-3 fw-bold"
                :class="pointsHistoryFilter === 'redeem' ? 'btn-danger text-white' : 'btn-light text-muted border'"
                @click="changePointsHistoryFilter('redeem')"
              >
                🔴 Tiêu / Trừ điểm (-)
              </button>
            </div>

            <span class="text-muted small">
              {{ customerPointsHistory?.items?.length || 0 }} giao dịch
            </span>
          </div>

          <!-- Loading State -->
          <div v-if="loadingPointsHistory" class="text-center py-5 bg-white rounded-3 border">
            <div class="spinner-border spinner-border-sm text-teal me-2"></div>
            <span class="text-muted">Đang tải lịch sử điểm thưởng...</span>
          </div>

          <!-- Empty State -->
          <div v-else-if="!customerPointsHistory?.items?.length" class="text-center py-5 bg-white rounded-3 border">
            <div class="fs-1 mb-2">📜</div>
            <h6 class="fw-bold mb-1">Chưa có giao dịch điểm nào</h6>
            <p class="text-muted small mb-0">Khách hàng chưa phát sinh tích điểm hoặc tiêu điểm trong hệ thống.</p>
          </div>

          <!-- Points History Timeline Table -->
          <div v-else class="card border-0 shadow-xs rounded-3 overflow-hidden">
            <div class="table-responsive">
              <table class="table table-hover align-middle mb-0 bg-white">
                <thead class="table-light">
                  <tr class="small text-muted text-uppercase">
                    <th class="ps-3 py-2" style="width: 140px;">Thời Gian</th>
                    <th class="py-2 text-center" style="width: 110px;">Biến Động</th>
                    <th class="py-2 text-end" style="width: 120px;">Số Dư Sau</th>
                    <th class="py-2">Chi Tiết / Lý Do</th>
                    <th class="py-2 pe-3 text-end" style="width: 160px;">Người Thực Hiện</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="log in customerPointsHistory.items" :key="log.transactionId">
                    <!-- Thời gian -->
                    <td class="ps-3 small font-monospace text-muted">
                      <div>{{ formatDateTime(log.createdAt).split(' ')[1] }}</div>
                      <div class="text-secondary fw-semibold">{{ formatDateTime(log.createdAt).split(' ')[0] }}</div>
                    </td>

                    <!-- Điểm thay đổi -->
                    <td class="text-center">
                      <span
                        class="badge rounded-pill px-2 py-1 font-monospace fw-bold fs-6"
                        :class="log.points > 0 ? 'bg-success-subtle text-success border border-success-subtle' : 'bg-danger-subtle text-danger border border-danger-subtle'"
                      >
                        {{ log.points > 0 ? '+' : '' }}{{ formatNumber(log.points) }}
                      </span>
                    </td>

                    <!-- Số dư sau -->
                    <td class="text-end font-monospace fw-bold text-dark">
                      {{ formatNumber(log.balanceAfter) }} đ
                    </td>

                    <!-- Ghi chú / Invoice -->
                    <td>
                      <div class="fw-semibold text-dark">{{ log.note }}</div>
                      <div v-if="log.invoiceCode" class="mt-1">
                        <span class="badge bg-light text-teal border font-monospace">
                          🧾 HĐ: #{{ log.invoiceCode }}
                        </span>
                      </div>
                    </td>

                    <!-- Người thực hiện -->
                    <td class="pe-3 text-end small text-muted">
                      <div class="fw-semibold text-secondary">{{ log.createdByName || 'Hệ thống tự động' }}</div>
                      <div style="font-size: 11px;" class="text-muted">Mã GD: #{{ log.transactionId }}</div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Modal Pagination if any -->
            <div v-if="customerPointsHistory.totalPages > 1" class="d-flex justify-content-between align-items-center p-2 px-3 border-top bg-light">
              <span class="text-muted small">Trang {{ pointsHistoryPage }} / {{ customerPointsHistory.totalPages }}</span>
              <div class="btn-group btn-group-sm">
                <button class="btn btn-outline-secondary" :disabled="pointsHistoryPage <= 1" @click="pointsHistoryPage--; fetchCustomerPointsHistory(customerPointsHistory.customer.customerId)">
                  &laquo; Trước
                </button>
                <button class="btn btn-outline-secondary" :disabled="pointsHistoryPage >= customerPointsHistory.totalPages" @click="pointsHistoryPage++; fetchCustomerPointsHistory(customerPointsHistory.customer.customerId)">
                  Sau &raquo;
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="modal-footer-custom bg-white">
          <button type="button" class="btn btn-secondary px-4 fw-semibold" @click="showPointsHistoryModal = false">Đóng</button>
        </div>
      </div>
    </div>

    <!-- ================================================================= -->
    <!-- MODAL 4: TẠO / SỬA KHUYẾN MÃI & VOUCHER                          -->
    <!-- ================================================================= -->
    <div v-if="showPromoModal" class="modal-backdrop-custom" @click.self="showPromoModal = false">
      <div class="modal-dialog-custom modal-lg-custom">
        <div class="modal-header-custom">
          <h5 class="fw-bold mb-0 text-teal">
            {{ isEditingPromo ? '✏️ Chỉnh Sửa Voucher / Khuyến Mãi' : '🎟️ Phát Hành Voucher Mới' }}
          </h5>
          <button class="btn-close" @click="showPromoModal = false"></button>
        </div>
        <form @submit.prevent="savePromotion">
          <div class="p-3">
            <div class="row g-3 mb-3">
              <div class="col-md-5">
                <label class="form-label small fw-bold">Mã Voucher (Code) *</label>
                <div class="input-group">
                  <input
                    v-model="promoForm.promotionCode"
                    class="form-control font-monospace text-uppercase fw-bold"
                    placeholder="VD: DOLA50K, VIP10"
                    :disabled="isEditingPromo"
                    required
                  />
                  <button v-if="!isEditingPromo" type="button" class="btn btn-outline-secondary" @click="generateRandomPromoCode">
                    🎲 Tự sinh
                  </button>
                </div>
              </div>

              <div class="col-md-7">
                <label class="form-label small fw-bold">Tên chương trình ưu đãi *</label>
                <input v-model="promoForm.promotionName" class="form-control" placeholder="VD: Khai trương giảm 10%, Ưu đãi cuối tuần..." required />
              </div>
            </div>

            <!-- Loại giảm giá & Giá trị -->
            <div class="row g-3 mb-3 p-3 bg-light rounded-3 border">
              <div class="col-md-6">
                <label class="form-label small fw-bold">Hình thức chiết khấu *</label>
                <select v-model="promoForm.promotionType" class="form-select">
                  <option value="percent">Phần trăm (%) trên tổng hóa đơn</option>
                  <option value="fixed">Số tiền cố định (đ) giảm trực tiếp</option>
                </select>
              </div>

              <div class="col-md-6">
                <label class="form-label small fw-bold">
                  {{ promoForm.promotionType === 'percent' ? 'Mức giảm (%) *' : 'Số tiền giảm (VNĐ) *' }}
                </label>
                <input
                  v-model.number="promoForm.promotionValue"
                  type="number"
                  min="1"
                  :max="promoForm.promotionType === 'percent' ? 100 : 100000000"
                  class="form-control font-monospace fw-bold"
                  placeholder="VD: 10 hoặc 50000"
                  required
                />
              </div>

              <div class="col-md-6">
                <label class="form-label small fw-bold">Đơn hàng tối thiểu (VNĐ)</label>
                <input v-model.number="promoForm.minimumOrderValue" type="number" min="0" class="form-control font-monospace" placeholder="0 nếu không yêu cầu" />
              </div>

              <div class="col-md-6">
                <label class="form-label small fw-bold">Giới hạn số lượt dùng (Toàn hệ thống)</label>
                <input v-model.number="promoForm.usageLimit" type="number" min="1" class="form-control font-monospace" placeholder="Để trống nếu không giới hạn" />
              </div>
            </div>

            <!-- Thời gian áp dụng -->
            <div class="row g-3 mb-3">
              <div class="col-md-6">
                <label class="form-label small fw-bold">Thời gian bắt đầu *</label>
                <input v-model="promoForm.startAt" type="datetime-local" class="form-control" required />
              </div>
              <div class="col-md-6">
                <label class="form-label small fw-bold">Thời gian kết thúc *</label>
                <input v-model="promoForm.endAt" type="datetime-local" class="form-control" required />
              </div>
            </div>

            <div class="mb-3">
              <label class="form-label small fw-bold">Trạng thái phát hành</label>
              <select v-model="promoForm.isActive" class="form-select">
                <option :value="true">🟢 Đang kích hoạt (Cho phép khách áp dụng)</option>
                <option :value="false">⏸️ Tạm dừng phát hành</option>
              </select>
            </div>

            <!-- TÙY CHỌN ĐĂNG LÊN TRANG TIN TỨC -->
            <div class="mb-3 p-3 bg-warning-subtle rounded-3 border border-warning-subtle d-flex align-items-center justify-content-between">
              <div>
                <div class="fw-bold small text-dark d-flex align-items-center gap-1">
                  <span>📰</span>
                  <span>Đăng bài lên trang Tin tức & Khuyến mãi</span>
                </div>
                <small class="text-muted">Voucher này sẽ tự động xuất hiện trên trang Tin Tức cho thực khách lưu mã và đặt bàn.</small>
              </div>
              <div class="form-check form-switch mb-0 fs-5">
                <input class="form-check-input" type="checkbox" v-model="promoForm.publishToNews" role="switch">
              </div>
            </div>
          </div>
          <div class="modal-footer-custom">
            <button type="button" class="btn btn-light" @click="showPromoModal = false">Hủy</button>
            <button type="submit" class="btn btn-primary fw-bold" :disabled="savingPromo">
              {{ savingPromo ? 'Đang lưu...' : (isEditingPromo ? 'Cập Nhật Voucher' : 'Phát Hành Ngay') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ================================================================= -->
    <!-- MODAL 5: CHI TIẾT LƯỢT DÙNG VOUCHER                              -->
    <!-- ================================================================= -->
    <div v-if="showPromoDetailModal" class="modal-backdrop-custom" @click.self="showPromoDetailModal = false">
      <div class="modal-dialog-custom modal-lg-custom">
        <div class="modal-header-custom">
          <div>
            <h5 class="fw-bold mb-0 text-teal">🎟️ Nhật Ký Sử Dụng Voucher {{ selectedPromoDetail?.promotionCode }}</h5>
            <div class="text-muted small">{{ selectedPromoDetail?.promotionName }}</div>
          </div>
          <button class="btn-close" @click="showPromoDetailModal = false"></button>
        </div>
        <div class="p-3" style="max-height: 65vh; overflow-y: auto;">
          <div class="row g-2 mb-3">
            <div class="col-4">
              <div class="p-2 border rounded-3 bg-light text-center">
                <div class="text-muted small">Đã Sử Dụng</div>
                <div class="fw-bold fs-6 text-teal">{{ selectedPromoDetail?.usedCount }} lượt</div>
              </div>
            </div>
            <div class="col-4">
              <div class="p-2 border rounded-3 bg-light text-center">
                <div class="text-muted small">Giới Hạn</div>
                <div class="fw-bold fs-6">{{ selectedPromoDetail?.usageLimit ? selectedPromoDetail.usageLimit + ' lượt' : 'Không giới hạn' }}</div>
              </div>
            </div>
            <div class="col-4">
              <div class="p-2 border rounded-3 bg-light text-center">
                <div class="text-muted small">Tổng Ngân Sách Giảm</div>
                <div class="fw-bold fs-6 text-gold">{{ formatCurrency(selectedPromoDetail?.totalDiscountFundUsed || 0) }}</div>
              </div>
            </div>
          </div>

          <h6 class="fw-bold text-dark border-bottom pb-2">👥 Danh Sách Đơn Hàng Áp Dụng</h6>
          <div v-if="!selectedPromoDetail?.usages?.length" class="text-muted small py-3 text-center">Chưa có đơn hàng nào dùng voucher này</div>
          <div v-else class="table-responsive">
            <table class="table table-sm table-bordered align-middle small mb-0">
              <thead class="table-light">
                <tr>
                  <th>Thời gian dùng</th>
                  <th>Mã đơn</th>
                  <th>Khách hàng</th>
                  <th>Số điện thoại</th>
                  <th class="text-end">Số tiền giảm</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="u in selectedPromoDetail.usages" :key="u.usageId">
                  <td>{{ formatDateTime(u.usedAt) }}</td>
                  <td class="font-monospace fw-bold">#ORD-{{ u.orderId }}</td>
                  <td>{{ u.customerName }}</td>
                  <td class="font-monospace">{{ u.customerPhone || '—' }}</td>
                  <td class="text-end fw-bold text-danger">-{{ formatCurrency(u.discountAmount) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="modal-footer-custom">
          <button type="button" class="btn btn-secondary btn-sm" @click="showPromoDetailModal = false">Đóng</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import axios from 'axios';

const apiCrm = 'http://localhost:5079/api/admin/crm';

// Tab active
const currentTab = ref('customers'); // 'customers' | 'promotions'

// Alerts
const successMsg = ref('');
const errorMsg = ref('');

// Stats chung
const crmStats = ref({});

// =========================================================================
// CUSTOMER STATE
// =========================================================================
const customers = ref([]);
const loadingCustomers = ref(false);
const customerSearch = ref('');
const customerRankFilter = ref('all');
const customerStatusFilter = ref('all');
const customerSortBy = ref('spent_desc');
const customerPage = ref(1);
const customerTotalPages = ref(1);
const customerTotalItems = ref(0);

// Modal create/edit customer
const showCustomerModal = ref(false);
const isEditingCustomer = ref(false);
const savingCustomer = ref(false);
const customerForm = ref({
  customerId: 0,
  fullName: '',
  phone: '',
  email: '',
  initialPoints: 0,
  isActive: true
});

// Modal adjust points
const showAdjustPointsModal = ref(false);
const targetCustomer = ref(null);
const adjustPointsAction = ref('add');
const adjustPointsValue = ref(10);
const adjustPointsReason = ref('');
const submittingPoints = ref(false);

// Modal detail customer
const showCustomerDetailModal = ref(false);
const selectedCustomerDetail = ref(null);

// Modal Lịch sử điểm thưởng
const showPointsHistoryModal = ref(false);
const loadingPointsHistory = ref(false);
const customerPointsHistory = ref(null);
const pointsHistoryFilter = ref('all'); // 'all' | 'earn' | 'redeem'
const pointsHistoryPage = ref(1);

// =========================================================================
// PROMOTION STATE
// =========================================================================
const promotions = ref([]);
const loadingPromotions = ref(false);
const promoSearch = ref('');
const promoTypeFilter = ref('all');
const promoStatusFilter = ref('all');

// Modal create/edit promo
const showPromoModal = ref(false);
const isEditingPromo = ref(false);
const savingPromo = ref(false);
const PUBLISHED_PROMOS_KEY = 'published_news_promo_ids';
const publishedNewsPromoIds = ref([]);

function loadPublishedNewsPromoIds() {
  try {
    const raw = localStorage.getItem(PUBLISHED_PROMOS_KEY);
    publishedNewsPromoIds.value = raw ? JSON.parse(raw) : [];
  } catch {
    publishedNewsPromoIds.value = [];
  }
}

function isPromoPublishedToNews(id) {
  // If no list is explicitly defined yet, default to all active promos being published
  if (!publishedNewsPromoIds.value || publishedNewsPromoIds.value.length === 0) return true;
  return publishedNewsPromoIds.value.includes(id);
}

function togglePromoPublishNews(p) {
  const id = p.promotionId;
  if (!publishedNewsPromoIds.value || publishedNewsPromoIds.value.length === 0) {
    // initialize with all current promo IDs
    publishedNewsPromoIds.value = promotions.value.map(x => x.promotionId);
  }
  const idx = publishedNewsPromoIds.value.indexOf(id);
  if (idx > -1) {
    publishedNewsPromoIds.value.splice(idx, 1);
    showSuccess(`Đã gỡ voucher "${p.promotionCode}" khỏi trang Tin Tức.`);
  } else {
    publishedNewsPromoIds.value.push(id);
    showSuccess(`Đã đăng voucher "${p.promotionCode}" lên trang Tin Tức!`);
  }
  localStorage.setItem(PUBLISHED_PROMOS_KEY, JSON.stringify(publishedNewsPromoIds.value));
}

const promoForm = ref({
  promotionId: 0,
  promotionCode: '',
  promotionName: '',
  promotionType: 'percent',
  promotionValue: 10,
  minimumOrderValue: 0,
  usageLimit: null,
  startAt: '',
  endAt: '',
  isActive: true,
  publishToNews: true
});

// Modal detail promo
const showPromoDetailModal = ref(false);
const selectedPromoDetail = ref(null);

// Debounce timer
let debounceTimer = null;
function debounceFetchCustomers() {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    customerPage.value = 1;
    fetchCustomers();
  }, 300);
}
function debounceFetchPromotions() {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    fetchPromotions();
  }, 300);
}

// =========================================================================
// API CALLS
// =========================================================================
async function fetchStats() {
  try {
    const res = await axios.get(`${apiCrm}/stats`);
    crmStats.value = res.data;
  } catch (err) {
    console.error('Lỗi tải thống kê CRM:', err);
  }
}

async function fetchCustomers() {
  loadingCustomers.value = true;
  try {
    const res = await axios.get(`${apiCrm}/customers`, {
      params: {
        search: customerSearch.value,
        rank: customerRankFilter.value,
        status: customerStatusFilter.value,
        sortBy: customerSortBy.value,
        page: customerPage.value,
        pageSize: 15
      }
    });
    customers.value = res.data.items || [];
    customerTotalItems.value = res.data.totalItems || 0;
    customerTotalPages.value = res.data.totalPages || 1;
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải danh sách khách hàng.';
  } finally {
    loadingCustomers.value = false;
  }
}

async function fetchPromotions() {
  loadingPromotions.value = true;
  try {
    const res = await axios.get(`${apiCrm}/promotions`, {
      params: {
        search: promoSearch.value,
        type: promoTypeFilter.value,
        status: promoStatusFilter.value,
        page: 1,
        pageSize: 50
      }
    });
    promotions.value = res.data.items || [];
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải danh sách khuyến mãi.';
  } finally {
    loadingPromotions.value = false;
  }
}

// Thao tác khách hàng
function openCreateCustomerModal() {
  isEditingCustomer.value = false;
  customerForm.value = {
    customerId: 0,
    fullName: '',
    phone: '',
    email: '',
    initialPoints: 0,
    isActive: true
  };
  showCustomerModal.value = true;
}

function openEditCustomerModal(c) {
  isEditingCustomer.value = true;
  customerForm.value = {
    customerId: c.customerId,
    fullName: c.fullName,
    phone: c.phone,
    email: c.email || '',
    isActive: c.isActive
  };
  showCustomerModal.value = true;
}

async function saveCustomer() {
  savingCustomer.value = true;
  errorMsg.value = '';
  try {
    if (isEditingCustomer.value) {
      await axios.put(`${apiCrm}/customers/${customerForm.value.customerId}`, {
        fullName: customerForm.value.fullName,
        phone: customerForm.value.phone,
        email: customerForm.value.email,
        isActive: customerForm.value.isActive
      });
      showSuccess('Cập nhật khách hàng thành công!');
    } else {
      await axios.post(`${apiCrm}/customers`, customerForm.value);
      showSuccess('Tạo khách hàng mới thành công!');
    }
    showCustomerModal.value = false;
    fetchCustomers();
    fetchStats();
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi lưu thông tin khách hàng.';
  } finally {
    savingCustomer.value = false;
  }
}

function openAdjustPointsModal(c) {
  targetCustomer.value = c;
  adjustPointsAction.value = 'add';
  adjustPointsValue.value = 10;
  adjustPointsReason.value = 'Tặng điểm chăm sóc khách hàng';
  showAdjustPointsModal.value = true;
}

async function submitAdjustPoints() {
  if (!targetCustomer.value) return;
  submittingPoints.value = true;
  errorMsg.value = '';
  try {
    const pts = adjustPointsAction.value === 'add' ? adjustPointsValue.value : -adjustPointsValue.value;
    const res = await axios.post(`${apiCrm}/customers/${targetCustomer.value.customerId}/adjust-points`, {
      points: pts,
      reason: adjustPointsReason.value
    });
    showSuccess(res.data.message || 'Điều chỉnh điểm thành công!');
    showAdjustPointsModal.value = false;
    fetchCustomers();
    fetchStats();
    if (customerPointsHistory.value?.customer?.customerId === targetCustomer.value.customerId) {
      fetchCustomerPointsHistory(targetCustomer.value.customerId);
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi điều chỉnh điểm.';
  } finally {
    submittingPoints.value = false;
  }
}

async function toggleCustomerStatus(c) {
  const confirmMsg = c.isActive
    ? `Bạn có chắc muốn tạm khóa tài khoản của khách hàng "${c.fullName}"?`
    : `Mở khóa lại cho khách hàng "${c.fullName}"?`;
  if (!confirm(confirmMsg)) return;

  try {
    const res = await axios.delete(`${apiCrm}/customers/${c.customerId}`);
    showSuccess(res.data.message || 'Cập nhật trạng thái thành công!');
    fetchCustomers();
    fetchStats();
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi thay đổi trạng thái khách hàng.';
  }
}

async function viewCustomerDetail(id) {
  try {
    const res = await axios.get(`${apiCrm}/customers/${id}`);
    selectedCustomerDetail.value = res.data;
    showCustomerDetailModal.value = true;
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải chi tiết khách hàng.';
  }
}

// =========================================================================
// POINTS HISTORY METHODS (UI/UX PRO)
// =========================================================================
async function openPointsHistoryModal(c) {
  showPointsHistoryModal.value = true;
  customerPointsHistory.value = null;
  pointsHistoryFilter.value = 'all';
  pointsHistoryPage.value = 1;
  await fetchCustomerPointsHistory(c.customerId);
}

async function fetchCustomerPointsHistory(customerId) {
  loadingPointsHistory.value = true;
  try {
    const res = await axios.get(`${apiCrm}/customers/${customerId}/points-history`, {
      params: {
        type: pointsHistoryFilter.value,
        page: pointsHistoryPage.value,
        pageSize: 20
      }
    });
    customerPointsHistory.value = res.data;
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải lịch sử điểm.';
  } finally {
    loadingPointsHistory.value = false;
  }
}

function changePointsHistoryFilter(type) {
  pointsHistoryFilter.value = type;
  pointsHistoryPage.value = 1;
  if (customerPointsHistory.value?.customer?.customerId) {
    fetchCustomerPointsHistory(customerPointsHistory.value.customer.customerId);
  }
}

function openAdjustPointsFromHistory() {
  if (!customerPointsHistory.value?.customer) return;
  openAdjustPointsModal(customerPointsHistory.value.customer);
}

// Thao tác khuyến mãi
function openCreatePromoModal() {
  isEditingPromo.value = false;
  const now = new Date();
  const nextMonth = new Date();
  nextMonth.setMonth(now.getMonth() + 1);

  promoForm.value = {
    promotionId: 0,
    promotionCode: '',
    promotionName: '',
    promotionType: 'percent',
    promotionValue: 10,
    minimumOrderValue: 0,
    usageLimit: null,
    startAt: formatInputDate(now),
    endAt: formatInputDate(nextMonth),
    isActive: true,
    publishToNews: true
  };
  showPromoModal.value = true;
}

function openEditPromoModal(p) {
  isEditingPromo.value = true;
  promoForm.value = {
    promotionId: p.promotionId,
    promotionCode: p.promotionCode,
    promotionName: p.promotionName,
    promotionType: p.promotionType,
    promotionValue: p.promotionValue,
    minimumOrderValue: p.minimumOrderValue || 0,
    usageLimit: p.usageLimit || null,
    startAt: formatInputDate(new Date(p.startAt)),
    endAt: formatInputDate(new Date(p.endAt)),
    isActive: p.isActive,
    publishToNews: isPromoPublishedToNews(p.promotionId)
  };
  showPromoModal.value = true;
}

function generateRandomPromoCode() {
  const prefixes = ['DOLA', 'VIP', 'CHAO', 'SUMMER', 'FOOD', 'MON'];
  const prefix = prefixes[Math.floor(Math.random() * prefixes.length)];
  const num = Math.floor(100 + Math.random() * 900);
  promoForm.value.promotionCode = `${prefix}${num}`;
}

async function savePromotion() {
  savingPromo.value = true;
  errorMsg.value = '';
  try {
    if (isEditingPromo.value) {
      await axios.put(`${apiCrm}/promotions/${promoForm.value.promotionId}`, {
        promotionName: promoForm.value.promotionName,
        promotionType: promoForm.value.promotionType,
        promotionValue: promoForm.value.promotionValue,
        minimumOrderValue: promoForm.value.minimumOrderValue,
        usageLimit: promoForm.value.usageLimit,
        startAt: new Date(promoForm.value.startAt).toISOString(),
        endAt: new Date(promoForm.value.endAt).toISOString(),
        isActive: promoForm.value.isActive
      });
      showSuccess('Cập nhật chương trình khuyến mãi thành công!');
    } else {
      await axios.post(`${apiCrm}/promotions`, {
        ...promoForm.value,
        startAt: new Date(promoForm.value.startAt).toISOString(),
        endAt: new Date(promoForm.value.endAt).toISOString()
      });
      showSuccess('Phát hành voucher mới thành công!');
    }
    showPromoModal.value = false;
    await fetchPromotions();
    await fetchStats();

    // Sync publish to news
    const savedPromo = promotions.value.find(x => x.promotionCode.toUpperCase() === promoForm.value.promotionCode.toUpperCase()) || { promotionId: promoForm.value.promotionId };
    if (savedPromo && savedPromo.promotionId) {
      if (!publishedNewsPromoIds.value || publishedNewsPromoIds.value.length === 0) {
        publishedNewsPromoIds.value = promotions.value.map(x => x.promotionId);
      }
      const idx = publishedNewsPromoIds.value.indexOf(savedPromo.promotionId);
      if (promoForm.value.publishToNews && idx === -1) {
        publishedNewsPromoIds.value.push(savedPromo.promotionId);
      } else if (!promoForm.value.publishToNews && idx > -1) {
        publishedNewsPromoIds.value.splice(idx, 1);
      }
      localStorage.setItem(PUBLISHED_PROMOS_KEY, JSON.stringify(publishedNewsPromoIds.value));
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi lưu voucher.';
  } finally {
    savingPromo.value = false;
  }
}

async function togglePromotionStatus(p) {
  try {
    const res = await axios.patch(`${apiCrm}/promotions/${p.promotionId}/toggle-status`);
    showSuccess(res.data.message || 'Cập nhật trạng thái thành công!');
    fetchPromotions();
    fetchStats();
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi đổi trạng thái voucher.';
  }
}

async function deletePromotion(p) {
  if (!confirm(`Bạn có chắc muốn xóa hoặc dừng voucher "${p.promotionCode}"?`)) return;
  try {
    const res = await axios.delete(`${apiCrm}/promotions/${p.promotionId}`);
    showSuccess(res.data.message || 'Xóa voucher thành công!');
    fetchPromotions();
    fetchStats();
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi xóa voucher.';
  }
}

async function viewPromotionDetail(id) {
  try {
    const res = await axios.get(`${apiCrm}/promotions/${id}`);
    selectedPromoDetail.value = res.data;
    showPromoDetailModal.value = true;
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Không thể tải chi tiết voucher.';
  }
}

// Helpers
function showSuccess(msg) {
  successMsg.value = msg;
  setTimeout(() => { if (successMsg.value === msg) successMsg.value = ''; }, 4000);
}

function formatCurrency(val) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val || 0);
}

function formatNumber(val) {
  return new Intl.NumberFormat('vi-VN').format(val || 0);
}

function formatDate(d) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('vi-VN');
}

function formatDateTime(d) {
  if (!d) return '—';
  return new Date(d).toLocaleString('vi-VN', { hour12: false });
}

function formatInputDate(date) {
  const pad = (n) => String(n).padStart(2, '0');
  const yyyy = date.getFullYear();
  const mm = pad(date.getMonth() + 1);
  const dd = pad(date.getDate());
  const hh = pad(date.getHours());
  const min = pad(date.getMinutes());
  return `${yyyy}-${mm}-${dd}T${hh}:${min}`;
}

function getInitials(name) {
  if (!name) return 'KH';
  const parts = name.trim().split(' ');
  return parts.length > 1 ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase() : name.substring(0, 2).toUpperCase();
}

function getRankIcon(rank) {
  return ({ Diamond: '💎', Gold: '🥇', Silver: '🥈' })[rank] || '🥉';
}

function getAvatarClass(rank) {
  return ({ Diamond: 'avatar-diamond', Gold: 'avatar-gold', Silver: 'avatar-silver' })[rank] || 'avatar-bronze';
}

function copyText(txt) {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(txt);
    showSuccess(`Đã sao chép mã '${txt}' vào clipboard!`);
  }
}

onMounted(() => {
  loadPublishedNewsPromoIds();
  fetchStats();
  fetchCustomers();
  fetchPromotions();
});
</script>

<style scoped>
.crm-promotion-container {
  padding-bottom: 3rem;
}

.page-title {
  color: #0F172A;
  letter-spacing: -0.02em;
}

/* Color Tokens */
.text-teal { color: #0F766E !important; }
.bg-teal { background-color: #0F766E !important; }
.bg-teal-subtle { background-color: #CCFBF1 !important; color: #0F766E !important; }

.text-gold { color: #D97706 !important; }
.bg-gold-subtle { background-color: #FEF3C7 !important; color: #D97706 !important; }

.text-emerald { color: #059669 !important; }
.bg-emerald-subtle { background-color: #D1FAE5 !important; color: #059669 !important; }
.bg-emerald-badge { background-color: #10B981 !important; color: #fff !important; }

.text-indigo { color: #4F46E5 !important; }
.bg-indigo-subtle { background-color: #E0E7FF !important; color: #4F46E5 !important; }

.text-purple { color: #7C3AED !important; }
.bg-purple-subtle { background-color: #EDE9FE !important; color: #7C3AED !important; }

.pulse-dot {
  display: inline-block;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background-color: #0F766E;
  margin-right: 4px;
  animation: pulse 1.8s infinite;
}

@keyframes pulse {
  0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(15, 118, 110, 0.7); }
  70% { transform: scale(1); box-shadow: 0 0 0 5px rgba(15, 118, 110, 0); }
  100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(15, 118, 110, 0); }
}

/* Segmented Control */
.segmented-control {
  background-color: #F1F5F9;
}
.seg-btn {
  border: none;
  background: transparent;
  padding: 8px 16px;
  border-radius: 12px;
  font-size: 13.5px;
  color: #64748B;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
}
.seg-btn:hover {
  color: #1E293B;
}
.seg-btn.active {
  background: #FFFFFF;
  color: #0F172A;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}
.seg-badge {
  font-size: 11px;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 20px;
  background-color: #E2E8F0;
  color: #475569;
}

/* KPI Cards */
.kpi-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.kpi-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.06) !important;
}
.kpi-icon-box {
  width: 50px;
  height: 50px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}
.kpi-label {
  font-size: 12px;
  font-weight: 700;
  color: #64748B;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}
.kpi-value {
  font-size: 21px;
  font-weight: 900;
  letter-spacing: -0.02em;
}
.kpi-sub {
  font-size: 11px;
}

/* Table Avatar & Pills */
.avatar-circle {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 800;
  color: white;
}
.avatar-diamond { background: linear-gradient(135deg, #0284C7, #38BDF8); }
.avatar-gold { background: linear-gradient(135deg, #D97706, #FBBF24); }
.avatar-silver { background: linear-gradient(135deg, #475569, #94A3B8); }
.avatar-bronze { background: linear-gradient(135deg, #92400E, #D97706); }

.rank-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: 11.5px;
  font-weight: 800;
}
.rank-diamond { background-color: #E0F2FE; color: #0284C7; border: 1px solid #BAE6FD; }
.rank-gold { background-color: #FEF3C7; color: #B45309; border: 1px solid #FDE68A; }
.rank-silver { background-color: #F1F5F9; color: #475569; border: 1px solid #CBD5E1; }
.rank-bronze { background-color: #FFFBEB; color: #92400E; border: 1px solid #FCD34D; }

.points-badge {
  display: inline-block;
  padding: 3px 8px;
  border-radius: 8px;
  background-color: #EEF2FF;
  color: #4F46E5;
  font-weight: 800;
  font-family: monospace;
  font-size: 12.5px;
}

.voucher-code-badge {
  display: inline-block;
  font-family: monospace;
  font-weight: 900;
  color: #0F766E;
  background: #F0FDFA;
  padding: 3px 8px;
  border-radius: 6px;
  border: 1.5px dashed #0D9488;
  font-size: 13px;
  letter-spacing: 0.05em;
}

/* UI/UX Pro Quick Rank Filter Chips */
.filter-chip {
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  color: #64748B;
  font-size: 12px;
  font-weight: 700;
  padding: 4px 12px;
  border-radius: 20px;
  cursor: pointer;
  transition: all 0.2s ease;
}
.filter-chip:hover {
  background: #F1F5F9;
  color: #1E293B;
  border-color: #CBD5E1;
}
.filter-chip.active {
  background: #0F766E;
  color: #FFFFFF;
  border-color: #0F766E;
  box-shadow: 0 2px 6px rgba(15, 118, 110, 0.25);
}
.chip-diamond.active {
  background: #0284C7;
  border-color: #0284C7;
  box-shadow: 0 2px 6px rgba(2, 132, 199, 0.25);
}
.chip-gold.active {
  background: #D97706;
  border-color: #D97706;
  box-shadow: 0 2px 6px rgba(217, 119, 6, 0.25);
}
.chip-silver.active {
  background: #475569;
  border-color: #475569;
  box-shadow: 0 2px 6px rgba(71, 85, 105, 0.25);
}
.chip-bronze.active {
  background: #92400E;
  border-color: #92400E;
  box-shadow: 0 2px 6px rgba(146, 64, 14, 0.25);
}

/* Points Interactive Chip in Table */
.points-chip-interactive {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  border-radius: 10px;
  background: linear-gradient(135deg, #EEF2FF 0%, #E0E7FF 100%);
  border: 1px solid #C7D2FE;
  color: #4338CA;
  cursor: pointer;
  font-family: inherit;
  font-size: 13px;
  font-weight: 800;
  transition: all 0.2s ease;
  box-shadow: 0 1px 3px rgba(79, 70, 229, 0.08);
}
.points-chip-interactive:hover {
  background: linear-gradient(135deg, #4338CA 0%, #3730A3 100%);
  color: #FFFFFF;
  border-color: #3730A3;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(67, 56, 202, 0.25);
}
.points-chip-interactive .points-arrow {
  font-size: 11px;
  opacity: 0.6;
  transition: transform 0.2s ease;
}
.points-chip-interactive:hover .points-arrow {
  opacity: 1;
  transform: translate(2px, -2px);
}

/* Minimalist Glowing Status Badge */
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 700;
}
.status-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
}
.status-active {
  background: #ECFDF5;
  color: #059669;
  border: 1px solid #A7F3D0;
}
.status-active .status-dot {
  background: #10B981;
  box-shadow: 0 0 6px rgba(16, 185, 129, 0.6);
}
.status-inactive {
  background: #FEF2F2;
  color: #DC2626;
  border: 1px solid #FECACA;
}
.status-inactive .status-dot {
  background: #EF4444;
}

/* Pro Action Buttons in Table */
.btn-action-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 9px;
  border-radius: 8px;
  font-size: 12px;
  font-weight: 700;
  border: 1px solid transparent;
  cursor: pointer;
  transition: all 0.18s ease;
}
.btn-action-teal {
  background: #F0FDFA;
  color: #0F766E;
  border-color: #99F6E4;
}
.btn-action-teal:hover {
  background: #0F766E;
  color: #FFFFFF;
  border-color: #0F766E;
  box-shadow: 0 2px 6px rgba(15, 118, 110, 0.25);
}
.btn-action-indigo {
  background: #EEF2FF;
  color: #4338CA;
  border-color: #C7D2FE;
}
.btn-action-indigo:hover {
  background: #4338CA;
  color: #FFFFFF;
  border-color: #4338CA;
  box-shadow: 0 2px 6px rgba(67, 56, 202, 0.25);
}
.btn-action-icon {
  width: 29px;
  height: 29px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: #F8FAFC;
  border: 1px solid #E2E8F0;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.18s ease;
}
.btn-action-icon:hover {
  background: #F1F5F9;
  border-color: #CBD5E1;
  transform: translateY(-1px);
}
.btn-action-lock:hover {
  background: #FEF2F2;
  border-color: #FCA5A5;
}
.btn-action-unlock:hover {
  background: #ECFDF5;
  border-color: #6EE7B7;
}

/* Modal Luxury Headers & Avatars */
.bg-teal-gradient {
  background: linear-gradient(135deg, #0F766E 0%, #115E59 100%);
}
.btn-teal {
  background-color: #0F766E;
  border-color: #0F766E;
}
.btn-teal:hover {
  background-color: #115E59;
  border-color: #115E59;
}
.avatar-circle-lg {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 800;
  color: white;
  border: 2px solid rgba(255, 255, 255, 0.85);
}

.customer-row:hover td {
  background-color: #F8FAFC !important;
}

/* Custom Modals */
.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background-color: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(4px);
  z-index: 1050;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
}
.modal-dialog-custom {
  background-color: #FFFFFF;
  border-radius: 20px;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.25);
  width: 100%;
  max-width: 540px;
  overflow: hidden;
  animation: modalFadeIn 0.2s ease-out;
}
.modal-sm-custom { max-width: 420px; }
.modal-lg-custom { max-width: 820px; }

@keyframes modalFadeIn {
  from { opacity: 0; transform: scale(0.96) translateY(-8px); }
  to { opacity: 1; transform: scale(1) translateY(0); }
}

.modal-header-custom {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid #E2E8F0;
  background-color: #F8FAFC;
}
.modal-footer-custom {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 12px 20px;
  border-top: 1px solid #E2E8F0;
  background-color: #F8FAFC;
}
</style>

<template>
  <div class="dola-theme customer-account-page" :style="{ '--dola-primary': accentColor }">
    <section class="dola-page-header account-hero">
      <div class="container py-4 py-md-5">
        <div class="d-flex flex-column flex-lg-row align-items-lg-end justify-content-between gap-4">
          <div>
            <span class="dola-pill-badge mb-3">Tài khoản cá nhân</span>
            <h1 class="dola-script-title account-title mb-2">Xin chào, {{ account?.fullName || 'Quý khách' }}</h1>
            <p class="text-body-dola mb-0">Quản lý thông tin, lịch đặt bàn, hóa đơn và điểm thành viên trong một nơi.</p>
          </div>
          <div class="account-actions d-flex flex-wrap gap-2 align-items-center">
            <span class="account-live-status"><i :class="{ syncing: isLiveSyncing }"></i>{{ isLiveSyncing ? 'Đang đồng bộ' : 'Cập nhật trực tiếp' }}</span>
            <button class="dola-btn-outline" type="button" @click="loadAccount" :disabled="loading">
              <span v-if="loading" class="spinner-border spinner-border-sm me-1"></span>
              {{ loading ? 'Đang tải...' : '🔄 Làm mới dữ liệu' }}
            </button>
          </div>
        </div>
      </div>
    </section>

    <section class="py-4 py-md-5">
      <div class="container">
        <div v-if="errorMessage" class="dola-alert mb-4" role="alert">
          <span>{{ errorMessage }}</span>
          <router-link v-if="!token" to="/login" class="dola-btn-primary alert-button">Đăng nhập</router-link>
        </div>

        <div v-if="loading" class="account-loading dola-card p-5 text-center">
          <div class="spinner-border text-gold mb-3"></div>
          <p class="text-body-dola mb-0">Đang tải thông tin tài khoản...</p>
        </div>

        <template v-else-if="account">
          <div class="row g-4 mb-4">
            <div class="col-12 col-lg-4">
              <article class="dola-card account-profile-card h-100 reveal-card" style="--delay: 0ms">
                <div class="profile-cover"></div>
                <div class="profile-body p-4">
                  <div class="account-avatar">{{ initials }}</div>
                  <div class="d-flex justify-content-between align-items-start gap-2 mt-3">
                    <div>
                      <h2 class="fs-5 fw-bold mb-1" style="color: #1F1A17;">{{ account.fullName }}</h2>
                      <p class="text-body-dola small mb-0">{{ account.email }}</p>
                    </div>
                    <span class="member-status">Thành viên</span>
                  </div>
                  <div class="profile-contact mt-4">
                    <div><span>Số điện thoại</span><strong>{{ account.phone }}</strong></div>
                    <div><span>Tham gia từ</span><strong>{{ formatDate(account.createdAt) }}</strong></div>
                  </div>
                  <button class="dola-btn-outline w-100 mt-4" type="button" @click="showEdit = !showEdit">
                    {{ showEdit ? 'Đóng chỉnh sửa' : 'Chỉnh sửa thông tin' }}
                  </button>
                  <button class="btn btn-sm btn-outline-danger w-100 mt-2 d-inline-flex align-items-center justify-content-center gap-1" type="button" @click="handleLogout">
                    <span>🚪</span>
                    <span>Đăng xuất khỏi tài khoản</span>
                  </button>
                </div>
              </article>
            </div>

            <div class="col-12 col-lg-8">
              <div class="row g-3 h-100">
                <div v-for="(metric, index) in metrics" :key="metric.label" class="col-6 col-md-3">
                  <article class="dola-card metric-card h-100 reveal-card" :style="{ '--delay': `${index * 70 + 70}ms` }">
                    <span class="metric-icon">{{ metric.icon }}</span>
                    <strong>{{ metric.value }}</strong>
                    <span>{{ metric.label }}</span>
                  </article>
                </div>
                <div class="col-12">
                  <article class="dola-card member-progress-card reveal-card" style="--delay: 350ms">
                    <div class="d-flex justify-content-between align-items-center gap-3 mb-2">
                      <div>
                        <span class="eyebrow">Quyền lợi thành viên</span>
                        <h3 class="fs-6 fw-bold mb-0" style="color: #1F1A17;">Điểm tích lũy của bạn</h3>
                      </div>
                      <strong class="text-gold fs-4">{{ account.pointsBalance.toLocaleString('vi-VN') }}</strong>
                    </div>
                    <div class="progress-track"><span :style="{ width: `${progressPercent}%` }"></span></div>
                    <p class="text-body-dola small mb-0 mt-2">{{ pointsToNextLevel }} điểm để mở ưu đãi tiếp theo.</p>
                  </article>
                </div>
              </div>
            </div>
          </div>

          <div v-if="showEdit" class="dola-card p-4 mb-4 edit-panel reveal-card" style="--delay: 0ms">
            <div class="d-flex justify-content-between align-items-center gap-3 mb-3">
              <div><span class="eyebrow">Thông tin cá nhân</span><h2 class="fs-5 fw-bold mb-0" style="color: #1F1A17;">Cập nhật tài khoản</h2></div>
              <span class="text-body-dola small">Thông tin này dùng để xác nhận đặt bàn</span>
            </div>
            <form class="row g-3" @submit.prevent="saveAccount">
              <div class="col-12 col-md-4"><label class="dola-form-label">Họ và tên</label><input v-model="editForm.fullName" class="dola-input" required /></div>
              <div class="col-12 col-md-4"><label class="dola-form-label">Số điện thoại</label><input v-model="editForm.phone" class="dola-input" required /></div>
              <div class="col-12 col-md-4"><label class="dola-form-label">Email</label><input v-model="editForm.email" class="dola-input" type="email" required /></div>
              <div class="col-12 d-flex flex-wrap justify-content-end gap-2"><button type="button" class="dola-btn-outline" @click="showEdit = false">Hủy</button><button class="dola-btn-primary" :disabled="saving">{{ saving ? 'Đang lưu...' : 'Lưu thay đổi' }}</button></div>
            </form>
          </div>

          <div class="dola-card p-2 p-md-3 mb-4 account-tabs-card">
            <div class="account-tabs" role="tablist">
              <button v-for="tab in tabs" :key="tab.key" type="button" :class="['account-tab', { active: activeTab === tab.key }]" @click="switchTab(tab.key)">
                <span>{{ tab.icon }}</span>{{ tab.label }}<small>{{ tab.count }}</small>
              </button>
            </div>
          </div>

          <!-- TAB BÀN ĐÃ ĐẶT (LUXURY DINING PASS & REALTIME) -->
          <section v-if="activeTab === 'reservations'" class="dola-card p-4 content-panel">
            <!-- Realtime Update Notification Banner -->
            <transition name="fade">
              <div v-if="realtimeAlert" class="realtime-banner mb-4 d-flex align-items-center justify-content-between p-3 rounded-3 shadow">
                <div class="d-flex align-items-center gap-2">
                  <span class="live-dot-pulse"></span>
                  <strong class="text-white">{{ realtimeAlert }}</strong>
                </div>
                <button class="btn btn-sm btn-link text-white-50 p-0 text-decoration-none" @click="realtimeAlert = ''">✕ Đóng</button>
              </div>
            </transition>

            <div class="section-heading-row mb-3">
              <div>
                <span class="eyebrow">Lịch hẹn ẩm thực của bạn</span>
                <div class="d-flex align-items-center gap-3 flex-wrap">
                  <h2 class="fs-4 fw-bold mb-0" style="color: #1F1A17;">Bàn Đã Đặt &amp; Trạng Thái</h2>
                  <span class="live-status-pill">
                    <span class="live-dot-pulse"></span>
                    <span>Đồng bộ trực tiếp Realtime</span>
                  </span>
                </div>
              </div>
              <router-link to="/customer/reservation" class="dola-btn-primary">+ Đặt bàn mới</router-link>
            </div>

            <!-- Bộ lọc trạng thái phiếu đặt bàn -->
            <div class="reservation-filter-bar d-flex flex-wrap gap-2 mb-4">
              <button
                type="button"
                class="res-filter-chip"
                :class="{ active: resFilter === 'all' }"
                @click="resFilter = 'all'"
              >
                🌟 Tất cả ({{ account.reservations.length }})
              </button>
              <button
                type="button"
                class="res-filter-chip"
                :class="{ active: resFilter === 'upcoming' }"
                @click="resFilter = 'upcoming'"
              >
                ⏳ Sắp tới &amp; Chờ duyệt ({{ upcomingCount }})
              </button>
              <button
                type="button"
                class="res-filter-chip"
                :class="{ active: resFilter === 'dining' }"
                @click="resFilter = 'dining'"
              >
                🍽 Đang dùng bữa ({{ diningCount }})
              </button>
              <button
                type="button"
                class="res-filter-chip"
                :class="{ active: resFilter === 'completed' }"
                @click="resFilter = 'completed'"
              >
                ✅ Hoàn tất ({{ completedCount }})
              </button>
              <button
                type="button"
                class="res-filter-chip"
                :class="{ active: resFilter === 'cancelled' }"
                @click="resFilter = 'cancelled'"
              >
                ✕ Đã hủy ({{ cancelledCount }})
              </button>
            </div>

            <!-- Empty State nếu không có kết quả -->
            <div v-if="!filteredReservations.length" class="empty-state py-5 text-center">
              <span class="fs-1">🪷</span>
              <h5 class="fw-bold mt-2" style="color: #1F1A17;">Chưa có phiếu đặt bàn nào trong mục này</h5>
              <p class="text-body-dola small mb-3">Quý khách có thể gửi yêu cầu đặt bàn trực tuyến để nhà hàng giữ vị trí đẹp nhất.</p>
              <router-link to="/customer/reservation" class="dola-btn-primary">Đặt bàn trải nghiệm ngay</router-link>
            </div>

            <!-- Danh Sách Thẻ Vé Đặt Bàn Cao Cấp (Luxury Dining Passes) -->
            <div v-else class="row g-4">
              <div v-for="item in filteredReservations" :key="item.reservationId" class="col-12">
                <article
                  class="luxury-dining-pass"
                  :class="{
                    'pass-just-updated': lastUpdatedReservationId === item.reservationId,
                    [`pass-status-${item.reservationStatus}`]: true
                  }"
                >
                  <!-- Card Header: Mã phiếu & Trạng thái -->
                  <div class="pass-header d-flex flex-wrap align-items-center justify-content-between gap-3">
                    <div class="d-flex align-items-center gap-2">
                      <span class="pass-lotus-icon">🪷</span>
                      <div>
                        <span class="pass-code">PHIẾU ĐẶT BÀN #RES-{{ item.reservationId }}</span>
                        <small class="text-white-50 d-block">Gửi lúc {{ formatDateTime(item.createdAt) }}</small>
                      </div>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                      <span :class="['luxury-status-badge', `status-${item.reservationStatus}`]">
                        <span class="status-icon">{{ reservationIcon(item.reservationStatus) }}</span>
                        <span>{{ reservationLabel(item.reservationStatus) }}</span>
                      </span>
                    </div>
                  </div>

                  <!-- Card Body: Lịch, Bàn, Số Khách & Ghi chú -->
                  <div class="pass-body">
                    <div class="row g-3 align-items-stretch">
                      <!-- Khối Thời Gian Dùng Bữa Nổi Bật -->
                      <div class="col-12 col-md-5 col-lg-4">
                        <div class="pass-highlight-box h-100">
                          <div class="d-flex justify-content-between align-items-start mb-2">
                            <span class="box-eyebrow">Thời gian dùng bữa</span>
                            <span class="timing-badge">{{ getReservationRelativeTime(item.reservationDate) }}</span>
                          </div>
                          <div class="pass-time-display text-gold fw-bold">
                            {{ extractTime(item.reservationDate) }}
                          </div>
                          <div class="pass-date-display text-white">
                            {{ extractDate(item.reservationDate) }}
                          </div>
                        </div>
                      </div>

                      <!-- Khối Thông Tin Chi Tiết -->
                      <div class="col-12 col-md-7 col-lg-8">
                        <div class="pass-info-grid h-100 d-flex flex-column justify-content-between">
                          <div class="row g-3">
                            <div class="col-12 col-sm-6">
                              <div class="pass-info-cell">
                                <span class="cell-label">👥 Số lượng khách</span>
                                <strong class="cell-value text-white fs-5">{{ item.guestCount }} Khách</strong>
                                <small class="text-white-50 d-block">Phục vụ theo tiêu chuẩn bàn tiệc</small>
                              </div>
                            </div>
                            <div class="col-12 col-sm-6">
                              <div class="pass-info-cell">
                                <span class="cell-label">🪑 Vị trí bàn &amp; Khu vực</span>
                                <template v-if="item.tableName">
                                  <strong class="cell-value text-gold fs-5">{{ item.tableName }}</strong>
                                  <small class="text-white-50 d-block">{{ item.areaName || 'Sảnh tiệc chính' }}</small>
                                </template>
                                <template v-else>
                                  <strong class="cell-value text-white-50 fs-6">Đang xếp bàn</strong>
                                  <small class="text-gold d-block">Lễ tân sắp xếp vị trí đẹp nhất</small>
                                </template>
                              </div>
                            </div>
                          </div>

                          <!-- Ghi chú nếu có -->
                          <div v-if="item.note" class="pass-note-quote mt-3">
                            <span class="quote-icon">💬</span>
                            <span class="quote-text fst-italic">"{{ item.note }}"</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- Card Footer: Hành động -->
                  <div class="pass-footer d-flex flex-wrap align-items-center justify-content-between gap-3">
                    <div class="d-flex align-items-center gap-3 small text-white-50">
                      <span v-if="['pending', 'confirmed'].includes(item.reservationStatus)" class="d-inline-flex align-items-center gap-1 text-gold">
                        <span class="live-dot-pulse"></span> Đang chờ tiếp đón
                      </span>
                      <span>Hotline hỗ trợ: <strong class="text-gold">1900 6868</strong></span>
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                      <button
                        v-if="canCancel(item)"
                        type="button"
                        class="btn btn-sm btn-outline-danger px-3 d-inline-flex align-items-center gap-1"
                        @click="cancelReservation(item)"
                      >
                        <span>✕</span>
                        <span>Hủy đặt bàn</span>
                      </button>
                      <router-link to="/customer/menu" class="btn btn-sm dola-btn-outline px-3 d-inline-flex align-items-center gap-1">
                        <span>📖</span>
                        <span>Xem thực đơn</span>
                      </router-link>
                      <button type="button" class="btn btn-sm dola-btn-primary px-3 d-inline-flex align-items-center gap-1" @click="openAiAssistant">
                        <span>✨</span>
                        <span>Tư vấn món tiệc (AI)</span>
                      </button>
                    </div>
                  </div>
                </article>
              </div>
            </div>
          </section>

          <section v-else-if="activeTab === 'invoices'" class="dola-card p-4 content-panel">
            <div class="section-heading-row"><div><span class="eyebrow">Lịch sử chi tiêu</span><h2 class="fs-4 fw-bold mb-0" style="color: #1F1A17;">Hóa đơn đã thanh toán</h2></div></div>
            <div v-if="!account.invoices.length" class="empty-state"><span>▣</span><p>Chưa có hóa đơn gắn với tài khoản này.</p></div>
            <div v-else class="invoice-list">
              <article v-for="invoice in account.invoices" :key="invoice.invoiceId" class="invoice-item">
                <div>
                  <strong style="color: #1F1A17;">{{ invoice.invoiceCode }}</strong>
                  <p>{{ formatDateTime(invoice.issuedAt) }} · {{ invoice.items.length }} món</p>
                  <div class="invoice-items"><span v-for="item in invoice.items.slice(0, 3)" :key="`${invoice.invoiceId}-${item.foodNameSnapshot}-${item.variantSnapshot}`">{{ item.quantity }}x {{ item.foodNameSnapshot }}</span></div>
                </div>
                <div class="invoice-actions"><strong style="color: var(--dola-wine); font-weight: 800;">{{ formatCurrency(invoice.totalAmount) }}</strong><button type="button" class="invoice-detail-link" @click="selectedInvoice = invoice">Xem bill</button></div>
              </article>
            </div>
          </section>

          <section v-else class="row g-4">
            <div class="col-12 col-lg-7"><article class="dola-card p-4 content-panel h-100"><div class="section-heading-row"><div><span class="eyebrow">Điểm thưởng</span><h2 class="fs-4 fw-bold mb-0" style="color: #1F1A17;">Biến động điểm</h2></div><strong class="text-gold fs-4">{{ account.pointsBalance }}</strong></div><div v-if="!account.pointHistory.length" class="empty-state py-5"><span>✦</span><p>Chưa có giao dịch điểm.</p></div><div v-else class="point-list"><div v-for="point in account.pointHistory" :key="point.pointTransactionId" class="point-item"><div><strong style="color: #1F1A17;">{{ point.note || 'Giao dịch điểm' }}</strong><small>{{ formatDateTime(point.createdAt) }}</small></div><b :class="point.points >= 0 ? 'point-plus' : 'point-minus'">{{ point.points >= 0 ? '+' : '' }}{{ point.points }}</b></div></div></article></div>
            <div class="col-12 col-lg-5"><article class="dola-card p-4 content-panel h-100"><span class="eyebrow">Cá nhân hóa</span><h2 class="fs-4 fw-bold mb-2" style="color: #1F1A17;">Màu giao diện</h2><p class="text-body-dola small">Chọn màu nhận diện phù hợp. Cài đặt được lưu trên thiết bị này.</p><div class="accent-picker"><button v-for="color in accents" :key="color" type="button" :style="{ backgroundColor: color }" :class="['accent-swatch', { selected: accentColor === color }]" :aria-label="`Chọn màu ${color}`" @click="setAccent(color)"></button></div><div class="mini-preferences mt-4"><div><span>Trạng thái tài khoản</span><strong class="text-success">Đang hoạt động</strong></div><div><span>Phương thức đặt bàn</span><strong style="color: #1F1A17;">Cần nhà hàng xác nhận</strong></div><div><span>Đặt món trước</span><strong class="text-gold">Chưa mở</strong></div></div></article></div>
          </section>

          <div v-if="selectedInvoice" class="bill-modal-backdrop" @click.self="selectedInvoice = null">
            <article class="bill-modal" role="dialog" aria-modal="true" aria-label="Chi tiết hóa đơn">
              <header class="bill-header">
                <div>
                  <span class="eyebrow">Hóa đơn thanh toán</span>
                  <h2>{{ selectedInvoice.invoiceCode }}</h2>
                </div>
                <button type="button" class="bill-close" aria-label="Đóng" @click="selectedInvoice = null">×</button>
              </header>

              <div class="bill-body">
                <div class="bill-brand">
                  <strong>NHÀ HÀNG HƯƠNG SEN</strong>
                  <span>Tinh hoa ẩm thực Việt</span>
                </div>

                <dl class="bill-meta">
                  <div><dt>Ngày thanh toán</dt><dd>{{ formatDateTime(selectedInvoice.paidAt || selectedInvoice.issuedAt) }}</dd></div>
                  <div><dt>Bàn phục vụ</dt><dd>{{ selectedInvoice.tableName || '—' }}</dd></div>
                  <div><dt>Khu vực</dt><dd>{{ selectedInvoice.areaName || '—' }}</dd></div>
                  <div><dt>Thu ngân</dt><dd>{{ selectedInvoice.cashierName || '—' }}</dd></div>
                  <div><dt>Phương thức</dt><dd>{{ paymentMethodLabel(selectedInvoice.paymentMethod) }}</dd></div>
                  <div><dt>Trạng thái</dt><dd class="bill-paid">{{ invoiceStatusLabel(selectedInvoice.invoiceStatus) }}</dd></div>
                </dl>

                <div class="bill-item-heading">
                  <span>Món ăn</span><span>Thành tiền</span>
                </div>
                <div class="bill-items">
                  <div v-for="item in selectedInvoice.items" :key="item.invoiceItemId || `${selectedInvoice.invoiceId}-${item.foodNameSnapshot}-${item.variantSnapshot}`">
                    <span class="bill-item-name">
                      <b>{{ item.quantity }} × {{ item.foodNameSnapshot }}</b>
                      <small>{{ item.variantSnapshot || 'Tiêu chuẩn' }} · {{ formatCurrency(item.unitPrice) }}/phần</small>
                      <small v-if="item.note">Ghi chú: {{ item.note }}</small>
                    </span>
                    <strong>{{ formatCurrency(item.lineTotal) }}</strong>
                  </div>
                </div>

                <div class="bill-summary">
                  <div><span>Tạm tính</span><strong>{{ formatCurrency(selectedInvoice.subtotal || selectedInvoice.totalAmount) }}</strong></div>
                  <div v-if="selectedInvoice.discountAmount > 0" class="bill-discount"><span>Tổng giảm trừ</span><strong>-{{ formatCurrency(selectedInvoice.discountAmount) }}</strong></div>
                  <div v-if="selectedInvoice.pointsUsed > 0"><span>Điểm đã sử dụng</span><strong>{{ selectedInvoice.pointsUsed }} điểm</strong></div>
                  <div class="bill-total"><span>Thành tiền</span><strong>{{ formatCurrency(selectedInvoice.totalAmount) }}</strong></div>
                  <div v-if="selectedInvoice.pointsEarned > 0" class="bill-earned"><span>Điểm được cộng</span><strong>+{{ selectedInvoice.pointsEarned }} điểm</strong></div>
                </div>

                <p v-if="selectedInvoice.transactionCode" class="bill-transaction">Mã giao dịch: {{ selectedInvoice.transactionCode }}</p>
                <p class="bill-thanks">Cảm ơn quý khách và hẹn gặp lại.</p>
              </div>

              <footer class="bill-actions">
                <button type="button" class="dola-btn-outline" style="color: #f59e0b; border-color: #f59e0b;" @click="openReviewModal(selectedInvoice)">⭐ Đánh giá</button>
                <button type="button" class="dola-btn-outline" @click="printBill">In hóa đơn</button>
                <button type="button" class="dola-btn-primary" @click="selectedInvoice = null">Đóng</button>
              </footer>
            </article>
          </div>

          <!-- Modal Đánh Giá Bữa Ăn -->
          <div v-if="showReviewModal" class="bill-modal-backdrop" @click.self="showReviewModal = false">
            <div class="card p-4 shadow-lg" style="max-width: 450px; width: 90%; background: #1c1917; color: #fff; border: 1px solid #d69c52; border-radius: 14px;">
              <h5 class="fw-bold mb-3" style="color: #d69c52;">⭐ Đánh Giá Bữa Ăn Hương Sen</h5>
              <div class="mb-3">
                <label class="form-label small text-white-50">Mức độ hài lòng của quý khách</label>
                <div class="d-flex gap-2 fs-2" style="cursor: pointer; user-select: none;">
                  <span v-for="star in 5" :key="star" @click="reviewForm.rating = star" :style="{ color: star <= reviewForm.rating ? '#f59e0b' : '#57534e' }">
                    ★
                  </span>
                </div>
              </div>
              <div class="mb-3">
                <label class="form-label small text-white-50">Cảm nhận và góp ý về món ăn / không gian / phục vụ</label>
                <textarea v-model="reviewForm.comment" rows="3" class="form-control" style="background: #292524; color: #fff; border: 1px solid #44403c;" placeholder="Món ăn vừa miệng, nước lẩu đậm đà, phục vụ chu đáo..."></textarea>
              </div>
              <div class="d-flex justify-content-end gap-2 border-top pt-3 border-secondary">
                <button class="btn btn-sm btn-outline-light" @click="showReviewModal = false">Đóng</button>
                <button class="btn btn-sm btn-warning fw-bold px-3 text-dark" :disabled="isSubmittingReview" @click="submitCustomerReview">
                  Gửi Đánh Giá
                </button>
              </div>
            </div>
          </div>
        </template>
      </div>
    </section>
  </div>
</template>

<script setup>
import { clearSession } from '@/auth/session';
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';
import signalRService from '@/services/signalr';

const route = useRoute();
const router = useRouter();
const API_BASE = 'http://localhost:5079/api';
const token = localStorage.getItem('token');
const loading = ref(true);
const saving = ref(false);
const showEdit = ref(false);
const errorMessage = ref('');

function handleLogout() {
  if (confirm('Quý khách có chắc chắn muốn đăng xuất khỏi tài khoản?')) {
    clearSession();
    router.push('/customer/intro');
  }
}

const activeTab = ref(
  route.query.tab && ['reservations', 'invoices', 'points'].includes(route.query.tab)
    ? route.query.tab
    : 'reservations'
);

watch(() => route.query.tab, (newTab) => {
  if (newTab && ['reservations', 'invoices', 'points'].includes(newTab)) {
    activeTab.value = newTab;
  }
});

function switchTab(key) {
  activeTab.value = key;
  router.replace({ query: { ...route.query, tab: key } });
}

const account = ref(null);
const selectedInvoice = ref(null);
const isLiveSyncing = ref(false);
const editForm = reactive({ fullName: '', phone: '', email: '' });
const accents = ['#D69C52', '#C97852', '#8FAF7A', '#B795C9'];
const accentColor = ref(localStorage.getItem('customer_accent') || accents[0]);

// Realtime & Filter States
const resFilter = ref('all');
const realtimeAlert = ref('');
const lastUpdatedReservationId = ref(null);
let alertTimer = null;

const upcomingCount = computed(() => {
  return (account.value?.reservations || []).filter(r => ['pending', 'confirmed'].includes(r.reservationStatus)).length;
});
const diningCount = computed(() => {
  return (account.value?.reservations || []).filter(r => r.reservationStatus === 'checked_in').length;
});
const completedCount = computed(() => {
  return (account.value?.reservations || []).filter(r => r.reservationStatus === 'completed').length;
});
const cancelledCount = computed(() => {
  return (account.value?.reservations || []).filter(r => ['cancelled', 'rejected', 'no_show'].includes(r.reservationStatus)).length;
});

const filteredReservations = computed(() => {
  const list = account.value?.reservations || [];
  if (resFilter.value === 'upcoming') {
    return list.filter(r => ['pending', 'confirmed'].includes(r.reservationStatus));
  }
  if (resFilter.value === 'dining') {
    return list.filter(r => r.reservationStatus === 'checked_in');
  }
  if (resFilter.value === 'completed') {
    return list.filter(r => r.reservationStatus === 'completed');
  }
  if (resFilter.value === 'cancelled') {
    return list.filter(r => ['cancelled', 'rejected', 'no_show'].includes(r.reservationStatus));
  }
  return list;
});

function handleReservationLiveUpdate(data) {
  console.log('SignalR ReservationUpdated event received:', data);
  scheduleAccountRefresh();
  const id = data?.reservationId;
  if (id) {
    lastUpdatedReservationId.value = id;
    setTimeout(() => {
      if (lastUpdatedReservationId.value === id) {
        lastUpdatedReservationId.value = null;
      }
    }, 8000);
  }
  const eventDesc = data?.eventType === 'confirmed' ? 'đã được xác nhận'
    : data?.eventType === 'table_assigned' ? 'đã được xếp bàn'
    : data?.eventType === 'checked_in' ? 'đã vào bàn dùng bữa'
    : data?.eventType === 'cancelled' ? 'đã hủy'
    : 'đã có cập nhật mới';
  realtimeAlert.value = `🔔 Phiếu đặt bàn ${id ? '#' + id : ''} vừa ${eventDesc} trực tiếp từ nhà hàng!`;
  window.clearTimeout(alertTimer);
  alertTimer = window.setTimeout(() => {
    realtimeAlert.value = '';
  }, 6000);
}

function extractTime(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function extractDate(dateStr) {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });
}

function getReservationRelativeTime(dateStr) {
  if (!dateStr) return '';
  const target = new Date(dateStr);
  const now = new Date();
  const startOfTarget = new Date(target.getFullYear(), target.getMonth(), target.getDate());
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const diffDays = Math.round((startOfTarget - startOfToday) / (1000 * 60 * 60 * 24));
  if (diffDays === 0) return '🔥 Hôm nay';
  if (diffDays === 1) return '⚡ Ngày mai';
  if (diffDays > 1 && diffDays <= 7) return `⏳ Còn ${diffDays} ngày`;
  if (diffDays > 7) return '📅 Sắp tới';
  return 'Đã qua';
}

function reservationIcon(status) {
  return ({
    pending: '⏳',
    confirmed: '✨',
    checked_in: '🍽',
    completed: '✅',
    cancelled: '✕',
    rejected: '✕',
    no_show: '✕'
  }[status] || '📋');
}

function openAiAssistant() {
  const trigger = document.querySelector('.ai-widget-wrapper .ai-launcher')
    || document.querySelector('.ai-floating-btn')
    || document.querySelector('.ai-consultant-widget');
  if (trigger) {
    trigger.click();
  } else {
    router.push('/customer/menu');
  }
}

const showReviewModal = ref(false);
const isSubmittingReview = ref(false);
const reviewForm = reactive({
  invoiceId: null,
  rating: 5,
  comment: ''
});

function openReviewModal(invoice) {
  reviewForm.invoiceId = invoice?.invoiceId || null;
  reviewForm.rating = 5;
  reviewForm.comment = '';
  showReviewModal.value = true;
}

async function submitCustomerReview() {
  isSubmittingReview.value = true;
  try {
    const res = await axios.post(`${API_BASE}/reviews/create`, {
      invoiceId: reviewForm.invoiceId,
      rating: reviewForm.rating,
      comment: reviewForm.comment
    }, {
      headers: token ? { Authorization: `Bearer ${token}` } : {}
    });
    if (res.data?.success) {
      alert(res.data.message || 'Cảm ơn quý khách đã gửi đánh giá!');
      showReviewModal.value = false;
    }
  } catch (err) {
    alert('Lỗi: ' + (err.response?.data?.message || err.message));
  } finally {
    isSubmittingReview.value = false;
  }
}

const metrics = computed(() => account.value ? [
  { icon: '✦', value: account.value.pointsBalance.toLocaleString('vi-VN'), label: 'Điểm tích lũy' },
  { icon: '◷', value: account.value.reservations.length, label: 'Đặt bàn' },
  { icon: '▣', value: account.value.invoices.length, label: 'Hóa đơn' },
  { icon: '◉', value: formatCurrency(account.value.totalSpent), label: 'Đã chi tiêu' },
] : []);
const initials = computed(() => (account.value?.fullName || 'KH').split(' ').map(v => v[0]).slice(-2).join('').toUpperCase());
const progressPercent = computed(() => Math.min(100, (account.value?.pointsBalance || 0) % 100));
const pointsToNextLevel = computed(() => 100 - ((account.value?.pointsBalance || 0) % 100));
const tabs = computed(() => [
  { key: 'reservations', icon: '🗓', label: 'Đặt bàn', count: account.value?.reservations.length || 0 },
  { key: 'invoices', icon: '▣', label: 'Hóa đơn', count: account.value?.invoices.length || 0 },
  { key: 'points', icon: '✦', label: 'Điểm thưởng', count: account.value?.pointsBalance || 0 },
]);

async function loadAccount() {
  loading.value = true;
  errorMessage.value = '';
  if (!token) {
    loading.value = false;
    errorMessage.value = 'Đăng nhập để quản lý tài khoản và xem lịch sử cá nhân.';
    return;
  }
  try {
    const { data } = await axios.get(`${API_BASE}/customer/account`);
    applyAccount(data.data, true);
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể tải thông tin tài khoản.';
  } finally {
    loading.value = false;
  }
}

function applyAccount(source, syncForm = false) {
  account.value = {
    ...source.account,
    pointsBalance: source.account.pointsBalance || 0,
    totalSpent: source.account.totalSpent || 0,
    reservations: source.reservations || [],
    invoices: source.invoices || [],
    pointHistory: source.pointHistory || [],
  };
  if (syncForm) {
    editForm.fullName = account.value.fullName;
    editForm.phone = account.value.phone;
    editForm.email = account.value.email || '';
    showEdit.value = account.value.customerId === 0;
  }
  localStorage.setItem('fullName', account.value.fullName);
}

async function refreshAccountSilently() {
  if (!token || isLiveSyncing.value) return;
  isLiveSyncing.value = true;
  try {
    const { data } = await axios.get(`${API_BASE}/customer/account`);
    applyAccount(data.data, !showEdit.value);
  } catch (error) {
    console.warn('Không thể đồng bộ tài khoản:', error);
  } finally {
    isLiveSyncing.value = false;
  }
}

let refreshTimer;
function scheduleAccountRefresh() {
  window.clearTimeout(refreshTimer);
  refreshTimer = window.setTimeout(refreshAccountSilently, 350);
}

async function saveAccount() {
  saving.value = true;
  errorMessage.value = '';
  try {
    await axios.put(`${API_BASE}/customer/account`, editForm);
    await loadAccount();
    showEdit.value = false;
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể lưu thông tin.';
  } finally {
    saving.value = false;
  }
}

async function cancelReservation(item) {
  const reason = window.prompt('Lý do hủy đặt bàn (không bắt buộc):', 'Thay đổi kế hoạch cá nhân');
  if (reason === null) return;
  try {
    await axios.post(`${API_BASE}/customer/reservations/${item.reservationId}/cancel`, { reason, phone: account.value.phone });
    await loadAccount();
    activeTab.value = 'reservations';
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Không thể hủy phiếu đặt bàn.';
  }
}

function canCancel(item) {
  return ['pending', 'confirmed'].includes(item.reservationStatus) && new Date(item.reservationDate) > new Date();
}
function setAccent(color) { accentColor.value = color; localStorage.setItem('customer_accent', color); }
function formatDate(value) { return value ? new Date(value).toLocaleDateString('vi-VN') : '—'; }
function formatDateTime(value) { return value ? new Date(value).toLocaleString('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }) : '—'; }
function formatCurrency(value) { return `${Number(value || 0).toLocaleString('vi-VN')} đ`; }
function reservationLabel(status) { return ({ pending: 'Chờ xác nhận', confirmed: 'Đã xác nhận & Giữ bàn', checked_in: 'Đang dùng bữa tại bàn', completed: 'Hoàn tất bữa tiệc', cancelled: 'Đã hủy', rejected: 'Từ chối', no_show: 'Không đến' }[status] || status); }
function paymentMethodLabel(method) { return ({ cash: 'Tiền mặt', transfer: 'Chuyển khoản', bank_transfer: 'Chuyển khoản', card: 'Thẻ ngân hàng', e_wallet: 'Ví điện tử' }[method] || method || 'Chưa cập nhật'); }
function invoiceStatusLabel(status) { return ({ paid: 'Đã thanh toán', completed: 'Đã thanh toán', pending: 'Chờ thanh toán', cancelled: 'Đã hủy' }[status] || status || 'Đã thanh toán'); }
function printBill() { window.print(); }

onMounted(() => {
  loadAccount();
  signalRService.on('AccountUpdated', scheduleAccountRefresh);
});

onUnmounted(() => {
  window.clearTimeout(refreshTimer);
  window.clearTimeout(alertTimer);
  signalRService.off('AccountUpdated', scheduleAccountRefresh);
});
</script>

<style scoped>
.account-hero { background: linear-gradient(135deg, var(--dola-surface-alt), var(--dola-canvas)); }
.account-title { font-size: clamp(2.2rem, 5vw, 3.6rem); }
.account-live-status { display: inline-flex; align-items: center; gap: 7px; min-height: 42px; padding: 0 12px; color: var(--dola-body); font-size: .76rem; font-weight: 700; white-space: nowrap; }
.account-live-status i { width: 8px; height: 8px; border-radius: 50%; background: #72c59a; box-shadow: 0 0 0 4px rgba(114, 197, 154, .13); }
.account-live-status i.syncing { animation: livePulse .75s infinite alternate; }

/* Realtime Toast Banner */
.realtime-banner {
  background: linear-gradient(135deg, rgba(214, 156, 82, 0.22), rgba(16, 185, 129, 0.18));
  border: 1px solid rgba(214, 156, 82, 0.6);
  border-radius: 12px;
  animation: bannerSlideDown 0.35s ease;
}
@keyframes bannerSlideDown {
  from { opacity: 0; transform: translateY(-10px); }
  to { opacity: 1; transform: translateY(0); }
}

.live-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: rgba(16, 185, 129, 0.12);
  border: 1px solid rgba(16, 185, 129, 0.35);
  color: #34d399;
  font-size: 0.74rem;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 999px;
}
.live-dot-pulse {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #10b981;
  box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7);
  animation: pulseGlow 1.8s infinite;
}
@keyframes pulseGlow {
  0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
  70% { transform: scale(1); box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
  100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}

/* Reservation Filter Chips */
.reservation-filter-bar {
  border-bottom: 1px solid var(--dola-hairline);
  padding-bottom: 14px;
}
.res-filter-chip {
  padding: 6px 14px;
  border-radius: 9999px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--dola-hairline);
  color: var(--dola-body);
  font-size: 0.8rem;
  font-weight: 600;
  transition: all 0.2s ease;
  cursor: pointer;
}
.res-filter-chip:hover {
  background: rgba(214, 156, 82, 0.12);
  color: #fff;
  border-color: rgba(214, 156, 82, 0.3);
}
.res-filter-chip.active {
  background: var(--dola-primary);
  color: #1a1611;
  font-weight: 700;
  border-color: var(--dola-primary);
  box-shadow: 0 4px 12px rgba(214, 156, 82, 0.35);
}

/* Luxury Dining Pass Card */
.luxury-dining-pass {
  position: relative;
  background: linear-gradient(135deg, #1c1813 0%, #171410 100%);
  border: 1px solid rgba(214, 156, 82, 0.25);
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.45);
  transition: all 0.25s ease;
}
.luxury-dining-pass:hover {
  border-color: rgba(214, 156, 82, 0.55);
  box-shadow: 0 14px 38px rgba(0, 0, 0, 0.6), 0 0 20px rgba(214, 156, 82, 0.12);
  transform: translateY(-2px);
}

/* Updated pulse effect when realtime notification fires */
.pass-just-updated {
  animation: updatedCardPulse 2s ease infinite alternate;
  border-color: #f59e0b !important;
}
@keyframes updatedCardPulse {
  0% { box-shadow: 0 0 12px rgba(245, 158, 11, 0.3); }
  100% { box-shadow: 0 0 26px rgba(245, 158, 11, 0.8); }
}

.pass-header {
  padding: 14px 20px;
  background: rgba(214, 156, 82, 0.06);
  border-bottom: 1px dashed rgba(214, 156, 82, 0.2);
}
.pass-lotus-icon {
  font-size: 1.4rem;
}
.pass-code {
  font-weight: 800;
  letter-spacing: 0.06em;
  color: var(--dola-primary);
  font-size: 0.88rem;
}

.luxury-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 700;
  white-space: nowrap;
}
.status-pending {
  background: rgba(245, 158, 11, 0.16);
  color: #fbbf24;
  border: 1px solid rgba(245, 158, 11, 0.4);
}
.status-confirmed {
  background: rgba(16, 185, 129, 0.16);
  color: #34d399;
  border: 1px solid rgba(16, 185, 129, 0.4);
}
.status-checked_in {
  background: rgba(168, 85, 247, 0.16);
  color: #c084fc;
  border: 1px solid rgba(168, 85, 247, 0.4);
}
.status-completed {
  background: rgba(59, 130, 246, 0.16);
  color: #60a5fa;
  border: 1px solid rgba(59, 130, 246, 0.4);
}
.status-cancelled, .status-rejected, .status-no_show {
  background: rgba(239, 68, 68, 0.16);
  color: #f87171;
  border: 1px solid rgba(239, 68, 68, 0.4);
}

.pass-body {
  padding: 20px;
}
.pass-highlight-box {
  background: rgba(0, 0, 0, 0.35);
  border: 1px solid rgba(214, 156, 82, 0.18);
  border-radius: 12px;
  padding: 16px;
}
.box-eyebrow {
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--dola-body);
  font-weight: 700;
}
.timing-badge {
  font-size: 0.72rem;
  font-weight: 700;
  background: rgba(214, 156, 82, 0.15);
  color: var(--dola-primary);
  padding: 2px 8px;
  border-radius: 6px;
}
.pass-time-display {
  font-size: 2rem;
  line-height: 1.1;
  margin: 6px 0 2px;
}
.pass-date-display {
  font-size: 0.88rem;
  font-weight: 500;
  opacity: 0.85;
}

.pass-info-grid {
  padding: 4px 6px;
}
.pass-info-cell {
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 10px;
  padding: 12px 14px;
}
.cell-label {
  display: block;
  font-size: 0.74rem;
  color: var(--dola-body);
  margin-bottom: 2px;
}
.cell-value {
  display: block;
  font-weight: 700;
}

.pass-note-quote {
  background: rgba(214, 156, 82, 0.07);
  border-left: 3px solid var(--dola-primary);
  border-radius: 0 8px 8px 0;
  padding: 8px 12px;
  font-size: 0.82rem;
  color: #e5ded3;
  display: flex;
  align-items: center;
  gap: 8px;
}
.quote-icon {
  font-size: 1rem;
}

.pass-footer {
  padding: 14px 20px;
  background: rgba(0, 0, 0, 0.25);
  border-top: 1px solid rgba(214, 156, 82, 0.12);
}

.account-profile-card, .metric-card, .member-progress-card, .content-panel, .edit-panel { position: relative; overflow: hidden; }
.profile-cover { height: 70px; background: linear-gradient(110deg, var(--dola-primary), rgba(214, 156, 82, .12)); opacity: .75; }
.profile-body { margin-top: -38px; position: relative; }
.account-avatar { width: 76px; height: 76px; display: grid; place-items: center; border-radius: 22px; background: var(--dola-canvas); border: 3px solid var(--dola-primary); color: var(--dola-primary); font-size: 1.5rem; font-weight: 800; box-shadow: 0 8px 24px rgba(0,0,0,.28); }
.member-status { display: inline-flex; align-items: center; padding: 4px 9px; border-radius: 999px; background: rgba(214,156,82,.14); color: var(--dola-primary); font-size: .72rem; font-weight: 700; white-space: nowrap; }
.profile-contact { display: grid; gap: 10px; border-top: 1px solid var(--dola-hairline); padding-top: 16px; }
.profile-contact div, .mini-preferences div { display: flex; justify-content: space-between; gap: 12px; font-size: .8rem; }
.profile-contact span, .mini-preferences span { color: var(--dola-muted); }
.profile-contact strong, .mini-preferences strong { color: var(--dola-ink); text-align: right; }
.metric-card { display: flex; flex-direction: column; justify-content: center; gap: 4px; padding: 18px; }
.metric-card strong { color: var(--dola-ink); font-size: clamp(1.05rem, 2vw, 1.5rem); overflow-wrap: anywhere; }
.metric-card > span:last-child { color: var(--dola-muted); font-size: .75rem; }
.metric-icon { color: var(--dola-primary); font-size: 1.35rem; }
.eyebrow { display: block; color: var(--dola-primary); font-size: .68rem; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; margin-bottom: 4px; }
.progress-track { height: 8px; border-radius: 99px; background: var(--dola-canvas); overflow: hidden; }
.progress-track span { display: block; height: 100%; border-radius: inherit; background: var(--dola-primary); transition: width .7s ease; }
.account-tabs { display: flex; gap: 6px; overflow-x: auto; scrollbar-width: none; }
.account-tabs::-webkit-scrollbar { display: none; }
.account-tab { flex: 1 0 auto; min-height: 44px; padding: 9px 15px; border: 1px solid transparent; border-radius: 7px; background: transparent; color: var(--dola-body); font: inherit; font-weight: 700; white-space: nowrap; transition: .2s ease; }
.account-tab small { margin-left: 6px; color: var(--dola-muted); }
.account-tab:hover, .account-tab.active { color: var(--dola-primary); border-color: var(--dola-primary); background: rgba(214,156,82,.09); }
.section-heading-row { display: flex; justify-content: space-between; align-items: center; gap: 18px; margin-bottom: 22px; }

.invoice-list, .point-list { display: grid; gap: 10px; }
.invoice-item, .point-item { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; padding: 15px; border: 1px solid var(--dola-hairline); border-radius: 7px; background: var(--dola-canvas); }
.invoice-actions { display: grid; justify-items: end; gap: 7px; white-space: nowrap; }
.invoice-detail-link { border: 0; background: transparent; color: var(--dola-primary); padding: 0; font: inherit; font-size: .76rem; font-weight: 800; cursor: pointer; }
.invoice-detail-link:hover { text-decoration: underline; }
.invoice-items { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 10px; }
.invoice-items span { color: var(--dola-body); font-size: .76rem; padding: 3px 8px; border-radius: 4px; background: var(--dola-surface-alt); }
.point-item small { display: block; }
.point-plus { color: #8fd19e; }
.point-minus { color: #ec8e8e; }
.empty-state { display: grid; place-items: center; gap: 8px; padding: 42px 16px; color: var(--dola-muted); text-align: center; }
.empty-state span { color: var(--dola-primary); font-size: 2rem; }
.empty-state p { margin: 0 0 6px; }
.accent-picker { display: flex; gap: 12px; flex-wrap: wrap; }
.accent-swatch { width: 34px; height: 34px; border: 3px solid transparent; border-radius: 50%; cursor: pointer; box-shadow: 0 0 0 1px var(--dola-hairline); transition: transform .2s ease; }
.accent-swatch:hover, .accent-swatch.selected { transform: scale(1.15); border-color: var(--dola-ink); }
.dola-alert { display: flex; justify-content: space-between; align-items: center; gap: 16px; padding: 13px 16px; border: 1px solid rgba(236,142,142,.55); border-radius: 7px; color: #f4b5b5; background: rgba(130,34,34,.18); }
.alert-button { min-height: 36px; padding: 7px 14px; font-size: .8rem; }
.bill-modal-backdrop { position: fixed; z-index: 1100; inset: 0; display: grid; place-items: center; padding: 20px; background: rgba(5, 22, 20, .72); backdrop-filter: blur(4px); }
.bill-modal { width: min(680px, 100%); max-height: min(820px, 92vh); overflow: auto; border: 1px solid var(--dola-hairline); border-radius: 8px; background: var(--dola-surface-alt); box-shadow: 0 24px 60px rgba(0, 0, 0, .35); animation: billReveal .22s ease both; }
.bill-header, .bill-actions { display: flex; align-items: center; justify-content: space-between; gap: 20px; padding: 18px 22px; }
.bill-header { position: sticky; top: 0; z-index: 2; border-bottom: 1px solid var(--dola-hairline); background: var(--dola-surface-alt); }
.bill-header h2 { margin: 0; color: var(--dola-ink); font-size: 1.25rem; }
.bill-close { width: 34px; height: 34px; border: 1px solid var(--dola-hairline); border-radius: 7px; background: transparent; color: var(--dola-ink); font-size: 1.4rem; line-height: 1; cursor: pointer; }
.bill-body { padding: 20px 22px; }
.bill-brand { display: grid; justify-items: center; gap: 3px; padding-bottom: 18px; color: var(--dola-primary); text-align: center; }
.bill-brand strong { letter-spacing: .08em; }
.bill-brand span { color: var(--dola-muted); font-size: .76rem; text-transform: uppercase; }
.bill-meta { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px 24px; margin: 0 0 20px; padding: 14px; border: 1px solid var(--dola-hairline); border-radius: 7px; background: var(--dola-canvas); }
.bill-meta div { min-width: 0; }
.bill-meta dt { color: var(--dola-muted); font-size: .7rem; font-weight: 600; }
.bill-meta dd { margin: 2px 0 0; color: var(--dola-ink); font-size: .84rem; font-weight: 700; overflow-wrap: anywhere; }
.bill-meta .bill-paid { color: #72c59a; }
.bill-item-heading { display: flex; justify-content: space-between; gap: 16px; padding: 0 0 8px; border-bottom: 1px solid var(--dola-primary); color: var(--dola-muted); font-size: .7rem; font-weight: 800; text-transform: uppercase; }
.bill-items > div { display: flex; justify-content: space-between; gap: 18px; padding: 13px 0; border-bottom: 1px solid var(--dola-hairline); color: var(--dola-ink); font-size: .84rem; }
.bill-item-name { min-width: 0; }
.bill-items b { display: block; color: var(--dola-ink); }
.bill-items small { display: block; margin-top: 3px; color: var(--dola-muted); }
.bill-items > div > strong { color: var(--dola-ink); white-space: nowrap; }
.bill-summary { display: grid; gap: 8px; padding-top: 16px; }
.bill-summary > div { display: flex; justify-content: space-between; gap: 16px; color: var(--dola-body); font-size: .82rem; }
.bill-summary strong { color: var(--dola-ink); }
.bill-summary .bill-discount strong { color: #ec8e8e; }
.bill-summary .bill-total { margin-top: 4px; padding-top: 12px; border-top: 1px solid var(--dola-hairline); color: var(--dola-ink); font-size: 1rem; font-weight: 800; }
.bill-summary .bill-total strong { color: var(--dola-primary); font-size: 1.2rem; }
.bill-summary .bill-earned { color: #72c59a; }
.bill-summary .bill-earned strong { color: #72c59a; }
.bill-transaction, .bill-thanks { margin: 15px 0 0; color: var(--dola-muted); font-size: .72rem; text-align: center; overflow-wrap: anywhere; }
.bill-thanks { color: var(--dola-primary); font-weight: 700; }
.bill-actions { position: sticky; bottom: 0; justify-content: flex-end; border-top: 1px solid var(--dola-hairline); background: var(--dola-surface-alt); }
.reveal-card { animation: accountReveal .5s ease both; animation-delay: var(--delay, 0ms); }
@keyframes accountReveal { from { opacity: 0; transform: translateY(12px); } to { opacity: 1; transform: translateY(0); } }
@keyframes livePulse { to { transform: scale(1.35); opacity: .45; } }
@keyframes billReveal { from { opacity: 0; transform: translateY(12px) scale(.98); } to { opacity: 1; transform: translateY(0) scale(1); } }
@media (max-width: 767.98px) {
  .section-heading-row { align-items: flex-start; flex-direction: column; }
  .account-actions > * { flex: 1 1 150px; }
  .account-live-status { flex: 1 1 100%; justify-content: center; }
  .dola-alert { align-items: flex-start; flex-direction: column; }
  .invoice-item { flex-direction: column; }
  .invoice-actions { justify-items: start; }
  .profile-contact div, .mini-preferences div { align-items: flex-start; flex-direction: column; gap: 2px; }
  .profile-contact strong, .mini-preferences strong { text-align: left; }
  .bill-modal-backdrop { padding: 8px; }
  .bill-header, .bill-actions, .bill-body { padding: 15px; }
  .bill-meta { grid-template-columns: 1fr; }
  .bill-items > div { gap: 10px; }
}
@media print {
  :global(body *) { visibility: hidden !important; }
  .bill-modal-backdrop, .bill-modal-backdrop * { visibility: visible !important; }
  .bill-modal-backdrop { position: absolute; display: block; padding: 0; background: #fff; }
  .bill-modal { width: 100%; max-height: none; overflow: visible; border: 0; box-shadow: none; color: #111; }
  .bill-actions, .bill-close { display: none !important; }
}
@media (prefers-reduced-motion: reduce) { .reveal-card { animation: none; } }
</style>

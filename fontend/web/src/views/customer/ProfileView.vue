<template>
  <div class="dola-theme py-5">
    <div class="container py-3">

      <!-- Header -->
      <div class="text-center mb-5">
        <span class="dola-pill-badge mb-2">👤 Trung Tâm Thành Viên</span>
        <h1 class="dola-font-title text-gold display-4 fw-bold">Hồ Sơ &amp; Hóa Đơn Của Bạn</h1>
        <p class="text-body-dola mx-auto fs-5" style="max-width: 620px;">
          Tra cứu điểm thưởng tích lũy, quản lý lịch đặt bàn và xem lại các hóa đơn đã dùng bữa với chế độ in bill tiện lợi.
        </p>
      </div>

      <!-- Phone Lookup Section (if not looked up yet) -->
      <div v-if="!customerInfo" class="row justify-content-center mb-5">
        <div class="col-12 col-md-8 col-lg-6">
          <div class="dola-card p-4 p-md-5 text-center">
            <div class="fs-1 mb-3">🔍</div>
            <h3 class="dola-font-title text-gold fs-2 mb-2">Tra Cứu Thông Tin Thành Viên</h3>
            <p class="text-body-dola small mb-4">
              Nhập số điện thoại quý khách đã từng đặt bàn hoặc thanh toán tại nhà hàng để xem điểm tích lũy và danh sách hóa đơn.
            </p>

            <div class="d-flex gap-2 mb-3">
              <input
                type="tel"
                class="dola-input"
                placeholder="Nhập số điện thoại (ví dụ: 0987654321)..."
                v-model="phoneInput"
                @keyup.enter="lookupCustomer"
              />
              <button class="dola-btn-primary text-nowrap" @click="lookupCustomer" :disabled="lookingUp">
                <span v-if="lookingUp" class="spinner-border spinner-border-sm me-1"></span>
                Tra Cứu
              </button>
            </div>

            <!-- Quick sample buttons for instant test -->
            <div class="d-flex justify-content-center gap-2 flex-wrap mb-3">
              <span class="text-white-50 small align-self-center">Gợi ý số mẫu:</span>
              <button class="btn btn-sm btn-outline-secondary text-gold rounded-pill" @click="phoneInput = '0987654321'; lookupCustomer();">
                0987654321 (Có 2 bill)
              </button>
              <button class="btn btn-sm btn-outline-secondary text-gold rounded-pill" @click="phoneInput = '0933112233'; lookupCustomer();">
                0933112233
              </button>
            </div>

            <div v-if="lookupError" class="p-3 rounded-2 text-start small mt-3" style="background: rgba(220, 53, 69, 0.15); border: 1px solid rgba(220, 53, 69, 0.4); color: #f87171;">
              ⚠️ {{ lookupError }}
            </div>
          </div>
        </div>
      </div>

      <!-- Main Profile Dashboard (when customer is loaded) -->
      <div v-else class="row g-4">

        <!-- Left Column: Member Card -->
        <div class="col-12 col-lg-4">
          <div class="d-flex flex-column gap-4 sticky-top" style="top: 100px;">

            <!-- VIP Member Card -->
            <div class="dola-card overflow-hidden">
              <div class="p-4 text-center position-relative" style="background: linear-gradient(135deg, #10302C, #184741); border-bottom: 1px solid var(--dola-hairline);">
                <div class="dola-avatar-large mx-auto mb-3">
                  {{ customerInfo.customerName?.charAt(0) || '👤' }}
                </div>
                <h4 class="text-white fw-bold mb-1">{{ customerInfo.customerName }}</h4>
                <div class="text-gold small mb-2">📞 {{ customerInfo.phone }}</div>
                <span class="dola-pill-badge">
                  ⭐ Thành Viên Thân Thiết
                </span>
              </div>

              <!-- Points & Stats Box -->
              <div class="p-4">
                <div class="p-3 rounded-3 text-center mb-3" style="background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);">
                  <div class="small text-white-50 mb-1">Điểm Tích Lũy Khả Dụng</div>
                  <div class="dola-font-title text-gold display-5 fw-bold lh-1 mb-1">
                    {{ customerInfo.loyaltyPoints?.toLocaleString('vi-VN') || 0 }}
                  </div>
                  <div class="small text-body-dola">
                    Quy đổi: <strong class="text-gold">{{ formatCurrency((customerInfo.loyaltyPoints || 0) * 1000) }}</strong>
                  </div>
                </div>

                <div class="d-flex flex-column gap-2 small mb-4">
                  <div class="d-flex justify-content-between p-2 rounded-2" style="background: rgba(255,255,255,0.03); border: 1px solid var(--dola-hairline);">
                    <span class="text-white-50">Tổng số lần ghé:</span>
                    <strong class="text-white">{{ customerInfo.totalOrders || 0 }} lần</strong>
                  </div>
                  <div class="d-flex justify-content-between p-2 rounded-2" style="background: rgba(255,255,255,0.03); border: 1px solid var(--dola-hairline);">
                    <span class="text-white-50">Tổng chi tiêu:</span>
                    <strong class="text-gold">{{ formatCurrency(customerInfo.totalSpent) }}</strong>
                  </div>
                  <div class="d-flex justify-content-between p-2 rounded-2" style="background: rgba(255,255,255,0.03); border: 1px solid var(--dola-hairline);">
                    <span class="text-white-50">Ngày tham gia:</span>
                    <strong class="text-white">{{ formatDate(customerInfo.createdAt) }}</strong>
                  </div>
                </div>

                <button class="dola-btn-outline w-100" @click="resetLookup">
                  🔄 Tra Cứu Số Khác
                </button>
              </div>
            </div>

            <!-- Member Privileges Box -->
            <div class="dola-card p-4">
              <h5 class="text-gold fw-bold mb-3 small text-uppercase" style="letter-spacing: 0.05em;">
                🎁 Quyền Lợi Thành Viên
              </h5>
              <ul class="list-unstyled d-flex flex-column gap-2 small text-body-dola mb-0">
                <li>✨ <strong class="text-white">Tích 1 điểm</strong> cho mỗi 10.000đ thanh toán.</li>
                <li>🎂 <strong class="text-white">Tặng quà sinh nhật</strong> và giảm 10% tiệc sinh nhật.</li>
                <li>🪑 <strong class="text-white">Ưu tiên xếp bàn VIP</strong> vào các dịp cuối tuần, lễ Tết.</li>
                <li>🧾 <strong class="text-white">Lưu trữ hóa đơn điện tử</strong> trọn đời để tra cứu dễ dàng.</li>
              </ul>
            </div>

          </div>
        </div>

        <!-- Right Column: Tabs (Bills, Bookings, Points) -->
        <div class="col-12 col-lg-8">
          <div class="dola-card p-4 p-md-5">

            <!-- Sub Nav Tabs -->
            <div class="d-flex gap-2 pb-3 mb-4 border-bottom border-dola-gold flex-wrap">
              <button
                class="btn rounded-pill px-4 py-2 fw-bold"
                :class="profileTab === 'invoices' ? 'dola-btn-primary' : 'text-body-dola'"
                @click="profileTab = 'invoices'"
              >
                🧾 Hóa Đơn Đã Dùng Bữa ({{ invoices.length }})
              </button>
              <button
                class="btn rounded-pill px-4 py-2 fw-bold"
                :class="profileTab === 'bookings' ? 'dola-btn-primary' : 'text-body-dola'"
                @click="profileTab = 'bookings'"
              >
                📋 Lịch Sử Đặt Bàn ({{ bookingHistory.length }})
              </button>
              <button
                class="btn rounded-pill px-4 py-2 fw-bold"
                :class="profileTab === 'points' ? 'dola-btn-primary' : 'text-body-dola'"
                @click="profileTab = 'points'"
              >
                💰 Biến Động Điểm ({{ pointHistory.length }})
              </button>
            </div>

            <!-- ===== TAB 1: INVOICES & BILLS ===== -->
            <div v-if="profileTab === 'invoices'">
              <div class="d-flex justify-content-between align-items-center mb-4">
                <div>
                  <h4 class="text-white fw-bold mb-1 fs-5">Danh Sách Hóa Đơn Dùng Bữa</h4>
                  <p class="text-body-dola small mb-0">Bấm vào bất kỳ hóa đơn nào để xem chi tiết hoặc mở <strong>chế độ in bill nhiệt K80</strong>.</p>
                </div>
              </div>

              <!-- Empty Invoices -->
              <div v-if="invoices.length === 0" class="text-center py-5">
                <div class="fs-1 mb-2">🧾</div>
                <h5 class="text-gold dola-font-title fs-3">Chưa Có Hóa Đơn Nào</h5>
                <p class="text-body-dola small mb-4">Quý khách chưa có hóa đơn thanh toán nào liên kết với số điện thoại này.</p>
                <router-link to="/customer/reservation" class="dola-btn-primary">
                  Đặt Bàn Thưởng Thức Ngay
                </router-link>
              </div>

              <!-- Invoices List -->
              <div v-else class="d-flex flex-column gap-3">
                <div
                  v-for="inv in invoices"
                  :key="inv.invoiceId"
                  class="p-4 rounded-3 d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3"
                  style="background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);"
                >
                  <div>
                    <div class="d-flex align-items-center gap-2 mb-1">
                      <span class="text-gold fw-bold fs-6">#{{ inv.invoiceCode }}</span>
                      <span class="badge bg-success text-white rounded-pill px-3 py-1 small">
                        ✓ Đã thanh toán
                      </span>
                    </div>
                    <div class="small text-body-dola mb-1">
                      🕒 {{ formatDateTime(inv.issuedAt) }} ·
                      🪑 <strong>{{ inv.tableName }}</strong> ({{ inv.areaName }})
                    </div>
                    <div class="small text-white-50">
                      👨‍💼 Thu ngân: {{ inv.cashierName }} ·
                      🍽️ {{ inv.items?.length || 0 }} món ăn
                    </div>
                  </div>

                  <div class="text-md-end pt-2 pt-md-0 border-top border-md-0 border-secondary border-opacity-25">
                    <div class="text-gold fw-bold fs-5 mb-2">{{ formatCurrency(inv.totalAmount) }}</div>
                    <button class="dola-btn-primary btn-sm py-1 px-3" style="height: 36px; font-size: 13px;" @click="openReceiptModal(inv)">
                      <span>🖨️</span>
                      <span>Xem In Bill K80</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- ===== TAB 2: BOOKINGS ===== -->
            <div v-else-if="profileTab === 'bookings'">
              <h4 class="text-white fw-bold mb-3 fs-5">Lịch Sử Đặt Bàn Gần Đây</h4>

              <div v-if="bookingHistory.length === 0" class="text-center py-5">
                <div class="fs-1 mb-2">📭</div>
                <h5 class="text-gold dola-font-title fs-3">Chưa Có Phiếu Đặt Bàn Nào</h5>
                <p class="text-body-dola small mb-4">Quý khách chưa tạo phiếu đặt bàn nào với số điện thoại này.</p>
                <router-link to="/customer/reservation" class="dola-btn-primary">
                  Đặt Bàn Ngay
                </router-link>
              </div>

              <div v-else class="d-flex flex-column gap-3">
                <div
                  v-for="b in bookingHistory"
                  :key="b.reservationId"
                  class="p-4 rounded-3"
                  style="background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);"
                >
                  <div class="d-flex justify-content-between align-items-start mb-2">
                    <div>
                      <span class="text-gold fw-bold fs-6">Phiếu #{{ b.reservationId }}</span>
                      <span class="badge ms-2" :class="getReservationBadgeClass(b.reservationStatus)">
                        {{ getReservationStatusText(b.reservationStatus) }}
                      </span>
                    </div>
                    <span class="dola-pill-badge">👥 {{ b.guestCount }} khách</span>
                  </div>
                  <div class="small text-body-dola mb-1">
                    🕒 <strong>{{ formatDateTime(b.reservationDate) }}</strong> ·
                    🪑 {{ b.assignedTable ? `${b.assignedTable.tableName} (${b.assignedTable.areaName})` : 'Chờ xếp bàn' }}
                  </div>
                  <div v-if="b.note" class="small text-white-50 fst-italic">
                    📝 {{ b.note }}
                  </div>
                </div>
              </div>
            </div>

            <!-- ===== TAB 3: POINTS HISTORY ===== -->
            <div v-else-if="profileTab === 'points'">
              <h4 class="text-white fw-bold mb-3 fs-5">Lịch Sử Tích Lũy Điểm Thưởng</h4>

              <div v-if="pointHistory.length === 0" class="text-center py-5">
                <div class="fs-1 mb-2">💰</div>
                <p class="text-body-dola small">Chưa có giao dịch điểm nào được ghi nhận.</p>
              </div>

              <div v-else class="d-flex flex-column gap-2">
                <div
                  v-for="pt in pointHistory"
                  :key="pt.pointTransactionId"
                  class="d-flex justify-content-between align-items-center p-3 rounded-2"
                  style="background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);"
                >
                  <div>
                    <div class="text-white fw-semibold small">{{ pt.note || 'Tích lũy từ hóa đơn' }}</div>
                    <div class="text-white-50" style="font-size: 0.75rem;">{{ formatDateTime(pt.createdAt) }}</div>
                  </div>
                  <div class="text-end">
                    <span class="fw-bold fs-6" :class="pt.points >= 0 ? 'text-success' : 'text-danger'">
                      {{ pt.points >= 0 ? '+' : '' }}{{ pt.points }} điểm
                    </span>
                    <div class="text-white-50" style="font-size: 0.72rem;">Số dư: {{ pt.balanceAfter }} điểm</div>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>

      </div>

    </div>

    <!-- ========================================================================= -->
    <!-- MODAL: CHẾ ĐỘ XEM & IN BILL (K80 THERMAL RECEIPT & DIGITAL CARD VIEW)     -->
    <!-- ========================================================================= -->
    <div v-if="selectedInvoice" class="modal-backdrop-custom" @click.self="selectedInvoice = null">
      <div class="modal-dialog-custom" style="max-width: 520px; background: transparent; box-shadow: none;">

        <!-- Top Action Bar (Không in) -->
        <div class="d-flex justify-content-between align-items-center p-3 mb-2 rounded-3 no-print"
          style="background: var(--dola-surface-alt); border: 1px solid var(--dola-hairline);">
          <div class="d-flex gap-2">
            <button
              class="btn btn-sm rounded-pill px-3 fw-bold"
              :class="receiptMode === 'k80' ? 'dola-btn-primary' : 'btn-outline-secondary text-white'"
              @click="receiptMode = 'k80'"
            >
              🖨️ Phiếu Nhiệt K80
            </button>
            <button
              class="btn btn-sm rounded-pill px-3 fw-bold"
              :class="receiptMode === 'digital' ? 'dola-btn-primary' : 'btn-outline-secondary text-white'"
              @click="receiptMode = 'digital'"
            >
              📱 Thẻ Điện Tử
            </button>
          </div>

          <div class="d-flex gap-2">
            <button class="btn btn-warning btn-sm rounded-pill fw-bold text-dark px-3" @click="printReceipt">
              🖨️ In Hóa Đơn
            </button>
            <button class="btn btn-outline-light btn-sm rounded-pill" @click="selectedInvoice = null">
              ✕
            </button>
          </div>
        </div>

        <!-- MODE 1: K80 THERMAL RECEIPT VIEW (BẢN IN NHIỆT THỰC TẾ) -->
        <div v-if="receiptMode === 'k80'" class="k80-receipt-wrapper k80-printable">

          <!-- Header Bill -->
          <div class="text-center mb-3">
            <div style="font-size: 1.8rem;">🪷</div>
            <div class="fw-bold fs-5 text-uppercase" style="letter-spacing: 0.05em;">NHÀ HÀNG HƯƠNG SEN</div>
            <div style="font-size: 11px;">DOLA RESTAURANT &amp; CUISINE</div>
            <div style="font-size: 11px; margin-top: 4px;">Đ/c: 123 Đường Ẩm Thực, Quận 1, TP. HCM</div>
            <div style="font-size: 11px;">Hotline: 1900 6868 - 0933 11 22 33</div>
            <div style="font-size: 10px; color: #555;">Wifi: HuongSen_Guest | Pass: huongsen2026</div>
          </div>

          <div class="k80-divider-double"></div>

          <!-- Title -->
          <div class="text-center my-2">
            <div class="fw-bold fs-6 text-uppercase" style="letter-spacing: 0.08em;">HÓA ĐƠN BÁN HÀNG</div>
            <div style="font-size: 11px;">(PHIẾU TÍNH TIỀN)</div>
          </div>

          <!-- Metadata -->
          <div style="font-size: 12px;" class="mb-2">
            <div class="d-flex justify-content-between">
              <span>Số HĐ: <strong>{{ selectedInvoice.invoiceCode }}</strong></span>
              <span>Bàn: <strong>{{ selectedInvoice.tableName }}</strong></span>
            </div>
            <div class="d-flex justify-content-between">
              <span>Ngày: {{ formatDateTime(selectedInvoice.issuedAt) }}</span>
              <span>Khu vực: {{ selectedInvoice.areaName }}</span>
            </div>
            <div class="d-flex justify-content-between">
              <span>Thu ngân: {{ selectedInvoice.cashierName }}</span>
              <span>PT: {{ selectedInvoice.paymentMethod === 'transfer' ? 'Chuyển khoản' : 'Tiền mặt' }}</span>
            </div>
            <div v-if="customerInfo" class="d-flex justify-content-between">
              <span>Khách: {{ customerInfo.customerName }}</span>
              <span>SĐT: {{ customerInfo.phone }}</span>
            </div>
          </div>

          <div class="k80-divider-dashed"></div>

          <!-- Items Table Header -->
          <div class="d-flex justify-content-between fw-bold text-uppercase" style="font-size: 11px; margin-bottom: 6px;">
            <span style="flex: 2;">Tên Món &amp; Size</span>
            <span style="flex: 1; text-align: center;">SL x Giá</span>
            <span style="flex: 1; text-align: right;">Thành Tiền</span>
          </div>

          <div class="k80-divider-dashed"></div>

          <!-- Items Rows -->
          <div class="d-flex flex-column gap-2 mb-2" style="font-size: 12px;">
            <div v-for="(item, idx) in selectedInvoice.items" :key="idx" class="d-flex flex-column">
              <div class="d-flex justify-content-between align-items-start">
                <div style="flex: 2; padding-right: 4px;">
                  <span class="fw-bold">{{ item.foodNameSnapshot }}</span>
                  <div v-if="item.variantSnapshot" style="font-size: 10px; color: #444;">
                    ({{ item.variantSnapshot }})
                  </div>
                </div>
                <div style="flex: 1; text-align: center; font-size: 11px;">
                  {{ item.quantity }} x {{ formatNumberOnly(item.unitPrice) }}
                </div>
                <div style="flex: 1; text-align: right; font-weight: bold;">
                  {{ formatNumberOnly(item.lineTotal) }}
                </div>
              </div>
            </div>
          </div>

          <div class="k80-divider-dashed"></div>

          <!-- Totals -->
          <div style="font-size: 12px;">
            <div class="d-flex justify-content-between mb-1">
              <span>Tạm tính (Subtotal):</span>
              <span>{{ formatNumberOnly(selectedInvoice.subtotal || selectedInvoice.totalAmount) }} đ</span>
            </div>
            <div v-if="selectedInvoice.discountAmount > 0" class="d-flex justify-content-between mb-1 text-danger">
              <span>Giảm giá / Ưu đãi:</span>
              <span>-{{ formatNumberOnly(selectedInvoice.discountAmount) }} đ</span>
            </div>
            <div v-if="selectedInvoice.pointsUsed > 0" class="d-flex justify-content-between mb-1">
              <span>Điểm trừ thanh toán:</span>
              <span>-{{ selectedInvoice.pointsUsed }} điểm</span>
            </div>

            <div class="k80-divider-double"></div>

            <div class="d-flex justify-content-between align-items-center k80-total-big my-2">
              <span>TỔNG CỘNG:</span>
              <span>{{ formatCurrency(selectedInvoice.totalAmount) }}</span>
            </div>

            <div class="k80-divider-dashed"></div>

            <div class="d-flex justify-content-between mb-1" style="font-size: 11px;">
              <span>Điểm tích lũy đợt này:</span>
              <strong>+{{ selectedInvoice.pointsEarned || Math.floor(selectedInvoice.totalAmount / 10000) }} điểm</strong>
            </div>
            <div class="d-flex justify-content-between mb-1" style="font-size: 11px;">
              <span>Trạng thái thanh toán:</span>
              <strong class="text-uppercase">ĐÃ THANH TOÁN (PAID)</strong>
            </div>
          </div>

          <div class="k80-divider-dashed"></div>

          <!-- Footer QR & Thank You -->
          <div class="text-center my-3">
            <div class="my-2 d-inline-block p-1 bg-white border border-dark">
              <!-- Simulated QR Code visual -->
              <div style="width: 80px; height: 80px; background: repeating-linear-gradient(45deg, #000 0, #000 4px, #fff 4px, #fff 8px); margin: 0 auto;"></div>
            </div>
            <div style="font-size: 10px; color: #444;">Mã tra cứu: {{ selectedInvoice.invoiceCode }}</div>
            <div class="fw-bold mt-2" style="font-size: 12px;">CẢM ƠN QUÝ KHÁCH &amp; HẸN GẶP LẠI!</div>
            <div style="font-size: 10px; color: #666; margin-top: 2px;">(Hóa đơn điện tử khởi tạo từ hệ thống POS Hương Sen)</div>
          </div>

        </div>

        <!-- MODE 2: DIGITAL DOLA CARD VIEW (THẺ ĐIỆN TỬ SANG TRỌNG) -->
        <div v-else class="dola-card p-4 p-md-5 overflow-hidden">
          <div class="d-flex justify-content-between align-items-start mb-4 pb-3 border-bottom border-dola-gold">
            <div>
              <span class="dola-pill-badge mb-2">Hóa Đơn Điện Tử</span>
              <h3 class="dola-font-title text-gold fs-2 mb-0">Hương Sen Restaurant</h3>
            </div>
            <div class="text-end">
              <span class="badge bg-success rounded-pill px-3 py-1">Đã thanh toán</span>
              <div class="text-gold fw-bold mt-1">#{{ selectedInvoice.invoiceCode }}</div>
            </div>
          </div>

          <div class="row g-2 mb-4 small text-body-dola">
            <div class="col-6">
              <span>Bàn phục vụ:</span> <strong class="text-white">{{ selectedInvoice.tableName }}</strong>
            </div>
            <div class="col-6">
              <span>Khu vực:</span> <strong class="text-white">{{ selectedInvoice.areaName }}</strong>
            </div>
            <div class="col-6">
              <span>Thời gian:</span> <strong class="text-white">{{ formatDateTime(selectedInvoice.issuedAt) }}</strong>
            </div>
            <div class="col-6">
              <span>Thu ngân:</span> <strong class="text-white">{{ selectedInvoice.cashierName }}</strong>
            </div>
          </div>

          <!-- Items -->
          <div class="p-3 rounded-3 mb-4" style="background: rgba(0,0,0,0.3); border: 1px solid var(--dola-hairline);">
            <h6 class="text-gold fw-bold mb-3 small text-uppercase">Danh Sách Món Ăn ({{ selectedInvoice.items?.length }})</h6>
            <div class="d-flex flex-column gap-2">
              <div v-for="(it, idx) in selectedInvoice.items" :key="idx"
                class="d-flex justify-content-between align-items-center py-1 border-bottom border-secondary border-opacity-25 small">
                <div>
                  <strong class="text-white">{{ it.foodNameSnapshot }}</strong>
                  <span v-if="it.variantSnapshot" class="text-white-50 ms-1">({{ it.variantSnapshot }})</span>
                  <div class="text-white-50" style="font-size: 0.72rem;">{{ it.quantity }} x {{ formatCurrency(it.unitPrice) }}</div>
                </div>
                <strong class="text-gold">{{ formatCurrency(it.lineTotal) }}</strong>
              </div>
            </div>
          </div>

          <!-- Total -->
          <div class="d-flex justify-content-between align-items-center pt-2">
            <div>
              <span class="text-white-50 small">Tổng thanh toán:</span>
              <div class="text-gold display-6 fw-bold dola-font-title">{{ formatCurrency(selectedInvoice.totalAmount) }}</div>
            </div>
            <button class="dola-btn-primary" @click="printReceipt">
              🖨️ In Phiếu K80
            </button>
          </div>
        </div>

      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import axios from 'axios';

const API_BASE = 'http://localhost:5079/api';

const phoneInput = ref(localStorage.getItem('customer_lookup_phone') || '');
const customerInfo = ref(null);
const bookingHistory = ref([]);
const pointHistory = ref([]);
const invoices = ref([]);

const lookingUp = ref(false);
const lookupError = ref('');
const profileTab = ref('invoices');

// Receipt Modal State
const selectedInvoice = ref(null);
const receiptMode = ref('k80'); // 'k80' or 'digital'

async function lookupCustomer() {
  if (!phoneInput.value.trim()) {
    lookupError.value = 'Vui lòng nhập số điện thoại để tra cứu.';
    return;
  }
  lookingUp.value = true;
  lookupError.value = '';
  try {
    const res = await axios.get(`${API_BASE}/customer/profile`, {
      params: { phone: phoneInput.value.trim() }
    });
    if (res.data?.success) {
      const data = res.data.data;
      customerInfo.value = {
        customerId: data.customerId,
        customerName: data.fullName,
        phone: data.phone,
        email: data.email,
        loyaltyPoints: data.pointsBalance || 0,
        totalSpent: data.totalSpent || 0,
        totalOrders: data.visitCount || 0,
        createdAt: data.createdAt,
      };
      bookingHistory.value = data.bookings || [];
      pointHistory.value = data.pointHistory || [];
      invoices.value = data.invoices || [];
      localStorage.setItem('customer_lookup_phone', phoneInput.value.trim());

      // If invoices exist, open tab invoices by default; otherwise bookings
      if (invoices.value.length > 0) {
        profileTab.value = 'invoices';
      } else if (bookingHistory.value.length > 0) {
        profileTab.value = 'bookings';
      }
    }
  } catch (err) {
    lookupError.value = err.response?.data?.message || 'Không tìm thấy hồ sơ với số điện thoại này. Quý khách vui lòng đặt bàn để trở thành thành viên.';
  } finally {
    lookingUp.value = false;
  }
}

function resetLookup() {
  customerInfo.value = null;
  bookingHistory.value = [];
  pointHistory.value = [];
  invoices.value = [];
  lookupError.value = '';
}

function openReceiptModal(inv) {
  selectedInvoice.value = inv;
  receiptMode.value = 'k80';
}

function printReceipt() {
  window.print();
}

function formatDate(dtStr) {
  if (!dtStr) return 'N/A';
  return new Date(dtStr).toLocaleDateString('vi-VN');
}

function formatDateTime(dtStr) {
  if (!dtStr) return '';
  return new Date(dtStr).toLocaleString('vi-VN', {
    hour: '2-digit', minute: '2-digit',
    day: '2-digit', month: '2-digit', year: 'numeric'
  });
}

function formatCurrency(val) {
  if (!val && val !== 0) return '0 ₫';
  return Number(val).toLocaleString('vi-VN') + ' ₫';
}

function formatNumberOnly(val) {
  if (!val && val !== 0) return '0';
  return Number(val).toLocaleString('vi-VN');
}

function getReservationBadgeClass(status) {
  const map = {
    pending: 'bg-warning text-dark',
    confirmed: 'bg-primary text-white',
    checked_in: 'bg-info text-dark',
    completed: 'bg-success text-white',
    cancelled: 'bg-danger text-white',
    rejected: 'bg-secondary text-white'
  };
  return map[status] || 'bg-secondary text-white';
}

function getReservationStatusText(status) {
  const map = {
    pending: 'Chờ xác nhận',
    confirmed: 'Đã xác nhận',
    checked_in: 'Đã đón khách',
    completed: 'Đã hoàn thành',
    cancelled: 'Đã hủy',
    rejected: 'Từ chối'
  };
  return map[status] || status;
}

onMounted(() => {
  if (phoneInput.value.trim()) {
    lookupCustomer();
  }
});
</script>

<style scoped>
.text-body-dola {
  color: var(--dola-body);
}

.dola-avatar-large {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  background: rgba(214, 156, 82, 0.2);
  border: 2px solid var(--dola-primary);
  color: var(--dola-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
  font-weight: bold;
}
</style>

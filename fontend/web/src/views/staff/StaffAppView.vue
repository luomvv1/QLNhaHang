<template>
  <div class="staff-app-container">
    <!-- Realtime Bank Toast -->
    <transition name="slide-down">
      <div v-if="liveBankToast" class="app-toast-alert" @click="viewToastInvoice(liveBankToast.invoice)">
        <div class="toast-icon">🔔</div>
        <div class="toast-info">
          <div class="toast-title">TIỀN VỀ TÀI KHOẢN (REALTIME)</div>
          <div class="toast-desc">
            Bàn <strong>{{ liveBankToast.tableDisplay }}</strong> vừa thanh toán:
            <span class="text-success fw-bold">+{{ formatCurrency(liveBankToast.amount) }}</span>
          </div>
        </div>
        <button class="toast-close" @click.stop="liveBankToast = null">×</button>
      </div>
    </transition>

    <!-- Global App Header -->
    <header class="app-topbar">
      <div class="app-topbar-left">
        <div class="staff-avatar">
          <span>👤</span>
          <span class="online-indicator"></span>
        </div>
        <div class="staff-meta">
          <div class="staff-name">{{ userName }}</div>
          <div class="staff-badge">{{ userRole === 'manager' || userRole === 'admin' ? 'Quản Lý / Thu Ngân' : 'Nhân Viên Phục Vụ' }}</div>
        </div>
      </div>

      <!-- Center: Title or Desktop Navigation Tabs -->
      <div class="app-topbar-center">
        <!-- Desktop / Tablet Horizontal Nav Tabs (Hidden on small mobile) -->
        <div class="desktop-nav-tabs d-none d-lg-flex">
          <button
            class="top-tab-btn"
            :class="{ active: activeTab === 'tables' }"
            @click="switchToTab('tables')"
          >
            <span>🪑</span> Sơ Đồ Bàn
          </button>
          <button
            class="top-tab-btn position-relative"
            :class="{ active: activeTab === 'order' }"
            @click="switchToTab('order')"
          >
            <span>🍽️</span> Gọi Món
            <span v-if="activeTable" class="top-tab-sub-pill">
              {{ activeTable.tableName }}
            </span>
          </button>
          <button
            class="top-tab-btn position-relative"
            :class="{ active: activeTab === 'ready' }"
            @click="switchToTab('ready')"
          >
            <span>🛎️</span> Bếp Xong
            <span v-if="allReadyItemsCount > 0" class="badge-count animate-bounce">
              {{ allReadyItemsCount }}
            </span>
          </button>
          <button
            class="top-tab-btn position-relative"
            :class="{ active: activeTab === 'reservations' }"
            @click="switchToTab('reservations')"
          >
            <span>📅</span> Khách Đặt Bàn
            <span v-if="todayReservationsPendingCount > 0" class="badge-count">
              {{ todayReservationsPendingCount }}
            </span>
          </button>
          <button
            class="top-tab-btn"
            :class="{ active: activeTab === 'more' }"
            @click="switchToTab('more')"
          >
            <span>⚙️</span> Tiện Ích
          </button>
        </div>

        <div v-if="activeTab === 'order' && activeTable" class="active-table-pill d-lg-none">
          <span class="table-dot"></span>
          <span>{{ activeTable.tableName }}</span>
          <small class="table-area">({{ activeTable.areaName || 'Sảnh' }})</small>
        </div>
      </div>

      <div class="app-topbar-right">
        <!-- Ready items bell (Mobile only) -->
        <button
          class="icon-action-btn position-relative d-lg-none"
          :class="{ 'has-ready': allReadyItemsCount > 0 }"
          @click="switchToTab('ready')"
          title="Món bếp đã nấu xong cần bưng"
        >
          <span>🛎️</span>
          <span v-if="allReadyItemsCount > 0" class="badge-count animate-bounce">
            {{ allReadyItemsCount }}
          </span>
        </button>

        <!-- Refresh button -->
        <button class="icon-action-btn" @click="refreshAllData" title="Làm mới dữ liệu">
          <span :class="{ 'spin-anim': isRefreshing }">🔄</span>
        </button>

        <!-- Switch to desktop POS -->
        <router-link to="/staff" class="mode-switch-btn" title="Chuyển sang chế độ POS máy tính bàn">
          <span>💻</span>
          <span class="d-none d-md-inline">POS Bàn</span>
        </router-link>
      </div>
    </header>

    <!-- Toast Success / Error Notifications -->
    <div v-if="successMsg || errorMsg" class="app-notifications-wrapper">
      <transition name="fade">
        <div v-if="successMsg" class="app-banner success-banner">
          <span>✅ {{ successMsg }}</span>
          <button @click="successMsg = ''">×</button>
        </div>
      </transition>
      <transition name="fade">
        <div v-if="errorMsg" class="app-banner error-banner">
          <span>⚠️ {{ errorMsg }}</span>
          <button @click="errorMsg = ''">×</button>
        </div>
      </transition>
    </div>

    <!-- MAIN CONTENT VIEW AREA (Scrollable) -->
    <main class="app-main-content">
      
      <!-- ============================================================== -->
      <!-- TAB 1: SƠ ĐỒ BÀN (TABLES)                                      -->
      <!-- ============================================================== -->
      <section v-if="activeTab === 'tables'" class="tab-pane-tables">
        <!-- ===== THỐNG KÊ NHANH GỌN GÀNG ===== -->
        <div class="tables-summary-compact">
          <div class="summary-left">
            <span class="summary-total">Tổng số <strong>{{ tables.length }}</strong> bàn</span>
            <span class="summary-tag">REALTIME AUTO</span>
          </div>
          <div class="summary-dots">
            <span class="stat-dot dot-empty">● Trống: <strong>{{ countEmpty }}</strong></span>
            <span class="stat-dot dot-serving">● Phục vụ: <strong>{{ countServing }}</strong></span>
            <span class="stat-dot dot-reserved">● Đặt trước: <strong>{{ countReserved }}</strong></span>
          </div>
        </div>

        <!-- ===== BỘ LỌC DẠNG DROPDOWN SỔ XUỐNG + SEARCH ===== -->
        <div class="tables-filter-bar">
          <!-- Dropdown Trạng thái -->
          <div class="filter-select-wrap">
            <select v-model="selectedStatus" class="app-select-control">
              <option value="">Tất cả trạng thái ({{ tables.length }})</option>
              <option value="empty">🟢 Bàn trống ({{ countEmpty }})</option>
              <option value="serving">🟠 Đang phục vụ ({{ countServing }})</option>
              <option value="reserved">🔵 Đã đặt trước ({{ countReserved }})</option>
            </select>
          </div>

          <!-- Dropdown Khu vực -->
          <div class="filter-select-wrap">
            <select v-model="selectedArea" class="app-select-control">
              <option value="">Tất cả khu vực</option>
              <option v-for="area in uniqueAreas" :key="area" :value="area">📍 {{ area }}</option>
            </select>
          </div>

          <!-- Search Box -->
          <div class="filter-search-wrap">
            <span class="search-icon">🔍</span>
            <input
              v-model="tableSearch"
              class="app-search-control"
              placeholder="Tìm tên bàn, số bàn..."
            />
            <button v-if="tableSearch" class="clear-search-btn" @click="tableSearch = ''">×</button>
          </div>

          <!-- Count Chip -->
          <span class="filter-count-chip">{{ filteredTables.length }} bàn</span>
        </div>

        <!-- Tables Grid (Responsive multi-column) -->
        <div v-if="isLoadingTables" class="app-loading-state">
          <div class="app-spinner"></div>
          <p>Đang tải sơ đồ bàn thời gian thực...</p>
        </div>

        <div v-else-if="filteredTables.length === 0" class="app-empty-state">
          <div class="empty-icon">🪑</div>
          <div class="empty-title">Không tìm thấy bàn phù hợp</div>
          <p class="empty-desc">Thử tìm với từ khóa khác hoặc bỏ chọn bộ lọc trạng thái.</p>
        </div>

        <div v-else class="tables-grid">
          <div
            v-for="table in filteredTables"
            :key="table.tableId"
            class="app-table-card"
            :class="[
              getTableClass(table.tableStatus),
              { 'has-ready-badge': table.activeOrder && table.activeOrder.readyCount > 0 },
              { 'is-active-selected': activeTable?.tableId === table.tableId }
            ]"
            @click="handleTableClick(table)"
          >
            <!-- Gradient Header Strip -->
            <div class="tc-header">
              <div class="tc-header-left">
                <span class="tc-name">{{ table.tableName }}</span>
                <span class="tc-area">{{ table.areaName || 'Sảnh chính' }}</span>
              </div>
              <div class="tc-capacity-badge">{{ table.capacity }} 👥</div>
            </div>

            <!-- Ready Food Alert -->
            <div
              v-if="table.activeOrder && table.activeOrder.readyCount > 0"
              class="tc-ready-alert animate-pulse"
              @click.stop="openReadyDishSheet(table)"
            >
              🛎️ {{ table.activeOrder.readyCount }} món đã xong — Bấm để bưng!
            </div>

            <!-- Body -->
            <div class="tc-body">
              <!-- Serving -->
              <template v-if="table.activeOrder">
                <div class="tc-order-meta">
                  <span class="tc-order-id">#{{ table.activeOrder.orderId }}</span>
                  <span v-if="table.activeOrder.linkedTables?.length > 0" class="tc-badge tc-badge-link">+{{ table.activeOrder.linkedTables.length }} Ghép</span>
                </div>
                <div class="tc-stats">
                  <span class="tc-stat-item">🍽️ {{ table.activeOrder.itemCount }} món</span>
                  <span v-if="table.activeOrder.servedCount > 0" class="tc-stat-item tc-stat-ok">✓ {{ table.activeOrder.servedCount }} bưng</span>
                  <span v-else-if="table.activeOrder.cookingCount > 0" class="tc-stat-item tc-stat-cook">🔥 {{ table.activeOrder.cookingCount }} nấu</span>
                </div>
                <div class="tc-amount">{{ formatCurrency(table.activeOrder.totalAmount) }}</div>
              </template>

              <!-- Empty -->
              <template v-else-if="table.tableStatus === 'empty'">
                <div class="tc-status-label tc-status-empty">
                  <span class="tc-status-dot dot-green"></span> Sẵn sàng đón khách
                </div>
                <div class="tc-hint">Nhấn để mở order mới</div>
              </template>

              <!-- Reserved -->
              <template v-else-if="table.tableStatus === 'reserved'">
                <div class="tc-reserved-name">👤 {{ table.reservation?.customerName || 'Khách đặt trước' }}</div>
                <div class="tc-reserved-time">⏰ {{ formatReservationTime(table.reservation?.reservationDate) }}</div>
                <div class="tc-reserved-guests">👥 {{ table.reservation?.guestCount || table.capacity }} khách</div>
              </template>

              <!-- Cleaning -->
              <template v-else>
                <div class="tc-status-label tc-status-cleaning">
                  <span class="tc-status-dot dot-grey"></span> Đang dọn dẹp
                </div>
              </template>
            </div>

            <!-- Footer Action Button -->
            <div class="tc-footer">
              <button
                v-if="table.activeOrder"
                class="tc-action-btn tc-btn-serving"
                @click.stop="openOrderForTable(table)"
              >
                🍽️ Vào Gọi Món
              </button>
              <button
                v-else-if="table.tableStatus === 'empty'"
                class="tc-action-btn tc-btn-open"
                @click.stop="openNewOrderModal(table)"
              >
                ＋ Mở Bàn Mới
              </button>
              <button
                v-else-if="table.tableStatus === 'reserved'"
                class="tc-action-btn tc-btn-checkin"
                @click.stop="quickCheckInByTable(table)"
              >
                🛎️ Đón Khách
              </button>
              <button
                v-else
                class="tc-action-btn tc-btn-secondary"
                @click.stop="handleTableClick(table)"
              >
                Xem chi tiết
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- ============================================================== -->
      <!-- TAB 2: GỌI MÓN (ORDER DESK) - RESPONSIVE SPLIT POS VIEW        -->
      <!-- ============================================================== -->
      <section v-else-if="activeTab === 'order'" class="tab-pane-order">
        <!-- Active Table Header Bar -->
        <div class="order-desk-header">
          <div class="desk-table-title">
            <button class="desk-back-btn" @click="switchToTab('tables')" title="Quay lại sơ đồ bàn">
              ⬅️
            </button>
            <div>
              <div class="desk-table-name">
                {{ activeTable ? activeTable.tableName : 'Chưa chọn bàn' }}
                <span class="desk-table-badge" :class="getTableBadgeClass(activeTable?.tableStatus)">
                  {{ getTableStatusText(activeTable?.tableStatus) }}
                </span>
              </div>
              <div class="desk-table-sub">
                📍 {{ activeTable?.areaName || 'Sảnh' }} • Sức chứa 👥 {{ activeTable?.capacity }} người
                <span v-if="currentOrder">• Đơn #{{ currentOrder.orderId }}</span>
              </div>
            </div>
          </div>

          <!-- Quick Actions on Active Table -->
          <div class="desk-quick-actions" v-if="currentOrder">
            <button class="action-pill-btn" @click="openSwitchTableModal" title="Đổi sang bàn khác">
              🔄 Đổi bàn
            </button>
            <button class="action-pill-btn" @click="openMergeTableModal" title="Ghép thêm bàn hoặc gộp bill">
              🔗 Ghép bàn
            </button>
            <button v-if="canCancelOrder" class="action-pill-btn btn-danger-pill" @click="cancelCurrentOrder" title="Hủy đơn">
              ❌ Hủy
            </button>
          </div>
        </div>

        <!-- No Table Selected or Empty Table -->
        <div v-if="!activeTable" class="app-empty-state">
          <div class="empty-icon">🪑</div>
          <div class="empty-title">Bạn chưa chọn bàn nào</div>
          <p class="empty-desc">Vui lòng chọn một bàn từ sơ đồ bàn để bắt đầu gọi món hoặc mở đơn.</p>
          <button class="btn-primary-gradient mt-3 px-4 py-2" @click="switchToTab('tables')">
            Về sơ đồ bàn
          </button>
        </div>

        <div v-else-if="!currentOrder && activeTable.tableStatus === 'empty'" class="app-empty-state">
          <div class="empty-icon">🍽️</div>
          <div class="empty-title">Bàn {{ activeTable.tableName }} chưa mở phục vụ</div>
          <p class="empty-desc">Khách đã vào ngồi? Bấm nút bên dưới để mở đơn hàng mới.</p>
          <button class="btn-primary-gradient mt-3 px-4 py-2 fw-bold" @click="createNewOrder(activeTable)">
            + Mở Đơn Mới Bàn {{ activeTable.tableName }}
          </button>
        </div>

        <!-- Active Order: Side-by-Side Responsive Layout -->
        <div v-else class="order-split-layout">
          <!-- LEFT COLUMN: THỰC ĐƠN CHỌN MÓN -->
          <div class="order-catalog-pane">
            <!-- Food Search Box -->
            <div class="food-search-box">
              <span class="search-icon">🔍</span>
              <input
                v-model="foodSearch"
                class="app-search-input"
                placeholder="Tìm nhanh món ăn (bò, gà, lẩu, nước...)"
              />
              <button v-if="foodSearch" class="clear-search-btn" @click="foodSearch = ''">×</button>
            </div>

            <!-- Category Horizontal Pills -->
            <div class="category-pills-row">
              <button
                class="cat-pill"
                :class="{ active: selectedCategory === '' }"
                @click="selectedCategory = ''"
              >
                🌟 Tất cả ({{ filteredFoods.length }})
              </button>
              <button
                v-for="cat in menuCategories"
                :key="cat.categoryId"
                class="cat-pill"
                :class="{ active: selectedCategory === cat.categoryId }"
                @click="selectedCategory = cat.categoryId"
              >
                {{ getCategoryEmoji(cat.categoryName) }} {{ cat.categoryName }}
              </button>
            </div>

            <!-- Food Cards Grid with Appetizing Photos -->
            <div class="food-cards-grid">
              <div
                v-for="food in filteredFoods"
                :key="food.foodId"
                class="app-food-card"
                @click="openSelectVariantModal(food)"
              >
                <!-- Food Image with Fallback -->
                <div class="food-card-media">
                  <img
                    v-if="food.imageUrl"
                    :src="food.imageUrl"
                    :alt="food.foodName"
                    class="food-img"
                    loading="lazy"
                    @error="food.imageUrl = null"
                  />
                  <div v-else class="food-img-fallback">
                    {{ getCategoryEmoji(food.categoryName) }}
                  </div>
                  <div class="food-variant-tag">
                    {{ food.availabilityText || (food.canOrder === false ? 'Hết món' : `${food.variants?.length || 1} size`) }}
                  </div>
                </div>

                <!-- Food Details -->
                <div class="food-card-body">
                  <h4 class="food-name">{{ food.foodName }}</h4>
                  <p class="food-desc">{{ food.description || 'Thơm ngon, chế biến tươi nóng sốt.' }}</p>
                  <div class="food-card-footer">
                    <div class="food-price">{{ formatCurrency(getFoodMinPrice(food)) }}</div>
                    <button class="food-add-btn" :disabled="food.canOrder === false" @click.stop="openSelectVariantModal(food)" :title="food.canOrder === false ? 'Hết món' : 'Chọn món'">
                      +
                    </button>
                  </div>
                </div>
              </div>

              <div v-if="filteredFoods.length === 0" class="col-12 text-center py-5 text-muted">
                Không tìm thấy món ăn nào phù hợp với từ khóa "{{ foodSearch }}".
              </div>
            </div>
          </div>

          <!-- RIGHT COLUMN: ORDER CART PANEL (Always visible on >=992px Landscape/Tablet/Desktop) -->
          <div class="order-cart-side-pane d-none d-lg-flex">
            <div class="side-cart-card">
              <div class="side-cart-header">
                <div>
                  <h4 class="side-cart-title">📋 ĐƠN BÀN {{ activeTable?.tableName }}</h4>
                  <small class="text-muted">Mã đơn #{{ currentOrder?.orderId }} • Giờ vào: {{ formatTime(currentOrder?.createdAt) }}</small>
                </div>
                <span class="badge bg-primary-subtle text-primary border border-primary-subtle px-2 py-1">
                  {{ orderItems.length }} dòng món
                </span>
              </div>

              <!-- Cart Items List -->
              <div class="side-cart-items-body">
                <div v-if="orderItems.length === 0" class="text-center py-5 text-muted">
                  <div class="fs-1 mb-2">🛒</div>
                  <p>Chưa có món nào trong đơn.<br>Chọn món từ thực đơn bên trái để thêm.</p>
                </div>

                <div v-else class="drawer-items-list">
                  <div
                    v-for="item in orderItems"
                    :key="item.orderItemId"
                    class="drawer-item-row"
                  >
                    <div class="item-main-info">
                      <div class="item-name-variant">
                        <span class="item-food-name">{{ item.foodNameSnapshot }}</span>
                        <span class="item-variant-tag">{{ item.variantSnapshot }}</span>
                      </div>
                      <div class="item-unit-price">
                        Đơn giá: {{ formatCurrency(item.UnitPrice) }}
                      </div>
                      <div v-if="item.Note" class="item-note-pill">
                        ✏️ {{ item.Note }}
                      </div>
                    </div>

                    <div class="item-control-side">
                      <div class="item-line-total">
                    {{ formatCurrency(item.ItemStatus === 'cancelled' ? 0 : item.LineTotal) }}
                      </div>

                      <div class="item-actions-row">
                        <span class="item-status-badge" :class="getItemStatusBadgeClass(item.ItemStatus)">
                          {{ getItemStatusText(item.ItemStatus) }}{{ item.CancellationReason ? ` · ${item.CancellationReason}` : '' }}
                        </span>

                        <!-- Quantity Stepper for Pending Items -->
                        <div v-if="item.ItemStatus === 'pending'" class="stepper-group">
                          <button class="stepper-btn" @click="updateItemQty(item, item.Quantity - 1)">-</button>
                          <span class="stepper-val">{{ item.Quantity }}</span>
                          <button class="stepper-btn" @click="updateItemQty(item, item.Quantity + 1)">+</button>
                          <button class="stepper-trash" @click="deleteItem(item)" title="Xóa món">🗑️</button>
                        </div>

                        <!-- Readonly Quantity for Sent Items -->
                        <div v-else class="item-fixed-qty" :title="item.CancellationReason">
                          <button v-if="canCancelItem(item)" class="btn btn-sm btn-outline-danger" @click="cancelSentItem(item)">Hủy món</button>
                          SL: <strong>{{ item.Quantity }}</strong>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Cart Footer Actions -->
              <div class="side-cart-footer">
                <div class="drawer-subtotal-row">
                  <span>Tạm tính ({{ totalItemQuantity }} món):</span>
                  <strong class="subtotal-amount">{{ formatCurrency(currentOrder?.subtotal) }}</strong>
                </div>

                <div class="drawer-action-buttons">
                  <button
                    class="btn-drawer-send-kitchen"
                    :disabled="pendingItemsCount === 0 || isSubmitting"
                    @click="confirmOrderToKitchen"
                  >
                    <span>⚡ GỬI XUỐNG BẾP</span>
                    <span class="badge-pending">{{ pendingItemsCount }} món chờ</span>
                  </button>

                  <button
                    class="btn-drawer-checkout"
                    :disabled="orderItems.length === 0 || isSubmitting"
                    @click="openCheckoutModal"
                  >
                    <span>💳 THANH TOÁN (QR / TIỀN MẶT)</span>
                    <span>{{ formatCurrency(currentOrder?.subtotal) }}</span>
                  </button>
                </div>

                <div class="drawer-sub-actions">
                  <button class="btn-sub-action" @click="openSwitchTableModal">
                    🔄 Đổi bàn
                  </button>
                  <button class="btn-sub-action" @click="openMergeTableModal">
                    🔗 Ghép bàn
                  </button>
                  <button class="btn-sub-action" @click="loadCurrentTableOrder">
                    🔄 Cập nhật
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ============================================================== -->
      <!-- TAB 3: BẾP TRẢ MÓN (READY KITCHEN ITEMS)                       -->
      <!-- ============================================================== -->
      <section v-else-if="activeTab === 'ready'" class="tab-pane-ready">
        <div class="pane-header">
          <div>
            <h3 class="pane-title">🛎️ MÓN ĐÃ NẤU XONG CẦN BƯNG RA BÀN</h3>
            <p class="pane-sub">Nhân viên phục vụ kiểm tra và bưng món ra cho khách đúng bàn.</p>
          </div>
          <button class="btn-refresh-sm" @click="refreshAllData">
            🔄 Làm mới
          </button>
        </div>

        <div v-if="allReadyItems.length === 0" class="app-empty-state">
          <div class="empty-icon">👨‍🍳</div>
          <div class="empty-title">Hiện không có món nào đang chờ bưng</div>
          <p class="empty-desc">Khi các quầy bếp (Nướng, Lẩu, Pha Chế) nấu xong món, danh sách sẽ tự động hiện tại đây kèm chuông báo!</p>
        </div>

        <!-- Responsive multi-column ready grid on wide screens -->
        <div v-else class="ready-items-grid">
          <div
            v-for="item in allReadyItems"
            :key="item.orderItemId"
            class="ready-item-card"
          >
            <div class="ready-card-left">
              <div class="ready-table-badge">
                {{ item.tableName }}
              </div>
              <div class="ready-item-meta">
                <h4 class="ready-food-name">{{ item.foodNameSnapshot }}</h4>
                <div class="ready-variant-name">
                  {{ item.variantSnapshot }} • <strong>SL: {{ item.quantity }}</strong>
                </div>
                <div v-if="item.note" class="ready-note-text">
                  ✏️ Ghi chú: {{ item.note }}
                </div>
              </div>
            </div>

            <div class="ready-card-right">
              <button
                class="btn-served-action"
                :disabled="isSubmitting"
                @click="markDishAsServed(item)"
              >
                <span>✅</span>
                <span>ĐÃ BƯNG RA BÀN</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- ============================================================== -->
      <!-- TAB 4: ĐẶT BÀN HÔM NAY (UI/UX PRO VIEW)                         -->
      <!-- ============================================================== -->
      <section v-else-if="activeTab === 'reservations'" class="tab-pane-reservations">
        <!-- Pane Header -->
        <div class="pane-header">
          <div>
            <div class="d-flex align-items-center gap-2 mb-1">
              <span class="badge bg-primary-subtle text-primary border border-primary-subtle px-2 py-0 fw-bold small">
                📅 HÔM NAY
              </span>
              <span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-0 fw-bold small">
                ● REALTIME
              </span>
            </div>
            <h3 class="pane-title mb-0">DANH SÁCH KHÁCH ĐẶT BÀN HÔM NAY</h3>
            <p class="pane-sub mb-0">Đón tiếp khách đặt chỗ, lọc nhanh, xếp bàn và mở Order tức thì.</p>
          </div>
          <div class="d-flex align-items-center gap-2">
            <button
              class="btn btn-sm btn-success fw-bold px-3 py-2 rounded-3 d-flex align-items-center gap-1 shadow-xs"
              @click="openAppHotlineBooking"
            >
              <span>➕</span>
              <span class="d-none d-sm-inline">Nhận Đặt Hotline</span>
            </button>
            <button class="btn-refresh-sm" @click="loadTodayReservations" title="Tải lại danh sách">
              🔄 Tải lại
            </button>
          </div>
        </div>

        <!-- Search & KPI Filter Bar -->
        <div class="res-control-toolbar mb-3 p-3 bg-white rounded-3 shadow-xs border">
          <div class="d-flex flex-wrap align-items-center justify-content-between gap-2">
            <!-- Search Input -->
            <div class="res-search-wrap">
              <span class="res-search-icon">🔍</span>
              <input
                v-model="resSearchQuery"
                type="text"
                class="form-control form-control-sm res-search-input"
                placeholder="Tìm theo tên khách, SĐT, số bàn..."
              />
              <button
                v-if="resSearchQuery"
                type="button"
                class="res-search-clear"
                @click="resSearchQuery = ''"
              >
                ✕
              </button>
            </div>

            <!-- KPI Filter Chips -->
            <div class="res-kpi-chips">
              <button
                type="button"
                class="kpi-chip-sm"
                :class="{ active: resActiveFilter === 'all' }"
                @click="resActiveFilter = 'all'"
              >
                <span>Tất cả</span>
                <span class="count-tag">{{ resStats.total }}</span>
              </button>
              <button
                type="button"
                class="kpi-chip-sm chip-amber"
                :class="{ active: resActiveFilter === 'pending' }"
                @click="resActiveFilter = 'pending'"
              >
                <span>⏳ Chờ duyệt</span>
                <span class="count-tag">{{ resStats.pending }}</span>
              </button>
              <button
                type="button"
                class="kpi-chip-sm chip-orange"
                :class="{ active: resActiveFilter === 'unassigned' }"
                @click="resActiveFilter = 'unassigned'"
              >
                <span>⚠️ Chưa xếp bàn</span>
                <span class="count-tag">{{ resStats.unassigned }}</span>
              </button>
              <button
                type="button"
                class="kpi-chip-sm chip-blue"
                :class="{ active: resActiveFilter === 'confirmed' }"
                @click="resActiveFilter = 'confirmed'"
              >
                <span>✅ Đã xếp bàn</span>
                <span class="count-tag">{{ resStats.confirmed }}</span>
              </button>
              <button
                type="button"
                class="kpi-chip-sm chip-green"
                :class="{ active: resActiveFilter === 'checked_in' }"
                @click="resActiveFilter = 'checked_in'"
              >
                <span>🍽️ Đang ăn</span>
                <span class="count-tag">{{ resStats.checkedIn }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Loading State -->
        <div v-if="isLoadingTodayReservations && todayReservations.length === 0" class="app-loading-state">
          <div class="app-spinner"></div>
          <p>Đang tải danh sách đặt bàn...</p>
        </div>

        <!-- Empty State -->
        <div v-else-if="filteredTodayReservations.length === 0" class="app-empty-state">
          <div class="empty-icon">📅</div>
          <div class="empty-title">
            {{ resSearchQuery ? 'Không tìm thấy kết quả phù hợp' : 'Không có lịch đặt bàn trong mục này' }}
          </div>
          <p class="empty-desc">
            {{ resSearchQuery ? 'Thử tìm với tên khách hoặc số điện thoại khác.' : 'Khi có khách đặt qua hotline hoặc web, danh sách sẽ tự động hiển thị.' }}
          </p>
          <button
            v-if="!resSearchQuery"
            class="btn btn-sm btn-success fw-bold px-3 py-2 mt-2 rounded-3"
            @click="openAppHotlineBooking"
          >
            ➕ Nhận Đặt Bàn Hotline Mới
          </button>
        </div>

        <!-- 1. VIEW NGANG CHO MÀN HÌNH TABLET / LAPTOP / DESKTOP (>= 768px) -->
        <div v-else class="reservations-table-wrapper d-none d-md-block">
          <div class="card border-0 shadow-sm overflow-hidden rounded-4">
            <div class="table-responsive">
              <table class="table table-hover align-middle mb-0 res-horizontal-table">
                <thead class="bg-dark text-white">
                  <tr>
                    <th class="ps-3 py-3">Khách Hàng</th>
                    <th class="py-3">Giờ Hẹn</th>
                    <th class="py-3 text-center">Số Khách</th>
                    <th class="py-3">Bàn Ăn</th>
                    <th class="py-3">Trạng Thái</th>
                    <th class="py-3">Ghi Chú</th>
                    <th class="pe-3 py-3 text-end">Thao Tác</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="res in filteredTodayReservations"
                    :key="res.reservationId"
                    :class="{ 'table-success-subtle': res.reservationStatus === 'checked_in' }"
                  >
                    <!-- Khách hàng -->
                    <td class="ps-3 py-3">
                      <div class="fw-bold text-dark fs-6">{{ res.customerName }}</div>
                      <div class="d-flex align-items-center gap-2 mt-1">
                        <small class="text-muted cursor-pointer" @click="copyResPhone(res.customerPhone)" title="Bấm để sao chép SĐT">
                          📞 {{ res.customerPhone }} 📋
                        </small>
                      </div>
                    </td>

                    <!-- Giờ hẹn & Countdown -->
                    <td class="py-3">
                      <div class="d-flex align-items-center gap-2">
                        <span class="fw-bold text-primary fs-6">
                          ⏰ {{ formatTime(res.reservationDate) }}
                        </span>
                        <span class="countdown-pill-sm" :class="getResCountdownClass(res.reservationDate, res.reservationStatus)">
                          {{ getResCountdownLabel(res.reservationDate, res.reservationStatus) }}
                        </span>
                      </div>
                    </td>

                    <!-- Số khách -->
                    <td class="py-3 text-center">
                      <span class="badge bg-light text-dark border px-2 py-1 fs-6">
                        👥 {{ res.guestCount }} người
                      </span>
                    </td>

                    <!-- Bàn gán -->
                    <td class="py-3">
                      <div v-if="res.table" class="d-flex align-items-center gap-2">
                        <span class="badge bg-primary-subtle text-primary border border-primary-subtle px-3 py-2 fs-6 fw-bold">
                          🪑 {{ res.table.tableName }} ({{ res.table.areaName }})
                        </span>
                        <button
                          v-if="res.reservationStatus !== 'checked_in'"
                          class="btn btn-sm btn-outline-secondary py-0 px-2 small"
                          title="Đổi sang bàn khác"
                          @click="openAssignResTableModal(res)"
                        >
                          Đổi
                        </button>
                      </div>
                      <div v-else class="d-flex align-items-center gap-2">
                        <span class="badge bg-warning-subtle text-warning border border-warning-subtle px-2 py-1 fw-bold">
                          ⚠️ Chưa có bàn
                        </span>
                        <button
                          class="btn btn-sm btn-primary py-0 px-2 fw-bold small"
                          @click="openAssignResTableModal(res)"
                        >
                          Xếp bàn
                        </button>
                      </div>
                    </td>

                    <!-- Trạng thái -->
                    <td class="py-3">
                      <span class="badge" :class="getReservationBadgeClass(res.reservationStatus)">
                        {{ getReservationStatusText(res.reservationStatus) }}
                      </span>
                    </td>

                    <!-- Ghi chú -->
                    <td class="py-3 text-muted small" style="max-width: 220px;">
                      {{ res.note || '—' }}
                    </td>

                    <!-- Thao tác -->
                    <td class="pe-3 py-3 text-end">
                      <!-- Confirmed with table -> Check-in -->
                      <button
                        v-if="res.reservationStatus === 'confirmed' && res.table"
                        class="btn btn-success btn-sm fw-bold px-3 shadow-xs"
                        @click="checkInReservationAndOpenOrder(res)"
                      >
                        🛎️ Đón Khách (Check-in)
                      </button>

                      <!-- Confirmed without table -> Assign -->
                      <button
                        v-else-if="res.reservationStatus === 'confirmed' && !res.table"
                        class="btn btn-primary btn-sm fw-bold px-3"
                        @click="openAssignResTableModal(res)"
                      >
                        🪑 Xếp Bàn Ngay
                      </button>

                      <!-- Pending -> Confirm -->
                      <div v-else-if="res.reservationStatus === 'pending'" class="d-inline-flex gap-1">
                        <button
                          class="btn btn-warning btn-sm fw-bold px-2"
                          @click="handleConfirmReservation(res)"
                        >
                          ✓ Duyệt
                        </button>
                        <button
                          class="btn btn-outline-danger btn-sm px-2"
                          title="Hủy đặt bàn"
                          @click="handleCancelReservation(res)"
                        >
                          ✕
                        </button>
                      </div>

                      <!-- Checked In -->
                      <span v-else-if="res.reservationStatus === 'checked_in'" class="text-success small fw-bold">
                        ✓ Đang dùng bữa
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- 2. VIEW DỌC CHO ĐIỆN THOẠI (< 768px) -->
        <div v-if="filteredTodayReservations.length > 0" class="reservations-cards-mobile d-md-none">
          <div
            v-for="res in filteredTodayReservations"
            :key="res.reservationId"
            class="app-res-card mb-3 p-3 bg-white rounded-3 shadow-xs border"
            :class="`res-border-${res.reservationStatus}`"
          >
            <!-- Card Top: Time, Countdown & Status -->
            <div class="d-flex align-items-center justify-content-between mb-2">
              <div class="d-flex align-items-center gap-2">
                <span class="fw-black fs-5 text-dark">
                  {{ formatTime(res.reservationDate) }}
                </span>
                <span class="countdown-pill-sm" :class="getResCountdownClass(res.reservationDate, res.reservationStatus)">
                  {{ getResCountdownLabel(res.reservationDate, res.reservationStatus) }}
                </span>
              </div>
              <span class="badge" :class="getReservationBadgeClass(res.reservationStatus)">
                {{ getReservationStatusText(res.reservationStatus) }}
              </span>
            </div>

            <!-- Customer & Guest Count -->
            <div class="d-flex align-items-center justify-content-between mb-2">
              <div class="fw-bold fs-6 text-dark">{{ res.customerName }}</div>
              <span class="badge bg-light text-dark border">
                👥 <strong>{{ res.guestCount }}</strong> khách
              </span>
            </div>

            <div class="d-flex align-items-center gap-2 mb-2">
              <span
                class="badge bg-light text-secondary border cursor-pointer"
                @click="copyResPhone(res.customerPhone)"
                title="Bấm để sao chép"
              >
                📞 {{ res.customerPhone }} 📋
              </span>
            </div>

            <!-- Note -->
            <div v-if="res.note" class="p-2 mb-2 rounded bg-light border-start border-3 border-secondary small text-muted fst-italic">
              “{{ res.note }}”
            </div>

            <!-- Table Allocation -->
            <div
              class="p-2 mb-3 rounded d-flex align-items-center justify-content-between"
              :class="res.table ? 'bg-success-subtle border border-success-subtle' : 'bg-warning-subtle border border-warning-subtle'"
            >
              <div v-if="res.table" class="small fw-bold text-success">
                🪑 {{ res.table.tableName }} ({{ res.table.areaName }})
              </div>
              <div v-else class="small fw-bold text-danger">
                ⚠️ Chưa xếp bàn cho khách
              </div>
              <button
                v-if="res.reservationStatus !== 'checked_in'"
                class="btn btn-xs btn-outline-dark py-1 px-2 small fw-bold"
                @click="openAssignResTableModal(res)"
              >
                {{ res.table ? 'Đổi bàn' : 'Xếp bàn' }}
              </button>
            </div>

            <!-- Action Button -->
            <div>
              <button
                v-if="res.reservationStatus === 'confirmed' && res.table"
                class="btn btn-success w-100 fw-bold py-2 shadow-xs"
                @click="checkInReservationAndOpenOrder(res)"
              >
                🛎️ Đón Khách Vào Bàn (Check-in)
              </button>
              <button
                v-else-if="res.reservationStatus === 'confirmed' && !res.table"
                class="btn btn-primary w-100 fw-bold py-2"
                @click="openAssignResTableModal(res)"
              >
                🪑 Xếp Bàn Ngay
              </button>
              <div v-else-if="res.reservationStatus === 'pending'" class="d-flex gap-2">
                <button
                  class="btn btn-warning flex-grow-1 fw-bold py-2"
                  @click="handleConfirmReservation(res)"
                >
                  ✓ Duyệt Nhận Chỗ
                </button>
                <button
                  class="btn btn-outline-danger px-3"
                  @click="handleCancelReservation(res)"
                >
                  ✕
                </button>
              </div>
              <div v-else-if="res.reservationStatus === 'checked_in'" class="text-center text-success small fw-bold py-1">
                ✓ Khách đang dùng bữa tại bàn
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ============================================================== -->
      <!-- TAB 5: TIỆN ÍCH & CÁ NHÂN (UTILITIES)                          -->
      <!-- ============================================================== -->
      <section v-else-if="activeTab === 'more'" class="tab-pane-more">
        <div class="user-profile-card">
          <div class="profile-avatar">👤</div>
          <div class="profile-info">
            <h3 class="profile-name">{{ userName }}</h3>
            <div class="profile-role">{{ userRole === 'manager' || userRole === 'admin' ? 'Quản Lý / Thu Ngân' : 'Nhân Viên Phục Vụ' }}</div>
            <div class="profile-status">🟢 Trực ca online</div>
          </div>
        </div>

        <div class="menu-action-group">
          <div class="menu-group-title">LỐI TẮT NHANH</div>
          
          <router-link to="/staff" class="menu-action-item">
            <div class="item-left">
              <span class="item-icon">💻</span>
              <div>
                <div class="item-title">Màn hình POS Màn Hình Rộng</div>
                <div class="item-sub">Giao diện POS đầy đủ cho máy tính để bàn / laptop quầy thu ngân</div>
              </div>
            </div>
            <span class="item-arrow">➡️</span>
          </router-link>

          <router-link v-if="userRole === 'manager' || userRole === 'admin'" to="/admin" class="menu-action-item">
            <div class="item-left">
              <span class="item-icon">⚙️</span>
              <div>
                <div class="item-title">Trang Quản Trị Hệ Thống</div>
                <div class="item-sub">Quản lý kho, định lượng, tài khoản, doanh thu</div>
              </div>
            </div>
            <span class="item-arrow">➡️</span>
          </router-link>

          <router-link to="/chef" class="menu-action-item">
            <div class="item-left">
              <span class="item-icon">👨‍🍳</span>
              <div>
                <div class="item-title">Màn hình Bếp (Kitchen KDS)</div>
                <div class="item-sub">Xem điều phối 3 quầy bếp: Nướng, Lẩu, Pha Chế</div>
              </div>
            </div>
            <span class="item-arrow">➡️</span>
          </router-link>

          <router-link to="/customer/menu" class="menu-action-item">
            <div class="item-left">
              <span class="item-icon">📖</span>
              <div>
                <div class="item-title">Trang Thực Đơn Khách Hàng</div>
                <div class="item-sub">Xem bảng giá, hình ảnh món và khuyến mãi đang hiển thị cho khách</div>
              </div>
            </div>
            <span class="item-arrow">➡️</span>
          </router-link>

          <div class="menu-action-item" @click="openRecentInvoicesModal">
            <div class="item-left">
              <span class="item-icon">📑</span>
              <div>
                <div class="item-title">Lịch Sử Hóa Đơn Gần Đây</div>
                <div class="item-sub">Xem lại các hóa đơn đã thanh toán trong ngày</div>
              </div>
            </div>
            <span class="item-arrow">➡️</span>
          </div>
        </div>

        <div class="menu-action-group mt-3">
          <div class="menu-group-title">HỆ THỐNG</div>
          <button class="menu-action-item text-danger border-0 w-100 bg-white" @click="handleLogout">
            <div class="item-left">
              <span class="item-icon">🚪</span>
              <div>
                <div class="item-title text-danger">Đăng xuất tài khoản</div>
                <div class="item-sub">Kết thúc ca làm việc của bạn</div>
              </div>
            </div>
            <span class="item-arrow">➡️</span>
          </button>
        </div>
      </section>

    </main>

    <!-- ============================================================== -->
    <!-- FLOATING CART BAR (MOBILE ONLY WHEN IN ORDER TAB)               -->
    <!-- ============================================================== -->
    <transition name="slide-up">
      <div
        v-if="activeTab === 'order' && currentOrder"
        class="floating-cart-bar d-lg-none"
        @click="showCartDrawer = true"
      >
        <div class="cart-bar-left">
          <div class="cart-icon-wrapper">
            <span>🛒</span>
            <span class="cart-badge-count">{{ totalItemQuantity }}</span>
          </div>
          <div class="cart-bar-info">
            <div class="cart-bar-total">{{ formatCurrency(currentOrder.subtotal) }}</div>
            <div class="cart-bar-sub">
              {{ orderItems.length }} món
              <span v-if="pendingItemsCount > 0" class="text-warning-highlight">
                • ⚡ {{ pendingItemsCount }} món chưa gửi bếp
              </span>
            </div>
          </div>
        </div>

        <div class="cart-bar-right">
          <button class="cart-view-btn">
            <span>Xem Đơn</span>
            <span class="cart-arrow">▲</span>
          </button>
        </div>
      </div>
    </transition>

    <!-- ============================================================== -->
    <!-- BOTTOM NAVIGATION BAR (VISIBLE ON MOBILE / TABLET PORTRAIT)     -->
    <!-- ============================================================== -->
    <nav class="app-bottom-navbar d-lg-none">
      <button
        class="nav-tab-item"
        :class="{ active: activeTab === 'tables' }"
        @click="switchToTab('tables')"
      >
        <span class="nav-tab-icon">🪑</span>
        <span class="nav-tab-label">Sơ Đồ Bàn</span>
      </button>

      <button
        class="nav-tab-item position-relative"
        :class="{ active: activeTab === 'order' }"
        @click="switchToTab('order')"
      >
        <span class="nav-tab-icon">🍽️</span>
        <span class="nav-tab-label">Gọi Món</span>
        <span v-if="activeTable" class="nav-active-table-badge">
          {{ activeTable.tableName }}
        </span>
      </button>

      <button
        class="nav-tab-item position-relative"
        :class="{ active: activeTab === 'ready' }"
        @click="switchToTab('ready')"
      >
        <span class="nav-tab-icon">🛎️</span>
        <span class="nav-tab-label">Bếp Xong</span>
        <span v-if="allReadyItemsCount > 0" class="nav-notification-badge animate-bounce">
          {{ allReadyItemsCount }}
        </span>
      </button>

      <button
        class="nav-tab-item position-relative"
        :class="{ active: activeTab === 'reservations' }"
        @click="switchToTab('reservations')"
      >
        <span class="nav-tab-icon">📅</span>
        <span class="nav-tab-label">Đặt Bàn</span>
        <span v-if="todayReservationsPendingCount > 0" class="nav-notification-badge">
          {{ todayReservationsPendingCount }}
        </span>
      </button>

      <button
        class="nav-tab-item"
        :class="{ active: activeTab === 'more' }"
        @click="switchToTab('more')"
      >
        <span class="nav-tab-icon">⚙️</span>
        <span class="nav-tab-label">Tiện Ích</span>
      </button>
    </nav>

    <!-- ============================================================== -->
    <!-- SLIDE-UP CART DRAWER (MOBILE ONLY)                             -->
    <!-- ============================================================== -->
    <transition name="drawer-fade">
      <div v-if="showCartDrawer" class="app-drawer-overlay d-lg-none" @click.self="showCartDrawer = false">
        <div class="app-cart-drawer">
          <!-- Drawer Header -->
          <div class="drawer-header">
            <div class="drawer-title-group">
              <h3 class="drawer-title">
                📋 ĐƠN HÀNG BÀN {{ activeTable?.tableName }}
              </h3>
              <div class="drawer-meta">
                Mã đơn: <strong>#{{ currentOrder?.orderId }}</strong> • Giờ mở: {{ formatTime(currentOrder?.createdAt) }}
              </div>
            </div>
            <button class="drawer-close-btn" @click="showCartDrawer = false">✕</button>
          </div>

          <!-- Drawer Items List -->
          <div class="drawer-body">
            <div v-if="orderItems.length === 0" class="app-empty-state py-4">
              <div class="empty-icon">🛒</div>
              <div class="empty-title">Chưa có món ăn nào trong đơn</div>
              <p class="empty-desc">Chọn món từ thực đơn bên dưới để thêm vào giỏ.</p>
            </div>

            <div v-else class="drawer-items-list">
              <div
                v-for="item in orderItems"
                :key="item.orderItemId"
                class="drawer-item-row"
              >
                <div class="item-main-info">
                  <div class="item-name-variant">
                    <span class="item-food-name">{{ item.foodNameSnapshot }}</span>
                    <span class="item-variant-tag">{{ item.variantSnapshot }}</span>
                  </div>
                  <div class="item-unit-price">
                    Đơn giá: {{ formatCurrency(item.UnitPrice) }}
                  </div>
                  <div v-if="item.Note" class="item-note-pill">
                    ✏️ {{ item.Note }}
                  </div>
                </div>

                <div class="item-control-side">
                  <div class="item-line-total">
                    {{ formatCurrency(item.ItemStatus === 'cancelled' ? 0 : item.LineTotal) }}
                  </div>

                  <!-- Item status & Stepper -->
                  <div class="item-actions-row">
                    <span class="item-status-badge" :class="getItemStatusBadgeClass(item.ItemStatus)">
                      {{ getItemStatusText(item.ItemStatus) }}{{ item.CancellationReason ? ` · ${item.CancellationReason}` : '' }}
                    </span>

                    <!-- Quantity Control for Pending Items -->
                    <div v-if="item.ItemStatus === 'pending'" class="stepper-group">
                      <button class="stepper-btn" @click="updateItemQty(item, item.Quantity - 1)">-</button>
                      <span class="stepper-val">{{ item.Quantity }}</span>
                      <button class="stepper-btn" @click="updateItemQty(item, item.Quantity + 1)">+</button>
                      <button class="stepper-trash" @click="deleteItem(item)" title="Xóa món">🗑️</button>
                    </div>

                    <!-- Readonly Quantity for Sent Items -->
                        <div v-else class="item-fixed-qty" :title="item.CancellationReason">
                          <button v-if="canCancelItem(item)" class="btn btn-sm btn-outline-danger" @click="cancelSentItem(item)">Hủy món</button>
                      SL: <strong>{{ item.Quantity }}</strong>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Drawer Footer Actions -->
          <div class="drawer-footer">
            <div class="drawer-subtotal-row">
              <span>Tạm tính ({{ totalItemQuantity }} món):</span>
              <strong class="subtotal-amount">{{ formatCurrency(currentOrder?.subtotal) }}</strong>
            </div>

            <div class="drawer-action-buttons">
              <!-- Send to Kitchen Button -->
              <button
                class="btn-drawer-send-kitchen"
                :disabled="pendingItemsCount === 0 || isSubmitting"
                @click="confirmOrderToKitchen"
              >
                <span>⚡ GỬI XUỐNG BẾP</span>
                <span class="badge-pending">{{ pendingItemsCount }} món chờ</span>
              </button>

              <!-- Checkout Button -->
              <button
                class="btn-drawer-checkout"
                :disabled="orderItems.length === 0 || isSubmitting"
                @click="openCheckoutModal"
              >
                <span>💳 THANH TOÁN TẠI BÀN (QR / TIỀN MẶT)</span>
                <span>{{ formatCurrency(currentOrder?.subtotal) }}</span>
              </button>
            </div>

            <!-- Table Actions row -->
            <div class="drawer-sub-actions">
              <button class="btn-sub-action" @click="openSwitchTableModal">
                🔄 Đổi bàn
              </button>
              <button class="btn-sub-action" @click="openMergeTableModal">
                🔗 Ghép bàn
              </button>
              <button class="btn-sub-action" @click="loadCurrentTableOrder">
                🔄 Cập nhật
              </button>
            </div>
          </div>
        </div>
      </div>
    </transition>

    <!-- ============================================================== -->
    <!-- MODAL / BOTTOM SHEET CHỌN SIZE & GHI CHÚ MÓN                   -->
    <!-- ============================================================== -->
    <transition name="drawer-fade">
      <div v-if="showVariantModal && selectedFood" class="app-drawer-overlay" @click.self="showVariantModal = false">
        <div class="app-variant-sheet">
          <div class="variant-sheet-header">
            <div class="sheet-title-group">
              <h3 class="sheet-title">{{ selectedFood.foodName }}</h3>
              <p class="sheet-desc">{{ selectedFood.description }}</p>
            </div>
            <button class="sheet-close-btn" @click="showVariantModal = false">✕</button>
          </div>

          <div class="variant-sheet-body">
            <!-- Chọn Size / Biến thể -->
            <div class="sheet-section">
              <label class="sheet-section-label">CHỌN SIZE / PHẦN ĂN:</label>
              <div class="variant-options-grid">
                <button type="button"
                  v-for="v in selectedFood.variants"
                  :key="v.variantId"
                  class="variant-option-card"
                  :class="{ active: selectedVariantId === v.variantId }"
                  :disabled="v.canOrder === false" @click="selectedVariantId = v.variantId"
                >
                  <div class="option-name">{{ v.variantName }}</div>
                  <small class="option-availability">{{ v.availabilityText || (v.canOrder === false ? 'Bếp tạm hết món' : '') }}</small>
                  <div class="option-price">{{ formatCurrency(v.price) }}</div>
                </button>
              </div>
            </div>

            <!-- Điều chỉnh số lượng -->
            <div class="sheet-section">
              <label class="sheet-section-label">SỐ LƯỢNG:</label>
              <div class="sheet-stepper-row">
                <button class="big-stepper-btn" @click="selectedQty = Math.max(1, selectedQty - 1)">-</button>
                <span class="big-stepper-val">{{ selectedQty }}</span>
                <button class="big-stepper-btn" @click="selectedQty++">+</button>
              </div>
            </div>

            <!-- Ghi chú nhanh -->
            <div class="sheet-section">
              <label class="sheet-section-label">GHI CHÚ BẾP:</label>
              <div class="quick-notes-pills">
                <button
                  v-for="note in quickNotes"
                  :key="note"
                  class="quick-note-chip"
                  @click="appendQuickNote(note)"
                >
                  {{ note }}
                </button>
              </div>
              <input
                v-model="itemNote"
                class="app-note-input"
                placeholder="Ví dụ: Ít cay, không hành, làm thật nóng..."
              />
            </div>
          </div>

          <div class="variant-sheet-footer">
            <button class="btn-confirm-add" :disabled="!selectedVariantId || selectedFood?.canOrder === false || !selectedFood?.variants?.some(v => v.variantId === selectedVariantId && v.canOrder !== false)" @click="confirmAddItemToOrder">
              <span>+ THÊM VÀO ĐƠN</span>
              <span class="add-total-val">{{ formatCurrency(calcSelectedTotal()) }}</span>
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- ============================================================== -->
    <!-- MODALS: ĐỔI BÀN, GHÉP BÀN, THANH TOÁN (REUSED COMPONENTS)      -->
    <!-- ============================================================== -->
    <StaffTableActionsModal
      :showSwitch="showSwitchModal"
      :showMerge="showMergeModal"
      :activeTable="activeTable"
      :emptyTables="emptyTables"
      :otherServingTables="otherServingTables"
      @close-switch="showSwitchModal = false"
      @close-merge="showMergeModal = false"
      @switched="handleTableSwitched"
      @merged="handleTableMerged"
    />

    <StaffCheckoutModal
      :show="showCheckoutModal"
      :orderId="currentOrder?.orderId"
      :tableName="activeTable?.tableName"
      :tableDisplay="activeTable?.tableName"
      :userName="userName"
      @close="showCheckoutModal = false"
      @success="handleCheckoutSuccess"
    />

    <StaffInvoiceModal
      :show="showInvoiceModal"
      :invoice="completedInvoice"
      @close="finishPaymentAndReturn"
    />

    <StaffRecentInvoicesModal
      :show="showRecentInvoicesModal"
      @close="showRecentInvoicesModal = false"
      @view-invoice="viewAndPrintInvoiceDetail"
    />

    <!-- ============================================================== -->
    <!-- MODAL XẾP BÀN CHO KHÁCH ĐẶT TRƯỚC (RESERVATION ASSIGN TABLE)    -->
    <!-- ============================================================== -->
    <div v-if="assigningRes" class="app-sub-modal-overlay" @click.self="assigningRes = null">
      <div class="app-sub-modal-dialog">
        <div class="sub-modal-header p-3 bg-light border-bottom d-flex align-items-center justify-content-between">
          <div>
            <span class="sub-eyebrow text-success fw-bold small d-block">ĐIỀU PHỐI BÀN ĂN</span>
            <h6 class="mb-0 fw-bold">
              Xếp bàn: {{ assigningRes.customerName }} ({{ assigningRes.guestCount }} khách)
            </h6>
          </div>
          <button type="button" class="btn-close" @click="assigningRes = null"></button>
        </div>

        <div class="sub-modal-body p-3" style="max-height: 65vh; overflow-y: auto;">
          <div v-if="isLoadingResTables" class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary"></div>
            <div class="mt-2 text-muted small">Đang tải danh sách bàn...</div>
          </div>
          <div v-else-if="resAvailableTables.length === 0" class="text-center py-4 text-muted">
            Không tìm thấy bàn nào khả dụng.
          </div>
          <div v-else class="area-group-list">
            <div v-for="(tablesInArea, area) in resTablesByArea" :key="area" class="mb-3">
              <div class="small fw-bold text-secondary mb-2">📍 {{ area }}</div>
              <div class="d-flex flex-wrap gap-2">
                <button
                  v-for="tbl in tablesInArea"
                  :key="tbl.tableId"
                  type="button"
                  class="btn btn-sm text-start p-2 border rounded-3 position-relative"
                  :class="{
                    'btn-success text-white border-success': selectedResTableId === tbl.tableId,
                    'btn-light text-dark': selectedResTableId !== tbl.tableId && tbl.tableStatus !== 'serving' && !tbl.isBookedOnDate,
                    'btn-secondary opacity-50': tbl.tableStatus === 'serving' || (tbl.isBookedOnDate && tbl.tableId !== assigningRes.table?.tableId)
                  }"
                  :disabled="tbl.tableStatus === 'serving' || (tbl.isBookedOnDate && tbl.tableId !== assigningRes.table?.tableId)"
                  @click="selectedResTableId = tbl.tableId"
                  style="min-width: 105px;"
                >
                  <div class="fw-bold">{{ tbl.tableName }}</div>
                  <div class="small opacity-75">👥 {{ tbl.capacity }} người</div>
                  <small v-if="tbl.tableStatus === 'serving'" class="d-block text-warning fw-bold">Đang ăn</small>
                  <small v-else-if="tbl.isBookedOnDate" class="d-block text-danger">Đã có khách</small>
                  <small v-else-if="tbl.capacity >= assigningRes.guestCount" class="d-block text-success fw-bold">✓ Phù hợp</small>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="sub-modal-footer p-3 bg-light border-top d-flex justify-content-end gap-2">
          <button type="button" class="btn btn-sm btn-outline-secondary fw-bold px-3" @click="assigningRes = null">
            Hủy
          </button>
          <button
            type="button"
            class="btn btn-sm btn-success fw-bold px-4"
            :disabled="!selectedResTableId || isSavingResAssign"
            @click="submitAssignResTable"
          >
            <span v-if="isSavingResAssign" class="spinner-border spinner-border-sm me-1"></span>
            Xác Nhận Xếp Bàn
          </button>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL TẠO LỊCH ĐẶT BÀN HOTLINE                                  -->
    <!-- ============================================================== -->
    <div v-if="showAppHotlineModal" class="app-sub-modal-overlay" @click.self="showAppHotlineModal = false">
      <div class="app-sub-modal-dialog">
        <div class="sub-modal-header p-3 bg-dark text-white d-flex align-items-center justify-content-between">
          <div>
            <span class="sub-eyebrow text-success small fw-bold d-block">HOTLINE BOOKING</span>
            <h6 class="mb-0 fw-bold">Nhận Đặt Bàn Qua Điện Thoại</h6>
          </div>
          <button type="button" class="btn-close btn-close-white" @click="showAppHotlineModal = false"></button>
        </div>

        <form @submit.prevent="submitAppHotlineBooking" class="p-3">
          <div class="mb-3">
            <label class="form-label small fw-bold">Tên khách hàng <span class="text-danger">*</span></label>
            <input
              v-model="appHotlineForm.name"
              type="text"
              class="form-control form-control-sm"
              placeholder="VD: Anh Minh, Chị Lan..."
              required
            />
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Số điện thoại <span class="text-danger">*</span></label>
            <input
              v-model="appHotlineForm.phone"
              type="tel"
              class="form-control form-control-sm"
              placeholder="VD: 0912345678"
              required
            />
          </div>

          <div class="row g-2 mb-3">
            <div class="col-6">
              <label class="form-label small fw-bold">Số khách</label>
              <input
                v-model.number="appHotlineForm.guestCount"
                type="number"
                min="1"
                max="50"
                class="form-control form-control-sm"
                required
              />
            </div>
            <div class="col-6">
              <label class="form-label small fw-bold">Giờ đến</label>
              <input
                v-model="appHotlineForm.time"
                type="time"
                class="form-control form-control-sm"
                required
              />
            </div>
          </div>

          <div class="mb-3">
            <label class="form-label small fw-bold">Ghi chú yêu cầu</label>
            <textarea
              v-model="appHotlineForm.note"
              class="form-control form-control-sm"
              rows="2"
              placeholder="VD: Cần ghế trẻ em, ngồi ngoài trời..."
            ></textarea>
          </div>

          <div class="d-flex justify-content-end gap-2 pt-2 border-top">
            <button type="button" class="btn btn-sm btn-outline-secondary fw-bold px-3" @click="showAppHotlineModal = false">
              Hủy
            </button>
            <button
              type="submit"
              class="btn btn-sm btn-success fw-bold px-4"
              :disabled="isSubmittingAppHotline"
            >
              <span v-if="isSubmittingAppHotline" class="spinner-border spinner-border-sm me-1"></span>
              Tạo Lịch Đặt
            </button>
          </div>
        </form>
      </div>
    </div>

  </div>
  <OperationalReasonDialog :dialog="reasonDialog" />
</template>

<script setup>
import OperationalReasonDialog from '@/components/OperationalReasonDialog.vue';
import { useReasonDialog } from '@/composables/useReasonDialog';
const { reasonDialog, askReason } = useReasonDialog();
import { clearSession } from '@/auth/session';
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';
import signalRService from '@/services/signalr';

import StaffTableActionsModal from './components/StaffTableActionsModal.vue';
import StaffCheckoutModal from './components/StaffCheckoutModal.vue';
import StaffInvoiceModal from './components/StaffInvoiceModal.vue';
import StaffRecentInvoicesModal from './components/StaffRecentInvoicesModal.vue';

const router = useRouter();
const apiBase = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/staff/orders`;
const paymentApiBase = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/staff/payment`;

// User & Navigation Tab
const userName = ref(localStorage.getItem('fullName') || 'Nhân viên');
const userRole = ref((localStorage.getItem('role') || 'staff').toLowerCase());
const activeTab = ref('tables'); // 'tables' | 'order' | 'ready' | 'reservations' | 'more'
const isRefreshing = ref(false);

// Tables & Filters
const tables = ref([]);
const isLoadingTables = ref(false);
const tableSearch = ref('');
const selectedArea = ref('');
const selectedStatus = ref('');
const showStatusDd = ref(false);
const showAreaDd = ref(false);

// Active Table & Current Order
const activeTable = ref(null);
const currentOrder = ref(null);
const orderItems = ref([]);
const showCartDrawer = ref(false);

// Menu Catalog & Food Search
const menuCategories = ref([]);
const menuFoods = ref([]);
const foodSearch = ref('');
const selectedCategory = ref('');

// Variant Modal
const showVariantModal = ref(false);
const selectedFood = ref(null);
const selectedVariantId = ref(null);
const selectedQty = ref(1);
const itemNote = ref('');
const quickNotes = ['Ít cay', 'Không hành', 'Không đá', 'Ít ngọt', 'Mang về', 'Làm nhanh'];

// Modals
const showSwitchModal = ref(false);
const showMergeModal = ref(false);
const showCheckoutModal = ref(false);
const showInvoiceModal = ref(false);
const completedInvoice = ref(null);
const liveBankToast = ref(null);
const showRecentInvoicesModal = ref(false);

// Reservations
const todayReservations = ref([]);
const isLoadingTodayReservations = ref(false);

// Notifications & Submitting state
const successMsg = ref('');
const errorMsg = ref('');
const isSubmitting = ref(false);
let heartbeatTimer = null;

// ==============================================================
// COMPUTED PROPERTIES
// ==============================================================
const uniqueAreas = computed(() => {
  const areas = tables.value.map(t => t.areaName).filter(Boolean);
  return [...new Set(areas)];
});

const filteredTables = computed(() => {
  return tables.value.filter(t => {
    if (selectedArea.value && t.areaName !== selectedArea.value) return false;
    if (selectedStatus.value && t.tableStatus !== selectedStatus.value) return false;
    if (tableSearch.value) {
      const q = tableSearch.value.toLowerCase();
      const matchName = (t.tableName || '').toLowerCase().includes(q);
      const matchCode = (t.tableCode || '').toLowerCase().includes(q);
      const matchArea = (t.areaName || '').toLowerCase().includes(q);
      if (!matchName && !matchCode && !matchArea) return false;
    }
    return true;
  });
});

const emptyTables = computed(() => {
  return tables.value.filter(t => t.tableStatus === 'empty' && t.tableId !== activeTable.value?.tableId);
});

const otherServingTables = computed(() => {
  return tables.value.filter(t =>
    t.tableStatus === 'serving' &&
    t.tableId !== activeTable.value?.tableId &&
    t.activeOrder &&
    t.activeOrder.orderId !== currentOrder.value?.orderId
  );
});

const countEmpty = computed(() => tables.value.filter(t => t.tableStatus === 'empty').length);
const countServing = computed(() => tables.value.filter(t => t.tableStatus === 'serving').length);
const countReserved = computed(() => tables.value.filter(t => t.tableStatus === 'reserved').length);

const filteredFoods = computed(() => {
  return menuFoods.value.filter(f => {
    if (selectedCategory.value && f.categoryId !== selectedCategory.value) return false;
    if (foodSearch.value) {
      const q = foodSearch.value.toLowerCase();
      if (!f.foodName.toLowerCase().includes(q)) return false;
    }
    return true;
  });
});

const pendingItemsCount = computed(() => {
  return orderItems.value.filter(i => i.ItemStatus === 'pending').length;
});

const totalItemQuantity = computed(() => {
  return orderItems.value.filter(i => i.ItemStatus !== 'cancelled').reduce((sum, i) => sum + i.Quantity, 0);
});

const canCancelOrder = computed(() => {
  if (!currentOrder.value) return false;
  return !orderItems.value.some(i => i.ItemStatus !== 'cancelled' &&
    (i.CanCancel === false || ['cooking', 'ready', 'served'].includes(i.ItemStatus)));
});

const todayReservationsPendingCount = computed(() =>
  todayReservations.value.filter(r => r.reservationStatus === 'pending' || r.reservationStatus === 'confirmed').length
);

// All Ready Dishes across tables that need to be served
const allReadyItems = ref([]);
const allReadyItemsCount = computed(() => {
  return tables.value.reduce((sum, t) => sum + (t.activeOrder?.readyCount || 0), 0);
});

// ==============================================================
// LIFECYCLE & SIGNALR REALTIME
// ==============================================================
onMounted(async () => {
  signalRService.on('FoodAvailabilityChanged', loadMenu);
  signalRService.on('PortionAvailabilityChanged', loadMenu);
  await Promise.all([loadTables(), loadMenu(), loadTodayReservations()]);
  await loadAllReadyDishes();

  // Close dropdowns when clicking outside
  document.addEventListener('click', (e) => {
    if (!e.target.closest('.tb-dropdown-wrap')) {
      showStatusDd.value = false;
      showAreaDd.value = false;
    }
  });

  signalRService.on('StaffOrderUpdated', async () => {
    await loadTodayReservations();
    await loadTablesSilently();
    if (activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
    await loadAllReadyDishes();
  });

  signalRService.on('KitchenUpdated', async () => {
    playNotificationChime();
    await loadTablesSilently();
    if (activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
    await loadAllReadyDishes();
  });

  signalRService.on('ReservationUpdated', async () => {
    await loadTodayReservations();
    await loadTablesSilently();
  });

  signalRService.on('PaymentReceived', async (data) => {
    // Nếu checkout modal đang mở cho đơn hàng này, modal sẽ tự phát âm ting ting và hiển thị Tick xanh cỡ lớn rồi emit @success
    if (showCheckoutModal.value && currentOrder.value?.orderId === data.orderId) {
      await loadTablesSilently();
      await loadAllReadyDishes();
      return;
    }

    playCashChime();
    completedInvoice.value = data.invoice;
    showCheckoutModal.value = false;
    showCartDrawer.value = false;
    showInvoiceModal.value = true;
    liveBankToast.value = null;
    successMsg.value = `🎉 Khách bàn ${data.tableName || data.tableDisplay} đã thanh toán ${formatCurrency(data.amount)}!`;

    await loadTablesSilently();
    if (activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
    await loadAllReadyDishes();
  });

  // Polling heartbeat
  heartbeatTimer = setInterval(async () => {
    await loadTablesSilently();
    if (activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
    await loadAllReadyDishes();
  }, 45000);
});

onUnmounted(() => {
  signalRService.off('FoodAvailabilityChanged', loadMenu);
  signalRService.off('PortionAvailabilityChanged', loadMenu);
  if (heartbeatTimer) clearInterval(heartbeatTimer);
  signalRService.off('StaffOrderUpdated');
  signalRService.off('KitchenUpdated');
  signalRService.off('ReservationUpdated');
  signalRService.off('PaymentReceived');
});

// ==============================================================
// TAB SWITCHING & REFRESH
// ==============================================================
function switchToTab(tab) {
  activeTab.value = tab;
  if (tab === 'ready') {
    loadAllReadyDishes();
  } else if (tab === 'reservations') {
    loadTodayReservations();
  }
}

async function refreshAllData() {
  isRefreshing.value = true;
  try {
    await Promise.all([loadTables(), loadMenu(), loadTodayReservations()]);
    if (activeTable.value) {
      await loadCurrentTableOrder();
    }
    await loadAllReadyDishes();
    successMsg.value = 'Đã cập nhật dữ liệu mới nhất!';
    setTimeout(() => { successMsg.value = ''; }, 2000);
  } finally {
    isRefreshing.value = false;
  }
}

// ==============================================================
// DATA FETCHING: TABLES & MENU
// ==============================================================
async function loadTables() {
  isLoadingTables.value = true;
  try {
    const res = await axios.get(`${apiBase}/tables`);
    tables.value = res.data ?? [];
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Lỗi khi tải sơ đồ bàn.';
  } finally {
    isLoadingTables.value = false;
  }
}

async function loadTablesSilently() {
  try {
    const res = await axios.get(`${apiBase}/tables`);
    if (res.data) {
      tables.value = res.data;
    }
  } catch (err) {}
}

async function loadMenu() {
  try {
    const res = await axios.get(`${apiBase}/menu`);
    menuCategories.value = res.data?.categories ?? [];
    menuFoods.value = res.data?.foods ?? [];
    if (selectedFood.value) {
      selectedFood.value = menuFoods.value.find(f => f.foodId === selectedFood.value.foodId) || null;
      if (!selectedFood.value) showVariantModal.value = false;
    }
  } catch (err) {
    console.error('Lỗi khi tải thực đơn:', err);
  }
}

async function loadAllReadyDishes() {
  const readyList = [];
  for (const t of tables.value) {
    if (t.activeOrder && t.activeOrder.readyCount > 0) {
      try {
        const res = await axios.get(`${apiBase}/table/${t.tableId}`);
        const items = res.data.order?.items ?? [];
        for (const item of items) {
          if (item.itemStatus === 'ready') {
            readyList.push({
              orderId: res.data.order.orderId,
              orderItemId: item.orderItemId,
              tableName: t.tableName,
              tableId: t.tableId,
              foodNameSnapshot: item.foodNameSnapshot,
              variantSnapshot: item.variantSnapshot,
              quantity: item.quantity,
              note: item.note
            });
          }
        }
      } catch (e) {}
    }
  }
  allReadyItems.value = readyList;
}

// ==============================================================
// TABLE INTERACTIONS
// ==============================================================
function handleTableClick(table) {
  if (table.activeOrder) {
    openOrderForTable(table);
  } else if (table.tableStatus === 'empty') {
    activeTable.value = table;
    activeTab.value = 'order';
    currentOrder.value = null;
    orderItems.value = [];
  } else if (table.tableStatus === 'reserved') {
    quickCheckInByTable(table);
  }
}

async function openOrderForTable(table) {
  activeTable.value = table;
  activeTab.value = 'order';
  await loadCurrentTableOrder();
}

function openReadyDishSheet(table) {
  activeTable.value = table;
  switchToTab('ready');
}

function openNewOrderModal(table) {
  createNewOrder(table);
}

async function createNewOrder(table) {
  if (!table) return;
  try {
    const res = await axios.post(`${apiBase}/create`, {
      tableId: table.tableId
    });
    successMsg.value = res.data?.message ?? 'Đã mở bàn thành công!';
    activeTable.value = table;
    activeTab.value = 'order';
    await Promise.all([loadTables(), loadCurrentTableOrder()]);
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể mở bàn mới.';
  }
}

async function loadCurrentTableOrder() {
  if (!activeTable.value) return;
  try {
    const res = await axios.get(`${apiBase}/table/${activeTable.value.tableId}`);
    activeTable.value = res.data.table;
    currentOrder.value = res.data.order;
    orderItems.value = (res.data.order?.items ?? []).map(i => ({
      orderItemId: i.orderItemId,
      FoodId: i.foodId,
      VariantId: i.variantId,
      foodNameSnapshot: i.foodNameSnapshot,
      variantSnapshot: i.variantSnapshot,
      UnitPrice: i.unitPrice,
      Quantity: i.quantity,
      LineTotal: i.lineTotal,
      Note: i.note,
      ItemStatus: i.itemStatus,
      CancellationReason: i.cancellationReason,
      CanCancel: i.canCancel
    }));
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Lỗi khi tải chi tiết bàn.';
  }
}

async function loadCurrentTableOrderSilently() {
  if (!activeTable.value) return;
  try {
    const res = await axios.get(`${apiBase}/table/${activeTable.value.tableId}`);
    if (res.data) {
      if (res.data.table) activeTable.value = res.data.table;
      if (!res.data.order) {
        currentOrder.value = null;
        orderItems.value = [];
        return;
      }
      if (res.data.order) {
        currentOrder.value = res.data.order;
        const serverItems = (res.data.order.items ?? []).map(i => ({
          orderItemId: i.orderItemId,
          FoodId: i.foodId,
          VariantId: i.variantId,
          foodNameSnapshot: i.foodNameSnapshot,
          variantSnapshot: i.variantSnapshot,
          UnitPrice: i.unitPrice,
          Quantity: i.quantity,
          LineTotal: i.lineTotal,
          Note: i.note,
          ItemStatus: i.itemStatus,
          CancellationReason: i.cancellationReason,
          CanCancel: i.canCancel
        }));
        orderItems.value = serverItems;
      }
    }
  } catch (err) {}
}

// ==============================================================
// ADD & MANAGE ORDER ITEMS
// ==============================================================
function openSelectVariantModal(food) {
  if (food.canOrder === false) return;
  selectedFood.value = food;
  selectedVariantId.value = food.variants?.find(v => v.canOrder !== false)?.variantId ?? null;
  selectedQty.value = 1;
  itemNote.value = '';
  showVariantModal.value = true;
}

function appendQuickNote(note) {
  if (!itemNote.value) {
    itemNote.value = note;
  } else if (!itemNote.value.includes(note)) {
    itemNote.value += `, ${note}`;
  }
}

function calcSelectedTotal() {
  if (!selectedFood.value) return 0;
  const v = selectedFood.value.variants?.find(x => x.variantId === selectedVariantId.value);
  const price = v ? v.price : (selectedFood.value.minPrice || 0);
  return price * selectedQty.value;
}

async function confirmAddItemToOrder() {
  if (!selectedFood.value || selectedFood.value.canOrder === false ||
      !selectedFood.value.variants?.some(v => v.variantId === selectedVariantId.value && v.canOrder !== false)) return;
  if (!currentOrder.value) {
    if (activeTable.value) {
      await createNewOrder(activeTable.value);
    }
    if (!currentOrder.value) return;
  }

  try {
    const res = await axios.post(`${apiBase}/${currentOrder.value.orderId}/items`, {
      foodId: selectedFood.value.foodId,
      variantId: selectedVariantId.value,
      quantity: selectedQty.value,
      note: itemNote.value
    });
    showVariantModal.value = false;
    successMsg.value = `Đã thêm '${selectedFood.value.foodName}' (${selectedQty.value})`;
    setTimeout(() => { if (successMsg.value) successMsg.value = ''; }, 2500);
    await loadCurrentTableOrder();
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Lỗi khi thêm món.';
  }
}

async function updateItemQty(item, newQty) {
  if (!currentOrder.value) return;
  try {
    await axios.patch(`${apiBase}/${currentOrder.value.orderId}/items/${item.orderItemId}`, {
      quantity: newQty
    });
    await loadCurrentTableOrder();
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể sửa số lượng.';
  }
}

async function deleteItem(item) {
  if (!confirm(`Xóa món '${item.foodNameSnapshot}' khỏi đơn?`)) return;
  try {
    await axios.delete(`${apiBase}/${currentOrder.value.orderId}/items/${item.orderItemId}`);
    await loadCurrentTableOrder();
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể xóa món.';
  }
}

async function confirmOrderToKitchen() {
  if (!currentOrder.value) return;
  isSubmitting.value = true;
  try {
    const res = await axios.post(`${apiBase}/${currentOrder.value.orderId}/confirm`);
    successMsg.value = res.data?.message ?? 'Đã gửi món xuống Bếp thành công!';
    showCartDrawer.value = false;
    await loadCurrentTableOrder();
    await loadTablesSilently();
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Lỗi khi gửi món xuống bếp.';
  } finally {
    isSubmitting.value = false;
  }
}

function canCancelItem(item) {
  return item.ItemStatus === 'confirmed' && item.CanCancel !== false;
}

async function cancelSentItem(item) {
  const reason = await askReason('Hủy món đã gửi bếp', `${item.foodNameSnapshot} · ${item.variantSnapshot}`, 'Hủy món');
  if (!reason || !currentOrder.value) return;
  try {
    const res = await axios.post(`${apiBase}/${currentOrder.value.orderId}/items/${item.orderItemId}/cancel`, { reason });
    successMsg.value = res.data.message;
    await loadCurrentTableOrder();
  } catch (err) { errorMsg.value = err.response?.data?.message || 'Không thể hủy món.'; }
}

async function cancelCurrentOrder() {
  if (!currentOrder.value) return;
  const reason = await askReason('Hủy toàn bộ đơn hàng', `Order #${currentOrder.value.orderId} · ${activeTable.value.tableName}`, 'Hủy đơn');
  if (!reason) return;
  try {
    const res = await axios.post(`${apiBase}/${currentOrder.value.orderId}/cancel`, { reason });
    successMsg.value = res.data?.message ?? 'Đã hủy đơn hàng thành công.';
    await Promise.all([loadTables(), loadCurrentTableOrder()]);
    switchToTab('tables');
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể hủy đơn hàng.';
  }
}

// ==============================================================
// READY ITEMS: BƯNG RA BÀN
// ==============================================================
async function markDishAsServed(item) {
  isSubmitting.value = true;
  try {
    await axios.patch(`${apiBase}/${item.orderId}/items/${item.orderItemId}/served`);
    playCashChime();
    successMsg.value = `✓ Đã phục vụ: ${item.foodNameSnapshot} (Bàn ${item.tableName})`;
    setTimeout(() => { if (successMsg.value) successMsg.value = ''; }, 2500);
    await Promise.all([loadTablesSilently(), loadAllReadyDishes()]);
    if (activeTable.value && activeTable.value.tableId === item.tableId) {
      await loadCurrentTableOrderSilently();
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Lỗi khi cập nhật trạng thái phục vụ.';
  } finally {
    isSubmitting.value = false;
  }
}

// ==============================================================
// ĐỔI BÀN & GHÉP BÀN
// ==============================================================
function openSwitchTableModal() {
  showSwitchModal.value = true;
}

function openMergeTableModal() {
  showMergeModal.value = true;
}

async function handleTableSwitched(newTargetTable) {
  showSwitchModal.value = false;
  successMsg.value = 'Đổi bàn thành công!';
  activeTable.value = newTargetTable;
  await Promise.all([loadTables(), loadCurrentTableOrder()]);
}

async function handleTableMerged(payload) {
  showMergeModal.value = false;
  successMsg.value = 'Thao tác ghép bàn thành công!';
  if (payload && payload.actionType === 'merge_orders') {
    const targetTable = tables.value.find(t => t.tableId === payload.targetTableId);
    if (targetTable) activeTable.value = targetTable;
  }
  await Promise.all([loadTables(), loadCurrentTableOrder()]);
}

// ==============================================================
// THANH TOÁN & HÓA ĐƠN
// ==============================================================
function openCheckoutModal() {
  if (!currentOrder.value) {
    errorMsg.value = 'Không tìm thấy đơn hàng cần thanh toán.';
    return;
  }
  const unconfirmed = orderItems.value.filter(i => i.ItemStatus === 'pending');
  if (unconfirmed.length > 0) {
    if (!confirm(`Đang có ${unconfirmed.length} món chưa gửi bếp. Bạn có muốn tiếp tục thanh toán không?`)) {
      return;
    }
  }
  showCheckoutModal.value = true;
}

function handleCheckoutSuccess(invoice) {
  completedInvoice.value = invoice;
  showCheckoutModal.value = false;
  showCartDrawer.value = false;
  showInvoiceModal.value = true;
  successMsg.value = 'Thanh toán thành công!';
  loadTablesSilently();
}

function finishPaymentAndReturn() {
  showInvoiceModal.value = false;
  completedInvoice.value = null;
  activeTable.value = null;
  currentOrder.value = null;
  orderItems.value = [];
  switchToTab('tables');
  loadTables();
}

function viewToastInvoice(inv) {
  completedInvoice.value = inv;
  liveBankToast.value = null;
  showInvoiceModal.value = true;
}

function openRecentInvoicesModal() {
  showRecentInvoicesModal.value = true;
}

async function viewAndPrintInvoiceDetail(invoiceId) {
  try {
    const res = await axios.get(`${paymentApiBase}/invoices/${invoiceId}`);
    completedInvoice.value = res.data;
    showRecentInvoicesModal.value = false;
    showInvoiceModal.value = true;
  } catch (err) {
    errorMsg.value = 'Không thể tải chi tiết hóa đơn.';
  }
}

// ==============================================================
// ĐẶT BÀN & CHECK-IN (ENHANCED UI/UX PRO)
// ==============================================================
const resSearchQuery = ref('');
const resActiveFilter = ref('all'); // 'all' | 'pending' | 'unassigned' | 'confirmed' | 'checked_in'
const copiedResPhone = ref('');

const assigningRes = ref(null);
const resAvailableTables = ref([]);
const isLoadingResTables = ref(false);
const selectedResTableId = ref(null);
const isSavingResAssign = ref(false);

const showAppHotlineModal = ref(false);
const isSubmittingAppHotline = ref(false);
const appHotlineForm = ref({
  name: '',
  phone: '',
  guestCount: 2,
  time: '',
  note: ''
});

const resStats = computed(() => {
  const total = todayReservations.value.length;
  const pending = todayReservations.value.filter(r => r.reservationStatus === 'pending').length;
  const unassigned = todayReservations.value.filter(r => r.reservationStatus === 'confirmed' && !r.table).length;
  const confirmed = todayReservations.value.filter(r => r.reservationStatus === 'confirmed' && r.table).length;
  const checkedIn = todayReservations.value.filter(r => r.reservationStatus === 'checked_in').length;
  return { total, pending, unassigned, confirmed, checkedIn };
});

const filteredTodayReservations = computed(() => {
  let list = todayReservations.value;
  if (resActiveFilter.value === 'pending') {
    list = list.filter(r => r.reservationStatus === 'pending');
  } else if (resActiveFilter.value === 'unassigned') {
    list = list.filter(r => r.reservationStatus === 'confirmed' && !r.table);
  } else if (resActiveFilter.value === 'confirmed') {
    list = list.filter(r => r.reservationStatus === 'confirmed' && r.table);
  } else if (resActiveFilter.value === 'checked_in') {
    list = list.filter(r => r.reservationStatus === 'checked_in');
  }

  if (resSearchQuery.value.trim()) {
    const q = resSearchQuery.value.trim().toLowerCase();
    list = list.filter(r => {
      const name = (r.customerName || '').toLowerCase();
      const phone = (r.customerPhone || '').toLowerCase();
      const table = (r.table?.tableName || '').toLowerCase();
      const area = (r.table?.areaName || '').toLowerCase();
      return name.includes(q) || phone.includes(q) || table.includes(q) || area.includes(q);
    });
  }

  return [...list].sort((a, b) => new Date(a.reservationDate).getTime() - new Date(b.reservationDate).getTime());
});

const resTablesByArea = computed(() => {
  const groups = {};
  for (const t of resAvailableTables.value) {
    const area = t.areaName || 'Sảnh chung';
    if (!groups[area]) groups[area] = [];
    groups[area].push(t);
  }
  return groups;
});

function getResCountdownLabel(dateStr, status) {
  if (status === 'checked_in') return 'Đang ăn';
  if (status === 'cancelled') return 'Đã hủy';
  if (!dateStr) return '';
  const now = new Date();
  const target = new Date(dateStr);
  const diffMinutes = Math.round((target - now) / 60000);
  if (diffMinutes < -15) return `Quá ${Math.abs(diffMinutes)}p`;
  if (diffMinutes < 0) return 'Đến giờ hẹn';
  if (diffMinutes <= 30) return `Còn ${diffMinutes}p`;
  const hours = Math.floor(diffMinutes / 60);
  const mins = diffMinutes % 60;
  return hours > 0 ? `Sau ${hours}h${mins > 0 ? mins + 'p' : ''}` : `Còn ${mins}p`;
}

function getResCountdownClass(dateStr, status) {
  if (status === 'checked_in') return 'badge-checked-in';
  if (status === 'cancelled') return 'badge-cancelled';
  if (!dateStr) return '';
  const now = new Date();
  const target = new Date(dateStr);
  const diffMinutes = Math.round((target - now) / 60000);
  if (diffMinutes < -15) return 'badge-overdue';
  if (diffMinutes <= 30) return 'badge-upcoming';
  return 'badge-normal';
}

function copyResPhone(phone) {
  if (!phone) return;
  navigator.clipboard.writeText(phone);
  copiedResPhone.value = phone;
  successMsg.value = `Đã sao chép số điện thoại: ${phone}`;
  setTimeout(() => {
    if (copiedResPhone.value === phone) copiedResPhone.value = '';
  }, 2000);
}

async function loadTodayReservations() {
  isLoadingTodayReservations.value = true;
  try {
    const today = new Date().toISOString().split('T')[0];
    const res = await axios.get(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations`, {
      params: { date: today }
    });
    if (res.data?.success) {
      todayReservations.value = res.data.data;
    }
  } catch (err) {
    console.error('Lỗi tải đặt bàn:', err);
  } finally {
    isLoadingTodayReservations.value = false;
  }
}

async function quickCheckInByTable(table) {
  await loadTodayReservations();
  const matchedRes = todayReservations.value.find(r =>
    r.table && r.table.tableId === table.tableId && r.reservationStatus === 'confirmed'
  );

  if (matchedRes) {
    checkInReservationAndOpenOrder(matchedRes);
  } else {
    switchToTab('reservations');
  }
}

async function checkInReservationAndOpenOrder(res) {
  if (res?.reservationStatus !== 'confirmed' || !res?.table?.tableId) {
    errorMsg.value = 'Phiếu đặt bàn phải được gán bàn trước khi check-in.';
    return;
  }

  if (!confirm(`Xác nhận đón khách ${res.customerName} (${res.guestCount} người)? Hệ thống sẽ mở Order ngay.`)) {
    return;
  }

  try {
    const resp = await axios.post(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/${res.reservationId}/check-in`);
    if (resp.data?.success) {
      successMsg.value = resp.data.message;
      await loadTables();
      const tableObj = tables.value.find(t => t.tableId === resp.data.data.tableId);
      if (tableObj) {
        openOrderForTable(tableObj);
      }
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể check-in đón khách.';
  }
}

async function handleConfirmReservation(res) {
  try {
    const resp = await axios.post(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/${res.reservationId}/confirm`, {});
    if (resp.data?.success) {
      successMsg.value = `Đã duyệt tiếp nhận lịch đặt của ${res.customerName}!`;
      await loadTodayReservations();
      const updated = todayReservations.value.find(r => r.reservationId === res.reservationId);
      if (updated && !updated.table) {
        openAssignResTableModal(updated);
      }
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi duyệt phiếu đặt bàn.';
  }
}

async function handleCancelReservation(res) {
  if (!confirm(`Xác nhận hủy lịch đặt của khách "${res.customerName}"?`)) return;
  try {
    const resp = await axios.post(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/${res.reservationId}/cancel`, {
      reason: 'Nhân viên hủy tại quầy'
    });
    if (resp.data?.success) {
      successMsg.value = `Đã hủy lịch đặt của ${res.customerName}.`;
      await loadTodayReservations();
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi hủy lịch.';
  }
}

async function openAssignResTableModal(res) {
  assigningRes.value = res;
  selectedResTableId.value = res.table?.tableId || null;
  isLoadingResTables.value = true;
  try {
    const today = new Date().toISOString().split('T')[0];
    const resp = await axios.get(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/tables`, {
      params: { date: today, excludeReservationId: res.reservationId }
    });
    if (resp.data?.success) {
      resAvailableTables.value = resp.data.data || [];
    }
  } catch (err) {
    errorMsg.value = 'Không thể tải danh sách bàn ăn.';
  } finally {
    isLoadingResTables.value = false;
  }
}

async function submitAssignResTable() {
  if (!assigningRes.value || !selectedResTableId.value) return;
  isSavingResAssign.value = true;
  try {
    const id = assigningRes.value.reservationId;
    const resp = await axios.post(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/${id}/assign-table`, {
      tableId: selectedResTableId.value,
      tableIds: [selectedResTableId.value]
    });
    if (resp.data?.success) {
      successMsg.value = 'Đã xếp bàn thành công!';
      assigningRes.value = null;
      await Promise.all([loadTodayReservations(), loadTables()]);
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi xếp bàn.';
  } finally {
    isSavingResAssign.value = false;
  }
}

function openAppHotlineBooking() {
  const now = new Date();
  now.setMinutes(now.getMinutes() + 30);
  const timeStr = now.toTimeString().slice(0, 5);
  appHotlineForm.value = {
    name: '',
    phone: '',
    guestCount: 2,
    time: timeStr,
    note: ''
  };
  showAppHotlineModal.value = true;
}

async function submitAppHotlineBooking() {
  isSubmittingAppHotline.value = true;
  try {
    const [hh, mm] = appHotlineForm.value.time.split(':').map(Number);
    const dateObj = new Date();
    dateObj.setHours(hh, mm, 0, 0);

    const payload = {
      customerName: appHotlineForm.value.name.trim(),
      customerPhone: appHotlineForm.value.phone.trim(),
      guestCount: appHotlineForm.value.guestCount,
      reservationDate: dateObj.toISOString(),
      note: appHotlineForm.value.note.trim()
    };

    const resp = await axios.post(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/create`, payload);
    if (resp.data?.success) {
      successMsg.value = 'Tạo lịch đặt bàn hotline thành công!';
      showAppHotlineModal.value = false;
      await loadTodayReservations();
      const createdId = resp.data.data?.reservationId;
      if (createdId) {
        const found = todayReservations.value.find(r => r.reservationId === createdId);
        if (found) {
          openAssignResTableModal(found);
        }
      }
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi tạo lịch hotline.';
  } finally {
    isSubmittingAppHotline.value = false;
  }
}

// ==============================================================
// AUDIO EFFECTS & HELPERS
// ==============================================================
function playCashChime() {
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    const ctx = new AudioContext();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.type = 'sine';
    osc.frequency.setValueAtTime(587.33, ctx.currentTime);
    osc.frequency.setValueAtTime(880, ctx.currentTime + 0.15);
    gain.gain.setValueAtTime(0.25, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.6);
    osc.start();
    osc.stop(ctx.currentTime + 0.6);
  } catch (e) {}
}

function playNotificationChime() {
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    const ctx = new AudioContext();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.type = 'sine';
    osc.frequency.setValueAtTime(659.25, ctx.currentTime);
    osc.frequency.setValueAtTime(987.77, ctx.currentTime + 0.12);
    gain.gain.setValueAtTime(0.2, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.45);
    osc.start();
    osc.stop(ctx.currentTime + 0.45);
  } catch (e) {}
}

function handleLogout() {
  if (confirm('Bạn có chắc muốn đăng xuất?')) {
    clearSession();
    router.push('/login');
  }
}

function formatCurrency(val) {
  if (!val && val !== 0) return '0 ₫';
  return Number(val).toLocaleString('vi-VN') + ' ₫';
}

function formatTime(dt) {
  if (!dt) return '';
  return new Date(dt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function formatReservationTime(dt) {
  if (!dt) return '';
  const d = new Date(dt);
  return `${d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} - ${d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}`;
}

function getFoodMinPrice(food) {
  if (food.variants && food.variants.length > 0) {
    return Math.min(...food.variants.map(v => v.price));
  }
  return food.minPrice || 0;
}

function getCategoryEmoji(catName) {
  if (!catName) return '🍽️';
  if (catName.includes('Nướng') || catName.includes('Chiên')) return '🥩';
  if (catName.includes('Nấu') || catName.includes('Lẩu')) return '🍲';
  if (catName.includes('Pha Chế') || catName.includes('Đồ Uống') || catName.includes('Bia')) return '🍹';
  return '🍽️';
}

function getTableClass(status) {
  switch (status) {
    case 'empty': return 'table-status-empty';
    case 'serving': return 'table-status-serving';
    case 'reserved': return 'table-status-reserved';
    case 'cleaning': return 'table-status-cleaning';
    default: return '';
  }
}

function getStatusClass(status) {
  switch (status) {
    case 'empty':    return 'cb-color-empty';
    case 'serving':  return 'cb-color-serving';
    case 'reserved': return 'cb-color-reserved';
    default: return 'cb-color-all';
  }
}

function getTableBadgeClass(status) {
  switch (status) {
    case 'empty': return 'badge bg-success';
    case 'serving': return 'badge bg-warning text-dark';
    case 'reserved': return 'badge bg-info text-white';
    default: return 'badge bg-secondary';
  }
}

function getTableStatusText(status) {
  switch (status) {
    case 'empty': return 'Bàn Trống';
    case 'serving': return 'Đang Phục Vụ';
    case 'reserved': return 'Đã Đặt';
    case 'cleaning': return 'Đang Dọn';
    default: return 'Khác';
  }
}

function getItemStatusText(status) {
  switch (status) {
    case 'pending': return 'Chờ gửi';
    case 'confirmed': return 'Đã gửi';
    case 'cooking': return 'Đang nấu';
    case 'ready': return 'Đã xong';
    case 'served': return 'Đã bưng';
    case 'cancelled': return 'Đã hủy';
    default: return status;
  }
}

function getItemStatusBadgeClass(status) {
  switch (status) {
    case 'pending': return 'badge-status-pending';
    case 'confirmed': return 'badge-status-confirmed';
    case 'cooking': return 'badge-status-cooking';
    case 'ready': return 'badge-status-ready';
    case 'served': return 'badge-status-served';
    case 'cancelled': return 'badge-status-cancelled';
    default: return '';
  }
}

function getReservationBadgeClass(status) {
  switch (status) {
    case 'confirmed': return 'badge bg-primary';
    case 'pending': return 'badge bg-warning text-dark';
    case 'checked_in': return 'badge bg-success';
    case 'cancelled': return 'badge bg-danger';
    default: return 'badge bg-secondary';
  }
}

function getReservationBorderClass(status) {
  switch (status) {
    case 'confirmed': return 'border-res-confirmed';
    case 'checked_in': return 'border-res-checkedin';
    default: return '';
  }
}

function getReservationStatusText(status) {
  switch (status) {
    case 'confirmed': return 'Đã xác nhận';
    case 'pending': return 'Chờ duyệt';
    case 'checked_in': return 'Đã đến';
    case 'cancelled': return 'Đã hủy';
    default: return status;
  }
}
</script>

<style scoped>
.variant-option-card:disabled, .food-add-btn:disabled { opacity: .55; cursor: not-allowed; }
/* ============================================================== */
/* APP SHELL & MULTI-SCREEN RESPONSIVE LAYOUT                     */
/* ============================================================== */
.staff-app-container {
  display: flex;
  flex-direction: column;
  height: 100dvh;
  width: 100vw;
  background-color: #f1f5f9;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  color: #0f172a;
  overflow: hidden;
  position: relative;
}

/* ============================================================== */
/* TOP BAR & DESKTOP NAVIGATION TABS                              */
/* ============================================================== */
.app-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 20px;
  background: linear-gradient(135deg, #091e1b, #0f3530);
  color: #ffffff;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.25);
  z-index: 40;
  flex-shrink: 0;
}

.app-topbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.staff-avatar {
  position: relative;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  border: 1px solid rgba(255, 255, 255, 0.25);
}

.online-indicator {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #22c55e;
  border: 2px solid #091e1b;
}

.staff-name {
  font-size: 0.95rem;
  font-weight: 700;
  color: #ffffff;
  line-height: 1.2;
}

.staff-badge {
  font-size: 0.72rem;
  color: #d4af37;
  font-weight: 600;
}

/* Desktop Navigation Tabs (Horizontal on Tablet Landscape & PC) */
.desktop-nav-tabs {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(0, 0, 0, 0.25);
  padding: 4px 8px;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.top-tab-btn {
  background: transparent;
  border: none;
  color: rgba(255, 255, 255, 0.75);
  font-size: 0.86rem;
  font-weight: 700;
  padding: 7px 14px;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.top-tab-btn:hover {
  color: #ffffff;
  background: rgba(255, 255, 255, 0.1);
}

.top-tab-btn.active {
  background: #d4af37;
  color: #091e1b;
  box-shadow: 0 2px 8px rgba(212, 175, 55, 0.4);
}

.top-tab-sub-pill {
  background: rgba(0, 0, 0, 0.2);
  color: inherit;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 0.72rem;
}

.active-table-pill {
  display: flex;
  align-items: center;
  gap: 6px;
  background: rgba(212, 175, 55, 0.2);
  border: 1px solid rgba(212, 175, 55, 0.4);
  padding: 4px 12px;
  border-radius: 999px;
  font-weight: 700;
  font-size: 0.85rem;
  color: #ffffff;
}

.table-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #d4af37;
}

.table-area {
  color: rgba(255, 255, 255, 0.7);
  font-weight: normal;
}

.app-topbar-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.icon-action-btn {
  background: rgba(255, 255, 255, 0.12);
  border: none;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.icon-action-btn:hover {
  background: rgba(255, 255, 255, 0.25);
}

.badge-count {
  position: absolute;
  top: -4px;
  right: -4px;
  background: #ef4444;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: bold;
  padding: 2px 6px;
  border-radius: 999px;
  border: 2px solid #091e1b;
}

.mode-switch-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: rgba(212, 175, 55, 0.2);
  border: 1px solid rgba(212, 175, 55, 0.5);
  color: #d4af37;
  padding: 7px 12px;
  border-radius: 8px;
  font-size: 0.82rem;
  font-weight: 700;
  text-decoration: none;
  transition: all 0.2s;
}

.mode-switch-btn:hover {
  background: #d4af37;
  color: #091e1b;
}

/* ============================================================== */
/* NOTIFICATIONS WRAPPER                                          */
/* ============================================================== */
.app-notifications-wrapper {
  position: absolute;
  top: 60px;
  left: 0;
  right: 0;
  z-index: 35;
  padding: 6px 16px;
  max-width: 800px;
  margin: 0 auto;
}

.app-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 14px;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 600;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  margin-bottom: 6px;
}

.success-banner {
  background: #dcfce7;
  color: #15803d;
  border: 1px solid #86efac;
}

.error-banner {
  background: #fee2e2;
  color: #b91c1c;
  border: 1px solid #fca5a5;
}

.app-banner button {
  background: transparent;
  border: none;
  font-size: 1.2rem;
  color: inherit;
  cursor: pointer;
}

/* ============================================================== */
/* MAIN CONTENT SCROLL AREA                                       */
/* ============================================================== */
.app-main-content {
  flex: 1;
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  padding: 16px;
  padding-bottom: 84px; /* Default for mobile */
}

@media (min-width: 992px) {
  .app-main-content {
    padding-bottom: 20px; /* No mobile bottom navbar on desktop/tablet */
  }
}

/* ============================================================== */
/* TAB 1: SƠ ĐỒ BÀN — COMPACT SUMMARY & NATIVE DROPDOWNS          */
/* ============================================================== */

.tables-summary-compact {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 8px 14px;
  margin-bottom: 10px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.03);
}

.summary-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.summary-total {
  font-size: 0.88rem;
  font-weight: 800;
  color: #0f172a;
}
.summary-total strong {
  color: #0f766e;
}

.summary-tag {
  background: #e8f5e9;
  color: #2e7d32;
  font-size: 0.68rem;
  font-weight: 900;
  padding: 2px 6px;
  border-radius: 4px;
  letter-spacing: 0.3px;
}

.summary-dots {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 0.78rem;
}

.stat-dot {
  font-weight: 600;
}
.stat-dot strong {
  font-weight: 800;
}
.dot-empty { color: #16a34a; }
.dot-serving { color: #d97706; }
.dot-reserved { color: #2563eb; }

/* Filter bar */
.tables-filter-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 14px;
  flex-wrap: nowrap;
  overflow-x: auto;
  scrollbar-width: none;
}
.tables-filter-bar::-webkit-scrollbar { display: none; }

.filter-select-wrap {
  flex-shrink: 0;
  min-width: 165px;
}

.app-select-control {
  width: 100%;
  height: 38px;
  padding: 6px 30px 6px 12px;
  font-size: 0.85rem;
  font-weight: 700;
  color: #1e293b;
  background-color: #ffffff;
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'%3e%3cpath fill='none' stroke='%23343a40' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='m2 5 6 6 6-6'/%3e%3c/svg%3e");
  background-repeat: no-repeat;
  background-position: right 10px center;
  background-size: 14px 10px;
  border: 1.5px solid #cbd5e1;
  border-radius: 8px;
  outline: none;
  cursor: pointer;
  appearance: none;
  -webkit-appearance: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}
.app-select-control:focus {
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15);
}

.filter-search-wrap {
  flex: 1;
  min-width: 140px;
  display: flex;
  align-items: center;
  position: relative;
  background: #ffffff;
  border: 1.5px solid #cbd5e1;
  border-radius: 8px;
  height: 38px;
  padding: 0 10px;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}
.filter-search-wrap:focus-within {
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15);
}
.search-icon {
  font-size: 0.82rem;
  margin-right: 6px;
  opacity: 0.7;
}
.app-search-control {
  flex: 1;
  border: none;
  outline: none;
  font-size: 0.85rem;
  font-weight: 600;
  color: #1e293b;
  background: transparent;
}
.app-search-control::placeholder {
  color: #94a3b8;
  font-weight: 500;
}
.clear-search-btn {
  border: none;
  background: #e2e8f0;
  color: #475569;
  border-radius: 50%;
  width: 18px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 800;
  cursor: pointer;
}

.filter-count-chip {
  flex-shrink: 0;
  background: #f1f5f9;
  color: #475569;
  font-size: 0.75rem;
  font-weight: 800;
  padding: 8px 12px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  white-space: nowrap;
}

/* ============================================================== */
/* TABLE CARDS — Premium Redesign                                  */
/* ============================================================== */
.tables-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(155px, 1fr));
  gap: 12px;
}
@media (min-width: 640px) {
  .tables-grid { grid-template-columns: repeat(auto-fill, minmax(176px, 1fr)); gap: 14px; }
}
@media (min-width: 1024px) {
  .tables-grid { grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px; }
}
@media (min-width: 1440px) {
  .tables-grid { grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 18px; }
}

.app-table-card {
  border-radius: 16px;
  border: 2px solid transparent;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 3px 12px rgba(0,0,0,0.08);
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
  position: relative;
  background: #ffffff;
}
.app-table-card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(0,0,0,0.12); }
.app-table-card:active { transform: scale(0.97); }

/* Status border colors */
.table-status-empty   { border-color: #4ade80; }
.table-status-serving { border-color: #facc15; }
.table-status-reserved { border-color: #60a5fa; }
.is-active-selected   { outline: 3px solid #0f3530; outline-offset: 2px; }

/* Gradient header */
.tc-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 11px 13px 10px;
  background: linear-gradient(135deg, #0f3530 0%, #1a5c54 100%);
  position: relative;
}
.table-status-empty   .tc-header { background: linear-gradient(135deg, #14532d 0%, #16a34a 100%); }
.table-status-serving  .tc-header { background: linear-gradient(135deg, #78350f 0%, #d97706 100%); }
.table-status-reserved .tc-header { background: linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%); }

.tc-header-left { display: flex; flex-direction: column; gap: 2px; }
.tc-name {
  font-size: 1.15rem;
  font-weight: 900;
  color: #ffffff;
  letter-spacing: 0.01em;
  line-height: 1.1;
}
.tc-area {
  font-size: 0.68rem;
  color: rgba(255,255,255,0.72);
  font-weight: 600;
  letter-spacing: 0.02em;
  text-transform: uppercase;
}
.tc-capacity-badge {
  background: rgba(255,255,255,0.18);
  border: 1px solid rgba(255,255,255,0.3);
  border-radius: 8px;
  padding: 3px 8px;
  font-size: 0.73rem;
  font-weight: 700;
  color: #ffffff;
  white-space: nowrap;
  backdrop-filter: blur(4px);
}

/* Ready alert strip */
.tc-ready-alert {
  background: linear-gradient(90deg, #ef4444, #dc2626);
  color: #ffffff;
  font-size: 0.71rem;
  font-weight: 800;
  padding: 5px 10px;
  text-align: center;
  cursor: pointer;
  letter-spacing: 0.01em;
}

/* Body */
.tc-body {
  padding: 10px 13px 8px;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.tc-order-meta {
  display: flex;
  align-items: center;
  gap: 6px;
}
.tc-order-id {
  font-size: 0.8rem;
  font-weight: 800;
  color: #854d0e;
  background: rgba(251, 191, 36, 0.15);
  padding: 2px 7px;
  border-radius: 6px;
}
.tc-badge {
  font-size: 0.65rem;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 999px;
}
.tc-badge-sub  { background: #fee2e2; color: #b91c1c; }
.tc-badge-link { background: #dbeafe; color: #1d4ed8; }

.tc-stats {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
.tc-stat-item {
  font-size: 0.74rem;
  color: #475569;
  font-weight: 600;
}
.tc-stat-ok   { color: #16a34a; }
.tc-stat-cook { color: #c2410c; }

.tc-amount {
  font-size: 1.05rem;
  font-weight: 900;
  color: #b91c1c;
  text-align: right;
  margin-top: 2px;
}

/* Status labels */
.tc-status-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.8rem;
  font-weight: 700;
  padding: 6px 0;
}
.tc-status-empty   { color: #15803d; }
.tc-status-cleaning { color: #64748b; }
.tc-status-dot {
  width: 8px; height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}
.dot-green { background: #22c55e; box-shadow: 0 0 0 2px rgba(34,197,94,0.25); }
.dot-grey  { background: #94a3b8; }

.tc-hint {
  font-size: 0.7rem;
  color: #94a3b8;
  font-weight: 500;
}

/* Reserved info */
.tc-reserved-name  { font-size: 0.83rem; font-weight: 700; color: #1d4ed8; }
.tc-reserved-time  { font-size: 0.73rem; color: #475569; margin-top: 2px; }
.tc-reserved-guests { font-size: 0.71rem; color: #64748b; margin-top: 1px; }

/* Footer action button */
.tc-footer { padding: 0 12px 12px; }
.tc-action-btn {
  width: 100%;
  border: none;
  border-radius: 10px;
  padding: 9px 8px;
  font-size: 0.8rem;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.15s;
  letter-spacing: 0.01em;
}
.tc-btn-serving  { background: linear-gradient(135deg,#d97706,#f59e0b); color: #ffffff; }
.tc-btn-serving:hover  { background: linear-gradient(135deg,#b45309,#d97706); }
.tc-btn-open     { background: linear-gradient(135deg,#16a34a,#22c55e); color: #ffffff; }
.tc-btn-open:hover     { background: linear-gradient(135deg,#15803d,#16a34a); }
.tc-btn-checkin  { background: linear-gradient(135deg,#2563eb,#3b82f6); color: #ffffff; }
.tc-btn-checkin:hover  { background: linear-gradient(135deg,#1d4ed8,#2563eb); }
.tc-btn-secondary { background: #f1f5f9; color: #475569; border: 1px solid #e2e8f0; }

/* ============================================================== */
/* TAB 2: GỌI MÓN (ORDER DESK) - 2-COLUMN SPLIT ON LANDSCAPE      */
/* ============================================================== */
.order-desk-header {
  background: #ffffff;
  border-radius: 12px;
  padding: 12px 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  margin-bottom: 14px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
}

.desk-table-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.desk-back-btn {
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  width: 38px;
  height: 38px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
  cursor: pointer;
}

.desk-table-name {
  font-size: 1.2rem;
  font-weight: 800;
  color: #0f172a;
  display: flex;
  align-items: center;
  gap: 8px;
}

.desk-table-sub {
  font-size: 0.8rem;
  color: #64748b;
  margin-top: 2px;
}

.desk-quick-actions {
  display: flex;
  gap: 8px;
}

.action-pill-btn {
  background: #f8fafc;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 6px 12px;
  font-size: 0.8rem;
  font-weight: 600;
  color: #334155;
  cursor: pointer;
  white-space: nowrap;
}

.btn-danger-pill {
  border-color: #fca5a5;
  color: #b91c1c;
  background: #fef2f2;
}

/* Side-by-side Layout on Landscape / Tablet / Desktop */
.order-split-layout {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}

.order-catalog-pane {
  flex: 1;
  min-width: 0;
}

.order-cart-side-pane {
  width: 380px;
  position: sticky;
  top: 10px;
  flex-shrink: 0;
}

@media (min-width: 1280px) {
  .order-cart-side-pane {
    width: 420px;
  }
}

.side-cart-card {
  background: #ffffff;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
  display: flex;
  flex-direction: column;
  max-height: calc(100vh - 120px);
  width: 100%;
}

.side-cart-header {
  padding: 14px 16px;
  border-bottom: 1px solid #e2e8f0;
  background: #f8fafc;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-top-left-radius: 14px;
  border-top-right-radius: 14px;
}

.side-cart-title {
  font-size: 1rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.side-cart-items-body {
  padding: 12px 16px;
  overflow-y: auto;
  flex: 1;
}

.side-cart-footer {
  padding: 14px 16px;
  border-top: 1px solid #e2e8f0;
  background: #f8fafc;
  border-bottom-left-radius: 14px;
  border-bottom-right-radius: 14px;
}

.food-search-box {
  margin-bottom: 12px;
}

.category-pills-row {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  margin-bottom: 14px;
  scrollbar-width: none;
}
.category-pills-row::-webkit-scrollbar { display: none; }

.cat-pill {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
  padding: 6px 14px;
  font-size: 0.82rem;
  font-weight: 700;
  color: #475569;
  white-space: nowrap;
  cursor: pointer;
  transition: all 0.2s;
}

.cat-pill.active {
  background: #0f3530;
  border-color: #0f3530;
  color: #d4af37;
}

.food-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 12px;
}

@media (min-width: 640px) {
  .food-cards-grid {
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 14px;
  }
}

@media (min-width: 1200px) {
  .food-cards-grid {
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 16px;
  }
}

.app-food-card {
  background: #ffffff;
  border-radius: 14px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.05);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.app-food-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 14px rgba(0, 0, 0, 0.08);
}

.app-food-card:active {
  transform: scale(0.98);
}

.food-card-media {
  position: relative;
  width: 100%;
  aspect-ratio: 4 / 3;
  background: #1e293b;
  overflow: hidden;
}

.food-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s;
}
.app-food-card:hover .food-img {
  transform: scale(1.05);
}

.food-img-fallback {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 3rem;
  background: linear-gradient(135deg, #091e1b, #134e4a);
}

.food-variant-tag {
  position: absolute;
  bottom: 6px;
  right: 6px;
  background: rgba(0, 0, 0, 0.7);
  color: #ffffff;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 4px;
}

.food-card-body {
  padding: 10px;
  display: flex;
  flex-direction: column;
  flex: 1;
  justify-content: space-between;
}

.food-name {
  font-size: 0.9rem;
  font-weight: 700;
  color: #0f172a;
  line-height: 1.3;
  margin-bottom: 4px;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.food-desc {
  font-size: 0.74rem;
  color: #64748b;
  line-height: 1.3;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  margin-bottom: 8px;
}

.food-card-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: auto;
}

.food-price {
  font-size: 1rem;
  font-weight: 800;
  color: #b91c1c;
}

.food-add-btn {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  background: #0f3530;
  color: #d4af37;
  border: none;
  font-size: 1.25rem;
  font-weight: bold;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(15, 53, 48, 0.3);
  transition: all 0.15s;
}

.food-add-btn:active {
  transform: scale(0.9);
}

/* ============================================================== */
/* TAB 3: BẾP TRẢ MÓN (READY ITEMS GRID)                          */
/* ============================================================== */
.pane-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 14px;
}

.pane-title {
  font-size: 1.15rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.pane-sub {
  font-size: 0.8rem;
  color: #64748b;
  margin: 3px 0 0;
}

.btn-refresh-sm {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 6px 12px;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}

.ready-items-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 12px;
}

@media (min-width: 768px) {
  .ready-items-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (min-width: 1200px) {
  .ready-items-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}

.ready-item-card {
  background: #ffffff;
  border-radius: 14px;
  border: 2px solid #86efac;
  padding: 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 2px 8px rgba(34, 197, 94, 0.15);
}

.ready-card-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.ready-table-badge {
  background: #0f3530;
  color: #d4af37;
  font-size: 1.05rem;
  font-weight: 800;
  padding: 10px 14px;
  border-radius: 10px;
  white-space: nowrap;
}

.ready-food-name {
  font-size: 0.98rem;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}

.ready-variant-name {
  font-size: 0.82rem;
  color: #475569;
  margin-top: 2px;
}

.ready-note-text {
  font-size: 0.75rem;
  color: #b45309;
  background: #fef3c7;
  padding: 2px 6px;
  border-radius: 4px;
  margin-top: 4px;
}

.btn-served-action {
  background: linear-gradient(135deg, #22c55e, #16a34a);
  color: #ffffff;
  border: none;
  border-radius: 10px;
  padding: 10px 16px;
  font-size: 0.85rem;
  font-weight: 800;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 6px;
  box-shadow: 0 3px 8px rgba(34, 197, 94, 0.35);
  white-space: nowrap;
}

.btn-served-action:active {
  transform: scale(0.96);
}

/* ============================================================== */
/* TAB 4: ĐẶT BÀN (VIEW NGANG TRÊN TABLET / LAPTOP >= 768px)       */
/* ============================================================== */
.res-horizontal-table thead th {
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.03em;
  background-color: #091e1b !important;
  color: #d4af37 !important;
}

.res-horizontal-table tbody tr {
  transition: background-color 0.15s;
}

.table-success-subtle {
  background-color: #f0fdf4 !important;
}

/* Mobile cards for < 768px */
.reservations-cards-mobile {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.reservation-card {
  background: #ffffff;
  border-radius: 12px;
  border: 1px solid #cbd5e1;
  padding: 12px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.05);
}

.border-res-confirmed { border-left: 5px solid #3b82f6; }
.border-res-checkedin { border-left: 5px solid #22c55e; }

.res-card-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.res-guest-name {
  font-size: 0.95rem;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}

.res-guest-phone {
  font-size: 0.78rem;
  color: #64748b;
  margin-top: 2px;
}

.res-card-mid {
  display: flex;
  gap: 16px;
  margin: 8px 0;
  font-size: 0.8rem;
}

.meta-label { color: #64748b; }

.res-note {
  font-size: 0.78rem;
  background: #f8fafc;
  padding: 6px 10px;
  border-radius: 6px;
  color: #334155;
  margin-bottom: 8px;
}

.btn-checkin-action {
  width: 100%;
  background: #3b82f6;
  color: #ffffff;
  border: none;
  border-radius: 8px;
  padding: 8px;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
}

/* ============================================================== */
/* TAB 5: TIỆN ÍCH (MORE)                                         */
/* ============================================================== */
.user-profile-card {
  background: linear-gradient(135deg, #091e1b, #134e4a);
  color: #ffffff;
  border-radius: 14px;
  padding: 16px;
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 16px;
  box-shadow: 0 4px 12px rgba(15, 53, 48, 0.2);
}

.profile-avatar {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.8rem;
}

.profile-name {
  font-size: 1.15rem;
  font-weight: 800;
  margin: 0;
}

.profile-role {
  font-size: 0.82rem;
  color: #d4af37;
  font-weight: 600;
}

.profile-status {
  font-size: 0.75rem;
  color: #86efac;
  margin-top: 2px;
}

.menu-action-group {
  background: #ffffff;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.menu-group-title {
  padding: 10px 14px 6px;
  font-size: 0.72rem;
  font-weight: 800;
  color: #64748b;
  letter-spacing: 0.05em;
  background: #f8fafc;
  border-bottom: 1px solid #f1f5f9;
}

.menu-action-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 14px;
  border-bottom: 1px solid #f1f5f9;
  text-decoration: none;
  color: inherit;
  cursor: pointer;
  transition: background 0.15s;
}

.menu-action-item:last-child {
  border-bottom: none;
}

.menu-action-item:hover {
  background: #f8fafc;
}

.item-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.item-icon {
  font-size: 1.3rem;
}

.item-title {
  font-size: 0.9rem;
  font-weight: 700;
  color: #0f172a;
}

.item-sub {
  font-size: 0.75rem;
  color: #64748b;
  margin-top: 2px;
}

.item-arrow {
  color: #94a3b8;
  font-size: 0.85rem;
}

/* ============================================================== */
/* FLOATING CART BAR (MOBILE ONLY)                                */
/* ============================================================== */
.floating-cart-bar {
  position: absolute;
  bottom: 66px;
  left: 12px;
  right: 12px;
  background: #091e1b;
  color: #ffffff;
  border: 1px solid #d4af37;
  border-radius: 14px;
  padding: 10px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
  z-index: 45;
  cursor: pointer;
  animation: pulse-glow 2s infinite;
}

.cart-bar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.cart-icon-wrapper {
  position: relative;
  font-size: 1.4rem;
}

.cart-badge-count {
  position: absolute;
  top: -6px;
  right: -8px;
  background: #ef4444;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 800;
  padding: 2px 6px;
  border-radius: 999px;
  border: 1px solid #091e1b;
}

.cart-bar-total {
  font-size: 1.05rem;
  font-weight: 800;
  color: #d4af37;
}

.cart-bar-sub {
  font-size: 0.72rem;
  color: #cbd5e1;
}

.text-warning-highlight {
  color: #facc15;
  font-weight: 700;
}

.cart-view-btn {
  background: #d4af37;
  color: #091e1b;
  border: none;
  border-radius: 8px;
  padding: 6px 12px;
  font-size: 0.82rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
}

/* ============================================================== */
/* BOTTOM NAVIGATION BAR (MOBILE ONLY)                            */
/* ============================================================== */
.app-bottom-navbar {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 62px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: space-around;
  box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.05);
  z-index: 50;
  padding-bottom: env(safe-area-inset-bottom);
}

.nav-tab-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: transparent;
  border: none;
  color: #64748b;
  cursor: pointer;
  padding: 6px 0;
  transition: all 0.15s;
}

.nav-tab-icon {
  font-size: 1.25rem;
  line-height: 1;
}

.nav-tab-label {
  font-size: 0.68rem;
  font-weight: 600;
  margin-top: 2px;
}

.nav-tab-item.active {
  color: #0f3530;
}

.nav-tab-item.active .nav-tab-icon {
  transform: translateY(-2px);
}

.nav-tab-item.active .nav-tab-label {
  font-weight: 800;
  color: #0f3530;
}

.nav-active-table-badge {
  position: absolute;
  top: 2px;
  right: 18%;
  background: #d4af37;
  color: #091e1b;
  font-size: 0.58rem;
  font-weight: 800;
  padding: 1px 4px;
  border-radius: 4px;
}

.nav-notification-badge {
  position: absolute;
  top: 2px;
  right: 22%;
  background: #ef4444;
  color: #ffffff;
  font-size: 0.6rem;
  font-weight: 800;
  padding: 1px 5px;
  border-radius: 999px;
}

/* ============================================================== */
/* SLIDE-UP CART DRAWER & VARIANT SHEET                           */
/* ============================================================== */
.app-drawer-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(4px);
  z-index: 100;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
}

.app-cart-drawer {
  background: #ffffff;
  border-top-left-radius: 20px;
  border-top-right-radius: 20px;
  max-height: 82dvh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 -8px 24px rgba(0, 0, 0, 0.25);
  overflow: hidden;
}

.drawer-header {
  padding: 14px 16px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #f8fafc;
}

.drawer-title {
  font-size: 1.05rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.drawer-meta {
  font-size: 0.72rem;
  color: #64748b;
  margin-top: 2px;
}

.drawer-close-btn {
  background: #e2e8f0;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 1rem;
  font-weight: bold;
  color: #475569;
  cursor: pointer;
}

.drawer-body {
  padding: 12px 16px;
  overflow-y: auto;
  flex: 1;
}

.drawer-items-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.drawer-item-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding-bottom: 10px;
  border-bottom: 1px solid #f1f5f9;
}

.item-main-info {
  flex: 1;
}

.item-food-name {
  font-size: 0.9rem;
  font-weight: 700;
  color: #0f172a;
}

.item-variant-tag {
  display: inline-block;
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  font-size: 0.7rem;
  font-weight: 600;
  color: #475569;
  padding: 1px 6px;
  border-radius: 4px;
  margin-left: 6px;
}

.item-unit-price {
  font-size: 0.75rem;
  color: #64748b;
  margin-top: 2px;
}

.item-note-pill {
  font-size: 0.72rem;
  background: #fef3c7;
  color: #92400e;
  padding: 2px 6px;
  border-radius: 4px;
  margin-top: 4px;
  display: inline-block;
}

.item-control-side {
  text-align: right;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}

.item-line-total {
  font-size: 0.92rem;
  font-weight: 800;
  color: #b91c1c;
}

.item-actions-row {
  display: flex;
  align-items: center;
  gap: 6px;
}

.item-status-badge {
  font-size: 0.68rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 4px;
}

.badge-status-pending { background: #fef08a; color: #854d0e; }
.badge-status-confirmed { background: #bae6fd; color: #0369a1; }
.badge-status-cooking { background: #fed7aa; color: #c2410c; }
.badge-status-ready { background: #bbf7d0; color: #15803d; }
.badge-status-served { background: #f1f5f9; color: #64748b; }
.badge-status-cancelled { background: #fecaca; color: #b91c1c; }

.stepper-group {
  display: flex;
  align-items: center;
  gap: 4px;
  background: #f1f5f9;
  border-radius: 6px;
  padding: 2px 4px;
}

.stepper-btn {
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 4px;
  width: 22px;
  height: 22px;
  font-size: 0.85rem;
  font-weight: bold;
  cursor: pointer;
}

.stepper-val {
  font-size: 0.85rem;
  font-weight: 700;
  min-width: 16px;
  text-align: center;
}

.stepper-trash {
  background: transparent;
  border: none;
  font-size: 0.85rem;
  cursor: pointer;
  margin-left: 2px;
}

.item-fixed-qty {
  font-size: 0.8rem;
  color: #475569;
}

.drawer-footer {
  padding: 12px 16px;
  border-top: 1px solid #e2e8f0;
  background: #f8fafc;
}

.drawer-subtotal-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.9rem;
  color: #475569;
  margin-bottom: 10px;
}

.subtotal-amount {
  font-size: 1.25rem;
  font-weight: 800;
  color: #b91c1c;
}

.drawer-action-buttons {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.btn-drawer-send-kitchen {
  width: 100%;
  background: #eab308;
  color: #0f172a;
  border: none;
  border-radius: 10px;
  padding: 12px 14px;
  font-size: 0.95rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
  box-shadow: 0 2px 6px rgba(234, 179, 8, 0.3);
}

.badge-pending {
  background: #0f172a;
  color: #ffffff;
  font-size: 0.72rem;
  padding: 2px 8px;
  border-radius: 999px;
}

.btn-drawer-checkout {
  width: 100%;
  background: linear-gradient(135deg, #091e1b, #0f3530);
  color: #d4af37;
  border: 1px solid #d4af37;
  border-radius: 10px;
  padding: 12px 14px;
  font-size: 0.92rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(15, 53, 48, 0.35);
}

.btn-drawer-send-kitchen:disabled, .btn-drawer-checkout:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.drawer-sub-actions {
  display: flex;
  gap: 6px;
  margin-top: 8px;
}

.btn-sub-action {
  flex: 1;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 6px;
  font-size: 0.76rem;
  font-weight: 600;
  color: #475569;
  cursor: pointer;
}

/* Variant Sheet */
.app-variant-sheet {
  background: #ffffff;
  border-top-left-radius: 20px;
  border-top-right-radius: 20px;
  max-height: 80dvh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 -8px 24px rgba(0, 0, 0, 0.25);
  overflow: hidden;
  max-width: 600px;
  margin: 0 auto;
  width: 100%;
}

.variant-sheet-header {
  padding: 14px 16px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  background: #f8fafc;
}

.sheet-title {
  font-size: 1.1rem;
  font-weight: 800;
  color: #0f172a;
  margin: 0;
}

.sheet-desc {
  font-size: 0.75rem;
  color: #64748b;
  margin: 2px 0 0;
}

.sheet-close-btn {
  background: #e2e8f0;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 1rem;
  cursor: pointer;
}

.variant-sheet-body {
  padding: 14px 16px;
  overflow-y: auto;
  flex: 1;
}

.sheet-section {
  margin-bottom: 14px;
}

.sheet-section-label {
  display: block;
  font-size: 0.75rem;
  font-weight: 800;
  color: #475569;
  margin-bottom: 8px;
}

.variant-options-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
  gap: 8px;
}

.variant-option-card {
  background: #f8fafc;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 10px;
  text-align: center;
  cursor: pointer;
  transition: all 0.15s;
}

.variant-option-card.active {
  background: #0f3530;
  border-color: #0f3530;
}

.variant-option-card.active .option-name { color: #ffffff; }
.variant-option-card.active .option-price { color: #d4af37; }

.option-name {
  font-size: 0.85rem;
  font-weight: 700;
  color: #0f172a;
}

.option-price {
  font-size: 0.82rem;
  font-weight: 800;
  color: #b91c1c;
  margin-top: 2px;
}

.sheet-stepper-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.big-stepper-btn {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  border: 1px solid #cbd5e1;
  background: #f8fafc;
  font-size: 1.4rem;
  font-weight: bold;
  cursor: pointer;
}

.big-stepper-val {
  font-size: 1.4rem;
  font-weight: 800;
  min-width: 40px;
  text-align: center;
}

.quick-notes-pills {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-bottom: 8px;
}

.quick-note-chip {
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
  padding: 4px 10px;
  font-size: 0.75rem;
  color: #475569;
  cursor: pointer;
}

.quick-note-chip:hover { background: #e2e8f0; }

.app-note-input {
  width: 100%;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 8px 10px;
  font-size: 0.85rem;
  outline: none;
}

.variant-sheet-footer {
  padding: 12px 16px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
}

.btn-confirm-add {
  width: 100%;
  background: linear-gradient(135deg, #091e1b, #0f3530);
  color: #d4af37;
  border: 1px solid #d4af37;
  border-radius: 10px;
  padding: 14px;
  font-size: 0.95rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: space-between;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(15, 53, 48, 0.35);
}

.add-total-val { font-size: 1.05rem; }

/* ============================================================== */
/* TOAST REALTIME ALERT                                           */
/* ============================================================== */
.app-toast-alert {
  position: absolute;
  top: 64px;
  left: 16px;
  right: 16px;
  max-width: 500px;
  margin: 0 auto;
  background: #ffffff;
  border: 2px solid #22c55e;
  border-radius: 12px;
  padding: 12px;
  display: flex;
  align-items: center;
  gap: 10px;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.2);
  z-index: 110;
  cursor: pointer;
}

.toast-icon { font-size: 1.6rem; }
.toast-info { flex: 1; }
.toast-title { font-size: 0.75rem; font-weight: 800; color: #15803d; }
.toast-desc { font-size: 0.82rem; color: #334155; margin-top: 2px; }
.toast-close { background: transparent; border: none; font-size: 1.3rem; color: #94a3b8; cursor: pointer; }

/* ============================================================== */
/* COMMON STATES & SPINNERS                                       */
/* ============================================================== */
.app-loading-state, .app-empty-state {
  text-align: center;
  padding: 40px 16px;
  color: #64748b;
}

.app-spinner {
  width: 36px;
  height: 36px;
  border: 3px solid #cbd5e1;
  border-top-color: #0f3530;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin: 0 auto 12px;
}

.empty-icon { font-size: 3rem; margin-bottom: 8px; }
.empty-title { font-size: 1.05rem; font-weight: 700; color: #334155; }
.empty-desc { font-size: 0.8rem; color: #64748b; max-width: 340px; margin: 6px auto 0; }

.btn-primary-gradient {
  background: linear-gradient(135deg, #091e1b, #0f3530);
  color: #d4af37;
  border: 1px solid #d4af37;
  border-radius: 8px;
  cursor: pointer;
}

/* ============================================================== */
/* ANIMATIONS                                                     */
/* ============================================================== */
@keyframes spin { to { transform: rotate(360deg); } }
.spin-anim { display: inline-block; animation: spin 0.8s linear infinite; }

@keyframes pulse-glow {
  0%, 100% { box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4); }
  50% { box-shadow: 0 6px 20px rgba(212, 175, 55, 0.5); }
}
.animate-pulse { animation: pulse-glow 1.5s infinite; }

@keyframes bounce {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-4px); }
}
.animate-bounce { animation: bounce 1s infinite; }

.drawer-fade-enter-active, .drawer-fade-leave-active { transition: opacity 0.25s ease; }
.drawer-fade-enter-from, .drawer-fade-leave-to { opacity: 0; }

.slide-up-enter-active, .slide-up-leave-active { transition: transform 0.25s ease, opacity 0.25s ease; }
.slide-up-enter-from, .slide-up-leave-to { transform: translateY(30px); opacity: 0; }

.slide-down-enter-active, .slide-down-leave-active { transition: transform 0.25s ease, opacity 0.25s ease; }
.slide-down-enter-from, .slide-down-leave-to { transform: translateY(-20px); opacity: 0; }

/* ============================================================== */
/* RESERVATIONS TAB STYLES                                        */
/* ============================================================== */
.res-control-toolbar {
  border-color: #e2e8f0;
}

.res-search-wrap {
  position: relative;
  flex: 1 1 240px;
  max-width: 380px;
}

.res-search-icon {
  position: absolute;
  left: 10px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 0.8rem;
  color: #94a3b8;
}

.res-search-input {
  padding-left: 30px;
  padding-right: 28px;
  border-radius: 8px;
}

.res-search-clear {
  position: absolute;
  right: 8px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  font-size: 0.75rem;
  color: #94a3b8;
  cursor: pointer;
}

.res-kpi-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.kpi-chip-sm {
  padding: 5px 10px;
  border-radius: 999px;
  border: 1px solid #e2e8f0;
  background: #f8fafc;
  color: #475569;
  font-size: 0.74rem;
  font-weight: 700;
  display: inline-flex;
  align-items: center;
  gap: 5px;
  cursor: pointer;
  transition: all 0.15s;
}

.kpi-chip-sm:hover {
  background: #f1f5f9;
}

.kpi-chip-sm .count-tag {
  padding: 1px 6px;
  border-radius: 999px;
  background: #fff;
  font-size: 0.68rem;
  font-weight: 800;
  box-shadow: 0 1px 2px rgba(0,0,0,0.06);
}

.kpi-chip-sm.active {
  background: #0f172a;
  color: #fff;
  border-color: #0f172a;
}

.kpi-chip-sm.active .count-tag {
  background: rgba(255, 255, 255, 0.2);
  color: #fff;
}

.kpi-chip-sm.chip-amber.active { background: #d97706; border-color: #d97706; }
.kpi-chip-sm.chip-orange.active { background: #ea580c; border-color: #ea580c; }
.kpi-chip-sm.chip-blue.active { background: #2563eb; border-color: #2563eb; }
.kpi-chip-sm.chip-green.active { background: #059669; border-color: #059669; }

.countdown-pill-sm {
  font-size: 0.68rem;
  font-weight: 800;
  padding: 2px 7px;
  border-radius: 999px;
}

.countdown-pill-sm.badge-overdue {
  background: #fee2e2;
  color: #dc2626;
  border: 1px solid #fca5a5;
  animation: pulse-glow 1.5s infinite;
}

.countdown-pill-sm.badge-upcoming {
  background: #fef3c7;
  color: #b45309;
  border: 1px solid #fde68a;
}

.countdown-pill-sm.badge-normal {
  background: #e0f2fe;
  color: #0369a1;
  border: 1px solid #bae6fd;
}

.countdown-pill-sm.badge-checked-in {
  background: #dcfce7;
  color: #15803d;
  border: 1px solid #bbf7d0;
}

.countdown-pill-sm.badge-cancelled {
  background: #f1f5f9;
  color: #64748b;
}

.app-res-card {
  border-top-width: 4px !important;
  transition: all 0.18s;
}

.res-border-pending { border-top-color: #f59e0b !important; }
.res-border-confirmed { border-top-color: #3b82f6 !important; }
.res-border-checked_in { border-top-color: #10b981 !important; }
.res-border-cancelled { border-top-color: #94a3b8 !important; opacity: 0.65; }

/* Sub-modal overlays */
.app-sub-modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 1300;
  display: grid;
  place-items: center;
  padding: 16px;
  background: rgba(15, 23, 42, 0.65);
  backdrop-filter: blur(4px);
}

.app-sub-modal-dialog {
  width: min(520px, 100%);
  max-height: 85vh;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 14px;
  overflow: hidden;
  box-shadow: 0 20px 45px rgba(0, 0, 0, 0.25);
}
</style>

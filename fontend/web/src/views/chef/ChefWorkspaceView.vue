<template>
  <div class="chef-kds d-flex flex-column min-vh-100 bg-light-app">
    <!-- ============================================================== -->
    <!-- 1. HEADER: THANH ĐIỀU HƯỚNG BẾP & QUẦY RA MÓN (LIGHT MODERN) -->
    <!-- ============================================================== -->
    <header class="navbar navbar-expand-lg bg-white px-3 py-2 shadow-sm border-bottom sticky-top">
      <div class="container-fluid d-flex flex-wrap justify-content-between align-items-center gap-2">
        <!-- Brand & Title -->
        <div class="d-flex align-items-center gap-3">
          <div class="brand-icon shadow-sm">
            <span>👨‍🍳</span>
          </div>
          <div>
            <div class="d-flex align-items-center gap-2">
              <h5 class="mb-0 text-slate-800 fw-bold tracking-wide">KITCHEN DISPLAY SYSTEM</h5>
              <span class="badge bg-indigo-subtle text-indigo border border-indigo-subtle fw-bold text-uppercase" style="font-size: 0.65rem;">
                KDS ĐA TRẠM BẾP
              </span>
            </div>
            <small class="text-muted">Hệ thống điều phối 3 quầy bếp: Quầy Nướng & Chiên • Quầy Nấu & Lẩu • Quầy Pha Chế & Đồ Uống</small>
          </div>
        </div>

        <!-- Clock & KPI Summary -->
        <div class="d-flex align-items-center gap-3">
          <!-- Digital Clock -->
          <div class="clock-display px-3 py-1 rounded-3 shadow-sm border bg-slate-50 text-center font-monospace">
            <span class="text-amber-600 fw-bold fs-5">{{ currentTime }}</span>
          </div>

          <!-- KPI Summary Badges -->
          <div class="d-none d-xl-flex align-items-center gap-2">
            <div class="kpi-pill kpi-pending" :class="{ 'pulse-alert': metrics.confirmedCount > 0 }">
              <span class="kpi-dot bg-amber"></span>
              <span class="kpi-label">Chờ làm</span>
              <span class="kpi-val">{{ metrics.confirmedCount }}</span>
            </div>
            <div class="kpi-pill kpi-cooking">
              <span class="kpi-dot bg-blue"></span>
              <span class="kpi-label">Đang nấu</span>
              <span class="kpi-val">{{ metrics.cookingCount }}</span>
            </div>
            <div class="kpi-pill kpi-ready" :class="{ 'pulse-ready': metrics.readyCount > 0 }">
              <span class="kpi-dot bg-emerald"></span>
              <span class="kpi-label">Chờ bưng</span>
              <span class="kpi-val text-emerald">{{ metrics.readyCount }}</span>
            </div>
            <div class="kpi-pill kpi-soldout" v-if="metrics.soldOutFoodsCount > 0">
              <span class="kpi-dot bg-rose"></span>
              <span class="kpi-label">Hết món</span>
              <span class="kpi-val text-rose">{{ metrics.soldOutFoodsCount }}</span>
            </div>
          </div>
        </div>

        <!-- Mode Switcher Tabs & Controls -->
        <div class="d-flex align-items-center gap-2">
          <!-- View Modes -->
          <div class="btn-group shadow-sm rounded-pill p-1 bg-slate-100 border" role="group">
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 transition fw-semibold"
              :class="viewMode === 'cards' ? 'btn-white text-indigo shadow-sm fw-bold' : 'text-slate-600 border-0'"
              @click="viewMode = 'cards'"
              title="Xem theo thẻ bàn"
            >
              📑 Thẻ Bàn ({{ visibleOrdersCount }})
            </button>
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 transition fw-semibold"
              :class="viewMode === 'queue' ? 'btn-white text-indigo shadow-sm fw-bold' : 'text-slate-600 border-0'"
              @click="viewMode = 'queue'"
              title="Xem hàng đợi phẳng theo món để nấu theo mẻ"
            >
              📋 Hàng Đợi ({{ filteredQueue.length }})
            </button>
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 transition position-relative fw-semibold"
              :class="viewMode === 'pass' ? 'btn-emerald text-white shadow-sm fw-bold' : 'text-slate-600 border-0'"
              @click="viewMode = 'pass'"
              title="Màn hình treo quầy ra món để phục vụ bưng đi trực quan"
            >
              🛎️ Màn Hình Ra Món (Pass)
              <span v-if="metrics.readyCount > 0" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger border border-white">
                {{ metrics.readyCount }}
              </span>
            </button>
            <button
              type="button"
              class="btn btn-sm rounded-pill px-3 transition fw-semibold"
              :class="viewMode === 'soldout' ? 'btn-rose text-white shadow-sm fw-bold' : 'text-slate-600 border-0'"
              @click="viewMode = 'soldout'"
              title="Báo hết món"
            >
              🚫 Hết Món
            </button>
          </div>

          <!-- Sound Notification Toggle -->
          <button
            class="btn btn-sm rounded-circle p-2 border bg-white shadow-sm"
            :class="soundEnabled ? 'text-emerald border-emerald-subtle' : 'text-muted'"
            @click="toggleSound"
            :title="soundEnabled ? 'Chuông báo: ĐANG BẬT' : 'Chuông báo: ĐÃ TẮT'"
          >
            {{ soundEnabled ? '🔔' : '🔕' }}
          </button>

          <!-- SignalR Realtime Live Status -->
          <span class="badge bg-emerald-subtle text-emerald border border-emerald-subtle px-3 py-2 rounded-pill fw-bold shadow-xs d-flex align-items-center gap-1" title="Kết nối trực tiếp Real-time qua WebSocket">
            <span class="realtime-dot"></span> REALTIME LIVE
          </span>
          <button class="btn btn-outline-secondary btn-sm rounded-circle p-2 shadow-sm bg-white" @click="fetchKitchenData" :disabled="loading" title="Làm mới thủ công">
            <span :class="{ 'spinner-border spinner-border-sm': loading }">🔄</span>
          </button>

          <!-- Navigation Links -->
          <router-link v-if="userRole === 'manager' || userRole === 'admin'" to="/admin" class="btn btn-outline-secondary btn-sm rounded-circle p-2 shadow-sm bg-white" title="Về Quản Trị">
            ⚙️
          </router-link>
          <router-link to="/staff" class="btn btn-outline-indigo btn-sm rounded-pill px-3 fw-semibold shadow-sm bg-white" title="Về Màn Phục Vụ">
            🤵 Phục Vụ
          </router-link>
          <button class="btn btn-outline-danger btn-sm rounded-circle p-2 shadow-sm bg-white" @click="handleLogout" title="Đăng xuất">
            🚪
          </button>
        </div>
      </div>
    </header>

    <!-- ============================================================== -->
    <!-- 2. THANH BỘ LỌC 4 PHÂN KHU BẾP (STATION FILTER BAR) -->
    <!-- ============================================================== -->
    <div class="bg-white px-3 py-2 border-bottom shadow-xs">
      <div class="d-flex flex-column gap-2">
        <div class="d-flex justify-content-between align-items-center flex-wrap gap-2">
          <!-- Station Title & Status -->
          <div class="d-flex align-items-center gap-2">
            <span class="fs-5">📂</span>
            <span class="fw-bold text-slate-700 small text-uppercase" style="letter-spacing: 0.5px;">
              QUẦY CHẾ BIẾN:
            </span>
            <span class="badge bg-indigo text-white fw-bold fs-6 px-3 py-1 rounded-pill shadow-xs">
              {{ selectedCategoryName }}
            </span>
            <small class="text-muted d-none d-lg-inline">
              (Màn hình tự động nhớ quầy bếp này trên thiết bị của bạn)
            </small>
          </div>

          <!-- Quick Action Buttons -->
          <div class="d-flex align-items-center gap-3 flex-wrap">
            <div class="form-check form-switch m-0 text-slate-600 small d-flex align-items-center gap-2">
              <input
                class="form-check-input"
                type="checkbox"
                id="autoCookingSwitch"
                v-model="autoCookingEnabled"
                @change="onAutoCookingToggle"
              />
              <label class="form-check-label text-slate-700 fw-semibold cursor-pointer" for="autoCookingSwitch" title="Tự động nhận nấu khi có đơn mới">
                ⚡ Tự nhận nấu
              </label>
            </div>

            <!-- Xong toàn bộ quầy này (1-Touch Station Clear) -->
            <button
              v-if="currentStationPendingCount > 0"
              class="btn btn-emerald btn-sm fw-bold px-3 rounded-pill shadow-sm"
              @click="readyAllStation"
              :disabled="actionInProgress"
              title="1 Chạm: Chuyển toàn bộ món chờ của quầy này ra quầy trung chuyển"
            >
              ⚡ Xong tất cả {{ currentStationPendingCount }} món quầy này
            </button>
          </div>
        </div>

        <!-- 3 Quầy Bếp Tiêu Chuẩn (Station Filter Pills) -->
        <div class="d-flex align-items-center gap-2 overflow-auto py-1">
          <!-- Button: Tất Cả Quầy -->
          <button
            class="btn btn-sm rounded-pill text-nowrap px-3 fw-bold transition shadow-xs"
            :class="selectedCategoryId === 0 ? 'btn-indigo text-white shadow' : 'btn-outline-slate text-slate-700 bg-white'"
            @click="selectCategory(0)"
          >
            🌐 Tất Cả Quầy Bếp ({{ totalItemsAllCategories }})
          </button>

          <!-- 3 Quầy Bếp Chuẩn từ API -->
          <button
            v-for="cat in activeCategoriesWithCount"
            :key="cat.categoryId"
            class="btn btn-sm rounded-pill text-nowrap px-3 fw-bold transition shadow-xs"
            :class="selectedCategoryId === cat.categoryId ? 'btn-indigo text-white shadow' : 'btn-outline-slate text-slate-700 bg-white'"
            @click="selectCategory(cat.categoryId)"
          >
            <span class="me-1">{{ getCategoryIcon(cat.categoryName) }}</span>
            {{ cat.categoryName }}
            <span
              class="badge rounded-pill ms-1"
              :class="selectedCategoryId === cat.categoryId ? 'bg-white text-indigo' : (cat.pendingCount > 0 ? 'bg-danger text-white' : 'bg-slate-200 text-slate-700')"
            >
              {{ cat.totalItems }}
            </span>
          </button>
        </div>
      </div>
    </div>

    <!-- Alert Banners -->
    <div class="container-fluid px-3 pt-2">
      <div v-if="successMsg" class="alert alert-success alert-dismissible fade show py-2 mb-2 bg-emerald-50 text-emerald-800 border-emerald shadow-sm rounded-3" role="alert">
        <span class="fw-semibold">✅ {{ successMsg }}</span>
        <button type="button" class="btn-close py-2" @click="successMsg = ''"></button>
      </div>
      <div v-if="errorMsg" class="alert alert-danger alert-dismissible fade show py-2 mb-2 bg-rose-50 text-rose-800 border-rose shadow-sm rounded-3" role="alert">
        <span class="fw-semibold">⚠️ {{ errorMsg }}</span>
        <button type="button" class="btn-close py-2" @click="errorMsg = ''"></button>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- 3. MAIN CONTENT: HIỂN THỊ THEO CHẾ ĐỘ (VIEW MODES) -->
    <!-- ============================================================== -->
    <main class="flex-grow-1 p-3 overflow-auto">
      <!-- Loading State -->
      <div v-if="loading && orders.length === 0 && queueItems.length === 0" class="text-center py-5">
        <div class="spinner-border text-indigo" role="status">
          <span class="visually-hidden">Đang nạp dữ liệu...</span>
        </div>
        <p class="text-muted mt-2">Đang đồng bộ dữ liệu bếp...</p>
      </div>

      <!-- ============================================================== -->
      <!-- VIEW MODE 1: THẺ BÀN ORDER (KANBAN CARDS) ⭐ SÁNG & ĐẸP -->
      <!-- ============================================================== -->
      <div v-else-if="viewMode === 'cards'">
        <!-- Filter Bar inside Cards View -->
        <div class="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
          <div class="d-flex align-items-center gap-2 flex-wrap">
            <span class="text-muted small fw-semibold">Lọc bàn:</span>
            <button
              class="btn btn-xs rounded-pill px-3 shadow-xs"
              :class="cardFilter === 'all' ? 'btn-slate-800 text-white fw-bold' : 'btn-white text-slate-700 border'"
              @click="cardFilter = 'all'"
            >
              Tất cả bàn ({{ visibleOrders.length }})
            </button>
            <button
              class="btn btn-xs rounded-pill px-3 shadow-xs"
              :class="cardFilter === 'urgent' ? 'btn-rose text-white fw-bold' : 'btn-white text-slate-700 border'"
              @click="cardFilter = 'urgent'"
            >
              🚨 Chờ > 15 phút ({{ visibleOrders.filter(o => o.elapsedMinutes >= 15).length }})
            </button>
            <button
              class="btn btn-xs rounded-pill px-3 shadow-xs"
              :class="cardFilter === 'pending' ? 'btn-amber text-slate-900 fw-bold' : 'btn-white text-slate-700 border'"
              @click="cardFilter = 'pending'"
            >
              ⏳ Còn món chờ làm ({{ visibleOrders.filter(o => o.pendingCookingCount > 0).length }})
            </button>
          </div>

          <div class="d-flex align-items-center gap-2">
            <input
              type="text"
              class="form-control form-control-sm bg-white text-slate-800 border rounded-pill shadow-xs"
              placeholder="🔍 Tìm bàn hoặc mã đơn..."
              v-model="cardSearch"
              style="width: 220px;"
            />
            <div class="text-slate-600 small text-nowrap d-none d-md-inline">
              📍 Quầy: <strong class="text-indigo">{{ selectedCategoryName }}</strong>
            </div>
          </div>
        </div>

        <!-- Empty State -->
        <div v-if="visibleOrders.length === 0" class="card bg-white border-0 shadow-sm text-center py-5 my-4 rounded-4">
          <div class="card-body">
            <div class="display-3 mb-2">🎉</div>
            <h4 class="text-slate-800 fw-bold">Hiện không có món nào thuộc quầy này cần làm!</h4>
            <p class="text-muted">Tất cả món đã hoàn thành hoặc chưa có order mới từ khách.</p>
            <button class="btn btn-outline-indigo btn-sm rounded-pill px-3" @click="fetchKitchenData">
              🔄 Kiểm tra lại
            </button>
          </div>
        </div>

        <!-- Kanban Cards Grid -->
        <div v-else class="row g-3">
          <div
            v-for="order in visibleOrders"
            :key="order.orderId"
            class="col-12 col-md-6 col-lg-4 col-xl-3"
          >
            <div class="card kds-ticket-card h-100 border-0 shadow-sm rounded-4" :class="getCardUrgencyClass(order.elapsedMinutes)">
              <!-- Card Header: TÊN BÀN IN CỠ ĐẠI SIÊU RÕ -->
              <div class="card-header border-0 rounded-top-4 py-2 px-3 d-flex justify-content-between align-items-center" :class="getHeaderUrgencyClass(order.elapsedMinutes)">
                <div>
                  <div class="d-flex align-items-center gap-2">
                    <h5 class="mb-0 fw-bold text-slate-900 fs-5">{{ order.tableDisplay }}</h5>
                    <span v-if="order.linkedTables && order.linkedTables.length > 1" class="badge bg-amber text-slate-900 fw-bold" style="font-size: 0.65rem;">
                      🔗 Ghép {{ order.linkedTables.length }} bàn
                    </span>
                  </div>
                  <small class="text-muted">
                    📍 {{ order.areaName }} • Order #{{ order.orderId }}
                  </small>
                </div>

                <!-- Wait Time Pill -->
                <div class="text-end">
                  <span class="badge rounded-pill fw-bold fs-6 shadow-xs" :class="getWaitBadgeClass(order.elapsedMinutes)">
                    ⏱️ {{ order.elapsedMinutes }}p
                  </span>
                </div>
              </div>

              <!-- Order Note (Ghi chú chung của bàn) -->
              <div v-if="order.orderNote" class="bg-amber-50 text-amber-900 px-3 py-1 small fw-semibold border-bottom border-amber-200">
                📌 Lưu ý: {{ order.orderNote }}
              </div>

              <!-- Card Body: Danh Sách Món -->
              <div class="card-body p-2 overflow-auto" style="max-height: 440px;">
                <div class="d-flex flex-column gap-2">
                  <div
                    v-for="item in order.items"
                    :key="item.orderItemId"
                    class="dish-item-row p-2 rounded-3 border transition"
                    :class="getItemRowClass(item.itemStatus)"
                  >
                    <!-- Item Top Row: Tên Món & SL -->
                    <div class="d-flex justify-content-between align-items-start gap-1">
                      <div class="flex-grow-1">
                        <div class="d-flex align-items-center gap-2 flex-wrap">
                          <span class="badge bg-indigo text-white fs-6 fw-bold px-2 py-1 rounded">x{{ item.quantity }}</span>
                          <strong class="text-slate-900 fs-6">{{ item.foodNameSnapshot }}</strong>
                          <span class="badge px-2 py-1" :class="getCategoryBadgeClass(item.categoryName)" style="font-size: 0.65rem;">
                            {{ getCategoryIcon(item.categoryName) }} {{ item.categoryName }}
                          </span>
                        </div>
                        <div class="text-muted small ps-4 mt-0">
                          🔹 {{ item.variantSnapshot }}
                        </div>
                      </div>

                      <div class="text-end flex-shrink-0">
                        <span class="badge" :class="getStatusBadgeClass(item.itemStatus)">
                          {{ getStatusLabel(item.itemStatus) }}
                        </span>
                      </div>
                    </div>

                    <!-- GHI CHÚ ĐẶC BIỆT CỦA KHÁCH (NỔI BẬT KHUNG ĐỎ CẢNH BÁO) -->
                    <div v-if="item.note" class="alert-note-box py-1 px-2 my-1 rounded text-rose fw-bold small d-flex align-items-center gap-1">
                      <span>⚠️ Ghi chú:</span>
                      <span class="text-dark">{{ item.note }}</span>
                    </div>

                    <!-- Item Bottom Action Row: 1-TOUCH BẾP -->
                    <div class="d-flex justify-content-between align-items-center mt-2 pt-1 border-top border-slate-100">
                      <small class="text-muted font-monospace">
                        <span v-if="item.waitMinutes > 0">⏱️ {{ item.waitMinutes }}p</span>
                        <span v-if="item.chefName" class="ms-1 text-indigo">👨‍🍳 {{ item.chefName }}</span>
                      </small>

                      <div class="d-flex align-items-center gap-1">
                        <!-- Tùy chọn: Đang nấu -->
                        <button
                          v-if="item.itemStatus === 'confirmed'"
                          class="btn btn-outline-warning btn-sm py-1 px-2 text-dark rounded-pill fw-semibold"
                          @click="updateItemStatus(item.orderItemId, 'cooking')"
                          :disabled="updatingItemId === item.orderItemId"
                          title="Đánh dấu đang nấu"
                        >
                          🍳 Đang nấu
                        </button>

                        <!-- NÚT 1-TOUCH CHÍNH: XONG MÓN (RA QUẦY) -->
                        <button
                          v-if="item.itemStatus === 'confirmed' || item.itemStatus === 'cooking'"
                          class="btn btn-emerald btn-sm fw-bold px-3 py-1 rounded-pill shadow-xs text-white"
                          @click="updateItemStatus(item.orderItemId, 'ready')"
                          :disabled="updatingItemId === item.orderItemId"
                          title="1 Chạm: Báo đã xong món, chuyển ra quầy ra món"
                        >
                          ✅ Xong món
                        </button>

                        <span v-else-if="item.itemStatus === 'ready'" class="badge bg-emerald-subtle text-emerald border border-emerald-subtle px-2 py-1 rounded-pill">
                          🛎️ Đã ra quầy
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- CÁC MÓN THUỘC QUẦY KHÁC TRONG CÙNG ORDER (GIỮ ĐẦY ĐỦ KHÚC THEO ORDER) -->
                  <div v-if="order.otherStationItems && order.otherStationItems.length > 0" class="mt-2 p-2 bg-slate-50 border border-slate-200 rounded-3">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                      <small class="text-muted fw-bold d-flex align-items-center gap-1" style="font-size: 0.72rem;">
                        <span>📋</span> Món quầy khác cùng Order #{{ order.orderId }}:
                      </small>
                      <span class="badge bg-slate-200 text-slate-700" style="font-size: 0.65rem;">
                        {{ order.otherStationItems.length }} món
                      </span>
                    </div>
                    <div class="d-flex flex-column gap-1">
                      <div
                        v-for="other in order.otherStationItems"
                        :key="other.orderItemId"
                        class="d-flex justify-content-between align-items-center small text-slate-600 px-2 py-1 bg-white rounded border border-slate-100"
                        style="font-size: 0.73rem;"
                      >
                        <div class="text-truncate me-1">
                          <span class="fw-semibold text-slate-800">{{ other.foodNameSnapshot }}</span>
                          <span class="text-muted ms-1">x{{ other.quantity }}</span>
                        </div>
                        <div class="d-flex align-items-center gap-1 flex-shrink-0">
                          <span class="badge" :class="getCategoryBadgeClass(other.categoryName)" style="font-size: 0.6rem;">
                            {{ getCategoryIcon(other.categoryName) }} {{ other.categoryName }}
                          </span>
                          <span class="badge" :class="getStatusBadgeClass(other.itemStatus)" style="font-size: 0.6rem;">
                            {{ getStatusLabel(other.itemStatus) }}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Card Footer: Tiến độ & 1-Chạm cả bàn -->
              <div class="card-footer bg-slate-50 border-0 rounded-bottom-4 p-2">
                <div class="d-flex justify-content-between align-items-center mb-1 small text-muted">
                  <span>Tiến độ bàn: {{ order.readyCount }}/{{ order.totalKitchenItems }} món hoàn tất</span>
                  <span class="fw-bold text-slate-800">{{ Math.round((order.readyCount / (order.totalKitchenItems || 1)) * 100) }}%</span>
                </div>
                <!-- Progress bar -->
                <div class="progress mb-2 rounded-pill bg-slate-200" style="height: 6px;">
                  <div
                    class="progress-bar bg-emerald rounded-pill"
                    role="progressbar"
                    :style="{ width: `${(order.readyCount / (order.totalKitchenItems || 1)) * 100}%` }"
                  ></div>
                </div>

                <!-- 1-Click Complete All Actions for Table -->
                <button
                  class="btn btn-emerald btn-sm fw-bold w-100 rounded-pill shadow-xs text-white"
                  @click="completeAllOrderItems(order.orderId)"
                  :disabled="order.pendingCookingCount === 0 && order.cookingCount === 0"
                  title="1 Chạm: Hoàn tất tất cả món của bàn này thuộc quầy hiện tại"
                >
                  ⚡ Nấu xong cả bàn ({{ order.pendingCookingCount + order.cookingCount }})
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ============================================================== -->
      <!-- VIEW MODE 2: HÀNG ĐỢI PHẲNG FIFO (ITEM QUEUE) -->
      <!-- ============================================================== -->
      <div v-else-if="viewMode === 'queue'">
        <!-- Queue Filter & Search -->
        <div class="row g-3 mb-3">
          <div class="col-12 col-md-8">
            <div class="d-flex align-items-center gap-2 flex-wrap">
              <span class="text-muted small fw-semibold">Trạng thái:</span>
              <button
                class="btn btn-sm rounded-pill shadow-xs"
                :class="queueStatusFilter === 'all' ? 'btn-slate-800 text-white fw-bold' : 'btn-white text-slate-700 border'"
                @click="queueStatusFilter = 'all'"
              >
                Tất cả ({{ filteredQueueByCat.length }})
              </button>
              <button
                class="btn btn-sm rounded-pill shadow-xs"
                :class="queueStatusFilter === 'confirmed' ? 'btn-amber text-slate-900 fw-bold' : 'btn-white text-slate-700 border'"
                @click="queueStatusFilter = 'confirmed'"
              >
                ⏳ Chờ làm ({{ filteredQueueByCat.filter(i => i.itemStatus === 'confirmed').length }})
              </button>
              <button
                class="btn btn-sm rounded-pill shadow-xs"
                :class="queueStatusFilter === 'cooking' ? 'btn-indigo text-white fw-bold' : 'btn-white text-slate-700 border'"
                @click="queueStatusFilter = 'cooking'"
              >
                🍳 Đang nấu ({{ filteredQueueByCat.filter(i => i.itemStatus === 'cooking').length }})
              </button>
              <button
                class="btn btn-sm rounded-pill shadow-xs"
                :class="queueStatusFilter === 'ready' ? 'btn-emerald text-white fw-bold' : 'btn-white text-slate-700 border'"
                @click="queueStatusFilter = 'ready'"
              >
                🛎️ Đã ra quầy ({{ filteredQueueByCat.filter(i => i.itemStatus === 'ready').length }})
              </button>
            </div>
          </div>

          <div class="col-12 col-md-4 text-md-end">
            <div class="input-group input-group-sm">
              <span class="input-group-text bg-white text-muted border-end-0 rounded-start-pill">🔍</span>
              <input
                type="text"
                class="form-control bg-white text-slate-800 border-start-0 rounded-end-pill"
                placeholder="Tìm món, số bàn..."
                v-model="queueSearch"
              />
            </div>
          </div>
        </div>

        <!-- Aggregate Summary of Items (Line Cook Batching) -->
        <div class="card bg-white border-0 shadow-sm mb-3 rounded-4" v-if="aggregatedQueue.length > 0">
          <div class="card-header bg-slate-50 border-0 rounded-top-4 py-2 px-3 text-slate-800 fw-bold d-flex justify-content-between align-items-center">
            <span>🔥 TỔNG HỢP MÓN CẦN NẤU THEO MẺ - {{ selectedCategoryName.toUpperCase() }}</span>
            <small class="text-muted fw-normal">Gộp món cùng loại để đầu bếp nấu nhanh</small>
          </div>
          <div class="card-body p-2">
            <div class="d-flex flex-wrap gap-2">
              <div
                v-for="summary in aggregatedQueue"
                :key="summary.foodName + summary.variant"
                class="bg-slate-50 border rounded-3 p-2 d-flex align-items-center gap-2 shadow-xs"
              >
                <span class="badge bg-rose text-white fs-6 fw-bold px-2 py-1 rounded-pill">Tổng: {{ summary.totalQuantity }}</span>
                <div>
                  <div class="fw-bold text-slate-900 small">{{ summary.foodName }}</div>
                  <div class="text-muted" style="font-size: 0.75rem;">
                    🔹 {{ summary.variant }} • <span class="badge bg-slate-200 text-slate-700">{{ summary.categoryName }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Flat Queue Table -->
        <div class="card bg-white border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="bg-slate-100 text-slate-600 text-uppercase" style="font-size: 0.75rem;">
                <tr>
                  <th style="width: 50px;">STT</th>
                  <th>Món Ăn & Biến Thể</th>
                  <th>Quầy Chế Biến</th>
                  <th class="text-center" style="width: 70px;">SL</th>
                  <th>Bàn Phục Vụ (Order)</th>
                  <th>Ghi Chú</th>
                  <th class="text-center" style="width: 120px;">Thời Gian Chờ</th>
                  <th class="text-center" style="width: 130px;">Trạng Thái</th>
                  <th class="text-end" style="width: 180px;">Thao Tác 1-Chạm</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="filteredQueue.length === 0">
                  <td colspan="9" class="text-center py-4 text-muted">
                    Không có món nào trong hàng đợi theo quầy này.
                  </td>
                </tr>
                <tr v-for="(item, index) in filteredQueue" :key="item.orderItemId">
                  <td class="text-muted fw-bold">{{ index + 1 }}</td>
                  <td>
                    <div class="fw-bold text-slate-900 fs-6">{{ item.foodNameSnapshot }}</div>
                    <small class="text-muted">🔹 {{ item.variantSnapshot }}</small>
                  </td>
                  <td>
                    <span class="badge bg-slate-100 text-slate-700 border px-2">
                      {{ item.categoryName }}
                    </span>
                  </td>
                  <td class="text-center">
                    <span class="badge bg-indigo text-white fs-6 fw-bold px-2 py-1 rounded">{{ item.quantity }}</span>
                  </td>
                  <td>
                    <!-- BÀN PHỤC VỤ CỰC TO -->
                    <span class="badge bg-amber text-slate-900 fs-6 fw-bold px-2 py-1 shadow-xs">
                      🏷️ {{ item.tableDisplay }}
                    </span>
                    <small class="text-muted d-block mt-1">Đơn #{{ item.orderId }}</small>
                  </td>
                  <td>
                    <span v-if="item.note" class="badge bg-rose-subtle text-rose border border-rose-subtle text-wrap text-start">
                      ⚠️ {{ item.note }}
                    </span>
                    <span v-else class="text-muted">—</span>
                  </td>
                  <td class="text-center">
                    <span class="badge rounded-pill fw-bold" :class="getWaitBadgeClass(item.waitMinutes)">
                      ⏱️ {{ item.waitMinutes }} phút
                    </span>
                  </td>
                  <td class="text-center">
                    <span class="badge" :class="getStatusBadgeClass(item.itemStatus)">
                      {{ getStatusLabel(item.itemStatus) }}
                    </span>
                  </td>
                  <td class="text-end">
                    <div class="d-flex justify-content-end align-items-center gap-1">
                      <button
                        v-if="item.itemStatus === 'confirmed' || item.itemStatus === 'cooking'"
                        class="btn btn-emerald btn-sm fw-bold px-3 rounded-pill text-white shadow-xs"
                        @click="updateItemStatus(item.orderItemId, 'ready')"
                        :disabled="updatingItemId === item.orderItemId"
                        title="1 Chạm: Nấu xong ra quầy"
                      >
                        ✅ Xong
                      </button>

                      <button
                        v-if="item.itemStatus === 'confirmed'"
                        class="btn btn-outline-warning btn-sm rounded-circle p-1 text-dark"
                        @click="updateItemStatus(item.orderItemId, 'cooking')"
                        :disabled="updatingItemId === item.orderItemId"
                        title="Đánh dấu đang nấu"
                      >
                        🍳
                      </button>

                      <span v-else-if="item.itemStatus === 'ready'" class="badge bg-emerald-subtle text-emerald border border-emerald-subtle rounded-pill">
                        🛎️ Tại quầy
                      </span>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- ============================================================== -->
      <!-- VIEW MODE 3: MÀN HÌNH TREO QUẦY RA MÓN (EXPEDITER PICKUP SCREEN) ⭐ -->
      <!-- Thiết kế đặc biệt: Phục vụ nhìn là bưng đi, KHÔNG CẦN ĐIỆN THOẠI -->
      <!-- ============================================================== -->
      <div v-else-if="viewMode === 'pass'">
        <!-- Banner Hướng Dẫn Vận Hành Trực Quan -->
        <div class="bg-emerald-50 border border-emerald-200 rounded-4 p-3 mb-3 d-flex justify-content-between align-items-center flex-wrap gap-2 shadow-xs">
          <div class="d-flex align-items-center gap-3">
            <span class="fs-2">🛎️</span>
            <div>
              <h5 class="mb-0 text-emerald-900 fw-bold">MÀN HÌNH TRỰC QUAN QUẦY RA MÓN (PICKUP SCREEN)</h5>
              <p class="mb-0 text-emerald-700 small">
                Nhân viên phục vụ chỉ cần nhìn màn hình này: Kiểm tra đúng <strong>Bàn Ăn</strong>, nhấc đĩa bưng thẳng ra bàn cho khách rồi bấm <strong>"Bưng cả bàn"</strong>!
              </p>
            </div>
          </div>

          <div class="d-flex align-items-center gap-2">
            <span class="badge bg-emerald text-white fs-6 px-3 py-2 rounded-pill shadow-xs">
              {{ passReadyItems.length }} đĩa đã sẵn sàng
            </span>
          </div>
        </div>

        <!-- Empty State nếu không có món nào đang chờ bưng -->
        <div v-if="passReadyTables.length === 0" class="card bg-white border-0 shadow-sm text-center py-5 my-4 rounded-4">
          <div class="card-body">
            <div class="display-3 mb-2">✨</div>
            <h4 class="text-slate-800 fw-bold">Quầy ra món hiện đang trống!</h4>
            <p class="text-muted">Tất cả các món đã được phục vụ bưng ra bàn cho khách hoàn tất.</p>
          </div>
        </div>

        <!-- GRID CÁC BÀN ĐANG CÓ MÓN SẴN SÀNG TẠI QUẦY (GOM THEO BÀN) -->
        <div v-else class="row g-3">
          <div
            v-for="tableGroup in passReadyTables"
            :key="tableGroup.orderId"
            class="col-12 col-md-6 col-lg-4"
          >
            <div class="card pickup-table-card border-0 shadow rounded-4 overflow-hidden h-100">
              <!-- Header Bàn Ăn CỰC ĐẠI -->
              <div class="card-header bg-emerald text-white p-3 d-flex justify-content-between align-items-center">
                <div>
                  <span class="text-white-50 text-uppercase fw-semibold" style="font-size: 0.75rem; letter-spacing: 0.5px;">BÀN CẦN PHỤC VỤ:</span>
                  <h3 class="mb-0 fw-bold text-white">{{ tableGroup.tableDisplay }}</h3>
                  <small class="text-emerald-100">Đơn #{{ tableGroup.orderId }}</small>
                </div>

                <div class="text-end">
                  <span class="badge bg-white text-emerald fw-bold fs-5 px-3 py-2 rounded-pill shadow-sm">
                    {{ tableGroup.items.length }} đĩa
                  </span>
                </div>
              </div>

              <!-- Danh sách các đĩa đã xong của bàn này -->
              <div class="card-body p-3 bg-white overflow-auto" style="max-height: 380px;">
                <div class="d-flex flex-column gap-2">
                  <div
                    v-for="item in tableGroup.items"
                    :key="item.orderItemId"
                    class="p-2 rounded-3 border bg-slate-50 d-flex justify-content-between align-items-center gap-2"
                  >
                    <div class="flex-grow-1">
                      <div class="d-flex align-items-center gap-2">
                        <span class="badge bg-indigo text-white fs-6 fw-bold px-2 py-1 rounded">x{{ item.quantity }}</span>
                        <h6 class="mb-0 fw-bold text-slate-900 fs-6">{{ item.foodNameSnapshot }}</h6>
                      </div>
                      <small class="text-muted ps-4">🔹 {{ item.variantSnapshot }}</small>

                      <!-- Ghi chú đặc biệt hiển thị KHUNG ĐỎ CẢNH BÁO -->
                      <div v-if="item.note" class="alert-note-box py-1 px-2 mt-1 rounded text-rose fw-bold small">
                        ⚠️ Ghi chú: {{ item.note }}
                      </div>
                    </div>

                    <!-- Bấm bưng riêng 1 đĩa nếu không bưng cả bàn -->
                    <button
                      class="btn btn-outline-secondary btn-sm rounded-pill px-2 text-nowrap"
                      @click="updateItemStatus(item.orderItemId, 'served')"
                      :disabled="updatingItemId === item.orderItemId"
                      title="Bưng riêng đĩa này"
                    >
                      Bưng lẻ
                    </button>
                  </div>
                </div>
              </div>

              <!-- Footer: NÚT 1-CHẠM "BƯNG CẢ BÀN" -->
              <div class="card-footer bg-slate-50 border-0 p-3">
                <button
                  class="btn btn-emerald btn-lg fw-bold w-100 rounded-pill shadow text-white py-2"
                  @click="serveAllTableReadyItems(tableGroup)"
                  title="Nhân viên đã bưng toàn bộ đĩa của bàn này ra phục vụ khách"
                >
                  🚀 BƯNG CẢ BÀN ({{ tableGroup.items.length }} MÓN)
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- DANH SÁCH MÓN VỪA BƯNG GẦN ĐÂY (HỖ TRỢ HOÀN TÁC TRỰC QUAN) -->
        <div class="card bg-white border-0 shadow-sm mt-4 rounded-4">
          <div class="card-header bg-slate-50 border-0 rounded-top-4 py-2 px-3 d-flex justify-content-between align-items-center">
            <span class="fw-bold text-muted small text-uppercase">
              🕒 ĐÃ BƯNG GẦN ĐÂY (HOÀN TÁC NẾU BẤM NHẦM)
            </span>
            <button class="btn btn-xs btn-outline-secondary rounded-pill" @click="fetchRecentCompleted">
              🔄 Tải lại
            </button>
          </div>
          <div class="card-body p-2">
            <div v-if="recentCompletedItems.length === 0" class="text-muted text-center py-2 small">
              Chưa có món nào hoàn tất gần đây.
            </div>
            <div class="d-flex flex-wrap gap-2" v-else>
              <div
                v-for="item in recentCompletedItems.slice(0, 10)"
                :key="item.orderItemId"
                class="bg-slate-50 border rounded-3 p-2 d-flex align-items-center gap-2 shadow-xs"
              >
                <div>
                  <span class="badge bg-slate-800 text-white fw-bold me-1">{{ item.tableDisplay }}</span>
                  <span class="text-slate-900 fw-semibold small">{{ item.foodNameSnapshot }} (x{{ item.quantity }})</span>
                  <span class="badge ms-1" :class="item.itemStatus === 'served' ? 'bg-slate-200 text-slate-700' : 'bg-emerald text-white'">
                    {{ item.itemStatus === 'served' ? 'Đã ra bàn' : 'Tại quầy' }}
                  </span>
                </div>
                <button
                  class="btn btn-outline-danger btn-xs py-0 px-2 rounded-pill"
                  @click="undoItem(item.orderItemId)"
                  title="Khôi phục lại món này về quầy nếu bấm nhầm"
                >
                  ↩️ Hoàn tác
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ============================================================== -->
      <!-- VIEW MODE 4: BÁO HẾT MÓN (SOLD OUT MANAGER) -->
      <!-- ============================================================== -->
      <div v-else-if="viewMode === 'soldout'">
        <div class="card bg-white border-0 shadow-sm rounded-4 overflow-hidden">
          <div class="card-header bg-slate-50 border-0 py-3 px-3 d-flex justify-content-between align-items-center flex-wrap gap-2">
            <div>
              <h5 class="text-slate-900 fw-bold mb-0">🚫 BÁO HẾT MÓN BẾP (OUT OF STOCK)</h5>
              <small class="text-muted">Khi bật 'Hết món', nhân viên phục vụ và khách quét QR sẽ không thể đặt món này.</small>
            </div>

            <div class="d-flex align-items-center gap-2">
              <input
                type="text"
                class="form-control form-control-sm bg-white text-slate-800 border rounded-pill"
                placeholder="Tìm món theo tên hoặc quầy..."
                v-model="foodSearchQuery"
                style="width: 260px;"
              />
              <button class="btn btn-outline-secondary btn-sm rounded-pill" @click="fetchFoods">🔄 Tải lại món</button>
            </div>
          </div>

          <div class="card-body p-0">
            <div class="table-responsive">
              <table class="table table-hover align-middle mb-0">
                <thead class="bg-slate-100 text-slate-600 text-uppercase" style="font-size: 0.75rem;">
                  <tr>
                    <th style="width: 60px;">ID</th>
                    <th>Tên Món Ăn</th>
                    <th>Quầy Chế Biến</th>
                    <th>Giá Tiêu Chuẩn</th>
                    <th class="text-center" style="width: 140px;">Trạng Thái Phục Vụ</th>
                    <th class="text-end" style="width: 160px;">Thao Tác Bếp</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-if="filteredFoods.length === 0">
                    <td colspan="6" class="text-center py-4 text-muted">
                      Không tìm thấy món ăn nào phù hợp với tìm kiếm.
                    </td>
                  </tr>
                  <tr v-for="food in filteredFoods" :key="food.foodId">
                    <td class="text-muted">#{{ food.foodId }}</td>
                    <td>
                      <div class="fw-bold text-slate-900 fs-6">{{ food.foodName }}</div>
                      <small class="text-muted">{{ food.description || 'Chưa có mô tả' }}</small>
                    </td>
                    <td>
                      <span class="badge bg-slate-100 text-slate-700 border">{{ food.categoryName }}</span>
                    </td>
                    <td class="font-monospace text-slate-900 fw-semibold">
                      {{ formatCurrency(food.minPrice) }}
                    </td>
                    <td class="text-center">
                      <span v-if="food.isActive" class="badge bg-emerald-subtle text-emerald border border-emerald-subtle px-2 py-1 rounded-pill">
                        🟢 Đang phục vụ
                      </span>
                      <span v-else class="badge bg-rose-subtle text-rose border border-rose-subtle px-2 py-1 rounded-pill">
                        🔴 Đã báo hết món
                      </span>
                    </td>
                    <td class="text-end">
                      <button
                        v-if="food.isActive"
                        class="btn btn-outline-danger btn-sm fw-bold px-3 rounded-pill"
                        @click="toggleFoodSoldOut(food.foodId, false)"
                        :disabled="togglingFoodId === food.foodId"
                      >
                        ⚠️ Báo Hết
                      </button>
                      <button
                        v-else
                        class="btn btn-emerald btn-sm fw-bold px-3 rounded-pill text-white"
                        @click="toggleFoodSoldOut(food.foodId, true)"
                        :disabled="togglingFoodId === food.foodId"
                      >
                        ✅ Mở Lại Món
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import signalRService from '@/services/signalr'

const router = useRouter()

// API Base URL
const API_BASE = 'http://localhost:5079/api/chef'

// Helper config axios with JWT token
const getAuthHeaders = () => {
  const token = localStorage.getItem('token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

// Current user details
const userRole = ref((localStorage.getItem('role') || 'chef').toLowerCase())
const userName = ref(localStorage.getItem('fullName') || 'Bếp Trưởng')

// View State: default to 'cards' for intuitive kitchen tickets
const viewMode = ref('cards') // 'cards' | 'queue' | 'pass' | 'soldout'
const cardFilter = ref('all') // 'all' | 'urgent' | 'pending'
const cardSearch = ref('')
const queueStatusFilter = ref('all') // 'all' | 'confirmed' | 'cooking' | 'ready'
const queueSearch = ref('')
const foodSearchQuery = ref('')

// Phân khu bếp theo Danh mục (Kitchen Station Filter với Persistence)
const savedStation = localStorage.getItem('chef_selected_station')
const selectedCategoryId = ref(savedStation ? parseInt(savedStation) : 0) // 0: Tất cả các quầy
const categories = ref([])
const autoCookingEnabled = ref(localStorage.getItem('chef_auto_cooking') === 'true')

// Data State
const loading = ref(false)
const actionInProgress = ref(false)
const orders = ref([])
const queueItems = ref([])
const recentCompletedItems = ref([])
const foods = ref([])
const metrics = ref({
  confirmedCount: 0,
  cookingCount: 0,
  readyCount: 0,
  soldOutFoodsCount: 0,
  totalActiveCount: 0
})

// Feedback & Alerts
const successMsg = ref('')
const errorMsg = ref('')
const updatingItemId = ref(null)
const togglingFoodId = ref(null)
const soundEnabled = ref(true)

// Real-time Clock & Auto-refresh timer
const currentTime = ref('')
const refreshCountdown = ref(10)
let clockInterval = null
let refreshInterval = null

// Web Audio API Chime Alert
const playAlertChime = (tone = 'high') => {
  if (!soundEnabled.value) return
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext
    if (!AudioContext) return
    const ctx = new AudioContext()
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()

    osc.type = 'sine'
    if (tone === 'ready') {
      osc.frequency.setValueAtTime(523.25, ctx.currentTime) // C5
      osc.frequency.setValueAtTime(659.25, ctx.currentTime + 0.1) // E5
      osc.frequency.setValueAtTime(783.99, ctx.currentTime + 0.2) // G5
    } else {
      osc.frequency.setValueAtTime(587.33, ctx.currentTime)
      osc.frequency.setValueAtTime(880, ctx.currentTime + 0.1)
    }

    gain.gain.setValueAtTime(0.15, ctx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.4)

    osc.connect(gain)
    gain.connect(ctx.destination)

    osc.start()
    osc.stop(ctx.currentTime + 0.4)
  } catch (e) {
    console.warn('Audio alert error:', e)
  }
}

const toggleSound = () => {
  soundEnabled.value = !soundEnabled.value
  if (soundEnabled.value) {
    playAlertChime('ready')
  }
}

// Format Currency
const formatCurrency = (val) => {
  if (!val && val !== 0) return '0 đ'
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val)
}

// Select Kitchen Station Category (Lưu vào localStorage)
const selectCategory = (catId) => {
  selectedCategoryId.value = catId
  localStorage.setItem('chef_selected_station', catId.toString())
  fetchKitchenData()
}

const onAutoCookingToggle = () => {
  localStorage.setItem('chef_auto_cooking', autoCookingEnabled.value.toString())
  if (autoCookingEnabled.value) {
    checkAutoCooking()
  }
}

// Icon nhận diện từng quầy bếp
const getCategoryIcon = (categoryName) => {
  if (!categoryName) return '🍽️'
  const name = categoryName.toLowerCase()
  if (name.includes('nướng') || name.includes('chiên') || name.includes('bò') || name.includes('thịt')) return '🔥'
  if (name.includes('nấu') || name.includes('lẩu') || name.includes('hấp') || name.includes('hải sản')) return '🍲'
  if (name.includes('uống') || name.includes('pha chế') || name.includes('bar') || name.includes('nước')) return '🍹'
  return '🍴'
}

// Badge màu cho từng quầy bếp
const getCategoryBadgeClass = (categoryName) => {
  if (!categoryName) return 'bg-slate-100 text-slate-700 border'
  const name = categoryName.toLowerCase()
  if (name.includes('nướng') || name.includes('chiên')) return 'bg-amber-100 text-amber-800 border border-amber-300'
  if (name.includes('lẩu') || name.includes('nấu')) return 'bg-indigo-100 text-indigo-800 border border-indigo-300'
  if (name.includes('uống') || name.includes('pha chế')) return 'bg-emerald-100 text-emerald-800 border border-emerald-300'
  return 'bg-slate-100 text-slate-700 border'
}

// Tên quầy bếp đang chọn
const selectedCategoryName = computed(() => {
  if (selectedCategoryId.value === 0) return 'Toàn bộ quầy bếp'
  const found = categories.value.find(c => c.categoryId === selectedCategoryId.value)
  return found ? found.categoryName : 'Quầy bếp'
})

// Danh sách 3 quầy bếp active từ API
const activeCategoriesWithCount = computed(() => {
  return categories.value.map(c => ({
    ...c,
    totalItems: c.totalItems ?? c.itemCount ?? 0,
    pendingCount: c.pendingCount ?? 0,
    cookingCount: c.cookingCount ?? 0,
    readyCount: c.readyCount ?? 0
  }))
})

const totalItemsAllCategories = computed(() => {
  return queueItems.value.length
})

// Số món chờ của quầy hiện tại
const currentStationPendingCount = computed(() => {
  if (selectedCategoryId.value === 0) {
    return queueItems.value.filter(i => i.itemStatus === 'confirmed').length
  }
  return queueItems.value.filter(i => i.categoryId === selectedCategoryId.value && i.itemStatus === 'confirmed').length
})

// Filtered Queue by Category first
const filteredQueueByCat = computed(() => {
  if (selectedCategoryId.value === 0) return queueItems.value
  return queueItems.value.filter(i => i.categoryId === selectedCategoryId.value)
})

// Filtered Queue with Search and Status filter
const filteredQueue = computed(() => {
  let list = filteredQueueByCat.value

  if (queueStatusFilter.value !== 'all') {
    list = list.filter(i => i.itemStatus === queueStatusFilter.value)
  }

  if (queueSearch.value.trim()) {
    const q = queueSearch.value.toLowerCase().trim()
    list = list.filter(i => {
      const matchFood = i.foodNameSnapshot?.toLowerCase().includes(q)
      const matchVariant = i.variantSnapshot?.toLowerCase().includes(q)
      const matchTables = i.tableDisplay?.toLowerCase().includes(q) || i.tables?.some(t => t.tableName?.toLowerCase().includes(q))
      return matchFood || matchVariant || matchTables
    })
  }

  return list
})

// Aggregated Queue (Group identical items for line cook batching)
const aggregatedQueue = computed(() => {
  const map = new Map()

  filteredQueueByCat.value
    .filter(i => i.itemStatus === 'confirmed' || i.itemStatus === 'cooking')
    .forEach(item => {
      const key = `${item.foodNameSnapshot}___${item.variantSnapshot}`
      if (!map.has(key)) {
        map.set(key, {
          foodName: item.foodNameSnapshot,
          variant: item.variantSnapshot,
          categoryName: item.categoryName,
          totalQuantity: 0
        })
      }
      map.get(key).totalQuantity += item.quantity
    })

  return Array.from(map.values())
})

// Visible Orders in Cards View
const visibleOrders = computed(() => {
  let result = orders.value

  if (selectedCategoryId.value > 0) {
    result = result
      .map(o => {
        const catItems = o.items.filter(i => i.categoryId === selectedCategoryId.value)
        if (catItems.length === 0) return null
        return {
          ...o,
          items: catItems,
          totalKitchenItems: catItems.length,
          pendingCookingCount: catItems.filter(i => i.itemStatus === 'confirmed').length,
          cookingCount: catItems.filter(i => i.itemStatus === 'cooking').length,
          readyCount: catItems.filter(i => i.itemStatus === 'ready').length
        }
      })
      .filter(o => o !== null)
  }

  if (cardFilter.value === 'urgent') {
    result = result.filter(o => o.elapsedMinutes >= 15)
  }
  if (cardFilter.value === 'pending') {
    result = result.filter(o => o.pendingCookingCount > 0)
  }

  if (cardSearch.value.trim()) {
    const q = cardSearch.value.toLowerCase().trim()
    result = result.filter(o => 
      o.tableDisplay?.toLowerCase().includes(q) ||
      o.orderId.toString().includes(q) ||
      o.areaName?.toLowerCase().includes(q) ||
      o.items.some(i => i.foodNameSnapshot?.toLowerCase().includes(q)) ||
      (o.otherStationItems && o.otherStationItems.some(i => i.foodNameSnapshot?.toLowerCase().includes(q)))
    )
  }

  return result
})

const visibleOrdersCount = computed(() => visibleOrders.value.length)

// Pass Station Items (Quầy ra món)
const passReadyItems = computed(() => {
  return filteredQueueByCat.value.filter(i => i.itemStatus === 'ready')
})

// GOM CÁC MÓN SẴN SÀNG THEO TỪNG BÀN (TABLE PICKUP TICKETS)
const passReadyTables = computed(() => {
  const tableMap = new Map()

  passReadyItems.value.forEach(item => {
    if (!tableMap.has(item.orderId)) {
      tableMap.set(item.orderId, {
        orderId: item.orderId,
        tableDisplay: item.tableDisplay,
        items: []
      })
    }
    tableMap.get(item.orderId).items.push(item)
  })

  return Array.from(tableMap.values())
})

// Filtered Foods for Sold Out Manager
const filteredFoods = computed(() => {
  if (!foodSearchQuery.value.trim()) return foods.value
  const q = foodSearchQuery.value.toLowerCase().trim()
  return foods.value.filter(f =>
    f.foodName.toLowerCase().includes(q) ||
    (f.categoryName && f.categoryName.toLowerCase().includes(q))
  )
})

// Fetch Metrics
const fetchMetrics = async () => {
  try {
    const res = await axios.get(`${API_BASE}/metrics`, { headers: getAuthHeaders() })
    metrics.value = res.data
  } catch (err) {
    console.error('Error fetching chef metrics:', err)
  }
}

// Fetch Kitchen Categories
const fetchCategories = async () => {
  try {
    const res = await axios.get(`${API_BASE}/categories`, { headers: getAuthHeaders() })
    categories.value = res.data || []
  } catch (err) {
    console.error('Error fetching categories:', err)
  }
}

// Fetch Orders (Table Cards)
const fetchKitchenOrders = async () => {
  try {
    const catParam = selectedCategoryId.value > 0 ? `?categoryId=${selectedCategoryId.value}` : ''
    const res = await axios.get(`${API_BASE}/orders${catParam}`, { headers: getAuthHeaders() })
    orders.value = res.data || []
  } catch (err) {
    console.error('Error fetching kitchen orders:', err)
  }
}

// Fetch FIFO Queue
const fetchKitchenQueue = async () => {
  try {
    const catParam = selectedCategoryId.value > 0 ? `?categoryId=${selectedCategoryId.value}` : ''
    const res = await axios.get(`${API_BASE}/queue${catParam}`, { headers: getAuthHeaders() })
    queueItems.value = res.data || []
  } catch (err) {
    console.error('Error fetching kitchen queue:', err)
  }
}

// Fetch Recent Completed Items for Recall
const fetchRecentCompleted = async () => {
  try {
    const res = await axios.get(`${API_BASE}/recent-completed?limit=25`, { headers: getAuthHeaders() })
    recentCompletedItems.value = res.data || []
  } catch (err) {
    console.error('Error fetching recent completed:', err)
  }
}

// Fetch Foods for Sold-Out Manager
const fetchFoods = async () => {
  try {
    const res = await axios.get(`${API_BASE}/foods`, { headers: getAuthHeaders() })
    foods.value = res.data || []
  } catch (err) {
    console.error('Error fetching foods for chef:', err)
  }
}

// Check and handle Auto-Cooking
const checkAutoCooking = async () => {
  if (!autoCookingEnabled.value) return
  const pendingItems = queueItems.value.filter(i => i.itemStatus === 'confirmed')
  if (pendingItems.length > 0) {
    try {
      await axios.patch(`${API_BASE}/categories/${selectedCategoryId.value}/start-cooking`, {}, { headers: getAuthHeaders() })
      await fetchKitchenData()
    } catch (e) {
      console.warn('Auto cooking error:', e)
    }
  }
}

// Full Fetch All Kitchen Data
const fetchKitchenData = async () => {
  loading.value = true
  try {
    await Promise.all([
      fetchMetrics(),
      fetchCategories(),
      fetchKitchenOrders(),
      fetchKitchenQueue(),
      fetchRecentCompleted()
    ])
    if (autoCookingEnabled.value) {
      await checkAutoCooking()
    }
  } catch (err) {
    errorMsg.value = 'Không thể đồng bộ dữ liệu với máy chủ bếp.'
  } finally {
    loading.value = false
  }
}

// Cập nhật ngầm tức thì qua SignalR (không set loading = true, không giật màn hình)
const fetchKitchenDataSilently = async () => {
  try {
    await Promise.all([
      fetchMetrics(),
      fetchCategories(),
      fetchKitchenOrders(),
      fetchKitchenQueue(),
      fetchRecentCompleted()
    ])
    if (autoCookingEnabled.value) {
      await checkAutoCooking()
    }
  } catch (err) {
    console.warn('Realtime update error:', err)
  }
}

// Ready All items in current Station (1-Touch Station Clear)
const readyAllStation = async () => {
  actionInProgress.value = true
  errorMsg.value = ''
  try {
    const res = await axios.patch(
      `${API_BASE}/categories/${selectedCategoryId.value}/ready-all`,
      {},
      { headers: getAuthHeaders() }
    )
    successMsg.value = res.data.message || 'Đã chuyển tất cả món ra quầy ra món!'
    playAlertChime('ready')
    await fetchKitchenData()

    setTimeout(() => {
      if (successMsg.value) successMsg.value = ''
    }, 4000)
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi hoàn tất quầy.'
  } finally {
    actionInProgress.value = false
  }
}

// 1-Touch Update Item Status (confirmed -> ready -> served)
const updateItemStatus = async (orderItemId, newStatus) => {
  updatingItemId.value = orderItemId
  errorMsg.value = ''
  try {
    const res = await axios.patch(
      `${API_BASE}/items/${orderItemId}/status`,
      { status: newStatus },
      { headers: getAuthHeaders() }
    )

    successMsg.value = res.data.message || 'Đã cập nhật trạng thái món.'
    if (newStatus === 'ready' || newStatus === 'served') {
      playAlertChime('ready')
    } else {
      playAlertChime()
    }

    await fetchKitchenData()

    setTimeout(() => {
      if (successMsg.value) successMsg.value = ''
    }, 4000)
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi cập nhật trạng thái món.'
  } finally {
    updatingItemId.value = null
  }
}

// BƯNG TOÀN BỘ CÁC ĐĨA SẴN SÀNG CỦA MỘT BÀN (1-TOUCH TABLE SERVE)
const serveAllTableReadyItems = async (tableGroup) => {
  errorMsg.value = ''
  try {
    const promises = tableGroup.items.map(it =>
      axios.patch(
        `${API_BASE}/items/${it.orderItemId}/status`,
        { status: 'served' },
        { headers: getAuthHeaders() }
      )
    )
    await Promise.all(promises)
    successMsg.value = `Đã xác nhận bưng toàn bộ ${tableGroup.items.length} món cho ${tableGroup.tableDisplay}!`
    playAlertChime('ready')
    await fetchKitchenData()

    setTimeout(() => {
      if (successMsg.value) successMsg.value = ''
    }, 4000)
  } catch (err) {
    errorMsg.value = 'Lỗi khi xác nhận bưng cả bàn.'
  }
}

// Quick Undo (Hoàn tác)
const undoItem = async (orderItemId) => {
  errorMsg.value = ''
  try {
    const res = await axios.patch(
      `${API_BASE}/items/${orderItemId}/undo`,
      {},
      { headers: getAuthHeaders() }
    )
    successMsg.value = res.data.message || 'Đã hoàn tác món thành công!'
    playAlertChime()
    await fetchKitchenData()

    setTimeout(() => {
      if (successMsg.value) successMsg.value = ''
    }, 4000)
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi hoàn tác món.'
  }
}

// Complete All Items in an Order (1-Touch Table Clear)
const completeAllOrderItems = async (orderId) => {
  errorMsg.value = ''
  try {
    const catParam = selectedCategoryId.value > 0 ? `?categoryId=${selectedCategoryId.value}` : ''
    const res = await axios.patch(
      `${API_BASE}/orders/${orderId}/ready-all${catParam}`,
      {},
      { headers: getAuthHeaders() }
    )
    successMsg.value = res.data.message || 'Đã hoàn tất tất cả món ăn của bàn!'
    playAlertChime('ready')
    await fetchKitchenData()

    setTimeout(() => {
      if (successMsg.value) successMsg.value = ''
    }, 4000)
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi hoàn tất bàn.'
  }
}

// Toggle Sold Out
const toggleFoodSoldOut = async (foodId, isActive) => {
  togglingFoodId.value = foodId
  errorMsg.value = ''
  try {
    const res = await axios.patch(
      `${API_BASE}/foods/${foodId}/toggle-sold-out`,
      { isActive },
      { headers: getAuthHeaders() }
    )
    successMsg.value = res.data.message || 'Đã cập nhật tình trạng món ăn.'
    await fetchFoods()
    await fetchMetrics()

    setTimeout(() => {
      if (successMsg.value) successMsg.value = ''
    }, 4000)
  } catch (err) {
    errorMsg.value = err.response?.data?.message || 'Lỗi khi cập nhật tình trạng món.'
  } finally {
    togglingFoodId.value = null
  }
}

// Helper methods for styling and badges (LIGHT THEME)
const getCardUrgencyClass = (elapsed) => {
  if (elapsed >= 20) return 'border-start-rose-thick shadow'
  if (elapsed >= 10) return 'border-start-amber-thick shadow-xs'
  return 'border-start-slate-thick'
}

const getHeaderUrgencyClass = (elapsed) => {
  if (elapsed >= 20) return 'bg-rose-50 text-rose-900 border-bottom border-rose-200'
  if (elapsed >= 10) return 'bg-amber-50 text-amber-900 border-bottom border-amber-200'
  return 'bg-slate-50 text-slate-900 border-bottom border-slate-200'
}

const getWaitBadgeClass = (minutes) => {
  if (minutes >= 20) return 'bg-danger text-white pulse-alert'
  if (minutes >= 10) return 'bg-warning text-dark'
  return 'bg-slate-100 text-slate-700 border'
}

const getItemRowClass = (status) => {
  if (status === 'cooking') return 'bg-blue-50 border-blue-200'
  if (status === 'ready') return 'bg-emerald-50 border-emerald-200'
  return 'bg-white border-slate-200'
}

const getStatusBadgeClass = (status) => {
  switch (status) {
    case 'confirmed': return 'bg-amber-100 text-amber-800 border border-amber-300'
    case 'cooking': return 'bg-blue-100 text-blue-800 border border-blue-300'
    case 'ready': return 'bg-emerald-100 text-emerald-800 border border-emerald-300'
    case 'served': return 'bg-slate-100 text-slate-600 border'
    default: return 'bg-slate-100 text-slate-800'
  }
}

const getStatusLabel = (status) => {
  switch (status) {
    case 'confirmed': return '⏳ Chờ làm'
    case 'cooking': return '🍳 Đang nấu'
    case 'ready': return '🛎️ Sẵn sàng'
    case 'served': return '✔️ Đã ra bàn'
    default: return status
  }
}

const handleLogout = () => {
  localStorage.clear()
  router.push('/login')
}

// Lifecycle Hooks
onMounted(() => {
  const updateClock = () => {
    const d = new Date()
    currentTime.value = d.toLocaleTimeString('vi-VN', { hour12: false })
  }
  updateClock()
  clockInterval = setInterval(updateClock, 1000)

  fetchKitchenData()
  fetchFoods()

  // ĐĂNG KÝ SỰ KIỆN REALTIME QUA SIGNALR WEBSOCKET (TỨC THÌ 0s)
  signalRService.on('KitchenUpdated', async () => {
    await fetchKitchenDataSilently()
  })
  signalRService.on('StaffOrderUpdated', async () => {
    await fetchKitchenDataSilently()
  })

  // Heartbeat nhẹ nhàng 60s (chạy ngầm silent không giật màn hình)
  refreshInterval = setInterval(async () => {
    await fetchKitchenDataSilently()
  }, 60000)
})

onUnmounted(() => {
  if (clockInterval) clearInterval(clockInterval)
  if (refreshInterval) clearInterval(refreshInterval)
  signalRService.off('KitchenUpdated')
  signalRService.off('StaffOrderUpdated')
})
</script>

<style scoped>
/* Base Light Theme */
.bg-light-app {
  background-color: #f1f5f9 !important;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
  user-select: none;
  color: #1e293b;
}

/* Brand icon */
.brand-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: linear-gradient(135deg, #e0e7ff 0%, #c7d2fe 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
}

/* Colors & Badges */
.text-slate-800 { color: #1e293b !important; }
.text-slate-900 { color: #0f172a !important; }
.text-slate-700 { color: #334155 !important; }
.text-slate-600 { color: #475569 !important; }
.bg-slate-50 { background-color: #f8fafc !important; }
.bg-slate-100 { background-color: #f1f5f9 !important; }
.bg-slate-200 { background-color: #e2e8f0 !important; }

.text-indigo { color: #4f46e5 !important; }
.bg-indigo { background-color: #4f46e5 !important; }
.bg-indigo-subtle { background-color: #eef2ff !important; }
.border-indigo-subtle { border-color: #c7d2fe !important; }
.btn-indigo {
  background-color: #4f46e5 !important;
  color: #ffffff !important;
  border: 1px solid #4338ca !important;
}
.btn-indigo:hover {
  background-color: #4338ca !important;
}

.text-emerald { color: #059669 !important; }
.text-emerald-900 { color: #064e3b !important; }
.text-emerald-700 { color: #047857 !important; }
.bg-emerald { background-color: #10b981 !important; }
.bg-emerald-50 { background-color: #ecfdf5 !important; }
.bg-emerald-subtle { background-color: #d1fae5 !important; }
.border-emerald { border-color: #a7f3d0 !important; }
.border-emerald-200 { border-color: #a7f3d0 !important; }
.border-emerald-subtle { border-color: #6ee7b7 !important; }
.btn-emerald {
  background-color: #10b981 !important;
  color: #ffffff !important;
  border: 1px solid #059669 !important;
}
.btn-emerald:hover {
  background-color: #059669 !important;
  color: #ffffff !important;
}

.text-amber { color: #d97706 !important; }
.text-amber-600 { color: #d97706 !important; }
.text-amber-900 { color: #78350f !important; }
.bg-amber { background-color: #f59e0b !important; }
.bg-amber-50 { background-color: #fffbeb !important; }
.bg-amber-100 { background-color: #fef3c7 !important; }
.border-amber-200 { border-color: #fde68a !important; }
.border-amber-300 { border-color: #fcd34d !important; }
.btn-amber {
  background-color: #f59e0b !important;
  color: #0f172a !important;
}

.text-rose { color: #e11d48 !important; }
.text-rose-800 { color: #9f1239 !important; }
.text-rose-900 { color: #881337 !important; }
.bg-rose { background-color: #f43f5e !important; }
.bg-rose-50 { background-color: #fff1f2 !important; }
.bg-rose-subtle { background-color: #ffe4e6 !important; }
.border-rose { border-color: #fecdd3 !important; }
.border-rose-200 { border-color: #fecdd3 !important; }
.border-rose-subtle { border-color: #fda4af !important; }
.btn-rose {
  background-color: #f43f5e !important;
  color: #ffffff !important;
}

.bg-blue-50 { background-color: #eff6ff !important; }
.bg-blue-100 { background-color: #dbeafe !important; }
.border-blue-200 { border-color: #bfdbfe !important; }
.border-blue-300 { border-color: #93c5fd !important; }

/* Buttons */
.btn-white {
  background-color: #ffffff !important;
  border-color: #e2e8f0 !important;
}
.btn-slate-800 {
  background-color: #1e293b !important;
  color: #ffffff !important;
}
.btn-outline-slate {
  border-color: #cbd5e1 !important;
}
.btn-outline-slate:hover {
  background-color: #f8fafc !important;
}

/* KPI Badges */
.kpi-pill {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 9999px;
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  font-size: 0.8rem;
  font-weight: 600;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
}
.kpi-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.kpi-val {
  font-weight: 800;
  font-size: 0.95rem;
}

/* Tickets & Cards */
.kds-ticket-card {
  transition: transform 0.15s ease, box-shadow 0.15s ease;
  background-color: #ffffff !important;
}
.kds-ticket-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.08), 0 4px 6px -4px rgba(0, 0, 0, 0.04) !important;
}

.border-start-slate-thick {
  border-left: 5px solid #cbd5e1 !important;
}
.border-start-amber-thick {
  border-left: 5px solid #f59e0b !important;
}
.border-start-rose-thick {
  border-left: 5px solid #f43f5e !important;
}

/* Pickup Table Card */
.pickup-table-card {
  transition: transform 0.15s ease, box-shadow 0.15s ease;
  border: 2px solid #a7f3d0 !important;
}
.pickup-table-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 15px 25px -5px rgba(16, 185, 129, 0.15) !important;
}

/* Highlight Special Request Alert Note */
.alert-note-box {
  background-color: #fff1f2 !important;
  border: 1px dashed #f43f5e !important;
}

/* Pulsing alert animations */
@keyframes pulse-red {
  0% { box-shadow: 0 0 0 0 rgba(244, 63, 94, 0.6); }
  70% { box-shadow: 0 0 0 8px rgba(244, 63, 94, 0); }
  100% { box-shadow: 0 0 0 0 rgba(244, 63, 94, 0); }
}

@keyframes pulse-green {
  0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.6); }
  70% { box-shadow: 0 0 0 8px rgba(16, 185, 129, 0); }
  100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}

.pulse-alert {
  animation: pulse-red 2s infinite;
}
.pulse-ready {
  animation: pulse-green 2s infinite;
}

.shadow-xs {
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
}

.transition {
  transition: all 0.15s ease-in-out;
}

.realtime-dot {
  width: 8px;
  height: 8px;
  background-color: #10b981;
  border-radius: 50%;
  display: inline-block;
  animation: pulse-dot 1.5s infinite;
}
@keyframes pulse-dot {
  0% { transform: scale(0.95); opacity: 0.8; box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
  70% { transform: scale(1); opacity: 1; box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
  100% { transform: scale(0.95); opacity: 0.8; box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
}
</style>


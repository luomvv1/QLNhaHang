<template>
  <div class="staff-order-pos d-flex flex-column min-vh-100 bg-light">
    <!-- Floating Realtime Bank Transfer Toast Notification -->
    <div
      v-if="liveBankToast"
      class="position-fixed top-0 end-0 p-3"
      style="z-index: 1095; max-width: 420px;"
    >
      <div class="toast show bg-white border border-success shadow-lg rounded-4 overflow-hidden" role="alert">
        <div class="toast-header bg-success text-white py-2 px-3 d-flex justify-content-between align-items-center">
          <strong class="me-auto d-flex align-items-center gap-2">
            <span class="fs-5">🔔</span>
            <span class="fw-bold">TIỀN VỀ TÀI KHOẢN (REALTIME)</span>
          </strong>
          <button type="button" class="btn-close btn-close-white" @click="liveBankToast = null"></button>
        </div>
        <div class="toast-body p-3">
          <div class="fw-bold text-dark fs-6 mb-1">
            Khách bàn <span class="badge bg-primary fs-6">{{ liveBankToast.tableDisplay }}</span> vừa thanh toán:
          </div>
          <div class="fs-3 fw-bolder text-success mb-2">
            +{{ formatCurrency(liveBankToast.amount) }}
          </div>
          <div class="small text-muted mb-3">
            Hệ thống đã tự động xuất hóa đơn và giải phóng bàn ăn về trạng thái Trống.
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-sm btn-success fw-bold flex-grow-1" @click="viewToastInvoice(liveBankToast.invoice)">
              🧾 Mở Hóa Đơn K80
            </button>
            <button class="btn btn-sm btn-outline-secondary" @click="liveBankToast = null">
              Đóng
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 1. Top Navbar / Header -->
    <header class="navbar navbar-expand-lg navbar-dark bg-dark px-3 py-2 shadow-sm sticky-top">
      <div class="container-fluid d-flex justify-content-between align-items-center">
        <!-- Logo & Brand -->
        <div class="d-flex align-items-center gap-2">
          <span class="fs-4">🍽️</span>
          <div>
            <h5 class="mb-0 text-white fw-bold">HỆ THỐNG GỌI MÓN TẠI BÀN (STAFF POS)</h5>
            <small class="text-white-50">Sơ đồ bàn thời gian thực & Thực đơn phục vụ</small>
          </div>
        </div>

        <!-- KPI Tóm tắt bàn -->
        <div class="d-none d-md-flex align-items-center gap-2">
          <span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1">
            🟢 Trống: <strong>{{ countEmpty }}</strong>
          </span>
          <span class="badge bg-warning-subtle text-warning border border-warning-subtle px-2 py-1">
            🟡 Phục vụ: <strong>{{ countServing }}</strong>
          </span>
          <span class="badge bg-info-subtle text-info border border-info-subtle px-2 py-1">
            🔵 Đã đặt: <strong>{{ countReserved }}</strong>
          </span>
          <span class="badge bg-secondary-subtle text-secondary border border-secondary-subtle px-2 py-1">
            ⚪ Dọn dẹp: <strong>{{ countCleaning }}</strong>
          </span>
        </div>

        <!-- Thông tin người dùng & Nút điều hướng -->
        <div class="d-flex align-items-center gap-2">
          <span class="badge bg-success-subtle text-success border border-success-subtle px-3 py-2 rounded-pill fw-bold shadow-xs d-flex align-items-center gap-1 d-none d-lg-inline-flex" title="Kết nối trực tiếp Real-time qua WebSocket">
            <span class="realtime-dot"></span> REALTIME LIVE
          </span>
          <router-link to="/staff/app" class="btn btn-warning btn-sm fw-bold shadow-sm d-flex align-items-center gap-1">
            <span>📱</span>
            <span>Chế Độ App</span>
          </router-link>
          <button class="btn btn-outline-light btn-sm fw-semibold" @click="refreshAllData" title="Làm mới dữ liệu từ server">
            🔄 Làm mới
          </button>
          <router-link v-if="userRole === 'manager' || userRole === 'admin'" to="/admin" class="btn btn-outline-warning btn-sm fw-semibold">
            ⚙️ Về Quản Trị
          </router-link>
          <router-link to="/chef" class="btn btn-outline-info btn-sm fw-semibold" :title="userRole === 'chef' ? 'Quay lại màn hình Bếp (KDS)' : 'Xem màn hình Bếp'">
            👨‍🍳 {{ userRole === 'chef' ? 'Về Bếp' : 'Sang Bếp' }}
          </router-link>
          <router-link to="/customer/menu" target="_blank" class="btn btn-outline-light btn-sm fw-semibold" title="Xem Thực Đơn & Trang Khách Hàng">
            📖 Trang Khách
          </router-link>
          <button class="btn btn-outline-info btn-sm fw-semibold position-relative" @click="openTodayReservationsModal" title="Xem danh sách khách đặt bàn hôm nay">
            📅 Bàn Đặt Hôm Nay
            <span v-if="todayReservationsPendingCount > 0" class="badge bg-danger rounded-pill ms-1">
              {{ todayReservationsPendingCount }}
            </span>
          </button>
          <button class="btn btn-outline-warning btn-sm fw-semibold" @click="openRecentInvoicesModal" title="Xem lịch sử hóa đơn">
            📑 Hóa Đơn Gần Đây
          </button>
          <div class="text-end text-white-50 d-none d-sm-block">
            <span class="d-block text-white fw-semibold small">👤 {{ userName }}</span>
            <small class="badge bg-primary-subtle text-primary border border-primary-subtle">
              {{ userRole === 'manager' ? 'Admin' : 'Nhân Viên' }}
            </small>
          </div>
          <button class="btn btn-outline-light btn-sm ms-2" @click="handleLogout" title="Đăng xuất">
            🚪 Đăng xuất
          </button>
        </div>
      </div>
    </header>

    <!-- 2. Alert Thông báo -->
    <div class="container-fluid px-3 pt-3">
      <div v-if="successMsg" class="alert alert-success alert-dismissible fade show shadow-sm py-2" role="alert">
        <span>✅ {{ successMsg }}</span>
        <button type="button" class="btn-close py-2" @click="successMsg = ''"></button>
      </div>
      <div v-if="errorMsg" class="alert alert-danger alert-dismissible fade show shadow-sm py-2" role="alert">
        <span>⚠️ {{ errorMsg }}</span>
        <button type="button" class="btn-close py-2" @click="errorMsg = ''"></button>
      </div>
    </div>

    <!-- 3. CHẾ ĐỘ 1: SƠ ĐỒ BÀN (Floor Plan) -->
    <main v-if="viewMode === 'tables'" class="container-fluid px-3 py-2 flex-grow-1">
      <!-- Toolbar lọc bàn -->
      <div class="card border-0 shadow-sm mb-3">
        <div class="card-body p-3">
          <div class="row g-2 align-items-center">
            <!-- Tìm kiếm bàn -->
            <div class="col-12 col-md-4">
              <div class="input-group input-group-sm">
                <span class="input-group-text bg-white border-end-0">🔍</span>
                <input
                  v-model="tableSearch"
                  class="form-control border-start-0"
                  placeholder="Tìm theo mã bàn, tên bàn, khu vực..."
                />
              </div>
            </div>

            <!-- Lọc theo Khu vực -->
            <div class="col-6 col-md-4">
              <select v-model="selectedArea" class="form-select form-select-sm">
                <option value="">Tất cả khu vực ({{ uniqueAreas.length }})</option>
                <option v-for="area in uniqueAreas" :key="area" :value="area">
                  📍 {{ area }}
                </option>
              </select>
            </div>

            <!-- Lọc theo Trạng thái -->
            <div class="col-6 col-md-4">
              <select v-model="selectedStatus" class="form-select form-select-sm">
                <option value="">Tất cả trạng thái bàn</option>
                <option value="empty">🟢 Bàn trống (Empty)</option>
                <option value="serving">🟡 Đang phục vụ (Serving)</option>
                <option value="reserved">🔵 Đã đặt trước (Reserved)</option>
                <option value="cleaning">⚪ Đang dọn dẹp (Cleaning)</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      <!-- Grid sơ đồ bàn -->
      <div v-if="isLoadingTables" class="text-center py-5">
        <div class="spinner-border text-primary" role="status"></div>
        <p class="mt-2 text-muted">Đang tải sơ đồ bàn thời gian thực...</p>
      </div>

      <div v-else class="row g-3">
        <div
          v-for="table in filteredTables"
          :key="table.tableId"
          class="col-12 col-sm-6 col-md-4 col-lg-3 col-xl-2"
        >
          <div
            class="card h-100 shadow-sm border table-card cursor-pointer transition"
            :class="getTableBorderClass(table.tableStatus)"
            @click="selectTable(table)"
          >
            <!-- Header thẻ bàn -->
            <div class="card-header bg-transparent d-flex justify-content-between align-items-center py-2 px-3 border-0">
              <span class="badge" :class="getTableBadgeClass(table.tableStatus)">
                {{ getTableStatusText(table.tableStatus) }}
              </span>
              <small class="text-muted fw-semibold">👥 {{ table.capacity }} người</small>
            </div>

            <!-- Body thẻ bàn -->
            <div class="card-body text-center py-3 px-2">
              <h4 class="fw-bold mb-1" :class="getTableTextClass(table.tableStatus)">
                {{ table.tableName }}
              </h4>
              <p class="text-muted small mb-2">📍 {{ table.areaName || 'Sảnh chung' }}</p>

              <!-- Nếu bàn đang phục vụ -->
              <div v-if="table.activeOrder" class="bg-warning-subtle rounded p-2 text-dark small mb-2 border border-warning-subtle">
                <div class="d-flex justify-content-between align-items-center mb-1">
                  <span class="fw-bold">Đơn #{{ table.activeOrder.orderId }}</span>
                  <span v-if="table.activeOrder.linkedTables && table.activeOrder.linkedTables.length > 0" class="badge bg-primary text-white" style="font-size: 0.65rem;">
                    +{{ table.activeOrder.linkedTables.length }} Bàn ghép
                  </span>
                </div>
                <div v-if="table.activeOrder.linkedTables && table.activeOrder.linkedTables.length > 0" class="text-primary fw-semibold small mb-1">
                  Ghép: {{ table.activeOrder.linkedTables.map(t => t.tableName).join(', ') }}
                </div>
                <!-- Cảnh báo có món đã nấu xong tại quầy cần bưng -->
                <div v-if="table.activeOrder.readyCount > 0" class="badge bg-danger text-white border border-danger-subtle w-100 py-1 my-1 animate-pulse shadow-sm" style="font-size: 0.75rem;">
                  🛎️ CÓ {{ table.activeOrder.readyCount }} MÓN ĐÃ XONG!
                </div>
                <div class="d-flex justify-content-between align-items-center text-muted small mt-1">
                  <span>🍽️ Tổng: {{ table.activeOrder.itemCount }} món</span>
                  <span v-if="table.activeOrder.servedCount > 0" class="text-success fw-bold">
                    ✅ Đã bưng: {{ table.activeOrder.servedCount }}
                  </span>
                  <span v-else-if="table.activeOrder.cookingCount > 0" class="text-primary">
                    🔥 Đang nấu: {{ table.activeOrder.cookingCount }}
                  </span>
                </div>
                <div class="text-danger fw-bold fs-6 mt-1">
                  {{ formatCurrency(table.activeOrder.totalAmount) }}
                </div>
              </div>

              <!-- Nếu bàn trống -->
              <div v-else-if="table.tableStatus === 'empty'" class="text-success small py-3">
                <span>🟢 Sẵn sàng đón khách</span>
              </div>

              <!-- Nếu bàn đã đặt -->
              <div v-else-if="table.tableStatus === 'reserved'" class="text-info small py-3">
                <span>🔵 Khách đã đặt trước</span>
              </div>

              <!-- Nếu bàn đang dọn -->
              <div v-else class="text-secondary small py-3">
                <span>⚪ Đang dọn dẹp bàn</span>
              </div>
            </div>

            <!-- Footer nút bấm thẻ bàn -->
            <div class="card-footer bg-transparent border-0 pt-0 pb-3 px-3">
              <div v-if="table.activeOrder" class="d-flex gap-1">
                <button
                  class="btn btn-warning btn-sm flex-grow-1 fw-bold shadow-sm"
                  @click.stop="openOrderForTable(table)"
                >
                  Vào Gọi Món 🍽️
                </button>
                <button
                  v-if="!table.activeOrder.isPrimary"
                  class="btn btn-outline-danger btn-sm"
                  @click.stop="handleUnlinkCurrentTable(table)"
                  title="Tháo ghép bàn này để trả về bàn trống"
                >
                  ✂️
                </button>
              </div>
              <button
                v-else-if="table.tableStatus === 'empty'"
                class="btn btn-outline-success btn-sm w-100 fw-bold shadow-sm"
                @click.stop="openNewOrderModal(table)"
              >
                + Mở Bàn Mới
              </button>
              <button
                v-else-if="table.tableStatus === 'reserved'"
                class="btn btn-primary btn-sm w-100 fw-bold shadow-sm"
                @click.stop="quickCheckInByTable(table)"
                title="Khách đặt trước đã đến: Check-in và mở Order ngay"
              >
                🛎️ Đón Khách Đặt Bàn
              </button>
              <button
                v-else
                class="btn btn-outline-secondary btn-sm w-100"
                @click.stop="selectTable(table)"
              >
                Xem chi tiết
              </button>
            </div>
          </div>
        </div>

        <div v-if="filteredTables.length === 0" class="col-12 text-center py-5">
          <div class="text-muted fs-5">Không tìm thấy bàn nào phù hợp với bộ lọc.</div>
        </div>
      </div>
    </main>

    <!-- 4. CHẾ ĐỘ 2: MÀN HÌNH ORDER TẠI BÀN (POS Desk) -->
    <main v-else class="container-fluid px-3 py-2 flex-grow-1 d-flex flex-column">
      <!-- Header Bàn đang chọn -->
      <div class="card border-0 shadow-sm mb-3">
        <div class="card-body p-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
          <!-- Thông tin bàn -->
          <div class="d-flex align-items-center gap-3">
            <button class="btn btn-outline-secondary btn-sm fw-semibold" @click="backToTables">
              ⬅️ Quay Lại Sơ Đồ Bàn
            </button>
            <div>
              <div class="d-flex align-items-center gap-2 flex-wrap">
                <h4 class="fw-bold mb-0 text-primary">
                  {{ activeTable?.tableName }}
                </h4>
                <span
                  v-if="currentOrder?.linkedTables && currentOrder.linkedTables.length > 1"
                  class="badge bg-primary-subtle text-primary border border-primary-subtle"
                >
                  ➕ Ghép: {{ currentOrder.linkedTables.filter(t => t.tableId !== activeTable?.tableId).map(t => t.tableName).join(', ') }}
                </span>
                <span class="badge bg-light text-dark border">
                  📍 {{ activeTable?.areaName || 'Sảnh chung' }}
                </span>
                <span class="badge" :class="getTableBadgeClass(activeTable?.tableStatus)">
                  {{ getTableStatusText(activeTable?.tableStatus) }}
                </span>
              </div>
              <small class="text-muted">
                Sức chứa: 👥 {{ activeTable?.capacity }} người | 
                <span v-if="currentOrder">Mã đơn: <strong>#{{ currentOrder.orderId }}</strong> | Giờ mở: {{ formatTime(currentOrder.createdAt) }}</span>
                <span v-else class="text-success">Bàn trống, chưa có đơn hàng</span>
              </small>
            </div>
          </div>

          <!-- Nhóm tác vụ phụ của Bàn -->
          <div class="d-flex align-items-center gap-2 flex-wrap">
            <button
              v-if="currentOrder"
              class="btn btn-outline-info btn-sm fw-semibold"
              @click="openSwitchTableModal"
            >
              🔄 Đổi bàn
            </button>
            <button
              v-if="currentOrder"
              class="btn btn-outline-primary btn-sm fw-semibold"
              @click="openMergeTableModal"
            >
              🔗 Ghép / Gộp bàn
            </button>
            <button
              v-if="currentOrder && !currentOrder.isPrimary"
              class="btn btn-outline-danger btn-sm fw-semibold"
              @click="handleUnlinkCurrentTable(activeTable)"
            >
              ✂️ Tháo ghép bàn
            </button>
            <button
              v-if="currentOrder && canCancelOrder"
              class="btn btn-outline-danger btn-sm fw-semibold"
              @click="cancelCurrentOrder"
            >
              ❌ Hủy đơn
            </button>
            <button
              v-if="!currentOrder && activeTable?.tableStatus === 'empty'"
              class="btn btn-success btn-sm fw-bold px-3"
              @click="createNewOrder(activeTable)"
            >
              + Mở Đơn Mới Ngay
            </button>
          </div>
        </div>
      </div>

      <!-- Không có Order và bàn trống -> Hiển thị thông báo mở đơn -->
      <div v-if="!currentOrder" class="card border-0 shadow-sm p-5 text-center my-auto">
        <div class="display-3 mb-3">🍽️</div>
        <h4 class="fw-bold text-dark">Bàn này chưa được mở phục vụ</h4>
        <p class="text-muted">Bấm nút bên dưới để mở đơn hàng mới và chọn món cho khách.</p>
        <div>
          <button class="btn btn-primary btn-lg fw-bold px-4 shadow" @click="createNewOrder(activeTable)">
            + Mở Order Bàn {{ activeTable?.tableName }}
          </button>
        </div>
      </div>

      <!-- Bố cục 2 Cột khi có Order -->
      <div v-else class="row g-3 flex-grow-1">
        <!-- CỘT TRÁI: THỰC ĐƠN CHỌN MÓN (Menu) -->
        <div class="col-12 col-lg-7 d-flex flex-column">
          <div class="card border-0 shadow-sm flex-grow-1 d-flex flex-column">
            <!-- Header Thực đơn & Tìm kiếm -->
            <div class="card-header bg-white border-bottom p-3">
              <div class="row g-2 align-items-center">
                <div class="col-12 col-md-6">
                  <h6 class="fw-bold mb-0 text-dark">📖 THỰC ĐƠN MÓN ĂN ({{ filteredFoods.length }} Món)</h6>
                </div>
                <div class="col-12 col-md-6">
                  <input
                    v-model="foodSearch"
                    class="form-control form-control-sm"
                    placeholder="🔍 Tìm nhanh món ăn theo tên..."
                  />
                </div>
              </div>

              <!-- Thanh Danh mục món (Tabs/Pills) -->
              <div class="d-flex gap-2 overflow-auto pt-2 mt-2 border-top">
                <button
                  class="btn btn-sm text-nowrap rounded-pill"
                  :class="selectedCategory === '' ? 'btn-primary' : 'btn-outline-secondary'"
                  @click="selectedCategory = ''"
                >
                  Tất cả
                </button>
                <button
                  v-for="cat in menuCategories"
                  :key="cat.categoryId"
                  class="btn btn-sm text-nowrap rounded-pill"
                  :class="selectedCategory === cat.categoryId ? 'btn-primary' : 'btn-outline-secondary'"
                  @click="selectedCategory = cat.categoryId"
                >
                  {{ cat.categoryName }}
                </button>
              </div>
            </div>

            <!-- Danh sách món ăn Grid Cards -->
            <div class="card-body p-3 overflow-auto" style="max-height: calc(100vh - 310px);">
              <div class="row g-2">
                <div
                  v-for="food in filteredFoods"
                  :key="food.foodId"
                  class="col-12 col-sm-6 col-md-4"
                >
                  <div class="card h-100 border shadow-sm food-item-card transition" @click="openSelectVariantModal(food)">
                    <div class="card-body p-2 d-flex flex-column justify-content-between">
                      <div>
                        <!-- Tên món -->
                        <h6 class="fw-bold mb-1 text-dark line-clamp-1">
                          {{ food.foodName }}
                        </h6>
                        <!-- Giá khởi điểm -->
                        <div class="text-danger fw-bold small">
                          {{ formatCurrency(getFoodMinPrice(food)) }}
                        </div>
                        <small class="d-block mt-1" :class="food.canOrder === false ? 'text-danger' : 'text-muted'">
                          {{ food.availabilityText || (food.canOrder === false ? 'Bếp tạm hết món' : `${food.variants.length} size`) }}
                        </small>
                      </div>

                      <button class="btn btn-outline-primary btn-sm w-100 mt-2 fw-semibold" :disabled="food.canOrder === false">
                        {{ food.canOrder === false ? (food.availabilityText || 'Hết món') : '+ Chọn món' }}
                      </button>
                    </div>
                  </div>
                </div>

                <div v-if="filteredFoods.length === 0" class="col-12 text-center py-4 text-muted">
                  Không tìm thấy món nào phù hợp.
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- CỘT PHẢI: HÓA ĐƠN ĐƠN HÀNG CỦA BÀN -->
        <div class="col-12 col-lg-5 d-flex flex-column pos-sticky-panel">
          <div class="card border-0 shadow-sm flex-grow-1 d-flex flex-column">
            <!-- Header Order -->
            <div class="card-header bg-white border-bottom p-3 d-flex justify-content-between align-items-center">
              <div>
                <h6 class="fw-bold mb-0 text-dark">📋 ĐƠN GỌI MÓN (ORDER)</h6>
                <small class="text-muted">Tổng số: {{ orderItems.length }} dòng món</small>
              </div>
              <span class="badge bg-primary-subtle text-primary border border-primary-subtle px-2 py-1">
                {{ currentOrder.orderStatus.toUpperCase() }}
              </span>
            </div>

            <!-- Danh sách các món trong Order -->
            <div class="card-body p-0 overflow-auto flex-grow-1" style="max-height: calc(100vh - 430px);">
              <div v-if="orderItems.length === 0" class="text-center py-5 text-muted">
                <div class="fs-1 mb-2">🛒</div>
                <p>Chưa có món nào trong đơn hàng.<br>Hãy chọn món từ thực đơn bên trái.</p>
              </div>

              <div v-else class="list-group list-group-flush">
                <div
                  v-for="item in orderItems"
                  :key="item.orderItemId"
                  class="list-group-item p-3 border-bottom"
                >
                  <div class="d-flex justify-content-between align-items-start mb-1">
                    <div>
                      <h6 class="fw-bold mb-0 text-dark">
                        {{ item.foodNameSnapshot }}
                        <span class="badge bg-light text-secondary border ms-1 small">
                          {{ item.variantSnapshot }}
                        </span>
                      </h6>
                      <small class="text-muted">Đơn giá: {{ formatCurrency(item.UnitPrice) }}</small>
                    </div>
                    <div class="text-end">
                      <span class="fw-bold text-danger">
                        {{ formatCurrency(item.ItemStatus === 'cancelled' ? 0 : item.LineTotal) }}
                      </span>
                    </div>
                  </div>

                  <!-- Ghi chú món nếu có -->
                  <div v-if="item.Note" class="text-warning-emphasis small fst-italic mb-2 bg-warning-subtle px-2 py-1 rounded">
                    ✏️ Ghi chú: {{ item.Note }}
                  </div>

                  <!-- Dòng điều khiển số lượng & Trạng thái món -->
                  <p v-if="item.CancellationReason" class="small text-danger mb-1">Đã hủy: {{ item.CancellationReason }}</p>
                  <div class="d-flex justify-content-between align-items-center mt-2">
                    <!-- Trạng thái món -->
                    <div>
                      <span class="badge" :class="getItemStatusBadgeClass(item.ItemStatus)">
                        {{ getItemStatusText(item.ItemStatus) }}
                      </span>
                    </div>

                    <!-- Điều khiển số lượng -->
                    <div class="d-flex align-items-center gap-1">
                      <!-- Nếu món còn PENDING -> Cho phép tăng giảm / xóa -->
                      <template v-if="item.ItemStatus === 'pending'">
                        <button
                          class="btn btn-outline-secondary btn-sm px-2 py-0"
                          @click="updateItemQty(item, item.Quantity - 1)"
                        >
                          -
                        </button>
                        <span class="fw-bold px-2">{{ item.Quantity }}</span>
                        <button
                          class="btn btn-outline-secondary btn-sm px-2 py-0"
                          @click="updateItemQty(item, item.Quantity + 1)"
                        >
                          +
                        </button>
                        <button
                          class="btn btn-outline-danger btn-sm py-0 ms-2"
                          @click="deleteItem(item)"
                          title="Xóa món này"
                        >
                          🗑️
                        </button>
                      </template>

                      <!-- Nếu món đã gửi bếp -> Không được tự giảm -->
                      <template v-else>
                        <button v-if="canCancelItem(item)" class="btn btn-outline-danger btn-sm" @click="cancelSentItem(item)">Hủy món</button>
                        <span class="badge bg-light text-dark border px-2 py-1">
                          Số lượng: <strong>{{ item.Quantity }}</strong>
                        </span>
                      </template>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Footer Tạm tính & Nút Gửi Bếp -->
            <div class="card-footer bg-white border-top p-3">
              <div class="d-flex justify-content-between align-items-center mb-2">
                <span class="text-muted">Tạm tính ({{ totalItemQuantity }} món):</span>
                <span class="fs-5 fw-bold text-danger">
                  {{ formatCurrency(currentOrder.subtotal) }}
                </span>
              </div>

              <!-- Nhóm nút bấm hành động -->
              <div class="d-grid gap-2">
                <button
                  class="btn btn-warning btn-lg fw-bold shadow-sm d-flex justify-content-between align-items-center px-3"
                  :disabled="pendingItemsCount === 0 || isSubmitting"
                  @click="confirmOrderToKitchen"
                >
                  <span>⚡ GỬI MÓN XUỐNG BẾP</span>
                  <span class="badge bg-dark text-white rounded-pill px-2 py-1">
                    {{ pendingItemsCount }} món chờ
                  </span>
                </button>

                <button
                  class="btn btn-success btn-lg fw-bold shadow-sm d-flex justify-content-between align-items-center px-3"
                  :disabled="orderItems.length === 0 || isSubmitting"
                  @click="openCheckoutModal"
                >
                  <span>💳 THANH TOÁN (CHECKOUT)</span>
                  <span class="badge bg-white text-success rounded-pill px-2 py-1 fs-6">
                    {{ formatCurrency(currentOrder.subtotal) }}
                  </span>
                </button>

                <div class="d-flex gap-2">
                  <button
                    class="btn btn-outline-secondary btn-sm flex-grow-1"
                    @click="showReceiptModal = true"
                    :disabled="orderItems.length === 0"
                  >
                    🧾 Xem tạm tính
                  </button>
                  <button
                    class="btn btn-outline-primary btn-sm flex-grow-1"
                    @click="loadCurrentTableOrder"
                  >
                    🔄 Cập nhật tiến độ
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- ============================================================== -->
    <!-- 5. MODAL CHỌN BIẾN THỂ & GHI CHÚ MÓN                           -->
    <!-- ============================================================== -->
    <div
      v-if="showVariantModal"
      class="modal fade show d-block"
      style="background: rgba(0, 0, 0, 0.5);"
      @click.self="showVariantModal = false"
    >
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg">
          <div class="modal-header bg-primary text-white">
            <h5 class="modal-title fw-bold">🍽️ {{ selectedFood?.foodName }}</h5>
            <button type="button" class="btn-close btn-close-white" @click="showVariantModal = false"></button>
          </div>
          <div class="modal-body p-4">
            <!-- 1. Chọn biến thể size -->
            <label class="form-label fw-bold">1. Chọn kích cỡ / biến thể:</label>
            <div class="d-grid gap-2 mb-3">
              <button type="button"
                v-for="v in selectedFood?.variants"
                :key="v.variantId"
                class="p-2 border rounded cursor-pointer transition d-flex justify-content-between align-items-center"
                :class="selectedVariantId === v.variantId ? 'border-primary bg-primary-subtle fw-bold' : 'border-light-subtle'"
                :disabled="v.canOrder === false" @click="selectedVariantId = v.variantId"
              >
                <div>
                  <span>{{ v.variantName }}<small class="d-block" :class="v.canOrder === false ? 'text-danger' : 'text-muted'">{{ v.availabilityText || (v.canOrder === false ? 'Bếp tạm hết món' : '') }}</small></span>
                  <small class="text-muted ms-1">({{ v.unit }})</small>
                </div>
                <span class="text-danger">{{ formatCurrency(v.price) }}</span>
              </button>
            </div>

            <!-- 2. Chọn số lượng -->
            <div class="mb-3">
              <label class="form-label fw-bold">2. Số lượng:</label>
              <div class="input-group" style="max-width: 180px;">
                <button class="btn btn-outline-secondary" @click="selectedQty = Math.max(1, selectedQty - 1)">-</button>
                <input v-model.number="selectedQty" type="number" min="1" class="form-control text-center fw-bold" />
                <button class="btn btn-outline-secondary" @click="selectedQty++">+</button>
              </div>
            </div>

            <!-- 3. Ghi chú món -->
            <div class="mb-2">
              <label class="form-label fw-bold">3. Ghi chú cho Bếp:</label>
              <input
                v-model="itemNote"
                class="form-control"
                placeholder="VD: Ít cay, không hành, ít đá..."
              />
              <!-- Gợi ý ghi chú nhanh -->
              <div class="d-flex flex-wrap gap-1 mt-2">
                <button
                  v-for="quick in quickNotes"
                  :key="quick"
                  type="button"
                  class="btn btn-sm btn-light border small py-0 px-2"
                  @click="itemNote = itemNote ? `${itemNote}, ${quick}` : quick"
                >
                  + {{ quick }}
                </button>
              </div>
            </div>
          </div>
          <div class="modal-footer border-top">
            <button class="btn btn-light" @click="showVariantModal = false">Hủy</button>
            <button
              class="btn btn-primary fw-bold px-4"
              :disabled="!selectedVariantId || selectedQty < 1 || selectedFood?.canOrder === false || !selectedFood?.variants?.some(v => v.variantId === selectedVariantId && v.canOrder !== false)"
              @click="confirmAddItemToOrder"
            >
              Thêm vào đơn hàng
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- 6. MODAL ĐỔI BÀN & GHÉP BÀN (StaffTableActionsModal)          -->
    <!-- ============================================================== -->
    <StaffTableActionsModal
      :show-switch="showSwitchModal"
      :show-merge="showMergeModal"
      :active-table="activeTable"
      :current-order="currentOrder"
      :empty-tables="emptyTables"
      :other-serving-tables="otherServingTables"
      :order-items-count="orderItems.length"
      @close-switch="showSwitchModal = false"
      @close-merge="showMergeModal = false"
      @switched="handleTableSwitched"
      @merged="handleTableMerged"
    />

    <!-- ============================================================== -->
    <!-- 7. MODAL XEM PHIẾU TẠM TÍNH (Receipt Preview)                  -->
    <!-- ============================================================== -->
    <div
      v-if="showReceiptModal"
      class="modal fade show d-block"
      style="background: rgba(0, 0, 0, 0.5);"
      @click.self="showReceiptModal = false"
    >
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg">
          <div class="modal-header bg-light">
            <h5 class="modal-title fw-bold">🧾 Phiếu Tạm Tính Bàn {{ activeTable?.tableName }}</h5>
            <button type="button" class="btn-close" @click="showReceiptModal = false"></button>
          </div>
          <div class="modal-body p-4 font-monospace">
            <!-- Header Bill -->
            <div class="text-center mb-3">
              <h5 class="fw-bold mb-0">NHÀ HÀNG HƯƠNG SEN</h5>
              <small class="text-muted">Phiếu Tạm Tính (Chưa thanh toán)</small>
              <div class="border-top border-bottom my-2 py-1 small">
                <div>Bàn: <strong>{{ activeTable?.tableName }}</strong> ({{ activeTable?.areaName }})</div>
                <div>Đơn: #{{ currentOrder?.orderId }} | Giờ vào: {{ formatTime(currentOrder?.createdAt) }}</div>
                <div>Phục vụ: {{ userName }}</div>
              </div>
            </div>

            <!-- Danh sách món -->
            <table class="table table-sm small mb-3">
              <thead>
                <tr>
                  <th>Món</th>
                  <th class="text-center">SL</th>
                  <th class="text-end">Đ.Giá</th>
                  <th class="text-end">T.Tiền</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in orderItems" :key="item.orderItemId">
                  <td>
                    {{ item.foodNameSnapshot }}<br>
                    <small class="text-muted">({{ item.variantSnapshot }})</small>
                  </td>
                  <td class="text-center">{{ item.Quantity }}</td>
                  <td class="text-end">{{ formatCurrency(item.UnitPrice) }}</td>
                  <td class="text-end fw-bold">{{ formatCurrency(item.LineTotal) }}</td>
                </tr>
              </tbody>
            </table>

            <!-- Tổng cộng -->
            <div class="d-flex justify-content-between align-items-center border-top pt-2 fs-5 fw-bold">
              <span>TỔNG TIỀN:</span>
              <span class="text-danger">{{ formatCurrency(currentOrder?.subtotal) }}</span>
            </div>
            <div class="text-center text-muted small mt-3">
              * Quý khách vui lòng kiểm tra lại món trước khi thanh toán *
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" @click="showReceiptModal = false">Đóng</button>
            <button class="btn btn-primary" @click="window.print()">🖨️ In Phiếu</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- 8. MODAL THANH TOÁN (StaffCheckoutModal)                       -->
    <!-- ============================================================== -->
    <StaffCheckoutModal
      v-if="currentOrder"
      :show="showCheckoutModal"
      :order-id="currentOrder.orderId"
      :table-name="activeTable?.tableName || ''"
      :table-display="activeTable?.tableName || ''"
      :user-name="userName"
      @close="showCheckoutModal = false"
      @success="handleCheckoutSuccess"
    />

    <!-- ============================================================== -->
    <!-- 9. MODAL IN HÓA ĐƠN K80 (StaffInvoiceModal)                     -->
    <!-- ============================================================== -->
    <StaffInvoiceModal
      :show="showInvoiceModal"
      :invoice="completedInvoice"
      :table-display="activeTable?.tableName || 'Bàn'"
      :cashier-name="userName"
      @close="finishPaymentAndReturn"
    />

    <!-- ============================================================== -->
    <!-- 10. MODAL LỊCH SỬ HÓA ĐƠN GẦN ĐÂY (StaffRecentInvoicesModal)  -->
    <!-- ============================================================== -->
    <StaffRecentInvoicesModal
      :show="showRecentInvoicesModal"
      @close="showRecentInvoicesModal = false"
      @view-invoice="viewAndPrintInvoiceDetail"
    />

    <!-- ============================================================== -->
    <!-- 11. MODAL ĐẶT BÀN HÔM NAY (StaffTodayReservationsModal)        -->
    <!-- ============================================================== -->
    <StaffTodayReservationsModal
      :show="showTodayReservationsModal"
      @close="showTodayReservationsModal = false"
      @check-in="checkInReservationAndOpenOrder"
      @table-assigned="loadTables"
    />
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

// Import các component modal con đã được tách riêng biệt
import StaffTableActionsModal from './components/StaffTableActionsModal.vue';
import StaffCheckoutModal from './components/StaffCheckoutModal.vue';
import StaffInvoiceModal from './components/StaffInvoiceModal.vue';
import StaffRecentInvoicesModal from './components/StaffRecentInvoicesModal.vue';
import StaffTodayReservationsModal from './components/StaffTodayReservationsModal.vue';

const router = useRouter();
const apiBase = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/staff/orders`;
const paymentApiBase = `${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/staff/payment`;

// ==============================================================
// 1. DỮ LIỆU USER & ĐIỀU HƯỚNG
// ==============================================================
const userName = ref(localStorage.getItem('fullName') || 'Nhân viên');
const userRole = ref((localStorage.getItem('role') || 'staff').toLowerCase());

// Quản lý chế độ xem (Chế độ xem sơ đồ bàn hoặc Order chi tiết)
const viewMode = ref('tables'); // 'tables' | 'order'
const activeTable = ref(null);
const currentOrder = ref(null);
const orderItems = ref([]);

// ==============================================================
// 2. SƠ ĐỒ BÀN & BỘ LỌC
// ==============================================================
const tables = ref([]);
const isLoadingTables = ref(false);
const tableSearch = ref('');
const selectedArea = ref('');
const selectedStatus = ref('');

// ==============================================================
// 3. THỰC ĐƠN GỌI MÓN (POS MENU)
// ==============================================================
const menuCategories = ref([]);
const menuFoods = ref([]);
const foodSearch = ref('');
const selectedCategory = ref('');

// Modal chọn biến thể & ghi chú món
const showVariantModal = ref(false);
const selectedFood = ref(null);
const selectedVariantId = ref(null);
const selectedQty = ref(1);
const itemNote = ref('');
const quickNotes = ['Ít cay', 'Không hành', 'Không đá', 'Ít ngọt', 'Mang về', 'Làm nhanh'];

// ==============================================================
// 4. QUẢN LÝ MODAL CON (ĐỔI BÀN, GỘP BÀN, CHECKOUT, IN BILL, ĐẶT BÀN)
// ==============================================================
const showSwitchModal = ref(false);
const showMergeModal = ref(false);
const showReceiptModal = ref(false);
const showCheckoutModal = ref(false);
const showInvoiceModal = ref(false);
const completedInvoice = ref(null);
const liveBankToast = ref(null);
const showRecentInvoicesModal = ref(false);
const showTodayReservationsModal = ref(false);
const todayReservations = ref([]);
const isLoadingTodayReservations = ref(false);

// Thông báo hệ thống
const successMsg = ref('');
const errorMsg = ref('');
const isSubmitting = ref(false);

let heartbeatTimer = null;

// ==============================================================
// 5. COMPUTED PROPERTIES
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
const countCleaning = computed(() => tables.value.filter(t => t.tableStatus === 'cleaning').length);

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

// ==============================================================
// 6. LIFECYCLE & SIGNALR REALTIME
// ==============================================================
onMounted(async () => {
  signalRService.on('FoodAvailabilityChanged', loadMenu);
  signalRService.on('PortionAvailabilityChanged', loadMenu);
  await Promise.all([loadTables(), loadMenu(), loadTodayReservations()]);

  // Lắng nghe sự kiện Order/Bếp cập nhật
  signalRService.on('StaffOrderUpdated', async () => {
    await loadTodayReservations();
    if (viewMode.value === 'tables') {
      await loadTablesSilently();
    } else if (viewMode.value === 'order' && activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
  });

  signalRService.on('KitchenUpdated', async () => {
    if (viewMode.value === 'tables') {
      await loadTablesSilently();
    } else if (viewMode.value === 'order' && activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
  });

  signalRService.on('ReservationUpdated', async () => {
    await loadTodayReservations();
    if (viewMode.value === 'tables') {
      await loadTablesSilently();
    }
  });

  // LẮNG NGHE CHUYỂN KHOẢN NGÂN HÀNG THÀNH CÔNG (REALTIME WEBHOOK)
  signalRService.on('PaymentReceived', async (data) => {
    // Nếu checkout modal đang mở cho đơn hàng này, modal sẽ tự phát âm ting ting và hiển thị Tick xanh cỡ lớn rồi emit @success
    if (showCheckoutModal.value && currentOrder.value?.orderId === data.orderId) {
      await loadTablesSilently();
      return;
    }

    playCashChime();
    completedInvoice.value = data.invoice;
    showCheckoutModal.value = false;
    showInvoiceModal.value = true;
    liveBankToast.value = null;
    successMsg.value = `🎉 Khách bàn ${data.tableName || data.tableDisplay} đã thanh toán ${formatCurrency(data.amount)}! Đang tự động mở lệnh in hóa đơn...`;

    setTimeout(() => {
      window.print();
    }, 600);

    await loadTablesSilently();
    if (viewMode.value === 'order' && activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
  });

  // Polling nhẹ nhàng 60s
  heartbeatTimer = setInterval(async () => {
    if (viewMode.value === 'tables') {
      await loadTablesSilently();
    } else if (viewMode.value === 'order' && activeTable.value) {
      await loadCurrentTableOrderSilently();
    }
  }, 60000);
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
// 7. CÁC HÀM XỬ LÝ DỮ LIỆU BÀN & MENU
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
  } catch (err) { }
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

async function refreshAllData() {
  if (viewMode.value === 'tables') {
    await loadTables();
  } else if (viewMode.value === 'order' && activeTable.value) {
    await loadCurrentTableOrder();
  }
  successMsg.value = 'Đã cập nhật dữ liệu mới nhất từ máy chủ!';
  setTimeout(() => { if (successMsg.value) successMsg.value = ''; }, 2000);
}

function selectTable(table) {
  openOrderForTable(table);
}

async function openOrderForTable(table) {
  activeTable.value = table;
  viewMode.value = 'order';
  await loadCurrentTableOrder();
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
    await Promise.all([loadTables(), loadCurrentTableOrder()]);
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể mở bàn mới.';
  }
}

function backToTables() {
  viewMode.value = 'tables';
  loadTables();
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
        viewMode.value = 'tables';
        await loadTablesSilently();
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
        const localPending = orderItems.value.filter(i => i.ItemStatus === 'pending' && !i.orderItemId);
        orderItems.value = [...serverItems, ...localPending];
      }
    }
  } catch (err) { }
}

// ==============================================================
// 8. TÁC VỤ GỌI MÓN (ORDER ITEMS)
// ==============================================================
function openSelectVariantModal(food) {
  if (food.canOrder === false) return;
  selectedFood.value = food;
  selectedVariantId.value = food.variants.find(v => v.canOrder !== false)?.variantId ?? null;
  selectedQty.value = 1;
  itemNote.value = '';
  showVariantModal.value = true;
}

async function confirmAddItemToOrder() {
  if (!selectedFood.value || selectedFood.value.canOrder === false ||
      !selectedFood.value.variants?.some(v => v.variantId === selectedVariantId.value && v.canOrder !== false)) return;
  if (!currentOrder.value) return;
  try {
    const res = await axios.post(`${apiBase}/${currentOrder.value.orderId}/items`, {
      foodId: selectedFood.value.foodId,
      variantId: selectedVariantId.value,
      quantity: selectedQty.value,
      note: itemNote.value
    });
    showVariantModal.value = false;
    successMsg.value = res.data?.message ?? 'Đã thêm món vào đơn.';
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
  if (!confirm(`Bạn có chắc muốn xóa món '${item.foodNameSnapshot}' khỏi đơn không?`)) return;
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
    await loadCurrentTableOrder();
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
    viewMode.value = 'tables';
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể hủy đơn hàng.';
  }
}

async function handleUnlinkCurrentTable(tableToUnlink) {
  const target = tableToUnlink || activeTable.value;
  if (!target) return;
  if (!confirm(`Bạn có chắc muốn tháo ghép bàn '${target.tableName}' để trả về trạng thái trống không?`)) return;
  try {
    const res = await axios.post(`${apiBase}/unlink-table`, {
      tableId: target.tableId
    });
    successMsg.value = res.data?.message ?? 'Đã tháo ghép bàn thành công.';
    if (target.tableId === activeTable.value?.tableId) {
      backToTables();
    } else {
      await Promise.all([loadTables(), loadCurrentTableOrder()]);
    }
  } catch (err) {
    errorMsg.value = err.response?.data?.message ?? 'Không thể tháo ghép bàn.';
  }
}

// ==============================================================
// 9. ĐỔI BÀN & GHÉP BÀN (HANDLERS TỪ CHILD COMPONENT)
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
  successMsg.value = 'Thao tác gộp bàn thành công!';
  if (payload && payload.actionType === 'merge_orders') {
    const targetTable = tables.value.find(t => t.tableId === payload.targetTableId);
    if (targetTable) {
      activeTable.value = targetTable;
    }
  }
  await Promise.all([loadTables(), loadCurrentTableOrder()]);
}

// ==============================================================
// 10. THANH TOÁN (CHECKOUT) & HÓA ĐƠN (INVOICE)
// ==============================================================
function openCheckoutModal() {
  if (!currentOrder.value) {
    errorMsg.value = 'Không tìm thấy đơn hàng cần thanh toán.';
    return;
  }
  const unconfirmed = orderItems.value.filter(i => i.ItemStatus === 'pending');
  if (unconfirmed.length > 0) {
    if (!confirm(`Đơn hàng đang có ${unconfirmed.length} món chưa gửi bếp. Bạn có chắc muốn tiếp tục thanh toán không?`)) {
      return;
    }
  }
  showCheckoutModal.value = true;
}

function handleCheckoutSuccess(invoice) {
  completedInvoice.value = invoice;
  showCheckoutModal.value = false;
  showInvoiceModal.value = true;
  successMsg.value = 'Thanh toán thành công!';
  loadTablesSilently();
}

function finishPaymentAndReturn() {
  showInvoiceModal.value = false;
  completedInvoice.value = null;
  viewMode.value = 'tables';
  activeTable.value = null;
  currentOrder.value = null;
  orderItems.value = [];
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

// ==============================================================
// 11. ĐẶT BÀN & CHECK-IN
// ==============================================================
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
    console.error('Lỗi tải danh sách đặt bàn hôm nay:', err);
  } finally {
    isLoadingTodayReservations.value = false;
  }
}

function openTodayReservationsModal() {
  loadTodayReservations();
  showTodayReservationsModal.value = true;
}

async function quickCheckInByTable(table) {
  await loadTodayReservations();
  const matchedRes = todayReservations.value.find(r => 
    r.table && r.table.tableId === table.tableId && r.reservationStatus === 'confirmed'
  );

  if (!matchedRes) {
    openTodayReservationsModal();
    return;
  }

  checkInReservationAndOpenOrder(matchedRes);
}

async function checkInReservationAndOpenOrder(res) {
  if (res?.reservationStatus !== 'confirmed' || !res?.table?.tableId) {
    errorMsg.value = 'Phiếu đặt bàn phải được xác nhận và phân bàn trước khi check-in.';
    return;
  }

  if (!confirm(`Xác nhận đón khách ${res.customerName} (${res.guestCount} người)? Hệ thống sẽ tự động kích hoạt bàn và mở Order ngay.`)) {
    return;
  }

  try {
    const resp = await axios.post(`${import.meta.env.VITE_API_URL || 'http://localhost:5079'}/api/admin/reservations/${res.reservationId}/check-in`);

    if (resp.data?.success) {
      showTodayReservationsModal.value = false;
      successMsg.value = resp.data.message;
      await loadTables();
      const tableObj = tables.value.find(t => t.tableId === resp.data.data.tableId);
      if (tableObj) {
        openOrderForTable(tableObj);
      }
    }
  } catch (err) {
    alert(err.response?.data?.message || 'Lỗi khi check-in cho khách.');
  }
}

// ==============================================================
// 12. CÁC HÀM TIỆN ÍCH HIỂN THỊ (HELPERS)
// ==============================================================
function getFoodMinPrice(food) {
  if (!food.variants || food.variants.length === 0) return food.minPrice || 0;
  return Math.min(...food.variants.map(v => v.price));
}

function formatCurrency(val) {
  if (!val && val !== 0) return '0 đ';
  return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
}

function formatTime(dt) {
  if (!dt) return '';
  const d = new Date(dt);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function getTableBorderClass(status) {
  switch (status) {
    case 'empty': return 'border-success-subtle';
    case 'serving': return 'border-warning shadow-sm';
    case 'reserved': return 'border-info-subtle';
    case 'cleaning': return 'border-secondary-subtle';
    default: return 'border-light';
  }
}

function getTableBadgeClass(status) {
  switch (status) {
    case 'empty': return 'bg-success text-white';
    case 'serving': return 'bg-warning text-dark fw-bold';
    case 'reserved': return 'bg-info text-white';
    case 'cleaning': return 'bg-secondary text-white';
    default: return 'bg-light text-dark';
  }
}

function getTableTextClass(status) {
  switch (status) {
    case 'serving': return 'text-warning-emphasis';
    case 'empty': return 'text-success';
    default: return 'text-dark';
  }
}

function getTableStatusText(status) {
  switch (status) {
    case 'empty': return '🟢 Trống';
    case 'serving': return '🟡 Đang phục vụ';
    case 'reserved': return '🔵 Đã đặt trước';
    case 'cleaning': return '⚪ Đang dọn dẹp';
    default: return status;
  }
}

function getItemStatusBadgeClass(status) {
  switch (status) {
    case 'pending': return 'bg-warning text-dark border border-warning';
    case 'confirmed': return 'bg-primary text-white';
    case 'cooking': return 'bg-info text-white';
    case 'ready': return 'bg-warning text-dark fw-bold border border-warning animate-pulse';
    case 'served': return 'bg-success text-white fw-bold shadow-xs';
    case 'cancelled': return 'bg-danger text-white';
    default: return 'bg-light text-dark';
  }
}

function getItemStatusText(status) {
  switch (status) {
    case 'pending': return '⏳ Chờ gửi bếp';
    case 'confirmed': return '🍳 Bếp đã nhận';
    case 'cooking': return '🔥 Đang nấu';
    case 'ready': return '🛎️ Đã nấu xong (Tại quầy)';
    case 'served': return '✅ Đã phục vụ (Đã ra bàn)';
    case 'cancelled': return '❌ Đã hủy';
    default: return status;
  }
}

function handleLogout() {
  clearSession();
  router.push('/login');
}
</script>

<style scoped>
.staff-order-pos { --staff-ink: #17332e; --staff-muted: #6c7e79; --staff-green: #1d6b57; --staff-line: #dce7e2; background: #edf3f1 !important; color: var(--staff-ink); }
.staff-order-pos > header { background: #122c29 !important; border-bottom: 1px solid rgba(255,255,255,.12); }
.staff-order-pos .card { border-radius: 10px; border-color: var(--staff-line) !important; box-shadow: 0 5px 16px rgba(19, 54, 47, .06) !important; }
.staff-order-pos .card-header, .staff-order-pos .card-footer { background: #fff !important; border-color: var(--staff-line) !important; }
.cursor-pointer { cursor: pointer; }.transition { transition: transform .18s ease, box-shadow .18s ease, border-color .18s ease; }
.table-card { min-height: 192px; overflow: hidden; background: #fff; }
.table-card:hover { transform: translateY(-3px); box-shadow: 0 12px 22px rgba(19, 54, 47, .12) !important; }
.table-card .card-body { display: flex; flex-direction: column; }
.table-card .table-card-title { color: var(--staff-ink); letter-spacing: -.02em; }
.food-item-card { min-height: 126px; background: #fff; }
.food-item-card:hover { transform: translateY(-2px); border-color: var(--staff-green) !important; box-shadow: 0 9px 18px rgba(29, 107, 87, .12) !important; }
.food-item-card h6 { color: var(--staff-ink) !important; }
.staff-order-pos .btn-primary { background: var(--staff-green); border-color: var(--staff-green); }.staff-order-pos .btn-outline-primary { color: var(--staff-green); border-color: #8dbbad; }.staff-order-pos .btn-outline-primary:hover { color: #fff; background: var(--staff-green); border-color: var(--staff-green); }
.staff-order-pos .form-control, .staff-order-pos .form-select { border-color: var(--staff-line); border-radius: 7px; }.staff-order-pos .form-control:focus, .staff-order-pos .form-select:focus { border-color: #79ae9e; box-shadow: 0 0 0 3px rgba(29,107,87,.12); }
.staff-order-pos .list-group-item { border-color: var(--staff-line) !important; }.staff-order-pos .badge { letter-spacing: .01em; }
.staff-order-pos .modal-content { overflow: hidden; border-radius: 12px; }.staff-order-pos .modal-header { border-bottom: 0; }.staff-order-pos .modal-footer { border-color: var(--staff-line) !important; }
.pos-section-label { color: var(--staff-muted); font-size: .68rem; font-weight: 800; letter-spacing: .12em; text-transform: uppercase; }
.pos-sticky-panel { position: sticky; top: 74px; }
.table-card .btn, .food-item-card .btn { min-height: 36px; }
.staff-order-pos .alert { border-radius: 8px; }
.line-clamp-1 {
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
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

/* Định dạng in nhiệt K80 chuyên dụng */
@media print {
  body * {
    visibility: hidden;
  }
  #printableInvoice, #printableInvoice * {
    visibility: visible;
  }
  #printableInvoice {
    position: absolute;
    left: 0;
    top: 0;
    width: 80mm;
    margin: 0;
    padding: 10px 4px;
    background: #ffffff !important;
    color: #000000 !important;
    font-size: 11px;
    box-shadow: none !important;
    border: none !important;
  }
}
@media (max-width: 991.98px) { .pos-sticky-panel { position: static; } .staff-order-pos > header h5 { font-size: .92rem; } }
@media (max-width: 575.98px) { .staff-order-pos > header .container-fluid { align-items: flex-start !important; flex-wrap: wrap; gap: 10px; } .staff-order-pos > header .container-fluid > div:last-child { width: 100%; justify-content: flex-end; flex-wrap: wrap; } .staff-order-pos > header h5 { font-size: .78rem; } .table-card { min-height: 176px; } }
</style>

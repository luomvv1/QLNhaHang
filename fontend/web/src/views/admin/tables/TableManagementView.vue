<template>
  <section class="table-management-view">
    <!-- Header -->
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-start align-items-md-center gap-3 mb-4">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge rounded-pill bg-primary-subtle text-primary border border-primary-subtle px-3 py-1 fw-bold fs-7">
            🪑 QUẢN TRỊ SƠ ĐỒ & BÀN ĂN
          </span>
          <span class="badge rounded-pill bg-light text-muted border px-2 py-1 small">
            {{ tables.length }} bàn · {{ uniqueAreas.length }} khu vực
          </span>
        </div>
        <h3 class="fw-bold mb-1 text-dark d-flex align-items-center gap-2">
          <span>🪑</span> Sơ Đồ & Phân Khu Vực Bàn Ăn
        </h3>
        <p class="text-muted mb-0 small">
          Quản lý phân khu, sức chứa và trạng thái hoạt động / sửa chữa của hệ thống bàn ăn.
        </p>
      </div>

      <div class="d-flex align-items-center gap-2 flex-wrap">
        <!-- Nút Thêm Khu Vực Mới -->
        <button
          class="btn btn-outline-success fw-bold px-3 py-2 rounded-3 shadow-xs d-flex align-items-center gap-1.5"
          @click="openCreateAreaModal"
          title="Tạo thêm một phân khu vực mới cho nhà hàng"
        >
          <span>➕</span>
          <span>Thêm Khu Vực Mới</span>
        </button>

        <!-- Nút Thêm Bàn Mới -->
        <button
          class="btn btn-primary fw-bold px-4 py-2 rounded-3 shadow-sm d-flex align-items-center gap-1.5"
          @click="openCreateModal"
        >
          <span>➕</span>
          <span>Thêm Bàn Ăn Mới</span>
        </button>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- THANH ĐIỀU HƯỚNG PHÂN KHU VỰC (AREA TABS)                       -->
    <!-- ============================================================== -->
    <div class="card border-0 shadow-sm rounded-4 p-2 mb-3 bg-white">
      <div class="d-flex align-items-center justify-content-between flex-wrap gap-2">
        <div class="d-flex align-items-center gap-1.5 area-nav-bar flex-grow-1 overflow-auto py-1">
          <span class="text-muted small fw-bold px-2 text-nowrap d-none d-md-inline">📍 Khu vực:</span>

          <!-- Tab Tất Cả -->
          <button
            class="btn btn-sm area-nav-tab"
            :class="selectedArea === '' ? 'active' : ''"
            @click="selectedArea = ''"
          >
            <span>🏢 Tất cả</span>
            <span class="badge ms-1.5" :class="selectedArea === '' ? 'bg-white text-dark' : 'bg-light text-muted border'">
              {{ tables.length }}
            </span>
          </button>

          <!-- Tab Từng Khu Vực -->
          <button
            v-for="area in uniqueAreas"
            :key="area"
            class="btn btn-sm area-nav-tab"
            :class="selectedArea === area ? 'active' : ''"
            @click="selectedArea = area"
          >
            <span>📍 {{ area }}</span>
            <span class="badge ms-1.5" :class="selectedArea === area ? 'bg-white text-dark' : 'bg-light text-muted border'">
              {{ getTablesCountByArea(area) }}
            </span>
          </button>
        </div>

        <button
          type="button"
          class="btn btn-xs btn-light text-success fw-bold border rounded-pill px-2.5 py-1 text-nowrap flex-shrink-0"
          @click="openCreateAreaModal"
        >
          + Thêm khu vực
        </button>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- THANH TÌM KIẾM & BỘ LỌC TRẠNG THÁI HOẠT ĐỘNG                  -->
    <!-- ============================================================== -->
    <div class="card card-body shadow-sm mb-4 border-0 rounded-4">
      <div class="row g-3 align-items-center">
        <!-- Tìm kiếm -->
        <div class="col-md-5">
          <div class="position-relative">
            <span class="position-absolute top-50 start-0 translate-middle-y ms-3 text-muted">🔍</span>
            <input
              v-model="searchQuery"
              class="form-control ps-5 rounded-3"
              placeholder="Tìm theo tên bàn, mã bàn hoặc khu vực..."
            />
          </div>
        </div>

        <!-- Lọc theo Khu Vực (Dropdown) -->
        <div class="col-md-3">
          <div class="input-group">
            <span class="input-group-text bg-light text-muted small">📍 Khu vực</span>
            <select v-model="selectedArea" class="form-select rounded-end-3">
              <option value="">Tất cả các khu vực</option>
              <option v-for="area in uniqueAreas" :key="area" :value="area">
                {{ area }} ({{ getTablesCountByArea(area) }})
              </option>
            </select>
          </div>
        </div>

        <!-- Lọc theo Trạng Thái Bàn (Hoạt động / Sửa chữa / Ngừng dùng) -->
        <div class="col-md-4">
          <div class="input-group">
            <span class="input-group-text bg-light text-muted small">⚡ Trạng thái</span>
            <select v-model="selectedOpStatus" class="form-select rounded-end-3">
              <option value="">Tất cả trạng thái ({{ tables.length }})</option>
              <option value="active">🟢 Đang hoạt động ({{ countActiveTables }})</option>
              <option value="maintenance">🟡 Đang sửa chữa ({{ countMaintenanceTables }})</option>
              <option value="disabled">🔴 Ngừng sử dụng ({{ countDisabledTables }})</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Quick KPI Status Filter Buttons -->
      <div class="d-flex justify-content-between align-items-center mt-3 pt-3 border-top flex-wrap gap-2">
        <div class="btn-group btn-group-sm">
          <button
            class="btn rounded-start-3"
            :class="selectedOpStatus === '' ? 'btn-primary' : 'btn-outline-primary'"
            @click="selectedOpStatus = ''"
          >
            Tất cả ({{ tables.length }})
          </button>
          <button
            class="btn"
            :class="selectedOpStatus === 'active' ? 'btn-success' : 'btn-outline-success'"
            @click="selectedOpStatus = 'active'"
          >
            🟢 Hoạt động ({{ countActiveTables }})
          </button>
          <button
            class="btn"
            :class="selectedOpStatus === 'maintenance' ? 'btn-warning text-dark' : 'btn-outline-warning text-dark'"
            @click="selectedOpStatus = 'maintenance'"
          >
            🟡 Sửa chữa ({{ countMaintenanceTables }})
          </button>
          <button
            class="btn rounded-end-3"
            :class="selectedOpStatus === 'disabled' ? 'btn-danger' : 'btn-outline-danger'"
            @click="selectedOpStatus = 'disabled'"
          >
            🔴 Ngừng dùng ({{ countDisabledTables }})
          </button>
        </div>

        <span class="text-muted small">
          Đang hiển thị: <strong class="text-primary">{{ filteredTables.length }}</strong> bàn ăn
          <span v-if="selectedArea" class="badge bg-primary-subtle text-primary border border-primary-subtle ms-1">
            📍 {{ selectedArea }}
          </span>
        </span>
      </div>
    </div>

    <!-- Alert Messages -->
    <div v-if="errorMessage" class="alert alert-danger py-2 small shadow-sm rounded-3">{{ errorMessage }}</div>
    <div v-if="successMessage" class="alert alert-success py-2 small shadow-sm rounded-3">{{ successMessage }}</div>

    <!-- ============================================================== -->
    <!-- DANH SÁCH BÀN ĂN DẠNG THẺ (CARD GRID)                          -->
    <!-- ============================================================== -->
    <div class="row g-3">
      <div
        v-for="table in filteredTables"
        :key="table.tableId"
        class="col-sm-6 col-lg-4 col-xl-3"
      >
        <div
          class="card table-card shadow-sm h-100 border-0 border-top border-4"
          :class="[
            getOperationalBorderClass(table),
            {'opacity-60 bg-light': isTableDisabled(table)}
          ]"
        >
          <div class="card-body d-flex flex-column justify-content-between">
            <!-- Header thẻ bàn -->
            <div>
              <div class="d-flex justify-content-between align-items-start mb-2">
                <span class="badge bg-dark font-monospace px-2 py-1 rounded">
                  {{ table.tableCode }}
                </span>
                <span class="badge px-2.5 py-1 rounded-pill" :class="getOperationalBadgeClass(table)">
                  {{ getOperationalText(table) }}
                </span>
              </div>

              <h5 class="fw-bold mb-1 text-dark">{{ table.tableName }}</h5>

              <div class="text-muted small mb-1 d-flex align-items-center gap-1">
                <span>📍 Khu vực:</span>
                <span class="badge bg-light text-primary border fw-semibold">{{ table.areaName || 'Khu Chung' }}</span>
              </div>

              <div class="text-muted small d-flex align-items-center gap-1">
                <span>👥 Sức chứa:</span>
                <strong>{{ table.capacity }} người</strong>
              </div>
            </div>

            <!-- Footer thao tác -->
            <div class="mt-3 pt-2 border-top">
              <div class="d-flex justify-content-between align-items-center">
                <!-- Nút Sửa -->
                <button
                  class="btn btn-sm btn-outline-primary px-3 rounded-3 fw-semibold"
                  @click="openEditModal(table)"
                  title="Chỉnh sửa thông tin bàn hoặc đổi khu vực"
                >
                  ✏️ Sửa
                </button>

                <!-- Thao tác nhanh trạng thái hoạt động -->
                <div class="d-flex gap-1">
                  <!-- Nếu đang ngừng dùng -> nút Mở lại -->
                  <button
                    v-if="isTableDisabled(table)"
                    class="btn btn-sm btn-outline-success px-2.5 rounded-3 fw-semibold"
                    @click="reactivateTable(table.tableId)"
                    title="Mở lại bàn cho khách sử dụng"
                  >
                    ✅ Mở lại bàn
                  </button>

                  <!-- Nếu đang sửa chữa -> nút Hoàn tất sửa -->
                  <button
                    v-else-if="isTableMaintenance(table)"
                    class="btn btn-sm btn-outline-success px-2.5 rounded-3 fw-semibold"
                    @click="setTableActive(table)"
                    title="Đã sửa chữa xong, đưa bàn vào hoạt động"
                  >
                    ✅ Xong sửa
                  </button>

                  <!-- Nếu đang hoạt động bình thường -->
                  <template v-else>
                    <button
                      class="btn btn-sm btn-outline-warning text-dark px-2 rounded-3"
                      @click="setTableMaintenance(table)"
                      title="Báo hỏng / Chuyển sang đang sửa chữa"
                    >
                      🟡 Sửa
                    </button>
                    <button
                      class="btn btn-sm btn-outline-danger px-2 rounded-3"
                      @click="deactivateTable(table.tableId)"
                      title="Tạm ngừng sử dụng bàn này"
                    >
                      ⛔ Ngừng
                    </button>
                  </template>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="!filteredTables.length" class="col-12 text-center text-muted py-5">
        <div class="fs-1 mb-2">🪑</div>
        <h6 class="fw-bold text-dark">Không tìm thấy bàn ăn nào</h6>
        <p class="text-muted small mb-3">Thử đổi bộ lọc hoặc thêm bàn mới vào khu vực này.</p>
        <button class="btn btn-sm btn-primary fw-bold px-3 py-2 rounded-3" @click="openCreateModal">
          ➕ Thêm Bàn Mới Ngay
        </button>
      </div>
    </div>

    <!-- ============================================================== -->
    <!-- MODAL THÊM / CHỈNH SỬA BÀN ĂN (ĐƠN GIẢN, ĐẸP MẮT)             -->
    <!-- ============================================================== -->
    <div v-if="showModal" class="modal-backdrop-custom" @click.self="showModal = false">
      <form class="card p-4 dialog shadow-lg border-0 rounded-4" @submit.prevent="saveTable">
        <div class="d-flex justify-content-between align-items-start mb-3 border-bottom pb-2">
          <div>
            <h5 class="fw-bold m-0 text-primary d-flex align-items-center gap-2">
              <span>{{ isEditing ? '✏️' : '➕' }}</span>
              <span>{{ isEditing ? 'Cập Nhật Bàn Ăn' : (isCreatingNewArea ? 'Thêm Khu Vực & Bàn Ăn Mới' : 'Thêm Bàn Ăn Mới') }}</span>
            </h5>
            <p class="text-muted small mb-0 mt-0.5">
              {{ isEditing ? 'Chỉnh sửa khu vực, tên bàn, sức chứa và trạng thái hoạt động' : 'Khai báo bàn mới vào hệ thống quản lý' }}
            </p>
          </div>
          <button type="button" class="btn-close" @click="showModal = false"></button>
        </div>

        <!-- 1. Tên Bàn & Mã Bàn -->
        <div class="row g-3 mb-3">
          <div class="col-7">
            <label class="form-label fw-semibold small">
              Tên bàn ăn <span class="text-danger">*</span>
            </label>
            <input
              v-model="form.tableName"
              class="form-control rounded-3"
              placeholder="VD: Bàn 01, Bàn VIP..."
              required
            />
          </div>

          <div class="col-5">
            <label class="form-label fw-semibold small">
              Mã bàn <span class="text-muted fw-normal">(Tự sinh)</span>
            </label>
            <input
              v-model="form.tableCode"
              class="form-control font-monospace rounded-3"
              placeholder="TB-01"
            />
          </div>
        </div>

        <!-- 2. KHU VỰC PHÂN BỐ (ĐƠN GIẢN HƠN XÍ NHƯNG VẪN ĐẸP) -->
        <div class="mb-3">
          <div class="d-flex justify-content-between align-items-center mb-1.5">
            <label class="form-label fw-semibold small m-0 text-dark">
              📍 Khu vực bàn <span class="text-danger">*</span>
            </label>
            <!-- Nút bật/tắt nhập khu vực mới -->
            <button
              type="button"
              class="btn btn-link btn-sm p-0 text-decoration-none text-primary fw-semibold small"
              @click="toggleNewAreaMode"
            >
              {{ isCreatingNewArea ? '← Chọn khu vực có sẵn' : '➕ Thêm khu vực mới' }}
            </button>
          </div>

          <!-- Chế độ 1: Chọn nhanh từ các khu vực hiện có (Pills 1 chạm) -->
          <div v-if="!isCreatingNewArea" class="d-flex flex-wrap gap-1.5">
            <button
              v-for="area in uniqueAreas"
              :key="area"
              type="button"
              class="btn btn-sm rounded-pill px-3 py-1 area-pill-btn"
              :class="form.areaName === area ? 'btn-primary text-white shadow-xs fw-bold' : 'btn-light border text-secondary'"
              @click="form.areaName = area"
            >
              📍 {{ area }}
            </button>
          </div>

          <!-- Chế độ 2: Ô nhập tên khu vực mới -->
          <div v-else>
            <div class="input-group input-group-sm">
              <span class="input-group-text bg-white text-muted">Tên khu mới:</span>
              <input
                ref="areaInputRef"
                v-model="form.areaName"
                class="form-control rounded-end-3"
                placeholder="VD: Tầng 3, Sân Thượng, VIP Lounge..."
                required
              />
            </div>
            <small class="text-muted mt-1 d-block" style="font-size: 0.72rem;">
              * Gõ tên khu vực mới và lưu bàn, khu vực này sẽ tự động xuất hiện trong hệ thống.
            </small>
          </div>
        </div>

        <!-- 3. SỨC CHỨA & TRẠNG THÁI BÀN (HOẠT ĐỘNG / SỬA CHỮA / NGỪNG DÙNG) -->
        <div class="row g-3 mb-3">
          <!-- Sức chứa -->
          <div class="col-6">
            <label class="form-label fw-semibold small">Sức chứa (số người) <span class="text-danger">*</span></label>
            <div class="input-group">
              <span class="input-group-text bg-light text-muted">👥</span>
              <input
                v-model.number="form.capacity"
                type="number"
                min="1"
                max="100"
                class="form-control"
                placeholder="4"
                required
              />
              <span class="input-group-text bg-light text-muted small">người</span>
            </div>
            <!-- Quick capacity chips -->
            <div class="d-flex gap-1 mt-1.5 flex-wrap">
              <button
                v-for="c in [2, 4, 6, 8, 10]"
                :key="c"
                type="button"
                class="btn btn-xs py-0 px-2 rounded-pill"
                :class="form.capacity === c ? 'btn-secondary text-white' : 'btn-light border text-muted'"
                @click="form.capacity = c"
                style="font-size: 0.7rem;"
              >
                {{ c }} chỗ
              </button>
            </div>
          </div>

          <!-- Trạng thái bàn: Hoạt động / Sửa chữa / Ngừng dùng -->
          <div class="col-6">
            <label class="form-label fw-semibold small">Trạng thái bàn <span class="text-danger">*</span></label>
            <select v-model="form.operationalStatus" class="form-select rounded-3">
              <option value="active">🟢 Đang hoạt động</option>
              <option value="maintenance">🟡 Đang sửa chữa (Bảo trì)</option>
              <option value="disabled">🔴 Ngừng sử dụng</option>
            </select>
            <small class="text-muted mt-1 d-block" style="font-size: 0.72rem;">
              * Trạng thái vận hành thiết bị của bàn.
            </small>
          </div>
        </div>

        <div class="d-flex justify-content-end gap-2 border-top pt-3">
          <button type="button" class="btn btn-light rounded-3 px-3" @click="showModal = false">Đóng</button>
          <button type="submit" class="btn btn-primary fw-bold px-4 rounded-3 shadow-sm">
            {{ isEditing ? 'Cập Nhật Bàn' : 'Lưu Bàn Ăn' }}
          </button>
        </div>
      </form>
    </div>
  </section>
</template>

<script setup>
import { computed, nextTick, onMounted, ref } from 'vue';
import axios from 'axios';

const api = 'http://localhost:5079/api/admin/table';
const tables = ref([]);
const searchQuery = ref('');
const selectedArea = ref('');
const selectedOpStatus = ref(''); // '' (all), 'active', 'maintenance', 'disabled'

const showModal = ref(false);
const isEditing = ref(false);
const isCreatingNewArea = ref(false);
const areaInputRef = ref(null);
const currentEditingTableStatus = ref('empty');
const errorMessage = ref('');
const successMessage = ref('');

const form = ref({
  tableId: 0,
  tableCode: '',
  tableName: '',
  areaName: '',
  capacity: 4,
  operationalStatus: 'active' // 'active', 'maintenance', 'disabled'
});

async function loadTables() {
  errorMessage.value = '';
  try {
    const res = await axios.get(`${api}/list`);
    tables.value = res.data?.data ?? [];
  } catch (e) {
    errorMessage.value = 'Không thể tải danh sách bàn ăn. Hãy kiểm tra Backend.';
  }
}

// Danh sách các khu vực hiện có (loại bỏ trùng lặp)
const uniqueAreas = computed(() => {
  const areas = tables.value.map(t => t.areaName || 'Khu Chung');
  return [...new Set(areas)].filter(Boolean);
});

// Helper kiểm tra trạng thái hoạt động / sửa / ngừng dùng
function isTableDisabled(table) {
  return !table.isActive || table.tableStatus === 'disabled';
}

function isTableMaintenance(table) {
  return table.isActive && table.tableStatus === 'maintenance';
}

function isTableActive(table) {
  return table.isActive && table.tableStatus !== 'disabled' && table.tableStatus !== 'maintenance';
}

function getTableOperationalStatus(table) {
  if (isTableDisabled(table)) return 'disabled';
  if (isTableMaintenance(table)) return 'maintenance';
  return 'active';
}

// Thống kê đếm số bàn theo trạng thái vận hành
const countActiveTables = computed(() => tables.value.filter(t => isTableActive(t)).length);
const countMaintenanceTables = computed(() => tables.value.filter(t => isTableMaintenance(t)).length);
const countDisabledTables = computed(() => tables.value.filter(t => isTableDisabled(t)).length);

// Lọc danh sách bàn
const filteredTables = computed(() => {
  return tables.value.filter(t => {
    // 1. Lọc theo trạng thái vận hành
    if (selectedOpStatus.value) {
      if (getTableOperationalStatus(t) !== selectedOpStatus.value) {
        return false;
      }
    }
    // 2. Lọc theo khu vực
    if (selectedArea.value && (t.areaName || 'Khu Chung') !== selectedArea.value) {
      return false;
    }
    // 3. Lọc theo tìm kiếm
    if (searchQuery.value) {
      const q = searchQuery.value.toLowerCase();
      const matchCode = t.tableCode?.toLowerCase().includes(q);
      const matchName = t.tableName?.toLowerCase().includes(q);
      const matchArea = (t.areaName ?? '').toLowerCase().includes(q);
      if (!matchCode && !matchName && !matchArea) return false;
    }
    return true;
  });
});

function getTablesCountByArea(area) {
  return tables.value.filter(t => (t.areaName || 'Khu Chung') === area).length;
}

// Chuyển đổi qua lại giữa chọn khu vực có sẵn và nhập mới
function toggleNewAreaMode() {
  isCreatingNewArea.value = !isCreatingNewArea.value;
  if (isCreatingNewArea.value) {
    form.value.areaName = '';
    nextTick(() => {
      areaInputRef.value?.focus();
    });
  } else {
    form.value.areaName = selectedArea.value || (uniqueAreas.value.length > 0 ? uniqueAreas.value[0] : 'Tầng 1 - Trong Nhà');
  }
}

// Mở modal thêm bàn mới
function openCreateModal() {
  isEditing.value = false;
  isCreatingNewArea.value = false;

  const nextNum = tables.value.length + 1;
  const numStr = nextNum < 10 ? '0' + nextNum : `${nextNum}`;

  // Tự động nhận diện tiền tố mã bàn (TB- hay B)
  let prefix = 'TB-';
  if (tables.value.length > 0 && tables.value[0].tableCode) {
    const match = tables.value[0].tableCode.match(/^([A-Za-z]+-?)/);
    if (match) prefix = match[1];
  }

  const defaultArea = selectedArea.value || (uniqueAreas.value.length > 0 ? uniqueAreas.value[0] : 'Tầng 1 - Trong Nhà');

  form.value = {
    tableId: 0,
    tableCode: `${prefix}${numStr}`,
    tableName: `Bàn ${numStr}`,
    areaName: defaultArea,
    capacity: 4,
    operationalStatus: 'active'
  };
  showModal.value = true;
}

// Mở modal thêm khu vực mới
function openCreateAreaModal() {
  isEditing.value = false;
  isCreatingNewArea.value = true;

  const nextNum = tables.value.length + 1;
  const numStr = nextNum < 10 ? '0' + nextNum : `${nextNum}`;
  let prefix = 'TB-';
  if (tables.value.length > 0 && tables.value[0].tableCode) {
    const match = tables.value[0].tableCode.match(/^([A-Za-z]+-?)/);
    if (match) prefix = match[1];
  }

  form.value = {
    tableId: 0,
    tableCode: `${prefix}${numStr}`,
    tableName: `Bàn 01 (${prefix}${numStr})`,
    areaName: '',
    capacity: 4,
    operationalStatus: 'active'
  };
  showModal.value = true;
  nextTick(() => {
    areaInputRef.value?.focus();
  });
}

function openEditModal(table) {
  isEditing.value = true;
  isCreatingNewArea.value = false;
  currentEditingTableStatus.value = table.tableStatus;

  form.value = {
    tableId: table.tableId,
    tableCode: table.tableCode,
    tableName: table.tableName,
    areaName: table.areaName || (uniqueAreas.value.length > 0 ? uniqueAreas.value[0] : 'Tầng 1 - Trong Nhà'),
    capacity: table.capacity || 4,
    operationalStatus: getTableOperationalStatus(table)
  };
  showModal.value = true;
}

async function saveTable() {
  errorMessage.value = '';
  successMessage.value = '';

  if (!form.value.areaName || !form.value.areaName.trim()) {
    errorMessage.value = 'Vui lòng chọn hoặc nhập tên khu vực cho bàn ăn!';
    return;
  }

  // Chuyển đổi operationalStatus sang TableStatus và IsActive chuẩn
  let tableStatus = 'empty';
  let isActive = true;

  if (form.value.operationalStatus === 'disabled') {
    isActive = false;
    tableStatus = 'disabled';
  } else if (form.value.operationalStatus === 'maintenance') {
    isActive = true;
    tableStatus = 'maintenance';
  } else {
    // active
    isActive = true;
    // Giữ nguyên trạng thái live nếu đang phục vụ hoặc chuyển về empty
    tableStatus = (isEditing.value && currentEditingTableStatus.value && currentEditingTableStatus.value !== 'disabled' && currentEditingTableStatus.value !== 'maintenance')
      ? currentEditingTableStatus.value
      : 'empty';
  }

  const payload = {
    tableId: form.value.tableId,
    tableCode: form.value.tableCode,
    tableName: form.value.tableName,
    areaName: form.value.areaName.trim(),
    capacity: form.value.capacity,
    tableStatus,
    isActive
  };

  try {
    if (isEditing.value) {
      await axios.put(`${api}/update`, payload);
      successMessage.value = `Đã cập nhật bàn "${form.value.tableName}" (Khu vực: ${form.value.areaName}) thành công!`;
    } else {
      const res = await axios.post(`${api}/insert`, payload);
      const createdCode = res.data?.tableCode || form.value.tableCode;
      successMessage.value = `Thêm bàn "${form.value.tableName}" (${createdCode}) vào khu vực "${form.value.areaName}" thành công!`;
    }

    showModal.value = false;
    await loadTables();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi lưu bàn ăn.';
  }
}

// Tạm ngừng sử dụng bàn
async function deactivateTable(id) {
  if (!confirm('Bạn có chắc muốn tạm Ngừng Sử Dụng bàn này không?')) return;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.delete(`${api}/delete`, {
      params: { tableId: id }
    });
    successMessage.value = 'Đã chuyển bàn sang trạng thái Ngừng Sử Dụng!';
    await loadTables();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi ngừng sử dụng bàn.';
  }
}

// Mở lại bàn hoạt động
async function reactivateTable(id) {
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await axios.patch(`${api}/reactivate`, null, {
      params: { tableId: id }
    });
    successMessage.value = 'Đã mở lại hoạt động cho bàn ăn thành công!';
    await loadTables();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi mở lại bàn.';
  }
}

// Chuyển nhanh bàn sang trạng thái đang sửa chữa
async function setTableMaintenance(table) {
  errorMessage.value = '';
  successMessage.value = '';

  const payload = {
    tableId: table.tableId,
    tableCode: table.tableCode,
    tableName: table.tableName,
    areaName: table.areaName,
    capacity: table.capacity,
    tableStatus: 'maintenance',
    isActive: true
  };

  try {
    await axios.put(`${api}/update`, payload);
    successMessage.value = `Đã chuyển "${table.tableName}" sang trạng thái Đang Sửa Chữa (Bảo trì)!`;
    await loadTables();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi cập nhật trạng thái sửa chữa.';
  }
}

// Đưa bàn sửa chữa xong về lại hoạt động
async function setTableActive(table) {
  errorMessage.value = '';
  successMessage.value = '';

  const payload = {
    tableId: table.tableId,
    tableCode: table.tableCode,
    tableName: table.tableName,
    areaName: table.areaName,
    capacity: table.capacity,
    tableStatus: 'empty',
    isActive: true
  };

  try {
    await axios.put(`${api}/update`, payload);
    successMessage.value = `Đã hoàn tất sửa chữa, "${table.tableName}" sẵn sàng hoạt động!`;
    await loadTables();
  } catch (e) {
    errorMessage.value = e.response?.data?.message ?? 'Lỗi khi đưa bàn về hoạt động.';
  }
}

// Text & Badge cho trạng thái vận hành của bàn (Hoạt động / Sửa chữa / Ngừng dùng)
function getOperationalText(table) {
  if (isTableDisabled(table)) return '🔴 Ngừng sử dụng';
  if (isTableMaintenance(table)) return '🟡 Đang sửa chữa';
  return '🟢 Đang hoạt động';
}

function getOperationalBadgeClass(table) {
  if (isTableDisabled(table)) return 'bg-danger-subtle text-danger border border-danger-subtle fw-semibold';
  if (isTableMaintenance(table)) return 'bg-warning-subtle text-warning-emphasis border border-warning-subtle fw-semibold';
  return 'bg-success-subtle text-success border border-success-subtle fw-semibold';
}

function getOperationalBorderClass(table) {
  if (isTableDisabled(table)) return 'border-danger';
  if (isTableMaintenance(table)) return 'border-warning';
  return 'border-success';
}

onMounted(loadTables);
</script>

<style scoped>
.table-management-view {
  min-height: calc(100vh - 100px);
}

.table-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  border-radius: 14px;
}
.table-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 0.5rem 1.2rem rgba(0, 0, 0, 0.08) !important;
}

.opacity-60 {
  opacity: 0.65;
}

/* Area Navigation Bar */
.area-nav-bar {
  scrollbar-width: thin;
}
.area-nav-bar::-webkit-scrollbar {
  height: 4px;
}
.area-nav-bar::-webkit-scrollbar-thumb {
  background-color: #cbd5e1;
  border-radius: 4px;
}

.area-nav-tab {
  font-weight: 600;
  font-size: 0.82rem;
  padding: 6px 14px;
  border-radius: 10px;
  color: #475569;
  border: 1px solid transparent;
  white-space: nowrap;
  transition: all 0.15s ease;
  background-color: transparent;
}
.area-nav-tab:hover {
  background-color: #f1f5f9;
  color: #0f172a;
}
.area-nav-tab.active {
  background-color: #0f172a;
  color: #ffffff;
  box-shadow: 0 2px 6px rgba(15, 23, 42, 0.15);
}

/* Quick Area Pill Buttons in Modal */
.area-pill-btn {
  font-size: 0.8rem;
  font-weight: 600;
  transition: all 0.15s ease;
}
.area-pill-btn:hover {
  transform: translateY(-1px);
}

.modal-backdrop-custom {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.55);
  display: grid;
  place-items: center;
  z-index: 1050;
  backdrop-filter: blur(4px);
  padding: 1rem;
}

.dialog {
  width: min(540px, calc(100vw - 2rem));
  max-height: 90vh;
  overflow-y: auto;
}
</style>
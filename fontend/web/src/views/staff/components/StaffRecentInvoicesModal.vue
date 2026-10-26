<template>
  <div v-if="show" class="invoice-history-overlay" @click.self="$emit('close')">
    <div class="invoice-history-dialog">
      <!-- 1. HEADER (Deep Teal & Slate Gradient) -->
      <div class="invoice-history-header">
        <div class="header-left">
          <div class="header-icon-box">
            <svg class="h-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="16" y1="13" x2="8" y2="13"></line>
              <line x1="16" y1="17" x2="8" y2="17"></line>
              <polyline points="10 9 9 9 8 9"></polyline>
            </svg>
          </div>
          <div>
            <div class="header-eyebrow">
              <span class="eyebrow-tag">POS BILLING & AUDIT</span>
              <span class="live-pill">
                <span class="pulse-dot"></span> ĐỐI SOÁT CA
              </span>
            </div>
            <h4 class="header-title mb-0">Lịch Sử Hóa Đơn Gần Đây</h4>
          </div>
        </div>

        <div class="header-actions">
          <button
            type="button"
            class="btn-action-refresh"
            :disabled="loading"
            @click="loadData"
            title="Làm mới danh sách"
          >
            <svg class="action-svg" :class="{ 'spin-anim': loading }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="23 4 23 10 17 10"></polyline>
              <polyline points="1 20 1 14 7 14"></polyline>
              <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path>
            </svg>
            <span class="d-none d-sm-inline">Làm mới</span>
          </button>
          <button
            type="button"
            class="btn-action-close"
            title="Đóng (Esc)"
            @click="$emit('close')"
          >
            <svg class="action-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>
      </div>

      <!-- 2. SUMMARY KPI STRIP (Interactive Click to Filter) -->
      <div class="kpi-summary-strip">
        <div
          class="kpi-card total-card"
          :class="{ active: activeFilter === 'all' }"
          @click="activeFilter = 'all'"
        >
          <div class="kpi-icon-wrap bg-teal-subtle">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
              <line x1="8" y1="21" x2="16" y2="21"></line>
              <line x1="12" y1="17" x2="12" y2="21"></line>
            </svg>
          </div>
          <div class="kpi-content">
            <div class="kpi-label">Tổng hóa đơn</div>
            <div class="kpi-value text-teal">{{ stats.totalInvoices }} <small>phiếu</small></div>
            <div class="kpi-sub">Gần nhất trong ca</div>
          </div>
        </div>

        <div class="kpi-card revenue-card">
          <div class="kpi-icon-wrap bg-emerald-subtle">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="12" y1="1" x2="12" y2="23"></line>
              <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
            </svg>
          </div>
          <div class="kpi-content">
            <div class="kpi-label">Tổng doanh thu</div>
            <div class="kpi-value text-emerald">{{ formatCurrency(stats.totalRevenue) }}</div>
            <div class="kpi-sub">Thực thu sau giảm trừ</div>
          </div>
        </div>

        <div
          class="kpi-card cash-card"
          :class="{ active: activeFilter === 'cash' }"
          @click="activeFilter = 'cash'"
        >
          <div class="kpi-icon-wrap bg-amber-subtle">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="2" y="6" width="20" height="12" rx="2"></rect>
              <circle cx="12" cy="12" r="2"></circle>
              <path d="M6 12h.01M18 12h.01"></path>
            </svg>
          </div>
          <div class="kpi-content">
            <div class="kpi-label">Tiền mặt (Cash)</div>
            <div class="kpi-value text-amber">{{ formatCurrency(stats.cashRevenue) }}</div>
            <div class="kpi-sub">{{ stats.cashCount }} hóa đơn</div>
          </div>
        </div>

        <div
          class="kpi-card qr-card"
          :class="{ active: activeFilter === 'transfer' }"
          @click="activeFilter = 'transfer'"
        >
          <div class="kpi-icon-wrap bg-indigo-subtle">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="3" y="3" width="7" height="7"></rect>
              <rect x="14" y="3" width="7" height="7"></rect>
              <rect x="14" y="14" width="7" height="7"></rect>
              <rect x="3" y="14" width="7" height="7"></rect>
            </svg>
          </div>
          <div class="kpi-content">
            <div class="kpi-label">VietQR Chuyển khoản</div>
            <div class="kpi-value text-indigo">{{ formatCurrency(stats.transferRevenue) }}</div>
            <div class="kpi-sub">{{ stats.transferCount }} hóa đơn</div>
          </div>
        </div>

        <div
          class="kpi-card other-card d-none d-md-flex"
          :class="{ active: activeFilter === 'other' }"
          @click="activeFilter = 'other'"
        >
          <div class="kpi-icon-wrap bg-purple-subtle">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
              <line x1="1" y1="10" x2="23" y2="10"></line>
            </svg>
          </div>
          <div class="kpi-content">
            <div class="kpi-label">Thẻ / Khác</div>
            <div class="kpi-value text-purple">{{ formatCurrency(stats.otherRevenue) }}</div>
            <div class="kpi-sub">{{ stats.otherCount }} hóa đơn</div>
          </div>
        </div>
      </div>

      <!-- 3. SEARCH & FILTER TOOLBAR -->
      <div class="filter-toolbar">
        <div class="search-box">
          <svg class="search-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            class="search-input"
            placeholder="Tìm theo Mã HĐ (#INV...), tên bàn, tên hoặc SĐT khách, thu ngân..."
          />
          <button
            v-if="searchQuery"
            type="button"
            class="search-clear-btn"
            @click="searchQuery = ''"
            title="Xóa tìm kiếm"
          >
            ✕
          </button>
        </div>

        <!-- Filter Chips with Modern Pills -->
        <div class="filter-chips">
          <button
            type="button"
            class="filter-chip"
            :class="{ active: activeFilter === 'all' }"
            @click="activeFilter = 'all'"
          >
            <span>Tất cả</span>
            <span class="chip-count">{{ stats.totalInvoices }}</span>
          </button>

          <button
            type="button"
            class="filter-chip chip-cash"
            :class="{ active: activeFilter === 'cash' }"
            @click="activeFilter = 'cash'"
          >
            <span class="dot-chip bg-emerald"></span>
            <span>Tiền mặt</span>
            <span class="chip-count">{{ stats.cashCount }}</span>
          </button>

          <button
            type="button"
            class="filter-chip chip-transfer"
            :class="{ active: activeFilter === 'transfer' }"
            @click="activeFilter = 'transfer'"
          >
            <span class="dot-chip bg-indigo"></span>
            <span>VietQR</span>
            <span class="chip-count">{{ stats.transferCount }}</span>
          </button>

          <button
            type="button"
            class="filter-chip chip-other"
            :class="{ active: activeFilter === 'other' }"
            @click="activeFilter = 'other'"
          >
            <span class="dot-chip bg-purple"></span>
            <span>Thẻ / Khác</span>
            <span class="chip-count">{{ stats.otherCount }}</span>
          </button>
        </div>
      </div>

      <!-- 4. INVOICES CONTENT -->
      <div class="invoices-content">
        <!-- Loading State with Skeletons -->
        <div v-if="loading" class="state-container py-5 text-center">
          <div class="spinner-border text-teal" role="status">
            <span class="visually-hidden">Đang tải...</span>
          </div>
          <p class="mt-3 text-muted fw-semibold">Đang tải lịch sử hóa đơn gần đây...</p>
        </div>

        <!-- Empty State -->
        <div v-else-if="filteredInvoices.length === 0" class="state-container py-5 text-center">
          <div class="empty-icon-box">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="9" y1="15" x2="15" y2="15"></line>
            </svg>
          </div>
          <h5 class="empty-title mt-3">Không tìm thấy hóa đơn phù hợp</h5>
          <p class="empty-desc text-muted mb-3">
            {{ searchQuery ? 'Không có hóa đơn nào khớp với từ khóa tìm kiếm.' : 'Chưa có hóa đơn nào được lập gần đây trong ca.' }}
          </p>
          <button
            v-if="searchQuery || activeFilter !== 'all'"
            type="button"
            class="btn-reset-filters"
            @click="resetFilters"
          >
            Xóa bộ lọc & Xem tất cả
          </button>
        </div>

        <!-- Data Presentation -->
        <div v-else class="table-container">
          <!-- Desktop / Tablet Table View -->
          <table class="table invoice-table d-none d-lg-table mb-0 align-middle">
            <thead>
              <tr>
                <th class="ps-4" style="width: 140px;">MÃ HÓA ĐƠN</th>
                <th style="width: 150px;">BÀN PHỤC VỤ</th>
                <th style="width: 160px;">THỜI GIAN</th>
                <th>KHÁCH HÀNG</th>
                <th style="width: 150px;">THU NGÂN</th>
                <th style="width: 160px;">PHƯƠNG THỨC</th>
                <th class="text-end" style="width: 160px;">TỔNG TIỀN</th>
                <th class="text-center pe-4" style="width: 130px;">THAO TÁC</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="inv in filteredInvoices"
                :key="inv.invoiceId"
                class="invoice-row"
                @click="$emit('view-invoice', inv.invoiceId)"
              >
                <!-- Mã HĐ -->
                <td class="ps-4">
                  <div class="inv-code-badge">
                    #{{ inv.invoiceCode || inv.invoiceId }}
                  </div>
                </td>

                <!-- Bàn ăn -->
                <td>
                  <div class="table-badge">
                    <svg class="tbl-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="M4 18v3"></path>
                      <path d="M20 18v3"></path>
                      <path d="M4 11h16"></path>
                      <path d="M5 11l1-7h12l1 7"></path>
                    </svg>
                    <span>{{ inv.tableDisplay || 'Bàn ?' }}</span>
                  </div>
                </td>

                <!-- Thời gian -->
                <td>
                  <div class="inv-time-cell">
                    <span class="time-main">{{ formatTimeOnly(inv.issuedAt) }}</span>
                    <span class="date-sub">{{ formatDateOnly(inv.issuedAt) }}</span>
                  </div>
                </td>

                <!-- Khách hàng -->
                <td>
                  <div class="inv-customer-cell">
                    <div class="customer-name">
                      {{ cleanCustomerName(inv.customerName) || 'Khách vãng lai' }}
                    </div>
                    <div v-if="inv.customerPhone" class="customer-phone">
                      {{ inv.customerPhone }}
                    </div>
                  </div>
                </td>

                <!-- Thu ngân -->
                <td>
                  <div class="inv-cashier-cell">
                    <span class="cashier-avatar">
                      {{ (inv.cashierName || 'TN').substring(0, 1).toUpperCase() }}
                    </span>
                    <span class="cashier-name">{{ inv.cashierName || 'Thu ngân' }}</span>
                  </div>
                </td>

                <!-- Phương thức thanh toán -->
                <td>
                  <div class="inv-method-cell">
                    <span class="method-badge" :class="getMethodBadgeClass(inv.paymentMethod)">
                      <span class="method-dot"></span>
                      {{ getMethodLabel(inv.paymentMethod) }}
                    </span>
                    <span v-if="inv.discountAmount > 0" class="discount-pill mt-1" title="Đã áp dụng giảm giá">
                      -{{ formatCurrency(inv.discountAmount) }}
                    </span>
                  </div>
                </td>

                <!-- Tổng tiền -->
                <td class="text-end">
                  <div class="inv-amount-cell">
                    <span class="amount-val">{{ formatCurrency(inv.totalAmount) }}</span>
                    <span v-if="inv.pointsEarned > 0" class="points-earned-pill">
                      +{{ inv.pointsEarned }} điểm
                    </span>
                  </div>
                </td>

                <!-- Thao tác -->
                <td class="text-center pe-4" @click.stop>
                  <button
                    type="button"
                    class="btn-view-print"
                    @click="$emit('view-invoice', inv.invoiceId)"
                    title="Xem đầy đủ hóa đơn và in lại bill K80"
                  >
                    <svg class="print-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="6 9 6 2 18 2 18 9"></polyline>
                      <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
                      <rect x="6" y="14" width="12" height="8"></rect>
                    </svg>
                    <span>Xem bill</span>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Mobile / Small Screen Card Grid View -->
          <div class="cards-grid d-lg-none">
            <div
              v-for="inv in filteredInvoices"
              :key="inv.invoiceId"
              class="invoice-mobile-card"
              :class="getMobileCardMethodClass(inv.paymentMethod)"
              @click="$emit('view-invoice', inv.invoiceId)"
            >
              <div class="card-stripe"></div>
              <div class="card-inner">
                <div class="card-top-row">
                  <span class="inv-code-badge">#{{ inv.invoiceCode || inv.invoiceId }}</span>
                  <span class="table-badge">
                    <svg class="tbl-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M4 18v3M20 18v3M4 11h16M5 11l1-7h12l1 7"></path>
                    </svg>
                    {{ inv.tableDisplay || 'Bàn ?' }}
                  </span>
                  <span class="card-time-badge">{{ formatTimeOnly(inv.issuedAt) }}</span>
                </div>

                <div class="card-mid-row">
                  <div class="card-cust-info">
                    <div class="card-cust-name">{{ inv.customerName || 'Khách vãng lai' }}</div>
                    <div v-if="inv.customerPhone" class="card-cust-phone">{{ inv.customerPhone }}</div>
                  </div>
                  <div class="card-amount-box text-end">
                    <div class="card-amount-val">{{ formatCurrency(inv.totalAmount) }}</div>
                    <div v-if="inv.discountAmount > 0" class="card-discount-val">
                      Giảm -{{ formatCurrency(inv.discountAmount) }}
                    </div>
                  </div>
                </div>

                <div class="card-detail-hint">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <circle cx="12" cy="12" r="9"></circle>
                    <path d="M12 11v5"></path>
                    <path d="M12 8h.01"></path>
                  </svg>
                  <span>Bấm để xem đầy đủ món ăn, giảm giá và thanh toán</span>
                </div>

                <div class="card-bottom-row" @click.stop>
                  <span class="method-badge" :class="getMethodBadgeClass(inv.paymentMethod)">
                    <span class="method-dot"></span>
                    {{ getMethodLabel(inv.paymentMethod) }}
                  </span>
                  <span class="card-cashier-label text-muted small">
                    TN: {{ inv.cashierName || 'Thu ngân' }}
                  </span>
                  <button
                    type="button"
                    class="btn-view-print ms-auto"
                    @click="$emit('view-invoice', inv.invoiceId)"
                  >
                    <svg class="print-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <polyline points="6 9 6 2 18 2 18 9"></polyline>
                      <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
                      <rect x="6" y="14" width="12" height="8"></rect>
                    </svg>
                    <span>Xem bill</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 5. FOOTER SUMMARY -->
      <div class="invoice-history-footer">
        <div class="footer-info">
          <span>Đang hiển thị <strong>{{ filteredInvoices.length }}</strong> / {{ invoices.length }} hóa đơn gần đây</span>
        </div>
        <div class="footer-actions">
          <button type="button" class="btn-close-modal" @click="$emit('close')">
            Đóng cửa sổ
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import axios from 'axios';

const props = defineProps({
  show: Boolean
});

const emit = defineEmits(['close', 'view-invoice']);

const apiBase = import.meta.env.VITE_API_URL || 'http://localhost:5079';
const paymentApiBase = `${apiBase}/api/staff/payment`;

const invoices = ref([]);
const loading = ref(false);
const searchQuery = ref('');
const activeFilter = ref('all'); // 'all' | 'cash' | 'transfer' | 'other'

watch(() => props.show, (val) => {
  if (val) {
    loadData();
  }
});

function handleKeyDown(e) {
  if (props.show && e.key === 'Escape') {
    emit('close');
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeyDown);
});

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyDown);
});

async function loadData() {
  loading.value = true;
  try {
    const res = await axios.get(`${paymentApiBase}/recent-invoices?limit=50`);
    invoices.value = res.data || [];
  } catch (err) {
    console.error('Lỗi tải danh sách hóa đơn:', err);
  } finally {
    loading.value = false;
  }
}

function resetFilters() {
  searchQuery.value = '';
  activeFilter.value = 'all';
}

const stats = computed(() => {
  const list = invoices.value;
  let totalRevenue = 0;
  let cashRevenue = 0;
  let cashCount = 0;
  let transferRevenue = 0;
  let transferCount = 0;
  let otherRevenue = 0;
  let otherCount = 0;

  for (const inv of list) {
    const amount = Number(inv.totalAmount) || 0;
    totalRevenue += amount;
    const method = (inv.paymentMethod || '').toLowerCase();

    if (method === 'cash') {
      cashRevenue += amount;
      cashCount++;
    } else if (method === 'transfer' || method === 'vietqr' || method === 'bank') {
      transferRevenue += amount;
      transferCount++;
    } else {
      otherRevenue += amount;
      otherCount++;
    }
  }

  return {
    totalInvoices: list.length,
    totalRevenue,
    cashRevenue,
    cashCount,
    transferRevenue,
    transferCount,
    otherRevenue,
    otherCount
  };
});

const filteredInvoices = computed(() => {
  let list = invoices.value;

  if (activeFilter.value === 'cash') {
    list = list.filter(inv => (inv.paymentMethod || '').toLowerCase() === 'cash');
  } else if (activeFilter.value === 'transfer') {
    list = list.filter(inv => {
      const m = (inv.paymentMethod || '').toLowerCase();
      return m === 'transfer' || m === 'vietqr' || m === 'bank';
    });
  } else if (activeFilter.value === 'other') {
    list = list.filter(inv => {
      const m = (inv.paymentMethod || '').toLowerCase();
      return m !== 'cash' && m !== 'transfer' && m !== 'vietqr' && m !== 'bank';
    });
  }

  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim().toLowerCase();
    list = list.filter(inv => {
      const idMatch = String(inv.invoiceId || '').toLowerCase().includes(q);
      const codeMatch = String(inv.invoiceCode || '').toLowerCase().includes(q);
      const tableMatch = String(inv.tableDisplay || '').toLowerCase().includes(q);
      const custMatch = String(inv.customerName || '').toLowerCase().includes(q);
      const phoneMatch = String(inv.customerPhone || '').toLowerCase().includes(q);
      const cashierMatch = String(inv.cashierName || '').toLowerCase().includes(q);
      return idMatch || codeMatch || tableMatch || custMatch || phoneMatch || cashierMatch;
    });
  }

  return list;
});

function formatCurrency(val) {
  if (!val && val !== 0) return '0 đ';
  return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
}

function formatTimeOnly(d) {
  if (!d) return '--:--';
  const date = new Date(d);
  return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
}

function formatDateOnly(d) {
  if (!d) return '';
  const date = new Date(d);
  return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function getMethodLabel(method) {
  const m = (method || '').toLowerCase();
  if (m === 'cash') return 'Tiền mặt';
  if (m === 'transfer' || m === 'vietqr' || m === 'bank') return 'VietQR';
  if (m === 'card') return 'Thẻ POS';
  if (m === 'e_wallet') return 'Ví điện tử';
  return method || 'Tiền mặt';
}

function getMethodBadgeClass(method) {
  const m = (method || '').toLowerCase();
  if (m === 'cash') return 'badge-cash';
  if (m === 'transfer' || m === 'vietqr' || m === 'bank') return 'badge-transfer';
  return 'badge-other';
}

function cleanCustomerName(name) {
  if (!name) return '';
  return name.replace(/\s*\((?:Khách\s+)?(?:Mới|Cũ|mới|cũ|New|Old|VIP|Thành viên|Hội viên)\)/gi, '').trim();
}

function getMobileCardMethodClass(method) {
  const m = (method || '').toLowerCase();
  if (m === 'cash') return 'card-stripe-cash';
  if (m === 'transfer' || m === 'vietqr' || m === 'bank') return 'card-stripe-transfer';
  return 'card-stripe-other';
}
</script>

<style scoped>
/* ==========================================================================
   MODERN F&B POS RECENT INVOICES MODAL STYLES (DEEP TEAL & SLATE LIGHT)
   ========================================================================== */

.invoice-history-overlay {
  position: fixed;
  inset: 0;
  z-index: 1080;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(8px);
  animation: modalFadeIn 0.22s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes modalFadeIn {
  from { opacity: 0; transform: scale(0.97); }
  to { opacity: 1; transform: scale(1); }
}

.invoice-history-dialog {
  width: min(1180px, 100%);
  max-height: 92vh;
  display: flex;
  flex-direction: column;
  background: #ffffff;
  border-radius: 20px;
  box-shadow: 0 25px 50px -12px rgba(15, 23, 42, 0.25), 0 0 0 1px rgba(226, 232, 240, 0.8);
  overflow: hidden;
}

/* 1. HEADER (Deep Teal Master Brand) */
.invoice-history-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  background: linear-gradient(135deg, #0f766e 0%, #115e59 100%);
  color: #ffffff;
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.header-icon-box {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.25);
  border-radius: 12px;
  box-shadow: inset 0 1px 1px rgba(255, 255, 255, 0.2);
}

.h-icon {
  width: 22px;
  height: 22px;
  stroke: #ffffff;
}

.header-eyebrow {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 2px;
}

.eyebrow-tag {
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  color: rgba(255, 255, 255, 0.75);
  text-transform: uppercase;
}

.live-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 2px 7px;
  background: rgba(255, 255, 255, 0.2);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 12px;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 800;
  letter-spacing: 0.04em;
}

.pulse-dot {
  width: 6px;
  height: 6px;
  background: #34d399;
  border-radius: 50%;
  box-shadow: 0 0 6px #34d399;
  animation: pulseDot 1.6s infinite;
}

@keyframes pulseDot {
  0% { transform: scale(0.9); opacity: 0.7; }
  50% { transform: scale(1.3); opacity: 1; }
  100% { transform: scale(0.9); opacity: 0.7; }
}

.header-title {
  font-size: 1.18rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  color: #ffffff;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn-action-refresh {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.22);
  border-radius: 10px;
  color: #ffffff;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-action-refresh:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.22);
  border-color: rgba(255, 255, 255, 0.4);
}

.action-svg {
  width: 15px;
  height: 15px;
}

.spin-anim {
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.btn-action-close {
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.2);
  border-radius: 10px;
  color: #ffffff;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-action-close:hover {
  background: rgba(239, 68, 68, 0.35);
  border-color: rgba(239, 68, 68, 0.6);
  color: #ffffff;
}

/* 2. SUMMARY KPI STRIP */
.kpi-summary-strip {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 12px;
  padding: 14px 24px;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
}

.kpi-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  background: #ffffff;
  border: 1.2px solid #e2e8f0;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.18s cubic-bezier(0.16, 1, 0.3, 1);
}

.kpi-card:hover {
  transform: translateY(-2px);
  border-color: #cbd5e1;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05);
}

.kpi-card.active {
  border-color: #0f766e;
  background: #f0fdfa;
  box-shadow: 0 0 0 1px #0f766e, 0 4px 12px rgba(15, 118, 110, 0.08);
}

.kpi-icon-wrap {
  width: 38px;
  height: 38px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
}

.kpi-icon-wrap svg {
  width: 18px;
  height: 18px;
}

.bg-teal-subtle { background: #ccfbf1; color: #0f766e; }
.bg-emerald-subtle { background: #dcfce7; color: #15803d; }
.bg-amber-subtle { background: #fef3c7; color: #b45309; }
.bg-indigo-subtle { background: #e0e7ff; color: #4338ca; }
.bg-purple-subtle { background: #f3e8ff; color: #7e22ce; }

.kpi-content {
  overflow: hidden;
}

.kpi-label {
  font-size: 0.72rem;
  font-weight: 700;
  color: #64748b;
  margin-bottom: 2px;
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.kpi-value {
  font-size: 1.15rem;
  font-weight: 900;
  letter-spacing: -0.02em;
  line-height: 1.2;
}

.kpi-value small {
  font-size: 0.78rem;
  font-weight: 600;
}

.kpi-sub {
  font-size: 0.68rem;
  color: #94a3b8;
  font-weight: 500;
  margin-top: 2px;
}

.text-teal { color: #0f766e; }
.text-emerald { color: #15803d; }
.text-amber { color: #b45309; }
.text-indigo { color: #4338ca; }
.text-purple { color: #7e22ce; }

/* 3. SEARCH & FILTER TOOLBAR */
.filter-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
  padding: 12px 24px;
  background: #ffffff;
  border-bottom: 1px solid #e2e8f0;
}

.search-box {
  position: relative;
  flex: 1;
  min-width: 280px;
  max-width: 460px;
}

.search-svg {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  width: 16px;
  height: 16px;
  stroke: #94a3b8;
  pointer-events: none;
}

.search-input {
  width: 100%;
  height: 40px;
  padding: 6px 36px 6px 38px;
  background: #f8fafc;
  border: 1.2px solid #e2e8f0;
  border-radius: 12px;
  font-size: 0.82rem;
  font-weight: 500;
  color: #172033;
  outline: none;
  transition: all 0.18s ease;
}

.search-input:focus {
  background: #ffffff;
  border-color: #0f766e;
  box-shadow: 0 0 0 3px rgba(15, 118, 110, 0.12);
}

.search-clear-btn {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #cbd5e1;
  border: none;
  border-radius: 50%;
  color: #475569;
  font-size: 0.7rem;
  cursor: pointer;
  transition: background 0.15s ease;
}

.search-clear-btn:hover {
  background: #94a3b8;
  color: #0f172a;
}

.filter-chips {
  display: flex;
  align-items: center;
  gap: 6px;
  overflow-x: auto;
  padding-bottom: 2px;
}

.filter-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  background: #f8fafc;
  border: 1.2px solid #e2e8f0;
  border-radius: 20px;
  color: #475569;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.15s ease;
}

.filter-chip:hover {
  background: #f1f5f9;
  border-color: #cbd5e1;
}

.filter-chip.active {
  background: #0f766e;
  border-color: #0f766e;
  color: #ffffff;
}

.dot-chip {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.bg-emerald { background: #10b981; }
.bg-indigo { background: #6366f1; }
.bg-purple { background: #a855f7; }

.chip-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 1px 6px;
  background: rgba(0, 0, 0, 0.08);
  border-radius: 10px;
  font-size: 0.68rem;
  font-weight: 800;
}

.filter-chip.active .chip-count {
  background: rgba(255, 255, 255, 0.25);
  color: #ffffff;
}

/* 4. CONTENT & TABLE */
.invoices-content {
  flex: 1;
  overflow-y: auto;
  min-height: 280px;
  max-height: calc(92vh - 280px);
}

.table-container {
  width: 100%;
}

.invoice-table {
  font-size: 0.82rem;
  border-collapse: separate;
  border-spacing: 0;
}

.invoice-table thead {
  position: sticky;
  top: 0;
  z-index: 10;
  background: #f8fafc;
  box-shadow: 0 1px 0 #e2e8f0;
}

.invoice-table thead th {
  padding: 12px 14px;
  color: #64748b;
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  white-space: nowrap;
  border-bottom: 1.5px solid #e2e8f0;
}

.invoice-row {
  cursor: pointer;
  transition: all 0.12s ease;
  border-bottom: 1px solid #f1f5f9;
}

.invoice-row:hover {
  background-color: #f8fafc;
}

.invoice-row td {
  padding: 12px 14px;
  border-bottom: 1px solid #f1f5f9;
}

.inv-code-badge {
  display: inline-block;
  padding: 4px 8px;
  background: #f1f5f9;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  color: #0f766e;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 0.76rem;
  font-weight: 800;
  letter-spacing: 0.02em;
}

.table-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 10px;
  background: #f0fdfa;
  border: 1px solid #ccfbf1;
  border-radius: 8px;
  color: #0f766e;
  font-size: 0.78rem;
  font-weight: 800;
  white-space: nowrap;
}

.tbl-icon {
  width: 13px;
  height: 13px;
  stroke: #0f766e;
}

.inv-time-cell {
  display: flex;
  flex-direction: column;
}

.time-main {
  font-weight: 700;
  color: #172033;
  font-size: 0.82rem;
}

.date-sub {
  color: #94a3b8;
  font-size: 0.7rem;
  font-weight: 500;
}

.customer-name {
  font-weight: 700;
  color: #172033;
}

.customer-phone {
  color: #64748b;
  font-size: 0.74rem;
  font-family: monospace;
}

.inv-cashier-cell {
  display: flex;
  align-items: center;
  gap: 7px;
}

.cashier-avatar {
  width: 22px;
  height: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: #e2e8f0;
  color: #475569;
  font-size: 0.68rem;
  font-weight: 800;
}

.cashier-name {
  color: #475569;
  font-weight: 600;
  font-size: 0.78rem;
}

.method-badge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 9px;
  border-radius: 8px;
  font-size: 0.74rem;
  font-weight: 700;
  white-space: nowrap;
}

.method-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
}

.badge-cash {
  background: #dcfce7;
  color: #15803d;
  border: 1px solid #bbf7d0;
}
.badge-cash .method-dot { background: #16a34a; }

.badge-transfer {
  background: #e0e7ff;
  color: #4338ca;
  border: 1px solid #c7d2fe;
}
.badge-transfer .method-dot { background: #4f46e5; }

.badge-other {
  background: #f3e8ff;
  color: #7e22ce;
  border: 1px solid #e9d5ff;
}
.badge-other .method-dot { background: #9333ea; }

.discount-pill {
  display: inline-block;
  font-size: 0.68rem;
  font-weight: 700;
  color: #b91c1c;
}

.amount-val {
  display: block;
  font-size: 0.98rem;
  font-weight: 900;
  color: #0f766e;
  letter-spacing: -0.01em;
}

.points-earned-pill {
  display: inline-block;
  font-size: 0.68rem;
  color: #15803d;
  font-weight: 700;
}

.btn-view-print {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: #ffffff;
  border: 1.2px solid #cbd5e1;
  border-radius: 8px;
  color: #0f172a;
  font-size: 0.76rem;
  font-weight: 800;
  cursor: pointer;
  transition: all 0.15s ease;
}

.print-svg {
  width: 14px;
  height: 14px;
  stroke: #0f766e;
}

.btn-view-print:hover {
  background: #0f766e;
  color: #ffffff;
  border-color: #0f766e;
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(15, 118, 110, 0.2);
}

.btn-view-print:hover .print-svg {
  stroke: #ffffff;
}

/* Empty State */
.empty-icon-box {
  width: 54px;
  height: 54px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(15, 118, 110, 0.08);
  border-radius: 50%;
  color: #0f766e;
}

.empty-icon-box svg {
  width: 26px;
  height: 26px;
}

.empty-title {
  font-size: 1rem;
  font-weight: 800;
  color: #172033;
}

.empty-desc {
  font-size: 0.8rem;
}

.btn-reset-filters {
  padding: 6px 16px;
  background: #f1f5f9;
  border: 1px solid #cbd5e1;
  border-radius: 20px;
  color: #0f766e;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-reset-filters:hover {
  background: #0f766e;
  color: #ffffff;
  border-color: #0f766e;
}

/* Mobile Cards */
.cards-grid {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 14px;
}

.invoice-mobile-card {
  position: relative;
  background: #ffffff;
  border: 1.2px solid #e2e8f0;
  border-radius: 14px;
  cursor: pointer;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.03);
  transition: all 0.15s ease;
}

.invoice-mobile-card:hover {
  border-color: #0f766e;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.07);
}

.card-stripe {
  height: 3.5px;
  width: 100%;
}

.card-stripe-cash .card-stripe { background: #16a34a; }
.card-stripe-transfer .card-stripe { background: #4f46e5; }
.card-stripe-other .card-stripe { background: #9333ea; }

.card-inner {
  padding: 12px 14px;
}

.card-top-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.card-time-badge {
  margin-left: auto;
  font-size: 0.74rem;
  font-weight: 600;
  color: #94a3b8;
}

.card-mid-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 10px;
}

.card-cust-name {
  font-size: 0.85rem;
  font-weight: 800;
  color: #172033;
}

.card-cust-phone {
  font-size: 0.74rem;
  color: #64748b;
  font-family: monospace;
}

.card-amount-val {
  font-size: 1.05rem;
  font-weight: 900;
  color: #0f766e;
}

.card-discount-val {
  font-size: 0.7rem;
  color: #b91c1c;
  font-weight: 700;
}

.card-detail-hint {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 2px 0 10px;
  color: #64748b;
  font-size: 0.72rem;
  line-height: 1.35;
}

.card-detail-hint svg {
  flex: 0 0 auto;
  width: 14px;
  height: 14px;
  color: #0f766e;
}

.card-bottom-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding-top: 10px;
  border-top: 1px dashed #e2e8f0;
}

.btn-view-print:focus-visible,
.btn-action-close:focus-visible,
.btn-action-refresh:focus-visible,
.filter-chip:focus-visible,
.search-clear-btn:focus-visible {
  outline: 3px solid rgba(14, 165, 233, 0.35);
  outline-offset: 2px;
}

/* 5. FOOTER */
.invoice-history-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 24px;
  background: #f8fafc;
  border-top: 1px solid #e2e8f0;
}

.footer-info {
  font-size: 0.78rem;
  color: #64748b;
}

.footer-info strong {
  color: #0f766e;
}

.btn-close-modal {
  padding: 7px 18px;
  background: #ffffff;
  border: 1.2px solid #cbd5e1;
  border-radius: 10px;
  color: #475569;
  font-size: 0.8rem;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-close-modal:hover {
  background: #0f172a;
  color: #ffffff;
  border-color: #0f172a;
}
</style>

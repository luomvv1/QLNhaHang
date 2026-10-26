<template>
  <div class="d-flex min-vh-100 bg-slate-50">
    <!-- Sidebar Navigation -->
    <aside
      id="adminSidebar"
      class="sidebar-wrapper offcanvas-lg offcanvas-start d-flex flex-column text-white shadow-lg"
      tabindex="-1"
      aria-label="Menu quản trị"
    >
      <!-- 1. Brand Logo & Header -->
      <div class="brand-header p-3 px-3 border-bottom border-white border-opacity-10 d-flex align-items-center gap-3">
        <div class="brand-logo-container">
          <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="brand-icon">
            <path d="M12 3c-4.97 0-9 4.03-9 9 0 2.12.74 4.07 1.97 5.61L12 21l7.03-3.39C20.26 16.07 21 14.12 21 12c0-4.97-4.03-9-9-9z" />
            <path d="M12 7v5l3 3" />
          </svg>
        </div>
        <div class="brand-info flex-grow-1 overflow-hidden">
          <div class="d-flex align-items-center gap-2">
            <h6 class="brand-title fw-bold m-0 tracking-wide text-white">HƯƠNG SEN</h6>
          </div>
          <div class="brand-badge-pill mt-1">
            <span class="badge-dot"></span>
            <span>{{ isAdmin ? 'QUẢN TRỊ VIÊN' : 'QUẢN LÝ NHÀ HÀNG' }}</span>
          </div>
        </div>
        <button
          type="button"
          class="btn-close btn-close-white d-lg-none ms-auto"
          data-bs-dismiss="offcanvas"
          data-bs-target="#adminSidebar"
          aria-label="Đóng menu"
        ></button>
      </div>

      <!-- 2. Menu Quick Search Filter -->
      <div class="sidebar-search-box px-3 pt-3 pb-2">
        <div class="search-input-wrapper">
          <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="search-icon">
            <circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" />
          </svg>
          <input
            ref="searchInputRef"
            v-model="searchKeyword"
            type="text"
            class="search-input"
            placeholder="Tìm chức năng... (Ctrl+K)"
            aria-label="Tìm chức năng quản trị"
          />
          <button
            v-if="searchKeyword"
            type="button"
            class="clear-search-btn"
            title="Xóa tìm kiếm"
            @click="searchKeyword = ''"
          >
            ×
          </button>
        </div>
      </div>

      <!-- 3. Navigation Menu Items (Grouped by Business Domain) -->
      <div class="sidebar-menu-scroll flex-grow-1 p-3 pt-1 overflow-auto" @click="closeMobileSidebar">
        <!-- Khi tìm kiếm không có kết quả -->
        <div v-if="filteredGroups.length === 0" class="text-center py-4 text-white-50 small">
          <div class="mb-1">🔍</div>
          <div>Không tìm thấy chức năng "{{ searchKeyword }}"</div>
        </div>

        <!-- Render các nhóm chức năng -->
        <div v-for="(group, groupIndex) in filteredGroups" :key="group.title" class="menu-group mb-3">
          <!-- Tiêu đề nhóm -->
          <div class="menu-group-header d-flex align-items-center justify-content-between px-2 mb-1.5">
            <span class="group-title">{{ group.title }}</span>
            <span class="group-count badge bg-white bg-opacity-10 text-white-50" style="font-size: 0.6rem;">{{ group.items.length }}</span>
          </div>

          <!-- Danh sách menu con trong nhóm -->
          <ul class="nav flex-column gap-1">
            <li v-for="item in group.items" :key="item.to" class="nav-item">
              <router-link
                :to="item.to"
                class="nav-link-custom"
                :exact-active-class="item.exact ? 'nav-active' : ''"
                :active-class="!item.exact ? 'nav-active' : ''"
              >
                <!-- SVG Icon -->
                <span class="nav-icon-wrapper" v-html="item.iconSvg"></span>
                <span class="nav-text flex-grow-1 text-truncate">{{ item.label }}</span>
                <span v-if="item.badge" :class="['badge rounded-pill', item.badgeClass || 'bg-primary']" style="font-size: 0.65rem;">
                  {{ item.badge }}
                </span>
              </router-link>
            </li>
          </ul>
        </div>

        <!-- Nhóm Không Gian Khác (Workspace Quick Switcher) -->
        <div v-if="!searchKeyword" class="menu-group workspace-group mt-3 pt-3 border-top border-white border-opacity-10">
          <div class="menu-group-header px-2 mb-1.5">
            <span class="group-title text-emerald-400">CHUYỂN KHÔNG GIAN</span>
          </div>
          <ul class="nav flex-column gap-1">
            <li class="nav-item">
              <router-link to="/staff" class="nav-link-custom nav-link-workspace">
                <span class="nav-icon-wrapper text-amber-400">
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <rect width="18" height="12" x="3" y="4" rx="2"/><line x1="2" x2="22" y1="20" y2="20"/><line x1="12" x2="12" y1="16" y2="20"/>
                  </svg>
                </span>
                <span class="nav-text flex-grow-1 text-truncate">Giao Diện Phục Vụ (POS)</span>
                <span class="badge bg-amber-500 bg-opacity-20 text-warning" style="font-size: 0.6rem;">Staff</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/chef" class="nav-link-custom nav-link-workspace">
                <span class="nav-icon-wrapper text-teal-400">
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M6 13.8a4.5 4.5 0 1 1 8.8-1.8 3.5 3.5 0 1 1 5.2 4.2V20H4v-3.8a3.5 3.5 0 0 1 2-2.4Z"/><line x1="6" x2="18" y1="20" y2="20"/>
                  </svg>
                </span>
                <span class="nav-text flex-grow-1 text-truncate">Màn Hình Bếp (KDS)</span>
                <span class="badge bg-teal-500 bg-opacity-20 text-info" style="font-size: 0.6rem;">Chef</span>
              </router-link>
            </li>
            <li class="nav-item">
              <router-link to="/customer" class="nav-link-custom nav-link-workspace">
                <span class="nav-icon-wrapper text-rose-400">
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <rect width="14" height="20" x="5" y="2" rx="2"/><path d="M12 18h.01"/>
                  </svg>
                </span>
                <span class="nav-text flex-grow-1 text-truncate">Cổng Thực Khách</span>
                <span class="badge bg-rose-500 bg-opacity-20 text-danger" style="font-size: 0.6rem;">Guest</span>
              </router-link>
            </li>
          </ul>
        </div>
      </div>

      <!-- 4. User Profile & Logout Bottom Section -->
      <div class="sidebar-user-footer p-3 border-top border-white border-opacity-10">
        <div class="d-flex align-items-center justify-content-between gap-2">
          <div class="d-flex align-items-center gap-2 overflow-hidden flex-grow-1">
            <div class="position-relative">
              <div class="user-avatar-circle">
                {{ userInitial }}
              </div>
              <span class="status-indicator-dot" title="Trực tuyến"></span>
            </div>
            <div class="text-truncate">
              <div class="fw-bold text-white small text-truncate" :title="userName">{{ userName }}</div>
              <div class="text-white-50 text-truncate" style="font-size: 0.72rem;">{{ isAdmin ? 'Quản trị viên cấp cao' : 'Quản lý ca trực' }}</div>
            </div>
          </div>
          <button
            class="btn btn-logout-icon"
            @click="handleLogout"
            title="Đăng xuất khỏi hệ thống"
            aria-label="Đăng xuất"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/>
            </svg>
          </button>
        </div>
      </div>
    </aside>

    <!-- Main Workspace Content Area -->
    <div class="admin-workspace flex-grow-1 d-flex flex-column min-vh-100 overflow-hidden">
      <!-- Top Navbar -->
      <header class="admin-topbar bg-white border-bottom px-4 py-2.5 shadow-xs d-flex justify-content-between align-items-center">
        <!-- Left: Mobile Toggle & Page Title Breadcrumb -->
        <div class="d-flex align-items-center gap-3">
          <button
            type="button"
            class="btn btn-light border d-lg-none p-1.5"
            data-bs-toggle="offcanvas"
            data-bs-target="#adminSidebar"
            aria-controls="adminSidebar"
            aria-label="Mở menu quản trị"
            title="Mở menu quản trị"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
          </button>
          
          <div class="d-flex align-items-center gap-2">
            <span class="fs-6 text-muted d-none d-md-inline">Trang Quản Trị</span>
            <span class="text-muted d-none d-md-inline">/</span>
            <span class="fs-5 fw-bold text-dark d-flex align-items-center gap-2">
              <span>{{ currentRouteInfo.title }}</span>
            </span>
          </div>
        </div>

        <!-- Right: Time, Status, Notifier & Account -->
        <div class="d-flex align-items-center gap-3">
          <div class="d-none d-md-flex align-items-center gap-2 text-muted small border-end pe-3">
            <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
            </svg>
            <span class="fw-medium">{{ liveTime }}</span>
          </div>

          <div class="d-flex align-items-center gap-2">
            <span class="badge bg-success-subtle text-success border border-success-subtle px-2.5 py-1.5 small fw-semibold d-none d-xl-inline-flex align-items-center gap-1.5">
              <span class="pulse-dot"></span>
              <span>Hệ Thống Trực Tuyến</span>
            </span>
            <AdminReservationNotifier />
            <button class="btn btn-sm btn-outline-danger border d-none d-sm-inline-flex align-items-center gap-1 px-2.5 py-1.5" @click="handleLogout">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/>
              </svg>
              <span>Đăng xuất</span>
            </button>
          </div>
        </div>
      </header>

      <!-- View Router Injection Area -->
      <main class="admin-content flex-grow-1 p-3 p-md-4 overflow-auto">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import AdminReservationNotifier from '@/components/AdminReservationNotifier.vue';
import { clearSession } from '@/auth/session';

const router = useRouter();
const route = useRoute();

const isAdmin = localStorage.getItem('role') === 'admin';
const userName = ref(localStorage.getItem('fullName') || 'Admin');
const userInitial = computed(() => (userName.value ? userName.value.charAt(0).toUpperCase() : 'A'));

const searchKeyword = ref('');
const searchInputRef = ref(null);
const liveTime = ref('');
let timerId = null;

// ==============================================================
// 1. HỆ THỐNG MENU ĐƯỢC PHÂN NHÓM CHUẨN UI/UX PRO MAX
// ==============================================================
const rawMenuGroups = computed(() => [
  {
    title: 'TỔNG QUAN & BÁO CÁO',
    items: [
      {
        to: '/admin',
        exact: true,
        label: 'Tổng Quan Điều Hành',
        keywords: 'tong quan dashboard kpi bao cao thong ke',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="7" height="9" x="3" y="3" rx="1.5"/><rect width="7" height="5" x="14" y="3" rx="1.5"/><rect width="7" height="9" x="14" y="12" rx="1.5"/><rect width="7" height="5" x="3" y="16" rx="1.5"/></svg>`
      },
      {
        to: '/admin/reports',
        exact: false,
        label: 'Báo Cáo & Doanh Thu',
        keywords: 'bao cao doanh thu loi nhuan hoa don thong ke tai chinh',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 3v18h18"/><path d="m7 14 5-5 4 4 5-7"/></svg>`
      },
      {
        to: '/admin/audit-logs',
        exact: false,
        label: 'Lịch Sử Thao Tác',
        keywords: 'lich su thao tac kiem toan audit log ai lam gi sua xoa duyet he thong uc-i04',
        badge: 'Audit',
        badgeClass: 'bg-indigo-600 text-white',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><path d="m9 15 2 2 4-4"/></svg>`
      },
      {
        to: '/admin/reviews',
        exact: false,
        label: 'Đánh Giá Khách Hàng',
        keywords: 'danh gia feedback review phan hoi sao binh luan',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>`
      }
    ]
  },
  {
    title: 'VẬN HÀNH NHÀ HÀNG',
    items: [
      {
        to: '/admin/tables',
        exact: false,
        label: 'Sơ Đồ Bàn Ăn',
        keywords: 'ban an so do ban phong khu vuc table floor',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 18v3M20 18v3M3 11h18M5 11V6a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v5M8 11v7M16 11v7"/></svg>`
      },
      {
        to: '/admin/reservations',
        exact: false,
        label: 'Đặt Bàn & Lịch Hẹn',
        keywords: 'dat ban reservation booking hen truoc tiep nhan check-in',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="18" height="18" x="3" y="4" rx="2"/><line x1="16" x2="16" y1="2" y2="6"/><line x1="8" x2="8" y1="2" y2="6"/><line x1="3" x2="21" y1="10" y2="10"/><path d="m9 16 2 2 4-4"/></svg>`
      },
      {
        to: '/admin/foods',
        exact: false,
        label: 'Thực Đơn & Món Ăn',
        keywords: 'thuc don mon an food menu gia bien the mon',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 4a8 8 0 0 0-8 8h16a8 8 0 0 0-8-8z"/><path d="M2 16h20"/><path d="M4 20h16M12 2v2"/></svg>`
      },
      {
        to: '/admin/categories',
        exact: false,
        label: 'Danh Mục Món',
        keywords: 'danh muc category phan loai nhom mon',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 8 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"/></svg>`
      }
    ]
  },
  {
    title: 'QUẢN LÝ KHO & BẾP',
    items: [
      {
        to: '/admin/inventory',
        exact: false,
        label: 'Kho & Nguyên Liệu',
        keywords: 'kho nguyen lieu ton kho vat tu stock inventory',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16.5 9.4 7.55 4.24M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.29 7 12 12 20.71 7"/><line x1="12" x2="12" y1="22" y2="12"/></svg>`
      },
      {
        to: '/admin/stock-receipts',
        exact: false,
        label: 'Phiếu Nhập / Xuất Kho',
        keywords: 'phieu kho nhap kho xuat kho stock receipt chung tu',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" x2="8" y1="13" y2="13"/><line x1="16" x2="8" y1="17" y2="17"/><line x1="10" x2="8" y1="9" y2="9"/></svg>`
      },
      {
        to: '/admin/daily-portions',
        exact: false,
        label: 'Điều Phối Suất Ăn',
        keywords: 'dieu phoi suat an portion phan an dinh luong bep',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M2 12h20M4 12v6a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-6M12 4v4M8 6h8"/></svg>`
      },
      {
        to: '/admin/kitchen-reconciliation',
        exact: false,
        label: 'Đối Soát Bếp & Kho',
        keywords: 'doi soat bep kho hao hut chenh lech kiem ke',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m16 3 4 4-4 4"/><path d="M20 7H4"/><path d="m8 21-4-4 4-4"/><path d="M4 17h16"/></svg>`
      },
      {
        to: '/admin/kitchen-decisions',
        exact: false,
        label: 'Lịch Sử Quyết Định Bếp',
        keywords: 'lich su quyet dinh bep chef audit kds huy mon het mon hoàn tác',
        badge: 'Audit',
        badgeClass: 'bg-teal-500 text-white',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 8v4l3 3"/><circle cx="12" cy="12" r="9"/><path d="M6 13.8a4.5 4.5 0 1 1 8.8-1.8 3.5 3.5 0 1 1 5.2 4.2V20H4v-3.8a3.5 3.5 0 0 1 2-2.4Z"/></svg>`
      }
    ]
  },
  {
    title: 'KHÁCH HÀNG & TIẾP THỊ',
    items: [
      {
        to: '/admin/crm-promotions',
        exact: false,
        label: 'Khách Hàng & Ưu Đãi',
        keywords: 'khach hang crm uu dai voucher khuyen mai diem thuong hoi vien',
        iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="8" width="18" height="4" rx="1"/><path d="M12 8v13M19 12v7a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2v-7M7.5 8a2.5 2.5 0 0 1 0-5A4.8 4.8 0 0 1 12 8a4.8 4.8 0 0 1 4.5-5 2.5 2.5 0 0 1 0 5"/></svg>`
      }
    ]
  },
  ...(isAdmin ? [
    {
      title: 'HỆ THỐNG & CẤU HÌNH',
      items: [
        {
          to: '/admin/users',
          exact: false,
          label: 'Tài Khoản & Nhân Viên',
          keywords: 'tai khoan nhan vien user staff phan quyen admin',
          iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>`
        },
        {
          to: '/admin/payment-settings',
          exact: false,
          label: 'Cấu Hình Ngân Hàng',
          keywords: 'ngan hang thanh toan qr vietqr bank config setting',
          iconSvg: `<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="20" height="14" x="2" y="5" rx="2"/><line x1="2" x2="22" y1="10" y2="10"/><path d="M7 15h2M12 15h5"/></svg>`
        }
      ]
    }
  ] : [])
]);

// 2. Lọc menu theo từ khóa tìm kiếm
const filteredGroups = computed(() => {
  const query = searchKeyword.value.trim().toLowerCase();
  if (!query) return rawMenuGroups.value;

  return rawMenuGroups.value
    .map(group => ({
      ...group,
      items: group.items.filter(item =>
        item.label.toLowerCase().includes(query) ||
        (item.keywords && item.keywords.toLowerCase().includes(query))
      )
    }))
    .filter(group => group.items.length > 0);
});

// 3. Tên trang hiện tại hiển thị trên Topbar
const currentRouteInfo = computed(() => {
  const path = route.path;
  const allItems = rawMenuGroups.value.flatMap(g => g.items);
  const match = allItems.find(item => item.to === path) ||
                allItems.find(item => path.startsWith(item.to) && item.to !== '/admin');
  return {
    title: match ? match.label : 'Hệ Thống Quản Trị'
  };
});

function closeMobileSidebar(event) {
  const sidebar = document.getElementById('adminSidebar');
  if (event.target.closest('a') && sidebar?.classList.contains('show')) {
    sidebar.querySelector('[data-bs-dismiss="offcanvas"]')?.click();
  }
}

function updateTime() {
  const now = new Date();
  liveTime.value = now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) +
    ' - ' + now.toLocaleDateString('vi-VN');
}

// Phím tắt Ctrl+K để focus ô tìm kiếm
function handleGlobalKeydown(e) {
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault();
    searchInputRef.value?.focus();
  }
}

onMounted(() => {
  updateTime();
  timerId = setInterval(updateTime, 1000);
  window.addEventListener('keydown', handleGlobalKeydown);
});

onUnmounted(() => {
  if (timerId) clearInterval(timerId);
  window.removeEventListener('keydown', handleGlobalKeydown);
});

function handleLogout() {
  if (confirm('Bạn có chắc chắn muốn đăng xuất khỏi trang quản trị?')) {
    clearSession();
    router.push('/login');
  }
}
</script>

<style scoped>
/* ==============================================================
   SIDEBAR CORE STYLES (UI/UX PRO MAX - LUXURY DARK PALETTE)
   ============================================================== */
.sidebar-wrapper {
  --bs-offcanvas-width: 270px;
  width: 270px;
  min-width: 270px;
  background: linear-gradient(180deg, #0b1120 0%, #0f172a 100%) !important;
  border-right: 1px solid rgba(255, 255, 255, 0.08);
}

.admin-workspace {
  min-width: 0;
}

.admin-topbar {
  gap: 12px;
  background-color: #ffffff;
}

@media (max-width: 991.98px) {
  .sidebar-wrapper {
    height: 100dvh;
  }
}

@media (max-width: 575.98px) {
  .admin-topbar {
    padding: 10px 14px !important;
  }
  .admin-content {
    padding: 14px !important;
  }
}

/* 1. Brand Logo Header */
.brand-header {
  background: rgba(255, 255, 255, 0.02);
}

.brand-logo-container {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: linear-gradient(135deg, rgba(245, 158, 11, 0.25) 0%, rgba(217, 119, 6, 0.15) 100%);
  border: 1px solid rgba(245, 158, 11, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fbbf24;
  box-shadow: 0 4px 12px rgba(245, 158, 11, 0.2);
}

.brand-title {
  font-family: 'Cinzel', 'Playfair Display', serif, system-ui;
  letter-spacing: 0.08em;
  font-size: 1.05rem;
}

.brand-badge-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 2px 8px;
  border-radius: 9999px;
  background: rgba(245, 158, 11, 0.12);
  border: 1px solid rgba(245, 158, 11, 0.3);
  color: #fbbf24;
  font-size: 0.62rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.badge-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background-color: #f59e0b;
  box-shadow: 0 0 6px #f59e0b;
}

/* 2. Search Input */
.sidebar-search-box {
  background: transparent;
}

.search-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 10px;
  color: #64748b;
  pointer-events: none;
}

.search-input {
  width: 100%;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 8px;
  padding: 7px 28px 7px 32px;
  color: #f1f5f9;
  font-size: 0.8rem;
  transition: all 0.2s ease;
}

.search-input:focus {
  outline: none;
  background: rgba(255, 255, 255, 0.09);
  border-color: rgba(99, 102, 241, 0.6);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2);
}

.search-input::placeholder {
  color: #64748b;
  font-size: 0.78rem;
}

.clear-search-btn {
  position: absolute;
  right: 8px;
  background: transparent;
  border: none;
  color: #94a3b8;
  font-size: 1rem;
  line-height: 1;
  cursor: pointer;
  padding: 0;
}

.clear-search-btn:hover {
  color: #ffffff;
}

/* 3. Menu Navigation List */
.sidebar-menu-scroll {
  scrollbar-width: thin;
  scrollbar-color: rgba(255, 255, 255, 0.12) transparent;
}

.sidebar-menu-scroll::-webkit-scrollbar {
  width: 4px;
}

.sidebar-menu-scroll::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.15);
  border-radius: 4px;
}

.menu-group-header {
  margin-top: 10px;
}

.group-title {
  font-size: 0.67rem;
  font-weight: 700;
  letter-spacing: 0.09em;
  color: #64748b;
  text-transform: uppercase;
}

.nav-link-custom {
  position: relative;
  display: flex;
  align-items: center;
  gap: 11px;
  padding: 8px 12px;
  color: #94a3b8;
  text-decoration: none;
  border-radius: 9px;
  font-size: 0.86rem;
  font-weight: 500;
  transition: all 0.18s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
}

.nav-link-custom:hover {
  background-color: rgba(255, 255, 255, 0.07);
  color: #ffffff;
  transform: translateX(2px);
}

.nav-icon-wrapper {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  color: currentColor;
  opacity: 0.85;
  transition: opacity 0.18s ease;
}

.nav-link-custom:hover .nav-icon-wrapper {
  opacity: 1;
}

/* Active State với Left Accent Pill + Indigo Glow */
.nav-active {
  background: linear-gradient(135deg, #4f46e5 0%, #6366f1 100%) !important;
  color: #ffffff !important;
  font-weight: 600 !important;
  box-shadow: 0 4px 14px rgba(79, 70, 229, 0.38);
}

.nav-active::before {
  content: '';
  position: absolute;
  left: 0;
  top: 6px;
  bottom: 6px;
  width: 3.5px;
  border-radius: 0 4px 4px 0;
  background-color: #fbbf24;
}

.nav-active .nav-icon-wrapper {
  opacity: 1;
  color: #ffffff;
}

/* Workspace Shortcut Links */
.nav-link-workspace {
  background: rgba(255, 255, 255, 0.02);
  border: 1px solid rgba(255, 255, 255, 0.04);
}

.nav-link-workspace:hover {
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(255, 255, 255, 0.1);
}

/* 4. User Profile Footer */
.sidebar-user-footer {
  background: rgba(11, 17, 32, 0.7);
  backdrop-filter: blur(8px);
}

.user-avatar-circle {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1 0%, #4338ca 100%);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 0.9rem;
  border: 2px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}

.status-indicator-dot {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #10b981;
  border: 2px solid #0f172a;
}

.btn-logout-icon {
  width: 34px;
  height: 34px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(239, 68, 68, 0.12);
  color: #f87171;
  border: 1px solid rgba(239, 68, 68, 0.25);
  border-radius: 8px;
  transition: all 0.2s ease;
}

.btn-logout-icon:hover {
  background: #ef4444;
  color: #ffffff;
  border-color: #ef4444;
  transform: scale(1.05);
}

/* Topbar live pulse dot */
.pulse-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background-color: #10b981;
  box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7);
  animation: pulse-green 2s infinite;
}

@keyframes pulse-green {
  0% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7);
  }
  70% {
    transform: scale(1);
    box-shadow: 0 0 0 6px rgba(16, 185, 129, 0);
  }
  100% {
    transform: scale(0.95);
    box-shadow: 0 0 0 0 rgba(16, 185, 129, 0);
  }
}
</style>

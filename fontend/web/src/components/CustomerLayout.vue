<template>
  <div class="d-flex flex-column min-vh-100 dola-theme">

    <!-- ===== TOP BAR ===== -->
    <div class="dola-topbar d-none d-md-block py-2 px-3 border-bottom border-dola-gold">
      <div class="container d-flex justify-content-between align-items-center">
        <div class="d-flex gap-4 small text-body-dola">
          <span>🕒 Giờ mở cửa: <strong class="text-gold">10:00 – 22:30</strong> hàng ngày</span>
          <span>📍 123 Đường Ẩm Thực, Quận 1, TP. Hồ Chí Minh</span>
        </div>
        <div class="d-flex gap-3 small text-body-dola align-items-center">
          <span>📞 Hotline: <strong class="text-gold">1900 6868</strong></span>
          <span class="opacity-50">|</span>
          <template v-if="isLoggedIn">
            <span class="text-white-50">Xin chào, <strong class="text-gold">{{ customerName }}</strong></span>
          </template>
          <template v-else>
            <router-link to="/login" class="text-gold text-decoration-none small fw-semibold hover-gold">
              🔑 Đăng Nhập
            </router-link>
            <span class="opacity-50">|</span>
            <router-link to="/login" class="text-white-50 text-decoration-none small hover-gold">
              🔐 Cổng Nhân Viên
            </router-link>
          </template>
        </div>
      </div>
    </div>

    <!-- ===== NAVBAR ===== -->
    <header class="navbar navbar-expand-lg sticky-top dola-navbar shadow-sm">
      <div class="container py-2">

        <!-- Brand -->
        <router-link to="/customer/intro" class="navbar-brand d-flex align-items-center gap-2 text-decoration-none">
          <span class="dola-brand-icon">🪷</span>
          <div>
            <div class="dola-font-title text-gold lh-1" style="font-size: 1.85rem; font-weight: 700;">Hương Sen</div>
            <small class="text-secondary" style="font-size: 0.7rem; letter-spacing: 0.15em; text-transform: uppercase;">Dola Restaurant &amp; Cuisine</small>
          </div>
        </router-link>

        <!-- Toggler -->
        <button class="navbar-toggler border-0 text-gold" type="button" data-bs-toggle="collapse"
          data-bs-target="#customerNav" aria-controls="customerNav" aria-expanded="false">
          <span class="navbar-toggler-icon"></span>
        </button>

        <!-- Nav links & Actions -->
        <div class="collapse navbar-collapse" id="customerNav">
          <ul class="navbar-nav mx-auto gap-1 my-2 my-lg-0">
            <li class="nav-item" v-for="link in navLinks" :key="link.path">
              <router-link :to="link.path" class="nav-link dola-nav-pill" active-class="dola-nav-pill-active">
                <span class="nav-icon">{{ link.icon }}</span>
                <span class="nav-label">{{ link.label }}</span>
              </router-link>
            </li>
          </ul>

          <!-- User / Auth & CTA Buttons -->
          <div class="d-flex align-items-center gap-2 ms-lg-2">
            <!-- ĐÃ ĐĂNG NHẬP: DROPDOWN CHỨC NĂNG KHÁCH HÀNG SANG TRỌNG -->
            <div v-if="isLoggedIn" class="position-relative user-menu-wrapper" ref="userMenuRef">
              <button
                type="button"
                class="btn user-trigger-btn d-flex align-items-center gap-2"
                @click.stop="toggleUserMenu"
                :class="{ active: isUserMenuOpen }"
                aria-label="Tài khoản khách hàng"
              >
                <div class="user-avatar-circle">{{ userInitials }}</div>
                <div class="user-meta d-none d-sm-flex flex-column text-start lh-1">
                  <span class="user-display-name text-truncate">{{ customerName }}</span>
                  <span class="user-badge-text">Thành viên ✨</span>
                </div>
                <span class="user-caret" :class="{ open: isUserMenuOpen }">▾</span>
              </button>

              <!-- Dropdown Menu Box -->
              <transition name="menu-pop">
                <div v-if="isUserMenuOpen" class="user-popover-menu shadow-lg" @click.stop>
                  <div class="popover-header">
                    <div class="d-flex align-items-center gap-3">
                      <div class="popover-avatar">{{ userInitials }}</div>
                      <div class="overflow-hidden">
                        <h6 class="mb-0 text-white fw-bold text-truncate">{{ customerName }}</h6>
                        <small class="text-gold">Thành viên thân thiết Hương Sen</small>
                      </div>
                    </div>
                  </div>

                  <div class="popover-divider"></div>

                  <div class="popover-links">
                    <button type="button" class="popover-link-item" @click="goTo('/customer/account?tab=points')">
                      <span class="link-icon">👤</span>
                      <div class="text-start">
                        <div class="link-title">Thông tin &amp; Điểm thưởng</div>
                        <div class="link-desc">Hồ sơ cá nhân, hạng thẻ thành viên</div>
                      </div>
                      <span class="link-arrow">›</span>
                    </button>

                    <button type="button" class="popover-link-item" @click="goTo('/customer/account?tab=reservations')">
                      <span class="link-icon">🗓</span>
                      <div class="text-start">
                        <div class="link-title">Bàn đã đặt &amp; Lịch hẹn</div>
                        <div class="link-desc">Theo dõi duyệt bàn trực tiếp realtime</div>
                      </div>
                      <span class="link-arrow">›</span>
                    </button>

                    <button type="button" class="popover-link-item" @click="goTo('/customer/account?tab=invoices')">
                      <span class="link-icon">🧾</span>
                      <div class="text-start">
                        <div class="link-title">Hóa đơn đã dùng bữa</div>
                        <div class="link-desc">Xem chi tiết món, bill thanh toán</div>
                      </div>
                      <span class="link-arrow">›</span>
                    </button>

                    <button type="button" class="popover-link-item" @click="goTo('/customer/reservation')">
                      <span class="link-icon">📅</span>
                      <div class="text-start">
                        <div class="link-title">Đặt bàn mới</div>
                        <div class="link-desc">Chọn bàn VIP, tiệc gia đình</div>
                      </div>
                      <span class="link-arrow">›</span>
                    </button>
                  </div>

                  <div class="popover-divider"></div>

                  <div class="popover-footer">
                    <button type="button" class="popover-logout-btn" @click="handleLogout">
                      <span class="logout-icon">🚪</span>
                      <span>Đăng xuất tài khoản</span>
                    </button>
                  </div>
                </div>
              </transition>
            </div>

            <!-- CHƯA ĐĂNG NHẬP -->
            <template v-else>
              <router-link to="/login" class="btn btn-sm dola-btn-outline py-1 px-3 d-inline-flex align-items-center gap-1" style="font-size: 0.85rem;">
                <span>🔑</span>
                <span>Đăng Nhập</span>
              </router-link>
            </template>

            <!-- CTA DUY NHẤT: Đặt Bàn Ngay (Desktop) -->
            <router-link to="/customer/reservation" class="dola-btn-primary ms-1 d-none d-lg-inline-flex">
              <span>📅</span>
              <span>Đặt Bàn Ngay</span>
            </router-link>
          </div>

          <!-- Nút Đặt Bàn hiển thị trên Mobile khi mở menu collapse -->
          <div class="d-lg-none mt-3 pt-3 border-top border-secondary border-opacity-25">
            <router-link to="/customer/reservation" class="dola-btn-primary w-100 justify-content-center">
              <span>📅</span>
              <span>Đặt Bàn Ngay</span>
            </router-link>
          </div>
        </div>
      </div>
    </header>

    <!-- ===== MAIN CONTENT ===== -->
    <main class="flex-grow-1 dola-main-content">
      <router-view v-slot="{ Component }">
        <transition name="fade-page" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </main>

    <!-- ===== FOOTER ===== -->
    <!-- ===== FOOTER ===== -->
    <footer class="dola-footer py-5 mt-auto border-top" style="border-color: var(--dola-hairline) !important; background-color: var(--dola-surface-alt);">
      <div class="container">
        <div class="row g-4">
          <!-- Brand Column -->
          <div class="col-12 col-md-4">
            <div class="d-flex align-items-center gap-2 mb-3">
              <span style="font-size: 2.2rem;">🪷</span>
              <div>
                <div class="dola-font-title" style="color: var(--dola-wine); font-size: 2.1rem; font-weight: 700; line-height: 1;">Hương Sen</div>
                <small style="font-size: 0.72rem; letter-spacing: 0.12em; text-transform: uppercase; color: #8C7F75; font-weight: 700;">Ẩm Thực Đương Đại &amp; Tiệc Sang</small>
              </div>
            </div>
            <p class="small mb-3" style="color: #574A40; line-height: 1.6;">
              Dola Restaurant – Hương Sen kế thừa tinh hoa ẩm thực truyền thống Việt Nam, 
              hòa quyện cùng phong cách phục vụ đẳng cấp trong không gian xanh thanh tao, ấm cúng.
            </p>
            <div class="d-flex gap-2">
              <a href="#" class="dola-social-btn" title="Facebook" aria-label="Facebook">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/></svg>
              </a>
              <a href="#" class="dola-social-btn" title="Instagram" aria-label="Instagram">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2.163c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z"/></svg>
              </a>
              <a href="#" class="dola-social-btn" title="TikTok" aria-label="TikTok">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor"><path d="M12.525.02c1.31-.02 2.61-.01 3.91-.02.08 1.53.63 3.09 1.75 4.17 1.12 1.11 2.7 1.62 4.24 1.79v4.03c-1.44-.05-2.89-.35-4.2-.97-.57-.26-1.1-.59-1.62-.99v9.12c-.03 2.11-.84 4.19-2.3 5.71-1.57 1.64-3.8 2.58-6.09 2.53-2.38-.05-4.66-1.1-6.19-2.92-1.55-1.84-2.27-4.32-1.95-6.72.33-2.4 1.69-4.54 3.73-5.74 1.9-1.12 4.24-1.35 6.32-.61v4.22c-1.04-.37-2.2-.33-3.21.12-.99.44-1.75 1.32-2.02 2.37-.28 1.05-.09 2.2.53 3.08.62.88 1.65 1.43 2.73 1.46 1.08.03 2.13-.48 2.77-1.35.43-.58.68-1.28.69-2v-16.89c.01-.1.01-.2.02-.3z"/></svg>
              </a>
              <a href="#" class="dola-social-btn" title="YouTube" aria-label="YouTube">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor"><path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/></svg>
              </a>
            </div>
          </div>

          <!-- Quick Links -->
          <div class="col-6 col-md-2">
            <div class="fw-bold mb-3 text-uppercase small" style="letter-spacing: 0.08em; color: var(--dola-wine);">Khám Phá</div>
            <ul class="list-unstyled d-flex flex-column gap-2 small">
              <li v-for="link in navLinks" :key="link.path">
                <router-link :to="link.path" class="dola-footer-link">{{ link.label }}</router-link>
              </li>
              <li>
                <router-link to="/customer/reservation" class="dola-footer-link fw-bold" style="color: var(--dola-wine);">📅 Đặt Bàn Ngay</router-link>
              </li>
            </ul>
          </div>

          <!-- Contact info -->
          <div class="col-6 col-md-3">
            <div class="fw-bold mb-3 text-uppercase small" style="letter-spacing: 0.08em; color: var(--dola-wine);">Liên Hệ</div>
            <ul class="list-unstyled d-flex flex-column gap-2 small" style="color: #574A40;">
              <li>📍 123 Đường Ẩm Thực, Quận 1, TP. HCM</li>
              <li>📞 Hotline: <strong style="color: var(--dola-wine); font-weight: 800;">1900 6868</strong></li>
              <li>✉️ info@huongsen-restaurant.vn</li>
              <li>🕒 10:00 – 22:30 (Thứ 2 – Chủ Nhật)</li>
            </ul>
          </div>

          <!-- Service hours & perks -->
          <div class="col-12 col-md-3">
            <div class="fw-bold mb-3 text-uppercase small" style="letter-spacing: 0.08em; color: var(--dola-wine);">Giờ Phục Vụ</div>
            <div class="d-flex flex-column gap-2 small mb-3" style="color: #574A40;">
              <div class="d-flex justify-content-between">
                <span>Thứ 2 – Thứ 6:</span>
                <strong style="color: #1F1A17;">10:00 – 22:30</strong>
              </div>
              <div class="d-flex justify-content-between">
                <span>Thứ 7 – Chủ Nhật:</span>
                <strong style="color: #1F1A17;">09:00 – 23:00</strong>
              </div>
              <div class="d-flex justify-content-between">
                <span>Phòng VIP / Tiệc cưới:</span>
                <strong style="color: var(--dola-wine); font-weight: 700;">Theo yêu cầu</strong>
              </div>
            </div>
            <div class="p-3 rounded-3 text-center" style="background: #FFFFFF; border: 1.5px solid var(--dola-hairline); box-shadow: 0 4px 14px rgba(44,30,20,0.04);">
              <span class="d-block mb-1" style="font-size: 1.25rem;">✨</span>
              <strong class="d-block mb-0.5" style="color: var(--dola-wine); font-size: 0.88rem;">Nhận Đặt Tiệc Sang Trọng</strong>
              <small style="color: #6E6157; font-size: 0.74rem;">Gia đình, sinh nhật &amp; hội nghị cao cấp</small>
            </div>
          </div>
        </div>

        <hr class="mt-4 mb-3" style="border-color: var(--dola-hairline);">
        <div class="d-flex flex-column flex-md-row justify-content-between align-items-center gap-2 small" style="color: #6E6157;">
          <div>© 2026 Nhà Hàng Hương Sen (Dola Restaurant). Bản quyền thuộc về hệ thống quản lý nhà hàng thông minh.</div>
          <div class="d-flex gap-3">
            <router-link to="/customer/intro" class="dola-footer-link">Giới thiệu</router-link>
            <router-link :to="customerAccountPath" class="dola-footer-link">Tra cứu hóa đơn</router-link>
            <router-link to="/login" class="dola-footer-link fw-semibold" style="color: var(--dola-wine);">Đăng nhập nội bộ</router-link>
          </div>
        </div>
      </div>
    </footer>

    <!-- Trợ Lý Ẩm Thực AI Floating Widget -->
    <AiConsultantWidget />
  </div>
</template>

<script setup>
import { clearSession } from '@/auth/session';
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import AiConsultantWidget from './AiConsultantWidget.vue';

const router = useRouter();
const route = useRoute();

const token = ref(localStorage.getItem('token'));
const customerName = ref(localStorage.getItem('fullName') || 'Khách Hàng');
const isLoggedIn = computed(() => !!token.value);
const isUserMenuOpen = ref(false);
const userMenuRef = ref(null);

const userInitials = computed(() => {
  const name = (customerName.value || 'KH').trim();
  const parts = name.split(/\s+/);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
});

function toggleUserMenu() {
  isUserMenuOpen.value = !isUserMenuOpen.value;
}

function closeUserMenu() {
  isUserMenuOpen.value = false;
}

function goTo(path) {
  closeUserMenu();
  router.push(path);
}

function handleClickOutside(event) {
  if (userMenuRef.value && !userMenuRef.value.contains(event.target)) {
    isUserMenuOpen.value = false;
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});

function checkAuth() {
  token.value = localStorage.getItem('token');
  customerName.value = localStorage.getItem('fullName') || 'Khách Hàng';
}

watch(() => route.path, () => {
  checkAuth();
  closeUserMenu();
});

const customerAccountPath = computed(() => token.value ? '/customer/account' : '/customer/profile');

const navLinks = computed(() => [
  { path: '/customer/intro',   icon: '🏠', label: 'Giới Thiệu' },
  { path: '/customer/menu',    icon: '📖', label: 'Thực Đơn' },
  { path: '/customer/area',    icon: '🪑', label: 'Không Gian' },
  { path: '/customer/news',    icon: '📰', label: 'Tin Tức' },
  { path: '/customer/contact', icon: '📞', label: 'Liên Hệ' },
]);

function handleLogout() {
  closeUserMenu();
  if (confirm('Quý khách có chắc chắn muốn đăng xuất khỏi tài khoản?')) {
    clearSession();
    checkAuth();
    router.push('/customer/intro');
  }
}
</script>

<style scoped>
.dola-topbar {
  background-color: var(--dola-surface-alt);
}
.text-body-dola {
  color: var(--dola-body);
}
.hover-gold:hover {
  color: #FFFFFF !important;
}

/* User Capsule Menu */
.user-menu-wrapper {
  user-select: none;
}
.user-trigger-btn {
  background: rgba(214, 156, 82, 0.1);
  border: 1px solid rgba(214, 156, 82, 0.35);
  color: #fff !important;
  border-radius: 9999px;
  padding: 4px 12px 4px 5px;
  transition: all 0.22s ease;
  cursor: pointer;
}
.user-trigger-btn:hover,
.user-trigger-btn.active {
  background: rgba(214, 156, 82, 0.22);
  border-color: var(--dola-primary);
  box-shadow: 0 0 16px rgba(214, 156, 82, 0.35);
}
.user-avatar-circle {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--dola-primary), #8f5319);
  color: #1a1611;
  font-weight: 800;
  font-size: 0.8rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1.5px solid #f6d29b;
}
.user-display-name {
  max-width: 130px;
  font-size: 0.86rem;
  font-weight: 600;
  color: #f7eedf;
}
.user-badge-text {
  font-size: 0.65rem;
  color: var(--dola-primary);
  font-weight: 600;
}
.user-caret {
  font-size: 0.75rem;
  color: var(--dola-primary);
  transition: transform 0.22s ease;
}
.user-caret.open {
  transform: rotate(180deg);
}

/* Luxury Dropdown Popover */
.user-popover-menu {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 300px;
  background: #191612;
  border: 1px solid rgba(214, 156, 82, 0.45);
  border-radius: 16px;
  padding: 10px;
  z-index: 1060;
  box-shadow: 0 20px 45px rgba(0, 0, 0, 0.65), 0 0 1px rgba(214, 156, 82, 0.3);
  backdrop-filter: blur(14px);
}
.popover-header {
  padding: 10px 10px 6px;
}
.popover-avatar {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--dola-primary), #7c4412);
  color: #fff;
  font-weight: 800;
  font-size: 1.05rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid rgba(214, 156, 82, 0.6);
  box-shadow: 0 4px 12px rgba(0,0,0,0.3);
}
.popover-divider {
  height: 1px;
  background: rgba(214, 156, 82, 0.16);
  margin: 8px 6px;
}
.popover-links {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.popover-link-item {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 9px 12px;
  background: transparent;
  border: none;
  border-radius: 10px;
  color: #f3ece4;
  cursor: pointer;
  transition: all 0.18s ease;
  text-decoration: none;
}
.popover-link-item:hover {
  background: rgba(214, 156, 82, 0.14);
  transform: translateX(4px);
  color: #fff;
}
.link-icon {
  font-size: 1.25rem;
  line-height: 1;
}
.link-title {
  font-size: 0.88rem;
  font-weight: 600;
  color: #f7edd8;
}
.link-desc {
  font-size: 0.72rem;
  color: var(--dola-body);
}
.link-arrow {
  margin-left: auto;
  color: rgba(214, 156, 82, 0.5);
  font-size: 1.1rem;
  font-weight: 300;
}
.popover-link-item:hover .link-arrow {
  color: var(--dola-primary);
  transform: translateX(2px);
}
.popover-footer {
  padding: 4px;
}
.popover-logout-btn {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 9px 12px;
  border-radius: 10px;
  background: rgba(239, 68, 68, 0.08);
  border: 1px solid rgba(239, 68, 68, 0.2);
  color: #f87171;
  font-size: 0.86rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.18s ease;
}
.popover-logout-btn:hover {
  background: rgba(239, 68, 68, 0.2);
  border-color: #ef4444;
  color: #fca5a5;
  transform: translateY(-1px);
}
.logout-icon {
  font-size: 1.1rem;
}

/* Transitions */
.menu-pop-enter-active,
.menu-pop-leave-active {
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}
.menu-pop-enter-from {
  opacity: 0;
  transform: translateY(-8px) scale(0.96);
}
.menu-pop-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.98);
}

.dola-topbar {
  background-color: #F4ECE1;
  border-bottom: 1px solid rgba(184, 125, 50, 0.2);
}

.dola-navbar {
  background-color: #FAF7F2;
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid rgba(184, 125, 50, 0.2);
}

.dola-brand-icon {
  font-size: 2rem;
  filter: drop-shadow(0 2px 4px rgba(184, 125, 50, 0.3));
}

.dola-nav-pill {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  border-radius: 9999px;
  color: #3D332B !important;
  font-weight: 600;
  font-size: 0.95rem;
  transition: all 0.2s ease;
}
.dola-nav-pill:hover {
  background: rgba(184, 125, 50, 0.12);
  color: #7A1C28 !important;
}
.dola-nav-pill-active {
  background: #7A1C28 !important;
  color: #FFFFFF !important;
  font-weight: 700;
  box-shadow: 0 4px 12px rgba(122, 28, 40, 0.3);
}

.dola-main-content {
  background-color: var(--dola-canvas);
}

.dola-footer {
  background-color: var(--dola-surface-alt);
}
.dola-footer-link {
  color: #574A40;
  text-decoration: none;
  display: inline-block;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}
.dola-footer-link:hover {
  color: var(--dola-wine);
  transform: translateX(3px);
}

.dola-social-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: #FFFFFF;
  border: 1.5px solid var(--dola-hairline);
  color: #574A40;
  text-decoration: none;
  transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 2px 6px rgba(44, 30, 20, 0.04);
}
.dola-social-btn:hover {
  background: var(--dola-wine);
  color: #FFFFFF !important;
  border-color: var(--dola-wine);
  transform: translateY(-3px);
  box-shadow: 0 6px 16px rgba(122, 28, 40, 0.35);
}

.fade-page-enter-active,
.fade-page-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}
.fade-page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}
.fade-page-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>

<template>
  <div class="manager-notifier position-relative">
    <!-- Notification Bell Trigger Button -->
    <button
      class="notification-trigger"
      type="button"
      :aria-expanded="isOpen"
      aria-label="Trung tâm thông báo quản lý"
      title="Trung tâm thông báo sự kiện nhà hàng"
      @click="togglePanel"
    >
      <span class="bell-icon" :class="{ 'has-unread-shake': unreadCount > 0 }">🔔</span>
      <span v-if="unreadCount > 0" class="notification-count animate-pulse">
        {{ unreadCount > 99 ? '99+' : unreadCount }}
      </span>
    </button>

    <!-- Notification Dropdown Flyout Panel -->
    <transition name="notice-pop">
      <section v-if="isOpen" class="notification-panel shadow-xl" aria-label="Danh sách thông báo quản lý">
        <!-- Panel Header -->
        <header class="panel-header d-flex align-items-center justify-content-between p-3 border-bottom">
          <div>
            <div class="kicker-title">TRUNG TÂM SỰ KIỆN NHÀ HÀNG</div>
            <strong class="fs-6 text-dark d-flex align-items-center gap-1.5">
              <span>Thông Báo Quản Lý</span>
              <span class="badge bg-emerald-100 text-emerald-800 fw-bold px-2 py-0.5 rounded-pill" style="font-size: 0.68rem;">
                Realtime
              </span>
            </strong>
          </div>
          <div class="d-flex align-items-center gap-2">
            <button
              v-if="notifications.length > 0"
              type="button"
              class="btn-mark-all"
              title="Đánh dấu tất cả là đã đọc"
              @click="markAllRead"
            >
              ✓ Đã đọc
            </button>
            <button
              type="button"
              class="btn-refresh-icon"
              title="Tải lại thông báo"
              :disabled="isLoading"
              @click="fetchNotifications"
            >
              <span :class="{ 'spin-loader': isLoading }">🔄</span>
            </button>
          </div>
        </header>

        <!-- Category Tabs Filter -->
        <div class="category-tabs d-flex px-3 pt-2 pb-1 border-bottom bg-slate-50 gap-1 overflow-x-auto">
          <button
            v-for="tab in categoryTabs"
            :key="tab.id"
            type="button"
            class="tab-pill-btn"
            :class="{ active: currentCategory === tab.id }"
            @click="currentCategory = tab.id"
          >
            <span>{{ tab.icon }}</span>
            <span>{{ tab.label }}</span>
            <span v-if="getTabCount(tab.id) > 0" class="tab-badge">
              {{ getTabCount(tab.id) }}
            </span>
          </button>
        </div>

        <!-- Notification List Items -->
        <div class="notification-scroll-container">
          <div v-if="filteredNotifications.length === 0" class="notification-empty py-5 text-center text-muted">
            <div class="empty-icon mb-2">🌿</div>
            <div class="fw-semibold small">Không có thông báo nào</div>
            <small class="text-secondary opacity-75">Các sự kiện từ Bếp và Đặt bàn sẽ xuất hiện tại đây.</small>
          </div>

          <div v-else class="notification-list">
            <router-link
              v-for="notice in filteredNotifications"
              :key="notice.id"
              :to="notice.link || '/admin/kitchen-decisions'"
              :class="['notification-item d-flex gap-3 p-3 text-decoration-none', { unread: !notice.read, [notice.severity]: true }]"
              @click="onNoticeClick(notice)"
            >
              <!-- Icon Indicator -->
              <div class="notice-icon-box flex-shrink-0" :class="notice.severity">
                <span>{{ notice.icon || '🔔' }}</span>
              </div>

              <!-- Notice Details -->
              <div class="flex-grow-1 overflow-hidden">
                <div class="d-flex align-items-center justify-content-between gap-1 mb-1">
                  <strong class="notice-title text-truncate text-dark">{{ notice.title }}</strong>
                  <span v-if="!notice.read" class="unread-dot flex-shrink-0" title="Chưa đọc"></span>
                </div>
                <p class="notice-detail text-secondary mb-1.5">{{ notice.detail }}</p>
                <div class="d-flex align-items-center justify-content-between text-muted" style="font-size: 0.7rem;">
                  <span class="actor-badge">👤 {{ notice.actor || 'Hệ thống' }}</span>
                  <time class="time-stamp">{{ formatTime(notice.createdAt) }}</time>
                </div>
              </div>
            </router-link>
          </div>
        </div>

        <!-- Panel Footer -->
        <footer class="panel-footer p-2.5 bg-slate-50 border-top text-center d-flex justify-content-between align-items-center px-3">
          <div class="d-flex align-items-center gap-2">
            <router-link
              to="/admin/kitchen-decisions"
              class="text-decoration-none small fw-bold text-teal-700 d-flex align-items-center gap-1 hover-underline"
              @click="isOpen = false"
            >
              <span>👨‍🍳 Quyết Định Bếp</span>
            </router-link>
            <span class="text-slate-300">|</span>
            <router-link
              to="/admin/audit-logs"
              class="text-decoration-none small fw-bold text-indigo-700 d-flex align-items-center gap-1 hover-underline"
              @click="isOpen = false"
            >
              <span>📋 Lịch Sử Thao Tác</span>
            </router-link>
          </div>
          <button type="button" class="btn-close-panel" @click="isOpen = false">Đóng</button>
        </footer>
      </section>
    </transition>

    <!-- Floating Live Toast Stack (Góc Màn Hình) -->
    <div class="toast-stack" aria-live="polite" aria-atomic="true">
      <transition-group name="toast-slide">
        <article
          v-for="notice in toasts"
          :key="notice.id"
          class="live-event-toast shadow-lg"
          :class="notice.severity"
          @click="onToastClick(notice)"
        >
          <span class="toast-icon">{{ notice.icon }}</span>
          <div class="toast-body flex-grow-1 overflow-hidden">
            <div class="toast-title fw-bold text-truncate">{{ notice.title }}</div>
            <p class="toast-desc text-truncate mb-0">{{ notice.detail }}</p>
          </div>
          <button type="button" class="btn-dismiss-toast" aria-label="Đóng" @click.stop="dismissToast(notice.id)">
            ×
          </button>
        </article>
      </transition-group>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';
import signalRService from '@/services/signalr';

const router = useRouter();

const isOpen = ref(false);
const isLoading = ref(false);
const notifications = ref([]);
const toasts = ref([]);
const currentCategory = ref('all');
let nextId = 1;

const categoryTabs = [
  { id: 'all', label: 'Tất cả', icon: '⚡' },
  { id: 'kitchen', label: 'Bếp', icon: '🍳' },
  { id: 'reservation', label: 'Đặt bàn', icon: '📅' },
];

const unreadCount = computed(() => notifications.value.filter(n => !n.read).length);

const filteredNotifications = computed(() => {
  if (currentCategory.value === 'all') return notifications.value;
  return notifications.value.filter(n => n.category === currentCategory.value);
});

function getTabCount(catId) {
  if (catId === 'all') return unreadCount.value;
  return notifications.value.filter(n => n.category === catId && !n.read).length;
}

// ÂM THANH THÔNG BÁO TINH TẾ (WEB AUDIO SYNTHESIZER)
function playNotificationChime() {
  try {
    const AudioCtx = window.AudioContext || window.webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;

    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.frequency.setValueAtTime(523.25, now); // C5
    gain1.gain.setValueAtTime(0.18, now);
    gain1.gain.exponentialRampToValueAtTime(0.01, now + 0.25);
    osc1.start(now);
    osc1.stop(now + 0.25);

    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.frequency.setValueAtTime(783.99, now + 0.1); // G5
    gain2.gain.setValueAtTime(0.22, now + 0.1);
    gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.5);
    osc2.start(now + 0.1);
    osc2.stop(now + 0.5);
  } catch (e) {}
}

async function fetchNotifications() {
  isLoading.value = true;
  try {
    const res = await axios.get('http://localhost:5079/api/admin/notifications?limit=35');
    if (Array.isArray(res.data)) {
      notifications.value = res.data.map(item => ({
        ...item,
        read: item.read || false,
        createdAt: item.createdAt || new Date().toISOString()
      }));
    }
  } catch (err) {
    console.error('Không thể tải thông báo quản lý:', err);
  } finally {
    isLoading.value = false;
  }
}

function pushRealtimeNotice(item) {
  const notice = {
    id: `rt-${Date.now()}-${nextId++}`,
    read: false,
    createdAt: new Date().toISOString(),
    severity: 'info',
    ...item
  };

  playNotificationChime();
  notifications.value.unshift(notice);
  if (notifications.value.length > 50) notifications.value = notifications.value.slice(0, 50);

  // Hiển thị Toast góc màn hình
  toasts.value.unshift(notice);
  if (toasts.value.length > 3) toasts.value = toasts.value.slice(0, 3);
  setTimeout(() => dismissToast(notice.id), 8000);
}

function dismissToast(id) {
  toasts.value = toasts.value.filter(t => t.id !== id);
}

function togglePanel() {
  isOpen.value = !isOpen.value;
  if (isOpen.value && notifications.value.length === 0) {
    fetchNotifications();
  }
}

function onNoticeClick(notice) {
  notice.read = true;
  isOpen.value = false;
}

function onToastClick(notice) {
  notice.read = true;
  dismissToast(notice.id);
  if (notice.link) {
    router.push(notice.link);
  } else {
    isOpen.value = true;
  }
}

function markAllRead() {
  notifications.value.forEach(n => { n.read = true; });
}

function formatTime(isoStr) {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  const now = new Date();
  const diffSec = Math.floor((now - d) / 1000);
  if (diffSec < 60) return 'Vừa xong';
  if (diffSec < 3600) return `${Math.floor(diffSec / 60)} phút trước`;
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) + ' · ' + d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
}

// ==============================================================
// XỬ LÝ SỰ KIỆN SIGNALR REALTIME
// ==============================================================
function handleReservationUpdated(payload = {}) {
  const eventType = payload.eventType || 'updated';
  const customer = payload.customerName ? `Khách ${payload.customerName}` : 'Một phiếu đặt bàn';
  let title = 'Đặt bàn';
  let icon = '📅';
  let severity = 'info';

  if (eventType === 'created') {
    title = 'Yêu cầu đặt bàn mới';
    icon = '✨';
    severity = 'success';
  } else if (eventType === 'cancelled') {
    title = 'Khách hủy đặt bàn';
    icon = '↩';
    severity = 'danger';
  } else if (eventType === 'confirmed') {
    title = 'Xác nhận đặt bàn thành công';
    icon = '✓';
    severity = 'success';
  }

  pushRealtimeNotice({
    category: 'reservation',
    icon,
    title,
    detail: `${customer} (${payload.phone || payload.customerPhone || ''}) · ${payload.guestCount || 2} khách`,
    severity,
    link: '/admin/reservations',
    actor: payload.customerName || 'Thực khách'
  });
}

function handleFoodAvailabilityChanged(payload = {}) {
  const isSoldOut = payload.isSoldOut;
  const foodName = payload.foodName || 'Món ăn';
  const reason = payload.reason ? ` · Lý do: ${payload.reason}` : '';

  pushRealtimeNotice({
    category: 'kitchen',
    icon: isSoldOut ? '🛑' : '✅',
    title: isSoldOut ? 'Bếp báo hết món!' : 'Bếp mở bán lại món',
    detail: `${foodName}${payload.variantName ? ` (${payload.variantName})` : ''}${reason}`,
    severity: isSoldOut ? 'warning' : 'success',
    link: '/admin/kitchen-decisions',
    actor: 'Bếp trưởng'
  });
}

function handleOrderCancellation(payload = {}) {
  const foodName = payload.foodName ? `Món '${payload.foodName}'` : `Đơn #${payload.orderId}`;
  const reason = payload.reason ? ` · Lý do: ${payload.reason}` : '';

  pushRealtimeNotice({
    category: 'kitchen',
    icon: '❌',
    title: payload.source === 'kitchen' ? 'Bếp từ chối / Hủy món!' : 'Đơn hàng bị hủy',
    detail: `${foodName} bàn ${payload.tableDisplay || ''}${reason}`,
    severity: 'danger',
    link: '/admin/kitchen-decisions',
    actor: payload.source === 'kitchen' ? 'Quản lý bếp' : 'Thu ngân'
  });
}

function handleManagerNotification(payload = {}) {
  // Chỉ nhận thông báo quản lý thuộc nhóm bếp hoặc đặt bàn
  const cat = payload.category || 'kitchen';
  if (cat !== 'kitchen' && cat !== 'reservation') return;

  pushRealtimeNotice({
    category: cat,
    icon: payload.icon || (cat === 'kitchen' ? '🍳' : '📅'),
    title: payload.title || 'Thông báo quản lý',
    detail: payload.detail || payload.message || '',
    severity: payload.severity || 'info',
    link: payload.link || (cat === 'kitchen' ? '/admin/kitchen-decisions' : '/admin/reservations'),
    actor: payload.actor || 'Hệ thống'
  });
}

onMounted(() => {
  fetchNotifications();
  signalRService.on('ReservationUpdated', handleReservationUpdated);
  signalRService.on('FoodAvailabilityChanged', handleFoodAvailabilityChanged);
  signalRService.on('OrderCancellationUpdated', handleOrderCancellation);
  signalRService.on('ManagerNotification', handleManagerNotification);
});

onUnmounted(() => {
  signalRService.off('ReservationUpdated', handleReservationUpdated);
  signalRService.off('FoodAvailabilityChanged', handleFoodAvailabilityChanged);
  signalRService.off('OrderCancellationUpdated', handleOrderCancellation);
  signalRService.off('ManagerNotification', handleManagerNotification);
});
</script>

<style scoped>
.manager-notifier {
  position: relative;
}

.notification-trigger {
  position: relative;
  width: 40px;
  height: 40px;
  display: grid;
  place-items: center;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  background: #ffffff;
  color: #0f766e;
  font-size: 1.15rem;
  cursor: pointer;
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  box-shadow: 0 1px 2px rgba(0,0,0,0.04);
}

.notification-trigger:hover {
  transform: translateY(-1px);
  border-color: #0f766e;
  background: #f0fdfa;
}

.has-unread-shake {
  display: inline-block;
  animation: gentle-shake 2.5s infinite;
}

@keyframes gentle-shake {
  0%, 100% { transform: rotate(0); }
  10%, 30% { transform: rotate(-10deg); }
  20%, 40% { transform: rotate(10deg); }
  50% { transform: rotate(0); }
}

.notification-count {
  position: absolute;
  top: -6px;
  right: -6px;
  min-width: 20px;
  height: 20px;
  display: grid;
  place-items: center;
  padding: 0 5px;
  border: 2px solid #ffffff;
  border-radius: 10px;
  background: #ef4444;
  color: #ffffff;
  font-size: 0.65rem;
  font-weight: 800;
  box-shadow: 0 2px 4px rgba(239, 68, 68, 0.4);
}

/* Panel Styles */
.notification-panel {
  position: absolute;
  z-index: 1060;
  top: calc(100% + 10px);
  right: 0;
  width: min(420px, calc(100vw - 24px));
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  background: #ffffff;
  box-shadow: 0 20px 45px -10px rgba(15, 23, 42, 0.2);
  overflow: hidden;
}

.kicker-title {
  font-size: 0.62rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  color: #0f766e;
}

.btn-mark-all {
  border: 0;
  background: #f0fdfa;
  color: #0f766e;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 4px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-mark-all:hover {
  background: #ccfbf1;
}

.btn-refresh-icon {
  border: 0;
  background: transparent;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 6px;
  font-size: 0.85rem;
}

.spin-loader {
  display: inline-block;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Category Tabs */
.category-tabs {
  scrollbar-width: none;
}
.category-tabs::-webkit-scrollbar {
  display: none;
}

.tab-pill-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: 1px solid transparent;
  background: transparent;
  padding: 4px 9px;
  border-radius: 20px;
  font-size: 0.74rem;
  font-weight: 600;
  color: #64748b;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.15s ease;
}

.tab-pill-btn:hover {
  background: #e2e8f0;
  color: #1e293b;
}

.tab-pill-btn.active {
  background: #0f766e;
  color: #ffffff;
}

.tab-badge {
  background: #ef4444;
  color: #fff;
  border-radius: 10px;
  font-size: 0.62rem;
  padding: 1px 5px;
  font-weight: 800;
}

/* Notification Items */
.notification-scroll-container {
  max-height: 380px;
  overflow-y: auto;
}

.notification-item {
  color: inherit;
  border-bottom: 1px solid #f1f5f9;
  transition: background 0.15s ease;
}

.notification-item:hover {
  background: #f8fafc;
}

.notification-item.unread {
  background: #f0fdfa;
  border-left: 3.5px solid #0f766e;
}

.notice-icon-box {
  width: 36px;
  height: 36px;
  display: grid;
  place-items: center;
  border-radius: 10px;
  font-size: 1rem;
  background: #f1f5f9;
}

.notice-icon-box.success { background: #dcfce7; }
.notice-icon-box.warning { background: #fef3c7; }
.notice-icon-box.danger { background: #fee2e2; }

.notice-title {
  font-size: 0.84rem;
  line-height: 1.3;
}

.notice-detail {
  font-size: 0.77rem;
  line-height: 1.4;
}

.unread-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #0f766e;
}

.actor-badge {
  background: #f1f5f9;
  padding: 1px 6px;
  border-radius: 4px;
  font-weight: 600;
}

.btn-close-panel {
  border: 0;
  background: transparent;
  color: #94a3b8;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
}

.btn-close-panel:hover {
  color: #334155;
}

/* Toast Live Floating Stack */
.toast-stack {
  position: fixed;
  z-index: 1090;
  top: 78px;
  right: 20px;
  width: min(380px, calc(100vw - 32px));
  display: grid;
  gap: 10px;
  pointer-events: none;
}

.live-event-toast {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  border-radius: 12px;
  background: #0f172a;
  color: #ffffff;
  pointer-events: auto;
  cursor: pointer;
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 15px 35px rgba(0, 0, 0, 0.35);
  transition: transform 0.2s ease;
}

.live-event-toast:hover {
  transform: translateY(-2px);
}

.live-event-toast.success { border-left: 4px solid #10b981; }
.live-event-toast.warning { border-left: 4px solid #f59e0b; }
.live-event-toast.danger { border-left: 4px solid #ef4444; }

.toast-icon {
  font-size: 1.25rem;
  flex-shrink: 0;
}

.toast-title {
  font-size: 0.82rem;
  color: #ffffff;
}

.toast-desc {
  font-size: 0.74rem;
  color: #cbd5e1;
}

.btn-dismiss-toast {
  border: 0;
  background: transparent;
  color: #94a3b8;
  font-size: 1.25rem;
  line-height: 1;
  padding: 0 4px;
  cursor: pointer;
}

.btn-dismiss-toast:hover {
  color: #ffffff;
}

/* Animations */
.notice-pop-enter-active, .notice-pop-leave-active,
.toast-slide-enter-active, .toast-slide-leave-active {
  transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.notice-pop-enter-from, .notice-pop-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.98);
}

.toast-slide-enter-from, .toast-slide-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>

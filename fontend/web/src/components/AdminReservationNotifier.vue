<template>
  <div class="reservation-notifier">
    <button
      class="notification-trigger"
      type="button"
      :aria-expanded="isOpen"
      aria-label="Thông báo đặt bàn"
      @click="isOpen = !isOpen"
    >
      <span aria-hidden="true">🔔</span>
      <span v-if="unreadCount" class="notification-count">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
    </button>

    <transition name="notice-pop">
      <section v-if="isOpen" class="notification-panel" aria-label="Danh sách thông báo đặt bàn">
        <header>
          <div>
            <span>ĐỒNG BỘ TRỰC TIẾP</span>
            <strong>Thông báo đặt bàn</strong>
          </div>
          <button v-if="notifications.length" type="button" class="mark-read" @click="markAllRead">Đã đọc</button>
        </header>

        <div v-if="!notifications.length" class="notification-empty">
          Chưa có thông báo mới.
        </div>
        <div v-else class="notification-list">
          <router-link
            v-for="notice in notifications"
            :key="notice.id"
            to="/admin/reservations"
            :class="['notification-item', { unread: !notice.read }]"
            @click="notice.read = true; isOpen = false"
          >
            <span class="notification-icon">{{ notice.icon }}</span>
            <span>
              <strong>{{ notice.title }}</strong>
              <small>{{ notice.detail }}</small>
              <time>{{ notice.time }}</time>
            </span>
          </router-link>
        </div>
      </section>
    </transition>

    <div class="toast-stack" aria-live="polite" aria-atomic="true">
      <transition-group name="toast-slide">
        <article v-for="notice in toasts" :key="notice.id" class="reservation-toast" @click="isOpen = true">
          <span class="toast-mark">{{ notice.icon }}</span>
          <div>
            <strong>{{ notice.title }}</strong>
            <p>{{ notice.detail }}</p>
          </div>
          <button type="button" aria-label="Đóng thông báo" @click.stop="dismissToast(notice.id)">×</button>
        </article>
      </transition-group>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue';
import signalRService from '@/services/signalr';

const notifications = ref([]);
const toasts = ref([]);
const isOpen = ref(false);
let nextId = 1;

const unreadCount = computed(() => notifications.value.filter(item => !item.read).length);

function describeReservationEvent(payload = {}) {
  const eventType = payload.eventType || 'updated';
  const customer = payload.customerName ? `Khách ${payload.customerName}` : 'Một phiếu đặt bàn';

  if (eventType === 'created') {
    const guests = payload.guestCount ? ` · ${payload.guestCount} khách` : '';
    const date = payload.reservationDate ? ` · ${payload.reservationDate}` : '';
    return { icon: '📅', title: 'Yêu cầu đặt bàn mới', detail: `${customer}${guests}${date}` };
  }
  if (eventType === 'cancelled') return { icon: '↩', title: 'Khách đã hủy đặt bàn', detail: `Phiếu #${payload.reservationId || 'mới cập nhật'}` };
  if (eventType === 'confirmed') return { icon: '✓', title: 'Phiếu đặt bàn đã được xác nhận', detail: `Phiếu #${payload.reservationId || 'mới cập nhật'}` };
  if (eventType === 'no_show') return { icon: '!', title: 'Khách không đến theo lịch hẹn', detail: `Phiếu #${payload.reservationId || 'mới cập nhật'}` };
  return { icon: '•', title: 'Trạng thái đặt bàn đã thay đổi', detail: `Phiếu #${payload.reservationId || 'mới cập nhật'}` };
}

function handleReservationUpdated(payload) {
  const content = describeReservationEvent(payload);
  const notice = {
    id: `${Date.now()}-${nextId++}`,
    ...content,
    time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
    read: false,
  };

  notifications.value.unshift(notice);
  notifications.value = notifications.value.slice(0, 12);
  toasts.value.unshift(notice);
  toasts.value = toasts.value.slice(0, 3);
  window.setTimeout(() => dismissToast(notice.id), 7000);
}

function dismissToast(id) {
  toasts.value = toasts.value.filter(item => item.id !== id);
}

function markAllRead() {
  notifications.value.forEach(item => { item.read = true; });
}

onMounted(() => {
  signalRService.on('ReservationUpdated', handleReservationUpdated);
});

onUnmounted(() => {
  signalRService.off('ReservationUpdated', handleReservationUpdated);
});
</script>

<style scoped>
.reservation-notifier { position: relative; }
.notification-trigger { position: relative; width: 38px; height: 38px; display: grid; place-items: center; border: 1px solid #dfe8e6; border-radius: 8px; background: #fff; color: #0f766e; font-size: 1rem; cursor: pointer; transition: transform .2s ease, border-color .2s ease; }
.notification-trigger:hover { transform: translateY(-1px); border-color: #0f766e; }
.notification-count { position: absolute; top: -7px; right: -7px; min-width: 18px; height: 18px; display: grid; place-items: center; padding: 0 4px; border: 2px solid #fff; border-radius: 10px; background: #dc5b43; color: #fff; font-size: .62rem; font-weight: 800; }
.notification-panel { position: absolute; z-index: 1050; top: calc(100% + 10px); right: 0; width: min(360px, calc(100vw - 32px)); overflow: hidden; border: 1px solid #dce8e5; border-radius: 8px; background: #fff; box-shadow: 0 18px 45px rgba(15, 53, 47, .18); }
.notification-panel header { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 15px 16px; border-bottom: 1px solid #e7efed; background: #f4fbf9; }
.notification-panel header span { display: block; color: #0f766e; font-size: .62rem; font-weight: 800; letter-spacing: .08em; }
.notification-panel header strong { display: block; color: #18332f; font-size: .94rem; }
.mark-read { border: 0; background: transparent; color: #0f766e; font-size: .76rem; font-weight: 700; cursor: pointer; }
.notification-list { max-height: 360px; overflow: auto; }
.notification-item { display: grid; grid-template-columns: 30px 1fr; gap: 10px; padding: 13px 16px; color: inherit; text-decoration: none; border-bottom: 1px solid #eef3f2; }
.notification-item:hover, .notification-item.unread { background: #f7fbfa; }
.notification-item.unread { border-left: 3px solid #d69c52; }
.notification-icon { width: 30px; height: 30px; display: grid; place-items: center; border-radius: 8px; background: #e7f5f1; font-size: .85rem; }
.notification-item strong, .notification-item small, .notification-item time { display: block; }
.notification-item strong { color: #18332f; font-size: .82rem; }
.notification-item small { margin-top: 3px; color: #647a75; font-size: .73rem; line-height: 1.35; }
.notification-item time { margin-top: 5px; color: #94a6a2; font-size: .67rem; }
.notification-empty { padding: 26px 16px; color: #71827e; text-align: center; font-size: .82rem; }
.toast-stack { position: fixed; z-index: 1080; top: 78px; right: 24px; width: min(360px, calc(100vw - 32px)); display: grid; gap: 10px; pointer-events: none; }
.reservation-toast { display: grid; grid-template-columns: 34px 1fr 20px; gap: 10px; align-items: start; padding: 13px; border: 1px solid rgba(214, 156, 82, .5); border-radius: 8px; background: #123b35; color: #fff; box-shadow: 0 14px 30px rgba(10, 40, 35, .28); pointer-events: auto; cursor: pointer; }
.toast-mark { width: 34px; height: 34px; display: grid; place-items: center; border-radius: 8px; background: rgba(214, 156, 82, .16); }
.reservation-toast strong { display: block; font-size: .82rem; }
.reservation-toast p { margin: 4px 0 0; color: #cae0db; font-size: .73rem; line-height: 1.35; }
.reservation-toast button { border: 0; background: transparent; color: #cae0db; font-size: 1.1rem; line-height: 1; cursor: pointer; }
.notice-pop-enter-active, .notice-pop-leave-active, .toast-slide-enter-active, .toast-slide-leave-active { transition: all .2s ease; }
.notice-pop-enter-from, .notice-pop-leave-to { opacity: 0; transform: translateY(-6px); }
.toast-slide-enter-from, .toast-slide-leave-to { opacity: 0; transform: translateX(20px); }
@media (max-width: 575.98px) { .toast-stack { top: 68px; right: 16px; }.notification-panel { position: fixed; top: 68px; right: 16px; } }
</style>

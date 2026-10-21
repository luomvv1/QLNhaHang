<template>
  <div v-if="show" class="reservation-overlay" @click.self="$emit('close')">
    <div class="reservation-dialog">
      <div class="reservation-header">
        <div>
          <span class="reservation-eyebrow">Bàn đặt hôm nay</span>
          <h5 class="mb-0 fw-bold">Lịch khách đến nhà hàng</h5>
        </div>
        <button type="button" class="reservation-close" title="Đóng" @click="$emit('close')">×</button>
      </div>
      <div class="reservation-body">
        <div class="reservation-legend">
          <span><i class="legend-dot pending"></i> Chờ xác nhận</span>
          <span><i class="legend-dot confirmed"></i> Đã xếp bàn</span>
          <span><i class="legend-dot checked-in"></i> Đã vào bàn</span>
        </div>
        <div v-if="loading" class="reservation-empty">
          <div class="spinner-border text-primary" role="status"></div>
          <div class="mt-2 text-muted">Đang tải danh sách...</div>
        </div>
        <div v-else-if="reservations.length === 0" class="reservation-empty">
          <div class="empty-icon">⌂</div>
          <h6 class="fw-bold">Hôm nay chưa có lịch đặt bàn</h6>
          <p class="text-muted mb-0">Các lịch mới sẽ xuất hiện ở đây.</p>
        </div>
        <div v-else class="reservation-grid">
          <article v-for="res in reservations" :key="res.reservationId" class="reservation-card" :class="`status-${res.reservationStatus}`">
            <div class="reservation-card-top">
              <div class="reservation-time">{{ formatTime(res.reservationDate) }}</div>
              <span class="reservation-status">{{ getStatusText(res.reservationStatus) }}</span>
            </div>
            <h6 class="reservation-name">{{ res.customerName }}</h6>
            <div class="reservation-meta">
              <span>☎ {{ res.customerPhone }}</span>
              <span>👥 {{ res.guestCount }} khách</span>
            </div>
            <p v-if="res.note" class="reservation-note">“{{ res.note }}”</p>
            <div class="reservation-table" :class="{ unassigned: !res.table }">
              <span class="table-marker">▦</span>
              <span v-if="res.table">{{ res.table.tableName }} · {{ res.table.areaName || 'Sảnh chung' }}</span>
              <span v-else>Chưa được phân bàn</span>
            </div>
            <button v-if="res.reservationStatus === 'confirmed' && res.table"
                    class="reservation-action primary"
                    @click="$emit('check-in', res)">
              Đón khách vào bàn <span>→</span>
            </button>
            <button v-else-if="res.reservationStatus === 'pending'" class="reservation-action muted" disabled>
              Chờ quản lý xác nhận
            </button>
            <button v-else-if="res.reservationStatus === 'confirmed'" class="reservation-action muted" disabled>
              Chờ phân bàn
            </button>
            <button v-else-if="res.reservationStatus === 'checked_in'" class="reservation-action done" disabled>
              Đã đón khách
            </button>
          </article>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';
import axios from 'axios';

const props = defineProps({ show: Boolean });
const emit = defineEmits(['close', 'check-in']);

const reservations = ref([]);
const loading = ref(false);

watch(() => props.show, (val) => {
  if (val) loadData();
});

async function loadData() {
  loading.value = true;
  try {
    const today = new Date().toLocaleDateString('en-CA');
    const res = await axios.get('http://localhost:5079/api/admin/reservations', { params: { date: today }});
    reservations.value = res.data.data || [];
  } catch (err) {
    console.error('Failed to load today reservations', err);
  } finally {
    loading.value = false;
  }
}

function formatTime(dt) {
  if (!dt) return '';
  return new Date(dt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}

function getStatusText(status) {
  switch (status) {
    case 'pending': return 'Chờ xếp bàn';
    case 'confirmed': return 'Đã xếp bàn';
    case 'checked_in': return 'Đã vào bàn';
    case 'cancelled': return 'Đã hủy';
    case 'no_show': return 'Vắng mặt';
    default: return status;
  }
}
</script>

<style scoped>
.reservation-overlay { position: fixed; inset: 0; z-index: 1100; display: grid; place-items: center; padding: 20px; background: rgba(8, 23, 22, .72); backdrop-filter: blur(5px); }
.reservation-dialog { width: min(1080px, 100%); max-height: min(820px, 92vh); overflow: hidden; border: 1px solid rgba(255,255,255,.14); border-radius: 14px; background: #f5f7f6; box-shadow: 0 24px 70px rgba(0,0,0,.3); }
.reservation-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 22px 26px; color: #f6f8f7; background: #132d2a; }
.reservation-header h5 { font-size: 1.18rem; letter-spacing: -.01em; }.reservation-eyebrow { display: block; margin-bottom: 4px; color: #9acfc0; font-size: .68rem; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }
.reservation-close { width: 34px; height: 34px; border: 1px solid rgba(255,255,255,.25); border-radius: 8px; color: #fff; background: transparent; font-size: 1.55rem; line-height: 1; cursor: pointer; }.reservation-close:hover { background: rgba(255,255,255,.1); }
.reservation-body { max-height: calc(min(820px, 92vh) - 82px); overflow-y: auto; padding: 20px 26px 26px; }.reservation-legend { display: flex; flex-wrap: wrap; gap: 16px; margin-bottom: 18px; color: #5e706c; font-size: .76rem; font-weight: 700; }.legend-dot { display: inline-block; width: 8px; height: 8px; margin-right: 6px; border-radius: 50%; }.legend-dot.pending { background: #dcaa55; }.legend-dot.confirmed { background: #3d8bd3; }.legend-dot.checked-in { background: #48a978; }
.reservation-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 14px; }.reservation-card { display: flex; min-height: 250px; flex-direction: column; padding: 17px; border: 1px solid #dce5e1; border-top: 3px solid #bdc9c5; border-radius: 10px; background: #fff; box-shadow: 0 5px 16px rgba(16, 45, 40, .06); }.reservation-card.status-pending { border-top-color: #dcaa55; }.reservation-card.status-confirmed { border-top-color: #3d8bd3; }.reservation-card.status-checked_in { border-top-color: #48a978; }.reservation-card.status-cancelled, .reservation-card.status-no_show { opacity: .66; }
.reservation-card-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 15px; }.reservation-time { color: #173e38; font-size: 1.5rem; font-weight: 800; letter-spacing: -.04em; }.reservation-status { padding: 4px 8px; border-radius: 999px; color: #536662; background: #edf2f0; font-size: .68rem; font-weight: 800; }.status-pending .reservation-status { color: #8a641d; background: #fff4d9; }.status-confirmed .reservation-status { color: #23669e; background: #e8f2fc; }.status-checked_in .reservation-status { color: #247549; background: #e8f7ee; }
.reservation-name { margin-bottom: 6px; color: #19312d; font-size: 1rem; }.reservation-meta { display: flex; flex-wrap: wrap; gap: 6px 12px; color: #6a7c77; font-size: .76rem; }.reservation-note { min-height: 34px; margin: 13px 0 0; color: #536662; font-size: .76rem; }.reservation-table { display: flex; align-items: center; gap: 8px; margin: auto 0 14px; padding: 9px 10px; border: 1px solid #dbe8e2; border-radius: 7px; color: #245b4d; background: #f0f8f4; font-size: .76rem; font-weight: 800; }.reservation-table.unassigned { color: #8b6a2b; border-color: #f0dfb0; background: #fff9e9; }.table-marker { font-size: 1rem; }.reservation-action { width: 100%; min-height: 40px; border: 1px solid transparent; border-radius: 7px; font: inherit; font-size: .78rem; font-weight: 800; cursor: pointer; }.reservation-action.primary { color: #fff; background: #1d6b57; }.reservation-action.primary:hover { background: #155644; }.reservation-action.primary span { float: right; font-size: 1.1rem; line-height: .8; }.reservation-action.muted { color: #7a8985; border-color: #dce5e1; background: #f1f4f3; cursor: not-allowed; }.reservation-action.done { color: #26764b; border-color: #bfe1cc; background: #ebf8ef; cursor: not-allowed; }.reservation-empty { display: grid; min-height: 260px; place-items: center; align-content: center; gap: 7px; text-align: center; }.empty-icon { display: grid; width: 52px; height: 52px; place-items: center; border-radius: 14px; color: #1d6b57; background: #dff1e9; font-size: 1.7rem; }
@media (max-width: 850px) { .reservation-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }.reservation-body { padding-left: 18px; padding-right: 18px; }
@media (max-width: 560px) { .reservation-overlay { padding: 8px; }.reservation-header { padding: 18px; }.reservation-grid { grid-template-columns: 1fr; }.reservation-dialog { max-height: 95vh; }.reservation-body { max-height: calc(95vh - 75px); } }
</style>

<template>
  <section class="operations-report" aria-labelledby="operations-report-title">
    <header class="operations-heading"><div><p class="module-eyebrow">VẬN HÀNH & KHÁCH HÀNG</p><h2 id="operations-report-title">Bốn góc nhìn vận hành trong một kỳ</h2><p>Mỗi bảng dùng đúng mốc thời gian của nguồn dữ liệu và có trạng thái tải/lỗi độc lập.</p></div></header>
    <div class="operations-grid">
      <ReservationOperationsBlock :from-date="fromDate" :to-date="toDate" />
      <KitchenTimeOperationsBlock :from-date="fromDate" :to-date="toDate" />
      <PromotionOperationsBlock :from-date="fromDate" :to-date="toDate" />
      <CustomerOperationsBlock :from-date="fromDate" :to-date="toDate" />
    </div>
  </section>
</template>
<script setup>
import ReservationOperationsBlock from './ReservationOperationsBlock.vue'
import KitchenTimeOperationsBlock from './KitchenTimeOperationsBlock.vue'
import PromotionOperationsBlock from './PromotionOperationsBlock.vue'
import CustomerOperationsBlock from './CustomerOperationsBlock.vue'
defineProps({ fromDate: { type: String, required: true }, toDate: { type: String, required: true } })
</script>
<style scoped>
.operations-report{display:flex;flex-direction:column;gap:16px}.operations-heading{padding:18px 20px;background:#f0fdfa;border:1px solid #99f6e4;border-radius:12px;color:#172033}.module-eyebrow{margin:0 0 5px;color:#0f766e;font-size:.7rem;font-weight:800;letter-spacing:.11em}.operations-heading h2{margin:0;font-size:1.15rem}.operations-heading p:not(.module-eyebrow){margin:5px 0 0;color:#64748b;font-size:.82rem}.operations-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}@media(max-width:1050px){.operations-grid{grid-template-columns:1fr}}
</style>

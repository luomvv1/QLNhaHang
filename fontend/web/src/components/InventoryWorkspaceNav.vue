<template>
  <nav class="inventory-workspace-nav" aria-label="Nghiệp vụ kho và bếp">
    <router-link v-for="item in items" :key="item.to" :to="item.to">
      <component :is="item.icon" :size="17" aria-hidden="true" /><span>{{ item.label }}</span>
    </router-link>
  </nav>
</template>
<script setup>
import { Boxes, Utensils, ClipboardList, RotateCcw } from '@lucide/vue';
const items = [
  { to: '/admin/inventory', label: 'Nguyên liệu & lô hàng', icon: Boxes },
  { to: '/admin/daily-portions', label: 'Điều phối suất ăn', icon: Utensils },
  { to: '/admin/stock-receipts', label: 'Phiếu kho', icon: ClipboardList },
  { to: '/admin/kitchen-reconciliation', label: 'Hoàn / hủy cuối ca', icon: RotateCcw },
];
</script>
<style scoped>
.inventory-workspace-nav { display: flex; align-items: center; gap: 6px; overflow-x: auto; margin: 0 0 18px; border-bottom: 1px solid #d9e2e8; padding: 0 0 8px; }
a { display: inline-flex; align-items: center; justify-content: center; flex: 0 0 auto; gap: 8px; min-height: 44px; padding: 8px 12px; border-radius: 6px; color: #4b606e; text-decoration: none; font-size: 13px; font-weight: 500; }
a:hover { background: #edf2f5; }a.router-link-exact-active { background: #e5f3ed; color: #11664b; font-weight: 600; }
a:focus-visible { outline: 3px solid #5ba8cf; outline-offset: -3px; }
</style>

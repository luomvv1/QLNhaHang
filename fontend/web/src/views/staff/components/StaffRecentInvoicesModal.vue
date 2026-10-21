<template>
  <div v-if="show" class="modal fade show d-block" style="background: rgba(0, 0, 0, 0.5);" @click.self="$emit('close')">
    <div class="modal-dialog modal-lg modal-dialog-centered">
      <div class="modal-content border-0 shadow-lg">
        <div class="modal-header bg-dark text-white">
          <h5 class="modal-title fw-bold">📑 HÓA ĐƠN GẦN ĐÂY</h5>
          <button type="button" class="btn-close btn-close-white" @click="$emit('close')"></button>
        </div>
        <div class="modal-body p-0" style="max-height: 70vh; overflow-y: auto;">
          <div v-if="loading" class="text-center py-5">
            <div class="spinner-border text-primary" role="status"></div>
          </div>
          <div v-else-if="invoices.length === 0" class="text-center py-5 text-muted">
            Chưa có hóa đơn nào gần đây.
          </div>
          <table v-else class="table table-hover mb-0 align-middle">
            <thead class="table-light sticky-top">
              <tr>
                <th class="ps-4">Mã HĐ</th>
                <th>Bàn</th>
                <th>Thời gian</th>
                <th>Khách hàng</th>
                <th class="text-end">Tổng tiền</th>
                <th class="text-center pe-4">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="inv in invoices" :key="inv.invoiceId">
                <td class="ps-4 fw-bold text-primary">#{{ inv.invoiceId }}</td>
                <td>{{ inv.tableDisplay || 'N/A' }}</td>
                <td>{{ formatDateTime(inv.issuedAt) }}</td>
                <td>{{ inv.customerName || 'Khách vãng lai' }}</td>
                <td class="text-end text-danger fw-bold">{{ formatCurrency(inv.totalAmount) }}</td>
                <td class="text-center pe-4">
                  <button class="btn btn-sm btn-outline-primary" @click="$emit('view-invoice', inv.invoiceId)">
                    Chi tiết & In
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue';
import axios from 'axios';

const props = defineProps({ show: Boolean });
const emit = defineEmits(['close', 'view-invoice']);

const invoices = ref([]);
const loading = ref(false);

watch(() => props.show, (val) => {
  if (val) loadData();
});

async function loadData() {
  loading.value = true;
  try {
    const res = await axios.get('http://localhost:5079/api/staff/payment/recent-invoices?limit=30');
    invoices.value = res.data || [];
  } catch (err) {
    console.error('Lỗi tải hóa đơn', err);
  } finally {
    loading.value = false;
  }
}

function formatCurrency(val) {
  if (!val && val !== 0) return '0 đ';
  return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
}

function formatDateTime(d) {
  if (!d) return '';
  return new Date(d).toLocaleString('vi-VN', { hour12: false });
}
</script>

<template>
  <Teleport to="body">
    <dialog ref="modal" class="reason-dialog" aria-labelledby="operational-reason-title" @cancel.prevent="finish(null)">
      <form @submit.prevent="finish(reason.trim())">
        <header>
          <h2 id="operational-reason-title">{{ dialog.title }}</h2>
          <button type="button" title="Đóng" aria-label="Đóng" @click="finish(null)"><X :size="20" /></button>
        </header>
        <p v-if="dialog.detail">{{ dialog.detail }}</p>
        <label for="operational-reason">Lý do <span>*</span></label>
        <textarea id="operational-reason" v-model="reason" autofocus required minlength="3" maxlength="500" rows="3" />
        <footer>
          <button type="button" class="secondary" @click="finish(null)">Quay lại</button>
          <button type="submit" class="confirm" :disabled="reason.trim().length < 3">{{ dialog.confirmText }}</button>
        </footer>
      </form>
    </dialog>
  </Teleport>
</template>

<script setup>
import { ref, watch, onBeforeUnmount } from 'vue';
import { X } from '@lucide/vue';
const props = defineProps({ dialog: { type: Object, required: true } });
const modal = ref(null);
const reason = ref('');
function finish(value) {
  const resolve = props.dialog.resolve;
  props.dialog.resolve = null;
  modal.value?.close();
  resolve?.(value);
}
watch(() => props.dialog.resolve, resolve => {
  if (resolve) { reason.value = ''; modal.value?.showModal(); }
  else modal.value?.close();
}, { flush: 'post' });
onBeforeUnmount(() => modal.value?.close());
</script>

<style scoped>
.reason-dialog { width: min(460px, calc(100vw - 32px)); max-height: calc(100dvh - 32px); overflow: auto; padding: 24px; border: 1px solid #d8e1e6; border-radius: 8px; color: #25353d; box-shadow: 0 12px 40px #0003; }
.reason-dialog::backdrop { background: #15252f88; }
header, footer { display: flex; align-items: center; gap: 12px; justify-content: space-between; }
h2 { margin: 0; font-size: 18px; font-weight: 600; }
header button { display: grid; place-items: center; min-width: 44px; height: 44px; border: 0; background: transparent; }
p { font-size: 14px; margin: 12px 0 20px; line-height: 1.5; }
label { display: block; margin: 12px 0 8px; font-weight: 600; font-size: 14px; }
label span { color: #b42332; }
textarea { display: block; width: 100%; box-sizing: border-box; border: 1px solid #aabac5; border-radius: 6px; padding: 10px; font: inherit; resize: vertical; }
footer { justify-content: flex-end; margin-top: 20px; flex-wrap: wrap; }
footer button { min-height: 44px; padding: 8px 16px; border-radius: 6px; border: 1px solid #d6e0e4; font: inherit; cursor: pointer; }
.secondary { background: #fff; color: #334854; }.confirm { background: #b42332; color: #fff; border-color: #b42332; }
button:disabled { opacity: .5; cursor: not-allowed; }textarea:focus-visible, button:focus-visible { outline: 3px solid #55a3df; outline-offset: 2px; }
</style>

import { onUnmounted, reactive } from 'vue';

export function useReasonDialog() {
  const reasonDialog = reactive({ title: '', detail: '', confirmText: '', resolve: null });
  function askReason(title, detail = '', confirmText = 'Xác nhận') {
    reasonDialog.resolve?.(null);
    return new Promise(resolve => Object.assign(reasonDialog, { title, detail, confirmText, resolve }));
  }
  onUnmounted(() => reasonDialog.resolve?.(null));
  return { reasonDialog, askReason };
}

import { defineStore } from 'pinia';
import { ref } from 'vue';

// Глобальні UI-сповіщення (винесено з orderStore: використовується як
// авторизацією, так і замовленнями, тож не належить жодному з них окремо).
export const useUiStore = defineStore('ui', () => {
  /** @type {import('vue').Ref<string | false>} */
  const showSuccessAlert = ref(false);

  const triggerSuccess = (msg) => {
    showSuccessAlert.value = msg;
    setTimeout(() => { showSuccessAlert.value = false; }, 3500);
  };

  return { showSuccessAlert, triggerSuccess };
});

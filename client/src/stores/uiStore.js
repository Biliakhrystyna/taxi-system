import { acceptHMRUpdate, defineStore } from 'pinia';
import { ref } from 'vue';

// Глобальні UI-сповіщення (винесено з orderStore: використовується як
// авторизацією, так і замовленнями, тож не належить жодному з них окремо).
export const useUiStore = defineStore('ui', () => {
  /** @type {import('vue').Ref<string | false>} */
  const showSuccessAlert = ref(false);
  /** @type {import('vue').Ref<string | false>} */
  const showErrorAlert = ref(false);

  const triggerSuccess = (msg) => {
    showSuccessAlert.value = msg;
    setTimeout(() => { showSuccessAlert.value = false; }, 3500);
  };

  const triggerError = (msg) => {
    showErrorAlert.value = msg;
    setTimeout(() => { showErrorAlert.value = false; }, 4000);
  };

  return { showSuccessAlert, showErrorAlert, triggerSuccess, triggerError };
});

// Без цього Vite оновлює файл стора "на льоту" (HMR), але вже створений
// в браузері екземпляр лишається зі старими методами/полями — доводилось би
// щоразу вручну перезавантажувати сторінку після будь-якої зміни в сторі.
if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useUiStore, import.meta.hot));
}

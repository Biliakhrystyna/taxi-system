import { watch } from 'vue';
import { useUiStore } from '../../../shared/uiStore';

const CHANNEL_NAME = 'taxi_tabs';

const roleLabel = (role) => (role === 'driver' ? 'водія' : 'пасажира');

/**
 * Одна людина не може тримати відкритими кабінети водія і пасажира одночасно.
 * Вкладка, що входить, оголошує свій email і роль; вкладка з тим самим email
 * в іншій ролі відповідає «зайнято» — і нова вкладка виходить.
 */
export function useTabGuard({ authState, currentUser, userRole, logout }) {
  const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);
  if (!channel) return;

  const tabId = Math.random().toString(36).slice(2);

  channel.onmessage = ({ data }) => {
    const user = currentUser.value;

    if (data.type === 'claim') {
      const conflicts = authState.value === 'main_app' && user?.email === data.email && userRole.value !== data.role;
      if (conflicts) {
        channel.postMessage({ type: 'busy', to: data.tabId, email: data.email, role: userRole.value });
      }
      return;
    }

    if (data.type === 'busy' && data.to === tabId && user?.email === data.email) {
      logout();
      useUiStore().triggerError(
        `Цей акаунт уже відкритий в іншій вкладці як ${roleLabel(data.role)}. Закрийте її або вийдіть, щоб зайти як ${roleLabel(user.role)}.`
      );
    }
  };

  watch(authState, (state) => {
    if (state === 'main_app' && currentUser.value) {
      channel.postMessage({ type: 'claim', tabId, email: currentUser.value.email, role: currentUser.value.role });
    }
  });
}

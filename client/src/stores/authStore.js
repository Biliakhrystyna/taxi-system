import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useOrderStore } from './orderStore';
import { useUiStore } from './uiStore';
import { authApi } from '../services/authApi';
import { ApiError } from '../services/http';

// Автентифікація, роль та реєстраційні дані користувача — тепер через REST
// до ASP.NET Core (AuthController), а не Firestore.
export const useAuthStore = defineStore('auth', () => {
  /** @type {import('vue').Ref<'passenger' | 'driver' | null>} */
  const userRole = ref(null);
  const authState = ref('role_selection');
  const isSignUp = ref(false);
  /** @type {import('vue').Ref<import('../types/user').ApiUser | null>} */
  const currentUser = ref(null);

  const emailInput = ref('');
  const passwordInput = ref('');
  const firstNameInput = ref('');
  const lastNameInput = ref('');
  const phoneInput = ref('');
  const licenseInput = ref('');
  const driverCarClass = ref('comfort');
  const documentUploaded = ref(false);
  const faceVerified = ref(false);

  const handleAuthSubmit = async () => {
    const orderStore = useOrderStore();
    const uiStore = useUiStore();

    orderStore.resetSession();

    if (!emailInput.value || !passwordInput.value) return;

    if (isSignUp.value) {
      if (passwordInput.value.length < 8) {
        uiStore.triggerError('Некоректний ввід: пароль має містити щонайменше 8 символів.');
        return;
      }

      const phoneDigits = phoneInput.value.replace(/\D/g, '');
      if (phoneDigits.length !== 10) {
        uiStore.triggerError('Некоректний ввід: номер телефону має містити 10 цифр.');
        return;
      }
    }

    try {
      if (isSignUp.value) {
        const user = await authApi.register({
          email: emailInput.value,
          password: passwordInput.value,
          first_name: firstNameInput.value,
          last_name: lastNameInput.value,
          phone: phoneInput.value,
          role: userRole.value,
          license_number: userRole.value === 'driver' ? licenseInput.value : null,
          driver_car_class: userRole.value === 'driver' ? driverCarClass.value : null,
        });

        currentUser.value = user;

        if (userRole.value === 'driver') {
          authState.value = 'verified_check';
        } else {
          uiStore.triggerSuccess('Реєстрація пасажира успішна!');
          authState.value = 'main_app';
          orderStore.subscribeToUserData(user.email, user.role);
        }
      } else {
        const user = await authApi.login({
          email: emailInput.value,
          password: passwordInput.value,
          role: userRole.value,
        });

        currentUser.value = user;
        uiStore.triggerSuccess(`Вітаємо, ${user.first_name}! Вхід успішний.`);

        if (user.status === 'pending_verification') {
          authState.value = 'verified_check';
        } else {
          authState.value = 'main_app';
          orderStore.subscribeToUserData(user.email, user.role);
        }
      }
    } catch (err) {
      if (err instanceof ApiError) {
        // Сервер уже повертає людяний текст (409/401/400) — показуємо як є.
        uiStore.triggerError(err.message || 'Помилка автентифікації.');
      } else {
        uiStore.triggerError("Сталася помилка з'єднання з сервером. Перевірте, чи запущений бекенд (dotnet run у server/TaxiSystem.Api).");
      }
    }
  };

  // Біометрична верифікація
  const verifyDriverDocuments = async () => {
    if (!currentUser.value) return;

    const orderStore = useOrderStore();
    const user = await authApi.verifyDriver(currentUser.value.email);

    currentUser.value = user;
    authState.value = 'main_app';
    orderStore.subscribeToUserData(user.email, user.role);

    useUiStore().triggerSuccess('Біометрію пройдено! Обліковий запис водія активовано.');
  };

  // ВИХІД З АКАУНТУ
  const logout = () => {
    useOrderStore().resetSession();

    currentUser.value = null;
    userRole.value = null;
    authState.value = 'role_selection';

    // Очищення інпутів форми
    emailInput.value = '';
    passwordInput.value = '';
    firstNameInput.value = '';
    lastNameInput.value = '';
    phoneInput.value = '';
  };

  return {
    userRole, authState, isSignUp, currentUser,
    emailInput, passwordInput, firstNameInput, lastNameInput, phoneInput, licenseInput, driverCarClass,
    documentUploaded, faceVerified,
    handleAuthSubmit, verifyDriverDocuments, logout
  };
});

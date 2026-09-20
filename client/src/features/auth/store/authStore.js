import { acceptHMRUpdate, defineStore } from 'pinia';
import { ref, watch } from 'vue';
import { useOrderStore } from '../../order/store/orderStore';
import { useUiStore } from '../../../shared/uiStore';
import { useTabGuard } from './useTabGuard';
import { authApi } from '../authApi';
import { ApiError, errorMessage } from '../../../shared/http';
import { normalizeLicense, phoneDigitsOf, validateSignUp } from '../authValidation';

// Автентифікація, роль та реєстраційні дані користувача — через REST
// до ASP.NET Core (AuthController).
export const useAuthStore = defineStore('auth', () => {
  /** @type {import('vue').Ref<'passenger' | 'driver' | null>} */
  const userRole = ref(null);
  const authState = ref('role_selection');
  const isSignUp = ref(false);
  /** @type {import('vue').Ref<import('../../../shared/types/user').ApiUser | null>} */
  const currentUser = ref(null);

  // При реєстрації — обов'язково email (потрібен для листа підтвердження).
  // При вході — email АБО телефон, сервер сам визначає, що саме це.
  const identifierInput = ref('');
  const passwordInput = ref('');
  const firstNameInput = ref('');
  const lastNameInput = ref('');
  const phoneInput = ref('');
  const licenseInput = ref('');
  const driverCarClass = ref('comfort');
  const documentUploaded = ref(false);
  const faceVerified = ref(false);
  const verificationCodeInput = ref('');

  // Відновлення пароля — той самий код, що й підтвердження email, просто
  // інший сценарій використання 
  const forgotPasswordEmail = ref('');
  const forgotPasswordCode = ref('');
  const forgotPasswordNewPassword = ref('');
  const forgotPasswordStep = ref('request'); // 'request' | 'reset'

  // Збережений вхід: після перезавантаження вкладки користувач лишається в
  // кабінеті, а не повертається на вибір ролі. sessionStorage — окремий для
  // кожної вкладки, тож водій і пасажир у сусідніх вікнах не перезаписують
  // один одного.
  const SESSION_KEY = 'taxi_session';

  const restoreSession = () => {
    try {
      const raw = sessionStorage.getItem(SESSION_KEY);
      if (!raw) return;

      const user = JSON.parse(raw);
      if (!user?.email || !['passenger', 'driver'].includes(user.role)) return;

      currentUser.value = user;
      userRole.value = user.role;
      authState.value = 'main_app';
      useOrderStore().subscribeToUserData(user.email, user.role);
    } catch {
      
    }
  };

  watch([authState, currentUser], () => {
    try {
      if (authState.value === 'main_app' && currentUser.value) {
        sessionStorage.setItem(SESSION_KEY, JSON.stringify(currentUser.value));
      } else if (authState.value === 'role_selection') {
        sessionStorage.removeItem(SESSION_KEY);
      }
    } catch {
      
    }
  }, { deep: true });

  const handleAuthSubmit = async () => {
    const orderStore = useOrderStore();
    const uiStore = useUiStore();

    orderStore.resetSession();

    if (!identifierInput.value || !passwordInput.value) return;

    if (isSignUp.value) {
      const validationError = validateSignUp({
        email: identifierInput.value,
        password: passwordInput.value,
        phone: phoneInput.value,
        role: userRole.value,
        license: licenseInput.value,
      });

      if (validationError) {
        uiStore.triggerError(validationError);
        return;
      }

      if (userRole.value === 'driver') {
        licenseInput.value = normalizeLicense(licenseInput.value);
      }
    }

    try {
      if (isSignUp.value) {
        const phoneDigits = phoneDigitsOf(phoneInput.value);
        const user = await authApi.register({
          email: identifierInput.value,
          password: passwordInput.value,
          first_name: firstNameInput.value,
          last_name: lastNameInput.value,
          phone: `+380${phoneDigits}`,
          role: userRole.value,
          license_number: userRole.value === 'driver' ? licenseInput.value : null,
          driver_car_class: userRole.value === 'driver' ? driverCarClass.value : null,
        });

        currentUser.value = user;

        if (user.email_confirmed) {
          // Пошта вже підтверджена раніше — це додавання нової ролі до вже
          // існуючого акаунта, а не первинна реєстрація, тож код не потрібен.
          if (user.role === 'driver' && user.status === 'pending_verification') {
            authState.value = 'verified_check';
          } else {
            uiStore.triggerSuccess(`Роль додано! Вітаємо, ${user.first_name}.`);
            authState.value = 'main_app';
            orderStore.subscribeToUserData(user.email, user.role);
          }
        } else {
          
          uiStore.triggerSuccess('Код підтвердження надіслано на вашу пошту.');
          authState.value = 'email_verification';
        }
      } else {
        const user = await authApi.login({
          identifier: identifierInput.value,
          password: passwordInput.value,
          role: userRole.value,
        });

        currentUser.value = user;
        uiStore.triggerSuccess(`Вітаємо, ${user.first_name}! Вхід успішний.`);

       
        if (user.role === 'driver' && user.status === 'pending_verification') {
          authState.value = 'verified_check';
        } else {
          authState.value = 'main_app';
          orderStore.subscribeToUserData(user.email, user.role);
        }
      }
    } catch (err) {
      if (err instanceof ApiError) {

        if (err.status === 403 && !isSignUp.value && identifierInput.value.includes('@')) {
          currentUser.value = { email: identifierInput.value };
          uiStore.triggerError(err.message);
          authState.value = 'email_verification';
          return;
        }

        
        uiStore.triggerError(err.message || 'Помилка автентифікації.');
      } else {
        uiStore.triggerError("Сталася помилка з'єднання з сервером. Перевірте, чи запущений бекенд (dotnet run у server/TaxiSystem.Api).");
      }
    }
  };

 
  const handleVerifyEmail = async () => {
    if (!currentUser.value || !verificationCodeInput.value) return;

    const orderStore = useOrderStore();
    const uiStore = useUiStore();

    try {
      const user = await authApi.verifyEmail({
        email: currentUser.value.email,
        code: verificationCodeInput.value,
      });

      currentUser.value = user;
      verificationCodeInput.value = '';

      if (user.role === 'driver') {
        authState.value = 'verified_check';
      } else {
        uiStore.triggerSuccess('Пошту підтверджено! Реєстрація завершена.');
        authState.value = 'main_app';
        orderStore.subscribeToUserData(user.email, user.role);
      }
    } catch (err) {
      uiStore.triggerError(errorMessage(err));
    }
  };

  const handleResendCode = async () => {
    if (!currentUser.value) return;
    const uiStore = useUiStore();

    try {
      await authApi.resendVerification(currentUser.value.email);
      uiStore.triggerSuccess('Новий код надіслано на вашу пошту.');
    } catch (err) {
      uiStore.triggerError(errorMessage(err));
    }
  };


  const handleForgotPasswordRequest = async () => {
    if (!forgotPasswordEmail.value) return;
    const uiStore = useUiStore();

    try {
      await authApi.forgotPassword(forgotPasswordEmail.value);
      uiStore.triggerSuccess('Якщо такий email зареєстровано — код надіслано на пошту.');
      forgotPasswordStep.value = 'reset';
    } catch (err) {
      uiStore.triggerError(errorMessage(err));
    }
  };


  const handleResetPassword = async () => {
    const uiStore = useUiStore();

    if (forgotPasswordNewPassword.value.length < 8) {
      uiStore.triggerError('Пароль має містити щонайменше 8 символів.');
      return;
    }

    try {
      await authApi.resetPassword({
        email: forgotPasswordEmail.value,
        code: forgotPasswordCode.value,
        new_password: forgotPasswordNewPassword.value,
      });

      uiStore.triggerSuccess('Пароль змінено! Тепер увійдіть із новим паролем.');

      forgotPasswordStep.value = 'request';
      forgotPasswordEmail.value = '';
      forgotPasswordCode.value = '';
      forgotPasswordNewPassword.value = '';
      isSignUp.value = false;
      authState.value = 'auth_form';
    } catch (err) {
      uiStore.triggerError(errorMessage(err));
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
    identifierInput.value = '';
    passwordInput.value = '';
    firstNameInput.value = '';
    lastNameInput.value = '';
    phoneInput.value = '';
    verificationCodeInput.value = '';
  };

  useTabGuard({ authState, currentUser, userRole, logout });

  return {
    userRole, authState, isSignUp, currentUser,
    identifierInput, passwordInput, firstNameInput, lastNameInput, phoneInput, licenseInput, driverCarClass,
    documentUploaded, faceVerified, verificationCodeInput,
    forgotPasswordEmail, forgotPasswordCode, forgotPasswordNewPassword, forgotPasswordStep,
    handleAuthSubmit, handleVerifyEmail, handleResendCode, verifyDriverDocuments, logout, restoreSession,
    handleForgotPasswordRequest, handleResetPassword,
  };
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useAuthStore, import.meta.hot));
}

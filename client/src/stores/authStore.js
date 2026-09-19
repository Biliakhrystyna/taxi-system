import { acceptHMRUpdate, defineStore } from 'pinia';
import { ref } from 'vue';
import { useOrderStore } from './orderStore';
import { useUiStore } from './uiStore';
import { authApi } from '../services/authApi';
import { ApiError } from '../services/http';

// Автентифікація, роль та реєстраційні дані користувача — через REST
// до ASP.NET Core (AuthController).
export const useAuthStore = defineStore('auth', () => {
  /** @type {import('vue').Ref<'passenger' | 'driver' | null>} */
  const userRole = ref(null);
  const authState = ref('role_selection');
  const isSignUp = ref(false);
  /** @type {import('vue').Ref<import('../types/user').ApiUser | null>} */
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
  // інший сценарій використання (див. AuthController.ForgotPassword/ResetPassword).
  const forgotPasswordEmail = ref('');
  const forgotPasswordCode = ref('');
  const forgotPasswordNewPassword = ref('');
  const forgotPasswordStep = ref('request'); // 'request' | 'reset'

  const handleAuthSubmit = async () => {
    const orderStore = useOrderStore();
    const uiStore = useUiStore();

    orderStore.resetSession();

    if (!identifierInput.value || !passwordInput.value) return;

    if (isSignUp.value) {
      if (passwordInput.value.length < 8) {
        uiStore.triggerError('Некоректний ввід: пароль має містити щонайменше 8 символів.');
        return;
      }

      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(identifierInput.value)) {
        uiStore.triggerError('Некоректний ввід: введіть справжню електронну пошту.');
        return;
      }

      // Поле телефону тепер містить лише 9 цифр після незмінного префіксу
      // +380 (сам префікс показаний у формі, не вводиться користувачем) —
      // перша цифра ніколи не 0/1/2 у реальних операторських кодах, тож це
      // відсіює сміття типу "000000000" без жорсткого списку кодів.
      const phoneDigits = phoneInput.value.replace(/\D/g, '');
      if (!/^[3-9]\d{8}$/.test(phoneDigits)) {
        uiStore.triggerError('Некоректний ввід: номер телефону має бути 9 цифр після +380 (не починається з 0-2).');
        return;
      }

      // Українське пластикове посвідчення водія: 3 літери + 6 цифр, без
      // пробілів/дефісів/крапок (самі пробіли з поля вводу прибираємо перед
      // перевіркою, решта спецсимволів провалює формат).
      if (userRole.value === 'driver') {
        const licenseNormalized = licenseInput.value.replace(/\s/g, '').toUpperCase();
        if (!/^[A-ZА-ЯҐЄІЇ]{3}\d{6}$/.test(licenseNormalized)) {
          uiStore.triggerError('Некоректний ввід: номер посвідчення водія має бути 3 літери + 6 цифр (напр. ВХХ123456).');
          return;
        }
        licenseInput.value = licenseNormalized;
      }
    }

    try {
      if (isSignUp.value) {
        const phoneDigits = phoneInput.value.replace(/\D/g, '');
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
          // Пошту підтверджуємо одразу після реєстрації, для обох ролей —
          // раніше за біометричну перевірку водія (спершу доводимо, що пошта
          // справжня, потім уже документи).
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

        // "pending_verification" стосується лише ролі водія (біометрія/
        // документи) — той самий акаунт може вже мати активну роль пасажира,
        // тож перевірка статусу має враховувати, у якій ролі саме входимо.
        if (user.role === 'driver' && user.status === 'pending_verification') {
          authState.value = 'verified_check';
        } else {
          authState.value = 'main_app';
          orderStore.subscribeToUserData(user.email, user.role);
        }
      }
    } catch (err) {
      if (err instanceof ApiError) {
        // 403 при вході = пароль вірний, але пошта не підтверджена — ведемо
        // на той самий екран коду замість глухого кута з текстом помилки.
        // Без email (увійшли за телефоном) — цей перехід неможливий, лишається
        // звичайний банер помилки, бо надіслати новий код нема на яку адресу.
        if (err.status === 403 && !isSignUp.value && identifierInput.value.includes('@')) {
          currentUser.value = { email: identifierInput.value };
          uiStore.triggerError(err.message);
          authState.value = 'email_verification';
          return;
        }

        // Сервер уже повертає людяний текст (409/401/400) — показуємо як є.
        uiStore.triggerError(err.message || 'Помилка автентифікації.');
      } else {
        uiStore.triggerError("Сталася помилка з'єднання з сервером. Перевірте, чи запущений бекенд (dotnet run у server/TaxiSystem.Api).");
      }
    }
  };

  // Одноразове підтвердження пошти кодом з листа (EmailConfirmed=true назавжди
  // після успіху) — до нього ні водій, ні пасажир не потрапляє в кабінет.
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
      uiStore.triggerError(err instanceof ApiError ? err.message : "Сталася помилка з'єднання з сервером.");
    }
  };

  const handleResendCode = async () => {
    if (!currentUser.value) return;
    const uiStore = useUiStore();

    try {
      await authApi.resendVerification(currentUser.value.email);
      uiStore.triggerSuccess('Новий код надіслано на вашу пошту.');
    } catch (err) {
      uiStore.triggerError(err instanceof ApiError ? err.message : "Сталася помилка з'єднання з сервером.");
    }
  };

  // Крок 1: запит коду на email. Навмисно завжди показуємо той самий успіх —
  // сервер теж не розкриває, чи існує такий email (щоб не давати спосіб
  // перебором з'ясовувати зареєстровані адреси).
  const handleForgotPasswordRequest = async () => {
    if (!forgotPasswordEmail.value) return;
    const uiStore = useUiStore();

    try {
      await authApi.forgotPassword(forgotPasswordEmail.value);
      uiStore.triggerSuccess('Якщо такий email зареєстровано — код надіслано на пошту.');
      forgotPasswordStep.value = 'reset';
    } catch (err) {
      uiStore.triggerError(err instanceof ApiError ? err.message : "Сталася помилка з'єднання з сервером.");
    }
  };

  // Крок 2: код + новий пароль.
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
      uiStore.triggerError(err instanceof ApiError ? err.message : "Сталася помилка з'єднання з сервером.");
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

  return {
    userRole, authState, isSignUp, currentUser,
    identifierInput, passwordInput, firstNameInput, lastNameInput, phoneInput, licenseInput, driverCarClass,
    documentUploaded, faceVerified, verificationCodeInput,
    forgotPasswordEmail, forgotPasswordCode, forgotPasswordNewPassword, forgotPasswordStep,
    handleAuthSubmit, handleVerifyEmail, handleResendCode, verifyDriverDocuments, logout,
    handleForgotPasswordRequest, handleResetPassword,
  };
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useAuthStore, import.meta.hot));
}

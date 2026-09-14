import { defineStore } from 'pinia';
import { ref } from 'vue';
import { db } from '../firebase';
import { doc, setDoc, updateDoc, getDoc } from 'firebase/firestore';
import { useOrderStore } from './orderStore';
import { useUiStore } from './uiStore';

// Автентифікація, роль та реєстраційні дані користувача.
// TODO(backend): замінити прямі виклики Firestore на REST-запити до
// ASP.NET Core AuthController (JWT + BCrypt) — див. docs/ARCHITECTURE.md.
export const useAuthStore = defineStore('auth', () => {
  const userRole = ref(null);
  const authState = ref('role_selection');
  const isSignUp = ref(false);
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

    const userDocRef = doc(db, "users", emailInput.value);

    if (isSignUp.value) {
      // РЕЄСТРАЦІЯ
      const newUser = {
        email: emailInput.value,
        password_hash: passwordInput.value,
        first_name: firstNameInput.value,
        last_name: lastNameInput.value,
        phone: phoneInput.value,
        role: userRole.value,
        status: userRole.value === 'driver' ? 'pending_verification' : 'active',
        total_trips: 0
      };

      if (userRole.value === 'driver') {
        newUser.license_number = licenseInput.value;
        newUser.driver_car_class = driverCarClass.value;
      }

      await setDoc(userDocRef, newUser);
      currentUser.value = newUser;

      if (userRole.value === 'driver') {
        authState.value = 'verified_check';
      } else {
        uiStore.triggerSuccess("Реєстрація пасажира успішна!");
        authState.value = 'main_app';
        orderStore.subscribeToUserData(currentUser.value.email, currentUser.value.role);
      }
    } else {
      // ВХІД
      const userSnap = await getDoc(userDocRef);
      if (userSnap.exists()) {
        const userData = userSnap.data();
        if (userData.password_hash === passwordInput.value && userData.role === userRole.value) {
          currentUser.value = userData;
          uiStore.triggerSuccess(`Вітаємо, ${userData.first_name}! Вхід успішний.`);

          if (userData.status === 'pending_verification') {
            authState.value = 'verified_check';
          } else {
            authState.value = 'main_app';
            orderStore.subscribeToUserData(currentUser.value.email, currentUser.value.role);
          }
        } else {
          alert("Невірний пароль або роль!");
        }
      } else {
        alert("Користувача не знайдено! Пройдіть реєстрацію.");
      }
    }
  };

  // Біометрична верифікація
  const verifyDriverDocuments = async () => {
    if (!currentUser.value) return;
    const userDocRef = doc(db, "users", currentUser.value.email);
    await updateDoc(userDocRef, {
      status: 'active'
    });

    currentUser.value.status = 'active';
    authState.value = 'main_app';

    const orderStore = useOrderStore();
    orderStore.subscribeToUserData(currentUser.value.email, currentUser.value.role);

    useUiStore().triggerSuccess("Біометрію пройдено! Обліковий запис водія активовано.");
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

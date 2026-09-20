<template>
  <div class="app-container">
    <div v-if="uiStore.showSuccessAlert" class="global-alert">
      🎉 {{ uiStore.showSuccessAlert }}
    </div>

    <div v-if="uiStore.showErrorAlert" class="global-alert error-alert">
      ⚠️ {{ uiStore.showErrorAlert }}
    </div>

    <header class="app-header">
      <h1>🚖 Система таксі перевезень</h1>
      <button v-if="authStore.authState === 'main_app'" class="btn logout-btn" @click="authStore.logout">🚪 Вийти з акаунту</button>
    </header>

    <p v-if="authStore.authState === 'role_selection'" class="dev-credit">Розробник: Біляк Христина (Група ОІ-32)</p>

    <RoleSelector v-if="authStore.authState === 'role_selection'" />

    <AuthForm v-if="authStore.authState === 'auth_form'" />

    <EmailVerification v-if="authStore.authState === 'email_verification'" />

    <ForgotPassword v-if="authStore.authState === 'forgot_password'" />

    <DriverVerification v-if="authStore.authState === 'verified_check'" />

    <div v-if="authStore.authState === 'main_app'">

      <div class="user-profile-banner">
        <span>👤 Користувач: <strong>{{ authStore.currentUser?.first_name }} {{ authStore.currentUser?.last_name }}</strong></span>
        <span class="counter-badge">📊 Ваш особистий лічильник поїздок: <strong>{{ orderStore.totalTripsCounter }}</strong></span>
      </div>

      <div class="main-grid">
        <PassengerDashboard v-if="authStore.currentUser && authStore.currentUser.role === 'passenger'" />

        <DriverDashboard v-if="authStore.currentUser?.role === 'driver'" />

        <TripHistoryTable />
      </div>
    </div>
    </div>
    <WeatherWarningModal />
    <TripRatingModal />
</template>

<script setup lang="ts">
import { useAuthStore } from './stores/authStore';
import { useOrderStore } from './stores/orderStore';
import { useUiStore } from './stores/uiStore';
import RoleSelector from './components/auth/RoleSelector.vue';
import AuthForm from './components/auth/AuthForm.vue';
import ForgotPassword from './components/auth/ForgotPassword.vue';
import EmailVerification from './components/auth/EmailVerification.vue';
import DriverVerification from './components/auth/DriverVerification.vue';
import WeatherWarningModal from './components/common/WeatherWarningModal.vue';
import TripRatingModal from './components/common/TripRatingModal.vue';
import PassengerDashboard from './components/passenger/PassengerDashboard.vue';
import DriverDashboard from './components/driver/DriverDashboard.vue';
import TripHistoryTable from './components/history/TripHistoryTable.vue';

const authStore = useAuthStore();
authStore.restoreSession();
const orderStore = useOrderStore();
const uiStore = useUiStore();
</script>

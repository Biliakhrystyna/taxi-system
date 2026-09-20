<template>
  <div class="auth-card login-card">
    <button class="back-link" @click="authStore.authState = 'role_selection'">← Назад до вибору ролі</button>
    <h2>
      {{ authStore.userRole === 'passenger' ? '🙋‍♀️ Кабінет Пасажира' : '🚖 Кабінет Водія' }} :
      {{ authStore.isSignUp ? 'Реєстрація' : 'Авторизація' }}
    </h2>

    <form @submit.prevent="authStore.handleAuthSubmit" class="auth-form">
      <div v-if="authStore.isSignUp" class="form-grid">

        <div class="form-group">
          <label>Ім'я:</label>
          <input type="text" v-model="authStore.firstNameInput" class="form-input" required />
        </div>

        <div class="form-group">
          <label>Прізвище:</label>
          <input type="text" v-model="authStore.lastNameInput" class="form-input" required />
        </div>

        <div class="form-group mt-2">
          <label>Номер телефону:</label>
          <div style="display: flex; align-items: center; gap: 8px;">
            <span style="font-weight: 800; color: #000000; flex-shrink: 0;">+380</span>
            <input type="text" v-model="authStore.phoneInput" placeholder="671234567" maxlength="9" class="form-input" style="flex: 1; min-width: 0;" required />
          </div>
        </div>

        <div v-if="authStore.userRole === 'driver'" class="driver-fields mt-3" style="grid-column: 1 / -1;">
          <div class="form-group">
            <label>🪪 Номер водійського посвідчення:</label>
            <input
              type="text"
              :value="authStore.licenseInput"
              @input="authStore.licenseInput = ($event.target as HTMLInputElement).value.toUpperCase()"
              placeholder="ААВ654321"
              class="form-input"
              required
            />
          </div>

          <div class="form-group mt-2">
            <label>🚗 Робочий клас автомобіля:</label>
            <select v-model="authStore.driverCarClass" class="form-select">
              <option value="econom">🍃 Економ (Доступний сегмент)</option>
              <option value="comfort">⭐ Комфорт (Оптимальний сегмент)</option>
              <option value="lux">💎 Люкс / Бізнес (Преміум сегмент)</option>
            </select>
          </div>
        </div>

      </div> <div class="form-group mt-3">
        <label>{{ authStore.isSignUp ? 'Електронна пошта (Email):' : 'Email або номер телефону:' }}</label>
        <input
          :type="authStore.isSignUp ? 'email' : 'text'"
          v-model="authStore.identifierInput"
          :placeholder="authStore.isSignUp ? 'name@example.com' : 'name@example.com або 0671234567'"
          class="form-input"
          required
        />
      </div>

      <div class="form-group mt-2">
        <label>Пароль:</label>
        <div style="position: relative;">
          <input
            :type="showPassword ? 'text' : 'password'"
            v-model="authStore.passwordInput"
            :placeholder="authStore.isSignUp ? 'Щонайменше 8 символів' : '••••••••'"
            class="form-input"
            style="width: 100%; padding-right: 40px;"
            required
          />
          <button
            type="button"
            @click="showPassword = !showPassword"
            style="position: absolute; right: 8px; top: 50%; transform: translateY(-50%); background: none; border: none; cursor: pointer; font-size: 16px; padding: 4px;"
            :title="showPassword ? 'Приховати пароль' : 'Показати пароль'"
          >
            {{ showPassword ? '🙈' : '👁️' }}
          </button>
        </div>
        <p v-if="!authStore.isSignUp" class="toggle-auth-text" style="text-align: right; margin-top: 6px;">
          <span @click="authStore.authState = 'forgot_password'; authStore.forgotPasswordEmail = authStore.identifierInput.includes('@') ? authStore.identifierInput : '';">
            Забули пароль?
          </span>
        </p>
      </div>

      <div class="mt-4">
        <button type="submit" class="btn success-btn w-full">
          {{ authStore.isSignUp ? '✨ Зареєструватися в системі' : '🔑 Увійти в кабінет' }}
        </button>
      </div>

      <p class="toggle-auth-text">
      {{ authStore.isSignUp ? 'Вже є акаунт?' : 'Вперше в системі?' }}
      <span @click="authStore.isSignUp = !authStore.isSignUp">
        {{ authStore.isSignUp ? 'Увійти' : 'Зареєструватися' }}
      </span>
    </p>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../store/authStore';

const authStore = useAuthStore();
const showPassword = ref(false);
</script>

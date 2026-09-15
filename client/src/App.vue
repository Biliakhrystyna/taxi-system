<template>
  <div class="app-container">
    <div v-if="uiStore.showSuccessAlert" class="global-alert">
      🎉 {{ uiStore.showSuccessAlert }}
    </div>

    <header class="app-header">
      <h1>🚖 Система таксі перевезень </h1>
      <p>Розробник: Біляк Христина (Група ОІ-32)</p>
      <button v-if="authStore.authState === 'main_app'" class="btn logout-btn" @click="authStore.logout">🚪 Вийти з акаунту</button>
    </header>

    <RoleSelector v-if="authStore.authState === 'role_selection'" />

    <AuthForm v-if="authStore.authState === 'auth_form'" />

    <DriverVerification v-if="authStore.authState === 'verified_check'" />

    <div v-if="authStore.authState === 'main_app'">

      <WeatherControls />

      <div class="user-profile-banner">
        <span>👤 Користувач: <strong>{{ authStore.currentUser?.first_name }} {{ authStore.currentUser?.last_name }}</strong></span>
        <span class="counter-badge">📊 Ваш особистий лічильник поїздок в базі: <strong>{{ orderStore.totalTripsCounter }}</strong></span>
      </div>

      <div class="main-grid">
        <PassengerDashboard v-if="authStore.currentUser && authStore.currentUser.role === 'passenger'" />

        <DriverDashboard v-if="authStore.currentUser?.role === 'driver'" />

        <TripHistoryTable />
      </div>
    </div>
    </div>
    <WeatherWarningModal />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useAuthStore } from './stores/authStore';
import { useOrderStore } from './stores/orderStore';
import { useUiStore } from './stores/uiStore';
import RoleSelector from './components/auth/RoleSelector.vue';
import AuthForm from './components/auth/AuthForm.vue';
import DriverVerification from './components/auth/DriverVerification.vue';
import WeatherControls from './components/common/WeatherControls.vue';
import WeatherWarningModal from './components/common/WeatherWarningModal.vue';
import PassengerDashboard from './components/passenger/PassengerDashboard.vue';
import DriverDashboard from './components/driver/DriverDashboard.vue';
import TripHistoryTable from './components/history/TripHistoryTable.vue';

const authStore = useAuthStore();
const orderStore = useOrderStore();
const uiStore = useUiStore();

onMounted(() => {
  // Якщо є збережені стани, можемо ініціалізувати
});
</script>

<style>
body {
  font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;

  /* Фото тла з яскравим проявленням текстури дороги */
  background-image: linear-gradient(rgba(11, 15, 25, 0.40), rgba(11, 15, 25, 0.55)), url('./assets/taxi.jpg.jpg');

  background-size: cover;          /* Розтягує картинку на весь екран */
  background-position: center;     /* Центрує текстуру розмітки */
  background-attachment: fixed;    /* Залишає тло нерухомим при прокручуванні */
  background-repeat: no-repeat;

  color: #f8fafc; /* Білий колір для загального тексту поза картками */
  margin: 0;
  padding: 20px;
}

.app-container {
  max-width: 1200px;
  margin: 0 auto;
  position: relative;
}

/* 🏢 ШАПКА ПЛАТФОРМИ (ФЛЕКС-АДАПТИВНІСТЬ БЕЗ НАЛАЗАННЯ КНОПКИ) */
.app-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  border-bottom: 3px solid #eab308; /* Фірмовий жовтий бордюр */
  padding-bottom: 20px;
  margin-bottom: 25px;
  position: relative;
  gap: 10px;
}
.app-header h1 {
  margin: 0;
  color: #eab308; /* Жовтий титул */
  font-size: 30px;
  font-weight: 900; /* Максимально жирний шрифт */
  text-transform: uppercase; /* Тільки великі літери */
  letter-spacing: 2px; /* Сучасний розріджений інтервал */
  text-shadow: 0 0 15px rgba(234, 179, 8, 0.3);
}
.app-header p {
  color: #ffffff;
  font-size: 16px;
  font-weight: 500;
  margin: 5px 0 0 0;
}
.logout-btn {
  background: #000000;
  color: #eab308;
  border: 2px solid #eab308; /* Жовтий обідок кнопки під колір теми */
  font-weight: bold;
  font-size: 12px;
  padding: 8px 16px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-top: 5px;
}
.logout-btn:hover {
  background: #eab308;
  color: #000000;
  border-color: #000000;
}

@media (min-width: 768px) {
  .app-header {
    flex-direction: row;
    justify-content: space-between;
    text-align: left;
    align-items: center;
  }
  .logout-btn {
    margin-top: 0;
  }
}

/* 🖨️ НОВИЙ СТИЛЬ КАРТОК: ЖОВТИЙ ФОН, ЧОРНИЙ ОБІДОК, ЧОРНИЙ ШРИФТ */
.auth-card, .screen-card {
  background: #eab308; /* Фірмовий насичений жовтий */
  color: #000000;      /* Текст всередині карток тепер СУТO ЧОРНИЙ */
  padding: 25px;
  border-radius: 12px;
  border: 3px solid #000000; /* Жирний чорний обідок */
  box-shadow: 0 15px 25px rgba(0, 0, 0, 0.6);
}
.auth-card h2, .screen-card h2 {
  margin-top: 0;
  font-size: 22px;
  color: #000000 !important; /* Назви вікон тепер теж строго чорні */
  border-bottom: 2px solid #000000; /* Чорна лінія розподілу */
  padding-bottom: 12px;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

/* Корекція кольору заголовків у різних боксах */
.passenger-box h2, .driver-box h2, .auth-card h2 { color: #000000 !important; }

.subtitle { color: #000000; font-size: 14px; margin-bottom: 25px; font-weight: 600; }
.back-link { background: none; border: none; color: #000000; font-weight: bold; cursor: pointer; font-size: 13px; margin-bottom: 15px; display: block; }
.back-link:hover { text-decoration: underline; }

/* 🌤️ БЛОК СИМУЛЯЦІЇ ПОГОДИ */
.weather-simulator span {
  color: #ffffff !important;
  font-weight: 700;
  text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
}

/* 🕹️ КНОПКИ ВИБОРУ РОЛЕЙ */
.btn-group-row { display: flex; gap: 20px; justify-content: center; }
.passenger-btn-big {
  background: #ffffff;
  color: #000000;
  padding: 20px;
  font-size: 18px;
  border-radius: 8px;
  width: 180px;
  font-weight: bold;
  border: 2px solid #000000;
  transition: 0.2s;
}
.passenger-btn-big:hover { background: #e2e8f0; transform: translateY(-2px); }

.driver-btn-big {
  background: #000000;
  color: #eab308;
  padding: 20px;
  font-size: 18px;
  border-radius: 8px;
  width: 180px;
  font-weight: bold;
  border: 2px solid #000000;
  transition: 0.2s;
}
.driver-btn-big:hover { background: #1e293b; transform: translateY(-2px); }

/* 📋 ФОРМИ, ТЕКСТИ ТА ІНПУТИ ВСЕРЕДИНІ ЖОВТИХ КАРТОК */
.auth-form, .booking-form { display: flex; flex-direction: column; gap: 15px; text-align: left; background: none; border: none; padding: 0; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-group label { font-size: 12px; color: #000000; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; }

/* Контрастні інпути для зчитування даних */
.form-input, .form-select {
  background: #ffffff;
  border: 2px solid #000000;
  border-radius: 6px;
  color: #000000;
  padding: 10px 12px;
  font-size: 14px;
  font-weight: 600;
  outline: none;
  transition: all 0.2s;
}
.form-input:focus, .form-select:focus {
  background: #fffdf2;
  box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.15);
}
.form-input:disabled { background: #e2e8f0; color: #ebedef; cursor: not-allowed; border-color: #94a3b8; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; }

/* 💳 КНОПКИ СПОСОБУ ОПЛАТИ */
.payment-methods .btn {
  background: #eab308 !important;
  color: #000000 !important;
  border: 3px solid #000000 !important;
  font-weight: 900 !important;
  text-transform: uppercase;
  transition: all 0.2s;
}
.payment-methods .btn[style*="border-color: rgb(56, 189, 248)"],
.payment-methods .btn[style*="border-color: #38bdf8"] {
  background: #000000 !important;
  color: #eab308 !important;
  border: 3px solid #000000 !important;
}

/* ⚡ СИСТЕМНІ КНОПКИ */
.btn { padding: 11px 18px; border: none; border-radius: 6px; font-weight: 800; cursor: pointer; transition: all 0.2s; display: inline-flex; align-items: center; justify-content: center; text-transform: uppercase; font-size: 13px; }

/* Виправлено: Кнопка відправки форми, коли вона активна/неактивна */
.success-btn { background: #000000; color: #eab308; border: 3px solid #000000; }
.success-btn:hover:not(:disabled) { background: #1e293b; color: #ffffff; }

/* Виправлено: Стан, коли кнопка верифікації заблокована (Не біла, а строга темно-сіра з контуром) */
.success-btn:disabled {
  background: #374151 !important;
  color: #9ca3af !important;
  border-color: #000000 !important;
  cursor: not-allowed;
}

.primary-btn { background: #ffffff; color: #000000; border: 2px solid #000000; }
.primary-btn:hover { background: #e2e8f0; }

.danger-btn { background: #dc2626; color: #ffffff; border: 2px solid #000000; }
.danger-btn:hover { background: #b91c1c; }

.reset-btn { background: #000000; color: #ffffff; font-size: 12px; border: 2px solid #000000; }
.reset-btn:hover { background: #dc2626; color: #ffffff; }

.toggle-auth-text { font-size: 13px; color: #000000; font-weight: 700; margin-top: 15px; text-align: center; }
.toggle-auth-text span { color: #000000; cursor: pointer; text-decoration: underline; }
.auth-form a { color: #000000 !important; text-decoration: underline; }

/* 🛡️ КЕРУВАННЯ МЕТЕОУМОВАМИ */
.env-controls { background: #111827; padding: 15px; border-radius: 8px; margin-bottom: 20px; border: 2px solid #eab308; }
.env-controls h3 { margin: 0 0 10px 0; font-size: 14px; color: #eab308; text-transform: uppercase; font-weight: bold; }
.flex-row { display: flex; justify-content: space-between; align-items: center; gap: 15px; }
.label-text { font-weight: 600; color: #ffffff; }

.user-profile-banner { background: #eab308; color: #000000; padding: 12px 20px; border-radius: 8px; margin-bottom: 20px; border: 3px solid #000000; display: flex; justify-content: space-between; align-items: center; font-weight: 700; }
.counter-badge { background: #000000; padding: 6px 12px; border-radius: 4px; border: 1px solid #000000; color: #eab308; font-weight: bold; }

/* 📦 АКТИВНІ ЗАМОВЛЕННЯ ВСЕРЕДИНІ ЖОВТОЇ КАРТКИ */
.active-order-box { background: #eab308; padding: 18px; border-radius: 8px; margin-top: 15px; border: 2px solid #000000; color: #000000; }
.active-order-box p, .active-order-box strong { color: #000000; }
.price-text { font-size: 22px; color: #b45309; font-weight: 900; }

/* 🏷️ ШІ ГЕОФЕНСИНГ БЕЙДЖІ */
.tag {
  padding: 8px 14px;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 900;
  text-align: center;
  text-transform: uppercase;
  width: 100%;
  display: block;
  box-sizing: border-box;
}
.safe-tag {
  background: #eab308 !important;
  color: #000000 !important;
  border: 3px solid #000000 !important;
}
.bonus-tag {
  background: #000000 !important;
  color: #eab308 !important;
  border: 3px solid #eab308 !important;
}
.bonus-alert { background: #000000; color: #fde047; padding: 12px; border-radius: 6px; border: 2px solid #000000; font-weight: bold; margin-bottom: 15px; font-size: 13px; text-align: left; }

/* 📸 БІОМЕТРІЯ ТА DRAG & DROP СКАНИ (ФІКС: КРЕМОВО-ЖОВТИЙ КОНТРАСТНИЙ БОКС) */
.upload-zone, .drop-zone {
  border: 3px dashed #000000 !important;
  padding: 25px;
  border-radius: 8px;
  margin: 15px 0;
  background: #fffbeb !important; /* Ніжно-кремове світле тло для виділення тексту */
  text-align: center;
  color: #000000 !important;
  font-weight: 800 !important;
  font-size: 14px;
}
.success-border, .drop-zone.active {
  background: #fef08a !important; /* Яскравіший жовтий при успішному завантаженні */
}

/* Виправлено: Кнопка Face-API тепер має чіткий шрифт і чорний текст на зеленому підтвердженому стані */
.auth-card button[style*="color: rgb(52, 211, 153)"],
.auth-card button[style*="color: #34d399"] {
  background: #10b981 !important; /* Насичений зелений ШІ-колір */
  color: #000000 !important;      /* Чорний контрастний шрифт літер */
  border: 3px solid #000000 !important;
  font-weight: 900 !important;
  font-size: 13px !important;
  letter-spacing: 0.5px;
}
/* Стартовий стан кнопки Face-API (чорний з жовтим) */
.auth-card button[style*="color: rgb(56, 189, 248)"],
.auth-card button[style*="color: #38bdf8"] {
  background: #000000 !important;
  color: #eab308 !important;
  border: 3px solid #000000 !important;
  font-weight: 900 !important;
}

/* 📊 ТАБЛИЦІ ІСТОРІЇ ПОЇЗДОК */
.history-box { margin-top: 25px; }
.history-table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 13px; }
.history-table th { background: #000000; padding: 12px; text-align: left; color: #eab308; border-bottom: 2px solid #000000; font-weight: bold; }
.history-table td { padding: 12px; border-bottom: 1px solid #000000; color: #000000; font-weight: 600; }
.badge { padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: bold; text-transform: uppercase; }
.badge.completed { background: #000000; color: #ffffff; border: 1px solid #000000; }
.badge.waiting { background: #ffffff; color: #000000; border: 2px solid #000000; }
.no-data { color: #4b5563; padding: 20px; text-align: center; font-size: 13px; font-weight: bold; }

/* 🔔 ГЛОБАЛЬНІ СПОВІЩЕННЯ */
.global-alert { position: fixed; top: 20px; left: 50%; transform: translateX(-50%); background: #000000; color: #eab308; padding: 12px 30px; border-radius: 30px; font-weight: bold; border: 2px solid #eab308; box-shadow: 0 10px 20px rgba(0,0,0,0.5); z-index: 9999; }

/* 🗂️ СІТКА GRID */
.main-grid { display: grid; grid-template-columns: 1fr; gap: 25px; }
.w-full { width: 100%; }
.mt-2 { margin-top: 8px; }
.mt-3 { margin-top: 12px; }
.mt-4 { margin-top: 16px; }
.text-center { text-align: center; }
</style>

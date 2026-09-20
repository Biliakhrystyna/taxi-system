<template>
  <div class="screen-card passenger-box">
    <h2>📱 Панель пасажира (Створення замовлення)</h2>
    <div class="weather-status" style="background: rgba(30, 41, 59, 0.4); border: 1px solid #334155; padding: 10px; border-radius: 8px; margin-bottom: 15px; display: flex; align-items: center; justify-content: space-between; gap: 10px;">
      <span style="font-size: 13px; font-weight: 700; color: #000000;">{{ orderStore.destinationCoords ? 'Погода на маршруті:' : 'Поточна погода:' }}</span>
      <span v-if="orderStore.isBadWeather" style="font-size: 12px; font-weight: 800; color: #ffffff; background: #dc2626; border: 1px solid #000000; padding: 4px 10px; border-radius: 6px; text-align: right;">
        ⚠️ Небезпечно{{ orderStore.weatherReason ? ': ' + orderStore.weatherReason : '' }}
      </span>
      <span v-else style="font-size: 12px; font-weight: 800; color: #ffffff; background: #16a34a; border: 1px solid #000000; padding: 4px 10px; border-radius: 6px;">☀️ Без небезпеки, дорога суха</span>
    </div>

    <div v-if="!orderStore.currentOrder || !orderStore.currentOrder.order_id" class="booking-form">

      <div class="form-group">
        <label>📍 Звідки (Точка А):</label>
        <AddressAutocomplete
          :model-value="orderStore.pickupLocation"
          placeholder="Введіть вулицю або клікніть на мапі"
          @update:model-value="orderStore.pickupLocation = $event"
          @select="onPickupSelect"
        />
      </div>

      <div class="form-group mt-3">
        <label>🏁 Куди (Точка B):</label>
        <AddressAutocomplete
          :model-value="orderStore.destinationLocation"
          placeholder="Введіть вулицю або клікніть на мапі"
          @update:model-value="orderStore.destinationLocation = $event"
          @select="onDestinationSelect"
        />
      </div>

      <div class="form-group mt-3">
        <label>🚗 Клас автомобіля:</label>
        <select v-model="orderStore.carClass" class="form-select">
          <option value="econom">🍃 Економ</option>
          <option value="comfort">⭐ Комфорт</option>
          <option value="lux">💎 Люкс</option>
        </select>
      </div>

      <div class="form-group mt-3">
        <label>💳 Спосіб оплати:</label>
        <div class="payment-methods" style="display: flex; gap: 10px; margin-top: 5px;">
          <button type="button" class="btn" style="flex: 1; background: #0f172a; border: 1px solid #475569;" :style="orderStore.paymentMethod === 'cash' ? 'border-color: #38bdf8; background: rgba(56, 189, 248, 0.1);' : ''" @click="orderStore.paymentMethod = 'cash'">
            💵 Готівка
          </button>
          <button type="button" class="btn" style="flex: 1; background: #0f172a; border: 1px solid #475569;" :style="orderStore.paymentMethod === 'card' ? 'border-color: #38bdf8; background: rgba(56, 189, 248, 0.1);' : ''" @click="orderStore.paymentMethod = 'card'">
            💳 Картка
          </button>
        </div>
      </div>

      <div v-if="orderStore.paymentMethod === 'card'" class="form-group mt-3" style="background: rgba(30, 41, 59, 0.6); padding: 15px; border-radius: 8px; border: 1px solid #475569;">

        <div v-if="savedCards.length" style="margin-bottom: 14px;">
          <label style="display: block; margin-bottom: 6px;">💳 Збережені картки:</label>

          <label
            v-for="card in savedCards"
            :key="card.id"
            style="display: flex; align-items: center; justify-content: space-between; background: #0f172a; border: 1px solid #475569; border-radius: 6px; padding: 8px 10px; margin-bottom: 6px; cursor: pointer;"
          >
            <span style="display: flex; align-items: center; gap: 8px; color: #e2e8f0; font-size: 13px;">
              <input type="radio" :value="card.id" v-model="selectedCardId" />
              {{ card.masked_number }} (до {{ card.expiry }})
            </span>
            <button type="button" class="btn reset-btn" style="padding: 4px 8px; font-size: 11px;" @click.stop.prevent="removeSavedCard(card.id)">✕</button>
          </label>

          <label style="display: flex; align-items: center; gap: 8px; color: #e2e8f0; font-size: 13px; cursor: pointer;">
            <input type="radio" value="new" v-model="selectedCardId" />
            ➕ Нова картка
          </label>
        </div>

        <template v-if="selectedCardId === 'new'">
          <div class="form-group">
            <label>Номер картки:</label>
            <input type="text" v-model="orderStore.cardNumber" placeholder="4441 4000 0000 0000" maxlength="19" class="form-input" style="letter-spacing: 2px; text-align: center;" />
          </div>

          <div style="display: flex; gap: 10px; margin-top: 12px;">
            <div class="form-group flex-1">
              <label>Термін дії:</label>
              <input type="text" v-model="orderStore.cardExpiry" placeholder="MM/YY" maxlength="5" class="form-input input-center" />
            </div>
            <div class="form-group flex-1">
              <label>CVC/CVV:</label>
              <input type="password" v-model="orderStore.cardCvv" placeholder="•••" maxlength="3" class="form-input input-center" style="letter-spacing: 3px;" />
            </div>
          </div>

          <label style="display: flex; align-items: center; gap: 6px; margin-top: 10px; font-size: 12px; color: #e2e8f0; cursor: pointer;">
            <input type="checkbox" v-model="rememberCard" />
            💾 Запам'ятати цю картку для наступного разу
          </label>

          <div v-if="isNewCardValid" class="mt-3 text-center text-xs text-success">
            ✅ Картку та платіжні дані успішно верифіковано
          </div>
        </template>
      </div>

      <div v-if="orderStore.estimatedPrice !== null" class="form-group mt-3" style="background: rgba(15, 23, 42, 0.4); padding: 10px 14px; border-radius: 8px; border: 1px dashed #475569; display: flex; justify-content: space-between; align-items: center;">
        <span style="font-size: 13px; color: #000000; font-weight: 700;">Орієнтовна вартість поїздки:</span>
        <span style="font-size: 18px; color: #000000; font-weight: 900;">≈ {{ orderStore.estimatedPrice }} грн</span>
      </div>

      <div class="mt-4">
        <button class="btn success-btn w-full" :disabled="!orderStore.pickupLocation || !orderStore.destinationLocation" @click="submitOrder">
          Сформувати замовлення
        </button>
        <p v-if="!orderStore.pickupLocation || !orderStore.destinationLocation" class="text-center mt-2 text-xs text-black">
          * Будь ласка, оберіть Точки А та В на мапі нижче, щоб розрахувати маршрут.
        </p>
      </div>
    </div>

    <div v-if="orderStore.currentOrder && orderStore.currentOrder.order_id" class="active-order-box">
      <h3>Статус замовлення: <span class="badge" :class="orderStore.currentOrder.current_status">{{ translateOrderStatus(orderStore.currentOrder.current_status) }}</span></h3>
      <p><strong>Звідки (А):</strong> <span class="person-name">{{ orderStore.currentOrder.pickup_location }}</span></p>
      <p><strong>Куди (B):</strong> <span class="person-name">{{ orderStore.currentOrder.destination }}</span></p>
      <p><strong>Вартість поїздки:</strong> {{ orderStore.currentOrder.estimated_cost }} грн</p>
      <p><strong>Тип оплати:</strong> {{ orderStore.currentOrder.payment_method || 'Готівка' }}</p>
      <p><strong>Статус транзакції:</strong>
        <span class="badge" :class="orderStore.currentOrder.payment_status && orderStore.currentOrder.payment_status.includes('Оплачено') ? 'paid' : 'pending'">
          {{ orderStore.currentOrder.payment_status || 'Очікує оплати' }}
        </span>
      </p>

      <div class="tags-container">
        <span v-if="orderStore.currentOrder.safe_route_applied" class="tag safe-tag">🛡️ Безпечний маршрут активовано</span>
      </div>
      <p v-if="orderStore.currentOrder.driver_name" style="color: #000000; margin-top: 10px;">👨‍✈️ Призначений водій: <span class="person-name">{{ orderStore.currentOrder.driver_name }}</span></p>

      <button
        v-if="['waiting', 'accepted'].includes(orderStore.currentOrder.current_status)"
        class="btn danger-btn w-full mt-3"
        @click="orderStore.updateStatus('cancelled')"
      >
        ✖️ Скасувати замовлення
      </button>
    </div>

    <TaxiMap role="passenger" :isBadWeather="orderStore.isBadWeather" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useOrderStore } from '../store/orderStore';
import { useAuthStore } from '../../auth/store/authStore';
import { useUiStore } from '../../../shared/uiStore';
import { savedCardsApi } from '../savedCardsApi';
import TaxiMap from '../../map/components/TaxiMap.vue';
import AddressAutocomplete from '../../map/components/AddressAutocomplete.vue';
import { translateOrderStatus } from '../orderStatus';
import type { GeocodedAddress } from '../../../shared/types/geo';
import type { SavedCard } from '../../../shared/types/payment';

const orderStore = useOrderStore();
const authStore = useAuthStore();
const uiStore = useUiStore();

const onPickupSelect = (address: GeocodedAddress) => {
  orderStore.pickupLocation = address.label;
  orderStore.pickupCoords = { lat: address.lat, lng: address.lng };
};

const onDestinationSelect = (address: GeocodedAddress) => {
  orderStore.destinationLocation = address.label;
  orderStore.destinationCoords = { lat: address.lat, lng: address.lng };
};

// "Збережені картки" — зручність, не реальна платіжна інтеграція (сервер
// зберігає лише замасковані останні 4 цифри).
const savedCards = ref<SavedCard[]>([]);
const selectedCardId = ref<number | 'new'>('new');
const rememberCard = ref(false);

const loadSavedCards = async () => {
  if (!authStore.currentUser?.email) return;
  try {
    savedCards.value = await savedCardsApi.list(authStore.currentUser.email);
    if (savedCards.value.length > 0) selectedCardId.value = savedCards.value[0].id;
  } catch {
    savedCards.value = [];
  }
};

onMounted(loadSavedCards);

const removeSavedCard = async (id: number) => {
  if (!authStore.currentUser?.email) return;
  try {
    await savedCardsApi.remove(id, authStore.currentUser.email);
    savedCards.value = savedCards.value.filter((c) => c.id !== id);
    if (selectedCardId.value === id) selectedCardId.value = 'new';
  } catch {
    
  }
};

// Алгоритм Луна — стандартна контрольна сума номерів карток.
const luhnCheck = (digits: string): boolean => {
  let sum = 0;
  let shouldDouble = false;
  for (let i = digits.length - 1; i >= 0; i--) {
    let d = parseInt(digits[i], 10);
    if (shouldDouble) {
      d *= 2;
      if (d > 9) d -= 9;
    }
    sum += d;
    shouldDouble = !shouldDouble;
  }
  return sum % 10 === 0;
};

const validateNewCard = (): string | null => {
  const raw = orderStore.cardNumber.trim();

  if (!/^[\d\s]+$/.test(raw)) return 'Некоректний номер картки: лише цифри.';

  const digits = raw.replace(/\s/g, '');
  if (digits.length !== 16) return 'Некоректний номер картки: має бути 16 цифр.';
  // Луна саму послідовність однакових цифр (напр. усі нулі) вважає коректною
  // математично — тому окремо відсіє явно фейкові номери цим патерном.
  if (/^(\d)\1{15}$/.test(digits)) return 'Некоректний номер картки.';
  if (!luhnCheck(digits)) return 'Некоректний номер картки.';

  const match = orderStore.cardExpiry.match(/^(\d{2})\/(\d{2})$/);
  if (!match) return 'Некоректний термін дії картки: формат MM/YY.';

  const month = parseInt(match[1], 10);
  const year = 2000 + parseInt(match[2], 10);
  if (month < 1 || month > 12) return 'Некоректний термін дії картки: місяць має бути 01-12.';

  const now = new Date();
  const expiryEnd = new Date(year, month, 0);
  if (expiryEnd < new Date(now.getFullYear(), now.getMonth(), 1)) return 'Термін дії картки вже минув.';
  // Реальні картки не видають на десятки років уперед — це так само підозріло,
  // як і прострочена дата.
  if (expiryEnd > new Date(now.getFullYear() + 10, now.getMonth(), 1)) return 'Некоректний термін дії картки.';

  if (!/^\d{3}$/.test(orderStore.cardCvv)) return 'Некоректний CVV: має бути 3 цифри.';

  return null;
};


const isNewCardValid = computed(() => validateNewCard() === null);

const submitOrder = async () => {
  const isNewCard = orderStore.paymentMethod === 'card' && selectedCardId.value === 'new';

  if (isNewCard) {
    const validationError = validateNewCard();
    if (validationError) {
      uiStore.triggerError(validationError);
      return;
    }
  }

  const isNewCardToRemember = isNewCard && rememberCard.value && authStore.currentUser?.email;

  if (isNewCardToRemember) {
    try {
      const saved = await savedCardsApi.add(authStore.currentUser!.email, orderStore.cardNumber, orderStore.cardExpiry);
      savedCards.value.unshift(saved);
      selectedCardId.value = saved.id;
      rememberCard.value = false;
    } catch {
     
    }
  }

  if (orderStore.isBadWeather) {
    await orderStore.loadRouteComparison();
    orderStore.showAIWarning = true;
  } else {
    orderStore.createOrder();
  }
};
</script>

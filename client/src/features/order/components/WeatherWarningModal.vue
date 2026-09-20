<template>
  <div v-if="orderStore.showAIWarning" class="modal-backdrop">
    <div class="modal-card modal-card--warning">

      <div class="modal-header">
        <span class="modal-icon">🌧️</span>
        <h3 class="modal-title modal-title--warning">⚠️ Попередження Метеомоніторингу</h3>
      </div>

      <p class="modal-text modal-text--centered">
        Система виявила <b class="modal-text-danger">небезпечні погодні умови на дорозі</b>{{ orderStore.weatherReason ? ' (' + orderStore.weatherReason + ')' : '' }}.
        <template v-if="orderStore.routesCoincide">
          Об'їзд з меншою кількістю поворотів для цієї ділянки збігається із стандартним маршрутом системи. Попри це, дорога може бути слизькою: керуйте обережно.
        </template>
        <template v-else>
          Розраховано альтернативний маршрут в обхід критичних ділянок дороги.
        </template>
      </p>

      <div class="route-summary">
        <template v-if="orderStore.routesCoincide && orderStore.routeComparison">
          <div class="route-row">
            <span class="route-row-label">🛣️ Єдиний маршрут:</span>
            <span class="text-bold">{{ orderStore.routeComparison.standardKm?.toFixed(1) }} км, {{ orderStore.routeComparison.standardTurns }} поворотів</span>
          </div>
        </template>
        <template v-else-if="orderStore.routeComparison && orderStore.routeComparison.standardKm !== null && orderStore.routeComparison.safeKm !== null">
          <div class="route-row route-row--spaced">
            <span class="route-row-label">🔵 Стандартний шлях:</span>
            <span class="text-bold">{{ orderStore.routeComparison.standardKm.toFixed(1) }} км, {{ orderStore.routeComparison.standardTurns }} поворотів</span>
          </div>
          <div class="route-row route-row--safe">
            <span>🛡️ Безпечний маршрут:</span>
            <span class="text-bold">{{ orderStore.routeComparison.safeKm.toFixed(1) }} км, {{ orderStore.routeComparison.safeTurns }} поворотів</span>
          </div>
          <div v-if="orderStore.routeComparison.safeKm > orderStore.routeComparison.standardKm" class="route-note">
            +{{ ((orderStore.routeComparison.safeKm - orderStore.routeComparison.standardKm) * 1000).toFixed(0) }} м в обхід
          </div>
        </template>
        <template v-else>
          <div class="route-row route-row--spaced">
            <span class="route-row-label">🔵 Стандартний шлях:</span>
            <span class="text-bold">Найкоротший (Але є ризик злетіти з дороги)</span>
          </div>
          <div class="route-row route-row--safe">
            <span>🛡️ Безпечний маршрут:</span>
            <span class="text-bold">в обхід критичних ділянок</span>
          </div>
        </template>
      </div>

      <div v-if="orderStore.routesCoincide" class="btn-stack mt-5">
        <button class="btn btn-warning" @click="orderStore.useSafeRoute = false; orderStore.showAIWarning = false; orderStore.createOrder();">
          Зрозуміло, замовити
        </button>
        <button class="btn btn-muted" @click="orderStore.showAIWarning = false;">
          Скасувати
        </button>
      </div>

      <div v-else class="btn-stack mt-5">
        <button class="btn btn-warning" @click="orderStore.useSafeRoute = true; orderStore.showAIWarning = false; orderStore.createOrder();">
          Активувати безпечний маршрут (Рекомендовано)
        </button>

        <button class="btn btn-muted" @click="orderStore.useSafeRoute = false; orderStore.showAIWarning = false; orderStore.createOrder();">
          🚫 Ігнорувати, їхати звичайним шляхом
        </button>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { useOrderStore } from '../store/orderStore';

const orderStore = useOrderStore();
</script>

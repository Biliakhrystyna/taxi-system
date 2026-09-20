<template>
  <div v-if="orderStore.showAIWarning" class="modal-backdrop" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0, 0, 0, 0.75); display: flex; align-items: center; justify-content: center; z-index: 9999; padding: 20px;">
    <div class="modal-card" style="background: #0f172a; border: 2px solid #ef4444; padding: 25px; border-radius: 12px; max-width: 500px; width: 100%; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);">

      <div style="text-align: center; margin-bottom: 15px;">
        <span style="font-size: 40px;">🌧️</span>
        <h3 style="color: #ef4444; margin: 10px 0 0 0; font-size: 20px; font-weight: bold; text-transform: uppercase;">⚠️ Попередження Метеомоніторингу</h3>
      </div>

      <p style="color: #e2e8f0; font-size: 14px; line-height: 1.6; text-align: center;">
        Система виявила <b style="color: #f87171;">небезпечні погодні умови на дорозі</b>{{ orderStore.weatherReason ? ' (' + orderStore.weatherReason + ')' : '' }}.
        <template v-if="orderStore.routesCoincide">
          Об'їзд з меншою кількістю поворотів для цієї ділянки збігається із стандартним маршрутом системи. Попри це, дорога може бути слизькою: керуйте обережно.
        </template>
        <template v-else>
          Розраховано альтернативний маршрут в обхід критичних ділянок дороги.
        </template>
      </p>

      <div style="background: rgba(30, 41, 59, 0.6); padding: 12px; border-radius: 8px; margin: 15px 0; border: 1px solid #334155;">
        <template v-if="orderStore.routesCoincide && orderStore.routeComparison">
          <div style="display: flex; justify-content: space-between; font-size: 13px;">
            <span style="color: #38bdf8;">🛣️ Єдиний маршрут:</span>
            <span style="font-weight: bold;">{{ orderStore.routeComparison.standardKm?.toFixed(1) }} км, {{ orderStore.routeComparison.standardTurns }} поворотів</span>
          </div>
        </template>
        <template v-else-if="orderStore.routeComparison && orderStore.routeComparison.standardKm !== null && orderStore.routeComparison.safeKm !== null">
          <div style="display: flex; justify-content: space-between; font-size: 13px; margin-bottom: 5px;">
            <span style="color: #38bdf8;">🔵 Стандартний шлях:</span>
            <span style="font-weight: bold;">{{ orderStore.routeComparison.standardKm.toFixed(1) }} км, {{ orderStore.routeComparison.standardTurns }} поворотів</span>
          </div>
          <div style="display: flex; justify-content: space-between; font-size: 13px; color: #f97316;">
            <span>🛡️ Безпечний маршрут:</span>
            <span style="font-weight: bold;">{{ orderStore.routeComparison.safeKm.toFixed(1) }} км, {{ orderStore.routeComparison.safeTurns }} поворотів</span>
          </div>
          <div v-if="orderStore.routeComparison.safeKm > orderStore.routeComparison.standardKm" style="font-size: 12px; color: #94a3b8; margin-top: 6px; text-align: right;">
            +{{ ((orderStore.routeComparison.safeKm - orderStore.routeComparison.standardKm) * 1000).toFixed(0) }} м в обхід
          </div>
        </template>
        <template v-else>
          <div style="display: flex; justify-content: space-between; font-size: 13px; margin-bottom: 5px;">
            <span style="color: #38bdf8;">🔵 Стандартний шлях:</span>
            <span style="font-weight: bold;">Найкоротший (Але є ризик злетіти з дороги)</span>
          </div>
          <div style="display: flex; justify-content: space-between; font-size: 13px; color: #f97316;">
            <span>🛡️ Безпечний маршрут:</span>
            <span style="font-weight: bold;">в обхід критичних ділянок</span>
          </div>
        </template>
      </div>

      <div v-if="orderStore.routesCoincide" style="display: flex; flex-direction: column; gap: 10px; margin-top: 20px;">
        <button class="btn" style="background: #f97316; color: white; font-weight: bold; padding: 12px;" @click="orderStore.useSafeRoute = false; orderStore.showAIWarning = false; orderStore.createOrder();">
          Зрозуміло, замовити
        </button>
        <button class="btn" style="background: rgba(71, 85, 105, 0.3); color: #cbd5e1; border: 1px solid #475569; padding: 10px;" @click="orderStore.showAIWarning = false;">
          Скасувати
        </button>
      </div>

      <div v-else style="display: flex; flex-direction: column; gap: 10px; margin-top: 20px;">
        <button class="btn" style="background: #f97316; color: white; font-weight: bold; padding: 12px;" @click="orderStore.useSafeRoute = true; orderStore.showAIWarning = false; orderStore.createOrder();">
          Активувати безпечний маршрут (Рекомендовано)
        </button>

        <button class="btn" style="background: rgba(71, 85, 105, 0.3); color: #cbd5e1; border: 1px solid #475569; padding: 10px;" @click="orderStore.useSafeRoute = false; orderStore.showAIWarning = false; orderStore.createOrder();">
          🚫 Ігнорувати, їхати звичайним шляхом
        </button>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { useOrderStore } from '../../stores/orderStore';

const orderStore = useOrderStore();
</script>

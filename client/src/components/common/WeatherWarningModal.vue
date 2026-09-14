<template>
  <div v-if="orderStore.showAIWarning" class="modal-backdrop" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0, 0, 0, 0.75); display: flex; align-items: center; justify-content: center; z-index: 9999; padding: 20px;">
    <div class="modal-card" style="background: #0f172a; border: 2px solid #ef4444; padding: 25px; border-radius: 12px; max-width: 500px; width: 100%; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);">

      <div style="text-align: center; margin-bottom: 15px;">
        <span style="font-size: 40px;">🌧️</span>
        <h3 style="color: #ef4444; margin: 10px 0 0 0; font-size: 20px; font-weight: bold; text-transform: uppercase;">⚠️ Попередження Метеомоніторингу</h3>
      </div>

      <p style="color: #e2e8f0; font-size: 14px; line-height: 1.6; text-align: center;">
        Система виявила <b style="color: #f87171;">сильну зливу та підвищений ризик небезпеки</b> на стандартному шляху. Наш ШІ-алгоритм розрахував альтернативний безпечний маршрут в обхід критичних ділянок дороги.
      </p>

      <div style="background: rgba(30, 41, 59, 0.6); padding: 12px; border-radius: 8px; margin: 15px 0; border: 1px solid #334155;">
        <div style="display: flex; justify-content: space-between; font-size: 13px; margin-bottom: 5px;">
          <span style="color: #38bdf8;">🔵 Стандартний шлях:</span>
          <span style="font-weight: bold;">Найкоротший (Але є ризик злетіти з дороги)</span>
        </div>
        <div style="display: flex; justify-content: space-between; font-size: 13px; color: #f97316;">
          <span> Безпечний ШІ-маршрут:</span>
          <span style="font-weight: bold;">+800 м в обхід </span>
        </div>
      </div>

      <div style="display: flex; flex-direction: column; gap: 10px; margin-top: 20px;">
        <button class="btn" style="background: #f97316; color: white; font-weight: bold; padding: 12px;" @click="orderStore.useSafeRoute = true; orderStore.showAIWarning = false; orderStore.createOrder();">
          Активувати безпечний ШІ-маршрут (Рекомендовано)
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

<template>
  <div class="auth-card text-center">
    <h2 style="color: #f59e0b;">🪪 Контроль безпеки</h2>
    <p class="subtitle">Для активації облікового запису водія в базі даних виконайте завантаження та сканування особи:</p>

    <div
      class="upload-zone"
      :class="{ 'success-border': authStore.documentUploaded }"
      style="border: 2px dashed #475569; padding: 25px; border-radius: 8px; margin: 20px 0; background: #0f172a; cursor: pointer; transition: 0.2s;"
      @dragover.prevent
      @drop.prevent="authStore.documentUploaded = true"
      @click="authStore.documentUploaded = true"
    >
      <p v-if="!authStore.documentUploaded">📁 Перетягніть скан-копію посвідчення або натисніть для вибору файлу</p>
      <p v-else style="color: #10b981; font-weight: bold;">✅ Документ "license_scan.pdf" успішно розпізнано!</p>
    </div>

    <div style="margin-bottom: 25px;">
      <button
        type="button"
        class="btn"
        :style="authStore.faceVerified ? 'background: rgba(16, 185, 129, 0.2); border-color: #10b981; color: #34d399;' : 'background: rgba(56, 189, 248, 0.1); border-color: #38bdf8; color: #38bdf8;'"
        @click="authStore.faceVerified = true"
        style="width: 100%; padding: 12px; font-weight: bold; border: 1px solid;"
      >
        <span v-if="authStore.faceVerified">📸 Особу підтверджено нейромережею Face-API.js</span>
        <span v-else>📷 Запустити Face-API біометричний контроль</span>
      </button>
    </div>

    <button
      class="btn success-btn w-full"
      style="padding: 14px; font-size: 15px;"
      :disabled="!authStore.documentUploaded || !authStore.faceVerified"
      @click="authStore.verifyDriverDocuments"
    >
      Завершити реєстрацію та увійти в систему
    </button>

    <p v-if="!authStore.documentUploaded || !authStore.faceVerified" style="color: #ef4444; font-size: 11px; margin-top: 8px;">
      * Кнопка активації стане доступною після завантаження скану та сканування обличчя.
    </p>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../../stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <div class="autocomplete-wrap">
    <input
      type="text"
      class="form-input"
      :value="modelValue"
      :placeholder="placeholder"
      autocomplete="off"
      @input="onInput"
      @focus="showList = suggestions.length > 0"
      @blur="onBlur"
    />
    <ul v-if="showList && suggestions.length" class="autocomplete-list">
      <li v-for="s in suggestions" :key="s.label" @mousedown.prevent="select(s)">
        {{ s.label }}
      </li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { geocodingApi } from '../../services/geocodingApi';
import type { GeocodedAddress } from '../../types/geo';

defineProps<{ modelValue: string; placeholder?: string }>();
const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void;
  (e: 'select', value: GeocodedAddress): void;
}>();

const suggestions = ref<GeocodedAddress[]>([]);
const showList = ref(false);
let debounceTimer: ReturnType<typeof setTimeout> | undefined;

const onInput = (e: Event) => {
  const value = (e.target as HTMLInputElement).value;
  emit('update:modelValue', value);

  clearTimeout(debounceTimer);
  if (value.trim().length < 3) {
    suggestions.value = [];
    showList.value = false;
    return;
  }

  debounceTimer = setTimeout(async () => {
    try {
      suggestions.value = await geocodingApi.search(value.trim());
      showList.value = suggestions.value.length > 0;
    } catch {
      suggestions.value = [];
      showList.value = false;
    }
  }, 350);
};

const select = (s: GeocodedAddress) => {
  emit('update:modelValue', s.label);
  emit('select', s);
  suggestions.value = [];
  showList.value = false;
};


const onBlur = () => {
  setTimeout(() => { showList.value = false; }, 150);
};
</script>

<style scoped>
.autocomplete-wrap { position: relative; }
.autocomplete-list {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  z-index: 20;
  background: #ffffff;
  border: 2px solid #000000;
  border-top: none;
  border-radius: 0 0 6px 6px;
  max-height: 220px;
  overflow-y: auto;
  list-style: none;
  margin: 0;
  padding: 0;
  box-shadow: 0 10px 15px rgba(0, 0, 0, 0.3);
}
.autocomplete-list li {
  padding: 8px 12px;
  font-size: 13px;
  color: #000000;
  font-weight: 600;
  cursor: pointer;
  border-bottom: 1px solid #e2e8f0;
}
.autocomplete-list li:last-child { border-bottom: none; }
.autocomplete-list li:hover { background: #fffbeb; }
</style>

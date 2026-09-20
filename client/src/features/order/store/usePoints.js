import { ref } from 'vue';

/** Точки А/Б маршруту пасажира: адреси й координати. */
export function usePoints() {
  const pickupLocation = ref('');
  const destinationLocation = ref('');
  /** @type {import('vue').Ref<import('../../../shared/types/geo').LatLng | null>} */
  const pickupCoords = ref(null);
  /** @type {import('vue').Ref<import('../../../shared/types/geo').LatLng | null>} */
  const destinationCoords = ref(null);

  const clearPoints = () => {
    pickupLocation.value = '';
    destinationLocation.value = '';
    pickupCoords.value = null;
    destinationCoords.value = null;
  };

  return { pickupLocation, destinationLocation, pickupCoords, destinationCoords, clearPoints };
}

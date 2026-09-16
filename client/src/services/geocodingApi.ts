import { http } from './http';
import type { GeocodedAddress } from '../types/geo';

export const geocodingApi = {
  /** Координати → людська адреса (для кліку на мапі). */
  reverse: (lat: number, lng: number) => http.get<GeocodedAddress | null>(`/api/geocoding/reverse?lat=${lat}&lng=${lng}`),
  /** Автопідказки адрес за текстом пошуку. */
  search: (query: string) => http.get<GeocodedAddress[]>(`/api/geocoding/search?query=${encodeURIComponent(query)}`),
};

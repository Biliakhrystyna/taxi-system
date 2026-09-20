import { http } from './http';
import type { GeocodedAddress } from '../types/geo';

export const geocodingApi = {
  
  reverse: (lat: number, lng: number) => http.get<GeocodedAddress | null>(`/api/geocoding/reverse?lat=${lat}&lng=${lng}`),
  
  search: (query: string) => http.get<GeocodedAddress[]>(`/api/geocoding/search?query=${encodeURIComponent(query)}`),
};

import { http } from '../../shared/http';
import type { GeocodedAddress } from '../../shared/types/geo';

export const geocodingApi = {
  
  reverse: (lat: number, lng: number) => http.get<GeocodedAddress | null>(`/api/geocoding/reverse?lat=${lat}&lng=${lng}`),
  
  search: (query: string) => http.get<GeocodedAddress[]>(`/api/geocoding/search?query=${encodeURIComponent(query)}`),
};

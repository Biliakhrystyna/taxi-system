import { http } from '../../shared/http';
import type { ApiOrder, OrderStatus } from '../../shared/types/order';
import type { UserRole } from '../../shared/types/user';

export interface CreateOrderPayload {
  passenger_email: string;
  pickup_location: string;
  destination: string;
  car_class: string;
  payment_method: 'cash' | 'card';
  is_bad_weather: boolean;
  safe_route_applied: boolean;
  safe_route_matches_standard?: boolean;
  pickup_lat?: number | null;
  pickup_lng?: number | null;
  destination_lat?: number | null;
  destination_lng?: number | null;
}

export interface UpdateOrderStatusPayload {
  new_status: OrderStatus;
  driver_email: string | null;
  driver_name: string | null;
}

export interface RateOrderPayload {
  passenger_email: string;
  rating: number;
}

export interface QuotePayload {
  car_class: string;
  is_bad_weather: boolean;
  safe_route_applied: boolean;
  safe_route_matches_standard: boolean;
  pickup_lat?: number | null;
  pickup_lng?: number | null;
  destination_lat?: number | null;
  destination_lng?: number | null;
}

export interface QuoteResult {
  estimated_cost: number;
}

export const orderApi = {
  create: (payload: CreateOrderPayload) => http.post<ApiOrder>('/api/orders', payload),

  getCurrent: (email: string, role: UserRole) =>
    http.get<ApiOrder | null>(`/api/orders/current?email=${encodeURIComponent(email)}&role=${role}`),

  getHistory: (email: string, role: UserRole) =>
    http.get<ApiOrder[]>(`/api/orders/history?email=${encodeURIComponent(email)}&role=${role}`),

  updateStatus: (orderId: string, payload: UpdateOrderStatusPayload) =>
    http.post<ApiOrder>(`/api/orders/${encodeURIComponent(orderId)}/status`, payload),

  rateOrder: (orderId: string, payload: RateOrderPayload) =>
    http.post<ApiOrder>(`/api/orders/${encodeURIComponent(orderId)}/rate`, payload),

  getQuote: (payload: QuotePayload) => {
    const params = new URLSearchParams({
      carClass: payload.car_class,
      isBadWeather: String(payload.is_bad_weather),
      safeRouteApplied: String(payload.safe_route_applied),
      safeRouteMatchesStandard: String(payload.safe_route_matches_standard),
    });
    if (payload.pickup_lat != null) params.set('pickupLat', String(payload.pickup_lat));
    if (payload.pickup_lng != null) params.set('pickupLng', String(payload.pickup_lng));
    if (payload.destination_lat != null) params.set('destinationLat', String(payload.destination_lat));
    if (payload.destination_lng != null) params.set('destinationLng', String(payload.destination_lng));
    return http.get<QuoteResult>(`/api/orders/quote?${params.toString()}`);
  },
};

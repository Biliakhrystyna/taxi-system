import { http } from './http';
import type { ApiOrder, DemandZoneName, OrderStatus } from '../types/order';
import type { UserRole } from '../types/user';

export interface CreateOrderPayload {
  passenger_email: string;
  pickup_location: string;
  destination: string;
  car_class: string;
  payment_method: 'cash' | 'card';
  zone: DemandZoneName;
  is_bad_weather: boolean;
  safe_route_applied: boolean;
}

export interface UpdateOrderStatusPayload {
  new_status: OrderStatus;
  driver_email: string | null;
  driver_name: string | null;
}

export const orderApi = {
  create: (payload: CreateOrderPayload) => http.post<ApiOrder>('/api/orders', payload),

  getCurrent: (email: string, role: UserRole) =>
    http.get<ApiOrder | null>(`/api/orders/current?email=${encodeURIComponent(email)}&role=${role}`),

  getHistory: (email: string, role: UserRole) =>
    http.get<ApiOrder[]>(`/api/orders/history?email=${encodeURIComponent(email)}&role=${role}`),

  updateStatus: (orderId: string, payload: UpdateOrderStatusPayload) =>
    http.post<ApiOrder>(`/api/orders/${encodeURIComponent(orderId)}/status`, payload),
};

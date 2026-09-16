export type OrderStatus = 'waiting' | 'accepted' | 'in_progress' | 'completed' | 'cancelled';
export type DemandZoneName = 'center' | 'outskirts';

/** Форма відповіді бекенду (`OrderResponse` у server/TaxiSystem.Api/DTOs/OrderDtos.cs). */
export interface ApiOrder {
  order_id: string;
  passenger_email: string;
  passenger_name: string;
  pickup_location: string;
  destination: string;
  car_class: string;
  estimated_cost: number;
  motivation_bonus: number;
  weather_hazard_level: 'HIGH' | 'NORMAL';
  current_status: OrderStatus;
  zone: DemandZoneName;
  safe_route_applied: boolean;
  payment_method: string;
  payment_status: string;
  driver_email: string | null;
  driver_name: string | null;
  end_time: string | null;
}

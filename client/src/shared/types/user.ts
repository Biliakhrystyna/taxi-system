export type UserRole = 'passenger' | 'driver';
export type AccountStatus = 'active' | 'pending_verification';
export type DriverCarClass = 'econom' | 'comfort' | 'lux';

/** Форма відповіді бекенду (`UserResponse` у server/TaxiSystem.Api/DTOs/AuthDtos.cs). */
export interface ApiUser {
  email: string;
  first_name: string;
  last_name: string;
  phone: string;
  /** Роль цього конкретного входу/реєстрації — не єдина роль акаунту. */
  role: UserRole;
  status: AccountStatus;
  email_confirmed: boolean;
  total_trips: number;
  license_number: string | null;
  driver_car_class: DriverCarClass | null;
  is_passenger: boolean;
  is_driver: boolean;
}

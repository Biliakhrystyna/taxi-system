export type UserRole = 'passenger' | 'driver';
export type AccountStatus = 'active' | 'pending_verification';
export type DriverCarClass = 'econom' | 'comfort' | 'lux';

/** Форма відповіді бекенду (`UserResponse` у server/TaxiSystem.Api/DTOs/AuthDtos.cs). */
export interface ApiUser {
  email: string;
  first_name: string;
  last_name: string;
  phone: string;
  role: UserRole;
  status: AccountStatus;
  total_trips: number;
  license_number: string | null;
  driver_car_class: DriverCarClass | null;
}

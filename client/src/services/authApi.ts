import { http } from './http';
import type { ApiUser, UserRole, DriverCarClass } from '../types/user';

export interface RegisterPayload {
  email: string;
  password: string;
  first_name: string;
  last_name: string;
  phone: string;
  role: UserRole;
  license_number: string | null;
  driver_car_class: DriverCarClass | null;
}

export interface LoginPayload {
  email: string;
  password: string;
  role: UserRole;
}

export const authApi = {
  register: (payload: RegisterPayload) => http.post<ApiUser>('/api/auth/register', payload),
  login: (payload: LoginPayload) => http.post<ApiUser>('/api/auth/login', payload),
  verifyDriver: (email: string) => http.post<ApiUser>(`/api/auth/verify-driver/${encodeURIComponent(email)}`),
};

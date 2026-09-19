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
  identifier: string;
  password: string;
  role: UserRole;
}

export interface VerifyEmailPayload {
  email: string;
  code: string;
}

export interface ResetPasswordPayload {
  email: string;
  code: string;
  new_password: string;
}

export const authApi = {
  register: (payload: RegisterPayload) => http.post<ApiUser>('/api/auth/register', payload),
  login: (payload: LoginPayload) => http.post<ApiUser>('/api/auth/login', payload),
  verifyDriver: (email: string) => http.post<ApiUser>(`/api/auth/verify-driver/${encodeURIComponent(email)}`),
  verifyEmail: (payload: VerifyEmailPayload) => http.post<ApiUser>('/api/auth/verify-email', payload),
  resendVerification: (email: string) => http.post<void>('/api/auth/resend-verification', { email }),
  forgotPassword: (email: string) => http.post<void>('/api/auth/forgot-password', { email }),
  resetPassword: (payload: ResetPasswordPayload) => http.post<ApiUser>('/api/auth/reset-password', payload),
};

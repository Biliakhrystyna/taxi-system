import { http } from '../../shared/http';
import type { SavedCard } from '../../shared/types/payment';

export const savedCardsApi = {
  list: (email: string) => http.get<SavedCard[]>(`/api/saved-cards?email=${encodeURIComponent(email)}`),
  add: (email: string, cardNumber: string, expiry: string) =>
    http.post<SavedCard>('/api/saved-cards', { email, card_number: cardNumber, expiry }),
  remove: (id: number, email: string) => http.del<void>(`/api/saved-cards/${id}?email=${encodeURIComponent(email)}`),
};

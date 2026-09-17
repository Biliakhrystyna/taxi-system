import type { OrderStatus } from '../types/order';

const STATUS_LABELS: Record<OrderStatus, string> = {
  waiting: 'Очікує водія',
  accepted: 'Водій прийняв',
  in_progress: 'В дорозі',
  completed: 'Завершено',
  cancelled: 'Скасовано',
};

/** Людський переклад статусу замовлення для відображення (клас .badge лишається англійським — це CSS-хук). */
export const translateOrderStatus = (status: OrderStatus): string => STATUS_LABELS[status] ?? status;

import type { OrderStatus } from '../../shared/types/order';

const STATUS_LABELS: Record<OrderStatus, string> = {
  waiting: 'Очікує водія',
  accepted: 'Водій прийняв',
  in_progress: 'В дорозі',
  completed: 'Завершено',
  cancelled: 'Скасовано',
};


export const translateOrderStatus = (status: OrderStatus): string => STATUS_LABELS[status] ?? status;

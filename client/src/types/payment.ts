/** "Збережена" демо-картка — лише маска, повний номер сервер не зберігає. */
export interface SavedCard {
  id: number;
  masked_number: string;
  expiry: string;
}

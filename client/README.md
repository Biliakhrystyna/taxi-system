# Taxi System: клієнт

Vue 3 + TypeScript + Pinia + Vite, мапа на Leaflet. Працює разом із сервером з `../server` (див. головний [README](../README.md)).

## Команди

```sh
npm install     # встановити залежності
npm run dev     # запуск для розробки (http://localhost:5173)
npm run build   # збірка для продакшену
```

Адреса сервера задається змінною `VITE_API_BASE_URL` у `.env.local` (за замовчуванням `http://localhost:5080`).

## Структура `src/`

```
features/
  auth/    вхід, реєстрація, верифікація водія
  order/   замовлення, кабінети пасажира й водія, історія, погода
  map/     мапа, автопідказки адрес, побудова маршрутів
shared/    спільне: http-клієнт, config, повідомлення (uiStore), типи
assets/    стилі (app.css підключає файли з styles/)
```

У кожній фічі: `components/` (екрани), `store/` (стан Pinia), файли `*Api.ts` (запити до сервера).

## Рекомендоване середовище

[VS Code](https://code.visualstudio.com/) + [Vue (Official)](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (Vetur вимкнути).

# Taxi System

Навчальний проєкт (виробнича практика): асинхронна клієнт-серверна система замовлення таксі.

- **Клієнт:** Vue 3 + TypeScript + Pinia + Leaflet.js
- **Сервер:** ASP.NET Core Web API (у розробці) + SignalR + BackgroundService
- **Зовнішні сервіси:** Open-Meteo REST API (прогноз погоди)
- **Безпека:** хешування паролів BCrypt
- **Методологія розробки:** Kanban (дошка задач — див. `docs/kanban-board.md`)
- **VCS:** Git / GitHub

## Структура репозиторію

```
client/   — фронтенд (Vue 3 + TS + Leaflet)
server/   — бекенд (ASP.NET Core Web API), у процесі розробки
docs/     — архітектурні нотатки, аналіз, Kanban-дошка
```

## Розробка

```sh
cd client
npm install
npm run dev
```

Бекенд (`server/`) буде додано поетапно — див. `docs/ARCHITECTURE.md` та `docs/kanban-board.md`.

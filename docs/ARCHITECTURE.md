# Архітектура

## Технологічний стек (чинне ТЗ практики)

| Шар | Технологія |
|---|---|
| Клієнт | Vue 3 + TypeScript + Pinia + Leaflet.js (HTML5/CSS3) |
| Сервер | ASP.NET Core Web API (у розробці) |
| Реальний час | SignalR Hub |
| Фонова обробка | ASP.NET Core `BackgroundService` |
| Зовнішній сервіс | Open-Meteo REST API (прогноз погоди) |
| Безпека паролів | BCrypt |
| Кешування на клієнті | `localStorage` |
| БД | реляційна, EF Core (провайдер — окреме рішення) |
| Методологія | Kanban (див. `kanban-board.md`) |
| VCS | Git / GitHub |

> Історія обговорення варіанту з Angular та OpenRouteService — у
> `ANALIZ-ANGULAR-DOTNET.md`. Чинна реалізація — Vue.js, без OpenRouteService.

## Схема взаємодії (цільова)

```
┌─────────────┐   REST (JWT)    ┌──────────────────────┐
│   Vue client │ ───────────────▶│  ASP.NET Core Web API │
│  (client/)   │◀─────────────── │      (server/)        │
└──────┬───────┘   SignalR WS    └──────────┬────────────┘
       │                                     │
       │ Leaflet.js                          │ EF Core
       │ (геодані з бекенду)                 ▼
       │                          ┌───────────────────────┐
       │                          │  Реляційна БД          │
       │                          │  Users / Drivers /     │
       │                          │  Orders / Trips        │
       │                          └───────────────────────┘
       │                                     ▲
       │                          BackgroundService
       │                          (DemandZoneCalculatorService)
       │                                     │
       │                          Open-Meteo REST API
       └─────────────────────────────────────┘
```

## Клієнт (`client/`) — поточний стан

Реалізовано (Kanban-картки цієї ітерації):
- `stores/authStore.ts`, `orderStore.ts`, `uiStore.ts` — розділені за відповідальністю
  (наразі `.js`, конвертація в `.ts` — окрема картка backlog).
- `components/map/geo/` — препроцесинг і фільтрація геопросторових даних
  (`zonePreprocessor.ts`, `geoFilter.ts`, `routeBuilder.ts`), TypeScript.
- Компоненти по ролях/екранах (`components/auth`, `components/passenger`,
  `components/driver`, `components/history`, `components/common`).

Дані наразі йдуть через Firebase Firestore (успадковано з прототипу) —
буде замінено REST + SignalR у картках нижче.

## Сервер (`server/`) — планова структура

```
server/src/
├── TaxiSystem.Api/              # Controllers, Program.cs, Hubs/, BackgroundServices/
├── TaxiSystem.Application/      # Services, DTO, BCrypt-хешування, Open-Meteo клієнт
├── TaxiSystem.Domain/           # Entities: User, Driver, Order, Trip
└── TaxiSystem.Infrastructure/   # EF Core DbContext, репозиторії
```

Ще не реалізовано — картки в `kanban-board.md`.

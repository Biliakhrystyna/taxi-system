# Kanban-дошка

Дошка ведеться як GitHub Project (Backlog → To Do → In Progress → In Review → Done),
WIP-ліміт для «In Progress» — 2 картки. Кожен комміт посилається на номер картки/issue.

## Done (ця ітерація — рефакторинг клієнта)

- [x] Скаффолдинг монорепо (`client/`, `server/`, `docs/`)
- [x] App.vue розбито на компоненти по ролях/екранах
- [x] `orderStore.js` розділено на `authStore` / `orderStore` / `uiStore`
- [x] Клієнт переведено на TypeScript (tooling + компоненти)
- [x] Модуль препроцесингу/фільтрації геоданих карти (`components/map/geo/`)

## Backlog (наступні картки)

### Бекенд
- [ ] Підняти ASP.NET Core Web API solution (`TaxiSystem.Api/Application/Domain/Infrastructure`)
- [ ] EF Core: сутності `User`, `Driver`, `Order`, `Trip` + міграції
- [ ] BCrypt-хешування паролів + `AuthController` (реєстрація/логін)
- [ ] JWT-автентифікація + ролі (`passenger`/`driver`/`admin`)
- [ ] SignalR `OrderHub` — статус замовлення й позиції водіїв у реальному часі
- [ ] `BackgroundService` для розрахунку зон дефіциту (заміна `zonePreprocessor.ts`-рандому
      на реальні дані з бекенду через SignalR)
- [ ] Інтеграція Open-Meteo (forecast) — предиктивний аналіз погоди
- [ ] Роль `admin` (передбачено ТЗ окремо не згадана, уточнити обсяг)

### Клієнт
- [ ] `authStore`/`orderStore`/`uiStore` — конвертація `.js` → `.ts`, типізація
- [ ] `services/http.ts` + `authApi.ts`/`orderApi.ts` — REST-шар замість Firestore
- [ ] `services/signalr/orderHubConnection.ts` — підключення до `OrderHub`
- [ ] `utils/localCache.ts` — кешування на клієнті (`localStorage`)
- [ ] Видалення Firebase SDK/Firestore після переходу на REST+SignalR

### Якість
- [ ] Базові xUnit-тести (`AuthService`, BCrypt-хешування)
- [ ] `npm run type-check` у CI (GitHub Actions) — опційно

## Примітка щодо BCrypt / .NET SDK

У середовищі розробки цієї сесії не встановлено .NET SDK — файли бекенду
писатимуться вручну та компілюватимуться/перевірятимуться на машині, де
SDK є. Перед першою карткою бекенду варто перевірити `dotnet --version`.

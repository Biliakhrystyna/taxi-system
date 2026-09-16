# Kanban-дошка

Дошка ведеться як GitHub Project (Backlog → To Do → In Progress → In Review → Done),
WIP-ліміт для «In Progress» — 2 картки. Кожен комміт посилається на номер картки/issue.

Нижче — журнал того, що вже зроблено (можна один-в-один переносити як закриті
картки з датою), і що лишилось у Backlog. Заголовки сформульовані як готові
назви issue/карток.

## Done

### Ітерація 1 — рефакторинг клієнта
- [x] Скаффолдинг монорепо (`client/`, `server/`, `docs/`)
- [x] Розбити `App.vue` на компоненти по ролях/екранах (auth/passenger/driver/history/common)
- [x] Розділити `orderStore.js` на `authStore` / `orderStore` / `uiStore`
- [x] Перевести клієнт на TypeScript (tooling: tsconfig/vite/package.json + компоненти)
- [x] Винести модуль препроцесингу/фільтрації геоданих карти (`components/map/geo/*.ts`)
- [x] Написати архітектурний опис і Kanban-дошку (`docs/ARCHITECTURE.md`, цей файл)
- [x] Знайти й виправити відсутню залежність `pinia` в `package.json`

### Ітерація 2 — бекенд ASP.NET Core Web API
- [x] Встановити .NET SDK 8 на машину розробки
- [x] Підняти проєкт `TaxiSystem.Api` (net8.0) + EF Core + SQLite
- [x] Створити сутності `User`, `Order` (одна таблиця замість `Orders`+`Trips`)
- [x] Реалізувати BCrypt-хешування паролів (`PasswordHasher`)
- [x] `AuthController` — реєстрація / логін / верифікація водія
- [x] `OrdersController` — створення (тариф рахує сервер) / поточне замовлення / статус / історія
- [x] `OrderHub` (SignalR) — push `OrderUpdated` / `ZonesUpdated`
- [x] `DemandZoneCalculatorService` (`BackgroundService`) — перерахунок зон попиту раз на 60с
- [x] Інтеграція Open-Meteo (`OpenMeteoClient` + `WeatherController`) — опади зараз + найближча
      година (`current` + `minutely_15`), не прогноз на добу наперед
- [x] Глобальна JSON naming policy `snake_case` (REST + SignalR) — щоб не переписувати
      клієнтські шаблони під camelCase
- [x] Локальна перевірка: `dotnet build` (0 помилок), `dotnet run`, ручний smoke-тест
      усіх ендпоінтів через `curl` (weather, register/login, create/current order)

## Backlog (наступні картки)

### Клієнт ↔ бекенд
- [ ] `services/http.ts` — базовий REST-клієнт
- [ ] `services/authApi.ts` + переписати `authStore.ts`: Firebase → REST (`/api/auth/*`)
- [ ] `services/orderApi.ts` + переписати `orderStore.ts`: Firebase → REST (`/api/orders/*`)
- [ ] `services/signalr/orderHubConnection.ts` — підключення до `/hubs/orders`,
      обробка `OrderUpdated`/`ZonesUpdated`
- [ ] `services/weatherApi.ts` — виклик `/api/weather/forecast` при формуванні замовлення
- [ ] Прибрати Firebase SDK / `firebase.js` / Firestore-залежність з `package.json`
- [ ] Наскрізне тестування повного флоу (реєстрація → замовлення → прийняв → завершив → історія)

### Опційно, якщо лишиться час
- [ ] `authStore`/`orderStore`/`uiStore` — конвертація `.js` → `.ts`
- [ ] `utils/localCache.ts` — кешування на клієнті (`localStorage`)
- [ ] Базові xUnit-тести (`PasswordHasher`, розрахунок тарифу)

## Свідомо поза обсягом цієї практики (не заводити картки)

Рішення від 2026-09-15, продиктовано дедлайном 4 дні — див. `ARCHITECTURE.md`:

- JWT-автентифікація / ролі `[Authorize]` — ТЗ вимагає лише BCrypt, не токени
- Роль `admin` + чат — велика фіча, не згадана буквально в ТЗ
- OpenRouteService / реальна маршрутизація в обхід зон — синусоїда лишається візуалізацією-симуляцією
- Рейтинги/відгуки, окрема сутність `Payments`
- EF Core міграції (лишається `EnsureCreated()`), Docker-compose, CI, юніт-тести — можна
  згадати в звіті як "плани на майбутнє"

---

## Ітерація 3 — хостинг-готовність БД і реальна безпечна маршрутизація

> Рішення від 2026-09-15: два пункти вище ("OpenRouteService / реальна маршрутизація" та
> "EF Core міграції") виявились актуальними раніше, ніж очікувалось — беремо в роботу.
> Записи вище навмисно не редагуються (історичний журнал), цей блок їх доповнює.

### Done
- [x] Перехід БД з SQLite на PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL`,
      `EnsureCreated()` → `Database.Migrate()` + перша міграція `InitialCreate`.
      Перевірено наживо в тимчасовому Docker-контейнері `postgres:16-alpine`:
      міграція застосувалась, register/login відпрацювали, bcrypt-хеш підтверджено
      напряму в таблиці `Users` (`$2a$11$...`). Змінені файли: `Program.cs`,
      `TaxiSystem.Api.csproj`, `appsettings.json`, `Migrations/*`, `.config/dotnet-tools.json`.

### In Progress / Backlog
- [x] Реальна безпечна маршрутизація через OpenRouteService (`alternative_routes`) замість
      синусоїди в `routeBuilder.ts` — обирає альтернативу з найменшою кількістю поворотів
      за типом кроку (`steps[].type`: Left/Right/Sharp*/roundabout/U-turn), не за кутом
      на сирій геометрії. Гілка `feature/ors-safe-routing`, ще не змержена в `main`.
      Перевірено наживо реальним ключем ORS на координатах Львова — `turn_count`
      збігається з ручним підрахунком по сирій відповіді API. Клієнт падає на стару
      симуляцію (`buildSimulatedSafeRoute`), якщо ORS недоступний/без ключа.
- [ ] (для хостингу, окремо від коду) обрати провайдера PostgreSQL для продакшн-БД
      (Supabase / Neon / Railway / Render) і підключити рядок з'єднання через
      конфігурацію хостингу, а не `appsettings.json`.

---

## Ітерація 4 — геокодування, реальна маршрутизація "стандарту", скасування, збережені картки

### Done
- [x] Геокодування адрес (`OpenRouteServiceGeocodingClient` + `GeocodingController`):
      автопідказки вулиць при вводі (`AddressAutocomplete.vue`) і зворотне геокодування
      при кліку на мапі — замість сирих координат у полях "Звідки"/"Куди".
- [x] Стандартний (не лише безпечний) маршрут тепер теж реальний, по дорогах
      (`GetFastestRouteAsync`, ендпоінт `/api/routing/route`) — раніше малювалась
      пряма лінія "навпростець" по мапі.
- [x] `OpenMeteoClient` аналізує ширший спектр небезпечної погоди: опади, замерзаючі
      опади (weather_code ожеледиці) і низьку температуру (ризик інею), а не лише
      "чи йде дощ".
- [x] Скасування замовлення пасажиром (`current_status = "cancelled"`), коректно
      виключене з "активних" на боці водія, потрапляє в історію поїздок. Заодно
      виправлено подвійний інкремент лічильника поїздок водія (локальний +
      SignalR-відлуння того самого запиту рахували двічі).
- [x] "Збережені картки" (`SavedCardsController`) — виключно зручність, не реальна
      платіжна інтеграція: сервер зберігає лише замасковані останні 4 цифри й термін
      дії, повний номер/CVV на диск не потрапляють.
- [x] Валідація реєстрації: телефон рівно 10 цифр, пароль від 8 символів — через
      стилізований банер помилки, без нативних браузерних popup.
- [x] Банер помилок перефарбовано під чорно-жовту тему сайту (був червоний).
      Дрібні текстові фікси: переклад статусів замовлення на українську, прибрано
      "в базі" з лічильника поїздок, виправлено випадковий ієрогліф у тексті.

Все на гілці `feature/ors-safe-routing`, ще не змержено в `main`.

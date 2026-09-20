# Taxi System

Навчальний проєкт (виробнича практика): клієнт-серверна система замовлення таксі з реальним маршрутом, прогнозом погоди й оновленнями в реальному часі.

## Технології

- **Клієнт:** Vue 3 + TypeScript + Pinia + Vite, мапа на Leaflet
- **Сервер:** ASP.NET Core 8 Web API, SignalR, фонові сервіси (BackgroundService)
- **База даних:** PostgreSQL (Entity Framework Core, міграції застосовуються при старті)
- **Зовнішні сервіси:**
  - Open-Meteo: прогноз погоди й небезпечних умов на дорозі
  - OpenRouteService: маршрути по дорогах, безпечний маршрут із мінімумом поворотів, геокодинг адрес
  - Nominatim (OpenStreetMap): резервний геокодинг
  - SendGrid: лист із кодом підтвердження пошти
- **Безпека:** хешування паролів BCrypt, підтвердження пошти кодом
- **Деплой:** Railway
- **Методологія:** Kanban (дошка в GitHub Projects)
- **VCS:** Git / GitHub

## Можливості

- Реєстрація й вхід за email або телефоном, підтвердження пошти кодом, відновлення пароля
- Один акаунт може мати дві ролі (пасажир і водій). Одночасно в обох ролях працювати не можна: сервер не дозволяє замовляти, поки триває поїздка водієм, і навпаки, а друга вкладка з іншою роллю виходить автоматично
- Верифікація водія (посвідчення, біометрія), клас авто водія
- Замовлення з пошуком адрес, вибором точок на мапі, реальним маршрутом і ціною за відстань, класом авто й погодою
- Попередження про небезпечну погоду на маршруті й безпечний маршрут в обхід критичних ділянок
- Оплата карткою або готівкою, збережені картки
- Прийняття замовлень водієм, оновлення статусів у реальному часі (SignalR), скасування
- Історія поїздок, оцінка поїздки пасажиром, лічильники поїздок за ролями

## Структура репозиторію

```
client/   фронтенд (Vue 3 + TS)
server/   бекенд (ASP.NET Core Web API)
```

### Клієнт (`client/src/`)

Код розділений за фічами:

```
features/
  auth/    вхід, реєстрація, верифікація: components/, store/, authApi.ts, authValidation.js
  order/   замовлення й поїздки: components/, store/, orderApi.ts, погода, SignalR
  map/     мапа, пошук адрес, маршрути: components/, geocodingApi.ts, routingApi.ts
shared/    спільне: http.ts, config.ts, uiStore.js, types/
assets/    стилі: app.css підключає файли з styles/ (змінні кольорів, кнопки, форми, модалки)
```

### Сервер (`server/TaxiSystem.Api/`)

```
Controllers/         HTTP-ендпоїнти (Auth, Orders, Weather, Routing, Geocoding, SavedCards)
DTOs/                моделі запитів і відповідей
Models/              таблиці БД, статуси замовлень, ролі
Data/                AppDbContext і міграції
Services/            клієнти зовнішніх API, розрахунок ціни (FareCalculator), допоміжна логіка
Hubs/                SignalR (OrderHub)
BackgroundServices/  фоновий моніторинг погоди
```

## Запуск локально

Потрібні: Node.js, .NET 8 SDK, PostgreSQL.

### 1. База даних

Створи порожню базу PostgreSQL (наприклад, `taxi`). Міграції застосовуються автоматично при старті сервера. Рядок підключення в репозиторії не зберігається, його задають у user-secrets (див. нижче).

### 2. Сервер

Ключі зберігаються в user-secrets і в репозиторій не потрапляють:

```sh
cd server/TaxiSystem.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=taxi;Username=<користувач>;Password=<пароль>"
dotnet user-secrets set "OpenRouteService:ApiKey" "<ключ>"
dotnet user-secrets set "SendGrid:ApiKey" "<ключ>"
dotnet user-secrets set "SendGrid:FromEmail" "<адреса відправника>"
dotnet run --launch-profile TaxiSystem.Api
```

Сервер стартує на `http://localhost:5080`. Без ключа SendGrid лист із кодом не відправляється, а без ключа OpenRouteService маршрути будуються наближено (пряма лінія).

### 3. Клієнт

```sh
cd client
npm install
npm run dev
```

Клієнт відкривається на `http://localhost:5173`. Адреса сервера задається змінною `VITE_API_BASE_URL` (файл `client/.env.local`), за замовчуванням `http://localhost:5080`.

## Змінні середовища сервера (деплой)

| Змінна | Призначення |
|---|---|
| `DATABASE_URL` | рядок підключення PostgreSQL (`postgresql://user:pass@host:port/db`); на Railway підставляється з підключеної бази, має пріоритет над `ConnectionStrings:Default` |
| `PORT` | порт, який віддає хостинг |
| `Client__Origin` | адреса(и) клієнта для CORS, через кому |
| `OpenRouteService__ApiKey` | ключ OpenRouteService |
| `SendGrid__ApiKey`, `SendGrid__FromEmail` | налаштування пошти |

Сервер збирається на Railway з папки `server/`.

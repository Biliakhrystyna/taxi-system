# server/ — ASP.NET Core Web API

Один проєкт `TaxiSystem.Api` (без розбиття на окремі csproj-шари — свідоме
спрощення заради швидкості; логічні шари лишились як папки: `Controllers/`,
`Services/`, `Data/`, `Models/`, `Hubs/`, `BackgroundServices/`, `DTOs/`).

## Запуск

```sh
cd server/TaxiSystem.Api
dotnet restore
dotnet run
```

API піднімається на `http://localhost:5080` (порт зафіксований у
`Properties/launchSettings.json`). БД — файл SQLite `taxi.db`, створюється
автоматично при першому старті (`Database.EnsureCreated()`, без EF-міграцій).

## Ендпоінти

| Метод | Шлях | Опис |
|---|---|---|
| POST | `/api/auth/register` | реєстрація (BCrypt-хешування пароля) |
| POST | `/api/auth/login` | вхід |
| POST | `/api/auth/verify-driver/{email}` | активація акаунту водія після біометрії |
| POST | `/api/orders` | створити замовлення (сервер рахує тариф) |
| GET | `/api/orders/current?email=&role=` | поточне активне замовлення |
| GET | `/api/orders/history?email=&role=` | завершені поїздки |
| POST | `/api/orders/{orderId}/status` | оновити статус замовлення |
| GET | `/api/weather/forecast?lat=&lng=` | прогноз погоди (Open-Meteo), Львів за замовчуванням |
| WS | `/hubs/orders` | SignalR-хаб: події `OrderUpdated`, `ZonesUpdated` |

## Свідомі спрощення (через дедлайн практики, не помилки)

- Без JWT/[Authorize] — логін повертає користувача напряму, клієнт тримає
  його в Pinia-сторі. ТЗ вимагає лише BCrypt-хешування паролів, не токени.
- Одна таблиця `Orders` замість `Orders` + `Trips` — завершене замовлення
  просто лишається рядком зі статусом `completed`.
- `EnsureCreated()` замість `dotnet ef migrations`.
- SQLite замість SQL Server/PostgreSQL.

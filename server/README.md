# server/ — ASP.NET Core Web API (заплановано)

Бекенд ще не реалізовано в коді — рефакторинг фронтенду йде першим (Kanban-картки
поточної ітерації), бекенд підключається наступними картками. Плановий шар:

```
server/
└── src/
    ├── TaxiSystem.Api/              # Controllers, Program.cs, SignalR Hubs, BackgroundServices
    ├── TaxiSystem.Application/      # Services, DTO, BCrypt password hashing, Open-Meteo client
    ├── TaxiSystem.Domain/           # Entities (User, Driver, Order, Trip)
    └── TaxiSystem.Infrastructure/   # EF Core DbContext, репозиторії
```

Деталі — див. `../docs/ARCHITECTURE.md` та `../docs/kanban-board.md`.

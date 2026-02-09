# RollWarehouse

Backend для управления складом рулонов металла  
Stack: **C# .NET 9**, **ASP.NET Core**, **EF Core (Npgsql)**, **Postgres**, **Docker**, **xUnit + Moq**.

---

## Что реализовано (кратко)

- CRUD (add / soft-delete / list) для рулонов: `id`, `length`, `weight`, `dateAdded`, `dateRemoved`
- Валидация входных данных (`length` и `weight` обязательны и > 0)
- Фильтрация списка по комбинации диапазонов (id, length, weight, dateAdded, dateRemoved)
- Статистика за период:
  - количество добавленных/удалённых,
  - avg/min/max длина и вес,
  - суммарный вес,
  - min/max промежуток между добавлением и удалением,
  - бонус: день с min/max количеством на складе и день с min/max суммарным весом.
- Hexagonal архитектура: `Domain` / `Application` / `Infrastructure` / `Presentation`
- Docker + docker-compose (Postgres + API)
- Миграции EF и автоприменение миграций при старте с retry
- Unit tests (xUnit + Moq), тесты не зависят от реальной БД

---

## Требования

- .NET 9 SDK
- Docker & docker-compose (v2 recommended)
- (опционально) `dotnet-ef` tool для локальной работы с миграциями:
```bash
dotnet tool install --global dotnet-ef
```

---

## Быстрый старт — запустить в Docker

В корне репозитория выполните:

```bash
docker-compose up --build
```

Это делает:

- собирает образ API,
- поднимает Postgres (service `db`) и API (service `api`),
- пробрасывает порты: `Postgres 5432`, API `5000` → контейнер `80`,
- API при старте применяет миграции (если есть) с retry.

Откройте Swagger UI:  
`http://localhost:5000/swagger`

Если хотите запустить в фоне:

```bash
docker-compose up --build -d
docker-compose logs -f api
```

---

## Локальный запуск (без Docker)

1. Восстановить пакеты и собрать:

```bash
dotnet restore
dotnet build
```

2. Тесты:

```bash
dotnet test
```

3. Запустить приложение:

```bash
dotnet run --project src/RollWarehouse.Presentation.Http
```

API будет слушать: `http://localhost:5000`.

---

## Миграции EF

Проект использует EF Core Migrations. Если нужно создать / применить миграции локально:

1. Установите `dotnet-ef`, если ещё не установлен:
```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

2. Создать миграцию:
```bash
dotnet ef migrations add InitialCreate \
  --project src/Infrastructure/RollWarehouse.Infrastructure.Persistence \
  --startup-project src/RollWarehouse.Presentation.Http
```

3. Применить миграции:
```bash
dotnet ef database update \
  --project src/Infrastructure/RollWarehouse.Infrastructure.Persistence \
  --startup-project src/RollWarehouse.Presentation.Http
```

> Примечание: при запуске через Docker миграции применяются автоматически в контейнере. Если вы запускаете локально, убедитесь, что Postgres доступен по connection string в `appsettings` или в ENV.

---

## Конфигурация (connection strings и ENV)

`RollWarehouse.Presentation.Http` читает connection string из стандартных мест (`appsettings.json` и ENV). Поддерживаются оба формата:

- `ConnectionStrings__Postgres` (ASP.NET Core hierarchical ENV)
- либо `CONN_STR` (альтернативный fallback)

**Формат строки подключения**:
```
Host=db;Port=5432;Database=rollwarehouse;Username=postgres;Password=postgres
```

## Endpoints — описание и примеры

### POST `/api/rolls`
Добавить рулон. `Content-Type: application/json`

Body:
```json
{
  "length": 12.5,
  "weight": 200.0
}
```

Пример curl:
```bash
curl -v -X POST http://localhost:5000/api/rolls \
  -H "Content-Type: application/json" \
  -d '{"length":12.5,"weight":200.0}'
```

Ответ: `201 Created` с объектом рулона.

---

### GET `/api/rolls`
Получить список рулонов. По умолчанию возвращаются **только активные** рулоны (`dateRemoved == null`).

Query-параметры (все необязательны, комбинируются):
- `idMin`, `idMax` (GUID)
- `weightMin`, `weightMax` (number)
- `lengthMin`, `lengthMax` (number)
- `dateAddedFrom`, `dateAddedTo` (ISO datetime)
- `dateRemovedFrom`, `dateRemovedTo` (ISO datetime)
- `includeRemoved` (bool) — если `true`, возвращаются также мягко удалённые.

Пример:
```bash
curl "http://localhost:5000/api/rolls?lengthMin=10&lengthMax=20"
curl "http://localhost:5000/api/rolls?includeRemoved=true"
```

---

### DELETE `/api/rolls/{id}`
Мягкое удаление (устанавливает `dateRemoved`) и возвращает удалённый объект.

Пример:
```bash
curl -X DELETE http://localhost:5000/api/rolls/<GUID>
```

Если нужен hard-delete, можно переключить реализацию репозитория (см. код).

---

### GET `/api/rolls/statistics?start={iso}&end={iso}`
Статистика за период `[start, end]` (оба параметра обязательны):

Пример:
```bash
curl "http://localhost:5000/api/rolls/statistics?start=2025-01-01T00:00:00Z&end=2025-12-31T23:59:59Z"
```

Ответ содержит: `addedCount`, `removedCount`, `averageLength`, `averageWeight`, `maxLength`, `minLength`, `maxWeight`, `minWeight`, `totalWeight`, `maxIntervalSeconds`, `minIntervalSeconds`, `dayWithMaxCount`, `dayWithMinCount`, `dayWithMaxTotalWeight`, `dayWithMinTotalWeight`.

---

## Отладка и часто встречающиеся проблемы

### Миграции / PendingModelChangesWarning
Ошибка `The model for context 'PersistenceContext' has pending changes` — вызывется, если вы пытаетесь `database update` без созданной миграции. Создайте миграцию командой `dotnet ef migrations add ...`, затем `dotnet ef database update`.

### dotnet-ef не найден
Установите:
```bash
dotnet tool install --global dotnet-ef
```

## Логи контейнера
Просмотр логов:
```bash
docker-compose logs -f api
docker-compose logs -f db
```

### Просмотр таблицы напрямую
```bash
docker-compose exec db psql -U postgres -d rollwarehouse -c 'select * from "Rolls" limit 20;'
```

---

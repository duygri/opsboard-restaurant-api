# OpsBoard

[![.NET CI](https://github.com/duygri/opsboard-restaurant-api/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/duygri/opsboard-restaurant-api/actions/workflows/dotnet-ci.yml)

OpsBoard is a backend-focused restaurant operations dashboard built as a portfolio project. It is not a generic CRUD demo: the main story is staff creating dine-in orders, moving them through a kitchen workflow, marking payment, and letting an admin inspect revenue and audit logs.

## What It Shows

- ASP.NET Core Web API with JWT authentication.
- Policy-based RBAC for Admin and Staff.
- EF Core persistence with PostgreSQL mappings and migrations.
- Domain rules for order lifecycle and table occupancy.
- Audit logging for important order actions.
- Daily revenue reporting in `Asia/Ho_Chi_Minh`.
- Backend tests for domain rules, services, authorization, and API workflows.

## Architecture

```text
OpsBoard.Api
  Controllers, auth policies, exception mapping

OpsBoard.Application
  DTOs, service abstractions, application exceptions

OpsBoard.Domain
  Entities, enums, order transition rules

OpsBoard.Infrastructure
  EF Core DbContext, PostgreSQL configuration, seed data, app services
```

Controllers stay thin. Business orchestration lives in services, and core invariants such as valid order transitions stay in the domain model.

## Prerequisites

- .NET SDK `10.0.109`
- PostgreSQL for local API runs
- Docker is not required for Milestone 1

The solution file is `OpsBoard.slnx`.

## Configuration

Default development connection string:

```json
"Host=localhost;Port=5432;Database=opsboard_dev;Username=postgres;Password=postgres"
```

Override it with user secrets or environment variables for your machine:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=opsboard_dev;Username=postgres;Password=postgres" --project src/OpsBoard.Api/OpsBoard.Api.csproj
```

When the API starts in `Development`, it runs EF migrations and seeds demo data.

## Seed Accounts

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@opsboard.local` | `Admin123!` |
| Staff | `staff@opsboard.local` | `Staff123!` |

## Run

```powershell
dotnet restore OpsBoard.slnx --configfile NuGet.Config --ignore-failed-sources
dotnet build OpsBoard.slnx --no-restore
dotnet run --project src/OpsBoard.Api/OpsBoard.Api.csproj
```

If your NuGet setup is normal, plain `dotnet restore OpsBoard.slnx` is enough. The checked-in `NuGet.Config` exists so this project can also restore in locked-down/offline-ish environments.

## Test

```powershell
dotnet test tests/OpsBoard.Tests/OpsBoard.Tests.csproj --no-restore
```

## Core API

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/menu-categories`
- `GET /api/menu-items`
- `GET /api/tables`
- `POST /api/orders`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `PATCH /api/orders/{id}/status`
- `POST /api/orders/{id}/cancel`
- `GET /api/reports/daily?date=yyyy-mm-dd`
- `GET /api/audit-logs`

`PATCH /api/orders/{id}/status` accepts:

```json
{ "targetStatus": "Preparing" }
```

## Demo Flow

1. Log in as Staff.
2. Read available tables and menu items.
3. Create an order with at least one item.
4. Move status through `Preparing -> Ready -> Served -> Paid`.
5. Log in as Admin.
6. View daily report and recent audit logs.

Order creation rules:

- An order requires at least one item.
- Quantities must be positive.
- A table must be `Available`.
- Only one active order is allowed per table.
- `Paid` and `Cancelled` release the table back to `Available`.

## V1 Limitations

- No React frontend yet.
- No real payment gateway.
- No table CRUD.
- No menu/user write endpoints in Milestone 1.
- No Docker Compose requirement.
- Swagger/OpenAPI UI is deferred until the matching package is available in the target environment.

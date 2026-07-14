# Architecture Notes

OpsBoard is intentionally small, but it is structured like a real backend service rather than a CRUD sample.

## Backend Boundaries

```text
OpsBoard.Api
  Controllers, authentication, authorization policies, exception mapping

OpsBoard.Application
  DTOs, service contracts, application exceptions

OpsBoard.Domain
  Entities, enums, order status transitions, table occupancy rules

OpsBoard.Infrastructure
  EF Core DbContext, PostgreSQL mappings, seed data, service implementations
```

The API layer handles transport concerns. Services orchestrate use cases. Domain entities protect invariants such as valid order transitions.

## Frontend Boundaries

```text
src/OpsBoard.Client/src/App.tsx
  Session orchestration, data loading, polling, mutation handlers

src/OpsBoard.Client/src/api
  Typed API wrapper and API error handling

src/OpsBoard.Client/src/components
  Login, floor, orders, report, audit, and shared UI components

src/OpsBoard.Client/src/session.ts
  Local storage session read/write/clear helpers
```

The React app stays deliberately lightweight. It demonstrates the workflow and recruiter-facing UI without introducing a state management library that the scope does not need.

## Key Decisions

- Policy-based authorization is enforced at the API layer.
- Staff can create and advance orders; Admin can inspect reports and audit logs.
- Audit logs are stored in PostgreSQL in a dedicated `audit_logs` table.
- Daily reports are calculated for `Asia/Ho_Chi_Minh`.
- Inventory is represented as a manual low-stock flag, not automatic stock deduction.
- Live-ish updates use 10-second polling, not WebSockets.
- Users and menu items are intended to be disabled rather than hard deleted in later milestones.

## Testing Story

The backend test suite covers domain rules, service rules, API workflows, authorization, reporting, and audit behavior. The frontend has focused tests for session safety and typed API error handling, plus lint/build checks in CI.

## V1 Tradeoffs

OpsBoard does not include multi-restaurant tenancy, real payment gateway integration, table CRUD, menu write screens, or Docker Compose. Those are deliberate exclusions to keep the portfolio story focused on a complete vertical slice.

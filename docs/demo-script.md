# OpsBoard Demo Script

This script is designed for a 5-minute portfolio walkthrough.

## Setup

Start the API:

```powershell
dotnet run --project src/OpsBoard.Api/OpsBoard.Api.csproj
```

Start the React client:

```powershell
cd src/OpsBoard.Client
npm install
npm run dev
```

Open `http://localhost:5173`.

## Staff Walkthrough

1. Sign in as Staff: `staff@opsboard.local` / `Staff123!`.
2. Confirm the Floor view shows table availability and menu items.
3. Select an available table.
4. Add at least one menu item to the cart.
5. Create the order.
6. Move the order through:
   `New -> Preparing -> Ready -> Served -> Paid`.

What this proves:

- JWT login works.
- Staff can operate the order workflow.
- Empty orders and unavailable tables are rejected by the API.
- Order transitions are validated by domain rules.
- Paying an order releases the table.

## Admin Walkthrough

1. Sign out.
2. Sign in as Admin: `admin@opsboard.local` / `Admin123!`.
3. Open Daily Report.
4. Confirm paid orders are included in revenue.
5. Open Audit Logs.
6. Confirm important order lifecycle actions are recorded.

What this proves:

- RBAC policies separate Staff and Admin capabilities.
- Revenue reporting uses `Asia/Ho_Chi_Minh`.
- Audit logs capture actor, action, entity, and timestamp.

## Reviewer Notes

The frontend polls every 10 seconds for a live-ish queue without WebSocket complexity. Docker is intentionally out of scope for V1 so the project can be reviewed with a normal .NET + PostgreSQL setup.

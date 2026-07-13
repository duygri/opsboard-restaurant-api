import {
  AlertCircle,
  ArrowRight,
  BadgeDollarSign,
  ClipboardList,
  Coffee,
  DoorOpen,
  Loader2,
  LogOut,
  ReceiptText,
  RefreshCcw,
  ShieldCheck,
  SquareStack,
  Utensils,
} from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'

type UserRole = 'Admin' | 'Staff'
type OrderStatus = 'New' | 'Preparing' | 'Ready' | 'Served' | 'Paid' | 'Cancelled'
type TableStatus = 'Available' | 'Occupied' | 'Reserved'
type ViewKey = 'floor' | 'orders' | 'report' | 'audit'

type CurrentUser = {
  id: string
  fullName: string
  email: string
  role: UserRole
}

type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  user: CurrentUser
}

type TableResponse = {
  id: string
  name: string
  status: TableStatus
}

type MenuCategoryResponse = {
  id: string
  name: string
  displayOrder: number
}

type MenuItemResponse = {
  id: string
  categoryId: string
  name: string
  description: string
  price: number
  isAvailable: boolean
  isLowStock: boolean
}

type OrderItemResponse = {
  id: string
  menuItemId: string
  itemNameSnapshot: string
  unitPriceSnapshot: number
  quantity: number
  lineTotal: number
}

type OrderDetailResponse = {
  id: string
  tableId: string
  tableName: string
  status: OrderStatus
  subtotal: number
  total: number
  createdAtUtc: string
  paidAtUtc: string | null
  items: OrderItemResponse[]
}

type DailyReportResponse = {
  date: string
  timeZone: string
  revenue: number
  paidOrderCount: number
  statusCounts: Array<{ status: OrderStatus; count: number }>
}

type AuditLogResponse = {
  id: string
  actorName: string
  action: string
  entityType: string
  entityId: string
  createdAtUtc: string
}

type Session = {
  token: string
  user: CurrentUser
}

const storageKey = 'opsboard.session'
const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''
const today = new Date().toISOString().slice(0, 10)
const nextStatus: Partial<Record<OrderStatus, OrderStatus>> = {
  New: 'Preparing',
  Preparing: 'Ready',
  Ready: 'Served',
  Served: 'Paid',
}

function App() {
  const [session, setSession] = useState<Session | null>(() => {
    const raw = window.localStorage.getItem(storageKey)
    return raw ? (JSON.parse(raw) as Session) : null
  })
  const [activeView, setActiveView] = useState<ViewKey>('floor')
  const [tables, setTables] = useState<TableResponse[]>([])
  const [categories, setCategories] = useState<MenuCategoryResponse[]>([])
  const [menuItems, setMenuItems] = useState<MenuItemResponse[]>([])
  const [orders, setOrders] = useState<OrderDetailResponse[]>([])
  const [report, setReport] = useState<DailyReportResponse | null>(null)
  const [auditLogs, setAuditLogs] = useState<AuditLogResponse[]>([])
  const [reportDate, setReportDate] = useState(today)
  const [selectedTableId, setSelectedTableId] = useState('')
  const [cart, setCart] = useState<Record<string, number>>({})
  const [loading, setLoading] = useState(false)
  const [actionId, setActionId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const isAdmin = session?.user.role === 'Admin'
  const availableTables = tables.filter((table) => table.status === 'Available')
  const activeOrders = orders.filter((order) => !['Paid', 'Cancelled'].includes(order.status))
  const occupiedCount = tables.filter((table) => table.status === 'Occupied').length
  const cartLines = useMemo(
    () =>
      Object.entries(cart)
        .map(([menuItemId, quantity]) => {
          const item = menuItems.find((candidate) => candidate.id === menuItemId)
          return item ? { item, quantity, lineTotal: item.price * quantity } : null
        })
        .filter((line): line is { item: MenuItemResponse; quantity: number; lineTotal: number } => Boolean(line)),
    [cart, menuItems],
  )
  const cartTotal = cartLines.reduce((sum, line) => sum + line.lineTotal, 0)

  const request = useCallback(
    async <T,>(path: string, options: RequestInit = {}) => {
      const response = await fetch(`${apiBaseUrl}${path}`, {
        ...options,
        headers: {
          'Content-Type': 'application/json',
          ...(session?.token ? { Authorization: `Bearer ${session.token}` } : {}),
          ...options.headers,
        },
      })

      if (response.status === 401) {
        window.localStorage.removeItem(storageKey)
        setSession(null)
      }

      if (!response.ok) {
        const body = await response.json().catch(() => null)
        throw new Error(body?.detail ?? body?.message ?? `Request failed with ${response.status}`)
      }

      return (await response.json()) as T
    },
    [session?.token],
  )

  const loadWorkspace = useCallback(async () => {
    if (!session) {
      return
    }

    setLoading(true)
    setError(null)
    try {
      const [tableData, categoryData, itemData, orderData] = await Promise.all([
        request<TableResponse[]>('/api/tables'),
        request<MenuCategoryResponse[]>('/api/menu-categories'),
        request<MenuItemResponse[]>('/api/menu-items'),
        request<OrderDetailResponse[]>('/api/orders'),
      ])
      setTables(tableData)
      setCategories(categoryData)
      setMenuItems(itemData)
      setOrders(orderData)
      setSelectedTableId((current) => current || tableData.find((table) => table.status === 'Available')?.id || '')

      if (session.user.role === 'Admin') {
        const [reportData, auditData] = await Promise.all([
          request<DailyReportResponse>(`/api/reports/daily?date=${reportDate}`),
          request<AuditLogResponse[]>('/api/audit-logs'),
        ])
        setReport(reportData)
        setAuditLogs(auditData)
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Unable to load OpsBoard data.')
    } finally {
      setLoading(false)
    }
  }, [reportDate, request, session])

  useEffect(() => {
    void loadWorkspace()
  }, [loadWorkspace])

  function handleSession(nextSession: Session | null) {
    setSession(nextSession)
    if (nextSession) {
      window.localStorage.setItem(storageKey, JSON.stringify(nextSession))
      setActiveView('floor')
    } else {
      window.localStorage.removeItem(storageKey)
      setTables([])
      setOrders([])
      setReport(null)
      setAuditLogs([])
    }
  }

  async function handleLogin(email: string, password: string) {
    setLoading(true)
    setError(null)
    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!response.ok) {
        throw new Error('Login failed. Check the seeded credentials or API server.')
      }

      const login = (await response.json()) as LoginResponse
      handleSession({ token: login.accessToken, user: login.user })
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : 'Login failed.')
    } finally {
      setLoading(false)
    }
  }

  async function createOrder() {
    if (!selectedTableId || cartLines.length === 0) {
      setError('Choose an available table and at least one menu item.')
      return
    }

    setActionId('create-order')
    setError(null)
    try {
      await request<OrderDetailResponse>('/api/orders', {
        method: 'POST',
        body: JSON.stringify({
          tableId: selectedTableId,
          items: cartLines.map((line) => ({ menuItemId: line.item.id, quantity: line.quantity })),
        }),
      })
      setCart({})
      setSelectedTableId('')
      await loadWorkspace()
      setActiveView('orders')
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : 'Unable to create order.')
    } finally {
      setActionId(null)
    }
  }

  async function moveOrder(order: OrderDetailResponse, targetStatus: OrderStatus) {
    setActionId(`${order.id}-${targetStatus}`)
    setError(null)
    try {
      await request<OrderDetailResponse>(`/api/orders/${order.id}/status`, {
        method: 'PATCH',
        body: JSON.stringify({ targetStatus }),
      })
      await loadWorkspace()
    } catch (moveError) {
      setError(moveError instanceof Error ? moveError.message : 'Unable to update order.')
    } finally {
      setActionId(null)
    }
  }

  async function cancelOrder(order: OrderDetailResponse) {
    setActionId(`${order.id}-cancel`)
    setError(null)
    try {
      await request<OrderDetailResponse>(`/api/orders/${order.id}/cancel`, { method: 'POST' })
      await loadWorkspace()
    } catch (cancelError) {
      setError(cancelError instanceof Error ? cancelError.message : 'Unable to cancel order.')
    } finally {
      setActionId(null)
    }
  }

  async function refreshReport() {
    if (!isAdmin) {
      return
    }

    setActionId('report')
    setError(null)
    try {
      setReport(await request<DailyReportResponse>(`/api/reports/daily?date=${reportDate}`))
      setAuditLogs(await request<AuditLogResponse[]>('/api/audit-logs'))
    } catch (reportError) {
      setError(reportError instanceof Error ? reportError.message : 'Unable to refresh admin data.')
    } finally {
      setActionId(null)
    }
  }

  if (!session) {
    return <LoginScreen error={error} loading={loading} onLogin={handleLogin} />
  }

  return (
    <main className="min-h-dvh bg-slate-100 text-slate-950">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-md bg-slate-950 text-white">
              <Utensils aria-hidden="true" size={22} />
            </div>
            <div>
              <h1 className="text-xl font-semibold tracking-normal">OpsBoard</h1>
              <p className="text-sm text-slate-500">Restaurant operations console</p>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2 text-sm">
            <span className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-slate-700">
              {session.user.fullName} · {session.user.role}
            </span>
            <button className="icon-button" type="button" onClick={() => void loadWorkspace()} aria-label="Refresh dashboard">
              <RefreshCcw aria-hidden="true" size={18} />
            </button>
            <button className="icon-button" type="button" onClick={() => handleSession(null)} aria-label="Log out">
              <LogOut aria-hidden="true" size={18} />
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto grid max-w-7xl gap-4 px-4 py-4 lg:grid-cols-[220px_1fr]">
        <aside className="sidebar-panel">
          <nav className="grid gap-1" aria-label="OpsBoard sections">
            <NavButton icon={<SquareStack size={18} />} label="Floor" active={activeView === 'floor'} onClick={() => setActiveView('floor')} />
            <NavButton icon={<ClipboardList size={18} />} label="Orders" active={activeView === 'orders'} onClick={() => setActiveView('orders')} />
            {isAdmin ? (
              <>
                <NavButton icon={<BadgeDollarSign size={18} />} label="Report" active={activeView === 'report'} onClick={() => setActiveView('report')} />
                <NavButton icon={<ShieldCheck size={18} />} label="Audit" active={activeView === 'audit'} onClick={() => setActiveView('audit')} />
              </>
            ) : null}
          </nav>
          <div className="mt-4 grid gap-2 text-sm">
            <MetricLine label="Active orders" value={activeOrders.length.toString()} />
            <MetricLine label="Occupied tables" value={`${occupiedCount}/${tables.length || 0}`} />
            <MetricLine label="Cart" value={formatMoney(cartTotal)} />
          </div>
        </aside>

        <section className="grid gap-4">
          {error ? (
            <div className="flex items-start gap-2 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              <AlertCircle aria-hidden="true" size={18} />
              <p>{error}</p>
            </div>
          ) : null}

          <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <SummaryCard label="Open orders" value={activeOrders.length.toString()} helper="Kitchen queue" icon={<ClipboardList size={20} />} />
            <SummaryCard label="Revenue today" value={formatMoney(report?.revenue ?? 0)} helper={isAdmin ? 'Paid orders' : 'Admin only'} icon={<BadgeDollarSign size={20} />} />
            <SummaryCard label="Available tables" value={availableTables.length.toString()} helper="Ready for seating" icon={<DoorOpen size={20} />} />
            <SummaryCard label="Low stock" value={menuItems.filter((item) => item.isLowStock).length.toString()} helper="Manual flags" icon={<Coffee size={20} />} />
          </section>

          {loading ? (
            <div className="panel flex items-center gap-2 text-sm text-slate-600">
              <Loader2 className="animate-spin" aria-hidden="true" size={18} />
              Loading workspace
            </div>
          ) : null}

          {activeView === 'floor' ? (
            <FloorView
              cart={cart}
              cartLines={cartLines}
              cartTotal={cartTotal}
              categories={categories}
              menuItems={menuItems}
              selectedTableId={selectedTableId}
              tables={tables}
              actionId={actionId}
              onCartChange={setCart}
              onCreateOrder={() => void createOrder()}
              onSelectTable={setSelectedTableId}
            />
          ) : null}

          {activeView === 'orders' ? (
            <OrdersView
              actionId={actionId}
              orders={activeOrders}
              onCancel={(order) => void cancelOrder(order)}
              onMove={(order, status) => void moveOrder(order, status)}
            />
          ) : null}

          {activeView === 'report' && isAdmin ? (
            <ReportView
              actionId={actionId}
              report={report}
              reportDate={reportDate}
              onDateChange={setReportDate}
              onRefresh={() => void refreshReport()}
            />
          ) : null}

          {activeView === 'audit' && isAdmin ? <AuditView logs={auditLogs} /> : null}
        </section>
      </div>
    </main>
  )
}

function LoginScreen({
  error,
  loading,
  onLogin,
}: {
  error: string | null
  loading: boolean
  onLogin: (email: string, password: string) => Promise<void>
}) {
  const [email, setEmail] = useState('staff@opsboard.local')
  const [password, setPassword] = useState('Staff123!')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await onLogin(email, password)
  }

  return (
    <main className="grid min-h-dvh place-items-center bg-slate-100 px-4 text-slate-950">
      <section className="grid w-full max-w-5xl overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm lg:grid-cols-[1.1fr_0.9fr]">
        <div className="border-b border-slate-200 bg-slate-950 p-8 text-white lg:border-b-0 lg:border-r">
          <div className="flex h-12 w-12 items-center justify-center rounded-md bg-emerald-500 text-slate-950">
            <Utensils aria-hidden="true" size={24} />
          </div>
          <h1 className="mt-8 max-w-md text-3xl font-semibold tracking-normal">Run the dining room from one operational board.</h1>
          <p className="mt-4 max-w-md text-sm leading-6 text-slate-300">
            Demo the complete flow: tables, order creation, kitchen queue, payment, revenue, and audit review.
          </p>
          <div className="mt-8 grid gap-3 text-sm text-slate-200">
            <LoginPoint label="Staff" text="Create dine-in orders and move them through the kitchen." />
            <LoginPoint label="Admin" text="Review daily revenue and audit trails." />
          </div>
        </div>
        <form className="grid gap-5 p-8" onSubmit={(event) => void submit(event)}>
          <div>
            <h2 className="text-2xl font-semibold tracking-normal">Sign in</h2>
            <p className="mt-1 text-sm text-slate-500">Use seeded credentials from the backend README.</p>
          </div>
          <label className="grid gap-2 text-sm font-medium text-slate-700">
            Email
            <input className="form-input" type="email" value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" />
          </label>
          <label className="grid gap-2 text-sm font-medium text-slate-700">
            Password
            <input className="form-input" type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" />
          </label>
          {error ? <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p> : null}
          <button className="primary-button" type="submit" disabled={loading}>
            {loading ? <Loader2 className="animate-spin" aria-hidden="true" size={18} /> : <ArrowRight aria-hidden="true" size={18} />}
            Sign in
          </button>
          <div className="grid gap-2 sm:grid-cols-2">
            <button className="secondary-button" type="button" onClick={() => { setEmail('staff@opsboard.local'); setPassword('Staff123!') }}>
              Staff demo
            </button>
            <button className="secondary-button" type="button" onClick={() => { setEmail('admin@opsboard.local'); setPassword('Admin123!') }}>
              Admin demo
            </button>
          </div>
        </form>
      </section>
    </main>
  )
}

function FloorView({
  actionId,
  cart,
  cartLines,
  cartTotal,
  categories,
  menuItems,
  selectedTableId,
  tables,
  onCartChange,
  onCreateOrder,
  onSelectTable,
}: {
  actionId: string | null
  cart: Record<string, number>
  cartLines: Array<{ item: MenuItemResponse; quantity: number; lineTotal: number }>
  cartTotal: number
  categories: MenuCategoryResponse[]
  menuItems: MenuItemResponse[]
  selectedTableId: string
  tables: TableResponse[]
  onCartChange: (cart: Record<string, number>) => void
  onCreateOrder: () => void
  onSelectTable: (tableId: string) => void
}) {
  function setQuantity(menuItemId: string, quantity: number) {
    const nextCart = { ...cart }
    if (quantity <= 0) {
      delete nextCart[menuItemId]
    } else {
      nextCart[menuItemId] = quantity
    }
    onCartChange(nextCart)
  }

  return (
    <div className="grid gap-4 xl:grid-cols-[1fr_360px]">
      <section className="panel">
        <SectionTitle title="Tables" subtitle="Seat guests only at available tables." />
        <div className="mt-4 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {tables.map((table) => (
            <button
              className={`table-tile ${selectedTableId === table.id ? 'table-tile-selected' : ''}`}
              disabled={table.status !== 'Available'}
              key={table.id}
              type="button"
              onClick={() => onSelectTable(table.id)}
            >
              <span className="text-sm font-semibold">{table.name}</span>
              <StatusBadge status={table.status} />
            </button>
          ))}
        </div>
      </section>

      <section className="panel xl:row-span-2">
        <SectionTitle title="Order cart" subtitle="Server-side totals are recalculated by the API." />
        <div className="mt-4 grid gap-3">
          {cartLines.length === 0 ? (
            <p className="rounded-md border border-dashed border-slate-300 px-3 py-6 text-center text-sm text-slate-500">Add menu items to start an order.</p>
          ) : (
            cartLines.map((line) => (
              <div className="flex items-center justify-between gap-3 rounded-md border border-slate-200 p-3" key={line.item.id}>
                <div>
                  <p className="text-sm font-medium">{line.item.name}</p>
                  <p className="text-xs text-slate-500">
                    {line.quantity} x {formatMoney(line.item.price)}
                  </p>
                </div>
                <p className="tabular-nums text-sm font-semibold">{formatMoney(line.lineTotal)}</p>
              </div>
            ))
          )}
        </div>
        <div className="mt-4 border-t border-slate-200 pt-4">
          <div className="flex items-center justify-between text-sm">
            <span className="text-slate-500">Selected table</span>
            <span className="font-medium">{tables.find((table) => table.id === selectedTableId)?.name ?? 'None'}</span>
          </div>
          <div className="mt-2 flex items-center justify-between text-base">
            <span className="font-medium">Total</span>
            <span className="tabular-nums font-semibold">{formatMoney(cartTotal)}</span>
          </div>
          <button className="primary-button mt-4 w-full" disabled={actionId === 'create-order'} type="button" onClick={onCreateOrder}>
            {actionId === 'create-order' ? <Loader2 className="animate-spin" aria-hidden="true" size={18} /> : <ReceiptText aria-hidden="true" size={18} />}
            Create order
          </button>
        </div>
      </section>

      <section className="panel">
        <SectionTitle title="Menu" subtitle="Low-stock flags stay visible during order entry." />
        <div className="mt-4 grid gap-4">
          {categories.map((category) => {
            const items = menuItems.filter((item) => item.categoryId === category.id && item.isAvailable)
            return (
              <div key={category.id}>
                <h3 className="text-sm font-semibold">{category.name}</h3>
                <div className="mt-2 grid gap-2 md:grid-cols-2">
                  {items.map((item) => (
                    <div className="rounded-md border border-slate-200 p-3" key={item.id}>
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-sm font-medium">{item.name}</p>
                          <p className="mt-1 line-clamp-2 text-xs leading-5 text-slate-500">{item.description}</p>
                        </div>
                        <span className="tabular-nums text-sm font-semibold">{formatMoney(item.price)}</span>
                      </div>
                      <div className="mt-3 flex items-center justify-between gap-2">
                        {item.isLowStock ? <span className="badge badge-amber">Low stock</span> : <span className="badge badge-slate">Available</span>}
                        <QuantityControl value={cart[item.id] ?? 0} onChange={(quantity) => setQuantity(item.id, quantity)} />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )
          })}
        </div>
      </section>
    </div>
  )
}

function OrdersView({
  actionId,
  orders,
  onCancel,
  onMove,
}: {
  actionId: string | null
  orders: OrderDetailResponse[]
  onCancel: (order: OrderDetailResponse) => void
  onMove: (order: OrderDetailResponse, targetStatus: OrderStatus) => void
}) {
  return (
    <section className="panel">
      <SectionTitle title="Kitchen queue" subtitle="Move each order through the lifecycle. Paid and cancelled orders leave the active queue." />
      <div className="mt-4 grid gap-3">
        {orders.length === 0 ? (
          <p className="rounded-md border border-dashed border-slate-300 px-3 py-8 text-center text-sm text-slate-500">No active orders.</p>
        ) : (
          orders.map((order) => {
            const target = nextStatus[order.status]
            return (
              <article className="rounded-md border border-slate-200 p-4" key={order.id}>
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="font-semibold">{order.tableName}</h3>
                      <StatusBadge status={order.status} />
                    </div>
                    <p className="mt-1 text-xs text-slate-500">{new Date(order.createdAtUtc).toLocaleString()}</p>
                  </div>
                  <p className="tabular-nums text-base font-semibold">{formatMoney(order.total)}</p>
                </div>
                <div className="mt-3 grid gap-2">
                  {order.items.map((item) => (
                    <div className="flex justify-between gap-3 text-sm" key={item.id}>
                      <span>{item.quantity} x {item.itemNameSnapshot}</span>
                      <span className="tabular-nums text-slate-600">{formatMoney(item.lineTotal)}</span>
                    </div>
                  ))}
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                  {target ? (
                    <button className="primary-button" type="button" disabled={actionId === `${order.id}-${target}`} onClick={() => onMove(order, target)}>
                      {actionId === `${order.id}-${target}` ? <Loader2 className="animate-spin" aria-hidden="true" size={16} /> : <ArrowRight aria-hidden="true" size={16} />}
                      Mark {target}
                    </button>
                  ) : null}
                  {order.status === 'New' || order.status === 'Preparing' ? (
                    <button className="danger-button" type="button" disabled={actionId === `${order.id}-cancel`} onClick={() => onCancel(order)}>
                      Cancel
                    </button>
                  ) : null}
                </div>
              </article>
            )
          })
        )}
      </div>
    </section>
  )
}

function ReportView({
  actionId,
  report,
  reportDate,
  onDateChange,
  onRefresh,
}: {
  actionId: string | null
  report: DailyReportResponse | null
  reportDate: string
  onDateChange: (date: string) => void
  onRefresh: () => void
}) {
  return (
    <section className="panel">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <SectionTitle title="Daily report" subtitle="Revenue is calculated from paid orders in Asia/Ho_Chi_Minh." />
        <div className="flex gap-2">
          <input className="form-input h-10 w-40" type="date" value={reportDate} onChange={(event) => onDateChange(event.target.value)} />
          <button className="icon-button" type="button" onClick={onRefresh} aria-label="Refresh report">
            {actionId === 'report' ? <Loader2 className="animate-spin" aria-hidden="true" size={18} /> : <RefreshCcw aria-hidden="true" size={18} />}
          </button>
        </div>
      </div>
      <div className="mt-4 grid gap-3 sm:grid-cols-3">
        <SummaryCard label="Revenue" value={formatMoney(report?.revenue ?? 0)} helper={report?.timeZone ?? 'Asia/Ho_Chi_Minh'} icon={<BadgeDollarSign size={20} />} />
        <SummaryCard label="Paid orders" value={(report?.paidOrderCount ?? 0).toString()} helper={report?.date ?? reportDate} icon={<ReceiptText size={20} />} />
        <SummaryCard label="Tracked statuses" value={(report?.statusCounts.length ?? 0).toString()} helper="Created today" icon={<ClipboardList size={20} />} />
      </div>
      <div className="mt-4 overflow-hidden rounded-md border border-slate-200">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
            <tr>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3 text-right">Count</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {(report?.statusCounts ?? []).map((count) => (
              <tr key={count.status}>
                <td className="px-4 py-3"><StatusBadge status={count.status} /></td>
                <td className="px-4 py-3 text-right tabular-nums font-medium">{count.count}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function AuditView({ logs }: { logs: AuditLogResponse[] }) {
  return (
    <section className="panel">
      <SectionTitle title="Audit logs" subtitle="Recent important actions across order lifecycle and admin workflows." />
      <div className="mt-4 grid gap-2">
        {logs.length === 0 ? (
          <p className="rounded-md border border-dashed border-slate-300 px-3 py-8 text-center text-sm text-slate-500">No audit logs yet.</p>
        ) : (
          logs.map((log) => (
            <article className="grid gap-2 rounded-md border border-slate-200 p-3 text-sm sm:grid-cols-[1fr_auto]" key={log.id}>
              <div>
                <p className="font-medium">{log.action}</p>
                <p className="mt-1 text-slate-500">{log.actorName} · {log.entityType} · {shortId(log.entityId)}</p>
              </div>
              <time className="text-xs text-slate-500">{new Date(log.createdAtUtc).toLocaleString()}</time>
            </article>
          ))
        )}
      </div>
    </section>
  )
}

function NavButton({ active, icon, label, onClick }: { active: boolean; icon: ReactNode; label: string; onClick: () => void }) {
  return (
    <button className={`nav-button ${active ? 'nav-button-active' : ''}`} type="button" onClick={onClick}>
      {icon}
      {label}
    </button>
  )
}

function SummaryCard({ helper, icon, label, value }: { helper: string; icon: ReactNode; label: string; value: string }) {
  return (
    <article className="panel flex items-center justify-between gap-3">
      <div>
        <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p>
        <p className="mt-1 text-2xl font-semibold tracking-normal tabular-nums">{value}</p>
        <p className="mt-1 text-xs text-slate-500">{helper}</p>
      </div>
      <div className="flex h-10 w-10 items-center justify-center rounded-md bg-emerald-50 text-emerald-700">{icon}</div>
    </article>
  )
}

function SectionTitle({ subtitle, title }: { subtitle: string; title: string }) {
  return (
    <div>
      <h2 className="text-lg font-semibold tracking-normal">{title}</h2>
      <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
    </div>
  )
}

function QuantityControl({ onChange, value }: { onChange: (value: number) => void; value: number }) {
  return (
    <div className="flex items-center rounded-md border border-slate-200">
      <button className="quantity-button" type="button" onClick={() => onChange(value - 1)} aria-label="Decrease quantity">-</button>
      <span className="w-9 text-center text-sm tabular-nums">{value}</span>
      <button className="quantity-button" type="button" onClick={() => onChange(value + 1)} aria-label="Increase quantity">+</button>
    </div>
  )
}

function StatusBadge({ status }: { status: OrderStatus | TableStatus }) {
  const className = {
    Available: 'badge-emerald',
    Occupied: 'badge-slate',
    Reserved: 'badge-amber',
    New: 'badge-slate',
    Preparing: 'badge-blue',
    Ready: 'badge-amber',
    Served: 'badge-violet',
    Paid: 'badge-emerald',
    Cancelled: 'badge-red',
  }[status]

  return <span className={`badge ${className}`}>{status}</span>
}

function MetricLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between rounded-md bg-slate-50 px-3 py-2">
      <span className="text-slate-500">{label}</span>
      <span className="font-semibold tabular-nums">{value}</span>
    </div>
  )
}

function LoginPoint({ label, text }: { label: string; text: string }) {
  return (
    <div className="rounded-md border border-white/10 bg-white/5 p-3">
      <p className="font-medium text-white">{label}</p>
      <p className="mt-1 text-slate-300">{text}</p>
    </div>
  )
}

function formatMoney(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)
}

function shortId(id: string) {
  return id.slice(0, 8)
}

export default App

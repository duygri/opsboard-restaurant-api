import {
  AlertCircle,
  BadgeDollarSign,
  ClipboardList,
  Coffee,
  DoorOpen,
  Loader2,
  LogOut,
  RefreshCcw,
  ShieldCheck,
  SquareStack,
  Utensils,
} from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'

import { ApiError, apiRequest, login } from './api/client'
import { AuditView } from './components/AuditView'
import { FloorView } from './components/FloorView'
import { LoginScreen } from './components/LoginScreen'
import { OrdersView } from './components/OrdersView'
import { ReportView } from './components/ReportView'
import { MetricLine, NavButton, SummaryCard } from './components/common'
import { clearStoredSession, readStoredSession, writeStoredSession } from './session'
import type {
  AuditLogResponse,
  CartLine,
  DailyReportResponse,
  MenuCategoryResponse,
  MenuItemResponse,
  OrderDetailResponse,
  OrderStatus,
  Session,
  TableResponse,
  ViewKey,
} from './types'
import { formatMoney } from './ui/format'

const today = new Date().toISOString().slice(0, 10)
const pollingIntervalMs = 10_000

function App() {
  const [session, setSession] = useState<Session | null>(() => readStoredSession())
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
  const cartLines = useMemo<CartLine[]>(
    () =>
      Object.entries(cart)
        .map(([menuItemId, quantity]) => {
          const item = menuItems.find((candidate) => candidate.id === menuItemId)
          return item ? { item, quantity, lineTotal: item.price * quantity } : null
        })
        .filter((line): line is CartLine => Boolean(line)),
    [cart, menuItems],
  )
  const cartTotal = cartLines.reduce((sum, line) => sum + line.lineTotal, 0)

  const request = useCallback(
    async <T,>(path: string, options: RequestInit = {}) => {
      try {
        return await apiRequest<T>(path, session?.token ?? null, options)
      } catch (requestError) {
        if (requestError instanceof ApiError && requestError.status === 401) {
          handleSession(null)
        }
        throw requestError
      }
    },
    [session?.token],
  )

  const loadWorkspace = useCallback(
    async ({ silent = false }: { silent?: boolean } = {}) => {
      if (!session) {
        return
      }

      if (!silent) {
        setLoading(true)
      }
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
        if (!silent) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load OpsBoard data.')
        }
      } finally {
        if (!silent) {
          setLoading(false)
        }
      }
    },
    [reportDate, request, session],
  )

  useEffect(() => {
    void loadWorkspace()
  }, [loadWorkspace])

  useEffect(() => {
    if (!session) {
      return undefined
    }

    const intervalId = window.setInterval(() => {
      void loadWorkspace({ silent: true })
    }, pollingIntervalMs)

    return () => window.clearInterval(intervalId)
  }, [loadWorkspace, session])

  function handleSession(nextSession: Session | null) {
    setSession(nextSession)
    if (nextSession) {
      writeStoredSession(nextSession)
      setActiveView('floor')
    } else {
      clearStoredSession()
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
      const loginResponse = await login(email, password)
      handleSession({ token: loginResponse.accessToken, user: loginResponse.user })
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
      await loadWorkspace({ silent: true })
    } catch (moveError) {
      setError(moveError instanceof Error ? moveError.message : 'Unable to update order.')
    } finally {
      setActionId(null)
    }
  }

  async function cancelOrder(order: OrderDetailResponse) {
    if (!window.confirm(`Cancel order for ${order.tableName}?`)) {
      return
    }

    setActionId(`${order.id}-cancel`)
    setError(null)
    try {
      await request<OrderDetailResponse>(`/api/orders/${order.id}/cancel`, { method: 'POST' })
      await loadWorkspace({ silent: true })
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

export default App

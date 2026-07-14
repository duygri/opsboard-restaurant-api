import { ArrowRight, Loader2 } from 'lucide-react'

import type { OrderDetailResponse, OrderStatus } from '../types'
import { formatMoney } from '../ui/format'
import { SectionTitle, StatusBadge } from './common'

const nextStatus: Partial<Record<OrderStatus, OrderStatus>> = {
  New: 'Preparing',
  Preparing: 'Ready',
  Ready: 'Served',
  Served: 'Paid',
}

export function OrdersView({
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

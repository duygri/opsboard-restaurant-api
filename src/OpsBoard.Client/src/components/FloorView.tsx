import { Loader2, ReceiptText } from 'lucide-react'

import type { CartLine, MenuCategoryResponse, MenuItemResponse, TableResponse } from '../types'
import { formatMoney } from '../ui/format'
import { QuantityControl, SectionTitle, StatusBadge } from './common'

export function FloorView({
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
  cartLines: CartLine[]
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

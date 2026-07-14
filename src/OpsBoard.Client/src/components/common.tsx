import type { ReactNode } from 'react'

import type { OrderStatus, TableStatus } from '../types'

export function NavButton({ active, icon, label, onClick }: { active: boolean; icon: ReactNode; label: string; onClick: () => void }) {
  return (
    <button className={`nav-button ${active ? 'nav-button-active' : ''}`} type="button" onClick={onClick}>
      {icon}
      {label}
    </button>
  )
}

export function SummaryCard({ helper, icon, label, value }: { helper: string; icon: ReactNode; label: string; value: string }) {
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

export function SectionTitle({ subtitle, title }: { subtitle: string; title: string }) {
  return (
    <div>
      <h2 className="text-lg font-semibold tracking-normal">{title}</h2>
      <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
    </div>
  )
}

export function QuantityControl({ onChange, value }: { onChange: (value: number) => void; value: number }) {
  return (
    <div className="flex items-center rounded-md border border-slate-200">
      <button className="quantity-button" type="button" onClick={() => onChange(value - 1)} aria-label="Decrease quantity">-</button>
      <span className="w-9 text-center text-sm tabular-nums">{value}</span>
      <button className="quantity-button" type="button" onClick={() => onChange(value + 1)} aria-label="Increase quantity">+</button>
    </div>
  )
}

export function StatusBadge({ status }: { status: OrderStatus | TableStatus }) {
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

export function MetricLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between rounded-md bg-slate-50 px-3 py-2">
      <span className="text-slate-500">{label}</span>
      <span className="font-semibold tabular-nums">{value}</span>
    </div>
  )
}

export function LoginPoint({ label, text }: { label: string; text: string }) {
  return (
    <div className="rounded-md border border-white/10 bg-white/5 p-3">
      <p className="font-medium text-white">{label}</p>
      <p className="mt-1 text-slate-300">{text}</p>
    </div>
  )
}

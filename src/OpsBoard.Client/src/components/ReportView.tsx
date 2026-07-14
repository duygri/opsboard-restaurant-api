import { BadgeDollarSign, ClipboardList, Loader2, ReceiptText, RefreshCcw } from 'lucide-react'

import type { DailyReportResponse } from '../types'
import { formatMoney } from '../ui/format'
import { SectionTitle, StatusBadge, SummaryCard } from './common'

export function ReportView({
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

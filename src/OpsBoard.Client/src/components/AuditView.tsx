import type { AuditLogResponse } from '../types'
import { shortId } from '../ui/format'
import { SectionTitle } from './common'

export function AuditView({ logs }: { logs: AuditLogResponse[] }) {
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

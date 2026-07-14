import { ArrowRight, Loader2, Utensils } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'

import { LoginPoint } from './common'

export function LoginScreen({
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
            <p className="mt-1 text-sm text-slate-500">Use seeded demo credentials. They are documented in the repository README.</p>
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

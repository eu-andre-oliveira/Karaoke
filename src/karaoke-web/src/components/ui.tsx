import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode } from 'react'

type Variante = 'primary' | 'secondary' | 'danger' | 'ghost'

const VARIANTES: Record<Variante, string> = {
  primary:
    'bg-gradient-to-r from-violet-500 to-fuchsia-500 text-white shadow-lg shadow-fuchsia-900/30 hover:from-violet-400 hover:to-fuchsia-400',
  secondary: 'bg-panel-2 text-slate-100 ring-1 ring-line hover:bg-line',
  danger: 'bg-rose-600/90 text-white hover:bg-rose-500',
  ghost: 'text-slate-300 hover:bg-white/5 hover:text-white',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante
  carregando?: boolean
}

export function Button({ variante = 'primary', carregando = false, disabled, className = '', children, ...rest }: ButtonProps) {
  return (
    <button
      type="button"
      disabled={disabled || carregando}
      aria-busy={carregando || undefined}
      className={`inline-flex min-h-11 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-fuchsia-400 disabled:cursor-not-allowed disabled:opacity-50 ${VARIANTES[variante]} ${className}`}
      {...rest}
    >
      {carregando && <Spinner />}
      {children}
    </button>
  )
}

export function Spinner() {
  return (
    <span
      aria-hidden="true"
      className="size-4 animate-spin rounded-full border-2 border-white/30 border-t-white motion-reduce:animate-none"
    />
  )
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <section className={`rounded-2xl bg-panel/90 p-5 ring-1 ring-line backdrop-blur ${className}`}>{children}</section>
}

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  rotulo: string
  ajuda?: string
}

export function Field({ rotulo, ajuda, className = '', ...rest }: FieldProps) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-medium text-slate-300">{rotulo}</span>
      <input
        className={`block min-h-11 w-full rounded-xl border-0 bg-ink/70 px-3.5 py-2 text-base text-slate-100 ring-1 ring-line placeholder:text-slate-500 focus:ring-2 focus:ring-fuchsia-400 focus:outline-none ${className}`}
        {...rest}
      />
      {ajuda && <span className="mt-1 block text-xs text-slate-400">{ajuda}</span>}
    </label>
  )
}

export function Alerta({ children, tipo = 'erro' }: { children: ReactNode; tipo?: 'erro' | 'info' }) {
  const cores = tipo === 'erro' ? 'bg-rose-950/60 text-rose-200 ring-rose-800' : 'bg-violet-950/60 text-violet-200 ring-violet-800'
  return (
    <div role={tipo === 'erro' ? 'alert' : 'status'} className={`rounded-xl px-4 py-3 text-sm ring-1 ${cores}`}>
      {children}
    </div>
  )
}

export function Tela({ children }: { children: ReactNode }) {
  return <div className="mx-auto w-full max-w-5xl px-4 py-6 sm:px-6">{children}</div>
}

export function Carregando({ texto = 'Carregando…' }: { texto?: string }) {
  return (
    <div role="status" className="flex items-center justify-center gap-3 py-16 text-slate-300">
      <Spinner />
      <span>{texto}</span>
    </div>
  )
}

import type { StatusConexao } from '../realtime/useFilaRealtime'

const VISUAL: Record<StatusConexao, { texto: string; cor: string }> = {
  conectando: { texto: 'Conectando…', cor: 'bg-amber-400' },
  conectado: { texto: 'Ao vivo', cor: 'bg-emerald-400' },
  reconectando: { texto: 'Reconectando…', cor: 'bg-amber-400 animate-pulse' },
  desconectado: { texto: 'Sem conexão', cor: 'bg-rose-500' },
}

export function ConnectionBadge({ status }: { status: StatusConexao }) {
  const { texto, cor } = VISUAL[status]
  return (
    <span role="status" className="inline-flex items-center gap-2 rounded-full bg-panel-2 px-3 py-1 text-xs font-medium text-slate-200 ring-1 ring-line">
      <span aria-hidden="true" className={`size-2 rounded-full ${cor}`} />
      {texto}
    </span>
  )
}

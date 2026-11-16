import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useCriarEvento, useMe, useMeusEventos } from '../api/queries'
import type { ModoFila } from '../api/types'
import { Alerta, Button, Card, Carregando, Field, Tela } from '../components/ui'
import { DESCRICAO_MODO, ROTULO_MODO } from '../lib/fila'

export default function HostDashboard() {
  const me = useMe()
  const eventos = useMeusEventos(!!me.data)
  const criar = useCriarEvento()
  const navigate = useNavigate()
  const [nome, setNome] = useState('')
  const [modo, setModo] = useState<ModoFila>('RoundRobin')

  if (me.isPending) return <Carregando />
  if (!me.data) return <Navigate to="/login?next=/host" replace />

  function enviar(e: FormEvent) {
    e.preventDefault()
    criar.mutate({ nome, modo }, { onSuccess: (evento) => navigate(`/host/${evento.codigo}`) })
  }

  return (
    <Tela>
      <h1 className="text-2xl font-bold">Olá, {me.data.nome ?? me.data.email}</h1>

      <div className="mt-6 grid gap-5 lg:grid-cols-[360px_1fr]">
        <Card>
          <h2 className="text-lg font-bold">Novo evento</h2>
          <form onSubmit={enviar} className="mt-4 space-y-4">
            <Field rotulo="Nome do evento" required minLength={3} maxLength={100} placeholder="Churrasco da família" value={nome} onChange={(e) => setNome(e.target.value)} />

            <fieldset>
              <legend className="mb-1.5 text-sm font-medium text-slate-300">Ordem da fila</legend>
              <div className="space-y-2">
                {(['RoundRobin', 'Fifo'] as const).map((m) => (
                  <label key={m} className="flex cursor-pointer gap-3 rounded-xl bg-ink/60 p-3 ring-1 ring-line has-[:checked]:ring-2 has-[:checked]:ring-fuchsia-400">
                    <input type="radio" name="modo" value={m} checked={modo === m} onChange={() => setModo(m)} className="mt-1 accent-fuchsia-500" />
                    <span>
                      <span className="block text-sm font-semibold">{ROTULO_MODO[m]}</span>
                      <span className="block text-xs text-slate-400">{DESCRICAO_MODO[m]}</span>
                    </span>
                  </label>
                ))}
              </div>
            </fieldset>

            {criar.error && <Alerta>{criar.error.message}</Alerta>}
            <Button type="submit" className="w-full" carregando={criar.isPending}>
              Criar evento
            </Button>
          </form>
        </Card>

        <Card>
          <h2 className="text-lg font-bold">Meus eventos</h2>
          {eventos.isPending && <Carregando />}
          {eventos.isError && <Alerta>{eventos.error.message}</Alerta>}
          {eventos.data?.length === 0 && <p className="py-6 text-sm text-slate-400">Você ainda não criou nenhum evento.</p>}
          <ul className="mt-3 divide-y divide-line/60">
            {eventos.data?.map((e) => (
              <li key={e.codigo} className="flex items-center justify-between gap-3 py-3">
                <div className="min-w-0">
                  <p className="truncate font-semibold">{e.nome}</p>
                  <p className="text-xs text-slate-400">
                    Código <span className="font-mono font-bold tracking-widest text-slate-200">{e.codigo}</span> ·{' '}
                    {e.ativo ? 'Ativo' : 'Encerrado'} · {ROTULO_MODO[e.modo]}
                  </p>
                </div>
                {e.ativo && (
                  <Link to={`/host/${e.codigo}`} className="shrink-0 rounded-xl bg-panel-2 px-4 py-2 text-sm font-semibold ring-1 ring-line hover:bg-line">
                    Abrir
                  </Link>
                )}
              </li>
            ))}
          </ul>
        </Card>
      </div>
    </Tela>
  )
}

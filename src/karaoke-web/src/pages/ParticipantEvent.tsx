import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { chaves, useEvento, useFila } from '../api/queries'
import type { AdicionarItem, Evento } from '../api/types'
import { ConnectionBadge } from '../components/ConnectionBadge'
import { NowPlaying } from '../components/NowPlaying'
import { QueueList } from '../components/QueueList'
import { SongPicker } from '../components/SongPicker'
import { Alerta, Button, Card, Carregando, Field, Tela } from '../components/ui'
import { MAX_AGUARDANDO_POR_PARTICIPANTE, podeAdicionarMais, posicaoDoParticipante, textoDaPosicao } from '../lib/fila'
import { useFilaRealtime } from '../realtime/useFilaRealtime'
import { useSessao, useSessoes, type SessaoParticipante } from '../state/sessoes'

export default function ParticipantEvent() {
  const { codigo = '' } = useParams()
  const cod = codigo.toUpperCase()
  const evento = useEvento(cod)
  const sessao = useSessao(cod)

  if (evento.isPending) return <Carregando texto="Procurando o evento…" />

  if (evento.isError) {
    return (
      <Tela>
        <Card className="mx-auto max-w-md text-center">
          <h1 className="text-xl font-bold">Evento não encontrado</h1>
          <p className="mt-2 text-sm text-slate-400">
            Confira o código <strong className="font-mono">{cod}</strong> e tente de novo.
          </p>
          <Link to="/" className="mt-4 inline-block font-semibold text-fuchsia-300 hover:underline">
            Voltar ao início
          </Link>
        </Card>
      </Tela>
    )
  }

  if (!evento.data.ativo) {
    return (
      <Tela>
        <Card className="mx-auto max-w-md text-center">
          <h1 className="text-xl font-bold">{evento.data.nome}</h1>
          <p className="mt-2 text-sm text-slate-400">Este evento já foi encerrado. Obrigado por cantar com a gente!</p>
        </Card>
      </Tela>
    )
  }

  return sessao ? <Sala evento={evento.data} sessao={sessao} /> : <FormEntrada evento={evento.data} />
}

function FormEntrada({ evento }: { evento: Evento }) {
  const salvar = useSessoes((s) => s.salvar)
  const [nome, setNome] = useState('')

  const entrar = useMutation({
    mutationFn: () => api.eventos.entrar(evento.codigo, nome.trim()),
    onSuccess: (r) => salvar(evento.codigo, { participanteId: r.participanteId, token: r.token, nome: r.nome }),
  })

  function enviar(e: FormEvent) {
    e.preventDefault()
    entrar.mutate()
  }

  return (
    <Tela>
      <Card className="mx-auto max-w-md">
        <p className="text-xs font-semibold tracking-wider text-fuchsia-300 uppercase">Você foi convidado para</p>
        <h1 className="mt-1 text-2xl font-bold">{evento.nome}</h1>
        <form onSubmit={enviar} className="mt-5 space-y-4">
          <Field rotulo="Como devemos te chamar?" required maxLength={40} autoFocus autoComplete="given-name" value={nome} onChange={(e) => setNome(e.target.value)} placeholder="Seu nome" />
          {entrar.error && <Alerta>{entrar.error.message}</Alerta>}
          <Button type="submit" className="w-full" carregando={entrar.isPending}>
            Entrar na festa
          </Button>
        </form>
      </Card>
    </Tela>
  )
}

function Sala({ evento, sessao }: { evento: Evento; sessao: SessaoParticipante }) {
  const qc = useQueryClient()
  const remover = useSessoes((s) => s.remover)
  const status = useFilaRealtime(evento.codigo)
  const fila = useFila(evento.codigo)

  const atualizar = () => qc.invalidateQueries({ queryKey: chaves.fila(evento.codigo) })

  const adicionar = useMutation({
    mutationFn: (item: AdicionarItem) => api.fila.adicionar(evento.codigo, sessao.token, item),
    onSettled: atualizar,
  })

  const tirar = useMutation({
    mutationFn: (itemId: number) => api.fila.remover(evento.codigo, itemId, sessao.token),
    onSettled: atualizar,
  })

  const f = fila.data
  if (fila.isPending) return <Carregando />
  if (!f) return <Tela><Alerta>Não foi possível carregar a fila.</Alerta></Tela>

  if (!f.ativo) {
    return (
      <Tela>
        <Card className="mx-auto max-w-md text-center">
          <h1 className="text-xl font-bold">{f.nome}</h1>
          <p className="mt-2 text-sm text-slate-400">O anfitrião encerrou o evento. Obrigado por cantar com a gente!</p>
        </Card>
      </Tela>
    )
  }

  const posicao = posicaoDoParticipante(f, sessao.participanteId)
  const podeMais = podeAdicionarMais(f, sessao.participanteId)
  const sessaoInvalida = adicionar.error instanceof ApiError && adicionar.error.status === 403

  return (
    <Tela>
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">{f.nome}</h1>
          <p className="text-sm text-slate-400">
            Olá, <strong className="text-slate-200">{sessao.nome}</strong> · código <span className="font-mono">{f.codigo}</span>
          </p>
        </div>
        <ConnectionBadge status={status} />
      </div>

      <div className="grid gap-5 md:grid-cols-2">
        <div className="space-y-4">
          <NowPlaying atual={f.atual} proximo={f.aguardando[0]} />

          <p aria-live="polite" className="rounded-xl bg-panel/90 px-4 py-3 text-sm text-slate-200 ring-1 ring-line">
            {textoDaPosicao(posicao, !!f.atual)}
          </p>

          <Card>
            <h2 className="mb-3 text-lg font-bold">Escolher música</h2>
            {!podeMais && (
              <div className="mb-3">
                <Alerta tipo="info">
                  Você já tem {MAX_AGUARDANDO_POR_PARTICIPANTE} músicas na fila. Espere uma ser cantada para pedir outra.
                </Alerta>
              </div>
            )}
            <SongPicker onAdicionar={(item) => adicionar.mutateAsync(item)} desabilitado={!podeMais} />
            {adicionar.error && (
              <div className="mt-3 space-y-2">
                <Alerta>{adicionar.error.message}</Alerta>
                {sessaoInvalida && (
                  <Button variante="secondary" className="w-full" onClick={() => remover(evento.codigo)}>
                    Entrar de novo no evento
                  </Button>
                )}
              </div>
            )}
          </Card>
        </div>

        <Card>
          <h2 className="mb-1 text-lg font-bold">Fila ({f.aguardando.length})</h2>
          {tirar.error && <Alerta>{tirar.error.message}</Alerta>}
          <QueueList
            itens={f.aguardando}
            meuId={sessao.participanteId}
            podeRemover={(item) => item.participanteId === sessao.participanteId}
            onRemover={(item) => tirar.mutate(item.id)}
            removendoId={tirar.isPending ? tirar.variables : undefined}
          />
        </Card>
      </div>
    </Tela>
  )
}

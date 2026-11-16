import { useQueryClient } from '@tanstack/react-query'
import { QRCodeSVG } from 'qrcode.react'
import { useRef, useState } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { chaves, useAcoesDoHost, useEvento, useFila, useMe, useMeusEventos } from '../api/queries'
import type { Fila, ModoFila } from '../api/types'
import { ConnectionBadge } from '../components/ConnectionBadge'
import { NowPlaying } from '../components/NowPlaying'
import { QueueList } from '../components/QueueList'
import { Alerta, Button, Card, Carregando, Tela } from '../components/ui'
import { YouTubePlayer } from '../components/YouTubePlayer'
import { DESCRICAO_MODO, ROTULO_MODO } from '../lib/fila'
import { ERROS_VIDEO_INDISPONIVEL } from '../lib/youtube'
import { useFilaRealtime } from '../realtime/useFilaRealtime'

export default function HostEvent() {
  const { codigo = '' } = useParams()
  const cod = codigo.toUpperCase()

  const me = useMe()
  const meusEventos = useMeusEventos(!!me.data)

  if (me.isPending) return <Carregando />
  if (!me.data) return <Navigate to={`/login?next=${encodeURIComponent(`/host/${cod}`)}`} replace />
  if (meusEventos.isPending) return <Carregando />

  const ehMeu = meusEventos.data?.some((e) => e.codigo === cod)
  if (!ehMeu) {
    return (
      <Tela>
        <Alerta>Esse evento não existe ou não é seu.</Alerta>
        <Link to="/host" className="mt-4 inline-block text-fuchsia-300 hover:underline">
          Voltar aos meus eventos
        </Link>
      </Tela>
    )
  }

  return <Palco codigo={cod} />
}

function Palco({ codigo }: { codigo: string }) {
  const qc = useQueryClient()
  const evento = useEvento(codigo)
  const fila = useFila(codigo)
  const status = useFilaRealtime(codigo)
  const acoes = useAcoesDoHost(codigo)

  const [avancarSozinho, setAvancarSozinho] = useState(true)
  const [aviso, setAviso] = useState<string | null>(null)
  const palco = useRef<HTMLDivElement>(null)
  const ocupado = useRef(false)

  const linkConvidado = `${window.location.origin}/e/${codigo}`
  const f = fila.data
  const erro = Object.values(acoes).find((m) => m.error)?.error

  /**
   * Termina a música atual (cantada ou pulada) e, se o modo automático estiver ligado,
   * já chama a próxima. Usa a fila fresca do servidor para decidir, não a do cache.
   */
  async function encerrarAtual(tipo: 'finalizar' | 'pular') {
    if (ocupado.current) return
    ocupado.current = true
    try {
      await acoes[tipo].mutateAsync()
      if (!avancarSozinho) return

      const atualizada = await qc.fetchQuery<Fila>({
        queryKey: chaves.fila(codigo),
        queryFn: () => api.fila.obter(codigo),
        staleTime: 0,
      })
      if (atualizada.ativo && !atualizada.atual && atualizada.aguardando.length > 0) {
        await acoes.proxima.mutateAsync()
      }
    } catch {
      // o erro aparece no alerta da tela
    } finally {
      ocupado.current = false
    }
  }

  function aoErroDoPlayer(codigoErro: number) {
    if (codigoErro === -1 || ERROS_VIDEO_INDISPONIVEL.includes(codigoErro)) {
      setAviso('Esse vídeo não pode ser tocado aqui (bloqueado para incorporação ou removido). Pulando…')
      void encerrarAtual('pular')
    }
  }

  function encerrarEvento() {
    if (window.confirm('Encerrar o evento? Ninguém mais poderá entrar ou pedir músicas.')) acoes.encerrar.mutate()
  }

  if (evento.isPending || fila.isPending) return <Carregando />
  if (evento.isError || fila.isError || !f) return <Tela><Alerta>Não foi possível carregar o evento.</Alerta></Tela>

  return (
    <Tela>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold">{f.nome}</h1>
          <p className="text-sm text-slate-400">
            {f.totalParticipantes} {f.totalParticipantes === 1 ? 'pessoa' : 'pessoas'} · {f.totalCantadas} cantadas
          </p>
        </div>
        <ConnectionBadge status={status} />
      </div>

      {!f.ativo && <div className="mb-4"><Alerta tipo="info">Este evento foi encerrado.</Alerta></div>}
      {aviso && <div className="mb-4"><Alerta tipo="info">{aviso}</Alerta></div>}
      {erro && <div className="mb-4"><Alerta>{erro.message}</Alerta></div>}

      <div className="grid gap-5 lg:grid-cols-[1fr_360px]">
        <div className="space-y-4">
          <div ref={palco} className="aspect-video overflow-hidden rounded-2xl bg-black ring-1 ring-line">
            {f.atual ? (
              <YouTubePlayer key={f.atual.id} videoId={f.atual.videoId} onEnded={() => void encerrarAtual('finalizar')} onError={aoErroDoPlayer} />
            ) : (
              <SalaDeEspera codigo={codigo} link={linkConvidado} />
            )}
          </div>

          <NowPlaying atual={f.atual} proximo={f.aguardando[0]} />

          <div className="flex flex-wrap items-center gap-2">
            <Button
              onClick={() => {
                setAviso(null)
                acoes.proxima.mutate()
              }}
              disabled={!f.ativo || !!f.atual || f.aguardando.length === 0}
              carregando={acoes.proxima.isPending}
            >
              Iniciar próxima
            </Button>
            <Button variante="secondary" onClick={() => void encerrarAtual('finalizar')} disabled={!f.atual}>
              Finalizar
            </Button>
            <Button variante="secondary" onClick={() => void encerrarAtual('pular')} disabled={!f.atual}>
              Pular
            </Button>
            <Button variante="ghost" onClick={() => void palco.current?.requestFullscreen()}>
              Tela cheia
            </Button>
            <label className="ml-auto flex cursor-pointer items-center gap-2 text-sm text-slate-300">
              <input type="checkbox" checked={avancarSozinho} onChange={(e) => setAvancarSozinho(e.target.checked)} className="size-4 accent-fuchsia-500" />
              Avançar sozinho
            </label>
          </div>
        </div>

        <aside className="space-y-4">
          <Card>
            <h2 className="text-sm font-semibold tracking-wider text-slate-400 uppercase">Entre pelo celular</h2>
            <p className="mt-2 text-center font-mono text-4xl font-extrabold tracking-[0.25em]" aria-label={`Código do evento: ${codigo.split('').join(' ')}`}>
              {codigo}
            </p>
            <div className="mx-auto mt-3 w-fit rounded-xl bg-white p-3">
              <QRCodeSVG value={linkConvidado} size={140} title={`QR code para entrar no evento ${codigo}`} />
            </div>
            <p className="mt-3 text-center text-xs break-all text-slate-400">{linkConvidado}</p>
          </Card>

          <Card>
            <div className="mb-3 flex items-center justify-between gap-3">
              <h2 className="text-lg font-bold">Fila ({f.aguardando.length})</h2>
              <label className="flex items-center gap-2 text-sm">
                <span className="sr-only">Ordem da fila</span>
                <select
                  value={f.modo}
                  disabled={!f.ativo || acoes.alterarModo.isPending}
                  onChange={(e) => acoes.alterarModo.mutate(e.target.value as ModoFila)}
                  className="min-h-9 rounded-lg bg-ink/70 px-2 text-sm ring-1 ring-line focus:ring-2 focus:ring-fuchsia-400 focus:outline-none"
                >
                  {(['RoundRobin', 'Fifo'] as const).map((m) => (
                    <option key={m} value={m}>
                      {ROTULO_MODO[m]}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <p className="mb-2 text-xs text-slate-400">{DESCRICAO_MODO[f.modo]}</p>

            <QueueList
              itens={f.aguardando}
              podeRemover={() => f.ativo}
              onRemover={(item) => acoes.remover.mutate(item.id)}
              removendoId={acoes.remover.isPending ? acoes.remover.variables : undefined}
            />
          </Card>

          {f.ativo && (
            <Button variante="danger" className="w-full" onClick={encerrarEvento} carregando={acoes.encerrar.isPending}>
              Encerrar evento
            </Button>
          )}
        </aside>
      </div>
    </Tela>
  )
}

/** Tela mostrada na TV quando não há música tocando: código e QR bem grandes. */
function SalaDeEspera({ codigo, link }: { codigo: string; link: string }) {
  return (
    <div className="flex size-full flex-col items-center justify-center gap-4 bg-gradient-to-br from-violet-950 to-fuchsia-950 p-6 text-center">
      <p className="text-sm font-semibold tracking-wider text-fuchsia-300 uppercase">Escolha sua música no celular</p>
      <div className="flex flex-wrap items-center justify-center gap-6">
        <div className="rounded-xl bg-white p-3">
          <QRCodeSVG value={link} size={120} />
        </div>
        <div>
          <p className="text-xs text-slate-300">Código do evento</p>
          <p className="font-mono text-5xl font-extrabold tracking-[0.2em] sm:text-6xl">{codigo}</p>
        </div>
      </div>
    </div>
  )
}

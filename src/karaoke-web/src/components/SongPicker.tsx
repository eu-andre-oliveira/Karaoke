import { useQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { api } from '../api/client'
import { chaves } from '../api/queries'
import type { AdicionarItem } from '../api/types'
import { useDebounce } from '../lib/useDebounce'
import { Button, Field } from './ui'

type Aba = 'biblioteca' | 'link'

interface Props {
  onAdicionar: (item: AdicionarItem) => Promise<unknown>
  desabilitado?: boolean
}

/** Escolha de música: busca na biblioteca compartilhada ou link avulso do YouTube. */
export function SongPicker({ onAdicionar, desabilitado }: Props) {
  const [aba, setAba] = useState<Aba>('biblioteca')

  return (
    <div>
      <div role="tablist" aria-label="Como escolher a música" className="mb-4 grid grid-cols-2 gap-1 rounded-xl bg-ink/70 p-1 ring-1 ring-line">
        {(
          [
            ['biblioteca', 'Buscar'],
            ['link', 'Colar link'],
          ] as const
        ).map(([id, rotulo]) => (
          <button
            key={id}
            role="tab"
            type="button"
            id={`aba-${id}`}
            aria-selected={aba === id}
            aria-controls={`painel-${id}`}
            onClick={() => setAba(id)}
            className={`min-h-10 rounded-lg text-sm font-semibold transition focus-visible:outline-2 focus-visible:outline-fuchsia-400 ${
              aba === id ? 'bg-panel-2 text-white' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            {rotulo}
          </button>
        ))}
      </div>

      <div role="tabpanel" id={`painel-${aba}`} aria-labelledby={`aba-${aba}`}>
        {aba === 'biblioteca' ? (
          <BuscaBiblioteca onAdicionar={onAdicionar} desabilitado={desabilitado} />
        ) : (
          <LinkYouTube onAdicionar={onAdicionar} desabilitado={desabilitado} />
        )}
      </div>
    </div>
  )
}

function BuscaBiblioteca({ onAdicionar, desabilitado }: Props) {
  const [termo, setTermo] = useState('')
  const termoDebounced = useDebounce(termo.trim(), 300)
  const [adicionando, setAdicionando] = useState<number | null>(null)

  const busca = useQuery({
    queryKey: chaves.biblioteca(termoDebounced),
    queryFn: () => api.biblioteca.buscar(termoDebounced),
    enabled: termoDebounced.length >= 2,
    staleTime: 30_000,
  })

  async function adicionar(bibliotecaId: number) {
    setAdicionando(bibliotecaId)
    try {
      await onAdicionar({ bibliotecaId })
    } catch {
      // o erro é exibido pelo componente pai
    } finally {
      setAdicionando(null)
    }
  }

  return (
    <div className="space-y-3">
      <Field
        rotulo="Título ou artista"
        type="search"
        value={termo}
        onChange={(e) => setTermo(e.target.value)}
        placeholder="Ex.: Evidências"
        autoComplete="off"
      />

      <div aria-live="polite">
        {termoDebounced.length < 2 && <p className="text-sm text-slate-400">Digite pelo menos 2 letras.</p>}
        {busca.isFetching && <p className="text-sm text-slate-400">Buscando…</p>}
        {busca.isError && <p className="text-sm text-rose-300">Não foi possível buscar agora.</p>}
        {busca.data && busca.data.length === 0 && !busca.isFetching && (
          <p className="text-sm text-slate-400">
            Nada encontrado. Use a aba <strong>Colar link</strong> para adicionar um vídeo do YouTube.
          </p>
        )}
      </div>

      <ul className="divide-y divide-line/60">
        {busca.data?.map((m) => (
          <li key={m.id} className="flex items-center gap-3 py-2.5">
            {m.thumbnailUrl && <img src={m.thumbnailUrl} alt="" loading="lazy" className="aspect-video w-16 shrink-0 rounded-md object-cover" />}
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold">{m.titulo}</p>
              <p className="truncate text-xs text-slate-400">{m.artista ?? 'Artista não informado'}</p>
            </div>
            <Button
              className="min-h-9 shrink-0 px-3 text-xs"
              disabled={desabilitado}
              carregando={adicionando === m.id}
              onClick={() => void adicionar(m.id)}
              aria-label={`Cantar ${m.titulo}`}
            >
              Cantar
            </Button>
          </li>
        ))}
      </ul>
    </div>
  )
}

function LinkYouTube({ onAdicionar, desabilitado }: Props) {
  const [titulo, setTitulo] = useState('')
  const [artista, setArtista] = useState('')
  const [url, setUrl] = useState('')
  const [enviando, setEnviando] = useState(false)

  async function enviar(e: FormEvent) {
    e.preventDefault()
    setEnviando(true)
    try {
      await onAdicionar({ titulo: titulo.trim(), artista: artista.trim() || null, url: url.trim() })
      setTitulo('')
      setArtista('')
      setUrl('')
    } catch {
      // o erro é exibido pelo componente pai
    } finally {
      setEnviando(false)
    }
  }

  return (
    <form onSubmit={enviar} className="space-y-3">
      <Field rotulo="Link do YouTube" type="url" required value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://www.youtube.com/watch?v=…" />
      <Field rotulo="Título da música" required maxLength={200} value={titulo} onChange={(e) => setTitulo(e.target.value)} />
      <Field rotulo="Artista (opcional)" maxLength={200} value={artista} onChange={(e) => setArtista(e.target.value)} />
      <Button type="submit" className="w-full" carregando={enviando} disabled={desabilitado}>
        Entrar na fila
      </Button>
    </form>
  )
}

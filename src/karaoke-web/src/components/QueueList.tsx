import type { ItemFila } from '../api/types'
import { Button } from './ui'

interface Props {
  itens: ItemFila[]
  /** Id do participante logado (convidado). Destaca "você" nas músicas dele. */
  meuId?: string
  /** Define se aparece o botão de remover em cada item. */
  podeRemover: (item: ItemFila) => boolean
  onRemover: (item: ItemFila) => void
  removendoId?: number
}

export function QueueList({ itens, meuId, podeRemover, onRemover, removendoId }: Props) {
  if (itens.length === 0) {
    return <p className="py-6 text-center text-sm text-slate-400">A fila está vazia.</p>
  }

  return (
    <ol className="divide-y divide-line/60">
      {itens.map((item, indice) => {
        const minha = item.participanteId === meuId
        return (
          <li key={item.id} className="flex items-center gap-3 py-3">
            <span aria-hidden="true" className="w-6 shrink-0 text-center text-sm font-bold text-slate-500">
              {indice + 1}
            </span>
            {item.thumbnailUrl && (
              <img src={item.thumbnailUrl} alt="" loading="lazy" className="aspect-video w-16 shrink-0 rounded-md object-cover" />
            )}
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold">{item.titulo}</p>
              <p className="truncate text-xs text-slate-400">
                {item.cantor}
                {minha && (
                  <span className="ml-2 rounded-full bg-fuchsia-500/20 px-2 py-0.5 text-[11px] font-semibold text-fuchsia-200">você</span>
                )}
              </p>
            </div>
            {podeRemover(item) && (
              <Button
                variante="ghost"
                className="min-h-9 shrink-0 px-3 text-xs"
                carregando={removendoId === item.id}
                onClick={() => onRemover(item)}
                aria-label={`Remover ${item.titulo} da fila`}
              >
                Remover
              </Button>
            )}
          </li>
        )
      })}
    </ol>
  )
}

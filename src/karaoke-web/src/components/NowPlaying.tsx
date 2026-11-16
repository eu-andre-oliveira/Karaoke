import type { ItemFila } from '../api/types'

function Equalizador() {
  return (
    <span aria-hidden="true" className="flex h-5 items-end gap-0.5">
      {[0, 150, 300, 450].map((atraso) => (
        <span key={atraso} className="eq-bar h-full w-1 rounded-sm bg-fuchsia-400" style={{ animationDelay: `${atraso}ms` }} />
      ))}
    </span>
  )
}

interface Props {
  atual: ItemFila | null | undefined
  proximo?: ItemFila
}

/** Cartão "cantando agora". A região é aria-live para leitores de tela anunciarem a troca. */
export function NowPlaying({ atual, proximo }: Props) {
  return (
    <div aria-live="polite" className="overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600/30 to-fuchsia-600/20 ring-1 ring-violet-400/30">
      {atual ? (
        <div className="flex items-center gap-4 p-4">
          {atual.thumbnailUrl && (
            <img src={atual.thumbnailUrl} alt="" className="aspect-video w-28 shrink-0 rounded-lg object-cover sm:w-36" />
          )}
          <div className="min-w-0">
            <p className="flex items-center gap-2 text-xs font-semibold tracking-wider text-fuchsia-300 uppercase">
              <Equalizador /> Cantando agora
            </p>
            <p className="mt-1 truncate text-lg font-bold sm:text-xl">{atual.titulo}</p>
            <p className="truncate text-sm text-slate-300">
              {atual.cantor}
              {atual.artista ? ` · ${atual.artista}` : ''}
            </p>
          </div>
        </div>
      ) : (
        <div className="p-5">
          <p className="text-xs font-semibold tracking-wider text-slate-400 uppercase">Palco livre</p>
          <p className="mt-1 text-base text-slate-200">
            {proximo ? (
              <>
                A seguir: <strong>{proximo.titulo}</strong> com {proximo.cantor}
              </>
            ) : (
              'Ninguém cantando agora. Escolha uma música!'
            )}
          </p>
        </div>
      )}
    </div>
  )
}

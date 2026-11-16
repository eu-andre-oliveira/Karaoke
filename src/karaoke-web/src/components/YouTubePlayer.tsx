import { useEffect, useRef } from 'react'
import { carregarYouTubeApi, type YTPlayer } from '../lib/youtube'

interface Props {
  videoId: string
  onEnded: () => void
  onError: (codigo: number) => void
}

/**
 * Player do YouTube (IFrame API). Cria um player por música: o pai usa `key={item.id}`,
 * então músicas repetidas em sequência também recomeçam do zero.
 */
export function YouTubePlayer({ videoId, onEnded, onError }: Props) {
  const container = useRef<HTMLDivElement>(null)
  const player = useRef<YTPlayer | null>(null)

  // Os callbacks mudam a cada render do pai; guardamos o mais recente sem recriar o player.
  const callbacks = useRef({ onEnded, onError })
  useEffect(() => {
    callbacks.current = { onEnded, onError }
  })

  useEffect(() => {
    let cancelado = false

    carregarYouTubeApi()
      .then((YT) => {
        if (cancelado || !container.current) return

        const alvo = document.createElement('div')
        container.current.appendChild(alvo)

        player.current = new YT.Player(alvo, {
          width: '100%',
          height: '100%',
          videoId,
          playerVars: { autoplay: 1, rel: 0, playsinline: 1, modestbranding: 1 },
          events: {
            onStateChange: (e) => {
              if (e.data === YT.PlayerState.ENDED) callbacks.current.onEnded()
            },
            onError: (e) => callbacks.current.onError(e.data),
          },
        })
      })
      .catch(() => callbacks.current.onError(-1))

    return () => {
      cancelado = true
      player.current?.destroy()
      player.current = null
    }
  }, [videoId])

  return <div ref={container} className="size-full [&>iframe]:size-full" />
}

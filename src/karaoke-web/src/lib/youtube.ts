// Tipagem mínima da YouTube IFrame API (só o que o app usa).
export interface YTPlayer {
  loadVideoById(videoId: string): void
  playVideo(): void
  destroy(): void
}

export interface YTNamespace {
  Player: new (
    elemento: HTMLElement,
    opcoes: {
      width?: string | number
      height?: string | number
      videoId?: string
      playerVars?: Record<string, string | number>
      events?: {
        onReady?: () => void
        onStateChange?: (e: { data: number }) => void
        onError?: (e: { data: number }) => void
      }
    },
  ) => YTPlayer
  PlayerState: { ENDED: number }
}

declare global {
  interface Window {
    YT?: YTNamespace
    onYouTubeIframeAPIReady?: () => void
  }
}

let carregando: Promise<YTNamespace> | null = null

/** Carrega o script da IFrame API uma única vez e resolve quando `window.YT` está pronto. */
export function carregarYouTubeApi(): Promise<YTNamespace> {
  if (window.YT?.Player) return Promise.resolve(window.YT)
  if (carregando) return carregando

  carregando = new Promise<YTNamespace>((resolve, reject) => {
    const anterior = window.onYouTubeIframeAPIReady
    window.onYouTubeIframeAPIReady = () => {
      anterior?.()
      if (window.YT) resolve(window.YT)
    }

    const script = document.createElement('script')
    script.src = 'https://www.youtube.com/iframe_api'
    script.async = true
    script.onerror = () => {
      carregando = null
      reject(new Error('Não foi possível carregar o player do YouTube.'))
    }
    document.head.appendChild(script)
  })

  return carregando
}

/** Códigos de erro do player que significam "esse vídeo não pode ser tocado aqui". */
export const ERROS_VIDEO_INDISPONIVEL = [2, 5, 100, 101, 150]

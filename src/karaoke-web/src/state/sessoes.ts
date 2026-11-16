import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface SessaoParticipante {
  participanteId: string
  token: string
  nome: string
}

interface SessoesState {
  /** Uma sessão de convidado por evento, indexada pelo código (maiúsculo). */
  sessoes: Record<string, SessaoParticipante>
  salvar: (codigo: string, sessao: SessaoParticipante) => void
  remover: (codigo: string) => void
}

/**
 * Guarda no navegador quem o convidado é em cada evento. Assim, recarregar a página
 * (ou fechar e abrir o celular de novo) não obriga a entrar outra vez.
 */
export const useSessoes = create<SessoesState>()(
  persist(
    (set) => ({
      sessoes: {},
      salvar: (codigo, sessao) =>
        set((s) => ({ sessoes: { ...s.sessoes, [codigo.toUpperCase()]: sessao } })),
      remover: (codigo) =>
        set((s) => {
          const { [codigo.toUpperCase()]: _removida, ...resto } = s.sessoes
          void _removida
          return { sessoes: resto }
        }),
    }),
    { name: 'karaoke.sessoes' },
  ),
)

export function useSessao(codigo: string): SessaoParticipante | undefined {
  return useSessoes((s) => s.sessoes[codigo.toUpperCase()])
}

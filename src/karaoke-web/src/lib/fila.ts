import type { Fila, ItemFila } from '../api/types'

/** Mesmo limite do back-end (Evento.MaxItensAguardandoPorParticipante). */
export const MAX_AGUARDANDO_POR_PARTICIPANTE = 3

export function itensDoParticipante(fila: Fila, participanteId: string): ItemFila[] {
  return fila.aguardando.filter((i) => i.participanteId === participanteId)
}

/** Posição (a partir de 1) da primeira música do participante na fila de espera, ou null. */
export function posicaoDoParticipante(fila: Fila, participanteId: string): number | null {
  const indice = fila.aguardando.findIndex((i) => i.participanteId === participanteId)
  return indice === -1 ? null : indice + 1
}

export function podeAdicionarMais(fila: Fila, participanteId: string): boolean {
  return itensDoParticipante(fila, participanteId).length < MAX_AGUARDANDO_POR_PARTICIPANTE
}

export function textoDaPosicao(posicao: number | null, cantandoAgora: boolean): string {
  if (posicao === null) return 'Escolha uma música para entrar na fila.'
  if (posicao === 1) return cantandoAgora ? 'Você é o próximo!' : 'Você canta assim que o anfitrião iniciar.'
  return `Faltam ${posicao - 1} músicas antes da sua.`
}

export const ROTULO_MODO = {
  Fifo: 'Ordem de chegada',
  RoundRobin: 'Revezamento',
} as const

export const DESCRICAO_MODO = {
  Fifo: 'Primeiro a pedir, primeiro a cantar.',
  RoundRobin: 'Todo mundo canta uma vez antes de alguém repetir.',
} as const

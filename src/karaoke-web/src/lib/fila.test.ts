import { describe, expect, it } from 'vitest'
import type { Fila, ItemFila } from '../api/types'
import { podeAdicionarMais, posicaoDoParticipante, textoDaPosicao } from './fila'

let proximoId = 1
const item = (participanteId: string, titulo = 'Música'): ItemFila => ({
  id: proximoId++,
  titulo,
  videoId: 'dQw4w9WgXcQ',
  participanteId,
  cantor: participanteId,
  status: 'Aguardando',
  adicionadoEm: '2026-01-01T20:00:00Z',
})

const fila = (aguardando: ItemFila[]): Fila => ({
  codigo: 'ABC234',
  nome: 'Festa',
  modo: 'Fifo',
  ativo: true,
  aguardando,
  totalCantadas: 0,
  totalParticipantes: 2,
})

describe('posicaoDoParticipante', () => {
  it('conta a partir de 1 pela primeira música do participante', () => {
    const f = fila([item('ana'), item('beto'), item('beto'), item('caio')])

    expect(posicaoDoParticipante(f, 'ana')).toBe(1)
    expect(posicaoDoParticipante(f, 'beto')).toBe(2)
    expect(posicaoDoParticipante(f, 'caio')).toBe(4)
  })

  it('devolve null quando não há música dele na fila', () => {
    expect(posicaoDoParticipante(fila([item('ana')]), 'beto')).toBeNull()
  })
})

describe('podeAdicionarMais', () => {
  it('bloqueia ao atingir o limite de músicas aguardando', () => {
    const f = fila([item('ana'), item('ana'), item('ana'), item('beto')])

    expect(podeAdicionarMais(f, 'ana')).toBe(false)
    expect(podeAdicionarMais(f, 'beto')).toBe(true)
  })
})

describe('textoDaPosicao', () => {
  it('orienta quem ainda não pediu música', () => {
    expect(textoDaPosicao(null, false)).toMatch(/escolha uma música/i)
  })

  it('avisa o próximo da fila', () => {
    expect(textoDaPosicao(1, true)).toBe('Você é o próximo!')
  })

  it('diz quantas músicas faltam', () => {
    expect(textoDaPosicao(4, true)).toBe('Faltam 3 músicas antes da sua.')
  })
})

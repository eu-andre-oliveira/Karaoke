import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { ItemFila } from '../api/types'
import { QueueList } from './QueueList'

const itens: ItemFila[] = [
  { id: 1, titulo: 'Evidências', videoId: 'aaaaaaaaaaa', participanteId: 'ana', cantor: 'Ana', status: 'Aguardando', adicionadoEm: '' },
  { id: 2, titulo: 'Faroeste Caboclo', videoId: 'bbbbbbbbbbb', participanteId: 'beto', cantor: 'Beto', status: 'Aguardando', adicionadoEm: '' },
]

describe('QueueList', () => {
  it('mostra mensagem quando a fila está vazia', () => {
    render(<QueueList itens={[]} podeRemover={() => true} onRemover={vi.fn()} />)

    expect(screen.getByText(/a fila está vazia/i)).toBeInTheDocument()
  })

  it('lista as músicas em ordem e destaca as do próprio participante', () => {
    render(<QueueList itens={itens} meuId="beto" podeRemover={() => false} onRemover={vi.fn()} />)

    const linhas = screen.getAllByRole('listitem')
    expect(linhas).toHaveLength(2)
    expect(linhas[0]).toHaveTextContent('Evidências')
    expect(linhas[1]).toHaveTextContent('Faroeste Caboclo')
    expect(linhas[1]).toHaveTextContent('você')
    expect(linhas[0]).not.toHaveTextContent('você')
  })

  it('só oferece "Remover" onde é permitido e avisa qual item foi escolhido', async () => {
    const onRemover = vi.fn()
    render(<QueueList itens={itens} meuId="beto" podeRemover={(i) => i.participanteId === 'beto'} onRemover={onRemover} />)

    expect(screen.getAllByRole('button')).toHaveLength(1)

    await userEvent.click(screen.getByRole('button', { name: /remover faroeste caboclo/i }))

    expect(onRemover).toHaveBeenCalledWith(itens[1])
  })
})

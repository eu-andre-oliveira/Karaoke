import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type * as ClientModule from '../api/client'
import type { Evento, Fila } from '../api/types'

vi.mock('../api/client', async (importOriginal) => {
  const original = await importOriginal<typeof ClientModule>()
  return {
    ...original,
    api: {
      eventos: { obter: vi.fn(), entrar: vi.fn() },
      fila: { obter: vi.fn(), adicionar: vi.fn(), remover: vi.fn() },
      biblioteca: { buscar: vi.fn().mockResolvedValue([]) },
    },
  }
})

// O SignalR não faz parte deste teste: a fila vem do REST mockado.
vi.mock('../realtime/useFilaRealtime', () => ({ useFilaRealtime: () => 'conectado' }))

import { api, ApiError } from '../api/client'
import ParticipantEvent from './ParticipantEvent'

const evento: Evento = { codigo: 'ABC234', nome: 'Churrasco da família', modo: 'Fifo', ativo: true, dataCriacao: '2026-01-01T20:00:00Z' }

const filaVazia: Fila = {
  codigo: 'ABC234',
  nome: 'Churrasco da família',
  modo: 'Fifo',
  ativo: true,
  aguardando: [],
  totalCantadas: 0,
  totalParticipantes: 1,
}

function renderizar() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/e/abc234']}>
        <Routes>
          <Route path="/e/:codigo" element={<ParticipantEvent />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ParticipantEvent', () => {
  beforeEach(() => {
    vi.mocked(api.eventos.obter).mockResolvedValue(evento)
    vi.mocked(api.fila.obter).mockResolvedValue(filaVazia)
    vi.mocked(api.eventos.entrar).mockResolvedValue({ participanteId: 'p1', token: 'tok-p1', nome: 'Ana', evento })
  })

  it('pede o nome, entra no evento e guarda a sessão no navegador', async () => {
    renderizar()

    await userEvent.type(await screen.findByLabelText(/como devemos te chamar/i), 'Ana')
    await userEvent.click(screen.getByRole('button', { name: /entrar na festa/i }))

    expect(await screen.findByText(/escolha uma música para entrar na fila/i)).toBeInTheDocument()
    expect(api.eventos.entrar).toHaveBeenCalledWith('ABC234', 'Ana')
    expect(localStorage.getItem('karaoke.sessoes')).toContain('tok-p1')
  })

  it('mostra erro amigável quando o evento não existe', async () => {
    vi.mocked(api.eventos.obter).mockRejectedValue(new ApiError(404, 'Evento não encontrado.'))

    renderizar()

    expect(await screen.findByRole('heading', { name: /evento não encontrado/i })).toBeInTheDocument()
  })

  it('avisa quando o evento já foi encerrado', async () => {
    vi.mocked(api.eventos.obter).mockResolvedValue({ ...evento, ativo: false })

    renderizar()

    expect(await screen.findByText(/já foi encerrado/i)).toBeInTheDocument()
    expect(screen.queryByLabelText(/como devemos te chamar/i)).not.toBeInTheDocument()
  })

  it('envia o link do YouTube com o token do convidado', async () => {
    localStorage.setItem(
      'karaoke.sessoes',
      JSON.stringify({ state: { sessoes: { ABC234: { participanteId: 'p1', token: 'tok-p1', nome: 'Ana' } } }, version: 0 }),
    )
    vi.mocked(api.fila.adicionar).mockResolvedValue({
      id: 10, titulo: 'Evidências', videoId: 'dQw4w9WgXcQ', participanteId: 'p1', cantor: 'Ana', status: 'Aguardando', adicionadoEm: '',
    })

    renderizar()

    await userEvent.click(await screen.findByRole('tab', { name: /colar link/i }))
    await userEvent.type(screen.getByLabelText(/link do youtube/i), 'https://youtu.be/dQw4w9WgXcQ')
    await userEvent.type(screen.getByLabelText(/título da música/i), 'Evidências')
    await userEvent.click(screen.getByRole('button', { name: /entrar na fila/i }))

    await waitFor(() =>
      expect(api.fila.adicionar).toHaveBeenCalledWith('ABC234', 'tok-p1', {
        titulo: 'Evidências',
        artista: null,
        url: 'https://youtu.be/dQw4w9WgXcQ',
      }),
    )
  })

  it('mostra a mensagem do servidor quando o pedido é recusado', async () => {
    localStorage.setItem(
      'karaoke.sessoes',
      JSON.stringify({ state: { sessoes: { ABC234: { participanteId: 'p1', token: 'tok-p1', nome: 'Ana' } } }, version: 0 }),
    )
    vi.mocked(api.fila.adicionar).mockRejectedValue(new ApiError(400, 'Esse link não é um vídeo do YouTube que o player consiga tocar.'))

    renderizar()

    await userEvent.click(await screen.findByRole('tab', { name: /colar link/i }))
    await userEvent.type(screen.getByLabelText(/link do youtube/i), 'https://exemplo.com/video')
    await userEvent.type(screen.getByLabelText(/título da música/i), 'X')
    await userEvent.click(screen.getByRole('button', { name: /entrar na fila/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/não é um vídeo do youtube/i)
  })
})

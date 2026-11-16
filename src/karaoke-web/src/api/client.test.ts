import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError, toApiError } from './client'

function respostaJson(status: number, corpo?: unknown): Response {
  return new Response(corpo === undefined ? null : JSON.stringify(corpo), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => vi.unstubAllGlobals())

describe('toApiError', () => {
  it('usa o "detail" do ProblemDetails', async () => {
    const erro = await toApiError(respostaJson(409, { detail: 'A fila está vazia.' }))

    expect(erro).toBeInstanceOf(ApiError)
    expect(erro.status).toBe(409)
    expect(erro.message).toBe('A fila está vazia.')
  })

  it('pega a primeira mensagem de erros de validação', async () => {
    const erro = await toApiError(respostaJson(400, { errors: { Email: ['E-mail inválido.'], Password: ['Curta.'] } }))

    expect(erro.message).toBe('E-mail inválido.')
  })

  it('cai na mensagem padrão do status quando o corpo não ajuda', async () => {
    const erro = await toApiError(new Response('<html>erro</html>', { status: 429 }))

    expect(erro.message).toMatch(/muitas tentativas/i)
  })
})

describe('api', () => {
  it('auth.me devolve null quando não há sessão (401)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respostaJson(401, { detail: 'x' })))

    await expect(api.auth.me()).resolves.toBeNull()
  })

  it('auth.me propaga erros que não são 401', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(respostaJson(500)))

    await expect(api.auth.me()).rejects.toMatchObject({ status: 500 })
  })

  it('envia o token do convidado no cabeçalho e o corpo como JSON', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respostaJson(201, { id: 1 }))
    vi.stubGlobal('fetch', fetchMock)

    await api.fila.adicionar('ABC234', 'tok-123', { titulo: 'Evidências', url: 'https://youtu.be/dQw4w9WgXcQ' })

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toBe('/api/eventos/ABC234/fila')
    expect(init.method).toBe('POST')
    expect(init.headers).toMatchObject({ 'X-Participante-Token': 'tok-123', 'Content-Type': 'application/json' })
    expect(JSON.parse(init.body as string)).toEqual({ titulo: 'Evidências', url: 'https://youtu.be/dQw4w9WgXcQ' })
  })

  it('trata 204 sem tentar ler o corpo', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))

    await expect(api.fila.finalizar('ABC234')).resolves.toBeUndefined()
  })

  it('codifica o termo da busca na URL', async () => {
    const fetchMock = vi.fn().mockResolvedValue(respostaJson(200, []))
    vi.stubGlobal('fetch', fetchMock)

    await api.biblioteca.buscar('amor & paz')

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/biblioteca/busca?termo=amor%20%26%20paz')
  })
})

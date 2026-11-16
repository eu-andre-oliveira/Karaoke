import type {
  AdicionarItem,
  EntrouNoEvento,
  Evento,
  Fila,
  ItemFila,
  ModoFila,
  MusicaBiblioteca,
  Usuario,
} from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

const MENSAGENS_PADRAO: Record<number, string> = {
  400: 'Confira os dados informados.',
  401: 'Faça login para continuar.',
  403: 'Você não tem permissão para isso.',
  404: 'Não encontrado.',
  409: 'Essa ação não pode ser feita agora.',
  429: 'Muitas tentativas. Aguarde um instante e tente de novo.',
}

interface ProblemLike {
  detail?: string
  errors?: Record<string, string[]>
}

/** Transforma a resposta de erro (ProblemDetails do ASP.NET) numa mensagem legível. */
export async function toApiError(res: Response): Promise<ApiError> {
  let mensagem: string | undefined

  try {
    const problema = (await res.json()) as ProblemLike
    mensagem = problema.detail ?? Object.values(problema.errors ?? {}).flat()[0]
  } catch {
    // corpo vazio ou não-JSON: cai na mensagem padrão
  }

  return new ApiError(res.status, mensagem ?? MENSAGENS_PADRAO[res.status] ?? 'Algo deu errado. Tente novamente.')
}

interface Opcoes {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** Token do convidado (cabeçalho X-Participante-Token). */
  token?: string
}

async function request<T>(caminho: string, { method = 'GET', body, token }: Opcoes = {}): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers['X-Participante-Token'] = token

  const res = await fetch(caminho, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    credentials: 'same-origin',
  })

  if (!res.ok) throw await toApiError(res)
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

const enc = encodeURIComponent

export const api = {
  auth: {
    /** Devolve null quando não há sessão (401) em vez de lançar erro. */
    async me(): Promise<Usuario | null> {
      try {
        return await request<Usuario>('/api/auth/me')
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) return null
        throw e
      }
    },
    login: (email: string, password: string, rememberMe = true) =>
      request<Usuario>('/api/auth/login', { method: 'POST', body: { email, password, rememberMe } }),
    register: (nomeCompleto: string, email: string, password: string) =>
      request<Usuario>('/api/auth/register', { method: 'POST', body: { nomeCompleto, email, password } }),
    logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  },

  eventos: {
    criar: (nome: string, modo: ModoFila) =>
      request<Evento>('/api/eventos', { method: 'POST', body: { nome, modo } }),
    meus: () => request<Evento[]>('/api/eventos'),
    obter: (codigo: string) => request<Evento>(`/api/eventos/${enc(codigo)}`),
    entrar: (codigo: string, nome: string) =>
      request<EntrouNoEvento>(`/api/eventos/${enc(codigo)}/participantes`, { method: 'POST', body: { nome } }),
    alterarModo: (codigo: string, modo: ModoFila) =>
      request<void>(`/api/eventos/${enc(codigo)}/modo`, { method: 'PUT', body: { modo } }),
    encerrar: (codigo: string) => request<void>(`/api/eventos/${enc(codigo)}/encerrar`, { method: 'POST' }),
  },

  fila: {
    obter: (codigo: string) => request<Fila>(`/api/eventos/${enc(codigo)}/fila`),
    adicionar: (codigo: string, token: string, item: AdicionarItem) =>
      request<ItemFila>(`/api/eventos/${enc(codigo)}/fila`, { method: 'POST', body: item, token }),
    remover: (codigo: string, itemId: number, token?: string) =>
      request<void>(`/api/eventos/${enc(codigo)}/fila/${itemId}`, { method: 'DELETE', token }),
    proxima: (codigo: string) => request<ItemFila>(`/api/eventos/${enc(codigo)}/fila/proxima`, { method: 'POST' }),
    finalizar: (codigo: string) =>
      request<void>(`/api/eventos/${enc(codigo)}/fila/atual/finalizar`, { method: 'POST' }),
    pular: (codigo: string) => request<void>(`/api/eventos/${enc(codigo)}/fila/atual/pular`, { method: 'POST' }),
  },

  biblioteca: {
    buscar: (termo: string) => request<MusicaBiblioteca[]>(`/api/biblioteca/busca?termo=${enc(termo)}`),
  },
}

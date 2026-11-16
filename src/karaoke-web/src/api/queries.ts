import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './client'
import type { ModoFila, Usuario } from './types'

export const chaves = {
  me: ['me'] as const,
  meusEventos: ['eventos', 'meus'] as const,
  evento: (codigo: string) => ['evento', codigo] as const,
  fila: (codigo: string) => ['fila', codigo] as const,
  biblioteca: (termo: string) => ['biblioteca', termo] as const,
}

// ---------- Sessão do anfitrião ----------

export function useMe() {
  return useQuery({
    queryKey: chaves.me,
    queryFn: api.auth.me,
    staleTime: 5 * 60_000,
    retry: false,
  })
}

export function useLogin() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { email: string; password: string }) => api.auth.login(v.email, v.password),
    onSuccess: (usuario: Usuario) => qc.setQueryData(chaves.me, usuario),
  })
}

export function useRegister() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { nome: string; email: string; password: string }) =>
      api.auth.register(v.nome, v.email, v.password),
    onSuccess: (usuario: Usuario) => qc.setQueryData(chaves.me, usuario),
  })
}

export function useLogout() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.auth.logout,
    onSuccess: () => {
      qc.setQueryData(chaves.me, null)
      qc.removeQueries({ queryKey: ['eventos'] })
    },
  })
}

// ---------- Eventos ----------

export function useMeusEventos(habilitado = true) {
  return useQuery({ queryKey: chaves.meusEventos, queryFn: api.eventos.meus, enabled: habilitado })
}

export function useCriarEvento() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { nome: string; modo: ModoFila }) => api.eventos.criar(v.nome, v.modo),
    onSuccess: () => qc.invalidateQueries({ queryKey: chaves.meusEventos }),
  })
}

export function useEvento(codigo: string) {
  return useQuery({
    queryKey: chaves.evento(codigo),
    queryFn: () => api.eventos.obter(codigo),
    retry: false,
  })
}

// ---------- Fila ----------

/**
 * A fila vem do REST na primeira carga e depois é substituída a cada push do SignalR
 * (useFilaRealtime escreve direto neste cache). Por isso nunca fica "velha" sozinha.
 */
export function useFila(codigo: string) {
  return useQuery({
    queryKey: chaves.fila(codigo),
    queryFn: () => api.fila.obter(codigo),
    staleTime: Infinity,
    retry: false,
  })
}

/** Ações do anfitrião sobre a fila e o evento. */
export function useAcoesDoHost(codigo: string) {
  const qc = useQueryClient()
  const atualizar = () => qc.invalidateQueries({ queryKey: chaves.fila(codigo) })

  return {
    proxima: useMutation({ mutationFn: () => api.fila.proxima(codigo), onSettled: atualizar }),
    finalizar: useMutation({ mutationFn: () => api.fila.finalizar(codigo), onSettled: atualizar }),
    pular: useMutation({ mutationFn: () => api.fila.pular(codigo), onSettled: atualizar }),
    remover: useMutation({
      mutationFn: (itemId: number) => api.fila.remover(codigo, itemId),
      onSettled: atualizar,
    }),
    alterarModo: useMutation({
      mutationFn: (modo: ModoFila) => api.eventos.alterarModo(codigo, modo),
      onSettled: atualizar,
    }),
    encerrar: useMutation({
      mutationFn: () => api.eventos.encerrar(codigo),
      onSettled: () => {
        void atualizar()
        void qc.invalidateQueries({ queryKey: chaves.meusEventos })
      },
    }),
  }
}

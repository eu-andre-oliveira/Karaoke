import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { chaves } from '../api/queries'
import type { Fila } from '../api/types'

export type StatusConexao = 'conectando' | 'conectado' | 'reconectando' | 'desconectado'

/**
 * Mantém uma conexão SignalR aberta com o evento e escreve cada nova fila direto
 * no cache do React Query. Quem usa `useFila` re-renderiza sozinho.
 * Ao reconectar entra de novo no grupo e já recebe a foto atual da fila.
 */
export function useFilaRealtime(codigo: string, habilitado = true): StatusConexao {
  const qc = useQueryClient()
  const [status, setStatus] = useState<StatusConexao>('conectando')

  useEffect(() => {
    if (!habilitado || !codigo) return

    let cancelado = false
    const conexao = new HubConnectionBuilder()
      .withUrl('/hubs/fila')
      .withAutomaticReconnect([0, 1_000, 3_000, 5_000, 10_000, 15_000])
      .configureLogging(LogLevel.Warning)
      .build()

    // Não pode retornar valor: o cliente SignalR trataria o retorno como resposta a uma chamada.
    const publicar = (fila: Fila): void => {
      qc.setQueryData(chaves.fila(codigo), fila)
    }

    const entrarNoGrupo = async () => {
      const fila = await conexao.invoke<Fila | null>('EntrarNoEvento', codigo)
      if (fila) publicar(fila)
    }

    conexao.on('FilaAtualizada', publicar)
    conexao.onreconnecting(() => setStatus('reconectando'))
    conexao.onreconnected(() => {
      setStatus('conectado')
      void entrarNoGrupo().catch(() => setStatus('desconectado'))
    })
    conexao.onclose(() => {
      if (!cancelado) setStatus('desconectado')
    })

    conexao
      .start()
      .then(entrarNoGrupo)
      .then(() => {
        if (!cancelado) setStatus('conectado')
      })
      .catch(() => {
        if (!cancelado) setStatus('desconectado')
      })

    return () => {
      cancelado = true
      void conexao.stop()
    }
  }, [codigo, habilitado, qc])

  return status
}

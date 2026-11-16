import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMe } from '../api/queries'
import { Button, Card, Field, Tela } from '../components/ui'

export default function Home() {
  const navigate = useNavigate()
  const me = useMe()
  const [codigo, setCodigo] = useState('')

  function entrar(e: FormEvent) {
    e.preventDefault()
    const limpo = codigo.trim().toUpperCase()
    if (limpo) navigate(`/e/${limpo}`)
  }

  return (
    <Tela>
      <div className="py-8 text-center sm:py-14">
        <h1 className="text-4xl font-extrabold tracking-tight sm:text-6xl">
          A fila do karaokê,{' '}
          <span className="bg-gradient-to-r from-violet-300 via-fuchsia-300 to-pink-300 bg-clip-text text-transparent">
            no celular de cada um
          </span>
        </h1>
        <p className="mx-auto mt-4 max-w-2xl text-base text-slate-300 sm:text-lg">
          O anfitrião toca os vídeos na TV. A galera escolhe as músicas pelo celular, em tempo real, sem briga por ordem.
        </p>
      </div>

      <div className="grid gap-5 md:grid-cols-2">
        <Card>
          <h2 className="text-xl font-bold">Entrar em um evento</h2>
          <p className="mt-1 text-sm text-slate-400">Digite o código que aparece na TV, sem precisar de cadastro.</p>
          <form onSubmit={entrar} className="mt-4 space-y-4">
            <Field
              rotulo="Código do evento"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase())}
              maxLength={6}
              required
              autoComplete="off"
              autoCapitalize="characters"
              placeholder="ABC234"
              className="text-center text-2xl font-bold tracking-[0.3em] uppercase"
            />
            <Button type="submit" className="w-full">
              Entrar
            </Button>
          </form>
        </Card>

        <Card>
          <h2 className="text-xl font-bold">Sou o anfitrião</h2>
          <p className="mt-1 text-sm text-slate-400">Crie um evento, projete o player na TV e controle a fila.</p>
          <div className="mt-4 flex flex-col gap-3">
            {me.data ? (
              <Link
                to="/host"
                className="inline-flex min-h-11 items-center justify-center rounded-xl bg-gradient-to-r from-violet-500 to-fuchsia-500 px-4 py-2 text-sm font-semibold text-white"
              >
                Ir para meus eventos
              </Link>
            ) : (
              <>
                <Link
                  to="/login"
                  className="inline-flex min-h-11 items-center justify-center rounded-xl bg-gradient-to-r from-violet-500 to-fuchsia-500 px-4 py-2 text-sm font-semibold text-white"
                >
                  Entrar
                </Link>
                <Link
                  to="/cadastro"
                  className="inline-flex min-h-11 items-center justify-center rounded-xl bg-panel-2 px-4 py-2 text-sm font-semibold ring-1 ring-line hover:bg-line"
                >
                  Criar conta
                </Link>
              </>
            )}
          </div>
        </Card>
      </div>
    </Tela>
  )
}

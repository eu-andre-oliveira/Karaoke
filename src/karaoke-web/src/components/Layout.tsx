import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useLogout, useMe } from '../api/queries'
import { Button } from './ui'

export function Layout() {
  const me = useMe()
  const logout = useLogout()
  const navigate = useNavigate()

  return (
    <>
      <a
        href="#conteudo"
        className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-50 focus:rounded-lg focus:bg-white focus:px-3 focus:py-2 focus:text-black"
      >
        Pular para o conteúdo
      </a>

      <header className="border-b border-line/70 bg-ink/60 backdrop-blur">
        <div className="mx-auto flex w-full max-w-5xl items-center justify-between gap-3 px-4 py-3 sm:px-6">
          <Link to="/" className="flex items-center gap-2 text-lg font-bold tracking-tight">
            <img src="/favicon.svg" alt="" className="size-8" />
            <span className="bg-gradient-to-r from-violet-300 to-fuchsia-300 bg-clip-text text-transparent">Karaokê</span>
          </Link>

          <nav aria-label="Principal" className="flex items-center gap-2 text-sm">
            {me.data ? (
              <>
                <Link to="/host" className="rounded-lg px-3 py-2 text-slate-200 hover:bg-white/5">
                  Meus eventos
                </Link>
                <Button
                  variante="ghost"
                  className="min-h-9 px-3"
                  carregando={logout.isPending}
                  onClick={() => logout.mutate(undefined, { onSuccess: () => navigate('/') })}
                >
                  Sair
                </Button>
              </>
            ) : (
              !me.isPending && (
                <Link to="/login" className="rounded-lg px-3 py-2 text-slate-200 hover:bg-white/5">
                  Sou anfitrião
                </Link>
              )
            )}
          </nav>
        </div>
      </header>

      <main id="conteudo">
        <Outlet />
      </main>
    </>
  )
}

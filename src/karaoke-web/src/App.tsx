import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Component, lazy, Suspense, type ErrorInfo, type ReactNode } from 'react'
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { Carregando, Tela } from './components/ui'
import Home from './pages/Home'
import Login from './pages/Login'
import ParticipantEvent from './pages/ParticipantEvent'
import Register from './pages/Register'

// Rotas do anfitrião carregam sob demanda: o convidado, no celular, não baixa o player nem o QR code.
const HostDashboard = lazy(() => import('./pages/HostDashboard'))
const HostEvent = lazy(() => import('./pages/HostEvent'))

const queryClient = new QueryClient({
  defaultOptions: { queries: { refetchOnWindowFocus: false } },
})

class ErroNaTela extends Component<{ children: ReactNode }, { falhou: boolean }> {
  state = { falhou: false }

  static getDerivedStateFromError() {
    return { falhou: true }
  }

  componentDidCatch(erro: Error, info: ErrorInfo) {
    console.error(erro, info.componentStack)
  }

  render() {
    if (!this.state.falhou) return this.props.children
    return (
      <Tela>
        <div role="alert" className="rounded-2xl bg-rose-950/60 p-6 text-rose-100 ring-1 ring-rose-800">
          <h1 className="text-xl font-bold">Algo quebrou por aqui</h1>
          <p className="mt-2 text-sm">Recarregue a página para tentar de novo.</p>
          <button type="button" onClick={() => window.location.reload()} className="mt-4 rounded-xl bg-rose-600 px-4 py-2 text-sm font-semibold">
            Recarregar
          </button>
        </div>
      </Tela>
    )
  }
}

function NaoEncontrado() {
  return (
    <Tela>
      <h1 className="text-2xl font-bold">Página não encontrada</h1>
      <Link to="/" className="mt-4 inline-block font-semibold text-fuchsia-300 hover:underline">
        Voltar ao início
      </Link>
    </Tela>
  )
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ErroNaTela>
          <Suspense fallback={<Carregando />}>
            <Routes>
              <Route element={<Layout />}>
                <Route index element={<Home />} />
                <Route path="login" element={<Login />} />
                <Route path="cadastro" element={<Register />} />
                <Route path="e/:codigo" element={<ParticipantEvent />} />
                <Route path="host" element={<HostDashboard />} />
                <Route path="host/:codigo" element={<HostEvent />} />
                <Route path="*" element={<NaoEncontrado />} />
              </Route>
            </Routes>
          </Suspense>
        </ErroNaTela>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

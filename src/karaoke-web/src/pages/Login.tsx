import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useLogin } from '../api/queries'
import { Alerta, Button, Card, Field, Tela } from '../components/ui'

/** Só aceitamos destino interno, para ninguém usar o login para redirecionar a outro site. */
export function destinoSeguro(next: string | null): string {
  return next && next.startsWith('/') && !next.startsWith('//') ? next : '/host'
}

export default function Login() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const login = useLogin()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function enviar(e: FormEvent) {
    e.preventDefault()
    login.mutate({ email, password }, { onSuccess: () => navigate(destinoSeguro(params.get('next')), { replace: true }) })
  }

  return (
    <Tela>
      <Card className="mx-auto max-w-md">
        <h1 className="text-2xl font-bold">Entrar</h1>
        <form onSubmit={enviar} className="mt-5 space-y-4">
          <Field rotulo="E-mail" type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <Field rotulo="Senha" type="password" required autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
          {login.error && <Alerta>{login.error.message}</Alerta>}
          <Button type="submit" className="w-full" carregando={login.isPending}>
            Entrar
          </Button>
        </form>
        <p className="mt-5 text-center text-sm text-slate-400">
          Ainda não tem conta?{' '}
          <Link to="/cadastro" className="font-semibold text-fuchsia-300 hover:underline">
            Criar conta
          </Link>
        </p>
      </Card>
    </Tela>
  )
}

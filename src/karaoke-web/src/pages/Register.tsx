import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useRegister } from '../api/queries'
import { Alerta, Button, Card, Field, Tela } from '../components/ui'

export default function Register() {
  const navigate = useNavigate()
  const registrar = useRegister()
  const [nome, setNome] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  function enviar(e: FormEvent) {
    e.preventDefault()
    registrar.mutate({ nome, email, password }, { onSuccess: () => navigate('/host', { replace: true }) })
  }

  return (
    <Tela>
      <Card className="mx-auto max-w-md">
        <h1 className="text-2xl font-bold">Criar conta de anfitrião</h1>
        <form onSubmit={enviar} className="mt-5 space-y-4">
          <Field rotulo="Seu nome" required minLength={2} maxLength={100} autoComplete="name" value={nome} onChange={(e) => setNome(e.target.value)} />
          <Field rotulo="E-mail" type="email" required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <Field
            rotulo="Senha"
            type="password"
            required
            minLength={6}
            autoComplete="new-password"
            ajuda="Mínimo de 6 caracteres, com letra maiúscula, minúscula e número."
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          {registrar.error && <Alerta>{registrar.error.message}</Alerta>}
          <Button type="submit" className="w-full" carregando={registrar.isPending}>
            Criar conta
          </Button>
        </form>
        <p className="mt-5 text-center text-sm text-slate-400">
          Já tem conta?{' '}
          <Link to="/login" className="font-semibold text-fuchsia-300 hover:underline">
            Entrar
          </Link>
        </p>
      </Card>
    </Tela>
  )
}

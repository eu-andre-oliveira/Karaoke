import { describe, expect, it } from 'vitest'
import { destinoSeguro } from './Login'

describe('destinoSeguro', () => {
  it('aceita caminhos internos', () => {
    expect(destinoSeguro('/host/ABC234')).toBe('/host/ABC234')
  })

  it('cai em /host quando não há destino', () => {
    expect(destinoSeguro(null)).toBe('/host')
  })

  it.each(['https://evil.com', '//evil.com', 'javascript:alert(1)', 'host'])('rejeita destino externo ou inválido: %s', (valor) => {
    expect(destinoSeguro(valor)).toBe('/host')
  })
})

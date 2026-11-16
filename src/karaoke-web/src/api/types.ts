import type { components } from './schema'

// Tipos gerados do Swagger da API (npm run gen:api). Aqui só damos nomes curtos.
type S = components['schemas']

export type Fila = S['FilaDto']
export type ItemFila = S['ItemFilaDto']
export type Evento = S['EventoDto']
export type EntrouNoEvento = S['EntrouNoEventoDto']
export type ModoFila = S['ModoFila']
export type StatusItemFila = S['StatusItemFila']
export type Usuario = S['UsuarioDto']
export type MusicaBiblioteca = S['MusicaBibliotecaDto']
export type AdicionarItem = S['AdicionarItemRequest']

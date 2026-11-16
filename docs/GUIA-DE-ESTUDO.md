# Guia: rodar, explorar e entender o projeto

## 1. Rodar pela primeira vez (Windows)

Pré-requisitos: **.NET SDK 9+** (`dotnet --version`) e **Node 24+** (`node -v`).

Abra **dois terminais** na pasta do projeto (`C:\Projetos\Karaoke\Karaoke`).

**Terminal 1: API**

```bash
dotnet run --project src/Karaoke.Api --launch-profile http
```

Espere aparecer `Now listening on: http://localhost:5009`. Confira:
- http://localhost:5009/health devolve `{"status":"ok"}`
- http://localhost:5009/swagger lista todos os endpoints

> A API cria e migra o banco sozinha (Sqlite, arquivo `src/Karaoke.Api/karaoke.db`).
> Esse arquivo já existia do projeto antigo e ganha as tabelas novas. Para começar do zero, apague-o antes de subir (ele não é versionado).

**Terminal 2: Front**

```bash
cd src/karaoke-web
npm install
npm run dev
```

Abra http://localhost:5173. O Vite repassa `/api` e `/hubs` para a API na 5009, então não há CORS.

### Roteiro de teste (5 minutos)

1. Em `localhost:5173` clique em **Criar conta**, cadastre-se (senha com maiúscula, minúscula e número, ex.: `Senha123`).
2. Em **Novo evento**, dê um nome e crie. Você cai no **palco**: código de 6 letras + QR.
3. Abra uma **aba anônima** em `http://localhost:5173/e/CODIGO` (troque pelo código). Digite um nome e entre.
4. Na aba anônima, aba **Colar link**: cole `https://www.youtube.com/watch?v=dQw4w9WgXcQ`, dê um título e **Entrar na fila**.
5. Volte ao palco: a música apareceu **sem recarregar**. Clique **Iniciar próxima**: o vídeo toca e a aba anônima muda para "Cantando agora".
6. Teste o **revezamento**: crie duas pessoas (duas abas anônimas) e peça várias músicas com uma delas. A fila intercala os cantores.
7. Teste os limites: peça uma 4ª música com a mesma pessoa (recusa), cole um link que não é do YouTube (recusa).

### Testar com o celular de verdade

O QR code contém o endereço que está na barra do navegador do anfitrião. Se você abrir `localhost`, o celular não consegue acessar. Faça assim:

1. Descubra o IP do PC (`ipconfig`, procure o IPv4, ex.: `192.168.0.15`).
2. No PC, abra o palco por `http://192.168.0.15:5173/host/CODIGO`.
3. No celular (mesmo Wi-Fi), escaneie o QR.
4. Se não abrir, libere a porta 5173 no Firewall do Windows (rede privada).

## 2. Comandos úteis

```bash
dotnet test                          # 64 testes do back
cd src/karaoke-web
npm test                             # 28 testes do front (uma vez)
npm run test:watch                   # reexecuta ao salvar (ótimo para estudar)
npm run lint                         # ESLint
npm run build                        # typecheck + build de produção
npm run gen:api                      # regenera os tipos a partir do openapi.json
```

Regenerar tipos depois de mudar a API: suba a API, baixe `http://localhost:5009/swagger/v1/swagger.json` para `src/karaoke-web/openapi.json` e rode `npm run gen:api`.

## 3. O que foi usado

### Back-end (.NET 9)

| Tecnologia | Para quê |
|---|---|
| ASP.NET Core Web API | Controllers REST finos |
| **SignalR** | Tempo real (WebSocket) para empurrar a fila |
| EF Core 9 (Sqlite / Postgres / SQL Server) | Persistência, migrations em `Karaoke.Infrastructure/Migrations` |
| ASP.NET Identity + cookie | Login do anfitrião (convidado usa token por evento) |
| Swashbuckle | Swagger / OpenAPI |
| Rate limiter nativo | Proteção de login, cadastro e entrada em evento |
| xUnit + Sqlite em memória | Testes, usando as migrations reais |

Estrutura (Clean Architecture): `Domain` (regras puras) → `Application` (casos de uso) → `Infrastructure` (banco) → `Api` (HTTP + hub).

### Front-end (React)

| Tecnologia | Para quê |
|---|---|
| React 19 + TypeScript estrito | Interface |
| Vite 8 | Dev server e build (com proxy para a API) |
| React Router 7 | Rotas, com `lazy` para code splitting |
| **TanStack Query** | Estado do servidor (cache, loading, erro, mutações) |
| Zustand | Sessão do convidado, guardada no `localStorage` |
| `@microsoft/signalr` | Cliente do tempo real |
| Tailwind CSS 4 | Estilo mobile-first |
| YouTube IFrame API | Player no palco |
| `qrcode.react` | QR code de entrada |
| openapi-typescript | Tipos gerados do Swagger |
| Vitest + Testing Library | Testes |
| ESLint + typescript-eslint + react-hooks | Qualidade |

## 4. Como ler o front, na ordem certa

Abra `src/karaoke-web/src` e siga esta ordem. Cada passo responde uma pergunta.

| # | Arquivo | Pergunta que responde | Conceito |
|---|---|---|---|
| 1 | `main.tsx` | Onde o app começa? | `createRoot`, `StrictMode` |
| 2 | `App.tsx` | Quais telas existem? | Rotas, `lazy` + `Suspense`, error boundary, `QueryClientProvider` |
| 3 | `api/types.ts` + `api/schema.d.ts` | De onde vêm os tipos? | Tipos gerados do Swagger (não edite o `schema.d.ts`) |
| 4 | `api/client.ts` | Como falamos com a API? | `fetch` tipado, `ApiError`, ProblemDetails, token no cabeçalho |
| 5 | `api/queries.ts` | Como os dados chegam às telas? | `useQuery`, `useMutation`, chaves de cache, `setQueryData` |
| 6 | `realtime/useFilaRealtime.ts` | **Como a fila atualiza sozinha?** | Hook com `useEffect` e cleanup, reconexão, escrita direta no cache |
| 7 | `state/sessoes.ts` | Por que recarregar não pede o nome de novo? | Zustand + `persist` |
| 8 | `pages/ParticipantEvent.tsx` | Tela do convidado | Componentes por estado (evita hook condicional), mutações, tratamento de erro |
| 9 | `components/SongPicker.tsx` + `lib/useDebounce.ts` | Como a busca não dispara a cada letra? | Debounce, `enabled` na query, abas acessíveis |
| 10 | `pages/HostEvent.tsx` | Tela mais complexa | Refs, avanço automático, `fetchQuery`, fullscreen |
| 11 | `components/YouTubePlayer.tsx` + `lib/youtube.ts` | Como integrar uma API externa não-React? | Carregar script uma vez, ciclo de vida do player, `key` para remontar |
| 12 | `*.test.ts(x)` | Como provar que funciona? | Mock de módulo, `userEvent`, teste de comportamento |

### Para ver funcionando no navegador (DevTools, F12)

- **Network, filtro `WS`:** clique em `fila`, aba *Messages*. Peça uma música em outra aba e veja chegar a mensagem `FilaAtualizada` com a fila inteira. É o tempo real na prática.
- **Network, filtro `Fetch/XHR`:** veja as chamadas REST (`/api/eventos/...`) e o cabeçalho `X-Participante-Token` nas ações do convidado.
- **Application → Local Storage → `karaoke.sessoes`:** a sessão do convidado. Apague e recarregue: ele volta a pedir o nome.
- **Application → Cookies → `karaoke.auth`:** o cookie `HttpOnly` do anfitrião (o JavaScript não consegue lê-lo, de propósito).
- **Rotas lazy:** abra `/e/CODIGO` e veja no Network que `HostEvent-*.js` (player e QR) **não** é baixado. Só carrega em `/host/...`.

### Experimentos para entender

1. Em `lib/fila.ts`, mude `MAX_AGUARDANDO_POR_PARTICIPANTE` para 5 e veja a tela refletir. Note que o servidor continua recusando a 4ª: a regra de verdade está no back, o front só espelha.
2. Em `useFilaRealtime.ts`, comente a linha `conexao.on('FilaAtualizada', publicar)`. A fila só atualiza ao recarregar. Isso mostra o papel do SignalR.
3. Rode `npm run test:watch`, quebre algo em `lib/fila.ts` e veja o teste falhar.

## 5. Mapa rápido do back-end

| Quero entender... | Abra |
|---|---|
| A regra de revezamento | `src/Karaoke.Domain/Services/OrdenadorFila.cs` e `tests/Karaoke.Domain.Tests/OrdenadorFilaTests.cs` |
| Validação do link do YouTube | `src/Karaoke.Domain/ValueObjects/YouTubeUrl.cs` |
| Regras da fila (limites, permissões) | `src/Karaoke.Application/UseCases/FilaService.cs` |
| Como o push chega ao navegador | `src/Karaoke.Api/Hubs/FilaHub.cs` (`SignalRFilaNotifier`) |
| Rotas HTTP | `src/Karaoke.Api/Controllers/EventosController.cs` |
| Configuração geral | `src/Karaoke.Api/Program.cs` |
| Tabelas | `src/Karaoke.Infrastructure/Data/KaraokeDbContext.cs` |

## 6. Perguntas que um entrevistador pode fazer

- **Como a fila atualiza em tempo real?** Ação por REST; o caso de uso grava e chama `IFilaNotifier`; o SignalR envia a fila inteira ao grupo do evento; no front o handler escreve no cache do TanStack Query e a tela re-renderiza.
- **Por que enviar a fila inteira e não só o evento (adicionou, removeu)?** A fila é pequena. Evita bugs de ordem e duplicação, e reconectar fica trivial: ao voltar, recebe a foto atual.
- **Por que cookie e não JWT?** Front e API ficam na mesma origem (proxy do Vite / nginx), então cookie `HttpOnly` é mais seguro que token no `localStorage`.
- **Como o convidado se identifica sem conta?** Token aleatório por evento, guardado no navegador e enviado no cabeçalho. Só dá poder sobre as músicas dele naquele evento.
- **Onde está a regra de negócio?** No back (Domain/Application). O front só espelha os limites para dar feedback rápido.
- **O que você faria com mais tempo?** Playlists no front, teste E2E com Playwright, PWA, votação da plateia, pontuação por afinação.
- **Como garante que front e back não divergem?** Tipos gerados do Swagger. Se o back muda um DTO, o `npm run build` quebra no front.

## 7. Problemas comuns

| Sintoma | Causa e solução |
|---|---|
| `address already in use` na 5009 ou 5173 | Outro processo usa a porta. Feche-o ou troque a porta |
| Tela abre mas dá erro de rede / 502 no console | A API não está rodando. Suba o terminal 1 |
| Badge "Sem conexão" | A API caiu, ou o proxy do Vite aponta para outra porta. Variável `VITE_API_TARGET` muda o alvo |
| "Esse vídeo não pode ser tocado aqui" | O dono do vídeo bloqueou incorporação. O app pula sozinho; use outro link |
| Celular não abre o QR | Abra o palco pelo IP do PC, não por `localhost`, e libere o firewall (seção 1) |
| Erro de migration ao subir a API | Banco antigo incompatível. Apague `src/Karaoke.Api/karaoke.db` e suba de novo |
| `npm install` reclama de TypeScript | O projeto usa TypeScript 5.9 de propósito (o 7 ainda não é suportado pelas ferramentas) |

## 8. Rodar tudo com Docker (quando for ao servidor)

```bash
cp .env.example .env     # defina POSTGRES_PASSWORD
docker compose up -d --build
```

Abre em `http://localhost:8080`. Essa parte ainda não foi executada de ponta a ponta nesta máquina, então reporte qualquer erro.

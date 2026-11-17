# Deploy na VPS (sem domínio próprio)

Sobe **Postgres + API + nginx (SPA)** com Docker Compose. Sem domínio, há três caminhos:

| Opção | Custo | HTTPS | Quando usar |
|---|---|---|---|
| **A. IP direto** (`http://IP:8080`) | grátis | não | Primeira subida e teste rápido |
| **B. Subdomínio grátis (DuckDNS) + Caddy** | grátis | sim (Let's Encrypt automático) | **Recomendado** para mostrar em entrevista |
| **C. Cloudflare Tunnel** | grátis | sim | Quando não quer abrir portas nem expor o IP |

Recomendação: faça **A** para validar, depois **B**.

Requisitos da VPS: Ubuntu/Debian, **1 GB de RAM ou mais** (o build do .NET e do Node é pesado), acesso SSH.

## 1. Preparar a VPS

```bash
# Docker + Compose
curl -fsSL https://get.docker.com | sh

# Firewall (ajuste a porta SSH se usar outra)
sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443
sudo ufw enable
```

## 2. Subir a aplicação (opção A)

```bash
git clone <url-do-seu-repositorio> karaoke && cd karaoke
cp .env.example .env
nano .env                 # defina POSTGRES_PASSWORD com uma senha forte
docker compose up -d --build
docker compose ps         # os 3 serviços devem estar "running"
```

Para testar por IP, libere a porta: `sudo ufw allow 8080` e abra `http://IP-DA-VPS:8080`.

> Repositório privado: use uma *deploy key* (somente leitura) ou um token do GitHub no `git clone`.

O banco é migrado sozinho na primeira subida. Os dados ficam no volume `dbdata` e sobrevivem a `docker compose down` (só `down -v` apaga).

## 3. HTTPS com subdomínio grátis (opção B)

1. Em [duckdns.org](https://www.duckdns.org), crie um subdomínio (ex.: `meukaraoke`) apontando para o **IP da VPS**.
2. Instale o Caddy no host:
   ```bash
   sudo apt install -y caddy
   ```
3. Copie o exemplo e edite o subdomínio:
   ```bash
   sudo cp deploy/Caddyfile.example /etc/caddy/Caddyfile
   sudo nano /etc/caddy/Caddyfile
   sudo systemctl reload caddy
   ```
4. Feche o acesso direto à porta 8080: no `.env`, use `WEB_PORT=127.0.0.1:8080`, depois
   ```bash
   docker compose up -d
   sudo ufw delete allow 8080     # se tinha liberado no passo 2
   ```
5. Abra `https://meukaraoke.duckdns.org`.

O Caddy emite o certificado sozinho (portas 80 e 443 precisam estar abertas) e repassa WebSocket, então o tempo real funciona sem configuração extra.

## 3b. Alternativa sem abrir portas: Cloudflare Tunnel (opção C)

Exige conta grátis na Cloudflare. O túnel faz conexão de saída, então a VPS não precisa expor 80/443.

- Para uma URL temporária `*.trycloudflare.com`, sem conta e sem domínio:
  ```bash
  cloudflared tunnel --url http://localhost:8080
  ```
  A URL muda a cada execução (serve para demo pontual).
- Para URL fixa é necessário um domínio gerenciado na Cloudflare.

## 4. Verificar

```bash
curl -s http://127.0.0.1:8080/health      # {"status":"ok"}
docker compose logs -f api                # logs da API
```

Teste no navegador: cadastre-se, crie um evento, abra o link do evento no celular e peça uma música. A fila deve atualizar nas duas telas sem recarregar.

## 5. Atualizar e operar

```bash
git pull
docker compose up -d --build              # reconstrói e reinicia só o que mudou
docker compose logs --tail 100 api
docker compose exec db pg_dump -U karaoke karaoke > backup.sql   # backup
```

## 6. Problemas comuns

| Sintoma | Causa e solução |
|---|---|
| Build falha com "killed" ou sem memória | RAM insuficiente. Crie swap (`sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile`) |
| Caddy não emite o certificado | O subdomínio não aponta para o IP da VPS, ou as portas 80/443 estão fechadas |
| Tela abre mas fica "Sem conexão" | O proxy do host não repassa WebSocket em `/hubs/`. O Caddy faz isso sozinho; em outro proxy, repasse `Upgrade` e `Connection` |
| `POSTGRES_PASSWORD` obrigatória | Falta preencher o `.env` |
| API reinicia em loop | `docker compose logs api` mostra o motivo (normalmente a senha do banco mudou depois de o volume existir: apague o volume com `docker compose down -v` se não houver dados a manter) |
| Celular não abre o link | Use o endereço público (IP ou subdomínio), não `localhost`, ao abrir o palco para gerar o QR |

> Este roteiro **ainda não foi executado de ponta a ponta** (o daemon Docker estava desligado na máquina de desenvolvimento). Se algum passo falhar na primeira subida, corrija e atualize este arquivo.

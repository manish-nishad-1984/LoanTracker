# Deployment Guide — LoanTracker on a VPS (Ubuntu 24.04)

Target: `motiwala.pratishthabridal.com` → server `213.210.37.67` (Hostinger KVM 2).

The production stack runs entirely in Docker via `docker-compose.prod.yml`:
Caddy (HTTPS) → frontend (nginx) → API (.NET) → PostgreSQL. Only ports **80/443**
are exposed to the internet; the database is internal-only.

---

## 0. Point the domain at the server (DNS)

In your DNS provider for `pratishthabridal.com`, add an **A record**:

| Type | Name       | Value           |
|------|------------|-----------------|
| A    | motiwala   | 213.210.37.67   |

Wait for it to propagate (check: `ping motiwala.pratishthabridal.com` resolves to the IP).
HTTPS will not work until DNS points here.

---

## 1. SSH into the server

```bash
ssh root@213.210.37.67
```

## 2. Install Docker + Compose plugin

```bash
apt update && apt upgrade -y
curl -fsSL https://get.docker.com | sh
docker version && docker compose version
```

## 3. Firewall (allow SSH + web only)

```bash
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
```

## 4. Clone the repository

```bash
cd /opt
git clone https://github.com/<your-username>/<your-repo>.git loantracker
cd loantracker
```
(For a private repo, use a GitHub Personal Access Token as the password when prompted,
or set up a deploy key.)

## 5. Create the production `.env`

```bash
cp .env.production.example .env
nano .env          # set a long random POSTGRES_PASSWORD, then save
```

Generate a strong password if you like:
```bash
openssl rand -base64 24
```

## 6. Build & start

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

First start: the API runs EF Core migrations automatically and creates the schema.
Caddy fetches the TLS certificate (takes ~30s once DNS is correct).

Check status / logs:
```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f caddy   # watch cert issuance
docker compose -f docker-compose.prod.yml logs -f api
```

## 7. Open the app

Visit **https://motiwala.pratishthabridal.com** — log in with `admin` / `admin123`
(change these in `frontend/src/contexts/AuthContext.tsx` before deploying if you want).

---

## Updating after you push new code

```bash
cd /opt/loantracker
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

## Database backup

```bash
docker exec loantracker_db pg_dump -U loantracker_user loantracker > backup_$(date +%F).sql
```

## Notes / hardening for later

- **Auth** is currently a frontend-only demo gate; the API itself is unauthenticated.
  Anyone who can reach `/api` can read/write data. Add real backend auth (JWT) before
  exposing sensitive data publicly. Until then, treat this as a private tool.
- Swagger is only enabled in the Development environment, so it is off in production.
- To expose the API/DB ports temporarily for debugging, add a `ports:` mapping in
  `docker-compose.prod.yml` (and a matching `ufw allow`), then remove it afterwards.

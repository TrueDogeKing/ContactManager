# ContactManager

Contact list app — ASP.NET Core REST API + React SPA + PostgreSQL.

## Prerequisites

- Docker
- VS Code + the **Dev Containers** extension (the container ships .NET, Node, `mise` and `vpr`)

## Quick start (Docker — everything in containers)

```bash
# 1. Open the repo in VS Code → "Reopen in Container"
#    (postCreate runs `vpr install` automatically)

# 2. Create your env file
cp .env.example .env

# 3. Build & run Postgres + API + frontend (auto-migrates and seeds)
vpr dev
```

- App (frontend): http://localhost:8080
- API + docs (Scalar): http://localhost:5080/scalar/v1

**Default login (seeded):** `admin@admin` / `Admin123!`
Sample contacts log in with their own email + `Password123!`.

## Local dev (hot reload, DB in Docker)

```bash
vpr install        # restore .NET + frontend deps (already run inside the container)
mise db:up         # start PostgreSQL only
mise ef:update     # apply EF Core migrations (first run)
vpr backend        # API at http://localhost:5298
vpr frontend       # Vite dev server at http://localhost:5173
```

> Auto-migration and seeding run only in the `vpr dev` (Docker) flow. For seeded data in
> local dev, start the API with `Database__MigrateAutomatically=true Database__SeedAutomatically=true`.

## Common tasks

```bash
vpr test               # all tests
vpr test:unit          # unit tests
vpr test:integration   # integration tests (Testcontainers → needs Docker)
mise db:down           # stop the database
mise db:reset          # recreate the database (wipes data)
```

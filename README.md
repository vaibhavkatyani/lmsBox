# LMS Box Monorepo

This repository contains both the React client (Vite) and the .NET 9 backend API, plus shared backend layers.

## Repository structure

```
./
├─ lmsbox.client/           # React + Vite frontend (Learner + Admin portals)
├─ lmsBox.Server/           # .NET 9 Web API (server)
├─ lmsbox.domain/           # Domain models (shared by server)
├─ lmsbox.infrastructure/   # EF Core DbContext, Migrations, Data access
├─ lmsBox.sln               # Solution file
└─ README.md
```

## Prerequisites

- Node.js LTS (v18+ recommended)
- .NET SDK 9.0
- (Optional) Azure CLI if deploying to Azure

## Quick start

### 1) Backend (API)

From `lmsBox.Server`:

```powershell
# Restore & run
dotnet restore
dotnet ef database update  # if migrations are set up
dotnet run
```

API will start on the configured port (see `appsettings.json`).

### 2) Frontend (client)

From `lmsbox.client`:

```powershell
# Install deps & run dev server
npm ci
npm run dev
```

The dev server runs with Vite. The API proxy is configured via the SPA proxy or CORS.

## Environment configuration

- Backend: `appsettings.json`, `appsettings.Development.json`
- Frontend: `.env`, `.env.development` (e.g., `VITE_API_BASE_URL` if used)

## CI/CD (suggested)

Use GitHub Actions with path filters so each pipeline triggers only when its part changes:

- Backend workflow: runs on changes to `lmsBox.Server/**`, `lmsbox.domain/**`, `lmsbox.infrastructure/**`
- Frontend workflow: runs on changes to `lmsbox.client/**`

## Licensing

Proprietary. All rights reserved (update if you plan to open source).

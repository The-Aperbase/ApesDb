# ApesDb

Minimal .NET 10 solution with a FastEndpoints API, worker service, Swagger, a root Nx/pnpm React workspace, Oxc frontend linting/formatting, CSharpier, local infrastructure via Docker Compose, and Linux CI.

## Prerequisites

- .NET 10 SDK
- Node.js 26
- pnpm 11
- Docker Desktop or a compatible Docker engine with Docker Compose

## Setup

```bash
dotnet tool restore
dotnet restore ApesDb.slnx
pnpm install
pnpm build
docker compose up -d
```

## Authentication

ApesDb uses Auth0 for cookie-based session authentication. Google SSO is the only login method and is restricted to a preset allowlist of emails.

### Auth0 setup

1. Create a **Regular Web Application** in your Auth0 tenant.
2. Under **Authentication > Social**, enable the **Google** connection for the application.
3. Configure the following application URLs:
   - Allowed Callback URLs:
     - `https://localhost:7250/api/auth/callback`
     - `https://apesdb.owencross.com/api/auth/callback`
   - Allowed Logout URLs:
     - `https://localhost:7250/`
     - `https://apesdb.owencross.com/`
   - Allowed Web Origins:
     - `https://localhost:7250`
     - `https://apesdb.owencross.com`

The application checks the Auth0 email against the database-backed `AllowedUsers` table during the login callback. An unlisted email is returned to the login page without creating an application user or session. No Auth0 Post Login allowlist action is required.

### Allowed user management

The allowlist starts empty when its migration is applied. Add users from the authenticated TickerQ dashboard by running the manual `add-allowed-user` function with a request such as:

```json
{
  "email": "person@example.com"
}
```

Emails are trimmed and stored in lowercase. Running the function repeatedly for the same email is safe. Invalid email requests fail the TickerQ run.

For an existing deployment, apply the migration and deploy the API and worker, add every required email through TickerQ, verify a new login, and then remove the old Auth0 Post Login allowlist action. Existing application sessions remain valid until logout or expiry; the database allowlist is checked when a new session is created.

### Local Auth0 configuration

Auth0 settings are not committed. Set them via user secrets before running the API:

```bash
dotnet user-secrets set "Auth0:Domain" "<your-auth0-domain>" --project src/backend/ApesDb.Api
dotnet user-secrets set "Auth0:ClientId" "<your-auth0-client-id>" --project src/backend/ApesDb.Api
dotnet user-secrets set "Auth0:ClientSecret" "<your-auth0-client-secret>" --project src/backend/ApesDb.Api
```

### Local IGDB configuration

IGDB client credentials are also not committed. Only the background worker registers the IGDB SDK, so set them on the worker project:

```bash
dotnet user-secrets set "Igdb:ClientId" "<your-igdb-client-id>" --project src/backend/ApesDb.Worker
dotnet user-secrets set "Igdb:ClientSecret" "<your-igdb-client-secret>" --project src/backend/ApesDb.Worker
```

## Run the API

For full-stack local development, start the Vite dev server first:

```bash
pnpm serve
```

Then start the API:

```bash
dotnet run --project src/backend/ApesDb.Api
```

Open the app through the API origin so auth cookies and API calls stay same-origin:

- `https://localhost:7250`

In Development, the API proxies SPA requests to Vite at `http://localhost:5173`, so frontend changes hot reload without rebuilding. In non-development environments, the API serves the built frontend from `wwwroot`.

Default local URL from `launchSettings.json`:

- `https://localhost:7250`

Swagger UI:

- `https://localhost:7250/swagger`

## Run the frontend dev server

```bash
pnpm serve
```

This starts the Vite dev server on `http://localhost:5173`. For normal full-stack development, also run the API and open `https://localhost:7250`; the API proxies SPA requests to Vite so HMR works while auth and API calls remain same-origin.

Build the frontend:

```bash
pnpm build
```

Lint and check formatting:

```bash
pnpm lint
pnpm format:check
```

Format frontend files:

```bash
pnpm format
```

## Format code

Check formatting:

```bash
dotnet csharpier check .
```

Format the repo:

```bash
dotnet csharpier format .
```

## Local services

Start the app, worker, Postgres, and Redis:

```bash
docker compose up -d
```

Local Redis requires the password `apesdb`. The API's committed local defaults match this through `Cache:ConnectionString=localhost:6379` and `Cache:Password=apesdb`.
The worker serves the TickerQ dashboard at `http://localhost:8081/tickerq/dashboard` with local development credentials `admin` / `apesdb`.

Stop services:

```bash
docker compose down
```

Remove services and volumes:

```bash
docker compose down -v
```

## Database migrations

Migrations are managed with [Flyway](https://documentation.red-gate.com/flyway) through Docker Compose.

Migration scripts live in `db/migrations/`. To add a new migration, create a Flyway versioned SQL file with the next version number, for example:

```bash
db/migrations/V2__Add_profile_fields.sql
```

Flyway tracks applied scripts in the `migrations.flyway_schema_history` table and only runs each migration once. `FLYWAY_BASELINE_ON_MIGRATE` is enabled in Compose so existing databases that were previously migrated by DbUp are adopted at version 1.

Flyway uses `migrations` as its default schema and also manages `public` and `worker`. App tables should be schema-qualified as `public` in migration scripts, and worker-owned scheduler tables should be schema-qualified as `worker`.

The local and deployment compose files run the `flyway` service against Postgres before the app and worker start. To run migrations manually:

```bash
docker compose run --rm flyway
```

Local Compose also runs Grafana OTEL LGTM at `http://localhost:3000` and exposes OTLP on ports `4317` and `4318`. The API and worker app settings export telemetry to it, and the Grafana instance provisions the dashboards in `observability/grafana`.

## Deployment environment variables

The production compose file expects the following environment variables:

- `AUTH0_DOMAIN`
- `AUTH0_CLIENT_ID`
- `AUTH0_CLIENT_SECRET`
- `IGDB_CLIENT_ID`
- `IGDB_CLIENT_SECRET`
- `POSTGRES_PASSWORD`
- `REDIS_PASSWORD`
- `TICKERQ_DASHBOARD_USERNAME`
- `TICKERQ_DASHBOARD_PASSWORD`

The deployment also supports these optional observability settings:

- `OTEL_EXPORTER_OTLP_ENDPOINT` defaults to `http://apesdb-observability_alloy:4317`.
- `OTEL_EXPORTER_OTLP_HTTP_ENDPOINT` defaults to `http://apesdb-observability_alloy:4318` and maps to `OpenTelemetry:OtlpProxy:Endpoint` for browser traces proxied by the API.
- `TELEMETRY_NETWORK` defaults to the external Swarm overlay network `observability-telemetry`.

Deploy the observability stack before the first ApesDb deployment so it can create the shared `observability-telemetry` overlay network. The API and worker export OTLP traces, metrics, and logs when `OpenTelemetry:Otlp:Endpoint` is configured. Deployment maps `OTEL_EXPORTER_OTLP_ENDPOINT` to that setting.

Application-owned Grafana dashboards and Git Sync setup instructions are in [`observability/`](observability/README.md).

The API and worker read database settings from `Database:*`; the worker reads IGDB credentials from `Igdb:*`; the API also reads cache settings from `Cache:*`, while the worker reads TickerQ dashboard settings from `TickerQ:Dashboard:*`. In Docker Compose, use the equivalent double-underscore environment variable names. The deployment compose file fails fast when required secrets are missing.

## Zero-downtime app deployments

The deployment Compose file keeps two app replicas running and replaces them one at a time with Docker Swarm's `start-first` update order. Each replacement must pass the anonymous `/health` probe before the rollout proceeds. The probe returns healthy only when both PostgreSQL and Redis are reachable.

A rolling update briefly runs three app containers on the single-node swarm, so the node must have enough CPU and memory for that peak. If PostgreSQL or Redis becomes unavailable, the dependency-aware probe marks app tasks unhealthy and Swarm may restart them until the dependency recovers.

Because old and new app versions overlap during a rollout, database migrations must remain compatible with both versions. Use expand-and-contract migrations for destructive schema changes instead of removing or renaming schema elements in the same deployment that stops using them.

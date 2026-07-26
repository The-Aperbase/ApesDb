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

### TickerQ worker recovery

The worker keeps TickerQ jobs in PostgreSQL and uses Redis only for node heartbeats and dead-node coordination. Each worker has a unique node identifier. With the committed 10-second heartbeat interval, a surviving worker normally releases and resumes work owned by a killed container within approximately 40 seconds.

Recovered jobs use at-least-once execution. The IGDB catalog stages commit each page and its cursor together, so a resumed stage continues from the last committed cursor. A normal exception follows the configured retries of 30 seconds, 2 minutes, and 10 minutes. After the final retry, the run remains failed across worker restarts and daily scheduling.

Resume a terminally failed run from the authenticated TickerQ dashboard by invoking `resume-igdb-sync` with:

```json
{
  "runId": "00000000-0000-0000-0000-000000000000"
}
```

The command accepts only a failed run with no active stage ticker. It clears the run and stage errors and creates a fresh ticker retry sequence without resetting the saved cursor or progress counters.

Deployments created before Redis heartbeat coordination may already contain a ticker owned by a container that no longer exists. Recover those rows once during the first rollout:

1. Scale every worker replica to zero and confirm no worker process is running.
2. Preview the catalog tickers that will be released:

   ```sql
   SELECT ticker."Id", ticker."Function", ticker."LockHolder", ticker."LockedAt"
   FROM worker."TimeTickers" AS ticker
   INNER JOIN public."IgdbSyncStages" AS stage ON stage."Id" = ticker."Id"
   WHERE ticker."Status" = 2
     AND stage."Status" = 'Running';
   ```

3. If every returned owner is stopped, release only those catalog tickers:

   ```sql
   BEGIN;

   UPDATE worker."TimeTickers" AS ticker
   SET "Status" = 0,
       "LockHolder" = NULL,
       "LockedAt" = NULL,
       "UpdatedAt" = now()
   FROM public."IgdbSyncStages" AS stage
   WHERE stage."Id" = ticker."Id"
     AND ticker."Status" = 2
     AND stage."Status" = 'Running';

   COMMIT;
   ```

4. Restore the worker replicas. The existing stage state remains `Running` until the released ticker resumes it from the saved cursor.

Do not run this procedure while any worker is live; heartbeat cleanup handles all subsequent container failures automatically.

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

The API and worker read database settings from `Database:*`; the worker reads IGDB credentials from `Igdb:*`; both services read Redis settings from `Cache:*`; and the worker reads TickerQ recovery and dashboard settings from `TickerQ:Recovery:*` and `TickerQ:Dashboard:*`. In Docker Compose, use the equivalent double-underscore environment variable names. The deployment compose file fails fast when required secrets are missing.

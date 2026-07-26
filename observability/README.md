# ApesDb Grafana Resources

ApesDb owns its application dashboards in this directory. The shared LGTM deployment owns Grafana and its `prometheus`, `loki`, and `tempo` data sources.

## Git Sync setup

Grafana Git Sync watches `observability/grafana` on the `main` branch and places its resources in an `ApesDb` folder. The repository configuration is stored separately in `observability/git-sync` so Grafana does not try to interpret it as a dashboard.

Before running the setup workflow:

1. Create an `aperbase-github` GitHub App connection in Grafana under **Administration > General > Provisioning**. Install the app for `The-Aperbase/ApesDb`.
2. Create a Grafana service account with sufficient permission to manage provisioning repositories.
3. Add `GRAFANA_SERVICE_ACCOUNT_TOKEN` as a GitHub Actions secret.
4. Ensure the existing `TS_OAUTH_CLIENT_ID` and `TS_AUDIENCE` organization secrets are available to this repository.
5. Run the **Grafana Git Sync** workflow with `apply` disabled to validate the resources and connection.
6. Run it again with `apply` enabled to create or update the `apesdb-dashboards` repository resource.

Git Sync polls every 60 seconds. It may edit dashboards through branches and pull requests, but it cannot commit directly to `main`.

Grafana's public root URL is `https://grafana.owencross.com`. For automation, the workflow maps `grafana.owencross.com` to `dokbox`'s Tailscale address and connects to `http://grafana.owencross.com`. This sends traffic directly to Dokploy's Traefik listener over Tailscale while retaining the hostname required by its Grafana route; it does not pass through Cloudflare.

## Game-list dashboard

`grafana/game-list.json` is a Grafana dashboard CRD for `GET /api/games`. It reports:

- Request throughput.
- P95 latency.
- 5xx error percentage.
- P50, P95, and P99 latency over time.
- Request rate split by response status.

The LGTM Alloy configuration maps the OTEL `service.namespace` and `service.name` resource attributes to the Prometheus `job` label. It leaves `deployment.environment` on `target_info`, so the dashboard joins request metrics to `target_info` using `job` and `instance`. For the API, the expected `job` value is `apesdb/apesdb-api`.

If the dashboard has no data after the first game-list request, verify these values in Grafana Explore before changing its queries:

```promql
http_server_request_duration_seconds_count
```

```promql
target_info{job="apesdb/apesdb-api"}
```

The dashboard assumes the stable LGTM datasource UID `prometheus` and the OTEL Prometheus translation of `http.server.request.duration` to `http_server_request_duration_seconds`.

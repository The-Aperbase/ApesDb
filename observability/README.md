# ApesDb Grafana Resources

ApesDb owns its application dashboards in this directory. The shared LGTM deployment owns Grafana and its `prometheus`, `loki`, and `tempo` data sources.

## Git Sync setup

Grafana owns the GitHub connection and repository sync configuration. ApesDb owns only the dashboard resources under `observability/grafana`.

Configure the repository once in Grafana at `https://grafana.owencross.com` under **Administration > General > Provisioning**:

1. Select the existing GitHub App connection named `Grafana-2026-07-26-4xdph1`.
2. Set the repository URL to `https://github.com/The-Aperbase/ApesDb`.
3. Set the branch to `main`.
4. Set the repository path to `observability/grafana`.
5. Use folder sync with the display name `ApesDb`.
6. Set the sync interval to 60 seconds.
7. Enable the branch and pull-request workflow, but disable direct pushes to `main`.
8. Leave dashboard previews disabled unless a public image renderer is configured.
9. Start synchronization and confirm that Grafana reports the repository as healthy.

After setup, Grafana polls the repository and applies dashboard changes merged into `main`. Dashboard edits made in Grafana use the GitHub App to create a branch and pull request. No GitHub Actions workflow, Grafana service-account token, or Tailscale route is required for synchronization.

The GitHub App is installed only for `The-Aperbase/ApesDb`. Grafana stores its App ID, installation ID, and private key; none of those credentials belong in this repository.

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

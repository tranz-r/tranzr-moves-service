# Observability validation checklist

Run after deploying the monitoring stack and instrumented application images.

## Prerequisites

- Monitoring pods healthy in `monitoring-system`
- Alloy Service exposes OTLP: `kubectl get svc -n monitoring-system monitoring-alloy`
- Tranzr workloads have `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` set

## Tempo (distributed traces)

Public API traffic enters through **tranzr-api-gateway** (YARP). Quote/checkout traces are stored with the gateway as the **root** span; `tranzr-moves-api` appears as child spans in the same trace. Kubernetes probes flood `/healthz` and `/ready` — use TraceQL filters below.

### Grafana Explore → Tempo (TraceQL)

**Quote journey (recommended):**

```traceql
{ resource.service.name = "tranzr-api-gateway" && span.http.route =~ ".*[Qq]uote.*" }
```

**Any non-probe gateway API traffic:**

```traceql
{ resource.service.name = "tranzr-api-gateway" && name !~ "GET /healthz|GET /ready" && span.http.route != "" }
```

**Backend spans inside the same traces (after opening a trace ID):**

Look for `tranzr-moves-api` children (controller actions, EF, RabbitMQ).

**Do not rely on** `service.name = tranzr-moves-api` alone in the simple search box — it mostly lists probe and Wolverine background roots, not edge HTTP roots.

### End-to-end checks

1. Complete a quote journey on staging/production.
2. Run the TraceQL query above (last 15 minutes).
3. Open a trace; confirm spans include:
   - `tranzr-api-gateway` — `GET|POST|PATCH /api/v2/quote/...`
   - `tranzr-moves-api` — controller actions (e.g. journey-state, ensure)
   - RabbitMQ publish when a message is sent
   - `tranzr-moves-notifications` or `tranzr-moves-worker-processor` on consume
   - Wolverine handler spans (`Wolverine` activity source)
4. Pay-later path: trigger balance charge flow; verify Scheduler → processor linkage.

## Loki (logs)

```logql
{namespace="tranzr-moves-staging"} |= "trace_id"
```

Confirm `trace_id` in structured logs matches a Tempo trace ID for the same request.

## Prometheus (metrics)

- Query `{__name__=~"wolverine.*"}` or service metrics from OTLP remote write.
- In Grafana → Explore → Prometheus, confirm Alloy remote-write targets and KPS scrape targets are up.

## Local development

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_EXPORTER_OTLP_PROTOCOL=grpc
export OTEL_SERVICE_NAME=tranzr-moves-api
```

Disable export: `OTEL_SDK_DISABLED=true` or omit `OTEL_EXPORTER_OTLP_ENDPOINT`.

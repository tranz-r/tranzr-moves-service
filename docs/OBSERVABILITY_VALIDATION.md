# Observability validation checklist

Run after deploying the monitoring stack and instrumented application images.

## Prerequisites

- Monitoring pods healthy in `monitoring-system`
- Alloy Service exposes OTLP: `kubectl get svc -n monitoring-system monitoring-alloy`
- Tranzr workloads have `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` set

## Tempo (distributed traces)

1. Open Grafana → Explore → Tempo.
2. Search `service.name=tranzr-api-gateway` for a recent HTTP request through the gateway.
3. Confirm child spans include:
   - `tranzr-moves-api` (ASP.NET Core)
   - RabbitMQ publish (`messaging.system=rabbitmq`) when a message is sent
   - `tranzr-moves-notifications` or `tranzr-moves-worker-processor` on consume
   - Wolverine handler spans (`Wolverine` activity source)
4. Pay-later path: trigger balance charge flow; verify Scheduler → `quote-v2-balance-charge` → Processor linkage.

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

# TranzrMoves.Worker

Background host for pay-later balance collection. Use the **same image** for two deployments with different `Worker:Role` values.

Kubernetes deployment guide (LLM-oriented): [docs/pay-later-worker-kubernetes-deployment.md](../../docs/pay-later-worker-kubernetes-deployment.md).

## Roles

| Role | Replicas | Hosted services | Wolverine consumer |
|------|----------|-----------------|-------------------|
| `Scheduler` | **1** | Redis expiry listener, recovery poll | No (publish only) |
| `Processor` | **N** | None | Yes |
| `All` | 1 (local only) | Listener + recovery | Yes |

`Worker:Role=All` is allowed only when `ASPNETCORE_ENVIRONMENT=Development`.

## Configuration

```bash
# Production scheduler (single instance)
Worker__Role=Scheduler

# Production processors (scale horizontally)
Worker__Role=Processor
```

### Environment variables by deployment

| Variable | Scheduler | Processor |
|----------|:---------:|:---------:|
| `Worker__Role` | `Scheduler` | `Processor` |
| `ConnectionStrings__TranzrMovesDatabaseConnection` | ✓ | ✓ |
| `ConnectionStrings__rabbitmq` | ✓ | ✓ |
| `ConnectionStrings__redis` | ✓ | — |
| `STRIPE_API_KEY` | — | ✓ |
| `CHECKOUT_SESSION_SUCCESS_URL` / `CHECKOUT_SESSION_CANCEL_URL` | — | ✓ |

The Worker uses a slim DI registration (`AddPayLaterWorkerServices`) — it does **not** load the full API stack (Mapbox, Turnstile, MediatR handlers, etc.).

`DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` may be omitted in Kubernetes (defaults to `Production`).

## Redis (scheduler)

Enable keyspace notifications so expiry events are published, e.g. in `redis.conf`:

```
notify-keyspace-events Ex
```

## Local development

See the [root README](../../README.md) for the full step-by-step local setup (Docker, migrations, secrets, Api, Worker, Stripe CLI, and testing).

## Flow

1. Api webhook schedules a Redis TTL key at pay-later due date.
2. Scheduler reacts on key expiry (or recovery scan) and publishes to RabbitMQ `quote-v2-balance-charge`.
3. Processor(s) consume messages and run balance collection via Stripe.

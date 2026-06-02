# Pay-Later Worker: Kubernetes Deployment (Scheduler + Processor)

Deploy **two** Kubernetes Deployments from the same container image. The image is already built and published to GHCR by CI — do not rebuild unless changing application code.

**Image:** `ghcr.io/<github-org>/tranzr-moves-worker:<tag>`  
**Entrypoint:** `dotnet TranzrMoves.Worker.dll`

---

## Roles

| Deployment | `Worker__Role` | Replicas | Does |
|------------|----------------|----------|------|
| `tranzr-moves-worker-scheduler` | `Scheduler` | **1** | Redis expiry listener + recovery poll → publishes to RabbitMQ |
| `tranzr-moves-worker-processor` | `Processor` | **N** | Consumes RabbitMQ → Stripe balance charge |

Do **not** use `Worker__Role=All` in production (Development only).

Omit `DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` — defaults to `Production`.

---

## Prerequisites (cluster must already provide)

- **PostgreSQL** — migrations applied; connection string available
- **Redis** — `notify-keyspace-events Ex` (Scheduler only)
- **RabbitMQ** — queue `quote-v2-balance-charge` (Wolverine auto-provisions)

---

## Environment variables

Use `__` for nested config (e.g. `Worker__Role` → `Worker:Role`).

### Scheduler

| Variable | Required |
|----------|:--------:|
| `Worker__Role` = `Scheduler` | Yes |
| `ConnectionStrings__TranzrMovesDatabaseConnection` | Yes |
| `ConnectionStrings__redis` | Yes |
| `ConnectionStrings__rabbitmq` | Yes |

### Processor

| Variable | Required |
|----------|:--------:|
| `Worker__Role` = `Processor` | Yes |
| `ConnectionStrings__TranzrMovesDatabaseConnection` | Yes |
| `ConnectionStrings__rabbitmq` | Yes |
| `STRIPE_API_KEY` | Yes |
| `CHECKOUT_SESSION_SUCCESS_URL` | Yes |
| `CHECKOUT_SESSION_CANCEL_URL` | Yes |

Processor does **not** need `ConnectionStrings__redis`.

---

## Scheduler Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tranzr-moves-worker-scheduler
spec:
  replicas: 1
  selector:
    matchLabels:
      app: tranzr-moves-worker
      role: scheduler
  template:
    metadata:
      labels:
        app: tranzr-moves-worker
        role: scheduler
    spec:
      containers:
        - name: worker
          image: ghcr.io/ORG/tranzr-moves-worker:TAG
          env:
            - name: Worker__Role
              value: Scheduler
            - name: ConnectionStrings__TranzrMovesDatabaseConnection
              valueFrom:
                secretKeyRef:
                  name: tranzr-moves-worker-secrets
                  key: postgres-connection
            - name: ConnectionStrings__redis
              valueFrom:
                secretKeyRef:
                  name: tranzr-moves-worker-secrets
                  key: redis-connection
            - name: ConnectionStrings__rabbitmq
              valueFrom:
                secretKeyRef:
                  name: tranzr-moves-worker-secrets
                  key: rabbitmq-connection
```

**Success log:** `Subscribed to Redis key expiry events for pay-later balance charges`

---

## Processor Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tranzr-moves-worker-processor
spec:
  replicas: 2
  selector:
    matchLabels:
      app: tranzr-moves-worker
      role: processor
  template:
    metadata:
      labels:
        app: tranzr-moves-worker
        role: processor
    spec:
      containers:
        - name: worker
          image: ghcr.io/ORG/tranzr-moves-worker:TAG
          env:
            - name: Worker__Role
              value: Processor
            - name: ConnectionStrings__TranzrMovesDatabaseConnection
              valueFrom:
                secretKeyRef:
                  name: tranzr-moves-worker-secrets
                  key: postgres-connection
            - name: ConnectionStrings__rabbitmq
              valueFrom:
                secretKeyRef:
                  name: tranzr-moves-worker-secrets
                  key: rabbitmq-connection
            - name: STRIPE_API_KEY
              valueFrom:
                secretKeyRef:
                  name: tranzr-moves-worker-secrets
                  key: stripe-api-key
            - name: CHECKOUT_SESSION_SUCCESS_URL
              value: "https://YOUR_APP/checkout/success?session_id={CHECKOUT_SESSION_ID}"
            - name: CHECKOUT_SESSION_CANCEL_URL
              value: "https://YOUR_APP/checkout/cancel"
```

**Success log:** `Started message listening at rabbitmq://queue/quote-v2-balance-charge`

Scale Processor with HPA; keep Scheduler at **1** replica.

---

## Rules

- Same GHCR image for both Deployments; only `Worker__Role` and env differ.
- Never run more than one Scheduler replica.
- Do not set Mapbox, Supabase, or Turnstile secrets on Worker pods.

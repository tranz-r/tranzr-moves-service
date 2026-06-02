# Notifications service

Transactional email is sent by **TranzrMoves.Notifications**, a separate deployable host in the same repository and solution as the API and Worker. The monolith does not call the email provider directly; it enqueues `SendNotification` messages through a durable Wolverine outbox.

## Flow

```text
API / Worker  →  Wolverine outbox (schema tranzrmoves)  →  RabbitMQ queue notifications-send
       →  TranzrMoves.Notifications consumer  →  Handlebars templates  →  SMTP4DEV (local) or ACS
       →  delivery log (schema notifications)
```

| Component | Responsibility |
|-----------|----------------|
| Monolith (`TranzrMoves.Api`, `TranzrMoves.Worker`) | Build `SendNotification` with template key + data; publish before `SaveChanges` where possible |
| RabbitMQ | Queue `notifications-send` |
| `TranzrMoves.Notifications` | Idempotent send, template render, provider send (ACS/SMTP), `NotificationDeliveries` audit |

## Database

Same Postgres database as the monolith (`tranzr` locally). Separate schemas:

| Schema | Owner | Contents |
|--------|--------|----------|
| `tranzrmoves` | Monolith | Quotes, payments, Wolverine **outbox** |
| `notifications` | Notifications host | `NotificationDeliveries`, Wolverine **inbox** |

Connection string: `ConnectionStrings:TranzrMovesDatabaseConnection` (shared).

Apply migrations manually (optional when running `docker compose up`, because `notifications-db-migrator` applies notifications migrations automatically):

```bash
dotnet ef database update --project Src/TranzrMoves.Notifications.Infrastructure --startup-project Src/TranzrMoves.Notifications
```

## Contract

Message type: `TranzrMoves.Notifications.Contracts.SendNotification`

- `MessageId` — caller-generated idempotency key (stable across retries)
- `CorrelationId` — business id (payment intent, session id, contact submission, etc.)
- `Category` — `Transactional` (v1) or `Marketing` (Phase 2; currently skipped by consumer)
- `TemplateKey` — maps to `*.html.hbs` / `*.txt.hbs` under Notifications.Infrastructure

## Configuration

| Setting | Where |
|---------|--------|
| `ConnectionStrings:TranzrMovesDatabaseConnection` | Api, Worker, Notifications |
| `ConnectionStrings:rabbitmq` | Api, Worker, Notifications |
| `Notifications:EmailProvider` | Notifications (`Acs` or `Smtp`) |
| `Notifications:Smtp:*` | Notifications (when provider is `Smtp`) |
| `COMMUNICATION_SERVICES_CONNECTION_STRING` | Notifications (when provider is `Acs`) |
| `Notifications:UseDurableMessaging` | `true` in prod; `false` in tests |

## Local development

```bash
docker compose up -d
dotnet ef database update --project Src/TranzrMoves.Infrastructure --startup-project Src/TranzrMoves.Api
dotnet run --project Src/TranzrMoves.Notifications
```

Health: `GET http://localhost:8081/healthz` when using docker-compose `notifications` service.
SMTP UI (local): `http://localhost:8025` when using docker-compose `smtp4dev` service.

## Phase 2 (not yet implemented)

- `Marketing` category with consent tables
- Quote reminder publisher from Worker
- Admin resend API (optional)

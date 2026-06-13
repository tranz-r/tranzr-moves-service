# tranzr-moves-services

Backend services for Tranzr Moves — HTTP API, pay-later background worker, and shared application/infrastructure layers.

## What you will run

To test locally you need **four things running** (plus Notifications if you want real email delivery):

| # | Component | How | Purpose |
|---|-----------|-----|---------|
| 1 | Postgres, Redis, RabbitMQ | `docker compose up -d` | Database and messaging |
| 2 | **Worker** | `dotnet run --project Src/TranzrMoves.Worker` | Redis expiry → RabbitMQ → Stripe balance charge |
| 3 | **API** | `dotnet run --project Src/TranzrMoves.Api` | Checkout, quotes, Stripe webhooks |
| 4 | **Stripe CLI** (for webhook testing) | `stripe listen --forward-to ...` | Forwards Stripe events to your local API |
| 5 | **Notifications** (optional) | `dotnet run --project Src/TranzrMoves.Notifications` | Consumes `notifications-send` + `notifications-consent`; sends email via ACS/SMTP |

See [Notifications](documents/notifications/notifications.md) for queue, schemas, and configuration.

**Pay-later flow:**

```
Stripe webhook → API schedules Redis key → key expires → Worker → RabbitMQ → handler → Stripe charge
```

Deposit remaining balance uses the same pipeline with a different Redis key and charge time — see [Balance charges](documents/balance-charges/balance-charges.md).

---

## Documentation

| Guide | Description |
|-------|-------------|
| [documents/](documents/README.md) | Feature architecture (markdown + diagrams) |
| [Balance charges](documents/balance-charges/balance-charges.md) | Pay-later and deposit remaining balance collection |
| [docs/](docs/pay-later-worker-kubernetes-deployment.md) | Kubernetes deployment (Scheduler / Processor) |

Diagram PNGs are generated from PlantUML sources in CI and via `./scripts/render-plantuml.sh`.

---

## Prerequisites

Install these before starting:

1. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. [Docker Desktop](https://docs.docker.com/get-docker/) (includes Docker Compose)
3. EF Core CLI: `dotnet tool install --global dotnet-ef`
4. [Stripe CLI](https://stripe.com/docs/stripe-cli) (only if testing checkout/webhooks locally)
5. Secret values from a teammate (Supabase, Stripe test key, Mapbox, Azure, etc.)

From the **repository root**, confirm tooling:

```bash
dotnet --version
docker compose version
dotnet ef --version
```

---

## Step 1 — Clone the repo and build

```bash
git clone <repo-url>
cd tranzr-moves-services
dotnet restore
dotnet build
```

**Success:** terminal prints `Build succeeded` with 0 errors.

---

## Step 2 — Start Postgres, Redis, and RabbitMQ

From the repository root:

```bash
docker compose up -d
```

Wait ~30 seconds, then check status:

```bash
docker compose ps
```

**Success:** all three containers (`tranzr-postgres`, `tranzr-redis`, `tranzr-rabbitmq`) show status `healthy`.

| Service | Address | Login |
|---------|---------|-------|
| Postgres | `localhost:5432` | user `tranzr`, password `tranzr`, database `tranzr` |
| Redis | `localhost:6379` | — |
| RabbitMQ | `localhost:5672` | guest / guest |
| RabbitMQ UI | http://localhost:15672 | guest / guest |

Confirm each service responds:

```bash
docker exec tranzr-postgres pg_isready -U tranzr -d tranzr
docker exec tranzr-redis redis-cli CONFIG GET notify-keyspace-events
docker exec tranzr-rabbitmq rabbitmq-diagnostics -q ping
```

The Redis command must return `Ex` (expiry notifications). Docker Compose configures this automatically.

Connection strings in `Src/TranzrMoves.Api/appsettings.json` and `Src/TranzrMoves.Worker/appsettings.json` already match these defaults.

---

## Step 3 — Apply database migrations

```bash
dotnet ef database update \
  --project Src/TranzrMoves.Infrastructure \
  --startup-project Src/TranzrMoves.Api
```

**Success:** terminal prints `Done.`

Confirm tables exist:

```bash
docker exec tranzr-postgres psql -U tranzr -d tranzr -c "\dt tranzrmoves.*"
```

---

## Step 4 — Configure user secrets

Secrets are **not** in git. Each app reads its own [user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) store.

Get values from a teammate, then run **every command below**. Each secret must be set on **both** the Api and Worker projects.

Replace the placeholder values (`YOUR_...`, `sk_test_...`, etc.) with real values.

```bash
# Supabase (API will not start without these)
dotnet user-secrets set SUPABASE_URL "https://YOUR_PROJECT.supabase.co" --project Src/TranzrMoves.Api
dotnet user-secrets set SUPABASE_URL "https://YOUR_PROJECT.supabase.co" --project Src/TranzrMoves.Worker

dotnet user-secrets set SUPABASE_KEY "YOUR_SUPABASE_KEY" --project Src/TranzrMoves.Api
dotnet user-secrets set SUPABASE_KEY "YOUR_SUPABASE_KEY" --project Src/TranzrMoves.Worker

# Stripe (use a test key: sk_test_...)
dotnet user-secrets set STRIPE_API_KEY "sk_test_YOUR_KEY" --project Src/TranzrMoves.Api
dotnet user-secrets set STRIPE_API_KEY "sk_test_YOUR_KEY" --project Src/TranzrMoves.Worker

dotnet user-secrets set TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2 "whsec_YOUR_SECRET" --project Src/TranzrMoves.Api
dotnet user-secrets set TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2 "whsec_YOUR_SECRET" --project Src/TranzrMoves.Worker

# Mapbox (Worker will not start without MAPBOX_BASE_URL)
dotnet user-secrets set MAPBOX_BASE_URL "https://api.mapbox.com" --project Src/TranzrMoves.Api
dotnet user-secrets set MAPBOX_BASE_URL "https://api.mapbox.com" --project Src/TranzrMoves.Worker

dotnet user-secrets set MAPBOX_TOKEN "pk.YOUR_TOKEN" --project Src/TranzrMoves.Api
dotnet user-secrets set MAPBOX_TOKEN "pk.YOUR_TOKEN" --project Src/TranzrMoves.Worker

# Azure Communication Services (email)
dotnet user-secrets set COMMUNICATION_SERVICES_CONNECTION_STRING "endpoint=https://YOUR_RESOURCE.communication.azure.com/;accesskey=YOUR_KEY" --project Src/TranzrMoves.Api

# Azure Blob Storage
dotnet user-secrets set AZURE_STORAGE_CONNECTION_STRING "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net" --project Src/TranzrMoves.Api
dotnet user-secrets set AZURE_STORAGE_CONNECTION_STRING "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net" --project Src/TranzrMoves.Worker

# Cloudflare Turnstile
dotnet user-secrets set TURNSTILE_SECRET_KEY "YOUR_TURNSTILE_SECRET" --project Src/TranzrMoves.Api
dotnet user-secrets set TURNSTILE_SECRET_KEY "YOUR_TURNSTILE_SECRET" --project Src/TranzrMoves.Worker

# Checkout redirect URLs (adjust port if your frontend differs)
dotnet user-secrets set CHECKOUT_SESSION_SUCCESS_URL "http://localhost:3000/checkout/success?session_id={CHECKOUT_SESSION_ID}" --project Src/TranzrMoves.Api
dotnet user-secrets set CHECKOUT_SESSION_SUCCESS_URL "http://localhost:3000/checkout/success?session_id={CHECKOUT_SESSION_ID}" --project Src/TranzrMoves.Worker

dotnet user-secrets set CHECKOUT_SESSION_CANCEL_URL "http://localhost:3000/checkout/cancel" --project Src/TranzrMoves.Api
dotnet user-secrets set CHECKOUT_SESSION_CANCEL_URL "http://localhost:3000/checkout/cancel" --project Src/TranzrMoves.Worker
```

**Verify secrets are configured:**

```bash
dotnet user-secrets list --project Src/TranzrMoves.Api
dotnet user-secrets list --project Src/TranzrMoves.Worker
```

Both commands should list the same keys. If either list is empty or missing keys, the app will fail at startup.

---

## Step 5 — Start the Worker (Terminal 1)

Open a terminal in the repository root. Leave it running.

```bash
dotnet run --project Src/TranzrMoves.Worker
```

**Success — look for these log lines:**

```
[INF] Starting TranzrMoves.Worker with role All
[INF] Subscribed to Redis key expiry events for pay-later balance charges
[INF] Started message listening at rabbitmq://queue/quote-v2-balance-charge
```

The Worker runs as role `All` in Development (Redis listener + RabbitMQ consumer in one process).

---

## Step 6 — Start the API (Terminal 2)

Open a **second** terminal in the repository root. Leave it running.

```bash
dotnet run --project Src/TranzrMoves.Api
```

**Success:**

| Check | URL |
|-------|-----|
| Health | http://localhost:5247/healthz |
| Swagger | http://localhost:5247/swagger |

---

## Step 7 — Forward Stripe webhooks (Terminal 3)

Open a **third** terminal. This forwards Stripe test-mode events to your local API.

```bash
stripe listen --forward-to http://localhost:5247/api/v1/checkout/stripe/webhook/v2
```

Stripe CLI prints a webhook signing secret (`whsec_...`). If it differs from the one you set in Step 4, update it:

```bash
dotnet user-secrets set TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2 "whsec_FROM_STRIPE_CLI" --project Src/TranzrMoves.Api
```

Then restart the API (Terminal 2).

Leave `stripe listen` running while testing checkout.

---

## Step 8 — Test the pay-later pipeline

### Full end-to-end test

1. Use the frontend or Swagger to complete a **pay-later** checkout against the local API.
2. Stripe CLI (Terminal 3) shows webhook events being forwarded.
3. The API receives the webhook and writes a Redis key: `paylater:charge:{quoteId}`.
4. When the key expires (at the due date), the Worker picks it up, publishes to RabbitMQ, and charges the balance via Stripe.

### Fast test without waiting for the due date

If you already have a pay-later quote in Postgres (`PaymentStatus = PaymentSetup`, with a `Later` payment that has a Stripe payment method), expire its Redis key manually.

Replace `YOUR_QUOTE_ID_NO_DASHES` with the quote GUID without dashes (e.g. `a1b2c3d4e5f6...`):

```bash
docker exec tranzr-redis redis-cli SET "paylater:charge:YOUR_QUOTE_ID_NO_DASHES" "{\"quoteId\":\"YOUR_QUOTE_ID_NO_DASHES\"}" EX 2
```

Within a few seconds, Terminal 1 (Worker) should log:

```
Published pay-later balance charge for quote ...
```

### Confirm it worked

1. **Worker logs** — message published and handler ran without errors.
2. **RabbitMQ UI** — http://localhost:15672 → Queues → `quote-v2-balance-charge` → message was consumed.
3. **Postgres** — new `Payment` row with `PaymentType = Balance` and a `PaymentIntentId`:

```bash
docker exec tranzr-postgres psql -U tranzr -d tranzr -c \
  "SELECT \"PaymentType\", \"PaymentIntentId\", \"Status\" FROM tranzrmoves.\"Payments\" ORDER BY \"CreatedAt\" DESC LIMIT 5;"
```

### Alternative — wait for recovery worker

If a quote has `DueDate <= today` and no paid balance yet, the Worker recovery scan publishes every 5 minutes. Watch Terminal 1 logs.

---

## Step 9 — Test the resume quote journey

The resume flow lets a customer pick up an incomplete quote. It has three pieces:

| Piece | What it does |
|-------|----------------|
| **Journey state** | `GET /api/v2/quote/{quoteId}/journey-state` — hydrate quote + step navigation on the same device/session |
| **Resume token** | `POST /api/v2/quote/resume` — validate a signed token from a reminder email and return journey hints |
| **Quote reminder email** | Worker publishes a transactional `quote-reminder` message; Notifications sends it with a resume link |

See [Notifications](documents/notifications/notifications.md) and [Quote V2 frontend guide](QUOTE_V2_FRONTEND_GUIDE.md) for API contracts and client behaviour.

### Extra services for the full email path

Steps 1–6 are enough to test same-session resume. To test reminder emails end-to-end, also run:

1. **Notifications migrations** (once), if you are not using the `notifications-db-migrator` container from Docker Compose:

```bash
dotnet ef database update \
  --project Src/TranzrMoves.Notifications.Infrastructure \
  --startup-project Src/TranzrMoves.Notifications
```

2. **Notifications host** (Terminal 4):

```bash
dotnet run --project Src/TranzrMoves.Notifications
```

**Success:** http://localhost:8081/healthz returns healthy.

3. **SMTP UI** — `docker compose up -d` already starts **smtp4dev**. Open http://localhost:8025 to read captured emails (Notifications uses SMTP on port `2525` in Development).

The Worker (Terminal 1) must stay running with role `All` or `Scheduler` — it hosts `QuoteReminderWorker`.

### Test A — Same-session resume (no email)

Use this to verify journey navigation without waiting for a reminder.

1. In Swagger (http://localhost:5247/swagger), under **Quote (v2)**:
   - `POST /api/v2/quote/ensure` — creates the `tranzr_guest` cookie
   - `POST /api/v2/quote/init` with `{ "quoteType": "Send" }` — note `quote.id` and `quote.version`
   - `PATCH /api/v2/quote/{quoteId}/customer-email-phone` with `{ "email": "you@example.com", "phoneNumber": "+447700900000" }` and header `If-Match: <quote.version>`
   - Optionally PATCH one or two more steps, then stop before payment
2. Call `GET /api/v2/quote/{quoteId}/journey-state`.
3. **Success:** response includes `journey.isResumable = true`, a `journey.resumeStepKey`, and `journey.steps[]` with `complete` / `current` / `locked` statuses. Use `journey.resumeUrl` (or `resumeStepKey`) to know which screen to open next.

### Test B — Reminder email + resume token (full pipeline)

1. **Create an eligible quote** — same as Test A: guest cookie, init, save customer email, leave the quote incomplete (do not reach payment).
2. **Fast-path the reminder scan** — defaults wait 24 idle hours and scan every 60 minutes. For local testing, restart the Worker with shorter intervals:

```bash
QuoteReminders__IdleHoursBeforeReminder=0 \
QuoteReminders__ScanIntervalMinutes=1 \
dotnet run --project Src/TranzrMoves.Worker
```

Alternatively, backdate the quote in Postgres (replace `YOUR_QUOTE_ID`):

```bash
docker exec tranzr-postgres psql -U tranzr -d tranzr -c \
  "UPDATE tranzrmoves.\"QuotesV2\" SET \"ModifiedAt\" = NOW() - INTERVAL '25 hours' WHERE \"Id\" = 'YOUR_QUOTE_ID';"
```

3. **Wait for the Worker scan** (up to one interval). Terminal 1 should log:

```
Quote reminder worker published 1 reminders
```

4. **Check the email** — open http://localhost:8025. You should see **Finish your quote #…** with a **Continue Your Quote** link shaped like:

```
http://localhost:3000/<resume-path>?token=<signed-token>
```

5. **Call the resume API** — copy the `token` query value. Use the **same browser session** (or curl cookie jar) that created the quote, because `POST /api/v2/quote/resume` returns **401** when the `tranzr_guest` cookie does not match the quote session:

```bash
curl -b cookies.txt -X POST http://localhost:5247/api/v2/quote/resume \
  -H "Content-Type: application/json" \
  -d '{"token":"PASTE_TOKEN_HERE"}'
```

6. **Hydrate full state** — on **200**, call `GET /api/v2/quote/{quoteId}/journey-state` (use `quoteId` from the resume response) and confirm the quote loads at the correct step.

### Test C — Frontend (optional)

If the frontend runs on http://localhost:3000, open the resume link from smtp4dev directly. The app should call `POST /api/v2/quote/resume`, then `GET .../journey-state`, and navigate using `journey.resumeUrl` / `resumeStepKey`.

### Confirm it worked

| Check | What to look for |
|-------|------------------|
| Worker logs | `Quote reminder worker published … reminders` |
| RabbitMQ UI | http://localhost:15672 → queue `notifications-send` message consumed |
| smtp4dev | `quote-reminder` email with resume CTA |
| Postgres | `LastResumeEmailSentAt` set on the quote row |
| Resume API | **200** + `isResumable: true` with same guest cookie; **401** if cookie/session differs |

Automated coverage: `Tests/TranzrMoves.UnitTests/Worker/QuoteReminderWorkerTests.cs` (message idempotency) and notification handler tests for the `quote-reminder` template.

---

## Troubleshooting

### Port 5432 already in use

Docker cannot start Postgres because another Postgres is bound to 5432.

- Stop the other Postgres instance and run `docker compose up -d` again, **or**
- Change the port in `docker-compose.yml` to `"5433:5432"` and update `ConnectionStrings:TranzrMovesDatabaseConnection` in both appsettings files to use port `5433`.

### `Missing configuration for supabase` (API)

Set `SUPABASE_URL` and `SUPABASE_KEY` on the Api project (Step 4).

### `MAPBOX_BASE_URL is required` (Worker)

Set `MAPBOX_BASE_URL` on the Worker project (Step 4).

### `Configuration 'Worker:Role' is required` (Worker)

Run the Worker with `dotnet run --project Src/TranzrMoves.Worker` (launch settings set Development automatically). Do not set `ASPNETCORE_ENVIRONMENT=Production`.

### Worker running but Redis expiry does nothing

- Confirm Redis notifications: `docker exec tranzr-redis redis-cli CONFIG GET notify-keyspace-events` → must include `E`.
- Key format must be `paylater:charge:{quoteId}` with **no dashes** in the GUID.
- The quote must exist in Postgres with a valid pay-later payment.

### RabbitMQ queue not draining

- Worker must be running (Terminal 1) with role `All` or `Processor`.
- Check Worker logs for startup errors.

### Webhooks not reaching the API

- Confirm `stripe listen` is running (Terminal 3).
- Confirm the API is on http://localhost:5247.
- Update `TRANZR_STRIPE_WEBHOOK_SIGNING_SECRET_V2` to match the secret Stripe CLI printed.

### Quote reminder email never arrives

- Confirm Notifications is running (http://localhost:8081/healthz) and smtp4dev is up (http://localhost:8025).
- Quote must have customer email saved (`PATCH .../customer-email-phone`) and still be incomplete (`PaymentStatus` pending or null).
- By default the quote must be idle for 24 hours — use the fast-path env vars or SQL backdate in [Step 9](#step-9--test-the-resume-quote-journey).
- Check Worker logs for `Quote reminder worker published` or errors.

### `POST /api/v2/quote/resume` returns 401

The signed token is bound to the quote's guest session. Call resume with the same `tranzr_guest` cookie that created the quote (same browser tab or saved curl cookie jar).

---

## Stop everything

```bash
# Terminals 1–3: Ctrl+C to stop Worker, API, and stripe listen

docker compose down        # stop containers, keep data
docker compose down -v     # stop containers and delete Postgres data
```

---

## Solution layout

| Project | Purpose |
|---------|---------|
| `Src/TranzrMoves.Api` | HTTP API (checkout, quotes, webhooks) |
| `Src/TranzrMoves.Worker` | Background worker (pay-later balance collection) |
| `Src/TranzrMoves.Application` | Application layer (commands, handlers) |
| `Src/TranzrMoves.Infrastructure` | Infrastructure (EF, Stripe, Redis, messaging) |
| `Src/TranzrMoves.Domain` | Domain entities and interfaces |
| `Tests/TranzrMoves.IntegrationTests` | Integration tests |
| `Tests/TranzrMoves.UnitTests` | Unit tests |
| `docker-compose.yml` | Local Postgres, Redis, RabbitMQ |

Worker role details: [Src/TranzrMoves.Worker/README.md](Src/TranzrMoves.Worker/README.md)

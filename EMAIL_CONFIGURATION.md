# Email configuration (deprecated)

Email is no longer sent from the API or Worker. Use the **TranzrMoves.Notifications** service with a configurable provider:
- `Notifications:EmailProvider=Acs` (Azure Communication Services)
- `Notifications:EmailProvider=Smtp` (local smtp4dev)

See [documents/notifications/notifications.md](documents/notifications/notifications.md) for architecture, queue name, and environment variables.

## Required secret (when Notifications provider is ACS)

```bash
COMMUNICATION_SERVICES_CONNECTION_STRING=endpoint=https://...;accesskey=...
```

The monolith publishes `SendNotification` to RabbitMQ (`notifications-send`); the Notifications host renders Handlebars templates and sends via ACS.

For local SMTP testing, run `rnwood/smtp4dev:latest` and set:

```bash
NOTIFICATIONS_EMAIL_PROVIDER=Smtp
```

## Legacy note

Older docs referred to AWS SES and MailKit. That stack has been removed from the monolith.

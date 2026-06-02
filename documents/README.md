# Feature documentation

Architecture and flow documentation for Tranzr Moves services. Each feature has its own folder with a markdown guide, PlantUML sources, and rendered PNG diagrams under `images/`.

## Features

| Feature | Description |
|---------|-------------|
| [balance-charges](balance-charges/balance-charges.md) | Pay-later and deposit remaining balance collection |
| [notifications](notifications/notifications.md) | Transactional email via Notifications worker + ACS |

## Conventions

For a new feature named `my-feature`:

```
documents/my-feature/
  my-feature.md
  my-feature-component.puml
  my-feature-sequence-*.puml
  images/
    *.png          # generated; commit after editing .puml files
```

1. Edit `.puml` sources and markdown; commit (PNG files optional locally).
2. CI runs when `documents/` changes (except `images/` only). It renders all `.puml`, replaces PNGs, and pushes `chore: update rendered PlantUML diagram PNGs`.

Preview locally: `./scripts/render-plantuml.sh` from the repository root.

## Operational guides

Kubernetes and deployment guides live under [`docs/`](../docs/) (for example [pay-later worker deployment](../docs/pay-later-worker-kubernetes-deployment.md)).

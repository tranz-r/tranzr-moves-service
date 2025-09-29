# Tech Stack

## Context

Global tech stack defaults for Agent OS projects, overridable in project-specific `.agent-os/product/tech-stack.md`.

- App Framework: ASP.NET Core 9 (Controller + MVC)
- Language: C# (latest supported by .NET 9)
- Primary Database: PostgreSQL 17+
- ORM: Entity Framework Core 9
- JavaScript Framework: React (latest stable)
- Build Tool (frontend): Vite (or Next.js if SSR/ISR is needed)
- Import Strategy: ES Modules
- Package Manager: pnpm (or npm)
- Node Version: 22 LTS
- CSS Framework: TailwindCSS 4.0+
- UI Components: shadcn/ui (React)
- UI Installation: shadcn CLI (component-by-component)
- Font Provider: Google Fonts
- Font Loading: Self-hosted for performance
- Icons: Lucide React
- Application Hosting (Backend): Azure Container Apps or AKS (Kubernetes) with YARP as API gateway
- Application Hosting (Frontend): Vercel (for Next.js) or Azure Static Web Apps (for Vite SPA)
- Hosting Region: Closest to user base (e.g., UK South)
- Database Hosting: Azure Database for PostgreSQL Flexible Server (or Supabase)
- Database Backups: PITR enabled + daily automated backups
- Asset Storage: Azure Blob Storage
- CDN: Azure Front Door (or Cloudflare CDN)
- Asset Access: Private with SAS-signed URLs
- Secrets Management: Azure Key Vault
- Caching/Queues (optional): Azure Cache for Redis / Azure Storage Queues
- Observability: OpenTelemetry + Azure Application Insights (logs/traces/metrics)
- CI/CD Platform: GitHub Actions (OIDC to Azure)
- CI/CD Trigger: Push to main/staging branches
- Tests: Unit + integration tests run before deployment (dotnet test)
- Production Environment: main branch deployments
- Staging Environment: staging branch deployments
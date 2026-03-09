# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore dependencies
dotnet restore

# Run the development server
dotnet run --project Odontari.Web

# Database: apply migrations
dotnet ef database update --project Odontari.Web

# Database: add a new migration after model changes
dotnet ef migrations add <MigrationName> --project Odontari.Web
```

No test project exists — validation is done via manual checklists in the root `.md` files.

## Architecture

**Odontari** is a multi-tenant dental clinic SaaS platform built on **ASP.NET Core 9.0 MVC** with **Entity Framework Core 9** + **SQL Server**.

### Project Layout

Single web project: `Odontari.Web/`

- `Areas/Clinica/` — All tenant-facing operations (appointments, patients, billing, clinical tools)
- `Areas/Saas/` — SuperAdmin panel (plans, clinics, subscriptions)
- `Areas/Identity/` — ASP.NET Identity UI (login, registration)
- `Controllers/HomeController.cs` — Public landing page only
- `Data/ApplicationDbContext.cs` — EF Core DbContext, 24+ entities
- `Models/` — Domain entities; `Models/Enums/` for all enums
- `ViewModels/` — Presentation-layer DTOs
- `Services/` — Scoped services (DI-registered in Program.cs)
- `Filters/` — Action filters for access control
- `wwwroot/js/` — Vanilla JS for clinical tools (odontograms, periodontogram)
- `wwwroot/css/` — Feature-scoped CSS files

### Multi-Tenant Pattern

Every business entity has a `ClinicaId` (NOT NULL). Tenant resolution uses `IClinicaActualService`, which reads `ApplicationUser.ClinicaId` from the logged-in user. All queries must filter by `ClinicaId` — never fetch data without scoping to the current clinic.

The filter `ValidarAccesoClinicaFilter` runs before every clinic-area action, verifying the clinic exists and its subscription is active.

### Roles

**SaaS-level** (no ClinicaId): `SuperAdmin`, `Soporte`, `Auditor`
**Clinic-level** (require ClinicaId): `AdminClinica`, `Recepcion`, `Doctor`, `Finanzas`

Role constants live in `Data/OdontariRoles.cs`. View-level permissions go beyond roles — `ValidarVistaPermisoAuthorizationFilter` checks both role and per-clinic dynamic blocking (`BloqueoVistaClinicaDinamica`) and per-user overrides (`UsuarioVistaPermiso`).

### Key Domain Concepts

- **Clinica** — Tenant root; holds fiscal config (RNC, NCF ranges for Dominican Republic invoicing), plan, and subscription.
- **Paciente** → **Cita** (appointment) → **ProcedimientoRealizado** (line items) → **OrdenCobro** (bill) → **Factura** (official invoice).
- **Odontograma** — Interactive SVG dental chart. Stored as `EstadoJson` (JSON). Comes in adult (32 teeth FDI) and pediatric (20 teeth) variants. Appointment-linked odontograms auto-sync findings to `ProcedimientosRealizados`.
- **Periodontograma** — Periodontal chart. Stored as JSON. Uses localStorage for in-progress editing. Saved events log detailed metrics to `HistorialClinico`.
- **ArchivoSubido** — Patient files in Azure Blob Storage. Uses rollback pattern: if DB save fails after blob upload, the blob is deleted.

### Services (DI-registered)

| Service | Purpose |
|---|---|
| `IClinicaActualService` | Resolves current tenant from logged-in user |
| `IPuertaEntradaService` | Validates clinic access gate (subscription check) |
| `IBlobUploadService` | Azure Blob Storage abstraction (upload/delete/stream) |
| `IAuditService` | Logs critical actions per clinic |
| `IUsuarioVistasPermisoService` | Per-user view permissions |
| `IBloqueoVistaClinicaService` | Per-clinic dynamic view blocking |
| `FacturaService` / `FacturaPdfService` | Dominican Republic NCF invoicing + PDF |
| `HistogramaExportService` / `ReporteFinancieroExportService` | Excel exports (ClosedXML) |
| `HistorialPagosPdfService` | PDF payment history (QuestPDF) |

### Frontend

Server-rendered Razor views. No JS framework — interactive clinical tools (odontogram, periodontogram) are implemented in vanilla JS files under `wwwroot/js/`. CSS is scoped by feature (e.g., `odontograma.css`, `periodontograma.css`, `agenda-dinamica.css`).

### Configuration

`appsettings.json` requires:
- `ConexionSql` — SQL Server connection string
- `AzureBlob:ConnectionString` + `AzureBlob:Container` — Azure Blob Storage

### Seed Data

On first run, `SeedData.cs` creates roles and:
- `superadmin@odontari.com` / `SuperAdmin2025!`
- Demo clinic with `recepcion@clinica.com`, `doctor@clinica.com`

### Reference Documentation

The root `.md` files are authoritative specs — consult them before modifying these features:
- `PERIODONTOGRAMA_ESPECIFICACION.md` — Complete periodontogram spec (FDI layout, 14 clinical parameters, JSON schema, validation rules)
- `FASES_ODONTARI.md` — Project phases, test credentials, database schema overview
- `REUSO-logica-subida-fotos.md` — Azure Blob + SQL rollback pattern
- `CHECKLIST_MULTITENANT_REVISION.md` — Multi-tenant compliance checklist

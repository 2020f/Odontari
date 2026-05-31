# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

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

# Build only (no run) — use to verify compilation without blocking on a running process
dotnet build Odontari.Web --no-restore
```

No test project exists — validation is done via manual checklists in the root `.md` files.

**Important:** `dotnet build` will report `MSB3021` lock errors when the app is already running. This is not a C# compile error. Check for `error CS` specifically to distinguish real errors from process-lock noise.

## Architecture

**Odontari** is a multi-tenant dental clinic SaaS platform built on **ASP.NET Core 9.0 MVC** with **Entity Framework Core 9** + **SQL Server**.

Single web project: `Odontari.Web/`

### Areas

| Area | Purpose |
|---|---|
| `Areas/Clinica/` | All tenant-facing operations — the main app |
| `Areas/Saas/` | SuperAdmin panel (plans, clinics, subscriptions) |
| `Areas/Identity/Pages/Account/` | Scaffolded Identity pages (Login, Register, ForgotPassword, ResendEmailConfirmation). Register/ForgotPassword/Resend are intentionally disabled — they render a styled notice instead of a functional form. |

### Multi-Tenant Pattern

Every business entity carries `ClinicaId` (NOT NULL, never nullable FK). The tenant is resolved via `IClinicaActualService.GetClinicaIdActual()` / `GetClinicaIdActualAsync()`, which reads `ApplicationUser.ClinicaId` from the logged-in user. **Every query against a tenant entity must filter by `ClinicaId` — no exceptions.**

`ValidarAccesoClinicaFilter` runs before every `Areas/Clinica` action to confirm the clinic exists and the subscription is active. `ValidarVistaPermisoAuthorizationFilter` adds a second check: per-clinic dynamic blocking (`BloqueoVistaClinicaDinamica`) and per-user overrides (`UsuarioVistaPermiso`). Controllable view keys are declared in `Data/VistasClinica.cs` — add new ones there when adding blockable modules.

### Roles

**SaaS-level** (no ClinicaId): `SuperAdmin`, `Soporte`, `Auditor`
**Clinic-level** (require ClinicaId): `AdminClinica`, `Recepcion`, `Doctor`, `Finanzas`

Role string constants live in `Data/OdontariRoles.cs`. Use them in `[Authorize(Roles = ...)]` attributes — never hardcode role name strings.

### Domain Flow

```
Paciente → Cita → ProcedimientoRealizado → OrdenCobro → Pago
                                                       → Factura (with NCF)
         → Odontograma (JSON blob)
         → Periodontograma (JSON blob)
         → HistorialClinico (event log)
         → HistoriaClinicaSistematica (one-to-one)
         → ArchivoSubido (Azure Blob)
         → ConsentimientoGenerado ← PlantillaConsentimiento
```

`AtencionController` is the doctor's working surface for a `Cita` — it loads the Expediente (procedures + odontogram + historial + consents) and drives status transitions.

### Key Domain Model Notes

**`Cita`** has no `Diagnostico` or `NotasCita` fields. Diagnoses live in `Odontograma.EstadoJson`; clinical notes are stored as `HistorialClinico` events linked via `CitaId`. `Cita.Doctor` is an `ApplicationUser?` navigation property (FK = `DoctorId` string). `ApplicationUser` has a `NombreCompleto` property — always prefer it over `Email` for display.

**`ProcedimientoRealizado`** carries `MarcadoRealizado` (bool), `PrecioAplicado` (decimal), `RealizadoAt` (DateTime?), and optional `Notas`. Only `MarcadoRealizado = true` rows count toward billing totals.

**`HistorialClinico`** is an append-only event log. Never delete or edit rows — always add new entries. Link to a cita via `CitaId` when the event originates from an appointment.

### Controllers (Areas/Clinica)

| Controller | Responsibility |
|---|---|
| `AtencionController` | Doctor's active-appointment surface: Expediente view, procedure CRUD, status transitions (EnSala → EnAtencion → Finalizada), consent generation |
| `ExpedienteController` | Read-only patient record views: Histograma (timeline), Odontograma, Periodontograma, HistoriaClinicaSistematica, file listing. `ResumenCita(int id)` returns JSON summary for the timeline drawer. `AgregarNotaClinica` (POST, Doctor/AdminClinica only) appends a `HistorialClinico` entry with `TipoEvento = "Nota clínica"` and the doctor's `UsuarioId` |
| `AgendaController` | Calendar views (VistaDinamica), appointment CRUD, status changes from agenda. AJAX endpoints: `GET DetalleCita(int id)` → JSON for the cita detail drawer; `GET GetHorasOcupadas(string doctorId, string fecha)` → JSON with `ocupadas[]` (inicio/fin strings) + `horaEntrada`/`horaSalida` for the DateTimePicker |
| `ConsentimientoController` | Template management (AdminClinica) + document signing flow (Doctor) — gated by `Plan.PermiteConsentimiento` |
| `SubirArchivosController` | Patient file upload/download/delete via Azure Blob — must follow rollback pattern |
| `CajaController` | Payment collection, invoice generation, payment history |
| `PersonalController` | Clinic user management (create, edit, toggle active) — enforces plan limits |

### Services (DI-registered in Program.cs)

| Service | Purpose |
|---|---|
| `IClinicaActualService` | Resolves current tenant from the logged-in user |
| `IPuertaEntradaService` | Subscription/access gate check |
| `IBlobUploadService` | Azure Blob upload/delete/stream with rollback pattern |
| `IAuditService` | Logs critical actions per clinic |
| `IUsuarioVistasPermisoService` | Per-user view permission cache |
| `IBloqueoVistaClinicaService` | Per-clinic dynamic view blocking cache |
| `IFacturaService` / `FacturaPdfService` | Dominican Republic NCF invoicing + PDF generation |
| `HistogramaExportService` / `ReporteFinancieroExportService` | Excel exports (ClosedXML) |
| `HistorialPagosPdfService` | PDF payment history (QuestPDF) |

### Frontend

Server-rendered Razor views. No JS framework. Interactive clinical tools use vanilla JS:

| JS file | Feature |
|---|---|
| `odontograma.js` / `odontograma-infantil.js` | Standalone odontogram (Expediente page) |
| `odontograma-atencion.js` / `odontograma-atencion-infantil.js` | Odontogram embedded in Atencion/Expediente |
| `periodontograma.js` | Full periodontal chart, uses localStorage for draft state |
| `agenda-datetime-picker.js` | `OdontariDatePicker` class — custom date/time picker used in appointment forms. Renders a calendar + hour-slot grid overlay. Fetches booked slots via `GET /Clinica/Agenda/GetHorasOcupadas?doctorId=&fecha=` to disable occupied times. Initialized per-view with `new OdontariDatePicker({ trigger, hiddenInput, doctorSelect, horasOcupadasUrl })`. The hidden input always receives `yyyy-MM-ddTHH:mm`. |

### CSS Architecture

CSS is feature-scoped — every new module gets its own `.css` file linked via `@section Styles`. Never add module-specific rules to global files.

**Globally loaded** (via `_LayoutClinica.cshtml`):

| File | Scope |
|---|---|
| `clinica-layout.css` | Sidebar, top nav, page shell |
| `clinica-content.css` | Rules scoped to `.clinica-content` wrapper: `.content-card`, `.content-header`, `.tbl-action-*`, `.odon-badge`, `.expediente-*`, `.histograma-*` |
| `odontari-forms.css` | `.odon-form-*` layout, `.odon-btn` / `.odon-btn-primary` buttons — available everywhere |

**Feature-scoped** (linked per-view):

| File | View |
|---|---|
| `histograma.css` | `Expediente/Histograma` — `.histo-section`, `.histo-tl-*` timeline, `.cita-drawer-*` floating panel |
| `subirarchivos.css` | `SubirArchivos/Index` — `.sa-card-body`, `.sa-upload-zone`, `.sa-file-item` |
| `personal-gafete.css` | `Personal/Index` — `.gafete-card`, `.gafete-*` employee badge cards |
| `consentimiento.css` | `Consentimiento/*` — `.consent-doc-*`, `.consent-sign-*` |
| `caja-cobrar.css` | `Caja/Cobrar` |
| `agenda.css` / `agenda-dinamica.css` | Agenda views. `agenda-dinamica.css` also contains `.cita-drawer-*` styles for the appointment detail drawer |
| `agenda-form.css` | Appointment create/edit forms |
| `agenda-datetime-picker.css` | `OdontariDatePicker` overlay, panel, calendar grid, hour-slot grid, 12h/24h toggle. z-index: 10100 (above all modals) |
| `odontograma.css` / `periodontograma.css` | Clinical chart views |

**Form pattern:** All create/edit views use `.odon-form-page` > `.odon-form-card` > `.odon-form-header` / `.odon-form-body` / `.odon-form-footer`.

**Card pattern for list/data sections:** `.histo-section` > `.histo-section-head` + `.histo-section-body` (used in Histograma and new feature sections — prefer this over Bootstrap `.card`).

### Razor Nullable Reference Types (NRT)

Razor treats `@model T` as `T?` (nullable) under NRT analysis. Using `Model?.X` changes the inferred type of the result and causes the compiler to cascade CS8602 warnings to every subsequent `Model.X` access in the view.

**Rule:** Use `Model!.X` (null-forgiving operator) when accessing the model in views where the controller always provides a non-null model. Reserve `Model?.X` only for optional sub-properties (e.g., `Model!.Paciente?.Nombre`). This eliminates cascading CS8602 warnings without masking real nullability issues.

### Identity Pages

Only `Login.cshtml` is a true scaffolded Identity page. `Register`, `ForgotPassword`, and `ResendEmailConfirmation` are stub scaffolds with empty `OnGet`/`OnPost` that render a "not available" notice using the same `_LayoutLogin.cshtml`. Do not add functional logic to those stubs.

### NCF / Dominican Republic Invoicing

`Clinica` holds fiscal config (RNC, RazonSocial, ItbisTasa, ModoFacturacion). NCF sequences are managed via `NCFRango` + `NCFMovimiento`. `FacturaService` allocates the next NCF and records the movement atomically. See `FASES_ODONTARI.md` for the full NCF flow.

### Consentimiento Informado Digital

`PlantillaConsentimiento` — admin-configured templates with text markers (`{NOMBRE_PACIENTE}`, `{CEDULA}`, `{EDAD}`, `{DOCTOR}`, `{FECHA}`, `{HORA}`, `{CLINICA}`). `Version` increments automatically when `TextoBase` changes.

`ConsentimientoGenerado` — snapshot document. `TextoFinal` stores the already-interpolated text at generation time (legal immutability). `FirmaDigitalBase64` stores the HTML5 Canvas signature as a base64 PNG. States: `Pendiente → Firmado | Rechazado | Cancelado`. The `TextoBase` field is plain text — `TextoFinal` renders with `white-space: pre-wrap` in CSS, no `Html.Raw()` needed.

Generated from `AtencionController/Expediente` (doctor flow) or managed from `ConsentimientoController/Index` (admin view with patient/doctor search).

### Azure Blob Rollback Pattern

When uploading patient files: upload to Blob first, then save to DB. If the DB save throws, delete the blob. This prevents orphaned blobs. See `REUSO-logica-subida-fotos.md` for the exact pattern — always follow it in `SubirArchivosController`.

### Configuration

`appsettings.json` requires:
- `ConexionSql` — SQL Server connection string
- `AzureBlob:ConnectionString` + `AzureBlob:Container` — Azure Blob Storage

### Seed Data

On first run, `SeedData.cs` creates roles and:
- `superadmin@odontari.com` / `SuperAdmin2025!`
- Demo clinic with `recepcion@clinica.com`, `doctor@clinica.com`

### VistaDinamica — Cita Detail Drawer

Clicking a cita block in `VistaDinamica` opens a slide-in drawer (not a navigation to the edit page). The block is a `<button data-cita-id="...">` that triggers a `fetch` to `DetalleCita`. The drawer shows patient data, doctor, date, and the reception note (Cita.Motivo). The **Edit** button in the drawer footer is rendered only when the server confirms `puedeEditar = true` (AdminClinica or Recepcion) in the JSON response — never infer edit permission client-side.

### Histograma Timeline — Notes

The timeline merges two sources:
1. **`HistorialClinico` rows** — clinical events, odontogram updates, procedure notes. `UsuarioId` is resolved to `NombreCompleto` via a single batched `_db.Users` query after materialization (not inside the EF Select, as `NombreCompleto` may be a computed C# property).
2. **`Cita.Motivo`** — reception notes, queried separately from `Citas` and merged in memory with `TipoEvento = "Nota de recepción"`, `AutorNombre = "Recepción"`.

`HistorialEventoViewModel` has `AutorNombre` (nullable string). Always resolve it server-side; the view only renders it if non-null.

Doctor clinical notes (`TipoEvento = "Nota clínica"`) are added via `AgregarNotaClinica` POST, which is restricted to Doctor + AdminClinica roles. Timeline items with `Id == 0` are synthetic (from Cita.Motivo) and have no `VerEvento` link.

### Timezone — Dominican Republic (UTC-4)

The server runs UTC (Azure). All past-date validations use `DateTime.UtcNow.AddHours(-4)` as the DR local time. The 30-minute tolerance in `AgendaController.Create` absorbs clock skew and form submission latency. The `min` attribute on date inputs uses `DateTime.UtcNow.AddHours(-4).Date` to avoid showing the wrong minimum date after 8 PM local time. **Never use `DateTime.Now` or `DateTime.Today` in server-side validation.**

### Azure Deployment

Data Protection keys are persisted to Azure Blob (`dataprotection-keys.xml` in the configured container) via `PersistKeysToAzureBlobStorage`. Without this, every App Service restart invalidates auth cookies. The blob connection string is read from `AzureBlob:ConnectionString` (App Service setting: `AzureBlob__ConnectionString`).

App Service application settings use `__` (double underscore) to map nested JSON keys: `AzureBlob:ConnectionString` → `AzureBlob__ConnectionString`.

### Plan Edit Rules

`PlanesController.Edit` (POST) enforces that `MaxUsuarios` and `MaxDoctores` can never be decreased below their current saved values. This guard protects active clinics already using the current quota. Numeric limits propagate immediately to clinic checks since `Clinica.Plan` is loaded fresh per request.

### EF Core Query Rules

**Always use `AsNoTracking()` on every read-only query.** Every controller action that only displays data must call `.AsNoTracking()` before any filter or include. Omitting it forces EF Core to register every loaded entity in its change-tracking graph, consuming significant memory and CPU under load.

**Never load entities into memory to count or filter — push to SQL.** The canonical mistake is:
```csharp
// BAD: loads all rows into memory
var list = await _db.Citas.Where(...).ToListAsync();
ViewBag.Count = list.Count(c => c.Estado == EstadoCita.Finalizada); // done in C#

// GOOD: single indexed COUNT(*) in SQL
ViewBag.Count = await _db.Citas.AsNoTracking().Where(...).CountAsync(c => c.Estado == EstadoCita.Finalizada);
```

**Navigation property fixup does not work with `AsNoTracking()`.** With change tracking enabled, EF Core automatically connects navigation properties (`pr.Cita`, `o.Paciente`) to entities already loaded in the same context. With `AsNoTracking()` this fixup is disabled — accessing an unincluded navigation returns `null`. The correct fix is to push any navigation-based filter into SQL before materializing:
```csharp
// BAD: pr.Cita is null with AsNoTracking if not explicitly Included
var list = await query.AsNoTracking().ToListAsync();
list = list.Where(pr => pr.Cita!.DoctorId == doctorId).ToList(); // NullReferenceException

// GOOD: filter in SQL before materialization
IQueryable<ProcedimientoRealizado> q = _db.ProcedimientosRealizados.AsNoTracking()...;
if (doctorId != null) q = q.Where(pr => pr.Cita!.DoctorId == doctorId);
var list = await q.ToListAsync();
```

**Declare conditional IQueryable as `IQueryable<T>`, not `var`.** After `.Include()`, the inferred type is `IIncludableQueryable<T, TProperty>`. Adding `.Where()` to it returns plain `IQueryable<T>`, which cannot be assigned back — causing `CS0266`. Always use explicit typing when the query is built conditionally:
```csharp
// Will not compile if doctorId branch is added:
var q = _db.Citas.AsNoTracking().Include(c => c.Paciente); // IIncludableQueryable
q = q.Where(...); // CS0266

// Correct:
IQueryable<Cita> q = _db.Citas.AsNoTracking().Include(c => c.Paciente);
if (filter) q = q.Where(...);
```

**`CommandTimeout` is 120s** (configured in `Program.cs`). The EF Core default is 30s — do not lower it.

**`NombreCompleto` on `ApplicationUser` may not be translatable in all EF Core Select projections.** When building a dictionary or projection that needs user display names, select raw fields (`u.Id`, `u.NombreCompleto`, `u.Email`) into an anonymous list first, then `.ToDictionary()` in memory:
```csharp
// Safe pattern — avoids potential translation failure on computed properties
var usersRaw = await _db.Users.AsNoTracking()
    .Where(u => ids.Contains(u.Id))
    .Select(u => new { u.Id, u.NombreCompleto, u.Email })
    .ToListAsync();
var dict = usersRaw.ToDictionary(u => u.Id, u => u.NombreCompleto ?? u.Email ?? "");
```

### Cita & Cobro Status Enums

`EstadoCita` drives the entire clinical workflow — status transitions are enforced by `AtencionController` and `AgendaController`:

```
Solicitada(0) → Confirmada(1) → EnSala(2) → EnAtencion(3) → Finalizada(4)
                                                            → Cancelada(5)
                                                            → NoShow(6)
```

`EstadoCobro` tracks payment state on `OrdenCobro`:
```
Pendiente(0) → Parcial(1) → Pagado(2)
             → Anulado(3)
```

An `OrdenCobro` is created when a `Cita` is marked `Finalizada`. A `Factura` (with NCF) is created on the first `Pago` via `IFacturaService.CrearFacturaSiNoExisteAsync`.

### Odontari.Landing (separate project)

`Odontari.Landing/` is a **Next.js 14 static site** (output: `"export"`) — completely independent of the ASP.NET Core backend. It has its own `AGENTS.md` with full details. Commands: `npm run dev` / `npm run build`. Design tokens and component conventions are defined in `tailwind.config.ts` and `app/globals.css`.

### Reference Documentation

Consult before modifying these features:
- `PERIODONTOGRAMA_ESPECIFICACION.md` — FDI layout, 14 clinical parameters, JSON schema, validation rules
- `FASES_ODONTARI.md` — Project phases, test credentials, schema overview
- `REUSO-logica-subida-fotos.md` — Azure Blob + SQL rollback pattern
- `CHECKLIST_MULTITENANT_REVISION.md` — Multi-tenant compliance checklist

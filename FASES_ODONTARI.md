# ODONTARI – Resumen de las 4 fases

## Estado actual

- **Fase 1** – Hecha: base del sistema, Identity, roles, seed SuperAdmin, migración SQL Server.
- **Fase 2** – Hecha: modelo de datos V1, enums, controladores y ViewModels Panel Clínica (Pacientes, Citas, Tratamientos, OrdenCobro/Pago) y Panel SaaS (Planes, Clínicas, Edit, Suscripciones).
- **Fase 3** – Hecha: UI V1 Panel Clínica (Dashboard, Agenda, Check-in, Atención doctor, Expediente/Procedimientos, Caja/Cobros) y Panel SaaS (Dashboard, CRUD Planes/Clínicas, Suscripciones). Seed clínica demo + usuario Recepción y Doctor.
- **Fase 4** – Pendiente: multitenancy estricto, bloqueo por suscripción, permisos por rol, auditoría.

---

## Base de datos: SQL Server

- **Cadena de conexión:** clave `ConexionSql` en `appsettings.json`.
- **Servidor:** `srSkimberlyn\SQLEXPRESS01`, base de datos `ODONTARI_DB`.
- **Migraciones:** una sola migración `InitialSqlServer` (Identity + modelo ODONTARI). Para aplicar: `dotnet ef database update` desde `Odontari.Web`. Para crear una nueva migración tras cambiar el modelo: `dotnet ef migrations add NombreMigracion`.

## Cómo probar (flujo completo)

1. Ejecutar: `dotnet run` desde `Odontari.Web`.
2. Ir a la URL que indique la consola (ej. http://localhost:5xxx).
3. **Credenciales:**
   - **SuperAdmin:** `superadmin@odontari.com` / `SuperAdmin2025!` → Panel SaaS (Dashboard, Planes, Clínicas, Suscripciones).
   - **Recepción (clínica demo):** `recepcion@clinica.com` / `Recepcion2025!` → Panel Clínica (Agenda, Pacientes, Caja).
   - **Doctor (clínica demo):** `doctor@clinica.com` / `Doctor2025!` → Panel Clínica (Mis citas, Expediente, procedimientos, finalizar atención).
4. **Flujo Cita → Cobro (con recepcion@clinica.com y doctor@clinica.com):**
   - Recepción: **Agenda** → Nueva cita (paciente, doctor, fecha/hora) → Confirmar → Check-in (En sala).
   - Doctor: **Mis citas (Atención)** o desde Agenda → Expediente → Agregar procedimiento (tratamiento) → Marcar realizado → Finalizar atención (genera orden de cobro).
   - Recepción: **Caja** → Ver orden pendiente → Cobrar (monto, método de pago).
5. Crear tratamientos en **Tratamientos** (catálogo) para poder agregarlos en el expediente.

---

## Estructura creada (Fase 1 y 2)

- **Identity:** `ApplicationUser` (ClinicaId, NombreCompleto, Activo), roles SaaS y Clínica, seed SuperAdmin.
- **Entidades:** Plan, Clinica, Suscripcion, Paciente, Cita, Tratamiento, ProcedimientoRealizado, OrdenCobro, Pago, AuditLog.
- **Enums:** EstadoCita, EstadoCobro.
- **Migraciones:** CreateIdentitySchema, AddApplicationUserFields, H01_H04_ModeloDatosV1.
- **Área SaaS:** `PlanesController`, `ClinicasController` con Index/Create y vistas mínimas.

---

## Próximos pasos (Fase 3)

- Panel Clínica: Agenda/Citas, Check-in, Atención doctor, Expediente/procedimientos, Orden de cobro, Caja/Cobros.
- Panel SaaS: Dashboard, CRUD completo Clínicas/Planes, Suscripciones, usuarios internos.
- Flujo completo: crear cita → confirmar → check-in → iniciar atención → procedimientos → finalizar → orden cobro → caja → pago.

## Fase 4

- Filtrar todo por `ClinicaId` (multitenancy).
- Bloquear crear/editar si suscripción vencida.
- Permisos por rol (Doctor no ve caja, etc.).
- Validar conflictos de agenda.
- Auditoría en acciones críticas.

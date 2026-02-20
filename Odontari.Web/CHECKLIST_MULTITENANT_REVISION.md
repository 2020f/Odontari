# Revisión Checklist Multitenant SaaS - Odontari

Revisión del proyecto contra el checklist multitenant. Estado: **cumplimiento alto**; se indican gaps menores y recomendaciones.

---

## A) Identidad del Tenant

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Existe entidad Tenant/Clinica con ID único | ✅ | `Clinica` con `Id` (int) único. |
| Cada Tenant tiene estado: Activo / Suspendido / Eliminado | ⚠️ Parcial | Solo `Activa` (bool). No hay "Suspendido" ni soft delete "Eliminado" diferenciados. |
| Datos mínimos: Nombre, Slug/Dominio, FechaCreacion | ⚠️ Parcial | `Nombre`, `FechaCreacion` ✅. No hay `Slug` ni dominio (no aplica si no usas subdominios). |
| Tenant NO se deduce por nombre, sino por TenantId real | ✅ | Se usa siempre `ClinicaId` (Id de `Clinica`). |

**Recomendación:** Si más adelante quieres estados granulares, añadir en `Clinica`: `bool Suspendida`, `DateTime? FechaEliminado` (soft delete). Slug solo si vas a usar subdominios.

---

## B) Resolución del Tenant

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Al iniciar sesión se determina TenantId del usuario | ✅ | `ApplicationUser.ClinicaId`; usuario pertenece a una clínica (o null = SaaS). |
| TenantId disponible en cada request | ✅ | `IClinicaActualService.GetClinicaIdActualAsync()` desde el usuario actual. |
| Request sin TenantId válido → rechazada | ✅ | Área Clinica: si `cid == null` → `RedirectToAction("SinClinica")`. |
| Operar con otro TenantId → AccessDenied | ✅ | Todas las consultas/escrituras usan `cid` del contexto; no se acepta ClinicaId del cliente en el panel Clínica. |

---

## C) Aislamiento de Datos (crítico)

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Tablas de negocio tienen TenantId (ClinicaId) | ✅ | Paciente, Cita, OrdenCobro, Tratamiento, Odontograma, HistorialClinico, HistoriaClinicaSistematica, Suscripcion, BloqueoVistaClinicaDinamica. |
| TenantId NOT NULL en esas tablas | ✅ | Todas las entidades de negocio tienen `ClinicaId` obligatorio (int). |
| Índice por TenantId | ✅ | Migraciones: `HasIndex("ClinicaId")` en Citas, Pacientes, OrdenCobro, Tratamientos, Odontograma, HistorialClinico, HistoriaClinicaSistematica, etc. |
| No hay endpoints sin filtrar por TenantId | ✅ | Controladores del área Clinica usan `_clinicaActual.GetClinicaIdActualAsync()` y filtran con `ClinicaId == cid`. |
| Lecturas: SIEMPRE filtro por ClinicaId | ✅ | PacientesController, CajaController, AgendaController, ReportesController, ExpedienteController, TratamientosController, PersonalController: todas las queries con `Where(x => x.ClinicaId == cid)`. |
| Escrituras: TenantId lo asigna el sistema | ✅ | Create/Edit asignan `ClinicaId = cid.Value`; nunca desde el ViewModel del cliente en el panel Clínica. |
| Updates/Deletes: validar que el registro sea del tenant actual | ✅ | Edit/Delete cargan entidad con `Where(x => x.ClinicaId == cid && x.Id == id)`; si no existe → `NotFound()`. |
| No buscar por ID global sin verificar TenantId | ✅ | En todas las acciones se combina `Id` con `ClinicaId == cid`. |

**Prueba obligatoria:** Usuario de clínica A no puede ver/editar registros de clínica B (por ID). **Cumplido:** las consultas siempre incluyen `ClinicaId == cid`; si el ID es de otra clínica se devuelve `NotFound()`.

---

## D) Usuarios y Roles por Tenant

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Usuario pertenece a un Tenant | ✅ | `ApplicationUser.ClinicaId` (nullable para usuarios SaaS). |
| Roles definidos (AdminTenant, Recepción, Médico, Finanzas) | ✅ | `OdontariRoles`: AdminClinica, Recepcion, Doctor, Finanzas (+ SuperAdmin, Soporte, Auditor). |
| AdminTenant NO administra otros tenants | ✅ | AdminClinica solo gestiona usuarios de su clínica; filtro por `u.ClinicaId == cid`. |
| Rol SaaSAdmin/SuperAdmin separado | ✅ | SuperAdmin (y Soporte, Auditor) sin ClinicaId; panel Saas solo `[Authorize(Roles = SuperAdmin)]`. |
| Vistas y menús por rol; bloquear rutas | ✅ | `ValidarVistaPermisoAuthorizationFilter` + bloqueo por clínica (BloqueoVistaClinicaDinamica) y por usuario (UsuarioVistaPermiso). Menú con `PuedeVer(key)`. |

---

## E) Seguridad (anti fugas)

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Validación en servidor | ✅ | Toda la lógica de tenant y permisos en servidor; no se confía en la UI. |
| Protección contra enumeración | ✅ | Si piden ID de otro tenant → `NotFound()` (no 403), sin revelar existencia del recurso. |
| Logs no exponen datos de otros tenants | ✅ | `AuditLog` tiene `ClinicaId`; se registra por tenant. Revisar que logs de aplicación no incluyan datos sensibles de otros tenants. |
| Auditoría: quién, fecha, tenant | ✅ | `IAuditService.RegistrarAsync(clinicaId, userId, accion, entidad, entidadId, detalle)`. |
| Políticas/authorizations para evitar bypass | ✅ | Área Clinica con `ValidarAccesoClinicaFilter` (suscripción vigente) y `ValidarVistaPermisoAuthorizationFilter`; rutas directas validadas. |

---

## F) Flujo SaaS (planes, suscripción, límites)

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Cada Tenant tiene Plan | ✅ | `Clinica.PlanId` → `Plan` (MaxUsuarios, MaxDoctores, etc.). |
| Plan controla límites | ✅ | `Plan`: MaxUsuarios, MaxDoctores, PermiteFacturacion, etc. |
| Suscripción: inicio, vencimiento, estado, trial | ✅ | `Suscripcion`: Inicio, Vencimiento, Activa, Suspendida. |
| Al vencer: bloquea acceso o solo lectura | ✅ | `PuertaEntradaService`: `Vencimiento.Date > hoy` para vigente; si vencida → no se permite acceso al panel. |
| No crear registros si vencido | ✅ | Login rechaza usuarios de clínica con suscripción vencida; `ValidarAccesoClinicaFilter` redirige a Bloqueo si no vigente. |
| Tenant suspendido no puede operar | ✅ | `clinica.Activa` y suscripción `!Suspendida` verificados en `ValidarAccesoPanelClinicaAsync`. |
| Pantalla/mensaje Plan vencido | ✅ | `HomeController.Bloqueo(motivo)` y vista; mensaje "Suscripción vencida (suspensión por vencimiento). Renueve para continuar." |

---

## G) Datos compartidos vs por Tenant

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Definir qué es global y qué por tenant | ✅ | Global: Plan, Roles (Identity). Por tenant: Clinica, Pacientes, Citas, Tratamientos, OrdenCobro, Pagos, Expediente, etc. |
| Catálogos globales no mezclan datos tenant | ✅ | Plan no tiene datos de pacientes/citas; solo se asocia a Clinica. |
| Datos "semi-globales" (ej. tratamientos) por tenant o con dueño | ✅ | Tratamientos son por clínica (`Tratamiento.ClinicaId`). |

---

## H) Migraciones y esquema

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Migraciones sin romper tenants existentes | ✅ | Uso estándar de EF Core migrations. |
| Datos semilla por tenant (si aplica) | ⚠️ | SeedData revisado para no mezclar tenants; creación de clínica/usuarios según flujo. |
| Estrategia Dev → QA → Prod | ⚠️ | Criterio de despliegue; no revisado en código. |
| Backups y restore | ⚠️ | Operacional; no en código. |
| Soft delete en entidades sensibles | ⚠️ Parcial | Paciente tiene `Activo`; Clinica no tiene soft delete (no hay `FechaEliminado`). |

---

## I) Reportes y exportaciones

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Reportes siempre filtrados por TenantId | ✅ | `ReportesController`: `cid = ClinicaId`; `BuildReporteFinancieroDataAsync(cid.Value, ...)`. |
| Exportar Excel/PDF sin mezclar tenants | ✅ | Los datos del reporte se construyen con `cid`; el export recibe `ReporteFinancieroData` ya filtrado. |
| Totales "globales" solo SaaSAdmin | ✅ | Panel Saas es solo SuperAdmin; dashboard clínica solo datos de su `cid`. |
| Dashboard tenant-scoped | ✅ | `HomeController.Index` (Clinica): citas y órdenes con `ClinicaId == cid`. |

---

## J) Archivos y fotos

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Archivos segregados por tenant | N/A | No hay upload de archivos/fotos en el proyecto actual. Si se añade, usar ruta/contendor con `ClinicaId`. |
| Tenant no accede a archivos de otro por URL | N/A | Idem. |
| Links temporales ligados al tenant | N/A | Idem. |

---

## K) Pruebas de caja negra

| Prueba | Cómo verificarlo |
|--------|-------------------|
| Usuario clínica A intenta acceso a registro de B por URL | Llamar a `/Clinica/Pacientes/Edit/999` con usuario de otra clínica; el 999 debe ser de la clínica actual para verlo; si no → NotFound. |
| Crear registro enviando ClinicaId de otro tenant | En panel Clínica no se envía ClinicaId en formularios de creación; se usa `cid` del servidor. En Saas, solo SuperAdmin puede crear usuarios y elige la clínica (por diseño). |
| Exportar reporte con dos tenants | El export se genera con `BuildReporteFinancieroDataAsync(cid.Value, ...)`; solo hay un `cid` por request (usuario actual). |
| Usuario sin permiso entra a ruta de admin | Rutas protegidas con `[Authorize(Roles = ...)]`; filtro de vistas bloquea acceso y redirige a VistaNoPermitida. |

---

## L) Multi-clínica por subdominio

| Requisito | Estado | Notas |
|-----------|--------|-------|
| clinica1.tuapp.com → Tenant correcto | ❌ No implementado | No hay resolución por subdominio en el código. |
| No cambiar de tenant solo cambiando URL estando logueado | ✅ | El tenant viene del usuario (`ClinicaId`); no de la URL. |

**Conclusión L:** Si en el futuro se requiere subdominio, habría que añadir middleware/host filter que resuelva `ClinicaId` desde el host y valide que coincida con el usuario.

---

## Resultado final (criterios "sí es multitenant")

| Criterio | Estado |
|----------|--------|
| TenantId (ClinicaId) obligatorio en todo dato de negocio | ✅ |
| El sistema asigna TenantId (no el cliente) en panel Clínica | ✅ |
| No existe consulta/acción sin filtro por tenant en área Clinica | ✅ |
| Roles por tenant + SuperAdmin separado | ✅ |
| Plan/suscripción controla acceso real | ✅ |

**Veredicto:** La implementación cumple los criterios multitenant SaaS. Los puntos marcados como ⚠️ son mejoras opcionales (estados más finos en Clinica, soft delete, Slug, subdominio) o aspectos operativos (backups, estrategia de despliegue).

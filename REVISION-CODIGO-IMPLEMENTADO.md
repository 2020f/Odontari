# Revisión de código – Módulos implementados

Revisión estática del código añadido o modificado. Fecha: 2025.

---

## 1. Módulo SubirArchivos

### 1.1 Modelo y datos
| Revisión | Estado |
|----------|--------|
| `ArchivoSubido` tiene ClinicaId, PacienteId y todos los campos acordados (Container, BlobName, ContentType, SizeBytes, CreatedAt, FileNameOriginal, Extension, Estado, Url, UsuarioId) | OK |
| Relaciones en `ApplicationDbContext`: FK a Clinica, Paciente, Usuario; DeleteBehavior correcto | OK |
| Navegación en `Paciente` y `Clinica` (ArchivosSubidos) | OK |

### 1.2 Servicio Blob
| Revisión | Estado |
|----------|--------|
| `IBlobUploadService` / `BlobUploadService` leen `AzureBlob:ConnectionString` y `AzureBlob:Container` | OK |
| `CreateIfNotExistsAsync` con firma correcta (metadata: null, encryptionScopeOptions: null) | OK |
| Límite 10 MB en servicio y en controlador (MaxFileBytes) | OK |
| Registro en `Program.cs` (Scoped) | OK |

### 1.3 Controlador SubirArchivos
| Revisión | Estado |
|----------|--------|
| Index: filtro por ClinicaId y PacienteId; ViewBag.PacienteIdExpediente y SeccionActivaExpediente para el menú | OK |
| Subir: validación vacío, content-type (imagen/PDF), tamaño; rollback de blob si falla SaveChanges | OK |
| Ver / Descargar: filtro por ClinicaId y Estado "Activo"; stream desde Blob | OK |
| Eliminar: verificación ClinicaId y PacienteId; borrado de blob y registro | OK |
| Formulario: nombre del input `archivos` coincide con parámetro `IFormFileCollection? archivos` | OK |

### 1.4 Vista y menú
| Revisión | Estado |
|----------|--------|
| SubirArchivos/Index usa `_ExpedienteMenu` y ViewBag correctos | OK |
| Pestaña "Archivos" en `_ExpedienteMenu` con asp-controller="SubirArchivos", asp-route-id y citaId | OK |
| Todas las pestañas del menú tienen asp-controller="Expediente" o "SubirArchivos" (navegación correcta desde cualquier vista) | OK |

### 1.5 Configuración
| Revisión | Estado |
|----------|--------|
| appsettings.json tiene sección AzureBlob (ConnectionString vacío, Container "odontari-archivos") | OK |

---

## 2. Odontograma – Sincronización a procedimientos

### 2.1 Backend
| Revisión | Estado |
|----------|--------|
| `GuardarOdontograma`: solo sincroniza si `request.CitaId.HasValue && request.CitaId.Value > 0` | OK |
| `GetListaHallazgosFromEstadoJson` (odontograma) no fue modificado; usa "teeth" y GetString para superficies | OK |
| Creación de Tratamiento si no existe; ProcedimientoRealizado con Notas (notasKey) para evitar duplicados | OK |

### 2.2 Scripts – Atención (pestaña en cita)
| Revisión | Estado |
|----------|--------|
| odontograma-atencion.js: lee `citaIdAt`, evita NaN; envía `CitaId: citaId > 0 ? citaId : null`; reload si r.ok && citaId | OK |
| odontograma-atencion-infantil.js: misma lógica; TipoOdontograma desde input | OK |
| Vista Atencion/Expediente: inputs `pacienteIdAt`, `citaIdAt` dentro del tab Odontograma | OK |

### 2.3 Scripts – Expediente (página Odontograma)
| Revisión | Estado |
|----------|--------|
| odontograma.js: lee opcional `citaId`; envía CitaId en payload; reload si hay citaId y éxito | OK |
| odontograma-infantil.js: mismo patrón con CitaId | OK |
| Vista Expediente/Odontograma: ViewBag.CitaId; hidden `id="citaId"` solo si citaId.HasValue | OK |

---

## 3. Periodontograma – Historial (histograma)

### 3.1 Backend
| Revisión | Estado |
|----------|--------|
| `GetIntFromJsonElement(JsonElement v)`: acepta ValueKind Number (TryGetInt32) y String (GetString + TryParse) | OK |
| `GetListaHallazgosFromPeriodontogramaJson`: sondaje y margen usan GetIntFromJsonElement (compatible con cliente que envía números) | OK |
| `GetPeriodontogramaResumenDescripcion` (CountArcade): conteo de bolsas usa GetIntFromJsonElement | OK |
| GuardarPeriodontograma: añade HistorialClinico con TipoEvento "Actualización periodontograma" y descripción completa | OK |

### 3.2 Frontend
| Revisión | Estado |
|----------|--------|
| periodontograma.js: PacienteId validado (no NaN, > 0); CitaId opcional con mismo criterio que odontograma | OK |
| Vista Periodontograma: pacienteIdPeriodonto, citaIdPeriodonto (solo si hay cita) | OK |

---

## 4. Posibles mejoras (no bloqueantes)

| Tema | Sugerencia |
|------|------------|
| SubirArchivos Ver/Descargar | El stream devuelto por Azure no siempre es disposable por FileResult; en escenarios de mucho tráfico, valorar envolver en `using` o asegurar cierre cuando termine la respuesta. |
| Mensaje de error en Subir | Línea 119: `$"{file.FileName}: error al subir. {ex.Message}"` está correcta (interpolación). |
| Reload desde Expediente/Odontograma | Si se guarda con citaId desde la página Odontograma (no desde la pestaña Atención), el reload recarga la misma página Odontograma; los procedimientos se ven al ir a Atención. Comportamiento coherente con el diseño. |

---

## 5. Resumen

- **SubirArchivos**: modelo, servicio Blob, controlador, vista y menú coherentes; multitenant (ClinicaId) y por paciente (PacienteId); configuración en appsettings.
- **Menú expediente**: controladores fijados en todas las pestañas; pestaña activa y enlaces correctos desde Archivos y desde el resto.
- **Odontograma**: envío de CitaId sin NaN en los cuatro scripts; backend sin cambios en la lógica de hallazgos; sincronización a procedimientos cuando hay CitaId válido.
- **Periodontograma**: lectura de valores numéricos (sondaje, margen) con GetIntFromJsonElement; registro en historial con descripción completa; envío de PacienteId/CitaId validado en el script.

**Conclusión:** El código revisado es coherente con lo acordado. No se detectan errores que impidan el funcionamiento; las mejoras indicadas son opcionales.

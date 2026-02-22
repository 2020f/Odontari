# Revisión tipo test – Módulos y correcciones implementados

Use esta lista para validar que todo lo nuevo funciona correctamente. Marque cada ítem al probar.

---

## 1. Módulo SubirArchivos (archivos y documentos)

### 1.1 Acceso y navegación
- [ ] Desde el expediente del paciente, la pestaña **Archivos** aparece en el menú (junto a Resumen, Histograma, Odontograma, etc.).
- [ ] Al hacer clic en **Archivos** se abre la vista "Archivos y documentos" del mismo paciente.
- [ ] La pestaña **Archivos** se muestra como activa cuando se está en esa vista.

### 1.2 Subida de archivos
- [ ] El formulario permite elegir **varios archivos** a la vez (input múltiple).
- [ ] Se aceptan **imágenes** (JPEG, PNG, GIF, WebP, BMP) y **PDF**.
- [ ] Al subir uno o más archivos válidos aparece mensaje de éxito (ej. "Se subieron X archivo(s) correctamente").
- [ ] Si se sube un tipo no permitido (ej. .docx), se muestra error y no se guarda.
- [ ] Archivos mayores a 10 MB son rechazados con mensaje de error.

### 1.3 Listado y acciones
- [ ] Los archivos subidos se listan en "Archivos disponibles" (nombre, tipo, tamaño, fecha).
- [ ] **Ver** abre el archivo en una nueva pestaña (imagen o PDF).
- [ ] **Descargar** descarga el archivo con el nombre original.
- [ ] **Eliminar** pide confirmación y, al confirmar, quita el archivo de la lista (y del storage).

### 1.4 Multitenant y por paciente
- [ ] Los archivos solo se ven para el paciente y la clínica actual (no se mezclan datos de otras clínicas).
- [ ] Al cambiar de paciente en el expediente, la pestaña Archivos muestra solo los archivos de ese paciente.

### 1.5 Configuración Blob (si aplica)
- [ ] Con **AzureBlob:ConnectionString** y **AzureBlob:Container** configurados en appsettings (o User Secrets), la subida y la descarga funcionan sin error.

---

## 2. Menú de navegación del expediente

### 2.1 Desde la vista Archivos
- [ ] Estando en "Archivos y documentos", al hacer clic en **Resumen** se va al Resumen del expediente (no queda en Archivos).
- [ ] **Histograma** lleva al Histograma del paciente.
- [ ] **Historial** lleva al Historial clínico.
- [ ] **Odontograma** lleva al Odontograma.
- [ ] **Periodontograma** lleva al Periodontograma.
- [ ] **Historia Clínica Sistemática** lleva a esa sección.
- [ ] La pestaña correspondiente a la vista actual se muestra como **activa** (resaltada).

### 2.2 Desde otras vistas del expediente
- [ ] Desde Resumen, Histograma u Odontograma, todas las pestañas llevan a la sección correcta y la pestaña activa se ve bien.

---

## 3. Odontograma – Sincronización con procedimientos (cobro)

### 3.1 Desde Atención (pestaña Odontograma en la cita)
- [ ] Se abre una cita en **Atención** (Expediente de la cita).
- [ ] Se va a la pestaña **Odontograma**.
- [ ] Se marcan hallazgos (caries, obturación, etc.) en uno o más dientes.
- [ ] Se hace clic en **Guardar odontograma**.
- [ ] Aparece "Guardado." y la página recarga.
- [ ] En la pestaña **Procedimientos** aparecen líneas nuevas con los hallazgos (tratamientos) listos para definir precio y cobrar.
- [ ] Si un hallazgo ya tenía tratamiento con precio en la clínica, el procedimiento se agrega con ese precio.

### 3.2 Odontograma infantil desde Atención
- [ ] Con un paciente menor de 14 años, en Atención se usa el odontograma infantil.
- [ ] Al guardar con hallazgos, los procedimientos se sincronizan igual que en adulto.

### 3.3 Desde Expediente (página Odontograma con cita)
- [ ] Se entra al expediente del paciente **desde un contexto con cita** (ej. enlace con citaId).
- [ ] Se abre la página **Odontograma** (desde el menú del expediente).
- [ ] Se guardan hallazgos: los procedimientos se sincronizan a la cita y, si corresponde, al volver a Procedimientos se ven.

### 3.4 Sin cita (solo expediente)
- [ ] Se abre el Odontograma desde el expediente **sin** cita en la URL.
- [ ] Al guardar, el odontograma se guarda correctamente y **no** se intenta sincronizar a procedimientos (comportamiento esperado).

---

## 4. Periodontograma – Historial (histograma del paciente)

### 4.1 Guardado y registro en historial
- [ ] Se abre el **Periodontograma** de un paciente (desde expediente o desde Atención).
- [ ] Se completan datos: sangrado, placa, profundidad de sondaje (ej. ≥4 mm), recesiones, etc.
- [ ] Se hace clic en **Guardar**.
- [ ] Aparece "Guardado.".

### 4.2 Hallazgos en el histograma
- [ ] Se abre el **Histograma** del mismo paciente (timeline / historial clínico).
- [ ] Aparece un evento **"Actualización periodontograma"** con fecha reciente.
- [ ] La **descripción** del evento incluye métricas (sitios con sangrado, placa, bolsas ≥4 mm, ≥6 mm, ausencias, implantes).
- [ ] Si hay hallazgos por diente (sondaje, recesión, sangrado, etc.), aparecen listados en la descripción (ej. "Diente 11 (V): Prof. sondaje M:4mm", "Diente 22: Sangrado M,C", etc.).

### 4.3 Sin datos
- [ ] Si se guarda el periodontograma sin datos relevantes, igual se crea el evento en el historial con texto tipo "Periodontograma actualizado." y métricas en 0.

---

## 5. Resumen de componentes tocados

| Componente | Qué se hizo |
|------------|-------------|
| **Modelo** | `ArchivoSubido` (ClinicaId, PacienteId, Container, BlobName, ContentType, SizeBytes, CreatedAt, FileNameOriginal, Extension, Estado, Url, UsuarioId). |
| **Servicios** | `IBlobUploadService` / `BlobUploadService` (Azure Blob: subir, eliminar, obtener stream). |
| **Controlador** | `SubirArchivosController` (Index, Subir, Ver, Descargar, Eliminar). Filtro por ClinicaId y PacienteId. |
| **Vistas** | `SubirArchivos/Index.cshtml`, pestaña "Archivos" en `_ExpedienteMenu.cshtml`. |
| **Menú** | Enlaces del menú expediente con `asp-controller="Expediente"` (o `SubirArchivos`) para que funcionen desde cualquier vista. |
| **Odontograma** | Envío correcto de `CitaId` en odontograma-atencion.js, odontograma-atencion-infantil.js, odontograma.js, odontograma-infantil.js. CitaId opcional en vista Expediente/Odontograma. |
| **Periodontograma** | Helper `GetIntFromJsonElement` en backend; lectura de valores numéricos (sondaje, margen) desde JSON; envío correcto de PacienteId/CitaId en periodontograma.js. |

---

## 6. Criterios de éxito global

- [ ] **SubirArchivos**: subir, listar, ver, descargar y eliminar archivos por paciente, sin mezclar clínicas.
- [ ] **Menú**: desde Archivos (y desde cualquier tab) todas las pestañas llevan a la sección correcta y la activa se ve bien.
- [ ] **Odontograma**: al guardar desde Atención (o desde Expediente con cita), los hallazgos se reflejan en Procedimientos para cobro.
- [ ] **Periodontograma**: al guardar, el evento y los hallazgos aparecen en el histograma del paciente con descripción completa.

Si algún ítem falla, anote: **Módulo**, **número de sección/ítem** y **qué ocurrió** (mensaje de error, pantalla en blanco, dato que no aparece, etc.) para depurar.

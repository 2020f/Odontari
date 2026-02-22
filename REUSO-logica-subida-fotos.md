# Lógica reutilizable: Subida de imagen a Azure Blob + registro en SQL

Este documento describe el **patrón / componente** usado en Minifotos para que puedas reutilizarlo en otro proyecto y referirte a él de forma clara ("la lógica de Minifotos", "Blob + SQL", "subida de fotos con registro en BD").

## Qué hace (resumen)

1. El usuario sube un archivo de imagen (formulario).
2. La app valida: archivo no vacío, solo imágenes (content-type), tamaño máximo (ej. 5 MB).
3. Se genera un **BlobName único** (ej. `yyyy/MM/{guid}.jpg`) y se **sube el archivo a un container de Azure Blob Storage**.
4. Se **inserta un registro en una tabla SQL** con: Container, BlobName, ContentType, SizeBytes, CreatedAt, FileNameOriginal, Extension, Estado, Url (y opcionalmente IDs de tablas relacionadas: UsuarioId, ProductoId, etc.).
5. Si el INSERT en SQL falla, se **borra el blob** subido (consistencia).
6. Opcional: listar registros, **ver** imagen (stream desde Blob) y **descargar** con nombre original (funciona con container privado).

## Dónde está en este proyecto

| Concepto | Ubicación |
|--------|------------|
| Configuración | `appsettings`: `ConnectionStrings:DefaultConnection`, `AzureBlob:ConnectionString`, `AzureBlob:Container` |
| Servicio Blob | `Services/IBlobUploadService.cs`, `Services/BlobUploadService.cs` (subir, eliminar, obtener stream) |
| Entidad y BD | `Models/Foto.cs`, `Data/MinifotosDbContext.cs`, tabla `Imagenes` |
| Flujo subida + validaciones | `Controllers/SubirFotoController.cs` (POST Index: validar → subir blob → insert SQL → rollback blob si falla) |
| Ver / Descargar | Mismo controller: `Ver(id)`, `Descargar(id)` usando `GetStreamAsync(blobName)` |
| Vistas | `Views/SubirFoto/Index.cshtml` (formulario), `Views/SubirFoto/Listar.cshtml` (lista con Ver/Descargar) |

## Cómo reutilizarlo en otro proyecto

- **Misma lógica, otra BD y otro container:**  
  Copia o referencia este flujo. En el otro proyecto:
  - Otra **tabla** (ej. `FotosPerfil`, `ImagenesProducto`) con los mismos campos básicos (Container, BlobName, ContentType, SizeBytes, CreatedAt, FileNameOriginal, Extension, Estado, Url) **más** columnas de relación (ej. `UsuarioId`, `ProductoId`).
  - Otra **entidad** y **DbContext** mapeados a esa tabla.
  - **Mismo servicio de Blob** (o uno igual leyendo otro `AzureBlob:Container` / otra key de config).
  - En el controller: después de subir el blob y antes de `SaveChanges`, asigna las **relaciones** (ej. `foto.UsuarioId = usuarioId`) según la información que tengas en ese proyecto.

- **Container distinto por contexto:**  
  En config (o en la petición) usa otro container: ej. `AzureBlob:Container` = `fotos-perfil` o `imagenes-productos`. El mismo `BlobUploadService` puede leer el container de configuración; si necesitas varios containers, puedes tener varias keys (`AzureBlob:ContainerPerfil`, `AzureBlob:ContainerProductos`) o pasar el nombre del container al servicio.

- **Resumen:**  
  La lógica “subir imagen a Blob + guardar registro en SQL (y ver/descargar)” es la misma; lo que cambia es: **qué tabla/entidad usas**, **qué relaciones rellenar** (UsuarioId, ProductoId, etc.) y **qué container usar**.

## Cómo referirse a esto en el futuro

- En este repo: "según **REUSO-logica-subida-fotos.md**".
- En otro proyecto: "usa la **lógica de Minifotos**" / "el **patrón Blob + SQL** de subida de fotos" / "como en **Minifotos**: subir a Blob, insert en BD, ver y descargar por stream".

Si en otro proyecto dices: *"Quiero subir imágenes con la misma lógica que Minifotos, pero la tabla tiene ProductoId y uso el container X"*, con este doc y el código de este proyecto se puede replicar el flujo adaptando entidad, DbContext y config.

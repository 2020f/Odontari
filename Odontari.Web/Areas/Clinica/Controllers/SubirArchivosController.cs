using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Areas.Clinica.Controllers;

/// <summary>
/// Módulo SubirArchivos: desde el historial clínico / expediente, el doctor puede subir imágenes y PDFs.
/// Archivos se guardan en Azure Blob Storage; en BD solo se guarda la referencia (Container, BlobName, Url, etc.).
/// Multitenant: siempre filtrar por ClinicaId. Asociado a PacienteId.
/// </summary>
[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Doctor)]
[Area("Clinica")]
public class SubirArchivosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;
    private readonly IBlobUploadService _blobUpload;

    public SubirArchivosController(ApplicationDbContext db, IClinicaActualService clinicaActual, IBlobUploadService blobUpload)
    {
        _db = db;
        _clinicaActual = clinicaActual;
        _blobUpload = blobUpload;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static readonly string[] ContentTypesImagen = { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" };
    private static readonly string ContentTypePdf = "application/pdf";
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>Lista de archivos del paciente + formulario de subida. Entrada desde expediente (id = pacienteId).</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int id, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var paciente = await _db.Pacientes
            .Include(p => p.Clinica)
            .FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var archivos = await _db.ArchivosSubidos
            .Where(a => a.ClinicaId == cid && a.PacienteId == id && a.Estado == "Activo")
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ArchivoSubidoItemViewModel
            {
                Id = a.Id,
                FileNameOriginal = a.FileNameOriginal,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                CreatedAt = a.CreatedAt,
                Extension = a.Extension
            })
            .ToListAsync();

        ViewBag.Paciente = paciente;
        ViewBag.PacienteId = id;
        ViewBag.PacienteIdExpediente = id;
        ViewBag.SeccionActivaExpediente = "archivos";
        ViewBag.CitaId = citaId;
        ViewBag.Archivos = archivos;
        return View();
    }

    /// <summary>Subir uno o más archivos (imagen o PDF). Validación: no vacío, content-type permitido, tamaño máx 10 MB.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subir(int pacienteId, IFormFileCollection? archivos, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == pacienteId);
        if (paciente == null) return NotFound();

        if (archivos == null || archivos.Count == 0)
        {
            TempData["ErrorArchivo"] = "Seleccione al menos un archivo (imagen o PDF).";
            return RedirectToAction(nameof(Index), new { id = pacienteId, citaId });
        }

        var containerName = _blobUpload.ContainerName;
        var subidos = 0;
        var errores = new List<string>();

        foreach (var file in archivos)
        {
            if (file.Length == 0) { errores.Add($"{file.FileName}: archivo vacío."); continue; }
            if (file.Length > MaxFileBytes) { errores.Add($"{file.FileName}: supera el tamaño máximo (10 MB)."); continue; }

            var contentType = file.ContentType ?? "application/octet-stream";
            var esImagen = ContentTypesImagen.Contains(contentType, StringComparer.OrdinalIgnoreCase);
            var esPdf = string.Equals(contentType, ContentTypePdf, StringComparison.OrdinalIgnoreCase);
            if (!esImagen && !esPdf)
            {
                errores.Add($"{file.FileName}: solo se permiten imágenes (JPEG, PNG, GIF, WebP, BMP) o PDF.");
                continue;
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension)) extension = esPdf ? ".pdf" : ".jpg";

            string blobName;
            try
            {
                await using var stream = file.OpenReadStream();
                blobName = await _blobUpload.UploadAsync(stream, contentType, extension);
            }
            catch (Exception ex)
            {
                errores.Add($"{file.FileName}: error al subir. {ex.Message}");
                continue;
            }

            var registro = new ArchivoSubido
            {
                ClinicaId = cid.Value,
                PacienteId = pacienteId,
                Container = containerName,
                BlobName = blobName,
                ContentType = contentType,
                SizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow,
                FileNameOriginal = file.FileName,
                Extension = extension,
                Estado = "Activo",
                Url = null,
                UsuarioId = UserId
            };

            _db.ArchivosSubidos.Add(registro);
            try
            {
                await _db.SaveChangesAsync();
                subidos++;
            }
            catch
            {
                try { await _blobUpload.DeleteAsync(blobName); } catch { /* rollback blob */ }
                errores.Add($"{file.FileName}: error al guardar el registro.");
            }
        }

        if (subidos > 0) TempData["MensajeArchivo"] = $"Se subieron {subidos} archivo(s) correctamente.";
        if (errores.Count > 0) TempData["ErrorArchivo"] = string.Join(" ", errores);
        return RedirectToAction(nameof(Index), new { id = pacienteId, citaId });
    }

    /// <summary>Ver archivo en el navegador (stream desde Blob).</summary>
    [HttpGet]
    public async Task<IActionResult> Ver(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return NotFound();

        var archivo = await _db.ArchivosSubidos
            .FirstOrDefaultAsync(a => a.Id == id && a.ClinicaId == cid && a.Estado == "Activo");
        if (archivo == null) return NotFound();

        var stream = await _blobUpload.GetStreamAsync(archivo.BlobName);
        if (stream == null) return NotFound();

        return File(stream, archivo.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Descargar archivo con nombre original.</summary>
    [HttpGet]
    public async Task<IActionResult> Descargar(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return NotFound();

        var archivo = await _db.ArchivosSubidos
            .FirstOrDefaultAsync(a => a.Id == id && a.ClinicaId == cid && a.Estado == "Activo");
        if (archivo == null) return NotFound();

        var stream = await _blobUpload.GetStreamAsync(archivo.BlobName);
        if (stream == null) return NotFound();

        var fileName = string.IsNullOrWhiteSpace(archivo.FileNameOriginal) ? "archivo" + archivo.Extension : archivo.FileNameOriginal;
        return File(stream, archivo.ContentType, fileName);
    }

    /// <summary>Eliminar registro y blob (soft: marcar Estado = Eliminado o borrar registro + blob).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, int pacienteId, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return NotFound();

        var archivo = await _db.ArchivosSubidos
            .FirstOrDefaultAsync(a => a.Id == id && a.ClinicaId == cid && a.PacienteId == pacienteId);
        if (archivo == null) return NotFound();

        try
        {
            await _blobUpload.DeleteAsync(archivo.BlobName);
        }
        catch { /* continuar para borrar registro */ }

        _db.ArchivosSubidos.Remove(archivo);
        await _db.SaveChangesAsync();
        TempData["MensajeArchivo"] = "Archivo eliminado.";
        return RedirectToAction(nameof(Index), new { id = pacienteId, citaId });
    }
}

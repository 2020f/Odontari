using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;
using System.Security.Claims;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Area("Clinica")]
[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Doctor)]
public class ConsentimientoController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public ConsentimientoController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<bool> PlanPermiteConsentimientoAsync(int clinicaId)
    {
        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == clinicaId);
        return clinica?.Plan?.PermiteConsentimiento ?? false;
    }

    // ─────────────────────────────────────────────────────────────────
    //  ADMIN: Gestión de plantillas + lista de documentos firmados
    // ─────────────────────────────────────────────────────────────────

    [Authorize(Roles = OdontariRoles.AdminClinica)]
    public async Task<IActionResult> Index(string? buscar, string? tab)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (!await PlanPermiteConsentimientoAsync(cid.Value))
            return RedirectToAction("VistaNoPermitida", "Home", new { area = "Clinica", vista = "Consentimiento" });

        var plantillas = await _db.PlantillasConsentimiento
            .Where(p => p.ClinicaId == cid)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new PlantillaConsentimientoListViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Titulo = p.Titulo,
                EsObligatorio = p.EsObligatorio,
                Activo = p.Activo,
                Version = p.Version,
                FechaCreacion = p.FechaCreacion,
                TotalDocumentos = p.Consentimientos.Count
            })
            .ToListAsync();

        var query = _db.ConsentimientosGenerados
            .Where(c => c.ClinicaId == cid)
            .AsQueryable();

        var buscarTrim = buscar?.Trim();
        if (!string.IsNullOrEmpty(buscarTrim))
        {
            query = query.Where(c =>
                c.Paciente.Nombre.Contains(buscarTrim) ||
                (c.Paciente.Apellidos != null && c.Paciente.Apellidos.Contains(buscarTrim)) ||
                (c.Doctor != null && c.Doctor.NombreCompleto != null && c.Doctor.NombreCompleto.Contains(buscarTrim)) ||
                (c.Doctor != null && c.Doctor.Email != null && c.Doctor.Email.Contains(buscarTrim)));
        }

        var documentos = await query
            .Include(c => c.Plantilla)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor)
            .OrderByDescending(c => c.FechaGeneracion)
            .Take(200)
            .Select(c => new ConsentimientoGeneradoListViewModel
            {
                Id = c.Id,
                PlantillaNombre = c.Plantilla.Nombre,
                PacienteNombre = c.Paciente.Nombre + (c.Paciente.Apellidos != null ? " " + c.Paciente.Apellidos : ""),
                DoctorNombre = c.Doctor != null ? c.Doctor.NombreCompleto ?? c.Doctor.Email! : "—",
                NombreFirmante = c.NombreFirmante,
                Estado = c.Estado,
                FechaGeneracion = c.FechaGeneracion,
                FechaFirma = c.FechaFirma,
                CitaId = c.CitaId
            })
            .ToListAsync();

        ViewBag.Documentos = documentos;
        ViewBag.Buscar = buscarTrim ?? "";
        ViewBag.TabActiva = (!string.IsNullOrEmpty(buscarTrim) || tab == "documentos") ? "documentos" : "plantillas";
        return View(plantillas);
    }

    [Authorize(Roles = OdontariRoles.AdminClinica)]
    public IActionResult CrearPlantilla()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        return View(new PlantillaConsentimientoEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OdontariRoles.AdminClinica)]
    public async Task<IActionResult> CrearPlantilla(PlantillaConsentimientoEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (!ModelState.IsValid) return View(vm);

        _db.PlantillasConsentimiento.Add(new PlantillaConsentimiento
        {
            ClinicaId = cid.Value,
            Nombre = vm.Nombre,
            Titulo = vm.Titulo,
            TextoBase = vm.TextoBase,
            EsObligatorio = vm.EsObligatorio,
            Activo = vm.Activo,
            Version = 1,
            FechaCreacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Exito"] = "Plantilla creada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = OdontariRoles.AdminClinica)]
    public async Task<IActionResult> EditarPlantilla(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var p = await _db.PlantillasConsentimiento.FirstOrDefaultAsync(x => x.ClinicaId == cid && x.Id == id);
        if (p == null) return NotFound();

        return View(new PlantillaConsentimientoEditViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Titulo = p.Titulo,
            TextoBase = p.TextoBase,
            EsObligatorio = p.EsObligatorio,
            Activo = p.Activo,
            Version = p.Version
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OdontariRoles.AdminClinica)]
    public async Task<IActionResult> EditarPlantilla(PlantillaConsentimientoEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (!ModelState.IsValid) return View(vm);

        var p = await _db.PlantillasConsentimiento.FirstOrDefaultAsync(x => x.ClinicaId == cid && x.Id == vm.Id);
        if (p == null) return NotFound();

        // Incrementar versión solo si el texto base cambió
        if (p.TextoBase != vm.TextoBase)
            p.Version++;

        p.Nombre = vm.Nombre;
        p.Titulo = vm.Titulo;
        p.TextoBase = vm.TextoBase;
        p.EsObligatorio = vm.EsObligatorio;
        p.Activo = vm.Activo;
        p.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Exito"] = "Plantilla actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = OdontariRoles.AdminClinica)]
    public async Task<IActionResult> TogglePlantilla(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var p = await _db.PlantillasConsentimiento.FirstOrDefaultAsync(x => x.ClinicaId == cid && x.Id == id);
        if (p == null) return NotFound();

        p.Activo = !p.Activo;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────────────────────────
    //  DOCTOR: Generar documento desde expediente de cita
    // ─────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(GenerarConsentimientoViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var uid = UserId;
        if (uid == null) return Unauthorized();
        if (!await PlanPermiteConsentimientoAsync(cid.Value))
            return RedirectToAction("VistaNoPermitida", "Home", new { area = "Clinica", vista = "Consentimiento" });

        var cita = await _db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Doctor)
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == vm.CitaId);
        if (cita == null) return NotFound();

        var plantilla = await _db.PlantillasConsentimiento
            .FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == vm.PlantillaId && p.Activo);
        if (plantilla == null) return NotFound();

        var clinica = await _db.Clinicas.FindAsync(cid);
        var doctor = await _db.Users.FindAsync(uid);

        // Calcular edad del paciente
        string edad = "—";
        if (cita.Paciente?.FechaNacimiento is { } fn)
        {
            int anos = (int)((DateTime.Today - fn.Date).TotalDays / 365.25);
            edad = anos.ToString();
        }

        // Interpolación de marcadores
        var textoFinal = plantilla.TextoBase
            .Replace("{NOMBRE_PACIENTE}", $"{cita.Paciente?.Nombre} {cita.Paciente?.Apellidos}".Trim())
            .Replace("{CEDULA}", cita.Paciente?.Cedula ?? "—")
            .Replace("{EDAD}", edad)
            .Replace("{DOCTOR}", doctor?.NombreCompleto ?? doctor?.Email ?? "—")
            .Replace("{FECHA}", DateTime.Now.ToString("dd/MM/yyyy"))
            .Replace("{HORA}", DateTime.Now.ToString("HH:mm"))
            .Replace("{CLINICA}", clinica?.Nombre ?? "—");

        var doc = new ConsentimientoGenerado
        {
            ClinicaId = cid.Value,
            PlantillaId = vm.PlantillaId,
            PacienteId = cita.PacienteId,
            CitaId = vm.CitaId,
            ProcedimientoRealizadoId = vm.ProcedimientoRealizadoId,
            DoctorId = uid,
            TextoFinal = textoFinal,
            VersionPlantilla = plantilla.Version,
            Estado = EstadoConsentimiento.Pendiente,
            FechaGeneracion = DateTime.UtcNow
        };
        _db.ConsentimientosGenerados.Add(doc);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Firmar), new { id = doc.Id });
    }

    // ─────────────────────────────────────────────────────────────────
    //  DOCTOR: Página de firma
    // ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Firmar(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var doc = await _db.ConsentimientosGenerados
            .Include(c => c.Paciente)
            .Include(c => c.Plantilla)
            .Include(c => c.Doctor)
            .Include(c => c.Clinica)
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (doc == null) return NotFound();

        var vm = new FirmarConsentimientoViewModel
        {
            ConsentimientoId = doc.Id,
            CitaId = doc.CitaId,
            ClinicaNombre = doc.Clinica.Nombre,
            ClinicaLogo = doc.Clinica.LogoUrl,
            TituloDocumento = doc.Plantilla.Titulo,
            TextoFinal = doc.TextoFinal,
            PacienteNombre = $"{doc.Paciente.Nombre} {doc.Paciente.Apellidos}".Trim(),
            DoctorNombre = doc.Doctor?.NombreCompleto ?? doc.Doctor?.Email ?? "—",
            FechaGeneracion = doc.FechaGeneracion.ToString("dd/MM/yyyy HH:mm"),
            Estado = doc.Estado
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarFirma(int consentimientoId, string firmaBase64, string nombreFirmante)
    {
        var cid = ClinicaId;
        if (cid == null) return Json(new { ok = false, msg = "Sin clínica" });

        var doc = await _db.ConsentimientosGenerados
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == consentimientoId);
        if (doc == null) return Json(new { ok = false, msg = "No encontrado" });

        if (doc.Estado == EstadoConsentimiento.Firmado)
            return Json(new { ok = false, msg = "Ya firmado" });

        if (string.IsNullOrWhiteSpace(firmaBase64) || firmaBase64.Length < 100)
            return Json(new { ok = false, msg = "Firma inválida" });

        doc.FirmaDigitalBase64 = firmaBase64;
        doc.NombreFirmante = nombreFirmante?.Trim();
        doc.Estado = EstadoConsentimiento.Firmado;
        doc.FechaFirma = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Json(new { ok = true, redirectUrl = Url.Action(nameof(VerDocumento), new { id = doc.Id }) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();

        var doc = await _db.ConsentimientosGenerados
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (doc == null) return NotFound();

        doc.Estado = EstadoConsentimiento.Rechazado;
        await _db.SaveChangesAsync();

        return RedirectToAction("Expediente", "Atencion", new { id = doc.CitaId });
    }

    // ─────────────────────────────────────────────────────────────────
    //  VER DOCUMENTO FIRMADO
    // ─────────────────────────────────────────────────────────────────

    public async Task<IActionResult> VerDocumento(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var doc = await _db.ConsentimientosGenerados
            .Include(c => c.Paciente)
            .Include(c => c.Plantilla)
            .Include(c => c.Doctor)
            .Include(c => c.Clinica)
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (doc == null) return NotFound();

        return View(doc);
    }
}

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

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Doctor)]
[Area("Clinica")]
public class AtencionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public AtencionController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Mis citas del día (para el doctor actual).</summary>
    public async Task<IActionResult> Index(DateTime? fecha)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var uid = UserId;
        if (uid == null) return Unauthorized();
        var dia = fecha ?? DateTime.Today;
        var inicio = dia.Date;
        var fin = inicio.AddDays(1);
        var citas = await _db.Citas
            .Where(c => c.ClinicaId == cid && c.DoctorId == uid && c.FechaHora >= inicio && c.FechaHora < fin && c.Estado != EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.ProcedimientosRealizados).ThenInclude(pr => pr.Tratamiento)
            .OrderBy(c => c.FechaHora)
            .ToListAsync();
        ViewBag.Fecha = dia;
        return View(citas);
    }

    /// <summary>Expediente de la cita: procedimientos, odontograma e historial clínico.</summary>
    public async Task<IActionResult> Expediente(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var cita = await _db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.ProcedimientosRealizados).ThenInclude(pr => pr.Tratamiento)
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (cita == null) return NotFound();
        ViewBag.Tratamientos = await _db.Tratamientos.Where(t => t.ClinicaId == cid && t.Activo).OrderBy(t => t.Nombre).ToListAsync();

        // Odontograma y Histograma del paciente
        var pacienteId = cita.PacienteId;
        var odontograma = await _db.Odontogramas
            .Where(o => o.PacienteId == pacienteId && o.ClinicaId == cid)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();
        ViewBag.OdontogramaEstadoJson = odontograma?.EstadoJson ?? "{}";
        ViewBag.PacienteId = pacienteId;

        var historial = await _db.HistorialClinico
            .Where(h => h.PacienteId == pacienteId && h.ClinicaId == cid)
            .OrderByDescending(h => h.FechaEvento)
            .Take(30)
            .Select(h => new HistorialEventoViewModel { FechaEvento = h.FechaEvento, TipoEvento = h.TipoEvento, Descripcion = h.Descripcion })
            .ToListAsync();
        ViewBag.Historial = historial;

        return View(cita);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarProcedimiento(int citaId, int tratamientoId)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var cita = await _db.Citas.Include(c => c.ProcedimientosRealizados).FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == citaId);
        var tratamiento = await _db.Tratamientos.FirstOrDefaultAsync(t => t.ClinicaId == cid && t.Id == tratamientoId);
        if (cita == null || tratamiento == null) return NotFound();
        _db.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = citaId,
            TratamientoId = tratamientoId,
            PrecioAplicado = tratamiento.PrecioBase,
            MarcadoRealizado = false
        });
        _db.HistorialClinico.Add(new HistorialClinico { PacienteId = cita.PacienteId, ClinicaId = cid.Value, CitaId = citaId, FechaEvento = DateTime.UtcNow, TipoEvento = "Tratamiento agregado", Descripcion = tratamiento.Nombre, UsuarioId = UserId });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Expediente), new { id = citaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarRealizado(int procedimientoId)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var pr = await _db.ProcedimientosRealizados
            .Include(pr => pr.Cita)
            .Include(pr => pr.Tratamiento)
            .FirstOrDefaultAsync(pr => pr.Cita!.ClinicaId == cid && pr.Id == procedimientoId);
        if (pr == null) return NotFound();
        pr.MarcadoRealizado = true;
        pr.RealizadoAt = DateTime.Now;
        _db.HistorialClinico.Add(new HistorialClinico { PacienteId = pr.Cita!.PacienteId, ClinicaId = pr.Cita.ClinicaId, CitaId = pr.CitaId, FechaEvento = DateTime.UtcNow, TipoEvento = "Procedimiento realizado", Descripcion = pr.Tratamiento?.Nombre ?? "Tratamiento", UsuarioId = UserId });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Expediente), new { id = pr.CitaId });
    }

    /// <summary>Finalizar atención: genera orden de cobro con total de procedimientos realizados.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizarAtencion(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var cita = await _db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.ProcedimientosRealizados).ThenInclude(pr => pr.Tratamiento)
            .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (cita == null) return NotFound();
        cita.Estado = EstadoCita.Finalizada;
        cita.FinAtencionAt = DateTime.Now;
        var total = cita.ProcedimientosRealizados.Where(pr => pr.MarcadoRealizado).Sum(pr => pr.PrecioAplicado);
        if (total > 0)
        {
            _db.OrdenesCobro.Add(new OrdenCobro
            {
                ClinicaId = cid.Value,
                PacienteId = cita.PacienteId,
                CitaId = cita.Id,
                Total = total,
                MontoPagado = 0,
                Estado = EstadoCobro.Pendiente,
                CreadoAt = DateTime.Now
            });
        }
        _db.HistorialClinico.Add(new HistorialClinico { PacienteId = cita.PacienteId, ClinicaId = cid.Value, CitaId = cita.Id, FechaEvento = DateTime.UtcNow, TipoEvento = "Atención finalizada", Descripcion = "Orden de cobro generada", UsuarioId = UserId });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { fecha = cita.FechaHora.Date });
    }
}

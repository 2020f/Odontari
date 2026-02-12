using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
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

    /// <summary>Expediente de la cita: procedimientos realizados y agregar más.</summary>
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
            .FirstOrDefaultAsync(pr => pr.Cita!.ClinicaId == cid && pr.Id == procedimientoId);
        if (pr == null) return NotFound();
        pr.MarcadoRealizado = true;
        pr.RealizadoAt = DateTime.Now;
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
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { fecha = cita.FechaHora.Date });
    }
}

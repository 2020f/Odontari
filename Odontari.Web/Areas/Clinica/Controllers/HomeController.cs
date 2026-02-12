using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Services;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Doctor + "," + OdontariRoles.Finanzas)]
[Area("Clinica")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public HomeController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    public async Task<IActionResult> Index()
    {
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction(nameof(SinClinica));
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        ViewBag.CitasHoy = await _db.Citas
            .Where(c => c.ClinicaId == cid && c.FechaHora >= hoy && c.FechaHora < manana && c.Estado != Models.Enums.EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor)
            .OrderBy(c => c.FechaHora)
            .ToListAsync();
        ViewBag.PendientesCobro = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && o.Estado == Models.Enums.EstadoCobro.Pendiente || o.Estado == Models.Enums.EstadoCobro.Parcial)
            .Include(o => o.Paciente)
            .Take(10)
            .ToListAsync();
        return View();
    }

    public IActionResult SinClinica()
    {
        return View();
    }

    /// <summary>Pantalla de bloqueo cuando clínica inactiva o suscripción vencida.</summary>
    public IActionResult Bloqueo(string? motivo)
    {
        ViewBag.Motivo = motivo ?? "Acceso no permitido.";
        return View();
    }
}

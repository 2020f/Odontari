using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Services;
using VistasClinica = Odontari.Web.Data.VistasClinica;

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
        ViewBag.MostrarAccesoDenegado = string.Equals(Request.Query["accesoDenegado"], "1", StringComparison.Ordinal);
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction(nameof(SinClinica));
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        IQueryable<Models.Cita> queryCitas = _db.Citas
            .Where(c => c.ClinicaId == cid && c.FechaHora >= hoy && c.FechaHora < manana && c.Estado != Models.Enums.EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor);
        if (User.IsInRole(OdontariRoles.Doctor))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                queryCitas =  queryCitas.Where(c => c.DoctorId == userId);
        }
        ViewBag.CitasHoy = await queryCitas.OrderBy(c => c.FechaHora).ToListAsync();
        ViewBag.PendientesCobro = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && (o.Estado == Models.Enums.EstadoCobro.Pendiente || o.Estado == Models.Enums.EstadoCobro.Parcial))
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

    /// <summary>Mensaje cuando el usuario no tiene permiso para una vista (sin salir del panel).</summary>
    public IActionResult VistaNoPermitida(string? vista)
    {
        ViewData["Title"] = "Sin autorización";
        ViewBag.VistaNombre = string.IsNullOrEmpty(vista) ? "esta sección" : VistasClinica.NombrePorClave(vista);
        return View();
    }
}

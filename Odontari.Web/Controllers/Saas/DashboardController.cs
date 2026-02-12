using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;

namespace Odontari.Web.Controllers.Saas;

[Authorize(Roles = OdontariRoles.SuperAdmin)]
[Area("Saas")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private static readonly DateTime Hoy = DateTime.Today;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // Suscripción vigente: Activa, no suspendida, no vencida
        var vigente = await _db.Suscripciones
            .Where(s => s.Activa && !s.Suspendida && s.Vencimiento >= Hoy)
            .Select(s => s.ClinicaId)
            .Distinct()
            .ToListAsync();
        var clinicasActivas = vigente.Count;
        var clinicasVencidas = await _db.Clinicas
            .Where(c => !vigente.Contains(c.Id) && c.Suscripciones.Any(s => s.Vencimiento < Hoy))
            .CountAsync();
        var clinicasSuspendidas = await _db.Suscripciones
            .Where(s => s.Suspendida)
            .Select(s => s.ClinicaId)
            .Distinct()
            .CountAsync();

        // MRR: suma PrecioMensual del plan de cada clínica con suscripción vigente
        var mrr = await _db.Clinicas
            .Where(c => vigente.Contains(c.Id))
            .Include(c => c.Plan)
            .SumAsync(c => c.Plan.PrecioMensual);

        // Renovaciones próximas (próximos 30 días)
        var renovacionesProximas = await _db.Suscripciones
            .Where(s => s.Activa && !s.Suspendida && s.Vencimiento >= Hoy && s.Vencimiento <= Hoy.AddDays(30))
            .Include(s => s.Clinica)
            .OrderBy(s => s.Vencimiento)
            .Take(15)
            .ToListAsync();

        var inicioMes = new DateTime(Hoy.Year, Hoy.Month, 1);
        var nuevasClinicasMes = await _db.Clinicas
            .Where(c => c.FechaCreacion >= inicioMes)
            .Include(c => c.Plan)
            .OrderByDescending(c => c.FechaCreacion)
            .Take(10)
            .ToListAsync();

        var actividadReciente = await _db.AuditLogs
            .OrderByDescending(a => a.CreadoAt)
            .Take(20)
            .ToListAsync();

        ViewBag.ClinicasActivas = clinicasActivas;
        ViewBag.ClinicasVencidas = clinicasVencidas;
        ViewBag.ClinicasSuspendidas = clinicasSuspendidas;
        ViewBag.Mrr = mrr;
        ViewBag.RenovacionesProximas = renovacionesProximas;
        ViewBag.NuevasClinicasMes = nuevasClinicasMes;
        ViewBag.ActividadReciente = actividadReciente;
        return View();
    }
}

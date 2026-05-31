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

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.UtcNow.AddHours(-4).Date;

        // Suscripción vigente: Activa, no suspendida, vencimiento estrictamente después de hoy (el día de vencimiento ya está vencida)
        var vigente = await _db.Suscripciones
            .AsNoTracking()
            .Where(s => s.Activa && !s.Suspendida && s.Vencimiento > hoy)
            .Select(s => s.ClinicaId)
            .Distinct()
            .ToListAsync();
        var clinicasActivas = vigente.Count;
        var clinicasVencidas = await _db.Clinicas
            .AsNoTracking()
            .Where(c => !vigente.Contains(c.Id) && c.Suscripciones.Any(s => s.Vencimiento <= hoy))
            .CountAsync();
        var clinicasSuspendidas = await _db.Suscripciones
            .AsNoTracking()
            .Where(s => s.Suspendida)
            .Select(s => s.ClinicaId)
            .Distinct()
            .CountAsync();

        // MRR: suma PrecioMensual del plan de cada clínica con suscripción vigente
        var mrr = await _db.Clinicas
            .AsNoTracking()
            .Where(c => vigente.Contains(c.Id))
            .SumAsync(c => c.Plan.PrecioMensual);

        // Renovaciones próximas (próximos 30 días; vigente = Vencimiento > Hoy)
        var renovacionesProximas = await _db.Suscripciones
            .AsNoTracking()
            .Where(s => s.Activa && !s.Suspendida && s.Vencimiento > hoy && s.Vencimiento <= hoy.AddDays(30))
            .Include(s => s.Clinica)
            .OrderBy(s => s.Vencimiento)
            .Take(15)
            .ToListAsync();

        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var nuevasClinicasMes = await _db.Clinicas
            .AsNoTracking()
            .Where(c => c.FechaCreacion >= inicioMes)
            .Include(c => c.Plan)
            .OrderByDescending(c => c.FechaCreacion)
            .Take(10)
            .ToListAsync();

        var actividadReciente = await _db.AuditLogs
            .AsNoTracking()
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

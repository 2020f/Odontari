using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;

namespace Odontari.Web.Controllers.Saas;

[Authorize(Roles = OdontariRoles.SuperAdmin)]
[Area("Saas")]
public class LogsController : Controller
{
    private readonly ApplicationDbContext _db;

    public LogsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? clinicaId, DateTime? desde, DateTime? hasta, string? accion)
    {
        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreadoAt);
        if (clinicaId.HasValue)
            query = query.Where(a => a.ClinicaId == clinicaId);
        if (desde.HasValue)
            query = query.Where(a => a.CreadoAt >= desde.Value);
        if (hasta.HasValue)
            query = query.Where(a => a.CreadoAt <= hasta.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(accion))
            query = query.Where(a => a.Accion.Contains(accion));

        var list = await query.Take(500).ToListAsync();
        ViewBag.Clinicas = await _db.Clinicas.OrderBy(c => c.Nombre).Select(c => new { c.Id, c.Nombre }).ToListAsync();
        ViewBag.ClinicaId = clinicaId;
        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;
        ViewBag.Accion = accion;
        return View(list);
    }
}

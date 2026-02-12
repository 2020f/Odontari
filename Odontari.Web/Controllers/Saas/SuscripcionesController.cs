using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;

namespace Odontari.Web.Controllers.Saas;

[Authorize(Roles = OdontariRoles.SuperAdmin)]
[Area("Saas")]
public class SuscripcionesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public SuscripcionesController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Suscripciones
            .Include(s => s.Clinica)
            .OrderByDescending(s => s.Vencimiento)
            .Select(s => new ViewModels.SuscripcionListViewModel
            {
                Id = s.Id,
                ClinicaId = s.ClinicaId,
                ClinicaNombre = s.Clinica.Nombre,
                Inicio = s.Inicio,
                Vencimiento = s.Vencimiento,
                Activa = s.Activa,
                Suspendida = s.Suspendida
            })
            .ToListAsync();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var s = await _db.Suscripciones.FindAsync(id);
        if (s == null) return NotFound();
        s.Activa = true;
        s.Suspendida = false;
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(s.ClinicaId, null, "Suscripcion_Activada", "Suscripcion", s.Id.ToString(), null);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspender(int id)
    {
        var s = await _db.Suscripciones.FindAsync(id);
        if (s == null) return NotFound();
        s.Suspendida = true;
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(s.ClinicaId, null, "Suscripcion_Suspendida", "Suscripcion", s.Id.ToString(), null);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Renovar(int id, int meses = 1)
    {
        var s = await _db.Suscripciones.FindAsync(id);
        if (s == null) return NotFound();
        var desde = s.Vencimiento > DateTime.Today ? s.Vencimiento : DateTime.Today;
        s.Vencimiento = desde.AddMonths(meses);
        s.Activa = true;
        s.Suspendida = false;
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(s.ClinicaId, null, "Suscripcion_Renovada", "Suscripcion", s.Id.ToString(), $"+{meses} mes(es)");
        return RedirectToAction(nameof(Index));
    }
}

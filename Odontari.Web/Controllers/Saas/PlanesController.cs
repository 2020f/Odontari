using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Controllers.Saas;

[Authorize(Roles = OdontariRoles.SuperAdmin)]
[Area("Saas")]
public class PlanesController : Controller
{
    private readonly ApplicationDbContext _db;

    public PlanesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Planes
            .OrderBy(p => p.Nombre)
            .Select(p => new PlanViewModel
            {
                Id = p.Id,
                Nombre = p.Nombre,
                PrecioMensual = p.PrecioMensual,
                MaxUsuarios = p.MaxUsuarios,
                MaxDoctores = p.MaxDoctores,
                PermiteFacturacion = p.PermiteFacturacion,
                PermiteOdontograma = p.PermiteOdontograma,
                Activo = p.Activo
            })
            .ToListAsync();
        return View(list);
    }

    public IActionResult Create() => View(new PlanEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanEditViewModel vm)
    {
        if (ModelState.IsValid)
        {
            _db.Planes.Add(new Plan
            {
                Nombre = vm.Nombre,
                PrecioMensual = vm.PrecioMensual,
                MaxUsuarios = vm.MaxUsuarios,
                MaxDoctores = vm.MaxDoctores,
                PermiteFacturacion = vm.PermiteFacturacion,
                PermiteOdontograma = vm.PermiteOdontograma,
                PermiteWhatsApp = vm.PermiteWhatsApp,
                PermiteARS = vm.PermiteARS,
                Activo = vm.Activo
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Planes.FindAsync(id);
        if (p == null) return NotFound();
        return View(new PlanEditViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            PrecioMensual = p.PrecioMensual,
            MaxUsuarios = p.MaxUsuarios,
            MaxDoctores = p.MaxDoctores,
            PermiteFacturacion = p.PermiteFacturacion,
            PermiteOdontograma = p.PermiteOdontograma,
            PermiteWhatsApp = p.PermiteWhatsApp,
            PermiteARS = p.PermiteARS,
            Activo = p.Activo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlanEditViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var p = await _db.Planes.FindAsync(id);
        if (p == null) return NotFound();
        if (ModelState.IsValid)
        {
            p.Nombre = vm.Nombre;
            p.PrecioMensual = vm.PrecioMensual;
            p.MaxUsuarios = vm.MaxUsuarios;
            p.MaxDoctores = vm.MaxDoctores;
            p.PermiteFacturacion = vm.PermiteFacturacion;
            p.PermiteOdontograma = vm.PermiteOdontograma;
            p.PermiteWhatsApp = vm.PermiteWhatsApp;
            p.PermiteARS = vm.PermiteARS;
            p.Activo = vm.Activo;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Controllers.Saas;

[Authorize(Roles = OdontariRoles.SuperAdmin)]
[Area("Saas")]
public class ClinicasController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public ClinicasController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Clinicas
            .Include(c => c.Plan)
            .OrderBy(c => c.Nombre)
            .Select(c => new ClinicaViewModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Email = c.Email,
                Telefono = c.Telefono,
                Activa = c.Activa,
                PlanNombre = c.Plan.Nombre
            })
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Planes = await _db.Planes.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        return View(new ClinicaEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClinicaEditViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var clinica = new Clinica
            {
                Nombre = vm.Nombre,
                Email = vm.Email,
                Telefono = vm.Telefono,
                Direccion = vm.Direccion,
                Activa = vm.Activa,
                PlanId = vm.PlanId
            };
            _db.Clinicas.Add(clinica);
            await _db.SaveChangesAsync();
            await _audit.RegistrarAsync(null, null, "Clinica_Creada", "Clinica", clinica.Id.ToString(), clinica.Nombre);
            _db.Suscripciones.Add(new Suscripcion
            {
                ClinicaId = clinica.Id,
                Inicio = DateTime.Today,
                Vencimiento = DateTime.Today.AddMonths(1),
                Activa = true,
                Suspendida = false
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Planes = await _db.Planes.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Clinicas.FindAsync(id);
        if (c == null) return NotFound();
        ViewBag.Planes = await _db.Planes.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        return View(new ClinicaEditViewModel
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Email = c.Email,
            Telefono = c.Telefono,
            Direccion = c.Direccion,
            Activa = c.Activa,
            PlanId = c.PlanId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClinicaEditViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var c = await _db.Clinicas.FindAsync(id);
        if (c == null) return NotFound();
        if (ModelState.IsValid)
        {
            c.Nombre = vm.Nombre;
            c.Email = vm.Email;
            c.Telefono = vm.Telefono;
            c.Direccion = vm.Direccion;
            c.Activa = vm.Activa;
            c.PlanId = vm.PlanId;
            await _db.SaveChangesAsync();
            await _audit.RegistrarAsync(c.Id, null, "Clinica_Editada", "Clinica", c.Id.ToString(), c.Nombre);
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Planes = await _db.Planes.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        return View(vm);
    }
}

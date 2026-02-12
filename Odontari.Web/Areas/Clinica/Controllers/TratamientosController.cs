using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Doctor)]
[Area("Clinica")]
public class TratamientosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public TratamientosController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();

    public async Task<IActionResult> Index()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var list = await _db.Tratamientos
            .Where(t => t.ClinicaId == cid)
            .OrderBy(t => t.Nombre)
            .Select(t => new TratamientoListViewModel
            {
                Id = t.Id,
                Nombre = t.Nombre,
                PrecioBase = t.PrecioBase,
                DuracionMinutos = t.DuracionMinutos,
                Categoria = t.Categoria,
                Activo = t.Activo
            })
            .ToListAsync();
        return View(list);
    }

    public IActionResult Create() => View(new TratamientoEditViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TratamientoEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (ModelState.IsValid)
        {
            _db.Tratamientos.Add(new Tratamiento
            {
                ClinicaId = cid.Value,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                PrecioBase = vm.PrecioBase,
                DuracionMinutos = vm.DuracionMinutos,
                Categoria = vm.Categoria,
                Activo = vm.Activo
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var t = await _db.Tratamientos.Where(x => x.ClinicaId == cid && x.Id == id).FirstOrDefaultAsync();
        if (t == null) return NotFound();
        return View(new TratamientoEditViewModel
        {
            Id = t.Id,
            Nombre = t.Nombre,
            Descripcion = t.Descripcion,
            PrecioBase = t.PrecioBase,
            DuracionMinutos = t.DuracionMinutos,
            Categoria = t.Categoria,
            Activo = t.Activo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TratamientoEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var t = await _db.Tratamientos.Where(x => x.ClinicaId == cid && x.Id == id).FirstOrDefaultAsync();
        if (t == null) return NotFound();
        if (ModelState.IsValid)
        {
            t.Nombre = vm.Nombre;
            t.Descripcion = vm.Descripcion;
            t.PrecioBase = vm.PrecioBase;
            t.DuracionMinutos = vm.DuracionMinutos;
            t.Categoria = vm.Categoria;
            t.Activo = vm.Activo;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }
}

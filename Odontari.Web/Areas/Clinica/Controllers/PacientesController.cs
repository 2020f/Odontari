using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Doctor)]
[Area("Clinica")]
public class PacientesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public PacientesController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();

    public async Task<IActionResult> Index(string? q)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var query = _db.Pacientes.Where(p => p.ClinicaId == cid);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Nombre.Contains(q) || (p.Apellidos != null && p.Apellidos.Contains(q)) || (p.Cedula != null && p.Cedula.Contains(q)) || (p.Telefono != null && p.Telefono.Contains(q)));
        var list = await query.OrderBy(p => p.Nombre).Select(p => new PacienteListViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Apellidos = p.Apellidos,
            Cedula = p.Cedula,
            Telefono = p.Telefono,
            Activo = p.Activo
        }).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        if (ClinicaId == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        return View(new PacienteEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PacienteEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (ModelState.IsValid)
        {
            var paciente = new Paciente
            {
                ClinicaId = cid.Value,
                Nombre = vm.Nombre,
                Apellidos = vm.Apellidos,
                Cedula = vm.Cedula,
                Telefono = vm.Telefono,
                Email = vm.Email,
                Direccion = vm.Direccion,
                FechaNacimiento = vm.FechaNacimiento,
                Alergias = vm.Alergias,
                NotasClinicas = vm.NotasClinicas,
                Activo = vm.Activo
            };
            _db.Pacientes.Add(paciente);
            await _db.SaveChangesAsync();

            // Historial inicial: evento "Creación de expediente"
            _db.HistorialClinico.Add(new HistorialClinico
            {
                PacienteId = paciente.Id,
                ClinicaId = cid.Value,
                FechaEvento = DateTime.UtcNow,
                TipoEvento = "Creación de expediente",
                Descripcion = "Paciente registrado en el sistema"
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
        var p = await _db.Pacientes.Where(x => x.ClinicaId == cid && x.Id == id).FirstOrDefaultAsync();
        if (p == null) return NotFound();
        var vm = new PacienteEditViewModel
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Apellidos = p.Apellidos,
            Cedula = p.Cedula,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion,
            FechaNacimiento = p.FechaNacimiento,
            Alergias = p.Alergias,
            NotasClinicas = p.NotasClinicas,
            Activo = p.Activo
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PacienteEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (id != vm.Id) return NotFound();
        var p = await _db.Pacientes.Where(x => x.ClinicaId == cid && x.Id == id).FirstOrDefaultAsync();
        if (p == null) return NotFound();
        if (ModelState.IsValid)
        {
            p.Nombre = vm.Nombre;
            p.Apellidos = vm.Apellidos;
            p.Cedula = vm.Cedula;
            p.Telefono = vm.Telefono;
            p.Email = vm.Email;
            p.Direccion = vm.Direccion;
            p.FechaNacimiento = vm.FechaNacimiento;
            p.Alergias = vm.Alergias;
            p.NotasClinicas = vm.NotasClinicas;
            p.Activo = vm.Activo;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }
}

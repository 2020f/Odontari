using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Doctor)]
[Area("Clinica")]
public class AgendaController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public AgendaController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();

    public async Task<IActionResult> Index(DateTime? fecha, string? doctorId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var dia = fecha ?? DateTime.Today;
        var inicio = dia.Date;
        var fin = inicio.AddDays(1);
        IQueryable<Cita> query = _db.Citas
            .Where(c => c.ClinicaId == cid && c.FechaHora >= inicio && c.FechaHora < fin && c.Estado != EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor);
        if (!string.IsNullOrEmpty(doctorId))
            query = query.Where(c => c.DoctorId == doctorId);
        var citas = await query.OrderBy(c => c.FechaHora).ToListAsync();
        ViewBag.Fecha = dia;
        var roleDoctorId = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.Where(u => u.ClinicaId == cid && doctorIds.Contains(u.Id)).Select(u => new { u.Id, NombreCompleto = u.NombreCompleto, Email = u.Email }).ToListAsync();
        ViewBag.DoctorIdSel = doctorId;
        var list = citas.Select(c => new CitaListViewModel
        {
            Id = c.Id,
            PacienteId = c.PacienteId,
            PacienteNombre = c.Paciente.Nombre + " " + (c.Paciente.Apellidos ?? ""),
            DoctorNombre = c.Doctor?.NombreCompleto ?? c.Doctor?.Email,
            FechaHora = c.FechaHora,
            Motivo = c.Motivo,
            Estado = c.Estado
        }).ToList();
        return View(list);
    }

    public async Task<IActionResult> Create(DateTime? fechaHora)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        ViewBag.Pacientes = await _db.Pacientes.Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorIdCreate = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIdsCreate = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorIdCreate).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.Where(u => u.ClinicaId == cid && doctorIdsCreate.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        return View(new CitaEditViewModel { FechaHora = fechaHora ?? DateTime.Now.Date.AddHours(9) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CitaEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (ModelState.IsValid)
        {
            var conflicto = await _db.Citas.AnyAsync(c => c.ClinicaId == cid && c.DoctorId == vm.DoctorId && c.FechaHora == vm.FechaHora && c.Estado != EstadoCita.Cancelada);
            if (conflicto) { ModelState.AddModelError("", "Ya existe una cita para ese doctor a esa hora."); return View(vm); }
            _db.Citas.Add(new Cita
            {
                ClinicaId = cid.Value,
                PacienteId = vm.PacienteId,
                DoctorId = vm.DoctorId,
                FechaHora = vm.FechaHora,
                Motivo = vm.Motivo,
                Estado = EstadoCita.Solicitada
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { fecha = vm.FechaHora.Date });
        }
        ViewBag.Pacientes = await _db.Pacientes.Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorId3 = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds3 = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId3).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.Where(u => u.ClinicaId == cid && doctorIds3.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoCita estado)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var cita = await _db.Citas.Where(c => c.ClinicaId == cid && c.Id == id).FirstOrDefaultAsync();
        if (cita == null) return NotFound();
        cita.Estado = estado;
        if (estado == EstadoCita.EnAtencion) cita.InicioAtencionAt = DateTime.Now;
        if (estado == EstadoCita.Finalizada) cita.FinAtencionAt = DateTime.Now;

        // Registrar en Histograma
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (estado == EstadoCita.EnSala)
            _db.HistorialClinico.Add(new HistorialClinico { PacienteId = cita.PacienteId, ClinicaId = cid.Value, CitaId = id, FechaEvento = DateTime.UtcNow, TipoEvento = "Check-in", Descripcion = "Paciente en sala", UsuarioId = uid });
        else if (estado == EstadoCita.EnAtencion)
            _db.HistorialClinico.Add(new HistorialClinico { PacienteId = cita.PacienteId, ClinicaId = cid.Value, CitaId = id, FechaEvento = DateTime.UtcNow, TipoEvento = "Atención iniciada", Descripcion = "Doctor inició la atención", UsuarioId = uid });

        await _db.SaveChangesAsync();
        if (estado == EstadoCita.EnAtencion)
            return RedirectToAction("Expediente", "Atencion", new { area = "Clinica", id = id });
        return RedirectToAction(nameof(Index), new { fecha = cita.FechaHora.Date });
    }

    public async Task<IActionResult> Editar(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var c = await _db.Citas.Include(c => c.Paciente).Include(c => c.Doctor).FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (c == null) return NotFound();
        ViewBag.Pacientes = await _db.Pacientes.Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorId4 = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds4 = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId4).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.Where(u => u.ClinicaId == cid && doctorIds4.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        return View(new CitaEditViewModel { Id = c.Id, PacienteId = c.PacienteId, DoctorId = c.DoctorId, FechaHora = c.FechaHora, Motivo = c.Motivo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, CitaEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var c = await _db.Citas.FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (c == null) return NotFound();
        if (ModelState.IsValid)
        {
            c.PacienteId = vm.PacienteId;
            c.DoctorId = vm.DoctorId;
            c.FechaHora = vm.FechaHora;
            c.Motivo = vm.Motivo;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { fecha = c.FechaHora.Date });
        }
        ViewBag.Pacientes = await _db.Pacientes.Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorId5 = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds5 = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId5).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.Where(u => u.ClinicaId == cid && doctorIds5.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        return View(vm);
    }
}

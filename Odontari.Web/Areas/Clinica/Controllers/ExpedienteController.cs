using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;
using System.Security.Claims;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Doctor)]
[Area("Clinica")]
public class ExpedienteController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public ExpedienteController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Vista principal del expediente del paciente con tabs.</summary>
    public async Task<IActionResult> Index(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var paciente = await _db.Pacientes
            .Include(p => p.Clinica)
            .FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var vm = new ExpedienteIndexViewModel
        {
            PacienteId = paciente.Id,
            Nombre = paciente.Nombre,
            Apellidos = paciente.Apellidos,
            Cedula = paciente.Cedula,
            Telefono = paciente.Telefono,
            Alergias = paciente.Alergias,
            NotasClinicas = paciente.NotasClinicas
        };

        // Historial (últimos 20 eventos)
        vm.Historial = await _db.HistorialClinico
            .Where(h => h.PacienteId == id && h.ClinicaId == cid)
            .OrderByDescending(h => h.FechaEvento)
            .Take(20)
            .Select(h => new HistorialEventoViewModel
            {
                FechaEvento = h.FechaEvento,
                TipoEvento = h.TipoEvento,
                Descripcion = h.Descripcion
            })
            .ToListAsync();

        ViewBag.Paciente = paciente;
        return View(vm);
    }

    /// <summary>Odontograma del paciente.</summary>
    [HttpGet]
    public async Task<IActionResult> Odontograma(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var odontograma = await _db.Odontogramas
            .Where(o => o.PacienteId == id && o.ClinicaId == cid)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();

        ViewBag.Paciente = paciente;
        ViewBag.EstadoJson = odontograma?.EstadoJson ?? "{}";
        ViewBag.OdontogramaId = odontograma?.Id;
        return View();
    }

    /// <summary>API: Obtener JSON del odontograma.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOdontogramaJson(int pacienteId)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var odontograma = await _db.Odontogramas
            .Where(o => o.PacienteId == pacienteId && o.ClinicaId == cid)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();

        var json = odontograma?.EstadoJson ?? "{}";
        return Content(json, "application/json");
    }

    /// <summary>API: Guardar odontograma (se llama desde JS; usuario autenticado).</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GuardarOdontograma([FromBody] GuardarOdontogramaRequest request)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        if (request == null) return BadRequest();

        var pacienteId = request.PacienteId;
        var estadoJson = request.EstadoJson ?? "{}";

        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == pacienteId);
        if (paciente == null) return NotFound();

        var existente = await _db.Odontogramas
            .Where(o => o.PacienteId == pacienteId && o.ClinicaId == cid)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();

        if (existente != null)
        {
            existente.EstadoJson = estadoJson;
            existente.UltimaModificacion = DateTime.UtcNow;
            existente.UltimoUsuarioId = UserId;
        }
        else
        {
            _db.Odontogramas.Add(new Odontograma
            {
                PacienteId = pacienteId,
                ClinicaId = cid.Value,
                EstadoJson = estadoJson,
                FechaRegistro = DateTime.UtcNow,
                UltimaModificacion = DateTime.UtcNow,
                UltimoUsuarioId = UserId
            });
        }

        await _db.SaveChangesAsync();

        // Registrar evento en historial
        _db.HistorialClinico.Add(new HistorialClinico
        {
            PacienteId = pacienteId,
            ClinicaId = cid.Value,
            FechaEvento = DateTime.UtcNow,
            TipoEvento = "Actualización odontograma",
            Descripcion = "Odontograma actualizado",
            UsuarioId = UserId
        });
        await _db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>Historia Clínica Sistemática (20 preguntas).</summary>
    [HttpGet]
    public async Task<IActionResult> HistoriaClinicaSistematica(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var hcs = await _db.HistoriasClinicasSistematicas
            .FirstOrDefaultAsync(h => h.PacienteId == id && h.ClinicaId == cid);

        var vm = hcs != null ? HistoriaClinicaSistematicaViewModel.FromEntity(hcs) : new HistoriaClinicaSistematicaViewModel { PacienteId = id };
        ViewBag.Paciente = paciente;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HistoriaClinicaSistematica(int id, HistoriaClinicaSistematicaViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        if (id != vm.PacienteId) return BadRequest();
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var hcs = await _db.HistoriasClinicasSistematicas.FirstOrDefaultAsync(h => h.PacienteId == id && h.ClinicaId == cid);
        var now = DateTime.UtcNow;

        if (hcs != null)
        {
            vm.ApplyToEntity(hcs);
            hcs.FechaActualizacion = now;
        }
        else
        {
            hcs = vm.ToEntity(id, cid.Value);
            hcs.FechaCreacion = now;
            hcs.FechaActualizacion = now;
            _db.HistoriasClinicasSistematicas.Add(hcs);
        }

        await _db.SaveChangesAsync();

        _db.HistorialClinico.Add(new HistorialClinico
        {
            PacienteId = id,
            ClinicaId = cid.Value,
            FechaEvento = now,
            TipoEvento = "Historia clínica sistemática",
            Descripcion = "Antecedentes médicos actualizados",
            UsuarioId = UserId
        });
        await _db.SaveChangesAsync();

        ViewBag.Paciente = paciente;
        return RedirectToAction(nameof(HistoriaClinicaSistematica), new { id });
    }
}

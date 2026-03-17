using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;
using VistasClinica = Odontari.Web.Data.VistasClinica;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Doctor + "," + OdontariRoles.Finanzas)]
[Area("Clinica")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public HomeController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.MostrarAccesoDenegado = string.Equals(Request.Query["accesoDenegado"], "1", StringComparison.Ordinal);
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction(nameof(SinClinica));
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        IQueryable<Models.Cita> queryCitas = _db.Citas
            .Where(c => c.ClinicaId == cid && c.FechaHora >= hoy && c.FechaHora < manana && c.Estado != Models.Enums.EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor);
        if (User.IsInRole(OdontariRoles.Doctor))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                queryCitas =  queryCitas.Where(c => c.DoctorId == userId);
        }
        ViewBag.CitasHoy = await queryCitas.OrderBy(c => c.FechaHora).ToListAsync();
        ViewBag.PendientesCobro = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && (o.Estado == Models.Enums.EstadoCobro.Pendiente || o.Estado == Models.Enums.EstadoCobro.Parcial))
            .Include(o => o.Paciente)
            .Take(10)
            .ToListAsync();

        // ── Panel de Recepción: estado de doctores y citas de mañana ──────────
        if (User.IsInRole(OdontariRoles.Recepcion) || User.IsInRole(OdontariRoles.AdminClinica))
        {
            var ahora = DateTime.Now;
            var mananaStart = hoy.AddDays(1);
            var mananaEnd   = hoy.AddDays(2);

            // Obtener IDs de doctores activos de esta clínica
            var roleDoctorId = await _db.Roles
                .Where(r => r.Name == OdontariRoles.Doctor)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var doctorIds = roleDoctorId != null
                ? await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId).Select(ur => ur.UserId).ToListAsync()
                : new List<string>();

            var doctores = await _db.Users
                .Where(u => u.ClinicaId == cid && doctorIds.Contains(u.Id) && u.Activo)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();

            // Citas de hoy activas para determinar estado del doctor
            var citasHoyActivas = await _db.Citas
                .Where(c => c.ClinicaId == cid
                         && c.FechaHora >= hoy
                         && c.FechaHora < manana
                         && c.Estado != EstadoCita.Cancelada)
                .Include(c => c.Paciente)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();

            var statusList = new List<DoctorStatusViewModel>();
            foreach (var doc in doctores)
            {
                // ¿Está el doctor actualmente en atención?
                var citaActiva = citasHoyActivas.FirstOrDefault(c =>
                    c.DoctorId == doc.Id &&
                    c.Estado == EstadoCita.EnAtencion);

                // Próxima cita pendiente del doctor hoy
                var proxima = citasHoyActivas
                    .Where(c => c.DoctorId == doc.Id
                             && c.FechaHora > ahora
                             && c.Estado != EstadoCita.Finalizada)
                    .OrderBy(c => c.FechaHora)
                    .FirstOrDefault();

                DoctorEstado estado;
                string? pacienteActual = null;

                if (citaActiva != null)
                {
                    estado = DoctorEstado.Ocupado;
                    pacienteActual = $"{citaActiva.Paciente?.Nombre} {citaActiva.Paciente?.Apellidos}".Trim();
                }
                else if (doc.HoraEntrada.HasValue && doc.HoraSalida.HasValue)
                {
                    var horaActual = ahora.TimeOfDay;
                    estado = (horaActual < doc.HoraEntrada.Value || horaActual >= doc.HoraSalida.Value)
                        ? DoctorEstado.FueraDeHorario
                        : DoctorEstado.Libre;
                }
                else
                {
                    estado = DoctorEstado.Libre;
                }

                statusList.Add(new DoctorStatusViewModel
                {
                    DoctorId      = doc.Id,
                    NombreCompleto = doc.NombreCompleto ?? doc.Email ?? doc.Id,
                    Estado        = estado,
                    PacienteActual = pacienteActual,
                    ProximaCitaTexto = proxima != null
                        ? $"{proxima.FechaHora:HH:mm} · {proxima.Paciente?.Nombre} {proxima.Paciente?.Apellidos}".Trim()
                        : null,
                    HorarioTexto = (doc.HoraEntrada.HasValue && doc.HoraSalida.HasValue)
                        ? $"{doc.HoraEntrada.Value:hh\\:mm} – {doc.HoraSalida.Value:hh\\:mm}"
                        : null
                });
            }

            ViewBag.DoctoresStatus = statusList;

            // Citas del día siguiente (filtradas por ClinicaId — multi-tenant)
            ViewBag.CitasManana = await _db.Citas
                .Where(c => c.ClinicaId == cid
                         && c.FechaHora >= mananaStart
                         && c.FechaHora < mananaEnd
                         && c.Estado != EstadoCita.Cancelada)
                .Include(c => c.Paciente)
                .Include(c => c.Doctor)
                .OrderBy(c => c.FechaHora)
                .ToListAsync();
        }

        return View();
    }

    public IActionResult SinClinica()
    {
        return View();
    }

    /// <summary>Pantalla de bloqueo cuando clínica inactiva o suscripción vencida.</summary>
    public IActionResult Bloqueo(string? motivo)
    {
        ViewBag.Motivo = motivo ?? "Acceso no permitido.";
        return View();
    }

    /// <summary>Mensaje cuando el usuario no tiene permiso para una vista (sin salir del panel).</summary>
    public IActionResult VistaNoPermitida(string? vista)
    {
        ViewData["Title"] = "Sin autorización";
        ViewBag.VistaNombre = string.IsNullOrEmpty(vista) ? "esta sección" : VistasClinica.NombrePorClave(vista);
        return View();
    }
}

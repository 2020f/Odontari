using System.Globalization;
using System.Security.Claims;
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

    /// <summary>Vista dinámica: calendario semanal/diario con filtros (Servicios, Personal, Clientes). No modifica la lógica existente de Index.</summary>
    [HttpGet]
    public async Task<IActionResult> VistaDinamica(DateTime? fecha, string vista = "semanal", string? doctorId = null, int? pacienteId = null, string? motivo = null, string? localizacion = null)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var esDoctor = User.IsInRole(OdontariRoles.Doctor);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (esDoctor && !string.IsNullOrEmpty(userId))
            doctorId = userId;

        var refDate = fecha ?? DateTime.Today;
        DateTime inicio;
        DateTime fin;
        if (string.Equals(vista, "diario", StringComparison.OrdinalIgnoreCase))
        {
            inicio = refDate.Date;
            fin = inicio.AddDays(1);
        }
        else
        {
            var diff = (7 + (refDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            inicio = refDate.Date.AddDays(-diff);
            fin = inicio.AddDays(7);
        }

        IQueryable<Cita> query = _db.Citas
            .AsNoTracking()
            .Where(c => c.ClinicaId == cid && c.FechaHora >= inicio && c.FechaHora < fin && c.Estado != EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor);
        if (!string.IsNullOrEmpty(doctorId))
            query = query.Where(c => c.DoctorId == doctorId);
        if (pacienteId.HasValue)
            query = query.Where(c => c.PacienteId == pacienteId.Value);
        if (!string.IsNullOrWhiteSpace(motivo))
            query = query.Where(c => c.Motivo != null && c.Motivo.Contains(motivo));

        var citas = await query.OrderBy(c => c.FechaHora).ToListAsync();
        var list = citas.Select(c => new CitaListViewModel
        {
            Id = c.Id,
            PacienteId = c.PacienteId,
            PacienteNombre = c.Paciente.Nombre + " " + (c.Paciente.Apellidos ?? ""),
            DoctorNombre = c.Doctor?.NombreCompleto ?? c.Doctor?.Email,
            FechaHora = c.FechaHora,
            DuracionMinutos = c.DuracionMinutos,
            Motivo = c.Motivo,
            Estado = c.Estado
        }).ToList();

        ViewBag.FechaInicio = inicio;
        ViewBag.FechaFin = fin;
        ViewBag.Vista = vista;
        ViewBag.EsDoctor = esDoctor;
        ViewBag.PuedeEditarCita = User.IsInRole(OdontariRoles.AdminClinica) || User.IsInRole(OdontariRoles.Recepcion);

        var roleDoctorId = await _db.Roles.AsNoTracking().Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds = await _db.UserRoles.AsNoTracking().Where(ur => ur.RoleId == roleDoctorId).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.AsNoTracking().Where(u => u.ClinicaId == cid && doctorIds.Contains(u.Id)).Select(u => new { u.Id, NombreCompleto = u.NombreCompleto ?? u.Email }).ToListAsync();
        ViewBag.Pacientes = await _db.Pacientes.AsNoTracking().Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var motivosDistinct = await _db.Citas.AsNoTracking().Where(c => c.ClinicaId == cid && c.Motivo != null && c.Motivo != "").Select(c => c.Motivo).Distinct().OrderBy(m => m).Take(50).ToListAsync();
        ViewBag.Motivos = motivosDistinct;

        ViewBag.DoctorIdSel = doctorId;
        ViewBag.PacienteIdSel = pacienteId;
        ViewBag.MotivoSel = motivo;
        ViewBag.LocalizacionSel = localizacion;
        ViewBag.FechaHoraModal = inicio.Date.AddHours(9);
        ViewBag.ReturnFecha = inicio.ToString("yyyy-MM-dd");
        ViewBag.ReturnVista = vista;
        ViewBag.ReturnDoctorId = doctorId ?? "";
        ViewBag.ReturnPacienteId = pacienteId.HasValue ? pacienteId.Value.ToString() : "";
        ViewBag.ReturnMotivo = motivo ?? "";

        return View(list);
    }

    public async Task<IActionResult> Index(DateTime? fecha, string? doctorId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var esDoctor = User.IsInRole(OdontariRoles.Doctor);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (esDoctor && !string.IsNullOrEmpty(userId))
        {
            doctorId = userId;
        }
        ViewBag.EsDoctor = esDoctor;
        var dia = fecha ?? DateTime.Today;
        var inicio = dia.Date;
        var fin = inicio.AddDays(1);
        IQueryable<Cita> query = _db.Citas
            .AsNoTracking()
            .Where(c => c.ClinicaId == cid && c.FechaHora >= inicio && c.FechaHora < fin && c.Estado != EstadoCita.Cancelada)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor);
        if (!string.IsNullOrEmpty(doctorId))
            query = query.Where(c => c.DoctorId == doctorId);
        var citas = await query.OrderBy(c => c.FechaHora).ToListAsync();
        ViewBag.Fecha = dia;
        var roleDoctorId = await _db.Roles.AsNoTracking().Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds = await _db.UserRoles.AsNoTracking().Where(ur => ur.RoleId == roleDoctorId).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.AsNoTracking().Where(u => u.ClinicaId == cid && doctorIds.Contains(u.Id)).Select(u => new { u.Id, NombreCompleto = u.NombreCompleto, Email = u.Email }).ToListAsync();
        ViewBag.DoctorIdSel = doctorId;
        // Resumen del día: single-pass COUNT per estado via individual CountAsync (avoids loading entities into memory)
        var queryResumen = _db.Citas.AsNoTracking().Where(c => c.ClinicaId == cid && c.FechaHora >= inicio && c.FechaHora < fin);
        if (!string.IsNullOrEmpty(doctorId)) queryResumen = queryResumen.Where(c => c.DoctorId == doctorId);
        ViewBag.ResumenTotal = await queryResumen.CountAsync();
        ViewBag.ResumenSolicitada = await queryResumen.CountAsync(c => c.Estado == EstadoCita.Solicitada);
        ViewBag.ResumenConfirmada = await queryResumen.CountAsync(c => c.Estado == EstadoCita.Confirmada);
        ViewBag.ResumenEnSala = await queryResumen.CountAsync(c => c.Estado == EstadoCita.EnSala);
        ViewBag.ResumenEnAtencion = await queryResumen.CountAsync(c => c.Estado == EstadoCita.EnAtencion);
        ViewBag.ResumenFinalizada = await queryResumen.CountAsync(c => c.Estado == EstadoCita.Finalizada);
        ViewBag.ResumenCancelada = await queryResumen.CountAsync(c => c.Estado == EstadoCita.Cancelada);
        var list = citas.Select(c => new CitaListViewModel
        {
            Id = c.Id,
            PacienteId = c.PacienteId,
            PacienteNombre = c.Paciente.Nombre + " " + (c.Paciente.Apellidos ?? ""),
            DoctorNombre = c.Doctor?.NombreCompleto ?? c.Doctor?.Email,
            FechaHora = c.FechaHora,
            DuracionMinutos = c.DuracionMinutos,
            Motivo = c.Motivo,
            Estado = c.Estado
        }).ToList();
        return View(list);
    }

    public async Task<IActionResult> Create(DateTime? fechaHora)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var esDoctor = User.IsInRole(OdontariRoles.Doctor) && !User.IsInRole(OdontariRoles.AdminClinica);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.EsDoctor = esDoctor;
        ViewBag.DoctorIdLogueado = esDoctor ? userId : null;
        ViewBag.Pacientes = await _db.Pacientes.AsNoTracking().Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorIdCreate = await _db.Roles.AsNoTracking().Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIdsCreate = await _db.UserRoles.AsNoTracking().Where(ur => ur.RoleId == roleDoctorIdCreate).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.AsNoTracking().Where(u => u.ClinicaId == cid && doctorIdsCreate.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        var vm = new CitaEditViewModel { FechaHora = fechaHora ?? DateTime.UtcNow.AddHours(-4).Date.AddHours(9), DuracionMinutos = 30 };
        if (esDoctor && userId != null) vm.DoctorId = userId;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CitaEditViewModel vm, string? returnToVistaDinamica, string? returnFecha, string? returnVista, string? returnDoctorId, string? returnPacienteId, string? returnMotivo, string? isModalForm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var desdeModal = isModalForm == "1";

        // Seguridad multitenant: si el usuario es Doctor, siempre forzar su propio ID como DoctorId.
        if (User.IsInRole(OdontariRoles.Doctor) && !User.IsInRole(OdontariRoles.AdminClinica))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid != null)
            {
                vm.DoctorId = uid;
                ModelState.Remove(nameof(vm.DoctorId)); // limpiar error de validación previo si lo hubiera
            }
        }

        if (ModelState.IsValid)
        {
            // 0. No se puede agendar en el pasado.
            // El servidor corre en UTC; los usuarios están en RD (UTC-4).
            // Comparamos la fecha enviada (hora local sin zona) contra la hora actual en RD,
            // con 30 minutos de tolerancia para evitar falsos positivos por latencia o diferencias de reloj.
            var ahoraRD = DateTime.UtcNow.AddHours(-4);
            if (vm.FechaHora < ahoraRD.AddMinutes(-30))
            {
                ModelState.AddModelError("", "No se puede agendar una cita en el pasado. Verifica la fecha y la hora.");
                if (desdeModal) return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                await CargarViewBagCreateAsync(cid.Value, vm);
                return View(vm);
            }

            var duracion = vm.DuracionMinutos <= 0 ? 30 : Math.Clamp(vm.DuracionMinutos, 5, 480);
            var finCita = vm.FechaHora.AddMinutes(duracion);

            // 1. El doctor no debe tener otra cita que se solape con esta (misma hora de consultorio)
            var citasDoctor = await _db.Citas
                .Where(c => c.ClinicaId == cid && c.DoctorId == vm.DoctorId && c.Estado != EstadoCita.Cancelada
                    && c.FechaHora.Date == vm.FechaHora.Date)
                .Select(c => new { c.FechaHora, c.DuracionMinutos })
                .ToListAsync();
            var hayConflicto = citasDoctor.Any(c => vm.FechaHora < c.FechaHora.AddMinutes(c.DuracionMinutos) && c.FechaHora < finCita);
            if (hayConflicto)
            {
                ModelState.AddModelError("", "Ya existe una cita para ese doctor que se solapa con el horario elegido. Elija otro doctor u otra hora/duración.");
                if (desdeModal) return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                await CargarViewBagCreateAsync(cid.Value, vm);
                return View(vm);
            }

            // 2. El doctor no debe estar fuera de su horario laboral (inicio y fin de cita dentro del turno)
            var doctor = await _db.Users
                .Where(u => u.Id == vm.DoctorId && u.ClinicaId == cid)
                .Select(u => new { u.HoraEntrada, u.HoraSalida })
                .FirstOrDefaultAsync();
            if (doctor != null && doctor.HoraEntrada.HasValue && doctor.HoraSalida.HasValue)
            {
                var horaInicio = vm.FechaHora.TimeOfDay;
                var horaFin = finCita.TimeOfDay;
                if (horaInicio < doctor.HoraEntrada.Value || horaFin > doctor.HoraSalida.Value)
                {
                    var ent = doctor.HoraEntrada.Value.ToString(@"hh\:mm");
                    var sal = doctor.HoraSalida.Value.ToString(@"hh\:mm");
                    ModelState.AddModelError("", $"La cita (inicio y fin) debe quedar dentro del horario laboral del doctor ({ent} - {sal}). Elija otra hora o duración.");
                    if (desdeModal) return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                    await CargarViewBagCreateAsync(cid.Value, vm);
                    return View(vm);
                }
            }

            _db.Citas.Add(new Cita
            {
                ClinicaId = cid.Value,
                PacienteId = vm.PacienteId,
                DoctorId = vm.DoctorId,
                FechaHora = vm.FechaHora,
                DuracionMinutos = duracion,
                Motivo = vm.Motivo,
                Estado = EstadoCita.Solicitada
            });
            await _db.SaveChangesAsync();

            if (desdeModal && !string.IsNullOrEmpty(returnToVistaDinamica))
            {
                var url = Url.Action(nameof(VistaDinamica), "Agenda", new { area = "Clinica", fecha = returnFecha ?? vm.FechaHora.ToString("yyyy-MM-dd"), vista = returnVista ?? "semanal", doctorId = returnDoctorId, pacienteId = returnPacienteId, motivo = returnMotivo });
                return Json(new { success = true, redirectUrl = url });
            }
            if (!string.IsNullOrEmpty(returnToVistaDinamica))
                return RedirectToAction(nameof(VistaDinamica), new { area = "Clinica", fecha = returnFecha ?? vm.FechaHora.Date.ToString("yyyy-MM-dd"), vista = returnVista ?? "semanal", doctorId = returnDoctorId, pacienteId = returnPacienteId, motivo = returnMotivo });
            return RedirectToAction(nameof(Index), new { fecha = vm.FechaHora.Date });
        }

        if (desdeModal)
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
        await CargarViewBagCreateAsync(cid.Value, vm);
        return View(vm);
    }

    private async Task CargarViewBagCreateAsync(int cid, CitaEditViewModel vm)
    {
        var esDoctor = User.IsInRole(OdontariRoles.Doctor) && !User.IsInRole(OdontariRoles.AdminClinica);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.EsDoctor = esDoctor;
        ViewBag.DoctorIdLogueado = esDoctor ? userId : null;
        ViewBag.Pacientes = await _db.Pacientes.Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorId = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.Where(u => u.ClinicaId == cid && doctorIds.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
    }

    /// <summary>
    /// AJAX: devuelve los datos de una cita para el drawer de detalle en VistaDinamica.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DetalleCita(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return Json(new { error = "Sin clínica" });

        var cita = await _db.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Doctor)
            .Where(c => c.ClinicaId == cid && c.Id == id)
            .FirstOrDefaultAsync();

        if (cita == null) return Json(new { error = "No encontrada" });

        var puedeEditar = User.IsInRole(OdontariRoles.AdminClinica) || User.IsInRole(OdontariRoles.Recepcion);
        var editUrl = puedeEditar ? Url.Action(nameof(Editar), "Agenda", new { area = "Clinica", id }) : null;

        int? edad = null;
        if (cita.Paciente.FechaNacimiento.HasValue)
        {
            var hoy = DateTime.Today;
            var fn  = cita.Paciente.FechaNacimiento.Value;
            edad = hoy.Year - fn.Year - (hoy.DayOfYear < fn.DayOfYear ? 1 : 0);
        }

        var estadoTexto = cita.Estado switch
        {
            EstadoCita.Solicitada  => "Pendiente",
            EstadoCita.Confirmada  => "Confirmada",
            EstadoCita.EnSala      => "En sala",
            EstadoCita.EnAtencion  => "En atención",
            EstadoCita.Finalizada  => "Finalizada",
            EstadoCita.Cancelada   => "Cancelada",
            EstadoCita.NoShow      => "No show",
            _                      => cita.Estado.ToString()
        };

        return Json(new
        {
            id         = cita.Id,
            paciente   = new
            {
                nombre   = cita.Paciente.Nombre + " " + (cita.Paciente.Apellidos ?? ""),
                cedula   = cita.Paciente.Cedula ?? "—",
                telefono = cita.Paciente.Telefono ?? "—",
                edad
            },
            doctor     = cita.Doctor?.NombreCompleto ?? cita.Doctor?.Email ?? "—",
            fechaHora  = cita.FechaHora.ToString("dddd d 'de' MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("es-ES")),
            duracion   = cita.DuracionMinutos,
            motivo     = cita.Motivo ?? "",
            estado     = estadoTexto,
            estadoSlug = cita.Estado.ToString().ToLowerInvariant(),
            puedeEditar,
            editUrl
        });
    }

    /// <summary>
    /// AJAX: devuelve los slots ocupados del doctor en la fecha dada.
    /// Usado por el DateTimePicker para deshabilitar horas no disponibles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHorasOcupadas(string doctorId, string fecha)
    {
        var cid = ClinicaId;
        if (cid == null) return Json(new { ocupadas = Array.Empty<object>() });
        if (string.IsNullOrWhiteSpace(doctorId) || string.IsNullOrWhiteSpace(fecha))
            return Json(new { ocupadas = Array.Empty<object>() });
        if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaDt))
            return Json(new { ocupadas = Array.Empty<object>() });

        var citas = await _db.Citas
            .AsNoTracking()
            .Where(c => c.ClinicaId == cid && c.DoctorId == doctorId
                && c.FechaHora.Date == fechaDt.Date
                && c.Estado != EstadoCita.Cancelada)
            .Select(c => new { c.FechaHora, c.DuracionMinutos })
            .ToListAsync();

        var ocupadas = citas.Select(c => new
        {
            inicio = c.FechaHora.ToString("HH:mm"),
            fin    = c.FechaHora.AddMinutes(c.DuracionMinutos > 0 ? c.DuracionMinutos : 30).ToString("HH:mm")
        }).ToList();

        var doctor = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == doctorId && u.ClinicaId == cid)
            .Select(u => new { u.HoraEntrada, u.HoraSalida })
            .FirstOrDefaultAsync();

        return Json(new
        {
            ocupadas,
            horaEntrada = doctor?.HoraEntrada?.ToString(@"hh\:mm"),
            horaSalida  = doctor?.HoraSalida?.ToString(@"hh\:mm")
        });
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
        if (estado == EstadoCita.EnAtencion) cita.InicioAtencionAt = DateTime.UtcNow.AddHours(-4);
        if (estado == EstadoCita.Finalizada) cita.FinAtencionAt = DateTime.UtcNow.AddHours(-4);

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
        var c = await _db.Citas.AsNoTracking().Include(c => c.Paciente).Include(c => c.Doctor).FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == id);
        if (c == null) return NotFound();
        ViewBag.Pacientes = await _db.Pacientes.AsNoTracking().Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorId4 = await _db.Roles.AsNoTracking().Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds4 = await _db.UserRoles.AsNoTracking().Where(ur => ur.RoleId == roleDoctorId4).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.AsNoTracking().Where(u => u.ClinicaId == cid && doctorIds4.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        var duracion = c.DuracionMinutos > 0 ? c.DuracionMinutos : 30;
        return View(new CitaEditViewModel { Id = c.Id, PacienteId = c.PacienteId, DoctorId = c.DoctorId, FechaHora = c.FechaHora, DuracionMinutos = duracion, Motivo = c.Motivo });
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
            var duracion = vm.DuracionMinutos <= 0 ? 30 : Math.Clamp(vm.DuracionMinutos, 5, 480);
            var finCita = vm.FechaHora.AddMinutes(duracion);

            var citasDoctor = await _db.Citas
                .Where(cita => cita.ClinicaId == cid && cita.DoctorId == vm.DoctorId && cita.Estado != EstadoCita.Cancelada && cita.Id != id
                    && cita.FechaHora.Date == vm.FechaHora.Date)
                .Select(cita => new { cita.FechaHora, cita.DuracionMinutos })
                .ToListAsync();
            var hayConflicto = citasDoctor.Any(cita => vm.FechaHora < cita.FechaHora.AddMinutes(cita.DuracionMinutos) && cita.FechaHora < finCita);
            if (hayConflicto)
            {
                ModelState.AddModelError("", "Ya existe una cita para ese doctor que se solapa con el horario elegido. Elija otro doctor u otra hora/duración.");
            }
            else
            {
                var doctor = await _db.Users
                    .Where(u => u.Id == vm.DoctorId && u.ClinicaId == cid)
                    .Select(u => new { u.HoraEntrada, u.HoraSalida })
                    .FirstOrDefaultAsync();
                if (doctor != null && doctor.HoraEntrada.HasValue && doctor.HoraSalida.HasValue)
                {
                    var horaInicio = vm.FechaHora.TimeOfDay;
                    var horaFin = finCita.TimeOfDay;
                    if (horaInicio < doctor.HoraEntrada.Value || horaFin > doctor.HoraSalida.Value)
                    {
                        var ent = doctor.HoraEntrada.Value.ToString(@"hh\:mm");
                        var sal = doctor.HoraSalida.Value.ToString(@"hh\:mm");
                        ModelState.AddModelError("", $"La cita (inicio y fin) debe quedar dentro del horario laboral del doctor ({ent} - {sal}). Elija otra hora o duración.");
                    }
                    else
                    {
                        c.PacienteId = vm.PacienteId;
                        c.DoctorId = vm.DoctorId;
                        c.FechaHora = vm.FechaHora;
                        c.DuracionMinutos = duracion;
                        c.Motivo = vm.Motivo;
                        await _db.SaveChangesAsync();
                        return RedirectToAction(nameof(Index), new { fecha = c.FechaHora.Date });
                    }
                }
                else
                {
                    c.PacienteId = vm.PacienteId;
                    c.DoctorId = vm.DoctorId;
                    c.FechaHora = vm.FechaHora;
                    c.DuracionMinutos = duracion;
                    c.Motivo = vm.Motivo;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { fecha = c.FechaHora.Date });
                }
            }
        }
        ViewBag.Pacientes = await _db.Pacientes.AsNoTracking().Where(p => p.ClinicaId == cid && p.Activo).OrderBy(p => p.Nombre).Select(p => new { p.Id, Nombre = p.Nombre + " " + (p.Apellidos ?? "") }).ToListAsync();
        var roleDoctorId5 = await _db.Roles.AsNoTracking().Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds5 = await _db.UserRoles.AsNoTracking().Where(ur => ur.RoleId == roleDoctorId5).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users.AsNoTracking().Where(u => u.ClinicaId == cid && doctorIds5.Contains(u.Id)).Select(u => new { u.Id, Nombre = u.NombreCompleto ?? u.Email ?? u.Id }).ToListAsync();
        return View(vm);
    }
}

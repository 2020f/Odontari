using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;

namespace Odontari.Web.Areas.Clinica.Controllers;

/// <summary>Reportes de clínica: financieros, operativos y cuentas por cobrar. Multitenant por ClinicaId.</summary>
[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Finanzas)]
[Area("Clinica")]
public class ReportesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public ReportesController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();

    /// <summary>Vista principal: filtros globales + KPIs + reportes financieros/operativos/cuentas por cobrar.</summary>
    public async Task<IActionResult> Index(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? doctorId,
        int? estadoCobro,
        string? agrupar) // dia | semana | mes para gráfico ingresos
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var fin = fechaFin?.Date ?? hoy;
        var inicio = fechaInicio?.Date ?? inicioMes;
        if (inicio > fin) (inicio, fin) = (fin, inicio);

        ViewBag.FechaInicio = inicio.ToString("yyyy-MM-dd");
        ViewBag.FechaFin = fin.ToString("yyyy-MM-dd");
        ViewBag.DoctorIdSel = doctorId ?? "";
        ViewBag.EstadoCobroSel = estadoCobro;
        ViewBag.Agrupar = agrupar ?? "mes";

        var finMasUno = fin.AddDays(1);
        var esDoctor = User.IsInRole(OdontariRoles.Doctor);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (esDoctor && !string.IsNullOrEmpty(userId)) doctorId = userId;

        // Doctores para filtro
        var roleDoctorId = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
        var doctorIds = await _db.UserRoles.Where(ur => ur.RoleId == roleDoctorId).Select(ur => ur.UserId).ToListAsync();
        ViewBag.Doctores = await _db.Users
            .Where(u => u.ClinicaId == cid && doctorIds.Contains(u.Id))
            .Select(u => new { u.Id, NombreCompleto = u.NombreCompleto ?? u.Email })
            .ToListAsync();

        // === Órdenes y pagos en período (filtro estado opcional) ===
        var queryOrdenes = _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && o.CreadoAt >= inicio && o.CreadoAt < finMasUno);
        if (!string.IsNullOrEmpty(doctorId))
        {
            var citaIds = await _db.Citas.Where(c => c.ClinicaId == cid && c.DoctorId == doctorId).Select(c => c.Id).ToListAsync();
            queryOrdenes = queryOrdenes.Where(o => o.CitaId != null && citaIds.Contains(o.CitaId.Value));
        }
        if (estadoCobro.HasValue && estadoCobro.Value >= 0 && estadoCobro.Value <= 2)
            queryOrdenes = queryOrdenes.Where(o => (int)o.Estado == estadoCobro.Value);

        var ordenesPeriodo = await queryOrdenes.ToListAsync();

        // Pagos en período (para cobrado real)
        var pagosEnPeriodo = await _db.Pagos
            .Where(p => p.OrdenCobro.ClinicaId == cid && p.FechaPago >= inicio && p.FechaPago < finMasUno)
            .Select(p => new { p.Monto, p.OrdenCobroId, p.FechaPago })
            .ToListAsync();
        if (!string.IsNullOrEmpty(doctorId))
        {
            var citaIds = await _db.Citas.Where(c => c.ClinicaId == cid && c.DoctorId == doctorId).Select(c => c.Id).ToListAsync();
            var ordenIdsDoctor = await _db.OrdenesCobro.Where(o => o.ClinicaId == cid && o.CitaId != null && citaIds.Contains(o.CitaId.Value)).Select(o => o.Id).ToListAsync();
            pagosEnPeriodo = pagosEnPeriodo.Where(p => ordenIdsDoctor.Contains(p.OrdenCobroId)).ToList();
        }

        // --- KPI ---
        var ingresosTotales = ordenesPeriodo.Sum(o => o.Total);
        var cobrado = pagosEnPeriodo.Sum(p => p.Monto);
        var pendienteOrdenes = ordenesPeriodo.Sum(o => o.Total - o.MontoPagado);

        IQueryable<Cita> queryCitas = _db.Citas
            .Where(c => c.ClinicaId == cid && c.FechaHora >= inicio && c.FechaHora < finMasUno)
            .Include(c => c.Paciente);
        if (!string.IsNullOrEmpty(doctorId)) queryCitas = queryCitas.Where(c => c.DoctorId == doctorId);
        var citasPeriodo = await queryCitas.ToListAsync();

        var citasAtendidas = citasPeriodo.Count(c => c.Estado == EstadoCita.Finalizada);
        var noShow = citasPeriodo.Count(c => c.Estado == EstadoCita.NoShow);
        var canceladas = citasPeriodo.Count(c => c.Estado == EstadoCita.Cancelada);
        var programadas = citasPeriodo.Count;

        var numDoctores = await queryCitas.Where(c => c.DoctorId != null).Select(c => c.DoctorId).Distinct().CountAsync();
        var produccionPromedio = numDoctores > 0 ? ingresosTotales / numDoctores : 0m;

        ViewBag.IngresosTotales = ingresosTotales;
        ViewBag.Cobrado = cobrado;
        ViewBag.Pendiente = pendienteOrdenes;
        ViewBag.CitasAtendidas = citasAtendidas;
        ViewBag.NoShow = noShow;
        ViewBag.ProduccionPromedioDoctor = produccionPromedio;
        ViewBag.CitasProgramadas = programadas;
        ViewBag.CitasCanceladas = canceladas;

        // --- Ingresos por período (para gráfico) ---
        var agrup = agrupar ?? "mes";
        List<object> ingresosPorPeriodo;
        if (agrup == "dia")
        {
            var porDia = pagosEnPeriodo.GroupBy(p => p.FechaPago.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Monto) }).OrderBy(x => x.Fecha).ToList();
            ingresosPorPeriodo = porDia.Select(x => (object)new { label = x.Fecha.ToString("dd/MM", CultureInfo.GetCultureInfo("es-ES")), value = (double)x.Total }).ToList();
        }
        else if (agrup == "semana")
        {
            var porSemana = pagosEnPeriodo
                .GroupBy(p => System.Globalization.ISOWeek.GetWeekOfYear(p.FechaPago))
                .Select(g => new { Semana = g.Key, Total = g.Sum(x => x.Monto), Fecha = g.Min(x => x.FechaPago) }).OrderBy(x => x.Fecha).ToList();
            ingresosPorPeriodo = porSemana.Select(x => (object)new { label = "S" + x.Semana, value = (double)x.Total }).ToList();
        }
        else
        {
            var porMes = pagosEnPeriodo.GroupBy(p => new { p.FechaPago.Year, p.FechaPago.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Monto) }).OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
            ingresosPorPeriodo = porMes.Select(x => (object)new { label = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy", CultureInfo.GetCultureInfo("es-ES")), value = (double)x.Total }).ToList();
        }
        ViewBag.IngresosPorPeriodoJson = System.Text.Json.JsonSerializer.Serialize(ingresosPorPeriodo);

        // --- Ingresos por doctor ---
        var ordenesConCita = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && o.CreadoAt >= inicio && o.CreadoAt < finMasUno && o.CitaId != null)
            .Include(o => o.Cita)
            .ToListAsync();
        if (!string.IsNullOrEmpty(doctorId))
            ordenesConCita = ordenesConCita.Where(o => o.Cita != null && o.Cita.DoctorId == doctorId).ToList();
        if (estadoCobro.HasValue && estadoCobro.Value >= 0 && estadoCobro.Value <= 2)
            ordenesConCita = ordenesConCita.Where(o => (int)o.Estado == estadoCobro.Value).ToList();
        var doctorIdsOrdenes = ordenesConCita.Select(o => o.Cita!.DoctorId).Distinct().ToList();
        var usuarios = await _db.Users.Where(u => doctorIdsOrdenes.Contains(u.Id)).Select(u => new { u.Id, u.NombreCompleto, u.Email }).ToListAsync();
        var ingresosPorDoctor = doctorIdsOrdenes.Select(did =>
        {
            var ord = ordenesConCita.Where(o => o.Cita!.DoctorId == did).ToList();
            var nombre = usuarios.FirstOrDefault(u => u.Id == did)?.NombreCompleto ?? usuarios.FirstOrDefault(u => u.Id == did)?.Email ?? did;
            return new { DoctorId = did, DoctorNombre = nombre ?? did, Total = ord.Sum(o => o.Total), Pagado = ord.Sum(o => o.MontoPagado), Pendiente = ord.Sum(o => o.Total - o.MontoPagado) };
        }).OrderByDescending(x => x.Total).ToList();
        ViewBag.IngresosPorDoctor = ingresosPorDoctor;
        ViewBag.IngresosPorDoctorJson = System.Text.Json.JsonSerializer.Serialize(ingresosPorDoctor.Select(d => new { label = d.DoctorNombre, value = (double)d.Total }));

        // --- Tratamientos más realizados ---
        var procedimientos = await _db.ProcedimientosRealizados
            .Where(pr => pr.Cita!.ClinicaId == cid && pr.Cita.FechaHora >= inicio && pr.Cita.FechaHora < finMasUno && pr.MarcadoRealizado)
            .Include(pr => pr.Tratamiento)
            .ToListAsync();
        if (!string.IsNullOrEmpty(doctorId)) procedimientos = procedimientos.Where(pr => pr.Cita!.DoctorId == doctorId).ToList();
        var porTratamiento = procedimientos
            .GroupBy(pr => new { pr.TratamientoId, Nombre = pr.Tratamiento?.Nombre ?? "Sin nombre" })
            .Select(g => new { Tratamiento = g.Key.Nombre, Cantidad = g.Count(), Total = g.Sum(pr => pr.PrecioAplicado) })
            .OrderByDescending(x => x.Total)
            .Take(15)
            .ToList();
        ViewBag.TratamientosMasRealizados = porTratamiento;

        // --- Citas (operativo) ---
        var totalCitas = citasPeriodo.Count;
        ViewBag.CitasAtendidasPct = totalCitas > 0 ? (double)citasAtendidas / totalCitas * 100 : 0;
        ViewBag.NoShowPct = totalCitas > 0 ? (double)noShow / totalCitas * 100 : 0;
        ViewBag.CanceladasPct = totalCitas > 0 ? (double)canceladas / totalCitas * 100 : 0;

        // No-show: lista de citas para enlace
        ViewBag.NoShowCitas = citasPeriodo.Where(c => c.Estado == EstadoCita.NoShow).Take(50).ToList();

        // --- Pacientes nuevos vs recurrentes ---
        var pacientesEnPeriodo = citasPeriodo.Where(c => c.Estado == EstadoCita.Finalizada).Select(c => c.PacienteId).Distinct().ToList();
        var primeraCitaPorPaciente = await _db.Citas
            .Where(c => c.ClinicaId == cid)
            .GroupBy(c => c.PacienteId)
            .Select(g => new { PacienteId = g.Key, PrimeraFecha = g.Min(c => c.FechaHora) })
            .ToListAsync();
        var nuevos = pacientesEnPeriodo.Count(pid => primeraCitaPorPaciente.Any(p => p.PacienteId == pid && p.PrimeraFecha >= inicio && p.PrimeraFecha < finMasUno));
        var recurrentes = pacientesEnPeriodo.Count - nuevos;
        ViewBag.PacientesNuevos = nuevos;
        ViewBag.PacientesRecurrentes = recurrentes;
        ViewBag.PacientesNuevosVsRecurrentesJson = System.Text.Json.JsonSerializer.Serialize(new[] { new { label = "Nuevos", value = nuevos }, new { label = "Recurrentes", value = recurrentes } });

        // --- Cuentas por cobrar ---
        var ordenesPendientesRaw = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && (o.Estado == EstadoCobro.Pendiente || o.Estado == EstadoCobro.Parcial))
            .Include(o => o.Paciente)
            .Include(o => o.Pagos)
            .OrderByDescending(o => o.CreadoAt)
            .ToListAsync();
        var cuentasPorCobrar = ordenesPendientesRaw.Select(o => new
        {
            o.Id,
            o.PacienteId,
            PacienteNombre = o.Paciente.Nombre + " " + (o.Paciente.Apellidos ?? ""),
            o.Total,
            o.MontoPagado,
            Saldo = o.Total - o.MontoPagado,
            UltimaVisita = o.Pagos.Any() ? o.Pagos.Max(p => p.FechaPago) : (DateTime?)null
        }).ToList();
        ViewBag.CuentasPorCobrar = cuentasPorCobrar;

        ViewData["Title"] = "Reportes";
        return View();
    }

    /// <summary>Listado de citas No-Show para el período (modal o parcial).</summary>
    public async Task<IActionResult> NoShowList(DateTime? fechaInicio, DateTime? fechaFin, string? doctorId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var inicio = fechaInicio?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var fin = fechaFin?.Date ?? DateTime.Today;
        var finMasUno = fin.AddDays(1);

        IQueryable<Cita> query = _db.Citas
            .Where(c => c.ClinicaId == cid && c.FechaHora >= inicio && c.FechaHora < finMasUno && c.Estado == EstadoCita.NoShow)
            .Include(c => c.Paciente)
            .Include(c => c.Doctor)
            .OrderBy(c => c.FechaHora);
        if (!string.IsNullOrEmpty(doctorId)) query = query.Where(c => c.DoctorId == doctorId);
        var list = await query.Select(c => new { c.Id, c.FechaHora, Paciente = c.Paciente.Nombre + " " + (c.Paciente.Apellidos ?? ""), Doctor = c.Doctor != null ? c.Doctor.NombreCompleto : "" }).ToListAsync();
        return Json(list);
    }
}

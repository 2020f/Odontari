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

/// <summary>Reportes de clínica: financieros, operativos y cuentas por cobrar. Multitenant por ClinicaId.</summary>
[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Finanzas)]
[Area("Clinica")]
public class ReportesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;
    private readonly ReporteFinancieroExportService _exportService;

    public ReportesController(ApplicationDbContext db, IClinicaActualService clinicaActual, ReporteFinancieroExportService exportService)
    {
        _db = db;
        _clinicaActual = clinicaActual;
        _exportService = exportService;
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

    /// <summary>Exportar reporte financiero a Excel (4 hojas: Resumen, Detalle, Cuentas por cobrar, Producción por doctor).</summary>
    public async Task<IActionResult> ExportExcel(DateTime? fechaInicio, DateTime? fechaFin, string? doctorId, int? estadoCobro)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var (inicio, fin) = NormalizarRangoFechas(fechaInicio, fechaFin);
        if (User.IsInRole(OdontariRoles.Doctor))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId)) doctorId = userId;
        }
        var data = await BuildReporteFinancieroDataAsync(cid.Value, inicio, fin, doctorId, estadoCobro);
        var bytes = _exportService.GenerateExcel(data);
        var nombre = $"ReporteFinanciero_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
    }

    /// <summary>Exportar reporte financiero a PDF (resumen ejecutivo formal).</summary>
    public async Task<IActionResult> ExportPdf(DateTime? fechaInicio, DateTime? fechaFin, string? doctorId, int? estadoCobro)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var (inicio, fin) = NormalizarRangoFechas(fechaInicio, fechaFin);
        if (User.IsInRole(OdontariRoles.Doctor))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId)) doctorId = userId;
        }
        var data = await BuildReporteFinancieroDataAsync(cid.Value, inicio, fin, doctorId, estadoCobro);
        var bytes = _exportService.GeneratePdf(data);
        var nombre = $"ReporteFinanciero_{inicio:yyyyMMdd}_{fin:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", nombre);
    }

    private static (DateTime inicio, DateTime fin) NormalizarRangoFechas(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var fin = fechaFin?.Date ?? hoy;
        var inicio = fechaInicio?.Date ?? inicioMes;
        if (inicio > fin) (inicio, fin) = (fin, inicio);
        return (inicio, fin);
    }

    private async Task<ReporteFinancieroData> BuildReporteFinancieroDataAsync(int cid, DateTime inicio, DateTime fin, string? doctorId, int? estadoCobro)
    {
        var finMasUno = fin.AddDays(1);
        var ci = CultureInfo.GetCultureInfo("es-ES");
        var userName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Sistema";

        var clinica = await _db.Clinicas.Where(c => c.Id == cid).Select(c => new { c.Nombre, c.Direccion, c.Telefono }).FirstOrDefaultAsync();
        var encabezado = new EncabezadoReporte
        {
            NombreClinica = clinica?.Nombre ?? "Clínica",
            RNC = "N/A",
            Direccion = clinica?.Direccion ?? "",
            Telefono = clinica?.Telefono ?? "",
            FechaGeneracion = DateTime.Now,
            RangoFechas = $"{inicio:dd/MM/yyyy} - {fin:dd/MM/yyyy}",
            UsuarioGenero = userName
        };

        IQueryable<OrdenCobro> queryOrdenes = _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && o.CreadoAt >= inicio && o.CreadoAt < finMasUno)
            .Include(o => o.Paciente)
            .Include(o => o.Pagos)
            .Include(o => o.Cita!)
                .ThenInclude(c => c.Doctor)
            .Include(o => o.Cita!)
                .ThenInclude(c => c.ProcedimientosRealizados!)
                .ThenInclude(pr => pr.Tratamiento);
        if (!string.IsNullOrEmpty(doctorId))
        {
            var citaIds = await _db.Citas.Where(c => c.ClinicaId == cid && c.DoctorId == doctorId).Select(c => c.Id).ToListAsync();
            queryOrdenes = queryOrdenes.Where(o => o.CitaId != null && citaIds.Contains(o.CitaId.Value));
        }
        if (estadoCobro.HasValue && estadoCobro.Value >= 0 && estadoCobro.Value <= 3)
            queryOrdenes = queryOrdenes.Where(o => (int)o.Estado == estadoCobro.Value);

        var ordenes = await queryOrdenes.OrderBy(o => o.CreadoAt).ToListAsync();

        var totalFacturado = ordenes.Sum(o => o.Total);
        var totalCobrado = ordenes.Sum(o => o.MontoPagado);
        var totalPendiente = ordenes.Sum(o => o.Total - o.MontoPagado);
        var totalAnulado = ordenes.Where(o => o.Estado == EstadoCobro.Anulado).Sum(o => o.Total);
        var resumen = new ResumenFinanciero
        {
            TotalFacturado = totalFacturado,
            TotalCobrado = totalCobrado,
            TotalPendiente = totalPendiente,
            TotalAnulado = totalAnulado,
            DescuentosAplicados = 0,
            TotalNetoReal = totalCobrado
        };

        var detalle = new List<DetalleIngresoRow>();
        foreach (var o in ordenes)
        {
            var tratamientos = o.Cita?.ProcedimientosRealizados?.Select(pr => pr.Tratamiento?.Nombre ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
            var metodoPago = o.Pagos?.Any() == true
                ? (o.Pagos.Count == 1 ? (o.Pagos.First().MetodoPago ?? "—") : "Varios")
                : "—";
            detalle.Add(new DetalleIngresoRow
            {
                Fecha = o.CreadoAt,
                NumeroCita = o.CitaId,
                Paciente = o.Paciente != null ? (o.Paciente.Nombre + " " + (o.Paciente.Apellidos ?? "")).Trim() : "",
                Doctor = o.Cita?.Doctor?.NombreCompleto ?? o.Cita?.Doctor?.Email ?? "—",
                Tratamiento = string.Join(", ", tratamientos),
                MetodoPago = metodoPago,
                MontoTotal = o.Total,
                MontoPagado = o.MontoPagado,
                SaldoPendiente = o.Total - o.MontoPagado,
                Estado = o.Estado switch { EstadoCobro.Pagado => "Pagado", EstadoCobro.Parcial => "Parcial", EstadoCobro.Pendiente => "Pendiente", EstadoCobro.Anulado => "Anulado", _ => o.Estado.ToString() }
            });
        }

        var porDoctor = ordenes
            .Where(o => o.Cita?.DoctorId != null)
            .GroupBy(o => o.Cita!.DoctorId)
            .Select(g =>
            {
                var first = g.First();
                var nombre = first.Cita?.Doctor?.NombreCompleto ?? first.Cita?.Doctor?.Email ?? g.Key ?? "";
                var pacientesDistintos = g.Select(x => x.PacienteId).Distinct().Count();
                return new ProduccionDoctorRow
                {
                    Doctor = nombre,
                    TotalFacturado = g.Sum(x => x.Total),
                    TotalCobrado = g.Sum(x => x.MontoPagado),
                    CantidadPacientesAtendidos = pacientesDistintos
                };
            })
            .OrderByDescending(x => x.TotalFacturado)
            .ToList();

        var procedimientos = await _db.ProcedimientosRealizados
            .Where(pr => pr.Cita!.ClinicaId == cid && pr.Cita.FechaHora >= inicio && pr.Cita.FechaHora < finMasUno && pr.MarcadoRealizado)
            .Include(pr => pr.Tratamiento)
            .ToListAsync();
        if (!string.IsNullOrEmpty(doctorId)) procedimientos = procedimientos.Where(pr => pr.Cita!.DoctorId == doctorId).ToList();
        var tratamientosVendidos = procedimientos
            .GroupBy(pr => pr.Tratamiento?.Nombre ?? "Sin nombre")
            .Select(g => new TratamientoVendidoRow { Tratamiento = g.Key, Cantidad = g.Count(), TotalGenerado = g.Sum(pr => pr.PrecioAplicado) })
            .OrderByDescending(x => x.TotalGenerado)
            .ToList();

        var ordenesCxC = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && (o.Estado == EstadoCobro.Pendiente || o.Estado == EstadoCobro.Parcial))
            .Include(o => o.Paciente)
            .Include(o => o.Pagos)
            .ToListAsync();
        var hoy = DateTime.Today;
        var cuentasPorCobrar = ordenesCxC.Select(o =>
        {
            var ultima = o.Pagos?.Any() == true ? o.Pagos.Max(p => p.FechaPago) : (DateTime?)null;
            var diasVencidos = ultima.HasValue && ultima.Value.Date < hoy ? (int)(hoy - ultima.Value.Date).TotalDays : (int?)null;
            return new CuentaPorCobrarRow
            {
                Paciente = (o.Paciente?.Nombre + " " + (o.Paciente?.Apellidos ?? "")).Trim(),
                TotalPendiente = o.Total - o.MontoPagado,
                UltimaFechaAtencion = ultima,
                DiasVencidos = diasVencidos
            };
        }).ToList();

        return new ReporteFinancieroData
        {
            Encabezado = encabezado,
            Resumen = resumen,
            DetalleIngresos = detalle,
            ProduccionPorDoctor = porDoctor,
            TratamientosMasVendidos = tratamientosVendidos,
            CuentasPorCobrar = cuentasPorCobrar
        };
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

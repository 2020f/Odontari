using System.Security.Claims;
using System.Text.Json;
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
public class ExpedienteController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;
    private readonly HistogramaExportService _histogramaExport;

    public ExpedienteController(ApplicationDbContext db, IClinicaActualService clinicaActual, HistogramaExportService histogramaExport)
    {
        _db = db;
        _clinicaActual = clinicaActual;
        _histogramaExport = histogramaExport;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Vista principal del expediente del paciente con tabs.</summary>
    public async Task<IActionResult> Index(int id, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        ViewBag.CitaId = citaId;
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
        ViewBag.PacienteIdExpediente = id;
        ViewBag.SeccionActivaExpediente = string.Equals(Request.Query["tab"], "historial", StringComparison.OrdinalIgnoreCase) ? "historial" : "resumen";
        return View(vm);
    }

    /// <summary>Histograma: historial clínico + resumen + timeline + resumen odontograma. Filtrado por ClinicaId y PacienteId. Opcional: filtro por rango de fechas.</summary>
    [HttpGet]
    public async Task<IActionResult> Histograma(int id, int? citaId, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        ViewBag.CitaId = citaId;

        var paciente = await _db.Pacientes
            .Include(p => p.Clinica)
            .FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var vm = new HistogramaViewModel
        {
            PacienteId = paciente.Id,
            Nombre = paciente.Nombre,
            Apellidos = paciente.Apellidos,
            Cedula = paciente.Cedula,
            Telefono = paciente.Telefono,
            Email = paciente.Email,
            FechaNacimiento = paciente.FechaNacimiento,
            Alergias = paciente.Alergias,
            NotasClinicas = paciente.NotasClinicas
        };

        // Historia clínica sistemática (para sección Datos del paciente)
        var hcs = await _db.HistoriasClinicasSistematicas
            .FirstOrDefaultAsync(h => h.PacienteId == id && h.ClinicaId == cid);
        if (hcs != null)
        {
            vm.HistoriaClinicaSistematica = new HistoriaClinicaSistematicaResumenViewModel
            {
                TieneDatos = true,
                AlergiasMedicamentos = hcs.AlergiasMedicamentos,
                AlergiasCuales = hcs.AlergiasCuales,
                AsmaBronquial = hcs.AsmaBronquial,
                ConvulsionesEpilepsia = hcs.ConvulsionesEpilepsia,
                Diabetes = hcs.Diabetes,
                EnfermedadesCardiacas = hcs.EnfermedadesCardiacas,
                Embarazo = hcs.Embarazo,
                EmbarazoSemanas = hcs.EmbarazoSemanas,
                EnfermedadesVenereas = hcs.EnfermedadesVenereas,
                FiebreReumatica = hcs.FiebreReumatica,
                Hepatitis = hcs.Hepatitis,
                HepatitisCual = hcs.HepatitisCual,
                ProblemasNeurologicos = hcs.ProblemasNeurologicos,
                ProblemasRenales = hcs.ProblemasRenales,
                ProblemasSinusales = hcs.ProblemasSinusales,
                SangradoExcesivo = hcs.SangradoExcesivo,
                TrastornosPsiquiatricos = hcs.TrastornosPsiquiatricos,
                TrastornosDigestivos = hcs.TrastornosDigestivos,
                TumoresBenignosMalignos = hcs.TumoresBenignosMalignos,
                TumoresCuales = hcs.TumoresCuales,
                TrastornosRespiratorios = hcs.TrastornosRespiratorios,
                TrastornosRespiratoriosCuales = hcs.TrastornosRespiratoriosCuales
            };
        }

        var hoy = DateTime.UtcNow.Date;

        // Última visita (última cita realizada/finalizada)
        var ultimaCita = await _db.Citas
            .Where(c => c.PacienteId == id && c.ClinicaId == cid && (c.Estado == Models.Enums.EstadoCita.Finalizada || c.Estado == Models.Enums.EstadoCita.EnAtencion))
            .OrderByDescending(c => c.FechaHora)
            .Include(c => c.Doctor)
            .FirstOrDefaultAsync();
        if (ultimaCita != null)
            vm.UltimaVisita = $"{ultimaCita.FechaHora:dd/MM/yyyy} — {ultimaCita.Motivo ?? "Consulta"} (Dr. {ultimaCita.Doctor?.NombreCompleto ?? "—"})";

        // Próxima cita (siguiente programada)
        var proximaCita = await _db.Citas
            .Where(c => c.PacienteId == id && c.ClinicaId == cid && c.FechaHora >= hoy && c.Estado != Models.Enums.EstadoCita.Cancelada)
            .OrderBy(c => c.FechaHora)
            .Include(c => c.Doctor)
            .FirstOrDefaultAsync();
        if (proximaCita != null)
            vm.ProximoPaso = $"Próxima cita: {proximaCita.FechaHora:dd/MM/yyyy HH:mm} — {proximaCita.Motivo ?? "—"}";

        // Timeline: HistorialClinico (citas, odontograma, procedimientos), opcionalmente filtrado por fechas
        var query = _db.HistorialClinico
            .Where(h => h.PacienteId == id && h.ClinicaId == cid);
        if (fechaInicio.HasValue)
            query = query.Where(h => h.FechaEvento.Date >= fechaInicio.Value.Date);
        if (fechaFin.HasValue)
            query = query.Where(h => h.FechaEvento.Date <= fechaFin.Value.Date);
        var historial = await query
            .OrderByDescending(h => h.FechaEvento)
            .Take(500)
            .Select(h => new HistorialEventoViewModel
            {
                Id = h.Id,
                CitaId = h.CitaId,
                FechaEvento = h.FechaEvento,
                TipoEvento = h.TipoEvento,
                Descripcion = h.Descripcion
            })
            .ToListAsync();
        vm.Timeline = historial;

        ViewBag.FechaInicio = fechaInicio;
        ViewBag.FechaFin = fechaFin;

        if (historial.Any())
            {
                var ultimoEvento = historial.FirstOrDefault(h => h.TipoEvento?.Contains("odontograma", StringComparison.OrdinalIgnoreCase) == true || h.TipoEvento?.Contains("Procedimiento") == true) ?? historial[0];
                vm.UltimoDiagnostico = ultimoEvento?.Descripcion;
                vm.UltimoDiagnosticoEsPeriodontograma = ultimoEvento?.TipoEvento?.Contains("periodontograma", StringComparison.OrdinalIgnoreCase) == true;
            }

        // Resumen odontograma (según tipo: infantil si edad < 14 años)
        var esInfantil = EsPacienteInfantil(paciente);
        var tipoOdontograma = esInfantil ? TipoOdontograma.Infantil : TipoOdontograma.Adulto;
        var odontograma = await _db.Odontogramas
            .Where(o => o.PacienteId == id && o.ClinicaId == cid && o.TipoOdontograma == tipoOdontograma)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();
        if (odontograma != null)
        {
            vm.ResumenOdontograma = ParseResumenOdontograma(odontograma.EstadoJson);
            vm.ResumenOdontograma.UltimaActualizacion = odontograma.UltimaModificacion;
        }

        ViewBag.Paciente = paciente;
        ViewBag.PacienteIdExpediente = id;
        ViewBag.SeccionActivaExpediente = "histograma";
        return View(vm);
    }

    /// <summary>Exporta el Timeline (histórico) del paciente en el rango de fechas indicado como PDF.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportarHistogramaPdf(int id, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var paciente = await _db.Pacientes
            .FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var query = _db.HistorialClinico
            .Where(h => h.PacienteId == id && h.ClinicaId == cid);
        if (fechaInicio.HasValue)
            query = query.Where(h => h.FechaEvento.Date >= fechaInicio.Value.Date);
        if (fechaFin.HasValue)
            query = query.Where(h => h.FechaEvento.Date <= fechaFin.Value.Date);
        var eventos = await query
            .OrderByDescending(h => h.FechaEvento)
            .Select(h => new HistorialEventoViewModel
            {
                Id = h.Id,
                CitaId = h.CitaId,
                FechaEvento = h.FechaEvento,
                TipoEvento = h.TipoEvento,
                Descripcion = h.Descripcion
            })
            .ToListAsync();

        var pdfBytes = _histogramaExport.GenerateTimelinePdf(
            paciente.Nombre,
            paciente.Apellidos,
            fechaInicio,
            fechaFin,
            eventos);

        var nombreSeguro = string.Join("_", ((paciente.Nombre ?? "") + " " + (paciente.Apellidos ?? "")).Trim().Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(nombreSeguro)) nombreSeguro = "Paciente";
        var rango = fechaInicio.HasValue && fechaFin.HasValue
            ? $"{fechaInicio.Value:yyyy-MM-dd}_{fechaFin.Value:yyyy-MM-dd}"
            : fechaInicio.HasValue ? $"{fechaInicio.Value:yyyy-MM-dd}" : fechaFin.HasValue ? $"hasta_{fechaFin.Value:yyyy-MM-dd}" : "completo";
        var fileName = $"Histograma_Timeline_{nombreSeguro}_{rango}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>Extrae la lista de hallazgos del JSON del odontograma (formato "Diente X (superficie): ESTADO" o "Diente X: ESTADO").</summary>
    private static List<string> GetListaHallazgosFromEstadoJson(string? estadoJson)
    {
        var lista = new List<string>();
        if (string.IsNullOrWhiteSpace(estadoJson)) return lista;
        try
        {
            using var doc = JsonDocument.Parse(estadoJson);
            if (!doc.RootElement.TryGetProperty("teeth", out var teethEl)) return lista;
            foreach (var prop in teethEl.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var num)) continue;
                var tooth = prop.Value;
                var status = tooth.TryGetProperty("status", out var s) ? s.GetString() ?? "NONE" : "NONE";
                if (!string.IsNullOrEmpty(status) && status != "NONE")
                    lista.Add($"Diente {num}: {status}");
                else if (tooth.TryGetProperty("surfaces", out var surfEl))
                    foreach (var sp in surfEl.EnumerateObject())
                        if (sp.Value.GetString() is string v && !string.IsNullOrEmpty(v) && v != "NONE")
                            lista.Add($"Diente {num} ({sp.Name}): {v}");
            }
            lista = lista.OrderBy(x => x).ToList();
        }
        catch { /* ignore */ }
        return lista;
    }

    private static ResumenOdontogramaViewModel ParseResumenOdontograma(string? estadoJson)
    {
        var resumen = new ResumenOdontogramaViewModel();
        if (string.IsNullOrWhiteSpace(estadoJson)) return resumen;
        try
        {
            using var doc = JsonDocument.Parse(estadoJson);
            if (!doc.RootElement.TryGetProperty("teeth", out var teethEl)) return resumen;
            var dientesConHallazgo = new List<int>();
            foreach (var prop in teethEl.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var num)) continue;
                var tooth = prop.Value;
                var status = tooth.TryGetProperty("status", out var s) ? s.GetString() ?? "NONE" : "NONE";
                var estadoPrincipal = status;
                if (string.IsNullOrEmpty(estadoPrincipal) || estadoPrincipal == "NONE")
                    if (tooth.TryGetProperty("surfaces", out var surf))
                        foreach (var sp in surf.EnumerateObject())
                            if (sp.Value.GetString() is string v && v != "NONE") { estadoPrincipal = v; break; }
                if (string.IsNullOrEmpty(estadoPrincipal) || estadoPrincipal == "NONE") continue;
                resumen.TotalHallazgos++;
                dientesConHallazgo.Add(num);
                if (estadoPrincipal == "CARIES") resumen.Caries++;
                else if (estadoPrincipal == "RESTAURACION" || estadoPrincipal == "OBTURACION") resumen.Restauraciones++;
                else if (estadoPrincipal == "AUSENTE" || estadoPrincipal == "EXTRAIDO" || estadoPrincipal == "EXFOLIADO") resumen.Ausentes++;
                else if (estadoPrincipal == "ENDODONCIA") resumen.Endodoncia++;
                else resumen.Otros++;
            }
            resumen.UltimosDientesConHallazgo = dientesConHallazgo.OrderBy(x => x).Take(20).ToList();
            resumen.ListaHallazgos = GetListaHallazgosFromEstadoJson(estadoJson);
        }
        catch { /* ignore parse errors */ }
        return resumen;
    }

    /// <summary>Determina si el paciente se considera infantil para odontograma (edad &lt; 14 años).</summary>
    private static bool EsPacienteInfantil(Paciente? paciente)
    {
        if (paciente?.FechaNacimiento == null) return false;
        var fn = paciente.FechaNacimiento.Value;
        if (fn.Date > DateTime.Today) return false; // Fecha futura = dato inválido, tratar como adulto
        var edadAnios = (DateTime.Today - fn).TotalDays / 365.25;
        return edadAnios >= 0 && edadAnios < 14;
    }

    /// <summary>Odontograma del paciente (adulto 32 dientes o infantil 20 dientes según edad).</summary>
    [HttpGet]
    public async Task<IActionResult> Odontograma(int id, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var esInfantil = EsPacienteInfantil(paciente);
        var tipoOdontograma = esInfantil ? TipoOdontograma.Infantil : TipoOdontograma.Adulto;
        var odontograma = await _db.Odontogramas
            .Where(o => o.PacienteId == id && o.ClinicaId == cid && o.TipoOdontograma == tipoOdontograma)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();

        ViewBag.Paciente = paciente;
        ViewBag.EstadoJson = odontograma?.EstadoJson ?? "{}";
        ViewBag.OdontogramaId = odontograma?.Id;
        ViewBag.PacienteIdExpediente = id;
        ViewBag.SeccionActivaExpediente = "odontograma";
        ViewBag.CitaId = citaId;
        ViewBag.EsInfantil = esInfantil;
        ViewBag.TipoOdontograma = (int)tipoOdontograma;
        return View();
    }

    /// <summary>API: Obtener JSON del odontograma (tipo 0=adulto, 1=infantil; si no se envía o es inválido se infiere por edad del paciente).</summary>
    [HttpGet]
    public async Task<IActionResult> GetOdontogramaJson(int pacienteId, int? tipo)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        if (pacienteId <= 0) return BadRequest();
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == pacienteId);
        if (paciente == null) return NotFound();
        var tipoOdontograma = (tipo.HasValue && (tipo.Value == 0 || tipo.Value == 1))
            ? (TipoOdontograma)tipo.Value
            : (EsPacienteInfantil(paciente) ? TipoOdontograma.Infantil : TipoOdontograma.Adulto);
        var odontograma = await _db.Odontogramas
            .Where(o => o.PacienteId == pacienteId && o.ClinicaId == cid && o.TipoOdontograma == tipoOdontograma)
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
        if (pacienteId <= 0) return BadRequest();

        var estadoJson = request.EstadoJson ?? "{}";
        const int maxEstadoJsonLength = 200_000;
        if (estadoJson.Length > maxEstadoJsonLength) return BadRequest("Estado del odontograma demasiado grande.");

        var tipoOdontograma = request.TipoOdontograma == 1 ? TipoOdontograma.Infantil : TipoOdontograma.Adulto;

        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == pacienteId);
        if (paciente == null) return NotFound();

        var esInfantil = EsPacienteInfantil(paciente);
        if (tipoOdontograma == TipoOdontograma.Infantil && !esInfantil)
            return BadRequest("El paciente no se considera infantil para odontograma temporal.");
        if (tipoOdontograma == TipoOdontograma.Adulto && esInfantil)
            return BadRequest("Para pacientes menores de 14 años use el odontograma infantil.");

        var existente = await _db.Odontogramas
            .Where(o => o.PacienteId == pacienteId && o.ClinicaId == cid && o.TipoOdontograma == tipoOdontograma)
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
                TipoOdontograma = tipoOdontograma,
                EstadoJson = estadoJson,
                FechaRegistro = DateTime.UtcNow,
                UltimaModificacion = DateTime.UtcNow,
                UltimoUsuarioId = UserId
            });
        }

        await _db.SaveChangesAsync();

        // Registrar evento en historial con la lista de hallazgos (cambios realizados) para poder ver "lo que se hizo"
        var listaHallazgos = GetListaHallazgosFromEstadoJson(estadoJson);
        var tituloOdonto = tipoOdontograma == TipoOdontograma.Infantil ? "Odontograma infantil" : "Odontograma";
        var descripcion = listaHallazgos.Count > 0
            ? tituloOdonto + " actualizado" + "\n" + string.Join("\n", listaHallazgos)
            : tituloOdonto + " actualizado";
        var citaIdParaHistorial = request.CitaId;
        _db.HistorialClinico.Add(new HistorialClinico
        {
            PacienteId = pacienteId,
            ClinicaId = cid.Value,
            CitaId = citaIdParaHistorial,
            FechaEvento = DateTime.UtcNow,
            TipoEvento = tipoOdontograma == TipoOdontograma.Infantil ? "Actualización odontograma infantil" : "Actualización odontograma",
            Descripcion = descripcion,
            UsuarioId = UserId
        });
        await _db.SaveChangesAsync();

        // Sincronizar hallazgos a procedimientos de la cita (para cobro), sin precio por defecto
        if (request.CitaId.HasValue && request.CitaId.Value > 0)
        {
            var cita = await _db.Citas
                .Include(c => c.ProcedimientosRealizados)
                .FirstOrDefaultAsync(c => c.ClinicaId == cid && c.Id == request.CitaId.Value && c.PacienteId == pacienteId);
            if (cita != null)
            {
                foreach (var linea in listaHallazgos)
                {
                    var (notasKey, nombreEstado) = ParseHallazgoLinea(linea);
                    if (string.IsNullOrWhiteSpace(nombreEstado)) continue;
                    var nombreTratamiento = NormalizarNombreTratamiento(nombreEstado);
                    var tratamiento = await _db.Tratamientos
                        .FirstOrDefaultAsync(t => t.ClinicaId == cid && t.Nombre.ToLower() == nombreTratamiento.ToLower());
                    if (tratamiento == null)
                    {
                        tratamiento = new Tratamiento
                        {
                            ClinicaId = cid.Value,
                            Nombre = nombreTratamiento,
                            PrecioBase = 0,
                            Activo = true,
                            DuracionMinutos = 30
                        };
                        _db.Tratamientos.Add(tratamiento);
                        await _db.SaveChangesAsync();
                    }
                    var yaExiste = cita.ProcedimientosRealizados.Any(pr => pr.TratamientoId == tratamiento.Id && pr.Notas == notasKey);
                    if (!yaExiste)
                    {
                        _db.ProcedimientosRealizados.Add(new ProcedimientoRealizado
                        {
                            CitaId = cita.Id,
                            TratamientoId = tratamiento.Id,
                            PrecioAplicado = tratamiento.PrecioBase, // Si ya se definió precio en la tabla Tratamiento, se usa; si no, 0
                            MarcadoRealizado = false,
                            Notas = notasKey
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }
        }

        return Ok();
    }

    // ----- Periodontograma (solo historial, no procedimientos para cobro) -----

    /// <summary>Vista del periodontograma del paciente (32 dientes FDI, parámetros periodontales).</summary>
    [HttpGet]
    public async Task<IActionResult> Periodontograma(int id, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var periodontograma = await _db.Periodontogramas
            .Where(o => o.PacienteId == id && o.ClinicaId == cid)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();

        ViewBag.Paciente = paciente;
        ViewBag.EstadoJson = periodontograma?.EstadoJson ?? "{}";
        ViewBag.PeriodontogramaId = periodontograma?.Id;
        ViewBag.PacienteIdExpediente = id;
        ViewBag.SeccionActivaExpediente = "periodontograma";
        ViewBag.CitaId = citaId;
        return View();
    }

    /// <summary>API: Obtener JSON del periodontograma.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPeriodontogramaJson(int pacienteId)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        if (pacienteId <= 0) return BadRequest();
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == pacienteId);
        if (paciente == null) return NotFound();

        var periodontograma = await _db.Periodontogramas
            .Where(o => o.PacienteId == pacienteId && o.ClinicaId == cid)
            .OrderByDescending(o => o.UltimaModificacion)
            .FirstOrDefaultAsync();

        var json = periodontograma?.EstadoJson ?? "{}";
        return Content(json, "application/json");
    }

    /// <summary>API: Guardar periodontograma. Solo se registra en historial del paciente (no se agrega a procedimientos para cobro).</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GuardarPeriodontograma([FromBody] GuardarPeriodontogramaRequest request)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        if (request == null) return BadRequest();

        var pacienteId = request.PacienteId;
        if (pacienteId <= 0) return BadRequest();

        var estadoJson = request.EstadoJson ?? "{}";
        const int maxEstadoJsonLength = 500_000;
        if (estadoJson.Length > maxEstadoJsonLength) return BadRequest("Estado del periodontograma demasiado grande.");

        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == pacienteId);
        if (paciente == null) return NotFound();

        var existente = await _db.Periodontogramas
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
            _db.Periodontogramas.Add(new Periodontograma
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

        var descripcionResumen = GetPeriodontogramaResumenDescripcion(estadoJson);
        _db.HistorialClinico.Add(new HistorialClinico
        {
            PacienteId = pacienteId,
            ClinicaId = cid.Value,
            CitaId = request.CitaId,
            FechaEvento = DateTime.UtcNow,
            TipoEvento = "Actualización periodontograma",
            Descripcion = descripcionResumen,
            UsuarioId = UserId
        });
        await _db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>Extrae lista de hallazgos por diente del JSON del periodontograma (para historial, como en odontograma).</summary>
    private static List<string> GetListaHallazgosFromPeriodontogramaJson(string? estadoJson)
    {
        var lista = new List<string>();
        if (string.IsNullOrWhiteSpace(estadoJson)) return lista;
        try
        {
            using var doc = JsonDocument.Parse(estadoJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("superior", out var supEl) || !root.TryGetProperty("inferior", out var infEl))
                return lista;

            void AddHallazgosArcade(JsonElement arcade)
            {
                foreach (var toothProp in arcade.EnumerateObject())
                {
                    if (!int.TryParse(toothProp.Name, out var numDiente)) continue;
                    var t = toothProp.Value;

                    if (t.TryGetProperty("ausencia", out var aus) && aus.GetBoolean())
                    { lista.Add("Diente " + numDiente + ": Ausente"); continue; }

                    if (t.TryGetProperty("implante", out var imp) && imp.GetBoolean())
                        lista.Add("Diente " + numDiente + ": Implante");

                    if (t.TryGetProperty("movilidad", out var mov) && mov.GetString() is string movVal && movVal != "0")
                        lista.Add("Diente " + numDiente + ": Movilidad " + movVal);

                    if (t.TryGetProperty("pronostico", out var pron) && pron.GetString() is string pronVal && pronVal != "Bueno")
                        lista.Add("Diente " + numDiente + ": Pronóstico " + pronVal);

                    if (t.TryGetProperty("furca", out var furca) && furca.GetString() is string furcaVal && furcaVal != "0")
                        lista.Add("Diente " + numDiente + ": Furca " + furcaVal);

                    var sitiosSangrado = new List<string>();
                    if (t.TryGetProperty("sangrado", out var s))
                    {
                        if (s.TryGetProperty("M", out var m) && m.GetBoolean()) sitiosSangrado.Add("M");
                        if (s.TryGetProperty("C", out var c) && c.GetBoolean()) sitiosSangrado.Add("C");
                        if (s.TryGetProperty("D", out var d) && d.GetBoolean()) sitiosSangrado.Add("D");
                    }
                    if (sitiosSangrado.Count > 0)
                        lista.Add("Diente " + numDiente + ": Sangrado " + string.Join(",", sitiosSangrado));

                    var sitiosSup = new List<string>();
                    if (t.TryGetProperty("supuracion", out var sup))
                    {
                        if (sup.TryGetProperty("M", out var m) && m.GetBoolean()) sitiosSup.Add("M");
                        if (sup.TryGetProperty("C", out var c) && c.GetBoolean()) sitiosSup.Add("C");
                        if (sup.TryGetProperty("D", out var d) && d.GetBoolean()) sitiosSup.Add("D");
                    }
                    if (sitiosSup.Count > 0)
                        lista.Add("Diente " + numDiente + ": Supuración " + string.Join(",", sitiosSup));

                    var sitiosPlaca = new List<string>();
                    if (t.TryGetProperty("placa", out var pl))
                    {
                        if (pl.TryGetProperty("M", out var m) && m.GetBoolean()) sitiosPlaca.Add("M");
                        if (pl.TryGetProperty("C", out var c) && c.GetBoolean()) sitiosPlaca.Add("C");
                        if (pl.TryGetProperty("D", out var d) && d.GetBoolean()) sitiosPlaca.Add("D");
                    }
                    if (sitiosPlaca.Count > 0)
                        lista.Add("Diente " + numDiente + ": Placa " + string.Join(",", sitiosPlaca));

                    if (t.TryGetProperty("sondajeVestibular", out var sV))
                    {
                        var sitios = new List<string>();
                        foreach (var site in new[] { "M", "C", "D" })
                        {
                            if (!sV.TryGetProperty(site, out var v)) continue;
                            if (v.GetString() is string vs && int.TryParse(vs, out var n) && n >= 4)
                                sitios.Add(site + ":" + n + "mm");
                        }
                        if (sitios.Count > 0)
                            lista.Add("Diente " + numDiente + " (V): Prof. sondaje " + string.Join(" ", sitios));
                    }
                    if (t.TryGetProperty("sondajePalatal", out var sP))
                    {
                        var sitios = new List<string>();
                        foreach (var site in new[] { "M", "C", "D" })
                        {
                            if (!sP.TryGetProperty(site, out var v)) continue;
                            if (v.GetString() is string vs && int.TryParse(vs, out var n) && n >= 4)
                                sitios.Add(site + ":" + n + "mm");
                        }
                        if (sitios.Count > 0)
                            lista.Add("Diente " + numDiente + " (P/L): Prof. sondaje " + string.Join(" ", sitios));
                    }

                    if (t.TryGetProperty("margenVestibular", out var mV))
                    {
                        foreach (var site in new[] { "M", "C", "D" })
                        {
                            if (!mV.TryGetProperty(site, out var v)) continue;
                            if (v.GetString() is string vs && int.TryParse(vs, out var n) && n < 0)
                                lista.Add("Diente " + numDiente + " (V): Recesión " + site + " " + n + "mm");
                        }
                    }
                    if (t.TryGetProperty("margenPalatal", out var mP))
                    {
                        foreach (var site in new[] { "M", "C", "D" })
                        {
                            if (!mP.TryGetProperty(site, out var v)) continue;
                            if (v.GetString() is string vs && int.TryParse(vs, out var n) && n < 0)
                                lista.Add("Diente " + numDiente + " (P/L): Recesión " + site + " " + n + "mm");
                        }
                    }
                }
            }

            AddHallazgosArcade(supEl);
            AddHallazgosArcade(infEl);
            lista = lista.OrderBy(x => x).ToList();
        }
        catch { /* ignorar */ }
        return lista;
    }

    /// <summary>Genera texto de resumen para HistorialClinico a partir del JSON del periodontograma (métricas + lista de hallazgos).</summary>
    private static string GetPeriodontogramaResumenDescripcion(string? estadoJson)
    {
        if (string.IsNullOrWhiteSpace(estadoJson)) return "Periodontograma actualizado.";
        try
        {
            using var doc = JsonDocument.Parse(estadoJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("superior", out var supEl) || !root.TryGetProperty("inferior", out var infEl))
                return "Periodontograma actualizado.";

            int sangrado = 0, placa = 0, bolsillos4 = 0, bolsillos6 = 0, ausentes = 0, implantes = 0;

            void CountArcade(JsonElement arcade)
            {
                foreach (var toothProp in arcade.EnumerateObject())
                {
                    var t = toothProp.Value;
                    if (t.TryGetProperty("ausencia", out var aus) && aus.GetBoolean()) ausentes++;
                    if (t.TryGetProperty("implante", out var imp) && imp.GetBoolean()) implantes++;
                    if (t.TryGetProperty("sangrado", out var s))
                    {
                        if (s.TryGetProperty("M", out var m) && m.GetBoolean()) sangrado++;
                        if (s.TryGetProperty("C", out var c) && c.GetBoolean()) sangrado++;
                        if (s.TryGetProperty("D", out var d) && d.GetBoolean()) sangrado++;
                    }
                    if (t.TryGetProperty("placa", out var pl))
                    {
                        if (pl.TryGetProperty("M", out var m) && m.GetBoolean()) placa++;
                        if (pl.TryGetProperty("C", out var c) && c.GetBoolean()) placa++;
                        if (pl.TryGetProperty("D", out var d) && d.GetBoolean()) placa++;
                    }
                    foreach (var key in new[] { "sondajeVestibular", "sondajePalatal" })
                    {
                        if (!t.TryGetProperty(key, out var sond)) continue;
                        foreach (var site in new[] { "M", "C", "D" })
                        {
                            if (!sond.TryGetProperty(site, out var v)) continue;
                            var str = v.GetString();
                            if (string.IsNullOrEmpty(str) || !int.TryParse(str, out var num)) continue;
                            if (num >= 6) bolsillos6++;
                            if (num >= 4) bolsillos4++;
                        }
                    }
                }
            }

            CountArcade(supEl);
            CountArcade(infEl);

            var lines = new List<string> { "Periodontograma actualizado." };
            lines.Add("Sitios con sangrado: " + sangrado);
            lines.Add("Sitios con placa: " + placa);
            lines.Add("Bolsas ≥4 mm: " + bolsillos4);
            lines.Add("Bolsas ≥6 mm: " + bolsillos6);
            lines.Add("Ausencias: " + ausentes);
            lines.Add("Implantes: " + implantes);

            var listaHallazgos = GetListaHallazgosFromPeriodontogramaJson(estadoJson);
            if (listaHallazgos.Count > 0)
            {
                lines.Add("");
                lines.Add("Lista de hallazgos:");
                lines.AddRange(listaHallazgos);
            }
            return string.Join("\n", lines);
        }
        catch { return "Periodontograma actualizado."; }
    }

    /// <summary>Parsea "Diente 11: IMPLANTE" o "Diente 13 (palatino): SELLANTE" en (notasKey, nombreEstado).</summary>
    private static (string notasKey, string nombreEstado) ParseHallazgoLinea(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea)) return ("", "");
        var idx = linea.IndexOf(": ", StringComparison.Ordinal);
        if (idx <= 0) return ("", "");
        var notasKey = linea[..idx].Trim();
        var nombreEstado = linea[(idx + 2)..].Trim();
        return (notasKey, nombreEstado);
    }

    private static string NormalizarNombreTratamiento(string estado)
    {
        if (string.IsNullOrWhiteSpace(estado)) return estado;
        if (estado.Length == 1) return estado.ToUpperInvariant();
        return char.ToUpperInvariant(estado[0]) + estado[1..].ToLowerInvariant();
    }

    /// <summary>Ver detalle de un evento del timeline (cuando no está asociado a una cita).</summary>
    [HttpGet]
    public async Task<IActionResult> VerEvento(int id, int pacienteId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var ev = await _db.HistorialClinico
            .FirstOrDefaultAsync(h => h.Id == id && h.ClinicaId == cid && h.PacienteId == pacienteId);
        if (ev == null) return NotFound();
        ViewBag.PacienteId = pacienteId;
        ViewBag.FechaEvento = ev.FechaEvento;
        ViewBag.TipoEvento = ev.TipoEvento;
        var desc = ev.Descripcion ?? "—";
        ViewBag.Descripcion = desc;
        // Si la descripción tiene varias líneas (ej. "Odontograma actualizado" + lista de hallazgos), pasamos las líneas para mostrar el detalle como en Resumen odontograma
        var lineas = desc.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        ViewBag.DescripcionLineas = lineas;
        return View();
    }

    /// <summary>Historia Clínica Sistemática (20 preguntas).</summary>
    [HttpGet]
    public async Task<IActionResult> HistoriaClinicaSistematica(int id, int? citaId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var paciente = await _db.Pacientes.FirstOrDefaultAsync(p => p.ClinicaId == cid && p.Id == id);
        if (paciente == null) return NotFound();

        var hcs = await _db.HistoriasClinicasSistematicas
            .FirstOrDefaultAsync(h => h.PacienteId == id && h.ClinicaId == cid);

        var vm = hcs != null ? HistoriaClinicaSistematicaViewModel.FromEntity(hcs) : new HistoriaClinicaSistematicaViewModel { PacienteId = id };
        ViewBag.Paciente = paciente;
        ViewBag.PacienteIdExpediente = id;
        ViewBag.SeccionActivaExpediente = "hcs";
        ViewBag.CitaId = citaId;
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

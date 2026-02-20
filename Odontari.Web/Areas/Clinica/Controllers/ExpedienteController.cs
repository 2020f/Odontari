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

    public ExpedienteController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
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

    /// <summary>Histograma: historial clínico + resumen + timeline + resumen odontograma. Filtrado por ClinicaId y PacienteId.</summary>
    [HttpGet]
    public async Task<IActionResult> Histograma(int id, int? citaId)
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

        // Timeline: HistorialClinico (citas, odontograma, procedimientos)
        var historial = await _db.HistorialClinico
            .Where(h => h.PacienteId == id && h.ClinicaId == cid)
            .OrderByDescending(h => h.FechaEvento)
            .Take(100)
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

        if (historial.Any())
            vm.UltimoDiagnostico = historial.FirstOrDefault(h => h.TipoEvento?.Contains("odontograma", StringComparison.OrdinalIgnoreCase) == true || h.TipoEvento?.Contains("Procedimiento") == true)?.Descripcion ?? historial[0].Descripcion;

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

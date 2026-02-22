using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;
using System.Security.Claims;

namespace Odontari.Web.Areas.Clinica.Controllers;

[Authorize(Roles = OdontariRoles.AdminClinica + "," + OdontariRoles.Recepcion + "," + OdontariRoles.Finanzas)]
[Area("Clinica")]
public class CajaController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;
    private readonly IFacturaService _facturaService;
    private readonly FacturaPdfService _facturaPdfService;
    private readonly HistorialPagosPdfService _historialPagosPdfService;

    public CajaController(ApplicationDbContext db, IClinicaActualService clinicaActual, IFacturaService facturaService, FacturaPdfService facturaPdfService, HistorialPagosPdfService historialPagosPdfService)
    {
        _db = db;
        _clinicaActual = clinicaActual;
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
        _historialPagosPdfService = historialPagosPdfService;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var ordenes = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && (o.Estado == EstadoCobro.Pendiente || o.Estado == EstadoCobro.Parcial))
            .Include(o => o.Paciente)
            .Include(o => o.Factura)
            .OrderByDescending(o => o.CreadoAt)
            .ToListAsync();
        var list = ordenes.Select(o => new OrdenCobroListViewModel
        {
            Id = o.Id,
            PacienteId = o.PacienteId,
            PacienteNombre = o.Paciente.Nombre + " " + (o.Paciente.Apellidos ?? ""),
            Total = o.Total,
            MontoPagado = o.MontoPagado,
            Estado = o.Estado,
            CreadoAt = o.CreadoAt,
            CitaId = o.CitaId,
            FacturaId = o.Factura?.Id
        }).ToList();
        return View(list);
    }

    public async Task<IActionResult> Cobrar(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var orden = await _db.OrdenesCobro.Include(o => o.Paciente).FirstOrDefaultAsync(o => o.ClinicaId == cid && o.Id == id);
        if (orden == null) return NotFound();
        var saldo = orden.Total - orden.MontoPagado;
        ViewBag.Orden = orden;
        var procedimientos = new List<(string Nombre, string? Notas, decimal Precio)>();
        if (orden.CitaId.HasValue)
        {
            var lista = await _db.ProcedimientosRealizados
                .Where(pr => pr.CitaId == orden.CitaId.Value && pr.MarcadoRealizado)
                .Include(pr => pr.Tratamiento)
                .OrderBy(pr => pr.Tratamiento != null ? pr.Tratamiento.Nombre : "")
                .Select(pr => new { pr.Tratamiento!.Nombre, pr.Notas, pr.PrecioAplicado })
                .ToListAsync();
            procedimientos = lista.Select(p => (p.Nombre, p.Notas, Precio: p.PrecioAplicado)).ToList();
        }
        ViewBag.Procedimientos = procedimientos;
        ViewBag.TotalProcedimientos = procedimientos.Sum(p => p.Precio);
        return View(new PagoRegistroViewModel { OrdenCobroId = id, SaldoPendiente = saldo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cobrar(int id, PagoRegistroViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return Unauthorized();
        var orden = await _db.OrdenesCobro.FirstOrDefaultAsync(o => o.ClinicaId == cid && o.Id == id);
        if (orden == null) return NotFound();
        if (vm.Monto <= 0) { ModelState.AddModelError("Monto", "El monto debe ser mayor que 0."); ViewBag.Orden = orden; return View(vm); }
        var saldo = orden.Total - orden.MontoPagado;
        if (vm.Monto > saldo) { ModelState.AddModelError("Monto", "El monto no puede superar el saldo pendiente."); ViewBag.Orden = orden; vm.SaldoPendiente = saldo; return View(vm); }
        _db.Pagos.Add(new Pago
        {
            OrdenCobroId = id,
            Monto = vm.Monto,
            FechaPago = DateTime.Now,
            MetodoPago = vm.MetodoPago,
            Referencia = vm.Referencia,
            RegistradoPorUserId = UserId
        });
        orden.MontoPagado += vm.Monto;
        orden.Estado = orden.MontoPagado >= orden.Total ? EstadoCobro.Pagado : EstadoCobro.Parcial;
        await _db.SaveChangesAsync();

        var facturaId = await _facturaService.CrearFacturaSiNoExisteAsync(id, vm.MetodoPago, UserId);
        if (facturaId.HasValue)
            TempData["FacturaId"] = facturaId.Value;
        TempData["CajaMsg"] = "Pago registrado correctamente." + (facturaId.HasValue ? " Puede descargar la factura en esta página o en Historial de pagos." : "");

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Descarga la factura en PDF. id = FacturaId.</summary>
    public async Task<IActionResult> DescargarFacturaPdf(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var factura = await _db.Facturas
            .Include(f => f.Clinica)
            .Include(f => f.Paciente)
            .Include(f => f.OrdenCobro).ThenInclude(o => o!.Cita)
            .FirstOrDefaultAsync(f => f.Id == id && f.ClinicaId == cid);
        if (factura == null) return NotFound();

        var lineas = new List<FacturaLineaPdf>();
        if (factura.OrdenCobro?.CitaId.HasValue == true)
        {
            var procs = await _db.ProcedimientosRealizados
                .Where(pr => pr.CitaId == factura.OrdenCobro.CitaId && pr.MarcadoRealizado)
                .Include(pr => pr.Tratamiento)
                .ToListAsync();
            foreach (var pr in procs)
            {
                var desc = (pr.Tratamiento?.Nombre ?? "Tratamiento") + (string.IsNullOrEmpty(pr.Notas) ? "" : $" ({pr.Notas})");
                var precio = pr.PrecioAplicado;
                lineas.Add(new FacturaLineaPdf { Descripcion = desc, Cantidad = 1, PrecioUnitario = precio, Subtotal = precio });
            }
        }
        if (lineas.Count == 0)
            lineas.Add(new FacturaLineaPdf { Descripcion = "Servicios de la orden de cobro", Cantidad = 1, PrecioUnitario = factura.Total, Subtotal = factura.Total });

        var data = new FacturaPdfData
        {
            RazonSocial = factura.Clinica.RazonSocial ?? factura.Clinica.Nombre,
            RNC = factura.Clinica.RNC,
            DireccionFiscal = factura.Clinica.DireccionFiscal ?? factura.Clinica.Direccion,
            Telefono = factura.Clinica.Telefono,
            Email = factura.Clinica.Email,
            NumeroInterno = factura.NumeroInterno,
            NCF = factura.NCF,
            EsFiscal = factura.TipoDocumento == TipoDocumentoFactura.Fiscal,
            FechaEmision = factura.FechaEmision,
            ClienteNombre = factura.Paciente.Nombre + " " + (factura.Paciente.Apellidos ?? ""),
            ClienteRNC = null,
            ClienteCedula = factura.Paciente.Cedula,
            Lineas = lineas,
            Subtotal = factura.Subtotal,
            Itbis = factura.Itbis,
            Total = factura.Total,
            FormaPago = factura.FormaPago,
            MensajeFactura = factura.Clinica.MensajeFactura,
            CondicionesPago = factura.Clinica.CondicionesPago
        };
        var pdf = _facturaPdfService.GeneratePdf(data);
        return File(pdf, "application/pdf", $"Factura-{factura.NumeroInterno}.pdf");
    }

    public async Task<IActionResult> HistorialPagos(DateTime? desde, DateTime? hasta, string? metodoPago, string? pacienteTexto, int pagina = 1)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var queryPagos = _db.Pagos
            .Where(p => p.OrdenCobro.ClinicaId == cid)
            .Include(p => p.OrdenCobro).ThenInclude(o => o!.Paciente)
            .Include(p => p.OrdenCobro).ThenInclude(o => o!.Factura)
            .AsQueryable();

        if (desde.HasValue) queryPagos = queryPagos.Where(p => p.FechaPago >= desde.Value);
        if (hasta.HasValue) queryPagos = queryPagos.Where(p => p.FechaPago < hasta.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(metodoPago)) queryPagos = queryPagos.Where(p => p.MetodoPago == metodoPago);
        if (!string.IsNullOrWhiteSpace(pacienteTexto))
        {
            var txt = pacienteTexto.Trim();
            queryPagos = queryPagos.Where(p => (p.OrdenCobro.Paciente.Nombre + " " + (p.OrdenCobro.Paciente.Apellidos ?? "")).Contains(txt));
        }

        const int porPagina = 25;
        var total = await queryPagos.CountAsync();
        var pagos = await queryPagos
            .OrderByDescending(p => p.FechaPago)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(p => new HistorialPagoItemViewModel
            {
                PagoId = p.Id,
                FechaPago = p.FechaPago,
                PacienteNombre = p.OrdenCobro.Paciente.Nombre + " " + (p.OrdenCobro.Paciente.Apellidos ?? ""),
                OrdenCobroId = p.OrdenCobroId,
                OrdenTotal = p.OrdenCobro.Total,
                Monto = p.Monto,
                MetodoPago = p.MetodoPago,
                Referencia = p.Referencia,
                FacturaId = p.OrdenCobro.Factura != null ? p.OrdenCobro.Factura.Id : (int?)null
            })
            .ToListAsync();

        ViewBag.Lista = pagos;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)porPagina);
        ViewBag.Total = total;
        ViewBag.Desde = desde;
        ViewBag.Hasta = hasta;
        ViewBag.MetodoPago = metodoPago;
        ViewBag.PacienteTexto = pacienteTexto;
        return View();
    }

    public async Task<IActionResult> ExportarHistorialPagosPdf(DateTime? desde, DateTime? hasta, string? metodoPago, string? pacienteTexto)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var queryPagos = _db.Pagos
            .Where(p => p.OrdenCobro.ClinicaId == cid)
            .Include(p => p.OrdenCobro).ThenInclude(o => o!.Paciente)
            .Include(p => p.OrdenCobro).ThenInclude(o => o!.Factura)
            .AsQueryable();
        if (desde.HasValue) queryPagos = queryPagos.Where(p => p.FechaPago >= desde.Value);
        if (hasta.HasValue) queryPagos = queryPagos.Where(p => p.FechaPago < hasta.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(metodoPago)) queryPagos = queryPagos.Where(p => p.MetodoPago == metodoPago);
        if (!string.IsNullOrWhiteSpace(pacienteTexto))
        {
            var txt = pacienteTexto.Trim();
            queryPagos = queryPagos.Where(p => (p.OrdenCobro.Paciente.Nombre + " " + (p.OrdenCobro.Paciente.Apellidos ?? "")).Contains(txt));
        }

        var items = await queryPagos
            .OrderByDescending(p => p.FechaPago)
            .Select(p => new HistorialPagoItemViewModel
            {
                PagoId = p.Id,
                FechaPago = p.FechaPago,
                PacienteNombre = p.OrdenCobro.Paciente.Nombre + " " + (p.OrdenCobro.Paciente.Apellidos ?? ""),
                OrdenCobroId = p.OrdenCobroId,
                OrdenTotal = p.OrdenCobro.Total,
                Monto = p.Monto,
                MetodoPago = p.MetodoPago,
                Referencia = p.Referencia,
                FacturaId = p.OrdenCobro.Factura != null ? p.OrdenCobro.Factura.Id : (int?)null
            })
            .ToListAsync();

        var pdf = _historialPagosPdfService.GeneratePdf(items, desde, hasta, metodoPago);
        return File(pdf, "application/pdf", "HistorialPagos.pdf");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Areas.Clinica.Controllers;

/// <summary>Configuración de factura y NCF. Solo AdminClinica.</summary>
[Authorize(Roles = OdontariRoles.AdminClinica)]
[Area("Clinica")]
public class ConfiguracionFacturaController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IClinicaActualService _clinicaActual;

    public ConfiguracionFacturaController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();

    /// <summary>Landing: Facturación y NCF con 3 pestañas.</summary>
    public async Task<IActionResult> Index(string tab)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var clinica = await _db.Clinicas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cid);
        if (clinica == null) return NotFound();

        ViewBag.TabActivo = string.IsNullOrEmpty(tab) ? "fiscales" : tab;
        ViewBag.Clinica = clinica;
        return View();
    }

    // ----- 1) Datos fiscales -----
    [HttpGet]
    public async Task<IActionResult> DatosFiscales()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var c = await _db.Clinicas.FirstOrDefaultAsync(x => x.Id == cid);
        if (c == null) return NotFound();

        var vm = new DatosFiscalesViewModel
        {
            RNC = c.RNC,
            RazonSocial = c.RazonSocial,
            NombreComercial = c.NombreComercial,
            DireccionFiscal = c.DireccionFiscal ?? c.Direccion,
            Telefono = c.Telefono,
            Email = c.Email
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatosFiscales(DatosFiscalesViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var c = await _db.Clinicas.FirstOrDefaultAsync(x => x.Id == cid);
        if (c == null) return NotFound();

        c.RNC = vm.RNC?.Trim();
        c.RazonSocial = vm.RazonSocial?.Trim();
        c.NombreComercial = vm.NombreComercial?.Trim();
        c.DireccionFiscal = vm.DireccionFiscal?.Trim();
        c.Telefono = vm.Telefono?.Trim();
        c.Email = vm.Email?.Trim();
        await _db.SaveChangesAsync();
        TempData["ConfigFacturaMsg"] = "Datos fiscales guardados correctamente.";
        return RedirectToAction(nameof(Index), new { tab = "fiscales" });
    }

    // ----- 2) Modo facturación + impuestos + formato + formas de pago -----
    [HttpGet]
    public async Task<IActionResult> ModoFacturacion()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var c = await _db.Clinicas.FirstOrDefaultAsync(x => x.Id == cid);
        if (c == null) return NotFound();

        ViewBag.Modo = new ModoFacturacionViewModel
        {
            ModoFacturacion = c.ModoFacturacion,
            PermitirInternaConFiscal = c.PermitirInternaConFiscal
        };
        ViewBag.ImpuestosFormato = new ConfiguracionImpuestosFormatoViewModel
        {
            ItbisTasa = c.ItbisTasa,
            ItbisAplicarPorDefecto = c.ItbisAplicarPorDefecto,
            MensajeFactura = c.MensajeFactura,
            CondicionesPago = c.CondicionesPago,
            NotaLegal = c.NotaLegal,
            MostrarFirma = c.MostrarFirma,
            MostrarQR = c.MostrarQR
        };
        ViewBag.FormasPago = new FormasPagoViewModel
        {
            FormaPagoEfectivo = c.FormaPagoEfectivo,
            FormaPagoTransferencia = c.FormaPagoTransferencia,
            FormaPagoTarjeta = c.FormaPagoTarjeta,
            FormaPagoCredito = c.FormaPagoCredito,
            FormaPagoMixto = c.FormaPagoMixto
        };
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModoFacturacion(ModoFacturacionViewModel modo, ConfiguracionImpuestosFormatoViewModel impuestos, FormasPagoViewModel formasPago)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var c = await _db.Clinicas.FirstOrDefaultAsync(x => x.Id == cid);
        if (c == null) return NotFound();

        c.ModoFacturacion = modo.ModoFacturacion;
        c.PermitirInternaConFiscal = modo.PermitirInternaConFiscal;
        c.ItbisTasa = impuestos.ItbisTasa;
        c.ItbisAplicarPorDefecto = impuestos.ItbisAplicarPorDefecto;
        c.MensajeFactura = impuestos.MensajeFactura?.Trim();
        c.CondicionesPago = impuestos.CondicionesPago?.Trim();
        c.NotaLegal = impuestos.NotaLegal?.Trim();
        c.MostrarFirma = impuestos.MostrarFirma;
        c.MostrarQR = impuestos.MostrarQR;
        c.FormaPagoEfectivo = formasPago.FormaPagoEfectivo;
        c.FormaPagoTransferencia = formasPago.FormaPagoTransferencia;
        c.FormaPagoTarjeta = formasPago.FormaPagoTarjeta;
        c.FormaPagoCredito = formasPago.FormaPagoCredito;
        c.FormaPagoMixto = formasPago.FormaPagoMixto;
        await _db.SaveChangesAsync();
        TempData["ConfigFacturaMsg"] = "Configuración de facturación guardada.";
        return RedirectToAction(nameof(Index), new { tab = "modo" });
    }

    // ----- 3) NCF Rangos -----
    [HttpGet]
    public async Task<IActionResult> NCFRangos()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var rangos = await _db.NCFRangos
            .Where(r => r.ClinicaId == cid)
            .Include(r => r.NCFTipo)
            .OrderBy(r => r.NCFTipo!.Codigo)
            .Select(r => new NCFRangoItemViewModel
            {
                Id = r.Id,
                TipoCodigo = r.NCFTipo!.Codigo,
                TipoNombre = r.NCFTipo.Nombre,
                Desde = r.Desde,
                Hasta = r.Hasta,
                Proximo = r.Proximo,
                Estado = r.Estado.ToString(),
                FechaVencimiento = r.FechaVencimiento,
                PorcentajeConsumido = 0
            })
            .ToListAsync();

        foreach (var r in rangos)
        {
            if (long.TryParse(r.Desde, out var d) && long.TryParse(r.Hasta, out var h) && h >= d)
                r.PorcentajeConsumido = (int)Math.Round((r.Proximo - d) * 100.0 / (h - d + 1));
        }

        ViewBag.Rangos = rangos;
        ViewBag.TiposNCF = await _db.NCFTipos.Where(t => t.Activo).OrderBy(t => t.Codigo).ToListAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CrearRango()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        ViewBag.TiposNCF = await _db.NCFTipos.Where(t => t.Activo).OrderBy(t => t.Codigo).ToListAsync();
        return View(new NCFRangoEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearRango(NCFRangoEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        if (string.IsNullOrWhiteSpace(vm.Desde) || string.IsNullOrWhiteSpace(vm.Hasta))
        {
            ModelState.AddModelError("", "Desde y Hasta son obligatorios.");
            ViewBag.TiposNCF = await _db.NCFTipos.Where(t => t.Activo).OrderBy(t => t.Codigo).ToListAsync();
            return View(vm);
        }

        long desde, hasta;
        if (!long.TryParse(vm.Desde.Trim(), out desde) || !long.TryParse(vm.Hasta.Trim(), out hasta) || desde > hasta)
        {
            ModelState.AddModelError("", "Desde y Hasta deben ser números válidos (Desde ≤ Hasta).");
            ViewBag.TiposNCF = await _db.NCFTipos.Where(t => t.Activo).OrderBy(t => t.Codigo).ToListAsync();
            return View(vm);
        }

        var rango = new NCFRango
        {
            ClinicaId = cid.Value,
            NCFTipoId = vm.NCFTipoId,
            SeriePrefijo = vm.SeriePrefijo?.Trim(),
            Desde = vm.Desde.Trim(),
            Hasta = vm.Hasta.Trim(),
            Proximo = desde,
            FechaAutorizacion = vm.FechaAutorizacion,
            FechaVencimiento = vm.FechaVencimiento,
            Nota = vm.Nota?.Trim(),
            Estado = EstadoNCFRango.Activo,
            Fuente = "Manual",
            CreadoAt = DateTime.UtcNow
        };
        _db.NCFRangos.Add(rango);
        await _db.SaveChangesAsync();
        TempData["ConfigFacturaMsg"] = "Rango NCF creado correctamente.";
        return RedirectToAction(nameof(NCFRangos));
    }

    [HttpGet]
    public async Task<IActionResult> EditarRango(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var r = await _db.NCFRangos.FirstOrDefaultAsync(x => x.Id == id && x.ClinicaId == cid);
        if (r == null) return NotFound();

        var vm = new NCFRangoEditViewModel
        {
            Id = r.Id,
            NCFTipoId = r.NCFTipoId,
            SeriePrefijo = r.SeriePrefijo,
            Desde = r.Desde,
            Hasta = r.Hasta,
            FechaAutorizacion = r.FechaAutorizacion,
            FechaVencimiento = r.FechaVencimiento,
            Nota = r.Nota
        };
        ViewBag.TiposNCF = await _db.NCFTipos.Where(t => t.Activo).OrderBy(t => t.Codigo).ToListAsync();
        ViewBag.ProximoActual = r.Proximo;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarRango(NCFRangoEditViewModel vm)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var r = await _db.NCFRangos.FirstOrDefaultAsync(x => x.Id == vm.Id && x.ClinicaId == cid);
        if (r == null) return NotFound();

        r.SeriePrefijo = vm.SeriePrefijo?.Trim();
        r.FechaAutorizacion = vm.FechaAutorizacion;
        r.FechaVencimiento = vm.FechaVencimiento;
        r.Nota = vm.Nota?.Trim();
        await _db.SaveChangesAsync();
        TempData["ConfigFacturaMsg"] = "Rango NCF actualizado.";
        return RedirectToAction(nameof(NCFRangos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PausarRango(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return NotFound();

        var r = await _db.NCFRangos.FirstOrDefaultAsync(x => x.Id == id && x.ClinicaId == cid);
        if (r == null) return NotFound();

        r.Estado = EstadoNCFRango.Pausado;
        await _db.SaveChangesAsync();
        TempData["ConfigFacturaMsg"] = "Rango pausado.";
        return RedirectToAction(nameof(NCFRangos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarRango(int id)
    {
        var cid = ClinicaId;
        if (cid == null) return NotFound();

        var r = await _db.NCFRangos.FirstOrDefaultAsync(x => x.Id == id && x.ClinicaId == cid);
        if (r == null) return NotFound();

        r.Estado = EstadoNCFRango.Activo;
        await _db.SaveChangesAsync();
        TempData["ConfigFacturaMsg"] = "Rango activado.";
        return RedirectToAction(nameof(NCFRangos));
    }

    // ----- Bitácora NCF -----
    [HttpGet]
    public async Task<IActionResult> Bitacora(string? ncf, int? facturaId, DateTime? desde, DateTime? hasta, int pagina = 1)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        const int porPagina = 20;
        var query = _db.NCFMovimientos
            .Where(m => m.ClinicaId == cid)
            .Include(m => m.NCFTipo)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(ncf))
            query = query.Where(m => m.NCFGenerado.Contains(ncf));
        if (facturaId.HasValue)
            query = query.Where(m => m.FacturaId == facturaId);
        if (desde.HasValue)
            query = query.Where(m => m.FechaHora >= desde.Value);
        if (hasta.HasValue)
            query = query.Where(m => m.FechaHora < hasta.Value.AddDays(1));

        var total = await query.CountAsync();
        var lista = await query
            .OrderByDescending(m => m.FechaHora)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(m => new NCFBitacoraItemViewModel
            {
                Id = m.Id,
                NCFGenerado = m.NCFGenerado,
                FacturaId = m.FacturaId,
                Estado = m.Estado.ToString(),
                FechaHora = m.FechaHora,
                Motivo = m.Motivo,
                TipoCodigo = m.NCFTipo!.Codigo,
                UsuarioNombre = m.Usuario != null ? m.Usuario.NombreCompleto : null
            })
            .ToListAsync();

        ViewBag.Lista = lista;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)porPagina);
        ViewBag.Total = total;
        ViewBag.FiltroNcf = ncf;
        ViewBag.FiltroFacturaId = facturaId;
        ViewBag.FiltroDesde = desde;
        ViewBag.FiltroHasta = hasta;
        return View();
    }
}

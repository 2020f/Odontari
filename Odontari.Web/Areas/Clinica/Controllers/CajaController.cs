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

    public CajaController(ApplicationDbContext db, IClinicaActualService clinicaActual)
    {
        _db = db;
        _clinicaActual = clinicaActual;
    }

    private int? ClinicaId => _clinicaActual.GetClinicaIdActual();
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index()
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var list = await _db.OrdenesCobro
            .Where(o => o.ClinicaId == cid && (o.Estado == EstadoCobro.Pendiente || o.Estado == EstadoCobro.Parcial))
            .Include(o => o.Paciente)
            .OrderByDescending(o => o.CreadoAt)
            .Select(o => new OrdenCobroListViewModel
            {
                Id = o.Id,
                PacienteId = o.PacienteId,
                PacienteNombre = o.Paciente.Nombre + " " + (o.Paciente.Apellidos ?? ""),
                Total = o.Total,
                MontoPagado = o.MontoPagado,
                Estado = o.Estado,
                CreadoAt = o.CreadoAt,
                CitaId = o.CitaId
            })
            .ToListAsync();
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
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> HistorialPagos(int? pacienteId)
    {
        var cid = ClinicaId;
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });
        var query = _db.OrdenesCobro.Where(o => o.ClinicaId == cid).Include(o => o.Paciente).Include(o => o.Pagos).AsQueryable();
        if (pacienteId.HasValue) query = query.Where(o => o.PacienteId == pacienteId);
        var list = await query.OrderByDescending(o => o.CreadoAt).Take(100).ToListAsync();
        return View(list);
    }
}

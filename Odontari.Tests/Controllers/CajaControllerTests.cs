using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Odontari.Tests.Helpers;
using Odontari.Web.Areas.Clinica.Controllers;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;
using System.Security.Claims;
using Xunit;

namespace Odontari.Tests.Controllers;

public class CajaControllerTests : IDisposable
{
    private readonly DbContextFactory _factory;
    private readonly Mock<IFacturaService> _facturaServiceMock;

    public CajaControllerTests()
    {
        _factory = new DbContextFactory();
        _facturaServiceMock = new Mock<IFacturaService>();
        // Por defecto devuelve null (sin factura creada) — tests que necesiten
        // factura lo sobreescriben con Setup explícito.
        _facturaServiceMock
            .Setup(s => s.CrearFacturaSiNoExisteAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);
    }

    public void Dispose() => _factory.Dispose();

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    private CajaController BuildController(ApplicationDbContext db, int clinicaId, string userId)
    {
        var mockClinicaService = new Mock<IClinicaActualService>();
        mockClinicaService.Setup(s => s.GetClinicaIdActual()).Returns(clinicaId);
        mockClinicaService.Setup(s => s.GetClinicaIdActualAsync()).ReturnsAsync(clinicaId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "Test Recepcion"),
            new(ClaimTypes.Role, OdontariRoles.Recepcion)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var httpContext = new DefaultHttpContext { User = principal };

        return new CajaController(
            db,
            mockClinicaService.Object,
            _facturaServiceMock.Object,
            null!,   // FacturaPdfService — no se usa en Cobrar
            null!    // HistorialPagosPdfService — no se usa en Cobrar
        )
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private async Task<(Clinica clinica, Paciente paciente, OrdenCobro orden)> SeedPendingOrdenAsync(decimal total = 1500m)
    {
        await using var db = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(db);
        var clinica = await SeedHelper.CrearClinicaAsync(db, plan.Id);
        var doctor = await SeedHelper.CrearUsuarioAsync(db);
        var paciente = await SeedHelper.CrearPacienteAsync(db, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(db, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(db, clinica.Id, paciente.Id, cita.Id, total);
        return (clinica, paciente, orden);
    }

    // -----------------------------------------------------------------------
    // COBRAR GET — aislamiento de tenant
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Cobrar_GET_should_return_not_found_when_orden_belongs_to_other_clinica()
    {
        // Arrange — orden en clinica2, usuario autenticado en clinica1
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica1 = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var clinica2 = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica2.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica2.Id, paciente.Id, doctor.Id);
        var ordenAjena = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica2.Id, paciente.Id, cita.Id);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica1.Id, Guid.NewGuid().ToString());

        // Act — intento cross-tenant
        var result = await controller.Cobrar(ordenAjena.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // -----------------------------------------------------------------------
    // COBRAR POST — validaciones de monto
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Cobrar_POST_should_return_view_with_error_when_monto_is_zero()
    {
        // Arrange
        var (clinica, _, orden) = await SeedPendingOrdenAsync();
        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 0 };

        // Act
        var result = await controller.Cobrar(orden.Id, vm) as ViewResult;

        // Assert — devuelve la vista con error de modelo, sin crear pago
        Assert.NotNull(result);
        Assert.False(controller.ModelState.IsValid);
        await using var dbCheck = _factory.CreateContext();
        Assert.Equal(0, await dbCheck.Pagos.CountAsync());
    }

    [Fact]
    public async Task Cobrar_POST_should_return_view_with_error_when_monto_is_negative()
    {
        // Arrange
        var (clinica, _, orden) = await SeedPendingOrdenAsync();
        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = -50 };

        // Act
        var result = await controller.Cobrar(orden.Id, vm) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Cobrar_POST_should_return_view_with_error_when_monto_exceeds_saldo()
    {
        // Arrange — orden de 1500, intento pagar 2000
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1500m);
        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 2000m };

        // Act
        var result = await controller.Cobrar(orden.Id, vm) as ViewResult;

        // Assert — rechaza overpago
        Assert.NotNull(result);
        Assert.False(controller.ModelState.IsValid);
        await using var dbCheck = _factory.CreateContext();
        Assert.Equal(0, await dbCheck.Pagos.CountAsync());
    }

    // -----------------------------------------------------------------------
    // COBRAR POST — happy path: pago completo
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Cobrar_POST_should_register_pago_and_set_estado_pagado_on_full_payment()
    {
        // Arrange
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1500m);
        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 1500m, MetodoPago = "Efectivo" };

        // Act
        var result = await controller.Cobrar(orden.Id, vm);

        // Assert — redirige (pago exitoso)
        Assert.IsType<RedirectToActionResult>(result);

        await using var dbCheck = _factory.CreateContext();

        // Pago registrado
        var pago = await dbCheck.Pagos.FirstOrDefaultAsync(p => p.OrdenCobroId == orden.Id);
        Assert.NotNull(pago);
        Assert.Equal(1500m, pago!.Monto);
        Assert.Equal("Efectivo", pago.MetodoPago);

        // OrdenCobro marcada como Pagado
        var ordenCheck = await dbCheck.OrdenesCobro.FindAsync(orden.Id);
        Assert.Equal(EstadoCobro.Pagado, ordenCheck!.Estado);
        Assert.Equal(1500m, ordenCheck.MontoPagado);
    }

    // -----------------------------------------------------------------------
    // COBRAR POST — pago parcial
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Cobrar_POST_should_set_estado_parcial_on_partial_payment()
    {
        // Arrange — pagar 500 de 1500
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1500m);
        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 500m };

        // Act
        await controller.Cobrar(orden.Id, vm);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var ordenCheck = await dbCheck.OrdenesCobro.FindAsync(orden.Id);
        Assert.Equal(EstadoCobro.Parcial, ordenCheck!.Estado);
        Assert.Equal(500m, ordenCheck.MontoPagado);
    }

    [Fact]
    public async Task Cobrar_POST_should_accumulate_montopagado_across_partial_payments()
    {
        // Arrange — dos pagos parciales que sumen el total
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1000m);
        var userId = Guid.NewGuid().ToString();

        // Primer pago: 600
        await using var db1 = _factory.CreateContext();
        await BuildController(db1, clinica.Id, userId)
            .Cobrar(orden.Id, new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 600m });

        // Segundo pago: 400 (completa el saldo)
        await using var db2 = _factory.CreateContext();
        await BuildController(db2, clinica.Id, userId)
            .Cobrar(orden.Id, new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 400m });

        // Assert — estado final Pagado, MontoPagado = 1000
        await using var dbCheck = _factory.CreateContext();
        var ordenCheck = await dbCheck.OrdenesCobro.FindAsync(orden.Id);
        Assert.Equal(EstadoCobro.Pagado, ordenCheck!.Estado);
        Assert.Equal(1000m, ordenCheck.MontoPagado);
        Assert.Equal(2, await dbCheck.Pagos.CountAsync(p => p.OrdenCobroId == orden.Id));
    }

    [Fact]
    public async Task Cobrar_POST_should_reject_overpayment_when_orden_already_partially_paid()
    {
        // Arrange — ya hay un pago de 800, saldo es 700; intento pagar 800 (excede saldo)
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1500m);
        var userId = Guid.NewGuid().ToString();

        // Primer pago: 800
        await using var db1 = _factory.CreateContext();
        await BuildController(db1, clinica.Id, userId)
            .Cobrar(orden.Id, new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 800m });

        // Segundo intento: 800 (supera saldo de 700)
        await using var db2 = _factory.CreateContext();
        var controller = BuildController(db2, clinica.Id, userId);
        var result = await controller.Cobrar(orden.Id, new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 800m }) as ViewResult;

        // Assert — rechazado, solo hay un pago
        Assert.NotNull(result);
        Assert.False(controller.ModelState.IsValid);
        await using var dbCheck = _factory.CreateContext();
        Assert.Equal(1, await dbCheck.Pagos.CountAsync(p => p.OrdenCobroId == orden.Id));
    }

    // -----------------------------------------------------------------------
    // COBRAR POST — aislamiento de tenant
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Cobrar_POST_should_return_not_found_when_orden_belongs_to_other_clinica()
    {
        // Arrange
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica1 = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var clinica2 = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica2.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica2.Id, paciente.Id, doctor.Id);
        var ordenAjena = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica2.Id, paciente.Id, cita.Id);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica1.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = ordenAjena.Id, Monto = 500m };

        // Act
        var result = await controller.Cobrar(ordenAjena.Id, vm);

        // Assert — no puede cobrar orden de otra clínica
        Assert.IsType<NotFoundResult>(result);
        await using var dbCheck = _factory.CreateContext();
        Assert.Equal(0, await dbCheck.Pagos.CountAsync());
    }

    // -----------------------------------------------------------------------
    // COBRAR POST — integración con FacturaService
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Cobrar_POST_should_call_factura_service_after_pago_registered()
    {
        // Arrange
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1000m);
        _facturaServiceMock
            .Setup(s => s.CrearFacturaSiNoExisteAsync(orden.Id, "Transferencia", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 1000m, MetodoPago = "Transferencia" };

        // Act
        await controller.Cobrar(orden.Id, vm);

        // Assert — el servicio de factura fue llamado con los datos correctos
        _facturaServiceMock.Verify(
            s => s.CrearFacturaSiNoExisteAsync(orden.Id, "Transferencia", It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Cobrar_POST_should_not_call_factura_service_when_pago_validation_fails()
    {
        // Arrange — monto inválido
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 1000m);
        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 0m };

        // Act
        await controller.Cobrar(orden.Id, vm);

        // Assert — NO se llama al servicio de factura cuando la validación falla
        _facturaServiceMock.Verify(
            s => s.CrearFacturaSiNoExisteAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Cobrar_POST_should_store_factura_id_in_tempdata_when_factura_created()
    {
        // Arrange
        var (clinica, _, orden) = await SeedPendingOrdenAsync(total: 500m);
        const int expectedFacturaId = 99;
        _facturaServiceMock
            .Setup(s => s.CrearFacturaSiNoExisteAsync(orden.Id, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFacturaId);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, Guid.NewGuid().ToString());
        var vm = new PagoRegistroViewModel { OrdenCobroId = orden.Id, Monto = 500m };

        // Act
        await controller.Cobrar(orden.Id, vm);

        // Assert — TempData contiene el FacturaId para mostrar el botón de descarga
        Assert.Equal(expectedFacturaId, controller.TempData["FacturaId"]);
    }
}

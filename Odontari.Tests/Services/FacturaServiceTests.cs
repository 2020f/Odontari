using Microsoft.EntityFrameworkCore;
using Odontari.Tests.Helpers;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Xunit;

namespace Odontari.Tests.Services;

public class FacturaServiceTests : IDisposable
{
    private readonly DbContextFactory _factory;

    public FacturaServiceTests()
    {
        _factory = new DbContextFactory();
    }

    public void Dispose() => _factory.Dispose();

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    private async Task<(int clinicaId, int pacienteId, int ordenId)> SeedBaseAsync(
        ModoFacturacion modo = ModoFacturacion.Interna,
        bool itbis = false, decimal itbisTasa = 18, decimal total = 1500m)
    {
        await using var db = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(db);
        var clinica = await SeedHelper.CrearClinicaAsync(db, plan.Id, modo, itbis, itbisTasa);
        var doctor = await SeedHelper.CrearUsuarioAsync(db);
        var paciente = await SeedHelper.CrearPacienteAsync(db, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(db, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(db, clinica.Id, paciente.Id, cita.Id, total);
        return (clinica.Id, paciente.Id, orden.Id);
    }

    // -----------------------------------------------------------------------
    // HAPPY PATH — MODO INTERNA
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_create_factura_interna_when_modo_is_interna()
    {
        // Arrange
        var (_, _, ordenId) = await SeedBaseAsync();
        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(ordenId, "Efectivo", null);

        // Assert
        Assert.NotNull(facturaId);
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(TipoDocumentoFactura.Interna, factura!.TipoDocumento);
        Assert.Null(factura.NCF);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
    }

    [Fact]
    public async Task should_set_numero_interno_to_1_for_first_factura_of_clinica()
    {
        // Arrange
        var (_, _, ordenId) = await SeedBaseAsync();
        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(ordenId, null, null);

        // Assert
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(1, factura!.NumeroInterno);
    }

    [Fact]
    public async Task should_increment_numero_interno_per_clinica()
    {
        // Arrange — dos órdenes distintas para la misma clínica
        await using (var db1 = _factory.CreateContext())
        {
            var plan = await SeedHelper.CrearPlanAsync(db1);
            var clinica = await SeedHelper.CrearClinicaAsync(db1, plan.Id);
            var doctor = await SeedHelper.CrearUsuarioAsync(db1);
            var paciente = await SeedHelper.CrearPacienteAsync(db1, clinica.Id);
            var cita1 = await SeedHelper.CrearCitaAsync(db1, clinica.Id, paciente.Id, doctor.Id);
            var cita2 = await SeedHelper.CrearCitaAsync(db1, clinica.Id, paciente.Id, doctor.Id);
            var orden1 = await SeedHelper.CrearOrdenCobroAsync(db1, clinica.Id, paciente.Id, cita1.Id);
            var orden2 = await SeedHelper.CrearOrdenCobroAsync(db1, clinica.Id, paciente.Id, cita2.Id);

            await using var db2 = _factory.CreateContext();
            var sut = new FacturaService(db2);
            var id1 = await sut.CrearFacturaSiNoExisteAsync(orden1.Id, null, null);

            await using var db3 = _factory.CreateContext();
            var sut2 = new FacturaService(db3);
            var id2 = await sut2.CrearFacturaSiNoExisteAsync(orden2.Id, null, null);

            var f1 = await db3.Facturas.FindAsync(id1);
            var f2 = await db3.Facturas.FindAsync(id2);
            Assert.Equal(1, f1!.NumeroInterno);
            Assert.Equal(2, f2!.NumeroInterno);
        }
    }

    // -----------------------------------------------------------------------
    // IDEMPOTENCIA
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_return_existing_factura_id_when_already_exists()
    {
        // Arrange
        var (_, _, ordenId) = await SeedBaseAsync();
        await using var db1 = _factory.CreateContext();
        var firstId = await new FacturaService(db1).CrearFacturaSiNoExisteAsync(ordenId, "Efectivo", null);

        // Act — segunda llamada con contexto fresco
        await using var db2 = _factory.CreateContext();
        var secondId = await new FacturaService(db2).CrearFacturaSiNoExisteAsync(ordenId, "Efectivo", null);

        // Assert — mismo Id, solo una factura en DB
        Assert.Equal(firstId, secondId);
        await using var db3 = _factory.CreateContext();
        Assert.Equal(1, await db3.Facturas.CountAsync());
    }

    [Fact]
    public async Task should_return_null_when_orden_not_found()
    {
        // Arrange
        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var result = await sut.CrearFacturaSiNoExisteAsync(99999, "Efectivo", null);

        // Assert
        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // CÁLCULO DE ITBIS
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_calculate_subtotal_and_itbis_when_itbis_enabled()
    {
        // Arrange — ITBIS 18%, total = 1180
        var (_, _, ordenId) = await SeedBaseAsync(itbis: true, itbisTasa: 18, total: 1180m);
        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(ordenId, null, null);

        // Assert
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(1000m, factura!.Subtotal);  // 1180 / 1.18 = 1000
        Assert.Equal(180m, factura.Itbis);        // 1180 - 1000 = 180
        Assert.Equal(1180m, factura.Total);
    }

    [Fact]
    public async Task should_set_itbis_to_zero_when_itbis_disabled()
    {
        // Arrange
        var (_, _, ordenId) = await SeedBaseAsync(itbis: false, total: 1500m);
        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(ordenId, null, null);

        // Assert
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(1500m, factura!.Subtotal);
        Assert.Equal(0m, factura.Itbis);
    }

    // -----------------------------------------------------------------------
    // MODO FISCAL — NCF
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_create_factura_fiscal_with_ncf_when_rango_available()
    {
        // Arrange
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id, proximo: 1, hasta: "50", prefijo: "B02");

        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(orden.Id, "Tarjeta", null);

        // Assert
        Assert.NotNull(facturaId);
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(TipoDocumentoFactura.Fiscal, factura!.TipoDocumento);
        Assert.NotNull(factura.NCF);
        Assert.Contains("B02", factura.NCF);
    }

    [Fact]
    public async Task should_increment_rango_proximo_after_ncf_assigned()
    {
        // Arrange
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        var rango = await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id, proximo: 5, hasta: "50");

        await using var db = _factory.CreateContext();
        await new FacturaService(db).CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var rangoActualizado = await dbCheck.NCFRangos.FindAsync(rango.Id);
        Assert.Equal(6, rangoActualizado!.Proximo);
    }

    [Fact]
    public async Task should_mark_rango_agotado_when_last_ncf_is_used()
    {
        // Arrange — Proximo == Hasta (último número disponible)
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        var rango = await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id, proximo: 10, hasta: "10");

        await using var db = _factory.CreateContext();
        await new FacturaService(db).CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var rangoFinal = await dbCheck.NCFRangos.FindAsync(rango.Id);
        Assert.Equal(EstadoNCFRango.Agotado, rangoFinal!.Estado);
    }

    [Fact]
    public async Task should_create_ncf_movimiento_when_fiscal_ncf_assigned()
    {
        // Arrange
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id);

        await using var db = _factory.CreateContext();
        var facturaId = await new FacturaService(db).CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var movimiento = await dbCheck.NCFMovimientos.FirstOrDefaultAsync(m => m.FacturaId == facturaId);
        Assert.NotNull(movimiento);
        Assert.Equal(EstadoNCFMovimiento.Emitido, movimiento.Estado);
    }

    [Fact]
    public async Task should_fall_back_to_interna_when_fiscal_mode_has_no_active_rango()
    {
        // Arrange — clínica fiscal sin rangos
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);

        await using var db = _factory.CreateContext();
        var facturaId = await new FacturaService(db).CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert — se crea factura interna silenciosamente (comportamiento actual documentado)
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.NotNull(factura);
        Assert.Equal(TipoDocumentoFactura.Interna, factura!.TipoDocumento);
        Assert.Null(factura.NCF);
    }

    [Fact]
    public async Task should_preserve_forma_pago_trimmed()
    {
        // Arrange
        var (_, _, ordenId) = await SeedBaseAsync();
        await using var db = _factory.CreateContext();

        // Act
        var facturaId = await new FacturaService(db).CrearFacturaSiNoExisteAsync(ordenId, "  Efectivo  ", null);

        // Assert
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal("Efectivo", factura!.FormaPago);
    }
}

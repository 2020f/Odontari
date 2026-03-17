using Microsoft.EntityFrameworkCore;
using Odontari.Tests.Helpers;
using Odontari.Web.Models.Enums;
using Odontari.Web.Services;
using Xunit;

namespace Odontari.Tests.Services;

/// <summary>
/// Tests de edge cases y bugs específicos en FacturaService.
/// Separados de FacturaServiceTests para facilitar identificar qué bug testa cada uno.
/// </summary>
public class FacturaServiceEdgeCaseTests : IDisposable
{
    private readonly DbContextFactory _factory;

    public FacturaServiceEdgeCaseTests()
    {
        _factory = new DbContextFactory();
    }

    public void Dispose() => _factory.Dispose();

    // -----------------------------------------------------------------------
    // BUG #2: long.Parse(rango.Hasta) → FormatException si Hasta no es numérico
    // ANTES del fix: lanzaba FormatException adentro de la transacción serializable.
    // DESPUÉS del fix: cae gracefully a factura interna.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_fallback_to_interna_gracefully_when_hasta_is_non_numeric()
    {
        // Arrange — rango con Hasta no numérico (dato corrupto o error de admin)
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        // Hasta con valor no numérico — simula dato inválido ingresado por el admin
        await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id, proximo: 1, hasta: "B02-50");

        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act — NO debe lanzar excepción
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(orden.Id, "Efectivo", null);

        // Assert — crea factura interna como fallback, no explota
        Assert.NotNull(facturaId);
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(TipoDocumentoFactura.Interna, factura!.TipoDocumento);
        Assert.Null(factura.NCF);
    }

    // -----------------------------------------------------------------------
    // BUG #4: Formato NCF validado correctamente con zero-padding
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_format_ncf_with_correct_zero_padding_matching_hasta_length()
    {
        // Arrange — Hasta = "00000050" (8 chars), Proximo = 1 → NCF esperado: "B0200000001"
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        // Hasta con zeros: 8 chars → número debe tener padding de 8 dígitos
        await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id, proximo: 1, hasta: "00000050", prefijo: "B02");

        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert — NCF con padding completo, no "B021"
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal("B0200000001", factura!.NCF);
    }

    [Fact]
    public async Task should_generate_sequential_ncfs_with_correct_incrementing_numbers()
    {
        // Arrange — dos órdenes en la misma clínica fiscal
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita1 = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var cita2 = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden1 = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita1.Id);
        var orden2 = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita2.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        await SeedHelper.CrearNCFRangoAsync(dbSeed, clinica.Id, ncfTipo.Id, proximo: 1, hasta: "00000050", prefijo: "B02");

        await using var db1 = _factory.CreateContext();
        var id1 = await new FacturaService(db1).CrearFacturaSiNoExisteAsync(orden1.Id, null, null);

        await using var db2 = _factory.CreateContext();
        var id2 = await new FacturaService(db2).CrearFacturaSiNoExisteAsync(orden2.Id, null, null);

        // Assert — NCFs correlativas y distintas
        await using var dbCheck = _factory.CreateContext();
        var f1 = await dbCheck.Facturas.FindAsync(id1);
        var f2 = await dbCheck.Facturas.FindAsync(id2);
        Assert.Equal("B0200000001", f1!.NCF);
        Assert.Equal("B0200000002", f2!.NCF);
        Assert.NotEqual(f1.NCF, f2.NCF);
    }

    // -----------------------------------------------------------------------
    // OrdenCobro sin CitaId (órden manual — escenario válido en el modelo)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_create_factura_when_orden_has_no_cita()
    {
        // Arrange — orden sin cita (CitaId = null)
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, citaId: null, total: 800m);

        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(orden.Id, "Efectivo", null);

        // Assert
        Assert.NotNull(facturaId);
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Null(factura!.CitaId);
        Assert.Equal(800m, factura.Total);
    }

    // -----------------------------------------------------------------------
    // ITBIS — edge case: tasa 0 con flag true (no debe calcular ITBIS)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_not_apply_itbis_when_tasa_is_zero_even_if_flag_enabled()
    {
        // Arrange — ItbisAplicarPorDefecto = true pero ItbisTasa = 0
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, itbis: true, itbisTasa: 0);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id, total: 1000m);

        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert — con tasa 0, subtotal == total e ITBIS == 0
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(1000m, factura!.Subtotal);
        Assert.Equal(0m, factura.Itbis);
    }

    // -----------------------------------------------------------------------
    // Rango agotado NO debe usarse
    // -----------------------------------------------------------------------

    [Fact]
    public async Task should_not_use_agotado_rango_even_if_proximo_in_range()
    {
        // Arrange — rango marcado como Agotado
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id, ModoFacturacion.Fiscal);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var orden = await SeedHelper.CrearOrdenCobroAsync(dbSeed, clinica.Id, paciente.Id, cita.Id);
        var ncfTipo = await SeedHelper.CrearNCFTipoAsync(dbSeed);
        // Rango creado con estado Agotado directamente
        var rango = new Odontari.Web.Models.NCFRango
        {
            ClinicaId = clinica.Id,
            NCFTipoId = ncfTipo.Id,
            SeriePrefijo = "B02",
            Desde = "1",
            Hasta = "50",
            Proximo = 1,
            Estado = EstadoNCFRango.Agotado, // Agotado pero Proximo < Hasta
            CreadoAt = DateTime.UtcNow
        };
        dbSeed.NCFRangos.Add(rango);
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var sut = new FacturaService(db);

        // Act
        var facturaId = await sut.CrearFacturaSiNoExisteAsync(orden.Id, null, null);

        // Assert — no usa el rango agotado, emite factura interna
        var factura = await db.Facturas.FindAsync(facturaId);
        Assert.Equal(TipoDocumentoFactura.Interna, factura!.TipoDocumento);
        Assert.Null(factura.NCF);
    }
}

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
using System.Security.Claims;
using Xunit;

namespace Odontari.Tests.Controllers;

public class AtencionControllerTests : IDisposable
{
    private readonly DbContextFactory _factory;

    public AtencionControllerTests()
    {
        _factory = new DbContextFactory();
    }

    public void Dispose() => _factory.Dispose();

    // -----------------------------------------------------------------------
    // HELPERS
    // -----------------------------------------------------------------------

    private static AtencionController BuildController(
        ApplicationDbContext db,
        int clinicaId,
        string userId,
        bool esDoctor = false,
        bool esAdmin = false)
    {
        var mockClinicaService = new Mock<IClinicaActualService>();
        mockClinicaService.Setup(s => s.GetClinicaIdActual()).Returns(clinicaId);
        mockClinicaService.Setup(s => s.GetClinicaIdActualAsync()).ReturnsAsync(clinicaId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "Test User")
        };
        if (esDoctor) claims.Add(new Claim(ClaimTypes.Role, OdontariRoles.Doctor));
        if (esAdmin)  claims.Add(new Claim(ClaimTypes.Role, OdontariRoles.AdminClinica));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var controller = new AtencionController(db, mockClinicaService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
        return controller;
    }

    private async Task<(Clinica clinica, ApplicationUser doctor, Paciente paciente)> SeedBaseAsync()
    {
        await using var db = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(db);
        var clinica = await SeedHelper.CrearClinicaAsync(db, plan.Id);
        var doctor = await SeedHelper.CrearUsuarioAsync(db);
        var paciente = await SeedHelper.CrearPacienteAsync(db, clinica.Id);
        return (clinica, doctor, paciente);
    }

    // -----------------------------------------------------------------------
    // INDEX — filtra por DoctorId
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Index_should_return_only_doctor_own_appointments()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var otroDoctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id, fechaHora: DateTime.Today.AddHours(9));
        await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, otroDoctor.Id, fechaHora: DateTime.Today.AddHours(10));

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        var result = await controller.Index(DateTime.Today) as ViewResult;
        var citas = result?.Model as List<Cita>;

        // Assert — solo ve su propia cita
        Assert.NotNull(citas);
        Assert.Single(citas);
        Assert.All(citas, c => Assert.Equal(doctor.Id, c.DoctorId));
    }

    [Fact]
    public async Task Index_should_exclude_cancelled_appointments()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id, EstadoCita.Cancelada, DateTime.Today.AddHours(9));

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        var result = await controller.Index(DateTime.Today) as ViewResult;
        var citas = result?.Model as List<Cita>;

        // Assert
        Assert.NotNull(citas);
        Assert.Empty(citas);
    }

    [Fact]
    public async Task Index_uses_today_when_no_date_provided()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id, fechaHora: DateTime.Today.AddHours(9));

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act — sin fecha explícita
        var result = await controller.Index(null) as ViewResult;
        var citas = result?.Model as List<Cita>;

        // Assert — la cita de hoy aparece
        Assert.NotNull(citas);
        Assert.Single(citas);
    }

    // -----------------------------------------------------------------------
    // EXPEDIENTE — aislamiento de tenant y roles
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Expediente_should_return_not_found_when_cita_belongs_to_other_clinica()
    {
        // Arrange — dos clínicas distintas
        await using var dbSeed = _factory.CreateContext();
        var plan = await SeedHelper.CrearPlanAsync(dbSeed);
        var clinica1 = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var clinica2 = await SeedHelper.CrearClinicaAsync(dbSeed, plan.Id);
        var doctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var paciente = await SeedHelper.CrearPacienteAsync(dbSeed, clinica2.Id);
        var citaDeOtraClinica = await SeedHelper.CrearCitaAsync(dbSeed, clinica2.Id, paciente.Id, doctor.Id);

        await using var db = _factory.CreateContext();
        // Usuario autenticado en clinica1 intenta ver cita de clinica2
        var controller = BuildController(db, clinica1.Id, doctor.Id, esAdmin: true);

        // Act
        var result = await controller.Expediente(citaDeOtraClinica.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Expediente_should_return_not_found_when_doctor_accesses_other_doctor_cita()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var otroDoctor = await SeedHelper.CrearUsuarioAsync(dbSeed);
        var citaDelOtro = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, otroDoctor.Id);

        await using var db = _factory.CreateContext();
        // doctor (rol Doctor puro) intenta ver cita asignada a otroDoctor
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        var result = await controller.Expediente(citaDelOtro.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Expediente_admin_can_see_any_doctor_appointment()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);

        await using var db = _factory.CreateContext();
        var adminId = Guid.NewGuid().ToString();
        var controller = BuildController(db, clinica.Id, adminId, esAdmin: true);

        // Act
        var result = await controller.Expediente(cita.Id);

        // Assert — admin ve la cita sin importar que no sea su DoctorId
        Assert.IsType<ViewResult>(result);
    }

    // -----------------------------------------------------------------------
    // MARCAR REALIZADO — bloqueado si precio == 0
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarcarRealizado_should_not_mark_when_precio_is_zero()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 0);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var pr = new ProcedimientoRealizado { CitaId = cita.Id, TratamientoId = tratamiento.Id, PrecioAplicado = 0, MarcadoRealizado = false };
        dbSeed.ProcedimientosRealizados.Add(pr);
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        var result = await controller.MarcarRealizado(pr.Id) as RedirectToActionResult;

        // Assert — redirige pero NO marca como realizado
        Assert.NotNull(result);
        await using var dbCheck = _factory.CreateContext();
        var prCheck = await dbCheck.ProcedimientosRealizados.FindAsync(pr.Id);
        Assert.False(prCheck!.MarcadoRealizado);
    }

    [Fact]
    public async Task MarcarRealizado_should_set_realizado_at_when_precio_greater_than_zero()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 500);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var pr = new ProcedimientoRealizado { CitaId = cita.Id, TratamientoId = tratamiento.Id, PrecioAplicado = 500, MarcadoRealizado = false };
        dbSeed.ProcedimientosRealizados.Add(pr);
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.MarcarRealizado(pr.Id);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var prCheck = await dbCheck.ProcedimientosRealizados.FindAsync(pr.Id);
        Assert.True(prCheck!.MarcadoRealizado);
        Assert.NotNull(prCheck.RealizadoAt);
    }

    // -----------------------------------------------------------------------
    // AGREGAR PROCEDIMIENTO
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AgregarProcedimiento_should_create_procedimiento_and_historial()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 800);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.AgregarProcedimiento(cita.Id, tratamiento.Id);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var pr = await dbCheck.ProcedimientosRealizados.FirstOrDefaultAsync(p => p.CitaId == cita.Id);
        Assert.NotNull(pr);
        Assert.Equal(800m, pr!.PrecioAplicado);

        var historial = await dbCheck.HistorialClinico.FirstOrDefaultAsync(h => h.CitaId == cita.Id && h.TipoEvento == "Tratamiento agregado");
        Assert.NotNull(historial);
    }

    [Fact]
    public async Task AgregarProcedimiento_should_return_not_found_for_tratamiento_from_other_clinica()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var plan2 = await SeedHelper.CrearPlanAsync(dbSeed);
        var otraClinica = await SeedHelper.CrearClinicaAsync(dbSeed, plan2.Id);
        var tratamientoAjeno = await SeedHelper.CrearTratamientoAsync(dbSeed, otraClinica.Id);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esAdmin: true);

        // Act
        var result = await controller.AgregarProcedimiento(cita.Id, tratamientoAjeno.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // -----------------------------------------------------------------------
    // ACTUALIZAR PRECIO
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ActualizarPrecioProcedimiento_should_normalize_negative_price_to_zero()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 500);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var pr = new ProcedimientoRealizado { CitaId = cita.Id, TratamientoId = tratamiento.Id, PrecioAplicado = 500 };
        dbSeed.ProcedimientosRealizados.Add(pr);
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.ActualizarPrecioProcedimiento(pr.Id, -100);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var prCheck = await dbCheck.ProcedimientosRealizados.FindAsync(pr.Id);
        Assert.Equal(0m, prCheck!.PrecioAplicado);
    }

    [Fact]
    public async Task ActualizarPrecioProcedimiento_should_also_update_tratamiento_precio_base()
    {
        // Arrange — side-effect documentado: también actualiza Tratamiento.PrecioBase
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 500);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        var pr = new ProcedimientoRealizado { CitaId = cita.Id, TratamientoId = tratamiento.Id, PrecioAplicado = 500 };
        dbSeed.ProcedimientosRealizados.Add(pr);
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.ActualizarPrecioProcedimiento(pr.Id, 750);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var tratCheck = await dbCheck.Tratamientos.FindAsync(tratamiento.Id);
        Assert.Equal(750m, tratCheck!.PrecioBase);
    }

    // -----------------------------------------------------------------------
    // FINALIZAR ATENCION
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FinalizarAtencion_should_create_orden_cobro_with_realized_procedures_total()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 2000);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        dbSeed.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = cita.Id, TratamientoId = tratamiento.Id,
            PrecioAplicado = 2000, MarcadoRealizado = true
        });
        // Procedimiento NO realizado — no debe sumarse
        dbSeed.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = cita.Id, TratamientoId = tratamiento.Id,
            PrecioAplicado = 500, MarcadoRealizado = false
        });
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.FinalizarAtencion(cita.Id);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var orden = await dbCheck.OrdenesCobro.FirstOrDefaultAsync(o => o.CitaId == cita.Id);
        Assert.NotNull(orden);
        Assert.Equal(2000m, orden!.Total);  // Solo el realizado
    }

    [Fact]
    public async Task FinalizarAtencion_should_not_create_orden_cobro_when_no_procedures_realized()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 1000);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        dbSeed.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = cita.Id, TratamientoId = tratamiento.Id,
            PrecioAplicado = 1000, MarcadoRealizado = false
        });
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.FinalizarAtencion(cita.Id);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var ordenes = await dbCheck.OrdenesCobro.Where(o => o.CitaId == cita.Id).ToListAsync();
        Assert.Empty(ordenes);
    }

    [Fact]
    public async Task FinalizarAtencion_is_idempotent_does_not_duplicate_orden_cobro()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 1500);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        dbSeed.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = cita.Id, TratamientoId = tratamiento.Id,
            PrecioAplicado = 1500, MarcadoRealizado = true
        });
        await dbSeed.SaveChangesAsync();

        // Primera llamada
        await using var db1 = _factory.CreateContext();
        await BuildController(db1, clinica.Id, doctor.Id, esDoctor: true).FinalizarAtencion(cita.Id);

        // Segunda llamada — la cita ya está Finalizada
        await using var db2 = _factory.CreateContext();
        await BuildController(db2, clinica.Id, doctor.Id, esDoctor: true).FinalizarAtencion(cita.Id);

        // Assert — solo una OrdenCobro
        await using var dbCheck = _factory.CreateContext();
        var count = await dbCheck.OrdenesCobro.CountAsync(o => o.CitaId == cita.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task FinalizarAtencion_should_set_cita_estado_to_finalizada()
    {
        // Arrange
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.FinalizarAtencion(cita.Id);

        // Assert
        await using var dbCheck = _factory.CreateContext();
        var citaCheck = await dbCheck.Citas.FindAsync(cita.Id);
        Assert.Equal(EstadoCita.Finalizada, citaCheck!.Estado);
    }

    // -----------------------------------------------------------------------
    // BUG #1 — Historial clínico con mensaje incorrecto cuando total == 0
    // -----------------------------------------------------------------------

    [Fact]
    public async Task FinalizarAtencion_historial_should_not_say_orden_generada_when_no_procedures_realized()
    {
        // Arrange — cita sin procedimientos realizados
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 1000);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        dbSeed.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = cita.Id, TratamientoId = tratamiento.Id,
            PrecioAplicado = 1000, MarcadoRealizado = false  // NO realizado
        });
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.FinalizarAtencion(cita.Id);

        // Assert — no se creó orden de cobro
        await using var dbCheck = _factory.CreateContext();
        Assert.Empty(await dbCheck.OrdenesCobro.Where(o => o.CitaId == cita.Id).ToListAsync());

        // El historial NO debe decir "Orden de cobro generada" cuando no hay orden
        var historial = await dbCheck.HistorialClinico
            .FirstOrDefaultAsync(h => h.CitaId == cita.Id && h.TipoEvento == "Atención finalizada");
        Assert.NotNull(historial);
        Assert.NotEqual("Orden de cobro generada", historial!.Descripcion);
    }

    [Fact]
    public async Task FinalizarAtencion_historial_should_say_orden_generada_when_procedures_realized()
    {
        // Arrange — cita con procedimiento realizado
        var (clinica, doctor, paciente) = await SeedBaseAsync();
        await using var dbSeed = _factory.CreateContext();
        var tratamiento = await SeedHelper.CrearTratamientoAsync(dbSeed, clinica.Id, precio: 1000);
        var cita = await SeedHelper.CrearCitaAsync(dbSeed, clinica.Id, paciente.Id, doctor.Id);
        dbSeed.ProcedimientosRealizados.Add(new ProcedimientoRealizado
        {
            CitaId = cita.Id, TratamientoId = tratamiento.Id,
            PrecioAplicado = 1000, MarcadoRealizado = true
        });
        await dbSeed.SaveChangesAsync();

        await using var db = _factory.CreateContext();
        var controller = BuildController(db, clinica.Id, doctor.Id, esDoctor: true);

        // Act
        await controller.FinalizarAtencion(cita.Id);

        // Assert — cuando SÍ hay orden, el mensaje es el correcto
        await using var dbCheck = _factory.CreateContext();
        var historial = await dbCheck.HistorialClinico
            .FirstOrDefaultAsync(h => h.CitaId == cita.Id && h.TipoEvento == "Atención finalizada");
        Assert.NotNull(historial);
        Assert.Equal("Orden de cobro generada", historial!.Descripcion);
    }
}

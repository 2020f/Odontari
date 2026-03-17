using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;

namespace Odontari.Tests.Helpers;

/// <summary>
/// Crea entidades base reutilizables entre tests. Cada método inserta y guarda,
/// devolviendo la entidad con su Id asignado por SQLite.
/// </summary>
public static class SeedHelper
{
    public static async Task<Plan> CrearPlanAsync(ApplicationDbContext db)
    {
        var plan = new Plan { Nombre = "Test", PrecioMensual = 0, MaxUsuarios = 10, MaxDoctores = 5 };
        db.Planes.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    public static async Task<Clinica> CrearClinicaAsync(
        ApplicationDbContext db, int planId,
        ModoFacturacion modo = ModoFacturacion.Interna,
        bool itbis = false, decimal itbisTasa = 18)
    {
        var clinica = new Clinica
        {
            Nombre = "Clínica Test",
            PlanId = planId,
            ModoFacturacion = modo,
            ItbisAplicarPorDefecto = itbis,
            ItbisTasa = itbisTasa
        };
        db.Clinicas.Add(clinica);
        await db.SaveChangesAsync();
        return clinica;
    }

    public static async Task<Paciente> CrearPacienteAsync(ApplicationDbContext db, int clinicaId, DateTime? fechaNacimiento = null)
    {
        var p = new Paciente { ClinicaId = clinicaId, Nombre = "Paciente Test", FechaNacimiento = fechaNacimiento };
        db.Pacientes.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public static async Task<ApplicationUser> CrearUsuarioAsync(ApplicationDbContext db, string? id = null)
    {
        var uid = id ?? Guid.NewGuid().ToString();
        var email = $"test_{Guid.NewGuid():N}@test.com";
        var user = new ApplicationUser
        {
            Id = uid,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<Cita> CrearCitaAsync(
        ApplicationDbContext db, int clinicaId, int pacienteId, string doctorId,
        EstadoCita estado = EstadoCita.EnAtencion,
        DateTime? fechaHora = null)
    {
        var cita = new Cita
        {
            ClinicaId = clinicaId,
            PacienteId = pacienteId,
            DoctorId = doctorId,
            FechaHora = fechaHora ?? DateTime.Today.AddHours(9),
            Estado = estado
        };
        db.Citas.Add(cita);
        await db.SaveChangesAsync();
        return cita;
    }

    public static async Task<OrdenCobro> CrearOrdenCobroAsync(
        ApplicationDbContext db, int clinicaId, int pacienteId, int? citaId = null,
        decimal total = 1500m)
    {
        var orden = new OrdenCobro
        {
            ClinicaId = clinicaId,
            PacienteId = pacienteId,
            CitaId = citaId,
            Total = total,
            MontoPagado = 0,
            Estado = EstadoCobro.Pendiente,
            CreadoAt = DateTime.Now
        };
        db.OrdenesCobro.Add(orden);
        await db.SaveChangesAsync();
        return orden;
    }

    public static async Task<Tratamiento> CrearTratamientoAsync(
        ApplicationDbContext db, int clinicaId, decimal precio = 1000m)
    {
        var t = new Tratamiento
        {
            ClinicaId = clinicaId,
            Nombre = "Extracción simple",
            PrecioBase = precio,
            DuracionMinutos = 30,
            Activo = true
        };
        db.Tratamientos.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    public static async Task<NCFTipo> CrearNCFTipoAsync(ApplicationDbContext db)
    {
        var tipo = new NCFTipo { Codigo = "B02", Nombre = "Consumidor Final", Activo = true };
        db.NCFTipos.Add(tipo);
        await db.SaveChangesAsync();
        return tipo;
    }

    public static async Task<NCFRango> CrearNCFRangoAsync(
        ApplicationDbContext db, int clinicaId, int ncfTipoId,
        long proximo = 1, string hasta = "50", string? prefijo = "B02")
    {
        var rango = new NCFRango
        {
            ClinicaId = clinicaId,
            NCFTipoId = ncfTipoId,
            SeriePrefijo = prefijo,
            Desde = "1",
            Hasta = hasta,
            Proximo = proximo,
            Estado = EstadoNCFRango.Activo,
            CreadoAt = DateTime.UtcNow
        };
        db.NCFRangos.Add(rango);
        await db.SaveChangesAsync();
        return rango;
    }
}

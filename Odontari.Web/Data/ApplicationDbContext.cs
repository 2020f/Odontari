using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Models;

namespace Odontari.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Clinica> Clinicas => Set<Clinica>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Tratamiento> Tratamientos => Set<Tratamiento>();
    public DbSet<ProcedimientoRealizado> ProcedimientosRealizados => Set<ProcedimientoRealizado>();
    public DbSet<OrdenCobro> OrdenesCobro => Set<OrdenCobro>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Odontograma> Odontogramas => Set<Odontograma>();
    public DbSet<HistorialClinico> HistorialClinico => Set<HistorialClinico>();
    public DbSet<HistoriaClinicaSistematica> HistoriasClinicasSistematicas => Set<HistoriaClinicaSistematica>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.ClinicaId).IsRequired(false);
            b.Property(u => u.NombreCompleto).HasMaxLength(200);
        });

        builder.Entity<Plan>(b =>
        {
            b.Property(p => p.Nombre).HasMaxLength(100);
            b.Property(p => p.PrecioMensual).HasPrecision(18, 2);
        });
        builder.Entity<Clinica>(b =>
        {
            b.Property(c => c.Nombre).HasMaxLength(200);
            b.Property(c => c.Email).HasMaxLength(256);
            b.HasOne(c => c.Plan).WithMany(p => p.Clinicas).HasForeignKey(c => c.PlanId);
        });
        builder.Entity<Suscripcion>(b =>
        {
            b.HasOne(s => s.Clinica).WithMany(c => c.Suscripciones).HasForeignKey(s => s.ClinicaId);
        });

        builder.Entity<Paciente>(b =>
        {
            b.Property(p => p.Nombre).HasMaxLength(200);
            b.Property(p => p.Cedula).HasMaxLength(50);
            b.HasOne(p => p.Clinica).WithMany(c => c.Pacientes).HasForeignKey(p => p.ClinicaId);
        });
        builder.Entity<Cita>(b =>
        {
            b.HasOne(c => c.Clinica).WithMany(c => c.Citas).HasForeignKey(c => c.ClinicaId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(c => c.Paciente).WithMany(p => p.Citas).HasForeignKey(c => c.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(c => c.Doctor).WithMany().HasForeignKey(c => c.DoctorId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Tratamiento>(b =>
        {
            b.Property(t => t.Nombre).HasMaxLength(200);
            b.Property(t => t.PrecioBase).HasPrecision(18, 2);
            b.HasOne(t => t.Clinica).WithMany(c => c.Tratamientos).HasForeignKey(t => t.ClinicaId);
        });
        builder.Entity<ProcedimientoRealizado>(b =>
        {
            b.Property(pr => pr.PrecioAplicado).HasPrecision(18, 2);
            b.HasOne(pr => pr.Cita).WithMany(c => c.ProcedimientosRealizados).HasForeignKey(pr => pr.CitaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(pr => pr.Tratamiento).WithMany(t => t.ProcedimientosRealizados).HasForeignKey(pr => pr.TratamientoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrdenCobro>(b =>
        {
            b.Property(o => o.Total).HasPrecision(18, 2);
            b.Property(o => o.MontoPagado).HasPrecision(18, 2);
            b.HasOne(o => o.Clinica).WithMany(c => c.OrdenesCobro).HasForeignKey(o => o.ClinicaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(o => o.Paciente).WithMany(p => p.OrdenesCobro).HasForeignKey(o => o.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(o => o.Cita).WithMany(c => c.OrdenesCobro).HasForeignKey(o => o.CitaId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Pago>(b =>
        {
            b.Property(p => p.Monto).HasPrecision(18, 2);
            b.HasOne(p => p.OrdenCobro).WithMany(o => o.Pagos).HasForeignKey(p => p.OrdenCobroId);
        });

        builder.Entity<AuditLog>(b =>
        {
            b.Property(a => a.Accion).HasMaxLength(100);
            b.Property(a => a.Entidad).HasMaxLength(100);
            b.Property(a => a.EntidadId).HasMaxLength(50);
        });

        builder.Entity<Odontograma>(b =>
        {
            b.HasOne(o => o.Paciente).WithMany(p => p.Odontogramas).HasForeignKey(o => o.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(o => o.Clinica).WithMany(c => c.Odontogramas).HasForeignKey(o => o.ClinicaId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<HistorialClinico>(b =>
        {
            b.Property(h => h.TipoEvento).HasMaxLength(100);
            b.HasOne(h => h.Paciente).WithMany(p => p.HistorialClinico).HasForeignKey(h => h.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(h => h.Clinica).WithMany(c => c.HistorialClinico).HasForeignKey(h => h.ClinicaId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<HistoriaClinicaSistematica>(b =>
        {
            b.HasOne(h => h.Paciente).WithOne(p => p.HistoriaClinicaSistematica).HasForeignKey<HistoriaClinicaSistematica>(h => h.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(h => h.Clinica).WithMany(c => c.HistoriasClinicasSistematicas).HasForeignKey(h => h.ClinicaId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

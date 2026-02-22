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
    public DbSet<Periodontograma> Periodontogramas => Set<Periodontograma>();
    public DbSet<HistorialClinico> HistorialClinico => Set<HistorialClinico>();
    public DbSet<HistoriaClinicaSistematica> HistoriasClinicasSistematicas => Set<HistoriaClinicaSistematica>();
    public DbSet<UsuarioVistaPermiso> UsuarioVistaPermisos => Set<UsuarioVistaPermiso>();
    public DbSet<BloqueoVistaClinicaDinamica> BloqueoVistaClinicaDinamicas => Set<BloqueoVistaClinicaDinamica>();
    public DbSet<ArchivoSubido> ArchivosSubidos => Set<ArchivoSubido>();
    public DbSet<NCFTipo> NCFTipos => Set<NCFTipo>();
    public DbSet<NCFRango> NCFRangos => Set<NCFRango>();
    public DbSet<NCFMovimiento> NCFMovimientos => Set<NCFMovimiento>();
    public DbSet<Factura> Facturas => Set<Factura>();

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
            b.Property(c => c.RNC).HasMaxLength(20);
            b.Property(c => c.RazonSocial).HasMaxLength(300);
            b.Property(c => c.NombreComercial).HasMaxLength(200);
            b.Property(c => c.DireccionFiscal).HasMaxLength(500);
            b.Property(c => c.LogoUrl).HasMaxLength(500);
            b.Property(c => c.ItbisTasa).HasPrecision(5, 2);
            b.Property(c => c.MensajeFactura).HasMaxLength(500);
            b.Property(c => c.CondicionesPago).HasMaxLength(500);
            b.Property(c => c.NotaLegal).HasMaxLength(1000);
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
        builder.Entity<Periodontograma>(b =>
        {
            b.HasOne(p => p.Paciente).WithMany(p => p.Periodontogramas).HasForeignKey(p => p.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Clinica).WithMany(c => c.Periodontogramas).HasForeignKey(p => p.ClinicaId).OnDelete(DeleteBehavior.Restrict);
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

        builder.Entity<UsuarioVistaPermiso>(b =>
        {
            b.HasKey(p => new { p.UserId, p.VistaKey });
            b.Property(p => p.VistaKey).HasMaxLength(50);
            b.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BloqueoVistaClinicaDinamica>(b =>
        {
            b.HasKey(x => new { x.ClinicaId, x.VistaKey });
            b.Property(x => x.VistaKey).HasMaxLength(50);
            b.HasOne(x => x.Clinica).WithMany().HasForeignKey(x => x.ClinicaId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ArchivoSubido>(b =>
        {
            b.Property(a => a.Container).HasMaxLength(200);
            b.Property(a => a.BlobName).HasMaxLength(500);
            b.Property(a => a.ContentType).HasMaxLength(200);
            b.Property(a => a.FileNameOriginal).HasMaxLength(500);
            b.Property(a => a.Extension).HasMaxLength(20);
            b.Property(a => a.Estado).HasMaxLength(50);
            b.Property(a => a.Url).HasMaxLength(1000);
            b.HasOne(a => a.Clinica).WithMany(c => c.ArchivosSubidos).HasForeignKey(a => a.ClinicaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.Paciente).WithMany(p => p.ArchivosSubidos).HasForeignKey(a => a.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.Usuario).WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<NCFTipo>(b =>
        {
            b.Property(t => t.Codigo).HasMaxLength(10);
            b.Property(t => t.Nombre).HasMaxLength(100);
            b.Property(t => t.Descripcion).HasMaxLength(200);
        });

        builder.Entity<NCFRango>(b =>
        {
            b.Property(r => r.SeriePrefijo).HasMaxLength(20);
            b.Property(r => r.Desde).HasMaxLength(50);
            b.Property(r => r.Hasta).HasMaxLength(50);
            b.Property(r => r.Fuente).HasMaxLength(20);
            b.Property(r => r.Nota).HasMaxLength(500);
            b.HasOne(r => r.Clinica).WithMany(c => c.NCFRangos).HasForeignKey(r => r.ClinicaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(r => r.NCFTipo).WithMany().HasForeignKey(r => r.NCFTipoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NCFMovimiento>(b =>
        {
            b.Property(m => m.NCFGenerado).HasMaxLength(50);
            b.Property(m => m.Motivo).HasMaxLength(500);
            b.HasOne(m => m.Clinica).WithMany(c => c.NCFMovimientos).HasForeignKey(m => m.ClinicaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.NCFTipo).WithMany().HasForeignKey(m => m.NCFTipoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(m => m.Factura).WithMany(f => f.NCFMovimientos).HasForeignKey(m => m.FacturaId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Factura>(b =>
        {
            b.Property(f => f.NCF).HasMaxLength(50);
            b.Property(f => f.FormaPago).HasMaxLength(50);
            b.Property(f => f.Nota).HasMaxLength(500);
            b.Property(f => f.Subtotal).HasPrecision(18, 2);
            b.Property(f => f.Itbis).HasPrecision(18, 2);
            b.Property(f => f.Total).HasPrecision(18, 2);
            b.HasOne(f => f.Clinica).WithMany(c => c.Facturas).HasForeignKey(f => f.ClinicaId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(f => f.NCFTipo).WithMany().HasForeignKey(f => f.NCFTipoId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(f => f.Paciente).WithMany(p => p.Facturas).HasForeignKey(f => f.PacienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(f => f.Cita).WithMany(c => c.Facturas).HasForeignKey(f => f.CitaId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(f => f.OrdenCobro).WithOne(o => o.Factura).HasForeignKey<Factura>(f => f.OrdenCobroId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}

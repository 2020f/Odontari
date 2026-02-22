using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

public class Clinica
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // --- Facturación / Datos fiscales (configuración por Admin) ---
    public string? RNC { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? DireccionFiscal { get; set; }
    public string? LogoUrl { get; set; }
    public ModoFacturacion ModoFacturacion { get; set; } = ModoFacturacion.Interna;
    public bool PermitirInternaConFiscal { get; set; } = true;
    public decimal ItbisTasa { get; set; } = 18;
    public bool ItbisAplicarPorDefecto { get; set; } = true;
    public string? MensajeFactura { get; set; }
    public string? CondicionesPago { get; set; }
    public string? NotaLegal { get; set; }
    public bool MostrarFirma { get; set; } = false;
    public bool MostrarQR { get; set; } = false;
    public bool FormaPagoEfectivo { get; set; } = true;
    public bool FormaPagoTransferencia { get; set; } = true;
    public bool FormaPagoTarjeta { get; set; } = true;
    public bool FormaPagoCredito { get; set; } = true;
    public bool FormaPagoMixto { get; set; } = true;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<OrdenCobro> OrdenesCobro { get; set; } = new List<OrdenCobro>();
    public ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();

    // Expediente clínico (Fase 5)
    public ICollection<Odontograma> Odontogramas { get; set; } = new List<Odontograma>();
    public ICollection<Periodontograma> Periodontogramas { get; set; } = new List<Periodontograma>();
    public ICollection<HistorialClinico> HistorialClinico { get; set; } = new List<HistorialClinico>();
    public ICollection<HistoriaClinicaSistematica> HistoriasClinicasSistematicas { get; set; } = new List<HistoriaClinicaSistematica>();
    public ICollection<ArchivoSubido> ArchivosSubidos { get; set; } = new List<ArchivoSubido>();
    public ICollection<NCFRango> NCFRangos { get; set; } = new List<NCFRango>();
    public ICollection<NCFMovimiento> NCFMovimientos { get; set; } = new List<NCFMovimiento>();
    public ICollection<Factura> Facturas { get; set; } = new List<Factura>();
}

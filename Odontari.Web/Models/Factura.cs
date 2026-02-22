using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

/// <summary>Factura (interna o fiscal). Puede existir sin NCF (modo interno); si es fiscal, NCF obligatorio.</summary>
public class Factura
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public int NumeroInterno { get; set; }     // Numeración interna siempre
    public TipoDocumentoFactura TipoDocumento { get; set; } = TipoDocumentoFactura.Interna;
    public int? NCFTipoId { get; set; }
    public NCFTipo? NCFTipo { get; set; }
    public string? NCF { get; set; }           // Número comprobante fiscal (si fiscal)
    public EstadoFactura Estado { get; set; } = EstadoFactura.Borrador;

    public DateTime FechaEmision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }

    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int? CitaId { get; set; }
    public Cita? Cita { get; set; }
    public int? OrdenCobroId { get; set; }
    public OrdenCobro? OrdenCobro { get; set; }

    public string? FormaPago { get; set; }     // Efectivo, Transferencia, Tarjeta, Crédito, Mixto
    public string? Nota { get; set; }
    public DateTime CreadoAt { get; set; }
    public string? UsuarioId { get; set; }

    public ICollection<NCFMovimiento> NCFMovimientos { get; set; } = new List<NCFMovimiento>();
}

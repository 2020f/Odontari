using Odontari.Web.Models.Enums;

namespace Odontari.Web.Models;

/// <summary>Bitácora/auditoría: cada vez que se usa o reserva un NCF.</summary>
public class NCFMovimiento
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;
    public int NCFTipoId { get; set; }
    public NCFTipo NCFTipo { get; set; } = null!;

    public string NCFGenerado { get; set; } = null!;
    public int? FacturaId { get; set; }
    public Factura? Factura { get; set; }
    public EstadoNCFMovimiento Estado { get; set; }
    public string? UsuarioId { get; set; }
    public ApplicationUser? Usuario { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Motivo { get; set; }  // Si anulado
}

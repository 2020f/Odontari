namespace Odontari.Web.Models;

/// <summary>Catálogo de tipos de comprobante fiscal (NCF) DGII. B01, B02, B14, E31, etc.</summary>
public class NCFTipo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;  // B01, B02, B14, E31
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool RequiereRNCCliente { get; set; }
    public bool Activo { get; set; } = true;
}

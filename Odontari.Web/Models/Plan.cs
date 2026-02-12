namespace Odontari.Web.Models;

public class Plan
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioMensual { get; set; }
    public int MaxUsuarios { get; set; }
    public int MaxDoctores { get; set; }
    public bool PermiteFacturacion { get; set; }
    public bool PermiteOdontograma { get; set; }
    public bool PermiteWhatsApp { get; set; }
    public bool PermiteARS { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Clinica> Clinicas { get; set; } = new List<Clinica>();
}

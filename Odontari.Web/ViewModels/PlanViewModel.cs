using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

public class PlanViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioMensual { get; set; }
    public int MaxUsuarios { get; set; }
    public int MaxDoctores { get; set; }
    public bool PermiteFacturacion { get; set; }
    public bool PermiteOdontograma { get; set; }
    public bool Activo { get; set; }
}

public class PlanEditViewModel
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;
    public decimal PrecioMensual { get; set; }
    public int MaxUsuarios { get; set; }
    public int MaxDoctores { get; set; }
    public bool PermiteFacturacion { get; set; }
    public bool PermiteOdontograma { get; set; }
    public bool PermiteWhatsApp { get; set; }
    public bool PermiteARS { get; set; }
    public bool Activo { get; set; } = true;
}

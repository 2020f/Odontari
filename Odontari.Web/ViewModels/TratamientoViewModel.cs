using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

public class TratamientoListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioBase { get; set; }
    public int DuracionMinutos { get; set; }
    public string? Categoria { get; set; }
    public bool Activo { get; set; }
}

public class TratamientoEditViewModel
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    [Required]
    public decimal PrecioBase { get; set; }
    public int DuracionMinutos { get; set; }
    public string? Categoria { get; set; }
    public bool Activo { get; set; } = true;
}

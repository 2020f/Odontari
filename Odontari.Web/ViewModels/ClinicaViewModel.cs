using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

public class ClinicaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activa { get; set; }
    public string PlanNombre { get; set; } = null!;
}

public class ClinicaEditViewModel
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Nombre { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activa { get; set; } = true;
    public int PlanId { get; set; }
}

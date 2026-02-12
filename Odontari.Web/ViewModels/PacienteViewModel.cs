using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

public class PacienteListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string? Cedula { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
}

public class PacienteEditViewModel
{
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Nombre { get; set; } = null!;
    [MaxLength(200)]
    public string? Apellidos { get; set; }
    [MaxLength(50)]
    public string? Cedula { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? NotasClinicas { get; set; }
    public bool Activo { get; set; } = true;
}

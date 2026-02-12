using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

/// <summary>Para crear usuario desde Panel Clínica (B14 Doctores y Personal).</summary>
public class PersonalCreateViewModel
{
    [Required, MaxLength(200)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = null!;

    [Required, EmailAddress]
    [Display(Name = "Correo")]
    public string Email { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = null!;

    [Required]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = null!;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}

/// <summary>Para editar usuario desde Panel Clínica.</summary>
public class PersonalEditViewModel
{
    public string Id { get; set; } = null!;

    [Required, MaxLength(200)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = null!;

    [Display(Name = "Correo")]
    public string Email { get; set; } = null!; // Solo lectura en vista

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña (dejar en blanco para no cambiar)")]
    public string? NuevaPassword { get; set; }

    [Required]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = null!;

    [Display(Name = "Activo")]
    public bool Activo { get; set; }
}

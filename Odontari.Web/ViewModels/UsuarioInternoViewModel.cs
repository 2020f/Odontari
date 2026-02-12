using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

public class UsuarioInternoListViewModel
{
    public string Id { get; set; } = null!;
    public string? NombreCompleto { get; set; }
    public string Email { get; set; } = null!;
    public string? Rol { get; set; }
    public bool Activo { get; set; }
}

public class UsuarioInternoCreateViewModel
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

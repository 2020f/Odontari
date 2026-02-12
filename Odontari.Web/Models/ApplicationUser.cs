using Microsoft.AspNetCore.Identity;

namespace Odontari.Web.Models;

/// <summary>
/// Usuario de ODONTARI. ClinicaId es null para usuarios del panel SaaS (SuperAdmin, Soporte, Auditor).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Clínica a la que pertenece el usuario. Null = usuario interno SaaS.</summary>
    public int? ClinicaId { get; set; }

    public string? NombreCompleto { get; set; }
    public bool Activo { get; set; } = true;
}

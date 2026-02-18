namespace Odontari.Web.Models;

/// <summary>
/// Permiso de vista por usuario (clínica). Si un usuario no tiene ninguna fila, puede ver todas las vistas.
/// Si tiene filas, solo puede ver las vistas listadas aquí.
/// </summary>
public class UsuarioVistaPermiso
{
    public string UserId { get; set; } = null!;
    /// <summary>Clave de la vista (nombre del controlador en área Clinica).</summary>
    public string VistaKey { get; set; } = null!;

    public ApplicationUser? User { get; set; }
}

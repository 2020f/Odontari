namespace Odontari.Web.Services;

/// <summary>Servicio para permisos de vistas por usuario (clínica).</summary>
public interface IUsuarioVistasPermisoService
{
    /// <summary>Obtiene las claves de vistas permitidas para el usuario. Si es null, puede ver todas.</summary>
    Task<HashSet<string>?> GetVistasPermitidasAsync(string userId, CancellationToken ct = default);

    /// <summary>Indica si el usuario puede acceder a la vista (controlador).</summary>
    Task<bool> PuedeAccederAsync(string userId, string controllerName, CancellationToken ct = default);

    /// <summary>Guarda los permisos: solo las claves en permitidas. Si permitidas tiene todas las vistas, se borran filas (acceso total).</summary>
    Task GuardarPermisosAsync(string userId, IReadOnlyList<string> permitidas, CancellationToken ct = default);
}

namespace Odontari.Web.Services;

/// <summary>
/// Auditoría mínima V1: registrar acciones críticas.
/// </summary>
public interface IAuditService
{
    Task RegistrarAsync(int? clinicaId, string? userId, string accion, string? entidad = null, string? entidadId = null, string? detalle = null);
}

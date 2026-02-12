namespace Odontari.Web.Services;

/// <summary>
/// Proporciona el ClinicaId del usuario actual (panel clínica). Null para usuarios SaaS.
/// </summary>
public interface IClinicaActualService
{
    int? GetClinicaIdActual();
    Task<int?> GetClinicaIdActualAsync();
}

namespace Odontari.Web.Services;

/// <summary>
/// Valida si el usuario puede entrar al Panel Clínica: clínica activa + suscripción vigente.
/// </summary>
public interface IPuertaEntradaService
{
    /// <summary>Si puede entrar al panel clínica. Si no, motivoBloqueo indica el motivo.</summary>
    Task<(bool PuedeEntrar, string? MotivoBloqueo)> ValidarAccesoPanelClinicaAsync(int clinicaId);
}

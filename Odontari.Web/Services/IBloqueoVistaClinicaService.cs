namespace Odontari.Web.Services;

/// <summary>Servicio para bloqueo dinámico de vistas por clínica. Solo AdminClinica configura; Recepcion/Doctor/Finanzas quedan bloqueados según la tabla.</summary>
public interface IBloqueoVistaClinicaService
{
    /// <summary>Obtiene las claves de vistas bloqueadas para la clínica (solo las que tienen Bloqueada = true).</summary>
    Task<HashSet<string>> GetVistasBloqueadasAsync(int clinicaId, CancellationToken ct = default);

    /// <summary>Indica si la vista está bloqueada para la clínica (roles no AdminClinica no pueden acceder).</summary>
    Task<bool> EstaBloqueadaAsync(int clinicaId, string vistaKey, CancellationToken ct = default);

    /// <summary>Guarda el estado de bloqueo: lista de claves que deben estar bloqueadas; el resto quedan no bloqueadas.</summary>
    Task GuardarBloqueosAsync(int clinicaId, IReadOnlyList<string> vistasBloqueadas, CancellationToken ct = default);
}

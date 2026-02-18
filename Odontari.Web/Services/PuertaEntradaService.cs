using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;

namespace Odontari.Web.Services;

public class PuertaEntradaService : IPuertaEntradaService
{
    private readonly ApplicationDbContext _db;

    public PuertaEntradaService(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Valida si la clínica puede acceder al panel. Siempre valida por fecha de vencimiento.
    /// Vencida = el día de vencimiento o después (Vencimiento &lt;= hoy) → no se permite login.
    /// </summary>
    public async Task<(bool PuedeEntrar, string? MotivoBloqueo)> ValidarAccesoPanelClinicaAsync(int clinicaId)
    {
        var hoy = DateTime.Today; // Validación siempre por fecha, sin hora.

        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == clinicaId);
        if (clinica == null)
        {
            return (false, "Clínica no encontrada.");
        }
        if (!clinica.Activa)
        {
            return (false, "La clínica está inactiva. Contacte al administrador del sistema.");
        }

        // Vigente solo si la fecha de vencimiento es estrictamente posterior a hoy (el día de vencimiento ya está vencida).
        var suscripcionVigente = await _db.Suscripciones
            .Where(s => s.ClinicaId == clinicaId && s.Activa && !s.Suspendida && s.Vencimiento.Date > hoy)
            .OrderByDescending(s => s.Vencimiento)
            .FirstOrDefaultAsync();

        if (suscripcionVigente == null)
        {
            var ultima = await _db.Suscripciones
                .Where(s => s.ClinicaId == clinicaId)
                .OrderByDescending(s => s.Vencimiento)
                .FirstOrDefaultAsync();

            // Suspensión por vencimiento: si hoy es el día de vencimiento o ya pasó, no se permite el acceso.
            if (ultima != null && ultima.Vencimiento.Date <= hoy)
                return (false, "La suscripción está vencida (suspensión por vencimiento). Renueve para continuar.");

            if (ultima != null && ultima.Suspendida)
                return (false, "La suscripción está suspendida. Contacte al administrador.");
            return (false, "No hay suscripción vigente. Contacte al administrador del sistema.");
        }
        return (true, null);
    }
}

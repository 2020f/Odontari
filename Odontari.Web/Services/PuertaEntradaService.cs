using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;

namespace Odontari.Web.Services;

public class PuertaEntradaService : IPuertaEntradaService
{
    private readonly ApplicationDbContext _db;

    public PuertaEntradaService(ApplicationDbContext db) => _db = db;

    public async Task<(bool PuedeEntrar, string? MotivoBloqueo)> ValidarAccesoPanelClinicaAsync(int clinicaId)
    {
        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == clinicaId);
        if (clinica == null)
        {
            return (false, "Clínica no encontrada.");
        }
        if (!clinica.Activa)
        {
            return (false, "La clínica está inactiva. Contacte al administrador del sistema.");
        }
        var suscripcionVigente = await _db.Suscripciones
            .Where(s => s.ClinicaId == clinicaId && s.Activa && !s.Suspendida && s.Vencimiento >= DateTime.Today)
            .OrderByDescending(s => s.Vencimiento)
            .FirstOrDefaultAsync();
        if (suscripcionVigente == null)
        {
            var ultima = await _db.Suscripciones.Where(s => s.ClinicaId == clinicaId).OrderByDescending(s => s.Vencimiento).FirstOrDefaultAsync();
            if (ultima != null && ultima.Vencimiento < DateTime.Today)
                return (false, "La suscripción está vencida. Renueve para continuar.");
            if (ultima != null && ultima.Suspendida)
                return (false, "La suscripción está suspendida. Contacte al administrador.");
            return (false, "No hay suscripción vigente. Contacte al administrador del sistema.");
        }
        return (true, null);
    }
}

using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;

namespace Odontari.Web.Services;

public class BloqueoVistaClinicaService : IBloqueoVistaClinicaService
{
    private readonly ApplicationDbContext _db;

    public BloqueoVistaClinicaService(ApplicationDbContext db) => _db = db;

    public async Task<HashSet<string>> GetVistasBloqueadasAsync(int clinicaId, CancellationToken ct = default)
    {
        var list = await _db.BloqueoVistaClinicaDinamicas
            .Where(b => b.ClinicaId == clinicaId && b.Bloqueada)
            .Select(b => b.VistaKey)
            .ToListAsync(ct);
        return list.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> EstaBloqueadaAsync(int clinicaId, string vistaKey, CancellationToken ct = default)
    {
        var key = vistaKey?.Trim() ?? "";
        if (string.IsNullOrEmpty(key)) return false;
        var list = await _db.BloqueoVistaClinicaDinamicas
            .Where(b => b.ClinicaId == clinicaId && b.Bloqueada)
            .Select(b => b.VistaKey)
            .ToListAsync(ct);
        return list.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }

    public async Task GuardarBloqueosAsync(int clinicaId, IReadOnlyList<string> vistasBloqueadas, CancellationToken ct = default)
    {
        var clavesBloqueadas = (vistasBloqueadas ?? new List<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existentes = await _db.BloqueoVistaClinicaDinamicas
            .Where(b => b.ClinicaId == clinicaId)
            .ToListAsync(ct);
        _db.BloqueoVistaClinicaDinamicas.RemoveRange(existentes);

        var todasClaves = VistasClinica.Todas.Select(t => t.Key).ToList();
        foreach (var key in todasClaves)
        {
            var bloqueada = clavesBloqueadas.Contains(key, StringComparer.OrdinalIgnoreCase);
            _db.BloqueoVistaClinicaDinamicas.Add(new BloqueoVistaClinicaDinamica
            {
                ClinicaId = clinicaId,
                VistaKey = key,
                Bloqueada = bloqueada
            });
        }
        await _db.SaveChangesAsync(ct);
    }
}

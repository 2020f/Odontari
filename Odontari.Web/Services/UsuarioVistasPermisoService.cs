using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;

namespace Odontari.Web.Services;

public class UsuarioVistasPermisoService : IUsuarioVistasPermisoService
{
    private readonly ApplicationDbContext _db;

    public UsuarioVistasPermisoService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HashSet<string>?> GetVistasPermitidasAsync(string userId, CancellationToken ct = default)
    {
        var list = await _db.UsuarioVistaPermisos
            .Where(p => p.UserId == userId)
            .Select(p => p.VistaKey)
            .ToListAsync(ct);
        if (list.Count == 0)
            return null; // Sin restricción = todas permitidas
        return list.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indica si el usuario puede acceder al controlador (vista). Claves deben coincidir con VistasClinica (Home, Agenda, Pacientes, Expediente, Atencion, Tratamientos, Caja, Reportes, Personal).
    /// </summary>
    public async Task<bool> PuedeAccederAsync(string userId, string controllerName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(controllerName)) return true;
        var permitidas = await GetVistasPermitidasAsync(userId, ct);
        if (permitidas == null)
            return true; // Sin filas = todas las vistas permitidas (acceso total)
        var key = controllerName.Trim();
        if (permitidas.Contains(key, StringComparer.OrdinalIgnoreCase))
            return true;
        // El ítem "Expediente clínico" del menú apunta a Pacientes; permitir si tiene Expediente.
        if (string.Equals(key, "Pacientes", StringComparison.OrdinalIgnoreCase) && permitidas.Contains("Expediente", StringComparer.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public async Task GuardarPermisosAsync(string userId, IReadOnlyList<string> permitidas, CancellationToken ct = default)
    {
        var todas = VistasClinica.TodasLasClaves;
        var permitidasSet = permitidas?.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        var todasPermitidas = permitidasSet.Count == todas.Count && todas.All(k => permitidasSet.Contains(k, StringComparer.OrdinalIgnoreCase));
        var existentes = await _db.UsuarioVistaPermisos.Where(p => p.UserId == userId).ToListAsync(ct);
        _db.UsuarioVistaPermisos.RemoveRange(existentes);
        if (!todasPermitidas && permitidasSet.Count > 0)
        {
            var todasClaves = VistasClinica.Todas.Select(t => t.Key).ToList();
            foreach (var key in permitidasSet)
            {
                var claveCanonica = todasClaves.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (claveCanonica != null)
                    _db.UsuarioVistaPermisos.Add(new UsuarioVistaPermiso { UserId = userId, VistaKey = claveCanonica });
            }
        }
        await _db.SaveChangesAsync(ct);
    }
}

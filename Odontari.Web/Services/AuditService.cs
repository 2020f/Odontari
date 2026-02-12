using Odontari.Web.Data;
using Odontari.Web.Models;

namespace Odontari.Web.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task RegistrarAsync(int? clinicaId, string? userId, string accion, string? entidad = null, string? entidadId = null, string? detalle = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ClinicaId = clinicaId,
            UserId = userId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalle = detalle
        });
        await _db.SaveChangesAsync();
    }
}

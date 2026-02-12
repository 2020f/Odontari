namespace Odontari.Web.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? ClinicaId { get; set; }
    public string? UserId { get; set; }
    public string Accion { get; set; } = null!;
    public string? Entidad { get; set; }
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public DateTime CreadoAt { get; set; } = DateTime.UtcNow;
}

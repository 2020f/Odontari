namespace Odontari.Web.Models;

public class Tratamiento
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal PrecioBase { get; set; }
    public int DuracionMinutos { get; set; }
    public string? Categoria { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<ProcedimientoRealizado> ProcedimientosRealizados { get; set; } = new List<ProcedimientoRealizado>();
}

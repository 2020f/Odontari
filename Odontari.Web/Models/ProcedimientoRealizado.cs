namespace Odontari.Web.Models;

public class ProcedimientoRealizado
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public Cita Cita { get; set; } = null!;
    public int TratamientoId { get; set; }
    public Tratamiento Tratamiento { get; set; } = null!;

    public decimal PrecioAplicado { get; set; }
    public bool MarcadoRealizado { get; set; }
    public DateTime? RealizadoAt { get; set; }
    public string? Notas { get; set; }
}

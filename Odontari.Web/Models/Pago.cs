namespace Odontari.Web.Models;

public class Pago
{
    public int Id { get; set; }
    public int OrdenCobroId { get; set; }
    public OrdenCobro OrdenCobro { get; set; } = null!;

    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public string? RegistradoPorUserId { get; set; }
}

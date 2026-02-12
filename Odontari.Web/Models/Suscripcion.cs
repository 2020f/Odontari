namespace Odontari.Web.Models;

public class Suscripcion
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public DateTime Inicio { get; set; }
    public DateTime Vencimiento { get; set; }
    public bool Activa { get; set; } = true;
    public bool Suspendida { get; set; }
}

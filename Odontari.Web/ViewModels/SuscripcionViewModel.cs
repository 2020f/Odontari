namespace Odontari.Web.ViewModels;

public class SuscripcionListViewModel
{
    public int Id { get; set; }
    public int ClinicaId { get; set; }
    public string ClinicaNombre { get; set; } = null!;
    public DateTime Inicio { get; set; }
    public DateTime Vencimiento { get; set; }
    public bool Activa { get; set; }
    public bool Suspendida { get; set; }
}

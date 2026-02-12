namespace Odontari.Web.Models;

public class Clinica
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public ICollection<Suscripcion> Suscripciones { get; set; } = new List<Suscripcion>();
    public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<OrdenCobro> OrdenesCobro { get; set; } = new List<OrdenCobro>();
    public ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
}

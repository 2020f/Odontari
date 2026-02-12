using System.ComponentModel.DataAnnotations;
using Odontari.Web.Models.Enums;

namespace Odontari.Web.ViewModels;

public class CitaListViewModel
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string PacienteNombre { get; set; } = null!;
    public string? DoctorNombre { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Motivo { get; set; }
    public EstadoCita Estado { get; set; }
}

public class CitaEditViewModel
{
    public int Id { get; set; }
    [Required]
    public int PacienteId { get; set; }
    [Required]
    public string DoctorId { get; set; } = null!;
    [Required, DataType(DataType.DateTime)]
    public DateTime FechaHora { get; set; }
    public string? Motivo { get; set; }
}

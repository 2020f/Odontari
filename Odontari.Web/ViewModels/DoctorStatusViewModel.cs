namespace Odontari.Web.ViewModels;

public enum DoctorEstado
{
    Libre = 0,
    Ocupado = 1,
    FueraDeHorario = 2
}

/// <summary>
/// Estado en tiempo real de un doctor para el panel de Recepción.
/// </summary>
public class DoctorStatusViewModel
{
    public string DoctorId { get; set; } = null!;
    public string NombreCompleto { get; set; } = null!;

    public DoctorEstado Estado { get; set; }

    /// <summary>Nombre del paciente en atención activa (EnAtencion). Solo cuando Estado = Ocupado.</summary>
    public string? PacienteActual { get; set; }

    /// <summary>Próxima cita pendiente hoy: "HH:mm – Nombre Paciente".</summary>
    public string? ProximaCitaTexto { get; set; }

    /// <summary>Horario laboral formateado, ej. "08:00 – 17:00". Null si no está definido.</summary>
    public string? HorarioTexto { get; set; }
}

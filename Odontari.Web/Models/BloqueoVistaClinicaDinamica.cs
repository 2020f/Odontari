namespace Odontari.Web.Models;

/// <summary>Bloqueo dinámico de una vista del panel clínica. Solo AdminClinica puede configurarlo; Recepcion, Doctor, Finanzas quedan bloqueados si Bloqueada = true.</summary>
public class BloqueoVistaClinicaDinamica
{
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    public string VistaKey { get; set; } = null!; // Home, Agenda, Pacientes, Reportes, Caja, etc.

    /// <summary>1 = bloqueada para roles Recepcion/Doctor/Finanzas; 0 = no bloqueada.</summary>
    public bool Bloqueada { get; set; }
}

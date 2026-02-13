namespace Odontari.Web.ViewModels;

/// <summary>Vista Histograma: panel histórico + resumen clínico del paciente.</summary>
public class HistogramaViewModel
{
    public int PacienteId { get; set; }

    // --- Datos base del paciente ---
    public string Nombre { get; set; } = null!;
    public string? Apellidos { get; set; }
    public string? Cedula { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? NotasClinicas { get; set; }

    // --- Resumen rápido ---
    public string? UltimaVisita { get; set; }
    public string? UltimoDiagnostico { get; set; }
    public string? ProximoPaso { get; set; }

    // --- Timeline (histórico por fecha) ---
    public List<HistorialEventoViewModel> Timeline { get; set; } = new();

    // --- Resumen odontograma (conteos y últimos dientes) ---
    public ResumenOdontogramaViewModel? ResumenOdontograma { get; set; }
}

public class ResumenOdontogramaViewModel
{
    public int Caries { get; set; }
    public int Restauraciones { get; set; }
    public int Ausentes { get; set; }
    public int Endodoncia { get; set; }
    public int Otros { get; set; }
    public int TotalHallazgos { get; set; }
    public List<int> UltimosDientesConHallazgo { get; set; } = new();
    public DateTime? UltimaActualizacion { get; set; }
}

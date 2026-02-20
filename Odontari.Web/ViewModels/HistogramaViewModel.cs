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

    // --- Resumen rápido / Resumen periodontograma ---
    public string? UltimaVisita { get; set; }
    public string? UltimoDiagnostico { get; set; }
    public string? ProximoPaso { get; set; }
    /// <summary>Si true, el bloque de último diagnóstico muestra "Resumen del periodontograma" en lugar de "Resumen rápido".</summary>
    public bool UltimoDiagnosticoEsPeriodontograma { get; set; }

    // --- Timeline (histórico por fecha) ---
    public List<HistorialEventoViewModel> Timeline { get; set; } = new();

    // --- Resumen odontograma (conteos y últimos dientes) ---
    public ResumenOdontogramaViewModel? ResumenOdontograma { get; set; }

    /// <summary>Historia clínica sistemática (antecedentes médicos) del paciente, para mostrar en Datos del paciente.</summary>
    public HistoriaClinicaSistematicaResumenViewModel? HistoriaClinicaSistematica { get; set; }
}

/// <summary>Resumen de historia clínica sistemática para mostrar en Histograma / Datos del paciente.</summary>
public class HistoriaClinicaSistematicaResumenViewModel
{
    public bool TieneDatos { get; set; }
    public bool? AlergiasMedicamentos { get; set; }
    public string? AlergiasCuales { get; set; }
    public bool? AsmaBronquial { get; set; }
    public bool? ConvulsionesEpilepsia { get; set; }
    public bool? Diabetes { get; set; }
    public bool? EnfermedadesCardiacas { get; set; }
    public bool? Embarazo { get; set; }
    public int? EmbarazoSemanas { get; set; }
    public bool? EnfermedadesVenereas { get; set; }
    public bool? FiebreReumatica { get; set; }
    public bool? Hepatitis { get; set; }
    public string? HepatitisCual { get; set; }
    public bool? ProblemasNeurologicos { get; set; }
    public bool? ProblemasRenales { get; set; }
    public bool? ProblemasSinusales { get; set; }
    public bool? SangradoExcesivo { get; set; }
    public bool? TrastornosPsiquiatricos { get; set; }
    public bool? TrastornosDigestivos { get; set; }
    public bool? TumoresBenignosMalignos { get; set; }
    public string? TumoresCuales { get; set; }
    public bool? TrastornosRespiratorios { get; set; }
    public string? TrastornosRespiratoriosCuales { get; set; }
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
    /// <summary>Lista de hallazgos en formato "Diente 14 (oclusal): OBTURACION", "Diente 27: AUSENTE", etc.</summary>
    public List<string> ListaHallazgos { get; set; } = new();
    public DateTime? UltimaActualizacion { get; set; }
}

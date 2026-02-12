namespace Odontari.Web.Models;

/// <summary>
/// Historia clínica sistemática: 20 preguntas de antecedentes médicos
/// para el tratamiento odontológico. Un registro por paciente.
/// </summary>
public class HistoriaClinicaSistematica
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;
    public int ClinicaId { get; set; }
    public Clinica Clinica { get; set; } = null!;

    // 1-2 Alergias
    public bool? AlergiasMedicamentos { get; set; }
    public string? AlergiasCuales { get; set; }

    // 3-10
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

    // 11-16
    public bool? ProblemasNeurologicos { get; set; }
    public bool? ProblemasRenales { get; set; }
    public bool? ProblemasSinusales { get; set; }
    public bool? SangradoExcesivo { get; set; }
    public bool? TrastornosPsiquiatricos { get; set; }
    public bool? TrastornosDigestivos { get; set; }

    // 17-18 Tumores
    public bool? TumoresBenignosMalignos { get; set; }
    public string? TumoresCuales { get; set; }

    // 19-20 Trastornos respiratorios
    public bool? TrastornosRespiratorios { get; set; }
    public string? TrastornosRespiratoriosCuales { get; set; }

    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

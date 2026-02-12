using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

public class HistoriaClinicaSistematicaViewModel
{
    public int PacienteId { get; set; }

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

    // 17-18
    public bool? TumoresBenignosMalignos { get; set; }
    public string? TumoresCuales { get; set; }

    // 19-20
    public bool? TrastornosRespiratorios { get; set; }
    public string? TrastornosRespiratoriosCuales { get; set; }

    public static HistoriaClinicaSistematicaViewModel FromEntity(Models.HistoriaClinicaSistematica h)
    {
        return new HistoriaClinicaSistematicaViewModel
        {
            PacienteId = h.PacienteId,
            AlergiasMedicamentos = h.AlergiasMedicamentos,
            AlergiasCuales = h.AlergiasCuales,
            AsmaBronquial = h.AsmaBronquial,
            ConvulsionesEpilepsia = h.ConvulsionesEpilepsia,
            Diabetes = h.Diabetes,
            EnfermedadesCardiacas = h.EnfermedadesCardiacas,
            Embarazo = h.Embarazo,
            EmbarazoSemanas = h.EmbarazoSemanas,
            EnfermedadesVenereas = h.EnfermedadesVenereas,
            FiebreReumatica = h.FiebreReumatica,
            Hepatitis = h.Hepatitis,
            HepatitisCual = h.HepatitisCual,
            ProblemasNeurologicos = h.ProblemasNeurologicos,
            ProblemasRenales = h.ProblemasRenales,
            ProblemasSinusales = h.ProblemasSinusales,
            SangradoExcesivo = h.SangradoExcesivo,
            TrastornosPsiquiatricos = h.TrastornosPsiquiatricos,
            TrastornosDigestivos = h.TrastornosDigestivos,
            TumoresBenignosMalignos = h.TumoresBenignosMalignos,
            TumoresCuales = h.TumoresCuales,
            TrastornosRespiratorios = h.TrastornosRespiratorios,
            TrastornosRespiratoriosCuales = h.TrastornosRespiratoriosCuales
        };
    }

    public Models.HistoriaClinicaSistematica ToEntity(int pacienteId, int clinicaId)
    {
        return new Models.HistoriaClinicaSistematica
        {
            PacienteId = pacienteId,
            ClinicaId = clinicaId,
            AlergiasMedicamentos = AlergiasMedicamentos,
            AlergiasCuales = AlergiasCuales,
            AsmaBronquial = AsmaBronquial,
            ConvulsionesEpilepsia = ConvulsionesEpilepsia,
            Diabetes = Diabetes,
            EnfermedadesCardiacas = EnfermedadesCardiacas,
            Embarazo = Embarazo,
            EmbarazoSemanas = EmbarazoSemanas,
            EnfermedadesVenereas = EnfermedadesVenereas,
            FiebreReumatica = FiebreReumatica,
            Hepatitis = Hepatitis,
            HepatitisCual = HepatitisCual,
            ProblemasNeurologicos = ProblemasNeurologicos,
            ProblemasRenales = ProblemasRenales,
            ProblemasSinusales = ProblemasSinusales,
            SangradoExcesivo = SangradoExcesivo,
            TrastornosPsiquiatricos = TrastornosPsiquiatricos,
            TrastornosDigestivos = TrastornosDigestivos,
            TumoresBenignosMalignos = TumoresBenignosMalignos,
            TumoresCuales = TumoresCuales,
            TrastornosRespiratorios = TrastornosRespiratorios,
            TrastornosRespiratoriosCuales = TrastornosRespiratoriosCuales
        };
    }

    public void ApplyToEntity(Models.HistoriaClinicaSistematica h)
    {
        h.AlergiasMedicamentos = AlergiasMedicamentos;
        h.AlergiasCuales = AlergiasCuales;
        h.AsmaBronquial = AsmaBronquial;
        h.ConvulsionesEpilepsia = ConvulsionesEpilepsia;
        h.Diabetes = Diabetes;
        h.EnfermedadesCardiacas = EnfermedadesCardiacas;
        h.Embarazo = Embarazo;
        h.EmbarazoSemanas = EmbarazoSemanas;
        h.EnfermedadesVenereas = EnfermedadesVenereas;
        h.FiebreReumatica = FiebreReumatica;
        h.Hepatitis = Hepatitis;
        h.HepatitisCual = HepatitisCual;
        h.ProblemasNeurologicos = ProblemasNeurologicos;
        h.ProblemasRenales = ProblemasRenales;
        h.ProblemasSinusales = ProblemasSinusales;
        h.SangradoExcesivo = SangradoExcesivo;
        h.TrastornosPsiquiatricos = TrastornosPsiquiatricos;
        h.TrastornosDigestivos = TrastornosDigestivos;
        h.TumoresBenignosMalignos = TumoresBenignosMalignos;
        h.TumoresCuales = TumoresCuales;
        h.TrastornosRespiratorios = TrastornosRespiratorios;
        h.TrastornosRespiratoriosCuales = TrastornosRespiratoriosCuales;
    }
}

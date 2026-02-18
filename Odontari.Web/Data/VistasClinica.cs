namespace Odontari.Web.Data;

/// <summary>Vistas (módulos) del área Clinica que se pueden limitar por usuario.</summary>
public static class VistasClinica
{
    /// <summary>Navegación principal (orden en la UI).</summary>
    public static readonly IReadOnlyList<(string Key, string Nombre)> NavegacionPrincipal = new[]
    {
        ("Home", "Dashboard"),
        ("Agenda", "Agenda"),
        ("Pacientes", "Pacientes"),
        ("Expediente", "Expediente clínico"),
        ("Atencion", "Mis citas")
    };

    /// <summary>Operaciones, cobros y reportes (vistas que suelen limitarse: Reportes, Cobros/Caja, Tratamientos, Expediente).</summary>
    public static readonly IReadOnlyList<(string Key, string Nombre)> OperacionesYReportes = new[]
    {
        ("Tratamientos", "Tratamientos"),
        ("Caja", "Cobros / Caja"),
        ("Reportes", "Reportes"),
        ("Personal", "Configuración")
    };

    /// <summary>Todas las vistas en orden: primero navegación, luego operaciones.</summary>
    public static readonly IReadOnlyList<(string Key, string Nombre)> Todas =
        NavegacionPrincipal.Concat(OperacionesYReportes).ToList();

    public static IReadOnlyList<string> TodasLasClaves => Todas.Select(t => t.Key).ToList();

    /// <summary>Nombre para mostrar de una vista por su clave (controlador).</summary>
    public static string NombrePorClave(string key)
    {
        var t = Todas.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        return t.Nombre ?? key;
    }
}

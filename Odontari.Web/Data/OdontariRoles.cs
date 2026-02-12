namespace Odontari.Web.Data;

/// <summary>
/// Roles de ODONTARI. SaaS: sin ClinicaId. Clínica: requieren ClinicaId.
/// </summary>
public static class OdontariRoles
{
    // Panel SaaS (SuperAdmin)
    public const string SuperAdmin = "SuperAdmin";
    public const string Soporte = "Soporte";
    public const string Auditor = "Auditor";

    // Panel Clínica (por tenant)
    public const string AdminClinica = "AdminClinica";
    public const string Recepcion = "Recepcion";
    public const string Doctor = "Doctor";
    public const string Finanzas = "Finanzas";

    public static string[] Todos => new[]
    {
        SuperAdmin, Soporte, Auditor,
        AdminClinica, Recepcion, Doctor, Finanzas
    };

    public static string[] RolesSaaS => new[] { SuperAdmin, Soporte, Auditor };
    public static string[] RolesClinica => new[] { AdminClinica, Recepcion, Doctor, Finanzas };
}

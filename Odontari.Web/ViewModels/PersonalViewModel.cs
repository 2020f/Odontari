using System.ComponentModel.DataAnnotations;

namespace Odontari.Web.ViewModels;

/// <summary>Para crear usuario desde Panel Clínica (B14 Doctores y Personal).</summary>
public class PersonalCreateViewModel
{
    [Required, MaxLength(200)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = null!;

    [Required, EmailAddress]
    [Display(Name = "Correo")]
    public string Email { get; set; } = null!;

    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = null!;

    [Required]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = null!;

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    /// <summary>Horario laboral (solo doctores): hora de entrada. Ej. 08:00.</summary>
    [Display(Name = "Hora de entrada")]
    public TimeSpan? HoraEntrada { get; set; } = new TimeSpan(8, 0, 0);
    /// <summary>Horario laboral (solo doctores): hora de salida. Ej. 17:00.</summary>
    [Display(Name = "Hora de salida")]
    public TimeSpan? HoraSalida { get; set; } = new TimeSpan(17, 0, 0);
}

/// <summary>Para editar usuario desde Panel Clínica.</summary>
public class PersonalEditViewModel
{
    public string Id { get; set; } = null!;

    [Required, MaxLength(200)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = null!;

    [Display(Name = "Correo")]
    public string Email { get; set; } = null!; // Solo lectura en vista

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña (dejar en blanco para no cambiar)")]
    public string? NuevaPassword { get; set; }

    [Required]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = null!;

    [Display(Name = "Activo")]
    public bool Activo { get; set; }

    /// <summary>Horario laboral (solo doctores): hora de entrada.</summary>
    [Display(Name = "Hora de entrada")]
    public TimeSpan? HoraEntrada { get; set; }
    /// <summary>Horario laboral (solo doctores): hora de salida.</summary>
    [Display(Name = "Hora de salida")]
    public TimeSpan? HoraSalida { get; set; }

    /// <summary>Permisos de vistas: cada item con Permitido = true significa que el usuario puede ver esa sección.</summary>
    public List<VistaPermisoItem> PermisosVistas { get; set; } = new();
}

/// <summary>Una vista (módulo) del área Clinica con su estado permitido para un usuario.</summary>
public class VistaPermisoItem
{
    public string VistaKey { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public bool Permitido { get; set; }
}

/// <summary>Una vista del área Clinica. Visible = true significa vista desbloqueada (switch ON); Visible = false significa bloqueada (switch OFF).</summary>
public class BloqueoVistaClinicaItem
{
    public string VistaKey { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    /// <summary>True = vista visible para la clínica (switch ON). False = vista bloqueada (switch OFF).</summary>
    public bool Visible { get; set; }
}

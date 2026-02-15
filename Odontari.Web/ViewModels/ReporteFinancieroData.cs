namespace Odontari.Web.ViewModels;

/// <summary>Datos para el reporte financiero (Excel/PDF): encabezado, resumen, detalle, por doctor, tratamientos, cuentas por cobrar.</summary>
public class ReporteFinancieroData
{
    public EncabezadoReporte Encabezado { get; set; } = new();
    public ResumenFinanciero Resumen { get; set; } = new();
    public List<DetalleIngresoRow> DetalleIngresos { get; set; } = new();
    public List<ProduccionDoctorRow> ProduccionPorDoctor { get; set; } = new();
    public List<TratamientoVendidoRow> TratamientosMasVendidos { get; set; } = new();
    public List<CuentaPorCobrarRow> CuentasPorCobrar { get; set; } = new();
}

public class EncabezadoReporte
{
    public string NombreClinica { get; set; } = "";
    public string RNC { get; set; } = "N/A";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public DateTime FechaGeneracion { get; set; }
    public string RangoFechas { get; set; } = "";
    public string UsuarioGenero { get; set; } = "";
}

public class ResumenFinanciero
{
    public decimal TotalFacturado { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal TotalPendiente { get; set; }
    public decimal TotalAnulado { get; set; }
    public decimal DescuentosAplicados { get; set; }
    public decimal TotalNetoReal { get; set; }
}

public class DetalleIngresoRow
{
    public DateTime Fecha { get; set; }
    public int? NumeroCita { get; set; }
    public string Paciente { get; set; } = "";
    public string Doctor { get; set; } = "";
    public string Tratamiento { get; set; } = "";
    public string MetodoPago { get; set; } = "";
    public decimal MontoTotal { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Estado { get; set; } = "";
}

public class ProduccionDoctorRow
{
    public string Doctor { get; set; } = "";
    public decimal TotalFacturado { get; set; }
    public decimal TotalCobrado { get; set; }
    public int CantidadPacientesAtendidos { get; set; }
}

public class TratamientoVendidoRow
{
    public string Tratamiento { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal TotalGenerado { get; set; }
}

public class CuentaPorCobrarRow
{
    public string Paciente { get; set; } = "";
    public decimal TotalPendiente { get; set; }
    public DateTime? UltimaFechaAtencion { get; set; }
    public int? DiasVencidos { get; set; }
}

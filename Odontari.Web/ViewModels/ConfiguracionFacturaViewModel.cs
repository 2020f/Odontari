using Odontari.Web.Models.Enums;

namespace Odontari.Web.ViewModels;

/// <summary>Datos fiscales de la clínica (emisor) para factura.</summary>
public class DatosFiscalesViewModel
{
    public string? RNC { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? DireccionFiscal { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
}

/// <summary>Configuración de impuestos y formato de factura.</summary>
public class ConfiguracionImpuestosFormatoViewModel
{
    public decimal ItbisTasa { get; set; } = 18;
    public bool ItbisAplicarPorDefecto { get; set; } = true;
    public string? MensajeFactura { get; set; }
    public string? CondicionesPago { get; set; }
    public string? NotaLegal { get; set; }
    public bool MostrarFirma { get; set; }
    public bool MostrarQR { get; set; }
}

/// <summary>Modo de facturación: Interna vs Fiscal.</summary>
public class ModoFacturacionViewModel
{
    public ModoFacturacion ModoFacturacion { get; set; } = ModoFacturacion.Interna;
    public bool PermitirInternaConFiscal { get; set; } = true;
}

/// <summary>Formas de pago activas.</summary>
public class FormasPagoViewModel
{
    public bool FormaPagoEfectivo { get; set; } = true;
    public bool FormaPagoTransferencia { get; set; } = true;
    public bool FormaPagoTarjeta { get; set; } = true;
    public bool FormaPagoCredito { get; set; } = true;
    public bool FormaPagoMixto { get; set; } = true;
}

/// <summary>Item de rango NCF para listado.</summary>
public class NCFRangoItemViewModel
{
    public int Id { get; set; }
    public string TipoCodigo { get; set; } = null!;
    public string TipoNombre { get; set; } = null!;
    public string Desde { get; set; } = null!;
    public string Hasta { get; set; } = null!;
    public long Proximo { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime? FechaVencimiento { get; set; }
    public int PorcentajeConsumido { get; set; }
}

/// <summary>Crear/editar rango NCF.</summary>
public class NCFRangoEditViewModel
{
    public int Id { get; set; }
    public int NCFTipoId { get; set; }
    public string? SeriePrefijo { get; set; }
    public string Desde { get; set; } = null!;
    public string Hasta { get; set; } = null!;
    public DateTime? FechaAutorizacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? Nota { get; set; }
}

/// <summary>Item de bitácora NCF.</summary>
public class NCFBitacoraItemViewModel
{
    public int Id { get; set; }
    public string NCFGenerado { get; set; } = null!;
    public int? FacturaId { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaHora { get; set; }
    public string? Motivo { get; set; }
    public string? TipoCodigo { get; set; }
    public string? UsuarioNombre { get; set; }
}

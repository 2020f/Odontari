namespace Odontari.Web.Services;

/// <summary>Crea factura para una orden de cobro si aún no existe (al registrar el primer pago).</summary>
public interface IFacturaService
{
    /// <summary>Si la orden no tiene factura, crea una (interna o fiscal según configuración de la clínica) y la asocia. Devuelve el Id de la factura creada o null si ya existía.</summary>
    Task<int?> CrearFacturaSiNoExisteAsync(int ordenCobroId, string? formaPago, string? usuarioId, CancellationToken ct = default);
}

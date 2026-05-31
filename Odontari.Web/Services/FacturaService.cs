using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Models.Enums;
using System.Data;

namespace Odontari.Web.Services;

public class FacturaService : IFacturaService
{
    private readonly ApplicationDbContext _db;

    public FacturaService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int?> CrearFacturaSiNoExisteAsync(int ordenCobroId, int clinicaId, string? formaPago, string? usuarioId, CancellationToken ct = default)
    {
        // La transacción Serializable bloquea la lectura del MAX hasta que esta
        // operación complete, evitando que dos requests concurrentes tomen el mismo
        // NumeroInterno o el mismo NCF.
        using var tran = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var orden = await _db.OrdenesCobro
                .Include(o => o.Paciente)
                .Include(o => o.Factura)
                .Include(o => o.Clinica)
                .Include(o => o.Cita)
                .FirstOrDefaultAsync(o => o.Id == ordenCobroId && o.ClinicaId == clinicaId, ct);

            // Re-verificar dentro de la transacción: otro request puede haber creado la factura
            // en el tiempo entre la carga y la adquisición del lock.
            if (orden == null || orden.Factura != null)
            {
                await tran.RollbackAsync(ct);
                return orden?.Factura?.Id;
            }

            var clinica = orden.Clinica;
            if (clinica == null)
                clinica = await _db.Clinicas.FirstOrDefaultAsync(c => c.Id == orden.ClinicaId, ct);
            if (clinica == null)
            {
                await tran.RollbackAsync(ct);
                return null;
            }

            // NumeroInterno: calculado dentro de la transacción serializable para que
            // ningún otro request pueda leer el mismo máximo antes de que este commit.
            var numeroInterno = await _db.Facturas
                .Where(f => f.ClinicaId == orden.ClinicaId)
                .MaxAsync(f => (int?)f.NumeroInterno, ct) ?? 0;
            numeroInterno++;

            decimal subtotal, itbis, total = orden.Total;
            if (clinica.ItbisAplicarPorDefecto && clinica.ItbisTasa > 0)
            {
                subtotal = Math.Round(total / (1 + clinica.ItbisTasa / 100), 2);
                itbis = total - subtotal;
            }
            else
            {
                subtotal = total;
                itbis = 0;
            }

            string? ncf = null;
            int? ncfTipoId = null;
            var tipoDoc = TipoDocumentoFactura.Interna;
            NCFRango? rangoFiscal = null;
            if (clinica.ModoFacturacion == ModoFacturacion.Fiscal)
            {
                // El lock serializable también protege la lectura y el incremento de Proximo,
                // garantizando que cada NCF se asigne una sola vez.
                var rango = await _db.NCFRangos
                    .Include(r => r.NCFTipo)
                    .Where(r => r.ClinicaId == orden.ClinicaId && r.Estado == EstadoNCFRango.Activo)
                    .OrderBy(r => r.NCFTipo!.Codigo)
                    .FirstOrDefaultAsync(ct);
                if (rango != null
                    && long.TryParse(rango.Hasta, out var hastaNum)
                    && rango.Proximo <= hastaNum)
                {
                    var ncfNum = rango.Proximo;
                    var len = rango.Hasta.TrimStart('0').Length == 0 ? rango.Hasta.Length : rango.Hasta.Length;
                    ncf = (rango.SeriePrefijo ?? "") + ncfNum.ToString("D" + len);
                    ncfTipoId = rango.NCFTipoId;
                    tipoDoc = TipoDocumentoFactura.Fiscal;
                    rango.Proximo++;
                    if (rango.Proximo > hastaNum)
                        rango.Estado = EstadoNCFRango.Agotado;
                    rangoFiscal = rango;
                }
            }

            var factura = new Factura
            {
                ClinicaId = orden.ClinicaId,
                NumeroInterno = numeroInterno,
                TipoDocumento = tipoDoc,
                NCFTipoId = ncfTipoId,
                NCF = ncf,
                Estado = EstadoFactura.Emitida,
                FechaEmision = DateTime.Now,
                Subtotal = subtotal,
                Itbis = itbis,
                Total = total,
                PacienteId = orden.PacienteId,
                CitaId = orden.CitaId,
                OrdenCobroId = orden.Id,
                FormaPago = formaPago?.Trim(),
                CreadoAt = DateTime.UtcNow,
                UsuarioId = usuarioId
            };
            _db.Facturas.Add(factura);
            await _db.SaveChangesAsync(ct);

            if (rangoFiscal != null && !string.IsNullOrEmpty(ncf))
            {
                _db.NCFMovimientos.Add(new NCFMovimiento
                {
                    ClinicaId = orden.ClinicaId,
                    NCFTipoId = rangoFiscal.NCFTipoId,
                    NCFGenerado = ncf,
                    FacturaId = factura.Id,
                    Estado = EstadoNCFMovimiento.Emitido,
                    UsuarioId = usuarioId,
                    FechaHora = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
            }

            await tran.CommitAsync(ct);
            return factura.Id;
        }
        catch
        {
            await tran.RollbackAsync(ct);
            throw;
        }
    }
}

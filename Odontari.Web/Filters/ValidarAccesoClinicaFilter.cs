using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Odontari.Web.Data;
using Odontari.Web.Services;

namespace Odontari.Web.Filters;

/// <summary>
/// Valida puerta de entrada al Panel Clínica: clínica activa + suscripción vigente.
/// Si falla, redirige a Bloqueo.
/// </summary>
public class ValidarAccesoClinicaFilter : IAsyncActionFilter
{
    private readonly IClinicaActualService _clinicaActual;
    private readonly IPuertaEntradaService _puertaEntrada;
    private readonly IUsuarioVistasPermisoService _vistasPermiso;

    public ValidarAccesoClinicaFilter(
        IClinicaActualService clinicaActual,
        IPuertaEntradaService puertaEntrada,
        IUsuarioVistasPermisoService vistasPermiso)
    {
        _clinicaActual = clinicaActual;
        _puertaEntrada = puertaEntrada;
        _vistasPermiso = vistasPermiso;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var area = context.RouteData.Values["area"] as string;
        if (area != "Clinica")
        {
            await next();
            return;
        }
        var controller = context.RouteData.Values["controller"] as string;
        var action = context.RouteData.Values["action"] as string;
        if (controller == "Home" && (action == "Bloqueo" || action == "SinClinica" || action == "VistaNoPermitida"))
        {
            await next();
            return;
        }
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null)
        {
            await next();
            return;
        }
        var (puedeEntrar, motivoBloqueo) = await _puertaEntrada.ValidarAccesoPanelClinicaAsync(cid.Value);
        if (!puedeEntrar)
        {
            context.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "area", "Clinica" },
                    { "controller", "Home" },
                    { "action", "Bloqueo" },
                    { "motivo", motivoBloqueo ?? "Acceso no permitido." }
                });
            return;
        }
        // Validar límite de vistas por usuario (configuración de la clínica)
        var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(controller))
        {
            var puedeAcceder = await _vistasPermiso.PuedeAccederAsync(userId, controller);
            if (!puedeAcceder)
            {
                context.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "area", "Clinica" },
                        { "controller", "Home" },
                        { "action", "VistaNoPermitida" },
                        { "vista", controller }
                    });
                return;
            }
        }
        await next();
    }
}

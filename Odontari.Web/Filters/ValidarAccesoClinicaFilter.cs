using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Odontari.Web.Data;
using Odontari.Web.Services;

namespace Odontari.Web.Filters;

/// <summary>
/// Valida puerta de entrada al Panel Clínica: clínica activa + suscripción vigente.
/// El bloqueo de vistas por clínica lo hace ValidarVistaPermisoAuthorizationFilter (BloqueoVistaClinicaDinamica).
/// </summary>
public class ValidarAccesoClinicaFilter : IAsyncActionFilter
{
    private readonly IClinicaActualService _clinicaActual;
    private readonly IPuertaEntradaService _puertaEntrada;

    public ValidarAccesoClinicaFilter(IClinicaActualService clinicaActual, IPuertaEntradaService puertaEntrada)
    {
        _clinicaActual = clinicaActual;
        _puertaEntrada = puertaEntrada;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var area = context.RouteData.Values["area"] as string;
        if (!string.Equals(area, "Clinica", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Nombre del controlador desde el descriptor de la acción (fiable para áreas: Reportes, Caja, etc.)
        var controller = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;
        if (string.IsNullOrEmpty(controller))
        {
            controller = context.RouteData.Values["controller"] as string;
            if (string.IsNullOrEmpty(controller) && context.ActionDescriptor.RouteValues.TryGetValue("controller", out var ctrl))
                controller = ctrl;
        }
        var action = (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName
            ?? context.RouteData.Values["action"] as string;

        if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(action, "Bloqueo", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(action, "SinClinica", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(action, "VistaNoPermitida", StringComparison.OrdinalIgnoreCase)))
        {
            await next();
            return;
        }

        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid != null)
        {
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
        }

        await next();
    }
}

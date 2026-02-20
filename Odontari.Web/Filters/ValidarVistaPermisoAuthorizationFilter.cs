using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Odontari.Web.Data;
using Odontari.Web.Services;

namespace Odontari.Web.Filters;

/// <summary>
/// Filtro de autorización: bloqueo en dos niveles (SaaS multitenant).
/// 1) Por clínica: BloqueoVistaClinicaDinamica — AdminClinica/SuperAdmin ignoran; Recepcion/Doctor/Finanzas no entran si la vista está bloqueada para la clínica.
/// 2) Por usuario: UsuarioVistaPermiso (permisos en Personal/Edit) — si el usuario tiene restricciones, solo puede acceder a las vistas permitidas.
/// </summary>
public class ValidarVistaPermisoAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IClinicaActualService _clinicaActual;
    private readonly IBloqueoVistaClinicaService _bloqueoVistas;
    private readonly IUsuarioVistasPermisoService _vistasPermiso;

    public ValidarVistaPermisoAuthorizationFilter(
        IClinicaActualService clinicaActual,
        IBloqueoVistaClinicaService bloqueoVistas,
        IUsuarioVistasPermisoService vistasPermiso)
    {
        _clinicaActual = clinicaActual;
        _bloqueoVistas = bloqueoVistas;
        _vistasPermiso = vistasPermiso;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var area = context.RouteData.Values["area"] as string;
        if (!string.Equals(area, "Clinica", StringComparison.OrdinalIgnoreCase))
            return;

        var controller = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;
        if (string.IsNullOrEmpty(controller))
            controller = context.RouteData.Values["controller"] as string;
        if (string.IsNullOrEmpty(controller))
            return;

        var action = (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName
            ?? context.RouteData.Values["action"] as string;

        if (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(action, "Bloqueo", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(action, "SinClinica", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(action, "VistaNoPermitida", StringComparison.OrdinalIgnoreCase)))
            return;

        if (context.HttpContext.User?.IsInRole(OdontariRoles.SuperAdmin) == true)
            return;
        if (context.HttpContext.User?.IsInRole(OdontariRoles.AdminClinica) == true)
            return;

        var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return;

        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null)
            return;

        var controllerKey = controller.Trim();

        // 1) Bloqueo por clínica: vista bloqueada para la clínica → no puede entrar (Recepcion/Doctor/Finanzas)
        var estaBloqueadaClinica = await _bloqueoVistas.EstaBloqueadaAsync(cid.Value, controllerKey);
        if (estaBloqueadaClinica)
        {
            context.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "area", "Clinica" },
                    { "controller", "Home" },
                    { "action", "VistaNoPermitida" },
                    { "vista", controllerKey }
                });
            return;
        }

        // 2) Bloqueo por usuario: permisos de vistas (Personal/Edit) — si tiene restricciones y esta vista no está permitida → no puede entrar
        var puedeAccederUsuario = await _vistasPermiso.PuedeAccederAsync(userId, controllerKey);
        if (!puedeAccederUsuario)
        {
            context.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "area", "Clinica" },
                    { "controller", "Home" },
                    { "action", "VistaNoPermitida" },
                    { "vista", controllerKey }
                });
            return;
        }
    }
}

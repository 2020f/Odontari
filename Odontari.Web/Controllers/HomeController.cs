using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Odontari.Web.Data;
using Odontari.Web.Models;

namespace Odontari.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>Mensaje de acceso denegado (sin redirección a otra app). Permite anónimo para poder mostrarlo.</summary>
    [AllowAnonymous]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return View();

        // Redirect clinic users directly — avoids cold-start DB failures in the view
        if (user.ClinicaId != null)
            return RedirectToAction("Index", "Home", new { area = "Clinica" });

        if (await _userManager.IsInRoleAsync(user, OdontariRoles.SuperAdmin))
            return RedirectToAction("Index", "Dashboard", new { area = "Saas" });

        ViewBag.NombreUsuario = user.NombreCompleto ?? user.Email ?? User.Identity?.Name ?? "Usuario";
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

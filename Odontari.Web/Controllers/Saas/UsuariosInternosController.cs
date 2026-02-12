using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Controllers.Saas;

[Authorize(Roles = OdontariRoles.SuperAdmin)]
[Area("Saas")]
public class UsuariosInternosController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public UsuariosInternosController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    /// <summary>Lista usuarios internos SaaS (ClinicaId == null, rol SuperAdmin/Soporte/Auditor).</summary>
    public async Task<IActionResult> Index()
    {
        var userIds = await _db.Users.Where(u => u.ClinicaId == null).Select(u => u.Id).ToListAsync();
        if (!userIds.Any())
        {
            return View(new List<UsuarioInternoListViewModel>());
        }
        var userRoles = await _db.UserRoles.Where(ur => userIds.Contains(ur.UserId)).ToListAsync();
        var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
        var roles = await _db.Roles.Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name ?? "");
        var users = await _db.Users.Where(u => u.ClinicaId == null).ToListAsync();

        var list = users.Select(u =>
        {
            var ur = userRoles.FirstOrDefault(ur => ur.UserId == u.Id);
            var rol = ur != null && roles.TryGetValue(ur.RoleId, out var name) ? name : null;
            return new UsuarioInternoListViewModel
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email ?? "",
                Rol = rol,
                Activo = u.Activo
            };
        }).OrderBy(u => u.Email).ToList();

        return View(list);
    }

    public IActionResult Create()
    {
        ViewBag.RolesSaaS = OdontariRoles.RolesSaaS;
        return View(new UsuarioInternoCreateViewModel { Activo = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioInternoCreateViewModel vm)
    {
        if (!OdontariRoles.RolesSaaS.Contains(vm.Rol))
            ModelState.AddModelError(nameof(vm.Rol), "Rol no válido para usuario interno.");

        if (await _userManager.FindByEmailAsync(vm.Email) != null)
            ModelState.AddModelError(nameof(vm.Email), "Ya existe un usuario con este correo.");

        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true,
                NombreCompleto = vm.NombreCompleto,
                ClinicaId = null,
                Activo = vm.Activo
            };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                ViewBag.RolesSaaS = OdontariRoles.RolesSaaS;
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, vm.Rol);
            await _audit.RegistrarAsync(null, null, "UsuarioInterno_Creado", "Usuario", user.Id, vm.Email);
            TempData["Message"] = "Usuario interno creado.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.RolesSaaS = OdontariRoles.RolesSaaS;
        return View(vm);
    }
}

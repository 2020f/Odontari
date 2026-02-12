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
public class UsuariosClinicaController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public UsuariosClinicaController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    /// <summary>Lista de clínicas para elegir y ver sus usuarios.</summary>
    public async Task<IActionResult> Index(int? clinicaId)
    {
        if (clinicaId == null)
        {
            var clinicas = await _db.Clinicas.Include(c => c.Plan).OrderBy(c => c.Nombre)
                .Select(c => new { c.Id, c.Nombre, PlanNombre = c.Plan.Nombre })
                .ToListAsync();
            ViewBag.Clinicas = clinicas;
            return View("SeleccionarClinica");
        }

        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == clinicaId);
        if (clinica == null) return NotFound();

        var users = await _db.Users.Where(u => u.ClinicaId == clinicaId).ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = userIds.Any()
            ? await _db.UserRoles.Where(ur => userIds.Contains(ur.UserId)).ToListAsync()
            : new List<IdentityUserRole<string>>();
        var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
        var roles = roleIds.Any()
            ? await _db.Roles.Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name ?? "")
            : new Dictionary<string, string>();

        var list = users.Select(u =>
        {
            var ur = userRoles.FirstOrDefault(ur => ur.UserId == u.Id);
            var rol = ur != null && roles.TryGetValue(ur.RoleId, out var name) ? name : null;
            return new UsuarioClinicaListViewModel
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email ?? "",
                Rol = rol,
                Activo = u.Activo
            };
        }).ToList();

        ViewBag.ClinicaId = clinica.Id;
        ViewBag.ClinicaNombre = clinica.Nombre;
        ViewBag.Plan = clinica.Plan;
        return View(list);
    }

    public async Task<IActionResult> Create(int clinicaId)
    {
        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == clinicaId);
        if (clinica == null) return NotFound();

        var (puede, mensaje) = await ValidarPuedeCrearUsuarioAsync(clinica);
        if (!puede)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(Index), new { clinicaId });
        }

        // Solo creamos Administrador de clínica; Doctor/Recepcion/Finanzas los crea el Admin en su panel.
        return View(new UsuarioClinicaCreateViewModel
        {
            ClinicaId = clinica.Id,
            ClinicaNombre = clinica.Nombre,
            Rol = OdontariRoles.AdminClinica,
            Activo = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioClinicaCreateViewModel vm)
    {
        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == vm.ClinicaId);
        if (clinica == null) return NotFound();

        // Gestor SaaS solo crea AdminClinica; el resto lo crea el admin de la clínica en su panel.
        vm.Rol = OdontariRoles.AdminClinica;
        var (puede, mensaje) = await ValidarPuedeCrearUsuarioAsync(clinica, vm.Rol);
        if (!puede)
        {
            ModelState.AddModelError("", mensaje ?? "No se puede crear el usuario.");
        }

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
                ClinicaId = vm.ClinicaId,
                Activo = vm.Activo
            };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, OdontariRoles.AdminClinica);
            await _audit.RegistrarAsync(clinica.Id, null, "UsuarioClinia_Creado", "Usuario", user.Id, vm.Email);
            TempData["Message"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Index), new { clinicaId = vm.ClinicaId });
        }
        vm.ClinicaNombre = clinica.Nombre;
        vm.Rol = OdontariRoles.AdminClinica;
        return View(vm);
    }

    private async Task<(bool Puede, string? Mensaje)> ValidarPuedeCrearUsuarioAsync(Clinica clinica, string? rolParaNuevo = null)
    {
        var vigente = await _db.Suscripciones
            .AnyAsync(s => s.ClinicaId == clinica.Id && s.Activa && !s.Suspendida && s.Vencimiento >= DateTime.Today);
        if (!vigente)
            return (false, "La clínica no tiene suscripción vigente.");
        if (!clinica.Activa)
            return (false, "La clínica está inactiva.");

        var countUsuarios = await _db.Users.CountAsync(u => u.ClinicaId == clinica.Id);
        if (countUsuarios >= clinica.Plan.MaxUsuarios)
            return (false, $"Has alcanzado el límite del plan ({clinica.Plan.MaxUsuarios} usuarios).");

        if (rolParaNuevo == OdontariRoles.Doctor)
        {
            var doctorRoleId = await _db.Roles.Where(r => r.Name == OdontariRoles.Doctor).Select(r => r.Id).FirstOrDefaultAsync();
            var countDoctores = await (from u in _db.Users
                                       join ur in _db.UserRoles on u.Id equals ur.UserId
                                       where u.ClinicaId == clinica.Id && ur.RoleId == doctorRoleId
                                       select u).CountAsync();
            if (countDoctores >= clinica.Plan.MaxDoctores)
                return (false, $"Has alcanzado el límite de doctores del plan ({clinica.Plan.MaxDoctores}).");
        }

        return (true, null);
    }
}

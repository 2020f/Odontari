using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;
using Odontari.Web.ViewModels;

namespace Odontari.Web.Areas.Clinica.Controllers;

/// <summary>B14) Doctores y Personal — Solo AdminClinica crea/edita usuarios de la clínica.</summary>
[Authorize(Roles = OdontariRoles.AdminClinica)]
[Area("Clinica")]
public class PersonalController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClinicaActualService _clinicaActual;
    private readonly IAuditService _audit;

    public PersonalController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IClinicaActualService clinicaActual,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _clinicaActual = clinicaActual;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var users = await _db.Users.Where(u => u.ClinicaId == cid).ToListAsync();
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
            string? horario = null;
            if (rol == OdontariRoles.Doctor && u.HoraEntrada.HasValue && u.HoraSalida.HasValue)
                horario = $"{u.HoraEntrada.Value.ToString(@"hh\:mm")} - {u.HoraSalida.Value.ToString(@"hh\:mm")}";
            return new UsuarioClinicaListViewModel
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email ?? "",
                Rol = rol,
                Activo = u.Activo,
                HorarioLaboral = horario ?? "—"
            };
        }).OrderBy(u => u.NombreCompleto).ToList();

        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == cid);
        if (clinica == null) return NotFound();

        var (puede, mensaje) = await ValidarPuedeCrearUsuarioAsync(clinica);
        if (!puede)
        {
            TempData["Error"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        ViewBag.RolesPersonal = new[] { OdontariRoles.Recepcion, OdontariRoles.Doctor, OdontariRoles.Finanzas, OdontariRoles.AdminClinica };
        return View(new PersonalCreateViewModel { Activo = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonalCreateViewModel vm)
    {
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var clinica = await _db.Clinicas.Include(c => c.Plan).FirstOrDefaultAsync(c => c.Id == cid);
        if (clinica == null) return NotFound();

        var (puede, mensaje) = await ValidarPuedeCrearUsuarioAsync(clinica, vm.Rol);
        if (!puede)
            ModelState.AddModelError("", mensaje ?? "No se puede crear el usuario.");

        if (!OdontariRoles.RolesClinica.Contains(vm.Rol))
            ModelState.AddModelError(nameof(vm.Rol), "Rol no válido.");

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
                ClinicaId = cid,
                Activo = vm.Activo
            };
            if (vm.Rol == OdontariRoles.Doctor)
            {
                user.HoraEntrada = vm.HoraEntrada;
                user.HoraSalida = vm.HoraSalida;
            }
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                ViewBag.RolesPersonal = new[] { OdontariRoles.Recepcion, OdontariRoles.Doctor, OdontariRoles.Finanzas, OdontariRoles.AdminClinica };
                return View(vm);
            }
            await _userManager.AddToRoleAsync(user, vm.Rol);
            await _audit.RegistrarAsync(cid, User.Identity?.Name, "Personal_Creado", "Usuario", user.Id, vm.Email + " " + vm.Rol);
            TempData["Message"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.RolesPersonal = new[] { OdontariRoles.Recepcion, OdontariRoles.Doctor, OdontariRoles.Finanzas, OdontariRoles.AdminClinica };
        return View(vm);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.ClinicaId == cid);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var rol = roles.FirstOrDefault() ?? "";

        return View(new PersonalEditViewModel
        {
            Id = user.Id,
            NombreCompleto = user.NombreCompleto ?? "",
            Email = user.Email ?? "",
            Rol = rol,
            Activo = user.Activo,
            HoraEntrada = user.HoraEntrada,
            HoraSalida = user.HoraSalida
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, PersonalEditViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.ClinicaId == cid);
        if (user == null) return NotFound();

        if (!OdontariRoles.RolesClinica.Contains(vm.Rol))
            ModelState.AddModelError(nameof(vm.Rol), "Rol no válido.");

        if (ModelState.IsValid)
        {
            user.NombreCompleto = vm.NombreCompleto;
            user.Activo = vm.Activo;
            if (vm.Rol == OdontariRoles.Doctor)
            {
                user.HoraEntrada = vm.HoraEntrada;
                user.HoraSalida = vm.HoraSalida;
            }
            else
            {
                user.HoraEntrada = null;
                user.HoraSalida = null;
            }
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(vm.NuevaPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, vm.NuevaPassword);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, vm.Rol);

            await _audit.RegistrarAsync(cid, User.Identity?.Name, "Personal_Editado", "Usuario", user.Id, vm.Email);
            TempData["Message"] = "Usuario actualizado.";
            return RedirectToAction(nameof(Index));
        }
        vm.Email = user.Email ?? "";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActivo(string id)
    {
        var cid = await _clinicaActual.GetClinicaIdActualAsync();
        if (cid == null) return RedirectToAction("SinClinica", "Home", new { area = "Clinica" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.ClinicaId == cid);
        if (user == null) return NotFound();

        user.Activo = !user.Activo;
        await _userManager.UpdateAsync(user);
        await _audit.RegistrarAsync(cid, User.Identity?.Name, user.Activo ? "Personal_Activado" : "Personal_Desactivado", "Usuario", user.Id, user.Email);
        TempData["Message"] = user.Activo ? "Usuario activado." : "Usuario desactivado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool Puede, string? Mensaje)> ValidarPuedeCrearUsuarioAsync(Models.Clinica clinica, string? rolParaNuevo = null)
    {
        var vigente = await _db.Suscripciones
            .AnyAsync(s => s.ClinicaId == clinica.Id && s.Activa && !s.Suspendida && s.Vencimiento > DateTime.Today);
        if (!vigente) return (false, "La clínica no tiene suscripción vigente.");
        if (!clinica.Activa) return (false, "La clínica está inactiva.");

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

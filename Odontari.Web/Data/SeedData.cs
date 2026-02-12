using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Models;

namespace Odontari.Web.Data;

public static class SeedData
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in OdontariRoles.Todos)
        {
            if (await roleManager.RoleExistsAsync(roleName)) continue;
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "superadmin@odontari.com";
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = "Super Administrador",
            Activo = true
            // ClinicaId = null (usuario SaaS)
        };
        var result = await userManager.CreateAsync(user, "SuperAdmin2025!");
        if (!result.Succeeded)
            throw new InvalidOperationException($"No se pudo crear SuperAdmin: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, OdontariRoles.SuperAdmin);
    }

    public static async Task SeedPlanBasicoAsync(ApplicationDbContext db)
    {
        if (await db.Planes.AnyAsync()) return;
        db.Planes.Add(new Models.Plan
        {
            Nombre = "Básico",
            PrecioMensual = 99m,
            MaxUsuarios = 5,
            MaxDoctores = 2,
            PermiteFacturacion = true,
            PermiteOdontograma = true,
            PermiteWhatsApp = false,
            PermiteARS = false,
            Activo = true
        });
        await db.SaveChangesAsync();
    }

    public static async Task SeedClinicaDemoAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (await db.Clinicas.AnyAsync()) return;
        var plan = await db.Planes.FirstOrDefaultAsync();
        if (plan == null) return;
        var clinica = new Clinica
        {
            Nombre = "Clínica Demo",
            Email = "demo@odontari.com",
            Telefono = "809-555-0000",
            Activa = true,
            PlanId = plan.Id
        };
        db.Clinicas.Add(clinica);
        await db.SaveChangesAsync();
        db.Suscripciones.Add(new Suscripcion
        {
            ClinicaId = clinica.Id,
            Inicio = DateTime.Today,
            Vencimiento = DateTime.Today.AddMonths(1),
            Activa = true,
            Suspendida = false
        });
        await db.SaveChangesAsync();
        foreach (var (email, pass, nombre, rol) in new[] {
            ("recepcion@clinica.com", "Recepcion2025!", "Recepción Demo", OdontariRoles.Recepcion),
            ("doctor@clinica.com", "Doctor2025!", "Doctor Demo", OdontariRoles.Doctor)
        })
        {
            if (await userManager.FindByEmailAsync(email) != null) continue;
            var u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, NombreCompleto = nombre, ClinicaId = clinica.Id, Activo = true };
            await userManager.CreateAsync(u, pass);
            await userManager.AddToRoleAsync(u, rol);
        }
    }

    public static async Task EnsureSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedSuperAdminAsync(userManager);
        await SeedPlanBasicoAsync(db);
        await SeedClinicaDemoAsync(db, userManager);
    }
}

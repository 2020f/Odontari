using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("ConexionSql") ?? throw new InvalidOperationException("Connection string 'ConexionSql' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Fase 1: login directo
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Home/AccesoDenegado";
    options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
    {
        OnRedirectToAccessDenied = ctx =>
        {
            var path = ctx.Request.Path.Value ?? "";
            if (path.StartsWith("/Clinica", StringComparison.OrdinalIgnoreCase))
                ctx.Response.Redirect("/Clinica/Home/Index?accesoDenegado=1");
            else if (path.StartsWith("/Saas", StringComparison.OrdinalIgnoreCase))
                ctx.Response.Redirect("/Saas/Dashboard/Index?accesoDenegado=1");
            else
                ctx.Response.Redirect("/Home/AccesoDenegado?ReturnUrl=" + Uri.EscapeDataString(ctx.Request.Path));
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add<Odontari.Web.Filters.ValidarAccesoClinicaFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Odontari.Web.Services.IClinicaActualService, Odontari.Web.Services.ClinicaActualService>();
builder.Services.AddScoped<Odontari.Web.Services.IPuertaEntradaService, Odontari.Web.Services.PuertaEntradaService>();
builder.Services.AddScoped<Odontari.Web.Services.IAuditService, Odontari.Web.Services.AuditService>();
builder.Services.AddScoped<Odontari.Web.Filters.ValidarAccesoClinicaFilter>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Interceptar GET /Home/AccesoDenegado con ReturnUrl de área y redirigir al dashboard con overlay (antes de auth)
app.Use(async (context, next) =>
{
    var path = (context.Request.Path.Value ?? "").TrimEnd('/');
    if (path.Equals("/Home/AccesoDenegado", StringComparison.OrdinalIgnoreCase))
    {
        var returnUrl = context.Request.Query["ReturnUrl"].FirstOrDefault() ?? "";
        var decoded = string.IsNullOrEmpty(returnUrl) ? "" : Uri.UnescapeDataString(returnUrl);
        if (decoded.StartsWith("/Clinica", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Clinica/Home/Index?accesoDenegado=1");
            return;
        }
        if (decoded.StartsWith("/Saas", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Saas/Dashboard/Index?accesoDenegado=1");
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// Fase 1: Seed roles y SuperAdmin
await Odontari.Web.Data.SeedData.EnsureSeedAsync(app.Services);

app.Run();

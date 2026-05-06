using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Odontari.Web.Data;
using Odontari.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. 952
var connectionString = builder.Configuration.GetConnectionString("ConexionSql") ?? throw new InvalidOperationException("Connection string 'ConexionSql' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.CommandTimeout(120)));
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
            var user = ctx.HttpContext.User;
            bool isClinica = path.StartsWith("/Clinica", StringComparison.OrdinalIgnoreCase);
            bool isSaas = path.StartsWith("/Saas", StringComparison.OrdinalIgnoreCase);
            if (isClinica && (user.IsInRole(OdontariRoles.AdminClinica) || user.IsInRole(OdontariRoles.Recepcion) || user.IsInRole(OdontariRoles.Doctor) || user.IsInRole(OdontariRoles.Finanzas)))
            {
                ctx.Response.Redirect("/Clinica/Home/Index?accesoDenegado=1");
                return Task.CompletedTask;
            }
            if (isSaas && user.IsInRole(OdontariRoles.SuperAdmin))
            {
                ctx.Response.Redirect("/Saas/Dashboard/Index?accesoDenegado=1");
                return Task.CompletedTask;
            }
            ctx.Response.Redirect("/Home/AccesoDenegado?ReturnUrl=" + Uri.EscapeDataString(path));
            return Task.CompletedTask;
        }
    };
});

// Data Protection — persiste las claves en Azure Blob para sobrevivir reinicios del App Service
var blobConnStr = builder.Configuration["AzureBlob:ConnectionString"];
var blobContainer = builder.Configuration["AzureBlob:Container"] ?? "fotos-generales";
if (!string.IsNullOrWhiteSpace(blobConnStr))
{
    try
    {
        var blobContainerClient = new BlobContainerClient(blobConnStr, blobContainer);
        blobContainerClient.CreateIfNotExists();
        var dpBlobClient = blobContainerClient.GetBlobClient("dataprotection-keys.xml");
        builder.Services.AddDataProtection()
            .SetApplicationName("OdontariWeb")
            .PersistKeysToAzureBlobStorage(dpBlobClient);
    }
    catch (Exception ex)
    {
        var startupLogger = LoggerFactory.Create(lb => lb.AddConsole()).CreateLogger("Startup");
        startupLogger.LogWarning(ex, "No se pudo configurar Data Protection en Azure Blob. La app arrancará con claves en memoria (sesiones se perderán al reiniciar).");
    }
}

builder.Services.AddControllersWithViews(o =>
{
    // Autorización: bloqueo de vistas por usuario (Reportes, Caja, etc.) — se ejecuta antes que el action filter
    o.Filters.Add<Odontari.Web.Filters.ValidarVistaPermisoAuthorizationFilter>();
    o.Filters.Add<Odontari.Web.Filters.ValidarAccesoClinicaFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Odontari.Web.Services.IClinicaActualService, Odontari.Web.Services.ClinicaActualService>();
builder.Services.AddScoped<Odontari.Web.Services.IPuertaEntradaService, Odontari.Web.Services.PuertaEntradaService>();
builder.Services.AddScoped<Odontari.Web.Services.IAuditService, Odontari.Web.Services.AuditService>();
builder.Services.AddScoped<Odontari.Web.Services.ReporteFinancieroExportService>();
builder.Services.AddScoped<Odontari.Web.Services.HistogramaExportService>();
builder.Services.AddScoped<Odontari.Web.Services.IFacturaService, Odontari.Web.Services.FacturaService>();
builder.Services.AddScoped<Odontari.Web.Services.FacturaPdfService>();
builder.Services.AddScoped<Odontari.Web.Services.HistorialPagosPdfService>();
builder.Services.AddScoped<Odontari.Web.Services.IUsuarioVistasPermisoService, Odontari.Web.Services.UsuarioVistasPermisoService>();
builder.Services.AddScoped<Odontari.Web.Services.IBloqueoVistaClinicaService, Odontari.Web.Services.BloqueoVistaClinicaService>();
builder.Services.AddScoped<Odontari.Web.Services.IBlobUploadService, Odontari.Web.Services.BlobUploadService>();
builder.Services.AddScoped<Odontari.Web.Filters.ValidarVistaPermisoAuthorizationFilter>();
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

// Fase 1: Seed roles y SuperAdmin (si la BD no está disponible, la app arranca igual)
try
{
    await Odontari.Web.Data.SeedData.EnsureSeedAsync(app.Services);
}
catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException || ex is TimeoutException)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Error durante el seed inicial. Verifique que SQL Server esté en ejecución. La aplicación arrancará pero roles/usuarios iniciales pueden no existir.");
}

app.Run();


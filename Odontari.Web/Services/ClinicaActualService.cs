using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Odontari.Web.Models;

namespace Odontari.Web.Services;

public class ClinicaActualService : IClinicaActualService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClinicaActualService(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public int? GetClinicaIdActual()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        var user = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
        return user?.ClinicaId;
    }

    public async Task<int?> GetClinicaIdActualAsync()
    {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User);
        return user?.ClinicaId;
    }
}

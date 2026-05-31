// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Odontari.Web.Data;
using Odontari.Web.Models;
using Odontari.Web.Services;

namespace Odontari.Web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPuertaEntradaService _puertaEntrada;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IPuertaEntradaService puertaEntrada,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _puertaEntrada = puertaEntrada;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            if (!ModelState.Any(m => m.Value?.Errors.Count > 0) &&
                User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(currentUser);
                    return LocalRedirect(GetDefaultPanelUrl(currentUser, currentRoles));
                }
            }

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user == null)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "No se pudo cargar el usuario. Intente iniciar sesion de nuevo.");
                        return Page();
                    }

                    var roles = await _userManager.GetRolesAsync(user);

                    if (user.ClinicaId != null && !roles.Contains(OdontariRoles.SuperAdmin))
                    {
                        var (puedeEntrar, motivoBloqueo) = await _puertaEntrada.ValidarAccesoPanelClinicaAsync(user.ClinicaId.Value);
                        if (!puedeEntrar)
                        {
                            await _signInManager.SignOutAsync();
                            ErrorMessage = motivoBloqueo ?? "Suscripcion vencida. Contacte al administrador para renovar.";
                            return RedirectToPage();
                        }
                    }

                    await _signInManager.RefreshSignInAsync(user);
                    return LocalRedirect(GetPostLoginUrl(returnUrl, user, roles));
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            return Page();
        }

        private string GetPostLoginUrl(string returnUrl, ApplicationUser user, IList<string> roles)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) ||
                !Url.IsLocalUrl(returnUrl) ||
                IsRootUrl(returnUrl) ||
                returnUrl.StartsWith("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                returnUrl.StartsWith("/Home/AccesoDenegado", StringComparison.OrdinalIgnoreCase))
            {
                return GetDefaultPanelUrl(user, roles);
            }

            if (roles.Contains(OdontariRoles.SuperAdmin))
            {
                if (returnUrl.Equals("/Saas", StringComparison.OrdinalIgnoreCase) ||
                    returnUrl.Equals("/Saas/", StringComparison.OrdinalIgnoreCase) ||
                    returnUrl.StartsWith("/Clinica", StringComparison.OrdinalIgnoreCase))
                {
                    return "/Saas/Dashboard/Index";
                }
            }

            if (user.ClinicaId != null && roles.Any(r => OdontariRoles.RolesClinica.Contains(r)))
            {
                if (returnUrl.Equals("/Clinica", StringComparison.OrdinalIgnoreCase) ||
                    returnUrl.Equals("/Clinica/", StringComparison.OrdinalIgnoreCase) ||
                    returnUrl.StartsWith("/Saas", StringComparison.OrdinalIgnoreCase))
                {
                    return "/Clinica/Home/Index";
                }
            }

            return returnUrl;
        }

        private static string GetDefaultPanelUrl(ApplicationUser user, IList<string> roles)
        {
            if (roles.Contains(OdontariRoles.SuperAdmin))
                return "/Saas/Dashboard/Index";

            if (user.ClinicaId != null && roles.Any(r => OdontariRoles.RolesClinica.Contains(r)))
                return "/Clinica/Home/Index";

            return "/";
        }

        private static bool IsRootUrl(string returnUrl)
        {
            return returnUrl == "~/" ||
                   returnUrl == "/" ||
                   returnUrl.Equals("/Index", StringComparison.OrdinalIgnoreCase);
        }
    }
}

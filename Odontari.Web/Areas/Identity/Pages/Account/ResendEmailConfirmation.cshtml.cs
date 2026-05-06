// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Odontari.Web.Areas.Identity.Pages.Account;

public class ResendEmailConfirmationModel : PageModel
{
    public IActionResult OnGet() => Page();
    public IActionResult OnPost() => Page();
}

using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

public abstract class PortalController : Controller
{
    protected void AddApiErrors(ApiRequestException exception)
    {
        if (exception.ValidationErrors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        foreach (var (field, messages) in exception.ValidationErrors)
        {
            var normalized = field.Split('.').LastOrDefault() ?? field;
            foreach (var message in messages)
                ModelState.AddModelError(normalized, message);
        }
    }

    protected async Task<IActionResult> HandleReadErrorAsync(ApiRequestException exception)
    {
        if (exception.IsUnauthorized)
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { expired = true });
        }

        Response.StatusCode = (int)exception.StatusCode;
        ViewData["ErrorDetail"] = exception.Message;
        return View("~/Views/Shared/Error.cshtml");
    }
}

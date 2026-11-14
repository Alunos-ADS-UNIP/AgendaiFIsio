using System.Security.Claims;
using AgendaiFisio.DTOs.Usuario;
using AgendaiFisio.Web.Models;
using AgendaiFisio.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaiFisio.Web.Controllers;

[Route("conta")]
public sealed class AccountController : PortalController
{
    private readonly IAgendaiFisioApiClient _api;

    public AccountController(IAgendaiFisioApiClient api) => _api = api;

    [AllowAnonymous]
    [HttpGet("entrar")]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        if (expired) ViewData["SessionExpired"] = true;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("entrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var login = await _api.PostAsync<UsuarioLoginDTO, LoginResponseDTO>(
                "/api/auth/login",
                new UsuarioLoginDTO { Email = model.Email.Trim(), Senha = model.Password },
                authenticated: false,
                cancellationToken);

            var user = login.Usuario;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.TipoUsuario)
            };
            if (user.PerfilId is Guid profileId)
                claims.Add(new Claim("perfilId", profileId.ToString()));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = login.ExpiresAtUtc });

            HttpContext.Session.SetString(AgendaiFisioApiClient.AccessTokenSessionKey, login.AccessToken);
            TempData["Success"] = "Login realizado com sucesso.";

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
        catch (ApiRequestException exception)
        {
            AddApiErrors(exception);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet("criar")]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost("criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync(
                "/api/auth/register",
                new
                {
                    Email = model.Email.Trim(),
                    Senha = model.Password,
                    TipoUsuario = model.UserType,
                    AceiteLgpd = model.AcceptTerms,
                    DataNascimento = model.UserType == "Paciente" ? model.BirthDate : null,
                    ConsentimentoResponsavelLegal = model.UserType == "Paciente" && model.LegalGuardianConsent,
                    NomeResponsavelLegal = model.UserType == "Paciente" ? model.LegalGuardianName?.Trim() : null
                },
                authenticated: false,
                cancellationToken);

            TempData["Success"] = "Conta criada. Entre para completar seu perfil.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiRequestException exception)
        {
            AddApiErrors(exception);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost("sair")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try { await _api.PostAsync("/api/auth/logout", new { }, true, cancellationToken); }
        catch (Exception) { }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet("acesso-negado")]
    public IActionResult AccessDenied() => View();
}

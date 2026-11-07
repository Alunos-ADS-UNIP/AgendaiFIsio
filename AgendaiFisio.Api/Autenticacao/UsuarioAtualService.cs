using AgendaiFisio.Constants;

namespace AgendaiFisio.Services.Auth;

// Centraliza a interpretação das claims para que controllers não conheçam o formato do JWT.
public sealed class UsuarioAtualService(IHttpContextAccessor httpContextAccessor)
    : IUsuarioAtualService
{
    public UsuarioAtual? Obter()
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        if (!Guid.TryParse(principal.FindFirst(JwtClaimNames.Subject)?.Value, out var usuarioId))
            return null;

        var tipoUsuario = PerfilDeUsuario.NormalizarAutenticado(
            principal.FindFirst(JwtClaimNames.Role)?.Value);
        if (tipoUsuario is null)
            return null;

        var email = principal.FindFirst(JwtClaimNames.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return null;

        Guid? perfilId = null;
        var perfilIdClaim = principal.FindFirst(JwtClaimNames.PerfilId)?.Value;
        if (!string.IsNullOrWhiteSpace(perfilIdClaim))
        {
            if (!Guid.TryParse(perfilIdClaim, out var perfilIdConvertido))
                return null;

            perfilId = perfilIdConvertido;
        }

        if (tipoUsuario != PerfilDeUsuario.Admin && perfilId is null)
            return null;

        return new UsuarioAtual(usuarioId, perfilId, email, tipoUsuario);
    }
}

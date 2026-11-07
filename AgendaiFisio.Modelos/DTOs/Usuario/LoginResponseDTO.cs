namespace AgendaiFisio.DTOs.Usuario;

// Resposta completa de autenticação, incluindo validade e identidade do usuário.
public sealed class LoginResponseDTO
{
    public string AccessToken { get; init; } = string.Empty;

    // Mantém compatibilidade com os clientes atuais enquanto eles migram para accessToken.
    public string Token => AccessToken;

    public string TokenType { get; init; } = "Bearer";
    public DateTime ExpiresAtUtc { get; init; }
    public UsuarioAutenticadoDTO Usuario { get; init; } = new();
}

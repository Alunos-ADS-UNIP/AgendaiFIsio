namespace AgendaiFisio.DTOs.Usuario;

// Identidade pública da conta autenticada e do perfil de domínio ligado a ela.
public sealed class UsuarioAutenticadoDTO
{
    public Guid UsuarioId { get; init; }
    public Guid? PerfilId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string TipoUsuario { get; init; } = string.Empty;
}

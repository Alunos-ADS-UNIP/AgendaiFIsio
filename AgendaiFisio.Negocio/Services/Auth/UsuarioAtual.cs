namespace AgendaiFisio.Services.Auth;

// Identidade já validada pelo middleware JWT e disponível durante a requisição.
public sealed record UsuarioAtual(
    Guid UsuarioId,
    Guid? PerfilId,
    string Email,
    string TipoUsuario);

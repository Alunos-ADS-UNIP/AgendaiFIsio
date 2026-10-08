namespace AgendaiFisio.Constants;

// Nomes estáveis usados tanto na emissão quanto na leitura dos tokens JWT.
public static class JwtClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string JwtId = "jti";
    public const string PerfilId = "perfil_id";
}

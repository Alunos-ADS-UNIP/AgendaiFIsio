namespace AgendaiFisio.Repositorio.Armazenamento;

public sealed class FotosPerfilOptions
{
    public const string Secao = "Storage";
    public string ProfilePhotosBucket { get; set; } = string.Empty;
    public long ProfilePhotosMaxBytes { get; set; } = 5 * 1024 * 1024;
    public int ProfilePhotosUrlExpirationMinutes { get; set; } = 60;
}

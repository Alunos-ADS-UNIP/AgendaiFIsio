using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using AgendaiFisio.Repositorio.Interfaces;
using Microsoft.Extensions.Options;

namespace AgendaiFisio.Repositorio.Armazenamento;

public sealed class S3ArmazenamentoFotoPerfil : IArmazenamentoFotoPerfil
{
    private readonly IAmazonS3 _s3;
    private readonly FotosPerfilOptions _options;

    public S3ArmazenamentoFotoPerfil(IAmazonS3 s3, IOptions<FotosPerfilOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public long TamanhoMaximoBytes => _options.ProfilePhotosMaxBytes;

    private string Bucket => !string.IsNullOrWhiteSpace(_options.ProfilePhotosBucket)
        ? _options.ProfilePhotosBucket
        : throw new InvalidOperationException(
            "Storage__ProfilePhotosBucket não foi configurada.");

    public async Task<string> SalvarAsync(
        Guid profissionalId,
        Stream conteudo,
        string tipoConteudo,
        CancellationToken cancellationToken = default)
    {
        var extensao = tipoConteudo switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => throw new ArgumentException("Tipo de imagem não permitido.", nameof(tipoConteudo))
        };
        var chave = $"profissionais/{profissionalId:N}/perfil-{Guid.NewGuid():N}.{extensao}";

        try
        {
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = chave,
                InputStream = conteudo,
                ContentType = tipoConteudo,
                AutoCloseStream = false
            }, cancellationToken);
            return chave;
        }
        catch (AmazonClientException ex)
        {
            throw new ArmazenamentoFotoException("Não foi possível salvar a foto no armazenamento.", ex);
        }
    }

    public async Task ExcluirAsync(string chave, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chave)) return;

        try
        {
            await _s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = Bucket,
                Key = chave
            }, cancellationToken);
        }
        catch (AmazonClientException ex)
        {
            throw new ArmazenamentoFotoException("Não foi possível excluir a foto do armazenamento.", ex);
        }
    }

    public string GerarUrlLeitura(string chave)
    {
        try
        {
            return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = Bucket,
                Key = chave,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(_options.ProfilePhotosUrlExpirationMinutes)
            });
        }
        catch (AmazonClientException ex)
        {
            throw new ArmazenamentoFotoException("Não foi possível gerar a URL da foto.", ex);
        }
    }
}

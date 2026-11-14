using System.Net;

namespace AgendaiFisio.Web.Services;

public interface IAgendaiFisioApiClient
{
    Task<T> GetAsync<T>(string path, bool authenticated = true, CancellationToken cancellationToken = default);
    Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, bool authenticated = true, CancellationToken cancellationToken = default);
    Task PostAsync<TRequest>(string path, TRequest body, bool authenticated = true, CancellationToken cancellationToken = default);
    Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default);
    Task PutAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default);
    Task<TResponse> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<TResponse> UploadAsync<TResponse>(string path, IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class ApiRequestException : Exception
{
    public ApiRequestException(HttpStatusCode statusCode, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }

    public HttpStatusCode StatusCode { get; }
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}

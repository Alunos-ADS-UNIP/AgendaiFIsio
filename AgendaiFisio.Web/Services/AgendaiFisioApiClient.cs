using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgendaiFisio.Web.Services;

public sealed class AgendaiFisioApiClient : IAgendaiFisioApiClient
{
    public const string AccessTokenSessionKey = "AgendaiFisio.AccessToken";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AgendaiFisioApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<T> GetAsync<T>(string path, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, authenticated, cancellationToken);

    public Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, bool authenticated = true, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Post, path, JsonContent.Create(body, options: JsonOptions), authenticated, cancellationToken);

    public async Task PostAsync<TRequest>(string path, TRequest body, bool authenticated = true, CancellationToken cancellationToken = default) =>
        await SendWithoutBodyAsync(HttpMethod.Post, path, JsonContent.Create(body, options: JsonOptions), authenticated, cancellationToken);

    public Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Put, path, JsonContent.Create(body, options: JsonOptions), true, cancellationToken);

    public async Task PutAsync<TRequest>(string path, TRequest body, CancellationToken cancellationToken = default) =>
        await SendWithoutBodyAsync(HttpMethod.Put, path, JsonContent.Create(body, options: JsonOptions), true, cancellationToken);

    public Task<TResponse> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(HttpMethod.Patch, path, null, true, cancellationToken);

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default) =>
        await SendWithoutBodyAsync(HttpMethod.Delete, path, null, true, cancellationToken);

    public async Task<TResponse> UploadAsync<TResponse>(string path, IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "arquivo", file.FileName);
        return await SendAsync<TResponse>(HttpMethod.Put, path, content, true, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        HttpContent? content,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, path, content, authenticated);
        using var response = await SendRequestAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);

        if (response.Content.Headers.ContentLength == 0)
            return default!;

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new ApiRequestException(response.StatusCode, "A API devolveu uma resposta vazia.");
    }

    private async Task SendWithoutBodyAsync(
        HttpMethod method,
        string path,
        HttpContent? content,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, path, content, authenticated);
        using var response = await SendRequestAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiRequestException(
                System.Net.HttpStatusCode.ServiceUnavailable,
                "O servidor demorou para responder. Tente novamente em instantes.");
        }
        catch (HttpRequestException)
        {
            throw new ApiRequestException(
                System.Net.HttpStatusCode.ServiceUnavailable,
                "Não foi possível conectar ao servidor do AgendaiFisio. Tente novamente em instantes.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, HttpContent? content, bool authenticated)
    {
        var request = new HttpRequestMessage(method, path.TrimStart('/')) { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (authenticated)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString(AccessTokenSessionKey);
            if (string.IsNullOrWhiteSpace(token))
                throw new ApiRequestException(System.Net.HttpStatusCode.Unauthorized, "Sua sessão expirou. Entre novamente.");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private static async Task<ApiRequestException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
            return new ApiRequestException(response.StatusCode, $"A API respondeu com o status {(int)response.StatusCode}.");

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            var message = ReadMessage(root) ?? $"Não foi possível concluir a solicitação ({(int)response.StatusCode}).";
            var errors = ReadValidationErrors(root);
            return new ApiRequestException(response.StatusCode, message, errors);
        }
        catch (JsonException)
        {
            return new ApiRequestException(response.StatusCode, text);
        }
    }

    private static string? ReadMessage(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in root.EnumerateObject())
        {
            if ((property.Name.Equals("erro", StringComparison.OrdinalIgnoreCase) ||
                 property.Name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
                 property.Name.Equals("title", StringComparison.OrdinalIgnoreCase)) &&
                property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }
        return null;
    }

    private static IReadOnlyDictionary<string, string[]> ReadValidationErrors(JsonElement root)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (root.ValueKind != JsonValueKind.Object) return result;

        JsonElement source = root;
        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            source = errors;

        foreach (var property in source.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var values = property.Value.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()!)
                    .ToArray();
                if (values.Length > 0) result[property.Name] = values;
            }
        }
        return result;
    }
}

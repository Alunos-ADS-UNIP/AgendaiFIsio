using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AgendaiFisio.OpenApi;

internal sealed class BloqueioAgendaOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var rota = context.Description.RelativePath?.TrimEnd('/');
        if (rota == "api/agenda/bloqueios/horarios" &&
            context.Description.HttpMethod == HttpMethods.Post)
        {
            operation.Summary = "Bloqueia uma hora cheia da agenda";
            operation.Description =
                "Bloqueia exatamente 60 minutos. O início deve ter minuto 00 e offset explícito.";
            DefinirExemplo(operation, """
                {
                  "dataHora": "2026-11-03T09:00:00-03:00",
                  "motivo": "Compromisso pessoal"
                }
                """);
        }
        else if (rota == "api/agenda/bloqueios/dias" &&
                 context.Description.HttpMethod == HttpMethods.Post)
        {
            operation.Summary = "Bloqueia um dia completo da agenda";
            operation.Description =
                "A data representa o dia civil completo no fuso America/Sao_Paulo.";
            DefinirExemplo(operation, """
                {
                  "data": "2026-11-04",
                  "motivo": "Congresso"
                }
                """);
        }
        else if (rota == "api/agenda/bloqueios" &&
                 context.Description.HttpMethod == HttpMethods.Get)
        {
            operation.Summary = "Lista bloqueios em um período";
            operation.Description =
                "inicio e fim são obrigatórios, usam offset explícito e podem abranger no máximo 93 dias.";
        }

        return Task.CompletedTask;
    }

    private static void DefinirExemplo(OpenApiOperation operation, string json)
    {
        if (operation.RequestBody?.Content is null)
            return;

        foreach (var mediaType in operation.RequestBody.Content.Values)
            mediaType.Example = JsonNode.Parse(json);
    }
}

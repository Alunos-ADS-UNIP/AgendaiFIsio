using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AgendaiFisio.OpenApi;

// Marca como protegidas somente as operações que realmente exigem autenticação.
internal sealed class AuthOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var permiteAnonimo = metadata.OfType<IAllowAnonymous>().Any();
        var exigeAutorizacao = metadata.OfType<IAuthorizeData>().Any();

        if (permiteAnonimo || !exigeAutorizacao)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                BearerSecuritySchemeTransformer.SchemeName,
                context.Document)] = []
        });

        return Task.CompletedTask;
    }
}

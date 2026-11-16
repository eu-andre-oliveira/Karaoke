using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Karaoke.Api.OpenApi;

/// <summary>
/// Marca como obrigatória toda propriedade não anulável. Sem isso o Swashbuckle só
/// considera [Required], e o cliente TypeScript gerado fica cheio de campos opcionais.
/// </summary>
public sealed class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null || schema.Properties.Count == 0) return;

        foreach (var (nome, propriedade) in schema.Properties)
        {
            if (!propriedade.Nullable)
                schema.Required.Add(nome);
        }
    }
}

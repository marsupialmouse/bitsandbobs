using NJsonSchema.Generation;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace BitsAndBobs.Infrastructure;

/// <summary>
/// Mark all types in generated Swagger schema as read-only.
/// </summary>
sealed class ReadOnlyOpenApiDocumentProcessor : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        foreach (var schema in context.Document.Components.Schemas.Values)
        {
            if (schema is null)
                continue;

            foreach (var property in schema.Properties)
                property.Value.IsReadOnly = true;

            foreach (var definition in schema.Definitions.Values)
                foreach (var property in definition.Properties.Values)
                    property.IsReadOnly = true;
        }
    }
}

using System.Text.Json;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace BitsAndBobs.Tests;

public static class McpExtensions
{
    public static T GetStructuredContent<T>(this CallToolResult result)
        where T : class
    {
        result.StructuredContent.ShouldNotBeNull();
        var content = result.StructuredContent.Deserialize<T>(JsonSerializerOptions.Web);
        content.ShouldNotBeNull();
        return content;
    }
}

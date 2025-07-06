using Microsoft.AspNetCore.Antiforgery;

namespace BitsAndBobs.Infrastructure.AntiForgery;

/// <summary>
/// Middleware that validates the anti forgery token on all requests with unsafe HTTP methods (POST, PUT, PATCH, DELETE).
/// </summary>
public class AntiForgeryMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IAntiforgery antiForgery)
    {
        var endpoint = context.GetEndpoint();

        if (!IsValidHttpMethodForForm(context.Request.Method)
            || endpoint?.Metadata.GetMetadata<IAntiforgeryMetadata>() is { RequiresValidation: false })
            return next(context);

        return InvokeAwaited(context, antiForgery);
    }

    private async Task InvokeAwaited(HttpContext context, IAntiforgery antiForgery)
    {
        try
        {
            await antiForgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid CSRF token");
            return;
        }

        await next(context);
    }

    private static bool IsValidHttpMethodForForm(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}

using Microsoft.AspNetCore.Antiforgery;

namespace BitsAndBobs.Infrastructure.AntiForgery;

/// <summary>
/// Middleware that sets the anti forgery token cookie and header on non-cached requests.
/// </summary>
public class AntiForgeryTokenMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IAntiforgery antiForgery)
    {
        context.Response.OnStarting(
            () =>
            {
                if (!context.Response.WillBeCached())
                    SetTokens(context, antiForgery);

                return Task.CompletedTask;
            });

        return next(context);
    }

    private static void SetTokens(HttpContext context, IAntiforgery antiForgery)
    {
        var tokens = antiForgery.GetAndStoreTokens(context);

        // Axios (React HTTP client) looks for this cookie and, if it exists, uses its value for the X-XSRF-TOKEN header for all HTTP requests.
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Strict });
    }
}

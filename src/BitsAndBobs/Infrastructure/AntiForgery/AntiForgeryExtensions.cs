namespace BitsAndBobs.Infrastructure.AntiForgery;

public static class AntiForgeryExtensions
{
    /// <summary>
    /// Add anti-forgery middleware to the request pipeline. The middleware sets the anti-forgery token cookie and
    /// header on non-cached API requests, and
    /// </summary>
    public static IApplicationBuilder UseAntiForgery(this IApplicationBuilder app) =>
        app.UseMiddleware<AntiForgeryTokenMiddleware>();

    /// <summary>
    /// Validates the anti-forgery token on all requests with unsafe HTTP methods (POST, PUT, PATCH, DELETE).
    /// </summary>
    /// <remarks>
    /// Call <see cref="DoNotRequireAntiForgery{TBuilder}"/> on a route or route group to exclude it from validation.
    /// </remarks>
    public static IApplicationBuilder UseRequireAntiForgery(this IApplicationBuilder app) =>
        app.UseMiddleware<AntiForgeryMiddleware>();
}

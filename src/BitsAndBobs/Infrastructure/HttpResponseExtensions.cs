using Microsoft.Net.Http.Headers;

namespace BitsAndBobs.Infrastructure;

public static class HttpResponseExtensions
{
    /// <summary>
    /// Checks whether the response has headers set to enable caching.
    /// </summary>
    /// <param name="response">The response to check</param>
    /// <returns><c>true</c> if the response will be cached, otherwise <c>false</c>.</returns>
    public static bool WillBeCached(this HttpResponse response)
    {
        if (!response.Headers.TryGetValue(HeaderNames.CacheControl, out var header))
            return false;

        if (!CacheControlHeaderValue.TryParse(header.ToString(), out var headerValue))
            return false;

        return !headerValue.NoCache && !headerValue.NoStore;
    }
}

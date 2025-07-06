using System.Net;
using BitsAndBobs.Infrastructure.AntiForgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Infrastructure;

[TestFixture]
public class AntiForgeryTests : TestBase
{
    private const string VerificationTokenCookieName = "XSRF-VERIFICATION-TOKEN";
    private const string RequestTokenCookieName = "XSRF-TOKEN";
    private const string RequestTokenHeaderName = "X-XSRF-TOKEN";

    private static readonly HttpMethod[] UnsafeHttpMethods =
    {
        HttpMethod.Delete, HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch
    };

    [SetUp]
    public void EnableAntiForgery()
    {
        AppFactory.ValidateAntiForgeryTokens = true;

        // Do not store cookies between requests (important for testing that validation fails when a cookie does not exist)
        HttpClientOptions = new WebApplicationFactoryClientOptions { HandleCookies = false };

        AppFactory.ConfiguringApp += app =>
        {
            app.Map("/api/test_xsrf", () => Results.Ok());
            app.MapPost("/api/test_xsrf_excluded", () => Results.Ok()).DoNotRequireAntiForgery();
            app.MapGet("/not_api/test_xsrf", () => Results.Ok());
            app.MapGet(
                "/api/test_xsrf_cached",
                (HttpContext context) =>
                {
                    context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=20";
                    return Results.Ok();
                }
            );
            app.MapGet(
                "/api/test_xsrf_no_store",
                (HttpContext context) =>
                {
                    context.Response.Headers[HeaderNames.CacheControl] = "no-store";
                    return Results.Ok();
                }
            );
        };
    }

    [Test]
    public async Task WhenNonCachedApiEndpointCalled_SetsAntiForgeryCookies()
    {
        var response = await HttpClient.GetAsync("/api/test_xsrf");

        var (verification, request) = GetCookies(response);

        verification.ShouldNotBeNull();
        verification.HttpOnly.ShouldBeTrue();
        verification.Value.ToString().ShouldNotBeNullOrEmpty();
        request.ShouldNotBeNull();
        request.HttpOnly.ShouldBeFalse();
        request.Value.ToString().ShouldNotBeNullOrEmpty();
        request.Value.ShouldNotBe(verification.Value);
    }

    [Test]
    public async Task WhenNoStoreApiEndpointCalled_SetsAntiForgeryCookies()
    {
        var response = await HttpClient.GetAsync("/api/test_xsrf_no_store");

        var (verification, request) = GetCookies(response);

        verification.ShouldNotBeNull();
        verification.HttpOnly.ShouldBeTrue();
        verification.Value.ToString().ShouldNotBeNullOrEmpty();
        request.ShouldNotBeNull();
        request.HttpOnly.ShouldBeFalse();
        request.Value.ToString().ShouldNotBeNullOrEmpty();
        request.Value.ShouldNotBe(verification.Value);
    }

    [Test]
    public async Task WhenNonApiEndpointCalled_DoesNotSetAntiForgeryTokens()
    {
        var response = await HttpClient.GetAsync("/not_api/test_xsrf");

        var (verification, request) = GetCookies(response);

        verification.ShouldBeNull();
        request.ShouldBeNull();
    }

    [Test]
    public async Task WhenApiEndpointHasCachedResponse_DoesNotSetAntiForgeryTokens()
    {
        var response = await HttpClient.GetAsync("/api/test_xsrf_cached");

        var (verification, request) = GetCookies(response);

        verification.ShouldBeNull();
        request.ShouldBeNull();
    }

    [Test]
    [TestCaseSource(nameof(UnsafeHttpMethods))]
    public async Task WhenUnsafeApiRequestHasXsrfCookieAndHeader_RequestIsOk(HttpMethod method)
    {
        var (verificationToken, requestToken) = await RequestTokens();
        var request = new HttpRequestMessage(method, "/api/test_xsrf");
        request.Headers.Add(RequestTokenHeaderName, requestToken);
        request.Headers.Add(HeaderNames.Cookie, $"{VerificationTokenCookieName}={verificationToken}");

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    [TestCaseSource(nameof(UnsafeHttpMethods))]
    public async Task WhenUnsafeApiRequestHasNoXsrfCookie_RequestIsBad(HttpMethod method)
    {
        var (_, requestToken) = await RequestTokens();
        var request = new HttpRequestMessage(method, "/api/test_xsrf");
        request.Headers.Add(RequestTokenHeaderName, requestToken);

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    [TestCaseSource(nameof(UnsafeHttpMethods))]
    public async Task WhenUnsafeApiRequestHasNoXsrfHeader_RequestIsBad(HttpMethod method)
    {
        var (verificationToken, _) = await RequestTokens();
        var request = new HttpRequestMessage(method, "/api/test_xsrf");
        request.Headers.Add(HeaderNames.Cookie, $"{VerificationTokenCookieName}={verificationToken}");

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    [TestCaseSource(nameof(UnsafeHttpMethods))]
    public async Task WhenUnsafeApiRequestHasInvalidXsrfHeader_RequestIsBad(HttpMethod method)
    {
        var (verificationToken, requestToken) = await RequestTokens();
        var request = new HttpRequestMessage(method, "/api/test_xsrf");
        request.Headers.Add(RequestTokenHeaderName, requestToken + "invalid!");
        request.Headers.Add(HeaderNames.Cookie, $"{VerificationTokenCookieName}={verificationToken}");

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    [TestCaseSource(nameof(UnsafeHttpMethods))]
    public async Task WhenUnsafeApiRequestHasInvalidXsrfCookie_RequestIsBad(HttpMethod method)
    {
        var (verificationToken, requestToken) = await RequestTokens();
        var request = new HttpRequestMessage(method, "/api/test_xsrf");
        request.Headers.Add(RequestTokenHeaderName, requestToken);
        request.Headers.Add(HeaderNames.Cookie, $"{VerificationTokenCookieName}={verificationToken}invalid!");

        var response = await HttpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task WhenNoTokenUsedForNonXsrfEndpoint_RequestIsOk()
    {
        var response = await HttpClient.PostAsync("/api/test_xsrf_excluded", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task<(string? verification, string? request)> RequestTokens()
    {
        var response = await HttpClient.GetAsync("/api/test_xsrf");
        return GetTokens(response);
    }

    private static (SetCookieHeaderValue? verification, SetCookieHeaderValue? request) GetCookies(
        HttpResponseMessage response
    )
    {
        if (!response.Headers.TryGetValues(HeaderNames.SetCookie, out var headers)
            || !SetCookieHeaderValue.TryParseList(
                headers.Select(x => x.ToString()).ToList(),
                out var cookieHeaderValues
            ))
            return (null, null);

        return (cookieHeaderValues.FirstOrDefault(x => x.Name == VerificationTokenCookieName),
                   cookieHeaderValues.FirstOrDefault(x => x.Name == RequestTokenCookieName));
    }

    private static (string? verification, string? request) GetTokens(HttpResponseMessage response)
    {
        var (verification, request) = GetCookies(response);
        return (verification?.Value.ToString(), request?.Value.ToString());
    }
}

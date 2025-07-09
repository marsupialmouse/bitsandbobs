using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;

namespace BitsAndBobs.Tests;

public abstract class TestBase
{
    /// <summary>
    /// Provides support for using the MS TestHost to host the application, allowing derived classes to use <see cref="HttpClient"/> to invoke
    /// application endpoints.
    /// </summary>
    protected sealed class ApplicationFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Allows tests to hook into service configuration process. This is typically used to register mock implementations in tests.
        /// </summary>
        public event Action<IServiceCollection>? ConfiguringServices;

        /// <summary>
        /// Allows tests to hook into the application configuration process. This is typically used to register middleware or configure the app.
        /// </summary>
        public event Action<WebApplication>? ConfiguringApp;

        /// <summary>
        /// Allows tests to require anti forgery tokens for requests using non-safe HTTP methods. To simplify testing, the default is <c>false</c>.
        /// </summary>
        public bool ValidateAntiForgeryTokens { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "dummy");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "dummy");
            Environment.SetEnvironmentVariable("AWS_REGION", "ap-southeast-2");

            builder
                .UseEnvironment("Test")
                .ConfigureLogging(x => x.ClearProviders().AddDebug().AddConsole().SetMinimumLevel(LogLevel.Trace));

            // override config values from test (runs after Startup.ConfigureServices
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();

                services.Replace(ServiceDescriptor.Singleton(Testing.Dynamo.Client));
                services.Replace(ServiceDescriptor.Singleton(Testing.Dynamo.Context));
                services.Replace(ServiceDescriptor.Singleton(Substitute.For<IEmailSender<User>>()));
                services.Replace(ServiceDescriptor.Singleton(Substitute.For<IEmailStore>()));

                if (!ValidateAntiForgeryTokens)
                {
                    services.Replace( ServiceDescriptor.Singleton(_ =>
                    {
                        var mockAntiForgery = Substitute.For<IAntiforgery>();
                        mockAntiForgery.ReturnsForAll(_ => new AntiforgeryTokenSet(
                                                          "request",
                                                          "verification",
                                                          "XSRF",
                                                          "XSRF"
                                                      )
                        );
                        return mockAntiForgery;
                    }));
                }

                ConfiguringServices?.Invoke(services);

                Program.ConfigureApp = app => ConfiguringApp?.Invoke(app);
            });
        }
    }

    private Lazy<HttpClient> _httpClient = null!;

    /// <summary>
    /// Settings for <see cref="HttpClient"/>. This property allows tests to control how the HttpClient works. For example, you may wish to
    /// disable cookies.
    /// </summary>
    /// <remarks>
    /// This property is only available in tests
    /// </remarks>
    protected WebApplicationFactoryClientOptions HttpClientOptions { get; set; } = new() { HandleCookies = true };

    protected ApplicationFactory AppFactory { get; private set; } = null!;
    protected HttpClient HttpClient => _httpClient.Value;

    protected static IAmazonDynamoDB DynamoClient => Testing.Dynamo.Client;
    protected static IDynamoDBContext DynamoContext => Testing.Dynamo.Context;

    [SetUp]
    public virtual void SetupApplicationFactory()
    {
        AppFactory = new ApplicationFactory();
        _httpClient = new Lazy<HttpClient>(() => AppFactory.CreateClient(HttpClientOptions));
    }

    [TearDown]
    public void TearDownApplicationFactory()
    {
        AppFactory.Dispose();
        AppFactory = null!;
        _httpClient = null!;
    }
}

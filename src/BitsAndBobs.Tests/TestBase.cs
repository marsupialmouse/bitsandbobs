using System.Security.Claims;
using System.Text.Encodings.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        /// Environment variables to set for the application. This is useful for overriding settings.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A mocked S3 client
        /// </summary>
        public IAmazonS3 S3Client { get;  } = Substitute.For<IAmazonS3>();

        /// <summary>
        /// Allows tests to require anti forgery tokens for requests using non-safe HTTP methods. To simplify testing, the default is <c>false</c>.
        /// </summary>
        public bool ValidateAntiForgeryTokens { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "dummy");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "dummy");
            Environment.SetEnvironmentVariable("AWS_REGION", "ap-southeast-2");
            Environment.SetEnvironmentVariable("AWS__Resources__AppBucketName", "grandpa-joe");

            foreach (var (key, value) in EnvironmentVariables)
                Environment.SetEnvironmentVariable(key, value);

            builder
                .UseEnvironment("Test")
                .ConfigureLogging(x => x.ClearProviders().AddDebug().AddConsole().SetMinimumLevel(LogLevel.Trace));

            // override config values from test (runs after Startup.ConfigureServices
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();

                services
                    .AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                services.Replace(ServiceDescriptor.Singleton(S3Client));
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

    protected IAmazonS3 S3Client => AppFactory.S3Client;

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

    protected UserId SetAuthenticatedClaimsPrincipal()
    {
        var user = new User { Username = "auth@enticated.com" };
        SetClaimsPrincipal(user);
        return user.Id;
    }

    protected void SetClaimsPrincipal(User user)
    {
       AppFactory.ConfiguringServices += services =>
        {
            services.AddSingleton(new TestAuthHandler.AuthenticatedUser(user));
        };
    }

    /// <summary>
    /// Sets an environment variable for the application. This is useful for overriding settings in tests.
    /// </summary>
    protected void SetEnv(string key, string value) =>
        AppFactory.EnvironmentVariables[key] = value;

    public class TestAuthHandler(
        IServiceProvider services,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var user = services.GetService<AuthenticatedUser>();

            if (user is null)
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity(user.Claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public class AuthenticatedUser(User user)
        {
            public IEnumerable<Claim> Claims =>
            [
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.NameIdentifier, user.Id.Value),
            ];
        }
    }
}

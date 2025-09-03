using System.Security.Claims;
using System.Text.Encodings.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using BitsAndBobs.Features.Identity;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using NSubstitute;
using NSubstitute.Extensions;

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
        /// Allows tests to hook into the service configuration process. This is typically used to register mock implementations in tests.
        /// </summary>
        public event Action<IServiceCollection>? ConfiguringServices;

        /// <summary>
        /// Allows tests to hook into the application configuration process. This is typically used to register middleware or configure the app.
        /// </summary>
        public event Action<WebApplication>? ConfiguringApp;

        /// <summary>
        /// Allows tests to alter the configuration of the message bus (e.g. add consumers)
        /// </summary>
        public Action<IBusRegistrationConfigurator>? ConfigureMessaging;

        /// <summary>
        /// Environment variables to set for the application. This is useful for overriding settings.
        /// </summary>
        public Dictionary<string, string?> Settings { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            { "AWS:Resources:AppBucketName", "grandpa-joe" },
            { "Jwt:Key", "grandpa-george-needs-128-bits-of-encryption-power" },
        };

        /// <summary>
        /// A mocked S3 client
        /// </summary>
        public IAmazonS3 S3Client { get;  } = Substitute.For<IAmazonS3>();

        /// <summary>
        /// Allows tests to require anti-forgery tokens for requests using non-safe HTTP methods. To simplify testing, the default is <c>false</c>.
        /// </summary>
        public bool ValidateAntiForgeryTokens { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "dummy");
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "dummy");
            Environment.SetEnvironmentVariable("AWS_REGION", "ap-southeast-2");

            foreach (var (key, value) in Settings)
                Environment.SetEnvironmentVariable(key, value);

            builder
                .UseEnvironment("Test")
                .ConfigureLogging(x => x.ClearProviders().AddDebug().AddConsole().SetMinimumLevel(LogLevel.Trace));

            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(Settings));

            // override config values from test (runs after Startup.ConfigureServices
            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();

                services.AddMassTransitTestHarness(ConfigureMessaging);

                services
                    .AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                services.Replace(ServiceDescriptor.Singleton(S3Client));
                services.Replace(ServiceDescriptor.Singleton(Testing.Dynamo.Client));
                services.Replace(ServiceDescriptor.Singleton(Testing.Dynamo.Context));

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
    private Lazy<IMcpClient> _mcpClient = null!;
    private Lazy<ITestHarness> _messagingHarness = null!;

    private static readonly SseClientTransportOptions McpTransportOptions = new()
    {
        Endpoint = new Uri("http://localhost/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp
    };

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
    protected IMcpClient McpClient => _mcpClient.Value;
    protected ITestHarness Messaging => _messagingHarness.Value;

    protected static IAmazonDynamoDB DynamoClient => Testing.Dynamo.Client;
    protected static IDynamoDBContext DynamoContext => Testing.Dynamo.Context;

    protected IAmazonS3 S3Client => AppFactory.S3Client;

    [SetUp]
    public virtual void SetupApplicationFactory()
    {
        AppFactory = new ApplicationFactory();
        _httpClient = new Lazy<HttpClient>(() => AppFactory.CreateClient(HttpClientOptions));
        _messagingHarness = new Lazy<ITestHarness>(() => AppFactory.Services.GetRequiredService<ITestHarness>());
        _mcpClient = new Lazy<IMcpClient>(() =>
            {
                var user = AppFactory.Services.GetService<TestAuthHandler.AuthenticatedUser>();

                if (user is not null)
                {
                    var jwtTokenFactory = AppFactory.Services.GetRequiredService<JwtTokenFactory>();
                    var identity = new ClaimsIdentity(user.Claims, JwtBearerDefaults.AuthenticationScheme);
                    var token = jwtTokenFactory.CreateFor(new ClaimsPrincipal(identity));

                    HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }

                return McpClientFactory
                       .CreateAsync(new SseClientTransport(McpTransportOptions, HttpClient, ownsHttpClient: false))
                       .ConfigureAwait(false)
                       .GetAwaiter()
                       .GetResult();
            }
        );
    }

    [TearDown]
    public async Task TearDownApplicationFactory()
    {
        if (_mcpClient.IsValueCreated)
        {
            await _mcpClient.Value.DisposeAsync();
            _mcpClient = null!;
        }

        await AppFactory.DisposeAsync();
        AppFactory = null!;
        _httpClient = null!;
    }

    /// <summary>
    /// Alter the configuration of the message bus (e.g. add consumers)
    /// </summary>
    protected void ConfigureMessaging(Action<IBusRegistrationConfigurator> configure) =>
        AppFactory.ConfigureMessaging = configure;

    /// <summary>
    /// Creates a valid user and saves it to the DB.
    /// </summary>
    protected async Task<User> CreateUser(
        string? emailAddress = null,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null
    )
    {
        emailAddress ??= $"test-{Guid.NewGuid()}@example.com";

        var user = new User
        {
            EmailAddress = emailAddress,
            NormalizedEmailAddress = emailAddress.ToUpperInvariant(),
            Username = emailAddress,
            NormalizedUsername = emailAddress.ToUpperInvariant(),
            EmailAddressConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName ?? emailAddress.Split('@')[0],
        };
        user.UpdateConcurrency();

        await new UserStore(Testing.DynamoClient, Testing.Dynamo.Context).CreateAsync(user, CancellationToken.None);

        return user;
    }

    /// <summary>
    /// Creates a valid user, saves it to the DB and sets the current user in the claims principal.
    /// </summary>
    protected async Task<User> CreateAuthenticatedUser(
        string? emailAddress = null,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null
    )
    {
        var user = await CreateUser(emailAddress, displayName, firstName, lastName);
        SetClaimsPrincipal(user);
        return user;
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
    protected void UpdateSetting(string key, string value) =>
        AppFactory.Settings[key] = value;

    protected async Task<T?> GetPublishedMessage<T>() where T : class =>
        (await Messaging.Published.SelectAsync<T>().FirstOrDefault())?.Context.Message;

    protected Task<bool> WaitForMessageToBeConsumed<TConsumer, TMessage>() where TConsumer : class, IConsumer where TMessage : class
    {
        var consumer = Messaging.GetConsumerHarness<TConsumer>();
        return consumer.Consumed.Any<TMessage>();
    }

    protected Task<bool> WaitForMessageToBeConsumed<TConsumer, TMessage>(FilterDelegate<IReceivedMessage<TMessage>> filter) where TConsumer : class, IConsumer where TMessage : class
    {
        var consumer = Messaging.GetConsumerHarness<TConsumer>();
        return consumer.Consumed.Any(filter);
    }

    protected async Task WaitForMessagesToBeConsumed<TConsumer, TMessage>(int numberOfMessages) where TConsumer : class, IConsumer where TMessage : class
    {
        var consumer = Messaging.GetConsumerHarness<TConsumer>();
        var count = 0;

        await foreach (var _ in consumer.Consumed.SelectAsync<TMessage>())
        {
            count++;
            if (count == numberOfMessages)
                break;
        }
    }

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

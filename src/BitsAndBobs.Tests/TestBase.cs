using System.Data;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Test")
                .ConfigureLogging(x => x.AddDebug().AddConsole().SetMinimumLevel(LogLevel.Debug));

            // override config values from test (runs after Startup.ConfigureServices
            builder.ConfigureTestServices(services => ConfiguringServices?.Invoke(services));
        }
    }

    private Lazy<HttpClient> _httpClient = null!;

    protected ApplicationFactory AppFactory { get; private set; } = null!;
    protected HttpClient Client => _httpClient.Value;

    protected static IAmazonDynamoDB DynamoClient => Testing.Dynamo.Client;
    protected static IDynamoDBContext DynamoContext => Testing.Dynamo.Context;

    [SetUp]
    public virtual void SetupApplicationFactory()
    {
        AppFactory = new ApplicationFactory();
        _httpClient = new Lazy<HttpClient>(() => AppFactory.CreateClient());
    }

    [TearDown]
    public void TearDownApplicationFactory()
    {
        AppFactory.Dispose();
        AppFactory = null!;
        _httpClient = null!;
    }
}

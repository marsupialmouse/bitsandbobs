using System.Runtime.CompilerServices;
using System.Text;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using BitsAndBobs.Features;
using BitsAndBobs.Features.Auctions;
using BitsAndBobs.Features.Auctions.Endpoints;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Features.UserContext;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.AntiForgery;
using BitsAndBobs.Infrastructure.DynamoDb;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NJsonSchema.Generation;
using StronglyTypedIds;

[assembly: InternalsVisibleTo("BitsAndBobs.Tests")]
[assembly: StronglyTypedIdDefaults("dynamodb-itemid")]

namespace BitsAndBobs;

public class Program
{
    /// <summary>
    /// An entry point for tests to hook into the application configuration process.
    /// </summary>
    public static Action<WebApplication>? ConfigureApp { get; set; }

    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var isTest = builder.Environment.EnvironmentName == "Test";
        var isNSwag = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NSWAG"));

        builder.AddServiceDefaults();

        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddMeter(BitsAndBobsDiagnostics.Meter.Name))
            .WithTracing(tracing => tracing.AddSource(BitsAndBobsDiagnostics.ActivitySource.Name));

        if (!isTest)
        {
            builder
                .Services.AddDataProtection()
                .SetApplicationName("BitsAndBobs")
                .PersistKeysToAWSSystemsManager($"/BitsAndBobs/{builder.Environment.EnvironmentName}/DataProtection");

            builder.Services.AddHostedService<CompleteAuctionsHostedService>();
        }

        builder
            .Services.AddOptions<AwsResourceOptions>()
            .Bind(builder.Configuration.GetSection(AwsResourceOptions.SectionName))
            .ValidateDataAnnotations();

        var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName);

        builder
            .Services.AddOptions<JwtOptions>()
            .Bind(jwtConfig)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Add AWS services to the container.
        var tablePrefix = $"{builder.Environment.EnvironmentName}-";
        builder.Services.AddAWSService<IAmazonS3>();
        builder.Services.AddAWSService<IAmazonDynamoDB>();
        builder.Services.AddKeyedSingleton<Table>(
            BitsAndBobsTable.Name,
            (services, _) => BitsAndBobsTable.CreateTableDefinition(
                services.GetRequiredService<IAmazonDynamoDB>(),
                tablePrefix
            )
        );
        builder.Services.AddSingleton<IDynamoDBContext>(services =>
            {
                var context = new DynamoDBContextBuilder()
                              .WithDynamoDBClient(services.GetRequiredService<IAmazonDynamoDB>)
                              .ConfigureContext(config =>
                                  {
                                      config.TableNamePrefix = tablePrefix;
                                  }
                              )
                              .Build();
                context.RegisterTableDefinition(services.GetRequiredKeyedService<Table>(BitsAndBobsTable.Name));
                return context;
            }
        );

        if (!isTest)
        {
            builder.Services.AddMassTransit(x =>
                {
                    var env = builder.Environment.EnvironmentName;

                    x.AddInMemoryInboxOutbox();
                    x.DisableUsageTelemetry();
                    x.AddConsumersFromNamespaceContaining<Program>();

                    x.AddConfigureEndpointsCallback((_, _, config) =>
                        {
                            config.UseMessageRetry(r => r.Exponential(
                                                       int.MaxValue,
                                                       TimeSpan.FromSeconds(1),
                                                       TimeSpan.FromHours(2),
                                                       TimeSpan.FromSeconds(1)
                                                   )
                            );
                        }
                    );

                    x.AddConfigureEndpointsCallback((_, cfg) =>
                        {
                            // Enable long polling on all receive endpoints (maximum 20 seconds)
                            if (cfg is IAmazonSqsReceiveEndpointConfigurator sqs)
                                sqs.WaitTimeSeconds = 20;
                        }
                    );

                    if (!isNSwag)
                    {
                        x.UsingAmazonSqs((context, config) =>
                            {
                                config.Host(
                                    Environment.GetEnvironmentVariable("AWS_REGION")
                                    ?? RegionEndpoint.APSoutheast2.SystemName,
                                    c => c.Scope(env, true)
                                );
                                config.ConfigureEndpoints(
                                    context,
                                    new KebabCaseEndpointNameFormatter($"{env.ToLowerInvariant()}-", false)
                                );
                            }
                        );
                    }
                }
            );
        }

        builder.Services.AddAuthorization();
        builder
            .Services.AddAuthentication(o =>
            {
                o.DefaultScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(
                IdentityConstants.ApplicationScheme,
                o =>
                {
                    o.Cookie.Name = "auth";
                    o.Cookie.SameSite = SameSiteMode.Strict;
                    o.ExpireTimeSpan = TimeSpan.FromDays(7);
                    o.SlidingExpiration = true;
                }
            )
            .AddJwtBearer(o =>
            {
                var jwtOptions = jwtConfig.Get<JwtOptions>()!;

                o.RequireHttpsMetadata = false;
                o.SaveToken = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidIssuer = jwtOptions.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                };
            });

        // Identity
        builder.Services.AddScoped<UserStore>();
        builder.Services.AddScoped<IUserStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserEmailStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserPasswordStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserSecurityStampStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserLockoutStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddIdentityCore<User>().AddDefaultTokenProviders().AddApiEndpoints();
        builder.Services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
            }
        );
        builder.Services.AddTransient<IEmailSender<User>, EmailStore>();
        builder.Services.AddTransient<EmailStore>();
        builder.Services.AddTransient<AuctionService>();
        builder.Services.AddTransient<IDistributedLockClient, BitsAndBobsTable.LockClient>();
        builder.Services.AddTransient<RecklessPublishEndpoint>();

        builder.Services.AddMvc();
        builder.Services.AddOpenApi();
        builder.Services.AddOpenApiDocument(o =>
            {
                o.DocumentProcessors.Add(new ReadOnlyOpenApiDocumentProcessor());
                o.DefaultResponseReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
            }
        );

        builder.Services.AddResponseCaching();
        builder.Services.AddAntiforgery(o =>
            {
                o.HeaderName = "X-XSRF-TOKEN";
                o.Cookie.Name = "XSRF-VERIFICATION-TOKEN";
            }
        );
        builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 1024 * 1024; // 1 MB
            }
        );

        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        builder.Services.AddHealthChecks().AddCheck<DynamoDbHealthCheck>("dynamodb");

        builder
            .Services.AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithResourcesFromAssembly()
            .WithToolsFromAssembly();


        var app = builder.Build();

        app.UseResponseCaching();
        app.MapDefaultEndpoints();

        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api"),
            a => a.UseAntiForgery().UseRequireAntiForgery()
        );

        var endpoints = app.MapGroup("/api");
        endpoints.MapUserContextEndpoints();
        endpoints.MapEmailEndpoints();
        endpoints.MapIdentityEndpoints();
        endpoints.MapAuctionEndpoints();

        app.MapMcp("/mcp");

       // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
        }

        app.UseHttpsRedirection();

        ConfigureApp?.Invoke(app);

        return app;
    }

    public static void Main(string[] args)
    {
        var app = CreateApp(args);
        app.Run();
    }
}

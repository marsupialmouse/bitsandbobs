using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BitsAndBobs.Features;
using BitsAndBobs.Features.Email;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Features.UserContext;
using BitsAndBobs.Infrastructure;
using BitsAndBobs.Infrastructure.AntiForgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using NJsonSchema.Generation;

[assembly: InternalsVisibleTo("BitsAndBobs.Tests")]

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

        builder.AddServiceDefaults();

        if (builder.Environment.EnvironmentName != "Test")
        {
            builder
                .Services.AddDataProtection()
                .SetApplicationName("BitsAndBobs")
                .PersistKeysToAWSSystemsManager($"/BitsAndBobs/{builder.Environment.EnvironmentName}/DataProtection");
        }

        builder
            .Services.AddOptions<AwsResourceOptions>()
            .Bind(builder.Configuration.GetSection(AwsResourceOptions.SectionName))
            .ValidateDataAnnotations();

        // Add services to the container.
        var tablePrefix = $"{builder.Environment.EnvironmentName}-";
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

        // Identity
        builder.Services.AddScoped<UserStore>();
        builder.Services.AddScoped<IUserStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserEmailStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserPasswordStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserSecurityStampStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddScoped<IUserLockoutStore<User>>(services => services.GetRequiredService<UserStore>());
        builder.Services.AddAuthorization();
        builder
            .Services.AddAuthentication()
            .AddCookie(
                IdentityConstants.ApplicationScheme,
                o =>
                {
                    o.Cookie.Name = "auth";
                    o.Cookie.SameSite = SameSiteMode.Strict;
                    o.ExpireTimeSpan = TimeSpan.FromDays(7);
                    o.SlidingExpiration = true;
                }
            );
        builder.Services.AddIdentityCore<User>().AddDefaultTokenProviders().AddApiEndpoints();
        builder.Services.Configure<IdentityOptions>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
            }
        );
        builder.Services.AddTransient<IEmailSender<User>, EmailStore>();
        builder.Services.AddTransient<IEmailStore, EmailStore>();

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

        builder.Services.AddHealthChecks().AddCheck<DynamoDbHealthCheck>("dynamodb");

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

       // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
        }

        app.UseHttpsRedirection();

        app.MapGet(
               "/api/tables",
               async (IAmazonDynamoDB db) =>
               {
                   var tables = await db.ListTablesAsync();

                   return tables.TableNames;
               }
           )
           .WithName("GetTables");

        ConfigureApp?.Invoke(app);

        return app;
    }

    public static void Main(string[] args)
    {
        var app = CreateApp(args);
        app.Run();
    }
}

using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

[assembly: InternalsVisibleTo("BitsAndBobs.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDataProtection()
       .SetApplicationName("BitsAndBobs")
       .PersistKeysToAWSSystemsManager($"/BitsAndBobs/{builder.Environment.EnvironmentName}/DataProtection");

// Add services to the container.
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddSingleton<IDynamoDBContext>(services => new DynamoDBContextBuilder()
                                                            .WithDynamoDBClient(
                                                                services.GetRequiredService<IAmazonDynamoDB>
                                                            )
                                                            .ConfigureContext(config =>
                                                                {
                                                                    config.TableNamePrefix =
                                                                        $"{builder.Environment.EnvironmentName}-";
                                                                }
                                                            )
                                                            .Build()
);

// Identity
builder.Services.AddScoped<UserStore>(services => new UserStore(
                                          services.GetRequiredService<IAmazonDynamoDB>(),
                                          services.GetRequiredService<IDynamoDBContext>(),
                                          $"{builder.Environment.EnvironmentName}-BitsAndBobs"
                                      )
);
builder.Services.AddScoped<IUserStore<User>>(services => services.GetRequiredService<UserStore>());
builder.Services.AddScoped<IUserEmailStore<User>>(services => services.GetRequiredService<UserStore>());
builder.Services.AddScoped<IUserPasswordStore<User>>(services => services.GetRequiredService<UserStore>());
builder.Services.AddScoped<IUserSecurityStampStore<User>>(services => services.GetRequiredService<UserStore>());
builder.Services.AddScoped<IUserLockoutStore<User>>(services => services.GetRequiredService<UserStore>());
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(o => o.Cookie.Name = "auth");
builder.Services.AddIdentityCore<User>().AddDefaultTokenProviders().AddApiEndpoints();
builder.Services.Configure<IdentityOptions>(options => { options.User.RequireUniqueEmail = true; });

builder.Services.AddMvc();
builder.Services.AddOpenApi();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

app.MapDefaultEndpoints();
var identityGroup = app.MapGroup("/api/identity");
identityGroup.MapIdentityApi<User>();
identityGroup.MapPost(
    "/logout",
    async (SignInManager<User> signInManager) =>
    {
        await signInManager.SignOutAsync().ConfigureAwait(false);
    }
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();

app.MapGet("/tables", async (IAmazonDynamoDB db) =>
   {
       var tables = await db.ListTablesAsync();

       return tables.TableNames;
   })
    .WithName("GetTables");

app.Run();

// For testing
public partial class Program { }

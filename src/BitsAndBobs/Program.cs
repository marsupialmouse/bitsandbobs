using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using Microsoft.AspNetCore.Identity;

[assembly: InternalsVisibleTo("BitsAndBobs.Tests")]

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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


builder.Services.AddIdentityCore<User>().AddDefaultTokenProviders();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

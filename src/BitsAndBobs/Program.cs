using Amazon.DynamoDBv2;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddAWSService<IAmazonDynamoDB>();

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


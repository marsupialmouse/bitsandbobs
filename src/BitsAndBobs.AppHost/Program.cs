using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<BitsAndBobs_Api>("api").WithExternalHttpEndpoints();

builder
    .AddViteApp("ui", "../BitsAndBobs.UI", packageManager: "yarn")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

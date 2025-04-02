using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<BitsAndBobs>("api").WithExternalHttpEndpoints();

builder
    .AddViteApp("ui", "../BitsAndBobs/clientapp", packageManager: "yarn")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

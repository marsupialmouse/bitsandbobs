using Projects;

var builder = DistributedApplication.CreateBuilder(args);


builder.AddProject<BitsAndBobs_Api>("api");

builder.Build().Run();
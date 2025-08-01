using Amazon;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig().WithProfile("dev").WithRegion(RegionEndpoint.APSoutheast2);
var awsResources = builder.AddAWSCloudFormationTemplate("BitsAndBobs-Database", "../Infrastructure/cfn/database.yaml")
                          .WithReference(awsConfig)
                          .WithParameter("Environment", "Development")
                          .WithTag("Project", "BitsAndBobs");

var api = builder.AddProject<BitsAndBobs>("api").WithReference(awsResources).WithExternalHttpEndpoints();

builder
    .AddViteApp("ui", "../BitsAndBobs/clientapp", packageManager: "yarn")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

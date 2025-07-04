using Amazon;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig().WithProfile("dev").WithRegion(RegionEndpoint.APSoutheast2);
var awsResources = builder.AddAWSCloudFormationTemplate("BitsAndBobs", "../Infrastructure/aws-resources.yaml")
                          .WithReference(awsConfig)
                          .WithParameter("Environment", "Development")
                          .WithTag("Project", "BitsAndBobs");

var api = builder.AddProject<BitsAndBobs>("api").WithReference(awsResources).WithExternalHttpEndpoints();

builder
    .AddViteApp("ui", "../BitsAndBobs/clientapp", packageManager: "yarn")
    .WaitFor(api);

builder.Build().Run();

using Amazon;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig().WithProfile("dev").WithRegion(RegionEndpoint.APSoutheast2);

var databaseStack = builder
                    .AddAWSCloudFormationTemplate("Dev-BitsAndBobs-Database", "../Infrastructure/cfn/database.yaml")
                    .WithReference(awsConfig)
                    .WithParameter("Environment", "Development");

var s3Stack = builder
              .AddAWSCloudFormationTemplate("Dev-BitsAndBobs-Storage", "../Infrastructure/cfn/storage.yaml")
              .WithReference(awsConfig)
              .WithParameter("Environment", "Development");

var api = builder
          .AddProject<BitsAndBobs>("api")
          .WithReference(databaseStack)
          .WithReference(s3Stack)
          .WithExternalHttpEndpoints();

builder
    .AddViteApp("ui", "../BitsAndBobs/clientapp", packageManager: "yarn")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

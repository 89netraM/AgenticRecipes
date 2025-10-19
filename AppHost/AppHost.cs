using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Web>("webfrontend").WithExternalHttpEndpoints().WithHttpHealthCheck("/health");

builder.Build().Run();

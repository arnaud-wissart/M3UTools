using Aspire.Hosting;
using M3UPlayer.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddM3UPlayerServiceDefaults();

var api = builder.AddProject("m3uplayer-api", "../M3UPlayer.Api/M3UPlayer.Api.csproj");

builder.Build().Run();

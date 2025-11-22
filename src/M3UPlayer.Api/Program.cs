using M3UPlayer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddM3UPlayerServiceDefaults();

var app = builder.Build();

app.MapM3UPlayerDefaultEndpoints();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Json(new { status = "OK", service = "M3UPlayer.Api" }));

app.Run();

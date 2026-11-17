using System.Text.Json.Serialization;
using ScanVault.Api.Data;
using ScanVault.Api.Errors;
using ScanVault.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ----- Register services (dependency injection) -----
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Swagger / OpenAPI documentation at /swagger
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<ScanVaultDbContext>();

builder.Services.AddScanVault(builder.Configuration, builder.Environment);

var app = builder.Build();

// ----- HTTP request pipeline (middleware runs in this order) -----
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();  // serves the React app's index.html
app.UseStaticFiles();   // serves the React build from wwwroot

app.MapControllers();
app.MapHealthChecks("/health");
app.MapFallbackToFile("index.html"); // lets React handle its own page routes

await app.InitialiseDatabaseAsync();
app.Run();

// Makes Program visible to the integration tests (WebApplicationFactory).
public partial class Program { }

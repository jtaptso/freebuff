using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CorsuiteAdmin.Api.Data;
using CorsuiteAdmin.Api.Services;
using CorsuiteAdmin.Api.Services.SAP;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=corsuiteadmin.db"));

// Register module services
builder.Services.AddScoped<IModuleService, ModuleService>();

// Register SAP integration services
builder.Services.AddSingleton<ISapConnectionService, SapDiApiConnectionService>();
builder.Services.AddScoped<ISqlConnectionValidator, SqlConnectionValidator>();
builder.Services.AddScoped<ISapSqlQueryService, SapSqlQueryService>();
builder.Services.AddSingleton<ICorsuiteFileScannerService, CorsuiteFileScannerService>();

// Register SAP DI API health check
builder.Services.AddSingleton<SapDiApiHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<SapDiApiHealthCheck>("sap_di_api", tags: new[] { "sap", "di_api" });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/sap", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("sap")
});

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Run SAP DI API startup check
var sapStartupCheck = app.Services.GetRequiredService<ILogger<SapDiApiStartupCheck>>();
try
{
    var checker = new SapDiApiStartupCheck(sapStartupCheck);
    checker.Check();
}
catch (Exception ex)
{
    sapStartupCheck.LogWarning(ex, "SAP DI API startup check failed");
}

app.Run();

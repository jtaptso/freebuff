using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CorsuiteAdmin.Api.Data;
using CorsuiteAdmin.Api.Services;
using CorsuiteAdmin.Api.Services.SAP;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CorsuiteAdmin";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CorsuiteAdminAPI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Configure Swagger with JWT Bearer authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CorsuiteAdmin API",
        Version = "v1",
        Description = "API for managing SAP Business One modules and connections"
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=corsuiteadmin.db"));

// Register module services
builder.Services.AddScoped<IModuleService, ModuleService>();

// Register SAP integration services - choose based on configuration
// Use DI Server (remote) or local COM (DI API) based on configuration
var useDiServer = builder.Configuration.GetValue<bool>("SAP:UseDiServer", false);
if (useDiServer)
{
    builder.Services.AddSingleton<ISapConnectionService, SapDiServerConnectionService>();
    builder.Services.AddSingleton<SapDiServerConnectionService>();
}
else
{
    builder.Services.AddSingleton<ISapConnectionService, SapDiApiConnectionService>();
}
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
app.UseAuthentication();
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

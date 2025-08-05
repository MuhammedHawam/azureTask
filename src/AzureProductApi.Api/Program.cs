using System.Reflection;
using AzureProductApi.Api.Middleware;
using AzureProductApi.Application.Common.Mappings;
using AzureProductApi.Application.Outlets.Commands.CreateOutlet;
using AzureProductApi.Domain.Interfaces;
using AzureProductApi.Infrastructure.Data;
using AzureProductApi.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with Azure AD authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Azure Outlet Management API",
        Version = "v1",
        Description = "A comprehensive API for managing retail outlets with Azure AD authentication",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@company.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add Azure AD authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your Azure AD JWT token"
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

// Configure Azure AD authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateOutletCommand).Assembly);
});

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(CreateOutletCommandValidator).Assembly);

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register repositories
builder.Services.AddScoped<IOutletRepository, OutletRepository>();

// Configure CORS for frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? 
                new[] { "http://localhost:3000", "https://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>();

// Configure API versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure Outlet Management API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

// Security headers
app.UseHsts();
app.UseHttpsRedirection();

// CORS
app.UseCors("FrontendPolicy");

// Request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");

// Global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception occurred");

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "An internal server error occurred",
                traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
            }));
        }
    });
});

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating the database");
        throw;
    }
}

Log.Information("Azure Outlet Management API starting up...");

try
{
    await app.RunAsync();
    Log.Information("Azure Outlet Management API shut down gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Azure Outlet Management API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
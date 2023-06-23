using System.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Md2HtmlAPI.Service;
using Md2HtmlAPI.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

// Pour développement pour visualiser dans OTLP
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Logger OpenTelemetry
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddOpenTelemetry(options =>
    {
        options.AddConsoleExporter();
    });
});

// Logger for this very class
var logger = loggerFactory.CreateLogger<Program>();

var corsOrigin = Environment.GetEnvironmentVariable("CORS_ORIGIN");

// https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
// Allow CORS first part: Cross Origin Resource Sharing
var corsPolicy = "_myAllowSpecificOrigins";

if (!String.IsNullOrEmpty(corsOrigin))
{
    builder.Services.ConfigureCors(corsPolicy, corsOrigin, logger);
}
// Add services to the container.
builder.Services.AddSingleton(loggerFactory);
// Inject the service
builder.Services.AddSingleton<IMd2HtmlService, Md2HtmlService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

const string API_TITLE = "Markdown to HTML API";
const string API_VERSION = "0.0.1";
const string API_DESCRIPTION = "API to convert Markdown documentation to HTML one";
const string API_MAINTAINER_NAME = "Guillaume ALAMOME";
const string API_MAINTAINER_EMAIL = "guillaume.alamome@gmail.com";

Activity.DefaultIdFormat = ActivityIdFormat.W3C;

var otlpUri = Environment.GetEnvironmentVariable("OTLP_URI");

logger.LogInformation($"OpenTelemetry end point: {otlpUri}");

// Configure important OpenTelemetry settings, the console exporter, and instrumentation library
if (!String.IsNullOrEmpty(otlpUri))
{
    builder.Services.ConfigureOpenTelemetryTracing(API_TITLE,
            API_VERSION,
            new Uri(otlpUri),
            "OTLP",
            "OTLP",
            "Exponential");
}

// Azure Active Directory authentication
// Configuration comes from appsettings.json
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocumentation(API_TITLE, API_VERSION, API_DESCRIPTION, API_MAINTAINER_NAME, API_MAINTAINER_EMAIL);
var app = builder.Build();

app.UseSwaggerDocumentation(API_TITLE, API_VERSION);

app.UseHttpsRedirection();

app.UseRouting();

if (!String.IsNullOrEmpty(corsOrigin))
{
    // Allow CORS second part
    app.UseCors(corsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();

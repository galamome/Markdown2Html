using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

namespace Md2HtmlAPI.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configure important OpenTelemetry settings, the console exporter, and instrumentation library
    /// </summary>
    /// <param name="services"></param>
    /// <param name="title"></param>
    /// <param name="version"></param>
    /// <param name="otlpUri"></param>
    /// <param name="metricsExporter">Switch between Prometheus/OTLP/Console</param>
    /// <param name="logExporter">Switch between Console/OTLP</param>
    /// <param name="histogramAggregation">Switch between Explicit/Exponential</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureOpenTelemetryTracing(this IServiceCollection services,
        string title,
        string version,
        Uri otlpUri,
        string metricsExporter,
        string logExporter,
        string histogramAggregation
        )
    {
        // Build a resource configuration action to set service information.
        Action<ResourceBuilder> configureResource = r => r.AddService(
            serviceName: title,
            serviceVersion: version,
            serviceInstanceId: Environment.MachineName);

        // Create a service to expose ActivitySource, and Metric Instruments
        // for manual instrumentation
        services.AddSingleton<Instrumentation>();

        // Configure OpenTelemetry tracing & metrics with auto-start using the
        // AddOpenTelemetry extension from OpenTelemetry.Extensions.Hosting.
        services.AddOpenTelemetry()
            .ConfigureResource(configureResource)
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                // Export vers la console (mode production)
                //.AddConsoleExporter()
                // DEBUG D'EXPORT VERS OTLP
                .AddOtlpExporter((opts) =>
                {
                    //opts.Protocol = OtlpExportProtocol.Grpc;
                    opts.Endpoint = otlpUri;
                })
                // FIN D'EXPORT VERS OTLP
                .AddSource(title)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: title, serviceVersion: version))
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();
            })
            .WithMetrics(builder =>
            {
                // Metrics

                // Ensure the MeterProvider subscribes to any custom Meters.
                builder
                    .AddMeter(Instrumentation.MeterName)
                    .SetExemplarFilter(new TraceBasedExemplarFilter())
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation();

                switch (histogramAggregation)
                {
                    case "exponential":
                        builder.AddView(instrument =>
                        {
                            return instrument.GetType().GetGenericTypeDefinition() == typeof(Histogram<>)
                                ? new Base2ExponentialBucketHistogramConfiguration()
                                : null;
                        });
                        break;
                    default:
                        // Explicit bounds histogram is the default.
                        // No additional configuration necessary.
                        break;
                }

                switch (metricsExporter)
                {

                    case "prometheus":
                        builder.AddPrometheusExporter();
                        break;
                    case "otlp":
                        builder.AddOtlpExporter(otlpOptions =>
                        {
                            // Use IConfiguration directly for Otlp exporter endpoint option.
                            otlpOptions.Endpoint = otlpUri;
                        });
                        break;
                    default:
                        builder.AddConsoleExporter();
                        break;
                }
            });


        return services;
    }

    public static IServiceCollection ConfigureCors(this IServiceCollection services,
        string corsPolicyName,
        string corsOrigin,
        ILogger logger)
    {
        logger.LogInformation($"CORS_ORIGIN will be allowed: {corsOrigin}");

        if (corsOrigin.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {

            logger.LogInformation($"Cors allow any origin !");
            services.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicyName,
                    policy =>
                    {
                        policy.AllowAnyOrigin();
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                    });
            });
        }
        else
        {
            var corsOrigins = corsOrigin.Split(';');
            logger.LogInformation($"List of allowed origins contains {corsOrigins?.Length} items");
            services.AddCors(options =>
            {
                options.AddPolicy(name: corsPolicyName,
                    policy =>
                    {
                        policy.WithOrigins(corsOrigins);
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                        policy.AllowCredentials();
                    });
            });
        }
        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services,
        string title,
        string version,
        string description,
        string maintainerName,
        string maintainerEmail)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Version = version,
                Title = title,
                Description = description,
                //TermsOfService = new Uri("https://example.com/terms"),
                Contact = new OpenApiContact
                {
                    Name = maintainerName,
                    Email = maintainerEmail
                }
                /*,
                License = new OpenApiLicense
                {
                    Name = "Example License",
                    Url = new Uri("https://example.com/license")
                }
                */
            });
            // using System.Reflection;
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFilename);

            options.IncludeXmlComments(xmlFilePath);

            // Custom operation Id for endpoints, different for each controller to remain
            // OAS3 compatible
            options.CustomOperationIds(
                    description => description.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                        ? null
                        : $"{actionDescriptor.RouteValues["controller"]}_{actionDescriptor.ActionName}");

            // Secure the Swagger UI
            /*
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    new string[]{}
                }
            });
            */
        });

        return services;
    }
}
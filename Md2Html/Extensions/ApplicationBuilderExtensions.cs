using Swashbuckle.AspNetCore.SwaggerUI;

namespace Md2HtmlAPI.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, string title, string version)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Generate the OpenAPI interface definition in JSON
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version} Json description");
            // Generate the OpenAPI interface definition in YAML (used to generate the client library)
            options.SwaggerEndpoint($"/swagger/{version}/swagger.yaml", $"{title} {version} Yaml description");
            // Display operation Id for each method
            options.DisplayOperationId();
            options.DocExpansion(DocExpansion.List);
        });

        return app;
    }
}

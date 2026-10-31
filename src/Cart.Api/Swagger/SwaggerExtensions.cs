using Microsoft.OpenApi;

namespace Cart.Api.Swagger;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwagger(this IServiceCollection services) =>
        services.AddEndpointsApiExplorer().AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste an access token from POST /api/dev/token."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
            });
        });
}

using ElectronicLibrary.PL.Middleware;

namespace ElectronicLibrary.PL.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder
        UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<
            GlobalExceptionHandlingMiddleware>();
    }
}
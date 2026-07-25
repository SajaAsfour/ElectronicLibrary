using System.Net;
using System.Text.Json;

namespace ElectronicLibrary.PL.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next,ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "Unauthorized request.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.Unauthorized,
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid operation.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.BadRequest,
                exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(
                exception,
                "Requested resource was not found.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.NotFound,
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context,
        HttpStatusCode statusCode,
        string message)
    {
        context.Response.StatusCode = (int)statusCode;

        context.Response.ContentType = "application/json";

        var response = new
        {
            statusCode = (int)statusCode,
            message
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
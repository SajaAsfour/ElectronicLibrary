using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.PL.Resources;
using Microsoft.Extensions.Localization;
using System.Net;
using System.Text.Json;

namespace ElectronicLibrary.PL.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IStringLocalizer<SharedResources> localizer)
    {
        _next = next;
        _logger = logger;
        _localizer = localizer;
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
                GetLocalizedMessage(exception.Message));
        }
        catch (EmailDeliveryException exception)
        {
            _logger.LogError(
                exception,
                "Email delivery failed.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.ServiceUnavailable,
                GetLocalizedMessage(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid operation.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.BadRequest,
                GetLocalizedMessage(exception.Message));
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(
                exception,
                "Requested resource was not found.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.NotFound,
                GetLocalizedMessage(exception.Message));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred.");

            await WriteErrorResponseAsync(
                context,
                HttpStatusCode.InternalServerError,
                GetLocalizedMessage("UnexpectedError"));
        }
    }

    private string GetLocalizedMessage(string resourceKey)
    {
        var localizedValue = _localizer[resourceKey];

        return localizedValue.ResourceNotFound
            ? resourceKey
            : localizedValue.Value;
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
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

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}

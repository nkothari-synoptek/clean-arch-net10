using System.Net;
using System.Text.Json;
using FluentValidation;

namespace InspectionService.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            ValidationException validationException => HandleValidationException(validationException),
            DomainException domainException => HandleDomainException(domainException),
            _ => HandleUnhandledException(exception)
        };

        _logger.LogError(
            exception,
            "Exception occurred: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, jsonOptions));
    }

    private (HttpStatusCode, object) HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var errorResponse = new
        {
            type = "ValidationError",
            title = "One or more validation errors occurred",
            status = 400,
            errors
        };

        return (HttpStatusCode.BadRequest, errorResponse);
    }

    private (HttpStatusCode, object) HandleDomainException(DomainException exception)
    {
        var errorResponse = new
        {
            type = "DomainError",
            title = "A domain rule was violated",
            status = 400,
            error = exception.Message
        };

        return (HttpStatusCode.BadRequest, errorResponse);
    }

    private (HttpStatusCode, object) HandleUnhandledException(Exception exception)
    {
        var errorResponse = new
        {
            type = "ServerError",
            title = "An unexpected error occurred",
            status = 500,
            error = "An internal server error occurred. Please try again later."
        };

        return (HttpStatusCode.InternalServerError, errorResponse);
    }
}

/// <summary>
/// Base exception for domain-specific errors
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

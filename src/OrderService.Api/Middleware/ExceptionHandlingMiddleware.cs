using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.Exceptions;

namespace OrderService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, type) = Map(exception);

        if (status >= (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Handled domain/application exception");
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Type = type,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (_environment.IsDevelopment() && status >= 500)
        {
            problem.Extensions["exception"] = exception.ToString();
        }

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static (int Status, string Title, string Type) Map(Exception exception) =>
        exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "https://orderservice.local/errors/validation"),
            EmptyOrderItemsException => (StatusCodes.Status400BadRequest, "Invalid order", "https://orderservice.local/errors/empty-order"),
            OrderNotFoundException or ProductNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "https://orderservice.local/errors/not-found"),
            InsufficientStockException => (StatusCodes.Status409Conflict, "Insufficient stock", "https://orderservice.local/errors/insufficient-stock"),
            InvalidOrderStateException => (StatusCodes.Status409Conflict, "Invalid order state", "https://orderservice.local/errors/invalid-state"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrency conflict", "https://orderservice.local/errors/concurrency"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "https://orderservice.local/errors/unauthorized"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument", "https://orderservice.local/errors/argument"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "https://orderservice.local/errors/internal")
        };
}

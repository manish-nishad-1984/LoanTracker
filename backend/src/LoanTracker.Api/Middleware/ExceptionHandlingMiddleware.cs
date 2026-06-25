using System.Net;
using System.Text.Json;
using LoanTracker.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LoanTracker.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException nfe => (StatusCodes.Status404NotFound, "Not Found", nfe.Message),
            BusinessException be => (StatusCodes.Status400BadRequest, "Business Rule Violation", be.Message),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Invalid Argument", ae.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                  "An unexpected error occurred. Please try again later.")
        };

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

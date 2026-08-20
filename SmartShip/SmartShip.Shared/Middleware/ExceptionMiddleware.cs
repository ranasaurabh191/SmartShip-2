using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartShip.Shared.Middleware
{
    /// <summary>
    /// Global exception handling middleware for ASP.NET Core HTTP pipelines in the SmartShip microservice architecture.
    /// Intercepts unhandled exceptions thrown during request processing, logs them appropriately based on exception severity,
    /// and formats consistent JSON error responses with standardized HTTP status codes.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
                // Categorize expected domain/validation exceptions as warnings vs unexpected system errors as errors
                if (ex is UnauthorizedAccessException ||
                    ex is KeyNotFoundException ||
                    ex is ArgumentException ||
                    ex is InvalidOperationException ||
                    ex is TimeoutException)
                {
                    _logger.LogWarning("Handled application exception for {Method} {Path}: {Message}",
                        context.Request.Method,
                        context.Request.Path,
                        ex.Message);
                }
                else
                {
                    _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                KeyNotFoundException => 404,
                UnauthorizedAccessException => 401,
                ArgumentException => 400,
                InvalidOperationException => 409,
                NotImplementedException => 501,
                TimeoutException => 408,
                _ => 500
            };

            return context.Response.WriteAsJsonAsync(new
            {
                statusCode = context.Response.StatusCode,
                message = ex switch
                {
                    KeyNotFoundException => ex.Message,
                    UnauthorizedAccessException => "Unauthorized.",
                    ArgumentException => ex.Message,
                    InvalidOperationException => ex.Message,
                    TimeoutException => "Request timed out.",
                    _ => "An unexpected error occurred."
                },
                timestamp = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt")
            });
        }
    }
}

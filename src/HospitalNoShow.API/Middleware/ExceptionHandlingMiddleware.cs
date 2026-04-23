using System.Net;
using System.Text.Json;

namespace HospitalNoShow.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, "Geçersiz İşlem"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Yetkisiz Erişim"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Kayıt Bulunamadı"),
            _ => (HttpStatusCode.InternalServerError, "Sunucu Hatası")
        };

        var response = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = exception.Message,
            traceId = context.TraceIdentifier
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

using FCG.Catalogo.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace FCG.Catalogo.Presentation.Middlewares;

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
        catch (NotFoundException ex)
        {
            _logger.LogWarning("[{Path}] Recurso não encontrado: {Message}", context.Request.Path, ex.Message);
            await WriteResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning("[{Path}] Conflito de dados: {Message}", context.Request.Path, ex.Message);
            await WriteResponse(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning("[{Path}] Acesso não autorizado: {Message}", context.Request.Path, ex.Message);
            await WriteResponse(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Path}] Erro inesperado", context.Request.Path);
            await WriteResponse(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno.");
        }
    }

    private static async Task WriteResponse(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var body = JsonSerializer.Serialize(new
        {
            status = (int)status,
            error = message,
            path = context.Request.Path.Value,
            timestamp = DateTime.UtcNow
        });

        await context.Response.WriteAsync(body);
    }
}
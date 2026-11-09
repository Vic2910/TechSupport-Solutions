using Microsoft.EntityFrameworkCore;
using TechSupport.API.Services;
namespace TechSupport.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ReglaException ex) { await Error(context, ex.Status, ex.Message); }
        catch (DbUpdateConcurrencyException) { await Error(context, 409, "El incidente cambió en otra solicitud. Actualice la página."); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado. RequestId: {RequestId}", context.TraceIdentifier);
            if (context.Response.HasStarted) throw;
            await Error(context, 500, "Ocurrió un error al procesar la solicitud.");
        }
    }
    static async Task Error(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { success = false, message,
            requestId = context.TraceIdentifier, timestamp = DateTime.UtcNow });
    }
}

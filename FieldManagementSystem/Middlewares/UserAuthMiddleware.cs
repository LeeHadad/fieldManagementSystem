using FieldManagementSystem.Utilities;

namespace FieldManagementSystem.Middlewares;

public class UserAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var emailValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing X-User-Email header. Access denied." });
            return;
        }

        var email = emailValues.FirstOrDefault();

        try
        {
            InputGuards.ValidateEmailOrThrow(email ?? string.Empty);
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            return;
        }

        await next(context);
    }
}
using Yarp.ReverseProxy.Transforms;

namespace ApiGateway.Middlewares;

public class CorrelationIdTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        // Propaga si ya viene, genera uno nuevo si no
        if (!context.HttpContext.Request.Headers
                .TryGetValue("X-Correlation-ID", out var existing))
        {
            existing = Guid.NewGuid().ToString("N");
        }

        context.ProxyRequest.Headers.TryAddWithoutValidation(
            "X-Correlation-ID", existing.ToString());

        // Expone el usuario autenticado al servicio destino (cuando actives JWT)
        var user = context.HttpContext.User.Identity?.Name;
        if (user is not null)
            context.ProxyRequest.Headers.TryAddWithoutValidation(
                "X-Authenticated-User", user);

        // Limpia headers que no deben pasar al interior
        context.ProxyRequest.Headers.Remove("Cookie");

        return ValueTask.CompletedTask;
    }
}
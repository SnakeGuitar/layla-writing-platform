using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.Policies;

// ── PASIVA: observa requests reales para degradar destinos con errores ────────
public class MinReplicasPassivePolicy : IPassiveHealthCheckPolicy
{
    public string Name => "MinReplicas";

    public void RequestProxied(
        HttpContext context,
        ClusterState cluster,
        DestinationState destination)
    {
        var statusCode = context.Response.StatusCode;

        destination.Health.Passive = statusCode < 500
            ? DestinationHealth.Healthy
            : DestinationHealth.Unhealthy;
    }
}
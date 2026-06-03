using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.Model;

namespace ApiGateway.Policies;

// ── ACTIVA: evalúa el resultado del probe periódico a /health ────────────────
public class MinReplicasActivePolicy : IActiveHealthCheckPolicy
{
    public string Name => "MinReplicas";

    public void ProbingCompleted(
        ClusterState cluster,
        IReadOnlyList<DestinationProbingResult> results)
    {
        if (results.Count == 0) return;

        foreach (var result in results)
        {
            // Considera saludable solo si respondió 2xx y sin excepción
            var isHealthy = result.Response is not null
                && result.Response.IsSuccessStatusCode
                && result.Exception is null;

            result.Destination.Health.Active = isHealthy
                ? DestinationHealth.Healthy
                : DestinationHealth.Unhealthy;
        }
    }
}
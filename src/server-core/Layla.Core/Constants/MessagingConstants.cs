namespace Layla.Core.Constants;

/// <summary>
/// RabbitMQ exchange and routing key constants.
/// </summary>
public static class MessagingConstants
{
    public const string WorldbuildingExchange = "worldbuilding.events";

    public static class RoutingKeys
    {
        public const string ProjectCreated = "project.created";
    }
}

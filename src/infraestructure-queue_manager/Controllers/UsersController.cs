using Microsoft.AspNetCore.Mvc;

namespace EventBus.Api.Controllers;

// TODO
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IRabbitMqPublisher _publisher;

    public UsersController(IRabbitMqPublisher publisher)
        => _publisher = publisher;

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        // Tu lógica de creación aquí...
        var userId = Guid.NewGuid();

        var @event = new UserCreatedEvent(
            UserId: userId,
            Email: request.Email,
            Name: request.Name,
            CreatedAt: DateTimeOffset.UtcNow);

        // Publica con routing key — el consumer filtra por patrón
        _publisher.Publish(@event, routingKey: "user.created");

        return CreatedAtAction(nameof(CreateUser), new { id = userId }, null);
    }
}

public record CreateUserRequest(string Name, string Email);
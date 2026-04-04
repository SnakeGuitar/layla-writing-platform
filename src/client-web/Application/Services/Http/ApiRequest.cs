namespace client_web.Application.Services.Http;

public class APIRequest
{
    public required string Endpoint { get; set; }
    public required HttpMethod Method { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>
    {
        { "Content-Type", "application/json" },
        { "Accept", "application/json" }
    };
    public string? Token { get; set; }
    public object? Body { get; set; }
}
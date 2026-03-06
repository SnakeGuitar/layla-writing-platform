namespace client_web.Services.Http;

public class RequestHttp
{
    public required string Endpoint { get; set; }
    public required HttpMethod Method { get; set; }
    public object? Body { get; set; }
    public string? Token { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}
namespace client_web.Services.Http;

public class RequestHttp<T>
{
    public required string Endpoint { get; set; }
    public required HttpMethod Method { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Token { get; set; }
    public T? Body { get; set; }
}
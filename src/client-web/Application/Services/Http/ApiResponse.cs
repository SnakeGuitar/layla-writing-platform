namespace client_web.Application.Services.Http;

public class APIResponse<T>
{
    public int? StatusCode { get; set; } = 200;
    public string? Message { get; set; } = string.Empty;
    public bool? IsError { get; set; } = false;
    public List<string>? ErrorDetails { get; set; } = default;
    public T? Data { get; set; } = default;
}
namespace client_web.Schemas;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public ApiError Error { get; set; }
}
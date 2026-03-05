namespace client_web.Services.Http;

public class ApiException : Exception
{
    public int Status { get; }
    public object? Data { get; }

    public ApiException(string message, int status, object? data = null)
        : base(message)
    {
        Status = status;
        Data = data;
    }
}
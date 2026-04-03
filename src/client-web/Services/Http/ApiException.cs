namespace client_web.Services.Http;

public class ApiException : Exception
{
    public int Status { get; }
    public object? ResponseData { get; }

    public ApiException(string message, int status, object? responseData = null, Exception? inner = null)
        : base(message, inner)
    {
        Status = status;
        ResponseData = responseData;
    }
}
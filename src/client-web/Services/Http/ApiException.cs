namespace client_web.Services.Http;

public class ApiException : Exception
{
    public int Status { get; }
    public object? ResponseData { get; }

    public ApiException(string message, int status, object? responseData = null)
        : base(message)
    {
        Status = status;
        ResponseData = responseData;
    }
}
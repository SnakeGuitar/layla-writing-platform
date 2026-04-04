namespace client_web.Application.Services.Http;

public class APIException : Exception
{
    public int Status { get; }
    public object? ResponseData { get; }

    public APIException(string message, int status, object? responseData = null, Exception? inner = null)
        : base(message, inner)
    {
        Status = status;
        ResponseData = responseData;
    }
}
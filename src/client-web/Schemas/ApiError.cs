namespace client_web.Schemas;

public class ApiError
{
    public string Message { get; set; }
    public string Code { get; set; }
    public List<string> Details { get; set; }
}
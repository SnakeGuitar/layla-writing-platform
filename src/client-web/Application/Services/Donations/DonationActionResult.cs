namespace client_web.Application.Services.Donations;

public class DonationActionResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public static DonationActionResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static DonationActionResult<T> Fail(string error) => new() { IsSuccess = false, Error = error };
}

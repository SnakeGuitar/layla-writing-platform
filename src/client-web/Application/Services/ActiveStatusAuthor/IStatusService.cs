namespace client_web.Application.Services.ActiveStatusAuthor;

public interface IStatusService
{
    Task WatchProjectAsync(Guid projectId);
}

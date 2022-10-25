using Microsoft.AspNetCore.Components.Forms;

namespace MediaFilesManagementSystem.Data;

public interface IVideoFilesManager : IDisposable
{
    event EventHandler? VideoFilesChanged;

    Task<string> AddVideoFile(IBrowserFile browserFile, User user, CancellationToken token);
    Task<string> ApplyChanges(VideoFile videoFile, User user, CancellationToken token);
    Task<string> DeleteVideoFile(VideoFile videoFile, User user, CancellationToken token);
    Task<string> RejectChanges(VideoFile videoFile, User user, CancellationToken token);
    Task<string> ReplaceVideoFile(IBrowserFile browserFile, User user, CancellationToken token);
    string GetHtmlVideoFilePath(VideoFile file);
}
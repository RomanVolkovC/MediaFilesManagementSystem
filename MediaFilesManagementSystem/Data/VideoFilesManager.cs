using MediaInfoLib;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MediaFilesManagementSystem.Data;

public class VideoFilesManager : IVideoFilesManager
{
    private const string REPLACING_FILE_PREFIX = "replace.";
    private const string BACKUP_FILE_PREFIX = "backup.";
    private const string DIRECTORY_NAME = "Videos";
    private const long MAX_ALLOWED_FILE_SIZE_IN_BYTES = 52428800;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ApplicationContext _context;
    private readonly string _pathToFiles;

    public VideoFilesManager(IDbContextFactory<ApplicationContext> contextFactory, IWebHostEnvironment environment)
    {
        _context = contextFactory.CreateDbContext();
        _pathToFiles = Path.Combine(environment.WebRootPath, "Videos");
    }

    public event EventHandler? VideoFilesChanged;

    public void Dispose()
    {
        _context.Dispose();
        _semaphore.Dispose();
    }

    public string GetHtmlVideoFilePath(VideoFile file) => Path.Combine(DIRECTORY_NAME, Path.GetFileName(file.Path));

    public async Task<string> AddVideoFile(IBrowserFile browserFile, User user, CancellationToken token)
    {
        string videoFileName = Path.GetFileName(browserFile.Name);
        foreach (var prefix in new string[] { REPLACING_FILE_PREFIX, BACKUP_FILE_PREFIX })
            if (videoFileName.StartsWith(prefix))
                return $"Имя файла не может начинаться с \"{prefix}\".";

        string videoFilePath = Path.Combine(_pathToFiles, videoFileName);
        bool videoFileCreated = false;

        EntityEntry<VideoFile>? videoFileEntity = null;

        bool semaphoreEntered = false;

        try
        {
            await _semaphore.WaitAsync(token);
            semaphoreEntered = true;

            if (File.Exists(videoFilePath))
                return "Файл с заданным именем уже существует.";

            FileStream fs = new(videoFilePath, FileMode.CreateNew);
            videoFileCreated = true;
            using (fs)
            {
                using var rs = browserFile.OpenReadStream(MAX_ALLOWED_FILE_SIZE_IN_BYTES, token);
                await rs.CopyToAsync(fs, token);
            }

            MediaInfo mi = new();
            if (mi.Open(videoFilePath) == 0)
                throw new Exception($"MediaInfo не удалось получить доступ к файлу \"{videoFilePath}\".");

            VideoFile videoFile;
            try
            {
                videoFile = new(mi, user);
            }
            finally
            {
                mi.Close();
            }

            videoFileEntity = _context.Entry(videoFile);
            videoFileEntity.State = EntityState.Added;

            await _context.SaveChangesAsync(token);
        }
        catch (Exception)
        {
            if (videoFileCreated)
                File.Delete(videoFilePath);

            throw;
        }
        finally
        {
            if (videoFileEntity != null)
                videoFileEntity.State = EntityState.Detached;
            if (semaphoreEntered)
                _semaphore.Release();
        }

        VideoFilesChanged?.Invoke(this, EventArgs.Empty);
        return string.Empty;
    }

    public async Task<string> ReplaceVideoFile(IBrowserFile browserFile, User user, CancellationToken token)
    {
        string videoFileName = Path.GetFileName(browserFile.Name);
        foreach (var prefix in new string[] { REPLACING_FILE_PREFIX, BACKUP_FILE_PREFIX })
            if (videoFileName.StartsWith(prefix))
                return $"Имя файла не может начинаться с \"{prefix}\".";

        string videoFilePath = Path.Combine(_pathToFiles, videoFileName);

        string replacingVideoFilePath = Path.Combine(_pathToFiles, REPLACING_FILE_PREFIX + videoFileName);
        bool replacingVideoFileCreated = false;

        string backupVideoFilePath = Path.Combine(_pathToFiles, BACKUP_FILE_PREFIX + videoFileName);
        bool videoFileBackuped = false;

        EntityEntry<VideoFile>? existingVideoFileEntity = null;
        EntityEntry<VideoFile>? replacingVideoFileEntity = null;

        bool semaphoreEntered = false;

        try
        {
            await _semaphore.WaitAsync(token);
            semaphoreEntered = true;

            var existingVideoFile = await _context.VideoFiles.AsNoTracking().FirstOrDefaultAsync(file => file.Path == videoFilePath, token);
            if (existingVideoFile == null)
                return await AddVideoFile(browserFile, user, token);

            if (existingVideoFile.State != VideoFileState.None)
                return "Невозможно заменить файл, так как он ожидает подтверждения изменений.";

            FileStream fs = new(replacingVideoFilePath, FileMode.CreateNew);
            replacingVideoFileCreated = true;
            using (fs)
            {
                using var rs = browserFile.OpenReadStream(MAX_ALLOWED_FILE_SIZE_IN_BYTES, token);
                await rs.CopyToAsync(fs, token);
            }

            MediaInfo mi = new();
            if (mi.Open(replacingVideoFilePath) == 0)
                throw new Exception($"MediaInfo не удалось получить доступ к файлу \"{replacingVideoFilePath}\".");

            VideoFile replacingVideoFile;
            try
            {
                replacingVideoFile = new(mi, user);
            }
            finally
            {
                mi.Close();
            }

            if (user.Role == Role.Administrator)
            {
                File.Replace(replacingVideoFilePath, videoFilePath, backupVideoFilePath);
                videoFileBackuped = true;
                replacingVideoFileCreated = false;
                replacingVideoFile.Path = videoFilePath;
            }
            else
            {
                replacingVideoFile.State = VideoFileState.Replacing;
                existingVideoFile.State = VideoFileState.BeingReplaced;
                existingVideoFile.BeingReplacedBy = user;
                existingVideoFile.BeingReplacedById = user.Id;
                existingVideoFile.BeingReplacedOn = replacingVideoFile;
                existingVideoFile.BeingReplacedOnId = replacingVideoFile.Id;
            }

            replacingVideoFileEntity = _context.Entry(replacingVideoFile);
            replacingVideoFileEntity.State = EntityState.Added;

            existingVideoFileEntity = _context.Entry(existingVideoFile);
            if (user.Role == Role.Administrator)
            {
                existingVideoFileEntity.State = EntityState.Deleted;
            }
            else
            {
                existingVideoFileEntity.Property(file => file.State).IsModified = true;
                existingVideoFileEntity.Property(file => file.BeingReplacedById).IsModified = true;
                existingVideoFileEntity.Property(file => file.BeingReplacedOnId).IsModified = true;
            }

            await _context.SaveChangesAsync(token);

            videoFileBackuped = false;
            File.Delete(backupVideoFilePath);
        }
        catch (Exception)
        {
            if (videoFileBackuped)
                File.Move(backupVideoFilePath, videoFilePath);
            if (replacingVideoFileCreated)
                File.Delete(replacingVideoFilePath);

            throw;
        }
        finally
        {
            if (existingVideoFileEntity != null)
                existingVideoFileEntity.State = EntityState.Detached;
            if (replacingVideoFileEntity != null)
                replacingVideoFileEntity.State = EntityState.Detached;
            if (semaphoreEntered)
                _semaphore.Release();
        }

        VideoFilesChanged?.Invoke(this, EventArgs.Empty);
        return string.Empty;
    }

    public async Task<string> DeleteVideoFile(VideoFile videoFile, User user, CancellationToken token)
    {
        bool videoFileChanged = false;

        string? videoFileDirectoryName = Path.GetDirectoryName(videoFile.Path);
        string videoFileBackupPath = videoFileDirectoryName == null
            ? Path.GetFileName(videoFile.Path)
            : Path.Combine(videoFileDirectoryName, BACKUP_FILE_PREFIX + Path.GetFileName(videoFile.Path));
        bool videoFileBackuped = false;

        EntityEntry<VideoFile>? videoFileEntity = null;

        bool semaphoreEntered = false;

        try
        {
            await _semaphore.WaitAsync(token);
            semaphoreEntered = true;

            if (videoFile.State != VideoFileState.None)
                return "Невозможно удалить файл, так как он ожидает подтверждения операции.";

            if (user.Role == Role.Administrator)
            {
                File.Move(videoFile.Path, videoFileBackupPath);
                videoFileBackuped = true;
            }
            else
            {
                videoFile.State = VideoFileState.Deleting;
                videoFile.DeletingBy = user;
                videoFile.DeletingById = user.Id;
                videoFileChanged = true;
            }

            videoFileEntity = _context.Entry(videoFile);
            if (user.Role == Role.Administrator)
            {
                videoFileEntity.State = EntityState.Deleted;
            }
            else
            {
                videoFileEntity.Property(file => file.State).IsModified = true;
                videoFileEntity.Property(file => file.DeletingById).IsModified = true;
            }

            await _context.SaveChangesAsync(token);

            videoFileChanged = false;
            videoFileBackuped = false;
            File.Delete(videoFileBackupPath);
        }
        catch (Exception)
        {
            if (videoFileChanged)
            {
                videoFile.DeletingBy = null;
                videoFile.State = VideoFileState.None;
            }
            if (videoFileBackuped)
                File.Move(videoFileBackupPath, videoFile.Path);

            throw;
        }
        finally
        {
            if (videoFileEntity != null)
                videoFileEntity.State = EntityState.Detached;
            if (semaphoreEntered)
                _semaphore.Release();
        }

        VideoFilesChanged?.Invoke(this, EventArgs.Empty);
        return string.Empty;
    }

    public async Task<string> ApplyChanges(VideoFile videoFile, User user, CancellationToken token)
    {
        if (user.Role != Role.Administrator)
            return "Только администратор может подтверждать изменения файлов.";

        string result;
        bool semaphoreEntered = false;

        try
        {
            await _semaphore.WaitAsync(token);
            semaphoreEntered = true;

            if (videoFile.State == VideoFileState.None)
                return string.Empty;

            result = await (videoFile.State switch
            {
                VideoFileState.Adding => ApplyAdding(videoFile, token),
                VideoFileState.Replacing => ApplyReplacinging(await _context.VideoFiles.Include(file => file.BeingReplacedOn).FirstAsync(file => file.BeingReplacedOnId == videoFile.Id, token), token),
                VideoFileState.BeingReplaced => ApplyReplacinging(videoFile, token),
                VideoFileState.Deleting => ApplyDeleting(videoFile, token),
                _ => throw new Exception("Неизвестное изменение файла."),
            });
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (semaphoreEntered)
                _semaphore.Release();
        }

        if (result == string.Empty)
            VideoFilesChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    public async Task<string> RejectChanges(VideoFile videoFile, User user, CancellationToken token)
    {
        if (user.Role != Role.Administrator)
            return "Только администратор может отменять изменения файлов.";

        string result;
        bool semaphoreEntered = false;

        try
        {
            await _semaphore.WaitAsync(token);
            semaphoreEntered = true;

            if (videoFile.State == VideoFileState.None)
                return string.Empty;

            result = await (videoFile.State switch
            {
                VideoFileState.Adding => RejectAdding(videoFile, token),
                VideoFileState.Replacing => RejectReplacing(await _context.VideoFiles.Include(file => file.BeingReplacedOn).Include(file => file.BeingReplacedBy).FirstAsync(file => file.BeingReplacedOnId == videoFile.Id, token), token),
                VideoFileState.BeingReplaced => RejectReplacing(videoFile, token),
                VideoFileState.Deleting => RejectDeleting(videoFile, token),
                _ => throw new Exception("Неизвестное изменение файла."),
            });
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (semaphoreEntered)
                _semaphore.Release();
        }

        if (result == string.Empty)
            VideoFilesChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    private async Task<string> ApplyAdding(VideoFile videoFile, CancellationToken token)
    {
        EntityEntry<VideoFile>? videoFileEntity = null;

        try
        {
            videoFile.State = VideoFileState.None;

            videoFileEntity = _context.Entry(videoFile);
            videoFileEntity.Property(file => file.State).IsModified = true;

            await _context.SaveChangesAsync(token);

            return string.Empty;
        }
        catch (Exception)
        {
            videoFile.State = VideoFileState.Adding;

            throw;
        }
        finally
        {
            if (videoFileEntity != null)
                videoFileEntity.State = EntityState.Detached;
        }
    }

    private async Task<string> ApplyReplacinging(VideoFile beingReplacedVideoFile, CancellationToken token)
    {
        if (beingReplacedVideoFile.BeingReplacedOn == null)
            throw new Exception("У файла должно быть указано, на какой другой файл он заменяется.");

        string oldReplacingFilePath = beingReplacedVideoFile.BeingReplacedOn.Path;
        bool replacingFileChanged = false;

        string? beingReplacedVideoFileDirectoryName = Path.GetDirectoryName(beingReplacedVideoFile.Path);
        string beingReplacedVideoFileBackupName = BACKUP_FILE_PREFIX + Path.GetFileName(beingReplacedVideoFile.Path);
        string beingReplacedVideoFileBackupPath = beingReplacedVideoFileDirectoryName == null
            ? beingReplacedVideoFileBackupName
            : Path.Combine(beingReplacedVideoFileDirectoryName, beingReplacedVideoFileBackupName);
        bool beingReplacedVideoFileBackuped = false;

        EntityEntry<VideoFile>? beingReplacedVideoFileEntity = null;
        EntityEntry<VideoFile>? replacingVideoFileEntity = null;

        try
        {
            File.Replace(oldReplacingFilePath, beingReplacedVideoFile.Path, beingReplacedVideoFileBackupPath);
            beingReplacedVideoFileBackuped = true;

            beingReplacedVideoFile.BeingReplacedOn.Path = beingReplacedVideoFile.Path;
            beingReplacedVideoFile.BeingReplacedOn.State = VideoFileState.None;
            replacingFileChanged = true;

            beingReplacedVideoFileEntity = _context.Entry(beingReplacedVideoFile);
            beingReplacedVideoFileEntity.State = EntityState.Deleted;

            replacingVideoFileEntity = _context.Entry(beingReplacedVideoFile.BeingReplacedOn);
            replacingVideoFileEntity.Property(file => file.Path).IsModified = true;
            replacingVideoFileEntity.Property(file => file.State).IsModified = true;

            await _context.SaveChangesAsync(token);

            replacingFileChanged = false;
            beingReplacedVideoFileBackuped = false;
            File.Delete(beingReplacedVideoFileBackupPath);

            return string.Empty;
        }
        catch (Exception)
        {
            if (replacingFileChanged)
            {
                beingReplacedVideoFile.BeingReplacedOn.Path = oldReplacingFilePath;
                beingReplacedVideoFile.BeingReplacedOn.State = VideoFileState.Replacing;
            }
            if (beingReplacedVideoFileBackuped)
            {
                File.Move(beingReplacedVideoFile.Path, oldReplacingFilePath);
                File.Move(beingReplacedVideoFileBackupPath, beingReplacedVideoFile.Path);
            }

            throw;
        }
        finally
        {
            if (beingReplacedVideoFileEntity != null)
                beingReplacedVideoFileEntity.State = EntityState.Detached;
            if (replacingVideoFileEntity != null)
                replacingVideoFileEntity.State = EntityState.Detached;
        }
    }

    private async Task<string> ApplyDeleting(VideoFile videoFile, CancellationToken token)
    {
        string? videoFileDirectoryName = Path.GetDirectoryName(videoFile.Path);
        string videoFileBackupName = BACKUP_FILE_PREFIX + Path.GetFileName(videoFile.Path);
        string videoFileBackupPath = videoFileDirectoryName == null ? videoFileBackupName : Path.Combine(videoFileDirectoryName, videoFileBackupName);
        bool videoFileBackuped = false;

        EntityEntry<VideoFile>? videoFileEntity = null;

        try
        {
            File.Move(videoFile.Path, videoFileBackupPath);
            videoFileBackuped = true;

            videoFileEntity = _context.Entry(videoFile);
            videoFileEntity.State = EntityState.Deleted;

            await _context.SaveChangesAsync(token);

            videoFileBackuped = false;
            File.Delete(videoFileBackupPath);

            return string.Empty;
        }
        catch (Exception)
        {
            if (videoFileBackuped)
                File.Move(videoFileBackupPath, videoFile.Path);

            throw;
        }
        finally
        {
            if (videoFileEntity != null)
                videoFileEntity.State = EntityState.Detached;
        }
    }

    private async Task<string> RejectAdding(VideoFile videoFile, CancellationToken token)
    {
        string? videoFileDirectoryName = Path.GetDirectoryName(videoFile.Path);
        string videoFileBackupName = BACKUP_FILE_PREFIX + Path.GetFileName(videoFile.Path);
        string videoFileBackupPath = videoFileDirectoryName == null ? videoFileBackupName : Path.Combine(videoFileDirectoryName, videoFileBackupName);
        bool videoFileBackuped = false;

        EntityEntry<VideoFile>? videoFileEntity = null;

        try
        {
            File.Move(videoFile.Path, videoFileBackupPath);
            videoFileBackuped = true;

            videoFileEntity = _context.Entry(videoFile);
            videoFileEntity.State = EntityState.Deleted;

            await _context.SaveChangesAsync(token);

            videoFileBackuped = false;
            File.Delete(videoFileBackupPath);

            return string.Empty;
        }
        catch (Exception)
        {
            if (videoFileBackuped)
                File.Move(videoFileBackupPath, videoFile.Path);

            throw;
        }
        finally
        {
            if (videoFileEntity != null)
                videoFileEntity.State = EntityState.Detached;
        }
    }

    private async Task<string> RejectReplacing(VideoFile beingReplacedVideoFile, CancellationToken token)
    {
        if (beingReplacedVideoFile.BeingReplacedBy == null)
            throw new Exception("У файла должно быть указано, кем он заменяется.");
        if (beingReplacedVideoFile.BeingReplacedOn == null)
            throw new Exception("У файла должно быть указано, на какой другой файл он заменяется.");

        User replacingUser = beingReplacedVideoFile.BeingReplacedBy;
        VideoFile replacingVideoFile = beingReplacedVideoFile.BeingReplacedOn;
        bool beingReplacedVideoFileChanged = false;

        string? replacingVideoFileDirectoryName = Path.GetDirectoryName(replacingVideoFile.Path);
        string replacingVideoFileBackupName = BACKUP_FILE_PREFIX + Path.GetFileName(replacingVideoFile.Path);
        string replacingVideoFileBackupPath = replacingVideoFileDirectoryName == null
            ? replacingVideoFileBackupName
            : Path.Combine(replacingVideoFileDirectoryName, replacingVideoFileBackupName);
        bool replacingMediaFileBackuped = false;

        EntityEntry<VideoFile>? beingReplacedVideoFileEntity = null;
        EntityEntry<VideoFile>? replacingVideoFileEntity = null;

        try
        {
            File.Move(replacingVideoFile.Path, replacingVideoFileBackupPath);
            replacingMediaFileBackuped = true;

            beingReplacedVideoFile.BeingReplacedBy = null;
            beingReplacedVideoFile.BeingReplacedById = null;
            beingReplacedVideoFile.BeingReplacedOn = null;
            beingReplacedVideoFile.BeingReplacedOnId = null;
            beingReplacedVideoFile.State = VideoFileState.None;
            beingReplacedVideoFileChanged = true;

            beingReplacedVideoFileEntity = _context.Entry(beingReplacedVideoFile);
            beingReplacedVideoFileEntity.Property(file => file.BeingReplacedById).IsModified = true;
            beingReplacedVideoFileEntity.Property(file => file.BeingReplacedOnId).IsModified = true;
            beingReplacedVideoFileEntity.Property(file => file.State).IsModified = true;

            replacingVideoFileEntity = _context.Entry(replacingVideoFile);
            replacingVideoFileEntity.State = EntityState.Deleted;

            await _context.SaveChangesAsync(token);

            beingReplacedVideoFileChanged = false;
            replacingMediaFileBackuped = false;
            File.Delete(replacingVideoFileBackupPath);

            return string.Empty;
        }
        catch (Exception)
        {
            if (beingReplacedVideoFileChanged)
            {
                beingReplacedVideoFile.BeingReplacedBy = replacingUser;
                beingReplacedVideoFile.BeingReplacedOn = replacingVideoFile;
                beingReplacedVideoFile.State = VideoFileState.BeingReplaced;
            }
            if (replacingMediaFileBackuped)
                File.Move(replacingVideoFileBackupPath, replacingVideoFile.Path);

            throw;
        }
        finally
        {
            if (beingReplacedVideoFileEntity != null)
                beingReplacedVideoFileEntity.State = EntityState.Detached;
            if (replacingVideoFileEntity != null)
                replacingVideoFileEntity.State = EntityState.Detached;
        }
    }

    private async Task<string> RejectDeleting(VideoFile videoFile, CancellationToken token)
    {
        if (videoFile.DeletingBy == null)
            throw new Exception("У файла должно быть указано, кем он удаляется.");

        EntityEntry<VideoFile>? videoFileEntity = null;

        try
        {
            videoFile.State = VideoFileState.None;
            videoFile.DeletingBy = null;
            videoFile.DeletingById = null;

            videoFileEntity = _context.Entry(videoFile);
            videoFileEntity.Property(file => file.State).IsModified = true;
            videoFileEntity.Property(file => file.DeletingById).IsModified = true;

            await _context.SaveChangesAsync(token);

            return string.Empty;
        }
        catch (Exception)
        {
            videoFile.State = VideoFileState.Deleting;

            throw;
        }
        finally
        {
            if (videoFileEntity != null)
                videoFileEntity.State = EntityState.Detached;
        }
    }
}

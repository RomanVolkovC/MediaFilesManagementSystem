using MediaFilesManagementSystem.Data;
using MediaFilesManagementSystem.Table;
using MediaFilesManagementSystem.Table.Data;
using MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MediaFilesManagementSystem.Pages.Shared;

public partial class Index : IDisposable
{
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private IColumn<VideoFile>[] _tableColumns;
	private Button<VideoFile>[] _buttons;
	private IQueryable<VideoFile> _videoFiles;
	private Table<VideoFile> _table;
	private User _user;
	private ApplicationContext _context;

	[Parameter]
	public string UserName { get; init; }

	[Inject]
	public IDbContextFactory<ApplicationContext> ContextFactory { get; init; }

    [Inject]
    public IVideoFilesManager VideoFilesManager { get; init; }

	[Inject]
	public IMessagesManager MessagesManager { get; init; }

	[Inject]
	public IJSRuntime JSRuntime { get; init; }

    protected override void OnInitialized()
	{
		if (UserName == null)
			throw new ArgumentNullException(nameof(UserName));

		_context = ContextFactory.CreateDbContext();
		_user = _context.Users.First(user => user.Name == UserName);

        List<IColumn<VideoFile>> tableColumns = new()
		{
			new Column<VideoFile, string>("Название файла",
				ShowVideoFile,
				new() { { "class", "link-primary hover" } },
				file => file.Path, path => Path.GetFileName(path),
				new StringFilter<string>()),
			new Column<VideoFile, string>("Кодек видео", file => file.VideoCodec, new StringFilter<string>()),
			new Column<VideoFile, int>("Ширина", file => file.Width, new NumberFilter<int>()),
			new Column<VideoFile, int>("Высота", file => file.Height, new NumberFilter<int>()),
			new Column<VideoFile, TimeSpan>("Длительность видео", file => file.Duration, new StringFilter<TimeSpan>()),
			new Column<VideoFile, int>("Битрейт видео", file => file.VideoBitrate, new NumberFilter<int>()),
			new Column<VideoFile, string>("Режим соотношения сторон", file => file.AspectRatioMode, new StringFilter<string>()),
			new Column<VideoFile, double>("Соотношение сторон", file => file.AspectRatio, new NumberFilter<double>()),
			new Column<VideoFile, string>("Режим частоты кадров", file => file.FrameRateMode, new StringFilter<string>()),
			new Column<VideoFile, double>("Частота кадров", file => file.FrameRate, new NumberFilter<double>()),
			new Column<VideoFile, string>("Тип сканирования", file => file.ScanType, new StringFilter<string>()),

			new Column<VideoFile, string>("Кодек аудио", file => file.AudioCodec, new StringFilter<string>()),
			new Column<VideoFile, TimeSpan>("Длительность аудио", file => file.AudioDuration, new StringFilter<TimeSpan>()),
			new Column<VideoFile, string>("Режим битрейта аудио", file => file.BitrateMode, new StringFilter<string>()),
			new Column<VideoFile, int>("Битрейт аудио", file => file.AudioBitrate, new NumberFilter<int>()),
			new Column<VideoFile, string>("Режим сжатия", file => file.CompressionMode, new StringFilter<string>()),
			new Column<VideoFile, string>("Позиции каналов", file => file.ChannelPositions, new StringFilter<string>()),
			new Column<VideoFile, int>("Частота дискретизации", file => file.SamplingRate, new NumberFilter<int>())
		};

		Dictionary<string, object> deleteButtonAttributesWithoutMediaFileState = new() { { "class", "btn btn-primary" } };
		Dictionary<string, object> deleteButtonAttributesWithMediaFileState = new(deleteButtonAttributesWithoutMediaFileState) { { "disabled", "disabled" } };

		List<Button<VideoFile>> buttons = new() { new("Удалить", OnDelete, videoFile =>
			videoFile.State == VideoFileState.None ? deleteButtonAttributesWithoutMediaFileState : deleteButtonAttributesWithMediaFileState) };

		if (_user.Role == Role.Administrator)
        {
			tableColumns.AddRange(
				new IColumn<VideoFile>[]
				{
					new Column<VideoFile, byte>("Состояние",
						file => (byte)file.State,
						state => typeof(VideoFileState).GetMember(((VideoFileState)state).ToString()).First().GetCustomAttribute<DisplayAttribute>()?.Name
							?? Enum.GetName((VideoFileState)state)
								?? throw new Exception($"Не удалось преобразовать {state} в {nameof(VideoFileState)}."),
						new EnumFilter<VideoFileState>()),
					new Column<VideoFile, string>("Добавлен пользователем", file => file.AddedBy.Name, new StringFilter<string>()),
					new Column<VideoFile, string>("Заменяется пользователем", file => file.BeingReplacedBy == null ? string.Empty : file.BeingReplacedBy.Name, new StringFilter<string>()),
					new Column<VideoFile, string>("Заменяется на",
						ShowChangingVideoFile,
						new() { { "class", "link-primary hover" } },
						file => file.BeingReplacedOn == null ? string.Empty : file.BeingReplacedOn.Path, path => path == string.Empty ? string.Empty : Path.GetFileName(path),
						new StringFilter<string>()),
					new Column<VideoFile, string>("Удаляется пользователем", file => file.DeletingBy == null ? string.Empty : file.DeletingBy.Name, new StringFilter<string>()),
				});

			_videoFiles = _context.VideoFiles.Include(file => file.AddedBy).Include(file => file.BeingReplacedBy).Include(file => file.BeingReplacedOn).Include(file => file.DeletingBy).AsNoTracking();

			Dictionary<string, object> applyButtonAttributesWithMediaFileState = new() { { "class", "btn btn-primary" } };
			Dictionary<string, object> applyButtonAttributesWithoutMediaFileState = new(applyButtonAttributesWithMediaFileState) { { "disabled", "disabled" } };

			buttons.Add(new("Подтвердить", OnApply, videoFile =>
				videoFile.State == VideoFileState.None ? applyButtonAttributesWithoutMediaFileState : applyButtonAttributesWithMediaFileState));

			Dictionary<string, object> rejectButtonAttributesWithMediaFileState = new() { { "class", "btn btn-primary" } };
			Dictionary<string, object> rejectButtonAttributesWithoutMediaFileState = new(applyButtonAttributesWithMediaFileState) { { "disabled", "disabled" } };

			buttons.Add(new("Отменить", OnReject, videoFile =>
				videoFile.State == VideoFileState.None ? rejectButtonAttributesWithoutMediaFileState : rejectButtonAttributesWithMediaFileState));
		}
        else
        {
			_videoFiles = _context.VideoFiles.Where(file => file.State == VideoFileState.None).AsNoTracking();
        }

		_tableColumns = tableColumns.ToArray();
		_buttons = buttons.ToArray();

		VideoFilesManager.VideoFilesChanged += VideoFileManager_MediaFilesChanged;
	}

    public void Dispose()
    {
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
		_context.Dispose();
		VideoFilesManager.VideoFilesChanged -= VideoFileManager_MediaFilesChanged;
	}

	private async Task OnDelete(VideoFile videoFile)
	{
		string error = await VideoFilesManager.DeleteVideoFile(videoFile, _user, _cancellationTokenSource.Token);

        if (error != string.Empty)
            await MessagesManager.ShowMessage(error);
    }
	
	private async Task OnApply(VideoFile videoFile)
    {
		string error = await VideoFilesManager.ApplyChanges(videoFile, _user, _cancellationTokenSource.Token);

        if (error != string.Empty)
			await MessagesManager.ShowMessage(error);
	}

	private async Task OnReject(VideoFile videoFile)
    {
		string error = await VideoFilesManager.RejectChanges(videoFile, _user, _cancellationTokenSource.Token);

        if (error != string.Empty)
			await MessagesManager.ShowMessage(error);
	}

	private void VideoFileManager_MediaFilesChanged(object? sender, EventArgs e) => InvokeAsync(_table.CollectionChanged);

	private void ShowChangingVideoFile(VideoFile file)
    {
		if (file.BeingReplacedOn != null)
			ShowVideoFile(file.BeingReplacedOn);
    }

	private void ShowVideoFile(VideoFile file) => JSRuntime.InvokeAsync<object>("open", VideoFilesManager.GetHtmlVideoFilePath(file), "_blank");
}

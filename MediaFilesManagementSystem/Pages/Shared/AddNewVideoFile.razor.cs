using MediaFilesManagementSystem.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MediaFilesManagementSystem.Pages.Shared;

public partial class AddNewVideoFile : IDisposable
{
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private IBrowserFile? _file;
	private bool _replace;
	private bool _adding;

	[Parameter]
	[EditorRequired]
	public User User { get; init; }

	[Inject]
	public IVideoFilesManager VideoFilesManager { get; set; }

	[Inject]
	public IMessagesManager MessagesManager { get; set; }

	public void Dispose()
	{
		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
	}

	protected override void OnInitialized()
	{
		if (User == null)
			throw new ArgumentNullException(nameof(User));
	}

    private async Task AddVideoFile()
	{
		if (_file == null)
			throw new NullReferenceException("Не указан файл, который необходимо добавить.");

		_adding = true;

		string error = await (_replace
			? VideoFilesManager.ReplaceVideoFile(_file, User, _cancellationTokenSource.Token)
			: VideoFilesManager.AddVideoFile(_file, User, _cancellationTokenSource.Token));

		if (error != string.Empty)
			await MessagesManager.ShowMessage(error);

		_adding = false;
	}
}

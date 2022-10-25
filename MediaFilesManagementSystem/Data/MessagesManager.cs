using Microsoft.JSInterop;

namespace MediaFilesManagementSystem.Data;

public class MessagesManager : IMessagesManager
{
    private readonly IJSRuntime _jsRuntime;

    public MessagesManager(IJSRuntime jSRuntime) => _jsRuntime = jSRuntime;

    public Task ShowMessage(string message) => _jsRuntime.InvokeVoidAsync("alert", message).AsTask();
}

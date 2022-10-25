using Microsoft.AspNetCore.Components;

namespace MediaFilesManagementSystem.Table.Data;

public record SimpleAsyncCallback(Func<Task> Callback) : IHandleEvent
{
    public Task Invoke() => Callback();
    public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
}

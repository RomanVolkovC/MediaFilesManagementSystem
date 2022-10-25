using Microsoft.AspNetCore.Components;

namespace MediaFilesManagementSystem.Table.Data;

public record SimpleCallback(Action Callback) : IHandleEvent
{
    public static Action Create(Action callback) => new SimpleCallback(callback).Invoke;
    public static Func<Task> Create(Func<Task> callback) => new SimpleAsyncCallback(callback).Invoke;

    public void Invoke() => Callback();
    public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);
}

namespace MediaFilesManagementSystem.Data;

public interface IMessagesManager
{
    Task ShowMessage(string message);
}
namespace MediaFilesManagementSystem.Table.Data;

public class Button<T>
{
    public Button(string name, Func<T, Task> onClick, Func<T, Dictionary<string, object>> getAttributes)
    {
        Name = name;
        OnClick = onClick;
        GetAttributes = getAttributes;
    }

    public string Name { get; }
    public Func<T, Task> OnClick { get; }
    public Func<T, Dictionary<string, object>> GetAttributes { get; }
}

namespace MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

public interface IFilter
{
    event EventHandler? ValidConditionSetted;

    bool HasCondition { get; }
    string? ErrorMessage { get; }
    string? Condition { get; set; }
}

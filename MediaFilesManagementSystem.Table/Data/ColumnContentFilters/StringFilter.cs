using Microsoft.EntityFrameworkCore;

namespace MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

public class StringFilter<T> : Filter<T>
    where T : IComparable<T>, IComparable
{
    private string? _condition;

    public override string? Condition
    {
        get => _condition;
        set
        {
            if (value == _condition)
                return;

            _condition = value;

            ResetMeetsCondition();

            if (!string.IsNullOrEmpty(value))
                MeetsCondition = item => EF.Functions.Like(item as string, value);

            OnValidConditionSetted();
        }
    }
}

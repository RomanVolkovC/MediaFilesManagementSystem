using System.Linq.Expressions;

namespace MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

public abstract class Filter<TItemValue> : IFilter
    where TItemValue : IComparable<TItemValue>, IComparable
{
    private static readonly Expression<Func<TItemValue, bool>> _defaultMeetsCondition = tItemValue => true;

    public event EventHandler? ValidConditionSetted;

    public bool HasCondition => MeetsCondition != _defaultMeetsCondition;
    public Expression<Func<TItemValue, bool>> MeetsCondition { get; protected set; } = _defaultMeetsCondition;
    public string? ErrorMessage { get; protected set; }
    public abstract string? Condition { get; set; }

    protected void OnValidConditionSetted() => ValidConditionSetted?.Invoke(this, EventArgs.Empty);
    protected void ResetMeetsCondition() => MeetsCondition = _defaultMeetsCondition;
}

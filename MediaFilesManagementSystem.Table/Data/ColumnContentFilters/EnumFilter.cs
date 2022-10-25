namespace MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

public class EnumFilter<TEnum> : Filter<byte>
    where TEnum : struct, Enum
{
    private string? _condition;

    public EnumFilter()
    {
        if (Enum.GetUnderlyingType(typeof(TEnum)) != typeof(byte))
            throw new Exception($"Тип данных {nameof(TEnum)} должен быть \"byte\".");
    }

    public override string? Condition
    {
        get => _condition;
        set
        {
            if (value == _condition)
                return;

            _condition = value;
            ErrorMessage = null;

            ResetMeetsCondition();

            if (string.IsNullOrEmpty(value))
            {
                OnValidConditionSetted();

                return;
            }
            
            if (Enum.TryParse(value, true, out TEnum tEnum) && Enum.IsDefined(tEnum))
            {
                byte byteTEnum = Convert.ToByte(tEnum);
                MeetsCondition = state => state == byteTEnum;

                OnValidConditionSetted();

                return;
            }

            ErrorMessage = "Такое значение отсутствует";
        }
    }
}

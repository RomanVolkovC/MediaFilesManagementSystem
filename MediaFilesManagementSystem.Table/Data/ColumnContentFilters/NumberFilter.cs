using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MediaFilesManagementSystem.Table.Data.ColumnContentFilters;

public class NumberFilter<TItemValue> : Filter<TItemValue>
    where TItemValue : struct, IComparable<TItemValue>, IComparable
{
    private delegate bool TryParse(string? s, out TItemValue result);

    private readonly TryParse _tryParseConditionValue;
    private string? _condition;
    private bool _greaterOrLess;
    private bool _greater;
    private bool _equals;

    public NumberFilter()
    {
        var TItemValueType = typeof(TItemValue);
        var TItemValueTryParse = TItemValueType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string), TItemValueType.MakeByRefType() });

        if (TItemValueTryParse == null || TItemValueTryParse.ReturnType != typeof(bool))
            throw new Exception($"Тип должен содержать метод \"public static bool TryParse(string? s, out {nameof(TItemValue)} result)\".");
        
        _tryParseConditionValue = TItemValueTryParse.CreateDelegate<TryParse>();
    }

    public override string? Condition
    {
        get => _condition;
        set
        {
            if (value == _condition)
                return;

            _condition = value;
            _greaterOrLess = false;
            ErrorMessage = null;

            ResetMeetsCondition();

            if (string.IsNullOrEmpty(value))
            {
                OnValidConditionSetted();

                return;
            }

            var match = Regex.Match(value, @"^(?<greaterOrLess><|>)?(?<equals>=)?(?<value>\w+)$", RegexOptions.Compiled);
            if (match.Success)
            {
                var greaterOrLessGroup = match.Groups["greaterOrLess"];

                if (greaterOrLessGroup.Success)
                {
                    _greaterOrLess = true;
                    _greater = greaterOrLessGroup.Value == ">";
                }

                _equals = match.Groups["equals"].Success;

                if (_tryParseConditionValue(match.Groups["value"].Value, out var conditionValueToCompare))
                {
                    var itemParam = Expression.Parameter(typeof(TItemValue));
                    var conditionValueConst = Expression.Constant(conditionValueToCompare);

                    BinaryExpression result = _greaterOrLess
                        ? _equals
                            ? _greater
                                ? Expression.GreaterThanOrEqual(itemParam, conditionValueConst)
                                : Expression.LessThanOrEqual(itemParam, conditionValueConst)
                            : _greater
                                ? Expression.GreaterThan(itemParam, conditionValueConst)
                                : Expression.LessThan(itemParam, conditionValueConst)
                        : Expression.Equal(itemParam, conditionValueConst);

                    MeetsCondition = Expression.Lambda<Func<TItemValue, bool>>(result, itemParam);

                    OnValidConditionSetted();

                    return;
                }
            }

            ErrorMessage = "Не удалось распознать условие.";
        }
    }
}

namespace FinanceTracker.Domain;
public static class FinanceRules
{
    public static DateOnly MonthOf(DateOnly date) => new(date.Year, date.Month, 1);
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    public static string Name(string value)
    {
        value = value.Trim();
        Require(value.Length is > 0 and <= 80, "Название должно содержать от 1 до 80 символов.");
        return value;
    }
    public static void Money(decimal value, bool positive = true)
    {
        Require((!positive || value > 0) && Math.Abs(value) <= 999_999_999m &&
            decimal.Round(value, 2) == value, "Укажите сумму до 999 999 999 ₽, с точностью до копеек.");
    }
}

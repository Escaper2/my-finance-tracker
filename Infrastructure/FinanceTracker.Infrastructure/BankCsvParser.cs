using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FinanceTracker.Application;
using FinanceTracker.Domain;
namespace FinanceTracker.Infrastructure;
public sealed class BankCsvParser : ICsvParser
{
    // Explicit, documented normalized bank export format; no guessing signs or transfer semantics.
    public CsvPreview Parse(string content)
    {
        using var reader = new StringReader(content.TrimStart('\uFEFF'));
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";", TrimOptions = TrimOptions.Trim, IgnoreBlankLines = true,
            ExceptionMessagesContainRawData = false
        });
        var rows = new List<CsvRow>(); var errors = new List<CsvError>();
        try
        {
            FinanceRules.Require(csv.Read(), "Файл пуст.");
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];
            FinanceRules.Require(new[] { "date", "amount", "type", "description", "category", "bank_id" }.SequenceEqual(headers),
                "Ожидается заголовок: date;amount;type;description;category;bank_id");
            while (csv.Read())
            {
                var number = csv.Parser.Row;
                if (rows.Count + errors.Count >= 20000) throw new InvalidOperationException("В файле допускается не более 20 000 строк.");
                try
                {
                    FinanceRules.Require(csv.Parser.Count == 6, "Ожидается ровно 6 колонок.");
                    FinanceRules.Require(DateOnly.TryParseExact(csv.GetField("date"), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date), "Дата должна иметь формат yyyy-MM-dd.");
                    var rawAmount = (csv.GetField("amount") ?? "").Replace(',', '.');
                    FinanceRules.Require(decimal.TryParse(rawAmount, NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out var amount), "Некорректная сумма.");
                    FinanceRules.Money(amount);
                    var type = csv.GetField("type") switch
                    {
                        "expense" => TransactionType.Expense, "income" => TransactionType.Income,
                        _ => throw new InvalidOperationException("Тип должен быть expense или income; переводы между своими счетами исключите.")
                    };
                    var description = csv.GetField("description") ?? "";
                    var category = csv.GetField("category") ?? "";
                    var bankId = csv.GetField("bank_id");
                    FinanceRules.Require(description.Length <= 500 && category.Length <= 80 && (bankId?.Length ?? 0) <= 100,
                        "Слишком длинное описание, категория или bank_id.");
                    rows.Add(new(number, date, amount, type, description, category, string.IsNullOrWhiteSpace(bankId) ? null : bankId));
                }
                catch (InvalidOperationException ex) { errors.Add(new(number, ex.Message)); }
            }
        }
        catch (CsvHelperException) { throw new InvalidOperationException("Не удалось прочитать CSV. Проверьте разделители и парные кавычки."); }
        return new(rows, errors);
    }
}


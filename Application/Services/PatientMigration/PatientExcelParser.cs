using ClosedXML.Excel;

namespace ClinicOps.Application.Services.PatientMigrations
{
    public interface IPatientExcelParser
    {
        IReadOnlyList<string> ReadHeaders(Stream stream);
        IEnumerable<PatientExcelDataRow> ReadDataRows(Stream stream);
    }

    public sealed class PatientExcelDataRow
    {
        public int RowNumber { get; init; }
        public IReadOnlyDictionary<string, object?> Values { get; init; } =
            new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    public class PatientExcelParser : IPatientExcelParser
    {
        public IReadOnlyList<string> ReadHeaders(Stream stream)
        {
            using var workbook = OpenWorkbook(stream);
            var worksheet = RequireWorksheet(workbook);
            return ReadHeaderRow(worksheet).Headers;
        }

        public IEnumerable<PatientExcelDataRow> ReadDataRows(Stream stream)
        {
            using var workbook = OpenWorkbook(stream);
            var worksheet = RequireWorksheet(workbook);
            var (headers, headerRowNumber, lastColumn) = ReadHeaderRow(worksheet);

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRowNumber;
            for (var r = headerRowNumber + 1; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty())
                    continue;

                var values = new Dictionary<string, object?>(StringComparer.Ordinal);
                var anyValue = false;
                for (var c = 1; c <= lastColumn; c++)
                {
                    var header = headers[c - 1];
                    var raw = ReadCell(row.Cell(c));
                    if (raw != null) anyValue = true;
                    if (!values.ContainsKey(header))
                        values[header] = raw;
                }

                if (!anyValue)
                    continue;

                yield return new PatientExcelDataRow
                {
                    RowNumber = r,
                    Values = values
                };
            }
        }

        private static XLWorkbook OpenWorkbook(Stream stream)
        {
            try
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                return new XLWorkbook(stream);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("The uploaded file is not a valid Excel workbook.", ex);
            }
        }

        private static IXLWorksheet RequireWorksheet(XLWorkbook workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.FirstRowUsed() != null);
            if (worksheet == null)
                throw new InvalidOperationException("The Excel file does not contain any worksheets with data.");
            return worksheet;
        }

        private static (List<string> Headers, int HeaderRowNumber, int LastColumn) ReadHeaderRow(IXLWorksheet worksheet)
        {
            var headerRow = worksheet.FirstRowUsed()
                ?? throw new InvalidOperationException("The Excel file does not contain a header row.");

            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastColumn < 1)
                throw new InvalidOperationException("The Excel file does not contain any columns.");

            var headers = new List<string>(lastColumn);
            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var anyHeader = false;

            for (var c = 1; c <= lastColumn; c++)
            {
                var raw = ReadCell(headerRow.Cell(c));
                var name = PatientMigrationRowProcessor.NormalizeText(raw);
                if (string.IsNullOrEmpty(name))
                    name = $"Column {c}";
                else
                    anyHeader = true;

                if (used.TryGetValue(name, out var count))
                {
                    used[name] = count + 1;
                    name = $"{name} ({count + 1})";
                }
                else
                {
                    used[name] = 1;
                }

                headers.Add(name);
            }

            if (!anyHeader)
                throw new InvalidOperationException("The Excel file does not contain a valid header row.");

            return (headers, headerRow.RowNumber(), lastColumn);
        }

        /// <summary>
        /// Reads the stored cell value. Formula cells use the cached result only; nothing is evaluated.
        /// </summary>
        internal static object? ReadCell(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty())
                return null;

            ClosedXML.Excel.XLCellValue value;
            try
            {
                value = cell.HasFormula ? cell.CachedValue : cell.Value;
            }
            catch
            {
                try
                {
                    value = cell.CachedValue;
                }
                catch
                {
                    return null;
                }
            }

            return value.Type switch
            {
                XLDataType.Blank => null,
                XLDataType.Text => NullIfBlank(value.GetText()),
                XLDataType.Number => value.GetNumber(),
                XLDataType.Boolean => value.GetBoolean(),
                XLDataType.DateTime => value.GetDateTime(),
                XLDataType.TimeSpan => value.GetTimeSpan(),
                XLDataType.Error => null,
                _ => NullIfBlank(value.ToString())
            };
        }

        private static object? NullIfBlank(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return text;
        }
    }
}

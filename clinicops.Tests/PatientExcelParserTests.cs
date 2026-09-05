using ClosedXML.Excel;
using ClinicOps.Application.Services.PatientMigrations;
using Xunit;

namespace ClinicOps.Tests.PatientMigration
{
    public class PatientExcelParserTests
    {
        private readonly PatientExcelParser _parser = new();

        [Fact]
        public void ReadsFlexibleHeadersFromFirstRow()
        {
            using var stream = CreateWorkbook(ws =>
            {
                ws.Cell(1, 1).Value = "Emri";
                ws.Cell(1, 2).Value = "Mbiemri";
                ws.Cell(1, 3).Value = "Telefoni";
                ws.Cell(2, 1).Value = "Arben";
                ws.Cell(2, 2).Value = "Krasniqi";
                ws.Cell(2, 3).Value = "044123";
            });

            var headers = _parser.ReadHeaders(stream);
            Assert.Equal(new[] { "Emri", "Mbiemri", "Telefoni" }, headers);
        }

        [Fact]
        public void EmptyWorkbookFails()
        {
            using var stream = CreateWorkbook(_ => { });
            var ex = Assert.Throws<InvalidOperationException>(() => _parser.ReadHeaders(stream));
            Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        }

        [Fact]
        public void UnsupportedOrMalformedStreamFails()
        {
            using var stream = new MemoryStream("this is not excel"u8.ToArray());
            Assert.Throws<InvalidOperationException>(() => _parser.ReadHeaders(stream));
        }

        [Fact]
        public void ReadsDataRowsWithoutEvaluatingFormulas()
        {
            using var stream = CreateWorkbook(ws =>
            {
                ws.Cell(1, 1).Value = "Name";
                ws.Cell(1, 2).Value = "DOB";
                ws.Cell(2, 1).Value = "Drita";
                ws.Cell(2, 2).Value = new DateTime(1992, 8, 20);
                ws.Cell(3, 1).FormulaA1 = "A2";
                ws.Cell(3, 1).SetValue("cached-name");
            });

            var rows = _parser.ReadDataRows(stream).ToList();
            Assert.True(rows.Count >= 1);
            Assert.Equal("Drita", rows[0].Values["Name"]?.ToString());
            Assert.Equal(new DateTime(1992, 8, 20), Assert.IsType<DateTime>(rows[0].Values["DOB"]));
        }

        [Fact]
        public void OneInvalidRowDoesNotPreventReadingOthers()
        {
            using var stream = CreateWorkbook(ws =>
            {
                ws.Cell(1, 1).Value = "Emri";
                ws.Cell(1, 2).Value = "Mbiemri";
                ws.Cell(1, 3).Value = "Datelindja";
                ws.Cell(2, 1).Value = "Valid";
                ws.Cell(2, 2).Value = "Person";
                ws.Cell(2, 3).Value = new DateTime(1990, 1, 1);
                ws.Cell(3, 1).Value = "Bad";
                ws.Cell(3, 2).Value = "Date";
                ws.Cell(3, 3).Value = "32/18/2025";
            });

            var rows = _parser.ReadDataRows(stream).ToList();
            Assert.Equal(2, rows.Count);

            Assert.True(PatientMigrationRowProcessor.TryProcessRow(
                new Dictionary<string, object?>
                {
                    ["firstName"] = rows[0].Values["Emri"],
                    ["lastName"] = rows[0].Values["Mbiemri"],
                    ["dateOfBirth"] = rows[0].Values["Datelindja"]
                },
                out _,
                out _));

            Assert.False(PatientMigrationRowProcessor.TryProcessRow(
                new Dictionary<string, object?>
                {
                    ["firstName"] = rows[1].Values["Emri"],
                    ["lastName"] = rows[1].Values["Mbiemri"],
                    ["dateOfBirth"] = rows[1].Values["Datelindja"]
                },
                out _,
                out var error));
            Assert.Contains("Invalid date of birth", error);
        }

        private static MemoryStream CreateWorkbook(Action<IXLWorksheet> populate)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Patients");
            populate(ws);
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
    }
}

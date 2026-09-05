using ClinicOps.Application.Services.PatientMigrations;
using Xunit;

namespace ClinicOps.Tests.PatientMigration
{
    public class PatientMigrationRowProcessorTests
    {
        [Fact]
        public void NormalizeText_TrimsAndTreatsBlankAsNull()
        {
            Assert.Equal("Edon", PatientMigrationRowProcessor.NormalizeText(" Edon "));
            Assert.Null(PatientMigrationRowProcessor.NormalizeText(""));
            Assert.Null(PatientMigrationRowProcessor.NormalizeText("   "));
            Assert.Null(PatientMigrationRowProcessor.NormalizeText(null));
        }

        [Theory]
        [InlineData("M", "Male")]
        [InlineData("male", "Male")]
        [InlineData("Mashkull", "Male")]
        [InlineData("F", "Female")]
        [InlineData("Female", "Female")]
        [InlineData("Femer", "Female")]
        [InlineData("Femër", "Female")]
        [InlineData("Other", "Other")]
        [InlineData("Tjetër", "Other")]
        public void Gender_MapsKnownValues(string input, string expected)
        {
            Assert.True(PatientMigrationRowProcessor.TryMapGender(input, out var gender, out var error));
            Assert.Equal(expected, gender);
            Assert.Null(error);
        }

        [Fact]
        public void Gender_EmptyIsValidOptional()
        {
            Assert.True(PatientMigrationRowProcessor.TryMapGender("  ", out var gender, out var error));
            Assert.Null(gender);
            Assert.Null(error);
        }

        [Fact]
        public void Gender_UncertainValueIsInvalid()
        {
            Assert.False(PatientMigrationRowProcessor.TryMapGender("unknown-xyz", out var gender, out var error));
            Assert.Null(gender);
            Assert.Contains("Unrecognized gender", error);
        }

        [Fact]
        public void DateOfBirth_AcceptsExcelDateTimeAndIsoString()
        {
            Assert.True(PatientMigrationRowProcessor.TryParseDateOfBirth(new DateTime(1990, 5, 1), out var fromCell, out _));
            Assert.Equal(new DateTime(1990, 5, 1), fromCell);

            Assert.True(PatientMigrationRowProcessor.TryParseDateOfBirth("15/03/1988", out var fromString, out _));
            Assert.Equal(new DateTime(1988, 3, 15), fromString);
        }

        [Fact]
        public void DateOfBirth_RejectsInvalidAndFutureDates()
        {
            Assert.False(PatientMigrationRowProcessor.TryParseDateOfBirth("32/18/2025", out _, out var invalid));
            Assert.Contains("Invalid date of birth", invalid);

            Assert.False(PatientMigrationRowProcessor.TryParseDateOfBirth(DateTime.UtcNow.Date.AddDays(1), out _, out var future));
            Assert.Contains("future", future);
        }

        [Fact]
        public void RequiredFields_AreReportedPerRow()
        {
            var mapped = new Dictionary<string, object?>
            {
                ["firstName"] = " ",
                ["lastName"] = "Gashi",
                ["dateOfBirth"] = "01/01/1990"
            };

            Assert.False(PatientMigrationRowProcessor.TryProcessRow(mapped, out _, out var error));
            Assert.Contains("First name is required", error);
        }

        [Fact]
        public void ValidRow_IsNormalized()
        {
            var mapped = new Dictionary<string, object?>
            {
                ["firstName"] = " Arben ",
                ["lastName"] = "Krasniqi",
                ["dateOfBirth"] = "12.04.1985",
                ["gender"] = "Mashkull",
                ["phone"] = "044123456"
            };

            Assert.True(PatientMigrationRowProcessor.TryProcessRow(mapped, out var row, out var error));
            Assert.Null(error);
            Assert.Equal("Arben", row.FirstName);
            Assert.Equal("Krasniqi", row.LastName);
            Assert.Equal(new DateTime(1985, 4, 12), row.DateOfBirth);
            Assert.Equal("Male", row.Gender);
            Assert.Equal("044123456", row.Phone);
        }

        [Fact]
        public void Phone_RejectsValuesLongerThanTwenty()
        {
            var mapped = new Dictionary<string, object?>
            {
                ["firstName"] = "Ana",
                ["lastName"] = "Berisha",
                ["dateOfBirth"] = "2000-01-01",
                ["phone"] = new string('1', 21)
            };

            Assert.False(PatientMigrationRowProcessor.TryProcessRow(mapped, out _, out var error));
            Assert.Contains("Phone cannot exceed", error);
        }

        [Fact]
        public void SuggestMappings_MatchesAlbanianAndEnglishHeaders()
        {
            var suggested = PatientMigrationRowProcessor.SuggestMappings(
                ["Emri", "Mbiemri", "Telefoni", "Datelindja", "Gjinia"]);

            Assert.Equal("Emri", suggested["firstName"]);
            Assert.Equal("Mbiemri", suggested["lastName"]);
            Assert.Equal("Telefoni", suggested["phone"]);
            Assert.Equal("Datelindja", suggested["dateOfBirth"]);
            Assert.Equal("Gjinia", suggested["gender"]);
        }
    }
}

using ClinicOps.Application.Services.PatientMigrations;
using ClinicOps.Domain.Enums;
using Xunit;

namespace ClinicOps.Tests.PatientMigration
{
    public class CaseMigrationRowProcessorTests
    {
        [Theory]
        [InlineData("Waiting", PatientCaseStatus.Waiting)]
        [InlineData("Në pritje", PatientCaseStatus.Waiting)]
        [InlineData("InConsultation", PatientCaseStatus.InConsultation)]
        [InlineData("Finished", PatientCaseStatus.Finished)]
        [InlineData("Përfunduar", PatientCaseStatus.Finished)]
        [InlineData("Completed", PatientCaseStatus.Finished)]
        [InlineData("Mbyllur", PatientCaseStatus.Mbyllur)]
        [InlineData("Closed", PatientCaseStatus.Mbyllur)]
        public void MapsKnownCaseStatuses(string input, PatientCaseStatus expected)
        {
            Assert.True(CaseMigrationRowProcessor.TryMapCaseStatus(input, out var status, out var error));
            Assert.Equal(expected, status);
            Assert.Null(error);
        }

        [Fact]
        public void EmptyStatusDefaultsToWaiting()
        {
            Assert.True(CaseMigrationRowProcessor.TryMapCaseStatus(" ", out var status, out var error));
            Assert.Equal(PatientCaseStatus.Waiting, status);
            Assert.Null(error);
        }

        [Fact]
        public void UnknownStatusIsInvalid()
        {
            Assert.False(CaseMigrationRowProcessor.TryMapCaseStatus("mystery", out _, out var error));
            Assert.Contains("Unrecognized case status", error);
        }

        [Fact]
        public void PatientLookup_FindsClinicPatient_AndAllowsMultipleCases()
        {
            var lookup = new ClinicPatientLookup();
            var id = Guid.NewGuid();
            lookup.Add(id, "Arben", "Krasniqi", new DateTime(1985, 4, 12), "0441");

            Assert.True(lookup.TryFind("arben", "krasniqi", new DateTime(1985, 4, 12), "0441", out var first, out _));
            Assert.True(lookup.TryFind("Arben", "Krasniqi", null, null, out var second, out _));
            Assert.Equal(id, first);
            Assert.Equal(id, second);
        }

        [Fact]
        public void PatientLookup_DoesNotSeeOtherClinicPatients()
        {
            var clinicA = new ClinicPatientLookup();
            var clinicB = new ClinicPatientLookup();
            clinicA.Add(Guid.NewGuid(), "Drita", "Gashi", new DateTime(1990, 1, 1), null);

            Assert.False(clinicB.TryFind("Drita", "Gashi", null, null, out _, out var error));
            Assert.Contains("No matching patient", error);
        }

        [Fact]
        public void PatientLookup_MatchesUniqueNameWithoutDateOfBirth()
        {
            var lookup = new ClinicPatientLookup();
            var id = Guid.NewGuid();
            lookup.Add(id, "Driton", "Krasniqi", new DateTime(1982, 3, 3), null);

            Assert.True(lookup.TryFind("Driton", "Krasniqi", null, null, out var found, out var error));
            Assert.Equal(id, found);
            Assert.Null(error);
        }

        [Fact]
        public void PatientLookup_RequiresDobOrPhoneWhenNameIsAmbiguous()
        {
            var lookup = new ClinicPatientLookup();
            lookup.Add(Guid.NewGuid(), "Ana", "Hoxha", new DateTime(2001, 2, 2), "111");
            lookup.Add(Guid.NewGuid(), "Ana", "Hoxha", new DateTime(1999, 5, 5), "222");

            Assert.False(lookup.TryFind("Ana", "Hoxha", null, null, out _, out var error));
            Assert.Contains("Multiple patients match this name", error);

            Assert.True(lookup.TryFind("Ana", "Hoxha", new DateTime(1999, 5, 5), null, out _, out var dobError));
            Assert.Null(dobError);

            Assert.True(lookup.TryFind("Ana", "Hoxha", null, "222", out _, out var phoneError));
            Assert.Null(phoneError);
        }

        [Fact]
        public void PatientLookup_RequiresPhoneWhenNameDobIsAmbiguous()
        {
            var lookup = new ClinicPatientLookup();
            lookup.Add(Guid.NewGuid(), "Ana", "Hoxha", new DateTime(2001, 2, 2), "111");
            lookup.Add(Guid.NewGuid(), "Ana", "Hoxha", new DateTime(2001, 2, 2), "222");

            Assert.False(lookup.TryFind("Ana", "Hoxha", new DateTime(2001, 2, 2), null, out _, out var error));
            Assert.Contains("Multiple patients", error);

            Assert.True(lookup.TryFind("Ana", "Hoxha", new DateTime(2001, 2, 2), "222", out _, out var phoneError));
            Assert.Null(phoneError);
        }

        [Fact]
        public void SuggestsCaseFieldMappings()
        {
            var suggested = CaseMigrationRowProcessor.SuggestMappings(
                ["Emri", "Mbiemri", "Datelindja", "Statusi", "Protokolli", "Mjeku", "Shërbimi", "Shënime"]);

            Assert.Equal("Emri", suggested["firstName"]);
            Assert.Equal("Datelindja", suggested["dateOfBirth"]);
            Assert.Equal("Protokolli", suggested["protocolNumber"]);
            Assert.Equal("Mjeku", suggested["assignedDoctor"]);
            Assert.Equal("Shërbimi", suggested["serviceName"]);
            Assert.Equal("Shënime", suggested["notes"]);
            Assert.False(suggested.ContainsKey("caseStatus"));
        }

        [Fact]
        public void SuggestsMappings_WithoutDateOfBirthColumn()
        {
            var suggested = CaseMigrationRowProcessor.SuggestMappings(
                ["Emri", "Mbiemri", "Shënime", "Statusi"]);

            Assert.Equal("Emri", suggested["firstName"]);
            Assert.Equal("Mbiemri", suggested["lastName"]);
            Assert.Equal("Shënime", suggested["notes"]);
            Assert.False(suggested.ContainsKey("dateOfBirth"));
        }

        [Fact]
        public void ImportedCasesAreAlwaysClosed()
        {
            Assert.Equal(PatientCaseStatus.Mbyllur, CaseMigrationRowProcessor.ImportedStatus);
        }
    }
}

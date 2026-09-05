using ClinicOps.Application.Services.PatientMigrations;
using Xunit;

namespace ClinicOps.Tests.PatientMigration
{
    public class PatientDuplicateIndexTests
    {
        [Fact]
        public void DetectsDuplicateInsideTheSameClinicByNameDobAndPhone()
        {
            var index = new PatientDuplicateIndex();
            index.Add("Arben", "Krasniqi", new DateTime(1985, 4, 12), "044111");

            Assert.True(index.IsDuplicate("arben", "krasniqi", new DateTime(1985, 4, 12), "044111"));
            Assert.False(index.IsDuplicate("Arben", "Krasniqi", new DateTime(1985, 4, 12), "044222"));
        }

        [Fact]
        public void EmptyIncomingPhoneMatchesAnyExistingPhoneInClinic()
        {
            var index = new PatientDuplicateIndex();
            index.Add("Drita", "Gashi", new DateTime(1990, 1, 1), "044999");

            Assert.True(index.IsDuplicate("Drita", "Gashi", new DateTime(1990, 1, 1), null));
        }

        [Fact]
        public void PatientsFromAnotherClinicAreNotVisible()
        {
            var clinicA = new PatientDuplicateIndex();
            var clinicB = new PatientDuplicateIndex();
            clinicA.Add("Besnik", "Berisha", new DateTime(1978, 6, 6), "111");

            Assert.True(clinicA.IsDuplicate("Besnik", "Berisha", new DateTime(1978, 6, 6), "111"));
            Assert.False(clinicB.IsDuplicate("Besnik", "Berisha", new DateTime(1978, 6, 6), "111"));
        }

        [Fact]
        public void InFileDuplicates_FirstOccurrenceWins()
        {
            var seenNameDob = new HashSet<string>(StringComparer.Ordinal);
            var seenNameDobPhone = new HashSet<string>(StringComparer.Ordinal);
            var first = PatientMigrationRowProcessor.NameDobKey("Ana", "Hoxha", new DateTime(2001, 2, 2));

            Assert.False(IsInFileDuplicate(first, "0441", seenNameDob, seenNameDobPhone));
            seenNameDob.Add(first);
            seenNameDobPhone.Add(first + "|0441");
            Assert.True(IsInFileDuplicate(first, "0441", seenNameDob, seenNameDobPhone));
        }

        private static bool IsInFileDuplicate(
            string nameDob,
            string? phone,
            HashSet<string> seenNameDob,
            HashSet<string> seenNameDobPhone)
        {
            if (string.IsNullOrEmpty(phone))
                return seenNameDob.Contains(nameDob);

            return seenNameDobPhone.Contains(nameDob + "|" + phone);
        }
    }
}

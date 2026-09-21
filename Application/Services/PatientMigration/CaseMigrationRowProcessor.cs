using ClinicOps.Application.Services.Patient;
using ClinicOps.Domain.Enums;

namespace ClinicOps.Application.Services.PatientMigrations
{
    public static class CaseMigrationRowProcessor
    {
        public const int NotesMaxLength = 500;
        public const int ProtocolMaxLength = 100;

        public static readonly string[] DestinationFieldKeys =
        {
            "firstName",
            "lastName",
            "dateOfBirth",
            "phone",
            "protocolNumber",
            "notes",
            "assignedDoctor",
            "serviceName",
            "createdAt",
            "completedAt"
        };

        public static readonly HashSet<string> RequiredFieldKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "firstName",
            "lastName"
        };

        public static PatientCaseStatus ImportedStatus => PatientCaseStatus.Mbyllur;

        public static Dictionary<string, string> SuggestMappings(IEnumerable<string> headers)
        {
            var suggestions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header) || !used.Add(header))
                    continue;

                var key = PatientMigrationRowProcessor.NormalizeHeaderKey(header);
                var field = key switch
                {
                    "emri" or "emer" or "name" or "firstname" or "first" or "patientfirstname"
                        or "emripacientit" or "givenname" or "pacienti" => "firstName",
                    "mbiemri" or "mbiemer" or "surname" or "lastname" or "last" or "familyname"
                        or "mbiemripacientit" or "patientlastname" => "lastName",
                    "datelindja" or "datelindje" or "dob" or "dateofbirth" or "birthdate" or "birthday"
                        or "lindja" or "dataelindjes" or "datalindjes" => "dateOfBirth",
                    "telefoni" or "telefon" or "phone" or "mobile" or "tel" or "cel" or "celular"
                        or "phonenumber" or "nrtelefonit" or "nrtel" => "phone",
                    "protokolli" or "protokoll" or "protocol" or "protocolnumber" or "nrprotokollit"
                        or "numriiprotokollit" => "protocolNumber",
                    "shenime" or "notes" or "note" or "koment" or "comments" or "verejtje" or "remark"
                        or "remarks" => "notes",
                    "mjeku" or "doctor" or "assigneddoctor" or "mjek" or "doktor" or "physician"
                        or "doctoremail" or "mjekuemail" => "assignedDoctor",
                    "sherbimi" or "service" or "servicename" or "sherbim" => "serviceName",
                    "createdat" or "casedate" or "datarastit" or "data" or "date" or "openedat"
                        or "created" or "dataehapjes" => "createdAt",
                    "completedat" or "closedat" or "datambylljes" or "finishedat" or "completed" => "completedAt",
                    _ => null
                };

                if (field != null && !suggestions.ContainsKey(field))
                    suggestions[field] = header;
            }

            return suggestions;
        }

        public static bool TryMapCaseStatus(object? raw, out PatientCaseStatus status, out string? error)
        {
            status = PatientCaseStatus.Waiting;
            error = null;

            var text = PatientMigrationRowProcessor.NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
                return true;

            if (PatientCaseStatusParser.TryParse(text, out status))
                return true;

            var key = PatientMigrationRowProcessor.NormalizeHeaderKey(text);
            status = key switch
            {
                "nepritje" or "waiting" or "inprogress" or "pritje" => PatientCaseStatus.Waiting,
                "nekonsultim" or "inconsultation" or "konsultim" => PatientCaseStatus.InConsultation,
                "perfunduar" or "finished" or "completed" or "complete" => PatientCaseStatus.Finished,
                "mbyllur" or "closed" or "close" => PatientCaseStatus.Mbyllur,
                _ => default
            };

            if (status == default)
            {
                error = $"Unrecognized case status: \"{text}\". Use Waiting, InConsultation, Finished or Mbyllur.";
                return false;
            }

            return true;
        }

        public static string? NormalizeNotes(object? raw, out string? error)
        {
            error = null;
            var text = PatientMigrationRowProcessor.NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
                return null;

            if (text.Length > NotesMaxLength)
            {
                error = $"Notes cannot exceed {NotesMaxLength} characters.";
                return null;
            }

            return text;
        }

        public static string? NormalizeProtocol(object? raw, out string? error)
        {
            error = null;
            var text = PatientMigrationRowProcessor.NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
                return null;

            if (text.Length > ProtocolMaxLength)
            {
                error = $"Protocol number cannot exceed {ProtocolMaxLength} characters.";
                return null;
            }

            return text;
        }
    }

    public sealed class ClinicPatientLookup
    {
        private readonly List<(Guid Id, string FirstName, string LastName, DateTime DateOfBirth, string? Phone)> _patients = [];

        public void Add(Guid id, string firstName, string lastName, DateTime dateOfBirth, string? phone)
        {
            _patients.Add((
                id,
                firstName.Trim().ToLowerInvariant(),
                lastName.Trim().ToLowerInvariant(),
                dateOfBirth.Date,
                PatientMigrationRowProcessor.NormalizePhone(phone, out _)));
        }

        public bool TryFind(
            string firstName,
            string lastName,
            DateTime? dateOfBirth,
            string? phone,
            out Guid patientId,
            out string? error)
        {
            patientId = default;
            error = null;

            var first = firstName.Trim().ToLowerInvariant();
            var last = lastName.Trim().ToLowerInvariant();
            var matches = _patients
                .Where(p => p.FirstName == first && p.LastName == last)
                .ToList();

            if (dateOfBirth.HasValue)
                matches = matches.Where(p => p.DateOfBirth == dateOfBirth.Value.Date).ToList();

            if (!string.IsNullOrEmpty(phone))
                matches = matches.Where(p => p.Phone == phone).ToList();

            if (matches.Count == 1)
            {
                patientId = matches[0].Id;
                return true;
            }

            if (matches.Count == 0)
            {
                error = dateOfBirth.HasValue || !string.IsNullOrEmpty(phone)
                    ? "No matching patient in this clinic for the given name and details. Import or register the patient first."
                    : "No matching patient in this clinic. Import or register the patient first.";
                return false;
            }

            error = !dateOfBirth.HasValue && string.IsNullOrEmpty(phone)
                ? "Multiple patients match this name. Map date of birth or phone to choose one."
                : "Multiple patients match this name. Map date of birth and phone to choose one.";
            return false;
        }
    }
}

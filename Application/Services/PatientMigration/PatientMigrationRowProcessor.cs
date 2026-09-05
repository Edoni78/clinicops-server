using System.Globalization;
using System.Text.RegularExpressions;

namespace ClinicOps.Application.Services.PatientMigrations
{
    /// <summary>
    /// Normalizes and validates Excel values against existing Patient / RegisterPatientRequest rules.
    /// </summary>
    public static class PatientMigrationRowProcessor
    {
        public const int FirstNameMaxLength = 100;
        public const int LastNameMaxLength = 100;
        public const int GenderMaxLength = 10;
        public const int PhoneMaxLength = 20;
        public static readonly DateTime MinDateOfBirth = new(1900, 1, 1);

        public static readonly string[] DestinationFieldKeys =
        {
            "firstName",
            "lastName",
            "dateOfBirth",
            "gender",
            "phone"
        };

        public static readonly HashSet<string> RequiredFieldKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "firstName",
            "lastName",
            "dateOfBirth"
        };

        public static string? NormalizeText(object? raw)
        {
            if (raw == null) return null;

            string text;
            switch (raw)
            {
                case string s:
                    text = s;
                    break;
                case DateTime:
                    return null;
                case double d when Math.Abs(d % 1) < 0.0000001:
                    text = d.ToString("0", CultureInfo.InvariantCulture);
                    break;
                case float f when Math.Abs(f % 1) < 0.0000001:
                    text = f.ToString("0", CultureInfo.InvariantCulture);
                    break;
                case decimal m when m == decimal.Truncate(m):
                    text = m.ToString("0", CultureInfo.InvariantCulture);
                    break;
                default:
                    text = Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
                    break;
            }

            text = text.Trim();
            return text.Length == 0 ? null : text;
        }

        public static bool TryParseDateOfBirth(object? raw, out DateTime date, out string? error)
        {
            date = default;
            error = null;

            if (raw == null)
            {
                error = "Date of birth is required.";
                return false;
            }

            if (raw is DateTime dt)
                return ValidateDate(dt, out date, out error);

            if (raw is DateTimeOffset dto)
                return ValidateDate(dto.Date, out date, out error);

            if (raw is double oaDouble)
                return TryParseNumericDate(oaDouble, out date, out error);

            if (raw is float oaFloat)
                return TryParseNumericDate(oaFloat, out date, out error);

            if (raw is decimal oaDecimal)
                return TryParseNumericDate((double)oaDecimal, out date, out error);

            if (raw is int oaInt)
                return TryParseNumericDate(oaInt, out date, out error);

            var text = NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
            {
                error = "Date of birth is required.";
                return false;
            }

            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy-M-d",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "dd.MM.yyyy",
                "d.M.yyyy",
                "dd-MM-yyyy",
                "d-M-yyyy",
                "dd/MM/yy",
                "d/M/yy",
                "yyyy/MM/dd",
                "dd MMM yyyy",
                "d MMM yyyy",
                "MMM d yyyy",
                "MMMM d yyyy"
            };

            if (DateTime.TryParseExact(
                    text,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed)
                || DateTime.TryParseExact(
                    text,
                    formats,
                    new CultureInfo("sq-AL"),
                    DateTimeStyles.None,
                    out parsed))
            {
                return ValidateDate(parsed, out date, out error);
            }

            // Last resort: prefer day-first for ambiguous values (Albanian/EU clinics).
            if (DateTime.TryParse(text, new CultureInfo("sq-AL"), DateTimeStyles.None, out parsed)
                || DateTime.TryParse(text, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, out parsed))
            {
                return ValidateDate(parsed, out date, out error);
            }

            error = $"Invalid date of birth: \"{Truncate(text, 40)}\".";
            return false;
        }

        public static bool TryMapGender(object? raw, out string? gender, out string? error)
        {
            gender = null;
            error = null;

            var text = NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
                return true;

            var key = Regex.Replace(text.ToLowerInvariant(), @"\s+", "");
            key = key.Replace("ë", "e").Replace("ç", "c");

            gender = key switch
            {
                "m" or "male" or "mashkull" => "Male",
                "f" or "female" or "femer" or "femere" or "femra" => "Female",
                "other" or "tjetër" or "tjeter" or "o" => "Other",
                _ => null
            };

            if (gender == null)
            {
                error = $"Unrecognized gender value: \"{Truncate(text, 20)}\".";
                return false;
            }

            if (gender.Length > GenderMaxLength)
            {
                error = $"Gender cannot exceed {GenderMaxLength} characters.";
                return false;
            }

            return true;
        }

        public static string? NormalizePhone(object? raw, out string? error)
        {
            error = null;
            var text = NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
                return null;

            if (text.Length > PhoneMaxLength)
            {
                error = $"Phone cannot exceed {PhoneMaxLength} characters.";
                return null;
            }

            return text;
        }

        public static string? NormalizeRequiredName(object? raw, string fieldLabel, int maxLength, out string? error)
        {
            error = null;
            var text = NormalizeText(raw);
            if (string.IsNullOrEmpty(text))
            {
                error = $"{fieldLabel} is required.";
                return null;
            }

            if (text.Length > maxLength)
            {
                error = $"{fieldLabel} cannot exceed {maxLength} characters.";
                return null;
            }

            return text;
        }

        public static bool TryProcessRow(
            IReadOnlyDictionary<string, object?> mappedValues,
            out ProcessedPatientRow row,
            out string? error)
        {
            row = new ProcessedPatientRow();
            error = null;
            var errors = new List<string>();

            mappedValues.TryGetValue("firstName", out var firstRaw);
            mappedValues.TryGetValue("lastName", out var lastRaw);
            mappedValues.TryGetValue("dateOfBirth", out var dobRaw);
            mappedValues.TryGetValue("gender", out var genderRaw);
            mappedValues.TryGetValue("phone", out var phoneRaw);

            var firstName = NormalizeRequiredName(firstRaw, "First name", FirstNameMaxLength, out var firstError);
            if (firstError != null) errors.Add(firstError);

            var lastName = NormalizeRequiredName(lastRaw, "Last name", LastNameMaxLength, out var lastError);
            if (lastError != null) errors.Add(lastError);

            DateTime? dob = null;
            if (!TryParseDateOfBirth(dobRaw, out var parsedDob, out var dobError))
                errors.Add(dobError ?? "Invalid date of birth.");
            else
                dob = parsedDob.Date;

            if (!TryMapGender(genderRaw, out var gender, out var genderError))
                errors.Add(genderError ?? "Invalid gender.");

            var phone = NormalizePhone(phoneRaw, out var phoneError);
            if (phoneError != null) errors.Add(phoneError);

            if (errors.Count > 0)
            {
                error = string.Join(" ", errors);
                row = new ProcessedPatientRow
                {
                    FirstName = firstName,
                    LastName = lastName,
                    DateOfBirth = dob,
                    Gender = gender,
                    Phone = phone
                };
                return false;
            }

            row = new ProcessedPatientRow
            {
                FirstName = firstName!,
                LastName = lastName!,
                DateOfBirth = dob,
                Gender = gender,
                Phone = phone
            };
            return true;
        }

        public static string NormalizeHeaderKey(string header)
        {
            var text = (header ?? string.Empty).Trim().ToLowerInvariant();
            text = text.Replace("ë", "e").Replace("ç", "c");
            return Regex.Replace(text, @"[\s_\-./]+", "");
        }

        public static Dictionary<string, string> SuggestMappings(IEnumerable<string> headers)
        {
            var suggestions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header) || !used.Add(header))
                    continue;

                var key = NormalizeHeaderKey(header);
                var field = key switch
                {
                    "emri" or "emer" or "name" or "firstname" or "first" or "patientfirstname"
                        or "emripacientit" or "givenname" => "firstName",
                    "mbiemri" or "mbiemer" or "surname" or "lastname" or "last" or "familyname"
                        or "mbiemripacientit" or "patientlastname" => "lastName",
                    "datelindja" or "datelindje" or "dob" or "dateofbirth" or "birthdate" or "birthday"
                        or "lindja" or "dataelindjes" or "datalindjes" => "dateOfBirth",
                    "gjinia" or "gender" or "sex" or "seksi" or "gjin" => "gender",
                    "telefoni" or "telefon" or "phone" or "mobile" or "tel" or "cel" or "celular"
                        or "phonenumber" or "nrtelefonit" or "nrtel" or "contact" => "phone",
                    _ => null
                };

                if (field != null && !suggestions.ContainsKey(field))
                    suggestions[field] = header;
            }

            return suggestions;
        }

        public static string DuplicateKey(string firstName, string lastName, DateTime dateOfBirth, string? phone)
        {
            var nameDob =
                firstName.Trim().ToLowerInvariant()
                + "|"
                + lastName.Trim().ToLowerInvariant()
                + "|"
                + dateOfBirth.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return string.IsNullOrEmpty(phone) ? nameDob : nameDob + "|" + phone;
        }

        public static string NameDobKey(string firstName, string lastName, DateTime dateOfBirth) =>
            firstName.Trim().ToLowerInvariant()
            + "|"
            + lastName.Trim().ToLowerInvariant()
            + "|"
            + dateOfBirth.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static bool TryParseNumericDate(double value, out DateTime date, out string? error)
        {
            date = default;
            error = null;

            // Excel serial dates for people alive today are roughly 1..60000.
            if (value is > 1 and < 80000)
            {
                try
                {
                    var oa = DateTime.FromOADate(value);
                    return ValidateDate(oa, out date, out error);
                }
                catch (ArgumentException)
                {
                    error = $"Invalid date of birth: \"{value}\".";
                    return false;
                }
            }

            error = $"Invalid date of birth: \"{value}\".";
            return false;
        }

        private static bool ValidateDate(DateTime value, out DateTime date, out string? error)
        {
            date = value.Date;
            error = null;

            if (date > DateTime.UtcNow.Date)
            {
                error = "Date of birth cannot be in the future.";
                return false;
            }

            if (date < MinDateOfBirth)
            {
                error = "Date of birth is too far in the past.";
                return false;
            }

            return true;
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "…";
    }

    public sealed class ProcessedPatientRow
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Clinic-scoped duplicate index matching PatientService registration rules:
    /// same first name, last name, date of birth; phone must match when provided.
    /// </summary>
    public sealed class PatientDuplicateIndex
    {
        private readonly HashSet<string> _nameDob = new(StringComparer.Ordinal);
        private readonly HashSet<string> _nameDobPhone = new(StringComparer.Ordinal);

        public void Add(string firstName, string lastName, DateTime dateOfBirth, string? phone)
        {
            var nameDob = PatientMigrationRowProcessor.NameDobKey(firstName, lastName, dateOfBirth);
            Add(nameDob, phone);
        }

        public void Add(string nameDob, string? phone)
        {
            _nameDob.Add(nameDob);
            if (!string.IsNullOrEmpty(phone))
                _nameDobPhone.Add(nameDob + "|" + phone);
        }

        public bool IsDuplicate(string firstName, string lastName, DateTime dateOfBirth, string? phone) =>
            IsDuplicate(PatientMigrationRowProcessor.NameDobKey(firstName, lastName, dateOfBirth), phone);

        public bool IsDuplicate(string nameDob, string? phone)
        {
            if (string.IsNullOrEmpty(phone))
                return _nameDob.Contains(nameDob);

            return _nameDobPhone.Contains(nameDob + "|" + phone);
        }
    }
}

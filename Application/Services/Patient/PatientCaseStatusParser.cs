using ClinicOps.Domain.Enums;

namespace ClinicOps.Application.Services.Patient
{
    public static class PatientCaseStatusParser
    {
        public static bool TryParse(string? status, out PatientCaseStatus result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(status))
                return false;

            var normalized = status.Trim() switch
            {
                "InProgress" => nameof(PatientCaseStatus.Waiting),
                "Completed" => nameof(PatientCaseStatus.Finished),
                "Closed" => nameof(PatientCaseStatus.Mbyllur),
                _ => status.Trim()
            };

            return Enum.TryParse(normalized, ignoreCase: true, out result);
        }

        public static string AllowedStatusesMessage =>
            "Invalid status. Use: Waiting, InConsultation, Finished, Mbyllur.";
    }
}

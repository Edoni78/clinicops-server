using ClinicOps.Domain.Entities;
using System.Security.Claims;

namespace ClinicOps.Application.Services.Patient
{
    public static class PatientCaseProtocolHelper
    {
        public static string Normalize(string? value) => (value ?? "").Trim();

        public static bool IsProtocolRequired(Clinic clinic) => clinic.UseProtocolNumber;

        public static bool HasProtocolNumber(PatientCase @case) =>
            !string.IsNullOrWhiteSpace(@case.ProtocolNumber);

        public static void EnsureProtocolBeforeFinish(Clinic clinic, PatientCase @case)
        {
            if (!IsProtocolRequired(clinic)) return;
            if (!HasProtocolNumber(@case))
                throw new InvalidOperationException(
                    "Numri i protokollit është i detyrueshëm për të mbyllur rastin. Vendoseni para përfundimit.");
        }

        public static void EnsureCanEditProtocol(Clinic clinic, ClaimsPrincipal user)
        {
            if (!clinic.UseProtocolNumber)
                throw new InvalidOperationException("Numri i protokollit nuk është aktivizuar për këtë klinikë.");

            if (user.IsInRole("ClinicAdmin") || user.IsInRole("SuperAdmin"))
                return;

            var isNurse = user.IsInRole("Nurse");
            var isDoctor = user.IsInRole("Doctor");

            if (isNurse && clinic.ProtocolEditableByNurse) return;
            if (isDoctor && clinic.ProtocolEditableByDoctor) return;

            throw new UnauthorizedAccessException("Nuk keni të drejtë të ndryshoni numrin e protokollit.");
        }

        public static bool CanUserEditProtocol(Clinic clinic, ClaimsPrincipal user)
        {
            if (!clinic.UseProtocolNumber) return false;
            if (user.IsInRole("ClinicAdmin") || user.IsInRole("SuperAdmin")) return true;
            if (user.IsInRole("Nurse") && clinic.ProtocolEditableByNurse) return true;
            if (user.IsInRole("Doctor") && clinic.ProtocolEditableByDoctor) return true;
            return false;
        }
    }
}

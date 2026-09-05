using ClinicOps.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class PatientCase
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public PatientCaseStatus Status { get; set; } = PatientCaseStatus.Waiting;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public string? AssignedDoctorUserId { get; set; }
        public ApplicationUser? AssignedDoctor { get; set; }

        public Guid? ServiceId { get; set; }
        public Service? Service { get; set; }

        [MaxLength(100)]
        public string? ProtocolNumber { get; set; }

        public bool IsWaiting => Status == PatientCaseStatus.Waiting;

        public bool HasProtocolNumber() => !string.IsNullOrWhiteSpace(ProtocolNumber);

        public static PatientCase OpenWaiting(
            Guid clinicId,
            Guid patientId,
            string assignedDoctorUserId,
            string? notes)
        {
            return new PatientCase
            {
                ClinicId = clinicId,
                PatientId = patientId,
                Status = PatientCaseStatus.Waiting,
                Notes = notes,
                AssignedDoctorUserId = assignedDoctorUserId,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateWaitingAssignment(string assignedDoctorUserId, string? notes)
        {
            AssignedDoctorUserId = assignedDoctorUserId;
            if (!string.IsNullOrEmpty(notes))
                Notes = notes;
        }

        public void AttachService(Guid serviceId)
        {
            ServiceId = serviceId;
        }

        public static string NormalizeProtocolNumber(string? value) => (value ?? "").Trim();

        public void SetProtocolNumber(string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Numri i protokollit nuk mund të jetë bosh.");

            ProtocolNumber = normalized.Trim();
        }

        public void EnsureProtocolBeforeFinish(Clinic clinic)
        {
            if (!clinic.IsProtocolRequired()) return;
            if (!HasProtocolNumber())
                throw new InvalidOperationException(
                    "Numri i protokollit është i detyrueshëm për të mbyllur rastin. Vendoseni para përfundimit.");
        }

        public bool CanTransitionTo(PatientCaseStatus to) =>
            (Status, to) switch
            {
                (PatientCaseStatus.Waiting, PatientCaseStatus.InConsultation) => true,
                (PatientCaseStatus.InConsultation, PatientCaseStatus.Finished) => true,
                (PatientCaseStatus.Finished, PatientCaseStatus.Mbyllur) => true,
                _ => Status == to,
            };

        /// <summary>
        /// Applies a status transition and related side effects.
        /// Caller must enforce cross-case rules (e.g. one InConsultation per clinic) and authorization.
        /// </summary>
        public void TransitionTo(PatientCaseStatus to, Clinic clinic)
        {
            if (to == PatientCaseStatus.Finished)
                EnsureProtocolBeforeFinish(clinic);

            if (to == PatientCaseStatus.Mbyllur)
            {
                if (Status != PatientCaseStatus.Finished)
                    throw new InvalidOperationException(
                        "Vetëm rastet e përfunduara nga mjeku mund të mbyllen nga infermieri.");
            }
            else if (!CanTransitionTo(to))
            {
                throw new InvalidOperationException(
                    $"Nuk lejohet kalimi nga {Status} në {to}.");
            }

            Status = to;
            if (to == PatientCaseStatus.Finished)
                CompletedAt = DateTime.UtcNow;
        }
    }
}

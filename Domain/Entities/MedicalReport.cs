using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    public class MedicalReport
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientCaseId { get; set; }
        public PatientCase PatientCase { get; set; } = null!;

        [MaxLength(2000)]
        public string? Anamneza { get; set; }

        [MaxLength(2000)]
        public string? Ekzaminimi { get; set; }

        [Required]
        [MaxLength(500)]
        public string Diagnosis { get; set; } = null!;

        [Required]
        public string Therapy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid DoctorId { get; set; }
        public string? DoctorUserId { get; set; }

        public static MedicalReport Create(
            Guid clinicId,
            Guid patientCaseId,
            string? anamneza,
            string? ekzaminimi,
            string diagnosis,
            string therapy,
            string doctorUserId)
        {
            var report = new MedicalReport
            {
                ClinicId = clinicId,
                PatientCaseId = patientCaseId,
                DoctorId = Guid.Empty,
                CreatedAt = DateTime.UtcNow
            };
            report.ApplyContent(anamneza, ekzaminimi, diagnosis, therapy, doctorUserId);
            return report;
        }

        public void ApplyContent(
            string? anamneza,
            string? ekzaminimi,
            string diagnosis,
            string therapy,
            string doctorUserId)
        {
            Anamneza = anamneza;
            Ekzaminimi = ekzaminimi;
            Diagnosis = diagnosis;
            Therapy = therapy;
            DoctorUserId = doctorUserId;
        }
    }
}

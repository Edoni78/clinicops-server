namespace ClinicOps.Domain.Entities
{
    public class PatientPrivacyState
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public bool IsDeleted { get; set; }
        public bool IsAnonymized { get; set; }

        public DateTime? DeletedAtUtc { get; set; }
        public DateTime? AnonymizedAtUtc { get; set; }

        public static PatientPrivacyState CreateAnonymized(Guid patientId)
        {
            return new PatientPrivacyState
            {
                PatientId = patientId,
                IsDeleted = false,
                IsAnonymized = true,
                AnonymizedAtUtc = DateTime.UtcNow
            };
        }

        public void MarkAnonymized()
        {
            IsAnonymized = true;
            AnonymizedAtUtc = DateTime.UtcNow;
        }

        public void MarkDeleted()
        {
            IsDeleted = true;
            DeletedAtUtc = DateTime.UtcNow;
        }
    }
}

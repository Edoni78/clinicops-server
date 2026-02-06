namespace ClinicOps.API.DTOs.Patient
{
    public class PatientResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public Guid? PatientCaseId { get; set; }
        public string? PatientCaseStatus { get; set; }
    }
}

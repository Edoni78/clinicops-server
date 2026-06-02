using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.Patient
{
    public class OpenPatientCaseRequest
    {
        [Required(ErrorMessage = "Assigned doctor is required")]
        public string AssignedDoctorUserId { get; set; } = null!;

        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public Guid? ClinicId { get; set; }
    }
}

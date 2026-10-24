using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.PatientCase
{
    public class DentalFindingDto
    {
        public Guid Id { get; set; }
        public int ToothNumber { get; set; }
        public string Dentition { get; set; } = "Adult";
        public string Condition { get; set; } = "Healthy";
        public string? Surfaces { get; set; }
        public string? Treatment { get; set; }
        public string? Notes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpsertDentalFindingRequest
    {
        [Range(11, 85)]
        public int ToothNumber { get; set; }

        [MaxLength(20)]
        public string Dentition { get; set; } = "Adult";

        [Required, MaxLength(40)]
        public string Condition { get; set; } = "Healthy";

        [MaxLength(80)]
        public string? Surfaces { get; set; }

        [MaxLength(80)]
        public string? Treatment { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

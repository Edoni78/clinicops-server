using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.Service
{
    public class ServiceDto
    {
        public Guid Id { get; set; }
        public Guid ClinicId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateServiceRequest
    {
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }

    public class UpdateServiceRequest
    {
        [MaxLength(300)]
        public string? Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }
    }
}

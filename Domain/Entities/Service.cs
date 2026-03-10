using System.ComponentModel.DataAnnotations;

namespace ClinicOps.Domain.Entities
{
    /// <summary>
    /// A service offered by a clinic (e.g. Kontrolle, Kontrolle + Analiza) with a price.
    /// </summary>
    public class Service
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}

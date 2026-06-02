using System.ComponentModel.DataAnnotations;

namespace ClinicOps.API.DTOs.PatientCase
{
    public class UpdateProtocolNumberRequest
    {
        [Required]
        [MaxLength(100)]
        public string ProtocolNumber { get; set; } = null!;
    }
}

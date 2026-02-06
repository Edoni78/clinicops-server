using ClinicOps.API.DTOs.Patient;
using ClinicOps.Application.Services.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        /// <summary>
        /// Register a patient at reception and create a waiting case
        /// </summary>
        /// <param name="request">Patient registration details</param>
        /// <returns>Registered patient with case information</returns>
        /// <response code="200">Patient registered successfully</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="401">Unauthorized - Invalid or missing token</response>
        /// <response code="403">Forbidden - SuperAdmin cannot register patients</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientResponseDto>> RegisterPatient(
            [FromBody] RegisterPatientRequest request)
        {
            // Get clinic ID from JWT claims
            var clinicIdClaim = User.FindFirst("clinicId")?.Value;

            if (string.IsNullOrEmpty(clinicIdClaim))
            {
                return Forbid("Only clinic users can register patients. SuperAdmin cannot perform this action.");
            }

            if (!Guid.TryParse(clinicIdClaim, out var clinicId))
            {
                return BadRequest("Invalid clinic ID in token.");
            }

            try
            {
                var result = await _patientService.RegisterPatientAtReceptionAsync(
                    clinicId,
                    request);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}

using ClinicOps.API.DTOs.DoctorProfile;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.DoctorProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctorProfileController : ControllerBase
    {
        private readonly IDoctorProfileService _doctorProfileService;

        public DoctorProfileController(IDoctorProfileService doctorProfileService)
        {
            _doctorProfileService = doctorProfileService;
        }

        [Authorize(Roles = "Doctor,ClinicAdmin")]
        [HttpGet("profile")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> GetProfile()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _doctorProfileService.EnsureAccessAsync(userId, User.IsInRole("Doctor"));
                return Ok(await _doctorProfileService.GetProfileAsync(userId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("signoff/{userId}")]
        [ProducesResponseType(typeof(DoctorSignoffDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorSignoffDto>> GetSignoff(string userId)
        {
            var requesterId = User.GetUserId();
            if (string.IsNullOrEmpty(requesterId))
                return Unauthorized();

            try
            {
                return Ok(await _doctorProfileService.GetSignoffAsync(userId, requesterId));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = "Doctor,ClinicAdmin")]
        [HttpPut("profile")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> UpdateProfile([FromBody] UpdateDoctorProfileRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _doctorProfileService.EnsureAccessAsync(userId, User.IsInRole("Doctor"));
                return Ok(await _doctorProfileService.UpdateProfileAsync(userId, request));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize(Roles = "Doctor,ClinicAdmin")]
        [HttpPost("profile/signature")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> UploadSignature(IFormFile? file, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _doctorProfileService.EnsureAccessAsync(userId, User.IsInRole("Doctor"));
                return Ok(await _doctorProfileService.UploadSignatureAsync(userId, file, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize(Roles = "Doctor,ClinicAdmin")]
        [HttpPost("profile/stamp")]
        [ProducesResponseType(typeof(DoctorProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorProfileDto>> UploadStamp(IFormFile? file, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                await _doctorProfileService.EnsureAccessAsync(userId, User.IsInRole("Doctor"));
                return Ok(await _doctorProfileService.UploadStampAsync(userId, file, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}

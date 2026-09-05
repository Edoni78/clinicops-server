using ClinicOps.API.DTOs.DoctorProfile;
using ClinicOps.Application.Services.Common;
using ClinicOps.Application.Services.DoctorProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class DoctorProfileController : ControllerBase
    {
        private readonly IDoctorProfileService _doctorProfileService;

        public DoctorProfileController(IDoctorProfileService doctorProfileService)
        {
            _doctorProfileService = doctorProfileService;
        }

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
                return Ok(await _doctorProfileService.GetProfileAsync(userId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

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
                return Ok(await _doctorProfileService.UpdateProfileAsync(userId, request));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

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
                return Ok(await _doctorProfileService.UploadSignatureAsync(userId, file, cancellationToken));
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
                return Ok(await _doctorProfileService.UploadStampAsync(userId, file, cancellationToken));
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

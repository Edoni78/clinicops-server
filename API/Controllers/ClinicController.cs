using ClinicOps.API.DTOs.Clinic;
using ClinicOps.Application.Services.ClinicProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClinicController : ControllerBase
    {
        private readonly IClinicProfileService _clinicProfileService;

        public ClinicController(IClinicProfileService clinicProfileService)
        {
            _clinicProfileService = clinicProfileService;
        }

        /// <summary>
        /// Get the logged-in clinic's profile (card: name, logo, address, phone, description).
        /// Only clinic users (with clinicId in token) can call this; SuperAdmin returns 400.
        /// </summary>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ClinicProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicProfileDto>> GetProfile()
        {
            try
            {
                return Ok(await _clinicProfileService.GetProfileAsync(User));
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

        /// <summary>
        /// Update the logged-in clinic's profile (name, address, phone, logo URL, description).
        /// Only clinic users (e.g. ClinicAdmin) can update.
        /// </summary>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ClinicProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicProfileDto>> UpdateProfile([FromBody] UpdateClinicProfileRequest request)
        {
            try
            {
                return Ok(await _clinicProfileService.UpdateProfileAsync(request, User));
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

        /// <summary>
        /// Upload a logo for the logged-in clinic. Saves file under wwwroot/uploads/clinics/{clinicId}/ and sets LogoUrl.
        /// </summary>
        [HttpPost("profile/logo")]
        [ProducesResponseType(typeof(ClinicProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicProfileDto>> UploadLogo(IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _clinicProfileService.UploadLogoAsync(file, User, cancellationToken));
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

using ClinicOps.API.DTOs.ClinicUser;
using ClinicOps.Application.Services.ClinicUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClinicUserController : ControllerBase
    {
        private readonly IClinicUserService _clinicUserService;

        public ClinicUserController(IClinicUserService clinicUserService)
        {
            _clinicUserService = clinicUserService;
        }

        /// <summary>
        /// List users for the clinic. ClinicAdmin/Nurse see their clinic; SuperAdmin can pass clinicId query. Optional role filter.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ClinicAdmin,SuperAdmin,Nurse,Doctor")]
        [ProducesResponseType(typeof(List<ClinicUserListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClinicUserListItemDto>>> List(
            [FromQuery] Guid? clinicId = null,
            [FromQuery] string? role = null)
        {
            try
            {
                return Ok(await _clinicUserService.ListAsync(User, clinicId, role));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Create a clinic user (Doctor, Nurse, or LabTechnician). ClinicAdmin: uses their clinic. SuperAdmin: pass clinicId in body or query.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ClinicAdmin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClinicUserListItemDto>> Create(
            [FromBody] CreateClinicUserRequest request,
            [FromQuery] Guid? clinicId = null)
        {
            try
            {
                var dto = await _clinicUserService.CreateAsync(request, User, clinicId);
                return CreatedAtAction(nameof(List), dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete a clinic staff user (Doctor, Nurse, LabTechnician).
        /// ClinicAdmin can delete users in their own clinic; SuperAdmin can delete by clinicId query.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "ClinicAdmin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id, [FromQuery] Guid? clinicId = null)
        {
            try
            {
                await _clinicUserService.DeleteAsync(id, User, clinicId);
                return NoContent();
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

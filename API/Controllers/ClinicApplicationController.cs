using ClinicOps.API.DTOs.ClinicApplication;
using ClinicOps.Application.Services.ClinicApplications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class ClinicApplicationController : ControllerBase
    {
        private readonly IClinicApplicationService _clinicApplicationService;

        public ClinicApplicationController(IClinicApplicationService clinicApplicationService)
        {
            _clinicApplicationService = clinicApplicationService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ClinicApplicationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClinicApplicationDto>>> List([FromQuery] string? status = null)
        {
            return Ok(await _clinicApplicationService.ListAsync(status));
        }

        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRejectRequest? request = null)
        {
            try
            {
                var result = await _clinicApplicationService.ApproveAsync(id, request?.ReviewNote);
                return Ok(new
                {
                    message = result.Message,
                    clinicId = result.ClinicId,
                    adminUserId = result.AdminUserId
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] ApproveRejectRequest? request = null)
        {
            try
            {
                await _clinicApplicationService.RejectAsync(id, request?.ReviewNote);
                return Ok(new { message = "Application rejected." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

using ClinicOps.API.DTOs.Service;
using ClinicOps.Application.Services.ClinicCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceController : ControllerBase
    {
        private readonly IClinicServiceCatalogService _serviceCatalog;

        public ServiceController(IClinicServiceCatalogService serviceCatalog)
        {
            _serviceCatalog = serviceCatalog;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<ServiceDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ServiceDto>>> List([FromQuery] Guid? clinicId = null)
        {
            try
            {
                return Ok(await _serviceCatalog.ListAsync(User, clinicId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceDto>> GetById(Guid id, [FromQuery] Guid? clinicId = null)
        {
            try
            {
                return Ok(await _serviceCatalog.GetByIdAsync(id, User, clinicId));
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

        [HttpPost]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceDto>> Create(
            [FromBody] CreateServiceRequest request,
            [FromQuery] Guid? clinicId = null)
        {
            try
            {
                var dto = await _serviceCatalog.CreateAsync(request, User, clinicId);
                return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceDto>> Update(
            Guid id,
            [FromBody] UpdateServiceRequest request,
            [FromQuery] Guid? clinicId = null)
        {
            try
            {
                return Ok(await _serviceCatalog.UpdateAsync(id, request, User, clinicId));
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

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? clinicId = null)
        {
            try
            {
                await _serviceCatalog.DeleteAsync(id, User, clinicId);
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

using ClinicOps.API.DTOs.CaseMigration;
using ClinicOps.API.DTOs.PatientMigration;
using ClinicOps.Application.Services.PatientMigrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/PatientCaseMigration")]
    [Authorize(Roles = "ClinicAdmin")]
    public class PatientCaseMigrationController : ControllerBase
    {
        private readonly IPatientCaseMigrationService _migrationService;

        public PatientCaseMigrationController(IPatientCaseMigrationService migrationService)
        {
            _migrationService = migrationService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(25 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
        [ProducesResponseType(typeof(PatientMigrationUploadResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PatientMigrationUploadResponse>> Upload(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            try
            {
                if (file == null)
                    return BadRequest("Please choose a non-empty Excel file.");

                return Ok(await _migrationService.UploadAsync(file, User, cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{migrationId:guid}/preview")]
        [ProducesResponseType(typeof(CaseMigrationPreviewResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CaseMigrationPreviewResponse>> Preview(
            Guid migrationId,
            [FromBody] PatientMigrationPreviewRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _migrationService.PreviewAsync(migrationId, request ?? new(), User, cancellationToken));
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

        [HttpGet("{migrationId:guid}/rows")]
        [ProducesResponseType(typeof(CaseMigrationRowsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CaseMigrationRowsResponse>> GetRows(
            Guid migrationId,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _migrationService.GetRowsAsync(migrationId, status, page, pageSize, User, cancellationToken));
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

        [HttpPost("{migrationId:guid}/confirm")]
        [ProducesResponseType(typeof(CaseMigrationConfirmResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CaseMigrationConfirmResponse>> Confirm(
            Guid migrationId,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _migrationService.ConfirmAsync(migrationId, User, cancellationToken));
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

        [HttpGet("{migrationId:guid}")]
        [ProducesResponseType(typeof(CaseMigrationStatusResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CaseMigrationStatusResponse>> Get(
            Guid migrationId,
            CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _migrationService.GetAsync(migrationId, User, cancellationToken));
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

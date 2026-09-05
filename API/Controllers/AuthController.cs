using ClinicOps.API.DTOs.Auth;
using ClinicOps.Application.Services.Auth;
using ClinicOps.Application.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicOps.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthLoginService _authLoginService;
        private readonly IAuthMfaService _authMfaService;
        private readonly IClinicRegistrationService _clinicRegistrationService;

        public AuthController(
            IAuthLoginService authLoginService,
            IAuthMfaService authMfaService,
            IClinicRegistrationService clinicRegistrationService)
        {
            _authLoginService = authLoginService;
            _authMfaService = authMfaService;
            _clinicRegistrationService = clinicRegistrationService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                return Ok(await _authLoginService.LoginAsync(request));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("mfa/verify-login")]
        public async Task<ActionResult<AuthResponse>> VerifyLoginMfa([FromBody] VerifyLoginMfaRequest request)
        {
            try
            {
                return Ok(await _authLoginService.VerifyMfaLoginAsync(request));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("mfa/setup")]
        [Authorize]
        public async Task<ActionResult<MfaSetupResponse>> GenerateMfaSetup()
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                return Ok(await _authMfaService.GenerateSetupAsync(userId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("mfa/enable")]
        [Authorize]
        public async Task<ActionResult<MfaEnabledResponse>> EnableMfa([FromBody] VerifyMfaCodeRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                return Ok(await _authMfaService.EnableAsync(userId, request.Code));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForClinic([FromBody] RegisterClinicRequest req)
        {
            try
            {
                await _clinicRegistrationService.ApplyForClinicAsync(req);
                return Ok("Application submitted successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

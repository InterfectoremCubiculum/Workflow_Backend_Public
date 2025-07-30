using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.AdminPanel.Dtos;
using WorkflowTime.Features.AdminPanel.Services;

namespace WorkflowTime.Features.AdminPanel.Controllers
{
    /// <summary>
    /// Provides API endpoints for managing application settings within the admin panel.
    /// </summary>
    /// <remarks>This controller is restricted to users with administrator access, as specified by the
    /// "OnlyAdministratorAccess" policy. It allows administrators to retrieve and update application settings,
    /// including user synchronization settings.</remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "OnlyAdministratorAccess")]
    public class AdminPanelController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        public AdminPanelController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        /// <summary>
        /// Retrieves a list of settings.
        /// </summary>
        /// <remarks>This method asynchronously fetches the settings from the underlying service and
        /// returns them as a list of <see cref="GetSettingDto"/> objects.</remarks>
        /// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="GetSettingDto"/> objects representing the
        /// settings.</returns>
        [HttpGet("Settings")]
        public async Task<ActionResult<List<GetSettingDto>>> GetSettings()
        {
            var result = await _settingsService.GetSettings();
            return Ok(result);
        }

        /// <summary>
        /// Updates the application settings with the specified parameters.
        /// </summary>
        /// <param name="parameters">A list of <see cref="UpdatedSettingDto"/> objects representing the settings to be updated. Cannot be null.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="NoContentResult"/>
        /// if the update is successful.</returns>
        [HttpPost("Settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] List<UpdatedSettingDto> parameters)
        {
            await _settingsService.UpdateSettings(parameters);
            return NoContent();
        }

        /// <summary>
        /// Updates user synchronization settings with the provided parameters.
        /// </summary>
        /// <remarks>This method processes a list of user synchronization settings and applies the updates
        /// asynchronously.</remarks>
        /// <param name="parameters">A list of updated settings to apply. Each setting is represented by an <see cref="UpdatedSettingDto"/>
        /// object.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation. Returns <see cref="NoContentResult"/>
        /// if the update is successful.</returns>
        [HttpPost("Settings/UserSync")]
        public async Task<IActionResult> UpdateSettings_UserSync([FromBody] List<UpdatedSettingDto> parameters)
        {
            await _settingsService.UpdateSettings_UserSync(parameters);
            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;
using WorkflowTime.Configuration;
using WorkflowTime.Features.UserManagement.Dtos;

namespace WorkflowTime.Features.Auth
{
    /// <summary>
    /// Provides authentication-related endpoints for login, logout, and retrieving user information.
    /// </summary>
    /// <remarks>This controller handles authentication processes using OpenID Connect and cookie-based
    /// authentication. It includes endpoints for logging in, logging out, and fetching authenticated user
    /// details.</remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppFrontendUrlOptions _appFrontendUrl;
        public AuthController(IOptions<AppFrontendUrlOptions> appFrontendUrl)
        {
            _appFrontendUrl = appFrontendUrl.Value;
        }

        /// <summary>
        /// Initiates the login process using OpenID Connect authentication.
        /// </summary>
        /// <remarks>This method is accessible anonymously and redirects the user to the authentication
        /// provider. Upon successful authentication, the user is redirected to the specified frontend URL.</remarks>
        /// <returns>An <see cref="IActionResult"/> that challenges the user with the OpenID Connect authentication scheme.</returns>
        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {

            string finalRedirectUri = _appFrontendUrl.Url + "/auth-popup-success";
            var properties = new AuthenticationProperties { RedirectUri = finalRedirectUri };
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Logs the user out by terminating their authentication sessions and deleting the authentication cookie.
        /// </summary>
        /// <remarks>This method signs the user out of both the cookie and OpenID Connect authentication
        /// schemes. It also deletes the authentication cookie to ensure the user is fully logged out.</remarks>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the logout operation, with a success message.</returns>
        [HttpGet("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            Response.Cookies.Delete("WorkflowTimeAuthCookie", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new { message = "Succesfully logout" });
        }

        /// <summary>
        /// Retrieves the authenticated user's information.
        /// </summary>
        /// <remarks>This method requires the user to be authenticated. It returns the user's ID, email,
        /// given name, surname, and role. If the user is not authenticated, the method returns an unauthorized
        /// response.</remarks>
        /// <returns>An <see cref="IActionResult"/> containing the user's information if authenticated; otherwise, an
        /// unauthorized response.</returns>
        [HttpGet("user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
  
            if (IsAuthenticated)
            {
                GetMeDto userInfo = new GetMeDto
                {
                    UserId = Guid.TryParse(User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier"), out var userId)
                        ? userId : Guid.Empty,
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("emails") ?? string.Empty,
                    GivenName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                    Surname = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
                    Role = User.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault() ?? string.Empty
                };

                return Ok(new
                {
                    IsAuthenticated,
                    getMeDto = userInfo
                });
            }
            return Unauthorized();
        }
    }
}

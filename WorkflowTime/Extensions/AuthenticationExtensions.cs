using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WorkflowTime.Configuration;
namespace WorkflowTime.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddWorkflowTimeAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            AppFrontendUrlOptions appFrontendUrlOptions,
            AzureAdOptions azureAdOptions)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Zmienić
                    options.Cookie.SameSite = SameSiteMode.None; // Zmienić jeżeli bedzie https dla frontendu
                    options.Cookie.Name = "WorkflowTimeAuthCookie";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.SlidingExpiration = true;
                })
                .AddOpenIdConnect(options =>
                {
                    options.ClientSecret = azureAdOptions.ClientSecret;
                    options.Authority = $"{azureAdOptions.Instance}{azureAdOptions.TenantId}";
                    options.ClientId = azureAdOptions.ClientId;
                    options.CallbackPath = azureAdOptions.CallbackPath;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("offline_access");
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProviderForSignOut = context =>
                        {
                            context.ProtocolMessage.PostLogoutRedirectUri = appFrontendUrlOptions.Url;
                            return Task.CompletedTask;
                        },
                        OnRedirectToIdentityProvider =  context =>
                        {
                            if (context.HttpContext.Request.Path.StartsWithSegments("/api/auth/login"))
                            {
                                context.ProtocolMessage.Prompt = "select_account";
                                return Task.CompletedTask;

                            }
                            if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
                            {
                                context.Response.StatusCode = 401;
                                context.HandleResponse();
                            }
                            return Task.CompletedTask;
                        },
                    };
                })
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();
            
            return services;
        }

        private static readonly string[] scopes = new[] { "https://graph.microsoft.com/.default" };

        public static IServiceCollection AddMicrosoftGraphClientCredentials(this IServiceCollection services, AzureAdOptions azureAdOptions)
        {
            services.AddSingleton<GraphServiceClient>(sp =>
            {
                var tenantId = azureAdOptions.TenantId;
                var clientId = azureAdOptions.ClientId;
                var clientSecret = azureAdOptions.ClientSecret;

                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);

                return new GraphServiceClient(clientSecretCredential, scopes);
            });

            return services;
        }
    }
}

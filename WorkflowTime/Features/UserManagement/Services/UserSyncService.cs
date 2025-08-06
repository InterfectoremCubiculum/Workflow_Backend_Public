using Microsoft.Graph.Models;
using Microsoft.Graph;
using WorkflowTime.Features.UserManagement.Validators;
using WorkflowTime.Features.UserManagment.Models;
using AutoMapper;
using WorkflowTime.Database;
using WorkflowTime.Features.DeltaLink;
using Microsoft.Graph.Users.Delta;
using Microsoft.EntityFrameworkCore;
using Polly.Registry;
using Polly;
using ZiggyCreatures.Caching.Fusion;
using Microsoft.Extensions.Options;
using WorkflowTime.Configuration;
using WorkflowTime.Enums;

namespace WorkflowTime.Features.UserManagement.Services
{
    public class UserSyncService : IUserSyncService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly GraphServiceClient _graphClient;
        private readonly DeltaLinkService _deltaLinkService;
        private readonly IMapper _mapper;
        private readonly ResiliencePipeline _pipeline;
        private readonly IFusionCache _fusionCache;
        private readonly MicrosoftAppOptions _options;
        private readonly ILogger<UserSyncService> _logger;

        private static readonly Guid Admin = new Guid("746a6c20-c038-4635-91ff-d9def6d29b16");
        private static readonly Guid User = new Guid("0cbb5633-864d-40cc-934f-2c296d238f6f");


        public UserSyncService
        (
            WorkflowTimeDbContext dbContext,
            GraphServiceClient graphClient,
            DeltaLinkService deltaLinkService,
            IMapper mapper,
            ResiliencePipelineProvider<string> pipeline,
            IFusionCache fusionCache,
            IOptions<MicrosoftAppOptions> options,
            ILogger<UserSyncService> logger
        )
        {
            _dbContext = dbContext;
            _graphClient = graphClient;
            _deltaLinkService = deltaLinkService;
            _mapper = mapper;
            _pipeline = pipeline.GetPipeline("GraphUserSync");
            _fusionCache = fusionCache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task Sync()
        {
            var selectParameters = new[] { "id", "givenName", "surname", "mail" };

            string? deltaLink = await _deltaLinkService.GetDeltaLink();
            DeltaGetResponse? deltaGetResponse;

            if (string.IsNullOrEmpty(deltaLink))
            {
                deltaGetResponse = await _pipeline.ExecuteAsync(async token =>
                    await _graphClient.Users.Delta.GetAsDeltaGetResponseAsync(request =>
                    {
                        request.QueryParameters.Select = selectParameters;
                    }, cancellationToken: token)
                );
            }
            else
            {
                var deltaRequest = new DeltaRequestBuilder(deltaLink, _graphClient.RequestAdapter);
                deltaGetResponse = await _pipeline.ExecuteAsync(async token =>
                    await deltaRequest.GetAsDeltaGetResponseAsync(request =>
                    {
                        request.QueryParameters.Select = selectParameters;
                    }, cancellationToken: token)
                );
            }
            if (deltaGetResponse == null)
            {
                throw new InvalidOperationException("deltaGetResponse is null. Unable to proceed with iteration.");
            }

            var users = new List<User>();
            var pageIterator = PageIterator<User, DeltaGetResponse>.CreatePageIterator(_graphClient, deltaGetResponse, user =>
            {
                users.Add(user);
                return true;
            });

            await pageIterator.IterateAsync();
            var userValidator = new UserValidator();
            var userModelValidator = new UserModelValidator();

            foreach (var user in users)
            {
                if (!Guid.TryParse(user.Id, out var userGuid))
                    continue;

                //SoftDelete user if "@removed" is in AdditionalData
                if (user.AdditionalData != null && user.AdditionalData.ContainsKey("@removed"))
                {

                    var userToDelete = await _dbContext.Users.FindAsync(userGuid);
                    if (userToDelete != null)
                    {
                        userToDelete.IsDeleted = true;
                        _dbContext.Users.Update(userToDelete);
                    }
                    continue;
                }

                var validationResult = userValidator.Validate(user);
                if (!validationResult.IsValid)
                    continue;

                var newOrModUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
                if (newOrModUser == null)
                {
                    var newUser = _mapper.Map<UserModel>(user);

                    var modelValidation = userModelValidator.Validate(newUser);
                    if (!modelValidation.IsValid)
                        continue;

                    await _dbContext.Users.AddAsync(newUser);
                }
                else
                {
                    _mapper.Map(user, newOrModUser);
                    var modelValidation = userModelValidator.Validate(newOrModUser);
                    if (!modelValidation.IsValid)
                        continue;
                    _dbContext.Users.Update(newOrModUser);
                }
            }

            if (!string.IsNullOrEmpty(deltaGetResponse.OdataNextLink))
            {
                await _deltaLinkService.SaveDeltaLink(deltaGetResponse.OdataNextLink);
            }
            await _dbContext.SaveChangesAsync();

            await SyncUsersRoles();

            await _fusionCache.RemoveByTagAsync("Users");
        }

        private async Task SyncUsersRoles()
        {
            try { 
                var users = await _dbContext.Users.Where(u => !u.IsDeleted)
                    .ToListAsync();
                var sp = await _graphClient.ServicePrincipals
                .GetAsync(req =>
                {
                    req.QueryParameters.Filter = $"appId eq '{_options.AppId}'";
                });

                var resourceId = sp.Value.FirstOrDefault()?.Id;

                foreach (var user in users)
                {
                    var appRoleAssignments = await _graphClient.Users[user.Id.ToString()]
                        .AppRoleAssignments
                        .GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Filter = $"resourceId eq {resourceId}"; ;
                            requestConfiguration.QueryParameters.Select = new[] { "appRoleId"};
                        });

                    if (appRoleAssignments?.Value != null && appRoleAssignments.Value.Count > 0)
                    {
                        if (appRoleAssignments?.Value != null && appRoleAssignments.Value.Count > 0)
                        {
                            var appRoleId = appRoleAssignments.Value.FirstOrDefault()?.AppRoleId;
                            if (appRoleId == Admin)
                                user.Role = UserRole.Admin;
                            else if (appRoleId == User)
                                user.Role = UserRole.User;
                            else
                                user.Role = UserRole.None;
                        }
                    }
                }

                _dbContext.UpdateRange(users);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SyncUsersRoles.");
            }
        }
    }
}

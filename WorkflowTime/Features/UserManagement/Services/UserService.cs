using WorkflowTime.Database;
using Microsoft.EntityFrameworkCore;
using WorkflowTime.Features.UserManagement.Dtos;
using WorkflowTime.Features.UserManagement.Queries;
using AutoMapper.QueryableExtensions;
using AutoMapper;
using FluentValidation;
using WorkflowTime.Exceptions;
using ZiggyCreatures.Caching.Fusion;

namespace WorkflowTime.Features.UserManagement.Services
{
    public class UserService : IUserService
    {

        private readonly WorkflowTimeDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFusionCache _cache;
        public UserService
        (
            WorkflowTimeDbContext dbContext, 
            IMapper mapper,
            ICurrentUserService currentUserService,
            IFusionCache cache
        )
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _cache = cache;
        }
        public async Task<GetMeDto> GetMe()
        {
            var userId = _currentUserService.UserId;
            var cacheKey = $"User_{userId}";

            var userCacheDto = await _cache.GetOrSetAsync<UserCacheDto>(cacheKey, async (ctx, _) =>
            {
                ctx.Tags = ["Users"];
                var user = await _dbContext.Users.FindAsync(userId)
                    ?? throw new NotFoundException("User not found");
                return _mapper.Map<UserCacheDto>(user);
            }, options => options.SetDuration(TimeSpan.FromDays(1)));

            userCacheDto.Role = _currentUserService.Roles.FirstOrDefault();
            return _mapper.Map<GetMeDto>(userCacheDto);
        }
        public async Task<List<GetSearchedUserDto>> Search(UserSearchQueryParameters parameters)
        {
            return await _dbContext.Users
                .Where(u => (!u.IsDeleted) && (
                            (u.Surname != null && u.Surname.Contains(parameters.SearchingPhrase)) ||
                            (u.GivenName != null && u.GivenName.Contains(parameters.SearchingPhrase)) ||
                            (u.Email != null && u.Email.Contains(parameters.SearchingPhrase))))
                .Take(parameters.ResponseLimit)
                .ProjectTo<GetSearchedUserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<List<GetUsersByGuidDto>> GetUsersByGuids(List<Guid> userIds)
        {
            if (userIds == null || !(userIds.Count > 0))
                return [];

            var results = new List<GetUsersByGuidDto>();
            foreach (var userId in userIds)
            {
                var cacheKey = $"User_{userId}";
                var userDto = await _cache.GetOrSetAsync<UserCacheDto>(cacheKey, async (ctx, _) =>
                {
                    ctx.Tags = ["Users"];
                    var user = await _dbContext.Users
                        .Where(u => u.Id == userId)
                        .ProjectTo<UserCacheDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync(cancellationToken: _);
                    return user ?? throw new NotFoundException($"User {userId} not found");
                }, options => options.SetDuration(TimeSpan.FromDays(1)));
                    
                results.Add(_mapper.Map<GetUsersByGuidDto>(userDto));
            }

            return results;
        }
    }
}

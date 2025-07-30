using Microsoft.Graph.Models;
using System.Security.Claims;
using WorkflowTime.Features.UserManagement.Dtos;
using WorkflowTime.Features.UserManagement.Queries;

namespace WorkflowTime.Features.UserManagement.Services
{
    public interface IUserService
    {
        Task<GetMeDto> GetMe();
        Task<List<GetSearchedUserDto>> Search(UserSearchQueryParameters parameters);
        Task<List<GetUsersByGuidDto>> GetUsersByGuids(List<Guid> userIds);

    }
}

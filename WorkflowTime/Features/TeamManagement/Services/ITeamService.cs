using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.TeamManagement.Dtos;
using WorkflowTime.Features.TeamManagement.Queries;

namespace WorkflowTime.Features.TeamManagement.Services
{
    public interface ITeamService
    {
        Task<GetTeamDto> GetTeam(int id);
        Task<GetTeamDto> CreateTeam(CreateTeamDto parameters);
        Task<List<GetSearchedTeamDto>> Search(TeamSearchQueryParameters parameters);
        Task<List<UsersInTeamDto>> UsersInTeam(int teamId);
    }
}

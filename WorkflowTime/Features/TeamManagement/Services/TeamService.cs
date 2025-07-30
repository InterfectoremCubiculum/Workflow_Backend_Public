using Microsoft.EntityFrameworkCore;
using WorkflowTime.Database;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.TeamManagement.Dtos;
using WorkflowTime.Features.TeamManagement.Queries;
using Team = WorkflowTime.Features.TeamManagement.Models.Team;

namespace WorkflowTime.Features.TeamManagement.Services
{
    public class TeamService : ITeamService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        public TeamService(WorkflowTimeDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<GetTeamDto> CreateTeam(CreateTeamDto parameters)
        {
            Team team = new() { Name = parameters.Name};

            await _dbContext.Teams.AddAsync(team);
            await _dbContext.SaveChangesAsync();

            return new GetTeamDto
            {
                Id = team.Id,
                Name = team.Name
            };
        }

        public async Task<GetTeamDto> GetTeam(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid team ID provided.");


            var team = await _dbContext.Teams.FindAsync(id) ?? throw new NotFoundException($"Team with ID {id} not found.");

            return new GetTeamDto
            {
                Id = team.Id,
                Name = team.Name
            };
        }
        public async Task<List<GetSearchedTeamDto>> Search(TeamSearchQueryParameters parameters)
        {
            var searchPhrase = parameters.SearchingPhrase?.ToLower() ?? "";

            var teams = await _dbContext.Teams
                .Where(t =>
                    !t.IsDeleted
                    && t.Name.ToLower().Contains(searchPhrase))
                .Select(t => new GetSearchedTeamDto { Id = t.Id, Name = t.Name })
                .Take(parameters.ResponseLimit)
                .ToListAsync();

            return teams;
        }

        public async Task<List<UsersInTeamDto>> UsersInTeam(int teamId)
        {
            if (teamId <= 0)
                throw new BadRequestException("Invalid team ID provided.");

            var usersInTeam = await _dbContext.Users
                .Where(u => u.TeamId == teamId && !u.IsDeleted)
                .Select(u => new UsersInTeamDto
                {
                    UserId = u.Id,
                    Name = u.GivenName,
                    Surname = u.Surname,
                    Email = u.Email ?? ""
                })
                .ToListAsync();

            return usersInTeam;
        }
    }
}

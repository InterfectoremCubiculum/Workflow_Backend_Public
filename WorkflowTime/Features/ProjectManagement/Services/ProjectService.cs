using Microsoft.EntityFrameworkCore;
using WorkflowTime.Database;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.ProjectManagement.Dtos;
using WorkflowTime.Features.ProjectManagement.Models;
using WorkflowTime.Features.ProjectManagement.Queries;

namespace WorkflowTime.Features.ProjectManagement.Services
{
    public class ProjectService : IProjectService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        public ProjectService(WorkflowTimeDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<GetProjectDto> CreateProject(CreateProjectDto projectDto)
        {

            Project project = new() { Name = projectDto.Name };
            await _dbContext.Projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();

            return new GetProjectDto { Id = project.Id, Name = project.Name };
        }

        public async Task<GetProjectDto> GetProject(int id)
        {
            if (id <= 0)
                throw new NotFoundException($"Project with ID {id} not found.");

            var project = await _dbContext.Projects.FindAsync(id);
            if (project is null)
                throw new NotFoundException($"Project with ID {id} not found.");

            return new GetProjectDto { Id = project.Id, Name = project.Name };
        }

        public async Task<List<GetSearchedProjectDto>> Search(ProjectSearchQueryParameters parameters)
        {
            if (String.IsNullOrEmpty(parameters.SearchingPhrase))
                return new List<GetSearchedProjectDto>();

            var projects = await _dbContext.Projects
                .Where(p =>
                !p.IsDeleted
                && p.Name.ToLower().Contains(parameters.SearchingPhrase))
                .Select(p => new GetSearchedProjectDto { Id = p.Id, Name = p.Name })
                .Take(parameters.ResponseLimit)
                .ToListAsync();

            return projects;
        }

        public async Task<List<UsersInProjectDto>> UsersInProject(int projectId)
        {
            if (projectId <= 0)
                throw new NotFoundException($"Project with ID {projectId} not found.");

            var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists)
                throw new NotFoundException($"Project with ID {projectId} not found.");


            var usersInProject = await _dbContext.Users
                .Where(u => u.ProjectId == projectId && !u.IsDeleted)
                .Select(u => new UsersInProjectDto
                {
                    UserId = u.Id,
                    Name = u.GivenName,
                    Surname = u.Surname,
                    Email = u.Email ?? ""
                })
                .ToListAsync();

            return usersInProject;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using WorkflowTime.Features.ProjectManagement.Dtos;
using WorkflowTime.Features.ProjectManagement.Queries;

namespace WorkflowTime.Features.ProjectManagement.Services
{
    public interface IProjectService
    {
        public Task<GetProjectDto> GetProject(int id);
        public Task<GetProjectDto> CreateProject(CreateProjectDto projectDto);
        public Task<List<GetSearchedProjectDto>> Search(ProjectSearchQueryParameters parameters);
        public Task<List<UsersInProjectDto>> UsersInProject(int projectId);
    }
}

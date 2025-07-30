using Microsoft.EntityFrameworkCore;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Features.Summary.Dtos;
using WorkflowTime.Features.Summary.Queries;

namespace WorkflowTime.Features.Summary.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        public SummaryService(WorkflowTimeDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<List<UserWorkSummaryDto>> GetProjectsWorkSummary(ProjectsWorkSummaryQueriesParameters parameters)
        {
            var projectUserMap = await _dbContext.Projects
                .Where(p => parameters.ProjectIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    UserIds = p.Users
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id)
                        .ToList()
                })
                .ToListAsync();

            var allUserIds = projectUserMap.SelectMany(p => p.UserIds).ToList();

            var userWorkSummaries = await GetUserWorkSummariesForUsers(new UserWorkSummaryQueriesParameters
            {
                UserIds = allUserIds,
                PeriodStart = parameters.PeriodStart,
                PeriodEnd = parameters.PeriodEnd
            });

            return userWorkSummaries;
        }

        public async Task<List<UserWorkSummaryDto>> GetTeamsWorkSummary(TeamsWorkSummaryQueriesParameters parameters)
        {
            var teamUserMap = await _dbContext.Teams
                .Where(t => parameters.TeamIds.Contains(t.Id) && !t.IsDeleted)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    UserIds = t.Users
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id)
                        .ToList()
                })
                .ToListAsync();

            var allUserIds = teamUserMap.SelectMany(t => t.UserIds).ToList();

            var userWorkSummaries = await GetUserWorkSummariesForUsers(new UserWorkSummaryQueriesParameters
            {
                UserIds = allUserIds,
                PeriodStart = parameters.PeriodStart,
                PeriodEnd = parameters.PeriodEnd
            });

            return userWorkSummaries;
        }

        public async Task<List<UserWorkSummaryDto>> GetWorkSummariesForUsers(UserWorkSummaryQueriesParameters parameters)
        {
            var summaries = await GetUserWorkSummariesForUsers(parameters);
            return summaries;
        }

        public async Task<List<UserWorkSummaryDto>> GetUserWorkSummariesForUsers(UserWorkSummaryQueriesParameters parameters)
        {
            var userIds = parameters.UserIds;
            var periodStart = parameters.PeriodStart.ToDateTime(TimeOnly.MinValue);
            var periodEnd = parameters.PeriodEnd.ToDateTime(TimeOnly.MaxValue);

            var usersQuery = _dbContext.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted);

            var timeSegmentsQuery = _dbContext.TimeSegments
                .Where(ts => userIds.Contains(ts.UserId)
                    && ts.StartTime >= periodStart
                    && ts.EndTime <= periodEnd
                    && !ts.IsDeleted);

            var dayOffQuery = _dbContext.DayOffRequests
                .Where(dor => userIds.Contains(dor.UserId)
                    && dor.StartDate >= parameters.PeriodStart
                    && dor.EndDate <= parameters.PeriodEnd
                    && !dor.IsDeleted);

            var projectsQuery = _dbContext.Projects
                .Where(p => p.Users.Any(u => userIds.Contains(u.Id)) && !p.IsDeleted);

            var summaries = await usersQuery
                .Select(user => new UserWorkSummaryDto
                {
                    UserId = user.Id,
                    Name = user.GivenName,
                    Email = user.Email,
                    Surname = user.Surname,
                    ProjectName = user.Project != null ? user.Project.Name : string.Empty,
                    TeamName = user.Team != null ? user.Team.Name : string.Empty,
                    TotalWorkHours = TimeSpan.FromSeconds(
                        timeSegmentsQuery
                            .Where(ts => ts.UserId == user.Id && ts.TimeSegmentType == TimeSegmentType.Work)
                            .Sum(ts => ts.DurationInSeconds ?? 0)),

                    TotalBreakHours = TimeSpan.FromSeconds(
                        timeSegmentsQuery
                            .Where(ts => ts.UserId == user.Id && ts.TimeSegmentType == TimeSegmentType.Break)
                            .Sum(ts => ts.DurationInSeconds ?? 0)),

                    TotalDaysWorked = timeSegmentsQuery
                        .Where(ts => ts.UserId == user.Id && ts.TimeSegmentType == TimeSegmentType.Work)
                        .Select(ts => ts.StartTime.Date)
                        .Distinct()
                        .Count(),

                    TotalDaysOff = dayOffQuery
                        .Where(dor => dor.UserId == user.Id)
                        .Count()
                })
                .ToListAsync();

            return summaries;
        }
    }
}

using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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
            return await GetUserWorkSummariesForUsers(new UserWorkSummaryQueriesParameters
            {
                UserIds = await MapProjetcsUsersIds(parameters.ProjectIds),
                PeriodStart = parameters.PeriodStart,
                PeriodEnd = parameters.PeriodEnd
            });
        }

        public async Task<List<UserWorkSummaryDayByDayDto>> GetProjectsWorkSummaryDayByDay(ProjectsWorkSummaryQueriesParameters parameters)
        {
            return await GetUserWorkSummariesDayByDayForUsers(new UserWorkSummaryQueriesParameters
            {
                UserIds = await MapProjetcsUsersIds(parameters.ProjectIds),
                PeriodStart = parameters.PeriodStart,
                PeriodEnd = parameters.PeriodEnd
            });

        }
        private async Task<List<Guid>> MapProjetcsUsersIds(List<int> projectsIds)
        {
            List<Guid> userIds;

            if (projectsIds.Count == 0)
            {
                userIds = await _dbContext.Projects
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.Users
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id))
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                userIds = await _dbContext.Projects
                    .Where(p => projectsIds.Contains(p.Id) && !p.IsDeleted)
                    .SelectMany(p => p.Users
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id))
                    .Distinct()
                    .ToListAsync();
            }

            return userIds;
        }
        public async Task<List<UserWorkSummaryDto>> GetTeamsWorkSummary(TeamsWorkSummaryQueriesParameters parameters)
        {
            return await GetUserWorkSummariesForUsers(new UserWorkSummaryQueriesParameters
            {
                UserIds = await MapTeamsUsersIds(parameters.TeamIds),
                PeriodStart = parameters.PeriodStart,
                PeriodEnd = parameters.PeriodEnd
            });
        }
        public async Task<List<UserWorkSummaryDayByDayDto>> GetTeamsWorkSummarDayByDay(TeamsWorkSummaryQueriesParameters parameters)
        {
            return await GetUserWorkSummariesDayByDayForUsers(new UserWorkSummaryQueriesParameters
            {
                UserIds = await MapTeamsUsersIds(parameters.TeamIds),
                PeriodStart = parameters.PeriodStart,
                PeriodEnd = parameters.PeriodEnd
            });
        }
        private async Task<List<Guid>> MapTeamsUsersIds(List<int> teamIds)
        {
            List<Guid> userIds;

            if (teamIds.Count == 0)
            {
                userIds = await _dbContext.Projects
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.Users
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id))
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                userIds = await _dbContext.Projects
                    .Where(p => teamIds.Contains(p.Id) && !p.IsDeleted)
                    .SelectMany(p => p.Users
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id))
                    .Distinct()
                    .ToListAsync();
            }

            return userIds;
        }
        public async Task<List<UserWorkSummaryDayByDayDto>> GetWorkSummariesDayByDayForUsers(UserWorkSummaryQueriesParameters parameters)
        {
            if(parameters.UserIds.Count == 0)
            {
                parameters.UserIds = await _dbContext.Users
                    .Where(u => !u.IsDeleted)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            var summaries = await GetUserWorkSummariesDayByDayForUsers(parameters);
            return summaries;
        }

        private async Task<List<UserWorkSummaryDayByDayDto>> GetUserWorkSummariesDayByDayForUsers(UserWorkSummaryQueriesParameters parameters)
        {
            var userIds = parameters.UserIds;
            var periodStart = parameters.PeriodStart.ToDateTime(TimeOnly.MinValue);
            var periodEnd = parameters.PeriodEnd.ToDateTime(TimeOnly.MaxValue);

            var timeSegments = await _dbContext.TimeSegments
                .Where(ts => userIds.Contains(ts.UserId)
                    && ts.StartTime >= periodStart
                    && ts.EndTime <= periodEnd
                    && !ts.IsDeleted)
                .ToListAsync();

            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
                .Include(u => u.Project)
                .Include(u => u.Team)
                .ToListAsync();

            var allDates = Enumerable.Range(0, (parameters.PeriodEnd.DayNumber - parameters.PeriodStart.DayNumber) + 1)
                .Select(offset => parameters.PeriodStart.AddDays(offset))
                .ToList();

            var result = new List<UserWorkSummaryDayByDayDto>();

            foreach (var user in users)
            {
                foreach (var date in allDates)
                {
                    var dayStart = date.ToDateTime(TimeOnly.MinValue);
                    var dayEnd = date.ToDateTime(TimeOnly.MaxValue);

                    var userDaySegments = timeSegments
                        .Where(ts => ts.UserId == user.Id && ts.StartTime >= dayStart && ts.EndTime <= dayEnd)
                        .ToList();

                    var workMinutes = userDaySegments
                        .Where(ts => ts.TimeSegmentType == TimeSegmentType.Work)
                        .Sum(ts => ts.DurationInSeconds ?? 0) / 60;

                    var breakMinutes = userDaySegments
                        .Where(ts => ts.TimeSegmentType == TimeSegmentType.Break)
                        .Sum(ts => ts.DurationInSeconds ?? 0) / 60;

                    result.Add(new UserWorkSummaryDayByDayDto
                    {
                        UserId = user.Id,
                        Name = user.GivenName,
                        Surname = user.Surname,
                        Email = user.Email,
                        Date = date,
                        ProjectName = user.Project != null ? user.Project.Name : string.Empty,
                        TeamName = user.Team != null ? user.Team.Name : string.Empty,
                        WorkMinutes = workMinutes,
                        BreakMinutes = breakMinutes
                    });
                }
            }

            return result;
        }

        public async Task<List<UserWorkSummaryDto>> GetWorkSummariesForUsers(UserWorkSummaryQueriesParameters parameters)
        {
            if (parameters.UserIds.Count == 0)
            {
                parameters.UserIds = await _dbContext.Users
                    .Where(u => !u.IsDeleted)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            var summaries = await GetUserWorkSummariesForUsers(parameters);
            return summaries;
        }

        private async Task<List<UserWorkSummaryDto>> GetUserWorkSummariesForUsers(UserWorkSummaryQueriesParameters parameters)
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

        public async Task<FileResult> ExportToCSV(UserWorkSummaryQueriesParameters parameters)
        {
            IEnumerable<object> summaries;
            if (parameters.UserIds.Count == 0)
            {
                parameters.UserIds = await _dbContext.Users
                    .Where(u => !u.IsDeleted)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            if (parameters.IsDayByDay)
                summaries = await GetUserWorkSummariesDayByDayForUsers(parameters);
            else
                summaries = await GetUserWorkSummariesForUsers(parameters);

            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(summaries);
            }
            memoryStream.Position = 0;
            var fileName = $"Summary_From_{parameters.PeriodStart:yyyy-MM-dd}_To_{parameters.PeriodEnd:yyyy-MM-dd}.csv";
            return new FileStreamResult(memoryStream, "text/csv")
            {
                FileDownloadName = fileName
            };
        }
    }
}

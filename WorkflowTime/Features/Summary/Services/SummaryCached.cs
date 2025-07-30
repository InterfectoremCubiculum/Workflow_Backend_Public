using AutoMapper;
using System.Text;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.Summary.Dtos;
using ZiggyCreatures.Caching.Fusion;

namespace WorkflowTime.Features.Summary.Services
{
    public class SummaryCached : ISummaryCached
    {
        private readonly IFusionCache _cache;
        private readonly IMapper _mapper;
        public SummaryCached(IFusionCache cache, IMapper mapper)
        {
            _cache = cache;
            _mapper = mapper;
        }
        public async Task<byte[]> ExportCsv(Guid token)
        {
            var data = await _cache.GetOrDefaultAsync<List<WorkSummaryCSV>>(token.ToString());

            if (data == null || data.Count == 0)
                throw new NotFoundException("No data found for the provided token or the data is empty.");

            var csvBuilder = new StringBuilder();

            csvBuilder.AppendLine("Name,Surname,Email,ProjectName,TeamName,TotalWorkHours,TotalBreakHours,TotalDaysWorked,TotalDaysOff");

            foreach (var row in data)
            {
                csvBuilder.AppendLine(string.Format(
                    "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                    row.Name,
                    row.Surname,
                    row.Email,
                    row.ProjectName ?? "",
                    row.TeamName ?? "",
                    $"{(int)row.TotalWorkHours.TotalDays:00}d {row.TotalWorkHours.Hours:00}:{row.TotalWorkHours.Minutes:00}:{row.TotalWorkHours.Seconds:00}",
                    $"{(int)row.TotalBreakHours.TotalDays:00}d {row.TotalBreakHours.Hours:00}:{row.TotalBreakHours.Minutes:00}:{row.TotalBreakHours.Seconds:00}",
                    row.TotalDaysWorked,
                    row.TotalDaysOff
                ));
            }

            return Encoding.UTF8.GetBytes(csvBuilder.ToString());
        }

        public async Task<Guid> CacheUserWork(List<UserWorkSummaryDto> userWorkSummaries)
        {
            var token = Guid.NewGuid();
            var mapped = _mapper.Map<List<WorkSummaryCSV>>(userWorkSummaries);
            await _cache.SetAsync(token.ToString(), mapped, TimeSpan.FromMinutes(10));


            return token;
        }
    }
}

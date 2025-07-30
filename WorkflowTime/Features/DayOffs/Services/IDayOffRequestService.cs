using WorkflowTime.Enums;
using WorkflowTime.Features.DayOffs.Dtos;
using WorkflowTime.Features.DayOffs.Queries;
using WorkflowTime.Utillity;

namespace WorkflowTime.Features.DayOffs.Services
{
    public interface IDayOffRequestService
    {
        public Task<PagedResponse<GetDayOffRequestDto>> GetDayOffRequests(DayOffsRequestQueryParameters parameters);
        public Task<List<GetCalendarDayOff>> GetCalendarDayOff(CalendarDayOffsRequestQueryParameters parameters);
        public Task<GetCreatedDayOffRequestDto> CreateDayOffRequest(CreateDayOffRequestDto dayOffRequest, Guid userId);
        public Task UpdateDayOffRequestStatus(int id, DayOffRequestStatus status);
        public Task UpdateDayOffRequest(UpdateDayOffRequestDto dayOffRequest);
        public Task DeleteDayOffRequest(int id);
        public Task UpdateDayOffState();
    }
}



using WorkflowTime.Enums;
using WorkflowTime.Features.WorkLog.Dtos;
using WorkflowTime.Features.WorkLog.Models;
using WorkflowTime.Features.WorkLog.Queries;
using WorkflowTime.Migrations;
using WorkflowTime.Queries;

namespace WorkflowTime.Features.WorkLog.Services
{
    public interface IWorkLogService
    {
        public Task<TimeSegment> StartBreak(Guid userId, WorkflowParameters? parameters);
        public Task<TimeSegment> ResumeWork(Guid userId, WorkflowParameters? parameters);
        public Task<TimeSegment> EndWork(Guid userId, WorkflowParameters? parameters);
        public Task<TimeSegment> StartWork(Guid userId, WorkflowParameters? parameters);
        public Task<List<UsersTimeSegmentDto>> GetUserWorkLog (UserWorkLogQueryParameters parameters);
        public Task<List<UsersTimelineWorklogDto>> UsersTimelineWorklog(UserTimelineWorklogQueryParameters parameters);
        public Task<TeamInTimeLineDto> TeamTimelineWorklog(GroupTimelineWorklogQueryParameters parameters);
        public Task<ProjectInTimeLineDto> ProjectTimelineWorklog(GroupTimelineWorklogQueryParameters parameters);
        public Task<TimeSegmentType?> GetUserActiveTimeSegmentType(Guid userId);
        public Task<WidgetSyncDto?> WidgetSync(Guid userId);
        public Task<TimeSegment> EditWorklog(Guid userId, WorkflowParameters parameters);
        public Task<List<TimeSegment>> StartBreakForUsers(List<Guid> userIds, DateTime? startTime);
        public Task ResolveActionRequest(int TimeSegmentId, ResolveActionCommand action);

    }
}

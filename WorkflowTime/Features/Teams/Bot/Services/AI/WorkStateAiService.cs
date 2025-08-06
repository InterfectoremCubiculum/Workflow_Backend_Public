using AutoMapper;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WorkflowTime.Enums;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.WorkLog.Dtos;
using WorkflowTime.Features.WorkLog.Services;

namespace WorkflowTime.Features.Teams.Bot.Services.AI
{
    public class WorkStateAiService
    {
        private readonly IWorkLogService _workLogService;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkStateAiService> _logger;
        private readonly INotificationService _hub;

        public WorkStateAiService
        (
            IWorkLogService workLogService,
            IMapper mapper,
            ILogger<WorkStateAiService> logger,
            INotificationService hub
        )
        {
            _workLogService = workLogService;
            _mapper = mapper;
            _logger = logger;
            _hub = hub;
        }
        public async Task<string> HandleAiResponse(Guid userId, WorkflowActionResult request, DateTimeOffset? utc)
        {

            var parameters = _mapper.Map<WorkflowActionResult, WorkflowParameters>(request);
            var TimeDiff = utc?.Offset;
            //if (TimeDiff is not null)
            //{
            //    if (parameters.StartTime.HasValue)
            //    {
            //        var local = new DateTimeOffset(parameters.StartTime.Value, (TimeSpan)TimeDiff);
            //        parameters.StartTime = local.UtcDateTime;
            //    }

            //    if (parameters.EndTime.HasValue)
            //    {
            //        var local = new DateTimeOffset(parameters.EndTime.Value, (TimeSpan)TimeDiff);
            //        parameters.EndTime = local.UtcDateTime;
            //    }
            //}


            string responseMessage;
            try
            {
                switch (request.Intent)
                {
                    case "StartWork":
                        var startTimeSegment = await _workLogService.StartWork(userId, parameters);
                        var userTime = ToUserOffset(startTimeSegment.StartTime, (TimeSpan)TimeDiff!);
                        responseMessage = $"Work session started. {userTime:yyyy-MM-dd HH:mm:ss}";
                        await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Work.ToString());
                        break;

                    case "EndWork":
                        var endTimeSegment = await _workLogService.EndWork(userId, parameters);
                        var userEnd = endTimeSegment.EndTime.HasValue
                            ? ToUserOffset(endTimeSegment.EndTime.Value, (TimeSpan)TimeDiff!)
                            : default;
                        responseMessage = $"Work session ended. {userEnd}";
                        await _hub.NotifyWorkStateChange(userId, null);
                        break;

                    case "StartBreak":
                        var breakTimeSegment = await _workLogService.StartBreak(userId, parameters);
                        var userBreakStart = ToUserOffset(breakTimeSegment.StartTime, (TimeSpan)TimeDiff!);
                        responseMessage = $"Break started at: {userBreakStart}";
                        await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Break.ToString());
                        break;

                    case "ResumeWork":
                        var resumeTimeSegment = await _workLogService.ResumeWork(userId, parameters);
                        var userResume = ToUserOffset(resumeTimeSegment.StartTime, (TimeSpan)TimeDiff!);
                        responseMessage = $"Resumed work session: {userResume}";
                        await _hub.NotifyWorkStateChange(userId, TimeSegmentType.Work.ToString());
                        break;

                    case "EditLog":
                        var editedLog = await _workLogService.EditWorklog(userId, parameters);
                        responseMessage = "Work log edited successfully." +
                            $"\nStart time: {ToUserOffset(editedLog.StartTime, (TimeSpan)TimeDiff!)}" +
                            $"\nEnd time: {(editedLog.EndTime is not null ? ToUserOffset((DateTime)editedLog.EndTime, (TimeSpan)TimeDiff) : "") }" +
                            $"\nTime segment type: {editedLog.TimeSegmentType}";
                        break;

                    default:
                        return "Unknown command.";

                }
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex, "Conflict error while processing AI response for user {Request}", JsonSerializer.Serialize(request));
                responseMessage = $"⚠️ {ex.Message}";
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, "Validation error while processing AI response for user {Request}", JsonSerializer.Serialize(request));
                responseMessage = $"❌ Validation error: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Conflict error while processing AI response for user {Request}", JsonSerializer.Serialize(request));
                responseMessage = "❌ Something went wrong while processing your request. Please try again later.";
            }
            return responseMessage;
        }
        private static DateTime ToUserOffset(DateTime utcTime, TimeSpan offset)
        {
            var localTime = DateTime.SpecifyKind(utcTime + offset, DateTimeKind.Unspecified);
            return localTime;
        }
    }
}

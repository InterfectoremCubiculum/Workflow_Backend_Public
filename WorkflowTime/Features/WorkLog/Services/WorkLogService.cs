using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using System.ComponentModel.DataAnnotations;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.AdminPanel.Services;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.Notifications.Services;
using WorkflowTime.Features.WorkLog.Dtos;
using WorkflowTime.Features.WorkLog.Models;
using WorkflowTime.Features.WorkLog.Queries;
using WorkflowTime.Queries;
namespace WorkflowTime.Features.WorkLog.Services
{
    public class WorkLogService : IWorkLogService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly int _maxReverseRegistrationTime;
        private readonly int _maxReverseregistrationTimeLogged;
        private readonly INotificationService _notificationService;
        private readonly INotificationTeamsService _notificationTeamsService;

        public WorkLogService
        (
            IMapper mapper, WorkflowTimeDbContext dbContext, 
            ISettingsService settingsService,
            INotificationService notificationService,
            INotificationTeamsService notificationTeamsService
        )
        {
            _mapper = mapper;
            _dbContext = dbContext;
            _maxReverseRegistrationTime = settingsService.GetSettingByKey<int>("max_reverse_registration_time");
            _maxReverseregistrationTimeLogged = settingsService.GetSettingByKey<int>("max_reverse_registration_time_logged");
            _notificationService = notificationService;
            _notificationTeamsService = notificationTeamsService;
        }
        public async Task<TimeSegment> StartWork(Guid userId, WorkflowParameters? parameters)
        {
            var lastSegment = await GetLastSegment(userId);
            if (lastSegment != null && lastSegment.EndTime == null)
            {
                throw new ConflictException($"There is active segment of type {lastSegment.TimeSegmentType}. First end this segment to Start Work.");
            }
            var now = DateTime.UtcNow;
            var proposedStartTime = parameters?.StartTime ?? now;
            var proposedEndTime = parameters?.EndTime;

            if (lastSegment != null && proposedStartTime < lastSegment.EndTime)
            {
                throw new ValidationException($"Proposed segment overlaps with the last segment. Start Time ({proposedStartTime:yyyy-MM-dd HH:mm:ss}) must be after last segment's End Time ({lastSegment.EndTime:yyyy-MM-dd HH:mm:ss}).");
            }

            if (proposedStartTime < now.AddMinutes(-_maxReverseRegistrationTime))
            {
                throw new ValidationException($"Start Time ({proposedStartTime:yyyy-MM-dd HH:mm:ss}) cannot be earlier than {_maxReverseRegistrationTime} minutes before now ({now:yyyy-MM-dd HH:mm:ss}).");
            }

            var newSegment = new TimeSegment
            {
                UserId = userId,
                TimeSegmentType = TimeSegmentType.Work,
                StartTime = proposedStartTime,
                EndTime = parameters?.EndTime ?? null
            };
            newSegment = await HandleAdminRequestNeeded(proposedStartTime, newSegment, userId);
            _dbContext.TimeSegments.Add(newSegment);
            await _dbContext.SaveChangesAsync();
            return newSegment;
        }

        public async Task<TimeSegment> EndWork(Guid userId, WorkflowParameters? parameters)
        {
            var activeSegment = await GetActiveSegment(userId);
            if (activeSegment == null)
            {
                throw new ConflictException("You need to Start Work to End Work.");
            }

            var now = DateTime.UtcNow;
            var proposedEndTime = parameters?.EndTime ?? now;

            if (proposedEndTime < activeSegment.StartTime)
            {
                throw new ValidationException("End Time cannot be earlier than StartTime.");
            }

            //if (proposedEndTime > now.AddMinutes(_maxReverseRegistrationTime))

            if (proposedEndTime > now)
            {
                throw new ValidationException($"End Time ({proposedEndTime:yyyy-MM-dd HH:mm:ss}) cannot be more than ahead of current time ({now:yyyy-MM-dd HH:mm:ss}).");
            }

            activeSegment.EndTime = proposedEndTime;
            _dbContext.TimeSegments.Update(activeSegment);
            await _dbContext.SaveChangesAsync();
            return activeSegment;
        }


        public async Task<TimeSegment> StartBreak(Guid userId, WorkflowParameters? parameters)
        {
            var activeSegment = await GetActiveSegment(userId);
            if (activeSegment == null)
            {
                throw new ConflictException("There is no active work segment to start a break.");
            }
            else if (activeSegment.TimeSegmentType == TimeSegmentType.Break)
            {
                throw new ConflictException("Break already in progress.");
            }

            var now = DateTime.UtcNow;
            var proposedStartTime = parameters?.StartTime ?? now;

            if (proposedStartTime < now.AddMinutes(-_maxReverseRegistrationTime))
            {
                throw new ValidationException($"Start Time of break cannot be earlier than {_maxReverseRegistrationTime} minutes before the current time.");
            }

            if (proposedStartTime < activeSegment.StartTime)
            {
                throw new ValidationException("Start Time of break cannot be earlier than start of the current work segment.");
            }

            activeSegment.EndTime = proposedStartTime;
            _dbContext.TimeSegments.Update(activeSegment);

            var newSegment = new TimeSegment
            {
                UserId = userId,
                TimeSegmentType = TimeSegmentType.Break,
                StartTime = proposedStartTime,
                EndTime = parameters?.EndTime ?? null
            };
            newSegment = await HandleAdminRequestNeeded(proposedStartTime, newSegment, userId);

            _dbContext.TimeSegments.Add(newSegment);
            await _dbContext.SaveChangesAsync();
            return newSegment;
        }

        public async Task<TimeSegment> ResumeWork(Guid userId, WorkflowParameters? parameters)
        {
            var activeSegment = await GetActiveSegment(userId);
            if (activeSegment == null)
            {
                throw new ConflictException("There is no active Work segment to resume.");
            }
            else if (activeSegment.TimeSegmentType != TimeSegmentType.Break)
            {
                throw new ConflictException("Cannot resume work when not on a break.");
            }

            var now = DateTime.UtcNow;
            var proposedStartTime = parameters?.StartTime ?? now;

            if (proposedStartTime < now.AddMinutes(-_maxReverseRegistrationTime))
            {
                throw new ValidationException($"Start Time cannot be earlier than {_maxReverseRegistrationTime} minutes before the current time.");
            }

            if (proposedStartTime < activeSegment.StartTime)
            {
                throw new ValidationException("Resume Start Time cannot be earlier than the break start time.");
            }

            activeSegment.EndTime = proposedStartTime;
            _dbContext.TimeSegments.Update(activeSegment);

            var newSegment = new TimeSegment
            {
                UserId = userId,
                TimeSegmentType = TimeSegmentType.Work,
                StartTime = proposedStartTime,
                EndTime = parameters?.EndTime ?? null
            };
            newSegment = await HandleAdminRequestNeeded(proposedStartTime, newSegment, userId);

            //if (newSegment.EndTime.HasValue && newSegment.EndTime > now.AddMinutes(-_maxReverseRegistrationTime))


            if (newSegment.EndTime.HasValue && newSegment.EndTime > now)
            {
                throw new ValidationException("End Time cannot be in the future.");
            }

            _dbContext.TimeSegments.Add(newSegment);
            await _dbContext.SaveChangesAsync();
            return newSegment;
        }

        public async Task<TimeSegment> EditWorklog(Guid userId, WorkflowParameters parameters)
        {
            var now = DateTime.UtcNow;

            TimeSegmentType? segmentType = null;
            if (!string.IsNullOrEmpty(parameters.Type) &&
                Enum.TryParse<TimeSegmentType>(parameters.Type, true, out var parsedType))
            {
                segmentType = parsedType;
            }

            var query = _dbContext.TimeSegments.Where(ts => ts.UserId == userId);
            if (segmentType.HasValue)
                query = query.Where(ts => ts.TimeSegmentType == segmentType);

            var lastSegment = await GetLastSegment(userId)
                ?? throw new NotFoundException("No matching time segment found for editing.");

            if (parameters.AddTime.HasValue)
            {
                if (lastSegment.EndTime.HasValue)
                    lastSegment.EndTime = lastSegment.EndTime.Value.Add(parameters.AddTime.Value);
                else
                    lastSegment.EndTime = lastSegment.StartTime.Add(parameters.AddTime.Value);
            }

            if (parameters.SubtractTime.HasValue)
            {
                if (lastSegment.EndTime.HasValue)
                    lastSegment.EndTime = lastSegment.EndTime.Value.Subtract(parameters.SubtractTime.Value);
                else
                    lastSegment.EndTime = lastSegment.StartTime.Subtract(parameters.SubtractTime.Value);
            }

            if (parameters.StartTime.HasValue)
            {
                var startTime = parameters.StartTime.Value;
                if (startTime < now.AddMinutes(-_maxReverseRegistrationTime) || startTime > now.AddMinutes(_maxReverseRegistrationTime))
                    throw new ValidationException($"StartTime out of allowed range. {startTime}");
                lastSegment.StartTime = startTime;
            }

            if (parameters.EndTime.HasValue)
            {
                var endTime = parameters.EndTime.Value;
                if (endTime  < now.AddMinutes(-_maxReverseRegistrationTime) || endTime > now.AddMinutes(_maxReverseRegistrationTime))
                    throw new ValidationException("End Time out of allowed range.");
                lastSegment.EndTime = endTime;
            }

            if (lastSegment.EndTime.HasValue && lastSegment.StartTime > lastSegment.EndTime)
                throw new ValidationException("Start Time cannot be after End Time.");

            if (segmentType.HasValue)
                lastSegment.TimeSegmentType = segmentType.Value;

            _dbContext.TimeSegments.Update(lastSegment);
            await _dbContext.SaveChangesAsync();

            return lastSegment;
        }


        public async Task<List<UsersTimeSegmentDto>> GetUserWorkLog(UserWorkLogQueryParameters parameters)
        {
            var query = _dbContext.TimeSegments.AsQueryable();

            query = query
                .Where(
                    ts => ts.UserId == parameters.UserId
                    && ts.IsDeleted == false
                    && ts.StartTime >= parameters.StartTime
                    && (ts.StartTime <= parameters.EndTime || ts.EndTime == null)
                );

            return await query.ProjectTo<UsersTimeSegmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
        }
        public async Task<List<UsersTimelineWorklogDto>> UsersTimelineWorklog(UserTimelineWorklogQueryParameters parameters)
        {
            var users = await _dbContext.TimeSegments.AsQueryable()
                .Where(ts => ts.IsDeleted == false
                && ts.StartTime >= parameters.DateFrom.ToDateTime(TimeOnly.MinValue)
                && (ts.EndTime == null || ts.EndTime <= parameters.DateTo.ToDateTime(TimeOnly.MaxValue))
                && parameters.UserIds != null && parameters.UserIds.Contains(ts.UserId))
                .ProjectTo<UsersTimelineWorklogDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return users;
        }

        public async Task<TeamInTimeLineDto> TeamTimelineWorklog(GroupTimelineWorklogQueryParameters parameters)
        {
            var team = await _dbContext.Teams.FindAsync(parameters.GroupId)
                ?? throw new NotFoundException($"Team with Id: {parameters.GroupId} not found.");

            var usersInTeam = await _dbContext.Users
                .Where(u => u.TeamId == parameters.GroupId && !u.IsDeleted)
                .ToListAsync();

            if (!usersInTeam.Any())
            {
                return new TeamInTimeLineDto
                {
                    Id = team.Id,
                    Name = team.Name,
                    TimeLines = new List<UsersTimelineWorklogDto>()
                };
            }

            var userIds = usersInTeam.Select(u => u.Id).ToList();

            var filteredTimeSegments = await _dbContext.TimeSegments
                .Where(ts =>
                    userIds.Contains(ts.UserId) &&
                    !ts.IsDeleted &&
                    ts.StartTime >= parameters.DateFrom.ToDateTime(TimeOnly.MinValue) &&
                    (ts.EndTime == null || ts.EndTime <= parameters.DateTo.ToDateTime(TimeOnly.MaxValue)))
                .ToListAsync();

            var timeSegmentDtos = filteredTimeSegments
                .Select(ts => new UsersTimelineWorklogDto
                {
                    Id = ts.Id,
                    TimeSegmentType = ts.TimeSegmentType,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    DurationInSeconds = ts.DurationInSeconds,
                    UserId = ts.UserId,
                    RequestAction = ts.RequestAction,
                    CreatedAt = ts.CreatedAt,
                })
                .ToList();

            var teamInTimeline = new TeamInTimeLineDto
            {
                Id = team.Id,
                Name = team.Name,
                TimeLines = timeSegmentDtos
            };

            return teamInTimeline;
        }


        public async Task<ProjectInTimeLineDto> ProjectTimelineWorklog(GroupTimelineWorklogQueryParameters parameters)
        {
            var project = await _dbContext.Projects.FindAsync(parameters.GroupId)
                ?? throw new NotFoundException($"Project with Id: {parameters.GroupId} not found.");

            var usersInProject = await _dbContext.Users
                .Where(u => u.ProjectId == parameters.GroupId && !u.IsDeleted)
                .ToListAsync();

            if (!usersInProject.Any())
            {
                var emptyProject = _mapper.Map<ProjectInTimeLineDto>(project);
                emptyProject.TimeLines = new List<UsersTimelineWorklogDto>();
                return emptyProject;
            }

            var userIds = usersInProject.Select(u => u.Id).ToList();

            var filteredTimeSegments = await _dbContext.TimeSegments
                .Where(ts =>
                    userIds.Contains(ts.UserId) &&
                    !ts.IsDeleted &&
                    ts.StartTime >= parameters.DateFrom.ToDateTime(TimeOnly.MinValue) &&
                    (ts.EndTime == null || ts.EndTime <= parameters.DateTo.ToDateTime(TimeOnly.MaxValue)))
                .ToListAsync();

            var timeSegmentDtos = filteredTimeSegments
                .Select(ts => new UsersTimelineWorklogDto
                {
                    Id = ts.Id,
                    TimeSegmentType = ts.TimeSegmentType,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    DurationInSeconds = ts.DurationInSeconds,
                    UserId = ts.UserId,
                    RequestAction = ts.RequestAction,
                    CreatedAt = ts.CreatedAt,
                })
                .ToList();

            var projectInTimeline = _mapper.Map<ProjectInTimeLineDto>(project);
            projectInTimeline.TimeLines = timeSegmentDtos;

            return projectInTimeline;
        }


        public async Task<TimeSegmentType?> GetUserActiveTimeSegmentType(Guid userId)
        {
            var activeSegment = await GetActiveSegment(userId);
            if (activeSegment == null)
            {
                return null;
            }
            return activeSegment.TimeSegmentType;
        }

        public async Task<WidgetSyncDto?> WidgetSync(Guid userId)
        {
            var activeSegment = await GetActiveSegment(userId);
            if (activeSegment == null)
            {
                return null;
            }

            var widgetSyncDto = new WidgetSyncDto
            {
                TimeSegmentType = activeSegment.TimeSegmentType,
                StartTime = activeSegment.StartTime,
                DurationInSeconds = (int)(DateTime.UtcNow - activeSegment.StartTime).TotalSeconds
            };
            return widgetSyncDto;
        }


        private async Task<TimeSegment?> GetActiveSegment(Guid userId)
        {
            return await _dbContext.TimeSegments
                .Where(ts => ts.UserId == userId && ts.EndTime == null && ts.IsDeleted == false)
                .OrderByDescending(ts => ts.StartTime)
                .FirstOrDefaultAsync();
        }
        private async Task<TimeSegment?> GetLastSegment(Guid userId)
        {
            return await _dbContext.TimeSegments
                .Where(ts => ts.UserId == userId && ts.EndTime != null && ts.IsDeleted == false)
                .OrderByDescending(ts => ts.StartTime)
                .FirstOrDefaultAsync();
        }
        public async Task<List<TimeSegment>> StartBreakForUsers(List<Guid> userIds, DateTime? startTime)
        {
            var now = DateTime.UtcNow;
            var proposedStartTime = startTime ?? now;
            if (proposedStartTime < now.AddMinutes(-_maxReverseRegistrationTime))
            {
                throw new ValidationException("Start Time of break cannot be earlier than 1 hour before the current time.");
            }

            var activeSegments = await _dbContext.TimeSegments
                .Where(ts => userIds.Contains(ts.UserId) && ts.EndTime == null && ts.IsDeleted == false)
                .ToListAsync();

            var result = new List<TimeSegment>();

            foreach (var userId in userIds)
            {
                var activeSegment = activeSegments.FirstOrDefault(ts => ts.UserId == userId);

                if (activeSegment == null)
                {
                    continue;
                }
                if (activeSegment.TimeSegmentType == TimeSegmentType.Break)
                {
                    continue;
                }
                if (proposedStartTime < activeSegment.StartTime)
                {
                    continue;
                }

                activeSegment.EndTime = proposedStartTime;
                _dbContext.TimeSegments.Update(activeSegment);

                var newSegment = new TimeSegment
                {
                    UserId = userId,
                    TimeSegmentType = TimeSegmentType.Break,
                    StartTime = proposedStartTime,
                    EndTime = null
                };

                _dbContext.TimeSegments.Add(newSegment);
                result.Add(newSegment);
            }

            await _dbContext.SaveChangesAsync();
            return result;
        }

        public async Task ResolveActionRequest(int TimeSegmentId, ResolveActionCommand action)
        {
            TimeSegment timeSegment = await _dbContext.TimeSegments.FindAsync(TimeSegmentId) 
                ?? throw new NotFoundException($"TimeSegment with Id: {TimeSegmentId} not found.");

            switch (action)
            {
                
                case ResolveActionCommand.Reject:
                    timeSegment.RequestAction = false;
                    timeSegment.IsDeleted = true;
                    break;
                case ResolveActionCommand.Approve:
                    timeSegment.RequestAction = false;
                    break;
                case ResolveActionCommand.SetStartTimeAsCreationDate:
                    timeSegment.RequestAction = false;
                    timeSegment.StartTime = timeSegment.CreatedAt ?? throw new ValidationException("CreatedAt cannot be null."); ;
                    break;
                default:
                    throw new ValidationException("Invalid action command.");
            }

            _dbContext.TimeSegments.Update(timeSegment);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<TimeSegment> HandleAdminRequestNeeded(DateTime proposed, TimeSegment segment, Guid userId)
        {

            var now = DateTime.UtcNow;
            if (proposed < now.AddMinutes(-_maxReverseregistrationTimeLogged))
            {
                var user = await _dbContext.Users.FindAsync(userId)
                    ?? throw new NotFoundException($"User with Id: {userId} not found.");
                string message = $"User {user.GivenName} {user.Surname} ({userId}) has requested to log time before the allowed limit.";
                
                segment.RequestAction=true;
                await _notificationTeamsService.SendNotification(UserRole.Admin, message);

                Notification note = new Notification("Admin Approval Needed", message);
                await _notificationService.CreateNotificationsBatch(UserRole.Admin, note);
                await _notificationService.SendNotification(UserRole.Admin, note);
            }
            return segment;
        }
    }
}

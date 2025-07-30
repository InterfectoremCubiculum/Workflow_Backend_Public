using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Exceptions;
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

        public WorkLogService(IMapper mapper, WorkflowTimeDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
        }
        public async Task<TimeSegment> StartWork(Guid userId, WorkflowParameters? parameters)
        {
            var activeSegment = await GetActiveSegment(userId);
            if (activeSegment != null)
            {
                throw new ConflictException($"There is active segment of type {activeSegment.TimeSegmentType}. First end this segment to Start Work.");
            }

            var now = DateTime.UtcNow;
            var proposedStartTime = parameters?.StartTime ?? now;
            if (proposedStartTime < now.AddHours(-1))
            {
                throw new ValidationException($"Start Time ({proposedStartTime:yyyy-MM-dd HH:mm:ss}) cannot be earlier than one hour before now ({now:yyyy-MM-dd HH:mm:ss}).");
            }

            var newSegment = new TimeSegment
            {
                UserId = userId,
                TimeSegmentType = TimeSegmentType.Work,
                StartTime = proposedStartTime,
                EndTime = parameters?.EndTime ?? null
            };

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
            else if (activeSegment.TimeSegmentType != TimeSegmentType.Work)
            {
                throw new ConflictException("Cannot End Work when not on a Work segment. (First End your Break)");
            }

            var now = DateTime.UtcNow;
            var proposedEndTime = parameters?.EndTime ?? now;

            if (proposedEndTime < activeSegment.StartTime)
            {
                throw new ValidationException("End Time cannot be earlier than StartTime.");
            }

            if (proposedEndTime > now.AddHours(1))
            {
                throw new ValidationException($"End Time ({proposedEndTime:yyyy-MM-dd HH:mm:ss}) cannot be more than 1 hour ahead of current time ({now:yyyy-MM-dd HH:mm:ss}).");
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

            if (proposedStartTime < now.AddHours(-1))
            {
                throw new ValidationException("Start Time of break cannot be earlier than 1 hour before the current time.");
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

            if (proposedStartTime < now.AddHours(-1))
            {
                throw new ValidationException("Start Time cannot be earlier than 1 hour before the current time.");
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

            if (newSegment.EndTime.HasValue && newSegment.EndTime > now.AddHours(1))
            {
                throw new ValidationException("End Time cannot be more than 1 hour in the future.");
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

            var lastSegment = await query.OrderByDescending(ts => ts.StartTime).FirstOrDefaultAsync() 
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
                if (startTime < now.AddHours(-1) || startTime > now.AddHours(1))
                    throw new ValidationException("StartTime out of allowed range.");
                lastSegment.StartTime = startTime;
            }

            if (parameters.EndTime.HasValue)
            {
                var endTime = parameters.EndTime.Value;
                if (endTime < now.AddHours(-1) || endTime > now.AddHours(1))
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

            var timeSegments = await _dbContext.TimeSegments
                .Include(ts => ts.User)
                .Where(ts => !ts.IsDeleted
                    && ts.StartTime >= parameters.DateFrom.ToDateTime(TimeOnly.MinValue)
                    && (ts.EndTime == null || ts.EndTime <= parameters.DateTo.ToDateTime(TimeOnly.MaxValue))
                    && ts.User.TeamId.HasValue 
                    && ts.User.TeamId == parameters.GroupId)
                    .ProjectTo<UsersTimelineWorklogDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

            var teamInTimeline = new TeamInTimeLineDto
            {
                Id = team.Id,
                Name = team.Name,
                TimeLines = timeSegments
            };
            return teamInTimeline;
        }

        public async Task<ProjectInTimeLineDto> ProjectTimelineWorklog(GroupTimelineWorklogQueryParameters parameters)
        {
            var project = await _dbContext.Projects.FindAsync(parameters.GroupId)
                ?? throw new NotFoundException($"Project with Id: {parameters.GroupId} not found.");

            var timeSegments = await _dbContext.TimeSegments
                .Include(ts => ts.User)
                .Where(ts => !ts.IsDeleted
                    && ts.StartTime >= parameters.DateFrom.ToDateTime(TimeOnly.MinValue)
                    && (ts.EndTime == null || ts.EndTime <= parameters.DateTo.ToDateTime(TimeOnly.MaxValue))
                    && ts.User.ProjectId.HasValue
                    && ts.User.ProjectId == parameters.GroupId)
                    .ProjectTo<UsersTimelineWorklogDto>(_mapper.ConfigurationProvider)
                    .ToListAsync();

            var projectInTimeline = _mapper.Map<ProjectInTimeLineDto>(project);
            projectInTimeline.TimeLines = timeSegments;
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

    }
}

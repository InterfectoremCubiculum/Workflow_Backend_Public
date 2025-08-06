using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.DayOffs.Dtos;
using WorkflowTime.Features.DayOffs.Models;
using WorkflowTime.Features.DayOffs.Queries;
using WorkflowTime.Utillity;

namespace WorkflowTime.Features.DayOffs.Services
{
    public class DayOffRequestService : IDayOffRequestService
    {
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly IMapper _mapper;

        public DayOffRequestService(WorkflowTimeDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        public async Task<PagedResponse<GetDayOffRequestDto>> GetDayOffRequests(DayOffsRequestQueryParameters parameters)
        {
            var query = _dbContext.DayOffRequests.AsQueryable();

            if (parameters.UserId != null)
            {
                if (!_dbContext.Users.Any(u => u.Id == parameters.UserId))
                    throw new NotFoundException($"User with ID {parameters.UserId} not found.");
                query = query.Where(d => d.UserId == parameters.UserId);
            }

            var sortOrder = parameters.SortOrder.Trim().ToLower();

            query = parameters.SortBy switch
            {
                DayOffRequestOrderBy.EndDate => sortOrder == "desc"
                                        ? query.OrderByDescending(d => d.EndDate)
                                        : query.OrderBy(d => d.EndDate),
                DayOffRequestOrderBy.StartDate => sortOrder == "desc"
                                        ? query.OrderByDescending(d => d.StartDate)
                                        : query.OrderBy(d => d.StartDate),
                _ => sortOrder == "desc"
                                        ? query.OrderByDescending(d => d.RequestDate)
                                        : query.OrderBy(d => d.RequestDate),
            };
            if (parameters.DayOffRequestStatuses != null && parameters.DayOffRequestStatuses.Count != 0)
            {
                query = query.Where(d => parameters.DayOffRequestStatuses.Contains(d.RequestStatus));
            }

            var items = await query
                .Where(d => d.IsDeleted == false)
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Include(d => d.User)
                .ProjectTo<GetDayOffRequestDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var totalCounts = await query.CountAsync();

            return new PagedResponse<GetDayOffRequestDto>(items, parameters.PageNumber, parameters.PageSize, totalCounts);
        }


        public async Task<GetCreatedDayOffRequestDto> CreateDayOffRequest(CreateDayOffRequestDto dayOffRequest, Guid userId)
        {
            var newRequest = new DayOffRequest
            {
                StartDate = dayOffRequest.StartDate,
                EndDate = dayOffRequest.EndDate,
                RequestStatus = dayOffRequest.RequestStatus,
                UserId = userId
            };

            await _dbContext.DayOffRequests.AddAsync(newRequest);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<GetCreatedDayOffRequestDto>(newRequest);
        }
        public async Task UpdateDayOffRequestStatus(int id, DayOffRequestStatus status)
        {
            await ValidateIfExists(id);

            if (!Enum.IsDefined(status))
                throw new BadRequestException("Invalid request status provided.");

            var dayOffRequest = new DayOffRequest
            {
                Id = id,
                RequestStatus = status
            };

            var request = _dbContext.DayOffRequests.Attach(dayOffRequest);
            request.Property(d => d.RequestStatus).IsModified = true;
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateDayOffRequest(UpdateDayOffRequestDto dayOffRequest)
        {
            await ValidateIfExists(dayOffRequest.Id);

            var request = await _dbContext.DayOffRequests.FindAsync(dayOffRequest.Id);
            request.StartDate = dayOffRequest.StartDate;
            request.EndDate = dayOffRequest.EndDate;
            request.RequestStatus = dayOffRequest.RequestStatus;
            _dbContext.DayOffRequests.Update(request);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<List<GetCalendarDayOff>> GetCalendarDayOff(CalendarDayOffsRequestQueryParameters parameters)
        {
            var query = _dbContext.DayOffRequests.AsQueryable();
            query = query.Where(d => d.UserId == parameters.UserId);

            if (parameters.DayOffRequestStatuses != null && parameters.DayOffRequestStatuses.Count != 0)
                query = query.Where(d => parameters.DayOffRequestStatuses.Contains(d.RequestStatus));

            query = query.Where(d =>
                d.IsDeleted == false
                && d.EndDate >= parameters.From
                && d.StartDate <= parameters.To);

            var dayOff = await query
                .ProjectTo<GetCalendarDayOff>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return dayOff;
        }

        public async Task DeleteDayOffRequest(int id)
        {
            await ValidateIfExists(id);

            var dayOffRequest = new DayOffRequest
            {
                Id = id,
                IsDeleted = true
            };

            var request = _dbContext.DayOffRequests.Attach(dayOffRequest);
            request.Property(d => d.IsDeleted).IsModified = true;
            await _dbContext.SaveChangesAsync();
        }

        private async Task ValidateIfExists(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid request ID provided.");

            bool exists = await _dbContext.DayOffRequests.AnyAsync(dar => dar.Id == id && !dar.IsDeleted);
            if (!exists)
                throw new NotFoundException($"Day off request with ID {id} not found or has been deleted.");
        }

        public async Task UpdateDayOffState()
        {
            var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

            await _dbContext.DayOffRequests
                .Where(d => d.EndDate < currentDate && d.RequestStatus == DayOffRequestStatus.Pending)
                .ForEachAsync(d => d.RequestStatus = DayOffRequestStatus.Expired);

            await _dbContext.DayOffRequests
                .Where(d => d.EndDate < currentDate && d.RequestStatus == DayOffRequestStatus.Approved)
                .ForEachAsync(d => d.RequestStatus = DayOffRequestStatus.Completed);

            await _dbContext.SaveChangesAsync();
        }

    }
}

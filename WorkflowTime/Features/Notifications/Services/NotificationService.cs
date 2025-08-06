using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using Polly;
using Polly.Registry;
using WorkflowTime.Database;
using WorkflowTime.Enums;
using WorkflowTime.Features.Hubs;
using WorkflowTime.Features.Notifications.Models;

namespace WorkflowTime.Features.Notifications.Services
{
    /// <summary>
    /// SingalR
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly WorkflowTimeDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ResiliencePipeline _notifyPipeLine;
        private readonly ResiliencePipeline _workStatePipeLine;

        public NotificationService
        (
            IHubContext<SignalRHub> hubContext,
            ILogger<NotificationService> logger,
            WorkflowTimeDbContext dbContext,
            IMapper mapper,
            ResiliencePipelineProvider<string> pipelineProvider
           
        )
        {
            _mapper = mapper;
            _hubContext = hubContext;
            _logger = logger;
            _dbContext = dbContext;
            _notifyPipeLine = pipelineProvider.GetPipeline("NotifySignalRPipeLine");
            _workStatePipeLine = pipelineProvider.GetPipeline("WorkStatePipeLine");

        }

        public async Task CreateNotificationsBatch(List<Notification> notifications)
        {
            _dbContext.Notifications.AddRange(notifications);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<SendedNotificationDto>> GetNotifications(Guid userId, bool read)
        {
            var results = await _dbContext.Notifications
                .Where(n => n.UserId == userId && n.IsRead == read)
                .Select(n => new SendedNotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                })
                .OrderByDescending( n => n.CreatedAt)
                .ToListAsync();

            return results;
        }

        public Task MarkNotifications(List<int> notificationsIds)
        {
            _dbContext.Notifications
                .Where(n => notificationsIds.Contains(n.Id))
                .ToList()
                .ForEach(n => n.IsRead = true);

            return _dbContext.SaveChangesAsync();
        }

        public async Task SendNotification(Guid userId, SendedNotificationDto notToSend)
        {
            try
            {
                await _notifyPipeLine.ExecuteAsync(async (ct) =>
                {
                    await _hubContext.Clients.User(userId.ToString())
                        .SendAsync("notifyClient", notToSend, ct);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }
        public async Task SendNotification(Guid userId, Notification noteToSend)
        {
            var mappedNote = _mapper.Map<SendedNotificationDto>(noteToSend);
            try 
            {
                await _notifyPipeLine.ExecuteAsync(async (ct) =>
                {
                    await _hubContext.Clients.User(userId.ToString())
                        .SendAsync("notifyClient", mappedNote, ct);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }

        public async Task NotifyWorkStateChange(Guid userId, string? state)
        {
            try
            {
                await _workStatePipeLine.ExecuteAsync(async (ct) =>
                {

                    await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("workStateChanged", new
                    {
                        state,
                        timestamp = DateTime.UtcNow
                    }, ct);
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending work state change to user {UserId}", userId);
            }
        }

        public async Task CreateNotificationsBatch(UserRole? userToInformRole, Notification notification)
        {
            var users = await _dbContext.Users
                .Where(u => u.Role == userToInformRole && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            List<Notification> notifications = new List<Notification>();

            foreach (var userId in users)
            {
                notifications.Add(new Notification
                (
                    notification.Title,
                    notification.Message,
                    userId
                ));
            }

            _dbContext.Notifications.AddRange(notifications);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SendNotification(UserRole userToInformRole, Notification noteToSend)
        {
            var mappedNote = _mapper.Map<SendedNotificationDto>(noteToSend);
            await _hubContext.Clients.Group($"group_{userToInformRole}").SendAsync("notifyClient", mappedNote);
        }
    }
}

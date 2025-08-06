using AutoMapper;
using Microsoft.Graph.Models;
using WorkflowTime.Features.DayOffs.Dtos;
using WorkflowTime.Features.DayOffs.Models;
using WorkflowTime.Features.Notifications;
using WorkflowTime.Features.Notifications.Models;
using WorkflowTime.Features.ProjectManagement.Models;
using WorkflowTime.Features.Summary.Dtos;
using WorkflowTime.Features.Teams.Bot.Services.AI;
using WorkflowTime.Features.Teams.Graph;
using WorkflowTime.Features.UserManagement.Dtos;
using WorkflowTime.Features.UserManagment.Models;
using WorkflowTime.Features.WorkLog.Dtos;
using WorkflowTime.Features.WorkLog.Models;

namespace WorkflowTime.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TimeSegment, UsersTimelineWorklogDto>();
            CreateMap<TimeSegment, UsersTimeSegmentDto>();

            CreateMap<UserModel, GetSearchedUserDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? "No email"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GivenName));
            CreateMap<UserModel, GetUsersByGuidDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? "No email"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GivenName));
            CreateMap<User, UserModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)))
                .ForMember(dest => dest.GivenName, opt => opt.MapFrom(src => src.GivenName ?? "Unknown"))
                .ForMember(dest => dest.Surname, opt => opt.MapFrom(src => src.Surname ?? "Unknown"))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Mail));

            CreateMap<DayOffRequest, GetDayOffRequestDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.GivenName))
                .ForMember(dest => dest.UserSurname, opt => opt.MapFrom(src => src.User.Surname));
            CreateMap<DayOffRequest, GetCreatedDayOffRequestDto>();
            CreateMap<DayOffRequest, GetCalendarDayOff>();

            CreateMap<Project, ProjectInTimeLineDto>()
                .ForMember(dest => dest.TimeLines, opt => opt.Ignore());

            CreateMap<WorkflowActionResult, WorkflowParameters>();

            CreateMap<Notification, SendedNotificationDto>();

            CreateMap<UserWorkSummaryDto, WorkSummaryCSV>();

            CreateMap<UserModel, UserCacheDto>()
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            CreateMap<UserCacheDto, GetMeDto>();

            CreateMap<UserCacheDto, GetUsersByGuidDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.GivenName));

            CreateMap<Presence, UserPresenceDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Availability));
        }
    }
}

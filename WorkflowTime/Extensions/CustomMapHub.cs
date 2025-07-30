using WorkflowTime.Features.Hubs;

namespace WorkflowTime.Extensions
{
    public static class CustomMapHub
    {
        public static WebApplication AddApplicationHubs(this WebApplication app)
        { 
            app.MapHub<SignalRHub>("/hub/signalR");
            return app;
        }
    }
}

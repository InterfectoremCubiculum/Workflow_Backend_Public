namespace WorkflowTime.Configuration
{
    public class MicrosoftAppOptions
    {
        public required string AppId { get; set; }
        public required string AppPassword { get; set; }
        public required string AppTenantId { get; set; }
        public required string AppType { get; set; }
        public required string ServiceUrl { get; set; }
    }
}

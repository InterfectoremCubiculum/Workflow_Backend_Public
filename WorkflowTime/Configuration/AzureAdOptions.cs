namespace WorkflowTime.Configuration
{
    public class AzureAdOptions
    {
        public required string Instance { get; set; }
        public required string Domain { get; set; }
        public required string TenantId { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string CallbackPath { get; set; }
        public required string SignedOutCallbackPath { get; set; }
    }
}

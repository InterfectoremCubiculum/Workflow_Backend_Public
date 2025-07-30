namespace WorkflowTime.Configuration
{
    public class OpenAIWorkflowAnalyzerOptions
    {
        public required string Endpoint { get; set; }
        public required string ApiKey { get; set; }
        public string Model { get; set; } = string.Empty;
        public required string DeploymentName {  get; set; }
    }
}

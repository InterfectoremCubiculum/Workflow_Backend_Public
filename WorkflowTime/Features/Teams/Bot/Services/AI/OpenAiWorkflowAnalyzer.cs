using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using NJsonSchema;
using OpenAI.Chat;
using Polly;
using Polly.Registry;
using System;
using System.Text.Json;
using WorkflowTime.Configuration;

namespace WorkflowTime.Features.Teams.Bot.Services.AI
{
    public class OpenAiWorkflowAnalyzer : IWorkflowAnalyzer
    {
        private readonly ILogger<OpenAiWorkflowAnalyzer> _logger;
        private readonly OpenAIWorkflowAnalyzerOptions _options;
        private readonly ResiliencePipeline _openAiPipeline;
        public OpenAiWorkflowAnalyzer
        (
            ILogger<OpenAiWorkflowAnalyzer> logger,
            IOptions<OpenAIWorkflowAnalyzerOptions> options,
            ResiliencePipelineProvider<string> pipelineProvider
        )
        {
            _options = options.Value;
            _logger = logger;
            _openAiPipeline = pipelineProvider.GetPipeline("OpenAiPipeline");
        }

        public async Task<WorkflowActionResult?> AnalyzeWorkflow(string inputText, CancellationToken cancellationToken)
        {
            string schemaJson = JsonSchema.FromType<WorkflowActionResult>().ToJson();

            List<ChatMessage> messages =
            [
                new SystemChatMessage(
                    "You are an assistant in a time tracking application. " +
                    "Based on the user's input, identify the user's intent from the following options: " +
                    "- StartWork: when they are beginning work." +
                    "- StartBreak: when they are starting a break." +
                    "- ResumeWork: when they are ending a break or returning to work." +
                    "- EndWork: when they are finishing their work day." +
                    "- EditLog: when they want to modify an existing work or break log (e.g., 'Change start time of my work from 9 to 10am'). " +
                    "If editing, extract the target time/type reference, and new Start/End times if applicable. " +
                    "If the user includes specific times (e.g., from 8 to 16:30, or I had a break at 9), " +
                    "When the user wants to add time, set the \"AddTime\" field with the amount of time to add (hours and minutes)." +
                    "When the user wants to subtract time, set the \"SubtractTime\" field similarly." +
                    "Use standard .NET TimeSpan format for durations (e.g., 00:05:00 for 5 minutes), not ISO 8601." +
                    "extract the start and end times using ISO 8601 format (e.g., 2025-07-24T08:00:00)." +
                    "Always respond strictly according to the schema." +
                    "If date not provided use this day which is actual time: " + DateTime.UtcNow.ToString("s")),
                new UserChatMessage(inputText),
            ];

            AzureOpenAIClient azureClient = new(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));

            ChatClient chatClient = azureClient.GetChatClient(_options.DeploymentName);

            var requestOptions = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = 1024,
                Temperature = 0.15f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                ResponseFormat =
                    ChatResponseFormat.CreateJsonSchemaFormat
                    (
                        "workflowAction",
                        BinaryData.FromString(schemaJson)

                    )
            };

            ChatCompletion? result = null;
            try
            {
                result = await _openAiPipeline.ExecuteAsync(async ct =>
                    await chatClient.CompleteChatAsync(messages, requestOptions, ct),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze workflow after retries for input: {InputText}", inputText);
                throw new Exception("Failed to analyze workflow", ex);
            }

            var contentPart = result?.Content.FirstOrDefault();
            WorkflowActionResult? workflowResult = null;

            if (contentPart?.Kind == ChatMessageContentPartKind.Text)
            {
                try
                {
                    var json = contentPart.Text;

                    workflowResult = JsonSerializer.Deserialize<WorkflowActionResult>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing AI response for user input: {InputText}", JsonSerializer.Serialize(contentPart.Text));
                }
            }
            return workflowResult;
        }
    }
}

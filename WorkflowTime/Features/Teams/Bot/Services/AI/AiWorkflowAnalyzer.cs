using System.Text;
using System.Text.Json;
using WorkflowTime.Enums;

namespace WorkflowTime.Features.Teams.Bot.Services.AI
{
    /// <summary>
    ///  CLU (Conversational Language Understanding) not used any more.
    ///  User OpenAIWorkflowAnalyzer instead.
    /// </summary>
    public class AiWorkflowAnalyzer : IWorkflowAnalyzer
    {

        public readonly IConfiguration _configuration;
        public readonly HttpClient _httpClient;
        public AiWorkflowAnalyzer(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }
        public async Task<(string Type, string? Time)?> AnalyzeWorkflow(string userMessage, CancellationToken cancellationToken)
        {
            var endpoint = _configuration["AiWorkflowAnalyzer:Endpoint"];
            var apiKey = _configuration["AiWorkflowAnalyzer:ApiKey"];
            var projectName = _configuration["AiWorkflowAnalyzer:ProjectName"];
            var deploymentName = _configuration["AiWorkflowAnalyzer:DeploymentName"];

            var url = $"{endpoint}/language/:analyze-conversations?api-version=2024-11-15-preview";
            var payload = new
            {
                kind = "Conversation",
                analysisInput = new
                {
                    conversationItem = new
                    {
                        id = "1",
                        text = userMessage,
                        modality = "text",
                        language = "en",
                        participantId = "user"
                    }
                },
                parameters = new
                {
                    projectName,
                    deploymentName,
                    verbose = true,
                    isLoggingEnabled = false
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return ParseCLUResponse(jsonResponse);
            }

            return null;
        }

        Task<WorkflowActionResult> IWorkflowAnalyzer.AnalyzeWorkflow(string inputText, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private (string Type, string? Time)? ParseCLUResponse(string response)
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            var result = root.GetProperty("result");
            var prediction = result.GetProperty("prediction");
            var topIntent = prediction.GetProperty("topIntent").GetString();
            var entities = prediction.GetProperty("entities");

            string? time = null;
            foreach (var entity in entities.EnumerateArray())
            {
                if (entity.GetProperty("category").GetString() == "WorkTime")
                {
                    time = entity.GetProperty("text").GetString();
                }
            }

            if (topIntent != null)
            {
                return (topIntent, time);
            }

            return null;
        }
    }
}

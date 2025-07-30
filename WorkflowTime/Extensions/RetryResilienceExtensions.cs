using Microsoft.Graph.Models.ODataErrors;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace WorkflowTime.Extensions
{
    public static class RetryResilienceExtensions
    {
        public static IServiceCollection AddRetryResilience(this IServiceCollection services)
        {
            services.AddResiliencePipeline("OpenAiPipeline", builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                                            .CreateLogger("OpenAiPipeline");

                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is HttpRequestException ||
                        ex is TaskCanceledException ||
                        ex.Message.Contains("ServiceUnavailable") || 
                        ex.Message.Contains("TooManyRequests")),
                    OnRetry = args =>
                    {
                        logger.LogWarning("Retrying OpenAI request due to: {ExceptionMessage}", args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(10)
                });
            });

            services.AddResiliencePipeline("NotifySignalRPipeLine", builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                                            .CreateLogger("NotifySignalRPipeLine");

                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromMinutes(5),
                    UseJitter = true,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is HttpRequestException ||
                        ex is TaskCanceledException ||
                        ex.Message.Contains("Timeout") ||
                        ex.Message.Contains("Failed to send")),
                    OnRetry = args =>
                    {
                        logger.LogWarning("Retrying notification due to: {ExceptionMessage}", args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(10)
                });
            });

            services.AddResiliencePipeline("WorkStatePipeLine", builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                                            .CreateLogger("WorkStatePipeLine");

                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 5,
                    Delay = TimeSpan.FromSeconds(5),
                    BackoffType = DelayBackoffType.Constant,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is HttpRequestException ||
                        ex is TaskCanceledException ||
                        ex.Message.Contains("Timeout") ||
                        ex.Message.Contains("Failed to send")),
                    OnRetry = args =>
                    {
                        logger.LogWarning("Retrying work state send due to: {ExceptionMessage}", args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });
                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(5)
                });
            });

            services.AddResiliencePipeline("GraphUserSync", builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                                            .CreateLogger("GraphUserSync");

                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMinutes(2),
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is HttpRequestException ||
                        ex is TaskCanceledException ||
                        ex is ODataError ||
                        ex.Message.Contains("ServiceUnavailable") ||
                        ex.Message.Contains("TooManyRequests")),
                    OnRetry = args =>
                    {
                        logger.LogWarning("Retrying Microsoft Graph sync due to: {ExceptionMessage}", args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });

                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(10)
                });
            });

            services.AddResiliencePipeline("TeamsNotificationPipeLine", builder =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                                            .CreateLogger("TeamsNotificationPipeLine");

                builder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMinutes(3),
                    UseJitter = true,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                        ex is HttpRequestException ||
                        ex is TaskCanceledException ||
                        ex.Message.Contains("Timeout") ||
                        ex.Message.Contains("failed") ||
                        ex.Message.Contains("Teams")),
                    OnRetry = args =>
                    {
                        logger.LogWarning("Retrying Teams notification due to: {ExceptionMessage}", args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });

                builder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(10)
                });
            });


            return services;
        }
    }
}

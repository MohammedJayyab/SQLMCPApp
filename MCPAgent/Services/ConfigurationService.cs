using Microsoft.Extensions.Configuration;
using LLMKit;
using LLMKit.Providers;

namespace MCPAgent.Services
{
    public static class ConfigurationService
    {
        public static LLMClient CreateLLMClient()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var provider = configuration["LLMKit:Provider"];
            var maxTokens = int.Parse(configuration["LLMKit:Settings:MaxTokens"]);
            var temperature = double.Parse(configuration["LLMKit:Settings:Temperature"]);
            var maxMessages = int.Parse(configuration["LLMKit:Settings:MaxMessages"]);

            ILLMProvider llmProvider = provider?.ToLower() switch
            {
                "gemini" => new GeminiProvider(
                    apiKey: configuration["LLMKit:Gemini:ApiKey"],
                    model: configuration["LLMKit:Gemini:Model"]
                ),
                "openai" => new OpenAIProvider(
                    apiKey: configuration["LLMKit:OpenAI:ApiKey"],
                    model: configuration["LLMKit:OpenAI:Model"]
                ),
                "deepseek" => new DeepSeekProvider(
                    apiKey: configuration["LLMKit:DeepSeek:ApiKey"],
                    model: configuration["LLMKit:DeepSeek:Model"]
                ),
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };

            return new LLMClient(llmProvider, maxTokens, temperature, maxMessages);
        }

        public static string GetConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return configuration["Database:ConnectionString"];
        }
    }
}
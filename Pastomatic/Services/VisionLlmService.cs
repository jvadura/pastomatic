using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pastomatic.Services
{
    public class VisionLlmService : IVisionLlmService
    {
        private readonly ILogger<VisionLlmService> _logger;
        private readonly IConfiguration _configuration;

        public VisionLlmService(ILogger<VisionLlmService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> DescribeImageAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            var endpoint = _configuration.GetValue<string>("Vision:Endpoint", "http://10.0.0.244:8000/v1")!;
            var model = _configuration.GetValue<string>("Vision:Model", "qwen2.5-vl-72b-instruct")!;
            var apiKey = _configuration.GetValue<string>("Vision:ApiKey", "not-needed")!;
            var maxTokens = _configuration.GetValue<int>("Vision:MaxTokens", 4096);
            var temperature = (float)_configuration.GetValue<double>("Vision:Temperature", 0.1);
            var timeoutSeconds = _configuration.GetValue<int>("Vision:TimeoutSeconds", 120);
            var systemPrompt = _configuration.GetValue<string>("Vision:SystemPrompt",
                "You are an expert image analyst. Provide an extremely detailed description of what you see in this image.")!;

            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = "not-needed";

            _logger.LogInformation("Sending image ({Size} bytes) to vision LLM at {Endpoint} model {Model}",
                imageBytes.Length, endpoint, model);

            var clientOptions = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            };

            var client = new ChatClient(model, new System.ClientModel.ApiKeyCredential(apiKey), clientOptions);

            var imageData = BinaryData.FromBytes(imageBytes);
            var imagePart = ChatMessageContentPart.CreateImagePart(imageData, "image/png", ChatImageDetailLevel.High);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(imagePart, ChatMessageContentPart.CreateTextPart("Describe this image in detail."))
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = maxTokens,
                Temperature = temperature
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var result = await client.CompleteChatAsync(messages, options, cts.Token);

            var description = result.Value.Content[0].Text;
            _logger.LogInformation("Got vision description: {Length} chars", description.Length);

            return description;
        }
    }
}

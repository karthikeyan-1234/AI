// SemanticKernelGenerationService.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Day_4___Memory___Embeddings___RAG.Services
{
    internal class SemanticKernelGenerationService : ITextGenerationService
    {
        private readonly IChatCompletionService _chatService;

        public SemanticKernelGenerationService(Kernel kernel)
        {
            _chatService = kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
        {
            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
                MaxTokens = 1000
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings, kernel: null, ct);
            return response.Content ?? string.Empty;
        }
    }
}
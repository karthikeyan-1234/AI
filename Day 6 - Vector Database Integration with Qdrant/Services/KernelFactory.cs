// KernelFactory.cs - Using Microsoft.SemanticKernel
using Day_6___Vector_Database_Integration_with_Qdrant.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    public static class KernelFactory
    {
        public static Kernel Create(KernelSettings settings)
        {
            var builder = Kernel.CreateBuilder();

            switch (settings.Provider.ToLowerInvariant())
            {
                case "ollama":

                    var ollamaHttpClient = new HttpClient
                    {
                        BaseAddress = new Uri(settings.Endpoint)
                    };

                    // Text Embedding Generation
                    builder.AddOpenAITextEmbeddingGeneration(
                        modelId: settings.EmbeddingModel,
                        apiKey: settings.ApiKey, // Can be any non-empty string
                        httpClient: ollamaHttpClient,
                        serviceId: "EmbeddingService");

                    // Chat Completion
                    builder.AddOpenAIChatCompletion(
                        modelId: settings.ChatModel,
                        endpoint: new Uri(settings.Endpoint),
                        apiKey: settings.ApiKey,
                        serviceId: "ChatService");
                    break;

                case "openai":

                    var HttpClient = new HttpClient
                    {
                        BaseAddress = new Uri(settings.Endpoint)
                    };

                    // Text Embedding Generation (OpenAI.com)
                    builder.AddOpenAITextEmbeddingGeneration(
                        modelId: settings.EmbeddingModel,
                        apiKey: settings.ApiKey,
                        httpClient: HttpClient,
                        serviceId: "EmbeddingService");

                    // Chat Completion (OpenAI.com)
                    builder.AddOpenAIChatCompletion(
                        modelId: settings.ChatModel,
                        apiKey: settings.ApiKey,
                        serviceId: "ChatService");
                    break;

                case "azure":
                    // Azure OpenAI
                    builder.AddAzureOpenAITextEmbeddingGeneration(
                        deploymentName: settings.EmbeddingModel,
                        endpoint: settings.Endpoint,
                        apiKey: settings.ApiKey,
                        serviceId: "EmbeddingService");

                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: settings.ChatModel,
                        endpoint: settings.Endpoint,
                        apiKey: settings.ApiKey,
                        serviceId: "ChatService");
                    break;

                default:
                    throw new ArgumentException($"Unsupported provider: {settings.Provider}");
            }

            VerifyServices(builder.Build());

            return builder.Build();
        }


        private static void VerifyServices(Kernel kernel)
        {
            try
            {
                var embedding = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
                Console.WriteLine("✅ Embedding service registered in kernel");
            }
            catch
            {
                Console.WriteLine("❌ Embedding service NOT in kernel");
                throw;
            }

            try
            {
                var chat = kernel.GetRequiredService<IChatCompletionService>();
                Console.WriteLine("✅ Chat service registered in kernel");
            }
            catch
            {
                Console.WriteLine("❌ Chat service NOT in kernel");
                throw;
            }
        }
    }
}
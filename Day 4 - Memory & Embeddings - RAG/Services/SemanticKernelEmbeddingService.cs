// SemanticKernelEmbeddingService.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Day_4___Memory___Embeddings___RAG.Services
{
    internal class SemanticKernelEmbeddingService : IEmbeddingService
    {
        private readonly ITextEmbeddingGenerationService _service;
        private Kernel _kernel;

        public SemanticKernelEmbeddingService(Kernel kernel)
        {
            _service = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            _kernel = kernel;
        }

        public async Task<IReadOnlyList<float>> GenerateAsync(string text, CancellationToken ct = default)
        {
            var vectors = await _service.GenerateEmbeddingsAsync([text],_kernel, ct);
            return vectors[0].ToArray();
        }

        public async Task<IReadOnlyList<IReadOnlyList<float>>> GenerateBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
        {
            var vectors = await _service.GenerateEmbeddingsAsync(texts.ToArray(), _kernel, ct);
            return vectors.Select(v => v.ToArray() as IReadOnlyList<float>).ToList();
        }
    }
}
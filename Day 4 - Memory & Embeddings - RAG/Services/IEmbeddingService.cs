// IEmbeddingService.cs (Your stable interface - KEEP THIS!)
namespace Day_4___Memory___Embeddings___RAG.Services
{
    public interface IEmbeddingService
    {
        Task<IReadOnlyList<float>> GenerateAsync(
            string text,
            CancellationToken ct = default);

        Task<IReadOnlyList<IReadOnlyList<float>>> GenerateBatchAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default);
    }
}
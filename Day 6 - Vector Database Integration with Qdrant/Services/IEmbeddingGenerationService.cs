// IEmbeddingService.cs (Your stable interface - KEEP THIS!)
namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    public interface IEmbeddingGenerationService
    {
        Task<IReadOnlyList<float>> GenerateAsync(
            string text,
            CancellationToken ct = default);

        Task<IReadOnlyList<IReadOnlyList<float>>> GenerateBatchAsync(
            IReadOnlyList<string> texts,
            CancellationToken ct = default);
    }
}
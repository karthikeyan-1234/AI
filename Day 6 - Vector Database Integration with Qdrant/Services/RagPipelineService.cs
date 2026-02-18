using Day_6___Vector_Database_Integration_with_Qdrant.Models;
using Day_6___Vector_Database_Integration_with_Qdrant.Services;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.TextGeneration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Services/RagPipelineService.cs
namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    public class RagPipelineService
    {
        private readonly IEmbeddingGenerationService _embeddingService;
        private readonly IResponseGenerationService _generationService;
        private readonly IVectorStoreService _vectorStore;
        private readonly IDocumentChunkingService _chunkingService;
        private readonly ILogger<RagPipelineService>? _logger;

        public RagPipelineService(
            IEmbeddingGenerationService embeddingService,
            IResponseGenerationService generationService,
            IVectorStoreService vectorStore,
            IDocumentChunkingService chunkingService,
            ILogger<RagPipelineService>? logger = null)
        {
            _embeddingService = embeddingService;
            _generationService = generationService;
            _vectorStore = vectorStore;
            _chunkingService = chunkingService;
            _logger = logger;
        }

        /// <summary>
        /// Index a document into the vector database
        /// </summary>
        public async Task IndexDocumentAsync(string collectionName, Document document, ChunkingOptions? chunkOptions = null, CancellationToken ct = default)
        {
            _logger?.LogInformation("Indexing document {DocumentId}", document.Id);

            // 1. Chunk the document. Document.Content is the property which holds the info
            var chunks = _chunkingService.ChunkDocument(document, chunkOptions);
            _logger?.LogInformation("Created {Count} chunks", chunks.Count);

            // 2. Generate embeddings for all chunks
            var texts = chunks.Select(c => c.Text).ToList();
            var embeddings = await _embeddingService.GenerateBatchAsync(texts, ct);

            // 3. Create collection if needed (first check vector size)
            var vectorSize = embeddings.FirstOrDefault()?.Count ?? 0;
            if (vectorSize == 0)
                throw new InvalidOperationException("Failed to generate embeddings");

            if (!await _vectorStore.CollectionExistsAsync(collectionName, ct))
            {
                await _vectorStore.CreateCollectionAsync(collectionName, vectorSize, ct);
            }

            // 4. Upsert chunks with embeddings
            var items = chunks.Zip(embeddings, (chunk, emb) => (chunk, emb)).ToList();
            await _vectorStore.UpsertChunksAsync(collectionName, items, ct);

            _logger?.LogInformation("Successfully indexed {Count} chunks to {Collection}", chunks.Count, collectionName);
        }

        /// <summary>
        /// Query the RAG pipeline
        /// </summary>
        public async Task<string> QueryAsync(string collectionName, string query, int topK = 5, double threshold = 0.7, CancellationToken ct = default)
        {
            _logger?.LogInformation("Processing query: {Query}", query);

            // 1. Generate embedding for query
            var queryEmbedding = await _embeddingService.GenerateAsync(query, ct);

            // 2. Search for relevant chunks
            var results = await _vectorStore.SearchAsync(collectionName, queryEmbedding, topK, threshold, ct);

            if (results.Count == 0)
            {
                _logger?.LogWarning("No relevant documents found for query");
                return "I couldn't find any relevant information in the knowledge base to answer your question.";
            }

            _logger?.LogInformation("Found {Count} relevant chunks", results.Count);

            // 3. Build context from results
            var context = BuildContext(results);

            // 4. Generate answer with context
            var prompt = $"""
            You are a helpful assistant. Answer the question using ONLY the provided context.
            If the context doesn't contain the answer, say "I don't have enough information."
            
            Context:
            {context}
            
            Question: {query}
            
            Answer:
            """;

            var answer = await _generationService.GenerateAsync(prompt, ct);

            return answer;
        }

        /// <summary>
        /// Query with metadata filtering
        /// </summary>
        public async Task<string> QueryWithFilterAsync(string collectionName, string query, Dictionary<string, object> filters, int topK = 5, double threshold = 0.7, CancellationToken ct = default)
        {
            var queryEmbedding = await _embeddingService.GenerateAsync(query, ct);
            var results = await _vectorStore.SearchWithFilterAsync(collectionName, queryEmbedding, filters, topK, threshold, ct);

            if (results.Count == 0)
                return "No relevant documents found matching the filters.";

            var context = BuildContext(results);

            var prompt = $"""
            Using the provided context, answer the question.
            
            Context:
            {context}
            
            Question: {query}
            
            Answer:
            """;

            return await _generationService.GenerateAsync(prompt, ct);
        }

        private string BuildContext(List<SearchResult> results)
        {
            return string.Join("\n\n---\n\n", results.Select(r =>
                $"[Relevance: {r.Score:F2}]\n{r.Text}"));
        }
    }
}
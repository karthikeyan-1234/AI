
using Day_6___Vector_Database_Integration_with_Qdrant.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    public interface IVectorStoreService
    {
        Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default); // Create a new collection in the vector database

        Task UpsertChunkAsync(string collectionName, DocumentChunk chunk, IReadOnlyList<float> embedding, CancellationToken ct = default); // Insert or update a document chunk with its embedding

        Task UpsertChunksAsync(string collectionName, List<(DocumentChunk Chunk, IReadOnlyList<float> Embedding)> items, CancellationToken ct = default); // Insert multiple chunks in batch (more efficient)

        Task<List<SearchResult>> SearchAsync(string collectionName, IReadOnlyList<float> queryEmbedding, int limit = 5, double threshold = 0.7, CancellationToken ct = default); // Search for similar chunks using a query embedding

        Task<List<SearchResult>> SearchWithFilterAsync(string collectionName, IReadOnlyList<float> queryEmbedding, Dictionary<string, object> filters, int limit = 5, double threshold = 0.7, CancellationToken ct = default); // Search with metadata filtering

        Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default); // Delete a collection

        Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default); // Check if collection exists
    }
}

using Day_6___Vector_Database_Integration_with_Qdrant.Models;

using Microsoft.Extensions.Logging;

using Qdrant.Client;
using Qdrant.Client.Grpc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    internal class QdrantVectorStoreService : IVectorStoreService
    {
        private readonly QdrantClient _client;
        private readonly ILogger<QdrantVectorStoreService>? _logger;
        private const float DEFAULT_THRESHOLD = 0.7f;

        public QdrantVectorStoreService(string host = "localhost", int port = 6334, bool useHttps = false, ILogger<QdrantVectorStoreService>? logger = null)
        {
            _client = new QdrantClient(host, port, useHttps);
            _logger = logger;
        }

        public async Task CreateCollectionAsync(string collectionName, int vectorSize, CancellationToken ct = default)
        {
            _logger?.LogInformation("Creating collection {Collection} with vector size {Size}", collectionName, vectorSize);

            try
            {
                // Check if collection exists
                var collections = await _client.ListCollectionsAsync(ct);
                if (collections.Contains(collectionName))
                {
                    _logger?.LogWarning("Collection {Collection} already exists", collectionName);
                    return;
                }

                // Create collection with vector configuration
                await _client.CreateCollectionAsync(collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = (ulong)vectorSize,
                        Distance = Distance.Cosine  // Cosine similarity works well with OpenAI embeddings
                    },
                    cancellationToken: ct
                );

                _logger?.LogInformation("Collection {Collection} created successfully", collectionName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create collection {Collection}", collectionName);
                throw;
            }
        }

        public async Task DeleteCollectionAsync(string collectionName, CancellationToken ct = default)
        {
            _logger?.LogInformation("Deleting collection {Collection}", collectionName);

            if (await CollectionExistsAsync(collectionName, ct))
            {
                await _client.DeleteCollectionAsync(collectionName, cancellationToken: ct);
                _logger?.LogInformation("Collection {Collection} deleted", collectionName);
            }
        }

        public async Task<bool> CollectionExistsAsync(string collectionName, CancellationToken ct = default)
        {
            var collections = await _client.ListCollectionsAsync(ct);
            return collections.Contains(collectionName);
        }

        public async Task<List<SearchResult>> SearchAsync(string collectionName, IReadOnlyList<float> queryEmbedding, int limit = 5, double threshold = 0.7, CancellationToken ct = default)
        {
            _logger?.LogDebug("Searching {Collection} with threshold {Threshold}", collectionName, threshold);

            var searchResult = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryEmbedding.Select(v => (float)v).ToArray(),
                limit: (ulong)limit,
                scoreThreshold: (float)threshold,
                cancellationToken: ct
            );

            return searchResult.Select(MapToSearchResult).ToList();
        }

        public async Task<List<SearchResult>> SearchWithFilterAsync(string collectionName, IReadOnlyList<float> queryEmbedding, Dictionary<string, object> filters, int limit = 5, double threshold = 0.7, CancellationToken ct = default)
        {
            _logger?.LogInformation("Searching {Collection} with {FilterCount} filters", collectionName, filters.Count);

            var filter = BuildFilter(filters);

            var searchResult = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryEmbedding.Select(v => (float)v).ToArray(),
                filter: filter,
                limit: (ulong)limit,
                scoreThreshold: (float)threshold,
                cancellationToken: ct
            );

            return searchResult.Select(MapToSearchResult).ToList();
        }

        public async Task UpsertChunkAsync(string collectionName, DocumentChunk chunk, IReadOnlyList<float> embedding, CancellationToken ct = default)
        {
            _logger?.LogDebug("Upserting chunk {ChunkId} to {Collection}", chunk.Id, collectionName);

            var point = BuildPoint(chunk, embedding);
            await _client.UpsertAsync(collectionName, new[] { point }, cancellationToken: ct);
        }

        public async Task UpsertChunksAsync(string collectionName, List<(DocumentChunk Chunk, IReadOnlyList<float> Embedding)> items, CancellationToken ct = default)
        {
            _logger?.LogInformation("Batch upserting {Count} chunks to {Collection}", items.Count, collectionName);

            var points = items.Select(item => BuildPoint(item.Chunk, item.Embedding)).ToList();
            await _client.UpsertAsync(collectionName, points, cancellationToken: ct);
        }


        #region private helper methods

        private PointStruct BuildPoint(DocumentChunk chunk, IReadOnlyList<float> embedding)
        {
            var point = new PointStruct
            {
                // FIXED: Id is already a valid GUID string
                Id = new PointId { Uuid = chunk.Id },
                Vectors = embedding.ToArray()
            };

            // Add payload items
            point.Payload["document_id"] = chunk.DocumentId;
            point.Payload["text"] = chunk.Text;
            point.Payload["index"] = chunk.Index;
            point.Payload["created_at"] = chunk.CreatedAt.ToString("O");

            // Store the original string ID for reference (optional)
            if (chunk.Metadata.ContainsKey("original_id"))
            {
                point.Payload["original_id"] = chunk.Metadata["original_id"].ToString() ?? "";
            }

            // Add metadata
            foreach (var kvp in chunk.Metadata)
            {
                if (kvp.Key != "original_id") // Skip if already added
                {
                    point.Payload[$"meta_{kvp.Key}"] = kvp.Value.ToString() ?? "";
                }
            }

            return point;
        }

        private SearchResult MapToSearchResult(ScoredPoint point)
        {
            var result = new SearchResult
            {
                Id = point.Id.Uuid,  // Now this is a valid GUID
                Score = point.Score,
                DocumentId = point.Payload.GetValueOrDefault("document_id")?.StringValue ?? "",
                Text = point.Payload.GetValueOrDefault("text")?.StringValue ?? "",
                Index = (int)(point.Payload.GetValueOrDefault("index")?.IntegerValue ?? 0)
            };

            // Use original_id if available for display
            if (point.Payload.TryGetValue("original_id", out var originalId))
            {
                result.Metadata["original_id"] = originalId.StringValue ?? "";
            }

            // Extract other metadata
            foreach (var kvp in point.Payload)
            {
                if (kvp.Key.StartsWith("meta_"))
                {
                    var key = kvp.Key[5..]; // Remove "meta_" prefix
                    result.Metadata[key] = kvp.Value.StringValue ?? kvp.Value.IntegerValue.ToString();
                }
            }

            return result;
        }

        private Filter BuildFilter(Dictionary<string, object> filters)
        {
            var conditions = new List<Condition>();

            foreach (var filter in filters)
            {
                var condition = new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = filter.Key,
                        Match = new Match
                        {
                            Keyword = filter.Value.ToString()
                        }
                    }
                };
                conditions.Add(condition);
            }

            return new Filter
            {
                Must = { conditions }
            };
        }

        #endregion
    }
}

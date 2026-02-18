using Day_5___Document_Processing___Chunking.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_5___Document_Processing___Chunking.Services
{
    public interface IDocumentChunkingService
    {
        /// <summary>
        /// Splits text into overlapping chunks for better retrieval
        /// </summary>
        List<DocumentChunk> ChunkText(string text, string? documentId = null, ChunkingOptions? options = null);

        /// <summary>
        /// Reads and chunks a file from disk
        /// </summary>
        Task<List<DocumentChunk>> ChunkFileAsync(string filePath, ChunkingOptions? options = null);

        /// <summary>
        /// Chunks a document with metadata
        /// </summary>
        List<DocumentChunk> ChunkDocument(Document document, ChunkingOptions? options = null);
    }
}

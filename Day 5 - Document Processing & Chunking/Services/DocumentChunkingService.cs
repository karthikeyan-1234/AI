// Services/DocumentChunkingService.cs
using Day_5___Document_Processing___Chunking.Models;

using Microsoft.Extensions.Logging;

using System.Text;
using System.Text.RegularExpressions;

namespace Day_5___Document_Processing___Chunking.Services
{
    public class DocumentChunkingService : IDocumentChunkingService
    {
        private readonly ILogger<DocumentChunkingService>? _logger;

        public DocumentChunkingService(ILogger<DocumentChunkingService>? logger = null)
        {
            _logger = logger;
        }

        public List<DocumentChunk> ChunkText(string text, string? documentId = null, ChunkingOptions? options = null)
        {
            options ??= new ChunkingOptions();
            documentId ??= Guid.NewGuid().ToString();

            _logger?.LogInformation("Chunking text of length {Length} with strategy {Strategy}",
                text.Length, options.Strategy);

            var chunks = options.Strategy switch
            {
                ChunkingStrategy.Paragraph => ChunkByParagraph(text, options),
                ChunkingStrategy.Sentence => ChunkBySentence(text, options),
                ChunkingStrategy.Token => ChunkByToken(text, options),
                ChunkingStrategy.Fixed => ChunkByFixedSize(text, options),
                _ => ChunkByParagraph(text, options)
            };

            // Assign IDs and metadata
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Id = $"{documentId}_{i}";
                chunks[i].DocumentId = documentId;
                chunks[i].Index = i;

                if (options.IncludeMetadata)
                {
                    chunks[i].Metadata["chunk_size"] = chunks[i].Text.Length;
                    chunks[i].Metadata["word_count"] = CountWords(chunks[i].Text);
                    chunks[i].Metadata["strategy"] = options.Strategy.ToString();
                    chunks[i].Metadata["document_id"] = documentId;
                }
            }

            _logger?.LogInformation("Created {Count} chunks", chunks.Count);
            return chunks;
        }

        public async Task<List<DocumentChunk>> ChunkFileAsync(string filePath, ChunkingOptions? options = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var content = await ReadFileContentAsync(filePath, extension);
            var documentId = Path.GetFileName(filePath);

            var chunks = ChunkText(content, documentId, options);

            // Add file-specific metadata
            foreach (var chunk in chunks)
            {
                chunk.Metadata["filename"] = Path.GetFileName(filePath);
                chunk.Metadata["filetype"] = extension;
                chunk.Metadata["filesize"] = new FileInfo(filePath).Length;
            }

            return chunks;
        }

        public List<DocumentChunk> ChunkDocument(Document document, ChunkingOptions? options = null)
        {
            var chunks = ChunkText(document.Content, document.Id, options);

            foreach (var chunk in chunks)
            {
                chunk.Metadata["title"] = document.Title;
                chunk.Metadata["content_type"] = document.ContentType;

                // Add all document metadata with prefix
                foreach (var kvp in document.Metadata)
                {
                    chunk.Metadata[$"doc_{kvp.Key}"] = kvp.Value;
                }
            }

            return chunks;
        }

        #region Chunking Strategies

        private List<DocumentChunk> ChunkByParagraph(string text, ChunkingOptions options)
        {
            // Split by paragraphs (double newlines)
            var paragraphs = Regex.Split(text, @"\n\s*\n")
                                   .Where(p => !string.IsNullOrWhiteSpace(p))
                                   .ToList();

            if (paragraphs.Count == 0)
                paragraphs = new List<string> { text };

            var chunks = new List<DocumentChunk>();
            var currentChunk = new StringBuilder();
            var currentSize = 0;

            foreach (var para in paragraphs)
            {
                var paraSize = para.Length;

                // If single paragraph is too big, split it further
                if (paraSize > options.ChunkSize)
                {
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(new DocumentChunk { Text = currentChunk.ToString() });
                        currentChunk.Clear();
                        currentSize = 0;
                    }

                    // Split large paragraph into sentences
                    var subChunks = ChunkBySentence(para, options);
                    chunks.AddRange(subChunks);
                    continue;
                }

                // If adding this paragraph would exceed chunk size, save current and start new
                if (currentSize + paraSize > options.ChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(new DocumentChunk { Text = currentChunk.ToString() });
                    currentChunk.Clear();

                    // Add overlap from previous chunk
                    if (options.Overlap > 0 && chunks.Count > 0)
                    {
                        var lastChunk = chunks.Last().Text;
                        var overlapText = GetOverlapText(lastChunk, options.Overlap);
                        currentChunk.Append(overlapText);
                        currentSize = overlapText.Length;
                    }
                }

                currentChunk.Append(para).Append("\n\n");
                currentSize += paraSize + 2;
            }

            if (currentChunk.Length > 0)
                chunks.Add(new DocumentChunk { Text = currentChunk.ToString() });

            return chunks;
        }

        private List<DocumentChunk> ChunkBySentence(string text, ChunkingOptions options)
        {
            // Simple sentence splitting
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                                 .Where(s => !string.IsNullOrWhiteSpace(s))
                                 .ToList();

            if (sentences.Count == 0)
                sentences = new List<string> { text };

            var chunks = new List<DocumentChunk>();
            var currentChunk = new StringBuilder();
            var currentSize = 0;

            foreach (var sentence in sentences)
            {
                var sentenceSize = sentence.Length;

                if (sentenceSize > options.ChunkSize)
                {
                    // Sentence too long, split by words
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(new DocumentChunk { Text = currentChunk.ToString() });
                        currentChunk.Clear();
                        currentSize = 0;
                    }

                    var words = sentence.Split(' ');
                    var tempChunk = new List<string>();
                    var tempSize = 0;

                    foreach (var word in words)
                    {
                        if (tempSize + word.Length + 1 > options.ChunkSize && tempChunk.Any())
                        {
                            chunks.Add(new DocumentChunk { Text = string.Join(" ", tempChunk) });
                            tempChunk.Clear();
                            tempSize = 0;
                        }
                        tempChunk.Add(word);
                        tempSize += word.Length + 1;
                    }

                    if (tempChunk.Any())
                        chunks.Add(new DocumentChunk { Text = string.Join(" ", tempChunk) });

                    continue;
                }

                if (currentSize + sentenceSize > options.ChunkSize && currentChunk.Length > 0)
                {
                    chunks.Add(new DocumentChunk { Text = currentChunk.ToString() });
                    currentChunk.Clear();

                    // Add overlap
                    if (options.Overlap > 0 && chunks.Count > 0)
                    {
                        var lastChunk = chunks.Last().Text;
                        currentChunk.Append(GetOverlapText(lastChunk, options.Overlap));
                        currentSize = currentChunk.Length;
                    }
                }

                currentChunk.Append(sentence).Append(' ');
                currentSize += sentenceSize + 1;
            }

            if (currentChunk.Length > 0)
                chunks.Add(new DocumentChunk { Text = currentChunk.ToString() });

            return chunks;
        }

        private List<DocumentChunk> ChunkByToken(string text, ChunkingOptions options)
        {
            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<DocumentChunk>();

            for (int i = 0; i < words.Length; i += options.ChunkSize - options.Overlap)
            {
                var chunkWords = words.Skip(i).Take(options.ChunkSize);
                var chunkText = string.Join(" ", chunkWords);

                if (!string.IsNullOrWhiteSpace(chunkText))
                    chunks.Add(new DocumentChunk { Text = chunkText });
            }

            return chunks;
        }

        private List<DocumentChunk> ChunkByFixedSize(string text, ChunkingOptions options)
        {
            var chunks = new List<DocumentChunk>();

            for (int i = 0; i < text.Length; i += options.ChunkSize - options.Overlap)
            {
                var length = Math.Min(options.ChunkSize, text.Length - i);
                var chunkText = text.Substring(i, length);

                if (!string.IsNullOrWhiteSpace(chunkText))
                    chunks.Add(new DocumentChunk { Text = chunkText });
            }

            return chunks;
        }

        #endregion

        #region Helper Methods

        private async Task<string> ReadFileContentAsync(string filePath, string extension)
        {
            return extension switch
            {
                ".txt" or ".md" or ".json" => await File.ReadAllTextAsync(filePath),
                ".pdf" => await ExtractPdfTextAsync(filePath),
                ".docx" => await ExtractDocxTextAsync(filePath),
                _ => throw new NotSupportedException($"File type {extension} not supported")
            };
        }

        private async Task<string> ExtractPdfTextAsync(string filePath)
        {
            try
            {
                // Using PdfPig
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath);
                var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
                return await Task.FromResult(text);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to extract PDF text from {FilePath}", filePath);
                return string.Empty;
            }
        }

        private async Task<string> ExtractDocxTextAsync(string filePath)
        {
            try
            {
                // Using DocumentFormat.OpenXml
                using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
                var body = wordDoc.MainDocumentPart?.Document.Body;
                var text = body?.InnerText ?? string.Empty;
                return await Task.FromResult(text);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to extract DOCX text from {FilePath}", filePath);
                return string.Empty;
            }
        }

        private string GetOverlapText(string text, int overlapSize)
        {
            if (overlapSize <= 0 || string.IsNullOrEmpty(text))
                return string.Empty;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var overlapWords = words.TakeLast(Math.Min(overlapSize, words.Length));
            return string.Join(" ", overlapWords);
        }

        private int CountWords(string text)
        {
            return text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        #endregion
    }
}
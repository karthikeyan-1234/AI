// Program.cs
using Day_5___Document_Processing___Chunking.Models;
using Day_5___Document_Processing___Chunking.Services;

using Microsoft.Extensions.Logging;

using System.Text;

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger<DocumentChunkingService>();

// Create chunking service
var chunkingService = new DocumentChunkingService(logger);

// Helper for safe string preview
static string SafePreview(string text, int maxLength = 100)
{
    if (string.IsNullOrEmpty(text)) return string.Empty;
    var trimmed = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "...";
}

// Test 1: Basic text chunking
Console.WriteLine("\n📄 Test 1: Basic Text Chunking");
Console.WriteLine("===============================");

var sampleText = @"
Semantic Kernel is a lightweight, open-source SDK that lets you easily combine AI services 
like OpenAI, Azure OpenAI, and Hugging Face with conventional programming languages like C# 
and Python. It's designed to help developers build AI applications that combine the best of 
both worlds: the reasoning capabilities of large language models and the deterministic 
behavior of traditional code.

One of the key features of Semantic Kernel is its plugin architecture. Plugins allow you to 
encapsulate existing APIs and services into functions that can be called by the AI. For 
example, you might create a plugin that queries your database, calls a REST API, or performs 
complex calculations.

The SDK also includes planners, which can dynamically orchestrate multiple functions to 
accomplish complex tasks. Given a user's goal, the planner can create and execute a step-by-
step plan using available plugins.

Memory and embeddings are another crucial component. Semantic Kernel can store and retrieve 
information using vector databases, enabling RAG (Retrieval Augmented Generation) patterns. 
This allows the AI to ground its responses in your specific data.

Finally, Semantic Kernel provides a unified programming model across different AI providers. 
Whether you're using OpenAI, Azure OpenAI, or local models via Ollama, your code remains the 
same - only the configuration changes.";

var options = new ChunkingOptions
{
    ChunkSize = 300,
    Overlap = 50,
    Strategy = ChunkingStrategy.Paragraph,
    IncludeMetadata = true
};

var chunks = chunkingService.ChunkText(sampleText, "doc1", options);

Console.WriteLine($"\nCreated {chunks.Count} chunks:\n");
foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.Index}:");
    Console.WriteLine($"  Text: {SafePreview(chunk.Text, 80)}");
    Console.WriteLine($"  Size: {chunk.Text.Length} chars, Words: {chunk.Metadata["word_count"]}");
    Console.WriteLine();
}

// Test 2: Different strategies
Console.WriteLine("\n📄 Test 2: Comparing Chunking Strategies");
Console.WriteLine("=========================================");

var strategies = new[] { ChunkingStrategy.Paragraph, ChunkingStrategy.Sentence, ChunkingStrategy.Token };

foreach (var strategy in strategies)
{
    var testOptions = new ChunkingOptions
    {
        Strategy = strategy,
        ChunkSize = 200,
        Overlap = 30
    };

    var strategyChunks = chunkingService.ChunkText(sampleText, "test", testOptions);
    Console.WriteLine($"{strategy}: {strategyChunks.Count} chunks");
}

// Test 3: Create a document for RAG
Console.WriteLine("\n📄 Test 3: Prepare for RAG");
Console.WriteLine("===========================");

var document = new Document
{
    Id = "manufacturing-doc-1",
    Title = "Krones ErgoBloc L Overview",
    Content = @"
The Krones ErgoBloc L is a state-of-the-art packaging machine for the beverage industry. 
It combines filling, capping, and labeling in a single, compact unit. The machine can handle 
up to 80,000 bottles per hour, making it ideal for high-volume production lines.

Key features include modular design for easy maintenance, servo-driven technology for precise 
control, and integrated CIP (Clean-in-Place) systems for hygienic operation. The machine 
supports various container types including glass, PET, and cans.

The ErgoBloc L is part of Krones' Industry 4.0 initiative, featuring IoT connectivity for 
predictive maintenance and real-time production monitoring. It integrates seamlessly with 
MES (Manufacturing Execution Systems) and ERP systems.",
    ContentType = "text/plain",
    Metadata = new Dictionary<string, object>
    {
        ["author"] = "Technical Documentation Team",
        ["department"] = "Manufacturing",
        ["version"] = "2.1",
        ["last_updated"] = "2024-01-15"
    }
};

var docChunks = chunkingService.ChunkDocument(document, new ChunkingOptions { ChunkSize = 150 });

Console.WriteLine($"Document '{document.Title}' split into {docChunks.Count} chunks:");
foreach (var chunk in docChunks)
{
    var preview = SafePreview(chunk.Text, 60);
    Console.WriteLine($"  [{chunk.Index}] {preview}");

    // Display metadata safely
    if (chunk.Metadata.TryGetValue("doc_version", out var version))
        Console.WriteLine($"      Version: {version}");
    else if (chunk.Metadata.TryGetValue("version", out var ver))
        Console.WriteLine($"      Version: {ver}");

    if (chunk.Metadata.TryGetValue("doc_author", out var author))
        Console.WriteLine($"      Author: {author}");
}

// Test 4: File processing
Console.WriteLine("\n📄 Test 4: File Processing");
Console.WriteLine("==========================");

// Create a sample file if it doesn't exist
var sampleFilePath = "sample.txt";
if (!File.Exists(sampleFilePath))
{
    await File.WriteAllTextAsync(sampleFilePath, sampleText);
    Console.WriteLine($"Created {sampleFilePath}");
}

try
{
    var fileChunks = await chunkingService.ChunkFileAsync(sampleFilePath);
    Console.WriteLine($"Processed {sampleFilePath}: {fileChunks.Count} chunks");

    foreach (var chunk in fileChunks.Take(3))
    {
        var preview = SafePreview(chunk.Text, 60);
        Console.WriteLine($"  [{chunk.Index}] {preview}");
    }

    if (fileChunks.Count > 3)
        Console.WriteLine($"  ... and {fileChunks.Count - 3} more chunks");
}
catch (Exception ex)
{
    Console.WriteLine($"Error processing file: {ex.Message}");
}

// Test 5: Custom chunking demonstration
Console.WriteLine("\n📄 Test 5: Custom Chunking Demo");
Console.WriteLine("===============================");

var customText = "This is a short document. It has multiple sentences. But it's not very long. We'll see how chunking works with different strategies.";

var customOptions = new[]
{
    new { Strategy = ChunkingStrategy.Paragraph, Name = "Paragraph" },
    new { Strategy = ChunkingStrategy.Sentence, Name = "Sentence" },
    new { Strategy = ChunkingStrategy.Token, Name = "Token" },
    new { Strategy = ChunkingStrategy.Fixed, Name = "Fixed" }
};

foreach (var opt in customOptions)
{
    var testOptions = new ChunkingOptions
    {
        Strategy = opt.Strategy,
        ChunkSize = 30,
        Overlap = 5
    };

    var customChunks = chunkingService.ChunkText(customText, "custom", testOptions);
    Console.WriteLine($"\n{opt.Name} Strategy ({customChunks.Count} chunks):");
    foreach (var chunk in customChunks)
    {
        Console.WriteLine($"  [{chunk.Index}] {SafePreview(chunk.Text, 40)}");
    }
}

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("✅ Day 5 Complete! Document chunking is ready for RAG pipeline.");
Console.WriteLine("=".PadRight(60, '='));

// Summary
Console.WriteLine("\n📊 Summary of what you built today:");
Console.WriteLine("  • Document chunking with multiple strategies");
Console.WriteLine("  • Paragraph, Sentence, Token, and Fixed-size chunking");
Console.WriteLine("  • Overlap support for context preservation");
Console.WriteLine("  • Metadata propagation from documents to chunks");
Console.WriteLine("  • File processing for TXT, PDF, and DOCX");
Console.WriteLine("  • Clean architecture ready for RAG pipeline");
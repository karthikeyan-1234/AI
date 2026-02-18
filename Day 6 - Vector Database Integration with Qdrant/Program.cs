// Program.cs

using Day_6___Vector_Database_Integration_with_Qdrant.Models;
using Day_6___Vector_Database_Integration_with_Qdrant.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

#region Setup DI and Logging
// Setup DI and logging
var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
#endregion

#region Register Services
// Register your services (from Day 4 & 5)
services.AddSingleton<KernelSettings>(sp => new KernelSettings
{
    Provider = "ollama",
    Endpoint = "http://192.168.1.150:11434/v1",
    EmbeddingModel = "nomic-embed-text",
    ChatModel = "llama3"
});

services.AddSingleton<Kernel>(sp => KernelFactory.Create(sp.GetRequiredService<KernelSettings>()));
services.AddSingleton<IEmbeddingGenerationService, SKEmbeddingGenerationService>();
services.AddSingleton<IResponseGenerationService, SKResponseGenerationService>();
services.AddSingleton<IDocumentChunkingService, DocumentChunkingService>();

// Register Qdrant service
services.AddSingleton<IVectorStoreService>(sp =>
    new QdrantVectorStoreService(
        host: "localhost",
        port: 6334,
        logger: sp.GetService<ILogger<QdrantVectorStoreService>>()
    ));

// Register RAG pipeline
services.AddSingleton<RagPipelineService>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var ragPipeline = serviceProvider.GetRequiredService<RagPipelineService>();
#endregion

try
{
    #region Print Header
    Console.WriteLine("\n🔷 DAY 6: QDRANT VECTOR DATABASE INTEGRATION");
    Console.WriteLine("==============================================\n");
    #endregion

    #region 1. Create Sample Documents
    // 1. Create sample documents
    Console.WriteLine("📄 Creating sample manufacturing documents...");

    var documents = new[]
    {
        new Document
        {
            Id = "krones-doc-1",
            Title = "Krones ErgoBloc L Technical Overview",
            Content = @"
The Krones ErgoBloc L is a high-performance packaging machine designed for the beverage industry.
It integrates filling, capping, and labeling operations into a single, compact unit.
The machine achieves speeds up to 80,000 containers per hour with exceptional precision.
Key features include servo-driven filling valves, automatic format changeovers, and CIP integration.
The modular design allows for easy maintenance and future upgrades.",
            Metadata = new Dictionary<string, object>
            {
                ["manufacturer"] = "Krones AG",
                ["category"] = "Packaging",
                ["year"] = "2024"
            }
        },
        new Document
        {
            Id = "fanuc-doc-1",
            Title = "Fanuc Robot M-710iC Specifications",
            Content = @"
The Fanuc Robot M-710iC is a 6-axis industrial robot designed for heavy-duty applications.
It offers a payload capacity of 70kg and a reach of 2050mm, making it ideal for spot welding.
The robot features the R-30iB controller with integrated vision guidance and force sensing.
Repeatability is ±0.07mm, ensuring high precision in manufacturing operations.
The IP67 rating allows operation in harsh environments with dust and water exposure.",
            Metadata = new Dictionary<string, object>
            {
                ["manufacturer"] = "Fanuc Corporation",
                ["category"] = "Robotics",
                ["year"] = "2023"
            }
        },
        new Document
        {
            Id = "siemens-doc-1",
            Title = "Siemens Sinumerik 840D CNC Controller",
            Content = @"
The Sinumerik 840D is a high-end CNC control system for complex machining applications.
It supports up to 31 axes and 10 channels for multi-task machining operations.
Advanced features include 5-axis simultaneous machining, 3D simulation, and Tool Center Point management.
The system integrates with CAD/CAM software through standard G-code and ISO programming.
ShopFloor programming allows operators to create programs directly on the machine.",
            Metadata = new Dictionary<string, object>
            {
                ["manufacturer"] = "Siemens AG",
                ["category"] = "CNC",
                ["year"] = "2024"
            }
        }
    };
    #endregion

    #region 2. Index Documents into Qdrant
    // 2. Index documents into Qdrant
    const string collectionName = "manufacturing_docs";

    Console.WriteLine($"\n🗂️  Indexing documents to collection '{collectionName}'...");

    // Delete existing collection for clean test
    var vectorStore = serviceProvider.GetRequiredService<IVectorStoreService>();
    if (await vectorStore.CollectionExistsAsync(collectionName))
    {
        await vectorStore.DeleteCollectionAsync(collectionName);
        Console.WriteLine("Deleted existing collection");
    }

    foreach (var doc in documents)
    {
        await ragPipeline.IndexDocumentAsync(collectionName, doc);
        Console.WriteLine($"  ✅ Indexed: {doc.Title}");
    }
    #endregion

    #region 3. Test Queries
    // 3. Test queries
    Console.WriteLine("\n🔍 Testing RAG Queries");
    Console.WriteLine("=======================\n");

    var testQueries = new[]
    {
        "What is the speed of the Krones packaging machine?",
        "Tell me about Fanuc robot capabilities",
        "What CNC features does Siemens offer?",
        "Which machines have high precision?",
        "What's the payload of the Fanuc robot?"
    };

    foreach (var query in testQueries)
    {
        Console.WriteLine($"\n📝 Query: {query}");
        Console.WriteLine(new string('-', 50));

        try
        {
            var answer = await ragPipeline.QueryAsync(collectionName, query, topK: 3);
            Console.WriteLine($"🤖 Answer: {answer}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }

        await Task.Delay(500); // Small delay between queries
    }
    #endregion

    #region 4. Test with Filters
    // 4. Test with filters
    Console.WriteLine("\n🔍 Testing Filtered Search");
    Console.WriteLine("===========================");

    var filterQueries = new[]
    {
        new { Query = "packaging machines", Filter = new Dictionary<string, object> { ["category"] = "Packaging" } },
        new { Query = "robotic systems", Filter = new Dictionary<string, object> { ["category"] = "Robotics" } },
        new { Query = "control systems", Filter = new Dictionary<string, object> { ["manufacturer"] = "Siemens AG" } }
    };

    foreach (var test in filterQueries)
    {
        Console.WriteLine($"\n📝 Query: {test.Query}");
        Console.WriteLine($"   Filter: {string.Join(", ", test.Filter.Select(kv => $"{kv.Key}={kv.Value}"))}");
        Console.WriteLine(new string('-', 50));

        try
        {
            var answer = await ragPipeline.QueryWithFilterAsync(collectionName, test.Query, test.Filter);
            Console.WriteLine($"🤖 Answer: {answer}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
    #endregion

    #region 5. Show Collection Stats
    // 5. Show collection stats
    Console.WriteLine("\n📊 Collection Statistics");
    Console.WriteLine("========================");

    var sampleSearch = await ragPipeline.QueryAsync(collectionName, "manufacturing equipment", topK: 2);
    Console.WriteLine($"Collection '{collectionName}' is ready with {documents.Length} documents");
    Console.WriteLine($"Sample search result: {sampleSearch[..Math.Min(100, sampleSearch.Length)]}...");

    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("✅ DAY 6 COMPLETE!");
    Console.WriteLine("   Vector database integration successful!");
    Console.WriteLine("   You now have a complete RAG pipeline with:");
    Console.WriteLine("   • Document chunking");
    Console.WriteLine("   • Embedding generation");
    Console.WriteLine("   • Qdrant vector storage");
    Console.WriteLine("   • Semantic search");
    Console.WriteLine("   • Context-aware generation");
    Console.WriteLine("=".PadRight(60, '='));
    #endregion
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Error in Day 6 test");
    Console.WriteLine($"\nTroubleshooting tips:");
    Console.WriteLine("1. Is Qdrant running? docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant");
    Console.WriteLine("2. Check Ollama connection");
    Console.WriteLine("3. Verify embedding model: ollama pull nomic-embed-text");
}

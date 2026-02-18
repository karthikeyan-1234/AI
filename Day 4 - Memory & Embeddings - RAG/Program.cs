// Program.cs - Simple RAG Test
using Day_4___Memory___Embeddings___RAG;
using Day_4___Memory___Embeddings___RAG.Services;

var settings = new KernelSettings
{
    Provider = "ollama",
    Endpoint = "http://192.168.1.150:11434/v1",
    EmbeddingModel = "nomic-embed-text",
    ChatModel = "llama3"
};

try
{
    Console.WriteLine("=== Simple RAG Test ===");

    // 1. Setup services
    Console.WriteLine("\n1. Setting up services...");
    var kernel = KernelFactory.Create(settings);
    var embeddingService = new SKEmbeddingGenerationService(kernel);
    var generationService = new SKTextGenerationService(kernel);
    Console.WriteLine("✅ Services ready");

    // 2. Test documents (simple manufacturing knowledge base)
    Console.WriteLine("\n2. Creating test knowledge base...");
    var documents = new[]
    {
        "The Krones ErgoBloc L is a packaging machine for beverages. It fills and packages bottles at high speed.",
        "Fanuc Robot M-710iC is an industrial robot used for welding and material handling in automotive factories.",
        "Siemens Sinumerik CNC controls are used for precision machining of metal parts with 5-axis capability.",
        "The Krones machine supports parallel operations, allowing multiple production lines to run simultaneously.",
        "Fanuc robots use R-30iB controllers and can handle payloads up to 70kg with 0.1mm repeatability.",
        "Siemens CNC systems support G-code programming and can interface with CAD/CAM software for complex parts."
    };

    Console.WriteLine($"Created {documents.Length} test documents");

    // 3. Generate embeddings for all documents
    Console.WriteLine("\n3. Generating embeddings for documents...");
    var documentEmbeddings = new List<IReadOnlyList<float>>();

    foreach (var doc in documents)
    {
        var embedding = await embeddingService.GenerateAsync(doc);
        documentEmbeddings.Add(embedding);
    }
    Console.WriteLine($"✅ Generated {documentEmbeddings.Count} embeddings");

    // 4. User query
    Console.WriteLine("\n4. Enter your query about manufacturing equipment:");
    Console.Write("> ");
    var query = Console.ReadLine() ?? "What is Krones ErgoBloc L?";
    Console.WriteLine($"Query: {query}");

    // 5. Generate embedding for query
    Console.WriteLine("\n5. Generating embedding for query...");
    var queryEmbedding = await embeddingService.GenerateAsync(query);
    Console.WriteLine($"✅ Query embedding: {queryEmbedding.Count} dimensions");

    // 6. Find most similar documents (simple vector similarity)
    Console.WriteLine("\n6. Finding relevant documents...");
    var similarities = new List<(string Document, float Score)>();

    for (int i = 0; i < documents.Length; i++)
    {
        var score = CosineSimilarity(queryEmbedding, documentEmbeddings[i]);
        similarities.Add((documents[i], score));
    }

    // Sort by similarity (highest first)
    var relevantDocs = similarities
        .OrderByDescending(s => s.Score)
        .Take(3)  // Top 3 most relevant
        .ToList();

    Console.WriteLine($"Top {relevantDocs.Count} relevant documents:");
    foreach (var (doc, score) in relevantDocs)
    {
        Console.WriteLine($"  [Score: {score:F3}] {doc.Substring(0, Math.Min(60, doc.Length))}...");
    }

    // 7. Build context from relevant documents
    Console.WriteLine("\n7. Building context for generation...");
    var context = string.Join("\n\n", relevantDocs.Select(r => r.Document));
    Console.WriteLine($"Context length: {context.Length} characters");

    // 8. Generate answer with context (simple RAG)
    Console.WriteLine("\n8. Generating answer...");
    var systemPrompt = @"You are a manufacturing expert. Answer the question using ONLY the provided context.
If the context doesn't contain the answer, say 'I don't have enough information in my knowledge base.'

Context:
" + context + @"

Question: " + query + @"

Answer:";

    var answer = await generationService.GenerateAsync(systemPrompt);

    Console.WriteLine("\n" + new string('=', 60));
    Console.WriteLine("🤖 ANSWER:");
    Console.WriteLine(answer);
    Console.WriteLine(new string('=', 60));

    // 9. Compare with direct answer (without context)
    Console.WriteLine("\n9. For comparison - Direct answer (no context):");
    var directAnswer = await generationService.GenerateAsync(query);
    Console.WriteLine(directAnswer);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

// Helper function for cosine similarity
static float CosineSimilarity(IReadOnlyList<float> vec1, IReadOnlyList<float> vec2)
{
    if (vec1.Count != vec2.Count)
        throw new ArgumentException("Vectors must have same dimensions");

    float dot = 0, mag1 = 0, mag2 = 0;
    for (int i = 0; i < vec1.Count; i++)
    {
        dot += vec1[i] * vec2[i];
        mag1 += vec1[i] * vec1[i];
        mag2 += vec2[i] * vec2[i];
    }

    if (mag1 == 0 || mag2 == 0) return 0;
    return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
}
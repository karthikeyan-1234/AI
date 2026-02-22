// Day 7 Section 2: Automatic Function Calling with Ollama (Updated for llama3.1:8b)
// Uses modern Semantic Kernel approach - FunctionCallingStepwisePlanner deprecated

using FunctionCallingStepwisePlanner;
using FunctionCallingStepwisePlanner.Plugins;  // Contains EcommercePlugin

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5) // Increased from default 100s for Ollama stability
};

var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, FunctionExecutionTraceFilter>();


var kernel = kernelBuilder
    .AddOpenAIChatCompletion(
        modelId: "llama3.1:8b",  // Switched from mistral-nemo (better tool calling support)
        endpoint: new Uri("http://192.168.1.150:11434/v1"),  // Local Ollama OpenAI-compatible API
        apiKey: "ollama",  // Required dummy key for OpenAI client
        httpClient: httpClient  // Custom client with longer timeout
        
    )
    .Build();


// Register EcommercePlugin (SearchByCategory, FilterByPrice, CheckStock, WithGoodRatings)
KernelPlugin ecommercePlugin = kernel.ImportPluginFromType<EcommercePlugin>();


// Add this to see which functions are being called
kernel.FunctionInvoking += (sender, e) =>
{
    Console.WriteLine($"⚡ Calling: {e.Function.Name}");
    foreach (var arg in e.Arguments)
    {
        Console.WriteLine($"   Arg: {arg.Key} = {arg.Value}");
    }
};

kernel.FunctionInvoked += (sender, e) =>
{
    Console.WriteLine($"✅ Completed: {e.Function.Name}");
    Console.WriteLine($"   Result: {e.Result}");
};

/*

0.0 ────────────── 0.5 ────────────── 1.0 ────────────── 2.0
 │                   │                  │                   │
Robotic Balanced           Creative            Chaotic
Deterministic      Sensible           Varied              Random

 */

// Configure Automatic Function Calling (replaces deprecated StepwisePlanner)
OpenAIPromptExecutionSettings executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.2f,  // For accurate results
    MaxTokens = 500,     // Reasonable limit for function results
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
    ChatSystemPrompt = "You must NEVER assume product IDs or ratings.\r\nYou must ALWAYS call Search first to obtain product IDs.\r\nThen call GetProductRating using returned IDs.\r\nUse only real function outputs for calculations."
};

while (true)
{
    Console.WriteLine("Enter your query about our products: ");

    var prompt = Console.ReadLine();

    int choice = 1;

    #region Test simple single-function query (matches SearchByCategory exactly)
    if (choice == 0)
    {
        FunctionResult result = await kernel.InvokePromptAsync(
            prompt!,  // Exact match for Category="Clothing" seed data
            new(executionSettings)
        );

        Console.WriteLine($"Auto Function Calling result: {result}");
    }
    #endregion

    #region Add streaming version for real-time responses
    if (choice == 1)
    {
        Console.WriteLine("\n🔄 Streaming response (auto function calling):");
        await foreach (var update in kernel.InvokePromptStreamingAsync(
            prompt!,
            new(executionSettings)))
        {
            Console.Write(update);
        }
        Console.WriteLine();
    }
    #endregion
}
// Expected: Calls SearchByCategory("Clothing") → Returns Red Shoes, Blue Shoes
// Progress: Day 9 Section 2 ✅ - Automatic Function Calling working with local Ollama

// Program.cs - Verified working with Semantic Kernel v1.x
// This program demonstrates basic Semantic Kernel setup with Ollama

// Import necessary namespaces for Semantic Kernel functionality
using Microsoft.SemanticKernel;                    // Core SK classes (Kernel, KernelBuilder)
using Microsoft.SemanticKernel.ChatCompletion;    // Chat-related interfaces and classes
using Microsoft.SemanticKernel.Connectors.OpenAI; // OpenAI connector classes

// CORRECT: Simple, working Ollama setup configuration
// Create a KernelBuilder instance to configure and build the Kernel
var builder = Kernel.CreateBuilder();

// CORRECT: Current SK v1.x pattern for OpenAI-compatible endpoints
// Configure Ollama as an OpenAI-compatible chat completion service
builder.AddOpenAIChatCompletion(
    modelId: "llama3",                               // Specify which LLM model to use (Ollama model name)
    endpoint: new Uri("http://192.168.1.150:11434/v1"), // Ollama server endpoint (OpenAI-compatible API)
    apiKey: "ollama"                                 // API key (Ollama ignores this, but SK requires a non-empty string)
);

// Build the Kernel instance with all configured services
// The Kernel acts as a container for AI services, plugins, and memory
var kernel = builder.Build();

// Retrieve the chat completion service from the Kernel's dependency injection container
// IChatCompletionService is the abstraction layer for different LLM providers
var chatService = kernel.GetRequiredService<IChatCompletionService>();

// Create a ChatHistory object to manage the conversation context
// ChatHistory maintains the message sequence (system, user, assistant messages)
var chatHistory = new ChatHistory();

// Add a system message to set the assistant's behavior and context
// System messages guide how the AI should respond (role, tone, expertise)
chatHistory.AddSystemMessage("You are a helpful assistant for .NET developers.");

// Add a user message containing the actual query/request
// This is the input prompt that the AI will process and respond to
chatHistory.AddUserMessage("Explain Semantic Kernel in one paragraph for experienced .NET developers.");

// Output status message to console to indicate the program is running
Console.WriteLine("🤖 Calling Ollama (Llama3)...\n");

// Wrap LLM call in try-catch to handle potential connection/API errors gracefully
try
{
    // Call the LLM through Semantic Kernel to get a response
    // GetChatMessageContentAsync sends the chat history to the configured LLM service
    var response = await chatService.GetChatMessageContentAsync(
        chatHistory,  // Pass the conversation history (system + user messages)
        executionSettings: new OpenAIPromptExecutionSettings  // Configure LLM generation parameters
        {
            Temperature = 0.7,   // Controls randomness (0.0 = deterministic, 1.0 = creative)
            MaxTokens = 500      // Limit response length to approximately 500 tokens
        },
        kernel: kernel           // Pass the Kernel context (enables function calling if needed)
    );

    // Display the AI's response content to the console
    // response.Content contains the generated text from the LLM
    Console.WriteLine($"📝 Response:\n{response.Content}\n");
}
catch (HttpRequestException ex)  // Catch network/connection-related errors
{
    // Handle connection failures (Ollama not running, wrong IP/port, network issues)
    Console.WriteLine($"❌ Connection error: {ex.Message}");

    // Provide troubleshooting guidance for common Ollama setup issues
    Console.WriteLine("Make sure Ollama is running: `ollama serve`");
    Console.WriteLine("And you have Llama3: `ollama pull llama3`");
}
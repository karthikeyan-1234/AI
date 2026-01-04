using Day_3___Native_C__Plugins;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;


var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.AddOpenAIChatCompletion(
    modelId: "mistral-nemo:latest",
    endpoint: new Uri("http://192.168.1.150:11434/v1"),
    apiKey: "ollama"
);


var kernel = kernelBuilder.Build();
kernel.Plugins.AddFromType<MachinePlugin>();


var chatService = kernel.GetRequiredService<IChatCompletionService>();
var chatHistory = new ChatHistory();

chatHistory.AddUserMessage("What is the status of Krones ErgoBloc L?");

try
{
    var response = await chatService.GetChatMessageContentAsync(
        chatHistory,
        executionSettings: new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 500,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        },
        kernel: kernel

    );
    Console.WriteLine($"📝 Response:\n{response.Content}\n");
}
catch (HttpRequestException ex) 
{
    Console.WriteLine($"❌ Connection error: {ex.Message}");
    Console.WriteLine("Make sure Ollama is running: `ollama serve`");
    Console.WriteLine("And you have Llama3: `ollama pull llama3`");
}




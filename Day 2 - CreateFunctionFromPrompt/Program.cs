using Microsoft.SemanticKernel;                    
using Microsoft.SemanticKernel.ChatCompletion;    
using Microsoft.SemanticKernel.Connectors.OpenAI; 


var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.AddOpenAIChatCompletion(
    modelId: "llama3",                               // Specify which LLM model to use (Ollama model name)
    endpoint: new Uri("http://192.168.1.150:11434/v1"), // Ollama server endpoint (OpenAI-compatible API)
    apiKey: "ollama"                                 // API key (Ollama ignores this, but SK requires a non-empty string)
);


var kernel = kernelBuilder.Build();

var prompt = """                       
You are a MES domain expert.
Given machine name: {{$machine}}
Explain what it does.
""";

var function = kernel.CreateFunctionFromPrompt(prompt);     //This is not a reference to C#/.Net function. This is kernel function
                                                            
var result = await kernel.InvokeAsync(function, new() { ["machine"] = "Krones ErgoBloc L" });   //Here {{$machine}} in original
                                                                                                //prompt gets replaced by "Krones ErgoBloc L"

Console.WriteLine(result);
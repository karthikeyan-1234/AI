using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Models
{
    public sealed class KernelSettings
    {
        public string Provider { get; init; } = "ollama";
        public string Endpoint { get; init; } = "http://localhost:11434/v1";
        public string EmbeddingModel { get; init; } = "nomic-embed-text";
        public string ChatModel { get; init; } = "llama3";

        public string ApiKey { get; init; } = "apikey";
    }

}

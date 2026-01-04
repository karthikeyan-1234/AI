using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_4___Memory___Embeddings___RAG.Services
{
    public interface ITextGenerationService
    {
        Task<string> GenerateAsync(string prompt,CancellationToken ct = default);
    }

}

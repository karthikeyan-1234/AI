using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    public interface IResponseGenerationService
    {
        Task<string> GenerateAsync(string prompt,CancellationToken ct = default);
    }

}

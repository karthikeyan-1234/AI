using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Models
{
    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Use actual GUID
        public string DocumentId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Index { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Helper to create consistent IDs
        public static string CreateValidId(string documentId, int index)
        {
            // Create a deterministic GUID from documentId and index
            using var md5 = System.Security.Cryptography.MD5.Create();
            var input = $"{documentId}_{index}";
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash).ToString();
        }

        public override string ToString() => $"Chunk {Index}: {Text[..Math.Min(50, Text.Length)]}...";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_5___Document_Processing___Chunking.Models
{
    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DocumentId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Index { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // For debugging/tracking
        public override string ToString() => $"Chunk {Index}: {Text[..Math.Min(50, Text.Length)]}...";
    }
}

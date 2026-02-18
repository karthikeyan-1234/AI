using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Models
{
    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Index { get; set; }
        public double Score { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public override string ToString() => $"[Score: {Score:F3}] {Text[..Math.Min(60, Text.Length)]}...";
    }
}

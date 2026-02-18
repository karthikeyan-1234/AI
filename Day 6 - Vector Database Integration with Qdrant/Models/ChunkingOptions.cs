using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Day_6___Vector_Database_Integration_with_Qdrant.Services
{
    public enum ChunkingStrategy
    {
        Paragraph,      // Split by paragraphs first, then size
        Sentence,       // Split by sentences
        Token,          // Split by token count (simple)
        Semantic,       // Semantic boundaries (advanced)
        Fixed           // Fixed size regardless of content
    }

    public class ChunkingOptions
    {
        public int ChunkSize { get; set; } = 500;      // Target chunk size in tokens/words
        public int Overlap { get; set; } = 50;         // Overlap between chunks
        public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.Paragraph;
        public bool IncludeMetadata { get; set; } = true;
    }
}

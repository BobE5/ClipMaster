using System;

namespace ClipMaster.Models
{
    public class RegexPattern
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsGlobal { get; set; } = true;  // Replace all occurrences
        public bool IsCaseSensitive { get; set; } = true;
        public bool IsMultiline { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUsedAt { get; set; } = DateTime.Now;
        public int UseCount { get; set; } = 0;
    }
}

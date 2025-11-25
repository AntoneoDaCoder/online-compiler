using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Contracts.DTOs
{
    public class EditorProblemVersionDto
    {
        public Guid ProblemId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public string Statement { get; set; }
        public int TotalTests { get; set; }
        public ICollection<string> SupportedLanguages { get; set; } = new List<string>();
        public ManifestDto? TestManifest { get; set; }
    }
}

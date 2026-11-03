using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Infrastructure.FileFormats.QuizPack
{
    public class QuizPackManifest
    {
        public string Format { get; init; } = "quizpack";

        public int SchemaVersion { get; init; }

        public Guid ExamId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string ExamFile { get; init; } = "exam.json";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Infrastructure.FileFormats.QuizPack
{
    public class QuizPackQuestion
    {
        public Guid Id { get; init; }

        public string Content { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public int Points { get; init; }

        public string? Image { get; init; }

        public List<QuizPackOption> Options { get; init; } = [];

        public List<Guid> CorrectOptionIds { get; init; } = [];
    }

    public class QuizPackOption
    {
        public Guid Id { get; init; }

        public string Content { get; init; } = string.Empty;
    }
}

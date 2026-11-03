using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Infrastructure.FileFormats.QuizPack
{
    public class QuizPackDocument
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public int DurationSeconds { get; init; }

        public List<QuizPackQuestion> Questions { get; init; } = [];
    }
}

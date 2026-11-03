using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuizApp.Domain.Enums;

namespace QuizApp.Domain.Models
{
    public class Question
    {
        public Guid Id { get; init; }

        public string? ImagePath { get; init; }

        public string Content { get; init; } = string.Empty;

        public QuestionType Type { get; init; }

        public int Points { get; init; }

        public IReadOnlyList<QuestionOption> Options { get; init; }
            = new List<QuestionOption>();

        public IReadOnlyList<Guid> CorrectOptionIds { get; init; }
            = new List<Guid>();
    }
}

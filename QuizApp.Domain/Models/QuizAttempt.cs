using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuizApp.Domain.Enums;

namespace QuizApp.Domain.Models
{

    public class QuizAttempt
    {
        public Guid Id { get; init; }

        public Guid QuizId { get; init; }

        public string ExamContentHash { get; init; } = string.Empty;

        public DateTime StartedAtUtc { get; init; }

        public DateTime DeadlineUtc { get; init; }

        public DateTime? SubmittedAtUtc { get; init; }

        public AttemptStatus Status { get; init; }

        public IReadOnlyList<Answer> Answers { get; init; }
            = new List<Answer>();
    }
}

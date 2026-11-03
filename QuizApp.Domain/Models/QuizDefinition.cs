using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Domain.Models
{
    public class QuizDefinition
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public int DurationSeconds { get; init; }

        public IReadOnlyList<Question> Questions { get; init; }
            = new List<Question>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Domain.Models
{
    public class Answer
    {
        public Guid QuestionId { get; init; }

        public IReadOnlyList<Guid> SelectedOptionIds { get; init; }
            = new List<Guid>();

        public DateTime AnsweredAtUtc { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Domain.Models
{
    public class QuizResult
    {
        public Guid AttemptId { get; init; }

        public int TotalQuestions { get; init; }

        public int CorrectAnswers { get; init; }

        public double EarnedPoints { get; init; }

        public double MaximumPoints { get; init; }

        public double ScorePercentage { get; init; }
    }
}

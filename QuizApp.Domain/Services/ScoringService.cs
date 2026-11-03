using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuizApp.Domain.Models;

namespace QuizApp.Domain.Services
{
    public class ScoringService
    {
        public QuizResult Calculate(
            QuizDefinition quiz,
            QuizAttempt attempt)
        {
            ArgumentNullException.ThrowIfNull(quiz);
            ArgumentNullException.ThrowIfNull(attempt);

            if (quiz.Id != attempt.QuizId)
            {
                throw new ArgumentException(
                    "The attempt does not belong to this quiz.",
                    nameof(attempt));
            }

            var answersByQuestionId = attempt.Answers
                .ToDictionary(answer => answer.QuestionId);

            int totalQuestions = quiz.Questions.Count;
            int correctAnswers = 0;

            double earnedPoints = 0;
            double maximumPoints = 0;

            foreach (var question in quiz.Questions)
            {
                maximumPoints += question.Points;

                if (!answersByQuestionId.TryGetValue(
                    question.Id, out var answer))
                {
                    continue;
                }

                var selectedIds = answer.SelectedOptionIds.ToHashSet();
                var correctIds = question.CorrectOptionIds.ToHashSet();

                if (selectedIds.SetEquals(correctIds))
                {
                    correctAnswers++;
                    earnedPoints += question.Points;
                }
            }

            double scorePercentage = maximumPoints > 0
                ? earnedPoints / maximumPoints * 100
                : 0;

            return new QuizResult
            {
                AttemptId = attempt.Id,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                EarnedPoints = earnedPoints,
                MaximumPoints = maximumPoints,
                ScorePercentage = scorePercentage
            };
        }
    }
}

namespace QuizApp.Application.Exceptions;

public class QuizContentMismatchException : Exception
{
    public QuizContentMismatchException()
        : base("The quiz content does not match the content used by this attempt.")
    {
    }
}
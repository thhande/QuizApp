namespace QuizApp.Application.Exceptions;

public class AttemptNotFoundException : Exception
{
    public AttemptNotFoundException(Guid attemptId)
        : base($"Quiz attempt '{attemptId}' was not found.")
    {
    }
}
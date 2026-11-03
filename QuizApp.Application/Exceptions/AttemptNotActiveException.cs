namespace QuizApp.Application.Exceptions;

public class AttemptNotActiveException : Exception
{
    public AttemptNotActiveException(string message)
        : base(message)
    {
    }
}
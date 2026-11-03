namespace QuizApp.Application.Tests.Fakes;

public class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _currentTime;

    public FixedTimeProvider(DateTimeOffset initialTime)
    {
        _currentTime = initialTime;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _currentTime;
    }

    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}
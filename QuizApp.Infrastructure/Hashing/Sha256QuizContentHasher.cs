
using System.Security.Cryptography;
using QuizApp.Application.Abstractions;

namespace QuizApp.Infrastructure.Hashing;

public class Sha256QuizContentHasher : IQuizContentHasher
{
    public async Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous |
                     FileOptions.SequentialScan);

        var hashBytes = await SHA256.HashDataAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

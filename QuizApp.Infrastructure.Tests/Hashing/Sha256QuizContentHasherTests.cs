
using QuizApp.Infrastructure.Hashing;

namespace QuizApp.Infrastructure.Tests.Hashing;

public class Sha256QuizContentHasherTests
{
    private readonly Sha256QuizContentHasher _hasher = new();

    [Fact]
    public async Task ComputeHashAsync_SameContent_ReturnsSameHash()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(directory);

        try
        {
            var file1 = Path.Combine(directory, "quiz1.quizpack");
            var file2 = Path.Combine(directory, "quiz2.quizpack");

            await File.WriteAllTextAsync(file1, "Quiz content");
            await File.WriteAllTextAsync(file2, "Quiz content");

            var hash1 = await _hasher.ComputeHashAsync(file1);
            var hash2 = await _hasher.ComputeHashAsync(file2);

            Assert.Equal(hash1, hash2);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ComputeHashAsync_DifferentContent_ReturnsDifferentHash()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(directory);

        try
        {
            var file1 = Path.Combine(directory, "quiz1.quizpack");
            var file2 = Path.Combine(directory, "quiz2.quizpack");

            await File.WriteAllTextAsync(file1, "Quiz content A");
            await File.WriteAllTextAsync(file2, "Quiz content B");

            var hash1 = await _hasher.ComputeHashAsync(file1);
            var hash2 = await _hasher.ComputeHashAsync(file2);

            Assert.NotEqual(hash1, hash2);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ComputeHashAsync_Returns64CharacterHexString()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(directory);

        try
        {
            var filePath = Path.Combine(directory, "quiz.quizpack");

            await File.WriteAllTextAsync(filePath, "Quiz content");

            var hash = await _hasher.ComputeHashAsync(filePath);

            Assert.Equal(64, hash.Length);
            Assert.Matches("^[0-9a-f]{64}$", hash);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ComputeHashAsync_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}.quizpack");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _hasher.ComputeHashAsync(filePath));
    }

    [Fact]
    public async Task ComputeHashAsync_EmptyFile_ReturnsCorrectHash()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(directory);

        try
        {
            var filePath = Path.Combine(directory, "empty.quizpack");

            await File.WriteAllBytesAsync(filePath, []);

            var hash = await _hasher.ComputeHashAsync(filePath);

            Assert.Equal(
                "e3b0c44298fc1c149afbf4c8996fb924" +
                "27ae41e4649b934ca495991b7852b855",
                hash);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

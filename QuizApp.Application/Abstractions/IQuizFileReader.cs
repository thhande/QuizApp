using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using QuizApp.Domain.Models;

namespace QuizApp.Application.Abstractions;

public interface IQuizFileReader
{
    Task<QuizDefinition> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
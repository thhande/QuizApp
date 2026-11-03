using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QuizApp.Infrastructure.FileFormats.QuizPack
{
    public static class QuizPackJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            PropertyNameCaseInsensitive = true,

            WriteIndented = true,

            Converters =
        {
            new JsonStringEnumConverter()
        }
        };
    }
}

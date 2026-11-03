using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Infrastructure.FileFormats.QuizPack
{
    public class QuizPackReadException : Exception
    {
        public QuizPackReadException(string message)
            : base(message)
        {
        }

        public QuizPackReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

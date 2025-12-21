// Create a new file: FileNameSanitizer.cs
using System.Text;

namespace FluidSimu
{
    public static class FileNameSanitizer
    {
        private static readonly HashSet<char> _invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

        /// <summary>
        /// Cleans a string to be used as a valid filename by replacing invalid characters.
        /// </summary>
        /// <param name="input">The raw string, e.g., from a model name.</param>
        /// <param name="replacement">The character to use for replacing invalid characters.</param>
        /// <returns>A sanitized, safe-to-use filename string.</returns>
        public static string Sanitize(string input, char replacement = '_')
        {
            // 1. Handle null or empty input
            if (string.IsNullOrWhiteSpace(input))
            {
                return "untitled_model";
            }

            var sb = new StringBuilder(input.Length);
            bool wasReplaced = false;

            // 2. Iterate and replace invalid characters
            foreach (char c in input)
            {
                if (_invalidChars.Contains(c))
                {
                    // Avoid multiple consecutive replacement characters (e.g., "My___File" from "My/*:File")
                    if (!wasReplaced)
                    {
                        sb.Append(replacement);
                        wasReplaced = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    wasReplaced = false;
                }
            }

            // 3. Handle cases where the string is only invalid characters (e.g., "<>")
            string sanitized = sb.ToString();
            if (string.IsNullOrWhiteSpace(sanitized.Replace(replacement.ToString(), "")))
            {
                return "untitled_model";
            }

            // 4. Trim any leading/trailing replacement characters
            return sanitized.Trim(replacement);
        }
    }
}
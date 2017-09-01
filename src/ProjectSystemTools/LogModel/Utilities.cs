using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal static class Utilities
    {
        public const int MaxDisplayedValueLength = 260;

        public static bool ContainsLineBreak(string text)
        {
            return text.IndexOf('\n') != -1;
        }

        public static KeyValuePair<string, string> ParseNameValue(string nameEqualsValue, int trimFromStart = 0)
        {
            var equals = nameEqualsValue.IndexOf('=');
            if (equals == -1)
            {
                return new KeyValuePair<string, string>(nameEqualsValue, "");
            }

            var name = nameEqualsValue.Substring(trimFromStart, equals - trimFromStart);
            var value = nameEqualsValue.Substring(equals + 1);
            return new KeyValuePair<string, string>(name, value);
        }

        public static int GetNumberOfLeadingSpaces(string line)
        {
            var result = 0;
            while (result < line.Length && line[result] == ' ')
            {
                result++;
            }

            return result;
        }

        public static string ParseQuotedSubstring(string text)
        {
            var firstQuote = text.IndexOf('"');
            if (firstQuote == -1)
            {
                return text;
            }

            var secondQuote = text.IndexOf('"', firstQuote + 1);
            if (secondQuote == -1)
            {
                return text;
            }

            if (secondQuote - firstQuote < 2)
            {
                return text;
            }

            return text.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }
    }
}

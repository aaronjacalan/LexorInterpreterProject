// Preprocesses source input for the interpreter.
// - Removes `%%` comments and blank lines
// - Produces (lineNumber, cleanedLine) pairs
// - Holds the set of LEXOR reserved words

using System.Text;

namespace LexorInterpreter.ProgramCodes
{
    public static class Lexer
    {
        // Returns true when the given identifier is a reserved word.
        public static bool IsReservedWord(string word)
            => LanguageDefinitions.ReservedWords.Contains(word);

        // Strips everything from %% onward on a single line.
        public static string StripComment(string line)
        {
            bool inStr = false;
            char strCh = '"';

            for (int i = 0; i < line.Length - 1; i++)
            {
                char c = line[i];
                if (!inStr && (c == '"' || c == '\''))
                {
                    inStr = true;
                    strCh = c;
                }
                else if (inStr && c == strCh)
                {
                    inStr = false;
                }
                else if (!inStr && c == '%' && line[i + 1] == '%')
                {
                    return line[..i];
                }
            }

            return line;
        }

        // Collapses runs of whitespace to a single space outside quoted text,
        // then normalizes common keyword spacing so source layout is flexible.
        public static string NormalizeLine(string line)
        {
            var sb = new StringBuilder(line.Length);
            bool inStr = false;
            char strCh = '"';
            bool pendingSpace = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (!inStr && (c == '"' || c == '\''))
                {
                    if (pendingSpace) { sb.Append(' '); pendingSpace = false; }
                    inStr = true;
                    strCh = c;
                    sb.Append(c);
                    continue;
                }

                if (inStr)
                {
                    sb.Append(c);
                    if (c == strCh) inStr = false;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = true;
                    continue;
                }

                if (pendingSpace)
                {
                    sb.Append(' ');
                    pendingSpace = false;
                }

                sb.Append(c);
            }

            return NormalizeKeywords(sb.ToString().Trim());
        }

        // Tokenizes the source into (lineNumber, cleanedLine) pairs, skipping blanks/comments.
        public static List<(int LineNumber, string Content)> Tokenize(string source)
        {
            var result = new List<(int, string)>();
            string[] raw = source.Split('\n');

            for (int i = 0; i < raw.Length; i++)
            {
                string cleaned = NormalizeLine(StripComment(raw[i]));
                if (!string.IsNullOrWhiteSpace(cleaned))
                    result.Add((i + 1, cleaned));
            }

            return result;
        }

        private static string NormalizeKeywords(string line)
        {
            if (TryNormalizeLabelPrefix(line, "PRINT", out string printRest))
                return "PRINT:" + printRest;

            if (TryNormalizeLabelPrefix(line, "SCAN", out string scanRest))
                return "SCAN:" + scanRest;

            line = NormalizeKeywordBeforeParen(line, "ELSE IF");
            line = NormalizeKeywordBeforeParen(line, "IF");
            line = NormalizeKeywordBeforeParen(line, "FOR");
            line = NormalizeRepeatWhen(line);
            line = NormalizeDeclarePrefix(line);

            return line;
        }

        private static bool TryNormalizeLabelPrefix(string line, string label, out string rest)
        {
            rest = "";
            if (!line.StartsWith(label, StringComparison.Ordinal)) return false;

            int i = label.Length;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i >= line.Length || line[i] != ':') return false;

            rest = line[(i + 1)..].TrimStart();
            return true;
        }

        private static string NormalizeKeywordBeforeParen(string line, string keyword)
        {
            if (!line.StartsWith(keyword, StringComparison.Ordinal)) return line;

            int i = keyword.Length;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i < line.Length && line[i] == '(')
                return keyword + " " + line[i..];

            return line;
        }

        private static string NormalizeRepeatWhen(string line)
        {
            const string prefix = "REPEAT";
            if (!line.StartsWith(prefix, StringComparison.Ordinal)) return line;

            int i = prefix.Length;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i + 4 > line.Length || !line.AsSpan(i).StartsWith("WHEN")) return line;

            i += 4;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            if (i < line.Length && line[i] == '(')
                return "REPEAT WHEN " + line[i..];

            return line;
        }

        private static string NormalizeDeclarePrefix(string line)
        {
            const string prefix = "DECLARE";
            if (!line.StartsWith(prefix, StringComparison.Ordinal)) return line;

            int i = prefix.Length;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            return prefix + " " + line[i..].TrimStart();
        }
    }
}

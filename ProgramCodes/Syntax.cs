namespace LexorInterpreter.ProgramCodes
{
    internal static class Syntax
    {
        public static bool IsKeywordLine(string line, params string[] keywords)
        {
            int i = 0;
            if (!ReadKeywords(line, keywords, ref i)) return false;
            SkipSpaces(line, ref i);
            return i == line.Length;
        }

        public static bool StartsWithKeyword(string line, string keyword, out string rest)
        {
            rest = string.Empty;
            int i = 0;
            if (!ReadKeyword(line, keyword, ref i)) return false;
            if (i < line.Length && !char.IsWhiteSpace(line[i])) return false;

            SkipSpaces(line, ref i);
            rest = line[i..];
            return true;
        }

        public static bool TryReadCommand(string line, string keyword, out string rest)
        {
            rest = string.Empty;
            int i = 0;
            if (!ReadKeyword(line, keyword, ref i)) return false;

            SkipSpaces(line, ref i);
            if (i >= line.Length || line[i] != ':') return false;

            rest = line[(i + 1)..].Trim();
            return true;
        }

        public static bool TryParenthesizedHeader(string line, string[] keywords, out string inner)
        {
            inner = string.Empty;
            int i = 0;
            if (!ReadKeywords(line, keywords, ref i)) return false;

            SkipSpaces(line, ref i);
            if (i >= line.Length || line[i] != '(' || !line.EndsWith(')')) return false;

            inner = line[(i + 1)..^1].Trim();
            return true;
        }

        private static bool ReadKeywords(string line, string[] keywords, ref int i)
        {
            for (int k = 0; k < keywords.Length; k++)
            {
                if (k > 0)
                {
                    if (i >= line.Length || !char.IsWhiteSpace(line[i])) return false;
                    SkipSpaces(line, ref i);
                }

                if (!ReadKeyword(line, keywords[k], ref i)) return false;
            }

            return true;
        }

        private static bool ReadKeyword(string line, string keyword, ref int i)
        {
            if (i + keyword.Length > line.Length) return false;
            if (!line.AsSpan(i, keyword.Length).SequenceEqual(keyword)) return false;
            i += keyword.Length;
            return true;
        }

        private static void SkipSpaces(string line, ref int i)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        }
    }
}

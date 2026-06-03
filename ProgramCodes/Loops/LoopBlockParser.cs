// Parses FOR and REPEAT loop headers and collects their body lines.
// FOR:   FOR (<init>, <cond>, <update>) … START FOR … END FOR
// REPEAT: REPEAT WHEN (<expr>) … START REPEAT … END REPEAT

namespace LexorInterpreter.ProgramCodes
{
    public enum LoopKind { For, Repeat }

    public sealed class LoopBlock
    {
        public LoopKind Kind { get; init; }

        // FOR only.
        public string? InitStatement   { get; init; }
        public string? Condition       { get; init; }
        public string? UpdateStatement { get; init; }

        public int ConditionLine { get; init; }

        public List<(int LineNumber, string Content)> Body { get; } = new();

        // Index of END FOR / END REPEAT in the original line list.
        public int EndIndex { get; set; }
    }

    public static class LoopBlockParser
    {
        public static (LoopBlock? block, string? error) Parse(
            List<(int LineNumber, string Content)> lines,
            int startIndex)
        {
            var (lineNum, content) = lines[startIndex];

            if (Syntax.TryParenthesizedHeader(content, new[] { "FOR" }, out _))
                return ParseFor(lines, startIndex);

            if (Syntax.TryParenthesizedHeader(content, new[] { "REPEAT", "WHEN" }, out _))
                return ParseRepeat(lines, startIndex);

            return (null, $"Line {lineNum}: Expected FOR or REPEAT WHEN loop header.");
        }

        private static (LoopBlock? block, string? error) ParseFor(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var (lineNum, content) = lines[i];

            if (!Syntax.TryParenthesizedHeader(content, new[] { "FOR" }, out string inner))
                return (null, $"Line {lineNum}: Malformed FOR header. Expected: FOR (<init>, <cond>, <update>)");

            var parts = SplitHeaderParts(inner);
            if (parts == null || parts.Count != 3)
                return (null, $"Line {lineNum}: FOR header must have exactly three parts separated by ','.");

            string init   = parts[0].Trim();
            string cond   = parts[1].Trim();
            string update = parts[2].Trim();

            if (string.IsNullOrWhiteSpace(init) || string.IsNullOrWhiteSpace(cond) || string.IsNullOrWhiteSpace(update))
                return (null, $"Line {lineNum}: FOR header parts (init; condition; update) must not be empty.");

            i++;

            if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "FOR"))
                return (null, $"Line {lineNum}: Expected 'START FOR' after FOR header.");
            i++;

            // Collect body until matching END FOR (depth-aware).
            var (body, endIdx, bodyErr) = CollectBody(lines, i, "FOR");
            if (bodyErr != null) return (null, bodyErr);

            var block = new LoopBlock
            {
                Kind            = LoopKind.For,
                InitStatement   = init,
                Condition       = cond,
                UpdateStatement = update,
                ConditionLine   = lineNum,
                EndIndex        = endIdx
            };
            block.Body.AddRange(body);
            return (block, null);
        }

        private static (LoopBlock? block, string? error) ParseRepeat(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var (lineNum, content) = lines[i];

            if (!Syntax.TryParenthesizedHeader(content, new[] { "REPEAT", "WHEN" }, out string condition))
                return (null, $"Line {lineNum}: Malformed REPEAT header. Expected: REPEAT WHEN (<bool expr>)");

            if (string.IsNullOrWhiteSpace(condition))
                return (null, $"Line {lineNum}: REPEAT WHEN condition must not be empty.");

            i++;

            if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "REPEAT"))
                return (null, $"Line {lineNum}: Expected 'START REPEAT' after REPEAT WHEN header.");
            i++;

            // Collect body until matching END REPEAT (depth-aware).
            var (body, endIdx, bodyErr) = CollectBody(lines, i, "REPEAT");
            if (bodyErr != null) return (null, bodyErr);

            var block = new LoopBlock
            {
                Kind          = LoopKind.Repeat,
                Condition     = condition,
                ConditionLine = lineNum,
                EndIndex      = endIdx
            };
            block.Body.AddRange(body);
            return (block, null);
        }

        // Collects lines until matching close keyword, respecting nested pairs.
        private static (List<(int, string)> body, int endIdx, string? error) CollectBody(
            List<(int LineNumber, string Content)> lines,
            int i,
            string loopKeyword)
        {
            var body  = new List<(int, string)>();
            int depth = 0;

            while (i < lines.Count)
            {
                string c = lines[i].Content;

                if (Syntax.IsKeywordLine(c, "START", loopKeyword))
                {
                    depth++;
                    body.Add(lines[i]);
                }
                else if (Syntax.IsKeywordLine(c, "END", loopKeyword))
                {
                    if (depth == 0)
                        return (body, i, null); // Found matching close keyword.
                    depth--;
                    body.Add(lines[i]);
                }
                else
                {
                    body.Add(lines[i]);
                }
                i++;
            }

            return (body, i, $"Missing 'END {loopKeyword}'.");
        }

        // Splits on top-level commas, ignoring those inside quotes or parens.
        private static List<string>? SplitHeaderParts(string text)
        {
            var  parts     = new List<string>();
            int  start     = 0;
            int  depth     = 0;
            bool inSingle  = false;
            bool inDouble  = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (!inDouble && c == '\'') { inSingle = !inSingle; continue; }
                if (!inSingle && c == '"')  { inDouble = !inDouble; continue; }
                if (inSingle || inDouble)   continue;

                if (c == '(') { depth++; continue; }
                if (c == ')') { if (depth > 0) depth--; continue; }

                if (c == ',' && depth == 0)
                {
                    parts.Add(text[start..i]);
                    start = i + 1;
                }
            }

            if (inSingle || inDouble) return null;
            parts.Add(text[start..]);
            return parts;
        }
    }
}

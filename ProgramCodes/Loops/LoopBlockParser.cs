// Parses FOR and REPEAT loop constructs out of a flat line list.
// Both loop types use depth-aware body collection so that nested loops
// (START FOR…END FOR and START REPEAT…END REPEAT) are handled correctly.
//
// FOR syntax:
//   FOR (<init>, <condition>, <update>)
//   START FOR
//     <statements>
//   END FOR
//
// REPEAT syntax:
//   REPEAT WHEN (<bool expression>)
//   START REPEAT
//     <statements>
//   END REPEAT

namespace LexorInterpreter.ProgramCodes
{
    // -----------------------------------------------------------------------
    // Data structures
    // -----------------------------------------------------------------------

    public enum LoopKind { For, Repeat }

    /// <summary>A fully parsed loop block ready for execution.</summary>
    public sealed class LoopBlock
    {
        public LoopKind Kind { get; init; }

        // FOR only: the three header parts.
        public string? InitStatement  { get; init; }   // e.g. "i = 1"
        public string? Condition      { get; init; }   // e.g. "i <= 5"
        public string? UpdateStatement{ get; init; }   // e.g. "i = i + 1"

        // REPEAT only: the condition expression.
        // (FOR condition is also stored here for uniform execution.)

        public int ConditionLine { get; init; }

        /// <summary>The body lines between START FOR/REPEAT and END FOR/REPEAT.</summary>
        public List<(int LineNumber, string Content)> Body { get; } = new();

        /// <summary>
        /// Index in the *original* line list of the last line consumed by this
        /// block (the END FOR or END REPEAT line).
        /// </summary>
        public int EndIndex { get; set; }
    }

    // -----------------------------------------------------------------------
    // Parser
    // -----------------------------------------------------------------------

    public static class LoopBlockParser
    {
        /// <summary>
        /// Tries to parse a loop block that starts at <paramref name="startIndex"/>.
        /// Returns the filled block plus the index past the closing keyword, or an error.
        /// </summary>
        public static (LoopBlock? block, string? error) Parse(
            List<(int LineNumber, string Content)> lines,
            int startIndex)
        {
            var (lineNum, content) = lines[startIndex];

            if (content.StartsWith("FOR ("))
                return ParseFor(lines, startIndex);

            if (content.StartsWith("REPEAT WHEN ("))
                return ParseRepeat(lines, startIndex);

            return (null, $"Line {lineNum}: Expected FOR or REPEAT WHEN loop header.");
        }

        // -------------------------------------------------------------------
        // FOR loop parser
        // -------------------------------------------------------------------

        private static (LoopBlock? block, string? error) ParseFor(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var (lineNum, content) = lines[i];

            // Extract the header: FOR (<init>, <cond>, <update>)
            if (!content.StartsWith("FOR (") || !content.EndsWith(")"))
                return (null, $"Line {lineNum}: Malformed FOR header. Expected: FOR (<init>, <cond>, <update>)");

            // Strip "FOR (" prefix and ")" suffix.
            string inner = content["FOR (".Length..^1].Trim();

            // Split on the TWO commas, but be careful: the sub-expressions
            // may themselves contain parentheses (function calls are not in LEXOR,
            // but extra parens in conditions are allowed).
            var parts = SplitHeaderParts(inner);
            if (parts == null || parts.Count != 3)
                return (null, $"Line {lineNum}: FOR header must have exactly three parts separated by ','.");

            string init   = parts[0].Trim();
            string cond   = parts[1].Trim();
            string update = parts[2].Trim();

            if (string.IsNullOrWhiteSpace(init) || string.IsNullOrWhiteSpace(cond) || string.IsNullOrWhiteSpace(update))
                return (null, $"Line {lineNum}: FOR header parts (init; condition; update) must not be empty.");

            i++;

            // Expect START FOR.
            if (i >= lines.Count || lines[i].Content != "START FOR")
                return (null, $"Line {lineNum}: Expected 'START FOR' after FOR header.");
            i++;

            // Collect body until the matching END FOR.
            var (body, endIdx, bodyErr) = CollectBody(lines, i, "START FOR", "END FOR");
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

        // -------------------------------------------------------------------
        // REPEAT loop parser
        // -------------------------------------------------------------------

        private static (LoopBlock? block, string? error) ParseRepeat(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var (lineNum, content) = lines[i];

            // Expected: REPEAT WHEN (<expr>)
            if (!content.StartsWith("REPEAT WHEN (") || !content.EndsWith(")"))
                return (null, $"Line {lineNum}: Malformed REPEAT header. Expected: REPEAT WHEN (<bool expr>)");

            string condition = content["REPEAT WHEN (".Length..^1].Trim();
            if (string.IsNullOrWhiteSpace(condition))
                return (null, $"Line {lineNum}: REPEAT WHEN condition must not be empty.");

            i++;

            // Expect START REPEAT.
            if (i >= lines.Count || lines[i].Content != "START REPEAT")
                return (null, $"Line {lineNum}: Expected 'START REPEAT' after REPEAT WHEN header.");
            i++;

            // Collect body until the matching END REPEAT.
            var (body, endIdx, bodyErr) = CollectBody(lines, i, "START REPEAT", "END REPEAT");
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

        // -------------------------------------------------------------------
        // Shared helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Collects body lines between the already-consumed opening keyword and
        /// the matching closing keyword, respecting nested start/end pairs.
        /// </summary>
        private static (List<(int, string)> body, int endIdx, string? error) CollectBody(
            List<(int LineNumber, string Content)> lines,
            int i,
            string openKeyword,
            string closeKeyword)
        {
            var body  = new List<(int, string)>();
            int depth = 0;

            while (i < lines.Count)
            {
                string c = lines[i].Content;

                if (c == openKeyword)
                {
                    depth++;
                    body.Add(lines[i]);
                }
                else if (c == closeKeyword)
                {
                    if (depth == 0)
                        return (body, i, null); // Found our closing keyword.
                    depth--;
                    body.Add(lines[i]);
                }
                else
                {
                    body.Add(lines[i]);
                }
                i++;
            }

            return (body, i, $"Missing '{closeKeyword}'.");
        }

        /// <summary>
        /// Splits a string on top-level commas (ignoring those inside
        /// single- or double-quoted strings and parentheses).
        /// Returns null if parsing detects a runaway string or paren.
        /// </summary>
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

            if (inSingle || inDouble) return null; // Runaway quote.
            parts.Add(text[start..]);
            return parts;
        }
    }
}

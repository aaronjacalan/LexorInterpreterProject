// Parses IF / ELSE IF / ELSE blocks out of a flat line list.
// Returns a structured IfBlock describing each branch's condition and body lines.
// Handles arbitrary nesting by tracking START IF / END IF depth.

namespace LexorInterpreter.ProgramCodes
{
    // One branch of an if-else chain: Condition == null means ELSE, otherwise IF/ELSE IF.
    public sealed class IfBranch
    {
        public string? Condition { get; init; }     // raw bool-expression text (null = ELSE)
        public int ConditionLine { get; init; }
        public List<(int LineNumber, string Content)> Body { get; init; } = new();
    }

    public sealed class IfBlock
    {
        // Ordered list: IF branch first, then zero or more ELSE IF, then optional ELSE.
        public List<IfBranch> Branches { get; } = new();
        // Index in the original line list of the END IF that closes this block.
        public int EndIndex { get; set; }
    }

    public static class IfBlockParser
    {
        // Parses an IF block at startIndex, returning the filled block and endIndex or an error.
        public static (IfBlock? block, string? error) Parse(
            List<(int LineNumber, string Content)> lines,
            int startIndex)
        {
            var block = new IfBlock();
            int i = startIndex;

            // Parse the leading IF (<condition>).
            var (firstBranch, err) = ParseIfHeader(lines[i]);
            if (err != null) return (null, err);
            i++;

            // Expect START IF.
            if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "IF"))
                return (null, $"Line {lines[i > 0 ? i - 1 : 0].LineNumber}: Expected 'START IF' after IF condition.");
            i++;

            // Collect body until the matching END IF (depth-aware).
            var (body, afterBody, bodyErr) = CollectBody(lines, i);
            if (bodyErr != null) return (null, bodyErr);
            firstBranch!.Body.AddRange(body);
            block.Branches.Add(firstBranch);
            i = afterBody; // points to END IF

            // i now points to END IF; skip it.
            i++;

            // Parse optional ELSE IF / ELSE chains.
            bool hasElse = false;
            while (i < lines.Count)
            {
                string content = lines[i].Content;

                if (hasElse)
                {
                    if (IsElseBranchHeader(content))
                        return (null, $"Line {lines[i].LineNumber}: Unexpected '{content}' after ELSE — ELSE must be the last branch.");
                    break;
                }

                if (IsElseIfHeader(content))
                {
                    var (elseIfBranch, elseIfErr) = ParseElseIfHeader(lines[i]);
                    if (elseIfErr != null) return (null, elseIfErr);
                    i++;

                    if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "IF"))
                        return (null, $"Line {lines[i - 1].LineNumber}: Expected 'START IF' after ELSE IF condition.");
                    i++;

                    var (elseIfBody, afterElseIfBody, elseIfBodyErr) = CollectBody(lines, i);
                    if (elseIfBodyErr != null) return (null, elseIfBodyErr);
                    elseIfBranch!.Body.AddRange(elseIfBody);
                    block.Branches.Add(elseIfBranch);
                    i = afterElseIfBody + 1; // skip END IF
                }
                else if (Syntax.IsKeywordLine(content, "ELSE"))
                {
                    int elseLine = lines[i].LineNumber;
                    i++;

                    if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "IF"))
                        return (null, $"Line {elseLine}: Expected 'START IF' after ELSE.");
                    i++;

                    var (elseBody, afterElseBody, elseBodyErr) = CollectBody(lines, i);
                    if (elseBodyErr != null) return (null, elseBodyErr);

                    block.Branches.Add(new IfBranch
                    {
                        Condition = null,
                        ConditionLine = elseLine,
                        Body = { },
                    });
                    block.Branches[^1].Body.AddRange(elseBody);
                    i = afterElseBody + 1; // skip END IF
                    hasElse = true; // ELSE must be the last branch
                }
                else
                {
                    break;
                }
            }

            block.EndIndex = i - 1; // last consumed index
            return (block, null);
        }

        // Helpers.

        private static (IfBranch? branch, string? error) ParseIfHeader(
            (int LineNumber, string Content) line)
        {
            if (!TryExtractParenthesizedCondition(line.Content, new[] { "IF" }, out string? condition, out string? error))
                return (null, $"Line {line.LineNumber}: {error}");

            return (new IfBranch { Condition = condition, ConditionLine = line.LineNumber }, null);
        }

        private static (IfBranch? branch, string? error) ParseElseIfHeader(
            (int LineNumber, string Content) line)
        {
            if (!TryExtractParenthesizedCondition(line.Content, new[] { "ELSE", "IF" }, out string? condition, out string? error))
                return (null, $"Line {line.LineNumber}: {error}");

            return (new IfBranch { Condition = condition, ConditionLine = line.LineNumber }, null);
        }

        // Accepts IF (<expr>) and IF(<expr>) (optional space before '(').
        private static bool TryExtractParenthesizedCondition(
            string content,
            string[] keywords,
            out string? condition,
            out string? error)
        {
            condition = null;
            error = null;

            if (!Syntax.TryParenthesizedHeader(content, keywords, out string inner))
            {
                string keywordText = string.Join(" ", keywords);
                error = $"Malformed {keywordText} condition. Expected: {keywordText} (<expr>)";
                return false;
            }

            condition = inner;
            return true;
        }

        // Collects body lines until matching END IF, respecting nesting.
        // Nested IF / ELSE IF / ELSE chains are consumed as a single unit.
        private static (List<(int, string)> body, int endIdx, string? error) CollectBody(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var body = new List<(int, string)>();
            int depth = 0;

            while (i < lines.Count)
            {
                string content = lines[i].Content;

                if (depth == 0 && IsIfHeader(content))
                {
                    var (chainEnd, chainErr) = ConsumeIfElseChain(lines, i);
                    if (chainErr != null) return (body, i, chainErr);
                    for (int j = i; j <= chainEnd; j++)
                        body.Add(lines[j]);
                    i = chainEnd + 1;
                    continue;
                }

                if (Syntax.IsKeywordLine(content, "START", "IF"))
                {
                    depth++;
                    body.Add(lines[i]);
                }
                else if (Syntax.IsKeywordLine(content, "END", "IF"))
                {
                    if (depth == 0)
                        return (body, i, null);
                    depth--;
                    body.Add(lines[i]);
                }
                else if (depth == 0 && IsElseBranchHeader(content))
                {
                    return (body, i, null); // Sibling ELSE IF/ELSE — handled by outer Parse loop.
                }
                else
                {
                    body.Add(lines[i]);
                }
                i++;
            }

            return (body, i, $"Missing 'END IF'.");
        }

        // Collects lines for one branch (between START IF and its END IF).
        private static (List<(int, string)> body, int endIdx, string? error) CollectSimpleBody(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var body = new List<(int, string)>();
            int depth = 0;

            while (i < lines.Count)
            {
                string content = lines[i].Content;

                if (Syntax.IsKeywordLine(content, "START", "IF"))
                {
                    depth++;
                    body.Add(lines[i]);
                }
                else if (Syntax.IsKeywordLine(content, "END", "IF"))
                {
                    if (depth == 0)
                        return (body, i, null);
                    depth--;
                    body.Add(lines[i]);
                }
                else
                {
                    body.Add(lines[i]);
                }
                i++;
            }

            return (body, i, $"Missing 'END IF'.");
        }

        // Consumes a full IF / ELSE IF* / ELSE? chain starting at the IF header line.
        private static (int chainEndIdx, string? error) ConsumeIfElseChain(
            List<(int LineNumber, string Content)> lines,
            int start)
        {
            if (start >= lines.Count || !IsIfHeader(lines[start].Content))
                return (start, "Malformed IF block.");

            int i = start + 1;

            if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "IF"))
                return (start, $"Line {lines[start].LineNumber}: Expected 'START IF' after IF condition.");

            i++;
            var (_, afterFirst, err) = CollectSimpleBody(lines, i);
            if (err != null) return (start, err);
            i = afterFirst + 1; // skip branch END IF

            while (i < lines.Count && IsElseBranchHeader(lines[i].Content))
            {
                i++; // skip ELSE IF (...) or ELSE

                if (i >= lines.Count || !Syntax.IsKeywordLine(lines[i].Content, "START", "IF"))
                    return (start, $"Line {lines[i - 1].LineNumber}: Expected 'START IF' after {lines[i - 1].Content}.");

                i++;
                var (_, afterBranch, branchErr) = CollectSimpleBody(lines, i);
                if (branchErr != null) return (start, branchErr);
                i = afterBranch + 1;
            }

            return (i - 1, null);
        }

        private static bool IsIfHeader(string content)
        {
            return Syntax.TryParenthesizedHeader(content, new[] { "IF" }, out _);
        }

        private static bool IsElseBranchHeader(string content)
            => Syntax.IsKeywordLine(content, "ELSE") || IsElseIfHeader(content);

        private static bool IsElseIfHeader(string content)
            => Syntax.TryParenthesizedHeader(content, new[] { "ELSE", "IF" }, out _);
    }
}

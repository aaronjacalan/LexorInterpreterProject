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
            if (i >= lines.Count || lines[i].Content != "START IF")
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
            while (i < lines.Count)
            {
                string content = lines[i].Content;

                if (content.StartsWith("ELSE IF "))
                {
                    var (elseIfBranch, elseIfErr) = ParseElseIfHeader(lines[i]);
                    if (elseIfErr != null) return (null, elseIfErr);
                    i++;

                    if (i >= lines.Count || lines[i].Content != "START IF")
                        return (null, $"Line {lines[i - 1].LineNumber}: Expected 'START IF' after ELSE IF condition.");
                    i++;

                    var (elseIfBody, afterElseIfBody, elseIfBodyErr) = CollectBody(lines, i);
                    if (elseIfBodyErr != null) return (null, elseIfBodyErr);
                    elseIfBranch!.Body.AddRange(elseIfBody);
                    block.Branches.Add(elseIfBranch);
                    i = afterElseIfBody + 1; // skip END IF
                }
                else if (content == "ELSE")
                {
                    int elseLine = lines[i].LineNumber;
                    i++;

                    if (i >= lines.Count || lines[i].Content != "START IF")
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
                    break; // ELSE is always last
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
            string content = line.Content;
            // Expected: IF (<expr>).
            if (!content.StartsWith("IF (") || !content.EndsWith(")"))
                return (null, $"Line {line.LineNumber}: Malformed IF condition. Expected: IF (<expr>)");

            // content = "IF (expr)" — take everything after "IF " to get "(expr)".
            string condition = content["IF ".Length..].Trim();
            // Remove the mandatory surrounding parens.
            if (condition.StartsWith("(") && condition.EndsWith(")"))
                condition = condition[1..^1].Trim();

            return (new IfBranch { Condition = condition, ConditionLine = line.LineNumber }, null);
        }

        private static (IfBranch? branch, string? error) ParseElseIfHeader(
            (int LineNumber, string Content) line)
        {
            string content = line.Content;
            if (!content.StartsWith("ELSE IF (") || !content.EndsWith(")"))
                return (null, $"Line {line.LineNumber}: Malformed ELSE IF condition. Expected: ELSE IF (<expr>)");

            string condition = content["ELSE IF ".Length..].Trim();
            if (condition.StartsWith("(") && condition.EndsWith(")"))
                condition = condition[1..^1].Trim();

            return (new IfBranch { Condition = condition, ConditionLine = line.LineNumber }, null);
        }

        // Collects body lines until matching END IF, respecting nesting.
        private static (List<(int, string)> body, int endIdx, string? error) CollectBody(
            List<(int LineNumber, string Content)> lines,
            int i)
        {
            var body = new List<(int, string)>();
            int depth = 0;

            while (i < lines.Count)
            {
                string content = lines[i].Content;

                if (content == "START IF")
                {
                    depth++;
                    body.Add(lines[i]);
                }
                else if (content == "END IF")
                {
                    if (depth == 0)
                        return (body, i, null); // Found our END IF.
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
    }
}

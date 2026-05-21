// Interprets a LEXOR source program.
// - Validates SCRIPT AREA / START SCRIPT / END SCRIPT structure
// - Runs all `DECLARE` lines first to build the symbol table
// - Executes the remaining statements line by line
//   (PRINT, SCAN, assignments, IF blocks, FOR loops, REPEAT loops)

namespace LexorInterpreter.ProgramCodes
{
    public class Interpreter
    {
        private readonly Dictionary<string, Variable> _symbolTable = new();
        private int _nestingDepth;
        private const int MaxNestingDepth = 50;

        // Returns null on success; otherwise returns a formatted error message.
        public string? Run(string source)
        {
            var lines = Lexer.Tokenize(source);

            string? structErr = ValidateStructure(lines);
            if (structErr != null) return NormalizeError(structErr);

            var body = ExtractBody(lines);

            // Split the body into DECLARE lines and executable statements.
            int boundary     = FindDeclareBoundary(body);
            var declareLines = body[..boundary];
            var execLines    = body[boundary..];

            // Process all variable declarations first so later statements can reference them safely.
            foreach (var (lineNum, content) in declareLines)
            {
                string? err = VariableDeclarator.Parse(content, lineNum, _symbolTable);
                if (err != null) return NormalizeError(err);
            }

            // Execute the remaining statements in order, stopping on first error.
            string? execErr = ExecuteLines(execLines);
            if (execErr != null) return NormalizeError(execErr);

            Console.WriteLine();
            return null;
        }

        // -----------------------------------------------------------------------
        // Core execution loop — handles IF blocks, FOR loops, REPEAT loops,
        // and plain statements. Called recursively for nested control structures.
        // -----------------------------------------------------------------------
        private string? ExecuteLines(List<(int LineNumber, string Content)> lines)
        {
            int i = 0;
            while (i < lines.Count)
            {
                var (lineNum, content) = lines[i];

                // ---- IF / ELSE IF / ELSE chain ----
                if (content.StartsWith("IF ("))
                {
                    var (block, parseErr) = IfBlockParser.Parse(lines, i);
                    if (parseErr != null) return $"Line {lineNum}: {parseErr}";

                    _nestingDepth++;
                    if (_nestingDepth > MaxNestingDepth)
                        Console.WriteLine($"[WARNING - Line {lineNum}] Deep nesting detected ({_nestingDepth} levels). Consider refactoring.");

                    string? execErr = IfExecutor.Execute(block!, _symbolTable, ExecuteLines);
                    if (execErr != null) return execErr;

                    _nestingDepth--;
                    i = block!.EndIndex + 1;
                }
                // ---- FOR loop ----
                else if (content.StartsWith("FOR ("))
                {
                    var (loop, parseErr) = LoopBlockParser.Parse(lines, i);
                    if (parseErr != null) return parseErr;

                    _nestingDepth++;
                    if (_nestingDepth > MaxNestingDepth)
                        Console.WriteLine($"[WARNING - Line {lineNum}] Deep nesting detected ({_nestingDepth} levels). Consider refactoring.");

                    string? execErr = LoopExecutor.Execute(loop!, _symbolTable, ExecuteLines, ExecuteStatement);
                    if (execErr != null) return execErr;

                    _nestingDepth--;
                    i = loop!.EndIndex + 1;
                }
                // ---- REPEAT WHEN loop ----
                else if (content.StartsWith("REPEAT WHEN ("))
                {
                    var (loop, parseErr) = LoopBlockParser.Parse(lines, i);
                    if (parseErr != null) return parseErr;

                    _nestingDepth++;
                    if (_nestingDepth > MaxNestingDepth)
                        Console.WriteLine($"[WARNING - Line {lineNum}] Deep nesting detected ({_nestingDepth} levels). Consider refactoring.");

                    string? execErr = LoopExecutor.Execute(loop!, _symbolTable, ExecuteLines, ExecuteStatement);
                    if (execErr != null) return execErr;

                    _nestingDepth--;
                    i = loop!.EndIndex + 1;
                }
                else
                {
                    string? err = ExecuteStatement(content, lineNum);
                    if (err != null) return err;
                    i++;
                }
            }
            return null;
        }

        // Dispatches a single (non-control-flow) statement line.
        private string? ExecuteStatement(string line, int lineNumber)
        {
            if (line.StartsWith("PRINT:"))
                return Printer.Execute(line, lineNumber, _symbolTable);

            if (line.StartsWith("SCAN:"))
                return Scanner.Execute(line, lineNumber, _symbolTable);

            if (IsAssignment(line))
                return VariableAssignor.Execute(line, lineNumber, _symbolTable);

            return $"Line {lineNumber}: Unrecognized statement '{line}'.";
        }

        // Validates SCRIPT AREA / START SCRIPT / END SCRIPT structure.
        private static string? ValidateStructure(List<(int LineNumber, string Content)> lines)
        {
            if (lines.Count == 0)
                return "[ERROR - Line 0] Source file is empty.";

            if (lines[0].Content != "SCRIPT AREA")
                return $"Line {lines[0].LineNumber}: Program must begin with 'SCRIPT AREA'.";

            var startLines = lines.Where(l => l.Content == "START SCRIPT").Select(l => l.LineNumber).ToList();
            var endLines   = lines.Where(l => l.Content == "END SCRIPT").Select(l => l.LineNumber).ToList();

            if (startLines.Count == 0) return "[ERROR - Line 0] Missing 'START SCRIPT'.";
            if (endLines.Count == 0)   return "[ERROR - Line 0] Missing 'END SCRIPT'.";

            if (startLines.Count > 1)
                return $"[ERROR - Line {startLines[1]}] Duplicate 'START SCRIPT'.";

            if (endLines.Count > 1)
                return $"[ERROR - Line {endLines[1]}] Duplicate 'END SCRIPT'.";

            int startIdx = lines.FindIndex(l => l.Content == "START SCRIPT");
            int endIdx   = lines.FindIndex(l => l.Content == "END SCRIPT");

            if (startIdx >= endIdx)
                return $"[ERROR - Line {lines[endIdx].LineNumber}] 'END SCRIPT' must come after 'START SCRIPT'.";

            return null;
        }

        // Extracts lines between START SCRIPT and END SCRIPT.
        private static List<(int LineNumber, string Content)> ExtractBody(
            List<(int LineNumber, string Content)> lines)
        {
            int start = lines.FindIndex(l => l.Content == "START SCRIPT") + 1;
            int end   = lines.FindIndex(l => l.Content == "END SCRIPT");
            return lines[start..end];
        }

        // Finds where leading DECLARE lines end.
        private static int FindDeclareBoundary(List<(int LineNumber, string Content)> body)
        {
            int i = 0;
            while (i < body.Count && body[i].Content.StartsWith("DECLARE "))
                i++;
            return i;
        }

        // Returns true if the line looks like an assignment.
        private static bool IsAssignment(string line)
            => line.Contains('=')
               && !line.StartsWith("DECLARE")
               && !line.StartsWith("PRINT");

        // Formats an interpreter error message.
        private static string NormalizeError(string message)
        {
            if (message.StartsWith("[ERROR - Line", StringComparison.Ordinal)) return message;

            const string prefix = "Line ";
            if (message.StartsWith(prefix, StringComparison.Ordinal))
            {
                int colon = message.IndexOf(':');
                if (colon > prefix.Length)
                {
                    string linePart = message[prefix.Length..colon].Trim();
                    if (int.TryParse(linePart, out int lineNum))
                    {
                        string rest = message[(colon + 1)..].Trim();
                        return $"[ERROR - Line {lineNum}] {rest}";
                    }
                }
            }

            string cleaned = message;
            if (cleaned.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned["Error:".Length..].Trim();

            return $"[ERROR - Line 0] {cleaned}";
        }
    }
}

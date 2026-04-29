// Interprets a LEXOR source program.
// - Validates SCRIPT AREA / START SCRIPT / END SCRIPT structure
// - Runs all `DECLARE` lines first to build the symbol table
// - Executes the remaining statements line by line (PRINT, assignments, etc.)

namespace LexorInterpreter.ProgramCodes
{
    public class Interpreter
    {
        private readonly Dictionary<string, Variable> _symbolTable = new();

        // Returns null on success; otherwise returns a formatted error message:
        // [ERROR - Line #] error msg
        public string? Run(string source)
        {
            var lines = Lexer.Tokenize(source);

            string? structErr = ValidateStructure(lines);
            if (structErr != null) return NormalizeError(structErr);

            var body = ExtractBody(lines);

            // Split the script body into a DECLARE section (to build the symbol table)
            // and an execution section (statements that can rely on declared variables).
            int boundary     = FindDeclareBoundary(body);
            var declareLines = body[..boundary];
            var execLines    = body[boundary..];

            // Process all variable declarations first so later statements can reference them safely.
            foreach (var (lineNum, content) in declareLines)
            {
                string? err = VariableDeclarator.Parse(content, lineNum, _symbolTable);
                if (err != null) return NormalizeError(err);
            }

            // Execute the remaining statements in order (e.g., PRINT and assignments), stopping on first error.
            foreach (var (lineNum, content) in execLines)
            {
                string? err = ExecuteStatement(content, lineNum);
                if (err != null) return NormalizeError(err);
            }

            Console.WriteLine();
            return null;
        }

        // Dispatches a single statement line.
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

        // Prints an interpreter error message.
        private static string NormalizeError(string message)
        {
            // Already formatted.
            if (message.StartsWith("[ERROR - Line", StringComparison.Ordinal)) return message;

            // Convert "Line N: msg" into the required format.
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

            // Fallback when no line number is present.
            string cleaned = message;
            if (cleaned.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned["Error:".Length..].Trim();

            return $"[ERROR - Line 0] {cleaned}";
        }
    }
}
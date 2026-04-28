// Interprets a LEXOR source program.
// - Validates SCRIPT AREA / START SCRIPT / END SCRIPT structure
// - Runs all `DECLARE` lines first to build the symbol table
// - Executes the remaining statements line by line (PRINT, assignments, etc.)

namespace LexorInterpreter.ProgramCodes
{
    public class Interpreter
    {
        private readonly Dictionary<string, Variable> _symbolTable = new();

        public void Run(string source)
        {
            var lines = Lexer.Tokenize(source);

            string? structErr = ValidateStructure(lines);
            if (structErr != null) { ReportError(structErr); return; }

            var body = ExtractBody(lines);

            int boundary     = FindDeclareBoundary(body);
            var declareLines = body[..boundary];
            var execLines    = body[boundary..];

            foreach (var (lineNum, content) in declareLines)
            {
                string? err = VariableDeclarator.Parse(content, lineNum, _symbolTable);
                if (err != null) { ReportError(err); return; }
            }

            foreach (var (lineNum, content) in execLines)
            {
                string? err = ExecuteStatement(content, lineNum);
                if (err != null) { ReportError(err); return; }
            }

            Console.WriteLine();
        }

        // Dispatches a single statement line.
        private string? ExecuteStatement(string line, int lineNumber)
        {
            if (line.StartsWith("PRINT:"))
                return Printer.Execute(line, lineNumber, _symbolTable);

            if (IsAssignment(line))
                return VariableAssignor.Execute(line, lineNumber, _symbolTable);

            return $"Line {lineNumber}: Unrecognized statement '{line}'.";
        }

        // Validates SCRIPT AREA / START SCRIPT / END SCRIPT structure.
        private static string? ValidateStructure(List<(int LineNumber, string Content)> lines)
        {
            if (lines.Count == 0)
                return "Error: Source file is empty.";

            if (lines[0].Content != "SCRIPT AREA")
                return $"Line {lines[0].LineNumber}: Program must begin with 'SCRIPT AREA'.";

            bool hasStart = lines.Any(l => l.Content == "START SCRIPT");
            bool hasEnd   = lines.Any(l => l.Content == "END SCRIPT");

            if (!hasStart) return "Error: Missing 'START SCRIPT'.";
            if (!hasEnd)   return "Error: Missing 'END SCRIPT'.";

            int startIdx = lines.FindIndex(l => l.Content == "START SCRIPT");
            int endIdx   = lines.FindIndex(l => l.Content == "END SCRIPT");

            if (startIdx >= endIdx)
                return "Error: 'END SCRIPT' must come after 'START SCRIPT'.";

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
        private static void ReportError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[LEXOR ERROR] {message}");
            Console.ResetColor();
        }
    }
}
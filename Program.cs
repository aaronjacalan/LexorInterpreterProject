using LexorInterpreter.ProgramCodes;
using System.Diagnostics;
using System.Globalization;

namespace LexorInterpreter;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();

        void Fail(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        Console.WriteLine("LEXOR Interpreter:\n");

        if (args.Length == 0)
        {
            Fail("[ERROR - Line 0] Usage: LexorInterpreter <filename>");
            return;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Fail($"[ERROR - Line 0] File not found -> '{filePath}'");
            return;
        }

        string sourceCode = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            Fail("[ERROR - Line 0] Source file is empty.");
            return;
        }

        var sw = Stopwatch.StartNew();
        string? err = new Interpreter().Run(sourceCode);
        sw.Stop();
        if (err != null)
        {
            Fail(err);
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(
            $"Code completed in {sw.Elapsed.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture)}.");
        Console.ResetColor();
    }
}

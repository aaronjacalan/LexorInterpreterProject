using LexorInterpreter.ProgramCodes;
using System.Diagnostics;
using System.Globalization;

namespace LexorInterpreter;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            Fail("[ERROR] Failed to clear console.");
        }

        void Fail(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        Console.WriteLine("\nLEXOR Interpreter:\n");

        if (args.Length == 0)
        {
            Fail("[ERROR] Usage: LexorInterpreter <filename>");
            return;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Fail($"[ERROR] File not found -> '{filePath}'");
            return;
        }

        string sourceCode = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            Fail("[ERROR] Source file is empty.");
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

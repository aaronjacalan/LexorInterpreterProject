using LexorInterpreter.ProgramCodes;

namespace LexorInterpreter;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("Welcome to de Interpreter:\n\n");

        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR - Line 0] Usage: LexorInterpreter <filename>");
            Console.ResetColor();
            return;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR - Line 0] File not found -> '{filePath}'");
            Console.ResetColor();
            return;
        }

        string sourceCode = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR - Line 0] Source file is empty.");
            Console.ResetColor();
            return;
        }

        string? err = new Interpreter().Run(sourceCode);
        if (err != null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(err);
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Completed Successfully");
        Console.ResetColor();
    }
}

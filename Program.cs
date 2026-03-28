using LexorInterpreter.ProgramCodes;

namespace LexorInterpreter;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Welcome to Lexor Interpreter");

        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Usage: LexorInterpreter <filename>");
            Console.ResetColor();
            return;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: File not found -> '{filePath}'");
            Console.ResetColor();
            return;
        }

        string sourceCode = File.ReadAllText(filePath);
        new Interpreter().Run(sourceCode);
    }
}

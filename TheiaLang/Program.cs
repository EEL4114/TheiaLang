using System.Diagnostics;
using TheiaLang;

Stopwatch sw = new Stopwatch();
Stopwatch sw2 = new Stopwatch();
sw.Start();
sw2.Start();

string programName = "Example";
string code = File.ReadAllText(programName + ".tia");
Lexer lexer = new Lexer(code);

List<Token> tokens = new List<Token>();
Token token;

do
{
    token = lexer.NextToken();
    tokens.Add(token);
} while (token.TokenType != TokenType.EOF);

Console.WriteLine($"Lexer took {sw2.ElapsedMilliseconds} ms");
sw2.Restart();

Parser parser = new Parser(tokens);
ProgramNode ast = parser.ParseProgram();
Console.WriteLine($"Parser took {sw2.ElapsedMilliseconds} ms");
sw2.Restart();

using StreamWriter writer = new StreamWriter($"{programName}_ast.txt");
AstPrinter.Print(ast, writer);
Console.WriteLine($"AST building took {sw2.ElapsedMilliseconds} ms");
sw2.Restart();

IRGenerator.Emit(ast, "Example.ll");

Process.Start(@"C:\Program Files\LLVM\bin\clang.exe", $"{programName}.ll -O3 -o {programName}.exe")?.WaitForExit();
Console.WriteLine($"LLVM took {sw2.ElapsedMilliseconds} ms");
sw2.Stop();

Console.WriteLine($"All Processes finished in {sw.ElapsedMilliseconds} ms");
Console.WriteLine("==================================================");

public class Log
{
    public static void Error(uint code, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write($"Error #{code}: ");
        Console.ResetColor();
        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }
}
using System.Diagnostics;
using TheiaLang;

const ConsoleColor LEXER_COL = ConsoleColor.Cyan;
const ConsoleColor PARSER_COL = ConsoleColor.Yellow;
const ConsoleColor IRGEN_COL = ConsoleColor.Green;
const ConsoleColor LLVM_COL = ConsoleColor.Magenta;

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
    // Log.Info(token.ToString());
} while (token.TokenType != TokenType.EOF);

int lexerTime = (int)sw2.Elapsed.TotalMilliseconds;

Console.Write($"Lexer took ");
Console.ForegroundColor = LEXER_COL;
Console.WriteLine($"{lexerTime} ms");
Console.ResetColor();
sw2.Restart();

Parser parser = new Parser(tokens);
(ProgramNode ast, Scope globalScope) = parser.ParseProgram();
int parserTime = (int)sw2.Elapsed.TotalMilliseconds;

Console.Write($"Parser took ");
Console.ForegroundColor = PARSER_COL;
Console.WriteLine($"{parserTime} ms");
Console.ResetColor();
sw2.Restart();

sw.Stop();
using StreamWriter writer = new StreamWriter($"{programName}_ast.txt");
AstPrinter.Print(ast, writer);
Console.WriteLine($"AST building took {sw2.ElapsedMilliseconds} ms");
sw.Start();
sw2.Restart();

IRGenerator.Emit(ast, globalScope, "Example.ll");
int IRgenTime = (int)sw2.Elapsed.TotalMilliseconds;
Console.Write($"IR Generation took ");
Console.ForegroundColor = IRGEN_COL;
Console.WriteLine($"{IRgenTime} ms");
Console.ResetColor();
sw2.Restart();

Process.Start(@"C:\Program Files\LLVM\bin\clang.exe", $"-x ir {programName}.ll -O0 -o {programName}.exe")?.WaitForExit();
int LLVMTime = (int)sw2.Elapsed.TotalMilliseconds;

Console.Write($"LLVM took ");
Console.ForegroundColor = LLVM_COL;
Console.WriteLine($"{LLVMTime} ms");
Console.ResetColor();
sw2.Stop();

Console.WriteLine($"All Processes finished in {sw.ElapsedMilliseconds} ms");

int totalTime = lexerTime + parserTime + IRgenTime + LLVMTime;

int lexerChars = (int)Math.Round(lexerTime / (double)totalTime * 50);
int parserChars = (int)Math.Round(parserTime / (double)totalTime * 50);
int IRgenChars = (int)Math.Round(IRgenTime / (double)totalTime * 50);
int llvmChars = 50 - lexerChars - parserChars - IRgenChars;

void PrintSegment(int count, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.Write(new string('■', count));
    Console.ResetColor();
}

Console.Write("[");
PrintSegment(lexerChars, LEXER_COL);
PrintSegment(parserChars, PARSER_COL);
PrintSegment(IRgenChars, IRGEN_COL);
PrintSegment(llvmChars, LLVM_COL);
Console.WriteLine("]");

public class
Log
{
    public static void Error(uint code, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write($"Error #{code}: ");
        Console.ResetColor();
        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }

    public static void Info(string text)
    {
        Console.WriteLine(text);
    }
}
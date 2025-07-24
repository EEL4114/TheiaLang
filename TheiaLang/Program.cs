using System.Diagnostics;
using TheiaLang;

const ConsoleColor LEXER_COL = ConsoleColor.Cyan;
const ConsoleColor PARSER_COL = ConsoleColor.Yellow;
const ConsoleColor SEM_COL = ConsoleColor.DarkRed;
const ConsoleColor IRGEN_COL = ConsoleColor.Green;
const ConsoleColor LLVM_COL = ConsoleColor.Magenta;

string programName = "Example";
string code = File.ReadAllText(programName + ".tia");
Stopwatch sw = new Stopwatch();
Stopwatch sw2 = new Stopwatch();
sw2.Start();
sw.Start();
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

Log.Time("Lexer took", lexerTime, LEXER_COL);
sw2.Restart();

Parser parser = new Parser(tokens);
(ProgramNode ast, Scope globalScope) = parser.ParseProgram(programName);
int parserTime = (int)sw2.Elapsed.TotalMilliseconds;

Log.Time("Parser took", parserTime, PARSER_COL);
sw2.Stop();

sw.Stop();
using StreamWriter writer = new StreamWriter($"{programName}_ast.txt");
sw2.Restart();
AstPrinter.Print(ast, writer);
int printTime = (int)sw2.ElapsedMilliseconds;
sw.Start();
sw2.Restart();

(ast, globalScope) = SemanticAnalyser.AnalyseProgram(ast, globalScope);
int semTime = (int)sw2.Elapsed.TotalMilliseconds;
Log.Time("Semantic Analysis took", semTime, SEM_COL);
sw2.Stop();

sw.Stop();
using StreamWriter writer2 = new StreamWriter($"{programName}_ast_full.txt");
sw2.Restart();
AstPrinter.Print(ast, writer2);
Console.WriteLine($"AST printing took {sw2.ElapsedMilliseconds + printTime} ms");
sw.Start();
sw2.Restart();

IRGenerator.Emit(ast, globalScope, $"{programName}.ll");
int IRgenTime = (int)sw2.Elapsed.TotalMilliseconds;
Log.Time("IR Generation took", IRgenTime, IRGEN_COL);
sw2.Restart();

Process.Start(@"C:\Program Files\LLVM\bin\clang.exe", $"-x ir {programName}.ll -O0 -o {programName}.exe")?.WaitForExit();

int LLVMTime = (int)sw2.Elapsed.TotalMilliseconds;

Console.Write($"LLVM took ");
Console.ForegroundColor = LLVM_COL;
Console.WriteLine($"{LLVMTime} ms");
Console.ResetColor();
sw2.Stop();

Console.WriteLine($"All Processes finished in {sw.ElapsedMilliseconds} ms");

double totalTime = lexerTime + parserTime + IRgenTime + LLVMTime;

int lexerChars = (int)Math.Round(lexerTime / (double)totalTime * 50);
int parserChars = (int)Math.Round(parserTime / (double)totalTime * 50);
int semChars = (int)Math.Round(semTime / (double)totalTime * 50);
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
PrintSegment(semChars, SEM_COL);
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

    public static void Time(string text, int time, ConsoleColor highlight = ConsoleColor.White, float linesPerS = 0)
    {
        Console.Write($"{text} ");
        Console.ForegroundColor = highlight;
        Console.Write($"{time} ms ");
        Console.ResetColor();
        if (linesPerS > 0)
            Console.WriteLine($"  -  {linesPerS} lines/s");
        else
            Console.WriteLine();
    }
}
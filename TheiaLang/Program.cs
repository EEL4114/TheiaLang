using System.Diagnostics;
using TheiaLang;

const ConsoleColor PRELOAD_COL = ConsoleColor.DarkGray;
const ConsoleColor LEXER_COL   = ConsoleColor.Cyan;
const ConsoleColor PARSER_COL  = ConsoleColor.Yellow;
const ConsoleColor SEM_COL     = ConsoleColor.DarkRed;
const ConsoleColor IRGEN_COL   = ConsoleColor.Green;
const ConsoleColor LLVM_COL    = ConsoleColor.Magenta;

const int BAR_CHARS = 100;

Stopwatch compileTimer  = new Stopwatch();
Stopwatch preloadTimer  = new Stopwatch();
Stopwatch lexTimer      = new Stopwatch();
Stopwatch parseTimer    = new Stopwatch();
Stopwatch printTimer    = new Stopwatch();
Stopwatch analysisTimer = new Stopwatch();
Stopwatch IRGenTimer    = new Stopwatch();
Stopwatch LLVMTimer     = new Stopwatch();

#region  Startup

preloadTimer.Start();

const string OUTPUT_PATH  = "Output/";
const string DEBUG_PATH  = "Debug/";

string PRELOAD_PATH_REL = Path.Combine(AppContext.BaseDirectory, "__preload.tia");
string OUTPUT_PATH_REL  = Path.Combine(AppContext.BaseDirectory, OUTPUT_PATH);
string DEBUG_PATH_REL   = Path.Combine(AppContext.BaseDirectory, DEBUG_PATH);

Directory.CreateDirectory(Path.GetDirectoryName(OUTPUT_PATH_REL)!);
Directory.CreateDirectory(Path.GetDirectoryName(DEBUG_PATH_REL)!);

if (!File.Exists(PRELOAD_PATH_REL))
    Log.Error(18, "Preload module could not be located");

string preloadCode = File.ReadAllText(PRELOAD_PATH_REL);
Lexer preloadLexer = new Lexer(preloadCode);
List<Token> preloadTokens = [];
Token token;

do
{
    token = preloadLexer.NextToken();
    preloadTokens.Add(token);
    // Log.Info(token.ToString());
    // Log.Info(token.TokenType.ToString());
} while (token.TokenType != TokenType.EOF);

Parser preloadParser = new Parser(preloadTokens);
(ProgramNode preloadAST, Scope preloadScope) = preloadParser.ParseProgram("__preload__");
using StreamWriter writer = new StreamWriter($"{DEBUG_PATH_REL}__preload__.ast");
AstPrinter.Print(preloadAST, writer, false);
using StreamWriter scopeWriter = new StreamWriter($"{DEBUG_PATH_REL}__preload__.scope");
ScopePrinter.Print("__preload", preloadScope, scopeWriter);

preloadTimer.Stop();

Log.Time("Preload took", preloadTimer.Elapsed.TotalMilliseconds, PRELOAD_COL);

#endregion

#region Args

if (args.Length == 0)
{
    Log.Usage();
    return;
}

if (args[0].EndsWith(".tia"))
{
    if (!Path.Exists(args[0]) || !File.Exists(args[0]))
        Log.Error(15, $"File '{args[0]}' could not be found");

    CompileFile(args[0][0..args[0].LastIndexOf('.')]);
}
else    // folder
{
    if (args[0].StartsWith("--test") && args.Length > 1)
    {
        string folder = args[1];
        if (!Directory.Exists(folder))
            Log.Error(15, $"Folder '{folder}' could not be found");

        IOrderedEnumerable<string> allTia = Directory.EnumerateFiles(folder, "*.tia", SearchOption.TopDirectoryOnly)
                                                     .OrderBy(f => f);

        int failures = 0;
        foreach (string tiaFile in allTia)
        {
            try
            {
                CompileFile(tiaFile[0..tiaFile.IndexOf('.')], insertLogs: false, timestamps: false);
            }
            catch (Exception ex)
            {
                failures++;
                Log.Error(17, ex.Message, false);
            }

            Log.Info($"=== Testing {Path.GetFileName(tiaFile)} ===");
        }

        Log.Info($"\n{allTia.Count()} files tested, {failures} failures.");
    }
    else
    {
        Log.Error(16, $"Invalid argument: '{args[0]}'", false);
        Log.Usage();
        return;
    }
}

#endregion

#region  Compilation

int CompileFile(string filePath,
                bool insertLogs = false,
                bool timestamps = true,
                bool writeLexerOutput = false)
{
    string code = File.ReadAllText(filePath + ".tia");

    compileTimer.Start();
    lexTimer.Start();
    Lexer lexer = new Lexer(code);
    List<Token> tokens = [];
    Token token;

    string programName = filePath[(1 + filePath.LastIndexOf('\\'))..];
    string lexPath = $"{DEBUG_PATH_REL}{programName}.lex";
    using StreamWriter lexerWriter = new StreamWriter(lexPath);

    do {
        token = lexer.NextToken();
        tokens.Add(token);

        lexerWriter.Write($"{token.TokenType} ");
        if (token.TokenType == TokenType.Punctuation_Semicolon)
            lexerWriter.WriteLine();

    } while (token.TokenType != TokenType.EOF);

    Log.Link(lexPath, "Lexer tokens: ");

    lexTimer.Stop();
    parseTimer.Start();

    Parser parser = new Parser(tokens);

    (ProgramNode ast, Scope globalScope) = parser.ParseProgram(programName);

    parseTimer.Stop();
    (ast, globalScope) = Elaboration.Lower(preloadScope, preloadAST, globalScope, ast);
    printTimer.Start();

    string writerPath = $"{DEBUG_PATH_REL}{programName}.ast";
    using StreamWriter writer = new StreamWriter(writerPath);
    AstPrinter.Print(ast, writer, timestamps);
    Log.Link(writerPath, "Preload AST: ");

    string sw2Path = $"{DEBUG_PATH_REL}{programName}.scope";
    using StreamWriter sw2 = new StreamWriter(sw2Path);
    ScopePrinter.Print(filePath, globalScope, sw2);
    Log.Link(sw2Path, "Scope Tree: ");

    printTimer.Stop();
    analysisTimer.Start();

    (ast, globalScope) = SemanticAnalyser.AnalyseProgram(ast, globalScope);

    analysisTimer.Stop();
    printTimer.Start();

    string writer2Path = $"{DEBUG_PATH_REL}{programName}_full.ast";
    using StreamWriter writer2 = new StreamWriter($"{DEBUG_PATH_REL}{programName}_full.ast");
    AstPrinter.Print(ast, writer2, timestamps);
    Log.Link(writer2Path, "Full AST: ");

    IRGenTimer.Start();

    string irgenPath = $"{OUTPUT_PATH_REL}{programName}.ll";
    IRGenerator.Emit(ast, globalScope, irgenPath, insertLogs);
    Log.Link(irgenPath, "IR: ");

    IRGenTimer.Stop();
    LLVMTimer.Start();

    ProcessStartInfo psi = new ProcessStartInfo
    {
        FileName = @"C:\Program Files\LLVM\bin\clang.exe",
        Arguments = $"-x ir {OUTPUT_PATH_REL}{programName}.ll -O0 -rtlib=compiler-rt -o {OUTPUT_PATH_REL}{programName}.exe",

        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };

    string asmPath = $"{OUTPUT_PATH_REL}{programName}.s";
    ProcessStartInfo psi2 = new ProcessStartInfo
    {
        FileName = @"C:\Program Files\LLVM\bin\clang.exe",
        Arguments = $"-x ir {OUTPUT_PATH_REL}{programName}.ll -O3 -S -o " + asmPath,

        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };

    Log.Link(asmPath, "ASM: ");


    using (Process process = Process.Start(psi)!)
    {
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(stderr);
    }

    using (Process process2 = Process.Start(psi2)!)
    {
        string stdout2 = process2.StandardOutput.ReadToEnd();
        string stderr2 = process2.StandardError.ReadToEnd();
        process2.WaitForExit();

        if (process2.ExitCode != 0)
            throw new Exception(stderr2);
    }

    LLVMTimer.Stop();
    compileTimer.Stop();

    return 0;
}

#endregion

double lexerTime  = lexTimer.Elapsed.TotalMilliseconds;
double parserTime = parseTimer.Elapsed.TotalMilliseconds;
double semTime    = analysisTimer.Elapsed.TotalMilliseconds;
double IRgenTime  = IRGenTimer.Elapsed.TotalMilliseconds;
double LLVMTime   = LLVMTimer.Elapsed.TotalMilliseconds;

Log.Time("Lexer took", lexerTime, LEXER_COL);
Log.Time("Parser took", parserTime, PARSER_COL);
Log.Time("Semantic Analysis took", semTime, SEM_COL);
Console.WriteLine($"AST printing took {printTimer.ElapsedMilliseconds} ms");
Log.Time("IR Generation took", IRgenTime, IRGEN_COL);
Log.Time("LLVM took", LLVMTime, LLVM_COL);

Console.WriteLine($"All Processes finished in {compileTimer.ElapsedMilliseconds} ms");

double totalTime = lexerTime + parserTime + IRgenTime + LLVMTime;

#region Chart Printing

int lexerChars  = (int)Math.Round(lexerTime / (double)totalTime * BAR_CHARS);
int parserChars = (int)Math.Round(parserTime / (double)totalTime * BAR_CHARS);
int semChars    = (int)Math.Round(semTime / (double)totalTime * BAR_CHARS);
int IRgenChars  = (int)Math.Round(IRgenTime / (double)totalTime * BAR_CHARS);
int llvmChars   = BAR_CHARS - lexerChars - parserChars - IRgenChars;

static void PrintSegment(int count, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.Write(new string('■', count));
    Console.ResetColor();
}

Console.Write("[");
PrintSegment(lexerChars,  LEXER_COL);
PrintSegment(parserChars, PARSER_COL);
PrintSegment(semChars,    SEM_COL);
PrintSegment(IRgenChars,  IRGEN_COL);
PrintSegment(llvmChars,   LLVM_COL);
Console.WriteLine("]");

#endregion

#region Log

public static class Log
{
    public static void Error(uint code, string message, bool exit = true)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write($"Error #{code}: ");
        Console.ResetColor();
        StackTrace stackTrace = new StackTrace();
        throw new Exception(message);
    }

    public static void Info(string text)
    {
        Console.WriteLine(text);
    }

    public static void Time(string text, double time, ConsoleColor highlight = ConsoleColor.White, float linesPerS = 0)
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

    public static void Link(string path, string label = "")
    {
        Uri uri = new Uri(path);
        Console.WriteLine(label + uri.AbsoluteUri);
    }

    public static void Usage()
    {
        Info("Usage: ");
        Info("  <path>.tia                       compile the specified file");
        Info("  --test <path>                    test all files in the specified folder");
    }

}

#endregion
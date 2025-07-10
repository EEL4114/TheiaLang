using TheiaLang;

class Program
{
    static void Main()
    {
        string code = File.ReadAllText("Example.tia");
        Lexer lexer = new Lexer(code);

        List<Token> tokens = new List<Token>();
        Token token;

        do
        {
            token = lexer.NextToken();
            tokens.Add(token);
            Console.WriteLine(token);
        } while (token.Type != TokenType.EOF);

        Parser parser = new Parser(tokens);
        ProgramNode ast = parser.ParseProgram();

        using StreamWriter writer = new StreamWriter("ast.txt");
        AstPrinter.Print(ast, writer);

        Console.WriteLine("Parsed OK!");
    }
}
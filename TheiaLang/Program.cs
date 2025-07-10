using TheiaLang;

class Program
{
    static void Main()
    {
        string code = File.ReadAllText("Example.tia");
        Lexer lexer = new Lexer(code);

        Token token;

        do
        {
            token = lexer.NextToken();
            Console.WriteLine(token);
        } while (token.Type != TokenType.EOF);
    }
}
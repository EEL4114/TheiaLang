using System.Text;

namespace TheiaLang;

public enum TokenType
{
    Identifier = 000,

    Literal_bool = 100,
    Literal_s32 = 102,
    Literal_f32 = 103,

    Keyword_bool = 200,
    Keyword_int = 201,
    Keyword_float = 202,

    Keyword_return = 300,

    Operator_Plus = 400,
    Operator_Equals = 401,
    Operator_Mult = 402,
    Operator_Greater = 403,

    Punctuation_Dot = 500,
    Punctuation_Semicolon = 501,
    Punctuation_ParenthesisL = 502,
    Punctuation_ParenthesisR = 503,
    Punctuation_BraceL = 504,
    Punctuation_BraceR = 505,

    EOF = 42069,
}

public sealed record Token(
    TokenType Type,
    string Lexeme,
    int Line,
    int Column);


class Lexer(string sourceCode)
{
    readonly string source = sourceCode;
    int pos, line, col;

    public Token NextToken()
    {
        SkipWhiteSpaceAndComments();

        if (IsAtEnd())
            return new Token(TokenType.EOF, "", line, col);

        char c = Advance();

        //  single char punctuation
        switch (c)
        {
            case ';': return MakeToken(TokenType.Punctuation_Semicolon, ";");
            case '(': return MakeToken(TokenType.Punctuation_ParenthesisL, "(");
            case ')': return MakeToken(TokenType.Punctuation_ParenthesisR, ")");
            case '{': return MakeToken(TokenType.Punctuation_BraceL, "{");
            case '}': return MakeToken(TokenType.Punctuation_BraceR, "}");
            case '+': return MakeToken(TokenType.Operator_Plus, "+");
            case '*': return MakeToken(TokenType.Operator_Mult, "*");
            case '=': return MakeToken(TokenType.Operator_Equals, "=");
            case '>': return MakeToken(TokenType.Operator_Greater, ">");
        }

        if (char.IsDigit(c))
            return ReadNumber(c);

        if (char.IsLetter(c) || c == '_')
            return ReadIdentifierOrKeyword(c);

        throw new Exception($"Unexpected character '{c}' at {line}:{col}");
    }

    private Token ReadNumber(char first)
    {
        int startCol = col - 1;
        StringBuilder sb = new StringBuilder().Append(first);

        while (!IsAtEnd() && char.IsDigit(Peek()))
            sb.Append(Advance());

        // optional fraction
        if (Peek() == '.')
        {
            sb.Append(Advance());
            while (!IsAtEnd() && char.IsDigit(Peek()))
                sb.Append(Advance());

            return new Token(TokenType.Literal_f32, sb.ToString(), line, startCol);
        }

        return new Token(TokenType.Literal_s32, sb.ToString(), line, startCol);
    }

    private Token ReadIdentifierOrKeyword(char first)
    {
        int startCol = col - 1;
        StringBuilder sb = new StringBuilder().Append(first);
        while (Peek() is char ch && (char.IsLetterOrDigit(ch) || ch == '_'))
            sb.Append(Advance());

        var lex = sb.ToString();
        return lex switch
        {
            "int" => new Token(TokenType.Keyword_int, lex, line, startCol),
            "float" => new Token(TokenType.Keyword_float, lex, line, startCol),
            "bool" => new Token(TokenType.Keyword_bool, lex, line, startCol),
            "return" => new Token(TokenType.Keyword_return, lex, line, startCol),
            "true" => new Token(TokenType.Literal_bool, lex, line, startCol),
            "false" => new Token(TokenType.Literal_bool, lex, line, startCol),
            _ => new Token(TokenType.Identifier, lex, line, startCol),
        };
    }

    private Token MakeToken(TokenType type, string lexeme)
    => new Token(type, lexeme, line, col - lexeme.Length);

    void SkipWhiteSpaceAndComments()
    {
        while (!IsAtEnd())
        {
            char c = Peek();

            if (c == ' ' || c == '\t')
            {
                Advance();
                continue;
            }

            if (c == '\n')
            {
                Advance();    // consume '\n'
                line++;
                col = 1;     // reset column at new line
                continue;
            }

            if (c == '\r' && PeekNext() == '\n')
            {
                Advance();    // consume '\r'
                Advance();    // consume '\n'
                line++;
                col = 1;
                continue;
            }

            break;
        }
    }

    private char Peek() => pos < source?.Length ? source[pos] : '\0';
    private char PeekNext() => pos + 1 < source.Length ? source[pos + 1] : '\0';

    private char Advance()
    {
        char c = source[pos++];
        col++;
        return c;
    }

    private bool IsAtEnd() => pos >= source.Length;
}
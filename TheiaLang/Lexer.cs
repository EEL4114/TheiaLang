using System.Text;

namespace TheiaLang;

public enum TokenType
{
    Identifier,

    Literal_bool,
    Literal_s32,
    Literal_f32,

    Keyword_bool,
    Keyword_s32,
    Keyword_f32,
    Keyword_struct,
    Keyword_union,

    Keyword_new,
    Keyword_return,

    Operator_Equals,
    Operator_Plus,
    Operator_Minus,
    Operator_Mult,
    Operator_Div,
    Operator_Greater,
    Operator_Less,

    Punctuation_Comma,
    Punctuation_Dot,
    Punctuation_Semicolon,
    Punctuation_ParenthesisL,
    Punctuation_ParenthesisR,
    Punctuation_BraceL,
    Punctuation_BraceR,

    EOF
}

public sealed record Token(
    TokenType TokenType,
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
            case ',': return MakeToken(TokenType.Punctuation_Comma, ",");
            case '.': return MakeToken(TokenType.Punctuation_Dot, ".");
            case ';': return MakeToken(TokenType.Punctuation_Semicolon, ";");
            case '(': return MakeToken(TokenType.Punctuation_ParenthesisL, "(");
            case ')': return MakeToken(TokenType.Punctuation_ParenthesisR, ")");
            case '{': return MakeToken(TokenType.Punctuation_BraceL, "{");
            case '}': return MakeToken(TokenType.Punctuation_BraceR, "}");
            case '+': return MakeToken(TokenType.Operator_Plus, "+");
            case '-': return MakeToken(TokenType.Operator_Minus, "-");
            case '*': return MakeToken(TokenType.Operator_Mult, "*");
            case '=': return MakeToken(TokenType.Operator_Equals, "=");
            case '>': return MakeToken(TokenType.Operator_Greater, ">");
        }

        if (char.IsDigit(c))
            return ReadNumber(c);

        if (char.IsLetter(c) || c == '_')
            return ReadIdentifierOrKeyword(c);

        Log.Error(0, $"Unexpected character '{c}' at {line + 1}:{col}");
        return null;
    }

    Token ReadNumber(char first)
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

    Token ReadIdentifierOrKeyword(char first)
    {
        int startCol = col - 1;
        StringBuilder sb = new StringBuilder().Append(first);
        while (Peek() is char ch && (char.IsLetterOrDigit(ch) || ch == '_'))
            sb.Append(Advance());

        string lexeme = sb.ToString();
        return lexeme switch
        {
            "int" => new Token(TokenType.Keyword_s32, lexeme, line, startCol),
            "s32" => new Token(TokenType.Keyword_s32, lexeme, line, startCol),
            "float" => new Token(TokenType.Keyword_f32, lexeme, line, startCol),
            "f32" => new Token(TokenType.Keyword_f32, lexeme, line, startCol),
            "bool" => new Token(TokenType.Keyword_bool, lexeme, line, startCol),
            "return" => new Token(TokenType.Keyword_return, lexeme, line, startCol),
            "true" => new Token(TokenType.Literal_bool, lexeme, line, startCol),
            "false" => new Token(TokenType.Literal_bool, lexeme, line, startCol),
            "struct" => new Token(TokenType.Keyword_struct, lexeme, line, startCol),
            "union" => new Token(TokenType.Keyword_union, lexeme, line, startCol),
            "new" => new Token(TokenType.Keyword_new, lexeme, line, startCol),
            _ => new Token(TokenType.Identifier, lexeme, line, startCol),
        };
    }

    Token MakeToken(TokenType type, string lexeme)
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

    char Peek() => pos < source?.Length ? source[pos] : '\0';
    char PeekNext() => pos + 1 < source.Length ? source[pos + 1] : '\0';

    char Advance()
    {
        char c = source[pos++];
        col++;
        return c;
    }

    bool IsAtEnd() => pos >= source.Length;
}
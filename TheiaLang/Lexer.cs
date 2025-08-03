using System.Text;

namespace TheiaLang;

// TODO handle single char tokens by their ASCII value?
public enum TokenType
{
    Identifier,

    Literal,

    Keyword_bool,

    Keyword_s8,
    Keyword_s16,
    Keyword_s32,
    Keyword_s64,
    Keyword_s128,
    Keyword_s256,

    Keyword_f16,
    Keyword_f32,
    Keyword_f64,
    Keyword_f128,

    Keyword_struct,
    Keyword_union,

    Keyword_if,
    Keyword_else,
    Keyword_for,
    Keyword_new,
    Keyword_return,

    Operator_Equal,
    Operator_Plus,
    Operator_Minus,
    Operator_Mult,
    Operator_Div,
    Operator_Greater,
    Operator_Less,

    Operator_AND,
    Operator_OR,
    Operator_EqualEqual,
    Operator_Inequal,
    Operator_Invert,

    Operator_PlusEqual,
    Operator_MinusEqual,
    Operator_MultEqual,
    Operator_DivEqual,

    Punctuation_At,
    Punctuation_Comma,
    Punctuation_Dot,
    Punctuation_Semicolon,
    Punctuation_ParenthesisL,
    Punctuation_ParenthesisR,
    Punctuation_BraceL,
    Punctuation_BraceR,
    Punctuation_BracketL,
    Punctuation_BracketR,

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
        // single char punctuation
        // TODO: this looks like it could be done in a more compact way
        switch (c)
        {
            case '+':
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.Operator_PlusEqual, "+=");
                }
                return MakeToken(TokenType.Operator_Plus, "+");
            case '-':
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.Operator_MinusEqual, "-=");
                }
                return MakeToken(TokenType.Operator_Minus, "-");
            case '*':
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.Operator_MultEqual, "*=");
                }
                return MakeToken(TokenType.Operator_Mult, "*");
            case '=':
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.Operator_EqualEqual, "==");
                }
                return MakeToken(TokenType.Operator_Equal, "=");
            case '>': return MakeToken(TokenType.Operator_Greater, ">");
            case '<': return MakeToken(TokenType.Operator_Less, "<");
            case '!': return MakeToken(TokenType.Operator_Invert, "!");
            case '@': return MakeToken(TokenType.Punctuation_At, "@");
            case ',': return MakeToken(TokenType.Punctuation_Comma, ",");
            case '.': return MakeToken(TokenType.Punctuation_Dot, ".");
            case ';': return MakeToken(TokenType.Punctuation_Semicolon, ";");
            case '(': return MakeToken(TokenType.Punctuation_ParenthesisL, "(");
            case ')': return MakeToken(TokenType.Punctuation_ParenthesisR, ")");
            case '{': return MakeToken(TokenType.Punctuation_BraceL, "{");
            case '}': return MakeToken(TokenType.Punctuation_BraceR, "}");
            case '[': return MakeToken(TokenType.Punctuation_BracketL, "[");
            case ']': return MakeToken(TokenType.Punctuation_BracketR, "]");
        }

        if (char.IsDigit(c))
            return ReadNumber(c);

        if (char.IsLetter(c) || c == '_')
            return ReadIdentifierOrKeyword(c);

        Token token;

        string s = $"{c}{Peek()}";
        token = s switch
        {
            "==" => MakeToken(TokenType.Operator_EqualEqual, s),
            "!=" => MakeToken(TokenType.Operator_Inequal, s),
            "&&" => MakeToken(TokenType.Operator_AND, s),
            "||" => MakeToken(TokenType.Operator_AND, s),
            _ => throw new NotImplementedException($"Unexpected character '{c}' at {line + 1}:{col - 1}"),
        };

        if (token != null)
        {
            Advance();
            return token;
        }

        Log.Error(0, $"Unexpected character '{c}' at {line + 1}:{col}");
        return null!;
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

            return new Token(TokenType.Literal, sb.ToString(), line, startCol);
        }

        return new Token(TokenType.Literal, sb.ToString(), line, startCol);
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
            "bool" => new Token(TokenType.Keyword_bool, lexeme, line, startCol),

            "s8" => new Token(TokenType.Keyword_s8, lexeme, line, startCol),
            "sbyte" => new Token(TokenType.Keyword_s8, lexeme, line, startCol),
            "s16" => new Token(TokenType.Keyword_s16, lexeme, line, startCol),
            "short" => new Token(TokenType.Keyword_s16, lexeme, line, startCol),
            "s32" => new Token(TokenType.Keyword_s32, lexeme, line, startCol),
            "int" => new Token(TokenType.Keyword_s32, lexeme, line, startCol),
            "s64" => new Token(TokenType.Keyword_s64, lexeme, line, startCol),
            "long" => new Token(TokenType.Keyword_s64, lexeme, line, startCol),
            "s128" => new Token(TokenType.Keyword_s128, lexeme, line, startCol),
            "s256" => new Token(TokenType.Keyword_s256, lexeme, line, startCol),

            "f16" => new Token(TokenType.Keyword_f16, lexeme, line, startCol),
            "half" => new Token(TokenType.Keyword_f16, lexeme, line, startCol),
            "f32" => new Token(TokenType.Keyword_f32, lexeme, line, startCol),
            "float" => new Token(TokenType.Keyword_f32, lexeme, line, startCol),
            "f64" => new Token(TokenType.Keyword_f64, lexeme, line, startCol),
            "double" => new Token(TokenType.Keyword_f64, lexeme, line, startCol),
            "f128" => new Token(TokenType.Keyword_f128, lexeme, line, startCol),

            "struct" => new Token(TokenType.Keyword_struct, lexeme, line, startCol),
            "union" => new Token(TokenType.Keyword_union, lexeme, line, startCol),

            "if" => new Token(TokenType.Keyword_if, lexeme, line, startCol),
            "else" => new Token(TokenType.Keyword_else, lexeme, line, startCol),

            "for" => new Token(TokenType.Keyword_for, lexeme, line, startCol),

            "return" => new Token(TokenType.Keyword_return, lexeme, line, startCol),
            "true" => new Token(TokenType.Literal, lexeme, line, startCol),
            "false" => new Token(TokenType.Literal, lexeme, line, startCol),
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

            if (c == '/' && PeekNext() == '/')  // comments
            {
                Advance();
                Advance();
                while (!IsAtEnd() && Peek() != '\n')
                    Advance();
                continue;
            }

            if (c == '/' && PeekNext() == '*')  // block comments
            {
                Advance();
                Advance();
                int depth = 1;                  // we support nested block comments
                while (depth > 0 && !IsAtEnd())
                {
                    if (Peek() == '/' && PeekNext() == '*')
                    {
                        depth++;
                        Advance(); Advance();
                    }
                    else if (Peek() == '*' && PeekNext() == '/')
                    {
                        depth--;
                        Advance(); Advance();
                    }
                    else
                    {
                        // track newlines inside comments so token positions remain accurate
                        if (Peek() == '\n')
                        {
                            line++;
                            col = 1;
                        }
                        Advance();
                    }
                }
                continue;
            }

            if (c == '\n')
            {
                Advance();      // consume '\n'
                line++;
                col = 1;        // reset column at new line
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
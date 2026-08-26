using System.Text;

namespace TheiaLang;

// TODO handle single char tokens by their ASCII value?
public enum TokenType
{
    Identifier   = 300,

    Literal      = 400,

    Keyword_bool = 500,

    Keyword_void = 501,

    Keyword_s8   = 510,
    Keyword_s16  = 511,
    Keyword_s32  = 512,
    Keyword_s64  = 513,
    Keyword_s128 = 514,
    Keyword_s256 = 515,

    // 52X reserved for signed int

    Keyword_f16  = 530,
    Keyword_f32  = 531,
    Keyword_f64  = 532,
    Keyword_f128 = 533,

    Keyword_struct = 540,
    Keyword_union  = 541,

    Keyword_if     = 542,
    Keyword_else   = 543,
    Keyword_for    = 544,
    Keyword_new    = 545,
    Keyword_return = 546,

    Operator_Equal   = 600,
    Operator_Plus    = 601,
    Operator_Minus   = 602,
    Operator_Mult    = 603,
    Operator_Div     = 604,
    Operator_Greater = 605,
    Operator_Less    = 606,

    Operator_AND        = 610,
    Operator_OR         = 611,
    Operator_EqualEqual = 612,
    Operator_Inequal    = 613,
    Operator_Invert     = 614,

    Operator_PlusEqual  = 620,
    Operator_MinusEqual = 621,
    Operator_MultEqual  = 622,
    Operator_DivEqual   = 623,

    Punctuation_At           = 700,
    Punctuation_Comma        = 701,
    Punctuation_Dot          = 702,
    Punctuation_Semicolon    = 703,
    Punctuation_ParenthesisL = 704,
    Punctuation_ParenthesisR = 705,
    Punctuation_BraceL       = 706,
    Punctuation_BraceR       = 707,
    Punctuation_BracketL     = 708,
    Punctuation_BracketR     = 709,
    Punctuation_Dollar       = 710,

    EOF = 65535
}

public sealed record Token(
    TokenType TokenType,
    string Lexeme,
    SourePosition Pos);

public readonly record struct SourePosition(
    int Row,
    int Column
)
{
    public static SourePosition None => default;
    public override string ToString()
    {
        return $"{Row}:{Column}";
    }
}

class Lexer(string sourceCode)
{
    readonly string source = sourceCode;
    int pos, row = 1, col = 1;

    public Token NextToken()
    {
        SkipWhiteSpaceAndComments();

        if (IsAtEnd())
            return new Token(TokenType.EOF, "", new SourePosition(row, col));

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
            case '/':
                if (Peek() == '=')
                {
                    Advance();
                    return MakeToken(TokenType.Operator_DivEqual, "/=");
                }
                return MakeToken(TokenType.Operator_Div, "/");
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
            case '$': return MakeToken(TokenType.Punctuation_Dollar, "$");
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
            "||" => MakeToken(TokenType.Operator_OR, s),
            _ => throw new NotImplementedException($"Unexpected character '{c}' at {row + 1}:{col - 1}"),
        };

        if (token != null)
        {
            Advance();
            return token;
        }

        Log.Error(0, $"Unexpected character '{c}' at {row + 1}:{col}");
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

            return new Token(TokenType.Literal, sb.ToString(), new SourePosition(row, startCol));
        }

        return new Token(TokenType.Literal, sb.ToString(), new SourePosition(row, startCol));
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
            "bool" => new Token(TokenType.Keyword_bool, lexeme, new SourePosition(row, startCol)),
            "void" => new Token(TokenType.Keyword_void, lexeme, new SourePosition(row, startCol)),

            "s8"    => new Token(TokenType.Keyword_s8, lexeme, new SourePosition(row, startCol)),
            "sbyte" => new Token(TokenType.Keyword_s8, lexeme, new SourePosition(row, startCol)),
            "s16"   => new Token(TokenType.Keyword_s16, lexeme, new SourePosition(row, startCol)),
            "short" => new Token(TokenType.Keyword_s16, lexeme, new SourePosition(row, startCol)),
            "s32"   => new Token(TokenType.Keyword_s32, lexeme, new SourePosition(row, startCol)),
            "int"   => new Token(TokenType.Keyword_s32, lexeme, new SourePosition(row, startCol)),
            "s64"   => new Token(TokenType.Keyword_s64, lexeme, new SourePosition(row, startCol)),
            "long"  => new Token(TokenType.Keyword_s64, lexeme, new SourePosition(row, startCol)),
            "s128"  => new Token(TokenType.Keyword_s128, lexeme, new SourePosition(row, startCol)),
            "s256"  => new Token(TokenType.Keyword_s256, lexeme, new SourePosition(row, startCol)),

            "f16"    => new Token(TokenType.Keyword_f16, lexeme, new SourePosition(row, startCol)),
            "half"   => new Token(TokenType.Keyword_f16, lexeme, new SourePosition(row, startCol)),
            "f32"    => new Token(TokenType.Keyword_f32, lexeme, new SourePosition(row, startCol)),
            "float"  => new Token(TokenType.Keyword_f32, lexeme, new SourePosition(row, startCol)),
            "f64"    => new Token(TokenType.Keyword_f64, lexeme, new SourePosition(row, startCol)),
            "double" => new Token(TokenType.Keyword_f64, lexeme, new SourePosition(row, startCol)),
            "f128"   => new Token(TokenType.Keyword_f128, lexeme, new SourePosition(row, startCol)),

            "struct" => new Token(TokenType.Keyword_struct, lexeme, new SourePosition(row, startCol)),
            "union"  => new Token(TokenType.Keyword_union, lexeme, new SourePosition(row, startCol)),

            "if"   => new Token(TokenType.Keyword_if, lexeme, new SourePosition(row, startCol)),
            "else" => new Token(TokenType.Keyword_else, lexeme, new SourePosition(row, startCol)),

            "for" => new Token(TokenType.Keyword_for, lexeme, new SourePosition(row, startCol)),

            "return" => new Token(TokenType.Keyword_return, lexeme, new SourePosition(row, startCol)),
            "true"   => new Token(TokenType.Literal, lexeme, new SourePosition(row, startCol)),
            "false"  => new Token(TokenType.Literal, lexeme, new SourePosition(row, startCol)),
            "new"    => new Token(TokenType.Keyword_new, lexeme, new SourePosition(row, startCol)),
            
            _ => new Token(TokenType.Identifier, lexeme, new SourePosition(row, startCol)),
        };
    }

    Token MakeToken(TokenType type, string lexeme)
    => new Token(type, lexeme, new SourePosition(row, col - lexeme.Length));

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
                            row++;
                            col = 1;
                        }
                        Advance();
                    }
                }
                continue;
            }

            if (c == '\n')
            {
                Advance();  // consume '\n'
                row++;
                col = 1;    // reset column at new line
                continue;
            }

            if (c == '\r' && PeekNext() == '\n')
            {
                Advance();  // consume '\r'
                Advance();  // consume '\n'
                row++;
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
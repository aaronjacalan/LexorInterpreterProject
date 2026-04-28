// Tokenizes LEXOR expression text into a small token stream.
// This is shared by the expression parser/evaluator.

using System.Globalization;

namespace LexorInterpreter.ProgramCodes
{
    internal enum TokenKind
    {
        // special
        EOF,

        // identifiers + literals
        IDENT, INT_LITERAL, FLOAT_LITERAL, CHAR_LITERAL, BOOL_LITERAL,

        // grouping
        LPAREN, RPAREN,

        // arithmetic
        PLUS, MINUS, STAR, SLASH, PERCENT,

        // comparisons
        GT, LT, GTE, LTE, EQEQ, NEQ,

        // gates
        AND, OR, NOT
    }

    internal sealed record Token(TokenKind Kind, string Lexeme);

    internal sealed class Tokenizer
    {
        private readonly string _src;
        private int _i;

        public Tokenizer(string src) => _src = src;

        public (List<Token>? tokens, string? error) Tokenize(int lineNumber)
        {
            var tokens = new List<Token>();
            while (true)
            {
                SkipWs();
                if (_i >= _src.Length) break;

                char c = _src[_i];
                if (char.IsLetter(c) || c == '_')
                {
                    string ident = ReadWhile(ch => char.IsLetterOrDigit(ch) || ch == '_');
                    string upper = ident.ToUpperInvariant();
                    if (upper is "AND") tokens.Add(new Token(TokenKind.AND, "AND"));
                    else if (upper is "OR") tokens.Add(new Token(TokenKind.OR, "OR"));
                    else if (upper is "NOT") tokens.Add(new Token(TokenKind.NOT, "NOT"));
                    else if (upper is "TRUE" or "FALSE") tokens.Add(new Token(TokenKind.BOOL_LITERAL, upper));
                    else tokens.Add(new Token(TokenKind.IDENT, ident));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    string num = ReadNumber();
                    tokens.Add(num.Contains('.') ? new Token(TokenKind.FLOAT_LITERAL, num) : new Token(TokenKind.INT_LITERAL, num));
                    continue;
                }

                if (c == '"')
                {
                    // BOOL literals may appear as "TRUE"/"FALSE" (common in declarations).
                    _i++; // skip "
                    string inner = ReadUntil('"');
                    if (_i >= _src.Length || _src[_i] != '"')
                        return (null, $"Line {lineNumber}: Unterminated string literal in expression.");
                    _i++; // closing "

                    string upper = inner.ToUpperInvariant();
                    if (upper is "TRUE" or "FALSE")
                        tokens.Add(new Token(TokenKind.BOOL_LITERAL, upper));
                    else
                        return (null, $"Line {lineNumber}: Strings are not valid in expressions (only \"TRUE\"/\"FALSE\" are allowed).");
                    continue;
                }

                if (c == '\'')
                {
                    _i++; // skip '
                    if (_i >= _src.Length) return (null, $"Line {lineNumber}: Unterminated CHAR literal in expression.");
                    char ch = _src[_i++];
                    if (_i >= _src.Length || _src[_i] != '\'')
                        return (null, $"Line {lineNumber}: CHAR literal must be a single character like 'c'.");
                    _i++; // closing '
                    tokens.Add(new Token(TokenKind.CHAR_LITERAL, ch.ToString(CultureInfo.InvariantCulture)));
                    continue;
                }

                switch (c)
                {
                    case '(':
                        _i++; tokens.Add(new Token(TokenKind.LPAREN, "(")); break;
                    case ')':
                        _i++; tokens.Add(new Token(TokenKind.RPAREN, ")")); break;
                    case '+':
                        _i++; tokens.Add(new Token(TokenKind.PLUS, "+")); break;
                    case '-':
                        _i++; tokens.Add(new Token(TokenKind.MINUS, "-")); break;
                    case '*':
                        _i++; tokens.Add(new Token(TokenKind.STAR, "*")); break;
                    case '/':
                        _i++; tokens.Add(new Token(TokenKind.SLASH, "/")); break;
                    case '%':
                        _i++; tokens.Add(new Token(TokenKind.PERCENT, "%")); break;
                    case '>':
                        _i++;
                        if (PeekChar('='))
                        {
                            _i++; tokens.Add(new Token(TokenKind.GTE, ">="));
                        }
                        else tokens.Add(new Token(TokenKind.GT, ">"));
                        break;
                    case '<':
                        _i++;
                        if (PeekChar('='))
                        {
                            _i++; tokens.Add(new Token(TokenKind.LTE, "<="));
                        }
                        else if (PeekChar('>'))
                        {
                            _i++; tokens.Add(new Token(TokenKind.NEQ, "<>"));
                        }
                        else tokens.Add(new Token(TokenKind.LT, "<"));
                        break;
                    case '=':
                        _i++;
                        if (PeekChar('='))
                        {
                            _i++; tokens.Add(new Token(TokenKind.EQEQ, "=="));
                        }
                        else
                        {
                            return (null, $"Line {lineNumber}: Single '=' is not valid inside an expression.");
                        }
                        break;
                    default:
                        return (null, $"Line {lineNumber}: Unexpected character '{c}' in expression.");
                }
            }

            tokens.Add(new Token(TokenKind.EOF, ""));
            return (tokens, null);
        }

        private void SkipWs()
        {
            while (_i < _src.Length && char.IsWhiteSpace(_src[_i])) _i++;
        }

        private bool PeekChar(char expected) => _i < _src.Length && _src[_i] == expected;

        private string ReadWhile(Func<char, bool> pred)
        {
            int start = _i;
            while (_i < _src.Length && pred(_src[_i])) _i++;
            return _src[start.._i];
        }

        private string ReadUntil(char end)
        {
            int start = _i;
            while (_i < _src.Length && _src[_i] != end) _i++;
            return _src[start.._i];
        }

        private string ReadNumber()
        {
            int start = _i;
            bool sawDot = false;
            while (_i < _src.Length)
            {
                char c = _src[_i];
                if (char.IsDigit(c)) { _i++; continue; }
                if (c == '.' && !sawDot) { sawDot = true; _i++; continue; }
                break;
            }
            return _src[start.._i];
        }
    }
}


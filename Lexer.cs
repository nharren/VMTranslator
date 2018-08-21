using System;
using System.Collections.Generic;
using System.IO;

namespace VMTranslator
{
    public class Lexer : IDisposable
    {
        private readonly StreamReader _streamReader;

        public Lexer(StreamReader streamReader)
        {
            _streamReader = streamReader;
        }

        public Token Read()
        {
            var lexeme = ReadLexeme();

            return new Token(GetTokenType(lexeme), lexeme);
        }

        private string ReadLexeme()
        {
            var chars = new List<char>();

            while (true)
            {
                // Check if we've reached the end of the stream.
                if (_streamReader.Peek() == -1)
                {
                    return null;
                }

                var currentChar = (char)_streamReader.Read();

                if (char.IsWhiteSpace(currentChar))
                {
                    if (chars.Count == 0)
                    {
                        // Skip whitespace before the token.
                        continue;
                    }
                    else
                    {
                        // Whitespace after the token indicates the end of the token.
                        break;
                    }
                }

                // Skip comments
                if (currentChar == '/' && _streamReader.Peek() == '/')
                {
                    _streamReader.ReadLine();
                    continue;
                }

                chars.Add(currentChar);
            }

            return new string(chars.ToArray());
        }

        private TokenType GetTokenType(string lexeme)
        {
            switch (lexeme)
            {
                case null:
                    return TokenType.EOF;
                case "pop":
                case "push":
                case "constant":
                case "local":
                case "argument":
                case "this":
                case "that":
                case "pointer":
                case "static":
                case "temp":
                case "add":
                case "sub":
                case "neg":
                case "eq":
                case "gt":
                case "lt":
                case "and":
                case "or":
                case "not":
                case "function":
                case "call":
                case "label":
                case "goto":
                case "if-goto":
                case "return":
                    return TokenType.Keyword;
                default:

                    int integer;

                    if (int.TryParse(lexeme, out integer))
                    {
                        return TokenType.IntegerLiteral;
                    }

                    return TokenType.Identifier;
            }
        }

        public void Dispose()
        {
            _streamReader.Dispose();
        }
    }
}

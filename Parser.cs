using System;
using System.Collections.Generic;
using System.Linq;

namespace VMTranslator
{
    public class Parser : IDisposable
    {
        private readonly Lexer _lexer;
        private static readonly IEnumerable<string> _validMemorySegments = new HashSet<string>()
        {
            "static",
            "this",
            "that",
            "pointer",
            "temp",
            "constant",
            "local",
            "argument"
        };

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
        }

        public Command Read()
        {
            while (true)
            {
                var token = _lexer.Read();

                if (token.Type == TokenType.EOF)
                {
                    return new Command(CommandType.EOF, null);
                }

                switch (token.Value)
                {
                    case "push":
                        return new Command(CommandType.Push, ParseMemorySegment(), ParseIntegerLiteral());
                    case "pop":
                        return new Command(CommandType.Pop, ParseMemorySegment(), ParseIntegerLiteral());
                    case "add":
                    case "sub":
                    case "neg":
                    case "eq":
                    case "gt":
                    case "lt":
                    case "and":
                    case "or":
                    case "not":
                        return new Command(CommandType.Arithmetic, token.Value);
                    case "function":
                        return new Command(CommandType.Function, ParseIdentifier(), ParseIntegerLiteral());
                    case "call":
                        return new Command(CommandType.Call, ParseIdentifier(), ParseIntegerLiteral());
                    case "label":
                        return new Command(CommandType.Label, ParseIdentifier());
                    case "goto":
                        return new Command(CommandType.Goto, ParseIdentifier());
                    case "if-goto":
                        return new Command(CommandType.IfGoto, ParseIdentifier());
                    case "return":
                        return new Command(CommandType.Return, token.Value);
                    default:
                        break;
                }
            }
        }

        private string ParseIdentifier()
        {
            var token = _lexer.Read();

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception($"Unexpected sequence: {token.Value}");
            }

            return token.Value;
        }

        private string ParseMemorySegment()
        {
            var memorySegment = _lexer.Read();
            
            if (!_validMemorySegments.Contains(memorySegment.Value))
            {
                throw new Exception($"Unexpected sequence: {memorySegment.Value}");
            }

            return memorySegment.Value;
        }

        private int ParseIntegerLiteral()
        {
            var token = _lexer.Read();

            if (token.Type != TokenType.IntegerLiteral)
            {
                throw new Exception($"Unexpected sequence: {token.Value}");
            }

            return int.Parse(token.Value);
        }

        public void Dispose()
        {
            _lexer.Dispose();
        }
    }
}

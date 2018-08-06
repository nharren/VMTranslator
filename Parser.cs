using System;

namespace VMTranslator
{
    public class Parser : IDisposable
    {
        private readonly Lexer _lexer;

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
                        return new Command(CommandType.Push, ParseMemorySegment(), ParseValue());
                    case "pop":
                        return new Command(CommandType.Pop, ParseMemorySegment(), ParseValue());
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
                    default:
                        break;
                }
            }
        }

        private string ParseMemorySegment()
        {
            var memorySegment = _lexer.Read();
            ValidateMemorySegment(memorySegment);
            return memorySegment.Value;
        }

        private int ParseValue()
        {
            var literal = _lexer.Read();

            if (literal.Type != TokenType.Literal)
            {
                throw new Exception($"Unexpected sequence: {literal.Value}");
            }

            return int.Parse(literal.Value);
        }

        private void ValidateMemorySegment(Token memorySegment)
        {
            switch (memorySegment.Value)
            {
                case "static":
                case "this":
                case "that":
                case "pointer":
                case "temp":
                case "constant":
                case "local":
                case "argument":
                    return;
                default:
                    throw new Exception($"Unexpected sequence: {memorySegment.Value}");
            }
        }

        public void Dispose()
        {
            _lexer.Dispose();
        }
    }
}

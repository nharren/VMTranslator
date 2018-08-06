namespace VMTranslator
{
    public struct Token
    {
        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; }

        public string Value { get; }
    }
}

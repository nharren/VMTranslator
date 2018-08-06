namespace VMTranslator
{
    public enum CommandType
    {
        EOF,
        Arithmetic,
        Push,
        Pop,
        Label,
        Goto,
        If,
        Function,
        Return,
        Call
    }
}

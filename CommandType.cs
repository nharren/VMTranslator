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
        IfGoto,
        Function,
        Return,
        Call
    }
}

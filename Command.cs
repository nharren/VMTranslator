namespace VMTranslator
{
    public struct Command
    {
        public Command(CommandType type, string arg1, int arg2 = 0)
        {
            Type = type;
            Arg1 = arg1;
            Arg2 = arg2;
        }

        public CommandType Type { get; }

        public string Arg1 { get; }

        public int Arg2 { get; }
    }
}

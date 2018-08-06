using System;
using System.Collections.Generic;

namespace VMTranslator
{
    public class Translator : IDisposable
    {
        private int labelIndex = 0;
        private readonly Parser _parser;
        private readonly string _fileName;
        private readonly Queue<string> _queue = new Queue<string>();
        private bool _isEOF;

        public Translator(Parser parser, string fileName)
        {
            _parser = parser;
            _fileName = fileName;
        }

        public void Dispose()
        {
            _parser.Dispose();
        }

        public string Read()
        {
            while (true)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }

                if (_isEOF)
                {
                    return null;
                }

                WriteCommand(_parser.Read());
            }
        }


        private void WriteCommand(Command command)
        {
            switch (command.Type)
            {
                case CommandType.EOF:
                    _queue.Enqueue($"// EOF");
                    WriteEndLoop();
                    _isEOF = true;
                    break;
                case CommandType.Arithmetic:
                    _queue.Enqueue($"// {command.Arg1}");
                    WriteArithmeticCommand(command);
                    break;
                case CommandType.Push:
                    _queue.Enqueue($"// push {command.Arg1} {command.Arg2}");
                    switch (command.Arg1)
                    {
                        case "constant":
                            WritePushConstantCommand(command);
                            break;
                        case "static":
                            WritePushStaticCommand(command);
                            break;
                        default:
                            WritePushCommand(command);
                            break;
                    }
                    break;
                case CommandType.Pop:
                    _queue.Enqueue($"// pop {command.Arg1} {command.Arg2}");
                    switch (command.Arg1)
                    {
                        case "static":
                            WritePopStaticCommand(command);
                            break;
                        default:
                            WritePopCommand(command);
                            break;
                    }
                    break;
                case CommandType.Label:
                    break;
                case CommandType.Goto:
                    break;
                case CommandType.If:
                    break;
                case CommandType.Function:
                    break;
                case CommandType.Return:
                    break;
                case CommandType.Call:
                    break;
                default:
                    break;
            }
        }

        private void WritePopStaticCommand(Command command)
        {
            PopFromStackToRegister("D");
            WriteInstruction($"@{_fileName}.{command.Arg2}");
            WriteInstruction("M=D");
        }

        private void WritePopCommand(Command command)
        {
            WriteInstruction($"@{TranslateMemorySegment(command.Arg1)}");
            WriteInstruction(command.Arg1 == "temp" || command.Arg1 == "pointer" ? "D=A" : "D=M");
            WriteInstruction($"@{command.Arg2}");
            WriteInstruction("D=D+A");
            WriteInstruction("@5");
            WriteInstruction("M=D");
            PopFromStackToRegister("D");
            WriteInstruction("@5");
            WriteInstruction("A=M");
            WriteInstruction("M=D");
        }

        private void WritePushConstantCommand(Command command)
        {
            WriteInstruction($"@{command.Arg2}");
            WriteInstruction("D=A");
            PushFromRegisterToStack("D");
        }

        private void WritePushStaticCommand(Command command)
        {
            WriteInstruction($"@{_fileName}.{command.Arg2}");
            WriteInstruction("D=M");
            PushFromRegisterToStack("D");
        }

        private void WritePushCommand(Command command)
        {
            WriteInstruction($"@{TranslateMemorySegment(command.Arg1)}");
            WriteInstruction(command.Arg1 == "temp" || command.Arg1 == "pointer" ? "D=A" : "D=M");
            WriteInstruction($"@{command.Arg2}");
            WriteInstruction("A=D+A");
            WriteInstruction("D=M");
            PushFromRegisterToStack("D");
        }

        private void WriteArithmeticCommand(Command command)
        {
            switch (command.Arg1)
            {
                case "add":
                    WriteTwoOperandArithmeticCommand("+");
                    break;
                case "sub":
                    WriteTwoOperandArithmeticCommand("-");
                    break;
                case "neg":
                    WriteOneOperandArithmeticCommand("-");
                    break;
                case "eq":
                    WriteComparisonCommand("JEQ");
                    break;
                case "gt":
                    WriteComparisonCommand("JGT");
                    break;
                case "lt":
                    WriteComparisonCommand("JLT");
                    break;
                case "and":
                    WriteTwoOperandArithmeticCommand("&");
                    break;
                case "or":
                    WriteTwoOperandArithmeticCommand("|");
                    break;
                case "not":
                    WriteOneOperandArithmeticCommand("!");
                    break;
            }
        }

        private void WriteComparisonCommand(string jumpCondition)
        {
            var generatedLabel1 = GenerateLabel();
            var generatedLabel2 = GenerateLabel();

            PopFromStackToRegister("D");
            WriteInstruction("@SP");
            WriteInstruction("A=M-1");
            WriteInstruction("D=M-D");
            WriteInstruction($"@{generatedLabel1}");
            WriteInstruction($"D;{jumpCondition}");
            ReplaceTopOfStackWith("0");
            WriteInstruction($"@{generatedLabel2}");
            WriteInstruction("0;JMP");
            WriteLabel($"({generatedLabel1})");
            ReplaceTopOfStackWith("-1");
            WriteLabel($"({generatedLabel2})");
        }

        private void WriteEndLoop()
        {
            var generatedLabel = GenerateLabel();
            WriteLabel($"({generatedLabel})");
            WriteInstruction($"@{generatedLabel}");
            WriteInstruction("0;JMP");
        }

        private void WriteOneOperandArithmeticCommand(string operation)
        {
            WriteInstruction("@SP");
            WriteInstruction("A=M-1");
            WriteInstruction($"M={operation}M");
        }

        private void WriteTwoOperandArithmeticCommand(string operation)
        {
            PopFromStackToRegister("D");
            WriteInstruction("@SP");
            WriteInstruction("A=M-1");
            WriteInstruction($"M=M{operation}D");
        }

        private void PushFromRegisterToStack(string register)
        {
            WriteInstruction("@SP");
            WriteInstruction("M=M+1");
            WriteInstruction("A=M-1");
            WriteInstruction($"M={register}");
        }

        private void PopFromStackToRegister(string register)
        {
            WriteInstruction("@SP");
            WriteInstruction("M=M-1");
            WriteInstruction("A=M");
            WriteInstruction($"{register}=M");
        }

        private void ReplaceTopOfStackWith(string value)
        {
            WriteInstruction("@SP");
            WriteInstruction("A=M-1");
            WriteInstruction($"M={value}");
        }

        private void WriteInstruction(string instruction)
        {
            _queue.Enqueue($"    {instruction}");
        }

        private string GenerateLabel()
        {
            return $"L{labelIndex++}";
        }

        private void WriteLabel(string label)
        {
            _queue.Enqueue($"{label}");
        }

        private string TranslateMemorySegment(string memorySegment)
        {
            switch (memorySegment)
            {
                case "local":
                    return "LCL";
                case "argument":
                    return "ARG";
                case "this":
                case "pointer":
                    return "THIS";
                case "that":
                    return "THAT";
                case "temp":
                    return "5";
                default:
                    return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace VMTranslator
{
    public class Translator : IDisposable
    {
        private int _labelIndex;
        private int _returnLabelIndex;
        private bool _isEOF;
        private readonly Parser _parser;
        private readonly string _fileName;
        private readonly Queue<string> _queue = new Queue<string>();
        private string _currentFunction;

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

        public void WriteBootstrap()
        {
            _queue.Enqueue("// Auto-generated hack bootstrap");

            WriteSendAddressToDataRegister("256");
            Write("@SP");
            Write("M=D");
            WriteCall("Sys.init", 0);
        }

        private void WriteCommand(Command command)
        {
            switch (command.Type)
            {
                case CommandType.Arithmetic:
                    WriteArithmeticOperation(command.Arg1);
                    break;
                case CommandType.Call:
                    WriteCall(command.Arg1, command.Arg2);
                    break;
                case CommandType.EOF:
                    _isEOF = true;
                    break;
                case CommandType.Function:
                    WriteFunction(command.Arg1, command.Arg2);
                    break;
                case CommandType.Goto:
                    WriteGoto(command.Arg1);
                    break;
                case CommandType.IfGoto:
                    WriteIfGoto(command.Arg1);
                    break;
                case CommandType.Label:
                    WriteLabel(command.Arg1);
                    break;
                case CommandType.Pop:
                    WritePop(command.Arg1, command.Arg2);
                    break;
                case CommandType.Push:
                    WritePush(command.Arg1, command.Arg2);
                    break;
                case CommandType.Return:
                    WriteReturn();
                    break;
            }
        }

        #region Command Writers
        private void WriteArithmeticOperation(string operation)
        {
            _queue.Enqueue($"// {operation}");

            switch (operation)
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

        private void WriteCall(string functionName, int argumentCount)
        {
            _queue.Enqueue($"// call {functionName} {argumentCount}");

            var returnAddress = $"{_fileName}${functionName}$ret.{_returnLabelIndex++}";

            WritePushAddress(returnAddress);
            WritePushAddressMemory("LCL");
            WritePushAddressMemory("ARG");
            WritePushAddressMemory("THIS");
            WritePushAddressMemory("THAT");

            WriteSendAddressMemoryToDataRegister("SP");

            Write("@LCL");
            Write("M=D");
            Write("@5");
            Write("D=D-A");
            Write('@' + argumentCount.ToString());
            Write("D=D-A");
            Write("@ARG");
            Write("M=D");

            // Go to called function
            InternalWriteGoto(functionName);

            // Return address
            InternalWriteLabel(returnAddress);
        }

        private void WriteFunction(string name, int localVariableCount)
        {
            _queue.Enqueue($"// function {name} {localVariableCount}");

            InternalWriteLabel(name);

            for (int i = 0; i < localVariableCount; i++)
            {
                WritePushAddress("0");
            }

            _currentFunction = name;
            _returnLabelIndex = 0;
        }

        private void WriteGoto(string address)
        {
            _queue.Enqueue($"// goto {address}");

            string label;

            if (_currentFunction != null)
            {
                label = _currentFunction + "$" + address;
            }
            else
            {
                label = address;
            }

            InternalWriteGoto(label);
        }

        private void WriteIfGoto(string address)
        {
            _queue.Enqueue($"// if-goto {address}");

            WritePopToDataRegister();

            string label;

            if (_currentFunction != null)
            {
                label = _currentFunction + "$" + address;
            }
            else
            {
                label = address;
            }

            Write($"@{label}");
            Write("D;JNE");
        }

        private void WriteLabel(string name)
        {
            _queue.Enqueue($"// label {name}");

            string label;

            if (_currentFunction != null)
            {
                label = _currentFunction + "$" + name;
            }
            else
            {
                label = name;
            }

            InternalWriteLabel(label);
        }

        private void WritePop(string segment, int index)
        {
            _queue.Enqueue($"// pop {segment} {index}");

            if (segment == "static")
            {
                WritePopToDataRegister();
                Write($"@{_fileName}.{index}");
                Write("M=D");
            }
            else
            {
                // Add index to memory segment address, e.g. @ARG + 2
                Write($"@{TranslateMemorySegment(segment)}");
                Write(segment == "temp" || segment == "pointer" ? "D=A" : "D=M");
                Write($"@{index}");
                Write("D=D+A");
                Write("@R13");
                Write("M=D");

                // Pop from stack into D register
                Write("@SP");
                Write("M=M-1");
                Write("A=M");
                Write("D=M");

                // Go to stored address an put the contents of D there
                Write("@R13");
                Write("A=M");
                Write("M=D");
            }
        }

        private void WritePush(string segment, int index)
        {
            _queue.Enqueue($"// push {segment} {index}");

            if (segment == "constant")
            {
                WriteSendAddressToDataRegister(index.ToString());
                WritePushFromDataRegister();
            }
            else if (segment == "static")
            {
                Write($"@{_fileName}.{index}");
                Write("D=M");
                WritePushFromDataRegister();
            }
            else
            {
                Write($"@{TranslateMemorySegment(segment)}");
                Write(segment == "temp" || segment == "pointer" ? "D=A" : "D=M");
                Write($"@{index}");
                Write("A=D+A");
                Write("D=M");
                WritePushFromDataRegister();
            }
        }

        private void WriteReturn()
        {
            _queue.Enqueue("// return");

            // Store return address in retAddr variable. This is done since, if the function has no arguments, the next command will override the return address.
            WriteSendAddressMemoryToDataRegister("LCL");
            Write("@5");
            Write("A=D-A");
            Write("D=M");
            Write("@retAddr");
            Write("M=D");

            // Push return value
            Write("@SP");
            Write("A=M-1");
            Write("D=M");
            Write("@ARG");
            Write("A=M");
            Write("M=D");

            // Reposition stack pointer to the return address
            Write("D=A+1");
            Write("@SP");
            Write("M=D");

            // Restore THAT
            WriteSendAddressMemoryToDataRegister("LCL");
            Write("@1");
            Write("A=D-A");
            Write("D=M");
            Write("@THAT");
            Write("M=D");

            // Restore THIS
            WriteSendAddressMemoryToDataRegister("LCL");
            Write("@2");
            Write("A=D-A");
            Write("D=M");
            Write("@THIS");
            Write("M=D");

            // Restore ARG
            WriteSendAddressMemoryToDataRegister("LCL");
            Write("@3");
            Write("A=D-A");
            Write("D=M");
            Write("@ARG");
            Write("M=D");

            // Restore LCL. This must be done last, as the previous restores rely on the LCL address of the returning function.
            WriteSendAddressMemoryToDataRegister("LCL");
            Write("@4");
            Write("A=D-A");
            Write("D=M");
            Write("@LCL");
            Write("M=D");

            // Jump to return address
            Write("@retAddr");
            Write("A=M");
            Write("0;JMP");
        }
        #endregion

        #region Helpers
        private void WriteSendAddressToDataRegister(string address)
        {
            Write('@' + address);
            Write("D=A");
        }

        private void WriteSendAddressMemoryToDataRegister(string address)
        {
            Write('@' + address);
            Write("D=M");
        }

        private void WritePushFromDataRegister()
        {
            Write("@SP");
            Write("M=M+1");
            Write("A=M-1");
            Write("M=D");
        }

        private void WritePopToDataRegister()
        {
            Write("@SP");
            Write("M=M-1");
            Write("A=M");
            Write("D=M");
        }

        private void WritePushAddress(string address)
        {
            WritePushAddressRegister(address, 'A');
        }

        private void WritePushAddressMemory(string address)
        {
            WritePushAddressRegister(address, 'M');
        }

        private void WritePushAddressRegister(string address, char register)
        {
            Write('@' + address);
            Write($"D={register}");
            WritePushFromDataRegister();
        }

        private void WriteComparisonCommand(string jumpCondition)
        {
            var generatedLabel1 = $"{_fileName}.l.{_labelIndex++}";
            var generatedLabel2 = $"{_fileName}.l.{_labelIndex++}";

            WritePopToDataRegister();
            WriteAddressTopOfStack();
            Write("D=M-D");
            Write('@' + generatedLabel1);
            Write($"D;{jumpCondition}");
            WriteAddressTopOfStack();
            Write("M=0");
            InternalWriteGoto(generatedLabel2);
            InternalWriteLabel(generatedLabel1);
            WriteAddressTopOfStack();
            Write("M=-1");
            InternalWriteLabel(generatedLabel2);
        }

        private void WriteAddressTopOfStack()
        {
            Write("@SP");
            Write("A=M-1");
        }

        private void WriteOneOperandArithmeticCommand(string operation)
        {
            Write("@SP");
            Write("A=M-1");
            Write($"M={operation}M");
        }

        private void WriteTwoOperandArithmeticCommand(string operation)
        {
            WritePopToDataRegister();
            Write("@SP");
            Write("A=M-1");
            Write($"M=M{operation}D");
        }

        private void InternalWriteLabel(string label)
        {
            _queue.Enqueue($"({label})");
        }

        private void InternalWriteGoto(string address)
        {
            Write('@' + address);
            Write("0;JMP");
        }

        private void Write(string instruction)
        {
            _queue.Enqueue($"    {instruction}");
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
        #endregion
    }
}

using System;
using System.IO;

namespace VMTranslator
{
    class VMTranslator
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("File not provided.");
            }

            for (int i = 0; i < args.Length; i++)
            {
                using (var stream = new FileStream(args[i], FileMode.Open))
                using (var streamReader = new StreamReader(stream))
                using (var lexer = new Lexer(streamReader))
                using (var parser = new Parser(lexer))
                using (var translator = new Translator(parser, Path.GetFileNameWithoutExtension(args[i])))
                using (var streamWriter = new StreamWriter(Path.ChangeExtension(args[i], ".asm")))
                {
                    string assemblyInstruction = null;

                    while ((assemblyInstruction = translator.Read()) != null)
                    {
                        streamWriter.WriteLine(assemblyInstruction);
                    }

                    Console.WriteLine($"Successfully processed '{Path.GetFileName(args[i])}'.");
                }
            }

            Console.WriteLine("Finished.");
            Console.ReadKey();
        }
    }
}

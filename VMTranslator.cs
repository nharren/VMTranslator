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
                Console.WriteLine("Directory or file not provided.");
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (Directory.Exists(args[i])) // Returns false if an error occurs
                {
                    ProcessDirectory(args[i]);
                }
                else if (File.Exists(args[i])) // Returns false if an error occurs
                {
                    ProcessFile(args[i], Path.ChangeExtension(args[i], ".asm"));
                }
                else
                {
                    throw new Exception($"File or directory not found: {args[i]}");
                }
            }

            Console.WriteLine("Finished.");
        }

        private static void ProcessFile(string filePath, string outputFilePath, bool writeBootstrap = false)
        {
            using (var stream = new FileStream(filePath, FileMode.Open))
            using (var streamReader = new StreamReader(stream))
            using (var lexer = new Lexer(streamReader))
            using (var parser = new Parser(lexer))
            using (var translator = new Translator(parser, Path.GetFileNameWithoutExtension(filePath)))
            using (var streamWriter = new StreamWriter(outputFilePath, append: true))
            {
                if (writeBootstrap)
                {
                    translator.WriteBootstrap();
                }

                string assemblyInstruction = null;

                while ((assemblyInstruction = translator.Read()) != null)
                {
                    streamWriter.WriteLine(assemblyInstruction);
                }

                Console.WriteLine($"Successfully translated '{Path.GetFileName(filePath)}'.");
            }
        }

        private static void ProcessDirectory(string directoryPath)
        {
            var outputFilePath = $@"{directoryPath}\{Path.GetFileName(directoryPath)}.asm";

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            var directoryFiles = Directory.GetFiles(directoryPath);
            var isBootstrapped = false;

            for (int i = 0; i < directoryFiles.Length; i++)
            {
                if (string.Equals(".vm", Path.GetExtension(directoryFiles[i]), StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessFile(directoryFiles[i], outputFilePath, !isBootstrapped);
                    isBootstrapped = true;
                }
            }
        }
    }
}

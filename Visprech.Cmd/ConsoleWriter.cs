using Visprech.Core.Interfaces;

namespace Visprech.Cmd
{
    internal class ConsoleWriter : IMessageWriter
    {
        public void Write(string text)
        {
            Console.WriteLine(text);
        }

        public void WriteEmptyLine()
        {
            Console.WriteLine();
        }

        public void WriteFailure(string text)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);
            Console.ResetColor(); 
            Console.WriteLine();
        }

        public void WriteHeader(string text)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public void WriteInternalError(string text)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public void WriteMainNotyfication(string text)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
        }

        public void WriteNotyfication(string text)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public void WriteSuccess(string text)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public void WriteWarn(string text)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();            
        }
    }
}

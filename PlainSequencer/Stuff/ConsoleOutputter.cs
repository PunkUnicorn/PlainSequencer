using PlainSequencer.Stuff.Interfaces;
using System;

namespace PlainSequencer.Stuff
{
    public class ConsoleOutputter : IConsoleOutputter
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
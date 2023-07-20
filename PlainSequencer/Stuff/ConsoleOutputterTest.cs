using PlainSequencer.Stuff.Interfaces;
using System;
using System.Text;

namespace PlainSequencer.Stuff
{
    public class ConsoleOutputterTest : IConsoleOutputter
    {
        private StringBuilder output = new StringBuilder();
        public string Output => output.ToString();

        public void WriteLine(string message)
        {
            output.AppendLine(message);
        }
    }
}
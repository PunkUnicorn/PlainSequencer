using PlainSequencer.Stuff.Interfaces;
using System;
using System.Text;

namespace PlainSequencer.Stuff
{
    public class ConsoleOutputterTest : IConsoleOutputter
    {
        private StringBuilder output = new StringBuilder();
        private StringBuilder error = new StringBuilder();

        public string Output => output.ToString();

        public string Error => error.ToString();

        public void WriteLine(string message)
        {
            output.AppendLine(message);
        }

        public void ErrorLine(string message)
        {
            error.AppendLine(message);
        }
    }
}
namespace PlainSequencer.Stuff.Interfaces
{
    public interface IConsoleOutputter
    {
        void WriteLine(string message);
        void ErrorLine(string message);
    }
}
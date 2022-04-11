using PlainSequencer.SequenceItemActions;
using System;

namespace PlainSequencer.Logging
{
    public class ProgressLogger : IProgressLogger
    {
        public void Error(string errorDetail)
        {
            Console.WriteLine(errorDetail);
        }

        public void Progress(ISequenceItemAction sequenceItemCheck, string message)
        {
            Console.WriteLine(message);
        }
    }
}

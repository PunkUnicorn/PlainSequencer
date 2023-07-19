using PlainSequencer.SequenceItemActions;
using System;

namespace PlainSequencer.Logging
{
    public interface ISequenceLogger
    {
        void Starting(SequenceItemAbstract sequenceItemAbstract);
        void Progress(SequenceItemAbstract sequenceItemCheck, string v, SequenceProgressLogLevel level);
        void DataOutProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level);
        void DataInProgress(SequenceItemAbstract item, string message, SequenceProgressLogLevel level);
        void Fail(SequenceItemAbstract item, string message);
        void Fail(SequenceItemAbstract item, Exception message);
        void Finished(SequenceItemAbstract sequenceItemAbstract);
        string GetSequenceDiagramNotation(string title, SequenceProgressLogLevel level = SequenceProgressLogLevel.Diagnostic);
    }
}
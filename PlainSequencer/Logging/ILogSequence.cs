using PlainSequencer.SequenceItemActions;
using System;

namespace PlainSequencer.Logging
{
    public interface ILogSequence
    {
        void StartItem(ISequenceItemAction item);
        void Progress(ISequenceItemAction sequenceItemCheck, string v, SequenceProgressLogLevel level);
        void DataOutProgress(ISequenceItemAction item, string message, SequenceProgressLogLevel level);
        void DataInProgress(ISequenceItemAction item, string message, SequenceProgressLogLevel level);
        void Fail(ISequenceItemAction item, string message);
        void Fail(ISequenceItemAction item, Exception message);
        void FinishedItem(ISequenceItemAction sequenceItemAbstract);
        void SequenceComplete(bool isSuccess, object model);
        string GetSequenceDiagramNotation(string title, SequenceProgressLogLevel level = SequenceProgressLogLevel.Diagnostic);
    }
}
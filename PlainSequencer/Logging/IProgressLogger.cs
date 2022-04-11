using PlainSequencer.SequenceItemActions;

namespace PlainSequencer.Logging
{
    public interface IProgressLogger
    {
        //start

        //info

        //end
        void Progress(ISequenceItemAction sequenceItemCheck, string v);
        void Error(string errorDetail);
    }
}
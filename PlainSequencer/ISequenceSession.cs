using PlainSequencer.Script;
using PlainSequencer.SequenceItemActions;

namespace PlainSequencer
{
    public interface ISequenceSession
    {
        string RunId { get; }
        ISequenceItemActionHierarchy Top { get; }
        int UniqueNo { get; }
        SequenceScript Script { get; }
    }
}

using PlainSequencer.Script;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemAction
    {
        SequenceItem SequenceItem { get; }

        IEnumerable<string> Compile(SequenceItem sequenceItem);
    }
}
using PlainSequencer.Script;
using System.Collections.Generic;

namespace PlainSequencer.SequenceItemActions
{
    public interface ISequenceItemActionHierarchy
    {
        SequenceItem SequenceItem { get; }

        ISequenceItemActionHierarchy Parent { get; set; }

        List<ISequenceItemActionHierarchy> Children { get; }

        SequenceItem[] NextSequenceItems { get; }
    }
}